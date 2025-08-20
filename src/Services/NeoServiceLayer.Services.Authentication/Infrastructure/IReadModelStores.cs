using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Services.Authentication.Queries;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Infrastructure
{
    public interface IUserReadModelStore
    {
        Task<UserReadModel?> GetByIdAsync(Guid userId);
        Task<UserReadModel?> GetByUsernameAsync(string username);
        Task<UserReadModel?> GetByEmailAsync(string email);
        Task<UserReadModel?> GetByResetTokenAsync(string resetToken);
        Task<List<UserReadModel>> SearchAsync(string? searchTerm, string? status, int pageNumber, int pageSize);
        Task<int> GetCountAsync(string? status, string? searchTerm);
        Task<List<UserReadModel>> GetUsersWithRoleAsync(string role);
        Task<List<UserReadModel>> GetUsersWithPermissionAsync(string permission);
        Task<UserStatistics> GetStatisticsAsync();
        Task<List<UserReadModel>> GetAllAsync(); // Added for compatibility
        Task SaveAsync(UserReadModel user);
        Task DeleteAsync(Guid userId);
        Task ClearAsync();
    }

    public interface ISessionReadModelStore
    {
        Task<SessionReadModel?> GetByIdAsync(Guid userId, Guid sessionId);
        Task<List<SessionReadModel>> GetUserSessionsAsync(Guid userId, bool activeOnly);
        Task<LoginStatistics> GetLoginStatisticsAsync(DateTime startDate, DateTime endDate);
        Task SaveAsync(SessionReadModel session);
        Task LogoutAllSessionsAsync(Guid userId, DateTime logoutTime);
        Task DeleteAsync(Guid sessionId);
        Task ClearAsync();
    }

    public interface ITokenReadModelStore
    {
        Task<RefreshTokenReadModel?> GetByIdAsync(Guid tokenId);
        Task<RefreshTokenReadModel?> GetByTokenAsync(string token);
        Task<List<RefreshTokenReadModel>> GetActiveTokensAsync(Guid userId);
        Task SaveAsync(RefreshTokenReadModel token);
        Task RevokeAllTokensAsync(Guid userId, DateTime revokedAt, string reason);
        Task DeleteAsync(Guid tokenId);
        Task ClearAsync();
    }
}