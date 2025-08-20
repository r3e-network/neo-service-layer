using System;
using System.Threading.Tasks;
using NeoServiceLayer.Services.Authentication.Models;

namespace NeoServiceLayer.Services.Authentication.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(string userId);
        Task<User> GetByUsernameAsync(string username);
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByPasswordResetTokenAsync(string resetToken);
        Task<User> CreateAsync(User user);
        Task UpdateAsync(User user);
        Task UpdatePasswordAsync(string userId, string passwordHash, string passwordSalt);
        Task UpdatePasswordResetTokenAsync(string userId, string token, DateTime expiry);
        Task ClearPasswordResetTokenAsync(string userId);
        Task UpdateMfaSettingsAsync(string userId, MfaSettings settings);
        Task<MfaSettings> GetMfaSettingsAsync(string userId);
        Task UpdateLockStatusAsync(string userId, bool isLocked, string reason);
        Task DeleteAsync(string userId);
    }
}