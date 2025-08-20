using System;
using System.Collections.Generic;
using NeoServiceLayer.Core.CQRS;

namespace NeoServiceLayer.Infrastructure.CQRS.Queries
{
    /// <summary>
    /// User-related queries for CQRS implementation
    /// </summary>
    
    public class GetUserByIdQuery : QueryBase<UserReadModel>
    {
        public Guid UserId { get; set; }
        public bool IncludeRoles { get; set; }
        public bool IncludePermissions { get; set; }
    }

    public class GetUserByEmailQuery : QueryBase<UserReadModel>
    {
        public string Email { get; set; }
        public Guid TenantId { get; set; }
    }

    public class GetUsersByTenantQuery : QueryBase<PagedResult<UserReadModel>>
    {
        public Guid TenantId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SearchTerm { get; set; }
        public string Status { get; set; }
        public string Role { get; set; }
        public string OrderBy { get; set; } = "CreatedAt";
        public bool Descending { get; set; } = true;
    }

    public class GetUserRolesQuery : QueryBase<List<RoleReadModel>>
    {
        public Guid UserId { get; set; }
        public bool IncludePermissions { get; set; }
    }

    public class GetUserPermissionsQuery : QueryBase<List<PermissionReadModel>>
    {
        public Guid UserId { get; set; }
        public string Resource { get; set; }
    }

    public class GetUserActivityQuery : QueryBase<List<UserActivityReadModel>>
    {
        public Guid UserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string ActivityType { get; set; }
        public int Limit { get; set; } = 100;
    }

    public class GetActiveSessionsQuery : QueryBase<List<SessionReadModel>>
    {
        public Guid UserId { get; set; }
    }

    // Blockchain queries
    public class GetTransactionByHashQuery : QueryBase<TransactionReadModel>
    {
        public string TransactionHash { get; set; }
        public Guid NetworkId { get; set; }
    }

    public class GetTransactionsByAddressQuery : QueryBase<PagedResult<TransactionReadModel>>
    {
        public string Address { get; set; }
        public Guid NetworkId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class GetWalletBalanceQuery : QueryBase<WalletBalanceReadModel>
    {
        public string Address { get; set; }
        public Guid NetworkId { get; set; }
        public bool IncludeTokens { get; set; }
    }

    public class GetSmartContractQuery : QueryBase<SmartContractReadModel>
    {
        public string ContractAddress { get; set; }
        public Guid NetworkId { get; set; }
    }

    // Compute queries
    public class GetComputeJobQuery : QueryBase<ComputeJobReadModel>
    {
        public Guid JobId { get; set; }
        public bool IncludeResult { get; set; }
    }

    public class GetComputeJobsQuery : QueryBase<PagedResult<ComputeJobReadModel>>
    {
        public Guid? UserId { get; set; }
        public string Status { get; set; }
        public string JobType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetComputeMetricsQuery : QueryBase<ComputeMetricsReadModel>
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string AggregationPeriod { get; set; } // Hour, Day, Week, Month
    }

    // Storage queries
    public class GetFileByIdQuery : QueryBase<FileReadModel>
    {
        public Guid FileId { get; set; }
    }

    public class GetFilesByUserQuery : QueryBase<PagedResult<FileReadModel>>
    {
        public Guid UserId { get; set; }
        public string StorageTier { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class GetStorageStatisticsQuery : QueryBase<StorageStatisticsReadModel>
    {
        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }
    }

    // Oracle queries
    public class GetDataFeedQuery : QueryBase<DataFeedReadModel>
    {
        public string FeedId { get; set; }
        public bool IncludeLatestData { get; set; }
    }

    public class GetDataFeedsQuery : QueryBase<List<DataFeedReadModel>>
    {
        public string FeedType { get; set; }
        public string Status { get; set; }
        public bool IncludeLatestData { get; set; }
    }

    public class GetPriceDataQuery : QueryBase<PriceDataReadModel>
    {
        public string Symbol { get; set; }
        public string Currency { get; set; } = "USD";
        public bool UseCache { get; set; } = true;
    }

    public class GetHistoricalPricesQuery : QueryBase<List<PriceDataReadModel>>
    {
        public string Symbol { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Interval { get; set; } // 1m, 5m, 1h, 1d
    }

    // Notification queries
    public class GetNotificationsByUserQuery : QueryBase<PagedResult<NotificationReadModel>>
    {
        public Guid UserId { get; set; }
        public bool UnreadOnly { get; set; }
        public string Type { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetNotificationTemplatesQuery : QueryBase<List<NotificationTemplateReadModel>>
    {
        public string Type { get; set; }
        public bool ActiveOnly { get; set; } = true;
    }

    // Analytics queries
    public class GetSystemMetricsQuery : QueryBase<SystemMetricsReadModel>
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> MetricTypes { get; set; }
    }

    public class GetServiceHealthQuery : QueryBase<ServiceHealthReadModel>
    {
        public string ServiceName { get; set; }
        public bool IncludeHistory { get; set; }
    }

    // Read models (DTOs)
    public class UserReadModel
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Status { get; set; }
        public Guid TenantId { get; set; }
        public List<RoleReadModel> Roles { get; set; }
        public List<PermissionReadModel> Permissions { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class RoleReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<PermissionReadModel> Permissions { get; set; }
    }

    public class PermissionReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Resource { get; set; }
        public string Action { get; set; }
    }

    public class UserActivityReadModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class SessionReadModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class TransactionReadModel
    {
        public Guid Id { get; set; }
        public string TransactionHash { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Value { get; set; }
        public string Status { get; set; }
        public long? BlockNumber { get; set; }
        public DateTime? BlockTimestamp { get; set; }
        public decimal? GasUsed { get; set; }
        public decimal? GasPrice { get; set; }
    }

    public class WalletBalanceReadModel
    {
        public string Address { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; }
        public List<TokenBalanceReadModel> TokenBalances { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class TokenBalanceReadModel
    {
        public string TokenAddress { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public decimal Balance { get; set; }
        public int Decimals { get; set; }
    }

    public class SmartContractReadModel
    {
        public Guid Id { get; set; }
        public string Address { get; set; }
        public string Name { get; set; }
        public string ContractType { get; set; }
        public object Abi { get; set; }
        public bool Verified { get; set; }
        public DateTime? DeployedAt { get; set; }
    }

    public class ComputeJobReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string JobType { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public object Result { get; set; }
        public string Error { get; set; }
    }

    public class ComputeMetricsReadModel
    {
        public int TotalJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int FailedJobs { get; set; }
        public double AverageExecutionTime { get; set; }
        public double SuccessRate { get; set; }
        public Dictionary<string, int> JobsByType { get; set; }
        public List<TimeSeriesDataPoint> Timeline { get; set; }
    }

    public class TimeSeriesDataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }

    public class FileReadModel
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public string StorageTier { get; set; }
        public bool IsEncrypted { get; set; }
        public DateTime UploadedAt { get; set; }
        public int AccessCount { get; set; }
        public DateTime? LastAccessedAt { get; set; }
    }

    public class StorageStatisticsReadModel
    {
        public long TotalFiles { get; set; }
        public long TotalSizeBytes { get; set; }
        public Dictionary<string, long> FilesByTier { get; set; }
        public Dictionary<string, long> FilesByType { get; set; }
        public double AverageFileSizeBytes { get; set; }
    }

    public class DataFeedReadModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FeedType { get; set; }
        public string Status { get; set; }
        public int UpdateInterval { get; set; }
        public object LatestValue { get; set; }
        public DateTime? LastUpdated { get; set; }
        public double DataQuality { get; set; }
    }

    public class PriceDataReadModel
    {
        public string Symbol { get; set; }
        public string Currency { get; set; }
        public decimal Price { get; set; }
        public decimal High24h { get; set; }
        public decimal Low24h { get; set; }
        public decimal Change24h { get; set; }
        public decimal Volume24h { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class NotificationReadModel
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class NotificationTemplateReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Subject { get; set; }
        public string BodyTemplate { get; set; }
        public bool IsActive { get; set; }
    }

    public class SystemMetricsReadModel
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public long DiskUsage { get; set; }
        public int ActiveConnections { get; set; }
        public double RequestsPerSecond { get; set; }
        public double AverageResponseTime { get; set; }
        public Dictionary<string, double> ServiceMetrics { get; set; }
    }

    public class ServiceHealthReadModel
    {
        public string ServiceName { get; set; }
        public string Status { get; set; } // Healthy, Degraded, Unhealthy
        public DateTime LastChecked { get; set; }
        public double Uptime { get; set; }
        public List<HealthCheckResult> HealthChecks { get; set; }
        public List<ServiceHealthHistoryPoint> History { get; set; }
    }

    public class HealthCheckResult
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public TimeSpan ResponseTime { get; set; }
    }

    public class ServiceHealthHistoryPoint
    {
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}