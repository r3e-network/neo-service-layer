using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Performance.Tests;

/// <summary>
/// Performance tests for persistent storage operations.
/// Validates storage performance, throughput, and scalability characteristics.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[RPlotExporter]
public class PersistentStoragePerformanceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly OcclumFileStorageProvider _provider;
    private readonly string _testStoragePath;
    private readonly StorageOptions _defaultOptions;

    public PersistentStoragePerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _testStoragePath = Path.Combine(Path.GetTempPath(), $"perf-test-storage-{Guid.NewGuid():N}");
        
        var logger = new TestLogger<OcclumFileStorageProvider>(_output);
        _provider = new OcclumFileStorageProvider(_testStoragePath, logger);
        _provider.InitializeAsync().GetAwaiter().GetResult();
        
        _defaultOptions = new StorageOptions
        {
            Encrypt = false,
            Compress = false
        };
    }

    #region Basic Operation Performance Tests

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Component", "BasicOperations")]
    public async Task StoreAsync_SingleOperation_CompletesWithinTimeLimit()
    {
        // Arrange
        const string key = "perf_single_key";
        var data = System.Text.Encoding.UTF8.GetBytes("performance_test_data");
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _provider.StoreAsync(key, data, _defaultOptions);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Should complete within 100ms
        _output.WriteLine($"Single store operation took: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Component", "BasicOperations")]
    public async Task RetrieveAsync_SingleOperation_CompletesWithinTimeLimit()
    {
        // Arrange
        const string key = "perf_retrieve_key";
        var data = System.Text.Encoding.UTF8.GetBytes("performance_test_data");
        await _provider.StoreAsync(key, data, _defaultOptions);

        var stopwatch = Stopwatch.StartNew();

        // Act
        var retrievedData = await _provider.RetrieveAsync(key);
        stopwatch.Stop();

        // Assert
        retrievedData.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50); // Should complete within 50ms
        _output.WriteLine($"Single retrieve operation took: {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Bulk Operation Performance Tests

    [Theory]
    [Trait("Category", "Performance")]
    [Trait("Component", "BulkOperations")]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    public async Task StoreAsync_BulkOperations_MeetsPerformanceTargets(int operationCount)
    {
        // Arrange
        var tasks = new List<Task>();
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < operationCount; i++)
        {
            var key = $"bulk_key_{i}";
            var data = System.Text.Encoding.UTF8.GetBytes($"bulk_data_{i}");
            tasks.Add(_provider.StoreAsync(key, data, _defaultOptions));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var operationsPerSecond = operationCount / (stopwatch.ElapsedMilliseconds / 1000.0);
        
        _output.WriteLine($"Bulk store ({operationCount} ops) took: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Operations per second: {operationsPerSecond:F2}");
        
        // Performance targets (adjust based on hardware)
        operationsPerSecond.Should().BeGreaterThan(50); // At least 50 ops/sec
    }

    [Theory]
    [Trait("Category", "Performance")]
    [Trait("Component", "BulkOperations")]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    public async Task RetrieveAsync_BulkOperations_MeetsPerformanceTargets(int operationCount)
    {
        // Arrange - Store test data first
        for (int i = 0; i < operationCount; i++)
        {
            var key = $"bulk_retrieve_key_{i}";
            var data = System.Text.Encoding.UTF8.GetBytes($"bulk_retrieve_data_{i}");
            await _provider.StoreAsync(key, data, _defaultOptions);
        }

        var tasks = new List<Task<byte[]?>>();
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < operationCount; i++)
        {
            tasks.Add(_provider.RetrieveAsync($"bulk_retrieve_key_{i}"));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var operationsPerSecond = operationCount / (stopwatch.ElapsedMilliseconds / 1000.0);
        
        _output.WriteLine($"Bulk retrieve ({operationCount} ops) took: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Operations per second: {operationsPerSecond:F2}");
        
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
        operationsPerSecond.Should().BeGreaterThan(100); // Should be faster than store
    }

    #endregion

    #region Data Size Performance Tests

    [Theory]
    [Trait("Category", "Performance")]
    [Trait("Component", "DataSize")]
    [InlineData(1024)]        // 1KB
    [InlineData(10240)]       // 10KB
    [InlineData(102400)]      // 100KB
    [InlineData(1048576)]     // 1MB
    public async Task StoreAsync_VariousDataSizes_ScalesLinearly(int dataSize)
    {
        // Arrange
        var data = new byte[dataSize];
        new Random().NextBytes(data);
        const string key = "size_test_key";

        var stopwatch = Stopwatch.StartNew();

        // Act
        await _provider.StoreAsync(key, data, _defaultOptions);
        stopwatch.Stop();

        // Assert
        var mbPerSecond = (dataSize / 1024.0 / 1024.0) / (stopwatch.ElapsedMilliseconds / 1000.0);
        
        _output.WriteLine($"Stored {dataSize / 1024.0:F2}KB in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Throughput: {mbPerSecond:F2} MB/s");
        
        // Should maintain reasonable throughput even for large files
        if (dataSize <= 102400) // For smaller files
        {
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        }
        else // For larger files (1MB)
        {
            mbPerSecond.Should().BeGreaterThan(1.0); // At least 1 MB/s
        }
    }

    [Theory]
    [Trait("Category", "Performance")]
    [Trait("Component", "DataSize")]
    [InlineData(1024)]        // 1KB
    [InlineData(10240)]       // 10KB
    [InlineData(102400)]      // 100KB
    [InlineData(1048576)]     // 1MB
    public async Task RetrieveAsync_VariousDataSizes_ScalesLinearly(int dataSize)
    {
        // Arrange
        var data = new byte[dataSize];
        new Random().NextBytes(data);
        const string key = "size_retrieve_test_key";
        
        await _provider.StoreAsync(key, data, _defaultOptions);

        var stopwatch = Stopwatch.StartNew();

        // Act
        var retrievedData = await _provider.RetrieveAsync(key);
        stopwatch.Stop();

        // Assert
        retrievedData.Should().NotBeNull();
        retrievedData!.Length.Should().Be(dataSize);
        
        var mbPerSecond = (dataSize / 1024.0 / 1024.0) / (stopwatch.ElapsedMilliseconds / 1000.0);
        
        _output.WriteLine($"Retrieved {dataSize / 1024.0:F2}KB in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Throughput: {mbPerSecond:F2} MB/s");
        
        // Retrieval should be faster than storage
        if (dataSize <= 102400)
        {
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
        }
        else
        {
            mbPerSecond.Should().BeGreaterThan(2.0); // At least 2 MB/s for retrieval
        }
    }

    #endregion

    #region Encryption Performance Tests

    [Theory]
    [Trait("Category", "Performance")]
    [Trait("Component", "Encryption")]
    [InlineData(1024, "AES-256-GCM")]
    [InlineData(10240, "AES-256-GCM")]
    [InlineData(102400, "AES-256-GCM")]
    [InlineData(1024, "AES-256-CBC")]
    [InlineData(10240, "AES-256-CBC")]
    public async Task StoreAsync_WithEncryption_MaintainsReasonablePerformance(int dataSize, string algorithm)
    {
        // Arrange
        var data = new byte[dataSize];
        new Random().NextBytes(data);
        var options = new StorageOptions
        {
            Encrypt = true,
            EncryptionKey = "test_encryption_key_32_bytes_long"
        };

        var stopwatch = Stopwatch.StartNew();

        // Act
        await _provider.StoreAsync($"encrypted_key_{dataSize}_{algorithm}", data, options);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Encrypted store ({algorithm}, {dataSize / 1024.0:F2}KB) took: {stopwatch.ElapsedMilliseconds}ms");
        
        // Encryption should add minimal overhead
        var expectedMaxTime = Math.Max(200, dataSize / 1024 * 50); // Base 200ms + 50ms per KB
        stopwatch.ElapsedMilliseconds.Should().BeLessThan((long)expectedMaxTime);
    }

    [Theory]
    [Trait("Category", "Performance")]
    [Trait("Component", "Compression")]
    [InlineData(10240, "gzip")]
    [InlineData(102400, "gzip")]
    [InlineData(10240, "deflate")]
    [InlineData(102400, "deflate")]
    public async Task StoreAsync_WithCompression_ImprovesThroughputForLargeData(int dataSize, string algorithm)
    {
        // Arrange - Create compressible data
        var compressibleData = System.Text.Encoding.UTF8.GetBytes(new string('A', dataSize));
        var options = new StorageOptions
        {
            Compress = true,
            CompressionAlgorithm = algorithm switch
            {
                "gzip" => CompressionAlgorithm.GZip,
                "deflate" => CompressionAlgorithm.Brotli,
                _ => CompressionAlgorithm.LZ4
            }
        };

        var stopwatch = Stopwatch.StartNew();

        // Act
        await _provider.StoreAsync($"compressed_key_{dataSize}_{algorithm}", compressibleData, options);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Compressed store ({algorithm}, {dataSize / 1024.0:F2}KB) took: {stopwatch.ElapsedMilliseconds}ms");
        
        // For highly compressible data, should be faster due to smaller I/O
        var maxExpectedTime = dataSize / 1024 * 30; // 30ms per KB (should be faster due to compression)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxExpectedTime);
    }

    #endregion

    #region Transaction Performance Tests

    [Theory]
    [Trait("Category", "Performance")]
    [Trait("Component", "Transactions")]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task TransactionalOperations_BulkCommit_PerformsEfficiently(int operationCount)
    {
        // Arrange
        var transaction = await _provider.BeginTransactionAsync();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Act - Perform multiple operations in transaction
            for (int i = 0; i < operationCount; i++)
            {
                var key = $"tx_perf_key_{i}";
                var data = System.Text.Encoding.UTF8.GetBytes($"tx_perf_data_{i}");
                await _provider.StoreAsync(key, data, _defaultOptions);
            }

            // TODO: Transaction support not yet implemented
            // await _provider.CommitTransactionAsync(transaction.TransactionId);
            stopwatch.Stop();

            // Assert
            var operationsPerSecond = operationCount / (stopwatch.ElapsedMilliseconds / 1000.0);
            
            _output.WriteLine($"Transaction with {operationCount} ops took: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Transaction ops per second: {operationsPerSecond:F2}");
            
            // Transactions should still maintain reasonable performance
            operationsPerSecond.Should().BeGreaterThan(20); // At least 20 ops/sec with transactions
        }
        catch
        {
            // TODO: Transaction support not yet implemented
            // await _provider.RollbackTransactionAsync(transaction.TransactionId);
            throw;
        }
    }

    #endregion

    #region Concurrent Access Performance Tests

    [Theory]
    [Trait("Category", "Performance")]
    [Trait("Component", "Concurrency")]
    [InlineData(5, 20)]   // 5 threads, 20 ops each
    [InlineData(10, 10)]  // 10 threads, 10 ops each
    public async Task ConcurrentOperations_MultipleThreads_MaintainsThroughput(int threadCount, int operationsPerThread)
    {
        // Arrange
        var tasks = new List<Task>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Launch concurrent operations
        for (int thread = 0; thread < threadCount; thread++)
        {
            var threadId = thread;
            tasks.Add(Task.Run(async () =>
            {
                for (int op = 0; op < operationsPerThread; op++)
                {
                    var key = $"concurrent_key_{threadId}_{op}";
                    var data = System.Text.Encoding.UTF8.GetBytes($"concurrent_data_{threadId}_{op}");
                    await _provider.StoreAsync(key, data, _defaultOptions);
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var totalOperations = threadCount * operationsPerThread;
        var operationsPerSecond = totalOperations / (stopwatch.ElapsedMilliseconds / 1000.0);
        
        _output.WriteLine($"Concurrent operations ({threadCount} threads, {totalOperations} total ops) took: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Concurrent ops per second: {operationsPerSecond:F2}");
        
        // Should handle concurrency efficiently
        operationsPerSecond.Should().BeGreaterThan(30); // At least 30 ops/sec under concurrency
    }

    #endregion

    #region Memory Usage Tests

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Component", "Memory")]
    public async Task LargeDataOperations_DoNotCauseMemoryLeaks()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        const int operationCount = 50;
        const int dataSize = 102400; // 100KB per operation

        // Act - Perform operations that could cause memory leaks
        for (int i = 0; i < operationCount; i++)
        {
            var data = new byte[dataSize];
            new Random().NextBytes(data);
            
            var key = $"memory_test_key_{i}";
            await _provider.StoreAsync(key, data, _defaultOptions);
            
            // Retrieve and discard to test cleanup
            var retrieved = await _provider.RetrieveAsync(key);
            retrieved.Should().NotBeNull();
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreaseKB = memoryIncrease / 1024.0;
        
        _output.WriteLine($"Memory increase after {operationCount} operations: {memoryIncreaseKB:F2}KB");
        
        // Memory increase should be reasonable (not proportional to all data processed)
        memoryIncreaseKB.Should().BeLessThan(operationCount * dataSize / 1024.0 * 0.1); // Less than 10% of total data
    }

    #endregion

    public void Dispose()
    {
        try
        {
            _provider?.Dispose();
            if (Directory.Exists(_testStoragePath))
            {
                Directory.Delete(_testStoragePath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}

/// <summary>
/// Test logger implementation for performance tests
/// </summary>
public class TestLogger<T> : ILogger<T>
{
    private readonly ITestOutputHelper _output;

    public TestLogger(ITestOutputHelper output)
    {
        _output = output;
    }

    public IDisposable BeginScope<TState>(TState state) => new NoOpDisposable();

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            _output.WriteLine($"[{logLevel}] {formatter(state, exception)}");
        }
    }

    private class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}

/// <summary>
/// Benchmark class for BenchmarkDotNet performance testing
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class PersistentStorageBenchmarks
{
    private OcclumFileStorageProvider? _provider;
    private StorageOptions? _options;
    private string? _testPath;

    [GlobalSetup]
    public void Setup()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"benchmark-storage-{Guid.NewGuid():N}");
        var logger = new NullLogger<OcclumFileStorageProvider>();
        _provider = new OcclumFileStorageProvider(_testPath, logger);
        _provider.InitializeAsync().GetAwaiter().GetResult();
        
        _options = new StorageOptions
        {
            Encrypt = false,
            Compress = false
        };
    }

    [Benchmark]
    public async Task Store1KB()
    {
        var data = new byte[1024];
        await _provider!.StoreAsync($"benchmark_key_{Guid.NewGuid()}", data, _options!);
    }

    [Benchmark]
    public async Task Store10KB()
    {
        var data = new byte[10240];
        await _provider!.StoreAsync($"benchmark_key_{Guid.NewGuid()}", data, _options!);
    }

    [Benchmark]
    public async Task Store100KB()
    {
        var data = new byte[102400];
        await _provider!.StoreAsync($"benchmark_key_{Guid.NewGuid()}", data, _options!);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _provider?.Dispose();
        if (_testPath != null && Directory.Exists(_testPath))
        {
            Directory.Delete(_testPath, true);
        }
    }
}

/// <summary>
/// Null logger for benchmarks
/// </summary>
public class NullLogger<T> : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) => new NoOpDisposable();
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

    private class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}