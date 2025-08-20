using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Caching;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Integration.Tests;

/// <summary>
/// Integration tests for caching services.
/// </summary>
public class CacheIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly TestWebApplicationFactory _factory;
    private ICacheService _cacheService;
    private IDistributedCache _distributedCache;

    public CacheIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _factory = new TestWebApplicationFactory(output);
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _cacheService = _factory.GetRequiredService<ICacheService>();
        _distributedCache = _factory.GetRequiredService<IDistributedCache>();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task CacheService_Should_Store_And_Retrieve_Simple_Values()
    {
        // Arrange
        const string key = "test-key-simple";
        var testData = new TestCacheModel 
        { 
            Id = Guid.NewGuid(), 
            Name = "Test Item", 
            Value = 42,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // Act - Set value
            var setResult = await _cacheService.SetAsync(key, testData, TimeSpan.FromMinutes(5));
            
            // Assert - Set operation succeeded
            Assert.True(setResult);

            // Act - Get value
            var retrievedData = await _cacheService.GetAsync<TestCacheModel>(key);

            // Assert - Retrieved data matches
            Assert.NotNull(retrievedData);
            Assert.Equal(testData.Id, retrievedData.Id);
            Assert.Equal(testData.Name, retrievedData.Name);
            Assert.Equal(testData.Value, retrievedData.Value);
            Assert.Equal(testData.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), 
                        retrievedData.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

            _output.WriteLine($"Successfully cached and retrieved: {testData.Name}");
        }
        finally
        {
            // Cleanup
            await _cacheService.RemoveAsync(key);
        }
    }

    [Fact]
    public async Task CacheService_Should_Handle_GetOrSet_Pattern()
    {
        // Arrange
        const string key = "test-key-get-or-set";
        var testData = new TestCacheModel 
        { 
            Id = Guid.NewGuid(), 
            Name = "GetOrSet Test", 
            Value = 100,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // Ensure key doesn't exist
            await _cacheService.RemoveAsync(key);

            // Act - First call should execute factory
            var factoryCallCount = 0;
            var result1 = await _cacheService.GetOrSetAsync(key, async () =>
            {
                factoryCallCount++;
                await Task.Delay(10); // Simulate some work
                return testData;
            }, TimeSpan.FromMinutes(5));

            // Act - Second call should use cache
            var result2 = await _cacheService.GetOrSetAsync(key, async () =>
            {
                factoryCallCount++;
                return new TestCacheModel { Id = Guid.NewGuid(), Name = "Should not be called" };
            }, TimeSpan.FromMinutes(5));

            // Assert
            Assert.Equal(1, factoryCallCount);
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.Id, result2.Id);
            Assert.Equal(testData.Id, result1.Id);

            _output.WriteLine($"GetOrSet pattern working correctly. Factory called {factoryCallCount} times");
        }
        finally
        {
            // Cleanup
            await _cacheService.RemoveAsync(key);
        }
    }

    [Fact]
    public async Task CacheService_Should_Handle_Multiple_Keys()
    {
        // Arrange
        var testItems = new Dictionary<string, TestCacheModel>
        {
            ["item-1"] = new() { Id = Guid.NewGuid(), Name = "Item 1", Value = 1 },
            ["item-2"] = new() { Id = Guid.NewGuid(), Name = "Item 2", Value = 2 },
            ["item-3"] = new() { Id = Guid.NewGuid(), Name = "Item 3", Value = 3 }
        };

        try
        {
            // Act - Set multiple values
            var setResult = await _cacheService.SetManyAsync(testItems, TimeSpan.FromMinutes(5));
            Assert.True(setResult);

            // Act - Get multiple values
            var retrievedItems = await _cacheService.GetManyAsync<TestCacheModel>(testItems.Keys);

            // Assert
            Assert.Equal(testItems.Count, retrievedItems.Count);
            
            foreach (var kvp in testItems)
            {
                Assert.True(retrievedItems.ContainsKey(kvp.Key));
                var retrieved = retrievedItems[kvp.Key];
                Assert.NotNull(retrieved);
                Assert.Equal(kvp.Value.Id, retrieved.Id);
                Assert.Equal(kvp.Value.Name, retrieved.Name);
                Assert.Equal(kvp.Value.Value, retrieved.Value);
            }

            _output.WriteLine($"Successfully processed {testItems.Count} cache items");
        }
        finally
        {
            // Cleanup
            await _cacheService.RemoveManyAsync(testItems.Keys);
        }
    }

    [Fact]
    public async Task CacheService_Should_Handle_Expiration()
    {
        // Arrange
        const string key = "test-key-expiration";
        var testData = new TestCacheModel 
        { 
            Id = Guid.NewGuid(), 
            Name = "Expiration Test", 
            Value = 999 
        };

        try
        {
            // Act - Set value with short expiration
            await _cacheService.SetAsync(key, testData, TimeSpan.FromMilliseconds(100));

            // Assert - Value exists immediately
            var immediateResult = await _cacheService.GetAsync<TestCacheModel>(key);
            Assert.NotNull(immediateResult);

            // Wait for expiration
            await Task.Delay(200);

            // Assert - Value should be expired
            var expiredResult = await _cacheService.GetAsync<TestCacheModel>(key);
            Assert.Null(expiredResult);

            _output.WriteLine("Cache expiration working correctly");
        }
        finally
        {
            // Cleanup
            await _cacheService.RemoveAsync(key);
        }
    }

    [Fact]
    public async Task CacheService_Should_Provide_Statistics()
    {
        // Act
        var statistics = await _cacheService.GetStatisticsAsync();

        // Assert
        Assert.NotNull(statistics);
        Assert.True(statistics.HitCount >= 0);
        Assert.True(statistics.MissCount >= 0);
        
        _output.WriteLine($"Cache statistics - Hits: {statistics.HitCount}, Misses: {statistics.MissCount}");
        _output.WriteLine($"Cache healthy: {statistics.IsHealthy}");

        if (statistics.Metadata != null)
        {
            foreach (var kvp in statistics.Metadata)
            {
                _output.WriteLine($"Cache metadata - {kvp.Key}: {kvp.Value}");
            }
        }
    }

    [Fact]
    public async Task DistributedCache_Should_Handle_Binary_Data()
    {
        // Arrange
        const string key = "test-binary-data";
        var binaryData = new byte[] { 1, 2, 3, 4, 5, 255, 254, 253 };
        
        try
        {
            // Act
            await _distributedCache.SetAsync(key, binaryData);
            var retrievedData = await _distributedCache.GetAsync(key);

            // Assert
            Assert.NotNull(retrievedData);
            Assert.Equal(binaryData.Length, retrievedData.Length);
            Assert.Equal(binaryData, retrievedData);

            _output.WriteLine($"Successfully cached and retrieved {binaryData.Length} bytes");
        }
        finally
        {
            // Cleanup
            await _distributedCache.RemoveAsync(key);
        }
    }

    [Fact]
    public async Task CacheService_Should_Handle_Concurrent_Access()
    {
        // Arrange
        const string keyPrefix = "concurrent-test-";
        const int concurrentOperations = 50;
        var tasks = new List<Task<bool>>();

        try
        {
            // Act - Perform concurrent cache operations
            for (int i = 0; i < concurrentOperations; i++)
            {
                var itemId = i;
                var task = Task.Run(async () =>
                {
                    var key = $"{keyPrefix}{itemId}";
                    var data = new TestCacheModel 
                    { 
                        Id = Guid.NewGuid(), 
                        Name = $"Concurrent Item {itemId}", 
                        Value = itemId 
                    };

                    // Set, get, and verify
                    await _cacheService.SetAsync(key, data, TimeSpan.FromMinutes(1));
                    var retrieved = await _cacheService.GetAsync<TestCacheModel>(key);
                    
                    return retrieved != null && retrieved.Value == itemId;
                });
                
                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result => Assert.True(result));
            
            _output.WriteLine($"Successfully completed {concurrentOperations} concurrent cache operations");
        }
        finally
        {
            // Cleanup
            var cleanupTasks = new List<Task>();
            for (int i = 0; i < concurrentOperations; i++)
            {
                var key = $"{keyPrefix}{i}";
                cleanupTasks.Add(_cacheService.RemoveAsync(key));
            }
            await Task.WhenAll(cleanupTasks);
        }
    }

    [Fact]
    public async Task CacheService_Should_Handle_Null_Values()
    {
        // Arrange
        const string key = "test-null-handling";

        try
        {
            // Act & Assert - Getting non-existent key returns null
            var nonExistent = await _cacheService.GetAsync<TestCacheModel>(key);
            Assert.Null(nonExistent);

            // Act & Assert - Setting null value should remove the key
            await _cacheService.SetAsync<TestCacheModel>(key, null);
            var afterNull = await _cacheService.GetAsync<TestCacheModel>(key);
            Assert.Null(afterNull);

            _output.WriteLine("Null value handling working correctly");
        }
        finally
        {
            // Cleanup
            await _cacheService.RemoveAsync(key);
        }
    }
}

/// <summary>
/// Test model for cache operations.
/// </summary>
public class TestCacheModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
}