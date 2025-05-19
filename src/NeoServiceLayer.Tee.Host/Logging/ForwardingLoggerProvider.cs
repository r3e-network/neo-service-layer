using System;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host.Logging
{
    /// <summary>
    /// A logger provider that forwards log messages to another logger.
    /// </summary>
    public class ForwardingLoggerProvider : ILoggerProvider
    {
        private readonly ILogger _targetLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForwardingLoggerProvider"/> class.
        /// </summary>
        /// <param name="targetLogger">The target logger to forward messages to.</param>
        public ForwardingLoggerProvider(ILogger targetLogger)
        {
            _targetLogger = targetLogger ?? throw new ArgumentNullException(nameof(targetLogger));
        }

        /// <summary>
        /// Creates a new logger with the specified category name.
        /// </summary>
        /// <param name="categoryName">The category name for the logger.</param>
        /// <returns>A logger that forwards messages to the target logger.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            return new ForwardingLogger(_targetLogger, categoryName);
        }

        /// <summary>
        /// Disposes the logger provider.
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose
        }

        /// <summary>
        /// A logger that forwards log messages to another logger.
        /// </summary>
        private class ForwardingLogger : ILogger
        {
            private readonly ILogger _targetLogger;
            private readonly string _categoryName;

            /// <summary>
            /// Initializes a new instance of the <see cref="ForwardingLogger"/> class.
            /// </summary>
            /// <param name="targetLogger">The target logger to forward messages to.</param>
            /// <param name="categoryName">The category name for the logger.</param>
            public ForwardingLogger(ILogger targetLogger, string categoryName)
            {
                _targetLogger = targetLogger;
                _categoryName = categoryName;
            }

            /// <summary>
            /// Begins a logical operation scope.
            /// </summary>
            /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
            /// <param name="state">The identifier for the scope.</param>
            /// <returns>A disposable object that ends the logical operation scope on dispose.</returns>
            public IDisposable BeginScope<TState>(TState state)
            {
                return _targetLogger.BeginScope(state);
            }

            /// <summary>
            /// Checks if the given log level is enabled.
            /// </summary>
            /// <param name="logLevel">The log level to check.</param>
            /// <returns>True if the log level is enabled, false otherwise.</returns>
            public bool IsEnabled(LogLevel logLevel)
            {
                return _targetLogger.IsEnabled(logLevel);
            }

            /// <summary>
            /// Writes a log entry.
            /// </summary>
            /// <typeparam name="TState">The type of the object to be written.</typeparam>
            /// <param name="logLevel">The log level.</param>
            /// <param name="eventId">The event ID.</param>
            /// <param name="state">The entry to be written.</param>
            /// <param name="exception">The exception related to this entry.</param>
            /// <param name="formatter">The function to create a string message of the state and exception.</param>
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                // Forward the log message to the target logger, but prefix it with the category name
                _targetLogger.Log(logLevel, eventId, state, exception, (s, e) =>
                {
                    string message = formatter(s, e);
                    return $"[{_categoryName}] {message}";
                });
            }
        }
    }
}
