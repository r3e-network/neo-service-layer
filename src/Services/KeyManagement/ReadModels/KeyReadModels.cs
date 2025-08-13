using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.KeyManagement.ReadModels
{
    /// <summary>
    /// Read model for cryptographic keys
    /// </summary>
    public class KeyReadModel
    {
        public string KeyId { get; set; } = string.Empty;
        public string KeyType { get; set; } = string.Empty;
        public string Algorithm { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public bool HasPrivateKey { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ActivatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevocationReason { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsExpired { get; set; }
        public int UsageCount { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public List<string> AuthorizedUsers { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
        public long Version { get; set; }
    }

    /// <summary>
    /// Key usage statistics
    /// </summary>
    public class KeyUsageStatistics
    {
        public string KeyId { get; set; } = string.Empty;
        public int TotalUsageCount { get; set; }
        public int SignOperations { get; set; }
        public int VerifyOperations { get; set; }
        public int EncryptOperations { get; set; }
        public int DecryptOperations { get; set; }
        public DateTime? FirstUsedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public Dictionary<string, int> UsageByUser { get; set; } = new();
        public Dictionary<DateTime, int> DailyUsage { get; set; } = new();
        public double AverageOperationTime { get; set; }
        public int FailedOperations { get; set; }
    }

    /// <summary>
    /// Key access audit entry
    /// </summary>
    public class KeyAccessAudit
    {
        public string KeyId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public string? FailureReason { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
    }

    /// <summary>
    /// Key rotation history
    /// </summary>
    public class KeyRotationHistory
    {
        public string KeyId { get; set; } = string.Empty;
        public DateTime RotatedAt { get; set; }
        public string RotatedBy { get; set; } = string.Empty;
        public string OldPublicKey { get; set; } = string.Empty;
        public string NewPublicKey { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Key metadata change history
    /// </summary>
    public class KeyMetadataHistory
    {
        public string KeyId { get; set; } = string.Empty;
        public string MetadataKey { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
    }
}