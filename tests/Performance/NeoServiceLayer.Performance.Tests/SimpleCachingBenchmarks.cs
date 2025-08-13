using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Caching;
using NeoServiceLayer.Performance.Tests.Infrastructure;

namespace NeoServiceLayer.Performance.Tests
{
    /// <summary>
    /// Simplified caching performance benchmarks to validate the performance testing infrastructure.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob]
    [MarkdownExporter]
    public class SimpleCachingBenchmarks
    {
        private ICacheService _memoryCache = null!;
        private SimpleSampleCacheData[] _testData = null!;
        private string[] _cacheKeys = null!;
        private IServiceProvider _serviceProvider = null!;

        [Params(100, 1000)]
        public int ItemCount { get; set; }

        [Params(512, 1024)]
        public int ItemSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });

            // Memory cache configuration
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 100 * 1024 * 1024; // 100 MB
                options.CompactionPercentage = 0.25;
            });

            services.Configure<MemoryCacheServiceOptions>(options =>
            {
                options.DefaultExpiration = TimeSpan.FromMinutes(30);
                options.KeyPrefix = "BENCH";
                options.MaxSize = 100 * 1024 * 1024;
            });

            services.AddSingleton<ICacheService, MemoryCacheService>();

            _serviceProvider = services.BuildServiceProvider();
            _memoryCache = _serviceProvider.GetRequiredService<ICacheService>();

            // Generate test data
            _testData = new SimpleSampleCacheData[ItemCount];
            _cacheKeys = new string[ItemCount];

            for (int i = 0; i < ItemCount; i++)
            {
                _testData[i] = SimpleSampleCacheData.Generate(ItemSize);
                _cacheKeys[i] = $"benchmark:item:{i}";
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _memoryCache?.ClearAsync().GetAwaiter().GetResult();
            if (_serviceProvider is IDisposable disposable)
                disposable.Dispose();
        }

        /// <summary>
        /// Benchmark memory cache SET operations.
        /// </summary>
        [Benchmark]
        public async Task MemoryCache_Set()
        {
            for (int i = 0; i < ItemCount; i++)
            {
                await _memoryCache.SetAsync(_cacheKeys[i], _testData[i]).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Benchmark memory cache GET operations.
        /// </summary>
        [Benchmark]
        public async Task MemoryCache_Get()
        {
            // Pre-populate cache
            for (int i = 0; i < ItemCount; i++)
            {
                await _memoryCache.SetAsync(_cacheKeys[i], _testData[i]).ConfigureAwait(false);
            }

            // Benchmark GET operations
            for (int i = 0; i < ItemCount; i++)
            {
                var result = await _memoryCache.GetAsync<SimpleSampleCacheData>(_cacheKeys[i]).ConfigureAwait(false);
                if (result == null)
                    throw new InvalidOperationException($"Cache miss for key: {_cacheKeys[i]}");
            }
        }

        /// <summary>
        /// Benchmark memory cache batch operations.
        /// </summary>
        [Benchmark]
        public async Task MemoryCache_BatchOperations()
        {
            var items = new Dictionary<string, SimpleSampleCacheData>();
            for (int i = 0; i < ItemCount; i++)
            {
                items[_cacheKeys[i]] = _testData[i];
            }

            // Set all items
            var result = await _memoryCache.SetManyAsync(items).ConfigureAwait(false);
            if (!result)
                throw new InvalidOperationException("Batch SET operation failed");

            // Get all items
            var retrieved = await _memoryCache.GetManyAsync<SimpleSampleCacheData>(_cacheKeys).ConfigureAwait(false);
            if (retrieved.Count != ItemCount)
                throw new InvalidOperationException($"Expected {ItemCount} results, got {retrieved.Count}");
        }

        /// <summary>
        /// Benchmark cache statistics retrieval.
        /// </summary>
        [Benchmark]
        public async Task MemoryCache_Statistics()
        {
            // Pre-populate with some data
            for (int i = 0; i < Math.Min(100, ItemCount); i++)
            {
                await _memoryCache.SetAsync(_cacheKeys[i], _testData[i]).ConfigureAwait(false);
            }

            // Perform some operations to generate statistics
            for (int i = 0; i < Math.Min(50, ItemCount); i++)
            {
                await _memoryCache.GetAsync<SimpleSampleCacheData>(_cacheKeys[i]).ConfigureAwait(false);
            }

            // Benchmark statistics retrieval
            var stats = await _memoryCache.GetStatisticsAsync().ConfigureAwait(false);

            if (!stats.IsHealthy)
                throw new InvalidOperationException("Cache is not healthy");
        }

        /// <summary>
        /// Benchmark performance under concurrent load.
        /// </summary>
        [Benchmark]
        public async Task MemoryCache_ConcurrentLoad()
        {
            // Pre-populate cache
            for (int i = 0; i < ItemCount; i++)
            {
                await _memoryCache.SetAsync(_cacheKeys[i], _testData[i]).ConfigureAwait(false);
            }

            // Concurrent access simulation
            var tasks = new List<Task>();
            var random = new Random(42);

            for (int i = 0; i < ItemCount / 4; i++) // Quarter of items for concurrent access
            {
                var keyIndex = random.Next(ItemCount);
                tasks.Add(_memoryCache.GetAsync<SimpleSampleCacheData>(_cacheKeys[keyIndex]));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
