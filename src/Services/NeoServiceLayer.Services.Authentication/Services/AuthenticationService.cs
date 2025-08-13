using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NeoServiceLayer.Services.Authentication.Infrastructure;
using NeoServiceLayer.Services.Authentication.Queries;
using OtpNet;
using QRCoder;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NeoServiceLayer.Services.Authentication.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserReadModelStore _userStore;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IUserReadModelStore userStore,
            ILogger<AuthenticationService> logger)
        {
            _userStore = userStore;
            _logger = logger;
        }

        public async Task<bool> UserExistsAsync(string username, string email)
        {
            var byUsername = await _userStore.GetByUsernameAsync(username);
            var byEmail = await _userStore.GetByEmailAsync(email);
            return byUsername != null || byEmail != null;
        }

        public async Task<Guid?> FindUserIdByUsernameOrEmailAsync(string usernameOrEmail)
        {
            var user = await _userStore.GetByUsernameAsync(usernameOrEmail) 
                      ?? await _userStore.GetByEmailAsync(usernameOrEmail);
            return user?.Id;
        }

        public async Task<Guid?> FindUserIdByEmailAsync(string email)
        {
            var user = await _userStore.GetByEmailAsync(email);
            return user?.Id;
        }

        public async Task<Guid?> FindUserIdByResetTokenAsync(string resetToken)
        {
            var user = await _userStore.GetByResetTokenAsync(resetToken);
            return user?.Id;
        }
    }

    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        public string HashPassword(string password)
        {
            using var algorithm = new Rfc2898DeriveBytes(
                password,
                SaltSize,
                Iterations,
                HashAlgorithmName.SHA256);

            var salt = algorithm.Salt;
            var hash = algorithm.GetBytes(HashSize);

            var hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

            return Convert.ToBase64String(hashBytes);
        }

        public bool VerifyPassword(string password, string hash)
        {
            var hashBytes = Convert.FromBase64String(hash);

            var salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            using var algorithm = new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256);

            var computedHash = algorithm.GetBytes(HashSize);

            for (int i = 0; i < HashSize; i++)
            {
                if (hashBytes[i + SaltSize] != computedHash[i])
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ITokenReadModelStore _tokenStore;
        private readonly ILogger<TokenService> _logger;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        public TokenService(
            IConfiguration configuration,
            ITokenReadModelStore tokenStore,
            ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _tokenStore = tokenStore;
            _logger = logger;
            _jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured");
            _jwtIssuer = configuration["Jwt:Issuer"] ?? "NeoServiceLayer";
            _jwtAudience = configuration["Jwt:Audience"] ?? "NeoServiceLayer";
        }

        public string GenerateAccessToken(Guid userId, string username, List<string> roles)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim("jti", Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _jwtIssuer,
                Audience = _jwtAudience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<(Guid userId, Guid tokenId)> ValidateRefreshTokenAsync(string token)
        {
            var refreshToken = await _tokenStore.GetByTokenAsync(token);
            if (refreshToken == null || !refreshToken.IsValid)
            {
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            return (refreshToken.UserId, refreshToken.Id);
        }
    }

    public class TwoFactorService : ITwoFactorService
    {
        private readonly IConfiguration _configuration;
        private readonly string _issuer;

        public TwoFactorService(IConfiguration configuration)
        {
            _configuration = configuration;
            _issuer = configuration["TwoFactor:Issuer"] ?? "NeoServiceLayer";
        }

        public string GenerateSecret()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(key);
        }

        public string GenerateQrCodeUri(string username, string secret)
        {
            var uri = $"otpauth://totp/{_issuer}:{username}?secret={secret}&issuer={_issuer}";
            
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new BitmapByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20);
            
            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}";
        }

        public List<string> GenerateBackupCodes(int count)
        {
            var codes = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var bytes = new byte[4];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(bytes);
                var code = BitConverter.ToUInt32(bytes, 0).ToString("D8");
                codes.Add($"{code.Substring(0, 4)}-{code.Substring(4, 4)}");
            }
            return codes;
        }

        public bool ValidateTotp(string secret, string code)
        {
            var key = Base32Encoding.ToBytes(secret);
            var totp = new Totp(key);
            
            // Allow for time drift by checking current, previous, and next time windows
            var currentCode = totp.ComputeTotp();
            var previousCode = totp.ComputeTotp(DateTime.UtcNow.AddSeconds(-30));
            var nextCode = totp.ComputeTotp(DateTime.UtcNow.AddSeconds(30));
            
            return code == currentCode || code == previousCode || code == nextCode;
        }
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendVerificationEmailAsync(string email, string username, string verificationToken)
        {
            // In a real implementation, this would send an actual email
            // For now, we'll just log it
            _logger.LogInformation(
                "Sending verification email to {Email} for user {Username} with token {Token}",
                email, username, verificationToken);

            // Simulate async operation
            await Task.Delay(100);
        }

        public async Task SendPasswordResetEmailAsync(string email, string username, string resetToken)
        {
            _logger.LogInformation(
                "Sending password reset email to {Email} for user {Username} with token {Token}",
                email, username, resetToken);

            await Task.Delay(100);
        }

        public async Task SendTwoFactorBackupCodesAsync(string email, string username, List<string> backupCodes)
        {
            _logger.LogInformation(
                "Sending {Count} backup codes to {Email} for user {Username}",
                backupCodes.Count, email, username);

            await Task.Delay(100);
        }
    }
}