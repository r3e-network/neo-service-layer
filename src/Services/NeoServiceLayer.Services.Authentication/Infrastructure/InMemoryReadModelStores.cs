using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NeoServiceLayer.Services.Authentication.Queries;
using NeoServiceLayer.ServiceFramework;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Infrastructure
{
    public class InMemoryUserReadModelStore : IUserReadModelStore
    {
        private readonly ConcurrentDictionary<Guid, UserReadModel> _users = new();
        private readonly ConcurrentDictionary<string, Guid> _usernameIndex = new();
        private readonly ConcurrentDictionary<string, Guid> _emailIndex = new();
        private readonly ConcurrentDictionary<string, Guid> _resetTokenIndex = new();

        public Task<UserReadModel?> GetByIdAsync(Guid userId)
        {
            _users.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        public Task<UserReadModel?> GetByUsernameAsync(string username)
        {
            if (_usernameIndex.TryGetValue(username.ToLowerInvariant(), out var userId))
            {
                _users.TryGetValue(userId, out var user);
                return Task.FromResult(user);
            }
            return Task.FromResult<UserReadModel?>(null);
        }

        public Task<UserReadModel?> GetByEmailAsync(string email)
        {
            if (_emailIndex.TryGetValue(email.ToLowerInvariant(), out var userId))
            {
                _users.TryGetValue(userId, out var user);
                return Task.FromResult(user);
            }
            return Task.FromResult<UserReadModel?>(null);
        }

        public Task<UserReadModel?> GetByResetTokenAsync(string resetToken)
        {
            if (_resetTokenIndex.TryGetValue(resetToken, out var userId))
            {
                _users.TryGetValue(userId, out var user);
                return Task.FromResult(user);
            }
            return Task.FromResult<UserReadModel?>(null);
        }

        public Task<List<UserReadModel>> SearchAsync(string? searchTerm, string? status, int pageNumber, int pageSize)
        {
            var query = _users.Values.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(u => u.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLowerInvariant();
                query = query.Where(u =>
                    u.Username.ToLowerInvariant().Contains(lowerSearchTerm) ||
                    u.Email.ToLowerInvariant().Contains(lowerSearchTerm));
            }

            var result = query
                .OrderBy(u => u.Username)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<int> GetCountAsync(string? status, string? searchTerm)
        {
            var query = _users.Values.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(u => u.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLowerInvariant();
                query = query.Where(u =>
                    u.Username.ToLowerInvariant().Contains(lowerSearchTerm) ||
                    u.Email.ToLowerInvariant().Contains(lowerSearchTerm));
            }

            return Task.FromResult(query.Count());
        }

        public Task<List<UserReadModel>> GetUsersWithRoleAsync(string role)
        {
            var users = _users.Values
                .Where(u => u.Roles.Contains(role))
                .ToList();
            return Task.FromResult(users);
        }

        public Task<List<UserReadModel>> GetUsersWithPermissionAsync(string permission)
        {
            var users = _users.Values
                .Where(u => u.Permissions.Contains(permission))
                .ToList();
            return Task.FromResult(users);
        }

        public Task<List<UserReadModel>> GetAllAsync()
        {
            return Task.FromResult(_users.Values.ToList());
        }

        public Task<UserStatistics> GetStatisticsAsync()
        {
            var now = DateTime.UtcNow;
            var weekAgo = now.AddDays(-7);
            var today = now.Date;

            var stats = new UserStatistics(
                TotalUsers: _users.Count,
                ActiveUsers: _users.Values.Count(u => u.Status == "Active"),
                SuspendedUsers: _users.Values.Count(u => u.Status == "Suspended"),
                DeletedUsers: _users.Values.Count(u => u.Status == "Deleted"),
                VerifiedUsers: _users.Values.Count(u => u.EmailVerified),
                TwoFactorEnabledUsers: _users.Values.Count(u => u.TwoFactorEnabled),
                UsersLoggedInToday: _users.Values.Count(u => u.LastLoginAt?.Date == today),
                NewUsersThisWeek: _users.Values.Count(u => u.CreatedAt >= weekAgo)
            );

            return Task.FromResult(stats);
        }

        public Task SaveAsync(UserReadModel user)
        {
            _users.AddOrUpdate(user.Id, user, (_, _) => user);
            _usernameIndex.AddOrUpdate(user.Username.ToLowerInvariant(), user.Id, (_, _) => user.Id);
            _emailIndex.AddOrUpdate(user.Email.ToLowerInvariant(), user.Id, (_, _) => user.Id);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid userId)
        {
            if (_users.TryRemove(userId, out var user))
            {
                _usernameIndex.TryRemove(user.Username.ToLowerInvariant(), out _);
                _emailIndex.TryRemove(user.Email.ToLowerInvariant(), out _);
            }
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _users.Clear();
            _usernameIndex.Clear();
            _emailIndex.Clear();
            _resetTokenIndex.Clear();
            return Task.CompletedTask;
        }
    }

    public class InMemorySessionReadModelStore : ISessionReadModelStore
    {
        private readonly ConcurrentDictionary<Guid, SessionReadModel> _sessions = new();
        private readonly ConcurrentDictionary<Guid, List<Guid>> _userSessionIndex = new();

        public Task<SessionReadModel?> GetByIdAsync(Guid userId, Guid sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session);
        }

        public Task<List<SessionReadModel>> GetUserSessionsAsync(Guid userId, bool activeOnly)
        {
            if (!_userSessionIndex.TryGetValue(userId, out var sessionIds))
            {
                return Task.FromResult(new List<SessionReadModel>());
            }

            var sessions = sessionIds
                .Select(id => _sessions.TryGetValue(id, out var s) ? s : null)
                .Where(s => s != null)
                .Cast<SessionReadModel>();

            if (activeOnly)
            {
                sessions = sessions.Where(s => s.IsActive);
            }

            return Task.FromResult(sessions.ToList());
        }

        public Task<LoginStatistics> GetLoginStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            var sessions = _sessions.Values
                .Where(s => s.StartedAt >= startDate && s.StartedAt <= endDate)
                .ToList();

            var loginsByDay = sessions
                .GroupBy(s => s.StartedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var loginsByHour = sessions
                .GroupBy(s => s.StartedAt.Hour.ToString("00"))
                .ToDictionary(g => g.Key, g => g.Count());

            var topDevices = sessions
                .Where(s => !string.IsNullOrEmpty(s.DeviceId))
                .GroupBy(s => s.DeviceId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key!)
                .ToList();

            var stats = new LoginStatistics(
                TotalLogins: sessions.Count,
                UniqueUsers: sessions.Select(s => s.UserId).Distinct().Count(),
                FailedAttempts: 0, // Would need to track this separately
                SuccessfulLogins: sessions.Count,
                LoginsByDay: loginsByDay,
                LoginsByHour: loginsByHour,
                TopDevices: topDevices
            );

            return Task.FromResult(stats);
        }

        public Task SaveAsync(SessionReadModel session)
        {
            _sessions.AddOrUpdate(session.Id, session, (_, _) => session);

            _userSessionIndex.AddOrUpdate(session.UserId,
                new List<Guid> { session.Id },
                (_, existing) =>
                {
                    if (!existing.Contains(session.Id))
                    {
                        existing.Add(session.Id);
                    }
                    return existing;
                });

            return Task.CompletedTask;
        }

        public Task LogoutAllSessionsAsync(Guid userId, DateTime logoutTime)
        {
            if (_userSessionIndex.TryGetValue(userId, out var sessionIds))
            {
                foreach (var sessionId in sessionIds)
                {
                    if (_sessions.TryGetValue(sessionId, out var session) && session.IsActive)
                    {
                        session.LoggedOutAt = logoutTime;
                        _sessions.TryUpdate(sessionId, session, session);
                    }
                }
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid sessionId)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                if (_userSessionIndex.TryGetValue(session.UserId, out var sessionIds))
                {
                    sessionIds.Remove(sessionId);
                    if (sessionIds.Count == 0)
                    {
                        _userSessionIndex.TryRemove(session.UserId, out _);
                    }
                }
            }
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _sessions.Clear();
            _userSessionIndex.Clear();
            return Task.CompletedTask;
        }
    }

    public class InMemoryTokenReadModelStore : ITokenReadModelStore
    {
        private readonly ConcurrentDictionary<Guid, RefreshTokenReadModel> _tokens = new();
        private readonly ConcurrentDictionary<string, Guid> _tokenIndex = new();
        private readonly ConcurrentDictionary<Guid, List<Guid>> _userTokenIndex = new();

        public Task<RefreshTokenReadModel?> GetByIdAsync(Guid tokenId)
        {
            _tokens.TryGetValue(tokenId, out var token);
            return Task.FromResult(token);
        }

        public Task<RefreshTokenReadModel?> GetByTokenAsync(string token)
        {
            if (_tokenIndex.TryGetValue(token, out var tokenId))
            {
                _tokens.TryGetValue(tokenId, out var refreshToken);
                return Task.FromResult(refreshToken);
            }
            return Task.FromResult<RefreshTokenReadModel?>(null);
        }

        public Task<List<RefreshTokenReadModel>> GetActiveTokensAsync(Guid userId)
        {
            if (!_userTokenIndex.TryGetValue(userId, out var tokenIds))
            {
                return Task.FromResult(new List<RefreshTokenReadModel>());
            }

            var tokens = tokenIds
                .Select(id => _tokens.TryGetValue(id, out var t) ? t : null)
                .Where(t => t != null && t.IsValid)
                .Cast<RefreshTokenReadModel>()
                .ToList();

            return Task.FromResult(tokens);
        }

        public Task SaveAsync(RefreshTokenReadModel token)
        {
            _tokens.AddOrUpdate(token.Id, token, (_, _) => token);
            _tokenIndex.AddOrUpdate(token.Token, token.Id, (_, _) => token.Id);

            _userTokenIndex.AddOrUpdate(token.UserId,
                new List<Guid> { token.Id },
                (_, existing) =>
                {
                    if (!existing.Contains(token.Id))
                    {
                        existing.Add(token.Id);
                    }
                    return existing;
                });

            return Task.CompletedTask;
        }

        public Task RevokeAllTokensAsync(Guid userId, DateTime revokedAt, string reason)
        {
            if (_userTokenIndex.TryGetValue(userId, out var tokenIds))
            {
                foreach (var tokenId in tokenIds)
                {
                    if (_tokens.TryGetValue(tokenId, out var token) && token.IsValid)
                    {
                        token.RevokedAt = revokedAt;
                        token.RevokedReason = reason;
                        _tokens.TryUpdate(tokenId, token, token);
                    }
                }
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid tokenId)
        {
            if (_tokens.TryRemove(tokenId, out var token))
            {
                _tokenIndex.TryRemove(token.Token, out _);
                if (_userTokenIndex.TryGetValue(token.UserId, out var tokenIds))
                {
                    tokenIds.Remove(tokenId);
                    if (tokenIds.Count == 0)
                    {
                        _userTokenIndex.TryRemove(token.UserId, out _);
                    }
                }
            }
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _tokens.Clear();
            _tokenIndex.Clear();
            _userTokenIndex.Clear();
            return Task.CompletedTask;
        }
    }
}