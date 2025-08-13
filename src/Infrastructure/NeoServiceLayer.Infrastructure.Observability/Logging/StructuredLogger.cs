using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Infrastructure.Observability.Logging
{
    /// <summary>
    /// Structured logging implementation with correlation ID support.
    /// </summary>
    public interface IStructuredLogger
    {
        string CorrelationId { get; }
        void LogOperation(string operation, Dictionary<string, object> properties = null, LogLevel level = LogLevel.Information);
        void LogMetric(string metricName, double value, Dictionary<string, object> dimensions = null);
        void LogException(Exception exception, string operation, Dictionary<string, object> properties = null);
        IDisposable BeginScope(string scopeName, Dictionary<string, object> properties = null);
        IStructuredLogger CreateChildLogger(string childOperation);
    }

    public class StructuredLogger : IStructuredLogger
    {
        private readonly ILogger _logger;
        private readonly string _correlationId;
        private readonly string _serviceName;
        private readonly Dictionary<string, object> _contextProperties;
        private readonly ActivitySource _activitySource;

        public string CorrelationId => _correlationId;

        public StructuredLogger(
            ILogger logger,
            string serviceName,
            string correlationId = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            _correlationId = correlationId ?? GenerateCorrelationId();
            _contextProperties = new Dictionary<string, object>
            {
                ["CorrelationId"] = _correlationId,
                ["ServiceName"] = _serviceName,
                ["MachineName"] = Environment.MachineName,
                ["ProcessId"] = Environment.ProcessId
            };
            _activitySource = new ActivitySource($"NeoServiceLayer.{serviceName}");
        }

        public void LogOperation(
            string operation,
            Dictionary<string, object> properties = null,
            LogLevel level = LogLevel.Information)
        {
            using var activity = _activitySource.StartActivity(operation, ActivityKind.Internal);
            activity?.SetTag("correlation.id", _correlationId);

            var logProperties = MergeProperties(properties);
            logProperties["Operation"] = operation;
            logProperties["Timestamp"] = DateTimeOffset.UtcNow;
            logProperties["TraceId"] = activity?.TraceId.ToString() ?? "unknown";
            logProperties["SpanId"] = activity?.SpanId.ToString() ?? "unknown";

            using (_logger.BeginScope(logProperties))
            {
                _logger.Log(
                    level,
                    "Operation {Operation} executed with correlation {CorrelationId}",
                    operation,
                    _correlationId);
            }
        }

        public void LogMetric(
            string metricName,
            double value,
            Dictionary<string, object> dimensions = null)
        {
            var logProperties = MergeProperties(dimensions);
            logProperties["MetricName"] = metricName;
            logProperties["MetricValue"] = value;
            logProperties["MetricTimestamp"] = DateTimeOffset.UtcNow;
            logProperties["MetricType"] = "gauge";

            using (_logger.BeginScope(logProperties))
            {
                _logger.LogInformation(
                    "Metric {MetricName} = {MetricValue} recorded for {CorrelationId}",
                    metricName,
                    value,
                    _correlationId);
            }
        }

        public void LogException(
            Exception exception,
            string operation,
            Dictionary<string, object> properties = null)
        {
            using var activity = _activitySource.StartActivity($"{operation}.Error", ActivityKind.Internal);
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity?.RecordException(exception);

            var logProperties = MergeProperties(properties);
            logProperties["Operation"] = operation;
            logProperties["ExceptionType"] = exception.GetType().Name;
            logProperties["ExceptionMessage"] = exception.Message;
            logProperties["StackTrace"] = exception.StackTrace;
            logProperties["ErrorTimestamp"] = DateTimeOffset.UtcNow;

            if (exception.InnerException != null)
            {
                logProperties["InnerException"] = exception.InnerException.Message;
            }

            using (_logger.BeginScope(logProperties))
            {
                _logger.LogError(
                    exception,
                    "Exception in operation {Operation} for correlation {CorrelationId}: {ExceptionMessage}",
                    operation,
                    _correlationId,
                    exception.Message);
            }
        }

        public IDisposable BeginScope(string scopeName, Dictionary<string, object> properties = null)
        {
            var scopeProperties = MergeProperties(properties);
            scopeProperties["ScopeName"] = scopeName;
            scopeProperties["ScopeStartTime"] = DateTimeOffset.UtcNow;

            var activity = _activitySource.StartActivity(scopeName, ActivityKind.Internal);
            activity?.SetTag("correlation.id", _correlationId);

            foreach (var prop in scopeProperties)
            {
                activity?.SetTag(prop.Key, prop.Value?.ToString());
            }

            var loggerScope = _logger.BeginScope(scopeProperties);

            return new CompositeDisposable(activity, loggerScope);
        }

        public IStructuredLogger CreateChildLogger(string childOperation)
        {
            var childLogger = new StructuredLogger(_logger, $"{_serviceName}.{childOperation}", _correlationId);
            childLogger._contextProperties["ParentService"] = _serviceName;
            childLogger._contextProperties["ChildOperation"] = childOperation;
            return childLogger;
        }

        private Dictionary<string, object> MergeProperties(Dictionary<string, object> additionalProperties)
        {
            var merged = new Dictionary<string, object>(_contextProperties);

            if (additionalProperties != null)
            {
                foreach (var kvp in additionalProperties)
                {
                    merged[kvp.Key] = kvp.Value;
                }
            }

            return merged;
        }

        private static string GenerateCorrelationId()
        {
            return $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 32);
        }

        private class CompositeDisposable : IDisposable
        {
            private readonly IDisposable[] _disposables;

            public CompositeDisposable(params IDisposable[] disposables)
            {
                _disposables = disposables;
            }

            public void Dispose()
            {
                foreach (var disposable in _disposables)
                {
                    disposable?.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Factory for creating structured loggers.
    /// </summary>
    public interface IStructuredLoggerFactory
    {
        IStructuredLogger CreateLogger(string serviceName, string correlationId = null);
    }

    public class StructuredLoggerFactory : IStructuredLoggerFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public StructuredLoggerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public IStructuredLogger CreateLogger(string serviceName, string correlationId = null)
        {
            var logger = _loggerFactory.CreateLogger($"NeoServiceLayer.{serviceName}");
            return new StructuredLogger(logger, serviceName, correlationId);
        }
    }
}
