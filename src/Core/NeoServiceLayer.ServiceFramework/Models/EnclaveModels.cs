using System;
using System.Collections.Generic;

namespace NeoServiceLayer.ServiceFramework.Models
{
    /// <summary>
    /// Sealing policy type.
    /// </summary>
    public enum SealingPolicyType
    {
        PerSession,
        Persistent,
        MrSigner,
        MrEnclave
    }

    /// <summary>
    /// Sealing policy.
    /// </summary>
    public class SealingPolicy
    {
        public SealingPolicyType Type { get; set; }
        public int ExpirationHours { get; set; }
    }

    /// <summary>
    /// Request to seal data in the enclave.
    /// </summary>
    public class SealDataRequest
    {
        public string Key { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public SealingPolicy? Policy { get; set; }
    }

    /// <summary>
    /// Request to list sealed items.
    /// </summary>
    public class ListSealedItemsRequest
    {
        public string? Prefix { get; set; }
        public int Limit { get; set; } = 100;
        public int MaxItems { get; set; } = 100;
    }

    /// <summary>
    /// List of sealed items.
    /// </summary>
    public class SealedItemsList
    {
        public List<string> Keys { get; set; } = new();
        public Dictionary<string, Dictionary<string, object>> Metadata { get; set; } = new();
        public List<object> Items { get; set; } = new();
    }

    /// <summary>
    /// Result of a backup operation.
    /// </summary>
    public class BackupResult
    {
        public string BackupId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public long Size { get; set; }
        public string Location { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Service storage information.
    /// </summary>
    public class ServiceStorageInfo
    {
        public long TotalSize { get; set; }
        public int ItemCount { get; set; }
        public DateTime LastModified { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
        public string ServiceName { get; set; } = string.Empty;
        public long StorageUsed { get; set; }
    }
}