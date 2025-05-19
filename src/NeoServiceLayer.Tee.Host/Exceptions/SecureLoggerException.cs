using System;

namespace NeoServiceLayer.Tee.Host.Exceptions
{
    /// <summary>
    /// Exception thrown when an error occurs during secure logging operations.
    /// </summary>
    public class SecureLoggerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecureLoggerException"/> class.
        /// </summary>
        public SecureLoggerException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureLoggerException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SecureLoggerException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureLoggerException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public SecureLoggerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
