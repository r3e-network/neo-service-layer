using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.EnclaveStorage.Models;

/// <summary>
/// Request to seal and store data.
/// </summary>
public class SealDataRequest
{
    /// <summary>
    /// Gets or sets the storage key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data to seal.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the sealing policy.
    /// </summary>
    public SealingPolicy Policy { get; set; } = new();
}

/// <summary>
/// Sealing policy configuration.
/// </summary>
public class SealingPolicy
{
    /// <summary>
    /// Gets or sets the sealing policy type.
    /// </summary>
    public SealingPolicyType Type { get; set; } = SealingPolicyType.MrEnclave;

    /// <summary>
    /// Gets or sets the expiration hours.
    /// </summary>
    public int ExpirationHours { get; set; } = 8760; // 1 year
}

/// <summary>
/// Sealing policy types.
/// </summary>
public enum SealingPolicyType
{
    /// <summary>Seal to specific enclave measurement.</summary>
    MrEnclave,
    /// <summary>Seal to enclave signer identity.</summary>
    MrSigner
}

/// <summary>
/// Result of sealing data.
/// </summary>
public class SealDataResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the storage ID.
    /// </summary>
    public string StorageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sealed data size.
    /// </summary>
    public long SealedSize { get; set; }

    /// <summary>
    /// Gets or sets the data fingerprint.
    /// </summary>
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration time.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Result of unsealing data.
/// </summary>
public class UnsealDataResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the unsealed data.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets whether the data was sealed.
    /// </summary>
    public bool Sealed { get; set; }

    /// <summary>
    /// Gets or sets the last accessed time.
    /// </summary>
    public DateTime LastAccessed { get; set; }

    /// <summary>
    /// Gets or sets the remaining reads allowed.
    /// </summary>
    public int? RemainingReads { get; set; }
}

/// <summary>
/// Request to list sealed items.
/// </summary>
public class ListSealedItemsRequest
{
    /// <summary>
    /// Gets or sets the service filter.
    /// </summary>
    public string? Service { get; set; }

    /// <summary>
    /// Gets or sets the key prefix filter.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 100;
}

/// <summary>
/// List of sealed items.
/// </summary>
public class SealedItemsList
{
    /// <summary>
    /// Gets or sets the sealed items.
    /// </summary>
    public List<SealedItem> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the total size in bytes.
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// Gets or sets the total item count.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the total pages.
    /// </summary>
    public int TotalPages { get; set; }
}

/// <summary>
/// Individual sealed item information.
/// </summary>
public class SealedItem
{
    /// <summary>
    /// Gets or sets the item key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item size.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Gets or sets the last accessed time.
    /// </summary>
    public DateTime LastAccessed { get; set; }

    /// <summary>
    /// Gets or sets the expiration time.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sealing policy type.
    /// </summary>
    public SealingPolicyType PolicyType { get; set; }
}

/// <summary>
/// Result of deleting sealed data.
/// </summary>
public class DeleteSealedDataResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets whether the data was deleted.
    /// </summary>
    public bool Deleted { get; set; }

    /// <summary>
    /// Gets or sets whether the data was securely shredded.
    /// </summary>
    public bool Shredded { get; set; }

    /// <summary>
    /// Gets or sets the deletion timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Enclave storage statistics.
/// </summary>
public class EnclaveStorageStatistics
{
    /// <summary>
    /// Gets or sets the total items count.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets or sets the total size in bytes.
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// Gets or sets the available space in bytes.
    /// </summary>
    public long AvailableSpace { get; set; }

    /// <summary>
    /// Gets or sets the number of services using storage.
    /// </summary>
    public int ServiceCount { get; set; }

    /// <summary>
    /// Gets or sets storage by service.
    /// </summary>
    public Dictionary<string, ServiceStorageInfo> ServiceStorage { get; set; } = new();

    /// <summary>
    /// Gets or sets the last backup time.
    /// </summary>
    public DateTime? LastBackup { get; set; }
}

/// <summary>
/// Storage information for a service.
/// </summary>
public class ServiceStorageInfo
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item count.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets the total size.
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// Gets or sets the quota used percentage.
    /// </summary>
    public double QuotaUsedPercent { get; set; }
}

/// <summary>
/// Backup request for sealed data.
/// </summary>
public class BackupRequest
{
    /// <summary>
    /// Gets or sets the backup location.
    /// </summary>
    public string BackupLocation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to include metadata.
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets the service filter.
    /// </summary>
    public string? ServiceFilter { get; set; }
}

/// <summary>
/// Result of backup operation.
/// </summary>
public class BackupResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the number of items backed up.
    /// </summary>
    public int ItemsBackedUp { get; set; }

    /// <summary>
    /// Gets or sets the total size backed up.
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// Gets or sets the backup ID.
    /// </summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backup timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
