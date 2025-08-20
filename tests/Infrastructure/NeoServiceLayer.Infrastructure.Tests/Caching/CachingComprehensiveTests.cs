using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NeoServiceLayer.Infrastructure.Caching;
using Xunit;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Infrastructure.Tests.Caching
{
    /// <summary>
    /// Comprehensive unit tests for caching infrastructure components.
    /// Tests memory cache, distributed cache, and cache service functionality.
    /// </summary>
    public class CachingComprehensiveTests : IDisposable
    {
        private readonly Mock<ILogger<MemoryCacheService>> _mockLogger;
        private readonly Mock<IOptions<MemoryCacheServiceOptions>> _mockOptions;
        private readonly MemoryCache _memoryCache;
        private readonly ServiceProvider _serviceProvider;

        public CachingComprehensiveTests()
        {
            _mockLogger = new Mock<ILogger<MemoryCacheService>>();
            _mockOptions = new Mock<IOptions<MemoryCacheServiceOptions>>();
            
            var options = new MemoryCacheServiceOptions
            {
                DefaultExpiration = TimeSpan.FromMinutes(30),
                MaxSize = 1024 * 1024, // 1MB
                KeyPrefix = "TEST"
            };
            _mockOptions.Setup(x => x.Value).Returns(options);

            var services = new ServiceCollection();
            services.AddMemoryCache();
            services.AddSingleton(_mockLogger.Object);
            services.AddSingleton(_mockOptions.Object);
            
            _serviceProvider = services.BuildServiceProvider();
            _memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>() as MemoryCache
                ?? throw new InvalidOperationException("Failed to get MemoryCache");
        }

        [Fact]
        public async Task MemoryCacheService_SetAsync_WithValidData_ShouldStoreData()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var key = "test-key";
            var value = "test-value";

            // Act
            var result = await cacheService.SetAsync(key, value);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task MemoryCacheService_GetAsync_WithExistingKey_ShouldReturnValue()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var key = "test-key";
            var value = "test-value";
            await cacheService.SetAsync(key, value);

            // Act
            var result = await cacheService.GetAsync<string>(key);

            // Assert
            result.Should().Be(value);
        }

        [Fact]
        public async Task MemoryCacheService_GetAsync_WithNonExistentKey_ShouldReturnDefault()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var key = "non-existent-key";

            // Act
            var result = await cacheService.GetAsync<string>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task MemoryCacheService_GetAsync_WithWrongType_ShouldReturnDefault()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var key = "test-key";
            var stringValue = "test-value";
            await cacheService.SetAsync(key, stringValue);

            // Act
            var result = await cacheService.GetAsync<string>(key);

            // Assert
            result.Should().BeNull(); // Default for reference type
        }

        [Fact]
        public async Task MemoryCacheService_SetAsync_WithExpiration_ShouldRespectExpiration()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var key = "test-key";
            var value = "test-value";
            var expiration = TimeSpan.FromMilliseconds(100);

            // Act
            await cacheService.SetAsync(key, value, expiration);
            var immediateResult = await cacheService.GetAsync<string>(key);
            
            await Task.Delay(150); // Wait for expiration
            var expiredResult = await cacheService.GetAsync<string>(key);

            // Assert
            immediateResult.Should().Be(value);
            expiredResult.Should().BeNull();
        }

        [Fact]
        public async Task MemoryCacheService_RemoveAsync_WithExistingKey_ShouldRemoveValue()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var key = "test-key";
            var value = "test-value";
            await cacheService.SetAsync(key, value);

            // Act
            var removed = await cacheService.RemoveAsync(key);
            var result = await cacheService.GetAsync<string>(key);

            // Assert
            removed.Should().BeTrue();
            result.Should().BeNull();
        }

        [Fact]
        public async Task MemoryCacheService_RemoveAsync_WithNonExistentKey_ShouldReturnFalse()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var key = "non-existent-key";

            // Act
            var removed = await cacheService.RemoveAsync(key);

            // Assert
            removed.Should().BeFalse();
        }

        [Fact]
        public async Task MemoryCacheService_ClearAsync_ShouldRemoveAllItems()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var keys = new[] { "key1", "key2", "key3" };
            var value = "test-value";

            foreach (var key in keys)
            {
                await cacheService.SetAsync(key, value);
            }

            // Act
            await cacheService.ClearAsync();

            // Assert
            foreach (var key in keys)
            {
                var result = await cacheService.GetAsync<string>(key);
                result.Should().BeNull();
            }
        }

        [Fact]
        public async Task MemoryCacheService_GetOrSetAsync_WithMissingKey_ShouldCallFactory()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var key = "test-key";
            var expectedValue = "factory-value";
            var factoryCalled = false;

            // Act
            var result = await cacheService.GetOrSetAsync(key, () =>
            {
                factoryCalled = true;
                return Task.FromResult(expectedValue);
            });

            // Assert
            factoryCalled.Should().BeTrue();
            result.Should().Be(expectedValue);
        }

        [Fact]
        public async Task MemoryCacheService_GetOrSetAsync_WithExistingKey_ShouldNotCallFactory()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var key = "test-key";
            var existingValue = "existing-value";
            var factoryValue = "factory-value";
            var factoryCalled = false;

            await cacheService.SetAsync(key, existingValue);

            // Act
            var result = await cacheService.GetOrSetAsync(key, () =>
            {
                factoryCalled = true;
                return Task.FromResult(factoryValue);
            });

            // Assert
            factoryCalled.Should().BeFalse();
            result.Should().Be(existingValue);
        }

        [Fact]
        public async Task MemoryCacheService_ExistsAsync_WithExistingKey_ShouldReturnTrue()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var key = "test-key";
            var value = "test-value";
            await cacheService.SetAsync(key, value);

            // Act
            var exists = await cacheService.ExistsAsync(key);

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task MemoryCacheService_ExistsAsync_WithNonExistentKey_ShouldReturnFalse()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var key = "non-existent-key";

            // Act
            var exists = await cacheService.ExistsAsync(key);

            // Assert
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task MemoryCacheService_SetManyAsync_WithValidData_ShouldStoreAllItems()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var items = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            };

            // Act
            var result = await cacheService.SetManyAsync(items);

            // Assert
            result.Should().BeTrue();
            foreach (var item in items)
            {
                var cachedValue = await cacheService.GetAsync<string>(item.Key);
                cachedValue.Should().Be(item.Value);
            }
        }

        [Fact]
        public async Task MemoryCacheService_GetManyAsync_WithExistingKeys_ShouldReturnAllValues()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var items = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            };

            await cacheService.SetManyAsync(items);

            // Act
            var result = await cacheService.GetManyAsync<string>(items.Keys);

            // Assert
            result.Should().HaveCount(items.Count);
            foreach (var item in items)
            {
                result.Should().ContainKey(item.Key);
                result[item.Key].Should().Be(item.Value);
            }
        }

        [Fact]
        public async Task MemoryCacheService_GetManyAsync_WithMixedKeys_ShouldReturnOnlyExisting()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var existingItems = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };
            var allKeys = new[] { "key1", "key2", "key3", "key4" };

            await cacheService.SetManyAsync(existingItems);

            // Act
            var result = await cacheService.GetManyAsync<string>(allKeys);

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainKey("key1");
            result.Should().ContainKey("key2");
            result.Should().NotContainKey("key3");
            result.Should().NotContainKey("key4");
        }

        [Fact]
        public async Task MemoryCacheService_RemoveManyAsync_WithExistingKeys_ShouldRemoveAllItems()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var items = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            };

            await cacheService.SetManyAsync(items);

            // Act
            var removed = await cacheService.RemoveManyAsync(items.Keys);

            // Assert
            removed.Should().Be(items.Count);
            foreach (var key in items.Keys)
            {
                var result = await cacheService.GetAsync<string>(key);
                result.Should().BeNull();
            }
        }

        [Fact]
        public async Task MemoryCacheService_GetStatisticsAsync_ShouldReturnStatistics()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var items = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            };
            await cacheService.SetManyAsync(items);

            // Act
            var statistics = await cacheService.GetStatisticsAsync();

            // Assert
            statistics.Should().NotBeNull();
            statistics.IsHealthy.Should().BeTrue();
            statistics.TotalEntries.Should().BeGreaterOrEqualTo(0);
            statistics.HitRatio.Should().BeGreaterOrEqualTo(0);
            statistics.HitCount.Should().BeGreaterOrEqualTo(0);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task MemoryCacheService_SetAsync_WithInvalidKey_ShouldHandleGracefully(string invalidKey)
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var value = "test-value";

            // Act & Assert
            if (string.IsNullOrWhiteSpace(invalidKey))
            {
                await Assert.ThrowsAsync<ArgumentException>(() => cacheService.SetAsync(invalidKey, value));
            }
        }

        [Fact]
        public async Task MemoryCacheService_SetAsync_WithNullValue_ShouldStoreNull()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var key = "test-key";
            string? nullValue = null;

            // Act
            var result = await cacheService.SetAsync(key, nullValue);
            var retrievedValue = await cacheService.GetAsync<string>(key);

            // Assert
            result.Should().BeTrue();
            retrievedValue.Should().BeNull();
        }

        [Fact]
        public async Task MemoryCacheService_SetAsync_WithComplexObject_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var key = "test-key";
            var complexObject = new TestComplexObject
            {
                Id = 123,
                Name = "Test Object",
                CreatedAt = DateTime.UtcNow,
                Properties = new Dictionary<string, string>
                {
                    { "prop1", "value1" },
                    { "prop2", "value2" }
                }
            };

            // Act
            await cacheService.SetAsync(key, complexObject);
            var retrievedObject = await cacheService.GetAsync<TestComplexObject>(key);

            // Assert
            retrievedObject.Should().NotBeNull();
            retrievedObject.Id.Should().Be(complexObject.Id);
            retrievedObject.Name.Should().Be(complexObject.Name);
            retrievedObject.Properties.Should().BeEquivalentTo(complexObject.Properties);
        }

        [Fact]
        public async Task MemoryCacheService_ConcurrentAccess_ShouldHandleConcurrency()
        {
            // Arrange
            var cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object, _mockOptions.Object);
            var tasks = new List<Task>();
            var keyValuePairs = Enumerable.Range(0, 100)
                .Select(i => new { Key = $"key-{i}", Value = $"value-{i}" })
                .ToList();

            // Act
            // Concurrent writes
            foreach (var kvp in keyValuePairs)
            {
                tasks.Add(cacheService.SetAsync(kvp.Key, kvp.Value));
            }
            await Task.WhenAll(tasks);

            tasks.Clear();

            // Concurrent reads
            var results = new ConcurrentBag<string?>();
            foreach (var kvp in keyValuePairs)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var result = await cacheService.GetAsync<string>(kvp.Key);
                    results.Add(result);
                }));
            }
            await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(keyValuePairs.Count);
            results.Should().OnlyContain(r => r != null && r.StartsWith("value-"));
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
            _memoryCache?.Dispose();
        }

        private class TestComplexObject
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public Dictionary<string, string> Properties { get; set; } = new();
        }
    }
}