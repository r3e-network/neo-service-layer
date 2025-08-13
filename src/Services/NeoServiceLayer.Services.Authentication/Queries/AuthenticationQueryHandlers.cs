using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Services.Authentication.Infrastructure;

namespace NeoServiceLayer.Services.Authentication.Queries
{
    public class AuthenticationQueryHandlers :
        IQueryHandler<GetUserByIdQuery, UserDto?>,
        IQueryHandler<GetUserByUsernameQuery, UserDto?>,
        IQueryHandler<GetUserByEmailQuery, UserDto?>,
        IQueryHandler<SearchUsersQuery, PagedResult<UserDto>>,
        IQueryHandler<GetUserSessionsQuery, List<SessionDto>>,
        IQueryHandler<GetSessionByIdQuery, SessionDto?>,
        IQueryHandler<GetLoginHistoryQuery, List<LoginAttemptDto>>,
        IQueryHandler<GetFailedLoginAttemptsQuery, List<LoginAttemptDto>>,
        IQueryHandler<GetUserRolesQuery, List<string>>,
        IQueryHandler<GetUserPermissionsQuery, List<string>>,
        IQueryHandler<GetUsersWithRoleQuery, List<UserDto>>,
        IQueryHandler<GetUsersWithPermissionQuery, List<UserDto>>,
        IQueryHandler<GetActiveRefreshTokensQuery, List<RefreshTokenDto>>,
        IQueryHandler<ValidateRefreshTokenQuery, RefreshTokenValidationResult>,
        IQueryHandler<GetUserStatisticsQuery, UserStatistics>,
        IQueryHandler<GetLoginStatisticsQuery, LoginStatistics>
    {
        private readonly IUserReadModelStore _userStore;
        private readonly ISessionReadModelStore _sessionStore;
        private readonly ITokenReadModelStore _tokenStore;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AuthenticationQueryHandlers> _logger;

        public AuthenticationQueryHandlers(
            IUserReadModelStore userStore,
            ISessionReadModelStore sessionStore,
            ITokenReadModelStore tokenStore,
            IMemoryCache cache,
            ILogger<AuthenticationQueryHandlers> logger)
        {
            _userStore = userStore;
            _sessionStore = sessionStore;
            _tokenStore = tokenStore;
            _cache = cache;
            _logger = logger;
        }

        public async Task<UserDto?> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"user:{query.UserId}";
            if (_cache.TryGetValue<UserDto>(cacheKey, out var cached))
            {
                return cached;
            }

            var user = await _userStore.GetByIdAsync(query.UserId);
            if (user == null)
            {
                return null;
            }

            var dto = MapToUserDto(user);
            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(5));
            return dto;
        }

        public async Task<UserDto?> HandleAsync(GetUserByUsernameQuery query, CancellationToken cancellationToken = default)
        {
            var user = await _userStore.GetByUsernameAsync(query.Username);
            return user != null ? MapToUserDto(user) : null;
        }

        public async Task<UserDto?> HandleAsync(GetUserByEmailQuery query, CancellationToken cancellationToken = default)
        {
            var user = await _userStore.GetByEmailAsync(query.Email);
            return user != null ? MapToUserDto(user) : null;
        }

        public async Task<PagedResult<UserDto>> HandleAsync(SearchUsersQuery query, CancellationToken cancellationToken = default)
        {
            var users = await _userStore.SearchAsync(
                query.SearchTerm,
                MapStatusFilter(query.Status),
                query.PageNumber,
                query.PageSize);

            var totalCount = await _userStore.GetCountAsync(MapStatusFilter(query.Status), query.SearchTerm);
            var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

            return new PagedResult<UserDto>(
                users.Select(MapToUserDto).ToList(),
                totalCount,
                query.PageNumber,
                query.PageSize,
                totalPages);
        }

        public async Task<List<SessionDto>> HandleAsync(GetUserSessionsQuery query, CancellationToken cancellationToken = default)
        {
            var sessions = await _sessionStore.GetUserSessionsAsync(query.UserId, query.ActiveOnly);
            return sessions.Select(MapToSessionDto).ToList();
        }

        public async Task<SessionDto?> HandleAsync(GetSessionByIdQuery query, CancellationToken cancellationToken = default)
        {
            var session = await _sessionStore.GetByIdAsync(query.UserId, query.SessionId);
            return session != null ? MapToSessionDto(session) : null;
        }

        public async Task<List<LoginAttemptDto>> HandleAsync(GetLoginHistoryQuery query, CancellationToken cancellationToken = default)
        {
            var user = await _userStore.GetByIdAsync(query.UserId);
            if (user == null)
            {
                return new List<LoginAttemptDto>();
            }

            return user.RecentLoginAttempts
                .OrderByDescending(a => a.AttemptTime)
                .Take(query.MaxRecords)
                .Select(MapToLoginAttemptDto)
                .ToList();
        }

        public async Task<List<LoginAttemptDto>> HandleAsync(GetFailedLoginAttemptsQuery query, CancellationToken cancellationToken = default)
        {
            var user = await _userStore.GetByIdAsync(query.UserId);
            if (user == null)
            {
                return new List<LoginAttemptDto>();
            }

            var attempts = user.RecentLoginAttempts
                .Where(a => !a.Success);

            if (query.Since.HasValue)
            {
                attempts = attempts.Where(a => a.AttemptTime >= query.Since.Value);
            }

            return attempts
                .OrderByDescending(a => a.AttemptTime)
                .Select(MapToLoginAttemptDto)
                .ToList();
        }

        public async Task<List<string>> HandleAsync(GetUserRolesQuery query, CancellationToken cancellationToken = default)
        {
            var user = await _userStore.GetByIdAsync(query.UserId);
            return user?.Roles ?? new List<string>();
        }

        public async Task<List<string>> HandleAsync(GetUserPermissionsQuery query, CancellationToken cancellationToken = default)
        {
            var user = await _userStore.GetByIdAsync(query.UserId);
            return user?.Permissions ?? new List<string>();
        }

        public async Task<List<UserDto>> HandleAsync(GetUsersWithRoleQuery query, CancellationToken cancellationToken = default)
        {
            var users = await _userStore.GetUsersWithRoleAsync(query.Role);
            return users.Select(MapToUserDto).ToList();
        }

        public async Task<List<UserDto>> HandleAsync(GetUsersWithPermissionQuery query, CancellationToken cancellationToken = default)
        {
            var users = await _userStore.GetUsersWithPermissionAsync(query.Permission);
            return users.Select(MapToUserDto).ToList();
        }

        public async Task<List<RefreshTokenDto>> HandleAsync(GetActiveRefreshTokensQuery query, CancellationToken cancellationToken = default)
        {
            var tokens = await _tokenStore.GetActiveTokensAsync(query.UserId);
            return tokens.Select(MapToRefreshTokenDto).ToList();
        }

        public async Task<RefreshTokenValidationResult> HandleAsync(ValidateRefreshTokenQuery query, CancellationToken cancellationToken = default)
        {
            var token = await _tokenStore.GetByTokenAsync(query.Token);
            
            if (token == null)
            {
                return new RefreshTokenValidationResult(false, null, null, "Token not found");
            }

            if (token.IsExpired)
            {
                return new RefreshTokenValidationResult(false, token.UserId, token.Id, "Token expired");
            }

            if (token.IsRevoked)
            {
                return new RefreshTokenValidationResult(false, token.UserId, token.Id, "Token revoked");
            }

            return new RefreshTokenValidationResult(true, token.UserId, token.Id, null);
        }

        public async Task<UserStatistics> HandleAsync(GetUserStatisticsQuery query, CancellationToken cancellationToken = default)
        {
            var cacheKey = "user:statistics";
            if (_cache.TryGetValue<UserStatistics>(cacheKey, out var cached))
            {
                return cached;
            }

            var stats = await _userStore.GetStatisticsAsync();
            _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(1));
            return stats;
        }

        public async Task<LoginStatistics> HandleAsync(GetLoginStatisticsQuery query, CancellationToken cancellationToken = default)
        {
            var startDate = query.StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = query.EndDate ?? DateTime.UtcNow;

            var stats = await _sessionStore.GetLoginStatisticsAsync(startDate, endDate);
            return stats;
        }

        private UserDto MapToUserDto(UserReadModel user)
        {
            return new UserDto(
                user.Id,
                user.Username,
                user.Email,
                MapStatus(user.Status),
                user.EmailVerified,
                user.TwoFactorEnabled,
                user.CreatedAt,
                user.LastLoginAt,
                user.LastPasswordChangeAt,
                user.Roles,
                user.Permissions);
        }

        private SessionDto MapToSessionDto(SessionReadModel session)
        {
            return new SessionDto(
                session.Id,
                session.UserId,
                session.IpAddress,
                session.UserAgent,
                session.DeviceId,
                session.StartedAt,
                session.LoggedOutAt,
                session.LastActivityAt,
                session.IsActive);
        }

        private LoginAttemptDto MapToLoginAttemptDto(LoginAttemptReadModel attempt)
        {
            return new LoginAttemptDto(
                attempt.IpAddress,
                attempt.Success,
                attempt.AttemptTime,
                attempt.FailureReason);
        }

        private RefreshTokenDto MapToRefreshTokenDto(RefreshTokenReadModel token)
        {
            return new RefreshTokenDto(
                token.Id,
                token.UserId,
                token.IssuedAt,
                token.ExpiresAt,
                token.DeviceId,
                token.IsValid,
                token.IsExpired,
                token.IsRevoked);
        }

        private UserStatusFilter MapStatus(string status)
        {
            return status switch
            {
                "Active" => UserStatusFilter.Active,
                "Suspended" => UserStatusFilter.Suspended,
                "Deleted" => UserStatusFilter.Deleted,
                _ => UserStatusFilter.All
            };
        }

        private string? MapStatusFilter(UserStatusFilter? filter)
        {
            return filter switch
            {
                UserStatusFilter.Active => "Active",
                UserStatusFilter.Suspended => "Suspended",
                UserStatusFilter.Deleted => "Deleted",
                _ => null
            };
        }
    }

    // Read models for projections
    public class UserReadModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public bool EmailVerified { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastPasswordChangeAt { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public List<LoginAttemptReadModel> RecentLoginAttempts { get; set; } = new();
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockedUntil { get; set; }
    }

    public class SessionReadModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? LoggedOutAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public bool IsActive => !LoggedOutAt.HasValue;
    }

    public class LoginAttemptReadModel
    {
        public string IpAddress { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime AttemptTime { get; set; }
        public string? FailureReason { get; set; }
    }

    public class RefreshTokenReadModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? DeviceId { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedReason { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool IsRevoked => RevokedAt.HasValue;
        public bool IsValid => !IsExpired && !IsRevoked;
    }
}