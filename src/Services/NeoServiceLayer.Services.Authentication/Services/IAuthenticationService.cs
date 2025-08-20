using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Services
{
    public interface IAuthenticationService
    {
        Task<bool> UserExistsAsync(string username, string email);
        Task<Guid?> FindUserIdByUsernameOrEmailAsync(string usernameOrEmail);
        Task<Guid?> FindUserIdByEmailAsync(string email);
        Task<Guid?> FindUserIdByResetTokenAsync(string resetToken);
    }

    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public interface ITokenService
    {
        string GenerateAccessToken(Guid userId, string username, List<string> roles);
        string GenerateRefreshToken();
        Task<(Guid userId, Guid tokenId)> ValidateRefreshTokenAsync(string token);
    }

    public interface ITwoFactorService
    {
        string GenerateSecret();
        string GenerateQrCodeUri(string username, string secret);
        List<string> GenerateBackupCodes(int count);
        bool ValidateTotp(string secret, string code);
    }

    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string email, string username, string verificationToken);
        Task SendPasswordResetEmailAsync(string email, string username, string resetToken);
        Task SendTwoFactorBackupCodesAsync(string email, string username, List<string> backupCodes);
    }
}