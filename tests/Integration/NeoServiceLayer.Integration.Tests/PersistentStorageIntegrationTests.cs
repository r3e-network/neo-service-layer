using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Configuration;
using NeoServiceLayer.Services.Configuration.Models;
using NeoServiceLayer.Services.KeyManagement;
using NeoServiceLayer.Services.Monitoring;
using NeoServiceLayer.Services.Notification;
using NeoServiceLayer.Services.Notification.Models;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;

namespace NeoServiceLayer.Integration.Tests;

/// <summary>
/// Integration tests for persistent storage across multiple services.
/// Validates data persistence, service interactions, and recovery scenarios.
/// </summary>
public class PersistentStorageIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly string _testStoragePath;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly ILogger<PersistentStorageIntegrationTests> _logger;

    public PersistentStorageIntegrationTests()
    {
        _testStoragePath = Path.Combine(Path.GetTempPath(), $"integration-test-storage-{Guid.NewGuid():N}");
        _mockEnclaveManager = new Mock<IEnclaveManager>();

        // Set test encryption key for OcclumFileStorageProvider
        Environment.SetEnvironmentVariable("ENCLAVE_MASTER_KEY", "test-encryption-key-for-integration-tests");

        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Add HTTP client factory
        services.AddHttpClient();

        // Add persistent storage
        services.AddSingleton<IPersistentStorageProvider>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<OcclumFileStorageProvider>>();
            return new OcclumFileStorageProvider(_testStoragePath, logger);
        });

        // Add mock enclave manager
        SetupEnclaveManager();
        services.AddSingleton(_mockEnclaveManager.Object);

        // Add service configurations
        services.AddSingleton<IServiceConfiguration>(provider =>
        {
            var mockConfig = new Mock<IServiceConfiguration>();
            mockConfig.Setup(x => x.GetValue(It.IsAny<string>(), It.IsAny<string>()))
                     .Returns((string key, string defaultValue) => defaultValue);
            return mockConfig.Object;
        });

        // Add services with persistent storage
        services.AddSingleton<IStorageService>(provider => new StorageService(
            provider.GetRequiredService<IEnclaveManager>(),
            provider.GetRequiredService<IServiceConfiguration>(),
            provider.GetRequiredService<ILogger<StorageService>>(),
            provider.GetRequiredService<IPersistentStorageProvider>()));

        services.AddSingleton<IKeyManagementService>(provider => new KeyManagementService(
            provider.GetRequiredService<IEnclaveManager>(),
            provider.GetRequiredService<IServiceConfiguration>(),
            provider.GetRequiredService<ILogger<KeyManagementService>>(),
            provider.GetRequiredService<IPersistentStorageProvider>()));

        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IMonitoringService, MonitoringService>();
        services.AddSingleton<IConfigurationService>(provider => new ConfigurationService(
            provider.GetRequiredService<ILogger<ConfigurationService>>(),
            provider.GetRequiredService<IEnclaveManager>(),
            provider.GetRequiredService<IServiceConfiguration>(),
            provider.GetRequiredService<IPersistentStorageProvider>()));

        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<PersistentStorageIntegrationTests>>();

        // Initialize persistent storage
        InitializeStorageAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeStorageAsync()
    {
        var storageProvider = _serviceProvider.GetRequiredService<IPersistentStorageProvider>();
        await storageProvider.InitializeAsync();
    }

    private void SetupEnclaveManager()
    {
        // Create in-memory storage for testing
        var _mockStorage = new Dictionary<string, string>();
        var _mockMetadata = new Dictionary<string, string>();

        // Setup basic enclave operations
        _mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                          .Returns(Task.CompletedTask);
        _mockEnclaveManager.Setup(x => x.InitializeEnclaveAsync()).ReturnsAsync(true);

        // Setup storage operations with proper state management
        _mockEnclaveManager.Setup(x => x.StorageStoreDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                          .Returns<string, string, string, CancellationToken>((key, data, encKey, ct) =>
                          {
                              _mockStorage[key] = data;
                              return Task.FromResult("{\"success\": true}");
                          });

        _mockEnclaveManager.Setup(x => x.StorageRetrieveDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                          .Returns<string, string, CancellationToken>((key, encKey, ct) =>
                          {
                              if (_mockStorage.TryGetValue(key, out var data))
                                  return Task.FromResult(data);
                              throw new KeyNotFoundException($"Key '{key}' not found in mock storage");
                          });

        _mockEnclaveManager.Setup(x => x.StorageDeleteDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                          .Returns<string, CancellationToken>((key, ct) =>
                          {
                              var removed = _mockStorage.Remove(key);
                              _mockMetadata.Remove(key);
                              return Task.FromResult(removed);
                          });

        // Setup JavaScript operations with proper metadata management
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                          .Returns<string, CancellationToken>((script, ct) =>
                          {
                              if (script.Contains("validateConfigurationValue"))
                                  return Task.FromResult("true");
                              if (script.Contains("storeMetadata"))
                              {
                                  // Extract key and metadata from script: storeMetadata('key', {json})
                                  var keyStart = script.IndexOf("'") + 1;
                                  var keyEnd = script.IndexOf("'", keyStart);
                                  var key = script.Substring(keyStart, keyEnd - keyStart);
                                  var metadataStart = script.IndexOf('{');
                                  var metadataEnd = script.LastIndexOf('}') + 1;
                                  var metadata = script.Substring(metadataStart, metadataEnd - metadataStart);
                                  _mockMetadata[key] = metadata;
                                  return Task.FromResult("true");
                              }
                              if (script.Contains("updateMetadata"))
                                  return Task.FromResult("true");
                              if (script.Contains("getMetadata"))
                              {
                                  // Extract key from script
                                  var keyStart = script.IndexOf("'") + 1;
                                  var keyEnd = script.IndexOf("'", keyStart);
                                  var key = script.Substring(keyStart, keyEnd - keyStart);
                                  if (_mockMetadata.TryGetValue(key, out var metadata))
                                      return Task.FromResult(metadata);
                                  return Task.FromResult("null");
                              }
                              if (script.Contains("getAllMetadata"))
                                  return Task.FromResult("[]");
                              return Task.FromResult("{}");
                          });

        // Setup key management operations with complete KeyMetadata JSON
        _mockEnclaveManager.Setup(x => x.KmsGenerateKeyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
                          .Returns<string, string, string, bool, string>((keyId, keyType, keyUsage, exportable, description) =>
                          {
                              // Complete KeyMetadata JSON response matching the class properties
                              var response = $@"{{
                                  ""KeyId"": ""{keyId}"",
                                  ""KeyType"": ""{keyType}"",
                                  ""KeyUsage"": ""{keyUsage}"",
                                  ""Exportable"": {exportable.ToString().ToLower()},
                                  ""Description"": ""{description}"",
                                  ""CreatedAt"": ""{DateTime.UtcNow:O}"",
                                  ""LastUsedAt"": null,
                                  ""PublicKeyHex"": ""04abcd1234567890""
                              }}";
                              return Task.FromResult(response);
                          });

        _mockEnclaveManager.Setup(x => x.KmsEncryptDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync((string keyId, string data, string alg, CancellationToken ct) => data + "encrypted");

        _mockEnclaveManager.Setup(x => x.KmsDecryptDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync((string keyId, string encData, string alg, CancellationToken ct) =>
                              encData.Replace("encrypted", ""));
    }

    #region Cross-Service Data Persistence Tests

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "CrossService")]
    public async Task StorageService_PersistentData_SurvivesServiceRestart()
    {
        // Arrange
        var storageService = _serviceProvider.GetRequiredService<IStorageService>();
        await storageService.InitializeAsync();
        await storageService.StartAsync();

        const string key = "persistent_test_key";
        var data = System.Text.Encoding.UTF8.GetBytes("persistent_test_data");
        var options = new NeoServiceLayer.Services.Storage.StorageOptions { Encrypt = false, Compress = false };

        // Act - Store data
        await storageService.StoreDataAsync(key, data, options, BlockchainType.NeoN3);

        // Simulate service restart by stopping and starting
        await storageService.StopAsync();
        await storageService.StartAsync();

        // Retrieve data after restart
        var retrievedData = await storageService.GetDataAsync(key, BlockchainType.NeoN3);

        // Assert
        retrievedData.Should().NotBeNull();
        retrievedData.Should().BeEquivalentTo(data);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "CrossService")]
    public async Task MultipleServices_SharedPersistentStorage_MaintainDataIntegrity()
    {
        // Arrange
        var storageService = _serviceProvider.GetRequiredService<IStorageService>();
        var keyService = _serviceProvider.GetRequiredService<IKeyManagementService>();
        var notificationService = _serviceProvider.GetRequiredService<INotificationService>();

        // Initialize all services
        await storageService.InitializeAsync();
        await storageService.StartAsync();
        await keyService.InitializeAsync();
        await keyService.StartAsync();
        await notificationService.InitializeAsync();
        await notificationService.StartAsync();

        // Act - Store data in different services
        var storageData = System.Text.Encoding.UTF8.GetBytes("storage_data");
        await storageService.StoreDataAsync("storage_key", storageData,
            new NeoServiceLayer.Services.Storage.StorageOptions(), BlockchainType.NeoN3);

        var keyData = await keyService.CreateKeyAsync("test_key", "AES256", "256", false, "test_description", BlockchainType.NeoN3);

        var notificationRequest = new SendNotificationRequest
        {
            Recipient = "test_user@example.com",
            Subject = "Test Notification",
            Message = "Test notification body",
            Channel = NotificationChannel.Email
        };
        await notificationService.SendNotificationAsync(notificationRequest, BlockchainType.NeoN3);

        // Assert - Verify persistent storage provider has all data
        var storageProvider = _serviceProvider.GetRequiredService<IPersistentStorageProvider>();
        var allKeys = await storageProvider.ListKeysAsync();

        // Should contain keys from all services
        allKeys.Should().Contain(k => k.Contains("storage"));
        allKeys.Should().Contain(k => k.Contains("key") || k.Contains("notification"));
    }

    #endregion

    #region Service Recovery Tests

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "Recovery")]
    public async Task ServicesWithPersistentStorage_RecoverFromFailure_RestoreCorrectState()
    {
        // Arrange
        var configService = _serviceProvider.GetRequiredService<IConfigurationService>();
        await configService.InitializeAsync();
        
        // Initialize enclave if the service supports it
        if (configService is IEnclaveService enclaveService)
        {
            await enclaveService.InitializeEnclaveAsync();
        }
        
        await configService.StartAsync();

        var originalSettings = new Dictionary<string, string>
        {
            { "setting1", "value1" },
            { "setting2", "value2" },
            { "setting3", "value3" }
        };

        // Store configuration settings
        foreach (var setting in originalSettings)
        {
            var setRequest = new SetConfigurationRequest
            {
                Key = setting.Key,
                Value = setting.Value
            };
            await configService.SetConfigurationAsync(setRequest, BlockchainType.NeoN3);
        }

        // Wait for persistence to complete
        await Task.Delay(200);

        // Verify configurations are stored and persisted to storage
        var storageProvider = _serviceProvider.GetRequiredService<IPersistentStorageProvider>();
        var allKeys = await storageProvider.ListKeysAsync();

        // Check that configuration entries are persisted with the correct prefix
        foreach (var setting in originalSettings)
        {
            var configKey = $"config:entry:{setting.Key}";
            allKeys.Should().Contain(configKey, $"Configuration {setting.Key} should be persisted in storage");
        }

        // Verify configurations can be retrieved from service
        foreach (var setting in originalSettings)
        {
            var getRequest = new GetConfigurationRequest
            {
                Key = setting.Key
            };
            var result = await configService.GetConfigurationAsync(getRequest, BlockchainType.NeoN3);
            result.Found.Should().BeTrue($"Configuration {setting.Key} should be found in service");
            result.Value.Should().Be(setting.Value);
        }

        // Act - Simulate service failure and restart
        await configService.StopAsync();
        await Task.Delay(200); // Allow time for cleanup

        // Create a new ConfigurationService instance to simulate restart
        var newConfigService = new ConfigurationService(
            _serviceProvider.GetRequiredService<ILogger<ConfigurationService>>(),
            _mockEnclaveManager.Object,
            _serviceProvider.GetRequiredService<IServiceConfiguration>(),
            storageProvider);

        await newConfigService.InitializeAsync();
        await newConfigService.StartAsync();

        // Wait for initialization to complete
        await Task.Delay(200);

        // Assert - Verify all settings are restored from persistent storage
        foreach (var setting in originalSettings)
        {
            var getRequest = new GetConfigurationRequest
            {
                Key = setting.Key
            };
            var result = await newConfigService.GetConfigurationAsync(getRequest, BlockchainType.NeoN3);
            result.Found.Should().BeTrue($"Configuration {setting.Key} should be restored after restart");
            result.Value.Should().Be(setting.Value);
        }
    }

    #endregion

    #region Transaction and Consistency Tests

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "Transactions")]
    public async Task PersistentStorage_TransactionalOperations_MaintainConsistency()
    {
        // Arrange
        var storageProvider = _serviceProvider.GetRequiredService<IPersistentStorageProvider>();
        var options = new NeoServiceLayer.Infrastructure.Persistence.StorageOptions();

        var testData = new Dictionary<string, byte[]>
        {
            { "tx_key1", System.Text.Encoding.UTF8.GetBytes("tx_data1") },
            { "tx_key2", System.Text.Encoding.UTF8.GetBytes("tx_data2") },
            { "tx_key3", System.Text.Encoding.UTF8.GetBytes("tx_data3") }
        };

        // Act - Perform transactional operations
        var transaction = await storageProvider.BeginTransactionAsync();

        try
        {
            // Store all data in transaction
            foreach (var item in testData)
            {
                if (transaction != null)
                {
                    await transaction.StoreAsync(item.Key, item.Value, options);
                }
                else
                {
                    await storageProvider.StoreAsync(item.Key, item.Value, options);
                }
            }

            // Commit transaction
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            // Assert - Verify all data is persisted
            foreach (var item in testData)
            {
                var retrievedData = await storageProvider.RetrieveAsync(item.Key);
                retrievedData.Should().NotBeNull();
                retrievedData.Should().BeEquivalentTo(item.Value);
            }
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "Transactions")]
    public async Task PersistentStorage_RollbackTransaction_DiscardsAllChanges()
    {
        // Arrange
        var storageProvider = _serviceProvider.GetRequiredService<IPersistentStorageProvider>();
        var options = new NeoServiceLayer.Infrastructure.Persistence.StorageOptions();

        var testData = new Dictionary<string, byte[]>
        {
            { "rollback_key1", System.Text.Encoding.UTF8.GetBytes("rollback_data1") },
            { "rollback_key2", System.Text.Encoding.UTF8.GetBytes("rollback_data2") }
        };

        // Act - Start transaction and store data
        var transaction = await storageProvider.BeginTransactionAsync();

        foreach (var item in testData)
        {
            if (transaction != null)
            {
                await transaction.StoreAsync(item.Key, item.Value, options);
            }
            else
            {
                await storageProvider.StoreAsync(item.Key, item.Value, options);
            }
        }

        // Rollback transaction
        if (transaction != null)
        {
            await transaction.RollbackAsync();
        }

        // Assert - Verify no data is persisted
        foreach (var item in testData)
        {
            var retrievedData = await storageProvider.RetrieveAsync(item.Key);
            retrievedData.Should().BeNull();
        }
    }

    #endregion

    #region Performance and Load Tests

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "Performance")]
    public async Task PersistentStorage_ConcurrentServiceOperations_MaintainDataIntegrity()
    {
        // Arrange
        var storageService = _serviceProvider.GetRequiredService<IStorageService>();
        var keyService = _serviceProvider.GetRequiredService<IKeyManagementService>();

        await storageService.InitializeAsync();
        await storageService.StartAsync();
        await keyService.InitializeAsync();
        await keyService.StartAsync();

        const int operationCount = 20; // Reduced for integration testing
        var tasks = new List<Task>();

        // Act - Perform concurrent operations
        for (int i = 0; i < operationCount; i++)
        {
            var index = i;

            // Storage operations
            tasks.Add(Task.Run(async () =>
            {
                var data = System.Text.Encoding.UTF8.GetBytes($"concurrent_data_{index}");
                await storageService.StoreDataAsync($"concurrent_key_{index}", data,
                    new NeoServiceLayer.Services.Storage.StorageOptions(), BlockchainType.NeoN3);
            }));

            // Key management operations
            tasks.Add(Task.Run(async () =>
            {
                await keyService.CreateKeyAsync($"concurrent_key_{index}", "AES256", "256", false, "concurrent_test", BlockchainType.NeoN3);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Verify storage provider integrity
        var storageProvider = _serviceProvider.GetRequiredService<IPersistentStorageProvider>();
        var validationResult = await storageProvider.ValidateIntegrityAsync();

        validationResult.Should().NotBeNull();
        validationResult.IsValid.Should().BeTrue();
        validationResult.Errors.Should().BeEmpty();
    }

    #endregion

    #region Backup and Recovery Tests

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "Backup")]
    public async Task PersistentStorage_BackupAndRestore_PreservesAllData()
    {
        // Arrange
        var storageProvider = _serviceProvider.GetRequiredService<IPersistentStorageProvider>();
        var options = new NeoServiceLayer.Infrastructure.Persistence.StorageOptions { Encrypt = true, Compress = false };

        var testData = new Dictionary<string, byte[]>
        {
            { "backup_key1", System.Text.Encoding.UTF8.GetBytes("backup_data1") },
            { "backup_key2", System.Text.Encoding.UTF8.GetBytes("backup_data2") },
            { "backup_key3", System.Text.Encoding.UTF8.GetBytes("backup_data3") }
        };

        // Store test data
        foreach (var item in testData)
        {
            await storageProvider.StoreAsync(item.Key, item.Value, options);
        }

        // Act - Create backup
        var backupPath = Path.Combine(Path.GetTempPath(), $"backup-{Guid.NewGuid():N}.zip");

        await storageProvider.BackupAsync(backupPath);

        // Simulate data loss by clearing storage
        foreach (var key in testData.Keys)
        {
            await storageProvider.DeleteAsync(key);
        }

        // Restore from backup
        await storageProvider.RestoreAsync(backupPath);

        // Assert - Verify all data is restored
        foreach (var item in testData)
        {
            var retrievedData = await storageProvider.RetrieveAsync(item.Key);
            retrievedData.Should().NotBeNull();
            retrievedData.Should().BeEquivalentTo(item.Value);
        }

        // Cleanup
        if (Directory.Exists(backupPath))
        {
            Directory.Delete(backupPath, true);
        }
    }

    #endregion

    #region Health and Monitoring Tests

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "Monitoring")]
    public async Task PersistentStorage_HealthChecks_ReportCorrectStatus()
    {
        // Arrange
        var storageProvider = _serviceProvider.GetRequiredService<IPersistentStorageProvider>();
        var monitoringService = _serviceProvider.GetRequiredService<IMonitoringService>();

        await monitoringService.InitializeAsync();
        await monitoringService.StartAsync();

        // Act - Check storage health
        var statistics = await storageProvider.GetStatisticsAsync();
        var validationResult = await storageProvider.ValidateIntegrityAsync();

        // Create some monitoring metrics
        var metrics = new Dictionary<string, object>
        {
            { "storage.operations.count", 100 },
            { "storage.response.time", 50.5 },
            { "storage.error.rate", 0.01 }
        };

        foreach (var metric in metrics)
        {
            var metricRequest = new RecordMetricRequest
            {
                MetricName = metric.Key,
                Value = Convert.ToDouble(metric.Value),
                ServiceName = "PersistentStorage"
            };
            await monitoringService.RecordMetricAsync(metricRequest, BlockchainType.NeoN3);
        }

        // Assert
        statistics.Should().NotBeNull();
        statistics.TotalKeys.Should().BeGreaterOrEqualTo(0);
        validationResult.Should().NotBeNull();
        validationResult.IsValid.Should().BeTrue();
    }

    #endregion

    public void Dispose()
    {
        try
        {
            _serviceProvider?.Dispose();
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
