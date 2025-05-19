using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.JavaScript;

namespace NeoServiceLayer.Tee.Host.JavaScript
{
    /// <summary>
    /// Implementation of gas accounting.
    /// </summary>
    public class GasAccounting : IGasAccounting
    {
        private readonly ILogger<GasAccounting> _logger;
        private readonly ConcurrentDictionary<string, ulong> _functionGasTotal;
        private readonly ConcurrentDictionary<string, ulong> _functionExecutionCount;
        private readonly ConcurrentDictionary<string, List<GasUsageRecord>> _functionGasHistory;
        private readonly ConcurrentDictionary<string, List<GasUsageRecord>> _userGasHistory;
        private readonly SemaphoreSlim _semaphore;
        private ulong _gasUsed;
        private ulong _gasLimit;

        /// <summary>
        /// Initializes a new instance of the GasAccounting class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public GasAccounting(ILogger<GasAccounting> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _functionGasTotal = new ConcurrentDictionary<string, ulong>();
            _functionExecutionCount = new ConcurrentDictionary<string, ulong>();
            _functionGasHistory = new ConcurrentDictionary<string, List<GasUsageRecord>>();
            _userGasHistory = new ConcurrentDictionary<string, List<GasUsageRecord>>();
            _semaphore = new SemaphoreSlim(1, 1);
            _gasUsed = 0;
            _gasLimit = 10000000; // 10 million gas units
        }

        /// <inheritdoc/>
        public void ResetGasUsed()
        {
            _gasUsed = 0;
        }

        /// <inheritdoc/>
        public ulong GetGasUsed()
        {
            return _gasUsed;
        }

        /// <inheritdoc/>
        public void UseGas(ulong amount)
        {
            // Check for overflow
            if (ulong.MaxValue - _gasUsed < amount)
            {
                _logger.LogWarning("Gas usage overflow");
                throw new InvalidOperationException("Gas usage overflow");
            }

            _gasUsed += amount;

            // Check if gas limit is exceeded
            if (_gasUsed > _gasLimit)
            {
                _logger.LogWarning("Gas limit exceeded: {GasUsed} > {GasLimit}", _gasUsed, _gasLimit);
                throw new InvalidOperationException($"Gas limit exceeded: {_gasUsed} > {_gasLimit}");
            }
        }

        /// <inheritdoc/>
        public void SetGasLimit(ulong limit)
        {
            _gasLimit = limit;
        }

        /// <inheritdoc/>
        public ulong GetGasLimit()
        {
            return _gasLimit;
        }

        /// <inheritdoc/>
        public bool IsGasLimitExceeded()
        {
            return _gasUsed > _gasLimit;
        }

        /// <inheritdoc/>
        public ulong CalculateGasCost(string operationType, ulong size = 0)
        {
            ulong gasCost = 0;

            switch (operationType)
            {
                case "function_call":
                    gasCost = 100;
                    break;
                case "property_access":
                    gasCost = 10;
                    break;
                case "array_access":
                    gasCost = 20;
                    break;
                case "object_creation":
                    gasCost = 50 + size;
                    break;
                case "array_creation":
                    gasCost = 30 + size;
                    break;
                case "string_operation":
                    gasCost = 5 + size / 100;
                    break;
                case "math_operation":
                    gasCost = 5;
                    break;
                case "comparison":
                    gasCost = 3;
                    break;
                case "loop_iteration":
                    gasCost = 10;
                    break;
                case "storage_read":
                    gasCost = 100 + size / 1024;
                    break;
                case "storage_write":
                    gasCost = 200 + size / 512;
                    break;
                case "crypto_operation":
                    gasCost = 500 + size / 256;
                    break;
                default:
                    gasCost = 1; // Default cost
                    break;
            }

            return gasCost;
        }

        /// <inheritdoc/>
        public void RecordGasUsage(string functionId, string userId, ulong gasUsed)
        {
            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            // Update total gas used for the function
            _functionGasTotal.AddOrUpdate(functionId, gasUsed, (key, oldValue) => oldValue + gasUsed);

            // Update execution count for the function
            _functionExecutionCount.AddOrUpdate(functionId, 1, (key, oldValue) => oldValue + 1);

            // Create gas usage record
            GasUsageRecord record = new GasUsageRecord
            {
                FunctionId = functionId,
                UserId = userId,
                GasUsed = gasUsed,
                Timestamp = DateTime.UtcNow
            };

            // Add to function gas history
            _functionGasHistory.AddOrUpdate(
                functionId,
                new List<GasUsageRecord> { record },
                (key, oldValue) =>
                {
                    oldValue.Add(record);
                    return oldValue;
                });

            // Add to user gas history
            if (!string.IsNullOrEmpty(userId))
            {
                _userGasHistory.AddOrUpdate(
                    userId,
                    new List<GasUsageRecord> { record },
                    (key, oldValue) =>
                    {
                        oldValue.Add(record);
                        return oldValue;
                    });
            }

            _logger.LogDebug("Recorded gas usage: Function={FunctionId}, User={UserId}, Gas={GasUsed}", functionId, userId, gasUsed);
        }

        /// <inheritdoc/>
        public ulong GetAverageGasUsage(string functionId)
        {
            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            if (_functionGasTotal.TryGetValue(functionId, out ulong total) &&
                _functionExecutionCount.TryGetValue(functionId, out ulong count) &&
                count > 0)
            {
                return total / count;
            }

            return 0;
        }

        /// <inheritdoc/>
        public ulong GetTotalGasUsage(string functionId)
        {
            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            if (_functionGasTotal.TryGetValue(functionId, out ulong total))
            {
                return total;
            }

            return 0;
        }

        /// <inheritdoc/>
        public ulong GetExecutionCount(string functionId)
        {
            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            if (_functionExecutionCount.TryGetValue(functionId, out ulong count))
            {
                return count;
            }

            return 0;
        }

        /// <inheritdoc/>
        public IReadOnlyList<GasUsageRecord> GetGasUsageHistory(string functionId)
        {
            if (string.IsNullOrEmpty(functionId))
            {
                throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
            }

            if (_functionGasHistory.TryGetValue(functionId, out List<GasUsageRecord> history))
            {
                return history.AsReadOnly();
            }

            return new List<GasUsageRecord>().AsReadOnly();
        }

        /// <inheritdoc/>
        public IReadOnlyList<GasUsageRecord> GetGasUsageHistoryForUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            if (_userGasHistory.TryGetValue(userId, out List<GasUsageRecord> history))
            {
                return history.AsReadOnly();
            }

            return new List<GasUsageRecord>().AsReadOnly();
        }
    }
}
