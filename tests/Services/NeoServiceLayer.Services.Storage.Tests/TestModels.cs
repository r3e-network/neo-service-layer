using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.Storage.Tests
{
    public class CacheStatistics
    {
        public long Hits { get; set; }
        public long Misses { get; set; }
        public long TotalRequests { get; set; }
        public double HitRate => TotalRequests > 0 ? (double)Hits / TotalRequests : 0;
        public DateTime LastReset { get; set; } = DateTime.UtcNow;
        public Dictionary<string, long> DetailedStats { get; set; } = new();
        public bool IsHealthy { get; set; } = true;
    }
    
    public class StorageMetrics
    {
        public long TotalSize { get; set; }
        public long UsedSize { get; set; }
        public long AvailableSize { get; set; }
        public int FileCount { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
    
    public class StorageTestData
    {
        public string Id { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public Dictionary<string, string> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
