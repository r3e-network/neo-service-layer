using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace NeoServiceLayer.Infrastructure.Security.Authentication
{
    /// <summary>
    /// Implementation of JWT token service for token generation and validation.
    /// </summary>
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtTokenService> _logger;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _tokenHandler = new JwtSecurityTokenHandler();

            var jwtSettings = configuration.GetSection("JwtSettings");
            _secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured");
            _issuer = jwtSettings["Issuer"] ?? "NeoServiceLayer";
            _audience = jwtSettings["Audience"] ?? "NeoServiceLayerServices";
            _expirationMinutes = jwtSettings.GetValue<int>("ExpirationMinutes", 60);
        }

        public string GenerateToken(string userId, string username, IEnumerable<string> roles, Dictionary<string, string>? additionalClaims = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Add roles
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            // Add additional claims
            if (additionalClaims != null)
            {
                claims.AddRange(additionalClaims.Select(kvp => new Claim(kvp.Key, kvp.Value)));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_expirationMinutes);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = _tokenHandler.WriteToken(token);
            
            _logger.LogInformation("Generated JWT token for user {Username} with ID {UserId}", username, userId);
            
            return tokenString;
        }

        public string GenerateServiceToken(string serviceName, string serviceId)
        {
            var claims = new List<Claim>
            {
                new Claim("service", "true"),
                new Claim("service_name", serviceName),
                new Claim("service_id", serviceId),
                new Claim(ClaimTypes.Name, serviceName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(24); // Service tokens have longer expiration

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = _tokenHandler.WriteToken(token);
            
            _logger.LogInformation("Generated JWT token for service {ServiceName} with ID {ServiceId}", serviceName, serviceId);
            
            return tokenString;
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
                
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                
                _logger.LogDebug("Successfully validated JWT token");
                
                return principal;
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning("JWT token validation failed: Token expired");
                return null;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("JWT token validation failed: {Message}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during JWT token validation");
                return null;
            }
        }

        public DateTime? GetTokenExpiration(string token)
        {
            try
            {
                var jwtToken = _tokenHandler.ReadJwtToken(token);
                return jwtToken.ValidTo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read token expiration");
                return null;
            }
        }
    }
}