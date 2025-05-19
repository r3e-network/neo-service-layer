using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NeoServiceLayer.Tee.Enclave
{
    /// <summary>
    /// Manages gas accounting for JavaScript execution in the Occlum enclave.
    /// </summary>
    public class GasAccountingManager
    {
        private readonly ILogger<GasAccountingManager> _logger;
        private readonly GasAccountingOptions _options;
        private long _gasUsed;
        private readonly object _lockObject = new object();
        private readonly DateTime _startTime;
        private bool _isExecutionLimited;
        private Timer _monitorTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="GasAccountingManager"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The gas accounting options.</param>
        public GasAccountingManager(
            ILogger<GasAccountingManager> logger,
            IOptions<GasAccountingOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _gasUsed = 0;
            _startTime = DateTime.UtcNow;
            _isExecutionLimited = _options.MaxGasLimit > 0;

            // Start monitoring if gas limiting is enabled
            if (_isExecutionLimited)
            {
                _monitorTimer = new Timer(
                    CheckGasLimit, 
                    null, 
                    TimeSpan.FromMilliseconds(100), 
                    TimeSpan.FromMilliseconds(100));
            }
        }

        /// <summary>
        /// Uses gas for an operation.
        /// </summary>
        /// <param name="amount">The amount of gas to use.</param>
        /// <exception cref="OutOfGasException">Thrown when the gas limit is exceeded.</exception>
        public void UseGas(long amount)
        {
            if (amount <= 0)
            {
                return;
            }

            lock (_lockObject)
            {
                _gasUsed += amount;
                
                // Check if we've exceeded the gas limit
                if (_isExecutionLimited && _gasUsed > _options.MaxGasLimit)
                {
                    _logger.LogWarning("Gas limit exceeded: {GasUsed} > {GasLimit}", 
                        _gasUsed, _options.MaxGasLimit);
                    
                    throw new OutOfGasException($"Gas limit exceeded: {_gasUsed} > {_options.MaxGasLimit}");
                }
            }
        }

        /// <summary>
        /// Gets the amount of gas used so far.
        /// </summary>
        /// <returns>The gas used.</returns>
        public long GetGasUsed()
        {
            lock (_lockObject)
            {
                // Add time-based gas cost
                if (_options.EnableTimeBasedGas)
                {
                    TimeSpan elapsed = DateTime.UtcNow - _startTime;
                    long timeBasedGas = (long)(elapsed.TotalMilliseconds * _options.GasPerMillisecond);
                    
                    // Add to the current gas used
                    _gasUsed += timeBasedGas;
                    
                    _logger.LogDebug("Added time-based gas: {TimeBasedGas} for {ElapsedMs}ms, total: {TotalGas}", 
                        timeBasedGas, elapsed.TotalMilliseconds, _gasUsed);
                }
                
                return _gasUsed;
            }
        }

        /// <summary>
        /// Resets the gas accounting.
        /// </summary>
        public void ResetGasUsed()
        {
            lock (_lockObject)
            {
                _gasUsed = 0;
            }
        }

        /// <summary>
        /// Checks if a gas limit has been exceeded.
        /// </summary>
        /// <param name="state">The state object.</param>
        private void CheckGasLimit(object state)
        {
            if (!_isExecutionLimited)
            {
                return;
            }

            lock (_lockObject)
            {
                if (_options.EnableTimeBasedGas)
                {
                    TimeSpan elapsed = DateTime.UtcNow - _startTime;
                    long timeBasedGas = (long)(elapsed.TotalMilliseconds * _options.GasPerMillisecond);
                    long totalGas = _gasUsed + timeBasedGas;
                    
                    if (totalGas > _options.MaxGasLimit)
                    {
                        _logger.LogWarning("Time-based gas limit exceeded: {TotalGas} > {GasLimit}", 
                            totalGas, _options.MaxGasLimit);
                        
                        // Stop the timer
                        _monitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                }
                else if (_gasUsed > _options.MaxGasLimit)
                {
                    _logger.LogWarning("Gas limit exceeded: {GasUsed} > {GasLimit}", 
                        _gasUsed, _options.MaxGasLimit);
                    
                    // Stop the timer
                    _monitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        /// <summary>
        /// Disposes the monitor timer.
        /// </summary>
        public void Dispose()
        {
            _monitorTimer?.Dispose();
            _monitorTimer = null;
        }
    }

    /// <summary>
    /// Options for gas accounting.
    /// </summary>
    public class GasAccountingOptions
    {
        /// <summary>
        /// Gets or sets the maximum gas limit.
        /// </summary>
        /// <remarks>
        /// A value of 0 or less indicates no limit.
        /// </remarks>
        public long MaxGasLimit { get; set; } = 1_000_000;

        /// <summary>
        /// Gets or sets a value indicating whether to enable time-based gas accounting.
        /// </summary>
        public bool EnableTimeBasedGas { get; set; } = true;

        /// <summary>
        /// Gets or sets the amount of gas per millisecond for time-based accounting.
        /// </summary>
        public double GasPerMillisecond { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the gas cost for basic operations.
        /// </summary>
        public int BasicOperationGas { get; set; } = 1;

        /// <summary>
        /// Gets or sets the gas cost for memory operations per byte.
        /// </summary>
        public double MemoryOperationGasPerByte { get; set; } = 0.1;

        /// <summary>
        /// Gets or sets the gas cost for storage operations per byte.
        /// </summary>
        public double StorageOperationGasPerByte { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the gas cost for cryptographic operations.
        /// </summary>
        public int CryptographicOperationGas { get; set; } = 50;
    }

    /// <summary>
    /// Exception thrown when gas limit is exceeded.
    /// </summary>
    [Serializable]
    public class OutOfGasException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OutOfGasException"/> class.
        /// </summary>
        public OutOfGasException() : base("Out of gas") { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OutOfGasException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public OutOfGasException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OutOfGasException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public OutOfGasException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OutOfGasException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected OutOfGasException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        /// <summary>
        /// Gets or sets the amount of gas used.
        /// </summary>
        public long GasUsed { get; set; }

        /// <summary>
        /// Gets or sets the gas limit.
        /// </summary>
        public long GasLimit { get; set; }
    }
}
