using Microsoft.EntityFrameworkCore;
using Neo.SecretsManagement.Service.Data;
using Neo.SecretsManagement.Service.Models;
using System.Security.Cryptography;
using System.Text.Json;

namespace Neo.SecretsManagement.Service.Services;

public class BackupService : IBackupService
{
    private readonly SecretsDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<BackupService> _logger;
    private readonly IAuditService _auditService;
    private readonly BackupServiceOptions _options;

    public BackupService(
        SecretsDbContext context,
        IEncryptionService encryptionService,
        ILogger<BackupService> logger,
        IAuditService auditService,
        IOptionsSnapshot<BackupServiceOptions> options)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
        _auditService = auditService;
        _options = options.Value;
    }

    public async Task<SecretBackup> CreateBackupAsync(BackupRequest request, string userId)
    {
        try
        {
            var backup = new SecretBackup
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                BackupType = request.BackupType,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                ExpiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddDays(_options.DefaultRetentionDays),
                Status = BackupStatus.InProgress
            };

            _context.SecretBackups.Add(backup);
            await _context.SaveChangesAsync();

            // Create backup data based on type
            var backupData = await CreateBackupDataAsync(request, userId);
            
            // Encrypt backup data
            var encryptedData = await _encryptionService.EncryptAsync(
                JsonSerializer.Serialize(backupData), 
                _options.BackupEncryptionKeyId
            );

            // Calculate integrity hash
            backup.Data = encryptedData;
            backup.IntegrityHash = CalculateIntegrityHash(encryptedData);
            backup.Status = BackupStatus.Completed;
            backup.CompletedAt = DateTime.UtcNow;
            backup.SecretCount = backupData.Secrets?.Count ?? 0;
            backup.Size = System.Text.Encoding.UTF8.GetByteCount(encryptedData);

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                "create",
                "backup",
                backup.Id.ToString(),
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["backup_name"] = backup.Name,
                    ["backup_type"] = backup.BackupType.ToString(),
                    ["secret_count"] = backup.SecretCount,
                    ["size_bytes"] = backup.Size,
                    ["expires_at"] = backup.ExpiresAt?.ToString("O") ?? "never"
                }
            );

            _logger.LogInformation("Backup {BackupId} '{BackupName}' created successfully with {SecretCount} secrets",
                backup.Id, backup.Name, backup.SecretCount);

            return backup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup '{BackupName}'", request.Name);
            await _auditService.LogAsync(
                userId,
                "create",
                "backup",
                request.Name,
                null,
                false,
                ex.Message
            );
            throw;
        }
    }

    public async Task<bool> RestoreBackupAsync(RestoreRequest request, string userId)
    {
        try
        {
            var backup = await _context.SecretBackups
                .FirstOrDefaultAsync(b => b.Id == request.BackupId);

            if (backup == null)
            {
                _logger.LogWarning("Backup {BackupId} not found for restore", request.BackupId);
                return false;
            }

            if (backup.Status != BackupStatus.Completed)
            {
                _logger.LogWarning("Cannot restore backup {BackupId} with status {Status}", 
                    request.BackupId, backup.Status);
                return false;
            }

            // Validate backup integrity
            if (!await ValidateBackupIntegrityAsync(backup.Id))
            {
                _logger.LogError("Backup {BackupId} failed integrity validation", request.BackupId);
                return false;
            }

            // Decrypt backup data
            var decryptedData = await _encryptionService.DecryptAsync(
                backup.Data, 
                _options.BackupEncryptionKeyId
            );

            var backupData = JsonSerializer.Deserialize<BackupData>(decryptedData);
            if (backupData == null)
            {
                _logger.LogError("Failed to deserialize backup data for backup {BackupId}", request.BackupId);
                return false;
            }

            // Start database transaction for restore
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var restoredCount = 0;
                var skippedCount = 0;
                var errorCount = 0;

                if (backupData.Secrets != null)
                {
                    foreach (var secretData in backupData.Secrets)
                    {
                        try
                        {
                            var existingSecret = await _context.Secrets
                                .FirstOrDefaultAsync(s => s.Path == secretData.Path);

                            if (existingSecret != null && !request.OverwriteExisting)
                            {
                                skippedCount++;
                                continue;
                            }

                            if (existingSecret != null)
                            {
                                // Update existing secret
                                existingSecret.Name = secretData.Name;
                                existingSecret.EncryptedValue = secretData.EncryptedValue;
                                existingSecret.KeyId = secretData.KeyId;
                                existingSecret.Type = secretData.Type;
                                existingSecret.Description = secretData.Description;
                                existingSecret.Tags = secretData.Tags;
                                existingSecret.ExpiresAt = secretData.ExpiresAt;
                                existingSecret.UpdatedAt = DateTime.UtcNow;
                                existingSecret.UpdatedBy = userId;
                            }
                            else
                            {
                                // Create new secret
                                var newSecret = new Secret
                                {
                                    Id = secretData.Id,
                                    Name = secretData.Name,
                                    Path = secretData.Path,
                                    EncryptedValue = secretData.EncryptedValue,
                                    KeyId = secretData.KeyId,
                                    Type = secretData.Type,
                                    Description = secretData.Description,
                                    Tags = secretData.Tags,
                                    ExpiresAt = secretData.ExpiresAt,
                                    CreatedAt = DateTime.UtcNow,
                                    CreatedBy = userId,
                                    UpdatedAt = DateTime.UtcNow,
                                    UpdatedBy = userId,
                                    CurrentVersion = 1,
                                    Status = SecretStatus.Active
                                };

                                _context.Secrets.Add(newSecret);
                            }

                            restoredCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to restore secret {SecretPath}", secretData.Path);
                            errorCount++;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _auditService.LogAsync(
                    userId,
                    "restore",
                    "backup",
                    backup.Id.ToString(),
                    null,
                    true,
                    null,
                    new Dictionary<string, object>
                    {
                        ["backup_name"] = backup.Name,
                        ["restored_count"] = restoredCount,
                        ["skipped_count"] = skippedCount,
                        ["error_count"] = errorCount,
                        ["overwrite_existing"] = request.OverwriteExisting
                    }
                );

                _logger.LogInformation("Backup {BackupId} restored successfully: {RestoredCount} restored, {SkippedCount} skipped, {ErrorCount} errors",
                    backup.Id, restoredCount, skippedCount, errorCount);

                return errorCount == 0;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup {BackupId}", request.BackupId);
            await _auditService.LogAsync(
                userId,
                "restore",
                "backup",
                request.BackupId.ToString(),
                null,
                false,
                ex.Message
            );
            return false;
        }
    }

    public async Task<List<SecretBackup>> ListBackupsAsync(string userId)
    {
        try
        {
            var backups = await _context.SecretBackups
                .Where(b => b.CreatedBy == userId || IsAdminUser(userId))
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new SecretBackup
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    BackupType = b.BackupType,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    CreatedBy = b.CreatedBy,
                    CompletedAt = b.CompletedAt,
                    ExpiresAt = b.ExpiresAt,
                    SecretCount = b.SecretCount,
                    Size = b.Size,
                    IntegrityHash = b.IntegrityHash
                    // Exclude Data field for security
                })
                .ToListAsync();

            await _auditService.LogAsync(
                userId,
                "list",
                "backups",
                "all",
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["backup_count"] = backups.Count
                }
            );

            return backups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list backups for user {UserId}", userId);
            await _auditService.LogAsync(
                userId,
                "list",
                "backups",
                "all",
                null,
                false,
                ex.Message
            );
            return new List<SecretBackup>();
        }
    }

    public async Task<SecretBackup?> GetBackupAsync(Guid backupId, string userId)
    {
        try
        {
            var backup = await _context.SecretBackups
                .Where(b => b.Id == backupId && (b.CreatedBy == userId || IsAdminUser(userId)))
                .Select(b => new SecretBackup
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    BackupType = b.BackupType,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    CreatedBy = b.CreatedBy,
                    CompletedAt = b.CompletedAt,
                    ExpiresAt = b.ExpiresAt,
                    SecretCount = b.SecretCount,
                    Size = b.Size,
                    IntegrityHash = b.IntegrityHash
                    // Exclude Data field for security
                })
                .FirstOrDefaultAsync();

            if (backup != null)
            {
                await _auditService.LogAsync(
                    userId,
                    "get",
                    "backup",
                    backupId.ToString(),
                    null,
                    true,
                    null,
                    new Dictionary<string, object>
                    {
                        ["backup_name"] = backup.Name
                    }
                );
            }

            return backup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get backup {BackupId} for user {UserId}", backupId, userId);
            await _auditService.LogAsync(
                userId,
                "get",
                "backup",
                backupId.ToString(),
                null,
                false,
                ex.Message
            );
            return null;
        }
    }

    public async Task<bool> DeleteBackupAsync(Guid backupId, string userId)
    {
        try
        {
            var backup = await _context.SecretBackups
                .FirstOrDefaultAsync(b => b.Id == backupId && (b.CreatedBy == userId || IsAdminUser(userId)));

            if (backup == null)
            {
                _logger.LogWarning("Backup {BackupId} not found or access denied for user {UserId}", backupId, userId);
                return false;
            }

            _context.SecretBackups.Remove(backup);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                "delete",
                "backup",
                backupId.ToString(),
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["backup_name"] = backup.Name,
                    ["deleted_at"] = DateTime.UtcNow.ToString("O")
                }
            );

            _logger.LogInformation("Backup {BackupId} '{BackupName}' deleted by user {UserId}",
                backupId, backup.Name, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete backup {BackupId}", backupId);
            await _auditService.LogAsync(
                userId,
                "delete",
                "backup",
                backupId.ToString(),
                null,
                false,
                ex.Message
            );
            return false;
        }
    }

    public async Task<bool> ValidateBackupIntegrityAsync(Guid backupId)
    {
        try
        {
            var backup = await _context.SecretBackups
                .FirstOrDefaultAsync(b => b.Id == backupId);

            if (backup == null)
            {
                _logger.LogWarning("Backup {BackupId} not found for integrity validation", backupId);
                return false;
            }

            if (string.IsNullOrEmpty(backup.Data) || string.IsNullOrEmpty(backup.IntegrityHash))
            {
                _logger.LogWarning("Backup {BackupId} missing data or integrity hash", backupId);
                return false;
            }

            var calculatedHash = CalculateIntegrityHash(backup.Data);
            var isValid = calculatedHash.Equals(backup.IntegrityHash, StringComparison.Ordinal);

            _logger.LogInformation("Backup {BackupId} integrity validation: {Result}", 
                backupId, isValid ? "PASSED" : "FAILED");

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate backup integrity for backup {BackupId}", backupId);
            return false;
        }
    }

    public async Task CleanupExpiredBackupsAsync()
    {
        try
        {
            var expiredBackups = await _context.SecretBackups
                .Where(b => b.ExpiresAt.HasValue && b.ExpiresAt.Value < DateTime.UtcNow)
                .ToListAsync();

            if (expiredBackups.Any())
            {
                var deletedCount = expiredBackups.Count;
                _context.SecretBackups.RemoveRange(expiredBackups);
                await _context.SaveChangesAsync();

                foreach (var backup in expiredBackups)
                {
                    await _auditService.LogAsync(
                        "system-cleanup",
                        "delete",
                        "backup",
                        backup.Id.ToString(),
                        null,
                        true,
                        "Expired backup cleaned up automatically",
                        new Dictionary<string, object>
                        {
                            ["backup_name"] = backup.Name,
                            ["expired_at"] = backup.ExpiresAt?.ToString("O") ?? "unknown",
                            ["cleanup_type"] = "automatic"
                        }
                    );
                }

                _logger.LogInformation("Cleaned up {DeletedCount} expired backups", deletedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired backups");
        }
    }

    private async Task<BackupData> CreateBackupDataAsync(BackupRequest request, string userId)
    {
        var backupData = new BackupData
        {
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            BackupType = request.BackupType,
            Version = "1.0"
        };

        switch (request.BackupType)
        {
            case BackupType.Full:
                backupData.Secrets = await GetAllSecretsForBackupAsync(userId);
                break;

            case BackupType.Incremental:
                backupData.Secrets = await GetIncrementalSecretsForBackupAsync(request.Since ?? DateTime.UtcNow.AddDays(-1), userId);
                break;

            case BackupType.Selective:
                backupData.Secrets = await GetSelectiveSecretsForBackupAsync(request.SecretPaths ?? new List<string>(), userId);
                break;
        }

        return backupData;
    }

    private async Task<List<SecretBackupData>> GetAllSecretsForBackupAsync(string userId)
    {
        var secrets = await _context.Secrets
            .Where(s => s.Status == SecretStatus.Active)
            .ToListAsync();

        return secrets.Select(MapSecretToBackupData).ToList();
    }

    private async Task<List<SecretBackupData>> GetIncrementalSecretsForBackupAsync(DateTime since, string userId)
    {
        var secrets = await _context.Secrets
            .Where(s => s.Status == SecretStatus.Active && s.UpdatedAt >= since)
            .ToListAsync();

        return secrets.Select(MapSecretToBackupData).ToList();
    }

    private async Task<List<SecretBackupData>> GetSelectiveSecretsForBackupAsync(List<string> paths, string userId)
    {
        var secrets = await _context.Secrets
            .Where(s => s.Status == SecretStatus.Active && paths.Contains(s.Path))
            .ToListAsync();

        return secrets.Select(MapSecretToBackupData).ToList();
    }

    private SecretBackupData MapSecretToBackupData(Secret secret)
    {
        return new SecretBackupData
        {
            Id = secret.Id,
            Name = secret.Name,
            Path = secret.Path,
            EncryptedValue = secret.EncryptedValue,
            KeyId = secret.KeyId,
            Type = secret.Type,
            Description = secret.Description,
            Tags = secret.Tags,
            ExpiresAt = secret.ExpiresAt,
            CreatedAt = secret.CreatedAt,
            UpdatedAt = secret.UpdatedAt
        };
    }

    private string CalculateIntegrityHash(string data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hashBytes);
    }

    private bool IsAdminUser(string userId)
    {
        // Implement your admin user logic here
        // This could check against user roles, groups, etc.
        return userId.StartsWith("admin-") || userId.Equals("system", StringComparison.OrdinalIgnoreCase);
    }
}

// Configuration options class
public class BackupServiceOptions
{
    public int DefaultRetentionDays { get; set; } = 30;
    public string BackupEncryptionKeyId { get; set; } = "backup-master-key";
    public long MaxBackupSizeBytes { get; set; } = 100 * 1024 * 1024; // 100MB
    public int MaxBackupsPerUser { get; set; } = 10;
}

// Backup data structure
public class BackupData
{
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public BackupType BackupType { get; set; }
    public string Version { get; set; } = "1.0";
    public List<SecretBackupData>? Secrets { get; set; }
}

// Secret data for backup
public class SecretBackupData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string EncryptedValue { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public SecretType Type { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}