using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;


namespace NeoServiceLayer.TestUtilities.MockServices
{
    /// <summary>
    /// Base class for mock services providing common functionality for testing.
    /// </summary>
    public abstract class MockServiceBase
    {
        protected readonly ILogger _logger;
        protected readonly Dictionary<string, object> _configuration;
        protected readonly Dictionary<string, object> _state;
        protected readonly List<MockServiceCall> _callHistory;
        protected bool _isHealthy = true;
        protected TimeSpan _responseDelay = TimeSpan.Zero;
        protected double _failureRate = 0.0;
        protected readonly Random _random = new(42);

        protected MockServiceBase(ILogger logger, Dictionary<string, object>? configuration = null)
        {
            _logger = logger;
            _configuration = configuration ?? new Dictionary<string, object>();
            _state = new Dictionary<string, object>();
            _callHistory = new List<MockServiceCall>();
        }

        /// <summary>
        /// Simulates service health status.
        /// </summary>
        public virtual async Task<ServiceHealthResult> CheckHealthAsync()
        {
            await SimulateDelay();

            return new ServiceHealthResult
            {
                IsHealthy = _isHealthy,
                ServiceName = GetType().Name,
                Timestamp = DateTime.UtcNow,
                ResponseTime = _responseDelay,
                Details = new Dictionary<string, object>
                {
                    ["callCount"] = _callHistory.Count,
                    ["lastCall"] = _callHistory.Count > 0 ? _callHistory[^1].Timestamp : null,
                    ["failureRate"] = _failureRate
                }
            };
        }

        /// <summary>
        /// Gets the call history for this mock service.
        /// </summary>
        public List<MockServiceCall> GetCallHistory() => new(_callHistory);

        /// <summary>
        /// Clears the call history.
        /// </summary>
        public void ClearCallHistory() => _callHistory.Clear();

        /// <summary>
        /// Configures the mock service behavior.
        /// </summary>
        public virtual void ConfigureMock(MockServiceConfiguration config)
        {
            _isHealthy = config.IsHealthy;
            _responseDelay = config.ResponseDelay;
            _failureRate = config.FailureRate;
            
            if (config.CustomConfiguration != null)
            {
                foreach (var item in config.CustomConfiguration)
                {
                    _configuration[item.Key] = item.Value;
                }
            }

            _logger.LogDebug("Mock service {ServiceName} configured: Health={IsHealthy}, Delay={Delay}ms, FailureRate={FailureRate}%",
                GetType().Name, _isHealthy, _responseDelay.TotalMilliseconds, _failureRate * 100);
        }

        /// <summary>
        /// Records a service call for tracking.
        /// </summary>
        protected void RecordCall(string methodName, object? parameters = null, object? result = null, Exception? exception = null)
        {
            var call = new MockServiceCall
            {
                ServiceName = GetType().Name,
                MethodName = methodName,
                Parameters = parameters,
                Result = result,
                Exception = exception,
                Timestamp = DateTime.UtcNow,
                Duration = _responseDelay
            };

            _callHistory.Add(call);
            
            _logger.LogDebug("Recorded call to {ServiceName}.{MethodName}", GetType().Name, methodName);
        }

        /// <summary>
        /// Simulates network/processing delay.
        /// </summary>
        protected async Task SimulateDelay()
        {
            if (_responseDelay > TimeSpan.Zero)
            {
                await Task.Delay(_responseDelay);
            }
        }

        /// <summary>
        /// Simulates random failures based on configured failure rate.
        /// </summary>
        protected void SimulateFailure(string operation)
        {
            if (_failureRate > 0 && _random.NextDouble() < _failureRate)
            {
                throw new MockServiceException($"Simulated failure in {GetType().Name}.{operation}");
            }
        }

        /// <summary>
        /// Gets or sets state for the mock service.
        /// </summary>
        protected T GetState<T>(string key, T defaultValue = default!)
        {
            return _state.TryGetValue(key, out var value) && value is T typedValue ? typedValue : defaultValue;
        }

        protected void SetState<T>(string key, T value)
        {
            if (value != null)
                _state[key] = value;
        }
    }

    #region Supporting Classes

    public class MockServiceCall
    {
        public string ServiceName { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public object? Parameters { get; set; }
        public object? Result { get; set; }
        public Exception? Exception { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class ServiceHealthResult
    {
        public bool IsHealthy { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
    }

    public class MockServiceConfiguration
    {
        public bool IsHealthy { get; set; } = true;
        public TimeSpan ResponseDelay { get; set; } = TimeSpan.Zero;
        public double FailureRate { get; set; } = 0.0;
        public Dictionary<string, object>? CustomConfiguration { get; set; }
    }

    public class MockServiceException : Exception
    {
        public MockServiceException(string message) : base(message) { }
        public MockServiceException(string message, Exception innerException) : base(message, innerException) { }
    }

    #endregion
}