using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Services.Backup;
using NeoServiceLayer.Services.Monitoring;
using NeoServiceLayer.Services.CrossChain;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Examples;

/// <summary>
/// Basic usage examples demonstrating how to use the NeoServiceLayer services
/// </summary>
public class BasicUsageExamples
{
    private readonly IServiceProvider _serviceProvider;

    public BasicUsageExamples()
    {
        _serviceProvider = ConfigureServices();
    }

    #region Service Configuration

    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Core services
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IHttpClientService, HttpClientService>();
        services.AddSingleton<IBlockchainClientFactory, BlockchainClientFactory>();

        // Business services
        services.AddTransient<StorageService>();
        services.AddTransient<BackupService>();
        services.AddTransient<MonitoringService>();
        services.AddTransient<CrossChainService>();

        return services.BuildServiceProvider();
    }

    #endregion

    #region Storage Service Examples

    public async Task StorageServiceBasicUsageExample()
    {
        var storageService = _serviceProvider.GetRequiredService<StorageService>();

        // Example 1: Store simple data
        var userData = new
        {
            userId = "user123",
            name = "John Doe",
            email = "john@example.com",
            balance = 1000.0m,
            createdAt = DateTime.UtcNow
        };

        var storeResult = await storageService.StoreDataAsync(new StorageRequest
        {
            Key = $"user:{userData.userId}",
            Data = JsonSerializer.SerializeToUtf8Bytes(userData),
            Metadata = new Dictionary<string, object>
            {
                { "type", "user_profile" },
                { "version", "1.0" },
                { "encrypted", false }
            }
        });

        Console.WriteLine($"Data stored: {storeResult.Success}");
        if (storeResult.Success)
        {
            Console.WriteLine($"Storage ID: {storeResult.StorageId}");
        }

        // Example 2: Retrieve data
        var retrieveResult = await storageService.RetrieveDataAsync($"user:{userData.userId}");
        if (retrieveResult.Success)
        {
            var retrievedUser = JsonSerializer.Deserialize<dynamic>(retrieveResult.Data);
            Console.WriteLine($"Retrieved user: {retrievedUser}");
        }

        // Example 3: Update data
        var updatedUserData = new
        {
            userId = userData.userId,
            name = "John Doe",
            email = "john.doe@newdomain.com", // Updated email
            balance = 1250.0m, // Updated balance
            lastModified = DateTime.UtcNow
        };

        var updateResult = await storageService.UpdateDataAsync(new StorageUpdateRequest
        {
            Key = $"user:{userData.userId}",
            Data = JsonSerializer.SerializeToUtf8Bytes(updatedUserData),
            UpdateMode = UpdateMode.Replace
        });

        Console.WriteLine($"Data updated: {updateResult.Success}");

        // Example 4: Search data
        var searchResult = await storageService.SearchDataAsync(new DataSearchRequest
        {
            Query = "type:user_profile",
            MaxResults = 10,
            SortBy = "createdAt",
            SortOrder = SortOrder.Descending
        });

        Console.WriteLine($"Found {searchResult.Results.Count} user profiles");
    }

    #endregion

    #region Backup Service Examples

    public async Task BackupServiceBasicUsageExample()
    {
        var backupService = _serviceProvider.GetRequiredService<BackupService>();
        var storageService = _serviceProvider.GetRequiredService<StorageService>();

        // Example 1: Create a full backup
        var fullBackupResult = await backupService.CreateBackupAsync(new BackupRequest
        {
            BackupId = Guid.NewGuid().ToString(),
            BackupType = BackupType.Full,
            DataSources = new List<string> { "user-data", "transaction-data", "system-config" },
            Metadata = new Dictionary<string, object>
            {
                { "backup_reason", "scheduled_backup" },
                { "retention_days", 30 },
                { "compression_enabled", true }
            }
        });

        Console.WriteLine($"Full backup created: {fullBackupResult.Success}");
        if (fullBackupResult.Success)
        {
            Console.WriteLine($"Backup ID: {fullBackupResult.BackupId}");
            Console.WriteLine($"Backup Size: {fullBackupResult.BackupSize} bytes");
        }

        // Example 2: Schedule regular incremental backups
        var backupSchedule = new BackupSchedule
        {
            ScheduleId = Guid.NewGuid().ToString(),
            Name = "Daily Incremental Backup",
            CronExpression = "0 2 * * *", // Daily at 2 AM
            BackupTemplate = new BackupRequest
            {
                BackupType = BackupType.Incremental,
                DataSources = new List<string> { "user-data", "transaction-data" }
            },
            IsActive = true,
            RetentionDays = 7
        };

        var scheduleResult = await backupService.ScheduleBackupAsync(backupSchedule);
        Console.WriteLine($"Backup scheduled: {scheduleResult.Success}");

        // Example 3: Validate backup integrity
        var validationResult = await backupService.ValidateBackupAsync(new BackupValidationRequest
        {
            BackupId = fullBackupResult.BackupId,
            ValidationLevel = ValidationLevel.Full,
            VerifyChecksums = true,
            VerifyCompression = true
        });

        Console.WriteLine($"Backup valid: {validationResult.IsValid}");
        if (!validationResult.IsValid)
        {
            Console.WriteLine("Validation errors:");
            validationResult.ValidationErrors.ForEach(Console.WriteLine);
        }

        // Example 4: Restore from backup
        var restoreResult = await backupService.RestoreBackupAsync(new RestoreRequest
        {
            BackupId = fullBackupResult.BackupId,
            RestoreTarget = "restored-data",
            RestoreOptions = new Dictionary<string, object>
            {
                { "overwrite", false },
                { "validate_after_restore", true }
            }
        });

        Console.WriteLine($"Restore completed: {restoreResult.Success}");
        Console.WriteLine($"Items restored: {restoreResult.RestoredItemCount}");
    }

    #endregion

    #region Monitoring Service Examples

    public async Task MonitoringServiceBasicUsageExample()
    {
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();

        // Example 1: Record system metrics
        await monitoringService.RecordMetricAsync(new MetricData
        {
            Name = "cpu_usage_percent",
            Value = 45.2,
            Tags = new Dictionary<string, string>
            {
                { "host", "web-server-01" },
                { "environment", "production" }
            },
            Timestamp = DateTime.UtcNow
        });

        await monitoringService.RecordMetricAsync(new MetricData
        {
            Name = "memory_usage_percent", 
            Value = 68.7,
            Tags = new Dictionary<string, string>
            {
                { "host", "web-server-01" },
                { "environment", "production" }
            },
            Timestamp = DateTime.UtcNow
        });

        Console.WriteLine("System metrics recorded");

        // Example 2: Create alert rules
        var cpuAlertRule = new AlertRule
        {
            RuleId = Guid.NewGuid().ToString(),
            Name = "High CPU Usage Alert",
            MetricName = "cpu_usage_percent",
            Condition = AlertCondition.GreaterThan,
            Threshold = 80.0,
            IsActive = true,
            NotificationChannels = new List<string> { "email", "slack" },
            Description = "Alert when CPU usage exceeds 80%"
        };

        var alertResult = await monitoringService.CreateAlertRuleAsync(cpuAlertRule);
        Console.WriteLine($"Alert rule created: {alertResult.Success}");

        // Example 3: Query metrics
        var metricsQuery = new MetricQuery
        {
            MetricName = "cpu_usage_percent",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow,
            Tags = new Dictionary<string, string> { { "host", "web-server-01" } },
            Aggregation = AggregationType.Average
        };

        var queryResult = await monitoringService.QueryMetricsAsync(metricsQuery);
        Console.WriteLine($"Found {queryResult.Metrics.Count} metric entries");
        Console.WriteLine($"Average CPU usage: {queryResult.Metrics.Average(m => m.Value):F2}%");

        // Example 4: Monitor system health
        var healthStatus = await monitoringService.GetSystemHealthAsync();
        Console.WriteLine($"System health: {healthStatus.OverallStatus}");
        
        foreach (var component in healthStatus.Components)
        {
            Console.WriteLine($"  {component.Key}: {(component.Value.IsHealthy ? "Healthy" : "Unhealthy")}");
            if (!string.IsNullOrEmpty(component.Value.Message))
            {
                Console.WriteLine($"    Message: {component.Value.Message}");
            }
        }
    }

    #endregion

    #region Cross-Chain Service Examples

    public async Task CrossChainServiceBasicUsageExample()
    {
        var crossChainService = _serviceProvider.GetRequiredService<CrossChainService>();

        // Example 1: Register a cross-chain bridge
        var bridgeConfig = new CrossChainBridgeConfig
        {
            BridgeId = Guid.NewGuid().ToString(),
            Name = "NeoN3-NeoX Bridge",
            SourceChain = BlockchainType.NeoN3,
            DestinationChain = BlockchainType.NeoX,
            BridgeContract = "0x1234567890abcdef1234567890abcdef12345678",
            IsActive = true,
            SupportedAssets = new[] { "GAS", "NEO", "USDT" },
            MinTransferAmount = 1.0m,
            MaxTransferAmount = 10000.0m,
            TransferFeePercent = 0.1m
        };

        var bridgeResult = await crossChainService.RegisterCrossChainBridgeAsync(bridgeConfig);
        Console.WriteLine($"Bridge registered: {bridgeResult.Success}");

        // Example 2: Initiate cross-chain transfer
        var transferRequest = new CrossChainTransferRequest
        {
            TransferId = Guid.NewGuid().ToString(),
            SourceChain = BlockchainType.NeoN3,
            DestinationChain = BlockchainType.NeoX,
            AssetType = "GAS",
            Amount = 100.0m,
            SourceAddress = "NbTiM6h8r99kpRtb428XcsUk1TzKed2gTc",
            DestinationAddress = "0x1234567890abcdef1234567890abcdef12345678",
            Metadata = new Dictionary<string, object>
            {
                { "priority", "normal" },
                { "user_id", "user123" }
            }
        };

        var transferResult = await crossChainService.InitiateCrossChainTransferAsync(transferRequest);
        Console.WriteLine($"Transfer initiated: {transferResult.Success}");
        if (transferResult.Success)
        {
            Console.WriteLine($"Transfer ID: {transferResult.TransferId}");
            Console.WriteLine($"Status: {transferResult.Status}");
            Console.WriteLine($"Source Transaction: {transferResult.SourceTransactionHash}");
        }

        // Example 3: Monitor transfer status
        var transferStatus = await crossChainService.GetCrossChainTransferStatusAsync(transferRequest.TransferId);
        Console.WriteLine($"Transfer Status: {transferStatus.Status}");
        Console.WriteLine($"Confirmations: {transferStatus.Confirmations}");

        if (transferStatus.Status == CrossChainTransferStatus.Completed)
        {
            Console.WriteLine($"Destination Transaction: {transferStatus.DestinationTransactionHash}");
            Console.WriteLine($"Completed At: {transferStatus.CompletedAt}");
        }

        // Example 4: Send cross-chain message
        var messageRequest = new CrossChainMessageRequest
        {
            MessageId = Guid.NewGuid().ToString(),
            SourceChain = BlockchainType.NeoN3,
            DestinationChain = BlockchainType.NeoX,
            MessageType = MessageType.ContractCall,
            Payload = JsonSerializer.Serialize(new
            {
                contract = "0xabcdef1234567890",
                method = "updatePrice",
                parameters = new[] { "GAS", "45.67" }
            }),
            Priority = MessagePriority.High,
            TimeoutSeconds = 300
        };

        var messageResult = await crossChainService.SendCrossChainMessageAsync(messageRequest);
        Console.WriteLine($"Message sent: {messageResult.Success}");
        Console.WriteLine($"Message Status: {messageResult.Status}");
    }

    #endregion

    #region Complete Workflow Example

    public async Task CompleteWorkflowExample()
    {
        Console.WriteLine("=== Complete Workflow Example ===");
        
        var storageService = _serviceProvider.GetRequiredService<StorageService>();
        var backupService = _serviceProvider.GetRequiredService<BackupService>();
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();
        var crossChainService = _serviceProvider.GetRequiredService<CrossChainService>();

        var workflowId = Guid.NewGuid().ToString();
        Console.WriteLine($"Starting workflow: {workflowId}");

        try
        {
            // Step 1: Store user transaction data
            Console.WriteLine("1. Storing user transaction data...");
            var transactionData = new
            {
                transactionId = Guid.NewGuid().ToString(),
                userId = "user456",
                amount = 500.0m,
                asset = "GAS",
                timestamp = DateTime.UtcNow,
                workflowId = workflowId
            };

            var storeResult = await storageService.StoreDataAsync(new StorageRequest
            {
                Key = $"transaction:{transactionData.transactionId}",
                Data = JsonSerializer.SerializeToUtf8Bytes(transactionData),
                Metadata = new Dictionary<string, object>
                {
                    { "workflow_id", workflowId },
                    { "type", "transaction" }
                }
            });

            if (!storeResult.Success)
            {
                throw new Exception($"Failed to store transaction data: {storeResult.Error}");
            }

            // Step 2: Record workflow metrics
            Console.WriteLine("2. Recording workflow metrics...");
            await monitoringService.RecordMetricAsync(new MetricData
            {
                Name = "workflow_step",
                Value = 1,
                Tags = new Dictionary<string, string>
                {
                    { "workflow_id", workflowId },
                    { "step", "data_stored" }
                },
                Timestamp = DateTime.UtcNow
            });

            // Step 3: Create backup of transaction data
            Console.WriteLine("3. Creating backup...");
            var backupResult = await backupService.CreateBackupAsync(new BackupRequest
            {
                BackupId = Guid.NewGuid().ToString(),
                BackupType = BackupType.Incremental,
                DataSources = new List<string> { $"transaction:{transactionData.transactionId}" },
                Metadata = new Dictionary<string, object>
                {
                    { "workflow_id", workflowId },
                    { "transaction_id", transactionData.transactionId }
                }
            });

            if (!backupResult.Success)
            {
                throw new Exception($"Failed to create backup: {backupResult.Error}");
            }

            // Step 4: Initiate cross-chain transfer
            Console.WriteLine("4. Initiating cross-chain transfer...");
            var transferResult = await crossChainService.InitiateCrossChainTransferAsync(new CrossChainTransferRequest
            {
                TransferId = Guid.NewGuid().ToString(),
                SourceChain = BlockchainType.NeoN3,
                DestinationChain = BlockchainType.NeoX,
                AssetType = transactionData.asset,
                Amount = transactionData.amount,
                SourceAddress = "NbTiM6h8r99kpRtb428XcsUk1TzKed2gTc",
                DestinationAddress = "0x1234567890abcdef1234567890abcdef12345678",
                Metadata = new Dictionary<string, object>
                {
                    { "workflow_id", workflowId },
                    { "user_id", transactionData.userId }
                }
            });

            if (!transferResult.Success)
            {
                throw new Exception($"Failed to initiate transfer: {transferResult.Error}");
            }

            // Step 5: Final workflow metrics
            Console.WriteLine("5. Recording completion metrics...");
            await monitoringService.RecordMetricAsync(new MetricData
            {
                Name = "workflow_completion",
                Value = 1,
                Tags = new Dictionary<string, string>
                {
                    { "workflow_id", workflowId },
                    { "status", "success" },
                    { "transfer_id", transferResult.TransferId }
                },
                Timestamp = DateTime.UtcNow
            });

            Console.WriteLine($"✓ Workflow {workflowId} completed successfully!");
            Console.WriteLine($"  - Transaction stored: {transactionData.transactionId}");
            Console.WriteLine($"  - Backup created: {backupResult.BackupId}");
            Console.WriteLine($"  - Transfer initiated: {transferResult.TransferId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Workflow {workflowId} failed: {ex.Message}");
            
            // Record failure metrics
            await monitoringService.RecordMetricAsync(new MetricData
            {
                Name = "workflow_completion",
                Value = 0,
                Tags = new Dictionary<string, string>
                {
                    { "workflow_id", workflowId },
                    { "status", "failed" },
                    { "error", ex.Message }
                },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    #endregion

    #region Error Handling Examples

    public async Task ErrorHandlingExamples()
    {
        var storageService = _serviceProvider.GetRequiredService<StorageService>();
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();

        Console.WriteLine("=== Error Handling Examples ===");

        // Example 1: Handling storage errors
        Console.WriteLine("1. Testing storage error handling...");
        try
        {
            var invalidRequest = new StorageRequest
            {
                Key = "", // Invalid empty key
                Data = new byte[] { 1, 2, 3 }
            };

            var result = await storageService.StoreDataAsync(invalidRequest);
            if (!result.Success)
            {
                Console.WriteLine($"  Expected error caught: {result.Error}");
                
                // Record error metric
                await monitoringService.RecordMetricAsync(new MetricData
                {
                    Name = "storage_errors",
                    Value = 1,
                    Tags = new Dictionary<string, string>
                    {
                        { "error_type", "invalid_key" },
                        { "service", "storage" }
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Exception caught: {ex.Message}");
        }

        // Example 2: Retry mechanism example
        Console.WriteLine("2. Testing retry mechanism...");
        var retryCount = 0;
        var maxRetries = 3;
        var success = false;

        while (retryCount < maxRetries && !success)
        {
            try
            {
                retryCount++;
                Console.WriteLine($"  Attempt {retryCount}/{maxRetries}");

                // Simulate an operation that might fail
                var randomResult = new Random().Next(1, 4);
                if (randomResult <= retryCount) // Increasing chance of success
                {
                    Console.WriteLine($"  ✓ Operation succeeded on attempt {retryCount}");
                    success = true;
                }
                else
                {
                    throw new Exception("Simulated transient failure");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Attempt {retryCount} failed: {ex.Message}");
                
                if (retryCount < maxRetries)
                {
                    var delayMs = 1000 * retryCount; // Exponential backoff
                    Console.WriteLine($"  Waiting {delayMs}ms before retry...");
                    await Task.Delay(delayMs);
                }
            }
        }

        if (!success)
        {
            Console.WriteLine($"  ✗ Operation failed after {maxRetries} attempts");
        }
    }

    #endregion

    #region Performance Monitoring Example

    public async Task PerformanceMonitoringExample()
    {
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();
        var storageService = _serviceProvider.GetRequiredService<StorageService>();

        Console.WriteLine("=== Performance Monitoring Example ===");

        // Start performance trace
        var traceConfig = new PerformanceTraceConfig
        {
            TraceName = "Bulk Data Operations",
            Duration = TimeSpan.FromMinutes(2),
            SamplingRate = 1.0, // 100% sampling for demo
            MetricsToTrace = new[] { "operation_duration", "data_size", "success_rate" }
        };

        var traceResult = await monitoringService.StartPerformanceTraceAsync(traceConfig);
        Console.WriteLine($"Performance trace started: {traceResult.TraceId}");

        // Perform monitored operations
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operations = new List<Task>();
        var successCount = 0;
        var failureCount = 0;

        for (int i = 0; i < 20; i++)
        {
            var operation = MonitoredOperation(i, storageService, monitoringService, traceResult.TraceId);
            operations.Add(operation);
        }

        var results = await Task.WhenAll(operations);
        stopwatch.Stop();

        successCount = results.Count(r => r);
        failureCount = results.Count(r => !r);

        // Record final performance metrics
        await monitoringService.RecordPerformanceEventAsync(new PerformanceEvent
        {
            TraceId = traceResult.TraceId,
            EventName = "bulk_operations_summary",
            Duration = stopwatch.Elapsed,
            Metadata = new Dictionary<string, object>
            {
                { "total_operations", operations.Count },
                { "successful_operations", successCount },
                { "failed_operations", failureCount },
                { "success_rate", (double)successCount / operations.Count * 100 }
            }
        });

        Console.WriteLine($"Bulk operations completed in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"  Success: {successCount}/{operations.Count} ({(double)successCount / operations.Count * 100:F1}%)");
        Console.WriteLine($"  Average time per operation: {stopwatch.ElapsedMilliseconds / operations.Count}ms");
    }

    private async Task<bool> MonitoredOperation(int operationId, StorageService storageService, MonitoringService monitoringService, string traceId)
    {
        var operationStopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var testData = new
            {
                id = operationId,
                data = $"Test data for operation {operationId}",
                timestamp = DateTime.UtcNow,
                randomValue = new Random().Next(1, 1000)
            };

            var result = await storageService.StoreDataAsync(new StorageRequest
            {
                Key = $"perf_test:{operationId}",
                Data = JsonSerializer.SerializeToUtf8Bytes(testData)
            });

            operationStopwatch.Stop();

            // Record performance event
            await monitoringService.RecordPerformanceEventAsync(new PerformanceEvent
            {
                TraceId = traceId,
                EventName = "storage_operation",
                Duration = operationStopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    { "operation_id", operationId },
                    { "success", result.Success },
                    { "data_size", JsonSerializer.SerializeToUtf8Bytes(testData).Length }
                }
            });

            return result.Success;
        }
        catch (Exception ex)
        {
            operationStopwatch.Stop();

            // Record failed operation
            await monitoringService.RecordPerformanceEventAsync(new PerformanceEvent
            {
                TraceId = traceId,
                EventName = "storage_operation",
                Duration = operationStopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    { "operation_id", operationId },
                    { "success", false },
                    { "error", ex.Message }
                }
            });

            return false;
        }
    }

    #endregion

    #region Main Demo Runner

    public static async Task Main(string[] args)
    {
        var examples = new BasicUsageExamples();

        Console.WriteLine("=== NeoServiceLayer Usage Examples ===\n");

        try
        {
            // Run all examples
            await examples.StorageServiceBasicUsageExample();
            Console.WriteLine();

            await examples.BackupServiceBasicUsageExample();
            Console.WriteLine();

            await examples.MonitoringServiceBasicUsageExample();
            Console.WriteLine();

            await examples.CrossChainServiceBasicUsageExample();
            Console.WriteLine();

            await examples.CompleteWorkflowExample();
            Console.WriteLine();

            await examples.ErrorHandlingExamples();
            Console.WriteLine();

            await examples.PerformanceMonitoringExample();
            Console.WriteLine();

            Console.WriteLine("✓ All examples completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error running examples: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    #endregion
}