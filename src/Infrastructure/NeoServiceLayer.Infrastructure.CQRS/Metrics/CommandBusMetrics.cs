using System;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Infrastructure.CQRS
{
    /// <summary>
    /// Metrics collection for command bus operations
    /// </summary>
    public class CommandBusMetrics
    {
        private readonly Meter _meter;
        private readonly Counter<long> _commandsExecuted;
        private readonly Counter<long> _commandsFailed;
        private readonly Histogram<double> _commandDuration;
        private readonly Counter<long> _retries;
        private readonly Counter<long> _circuitBreakerOpened;
        private readonly Counter<long> _circuitBreakerClosed;
        private readonly ConcurrentDictionary<string, long> _commandCounts;

        public CommandBusMetrics()
        {
            _meter = new Meter("NeoServiceLayer.CommandBus", "1.0.0");
            _commandCounts = new ConcurrentDictionary<string, long>();

            _commandsExecuted = _meter.CreateCounter<long>(
                "commands_executed_total",
                description: "Total number of commands executed successfully");

            _commandsFailed = _meter.CreateCounter<long>(
                "commands_failed_total",
                description: "Total number of commands that failed");

            _commandDuration = _meter.CreateHistogram<double>(
                "command_duration_milliseconds",
                unit: "ms",
                description: "Duration of command execution in milliseconds");

            _retries = _meter.CreateCounter<long>(
                "command_retries_total",
                description: "Total number of command execution retries");

            _circuitBreakerOpened = _meter.CreateCounter<long>(
                "circuit_breaker_opened_total",
                description: "Total number of times circuit breaker opened");

            _circuitBreakerClosed = _meter.CreateCounter<long>(
                "circuit_breaker_closed_total",
                description: "Total number of times circuit breaker closed");
        }

        public void RecordCommandSuccess(string commandType, long durationMs)
        {
            _commandsExecuted.Add(1, new KeyValuePair<string, object?>("command_type", commandType));
            _commandDuration.Record(durationMs, new KeyValuePair<string, object?>("command_type", commandType));
            _commandCounts.AddOrUpdate(commandType, 1, (key, value) => value + 1);
        }

        public void RecordCommandFailure(string commandType, long durationMs)
        {
            _commandsFailed.Add(1, new KeyValuePair<string, object?>("command_type", commandType));
            _commandDuration.Record(durationMs, new KeyValuePair<string, object?>("command_type", commandType));
        }

        public void RecordRetry()
        {
            _retries.Add(1);
        }

        public void RecordCircuitBreakerOpen()
        {
            _circuitBreakerOpened.Add(1);
        }

        public void RecordCircuitBreakerClose()
        {
            _circuitBreakerClosed.Add(1);
        }

        public long GetCommandCount(string commandType)
        {
            return _commandCounts.TryGetValue(commandType, out var count) ? count : 0;
        }
    }

    /// <summary>
    /// Metrics collection for query bus operations
    /// </summary>
    public class QueryBusMetrics
    {
        private readonly Meter _meter;
        private readonly Counter<long> _queriesExecuted;
        private readonly Counter<long> _queriesFailed;
        private readonly Histogram<double> _queryDuration;
        private readonly Counter<long> _cacheHits;
        private readonly Counter<long> _cacheMisses;
        private readonly ConcurrentDictionary<string, long> _queryCounts;

        public QueryBusMetrics()
        {
            _meter = new Meter("NeoServiceLayer.QueryBus", "1.0.0");
            _queryCounts = new ConcurrentDictionary<string, long>();

            _queriesExecuted = _meter.CreateCounter<long>(
                "queries_executed_total",
                description: "Total number of queries executed successfully");

            _queriesFailed = _meter.CreateCounter<long>(
                "queries_failed_total",
                description: "Total number of queries that failed");

            _queryDuration = _meter.CreateHistogram<double>(
                "query_duration_milliseconds",
                unit: "ms",
                description: "Duration of query execution in milliseconds");

            _cacheHits = _meter.CreateCounter<long>(
                "query_cache_hits_total",
                description: "Total number of query cache hits");

            _cacheMisses = _meter.CreateCounter<long>(
                "query_cache_misses_total",
                description: "Total number of query cache misses");
        }

        public void RecordQuerySuccess(string queryType, long durationMs)
        {
            _queriesExecuted.Add(1, new KeyValuePair<string, object?>("query_type", queryType));
            _queryDuration.Record(durationMs, new KeyValuePair<string, object?>("query_type", queryType));
            _queryCounts.AddOrUpdate(queryType, 1, (key, value) => value + 1);
        }

        public void RecordQueryFailure(string queryType, long durationMs)
        {
            _queriesFailed.Add(1, new KeyValuePair<string, object?>("query_type", queryType));
            _queryDuration.Record(durationMs, new KeyValuePair<string, object?>("query_type", queryType));
        }

        public void RecordCacheHit(string queryType)
        {
            _cacheHits.Add(1, new KeyValuePair<string, object?>("query_type", queryType));
        }

        public void RecordCacheMiss(string queryType)
        {
            _cacheMisses.Add(1, new KeyValuePair<string, object?>("query_type", queryType));
        }

        public long GetQueryCount(string queryType)
        {
            return _queryCounts.TryGetValue(queryType, out var count) ? count : 0;
        }
    }
}