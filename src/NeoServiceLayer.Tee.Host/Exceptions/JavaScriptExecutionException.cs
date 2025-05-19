using System;

namespace NeoServiceLayer.Tee.Host.Exceptions
{
    /// <summary>
    /// Exception thrown when an error occurs during JavaScript execution.
    /// </summary>
    public class JavaScriptExecutionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptExecutionException"/> class.
        /// </summary>
        public JavaScriptExecutionException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptExecutionException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public JavaScriptExecutionException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptExecutionException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public JavaScriptExecutionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
