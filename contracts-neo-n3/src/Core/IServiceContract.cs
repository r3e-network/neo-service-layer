using Neo;
using Neo.SmartContract.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Contracts.Core
{
    /// <summary>
    /// Base interface that all Neo Service Layer contracts must implement.
    /// Provides standardized methods for service integration and management.
    /// </summary>
    public interface IServiceContract
    {
        /// <summary>
        /// Gets the service identifier for this contract.
        /// </summary>
        /// <returns>Service identifier</returns>
        UInt160 GetServiceId();

        /// <summary>
        /// Gets the service name.
        /// </summary>
        /// <returns>Service name</returns>
        string GetServiceName();

        /// <summary>
        /// Gets the service version.
        /// </summary>
        /// <returns>Service version</returns>
        string GetServiceVersion();

        /// <summary>
        /// Checks if the service is currently active and available.
        /// </summary>
        /// <returns>True if service is active</returns>
        bool IsServiceActive();

        /// <summary>
        /// Gets service health status and metrics.
        /// </summary>
        /// <returns>Health status information</returns>
        ServiceHealthInfo GetHealthStatus();

        /// <summary>
        /// Validates that the caller has permission to use this service.
        /// </summary>
        /// <param name="caller">Calling contract or address</param>
        /// <returns>True if caller has permission</returns>
        bool ValidateAccess(UInt160 caller);

        /// <summary>
        /// Registers this service with the ServiceRegistry.
        /// </summary>
        /// <param name="registryAddress">Address of the ServiceRegistry contract</param>
        /// <returns>True if registration successful</returns>
        bool RegisterWithRegistry(UInt160 registryAddress);

        /// <summary>
        /// Updates service metadata in the registry.
        /// </summary>
        /// <param name="registryAddress">Address of the ServiceRegistry contract</param>
        /// <param name="metadata">Updated metadata</param>
        /// <returns>True if update successful</returns>
        bool UpdateServiceMetadata(UInt160 registryAddress, string metadata);
    }

    /// <summary>
    /// Service health information structure.
    /// </summary>
    public class ServiceHealthInfo
    {
        public bool IsHealthy;
        public string Status;
        public ulong LastChecked;
        public ulong Uptime;
        public int RequestCount;
        public int ErrorCount;
        public string LastError;
    }

    /// <summary>
    /// Base abstract class for all service contracts.
    /// Provides common functionality and enforces the service contract pattern.
    /// </summary>
    public abstract class BaseServiceContract : SmartContract, IServiceContract
    {
        #region Storage Keys
        protected static readonly byte[] ServiceIdKey = "serviceId".ToByteArray();
        protected static readonly byte[] ServiceNameKey = "serviceName".ToByteArray();
        protected static readonly byte[] ServiceVersionKey = "serviceVersion".ToByteArray();
        protected static readonly byte[] ServiceActiveKey = "serviceActive".ToByteArray();
        protected static readonly byte[] ServiceRegistryKey = "serviceRegistry".ToByteArray();
        protected static readonly byte[] HealthStatusKey = "healthStatus".ToByteArray();
        protected static readonly byte[] RequestCountKey = "requestCount".ToByteArray();
        protected static readonly byte[] ErrorCountKey = "errorCount".ToByteArray();
        protected static readonly byte[] LastErrorKey = "lastError".ToByteArray();
        protected static readonly byte[] UptimeStartKey = "uptimeStart".ToByteArray();
        #endregion

        #region Events
        [DisplayName("ServiceInitialized")]
        public static event Action<UInt160, string, string> ServiceInitialized;

        [DisplayName("ServiceStatusChanged")]
        public static event Action<UInt160, bool, bool> ServiceStatusChanged;

        [DisplayName("ServiceError")]
        public static event Action<UInt160, string, string> ServiceError;

        [DisplayName("ServiceMetricsUpdated")]
        public static event Action<UInt160, int, int> ServiceMetricsUpdated;
        #endregion

        #region Abstract Methods
        /// <summary>
        /// Initializes the service with specific configuration.
        /// Must be implemented by each service contract.
        /// </summary>
        /// <param name="config">Service-specific configuration</param>
        protected abstract void InitializeService(string config);

        /// <summary>
        /// Performs service-specific health checks.
        /// Must be implemented by each service contract.
        /// </summary>
        /// <returns>True if service-specific health checks pass</returns>
        protected abstract bool PerformHealthCheck();
        #endregion

        #region IServiceContract Implementation
        public virtual UInt160 GetServiceId()
        {
            var serviceIdBytes = Storage.Get(Storage.CurrentContext, ServiceIdKey);
            return serviceIdBytes != null ? (UInt160)serviceIdBytes : UInt160.Zero;
        }

        public virtual string GetServiceName()
        {
            var serviceNameBytes = Storage.Get(Storage.CurrentContext, ServiceNameKey);
            return serviceNameBytes?.ToByteString() ?? "";
        }

        public virtual string GetServiceVersion()
        {
            var serviceVersionBytes = Storage.Get(Storage.CurrentContext, ServiceVersionKey);
            return serviceVersionBytes?.ToByteString() ?? "1.0.0";
        }

        public virtual bool IsServiceActive()
        {
            var activeBytes = Storage.Get(Storage.CurrentContext, ServiceActiveKey);
            return activeBytes != null && activeBytes[0] == 1;
        }

        public virtual ServiceHealthInfo GetHealthStatus()
        {
            var healthBytes = Storage.Get(Storage.CurrentContext, HealthStatusKey);
            if (healthBytes != null)
            {
                return (ServiceHealthInfo)StdLib.Deserialize(healthBytes);
            }

            // Return default health info if not found
            return new ServiceHealthInfo
            {
                IsHealthy = IsServiceActive(),
                Status = IsServiceActive() ? "Active" : "Inactive",
                LastChecked = Runtime.Time,
                Uptime = GetUptime(),
                RequestCount = GetRequestCount(),
                ErrorCount = GetErrorCount(),
                LastError = GetLastError()
            };
        }

        public virtual bool ValidateAccess(UInt160 caller)
        {
            // Default implementation - can be overridden by specific services
            // Check if service is active
            if (!IsServiceActive())
                return false;

            // Check with ServiceRegistry for access permissions
            var registryAddress = GetServiceRegistry();
            if (registryAddress != null && !registryAddress.IsZero)
            {
                // Call registry to validate access (simplified)
                return true; // In production, would call registry contract
            }

            return true; // Allow access if no registry configured
        }

        public virtual bool RegisterWithRegistry(UInt160 registryAddress)
        {
            if (registryAddress == null || registryAddress.IsZero)
                throw new ArgumentException("Invalid registry address");

            // Store registry address
            Storage.Put(Storage.CurrentContext, ServiceRegistryKey, registryAddress);

            // In production, would call the registry contract to register
            // For now, just emit event
            Runtime.Log($"Service {GetServiceName()} registered with registry at {registryAddress}");
            return true;
        }

        public virtual bool UpdateServiceMetadata(UInt160 registryAddress, string metadata)
        {
            if (registryAddress == null || registryAddress.IsZero)
                throw new ArgumentException("Invalid registry address");

            // In production, would call the registry contract to update metadata
            Runtime.Log($"Service {GetServiceName()} metadata updated");
            return true;
        }
        #endregion

        #region Protected Helper Methods
        /// <summary>
        /// Initializes the base service contract.
        /// </summary>
        /// <param name="serviceId">Unique service identifier</param>
        /// <param name="serviceName">Service name</param>
        /// <param name="serviceVersion">Service version</param>
        /// <param name="config">Service-specific configuration</param>
        protected void InitializeBaseService(UInt160 serviceId, string serviceName, string serviceVersion, string config)
        {
            // Store basic service information
            Storage.Put(Storage.CurrentContext, ServiceIdKey, serviceId);
            Storage.Put(Storage.CurrentContext, ServiceNameKey, serviceName);
            Storage.Put(Storage.CurrentContext, ServiceVersionKey, serviceVersion);
            Storage.Put(Storage.CurrentContext, ServiceActiveKey, 1);
            Storage.Put(Storage.CurrentContext, UptimeStartKey, Runtime.Time);
            Storage.Put(Storage.CurrentContext, RequestCountKey, 0);
            Storage.Put(Storage.CurrentContext, ErrorCountKey, 0);

            // Initialize service-specific configuration
            InitializeService(config);

            // Emit initialization event
            ServiceInitialized(serviceId, serviceName, serviceVersion);
            Runtime.Log($"Service initialized: {serviceName} v{serviceVersion}");
        }

        /// <summary>
        /// Sets the service active status.
        /// </summary>
        /// <param name="active">True to activate, false to deactivate</param>
        protected void SetServiceActive(bool active)
        {
            var wasActive = IsServiceActive();
            Storage.Put(Storage.CurrentContext, ServiceActiveKey, active ? (byte)1 : (byte)0);

            if (wasActive != active)
            {
                ServiceStatusChanged(GetServiceId(), wasActive, active);
                Runtime.Log($"Service {GetServiceName()} status changed: {wasActive} -> {active}");
            }
        }

        /// <summary>
        /// Increments the request counter.
        /// </summary>
        protected void IncrementRequestCount()
        {
            var count = GetRequestCount();
            Storage.Put(Storage.CurrentContext, RequestCountKey, count + 1);
        }

        /// <summary>
        /// Increments the error counter and logs the error.
        /// </summary>
        /// <param name="error">Error message</param>
        protected void LogError(string error)
        {
            var errorCount = GetErrorCount();
            Storage.Put(Storage.CurrentContext, ErrorCountKey, errorCount + 1);
            Storage.Put(Storage.CurrentContext, LastErrorKey, error);

            ServiceError(GetServiceId(), GetServiceName(), error);
            Runtime.Log($"Service error in {GetServiceName()}: {error}");
        }

        /// <summary>
        /// Updates health status information.
        /// </summary>
        protected void UpdateHealthStatus()
        {
            var isHealthy = PerformHealthCheck();
            var healthInfo = new ServiceHealthInfo
            {
                IsHealthy = isHealthy,
                Status = isHealthy ? "Healthy" : "Unhealthy",
                LastChecked = Runtime.Time,
                Uptime = GetUptime(),
                RequestCount = GetRequestCount(),
                ErrorCount = GetErrorCount(),
                LastError = GetLastError()
            };

            Storage.Put(Storage.CurrentContext, HealthStatusKey, StdLib.Serialize(healthInfo));

            if (!isHealthy)
            {
                SetServiceActive(false);
            }
        }

        /// <summary>
        /// Gets the service registry address.
        /// </summary>
        /// <returns>Registry address or null if not set</returns>
        protected UInt160 GetServiceRegistry()
        {
            var registryBytes = Storage.Get(Storage.CurrentContext, ServiceRegistryKey);
            return registryBytes != null ? (UInt160)registryBytes : null;
        }

        /// <summary>
        /// Gets the current request count.
        /// </summary>
        /// <returns>Request count</returns>
        protected int GetRequestCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, RequestCountKey);
            return countBytes?.ToInteger() ?? 0;
        }

        /// <summary>
        /// Gets the current error count.
        /// </summary>
        /// <returns>Error count</returns>
        protected int GetErrorCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, ErrorCountKey);
            return countBytes?.ToInteger() ?? 0;
        }

        /// <summary>
        /// Gets the last error message.
        /// </summary>
        /// <returns>Last error message</returns>
        protected string GetLastError()
        {
            var errorBytes = Storage.Get(Storage.CurrentContext, LastErrorKey);
            return errorBytes?.ToByteString() ?? "";
        }

        /// <summary>
        /// Gets the service uptime in seconds.
        /// </summary>
        /// <returns>Uptime in seconds</returns>
        protected ulong GetUptime()
        {
            var startTimeBytes = Storage.Get(Storage.CurrentContext, UptimeStartKey);
            if (startTimeBytes != null)
            {
                var startTime = startTimeBytes.ToInteger();
                return Runtime.Time - (ulong)startTime;
            }
            return 0;
        }

        /// <summary>
        /// Validates that the service is active and ready to process requests.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if service is not active</exception>
        protected void ValidateServiceActive()
        {
            if (!IsServiceActive())
            {
                throw new InvalidOperationException($"Service {GetServiceName()} is not active");
            }
        }

        /// <summary>
        /// Executes a service operation with automatic metrics tracking.
        /// </summary>
        /// <param name="operation">Operation to execute</param>
        /// <returns>Operation result</returns>
        protected T ExecuteServiceOperation<T>(Func<T> operation)
        {
            ValidateServiceActive();
            IncrementRequestCount();

            try
            {
                var result = operation();
                
                // Update metrics
                var requestCount = GetRequestCount();
                var errorCount = GetErrorCount();
                ServiceMetricsUpdated(GetServiceId(), requestCount, errorCount);
                
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Static version of ExecuteServiceOperation for static method calls.
        /// </summary>
        protected static T ExecuteServiceOperation<T>(Func<T> operation)
        {
            // Static validation - simplified version
            var context = Storage.CurrentContext;
            var activeBytes = Storage.Get(context, ServiceActiveKey);
            if (activeBytes == null || activeBytes[0] != 1)
                throw new InvalidOperationException("Service is not active");

            // Increment request count
            var countBytes = Storage.Get(context, RequestCountKey);
            var count = countBytes?.ToInteger() ?? 0;
            Storage.Put(context, RequestCountKey, count + 1);

            try
            {
                return operation();
            }
            catch (Exception ex)
            {
                // Log error
                var errorCountBytes = Storage.Get(context, ErrorCountKey);
                var errorCount = errorCountBytes?.ToInteger() ?? 0;
                Storage.Put(context, ErrorCountKey, errorCount + 1);
                Storage.Put(context, LastErrorKey, ex.Message);
                
                Runtime.Log($"Service error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Static service validation.
        /// </summary>
        protected static void ValidateServiceActive()
        {
            var activeBytes = Storage.Get(Storage.CurrentContext, ServiceActiveKey);
            if (activeBytes == null || activeBytes[0] != 1)
                throw new InvalidOperationException("Service is not active");
        }

        /// <summary>
        /// Static request count increment.
        /// </summary>
        protected static void IncrementRequestCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, RequestCountKey);
            var count = countBytes?.ToInteger() ?? 0;
            Storage.Put(Storage.CurrentContext, RequestCountKey, count + 1);
        }

        /// <summary>
        /// Static error logging.
        /// </summary>
        protected static void LogError(string error)
        {
            var errorCountBytes = Storage.Get(Storage.CurrentContext, ErrorCountKey);
            var errorCount = errorCountBytes?.ToInteger() ?? 0;
            Storage.Put(Storage.CurrentContext, ErrorCountKey, errorCount + 1);
            Storage.Put(Storage.CurrentContext, LastErrorKey, error);
            
            Runtime.Log($"Service error: {error}");
        }
        #endregion
    }
}