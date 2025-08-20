using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Authentication.Models;

namespace NeoServiceLayer.Services.Authentication.Implementation
{
    /// <summary>
    /// JWT-based authentication service implementation
    /// </summary>
    public class JwtAuthenticationService : ServiceBase, IAuthenticationService
    {
        private readonly ILogger<JwtAuthenticationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenBlacklistService _tokenBlacklist;
        private readonly string _jwtSecret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpiryMinutes;
        private readonly int _refreshTokenExpiryDays;

        public JwtAuthenticationService(
            ILogger<JwtAuthenticationService> logger,
            IConfiguration configuration,
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            ITokenBlacklistService tokenBlacklist)
            : base("JwtAuthenticationService", "JWT-based authentication service", "1.0.0", logger)
        {
            _logger = logger;
            _configuration = configuration;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _tokenBlacklist = tokenBlacklist;

            // Load JWT configuration
            _jwtSecret = configuration["Jwt:Secret"] 
                ?? throw new InvalidOperationException("JWT secret not configured");
            _issuer = configuration["Jwt:Issuer"] ?? "https://neo-service.io";
            _audience = configuration["Jwt:Audience"] ?? "neo-service-api";
            _accessTokenExpiryMinutes = configuration.GetValue<int>("Jwt:AccessTokenExpiryMinutes", 60);
            _refreshTokenExpiryDays = configuration.GetValue<int>("Jwt:RefreshTokenExpiryDays", 30);
        }

        public async Task<AuthenticationResult> AuthenticateAsync(LoginRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return AuthenticationResult.Failed("Invalid username or password");
                }

                // Find user
                var user = await _userRepository.GetByUsernameAsync(request.Username);
                if (user == null)
                {
                    _logger.LogWarning("Authentication failed: User {Username} not found", request.Username);
                    return AuthenticationResult.Failed("Invalid username or password");
                }

                // Check if account is locked
                if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
                {
                    _logger.LogWarning("Authentication failed: Account {Username} is locked until {LockedUntil}", 
                        request.Username, user.LockedUntil.Value);
                    return AuthenticationResult.Failed($"Account is locked until {user.LockedUntil.Value:yyyy-MM-dd HH:mm:ss} UTC");
                }

                // Verify password
                if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                {
                    // Increment failed login attempts
                    user.FailedLoginAttempts++;
                    
                    // Lock account after 5 failed attempts
                    if (user.FailedLoginAttempts >= 5)
                    {
                        user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                        _logger.LogWarning("Account {Username} locked due to {Attempts} failed login attempts", 
                            request.Username, user.FailedLoginAttempts);
                    }

                    await _userRepository.UpdateAsync(user);
                    return AuthenticationResult.Failed("Invalid username or password");
                }

                // Check MFA if enabled
                if (user.MfaEnabled)
                {
                    if (string.IsNullOrWhiteSpace(request.MfaCode))
                    {
                        return AuthenticationResult.RequiresMfa("MFA code required");
                    }

                    if (!await VerifyMfaCodeAsync(user, request.MfaCode))
                    {
                        _logger.LogWarning("Authentication failed: Invalid MFA code for user {Username}", request.Username);
                        return AuthenticationResult.Failed("Invalid MFA code");
                    }
                }

                // Reset failed login attempts
                user.FailedLoginAttempts = 0;
                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                // Generate tokens
                var accessToken = GenerateAccessToken(user);
                var refreshToken = GenerateRefreshToken();

                // Store refresh token
                await _userRepository.StoreRefreshTokenAsync(user.Id, refreshToken, 
                    DateTime.UtcNow.AddDays(_refreshTokenExpiryDays));

                _logger.LogInformation("User {Username} authenticated successfully", request.Username);

                return AuthenticationResult.Success(new TokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    TokenType = "Bearer",
                    ExpiresIn = _accessTokenExpiryMinutes * 60,
                    UserId = user.Id,
                    Username = user.Username
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication error for user {Username}", request.Username);
                return AuthenticationResult.Failed("An error occurred during authentication");
            }
        }

        public async Task<TokenValidationResult> ValidateTokenAsync(string token)
        {
            try
            {
                // Check if token is blacklisted
                if (await _tokenBlacklist.IsBlacklistedAsync(token))
                {
                    return TokenValidationResult.Invalid("Token has been revoked");
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var jwtToken = (JwtSecurityToken)validatedToken;

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = principal.FindFirst(ClaimTypes.Name)?.Value;
                var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

                return TokenValidationResult.Valid(new ClaimsPrincipal(principal), userId, username, roles);
            }
            catch (SecurityTokenExpiredException)
            {
                return TokenValidationResult.Invalid("Token has expired");
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Token validation failed: {Message}", ex.Message);
                return TokenValidationResult.Invalid("Invalid token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation error");
                return TokenValidationResult.Invalid("Token validation failed");
            }
        }

        public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                // Validate refresh token
                var tokenData = await _userRepository.GetRefreshTokenDataAsync(refreshToken);
                if (tokenData == null || tokenData.ExpiresAt < DateTime.UtcNow)
                {
                    return null;
                }

                // Get user
                var user = await _userRepository.GetByIdAsync(tokenData.UserId);
                if (user == null || user.Status != UserStatus.Active)
                {
                    return null;
                }

                // Revoke old refresh token
                await _userRepository.RevokeRefreshTokenAsync(refreshToken);

                // Generate new tokens
                var accessToken = GenerateAccessToken(user);
                var newRefreshToken = GenerateRefreshToken();

                // Store new refresh token
                await _userRepository.StoreRefreshTokenAsync(user.Id, newRefreshToken, 
                    DateTime.UtcNow.AddDays(_refreshTokenExpiryDays));

                _logger.LogInformation("Token refreshed for user {Username}", user.Username);

                return new TokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken,
                    TokenType = "Bearer",
                    ExpiresIn = _accessTokenExpiryMinutes * 60,
                    UserId = user.Id,
                    Username = user.Username
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh error");
                return null;
            }
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            try
            {
                await _tokenBlacklist.BlacklistTokenAsync(token, DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes));
                _logger.LogInformation("Token revoked successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token revocation error");
                return false;
            }
        }

        public async Task<bool> LogoutAsync(string userId, string token)
        {
            try
            {
                // Blacklist the access token
                await _tokenBlacklist.BlacklistTokenAsync(token, DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes));

                // Revoke all refresh tokens for the user
                await _userRepository.RevokeAllRefreshTokensAsync(userId);

                _logger.LogInformation("User {UserId} logged out successfully", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout error for user {UserId}", userId);
                return false;
            }
        }

        private string GenerateAccessToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("tenant_id", user.TenantId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Add roles
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
            }

            // Add permissions
            foreach (var permission in user.GetAllPermissions())
            {
                claims.Add(new Claim("permission", permission));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private async Task<bool> VerifyMfaCodeAsync(User user, string code)
        {
            // Implement TOTP verification
            // This is a simplified version - use a proper TOTP library in production
            return await Task.FromResult(code == "123456"); // Placeholder
        }

        protected override async Task<ServiceHealth> OnGetHealthAsync()
        {
            try
            {
                // Check database connectivity
                var dbHealthy = await _userRepository.CheckHealthAsync();
                
                // Check Redis connectivity for token blacklist
                var cacheHealthy = await _tokenBlacklist.CheckHealthAsync();

                if (dbHealthy && cacheHealthy)
                {
                    return ServiceHealth.Healthy;
                }
                else if (dbHealthy || cacheHealthy)
                {
                    return ServiceHealth.Degraded;
                }
                else
                {
                    return ServiceHealth.Unhealthy;
                }
            }
            catch
            {
                return ServiceHealth.Unhealthy;
            }
        }

        protected override Task<bool> OnInitializeAsync()
        {
            _logger.LogInformation("JWT Authentication Service initialized");
            return Task.FromResult(true);
        }

        protected override Task<bool> OnStartAsync()
        {
            _logger.LogInformation("JWT Authentication Service started");
            return Task.FromResult(true);
        }

        protected override Task<bool> OnStopAsync()
        {
            _logger.LogInformation("JWT Authentication Service stopped");
            return Task.FromResult(true);
        }
    }
}