namespace NeoServiceLayer.Services.Storage;

/// <summary>
/// Storage statistics information.
/// </summary>
public class StorageStatistics
{
    public int TotalItems { get; set; }
    public long TotalSizeBytes { get; set; }
    public int RequestCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public DateTime LastRequestTime { get; set; }
    public double CacheHitRate { get; set; }
}

/// <summary>
/// Storage usage information.
/// </summary>
public class StorageUsage
{
    public int TotalItems { get; set; }
    public long TotalSizeBytes { get; set; }
    public int CompressedItems { get; set; }
    public int EncryptedItems { get; set; }
    public int ChunkedItems { get; set; }
    public long AvailableSpaceBytes { get; set; }
    public long UsedSpaceBytes { get; set; }
    public double UsagePercentage => AvailableSpaceBytes > 0 ? (double)UsedSpaceBytes / (UsedSpaceBytes + AvailableSpaceBytes) * 100 : 0;
}
