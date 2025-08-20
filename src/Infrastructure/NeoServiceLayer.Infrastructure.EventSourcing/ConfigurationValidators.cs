using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Infrastructure.EventSourcing
{
    /// <summary>
    /// Validates event store configuration
    /// </summary>
    public class EventStoreConfigurationValidator : IValidateOptions<EventStoreConfiguration>
    {
        public ValidateOptionsResult Validate(string? name, EventStoreConfiguration options)
        {
            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                failures.Add("EventStore:ConnectionString is required");
            }

            if (options.BatchSize <= 0)
            {
                failures.Add("EventStore:BatchSize must be greater than 0");
            }

            if (options.CommandTimeout <= 0)
            {
                failures.Add("EventStore:CommandTimeout must be greater than 0");
            }

            if (options.SnapshotFrequency <= 0)
            {
                failures.Add("EventStore:SnapshotFrequency must be greater than 0");
            }

            if (failures.Any())
            {
                return ValidateOptionsResult.Fail(failures);
            }

            return ValidateOptionsResult.Success;
        }
    }

    /// <summary>
    /// Validates event bus configuration
    /// </summary>
    public class EventBusConfigurationValidator : IValidateOptions<EventBusConfiguration>
    {
        public ValidateOptionsResult Validate(string? name, EventBusConfiguration options)
        {
            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(options.HostName))
            {
                failures.Add("EventBus:HostName is required");
            }

            if (options.Port <= 0 || options.Port > 65535)
            {
                failures.Add("EventBus:Port must be between 1 and 65535");
            }

            if (string.IsNullOrWhiteSpace(options.UserName))
            {
                failures.Add("EventBus:UserName is required");
            }

            if (string.IsNullOrWhiteSpace(options.Password))
            {
                failures.Add("EventBus:Password is required");
            }

            if (string.IsNullOrWhiteSpace(options.ExchangeName))
            {
                failures.Add("EventBus:ExchangeName is required");
            }

            if (options.RetryCount <= 0)
            {
                failures.Add("EventBus:RetryCount must be greater than 0");
            }

            if (options.RetryDelayMs <= 0)
            {
                failures.Add("EventBus:RetryDelayMs must be greater than 0");
            }

            if (options.MessageTtlSeconds <= 0)
            {
                failures.Add("EventBus:MessageTtlSeconds must be greater than 0");
            }

            if (failures.Any())
            {
                return ValidateOptionsResult.Fail(failures);
            }

            return ValidateOptionsResult.Success;
        }
    }

    /// <summary>
    /// Validates event processing configuration
    /// </summary>
    public class EventProcessingConfigurationValidator : IValidateOptions<EventProcessingConfiguration>
    {
        public ValidateOptionsResult Validate(string? name, EventProcessingConfiguration options)
        {
            var failures = new List<string>();

            if (options.MaxConcurrentHandlers <= 0)
            {
                failures.Add("EventProcessing:MaxConcurrentHandlers must be greater than 0");
            }

            if (options.HandlerTimeoutSeconds <= 0)
            {
                failures.Add("EventProcessing:HandlerTimeoutSeconds must be greater than 0");
            }

            if (options.MaxRetryAttempts <= 0)
            {
                failures.Add("EventProcessing:MaxRetryAttempts must be greater than 0");
            }

            if (options.RetryDelayMs <= 0)
            {
                failures.Add("EventProcessing:RetryDelayMs must be greater than 0");
            }

            if (options.ProcessingBatchSize <= 0)
            {
                failures.Add("EventProcessing:ProcessingBatchSize must be greater than 0");
            }

            if (failures.Any())
            {
                return ValidateOptionsResult.Fail(failures);
            }

            return ValidateOptionsResult.Success;
        }
    }
}