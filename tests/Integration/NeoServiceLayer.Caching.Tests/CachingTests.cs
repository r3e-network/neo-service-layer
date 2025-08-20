using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using NeoServiceLayer.Infrastructure.Caching;

namespace NeoServiceLayer.Caching.Tests;

/// <summary>
/// Tests for caching implementations to verify performance improvements.
/// </summary>
public class CachingTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly ICacheService _cacheService;

    public CachingTests()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddLogging();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();
        _cacheService = _serviceProvider.GetRequiredService<ICacheService>();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNullForNonExistentKey()
    {
        // Arrange
        var key = "non-existent-key";

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ShouldStoreCacheEntry()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        // Act
        await _cacheService.SetAsync(key, value);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task SetAsync_WithExpiration_ShouldExpireEntry()
    {
        // Arrange
        var key = "expiring-key";
        var value = "expiring-value";
        var expiration = TimeSpan.FromMilliseconds(100);

        // Act
        await _cacheService.SetAsync(key, value, expiration);
        var resultBeforeExpiration = await _cacheService.GetAsync<string>(key);
        
        // Wait for expiration
        await Task.Delay(150);
        var resultAfterExpiration = await _cacheService.GetAsync<string>(key);

        // Assert
        Assert.Equal(value, resultBeforeExpiration);
        Assert.Null(resultAfterExpiration);
    }

    [Fact]
    public async Task GetOrSetAsync_ShouldReturnCachedValue()
    {
        // Arrange
        var key = "get-or-set-key";
        var value = "cached-value";
        var factoryCallCount = 0;
        
        Func<Task<string>> factory = async () =>
        {
            factoryCallCount++;
            await Task.Delay(10); // Simulate async work
            return value;
        };

        // Act
        var result1 = await _cacheService.GetOrSetAsync(key, factory);
        var result2 = await _cacheService.GetOrSetAsync(key, factory);
        var result3 = await _cacheService.GetOrSetAsync(key, factory);

        // Assert
        Assert.Equal(value, result1);
        Assert.Equal(value, result2);
        Assert.Equal(value, result3);
        Assert.Equal(1, factoryCallCount); // Factory should only be called once
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveCacheEntry()
    {
        // Arrange
        var key = "remove-key";
        var value = "remove-value";

        // Act
        await _cacheService.SetAsync(key, value);
        var resultBeforeRemove = await _cacheService.GetAsync<string>(key);
        await _cacheService.RemoveAsync(key);
        var resultAfterRemove = await _cacheService.GetAsync<string>(key);

        // Assert
        Assert.Equal(value, resultBeforeRemove);
        Assert.Null(resultAfterRemove);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnCorrectStatus()
    {
        // Arrange
        var existingKey = "existing-key";
        var nonExistingKey = "non-existing-key";
        var value = "test-value";

        // Act
        await _cacheService.SetAsync(existingKey, value);
        var existsResult = await _cacheService.ExistsAsync(existingKey);
        var notExistsResult = await _cacheService.ExistsAsync(nonExistingKey);

        // Assert
        Assert.True(existsResult);
        Assert.False(notExistsResult);
    }

    [Fact]
    public async Task GetManyAsync_ShouldReturnMultipleValues()
    {
        // Arrange
        var keys = new[] { "key1", "key2", "key3" };
        var values = new[] { "value1", "value2", "value3" };

        // Act
        for (int i = 0; i < keys.Length; i++)
        {
            await _cacheService.SetAsync(keys[i], values[i]);
        }

        var results = await _cacheService.GetManyAsync<string>(keys);

        // Assert
        Assert.Equal(keys.Length, results.Count);
        for (int i = 0; i < keys.Length; i++)
        {
            Assert.Equal(values[i], results[keys[i]]);
        }
    }

    [Fact]
    public async Task SetManyAsync_ShouldSetMultipleValues()
    {
        // Arrange
        var items = new Dictionary<string, string>
        {
            { "batch-key1", "batch-value1" },
            { "batch-key2", "batch-value2" },
            { "batch-key3", "batch-value3" }
        };

        // Act
        var success = await _cacheService.SetManyAsync(items);

        // Assert
        Assert.True(success);
        foreach (var kvp in items)
        {
            var result = await _cacheService.GetAsync<string>(kvp.Key);
            Assert.Equal(kvp.Value, result);
        }
    }

    [Fact]
    public async Task RemoveManyAsync_ShouldRemoveMultipleValues()
    {
        // Arrange
        var keys = new[] { "remove-many-key1", "remove-many-key2", "remove-many-key3" };
        
        foreach (var key in keys)
        {
            await _cacheService.SetAsync(key, $"value-{key}");
        }

        // Act
        var removedCount = await _cacheService.RemoveManyAsync(keys);

        // Assert
        Assert.Equal(keys.Length, removedCount);
        foreach (var key in keys)
        {
            var exists = await _cacheService.ExistsAsync(key);
            Assert.False(exists);
        }
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnValidStatistics()
    {
        // Arrange
        var key = "stats-key";
        var value = "stats-value";

        // Act
        await _cacheService.SetAsync(key, value);
        await _cacheService.GetAsync<string>(key); // Hit
        await _cacheService.GetAsync<string>("non-existent"); // Miss
        
        var stats = await _cacheService.GetStatisticsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.IsHealthy);
        Assert.True(stats.HitCount >= 0);
        Assert.True(stats.MissCount >= 0);
    }

    [Fact]
    public async Task CachePerformance_ShouldImproveResponseTime()
    {
        // Arrange
        var key = "performance-key";
        var expensiveOperationCallCount = 0;
        
        async Task<string> ExpensiveOperation()
        {
            expensiveOperationCallCount++;
            await Task.Delay(100); // Simulate expensive operation
            return "expensive-result";
        }

        // Act
        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        var result1 = await _cacheService.GetOrSetAsync(key, ExpensiveOperation);
        sw1.Stop();

        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        var result2 = await _cacheService.GetOrSetAsync(key, ExpensiveOperation);
        sw2.Stop();

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(1, expensiveOperationCallCount);
        Assert.True(sw2.ElapsedMilliseconds < sw1.ElapsedMilliseconds / 2, 
            $"Cache should be significantly faster. First: {sw1.ElapsedMilliseconds}ms, Second: {sw2.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var key = "concurrent-key";
        var callCount = 0;
        var tasks = new Task<string>[10];
        
        async Task<string> Factory()
        {
            Interlocked.Increment(ref callCount);
            await Task.Delay(50);
            return "concurrent-value";
        }

        // Act
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = _cacheService.GetOrSetAsync(key, Factory);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r => Assert.Equal("concurrent-value", r));
        Assert.Equal(1, callCount); // Factory should only be called once despite concurrent access
    }

    [Fact]
    public async Task ComplexObjectCaching_ShouldSerializeCorrectly()
    {
        // Arrange
        var key = "complex-object-key";
        var complexObject = new TestComplexObject
        {
            Id = Guid.NewGuid(),
            Name = "Test Object",
            Value = 42.5m,
            CreatedAt = DateTime.UtcNow,
            Tags = new[] { "tag1", "tag2", "tag3" },
            Metadata = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 123 },
                { "key3", true }
            }
        };

        // Act
        await _cacheService.SetAsync(key, complexObject);
        var result = await _cacheService.GetAsync<TestComplexObject>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(complexObject.Id, result.Id);
        Assert.Equal(complexObject.Name, result.Name);
        Assert.Equal(complexObject.Value, result.Value);
        Assert.Equal(complexObject.Tags.Length, result.Tags.Length);
        Assert.Equal(complexObject.Metadata.Count, result.Metadata.Count);
    }

    public void Dispose()
    {
        (_serviceProvider as IDisposable)?.Dispose();
    }

    private class TestComplexObject
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public string[] Tags { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}