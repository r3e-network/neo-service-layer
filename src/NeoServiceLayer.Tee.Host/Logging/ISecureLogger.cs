using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Shared.Models.Logging;

namespace NeoServiceLayer.Tee.Host.Logging
{
    /// <summary>
    /// Interface for secure logging in enclaves.
    /// </summary>
    public interface ISecureLogger : IDisposable
    {
        /// <summary>
        /// Logs a message securely.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">The log message.</param>
        /// <param name="args">The arguments for the log message.</param>
        void Log(LogLevel level, string message, params object[] args);

        /// <summary>
        /// Logs a message securely with additional properties.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">The log message.</param>
        /// <param name="properties">The additional properties for the log entry.</param>
        /// <param name="args">The arguments for the log message.</param>
        void LogWithProperties(LogLevel level, string message, Dictionary<string, string> properties, params object[] args);

        /// <summary>
        /// Logs an exception securely.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">The log message.</param>
        /// <param name="args">The arguments for the log message.</param>
        void LogException(LogLevel level, Exception exception, string message, params object[] args);

        /// <summary>
        /// Checks if a log level is enabled.
        /// </summary>
        /// <param name="level">The log level to check.</param>
        /// <returns>True if the log level is enabled, false otherwise.</returns>
        bool IsEnabled(LogLevel level);

        /// <summary>
        /// Gets the log entries.
        /// </summary>
        /// <param name="count">The maximum number of log entries to get.</param>
        /// <param name="level">The minimum log level to include.</param>
        /// <param name="source">The source to filter by.</param>
        /// <returns>A list of log entries.</returns>
        Task<IReadOnlyList<SecureLogEntry>> GetLogEntriesAsync(int count = 100, LogLevel level = LogLevel.Information, string source = null);

        /// <summary>
        /// Clears the log entries.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task ClearLogEntriesAsync();
    }
}
