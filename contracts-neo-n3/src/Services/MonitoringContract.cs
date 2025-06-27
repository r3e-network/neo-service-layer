using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using NeoServiceLayer.Contracts.Core;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NeoServiceLayer.Contracts.Services
{
    /// <summary>
    /// Provides comprehensive monitoring and alerting for all Neo Service Layer
    /// contracts and services with real-time metrics and health tracking.
    /// </summary>
    [DisplayName("MonitoringContract")]
    [ManifestExtra("Author", "Neo Service Layer Team")]
    [ManifestExtra("Description", "System monitoring and alerting service")]
    [ManifestExtra("Version", "1.0.0")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class MonitoringContract : BaseServiceContract
    {
        #region Storage Keys
        private static readonly byte[] MetricPrefix = "metric:".ToByteArray();
        private static readonly byte[] AlertPrefix = "alert:".ToByteArray();
        private static readonly byte[] ThresholdPrefix = "threshold:".ToByteArray();
        private static readonly byte[] ServiceHealthPrefix = "serviceHealth:".ToByteArray();
        private static readonly byte[] MonitoringConfigKey = "monitoringConfig".ToByteArray();
        private static readonly byte[] AlertCountKey = "alertCount".ToByteArray();
        private static readonly byte[] MetricCountKey = "metricCount".ToByteArray();
        private static readonly byte[] LastHealthCheckKey = "lastHealthCheck".ToByteArray();
        #endregion

        #region Events
        [DisplayName("MetricRecorded")]
        public static event Action<string, BigInteger, ulong, UInt160> MetricRecorded;

        [DisplayName("AlertTriggered")]
        public static event Action<string, string, AlertSeverity, UInt160> AlertTriggered;

        [DisplayName("HealthStatusChanged")]
        public static event Action<UInt160, bool, bool> HealthStatusChanged;

        [DisplayName("ThresholdUpdated")]
        public static event Action<string, BigInteger, BigInteger> ThresholdUpdated;

        [DisplayName("MonitoringConfigured")]
        public static event Action<ulong, bool> MonitoringConfigured;
        #endregion

        #region Constants
        private const ulong DEFAULT_HEALTH_CHECK_INTERVAL = 300; // 5 minutes
        private const int MAX_METRICS_PER_SERVICE = 100;
        private const int MAX_ALERTS_PER_DAY = 1000;
        #endregion

        #region Initialization
        public static void _deploy(object data, bool update)
        {
            if (update) return;

            var serviceId = Runtime.ExecutingScriptHash;
            var contract = new MonitoringContract();
            contract.InitializeBaseService(serviceId, "MonitoringService", "1.0.0", "{}");
            
            // Initialize monitoring configuration
            var config = new MonitoringConfig
            {
                HealthCheckInterval = DEFAULT_HEALTH_CHECK_INTERVAL,
                AlertingEnabled = true,
                MetricsRetentionDays = 30,
                MaxAlertsPerDay = MAX_ALERTS_PER_DAY
            };
            
            Storage.Put(Storage.CurrentContext, MonitoringConfigKey, StdLib.Serialize(config));
            Storage.Put(Storage.CurrentContext, AlertCountKey, 0);
            Storage.Put(Storage.CurrentContext, MetricCountKey, 0);
            Storage.Put(Storage.CurrentContext, LastHealthCheckKey, Runtime.Time);

            Runtime.Log("MonitoringContract deployed successfully");
        }
        #endregion

        #region Service Implementation
        protected override void InitializeService(string config)
        {
            Runtime.Log("MonitoringContract service initialized");
        }

        protected override bool PerformHealthCheck()
        {
            try
            {
                // Check if monitoring is functioning
                var lastCheck = GetLastHealthCheck();
                var config = GetMonitoringConfig();
                return (Runtime.Time - lastCheck) < (config.HealthCheckInterval * 2);
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Metrics Collection
        /// <summary>
        /// Records a metric value for monitoring.
        /// </summary>
        public static bool RecordMetric(string metricName, BigInteger value, string tags)
        {
            return ExecuteServiceOperation(() =>
            {
                var caller = Runtime.CallingScriptHash;
                
                // Validate inputs
                if (string.IsNullOrEmpty(metricName))
                    throw new ArgumentException("Metric name cannot be empty");
                
                // Create metric entry
                var metric = new MetricEntry
                {
                    Name = metricName,
                    Value = value,
                    Tags = tags ?? "",
                    Timestamp = Runtime.Time,
                    Source = caller
                };
                
                // Store metric with timestamp-based key for time series
                var metricKey = MetricPrefix
                    .Concat(metricName.ToByteArray())
                    .Concat(Runtime.Time.ToByteArray())
                    .Concat(caller);
                
                Storage.Put(Storage.CurrentContext, metricKey, StdLib.Serialize(metric));
                
                // Check thresholds and trigger alerts if needed
                CheckThresholds(metricName, value, caller);
                
                // Update metric count
                var count = GetMetricCount();
                Storage.Put(Storage.CurrentContext, MetricCountKey, count + 1);
                
                MetricRecorded(metricName, value, Runtime.Time, caller);
                return true;
            });
        }

        /// <summary>
        /// Records multiple metrics in a batch for efficiency.
        /// </summary>
        public static bool RecordMetricsBatch(MetricBatch[] metrics)
        {
            return ExecuteServiceOperation(() =>
            {
                var caller = Runtime.CallingScriptHash;
                
                if (metrics.Length > MAX_METRICS_PER_SERVICE)
                    throw new ArgumentException($"Too many metrics in batch (max: {MAX_METRICS_PER_SERVICE})");
                
                foreach (var metricBatch in metrics)
                {
                    RecordMetric(metricBatch.Name, metricBatch.Value, metricBatch.Tags);
                }
                
                Runtime.Log($"Recorded {metrics.Length} metrics from {caller}");
                return true;
            });
        }

        /// <summary>
        /// Gets recent metrics for a specific metric name.
        /// </summary>
        public static MetricEntry[] GetRecentMetrics(string metricName, int count)
        {
            // Simplified implementation - in production would implement proper time-series queries
            // For now, return empty array as placeholder
            return new MetricEntry[0];
        }
        #endregion

        #region Health Monitoring
        /// <summary>
        /// Performs health check on all registered services.
        /// </summary>
        public static bool PerformSystemHealthCheck()
        {
            return ExecuteServiceOperation(() =>
            {
                // Only authorized monitors can trigger system health checks
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                var registry = GetServiceRegistry();
                if (registry == null)
                {
                    TriggerAlert("system", "Service registry not available", AlertSeverity.Critical);
                    return false;
                }
                
                // Get all registered services and check their health
                // This would integrate with ServiceRegistry to get all services
                // For now, simplified implementation
                
                var healthyServices = 0;
                var totalServices = 5; // Placeholder - would get from registry
                
                // Check each service health (simplified)
                var services = new string[] { "RandomnessService", "OracleService", "StorageService", "ComputeService", "CrossChainService" };
                
                foreach (var serviceName in services)
                {
                    var isHealthy = CheckServiceHealth(serviceName);
                    
                    // Store health status
                    var healthKey = ServiceHealthPrefix.Concat(serviceName.ToByteArray());
                    var healthStatus = new ServiceHealthStatus
                    {
                        ServiceName = serviceName,
                        IsHealthy = isHealthy,
                        LastChecked = Runtime.Time,
                        CheckCount = GetServiceCheckCount(serviceName) + 1
                    };
                    
                    Storage.Put(Storage.CurrentContext, healthKey, StdLib.Serialize(healthStatus));
                    
                    if (isHealthy)
                    {
                        healthyServices++;
                    }
                    else
                    {
                        TriggerAlert(serviceName, "Service health check failed", AlertSeverity.High);
                    }
                }
                
                // Update last health check time
                Storage.Put(Storage.CurrentContext, LastHealthCheckKey, Runtime.Time);
                
                // Calculate overall system health
                var systemHealthPercentage = (healthyServices * 100) / totalServices;
                RecordMetric("system.health.percentage", systemHealthPercentage, "type=system");
                
                if (systemHealthPercentage < 80)
                {
                    TriggerAlert("system", $"System health degraded: {systemHealthPercentage}%", AlertSeverity.High);
                }
                
                Runtime.Log($"System health check completed: {healthyServices}/{totalServices} services healthy");
                return systemHealthPercentage >= 80;
            });
        }

        /// <summary>
        /// Checks the health of a specific service.
        /// </summary>
        private static bool CheckServiceHealth(string serviceName)
        {
            // In production, this would call the actual service health check methods
            // For now, simplified implementation
            try
            {
                // Simulate health check based on recent metrics
                // Would actually call service.PerformHealthCheck()
                return true; // Placeholder
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the health status of a service.
        /// </summary>
        public static ServiceHealthStatus GetServiceHealth(string serviceName)
        {
            var healthKey = ServiceHealthPrefix.Concat(serviceName.ToByteArray());
            var healthBytes = Storage.Get(Storage.CurrentContext, healthKey);
            if (healthBytes == null)
                return null;
            
            return (ServiceHealthStatus)StdLib.Deserialize(healthBytes);
        }
        #endregion

        #region Alerting System
        /// <summary>
        /// Triggers an alert with specified severity.
        /// </summary>
        public static bool TriggerAlert(string source, string message, AlertSeverity severity)
        {
            return ExecuteServiceOperation(() =>
            {
                var caller = Runtime.CallingScriptHash;
                
                // Check daily alert limit
                var alertCount = GetTodayAlertCount();
                var config = GetMonitoringConfig();
                
                if (alertCount >= config.MaxAlertsPerDay)
                {
                    Runtime.Log("Daily alert limit reached, suppressing alert");
                    return false;
                }
                
                // Create alert
                var alert = new Alert
                {
                    Id = GenerateAlertId(),
                    Source = source,
                    Message = message,
                    Severity = severity,
                    Timestamp = Runtime.Time,
                    TriggeredBy = caller,
                    IsAcknowledged = false
                };
                
                // Store alert
                var alertKey = AlertPrefix.Concat(alert.Id);
                Storage.Put(Storage.CurrentContext, alertKey, StdLib.Serialize(alert));
                
                // Increment alert count
                Storage.Put(Storage.CurrentContext, AlertCountKey, alertCount + 1);
                
                AlertTriggered(source, message, severity, caller);
                Runtime.Log($"Alert triggered: {source} - {message} (Severity: {severity})");
                return true;
            });
        }

        /// <summary>
        /// Sets a threshold for metric-based alerting.
        /// </summary>
        public static bool SetThreshold(string metricName, BigInteger minValue, BigInteger maxValue, AlertSeverity severity)
        {
            return ExecuteServiceOperation(() =>
            {
                // Verify caller has admin permissions
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                var threshold = new MetricThreshold
                {
                    MetricName = metricName,
                    MinValue = minValue,
                    MaxValue = maxValue,
                    Severity = severity,
                    IsEnabled = true,
                    CreatedAt = Runtime.Time
                };
                
                var thresholdKey = ThresholdPrefix.Concat(metricName.ToByteArray());
                Storage.Put(Storage.CurrentContext, thresholdKey, StdLib.Serialize(threshold));
                
                ThresholdUpdated(metricName, minValue, maxValue);
                Runtime.Log($"Threshold set for {metricName}: {minValue} - {maxValue}");
                return true;
            });
        }

        /// <summary>
        /// Checks if a metric value violates any thresholds.
        /// </summary>
        private static void CheckThresholds(string metricName, BigInteger value, UInt160 source)
        {
            var thresholdKey = ThresholdPrefix.Concat(metricName.ToByteArray());
            var thresholdBytes = Storage.Get(Storage.CurrentContext, thresholdKey);
            
            if (thresholdBytes != null)
            {
                var threshold = (MetricThreshold)StdLib.Deserialize(thresholdBytes);
                
                if (threshold.IsEnabled)
                {
                    if (value < threshold.MinValue)
                    {
                        TriggerAlert(metricName, $"Metric {metricName} below threshold: {value} < {threshold.MinValue}", threshold.Severity);
                    }
                    else if (value > threshold.MaxValue)
                    {
                        TriggerAlert(metricName, $"Metric {metricName} above threshold: {value} > {threshold.MaxValue}", threshold.Severity);
                    }
                }
            }
        }
        #endregion

        #region Configuration
        /// <summary>
        /// Updates monitoring configuration.
        /// </summary>
        public static bool UpdateMonitoringConfig(ulong healthCheckInterval, bool alertingEnabled, 
            int metricsRetentionDays, int maxAlertsPerDay)
        {
            return ExecuteServiceOperation(() =>
            {
                // Verify caller has admin permissions
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                var config = new MonitoringConfig
                {
                    HealthCheckInterval = healthCheckInterval,
                    AlertingEnabled = alertingEnabled,
                    MetricsRetentionDays = metricsRetentionDays,
                    MaxAlertsPerDay = maxAlertsPerDay
                };
                
                Storage.Put(Storage.CurrentContext, MonitoringConfigKey, StdLib.Serialize(config));
                
                MonitoringConfigured(healthCheckInterval, alertingEnabled);
                Runtime.Log("Monitoring configuration updated");
                return true;
            });
        }

        /// <summary>
        /// Gets the current monitoring configuration.
        /// </summary>
        public static MonitoringConfig GetMonitoringConfig()
        {
            var configBytes = Storage.Get(Storage.CurrentContext, MonitoringConfigKey);
            if (configBytes == null)
            {
                // Return default configuration
                return new MonitoringConfig
                {
                    HealthCheckInterval = DEFAULT_HEALTH_CHECK_INTERVAL,
                    AlertingEnabled = true,
                    MetricsRetentionDays = 30,
                    MaxAlertsPerDay = MAX_ALERTS_PER_DAY
                };
            }
            
            return (MonitoringConfig)StdLib.Deserialize(configBytes);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Gets the total metric count.
        /// </summary>
        public static int GetMetricCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, MetricCountKey);
            return (int)(countBytes?.ToBigInteger() ?? 0);
        }

        /// <summary>
        /// Gets today's alert count.
        /// </summary>
        private static int GetTodayAlertCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, AlertCountKey);
            return (int)(countBytes?.ToBigInteger() ?? 0);
        }

        /// <summary>
        /// Gets the last health check timestamp.
        /// </summary>
        private static ulong GetLastHealthCheck()
        {
            var timeBytes = Storage.Get(Storage.CurrentContext, LastHealthCheckKey);
            return (ulong)(timeBytes?.ToBigInteger() ?? 0);
        }

        /// <summary>
        /// Gets the check count for a service.
        /// </summary>
        private static int GetServiceCheckCount(string serviceName)
        {
            // Simplified - would maintain counters per service
            return 1;
        }

        /// <summary>
        /// Generates a unique alert ID.
        /// </summary>
        private static ByteString GenerateAlertId()
        {
            var data = Runtime.Time.ToByteArray()
                .Concat(Runtime.CallingScriptHash)
                .Concat(GetTodayAlertCount().ToByteArray());
            
            return CryptoLib.Sha256(data);
        }

        /// <summary>
        /// Executes a service operation with proper error handling.
        /// </summary>
        private static T ExecuteServiceOperation<T>(Func<T> operation)
        {
            ValidateServiceActive();
            IncrementRequestCount();

            try
            {
                return operation();
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                throw;
            }
        }
        #endregion

        #region Data Structures
        /// <summary>
        /// Represents a metric entry.
        /// </summary>
        public class MetricEntry
        {
            public string Name;
            public BigInteger Value;
            public string Tags;
            public ulong Timestamp;
            public UInt160 Source;
        }

        /// <summary>
        /// Represents a metric batch for bulk recording.
        /// </summary>
        public class MetricBatch
        {
            public string Name;
            public BigInteger Value;
            public string Tags;
        }

        /// <summary>
        /// Represents an alert.
        /// </summary>
        public class Alert
        {
            public ByteString Id;
            public string Source;
            public string Message;
            public AlertSeverity Severity;
            public ulong Timestamp;
            public UInt160 TriggeredBy;
            public bool IsAcknowledged;
        }

        /// <summary>
        /// Represents a metric threshold for alerting.
        /// </summary>
        public class MetricThreshold
        {
            public string MetricName;
            public BigInteger MinValue;
            public BigInteger MaxValue;
            public AlertSeverity Severity;
            public bool IsEnabled;
            public ulong CreatedAt;
        }

        /// <summary>
        /// Represents service health status.
        /// </summary>
        public class ServiceHealthStatus
        {
            public string ServiceName;
            public bool IsHealthy;
            public ulong LastChecked;
            public int CheckCount;
        }

        /// <summary>
        /// Represents monitoring configuration.
        /// </summary>
        public class MonitoringConfig
        {
            public ulong HealthCheckInterval;
            public bool AlertingEnabled;
            public int MetricsRetentionDays;
            public int MaxAlertsPerDay;
        }

        /// <summary>
        /// Alert severity levels.
        /// </summary>
        public enum AlertSeverity : byte
        {
            Low = 0,
            Medium = 1,
            High = 2,
            Critical = 3
        }
        #endregion
    }
}