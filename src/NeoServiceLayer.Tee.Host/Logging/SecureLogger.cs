using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Shared.Models.Logging;
using NeoServiceLayer.Tee.Host.Exceptions;
using NeoServiceLayer.Tee.Host.Events;
using NeoServiceLayer.Tee.Shared.Interfaces;

namespace NeoServiceLayer.Tee.Host.Logging
{
    /// <summary>
    /// Provides secure logging for enclaves.
    /// </summary>
    public class SecureLogger : ISecureLogger, IDisposable
    {
        private readonly ILogger<SecureLogger> _logger;
        private readonly IOcclumInterface _occlumInterface;
        private readonly IEnclaveEventSystem _eventSystem;
        private readonly SecureLoggerOptions _options;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _logProcessingTask;
        private readonly BlockingCollection<SecureLogEntry> _logQueue;
        private readonly SemaphoreSlim _fileSemaphore;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureLogger"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="occlumInterface">The Occlum interface to use for secure operations.</param>
        /// <param name="eventSystem">The event system to use for publishing log events.</param>
        /// <param name="options">The options for the secure logger.</param>
        public SecureLogger(
            ILogger<SecureLogger> logger,
            IOcclumInterface occlumInterface,
            IEnclaveEventSystem eventSystem,
            SecureLoggerOptions options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _occlumInterface = occlumInterface ?? throw new ArgumentNullException(nameof(occlumInterface));
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _options = options ?? new SecureLoggerOptions();
            _cancellationTokenSource = new CancellationTokenSource();
            _logQueue = new BlockingCollection<SecureLogEntry>(_options.MaxQueueSize);
            _fileSemaphore = new SemaphoreSlim(1, 1);
            _disposed = false;

            // Create the log directory if it doesn't exist
            if (!string.IsNullOrEmpty(_options.LogDirectory) && !Directory.Exists(_options.LogDirectory))
            {
                Directory.CreateDirectory(_options.LogDirectory);
            }

            // Start the log processing task
            _logProcessingTask = Task.Run(() => ProcessLogsAsync(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// Logs a message securely.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">The log message.</param>
        /// <param name="args">The arguments for the log message.</param>
        public void Log(LogLevel level, string message, params object[] args)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("Message cannot be null or empty", nameof(message));
            }

            // Check if the log level is enabled
            if (!IsEnabled(level))
            {
                return;
            }

            try
            {
                // Format the message
                string formattedMessage = args != null && args.Length > 0
                    ? string.Format(message, args)
                    : message;

                // Create the log entry
                var logEntry = new SecureLogEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Level = level,
                    Message = formattedMessage,
                    Timestamp = DateTime.UtcNow,
                    EnclaveId = _occlumInterface.GetEnclaveId().ToString(),
                    Source = _options.Source
                };

                // Add the log entry to the queue
                if (!_logQueue.TryAdd(logEntry, _options.EnqueueTimeoutMs))
                {
                    _logger.LogWarning("Failed to add log entry to the queue: queue is full or timed out");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log message");
            }
        }

        /// <summary>
        /// Logs a message securely with additional properties.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">The log message.</param>
        /// <param name="properties">The additional properties for the log entry.</param>
        /// <param name="args">The arguments for the log message.</param>
        public void LogWithProperties(LogLevel level, string message, Dictionary<string, string> properties, params object[] args)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("Message cannot be null or empty", nameof(message));
            }

            // Check if the log level is enabled
            if (!IsEnabled(level))
            {
                return;
            }

            try
            {
                // Format the message
                string formattedMessage = args != null && args.Length > 0
                    ? string.Format(message, args)
                    : message;

                // Create the log entry
                var logEntry = new SecureLogEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Level = level,
                    Message = formattedMessage,
                    Timestamp = DateTime.UtcNow,
                    EnclaveId = _occlumInterface.GetEnclaveId().ToString(),
                    Source = _options.Source,
                    Properties = properties ?? new Dictionary<string, string>()
                };

                // Add the log entry to the queue
                if (!_logQueue.TryAdd(logEntry, _options.EnqueueTimeoutMs))
                {
                    _logger.LogWarning("Failed to add log entry to the queue: queue is full or timed out");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log message with properties");
            }
        }

        /// <summary>
        /// Logs an exception securely.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The log message.</param>
        /// <param name="args">The arguments for the log message.</param>
        public void LogException(LogLevel level, Exception exception, string message, params object[] args)
        {
            CheckDisposed();

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("Message cannot be null or empty", nameof(message));
            }

            // Check if the log level is enabled
            if (!IsEnabled(level))
            {
                return;
            }

            try
            {
                // Format the message
                string formattedMessage = args != null && args.Length > 0
                    ? string.Format(message, args)
                    : message;

                // Create the log entry
                var logEntry = new SecureLogEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Level = level,
                    Message = formattedMessage,
                    Timestamp = DateTime.UtcNow,
                    EnclaveId = _occlumInterface.GetEnclaveId().ToString(),
                    Source = _options.Source,
                    Exception = exception.ToString(),
                    Properties = new Dictionary<string, string>
                    {
                        { "ExceptionType", exception.GetType().FullName },
                        { "ExceptionMessage", exception.Message },
                        { "StackTrace", exception.StackTrace }
                    }
                };

                // Add inner exception details
                var innerException = exception.InnerException;
                int innerExceptionCount = 0;
                while (innerException != null)
                {
                    logEntry.Properties.Add($"InnerExceptionType{innerExceptionCount}", innerException.GetType().FullName);
                    logEntry.Properties.Add($"InnerExceptionMessage{innerExceptionCount}", innerException.Message);
                    innerException = innerException.InnerException;
                    innerExceptionCount++;
                }

                // Add the log entry to the queue
                if (!_logQueue.TryAdd(logEntry, _options.EnqueueTimeoutMs))
                {
                    _logger.LogWarning("Failed to add log entry to the queue: queue is full or timed out");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log exception");
            }
        }

        /// <summary>
        /// Checks if a log level is enabled.
        /// </summary>
        /// <param name="level">The log level to check.</param>
        /// <returns>True if the log level is enabled, false otherwise.</returns>
        public bool IsEnabled(LogLevel level)
        {
            return level >= _options.MinimumLevel;
        }

        /// <summary>
        /// Gets the log entries.
        /// </summary>
        /// <param name="count">The maximum number of log entries to get.</param>
        /// <param name="level">The minimum log level to include.</param>
        /// <param name="source">The source to filter by.</param>
        /// <returns>A list of log entries.</returns>
        public async Task<IReadOnlyList<SecureLogEntry>> GetLogEntriesAsync(int count = 100, LogLevel level = LogLevel.Information, string source = null)
        {
            CheckDisposed();

            if (count <= 0)
            {
                throw new ArgumentException("Count must be greater than zero", nameof(count));
            }

            try
            {
                // Get the log entries from the file
                var logEntries = await ReadLogEntriesFromFileAsync();

                // Filter the log entries
                var filteredLogEntries = logEntries
                    .Where(e => e.Level >= level)
                    .Where(e => string.IsNullOrEmpty(source) || e.Source == source)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(count)
                    .ToList();

                return filteredLogEntries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get log entries");
                throw new SecureLoggerException("Failed to get log entries", ex);
            }
        }

        /// <summary>
        /// Clears the log entries.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ClearLogEntriesAsync()
        {
            CheckDisposed();

            try
            {
                // Clear the log file
                await _fileSemaphore.WaitAsync();
                try
                {
                    string logFilePath = GetLogFilePath();
                    if (File.Exists(logFilePath))
                    {
                        File.Delete(logFilePath);
                    }
                }
                finally
                {
                    _fileSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear log entries");
                throw new SecureLoggerException("Failed to clear log entries", ex);
            }
        }

        /// <summary>
        /// Disposes the secure logger.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the secure logger.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Stop the log processing task
                    _cancellationTokenSource.Cancel();
                    try
                    {
                        _logProcessingTask.Wait();
                    }
                    catch (AggregateException)
                    {
                        // Ignore task cancellation exceptions
                    }

                    // Dispose resources
                    _cancellationTokenSource.Dispose();
                    _logQueue.Dispose();
                    _fileSemaphore.Dispose();
                }

                _disposed = true;
            }
        }

        private async Task ProcessLogsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Log processing task started");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    SecureLogEntry logEntry = null;

                    try
                    {
                        // Get the next log entry from the queue
                        logEntry = _logQueue.Take(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Task was canceled, exit the loop
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error taking log entry from the queue");
                        continue;
                    }

                    if (logEntry == null)
                    {
                        continue;
                    }

                    try
                    {
                        // Process the log entry
                        await ProcessLogEntryAsync(logEntry);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing log entry");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in log processing task");
            }

            _logger.LogInformation("Log processing task stopped");
        }

        private async Task ProcessLogEntryAsync(SecureLogEntry logEntry)
        {
            try
            {
                // Write the log entry to the file
                if (_options.EnableFileLogging)
                {
                    await WriteLogEntryToFileAsync(logEntry);
                }

                // Publish the log entry as an event
                if (_options.EnableEventLogging)
                {
                    await _eventSystem.PublishAsync("LogEntry", logEntry);
                }

                // Forward the log entry to the standard logger
                if (_options.EnableStandardLogging)
                {
                    LogToStandardLogger(logEntry);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing log entry");
            }
        }

        private async Task WriteLogEntryToFileAsync(SecureLogEntry logEntry)
        {
            if (string.IsNullOrEmpty(_options.LogDirectory))
            {
                return;
            }

            await _fileSemaphore.WaitAsync();
            try
            {
                string logFilePath = GetLogFilePath();
                string logEntryJson = JsonSerializer.Serialize(logEntry);

                // Seal the log entry if enabled
                if (_options.EnableSealing)
                {
                    byte[] logEntryBytes = Encoding.UTF8.GetBytes(logEntryJson);
                    byte[] sealedLogEntryBytes = _occlumInterface.SealData(logEntryBytes);
                    logEntryJson = Convert.ToBase64String(sealedLogEntryBytes);
                }

                // Append the log entry to the file
                await File.AppendAllTextAsync(logFilePath, logEntryJson + Environment.NewLine);
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        private async Task<List<SecureLogEntry>> ReadLogEntriesFromFileAsync()
        {
            if (string.IsNullOrEmpty(_options.LogDirectory))
            {
                return new List<SecureLogEntry>();
            }

            await _fileSemaphore.WaitAsync();
            try
            {
                string logFilePath = GetLogFilePath();
                if (!File.Exists(logFilePath))
                {
                    return new List<SecureLogEntry>();
                }

                var logEntries = new List<SecureLogEntry>();
                string[] logEntryLines = await File.ReadAllLinesAsync(logFilePath);

                foreach (string logEntryLine in logEntryLines)
                {
                    if (string.IsNullOrWhiteSpace(logEntryLine))
                    {
                        continue;
                    }

                    try
                    {
                        string logEntryJson = logEntryLine;

                        // Unseal the log entry if enabled
                        if (_options.EnableSealing)
                        {
                            byte[] sealedLogEntryBytes = Convert.FromBase64String(logEntryLine);
                            byte[] logEntryBytes = _occlumInterface.UnsealData(sealedLogEntryBytes);
                            logEntryJson = Encoding.UTF8.GetString(logEntryBytes);
                        }

                        var logEntry = JsonSerializer.Deserialize<SecureLogEntry>(logEntryJson);
                        if (logEntry != null)
                        {
                            logEntries.Add(logEntry);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deserializing log entry");
                    }
                }

                return logEntries;
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        private void LogToStandardLogger(SecureLogEntry logEntry)
        {
            if (logEntry.Exception != null)
            {
                _logger.Log(logEntry.Level, logEntry.Exception, logEntry.Message);
            }
            else
            {
                _logger.Log(logEntry.Level, logEntry.Message);
            }
        }

        private string GetLogFilePath()
        {
            string fileName = $"enclave_{_occlumInterface.GetEnclaveId()}.log";
            return Path.Combine(_options.LogDirectory, fileName);
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SecureLogger));
            }
        }
    }
}
