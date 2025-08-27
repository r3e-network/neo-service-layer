using Microsoft.EntityFrameworkCore;
using Neo.SecretsManagement.Service.Data;
using Neo.SecretsManagement.Service.Models;
using System.Security.Cryptography;
using System.Text.Json;

namespace Neo.SecretsManagement.Service.Services;

public class KeyManagementService : IKeyManagementService
{
    private readonly SecretsDbContext _context;
    private readonly IHsmService _hsmService;
    private readonly IAuditService _auditService;
    private readonly ILogger<KeyManagementService> _logger;

    public KeyManagementService(
        SecretsDbContext context,
        IHsmService hsmService,
        IAuditService auditService,
        ILogger<KeyManagementService> logger)
    {
        _context = context;
        _hsmService = hsmService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<EncryptionKey> GenerateKeyAsync(GenerateKeyRequest request, string userId)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Key name is required", nameof(request));

            // Check if key name already exists
            var existingKey = await _context.EncryptionKeys
                .FirstOrDefaultAsync(k => k.Name == request.Name);
            
            if (existingKey != null)
                throw new InvalidOperationException($"Key with name '{request.Name}' already exists");

            var keyId = GenerateKeyId();
            string? hsmSlotId = null;

            // Generate key in HSM if requested
            if (request.UseHsm)
            {
                hsmSlotId = await _hsmService.GenerateKeyAsync(keyId, request.Algorithm, request.KeySize);
                _logger.LogInformation("Generated key {KeyId} in HSM slot {HsmSlotId}", keyId, hsmSlotId);
            }
            else
            {
                // For software keys, we'll store the key ID and generate the actual key material on demand
                _logger.LogInformation("Generated software key {KeyId}", keyId);
            }

            var key = new EncryptionKey
            {
                Id = Guid.NewGuid(),
                KeyId = keyId,
                Name = request.Name,
                Type = request.Type,
                Algorithm = request.Algorithm,
                KeySize = request.KeySize,
                Status = KeyStatus.Active,
                HsmSlotId = hsmSlotId,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt,
                RotationIntervalDays = request.RotationIntervalDays,
                Metadata = request.Metadata
            };

            _context.EncryptionKeys.Add(key);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "GenerateKey", "EncryptionKey", key.Id.ToString(), 
                key.Name, true, details: new Dictionary<string, object>
                {
                    ["key_type"] = request.Type.ToString(),
                    ["algorithm"] = request.Algorithm,
                    ["key_size"] = request.KeySize,
                    ["use_hsm"] = request.UseHsm
                });

            _logger.LogInformation("Generated encryption key {KeyId} ({KeyName}) by user {UserId}", 
                keyId, request.Name, userId);

            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate key {KeyName} for user {UserId}", request.Name, userId);
            
            await _auditService.LogAsync(userId, "GenerateKey", "EncryptionKey", "", 
                request.Name, false, ex.Message);
            
            throw;
        }
    }

    public async Task<EncryptionKey?> GetKeyAsync(string keyId)
    {
        try
        {
            return await _context.EncryptionKeys
                .FirstOrDefaultAsync(k => k.KeyId == keyId && k.Status != KeyStatus.Revoked);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get key {KeyId}", keyId);
            return null;
        }
    }

    public async Task<List<EncryptionKey>> ListKeysAsync(KeyStatus? status = null)
    {
        try
        {
            var query = _context.EncryptionKeys.AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(k => k.Status == status.Value);
            }

            return await query
                .OrderByDescending(k => k.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list keys");
            throw;
        }
    }

    public async Task<bool> RotateKeyAsync(Guid keyId, string userId)
    {
        try
        {
            var key = await _context.EncryptionKeys.FindAsync(keyId);
            if (key == null)
            {
                _logger.LogWarning("Key not found for rotation: {KeyId}", keyId);
                return false;
            }

            if (key.Status != KeyStatus.Active)
            {
                _logger.LogWarning("Cannot rotate key {KeyId} with status {Status}", keyId, key.Status);
                return false;
            }

            var newKeyId = GenerateKeyId();
            string? newHsmSlotId = null;

            // Generate new key
            if (!string.IsNullOrEmpty(key.HsmSlotId))
            {
                // Rotate in HSM
                newHsmSlotId = await _hsmService.GenerateKeyAsync(newKeyId, key.Algorithm, key.KeySize);
                await _hsmService.RevokeKeyAsync(key.KeyId); // Revoke old key in HSM
                _logger.LogInformation("Rotated HSM key {OldKeyId} to {NewKeyId}", key.KeyId, newKeyId);
            }
            else
            {
                // Software key rotation
                _logger.LogInformation("Rotated software key {OldKeyId} to {NewKeyId}", key.KeyId, newKeyId);
            }

            // Update existing key
            key.Status = KeyStatus.Rotated;
            key.LastRotatedAt = DateTime.UtcNow;

            // Create new key version
            var newKey = new EncryptionKey
            {
                Id = Guid.NewGuid(),
                KeyId = newKeyId,
                Name = key.Name + "-rotated",
                Type = key.Type,
                Algorithm = key.Algorithm,
                KeySize = key.KeySize,
                Status = KeyStatus.Active,
                HsmSlotId = newHsmSlotId,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = key.ExpiresAt?.AddDays(key.RotationIntervalDays),
                RotationIntervalDays = key.RotationIntervalDays,
                Metadata = new Dictionary<string, object>(key.Metadata)
                {
                    ["rotated_from"] = key.KeyId,
                    ["rotation_date"] = DateTime.UtcNow.ToString("O")
                }
            };

            _context.EncryptionKeys.Add(newKey);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "RotateKey", "EncryptionKey", key.Id.ToString(), 
                key.Name, true, details: new Dictionary<string, object>
                {
                    ["old_key_id"] = key.KeyId,
                    ["new_key_id"] = newKeyId
                });

            _logger.LogInformation("Rotated encryption key {KeyId} by user {UserId}", keyId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate key {KeyId} by user {UserId}", keyId, userId);
            
            await _auditService.LogAsync(userId, "RotateKey", "EncryptionKey", keyId.ToString(), 
                "", false, ex.Message);
            
            return false;
        }
    }

    public async Task<bool> RevokeKeyAsync(Guid keyId, string userId)
    {
        try
        {
            var key = await _context.EncryptionKeys.FindAsync(keyId);
            if (key == null)
            {
                _logger.LogWarning("Key not found for revocation: {KeyId}", keyId);
                return false;
            }

            // Revoke in HSM if applicable
            if (!string.IsNullOrEmpty(key.HsmSlotId))
            {
                await _hsmService.RevokeKeyAsync(key.KeyId);
            }

            key.Status = KeyStatus.Revoked;
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "RevokeKey", "EncryptionKey", key.Id.ToString(), 
                key.Name, true);

            _logger.LogInformation("Revoked encryption key {KeyId} by user {UserId}", keyId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke key {KeyId} by user {UserId}", keyId, userId);
            
            await _auditService.LogAsync(userId, "RevokeKey", "EncryptionKey", keyId.ToString(), 
                "", false, ex.Message);
            
            return false;
        }
    }

    public async Task<bool> UpdateKeyStatusAsync(Guid keyId, KeyStatus status, string userId)
    {
        try
        {
            var key = await _context.EncryptionKeys.FindAsync(keyId);
            if (key == null)
            {
                return false;
            }

            var oldStatus = key.Status;
            key.Status = status;
            
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "UpdateKeyStatus", "EncryptionKey", key.Id.ToString(), 
                key.Name, true, details: new Dictionary<string, object>
                {
                    ["old_status"] = oldStatus.ToString(),
                    ["new_status"] = status.ToString()
                });

            _logger.LogInformation("Updated key {KeyId} status from {OldStatus} to {NewStatus} by user {UserId}", 
                keyId, oldStatus, status, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update key status for {KeyId} by user {UserId}", keyId, userId);
            return false;
        }
    }

    public async Task<KeyStatistics> GetKeyStatisticsAsync()
    {
        try
        {
            var totalKeys = await _context.EncryptionKeys.CountAsync();
            var activeKeys = await _context.EncryptionKeys.CountAsync(k => k.Status == KeyStatus.Active);
            var revokedKeys = await _context.EncryptionKeys.CountAsync(k => k.Status == KeyStatus.Revoked);
            var expiredKeys = await _context.EncryptionKeys.CountAsync(k => k.Status == KeyStatus.Expired);
            
            var keysExpiringIn30Days = await _context.EncryptionKeys
                .CountAsync(k => k.Status == KeyStatus.Active && k.ExpiresAt != null && k.ExpiresAt <= DateTime.UtcNow.AddDays(30));

            var keysByType = await _context.EncryptionKeys
                .Where(k => k.Status == KeyStatus.Active)
                .GroupBy(k => k.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            var keysByAlgorithm = await _context.EncryptionKeys
                .Where(k => k.Status == KeyStatus.Active)
                .GroupBy(k => k.Algorithm)
                .Select(g => new { Algorithm = g.Key, Count = g.Count() })
                .ToListAsync();

            var hsmKeys = await _context.EncryptionKeys.CountAsync(k => k.Status == KeyStatus.Active && !string.IsNullOrEmpty(k.HsmSlotId));

            return new KeyStatistics
            {
                TotalKeys = totalKeys,
                ActiveKeys = activeKeys,
                RevokedKeys = revokedKeys,
                ExpiredKeys = expiredKeys,
                KeysExpiringIn30Days = keysExpiringIn30Days,
                HsmKeys = hsmKeys,
                SoftwareKeys = activeKeys - hsmKeys,
                KeysByType = keysByType.ToDictionary(x => x.Type.ToString(), x => x.Count),
                KeysByAlgorithm = keysByAlgorithm.ToDictionary(x => x.Algorithm, x => x.Count),
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get key statistics");
            throw;
        }
    }

    public async Task<List<EncryptionKey>> GetKeysRequiringRotationAsync()
    {
        try
        {
            var cutoffDate = DateTime.UtcNow;
            
            return await _context.EncryptionKeys
                .Where(k => k.Status == KeyStatus.Active)
                .Where(k => 
                    // Keys past expiration date
                    (k.ExpiresAt != null && k.ExpiresAt <= cutoffDate) ||
                    // Keys past rotation interval
                    (k.LastRotatedAt == null && k.CreatedAt.AddDays(k.RotationIntervalDays) <= cutoffDate) ||
                    (k.LastRotatedAt != null && k.LastRotatedAt.Value.AddDays(k.RotationIntervalDays) <= cutoffDate))
                .OrderBy(k => k.ExpiresAt ?? k.CreatedAt.AddDays(k.RotationIntervalDays))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get keys requiring rotation");
            throw;
        }
    }

    public async Task EnsureDefaultKeysAsync()
    {
        try
        {
            // Check if master key exists
            var masterKey = await _context.EncryptionKeys
                .FirstOrDefaultAsync(k => k.KeyId == "master-key-1");

            if (masterKey == null)
            {
                _logger.LogInformation("Creating default master key");
                
                var request = new GenerateKeyRequest
                {
                    Name = "Default Master Key",
                    Type = KeyType.MasterKey,
                    Algorithm = "AES-256-GCM",
                    KeySize = 256,
                    UseHsm = false,
                    RotationIntervalDays = 365,
                    Metadata = new Dictionary<string, object>
                    {
                        ["purpose"] = "default_master_key",
                        ["auto_created"] = true
                    }
                };

                var key = await GenerateKeyAsync(request, "system");
                
                // Update the key ID to the expected value
                key.KeyId = "master-key-1";
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created default master key with ID {KeyId}", key.KeyId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure default keys");
            throw;
        }
    }

    public async Task<bool> ValidateKeyAsync(string keyId)
    {
        try
        {
            var key = await GetKeyAsync(keyId);
            if (key == null) return false;
            
            // Check if key is active
            if (key.Status != KeyStatus.Active) return false;
            
            // Check if key is expired
            if (key.ExpiresAt.HasValue && key.ExpiresAt.Value <= DateTime.UtcNow) return false;
            
            // Validate with HSM if applicable
            if (!string.IsNullOrEmpty(key.HsmSlotId))
            {
                return await _hsmService.ValidateKeyAsync(keyId);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate key {KeyId}", keyId);
            return false;
        }
    }

    public async Task<string> ExportKeyAsync(string keyId, string format)
    {
        try
        {
            var key = await GetKeyAsync(keyId);
            if (key == null)
                throw new ArgumentException($"Key not found: {keyId}");

            if (!string.IsNullOrEmpty(key.HsmSlotId))
                throw new InvalidOperationException("HSM keys cannot be exported");

            // In a real implementation, this would export the actual key material
            // For security, we'll return metadata only
            var exportData = new
            {
                KeyId = key.KeyId,
                Name = key.Name,
                Type = key.Type.ToString(),
                Algorithm = key.Algorithm,
                KeySize = key.KeySize,
                CreatedAt = key.CreatedAt,
                Status = key.Status.ToString(),
                ExportedAt = DateTime.UtcNow,
                Format = format
            };

            return format.ToLower() switch
            {
                "json" => JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true }),
                _ => throw new NotSupportedException($"Export format not supported: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export key {KeyId} in format {Format}", keyId, format);
            throw;
        }
    }

    public async Task<EncryptionKey> ImportKeyAsync(string keyData, string format, string userId)
    {
        try
        {
            // Parse the key data based on format
            var keyInfo = format.ToLower() switch
            {
                "json" => JsonSerializer.Deserialize<Dictionary<string, object>>(keyData),
                _ => throw new NotSupportedException($"Import format not supported: {format}")
            } ?? throw new ArgumentException("Invalid key data");

            // Extract key information
            var name = keyInfo["Name"]?.ToString() ?? throw new ArgumentException("Key name is required");
            var algorithm = keyInfo["Algorithm"]?.ToString() ?? "AES-256-GCM";
            var keySize = int.TryParse(keyInfo["KeySize"]?.ToString(), out var size) ? size : 256;

            // Create key request
            var request = new GenerateKeyRequest
            {
                Name = name + "-imported",
                Type = KeyType.Symmetric,
                Algorithm = algorithm,
                KeySize = keySize,
                UseHsm = false,
                Metadata = new Dictionary<string, object>
                {
                    ["imported"] = true,
                    ["import_date"] = DateTime.UtcNow.ToString("O"),
                    ["import_format"] = format
                }
            };

            var key = await GenerateKeyAsync(request, userId);

            _logger.LogInformation("Imported key {KeyId} from format {Format} by user {UserId}", 
                key.KeyId, format, userId);

            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import key from format {Format} by user {UserId}", format, userId);
            throw;
        }
    }

    private static string GenerateKeyId()
    {
        return $"key-{Guid.NewGuid():N}";
    }

    public class KeyStatistics
    {
        public int TotalKeys { get; set; }
        public int ActiveKeys { get; set; }
        public int RevokedKeys { get; set; }
        public int ExpiredKeys { get; set; }
        public int KeysExpiringIn30Days { get; set; }
        public int HsmKeys { get; set; }
        public int SoftwareKeys { get; set; }
        public Dictionary<string, int> KeysByType { get; set; } = new();
        public Dictionary<string, int> KeysByAlgorithm { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
}