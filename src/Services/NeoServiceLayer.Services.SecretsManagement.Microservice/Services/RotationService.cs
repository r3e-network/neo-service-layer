using Microsoft.EntityFrameworkCore;
using Neo.SecretsManagement.Service.Data;
using Neo.SecretsManagement.Service.Models;

namespace Neo.SecretsManagement.Service.Services;

public class RotationService : IRotationService
{
    private readonly SecretsDbContext _context;
    private readonly ISecretService _secretService;
    private readonly IKeyManagementService _keyManagementService;
    private readonly ILogger<RotationService> _logger;
    private readonly IAuditService _auditService;

    public RotationService(
        SecretsDbContext context,
        ISecretService secretService,
        IKeyManagementService keyManagementService,
        ILogger<RotationService> logger,
        IAuditService auditService)
    {
        _context = context;
        _secretService = secretService;
        _keyManagementService = keyManagementService;
        _logger = logger;
        _auditService = auditService;
    }

    public async Task<bool> RotateSecretAsync(Guid secretId, string? newValue = null)
    {
        try
        {
            var secret = await _context.Secrets
                .FirstOrDefaultAsync(s => s.Id == secretId);

            if (secret == null)
            {
                _logger.LogWarning("Secret {SecretId} not found for rotation", secretId);
                return false;
            }

            // Generate new value if not provided
            var rotationValue = newValue ?? GenerateNewSecretValue(secret.Type);

            // Create new version
            var newVersion = new SecretVersion
            {
                Id = Guid.NewGuid(),
                SecretId = secretId,
                Version = secret.CurrentVersion + 1,
                EncryptedValue = secret.EncryptedValue, // Will be updated by service
                KeyId = secret.KeyId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system-rotation"
            };

            // Update secret through service to ensure proper encryption
            var updateRequest = new UpdateSecretRequest
            {
                Value = rotationValue,
                Description = $"Rotated version {newVersion.Version}",
                Tags = secret.Tags,
                ExpiresAt = secret.ExpiresAt?.AddDays(90) // Extend expiry
            };

            var rotationSuccess = await _secretService.UpdateSecretAsync(
                secret.Path, updateRequest, "system-rotation");

            if (rotationSuccess)
            {
                // Create rotation job record
                var rotationJob = new RotationJob
                {
                    Id = Guid.NewGuid(),
                    SecretId = secretId,
                    Type = RotationType.Manual,
                    Status = RotationJobStatus.Completed,
                    ScheduledAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    NewVersion = newVersion.Version
                };

                _context.RotationJobs.Add(rotationJob);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    "system-rotation",
                    "rotate",
                    "secret",
                    secretId.ToString(),
                    secret.Path,
                    true,
                    null,
                    new Dictionary<string, object>
                    {
                        ["old_version"] = secret.CurrentVersion,
                        ["new_version"] = newVersion.Version,
                        ["rotation_type"] = "manual"
                    }
                );

                _logger.LogInformation("Secret {SecretId} rotated successfully to version {Version}",
                    secretId, newVersion.Version);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate secret {SecretId}", secretId);
            await _auditService.LogAsync(
                "system-rotation",
                "rotate",
                "secret",
                secretId.ToString(),
                null,
                false,
                ex.Message
            );
            return false;
        }
    }

    public async Task<bool> RotateKeyAsync(Guid keyId)
    {
        try
        {
            var key = await _context.EncryptionKeys
                .FirstOrDefaultAsync(k => k.Id == keyId);

            if (key == null)
            {
                _logger.LogWarning("Encryption key {KeyId} not found for rotation", keyId);
                return false;
            }

            // Generate new key version
            var newKeyId = await _keyManagementService.GenerateKeyAsync(
                $"{key.Name}-v{key.Version + 1}",
                key.Algorithm,
                key.KeySize
            );

            if (string.IsNullOrEmpty(newKeyId))
            {
                _logger.LogError("Failed to generate new key for rotation");
                return false;
            }

            // Update key version
            key.Version += 1;
            key.CreatedAt = DateTime.UtcNow;
            key.LastUsedAt = DateTime.UtcNow;

            // Create rotation job record
            var rotationJob = new RotationJob
            {
                Id = Guid.NewGuid(),
                KeyId = keyId,
                Type = RotationType.Manual,
                Status = RotationJobStatus.Completed,
                ScheduledAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                NewVersion = key.Version
            };

            _context.RotationJobs.Add(rotationJob);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "system-rotation",
                "rotate",
                "key",
                keyId.ToString(),
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["old_version"] = key.Version - 1,
                    ["new_version"] = key.Version,
                    ["algorithm"] = key.Algorithm,
                    ["key_size"] = key.KeySize
                }
            );

            _logger.LogInformation("Encryption key {KeyId} rotated successfully to version {Version}",
                keyId, key.Version);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate key {KeyId}", keyId);
            await _auditService.LogAsync(
                "system-rotation",
                "rotate",
                "key",
                keyId.ToString(),
                null,
                false,
                ex.Message
            );
            return false;
        }
    }

    public async Task ScheduleRotationAsync(Guid? secretId, Guid? keyId, RotationType type, DateTime scheduledTime)
    {
        try
        {
            var rotationJob = new RotationJob
            {
                Id = Guid.NewGuid(),
                SecretId = secretId,
                KeyId = keyId,
                Type = type,
                Status = RotationJobStatus.Pending,
                ScheduledAt = scheduledTime,
                CreatedAt = DateTime.UtcNow
            };

            _context.RotationJobs.Add(rotationJob);
            await _context.SaveChangesAsync();

            var resourceType = secretId.HasValue ? "secret" : "key";
            var resourceId = secretId?.ToString() ?? keyId?.ToString() ?? "unknown";

            await _auditService.LogAsync(
                "system-scheduler",
                "schedule_rotation",
                resourceType,
                resourceId,
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["rotation_type"] = type.ToString(),
                    ["scheduled_time"] = scheduledTime.ToString("O"),
                    ["job_id"] = rotationJob.Id.ToString()
                }
            );

            _logger.LogInformation("Rotation job {JobId} scheduled for {ResourceType} {ResourceId} at {ScheduledTime}",
                rotationJob.Id, resourceType, resourceId, scheduledTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule rotation job");
        }
    }

    public async Task<List<RotationJob>> GetPendingRotationJobsAsync()
    {
        try
        {
            var pendingJobs = await _context.RotationJobs
                .Where(r => r.Status == RotationJobStatus.Pending && r.ScheduledAt <= DateTime.UtcNow)
                .Include(r => r.Secret)
                .Include(r => r.Key)
                .OrderBy(r => r.ScheduledAt)
                .ToListAsync();

            return pendingJobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending rotation jobs");
            return new List<RotationJob>();
        }
    }

    public async Task<List<RotationJob>> GetRotationHistoryAsync(Guid? secretId = null, Guid? keyId = null)
    {
        try
        {
            var query = _context.RotationJobs.AsQueryable();

            if (secretId.HasValue)
            {
                query = query.Where(r => r.SecretId == secretId.Value);
            }

            if (keyId.HasValue)
            {
                query = query.Where(r => r.KeyId == keyId.Value);
            }

            var history = await query
                .Include(r => r.Secret)
                .Include(r => r.Key)
                .OrderByDescending(r => r.CreatedAt)
                .Take(100) // Limit to last 100 records
                .ToListAsync();

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rotation history");
            return new List<RotationJob>();
        }
    }

    public async Task<bool> CancelRotationJobAsync(Guid jobId, string userId)
    {
        try
        {
            var job = await _context.RotationJobs
                .FirstOrDefaultAsync(r => r.Id == jobId);

            if (job == null)
            {
                _logger.LogWarning("Rotation job {JobId} not found", jobId);
                return false;
            }

            if (job.Status != RotationJobStatus.Pending)
            {
                _logger.LogWarning("Cannot cancel rotation job {JobId} with status {Status}",
                    jobId, job.Status);
                return false;
            }

            job.Status = RotationJobStatus.Cancelled;
            job.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var resourceType = job.SecretId.HasValue ? "secret" : "key";
            var resourceId = job.SecretId?.ToString() ?? job.KeyId?.ToString() ?? "unknown";

            await _auditService.LogAsync(
                userId,
                "cancel_rotation",
                resourceType,
                resourceId,
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["job_id"] = jobId.ToString(),
                    ["cancelled_at"] = DateTime.UtcNow.ToString("O")
                }
            );

            _logger.LogInformation("Rotation job {JobId} cancelled by user {UserId}", jobId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel rotation job {JobId}", jobId);
            await _auditService.LogAsync(
                userId,
                "cancel_rotation",
                "rotation_job",
                jobId.ToString(),
                null,
                false,
                ex.Message
            );
            return false;
        }
    }

    private string GenerateNewSecretValue(SecretType type)
    {
        return type switch
        {
            SecretType.Password => GeneratePassword(32),
            SecretType.ApiKey => GenerateApiKey(),
            SecretType.Certificate => GeneratePlaceholderCert(),
            SecretType.DatabaseConnection => GenerateDbConnection(),
            SecretType.OAuth2Token => GenerateOAuth2Token(),
            SecretType.JWT => GenerateJwtToken(),
            SecretType.EncryptionKey => GenerateEncryptionKey(),
            SecretType.Generic => GenerateGenericSecret(),
            _ => GenerateGenericSecret()
        };
    }

    private string GeneratePassword(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private string GenerateApiKey()
    {
        return $"ak_{Guid.NewGuid():N}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }

    private string GeneratePlaceholderCert()
    {
        return "-----BEGIN CERTIFICATE-----\n[Generated Certificate Placeholder]\n-----END CERTIFICATE-----";
    }

    private string GenerateDbConnection()
    {
        return $"Server=localhost;Database=rotated_db_{Guid.NewGuid():N[..8]};User Id=user;Password={GeneratePassword(16)};";
    }

    private string GenerateOAuth2Token()
    {
        return $"oauth2_{Guid.NewGuid():N}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }

    private string GenerateJwtToken()
    {
        return $"jwt_{Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=')}";
    }

    private string GenerateEncryptionKey()
    {
        var keyBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }

    private string GenerateGenericSecret()
    {
        return $"secret_{Guid.NewGuid():N}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }
}