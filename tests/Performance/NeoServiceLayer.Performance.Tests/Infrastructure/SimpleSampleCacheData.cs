using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeoServiceLayer.Performance.Tests.Infrastructure;

/// <summary>
/// Simple sample cache data for performance testing.
/// </summary>
public static class SimpleSampleCacheData
{
    /// <summary>
    /// Generates sample cache data with specified size.
    /// </summary>
    /// <param name="dataSize">Size of data in bytes.</param>
    /// <returns>Sample cache data.</returns>
    public static string GenerateData(int dataSize)
    {
        if (dataSize <= 0)
            return string.Empty;

        var random = new Random(42); // Fixed seed for consistent benchmarks
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var result = new StringBuilder(dataSize);

        for (int i = 0; i < dataSize; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Generates a list of cache keys for testing.
    /// </summary>
    /// <param name="count">Number of keys to generate.</param>
    /// <returns>List of cache keys.</returns>
    public static List<string> GenerateKeys(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => $"test-key-{i:D6}")
            .ToList();
    }

    /// <summary>
    /// Generates sample cache entries for testing.
    /// </summary>
    /// <param name="count">Number of entries to generate.</param>
    /// <param name="valueSize">Size of each value in bytes.</param>
    /// <returns>Dictionary of cache entries.</returns>
    public static Dictionary<string, string> GenerateEntries(int count, int valueSize = 100)
    {
        var keys = GenerateKeys(count);
        var result = new Dictionary<string, string>();

        foreach (var key in keys)
        {
            result[key] = GenerateData(valueSize);
        }

        return result;
    }

    /// <summary>
    /// Common cache keys used in benchmarks.
    /// </summary>
    public static readonly string[] CommonKeys = {
        "user-profile-123",
        "session-data-456",
        "configuration-settings",
        "temporary-token-789",
        "cached-result-abc",
        "performance-metrics",
        "application-state",
        "user-preferences-def"
    };

    /// <summary>
    /// Sample data sizes for testing different scenarios.
    /// </summary>
    public static readonly int[] DataSizes = { 100, 1024, 10240, 102400 }; // 100B, 1KB, 10KB, 100KB
}