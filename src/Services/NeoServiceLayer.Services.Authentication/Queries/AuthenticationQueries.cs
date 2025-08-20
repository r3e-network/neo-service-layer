using System;
using System.Collections.Generic;
using NeoServiceLayer.Core.CQRS;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Queries
{
    // User queries
    public record GetUserByIdQuery(Guid UserId) : IQuery<UserDto?>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string InitiatedBy { get; } = "System";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public int? TimeoutSeconds { get; } = 30;
        public bool AllowCached { get; } = true;
    }

    public record GetUserByUsernameQuery(string Username) : IQuery<UserDto?>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string InitiatedBy { get; } = "System";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public int? TimeoutSeconds { get; } = 30;
        public bool AllowCached { get; } = true;
    }

    public record GetUserByEmailQuery(string Email) : IQuery<UserDto?>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string InitiatedBy { get; } = "System";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public int? TimeoutSeconds { get; } = 30;
        public bool AllowCached { get; } = true;
    }

    public record SearchUsersQuery(
        string? SearchTerm = null,
        UserStatusFilter? Status = null,
        int PageNumber = 1,
        int PageSize = 20) : IQuery<PagedResult<UserDto>>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string InitiatedBy { get; } = "System";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public int? TimeoutSeconds { get; } = 30;
        public bool AllowCached { get; } = true;
    }

    // Session queries
    public record GetUserSessionsQuery(
        Guid UserId,
        bool ActiveOnly = false) : IQuery<List<SessionDto>>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string InitiatedBy { get; } = "System";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public int? TimeoutSeconds { get; } = 30;
        public bool AllowCached { get; } = true;
    }

    public record GetSessionByIdQuery(
        Guid UserId,
        Guid SessionId) : IQuery<SessionDto?>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string InitiatedBy { get; } = "System";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public int? TimeoutSeconds { get; } = 30;
        public bool AllowCached { get; } = true;
    }

    // Login history queries
    public record GetLoginHistoryQuery(
        Guid UserId,
        int MaxRecords = 10) : IQuery<List<LoginAttemptDto>>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string InitiatedBy { get; } = "System";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public int? TimeoutSeconds { get; } = 30;
        public bool AllowCached { get; } = true;
    }

    public record GetFailedLoginAttemptsQuery(
        Guid UserId,
        DateTime? Since = null) : IQuery<List<LoginAttemptDto>>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string InitiatedBy { get; } = "System";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public int? TimeoutSeconds { get; } = 30;
        public bool AllowCached { get; } = true;
    }

    // Role and permission queries
    public record GetUserRolesQuery(Guid UserId) : IQuery<List<string>>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string InitiatedBy { get; } = "System";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public int? TimeoutSeconds { get; } = 30;
        public bool AllowCached { get; } = true;
    }

    public record GetUserPermissionsQuery(Guid UserId) : IQuery<List<string>>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string InitiatedBy { get; } = "System";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public int? TimeoutSeconds { get; } = 30;
        public bool AllowCached { get; } = true;
    }

    public record GetUsersWithRoleQuery(string Role) : IQuery<List<UserDto>>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string InitiatedBy { get; } = "System";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public int? TimeoutSeconds { get; } = 30;
        public bool AllowCached { get; } = true;
    }

    public record GetUsersWithPermissionQuery(string Permission) : IQuery<List<UserDto>>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string InitiatedBy { get; } = "System";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public int? TimeoutSeconds { get; } = 30;
        public bool AllowCached { get; } = true;
    }

    // Token queries
    public record GetActiveRefreshTokensQuery(Guid UserId) : IQuery<List<RefreshTokenDto>>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string InitiatedBy { get; } = "System";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public int? TimeoutSeconds { get; } = 30;
        public bool AllowCached { get; } = true;
    }

    public record ValidateRefreshTokenQuery(string Token) : IQuery<RefreshTokenValidationResult>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string InitiatedBy { get; } = "System";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public int? TimeoutSeconds { get; } = 30;
        public bool AllowCached { get; } = true;
    }

    // Statistics queries
    public record GetUserStatisticsQuery() : IQuery<UserStatistics>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string InitiatedBy { get; } = "System";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public int? TimeoutSeconds { get; } = 30;
        public bool AllowCached { get; } = true;
    }

    public record GetLoginStatisticsQuery(
        DateTime? StartDate = null,
        DateTime? EndDate = null) : IQuery<LoginStatistics>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string InitiatedBy { get; } = "System";
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public int? TimeoutSeconds { get; } = 30;
        public bool AllowCached { get; } = true;
    }

    // DTOs
    public record UserDto(
        Guid Id,
        string Username,
        string Email,
        UserStatusFilter Status,
        bool EmailVerified,
        bool TwoFactorEnabled,
        DateTime CreatedAt,
        DateTime? LastLoginAt,
        DateTime? LastPasswordChangeAt,
        List<string> Roles,
        List<string> Permissions);

    public record SessionDto(
        Guid Id,
        Guid UserId,
        string IpAddress,
        string UserAgent,
        string? DeviceId,
        DateTime StartedAt,
        DateTime? LoggedOutAt,
        DateTime LastActivityAt,
        bool IsActive);

    public record LoginAttemptDto(
        string IpAddress,
        bool Success,
        DateTime AttemptTime,
        string? FailureReason);

    public record RefreshTokenDto(
        Guid Id,
        Guid UserId,
        DateTime IssuedAt,
        DateTime ExpiresAt,
        string? DeviceId,
        bool IsValid,
        bool IsExpired,
        bool IsRevoked);

    public record RefreshTokenValidationResult(
        bool IsValid,
        Guid? UserId,
        Guid? TokenId,
        string? FailureReason);

    public record UserStatistics(
        int TotalUsers,
        int ActiveUsers,
        int SuspendedUsers,
        int DeletedUsers,
        int VerifiedUsers,
        int TwoFactorEnabledUsers,
        int UsersLoggedInToday,
        int NewUsersThisWeek);

    public record LoginStatistics(
        int TotalLogins,
        int UniqueUsers,
        int FailedAttempts,
        int SuccessfulLogins,
        Dictionary<DateTime, int> LoginsByDay,
        Dictionary<string, int> LoginsByHour,
        List<string> TopDevices);

    public record PagedResult<T>(
        List<T> Items,
        int TotalCount,
        int PageNumber,
        int PageSize,
        int TotalPages)
    {
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
    }

    public enum UserStatusFilter
    {
        Active,
        Suspended,
        Deleted,
        All
    }
}