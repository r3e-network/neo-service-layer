using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Services.Backup;
using NeoServiceLayer.Services.Monitoring;
using NeoServiceLayer.Services.CrossChain;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Examples;

/// <summary>
/// Advanced workflow examples demonstrating complex real-world scenarios
/// </summary>
public class AdvancedWorkflowExamples
{
    private readonly IServiceProvider _serviceProvider;

    public AdvancedWorkflowExamples()
    {
        _serviceProvider = ConfigureServices();
    }

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

    #region E-Commerce Platform Example

    public async Task ECommercePlatformWorkflow()
    {
        Console.WriteLine("=== E-Commerce Platform Workflow ===");
        
        var storageService = _serviceProvider.GetRequiredService<StorageService>();
        var backupService = _serviceProvider.GetRequiredService<BackupService>();
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();
        var crossChainService = _serviceProvider.GetRequiredService<CrossChainService>();

        var orderId = Guid.NewGuid().ToString();
        var customerId = "customer_" + Guid.NewGuid().ToString("N")[..8];
        
        Console.WriteLine($"Processing order: {orderId} for customer: {customerId}");

        try
        {
            // 1. Create customer profile and order
            var customer = new
            {
                customerId = customerId,
                name = "Alice Johnson",
                email = "alice@example.com",
                walletAddress = "NbTiM6h8r99kpRtb428XcsUk1TzKed2gTc",
                preferredChain = "NeoN3",
                createdAt = DateTime.UtcNow
            };

            var order = new
            {
                orderId = orderId,
                customerId = customerId,
                items = new[]
                {
                    new { productId = "prod_001", name = "Neo Art NFT", price = 50.0m, currency = "GAS" },
                    new { productId = "prod_002", name = "Digital Asset Bundle", price = 25.0m, currency = "GAS" }
                },
                totalAmount = 75.0m,
                currency = "GAS",
                status = "pending",
                createdAt = DateTime.UtcNow
            };

            // Store customer and order data
            await storageService.StoreDataAsync(new StorageRequest
            {
                Key = $"customer:{customerId}",
                Data = JsonSerializer.SerializeToUtf8Bytes(customer),
                Metadata = new Dictionary<string, object> { { "type", "customer_profile" } }
            });

            await storageService.StoreDataAsync(new StorageRequest
            {
                Key = $"order:{orderId}",
                Data = JsonSerializer.SerializeToUtf8Bytes(order),
                Metadata = new Dictionary<string, object> 
                { 
                    { "type", "order" },
                    { "customer_id", customerId },
                    { "amount", order.totalAmount }
                }
            });

            // Record order metrics
            await monitoringService.RecordMetricAsync(new MetricData
            {
                Name = "ecommerce_orders",
                Value = 1,
                Tags = new Dictionary<string, string>
                {
                    { "status", "created" },
                    { "customer_type", "returning" },
                    { "order_value_range", GetOrderValueRange(order.totalAmount) }
                },
                Timestamp = DateTime.UtcNow
            });

            Console.WriteLine("✓ Customer profile and order created");

            // 2. Process payment via cross-chain
            Console.WriteLine("Processing cross-chain payment...");
            
            var paymentTransfer = await crossChainService.InitiateCrossChainTransferAsync(new CrossChainTransferRequest
            {
                TransferId = Guid.NewGuid().ToString(),
                SourceChain = BlockchainType.NeoN3,
                DestinationChain = BlockchainType.NeoX, // Payment processor on NeoX
                AssetType = "GAS",
                Amount = order.totalAmount,
                SourceAddress = customer.walletAddress,
                DestinationAddress = "0x1234567890abcdef1234567890abcdef12345678", // Payment processor
                Metadata = new Dictionary<string, object>
                {
                    { "order_id", orderId },
                    { "customer_id", customerId },
                    { "payment_type", "order_payment" }
                }
            });

            if (paymentTransfer.Success)
            {
                // Update order status
                var updatedOrder = new
                {
                    orderId = order.orderId,
                    customerId = order.customerId,
                    items = order.items,
                    totalAmount = order.totalAmount,
                    currency = order.currency,
                    status = "payment_processing",
                    paymentTransferId = paymentTransfer.TransferId,
                    updatedAt = DateTime.UtcNow,
                    createdAt = order.createdAt
                };

                await storageService.UpdateDataAsync(new StorageUpdateRequest
                {
                    Key = $"order:{orderId}",
                    Data = JsonSerializer.SerializeToUtf8Bytes(updatedOrder),
                    UpdateMode = UpdateMode.Replace
                });

                Console.WriteLine($"✓ Payment initiated: {paymentTransfer.TransferId}");
            }
            else
            {
                throw new Exception($"Payment failed: {paymentTransfer.Error}");
            }

            // 3. Create comprehensive backup
            var backupResult = await backupService.CreateBackupAsync(new BackupRequest
            {
                BackupId = Guid.NewGuid().ToString(),
                BackupType = BackupType.Incremental,
                DataSources = new List<string> 
                { 
                    $"customer:{customerId}",
                    $"order:{orderId}"
                },
                Metadata = new Dictionary<string, object>
                {
                    { "backup_type", "ecommerce_transaction" },
                    { "order_id", orderId },
                    { "value", order.totalAmount }
                }
            });

            Console.WriteLine($"✓ Transaction backup created: {backupResult.BackupId}");

            // 4. Monitor payment status and fulfill order
            await MonitorPaymentAndFulfillOrder(orderId, paymentTransfer.TransferId, storageService, monitoringService, crossChainService);

            // 5. Generate order analytics
            await GenerateOrderAnalytics(customerId, orderId, monitoringService);

            Console.WriteLine($"✓ E-commerce workflow completed for order {orderId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ E-commerce workflow failed: {ex.Message}");
            
            // Record failure metrics
            await monitoringService.RecordMetricAsync(new MetricData
            {
                Name = "ecommerce_orders",
                Value = 1,
                Tags = new Dictionary<string, string>
                {
                    { "status", "failed" },
                    { "error_type", ex.GetType().Name }
                },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    private async Task MonitorPaymentAndFulfillOrder(string orderId, string transferId, StorageService storage, MonitoringService monitoring, CrossChainService crossChain)
    {
        var maxChecks = 10;
        var checkInterval = 2000; // 2 seconds

        for (int check = 0; check < maxChecks; check++)
        {
            var transferStatus = await crossChain.GetCrossChainTransferStatusAsync(transferId);
            
            await monitoring.RecordMetricAsync(new MetricData
            {
                Name = "payment_status_check",
                Value = 1,
                Tags = new Dictionary<string, string>
                {
                    { "transfer_id", transferId },
                    { "status", transferStatus.Status.ToString() },
                    { "check_number", check.ToString() }
                },
                Timestamp = DateTime.UtcNow
            });

            if (transferStatus.Status == CrossChainTransferStatus.Completed)
            {
                // Payment confirmed - fulfill order
                var fulfillmentData = new
                {
                    orderId = orderId,
                    status = "fulfilled",
                    paymentConfirmed = true,
                    fulfillmentDate = DateTime.UtcNow,
                    deliveryMethod = "digital_delivery"
                };

                await storage.StoreDataAsync(new StorageRequest
                {
                    Key = $"fulfillment:{orderId}",
                    Data = JsonSerializer.SerializeToUtf8Bytes(fulfillmentData),
                    Metadata = new Dictionary<string, object>
                    {
                        { "type", "order_fulfillment" },
                        { "order_id", orderId }
                    }
                });

                Console.WriteLine($"✓ Order {orderId} fulfilled successfully");
                break;
            }
            else if (transferStatus.Status == CrossChainTransferStatus.Failed)
            {
                Console.WriteLine($"✗ Payment failed for order {orderId}");
                break;
            }

            if (check < maxChecks - 1)
            {
                await Task.Delay(checkInterval);
            }
        }
    }

    private async Task GenerateOrderAnalytics(string customerId, string orderId, MonitoringService monitoring)
    {
        var analytics = new[]
        {
            new MetricData
            {
                Name = "customer_lifetime_value",
                Value = 275.0, // Simulated CLV calculation
                Tags = new Dictionary<string, string> { { "customer_id", customerId } },
                Timestamp = DateTime.UtcNow
            },
            new MetricData
            {
                Name = "order_completion_time",
                Value = 45.0, // Seconds
                Tags = new Dictionary<string, string> 
                { 
                    { "order_id", orderId },
                    { "process_type", "cross_chain_payment" }
                },
                Timestamp = DateTime.UtcNow
            }
        };

        foreach (var metric in analytics)
        {
            await monitoring.RecordMetricAsync(metric);
        }
    }

    private string GetOrderValueRange(decimal amount)
    {
        return amount switch
        {
            < 25 => "low",
            >= 25 and < 100 => "medium",
            >= 100 and < 500 => "high",
            _ => "premium"
        };
    }

    #endregion

    #region DeFi Liquidity Pool Example

    public async Task DeFiLiquidityPoolWorkflow()
    {
        Console.WriteLine("=== DeFi Liquidity Pool Workflow ===");
        
        var storageService = _serviceProvider.GetRequiredService<StorageService>();
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();
        var crossChainService = _serviceProvider.GetRequiredService<CrossChainService>();

        var poolId = Guid.NewGuid().ToString();
        var providerId = "provider_" + Guid.NewGuid().ToString("N")[..8];

        try
        {
            // 1. Create liquidity pool
            var liquidityPool = new
            {
                poolId = poolId,
                tokenA = new { symbol = "GAS", amount = 1000.0m },
                tokenB = new { symbol = "NEO", amount = 50.0m },
                totalLiquidity = 100.0m,
                feeRate = 0.003m, // 0.3%
                createdAt = DateTime.UtcNow,
                chain = "NeoN3"
            };

            await storageService.StoreDataAsync(new StorageRequest
            {
                Key = $"pool:{poolId}",
                Data = JsonSerializer.SerializeToUtf8Bytes(liquidityPool),
                Metadata = new Dictionary<string, object>
                {
                    { "type", "liquidity_pool" },
                    { "token_pair", "GAS-NEO" }
                }
            });

            Console.WriteLine($"✓ Liquidity pool created: {poolId}");

            // 2. Add liquidity provider
            var provider = new
            {
                providerId = providerId,
                address = "NbTiM6h8r99kpRtb428XcsUk1TzKed2gTc",
                contributions = new[]
                {
                    new { token = "GAS", amount = 100.0m },
                    new { token = "NEO", amount = 5.0m }
                },
                liquidityTokens = 10.0m,
                joinedAt = DateTime.UtcNow
            };

            await storageService.StoreDataAsync(new StorageRequest
            {
                Key = $"provider:{providerId}",
                Data = JsonSerializer.SerializeToUtf8Bytes(provider),
                Metadata = new Dictionary<string, object>
                {
                    { "type", "liquidity_provider" },
                    { "pool_id", poolId }
                }
            });

            // 3. Monitor pool metrics
            var poolMetrics = new[]
            {
                new MetricData
                {
                    Name = "pool_tvl",
                    Value = (double)(liquidityPool.tokenA.amount * 1.5 + liquidityPool.tokenB.amount * 45), // USD value
                    Tags = new Dictionary<string, string>
                    {
                        { "pool_id", poolId },
                        { "token_pair", "GAS-NEO" }
                    },
                    Timestamp = DateTime.UtcNow
                },
                new MetricData
                {
                    Name = "pool_utilization",
                    Value = 0.65, // 65% utilization
                    Tags = new Dictionary<string, string>
                    {
                        { "pool_id", poolId },
                        { "metric_type", "utilization_rate" }
                    },
                    Timestamp = DateTime.UtcNow
                }
            };

            foreach (var metric in poolMetrics)
            {
                await monitoringService.RecordMetricAsync(metric);
            }

            // 4. Simulate cross-chain arbitrage
            await SimulateCrossChainArbitrage(poolId, crossChainService, monitoringService);

            // 5. Calculate and distribute rewards
            await CalculatePoolRewards(poolId, providerId, storageService, monitoringService);

            Console.WriteLine("✓ DeFi liquidity pool workflow completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ DeFi workflow failed: {ex.Message}");
        }
    }

    private async Task SimulateCrossChainArbitrage(string poolId, CrossChainService crossChain, MonitoringService monitoring)
    {
        Console.WriteLine("Executing cross-chain arbitrage opportunity...");

        // Detect price difference between chains
        var neoN3Price = 22.50m; // GAS price on NeoN3
        var neoXPrice = 22.85m;  // GAS price on NeoX
        var priceDifference = Math.Abs(neoXPrice - neoN3Price);
        var arbitrageAmount = 50.0m;

        if (priceDifference > 0.25m) // Profitable arbitrage threshold
        {
            var arbitrageTransfer = await crossChain.InitiateCrossChainTransferAsync(new CrossChainTransferRequest
            {
                TransferId = Guid.NewGuid().ToString(),
                SourceChain = BlockchainType.NeoN3,
                DestinationChain = BlockchainType.NeoX,
                AssetType = "GAS",
                Amount = arbitrageAmount,
                SourceAddress = "NbTiM6h8r99kpRtb428XcsUk1TzKed2gTc",
                DestinationAddress = "0x1234567890abcdef1234567890abcdef12345678",
                Metadata = new Dictionary<string, object>
                {
                    { "arbitrage_opportunity", true },
                    { "pool_id", poolId },
                    { "expected_profit", (double)(priceDifference * arbitrageAmount) }
                }
            });

            await monitoring.RecordMetricAsync(new MetricData
            {
                Name = "arbitrage_opportunity",
                Value = (double)(priceDifference * arbitrageAmount),
                Tags = new Dictionary<string, string>
                {
                    { "pool_id", poolId },
                    { "source_chain", "NeoN3" },
                    { "dest_chain", "NeoX" },
                    { "success", arbitrageTransfer.Success.ToString() }
                },
                Timestamp = DateTime.UtcNow
            });

            Console.WriteLine($"✓ Arbitrage executed: {arbitrageTransfer.Success}");
        }
    }

    private async Task CalculatePoolRewards(string poolId, string providerId, StorageService storage, MonitoringService monitoring)
    {
        Console.WriteLine("Calculating liquidity provider rewards...");

        var rewards = new
        {
            providerId = providerId,
            poolId = poolId,
            rewardPeriod = DateTime.UtcNow.AddDays(-1).Date,
            tradingFees = 2.45m,
            liquidityMining = 5.0m,
            totalRewards = 7.45m,
            calculatedAt = DateTime.UtcNow
        };

        await storage.StoreDataAsync(new StorageRequest
        {
            Key = $"rewards:{providerId}:{DateTime.UtcNow:yyyy-MM-dd}",
            Data = JsonSerializer.SerializeToUtf8Bytes(rewards),
            Metadata = new Dictionary<string, object>
            {
                { "type", "liquidity_rewards" },
                { "pool_id", poolId },
                { "provider_id", providerId }
            }
        });

        await monitoring.RecordMetricAsync(new MetricData
        {
            Name = "liquidity_rewards_distributed",
            Value = (double)rewards.totalRewards,
            Tags = new Dictionary<string, string>
            {
                { "pool_id", poolId },
                { "reward_type", "daily_distribution" }
            },
            Timestamp = DateTime.UtcNow
        });

        Console.WriteLine($"✓ Rewards calculated: {rewards.totalRewards} tokens");
    }

    #endregion

    #region IoT Data Pipeline Example

    public async Task IoTDataPipelineWorkflow()
    {
        Console.WriteLine("=== IoT Data Pipeline Workflow ===");

        var storageService = _serviceProvider.GetRequiredService<StorageService>();
        var backupService = _serviceProvider.GetRequiredService<BackupService>();
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();

        var deviceIds = Enumerable.Range(1, 10).Select(i => $"device_{i:D3}").ToArray();
        var batchId = Guid.NewGuid().ToString();

        try
        {
            // 1. Simulate IoT data ingestion
            Console.WriteLine("Ingesting IoT sensor data...");
            var iotData = new ConcurrentBag<object>();
            var ingestionTasks = deviceIds.Select(deviceId => IngestDeviceData(deviceId, iotData, storageService, monitoringService)).ToArray();
            
            await Task.WhenAll(ingestionTasks);
            Console.WriteLine($"✓ Ingested data from {iotData.Count} sensors");

            // 2. Process and aggregate data
            Console.WriteLine("Processing and aggregating sensor data...");
            var aggregatedData = await ProcessIoTData(iotData.ToArray(), storageService, monitoringService);
            Console.WriteLine($"✓ Processed {aggregatedData.Count} aggregated data points");

            // 3. Create hourly backup
            var backupResult = await backupService.CreateBackupAsync(new BackupRequest
            {
                BackupId = Guid.NewGuid().ToString(),
                BackupType = BackupType.Incremental,
                DataSources = deviceIds.Select(d => $"sensor_data:{d}").ToList(),
                Metadata = new Dictionary<string, object>
                {
                    { "backup_type", "iot_hourly" },
                    { "batch_id", batchId },
                    { "device_count", deviceIds.Length }
                }
            });

            Console.WriteLine($"✓ IoT data backup created: {backupResult.BackupId}");

            // 4. Generate analytics and alerts
            await GenerateIoTAnalytics(aggregatedData, monitoringService);

            Console.WriteLine("✓ IoT data pipeline workflow completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ IoT pipeline failed: {ex.Message}");
        }
    }

    private async Task IngestDeviceData(string deviceId, ConcurrentBag<object> dataBag, StorageService storage, MonitoringService monitoring)
    {
        var random = new Random();
        var dataPoints = 10;

        for (int i = 0; i < dataPoints; i++)
        {
            var sensorData = new
            {
                deviceId = deviceId,
                timestamp = DateTime.UtcNow.AddMinutes(-random.Next(60)),
                temperature = Math.Round(20 + random.NextDouble() * 15, 2),
                humidity = Math.Round(30 + random.NextDouble() * 40, 2),
                pressure = Math.Round(1000 + random.NextDouble() * 50, 2),
                batteryLevel = Math.Round(random.NextDouble() * 100, 1)
            };

            dataBag.Add(sensorData);

            await storage.StoreDataAsync(new StorageRequest
            {
                Key = $"sensor_data:{deviceId}:{sensorData.timestamp:yyyy-MM-dd-HH-mm-ss}",
                Data = JsonSerializer.SerializeToUtf8Bytes(sensorData),
                Metadata = new Dictionary<string, object>
                {
                    { "type", "sensor_reading" },
                    { "device_id", deviceId }
                }
            });

            // Record ingestion metric
            await monitoring.RecordMetricAsync(new MetricData
            {
                Name = "iot_data_ingested",
                Value = 1,
                Tags = new Dictionary<string, string>
                {
                    { "device_id", deviceId },
                    { "data_type", "sensor_reading" }
                },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    private async Task<List<object>> ProcessIoTData(object[] rawData, StorageService storage, MonitoringService monitoring)
    {
        var aggregatedData = new List<object>();
        var deviceGroups = rawData.Cast<dynamic>()
            .GroupBy(d => (string)d.GetProperty("deviceId").GetString());

        foreach (var deviceGroup in deviceGroups)
        {
            var deviceData = deviceGroup.ToList();
            var avgTemperature = deviceData.Average(d => (double)d.GetProperty("temperature").GetDouble());
            var avgHumidity = deviceData.Average(d => (double)d.GetProperty("humidity").GetDouble());
            var avgPressure = deviceData.Average(d => (double)d.GetProperty("pressure").GetDouble());
            var minBattery = deviceData.Min(d => (double)d.GetProperty("batteryLevel").GetDouble());

            var aggregated = new
            {
                deviceId = deviceGroup.Key,
                aggregationPeriod = "1_hour",
                avgTemperature = Math.Round(avgTemperature, 2),
                avgHumidity = Math.Round(avgHumidity, 2),
                avgPressure = Math.Round(avgPressure, 2),
                minBatteryLevel = Math.Round(minBattery, 1),
                dataPointCount = deviceData.Count,
                processedAt = DateTime.UtcNow
            };

            aggregatedData.Add(aggregated);

            await storage.StoreDataAsync(new StorageRequest
            {
                Key = $"aggregated:{deviceGroup.Key}:{DateTime.UtcNow:yyyy-MM-dd-HH}",
                Data = JsonSerializer.SerializeToUtf8Bytes(aggregated),
                Metadata = new Dictionary<string, object>
                {
                    { "type", "hourly_aggregate" },
                    { "device_id", deviceGroup.Key }
                }
            });

            // Check for alerts
            if (avgTemperature > 30 || avgHumidity > 80 || minBattery < 20)
            {
                await monitoring.RecordMetricAsync(new MetricData
                {
                    Name = "iot_alerts",
                    Value = 1,
                    Tags = new Dictionary<string, string>
                    {
                        { "device_id", deviceGroup.Key },
                        { "alert_type", avgTemperature > 30 ? "high_temp" : avgHumidity > 80 ? "high_humidity" : "low_battery" }
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        return aggregatedData;
    }

    private async Task GenerateIoTAnalytics(List<object> aggregatedData, MonitoringService monitoring)
    {
        Console.WriteLine("Generating IoT analytics...");

        var analytics = new[]
        {
            new MetricData
            {
                Name = "iot_data_quality",
                Value = 0.95, // 95% data quality score
                Tags = new Dictionary<string, string>
                {
                    { "metric_type", "data_quality_score" },
                    { "time_period", "hourly" }
                },
                Timestamp = DateTime.UtcNow
            },
            new MetricData
            {
                Name = "iot_processing_efficiency",
                Value = aggregatedData.Count,
                Tags = new Dictionary<string, string>
                {
                    { "metric_type", "processed_devices" },
                    { "processing_stage", "aggregation" }
                },
                Timestamp = DateTime.UtcNow
            }
        };

        foreach (var metric in analytics)
        {
            await monitoring.RecordMetricAsync(metric);
        }
    }

    #endregion

    #region Disaster Recovery Simulation

    public async Task DisasterRecoverySimulation()
    {
        Console.WriteLine("=== Disaster Recovery Simulation ===");

        var storageService = _serviceProvider.GetRequiredService<StorageService>();
        var backupService = _serviceProvider.GetRequiredService<BackupService>();
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();

        var simulationId = Guid.NewGuid().ToString();

        try
        {
            // 1. Setup critical system state
            Console.WriteLine("1. Setting up critical system state...");
            var criticalData = new[]
            {
                new { key = "system:config:database", value = "prod_db_connection_string", criticality = "high" },
                new { key = "system:config:api_keys", value = "encrypted_api_keys", criticality = "high" },
                new { key = "system:state:active_sessions", value = "session_data", criticality = "medium" },
                new { key = "system:cache:user_preferences", value = "user_prefs", criticality = "low" }
            };

            foreach (var data in criticalData)
            {
                await storageService.StoreDataAsync(new StorageRequest
                {
                    Key = data.key,
                    Data = JsonSerializer.SerializeToUtf8Bytes(data.value),
                    Metadata = new Dictionary<string, object>
                    {
                        { "criticality", data.criticality },
                        { "simulation_id", simulationId }
                    }
                });
            }

            // 2. Create comprehensive backup
            Console.WriteLine("2. Creating comprehensive backup...");
            var backupResult = await backupService.CreateBackupAsync(new BackupRequest
            {
                BackupId = Guid.NewGuid().ToString(),
                BackupType = BackupType.Full,
                DataSources = criticalData.Select(d => d.key).ToList(),
                Metadata = new Dictionary<string, object>
                {
                    { "backup_type", "disaster_recovery" },
                    { "simulation_id", simulationId }
                }
            });

            // 3. Monitor system health before disaster
            var preDisasterHealth = await monitoringService.GetSystemHealthAsync();
            Console.WriteLine($"3. Pre-disaster health: {preDisasterHealth.OverallStatus}");

            // 4. Simulate disaster (data corruption/loss)
            Console.WriteLine("4. Simulating disaster scenario...");
            await SimulateDataLoss(criticalData, storageService, monitoringService, simulationId);

            // 5. Detect and respond to disaster
            Console.WriteLine("5. Detecting disaster and initiating response...");
            var postDisasterHealth = await monitoringService.GetSystemHealthAsync();
            Console.WriteLine($"   Post-disaster health: {postDisasterHealth.OverallStatus}");

            // 6. Execute disaster recovery
            Console.WriteLine("6. Executing disaster recovery...");
            var recoveryStart = DateTime.UtcNow;

            var restoreResult = await backupService.RestoreBackupAsync(new RestoreRequest
            {
                BackupId = backupResult.BackupId,
                RestoreTarget = "disaster_recovery",
                RestoreOptions = new Dictionary<string, object>
                {
                    { "overwrite", true },
                    { "validate", true },
                    { "priority", "high" }
                }
            });

            var recoveryTime = DateTime.UtcNow - recoveryStart;

            // 7. Validate recovery
            Console.WriteLine("7. Validating recovery...");
            var validationResult = await ValidateRecovery(criticalData, storageService);
            
            // 8. Record recovery metrics
            await RecordRecoveryMetrics(simulationId, recoveryTime, validationResult, monitoringService);

            Console.WriteLine($"✓ Disaster recovery simulation completed");
            Console.WriteLine($"   Recovery time: {recoveryTime.TotalSeconds:F2} seconds");
            Console.WriteLine($"   Data recovery rate: {validationResult.recoveryRate:P2}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Disaster recovery failed: {ex.Message}");
        }
    }

    private async Task SimulateDataLoss(dynamic[] criticalData, StorageService storage, MonitoringService monitoring, string simulationId)
    {
        // Simulate partial data loss (lose 30% of data)
        var dataToLose = criticalData.Take(2).ToArray();

        foreach (var data in dataToLose)
        {
            await storage.DeleteDataAsync(data.key);
            
            await monitoring.RecordMetricAsync(new MetricData
            {
                Name = "disaster_simulation_data_loss",
                Value = 1,
                Tags = new Dictionary<string, string>
                {
                    { "simulation_id", simulationId },
                    { "data_key", data.key },
                    { "criticality", data.criticality }
                },
                Timestamp = DateTime.UtcNow
            });
        }

        Console.WriteLine($"   Simulated loss of {dataToLose.Length} critical data items");
    }

    private async Task<(double recoveryRate, int recoveredItems)> ValidateRecovery(dynamic[] originalData, StorageService storage)
    {
        int recoveredItems = 0;

        foreach (var data in originalData)
        {
            var retrieveResult = await storage.RetrieveDataAsync(data.key);
            if (retrieveResult.Success)
            {
                recoveredItems++;
            }
        }

        var recoveryRate = (double)recoveredItems / originalData.Length;
        return (recoveryRate, recoveredItems);
    }

    private async Task RecordRecoveryMetrics(string simulationId, TimeSpan recoveryTime, (double recoveryRate, int recoveredItems) validation, MonitoringService monitoring)
    {
        var metrics = new[]
        {
            new MetricData
            {
                Name = "disaster_recovery_time",
                Value = recoveryTime.TotalSeconds,
                Tags = new Dictionary<string, string>
                {
                    { "simulation_id", simulationId },
                    { "recovery_type", "full_restore" }
                },
                Timestamp = DateTime.UtcNow
            },
            new MetricData
            {
                Name = "disaster_recovery_success_rate",
                Value = validation.recoveryRate,
                Tags = new Dictionary<string, string>
                {
                    { "simulation_id", simulationId },
                    { "metric_type", "data_recovery_percentage" }
                },
                Timestamp = DateTime.UtcNow
            }
        };

        foreach (var metric in metrics)
        {
            await monitoring.RecordMetricAsync(metric);
        }
    }

    #endregion

    #region Main Demo Runner

    public static async Task Main(string[] args)
    {
        var examples = new AdvancedWorkflowExamples();

        Console.WriteLine("=== Advanced NeoServiceLayer Workflow Examples ===\n");

        try
        {
            await examples.ECommercePlatformWorkflow();
            Console.WriteLine();

            await examples.DeFiLiquidityPoolWorkflow();
            Console.WriteLine();

            await examples.IoTDataPipelineWorkflow();
            Console.WriteLine();

            await examples.DisasterRecoverySimulation();
            Console.WriteLine();

            Console.WriteLine("✓ All advanced workflow examples completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error running advanced examples: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    #endregion
}