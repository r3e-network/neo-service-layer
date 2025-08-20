using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Http;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Services.Backup;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Services.Monitoring;
using NeoServiceLayer.Services.CrossChain;
using NeoServiceLayer.TestInfrastructure;
using System.Text.Json;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Integration.Tests;

/// <summary>
/// Comprehensive integration tests covering cross-service interactions and workflows
/// </summary>
public class ComprehensiveIntegrationTests : TestBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceCollection _services;

    public ComprehensiveIntegrationTests()
    {
        _services = new ServiceCollection();
        SetupServices();
        _serviceProvider = _services.BuildServiceProvider();
    }

    private void SetupServices()
    {
        // Core services
        _services.AddLogging(builder => builder.AddConsole());
        _services.AddSingleton<IHttpClientService, HttpClientService>();
        _services.AddSingleton<IBlockchainClientFactory, MockBlockchainClientFactory>();
        
        // Business services
        _services.AddTransient<BackupService>();
        _services.AddTransient<StorageService>();
        _services.AddTransient<MonitoringService>();
        _services.AddTransient<CrossChainService>();
    }

    #region End-to-End User Journey Tests

    [Fact]
    public async Task CompleteUserJourney_BackupStorageAndCrossChainTransfer_ShouldWorkEndToEnd()
    {
        // Arrange
        var backupService = _serviceProvider.GetRequiredService<BackupService>();
        var storageService = _serviceProvider.GetRequiredService<StorageService>();
        var crossChainService = _serviceProvider.GetRequiredService<CrossChainService>();
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();

        var userId = Guid.NewGuid().ToString();
        var testData = JsonSerializer.Serialize(new
        {
            userId = userId,
            balance = 1000.0m,
            assets = new[] { "GAS", "NEO" },
            timestamp = DateTime.UtcNow
        });

        // Act & Assert

        // Step 1: Store user data
        var storeResult = await storageService.StoreDataAsync(new StorageRequest
        {
            Key = $"user:{userId}",
            Data = System.Text.Encoding.UTF8.GetBytes(testData),
            Metadata = new Dictionary<string, object> { { "type", "user_profile" } }
        });
        storeResult.Success.Should().BeTrue();

        // Step 2: Create backup of user data
        var backupResult = await backupService.CreateBackupAsync(new BackupRequest
        {
            BackupId = Guid.NewGuid().ToString(),
            BackupType = BackupType.Full,
            DataSources = new List<string> { $"user:{userId}" },
            Metadata = new Dictionary<string, object> { { "user_id", userId } }
        });
        backupResult.Success.Should().BeTrue();

        // Step 3: Monitor the operations
        await monitoringService.RecordMetricAsync(new MetricData
        {
            Name = "user_operations",
            Value = 1,
            Tags = new Dictionary<string, string> { { "operation", "backup_created" }, { "user", userId } },
            Timestamp = DateTime.UtcNow
        });

        // Step 4: Initiate cross-chain transfer
        var transferResult = await crossChainService.InitiateCrossChainTransferAsync(new CrossChainTransferRequest
        {
            TransferId = Guid.NewGuid().ToString(),
            SourceChain = BlockchainType.NeoN3,
            DestinationChain = BlockchainType.NeoX,
            AssetType = "GAS",
            Amount = 100.0m,
            SourceAddress = GenerateTestAddress(BlockchainType.NeoN3),
            DestinationAddress = GenerateTestAddress(BlockchainType.NeoX),
            Metadata = new Dictionary<string, object> { { "user_id", userId } }
        });
        transferResult.Success.Should().BeTrue();

        // Step 5: Verify all operations are recorded
        var metrics = await monitoringService.QueryMetricsAsync(new MetricQuery
        {
            MetricName = "user_operations",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow,
            Tags = new Dictionary<string, string> { { "user", userId } }
        });
        metrics.Success.Should().BeTrue();
        metrics.Metrics.Should().NotBeEmpty();

        // Step 6: Verify data integrity through backup validation
        var validationResult = await backupService.ValidateBackupAsync(new BackupValidationRequest
        {
            BackupId = backupResult.BackupId,
            ValidationLevel = ValidationLevel.Full,
            VerifyChecksums = true
        });
        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DisasterRecoveryWorkflow_ShouldRestoreSystemState()
    {
        // Arrange
        var backupService = _serviceProvider.GetRequiredService<BackupService>();
        var storageService = _serviceProvider.GetRequiredService<StorageService>();
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();

        var backupId = Guid.NewGuid().ToString();
        var criticalData = new[]
        {
            new { key = "system:config", value = "production_config" },
            new { key = "system:state", value = "active" },
            new { key = "system:version", value = "1.0.0" }
        };

        // Setup initial system state
        foreach (var data in criticalData)
        {
            await storageService.StoreDataAsync(new StorageRequest
            {
                Key = data.key,
                Data = System.Text.Encoding.UTF8.GetBytes(data.value),
                Metadata = new Dictionary<string, object> { { "critical", true } }
            });
        }

        // Create system backup
        var backupResult = await backupService.CreateBackupAsync(new BackupRequest
        {
            BackupId = backupId,
            BackupType = BackupType.Full,
            DataSources = criticalData.Select(d => d.key).ToList(),
            Metadata = new Dictionary<string, object> 
            { 
                { "type", "disaster_recovery" },
                { "critical", true }
            }
        });
        backupResult.Success.Should().BeTrue();

        // Act: Simulate system failure and restore
        
        // Simulate data loss
        foreach (var data in criticalData)
        {
            await storageService.DeleteDataAsync(data.key);
        }

        // Record disaster event
        await monitoringService.RecordMetricAsync(new MetricData
        {
            Name = "system_events",
            Value = 1,
            Tags = new Dictionary<string, string> { { "event", "disaster_detected" } },
            Timestamp = DateTime.UtcNow
        });

        // Restore from backup
        var restoreResult = await backupService.RestoreBackupAsync(new RestoreRequest
        {
            BackupId = backupId,
            RestoreTarget = "system",
            RestoreOptions = new Dictionary<string, object>
            {
                { "overwrite", true },
                { "validate", true },
                { "critical_priority", true }
            }
        });

        // Assert
        restoreResult.Success.Should().BeTrue();
        restoreResult.RestoredItemCount.Should().Be(criticalData.Length);

        // Verify system is restored
        foreach (var data in criticalData)
        {
            var retrieveResult = await storageService.RetrieveDataAsync(data.key);
            retrieveResult.Success.Should().BeTrue();
            System.Text.Encoding.UTF8.GetString(retrieveResult.Data).Should().Be(data.value);
        }

        // Record recovery completion
        await monitoringService.RecordMetricAsync(new MetricData
        {
            Name = "system_events",
            Value = 1,
            Tags = new Dictionary<string, string> { { "event", "disaster_recovery_completed" } },
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion

    #region Cross-Service Communication Tests

    [Fact]
    public async Task CrossServiceCommunication_MonitoringAndStorage_ShouldShareData()
    {
        // Arrange
        var storageService = _serviceProvider.GetRequiredService<StorageService>();
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();

        var sessionId = Guid.NewGuid().ToString();
        var metricsData = new List<MetricData>();

        // Act: Generate metrics over time
        for (int i = 0; i < 10; i++)
        {
            var metric = new MetricData
            {
                Name = "api_response_time",
                Value = 100 + (i * 10), // Simulate increasing response times
                Tags = new Dictionary<string, string> 
                { 
                    { "endpoint", "/api/users" },
                    { "session", sessionId }
                },
                Timestamp = DateTime.UtcNow.AddSeconds(i)
            };
            
            metricsData.Add(metric);
            await monitoringService.RecordMetricAsync(metric);
        }

        // Store aggregated metrics in storage
        var aggregatedData = JsonSerializer.Serialize(new
        {
            sessionId = sessionId,
            totalMetrics = metricsData.Count,
            averageValue = metricsData.Average(m => m.Value),
            maxValue = metricsData.Max(m => m.Value),
            minValue = metricsData.Min(m => m.Value),
            timestamp = DateTime.UtcNow
        });

        var storeResult = await storageService.StoreDataAsync(new StorageRequest
        {
            Key = $"metrics:aggregated:{sessionId}",
            Data = System.Text.Encoding.UTF8.GetBytes(aggregatedData),
            Metadata = new Dictionary<string, object> 
            { 
                { "type", "metrics_summary" },
                { "session_id", sessionId }
            }
        });

        // Assert
        storeResult.Success.Should().BeTrue();

        // Verify metrics can be queried
        var queryResult = await monitoringService.QueryMetricsAsync(new MetricQuery
        {
            MetricName = "api_response_time",
            StartTime = DateTime.UtcNow.AddMinutes(-1),
            EndTime = DateTime.UtcNow,
            Tags = new Dictionary<string, string> { { "session", sessionId } }
        });
        
        queryResult.Success.Should().BeTrue();
        queryResult.Metrics.Should().HaveCount(10);

        // Verify stored aggregation
        var retrieveResult = await storageService.RetrieveDataAsync($"metrics:aggregated:{sessionId}");
        retrieveResult.Success.Should().BeTrue();
        
        var storedData = JsonSerializer.Deserialize<dynamic>(
            System.Text.Encoding.UTF8.GetString(retrieveResult.Data));
        storedData.Should().NotBeNull();
    }

    #endregion

    #region Performance Integration Tests

    [Fact]
    public async Task HighLoadIntegrationTest_AllServices_ShouldMaintainPerformance()
    {
        // Arrange
        var services = new[]
        {
            _serviceProvider.GetRequiredService<BackupService>(),
            _serviceProvider.GetRequiredService<StorageService>(),
            _serviceProvider.GetRequiredService<MonitoringService>(),
            _serviceProvider.GetRequiredService<CrossChainService>()
        };

        var operationCount = 50; // Reduced for test performance
        var tasks = new List<Task>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act: Generate high load across all services
        for (int i = 0; i < operationCount; i++)
        {
            var index = i; // Capture for closure
            
            // Storage operations
            tasks.Add(Task.Run(async () =>
            {
                var storageService = _serviceProvider.GetRequiredService<StorageService>();
                await storageService.StoreDataAsync(new StorageRequest
                {
                    Key = $"load_test:storage:{index}",
                    Data = System.Text.Encoding.UTF8.GetBytes($"test_data_{index}"),
                    Metadata = new Dictionary<string, object> { { "load_test", true } }
                });
            }));

            // Monitoring operations
            tasks.Add(Task.Run(async () =>
            {
                var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();
                await monitoringService.RecordMetricAsync(new MetricData
                {
                    Name = "load_test_metric",
                    Value = index,
                    Tags = new Dictionary<string, string> { { "test_type", "load" } },
                    Timestamp = DateTime.UtcNow
                });
            }));

            // Backup operations (every 5th iteration)
            if (index % 5 == 0)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var backupService = _serviceProvider.GetRequiredService<BackupService>();
                    await backupService.CreateBackupAsync(new BackupRequest
                    {
                        BackupId = Guid.NewGuid().ToString(),
                        BackupType = BackupType.Incremental,
                        DataSources = new List<string> { $"load_test:storage:{index}" },
                        Metadata = new Dictionary<string, object> { { "load_test", true } }
                    });
                }));
            }
        }

        // Wait for all operations to complete
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessOrEqualTo(60000); // Should complete within 60 seconds
        tasks.Should().HaveCount(operationCount * 2 + (operationCount / 5)); // Storage + Monitoring + some Backups

        // Verify some operations succeeded by checking storage
        var storageService = _serviceProvider.GetRequiredService<StorageService>();
        var testRetrieve = await storageService.RetrieveDataAsync("load_test:storage:0");
        testRetrieve.Success.Should().BeTrue();
    }

    #endregion

    #region Data Flow Integration Tests

    [Fact]
    public async Task DataFlowIntegration_CrossChainWithBackupAndMonitoring_ShouldTrackCompleteFlow()
    {
        // Arrange
        var crossChainService = _serviceProvider.GetRequiredService<CrossChainService>();
        var backupService = _serviceProvider.GetRequiredService<BackupService>();
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();

        var flowId = Guid.NewGuid().ToString();
        var transferId = Guid.NewGuid().ToString();

        // Act: Execute complete data flow

        // Step 1: Start monitoring the flow
        await monitoringService.RecordMetricAsync(new MetricData
        {
            Name = "data_flow_events",
            Value = 1,
            Tags = new Dictionary<string, string> 
            { 
                { "flow_id", flowId },
                { "event", "flow_started" }
            },
            Timestamp = DateTime.UtcNow
        });

        // Step 2: Initiate cross-chain transfer
        var transferResult = await crossChainService.InitiateCrossChainTransferAsync(
            new CrossChainTransferRequest
            {
                TransferId = transferId,
                SourceChain = BlockchainType.NeoN3,
                DestinationChain = BlockchainType.NeoX,
                AssetType = "GAS",
                Amount = 250.0m,
                SourceAddress = GenerateTestAddress(BlockchainType.NeoN3),
                DestinationAddress = GenerateTestAddress(BlockchainType.NeoX),
                Metadata = new Dictionary<string, object> { { "flow_id", flowId } }
            });

        transferResult.Success.Should().BeTrue();

        // Step 3: Create backup of transfer state
        var backupResult = await backupService.CreateBackupAsync(new BackupRequest
        {
            BackupId = Guid.NewGuid().ToString(),
            BackupType = BackupType.Incremental,
            DataSources = new List<string> { $"crosschain:transfer:{transferId}" },
            Metadata = new Dictionary<string, object> 
            { 
                { "flow_id", flowId },
                { "transfer_id", transferId }
            }
        });

        backupResult.Success.Should().BeTrue();

        // Step 4: Monitor transfer progress
        await monitoringService.RecordMetricAsync(new MetricData
        {
            Name = "data_flow_events",
            Value = 1,
            Tags = new Dictionary<string, string> 
            { 
                { "flow_id", flowId },
                { "event", "transfer_initiated" },
                { "transfer_id", transferId }
            },
            Timestamp = DateTime.UtcNow
        });

        // Step 5: Get transfer status
        var statusResult = await crossChainService.GetCrossChainTransferStatusAsync(transferId);
        statusResult.Should().NotBeNull();
        statusResult.TransferId.Should().Be(transferId);

        // Step 6: Complete monitoring
        await monitoringService.RecordMetricAsync(new MetricData
        {
            Name = "data_flow_events",
            Value = 1,
            Tags = new Dictionary<string, string> 
            { 
                { "flow_id", flowId },
                { "event", "flow_completed" }
            },
            Timestamp = DateTime.UtcNow
        });

        // Assert: Verify complete flow is tracked
        var flowMetrics = await monitoringService.QueryMetricsAsync(new MetricQuery
        {
            MetricName = "data_flow_events",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow,
            Tags = new Dictionary<string, string> { { "flow_id", flowId } }
        });

        flowMetrics.Success.Should().BeTrue();
        flowMetrics.Metrics.Should().HaveCountGreaterOrEqualTo(3); // Started, transfer initiated, completed

        // Verify backup integrity
        var validationResult = await backupService.ValidateBackupAsync(new BackupValidationRequest
        {
            BackupId = backupResult.BackupId,
            ValidationLevel = ValidationLevel.Basic,
            VerifyChecksums = true
        });

        validationResult.IsValid.Should().BeTrue();
    }

    #endregion

    #region Error Handling Integration Tests

    [Fact]
    public async Task ErrorHandlingIntegration_ServiceFailureRecovery_ShouldHandleGracefully()
    {
        // Arrange
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();
        var backupService = _serviceProvider.GetRequiredService<BackupService>();

        var errorScenarioId = Guid.NewGuid().ToString();

        // Act & Assert

        // Step 1: Record start of error scenario
        await monitoringService.RecordMetricAsync(new MetricData
        {
            Name = "error_scenarios",
            Value = 1,
            Tags = new Dictionary<string, string> 
            { 
                { "scenario_id", errorScenarioId },
                { "event", "scenario_started" }
            },
            Timestamp = DateTime.UtcNow
        });

        // Step 2: Simulate service error by attempting invalid operation
        var invalidBackupResult = await backupService.CreateBackupAsync(new BackupRequest
        {
            BackupId = "", // Invalid empty ID
            BackupType = BackupType.Full,
            DataSources = new List<string> { "non_existent_data" }
        });

        invalidBackupResult.Success.Should().BeFalse();
        invalidBackupResult.Error.Should().NotBeNullOrEmpty();

        // Step 3: Record error event
        await monitoringService.RecordMetricAsync(new MetricData
        {
            Name = "error_scenarios",
            Value = 1,
            Tags = new Dictionary<string, string> 
            { 
                { "scenario_id", errorScenarioId },
                { "event", "error_handled" },
                { "error_type", "invalid_backup_request" }
            },
            Timestamp = DateTime.UtcNow
        });

        // Step 4: Verify system continues functioning with valid request
        var validBackupResult = await backupService.CreateBackupAsync(new BackupRequest
        {
            BackupId = Guid.NewGuid().ToString(),
            BackupType = BackupType.Full,
            DataSources = new List<string> { "test_data_source" }
        });

        validBackupResult.Success.Should().BeTrue();

        // Step 5: Record recovery
        await monitoringService.RecordMetricAsync(new MetricData
        {
            Name = "error_scenarios",
            Value = 1,
            Tags = new Dictionary<string, string> 
            { 
                { "scenario_id", errorScenarioId },
                { "event", "recovery_successful" }
            },
            Timestamp = DateTime.UtcNow
        });

        // Verify error scenario is fully tracked
        var errorMetrics = await monitoringService.QueryMetricsAsync(new MetricQuery
        {
            MetricName = "error_scenarios",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow,
            Tags = new Dictionary<string, string> { { "scenario_id", errorScenarioId } }
        });

        errorMetrics.Success.Should().BeTrue();
        errorMetrics.Metrics.Should().HaveCount(3); // Started, error handled, recovery successful
    }

    #endregion

    #region Helper Methods

    private string GenerateTestAddress(BlockchainType blockchainType)
    {
        return blockchainType switch
        {
            BlockchainType.NeoN3 => GenerateNeoN3Address(),
            BlockchainType.NeoX => "0x" + Guid.NewGuid().ToString("N")[..40],
            _ => throw new NotSupportedException($"Blockchain type {blockchainType} not supported")
        };
    }

    private string GenerateNeoN3Address()
    {
        const string base58Chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        var random = new Random();
        var addressChars = new char[33];

        for (int i = 0; i < 33; i++)
        {
            addressChars[i] = base58Chars[random.Next(base58Chars.Length)];
        }

        return $"N{new string(addressChars)}";
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    #endregion
}