using Neo.SecretsManagement.Service.Models;

namespace Neo.SecretsManagement.Service.Services;

public interface ISecretService
{
    Task<SecretResponse> CreateSecretAsync(CreateSecretRequest request, string userId);
    Task<SecretResponse?> GetSecretAsync(string path, string userId, bool includeValue = false);
    Task<SecretResponse?> GetSecretByIdAsync(Guid secretId, string userId, bool includeValue = false);
    Task<List<SecretResponse>> ListSecretsAsync(ListSecretsRequest request, string userId);
    Task<bool> UpdateSecretAsync(string path, UpdateSecretRequest request, string userId);
    Task<bool> DeleteSecretAsync(string path, string userId);
    Task<ShareSecretResponse> ShareSecretAsync(string path, ShareSecretRequest request, string userId);
    Task<bool> RevokeShareAsync(Guid shareId, string userId);
    Task<SecretResponse> RotateSecretAsync(string path, RotateSecretRequest request, string userId);
    Task<List<SecretVersion>> GetSecretVersionsAsync(string path, string userId);
    Task<SecretResponse?> GetSecretVersionAsync(string path, int version, string userId, bool includeValue = false);
    Task<SecretStatistics> GetSecretStatisticsAsync(string userId);
    Task<List<SecretResponse>> GetExpiringSecretsAsync(int daysAhead, string userId);
    Task<bool> ValidateAccessAsync(string path, string userId, SecretOperation operation);
}

public interface ISecretPolicyService
{
    Task<SecretPolicy> CreatePolicyAsync(SecretPolicy policy, string userId);
    Task<SecretPolicy?> GetPolicyAsync(Guid policyId);
    Task<List<SecretPolicy>> ListPoliciesAsync(string userId);
    Task<bool> UpdatePolicyAsync(Guid policyId, SecretPolicy policy, string userId);
    Task<bool> DeletePolicyAsync(Guid policyId, string userId);
    Task<bool> EvaluatePolicyAsync(string path, string userId, SecretOperation operation, Dictionary<string, object> context);
    Task<List<SecretPolicy>> GetApplicablePoliciesAsync(string path);
}

public interface IAuditService
{
    Task LogAsync(string userId, string operation, string resourceType, string resourceId, 
                  string? resourcePath = null, bool success = true, string? errorMessage = null, 
                  Dictionary<string, object>? details = null, string? clientIp = null, string? userAgent = null);
    Task<List<AuditLogEntry>> GetAuditLogsAsync(string? userId = null, string? operation = null, 
                                               DateTime? fromDate = null, DateTime? toDate = null, 
                                               int skip = 0, int take = 100);
    Task<int> GetAuditLogCountAsync(string? userId = null, string? operation = null, 
                                   DateTime? fromDate = null, DateTime? toDate = null);
    Task CleanupOldLogsAsync(int retentionDays);
    Task<Dictionary<string, int>> GetOperationStatisticsAsync(DateTime fromDate, DateTime toDate);
    Task<Dictionary<string, int>> GetUserActivityStatisticsAsync(DateTime fromDate, DateTime toDate);
}

public interface IRotationService
{
    Task<bool> RotateSecretAsync(Guid secretId, string? newValue = null);
    Task<bool> RotateKeyAsync(Guid keyId);
    Task ScheduleRotationAsync(Guid? secretId, Guid? keyId, RotationType type, DateTime scheduledTime);
    Task<List<RotationJob>> GetPendingRotationJobsAsync();
    Task<List<RotationJob>> GetRotationHistoryAsync(Guid? secretId = null, Guid? keyId = null);
    Task<bool> CancelRotationJobAsync(Guid jobId, string userId);
}

public interface IBackupService
{
    Task<SecretBackup> CreateBackupAsync(BackupRequest request, string userId);
    Task<bool> RestoreBackupAsync(RestoreRequest request, string userId);
    Task<List<SecretBackup>> ListBackupsAsync(string userId);
    Task<SecretBackup?> GetBackupAsync(Guid backupId, string userId);
    Task<bool> DeleteBackupAsync(Guid backupId, string userId);
    Task<bool> ValidateBackupIntegrityAsync(Guid backupId);
    Task CleanupExpiredBackupsAsync();
}