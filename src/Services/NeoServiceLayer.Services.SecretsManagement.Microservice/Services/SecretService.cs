using Microsoft.EntityFrameworkCore;
using Neo.SecretsManagement.Service.Data;
using Neo.SecretsManagement.Service.Models;
using System.Text.RegularExpressions;

namespace Neo.SecretsManagement.Service.Services;

public class SecretService : ISecretService
{
    private readonly SecretsDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ISecretPolicyService _policyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<SecretService> _logger;

    public SecretService(
        SecretsDbContext context,
        IEncryptionService encryptionService,
        ISecretPolicyService policyService,
        IAuditService auditService,
        ILogger<SecretService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _policyService = policyService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<SecretResponse> CreateSecretAsync(CreateSecretRequest request, string userId)
    {
        try
        {
            // Validate request
            ValidateSecretPath(request.Path);
            
            if (string.IsNullOrWhiteSpace(request.Value))
                throw new ArgumentException("Secret value cannot be empty");

            // Check if secret already exists
            var existingSecret = await _context.Secrets
                .FirstOrDefaultAsync(s => s.Path == request.Path && s.Status != SecretStatus.Deleted);
            
            if (existingSecret != null)
                throw new InvalidOperationException($"Secret already exists at path: {request.Path}");

            // Check policy permissions
            if (!await _policyService.EvaluatePolicyAsync(request.Path, userId, SecretOperation.Create, new()))
            {
                throw new UnauthorizedAccessException($"Access denied for creating secret at path: {request.Path}");
            }

            // Encrypt the secret value
            var encryptedValue = await _encryptionService.EncryptWithDataKeyAsync(request.Value, out var encryptedDataKey);

            // Create secret entity
            var secret = new Secret
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Path = request.Path,
                Description = request.Description ?? string.Empty,
                Type = request.Type,
                EncryptedValue = $"{encryptedDataKey}:{encryptedValue}", // Store data key with encrypted value
                KeyId = "data-key-encryption", // Uses envelope encryption
                CreatedBy = userId,
                LastModifiedBy = userId,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt,
                Status = SecretStatus.Active,
                Tags = request.Tags,
                Metadata = request.Metadata,
                Version = 1
            };

            _context.Secrets.Add(secret);

            // Create first version
            var version = new SecretVersion
            {
                Id = Guid.NewGuid(),
                SecretId = secret.Id,
                Version = 1,
                EncryptedValue = secret.EncryptedValue,
                KeyId = secret.KeyId,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                ChangeReason = "Initial creation",
                Metadata = new Dictionary<string, object>
                {
                    ["created_via"] = "api",
                    ["client_info"] = "secrets-management-service"
                }
            };

            _context.SecretVersions.Add(version);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "CreateSecret", "Secret", secret.Id.ToString(), 
                secret.Path, true, details: new Dictionary<string, object>
                {
                    ["secret_type"] = request.Type.ToString(),
                    ["has_expiration"] = request.ExpiresAt.HasValue,
                    ["tags_count"] = request.Tags.Count
                });

            _logger.LogInformation("Created secret {SecretPath} (ID: {SecretId}) by user {UserId}", 
                secret.Path, secret.Id, userId);

            return MapToResponse(secret, includeValue: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create secret at path {SecretPath} by user {UserId}", 
                request.Path, userId);
            
            await _auditService.LogAsync(userId, "CreateSecret", "Secret", "", 
                request.Path, false, ex.Message);
            
            throw;
        }
    }

    public async Task<SecretResponse?> GetSecretAsync(string path, string userId, bool includeValue = false)
    {
        try
        {
            // Check policy permissions
            var operation = includeValue ? SecretOperation.Read : SecretOperation.List;
            if (!await _policyService.EvaluatePolicyAsync(path, userId, operation, new()))
            {
                await _auditService.LogAsync(userId, "GetSecret", "Secret", "", 
                    path, false, "Access denied");
                return null;
            }

            var secret = await _context.Secrets
                .FirstOrDefaultAsync(s => s.Path == path && s.Status == SecretStatus.Active);

            if (secret == null)
            {
                return null;
            }

            // Log access
            var accessLog = new SecretAccess
            {
                SecretId = secret.Id,
                UserId = userId,
                Operation = operation,
                AccessedAt = DateTime.UtcNow,
                Success = true
            };
            
            _context.SecretAccesses.Add(accessLog);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "GetSecret", "Secret", secret.Id.ToString(), 
                secret.Path, true, details: new Dictionary<string, object>
                {
                    ["include_value"] = includeValue,
                    ["secret_type"] = secret.Type.ToString()
                });

            return MapToResponse(secret, includeValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secret at path {SecretPath} by user {UserId}", 
                path, userId);
            
            await _auditService.LogAsync(userId, "GetSecret", "Secret", "", 
                path, false, ex.Message);
            
            return null;
        }
    }

    public async Task<SecretResponse?> GetSecretByIdAsync(Guid secretId, string userId, bool includeValue = false)
    {
        try
        {
            var secret = await _context.Secrets
                .FirstOrDefaultAsync(s => s.Id == secretId && s.Status == SecretStatus.Active);

            if (secret == null)
            {
                return null;
            }

            return await GetSecretAsync(secret.Path, userId, includeValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secret by ID {SecretId} by user {UserId}", 
                secretId, userId);
            return null;
        }
    }

    public async Task<List<SecretResponse>> ListSecretsAsync(ListSecretsRequest request, string userId)
    {
        try
        {
            var query = _context.Secrets
                .Where(s => s.Status == SecretStatus.Active)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.PathPrefix))
            {
                query = query.Where(s => s.Path.StartsWith(request.PathPrefix));
            }

            if (request.Type.HasValue)
            {
                query = query.Where(s => s.Type == request.Type.Value);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(s => s.Status == request.Status.Value);
            }

            if (request.Tags != null && request.Tags.Any())
            {
                // This would need a more sophisticated implementation for JSON querying
                // For now, we'll filter in memory after retrieval
            }

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending 
                    ? query.OrderByDescending(s => s.Name)
                    : query.OrderBy(s => s.Name),
                "created" => request.SortDescending 
                    ? query.OrderByDescending(s => s.CreatedAt)
                    : query.OrderBy(s => s.CreatedAt),
                "modified" => request.SortDescending 
                    ? query.OrderByDescending(s => s.LastModifiedAt)
                    : query.OrderBy(s => s.LastModifiedAt),
                _ => query.OrderBy(s => s.Name)
            };

            var secrets = await query
                .Skip(request.Skip)
                .Take(Math.Min(request.Take, 1000)) // Limit to 1000 max
                .ToListAsync();

            // Filter by tags if specified (in-memory filtering)
            if (request.Tags != null && request.Tags.Any())
            {
                secrets = secrets.Where(s => request.Tags.All(tag => s.Tags.ContainsKey(tag.Key) && s.Tags[tag.Key] == tag.Value)).ToList();
            }

            // Filter by policy permissions
            var authorizedSecrets = new List<Secret>();
            foreach (var secret in secrets)
            {
                if (await _policyService.EvaluatePolicyAsync(secret.Path, userId, SecretOperation.List, new()))
                {
                    authorizedSecrets.Add(secret);
                }
            }

            // Audit log
            await _auditService.LogAsync(userId, "ListSecrets", "Secret", "", 
                request.PathPrefix ?? "all", true, details: new Dictionary<string, object>
                {
                    ["result_count"] = authorizedSecrets.Count,
                    ["filter_count"] = secrets.Count,
                    ["path_prefix"] = request.PathPrefix ?? "none"
                });

            return authorizedSecrets.Select(s => MapToResponse(s, includeValue: false)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list secrets by user {UserId}", userId);
            
            await _auditService.LogAsync(userId, "ListSecrets", "Secret", "", 
                request.PathPrefix ?? "all", false, ex.Message);
            
            return new List<SecretResponse>();
        }
    }

    public async Task<bool> UpdateSecretAsync(string path, UpdateSecretRequest request, string userId)
    {
        try
        {
            // Check policy permissions
            if (!await _policyService.EvaluatePolicyAsync(path, userId, SecretOperation.Update, new()))
            {
                await _auditService.LogAsync(userId, "UpdateSecret", "Secret", "", 
                    path, false, "Access denied");
                return false;
            }

            var secret = await _context.Secrets
                .FirstOrDefaultAsync(s => s.Path == path && s.Status == SecretStatus.Active);

            if (secret == null)
            {
                return false;
            }

            var hasValueChange = false;
            var oldVersion = secret.Version;

            // Update secret value if provided
            if (!string.IsNullOrEmpty(request.Value))
            {
                var encryptedValue = await _encryptionService.EncryptWithDataKeyAsync(request.Value, out var encryptedDataKey);
                secret.EncryptedValue = $"{encryptedDataKey}:{encryptedValue}";
                secret.Version++;
                hasValueChange = true;

                // Create new version
                var version = new SecretVersion
                {
                    Id = Guid.NewGuid(),
                    SecretId = secret.Id,
                    Version = secret.Version,
                    EncryptedValue = secret.EncryptedValue,
                    KeyId = secret.KeyId,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    ChangeReason = request.ChangeReason ?? "Value updated",
                    Metadata = new Dictionary<string, object>
                    {
                        ["updated_via"] = "api",
                        ["previous_version"] = oldVersion
                    }
                };

                _context.SecretVersions.Add(version);
            }

            // Update metadata
            if (!string.IsNullOrEmpty(request.Description))
            {
                secret.Description = request.Description;
            }

            if (request.ExpiresAt.HasValue)
            {
                secret.ExpiresAt = request.ExpiresAt.Value;
            }

            if (request.Tags != null)
            {
                secret.Tags = request.Tags;
            }

            if (request.Metadata != null)
            {
                secret.Metadata = request.Metadata;
            }

            secret.LastModifiedBy = userId;
            secret.LastModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log access
            var accessLog = new SecretAccess
            {
                SecretId = secret.Id,
                UserId = userId,
                Operation = SecretOperation.Update,
                AccessedAt = DateTime.UtcNow,
                Success = true
            };
            
            _context.SecretAccesses.Add(accessLog);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "UpdateSecret", "Secret", secret.Id.ToString(), 
                secret.Path, true, details: new Dictionary<string, object>
                {
                    ["has_value_change"] = hasValueChange,
                    ["old_version"] = oldVersion,
                    ["new_version"] = secret.Version,
                    ["change_reason"] = request.ChangeReason ?? "Not specified"
                });

            _logger.LogInformation("Updated secret {SecretPath} (ID: {SecretId}) by user {UserId}, version {OldVersion} -> {NewVersion}", 
                secret.Path, secret.Id, userId, oldVersion, secret.Version);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update secret at path {SecretPath} by user {UserId}", 
                path, userId);
            
            await _auditService.LogAsync(userId, "UpdateSecret", "Secret", "", 
                path, false, ex.Message);
            
            return false;
        }
    }

    public async Task<bool> DeleteSecretAsync(string path, string userId)
    {
        try
        {
            // Check policy permissions
            if (!await _policyService.EvaluatePolicyAsync(path, userId, SecretOperation.Delete, new()))
            {
                await _auditService.LogAsync(userId, "DeleteSecret", "Secret", "", 
                    path, false, "Access denied");
                return false;
            }

            var secret = await _context.Secrets
                .FirstOrDefaultAsync(s => s.Path == path && s.Status == SecretStatus.Active);

            if (secret == null)
            {
                return false;
            }

            // Soft delete
            secret.Status = SecretStatus.Deleted;
            secret.LastModifiedBy = userId;
            secret.LastModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log access
            var accessLog = new SecretAccess
            {
                SecretId = secret.Id,
                UserId = userId,
                Operation = SecretOperation.Delete,
                AccessedAt = DateTime.UtcNow,
                Success = true
            };
            
            _context.SecretAccesses.Add(accessLog);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "DeleteSecret", "Secret", secret.Id.ToString(), 
                secret.Path, true, details: new Dictionary<string, object>
                {
                    ["secret_type"] = secret.Type.ToString(),
                    ["final_version"] = secret.Version
                });

            _logger.LogInformation("Deleted secret {SecretPath} (ID: {SecretId}) by user {UserId}", 
                secret.Path, secret.Id, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret at path {SecretPath} by user {UserId}", 
                path, userId);
            
            await _auditService.LogAsync(userId, "DeleteSecret", "Secret", "", 
                path, false, ex.Message);
            
            return false;
        }
    }

    public async Task<ShareSecretResponse> ShareSecretAsync(string path, ShareSecretRequest request, string userId)
    {
        try
        {
            // Check if user has permission to share
            if (!await _policyService.EvaluatePolicyAsync(path, userId, SecretOperation.Share, new()))
            {
                throw new UnauthorizedAccessException("Access denied for sharing secret");
            }

            var secret = await _context.Secrets
                .FirstOrDefaultAsync(s => s.Path == path && s.Status == SecretStatus.Active);

            if (secret == null)
            {
                throw new ArgumentException($"Secret not found at path: {path}");
            }

            var shareId = Guid.NewGuid();
            string? shareToken = null;

            if (request.GenerateShareToken)
            {
                shareToken = await _encryptionService.GenerateSecureRandomAsync(32);
            }

            var share = new SecretShare
            {
                Id = shareId,
                SecretId = secret.Id,
                SharedWithUserId = request.UserId,
                SharedByUserId = userId,
                Permissions = request.Permissions,
                SharedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt,
                Status = SecretShareStatus.Active,
                ShareToken = shareToken
            };

            _context.SecretShares.Add(share);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "ShareSecret", "Secret", secret.Id.ToString(), 
                secret.Path, true, details: new Dictionary<string, object>
                {
                    ["shared_with_user"] = request.UserId,
                    ["permissions"] = string.Join(",", request.Permissions),
                    ["has_expiration"] = request.ExpiresAt.HasValue,
                    ["has_share_token"] = !string.IsNullOrEmpty(shareToken)
                });

            _logger.LogInformation("Shared secret {SecretPath} (ID: {SecretId}) by user {UserId} with user {SharedWithUserId}", 
                secret.Path, secret.Id, userId, request.UserId);

            return new ShareSecretResponse
            {
                ShareId = shareId,
                ShareToken = shareToken,
                SharedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share secret at path {SecretPath} by user {UserId}", 
                path, userId);
            
            await _auditService.LogAsync(userId, "ShareSecret", "Secret", "", 
                path, false, ex.Message);
            
            throw;
        }
    }

    public async Task<bool> RevokeShareAsync(Guid shareId, string userId)
    {
        try
        {
            var share = await _context.SecretShares
                .Include(s => s.Secret)
                .FirstOrDefaultAsync(s => s.Id == shareId);

            if (share == null || share.SharedByUserId != userId)
            {
                return false;
            }

            share.Status = SecretShareStatus.Revoked;

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "RevokeShare", "SecretShare", shareId.ToString(), 
                share.Secret?.Path, true);

            _logger.LogInformation("Revoked secret share {ShareId} by user {UserId}", shareId, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke share {ShareId} by user {UserId}", shareId, userId);
            return false;
        }
    }

    public async Task<SecretResponse> RotateSecretAsync(string path, RotateSecretRequest request, string userId)
    {
        try
        {
            // Check policy permissions
            if (!await _policyService.EvaluatePolicyAsync(path, userId, SecretOperation.Rotate, new()))
            {
                throw new UnauthorizedAccessException("Access denied for rotating secret");
            }

            var secret = await _context.Secrets
                .FirstOrDefaultAsync(s => s.Path == path && s.Status == SecretStatus.Active);

            if (secret == null)
            {
                throw new ArgumentException($"Secret not found at path: {path}");
            }

            string newValue;
            if (!string.IsNullOrEmpty(request.NewValue))
            {
                newValue = request.NewValue;
            }
            else
            {
                // Auto-generate new value based on secret type
                newValue = await GenerateSecretValueByTypeAsync(secret.Type);
            }

            // Update with new value
            var updateRequest = new UpdateSecretRequest
            {
                Value = newValue,
                ChangeReason = request.ChangeReason ?? "Secret rotation"
            };

            await UpdateSecretAsync(path, updateRequest, userId);

            // Mark for rotation status if needed
            secret.Status = SecretStatus.Active;
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "RotateSecret", "Secret", secret.Id.ToString(), 
                secret.Path, true, details: new Dictionary<string, object>
                {
                    ["rotation_type"] = string.IsNullOrEmpty(request.NewValue) ? "auto_generated" : "manual",
                    ["secret_type"] = secret.Type.ToString(),
                    ["force_rotation"] = request.ForceRotation
                });

            _logger.LogInformation("Rotated secret {SecretPath} (ID: {SecretId}) by user {UserId}", 
                secret.Path, secret.Id, userId);

            return MapToResponse(secret, includeValue: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate secret at path {SecretPath} by user {UserId}", 
                path, userId);
            
            await _auditService.LogAsync(userId, "RotateSecret", "Secret", "", 
                path, false, ex.Message);
            
            throw;
        }
    }

    public async Task<List<SecretVersion>> GetSecretVersionsAsync(string path, string userId)
    {
        try
        {
            // Check policy permissions
            if (!await _policyService.EvaluatePolicyAsync(path, userId, SecretOperation.Read, new()))
            {
                return new List<SecretVersion>();
            }

            var secret = await _context.Secrets
                .Include(s => s.Versions)
                .FirstOrDefaultAsync(s => s.Path == path && s.Status == SecretStatus.Active);

            if (secret == null)
            {
                return new List<SecretVersion>();
            }

            return secret.Versions.OrderByDescending(v => v.Version).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secret versions for path {SecretPath} by user {UserId}", 
                path, userId);
            return new List<SecretVersion>();
        }
    }

    public async Task<SecretResponse?> GetSecretVersionAsync(string path, int version, string userId, bool includeValue = false)
    {
        try
        {
            // Check policy permissions
            var operation = includeValue ? SecretOperation.Read : SecretOperation.List;
            if (!await _policyService.EvaluatePolicyAsync(path, userId, operation, new()))
            {
                return null;
            }

            var secretVersion = await _context.SecretVersions
                .Include(sv => sv.Secret)
                .FirstOrDefaultAsync(sv => sv.Secret.Path == path && sv.Version == version && 
                                          sv.Secret.Status == SecretStatus.Active);

            if (secretVersion == null)
            {
                return null;
            }

            var response = MapToResponse(secretVersion.Secret, includeValue);
            response.Version = version;

            if (includeValue && !string.IsNullOrEmpty(secretVersion.EncryptedValue))
            {
                response.Value = await DecryptSecretValueAsync(secretVersion.EncryptedValue);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secret version {Version} for path {SecretPath} by user {UserId}", 
                version, path, userId);
            return null;
        }
    }

    public async Task<SecretStatistics> GetSecretStatisticsAsync(string userId)
    {
        try
        {
            var totalSecrets = await _context.Secrets.CountAsync(s => s.Status != SecretStatus.Deleted);
            var activeSecrets = await _context.Secrets.CountAsync(s => s.Status == SecretStatus.Active);
            var expiredSecrets = await _context.Secrets.CountAsync(s => s.Status == SecretStatus.Expired);
            
            var secretsExpiringIn30Days = await _context.Secrets
                .CountAsync(s => s.Status == SecretStatus.Active && s.ExpiresAt != null && 
                               s.ExpiresAt <= DateTime.UtcNow.AddDays(30));

            var secretsByType = await _context.Secrets
                .Where(s => s.Status == SecretStatus.Active)
                .GroupBy(s => s.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            var totalAccesses = await _context.SecretAccesses.CountAsync();
            var accessesLast24Hours = await _context.SecretAccesses
                .CountAsync(a => a.AccessedAt >= DateTime.UtcNow.AddDays(-1));
            var failedAccessesLast24Hours = await _context.SecretAccesses
                .CountAsync(a => a.AccessedAt >= DateTime.UtcNow.AddDays(-1) && !a.Success);

            return new SecretStatistics
            {
                TotalSecrets = totalSecrets,
                ActiveSecrets = activeSecrets,
                ExpiredSecrets = expiredSecrets,
                SecretsExpiringIn30Days = secretsExpiringIn30Days,
                SecretsByType = secretsByType.ToDictionary(x => x.Type.ToString(), x => x.Count),
                TotalAccesses = totalAccesses,
                AccessesLast24Hours = accessesLast24Hours,
                FailedAccessesLast24Hours = failedAccessesLast24Hours,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secret statistics for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<SecretResponse>> GetExpiringSecretsAsync(int daysAhead, string userId)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);
            
            var expiringSecrets = await _context.Secrets
                .Where(s => s.Status == SecretStatus.Active && s.ExpiresAt != null && 
                           s.ExpiresAt <= cutoffDate)
                .OrderBy(s => s.ExpiresAt)
                .ToListAsync();

            // Filter by user permissions
            var authorizedSecrets = new List<Secret>();
            foreach (var secret in expiringSecrets)
            {
                if (await _policyService.EvaluatePolicyAsync(secret.Path, userId, SecretOperation.List, new()))
                {
                    authorizedSecrets.Add(secret);
                }
            }

            return authorizedSecrets.Select(s => MapToResponse(s, includeValue: false)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get expiring secrets for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ValidateAccessAsync(string path, string userId, SecretOperation operation)
    {
        try
        {
            return await _policyService.EvaluatePolicyAsync(path, userId, operation, new());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate access for path {SecretPath} by user {UserId}", path, userId);
            return false;
        }
    }

    private static void ValidateSecretPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Secret path cannot be empty");

        if (path.Length > 500)
            throw new ArgumentException("Secret path too long (max 500 characters)");

        if (path.Contains("//") || path.StartsWith('/') || path.EndsWith('/'))
            throw new ArgumentException("Invalid secret path format");

        // Allow alphanumeric, hyphens, underscores, dots, and forward slashes
        if (!Regex.IsMatch(path, @"^[a-zA-Z0-9\-_./]+$"))
            throw new ArgumentException("Secret path contains invalid characters");
    }

    private async Task<string> DecryptSecretValueAsync(string encryptedData)
    {
        try
        {
            var parts = encryptedData.Split(':', 2);
            if (parts.Length != 2)
                throw new InvalidOperationException("Invalid encrypted data format");

            var encryptedDataKey = parts[0];
            var ciphertext = parts[1];

            return await _encryptionService.DecryptWithDataKeyAsync(ciphertext, encryptedDataKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt secret value");
            throw;
        }
    }

    private async Task<string> GenerateSecretValueByTypeAsync(SecretType type)
    {
        return type switch
        {
            SecretType.Password => await _encryptionService.GenerateSecureRandomAsync(32),
            SecretType.ApiKey => $"sk-{await _encryptionService.GenerateSecureRandomAsync(48)}",
            SecretType.DatabaseConnection => "postgresql://user:password@localhost:5432/database",
            SecretType.OAuthToken => await _encryptionService.GenerateSecureRandomAsync(64),
            SecretType.SshKey => "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5... (generated key)",
            _ => await _encryptionService.GenerateSecureRandomAsync(32)
        };
    }

    private async Task<SecretResponse> MapToResponse(Secret secret, bool includeValue)
    {
        var response = new SecretResponse
        {
            Id = secret.Id,
            Name = secret.Name,
            Path = secret.Path,
            Description = secret.Description,
            Type = secret.Type,
            CreatedAt = secret.CreatedAt,
            LastModifiedAt = secret.LastModifiedAt,
            ExpiresAt = secret.ExpiresAt,
            Status = secret.Status,
            Tags = secret.Tags,
            Metadata = secret.Metadata,
            Version = secret.Version
        };

        if (includeValue && !string.IsNullOrEmpty(secret.EncryptedValue))
        {
            try
            {
                response.Value = await DecryptSecretValueAsync(secret.EncryptedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt secret value for response");
                response.Value = "[DECRYPTION_ERROR]";
            }
        }

        return response;
    }
}