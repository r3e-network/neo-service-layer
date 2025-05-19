using System;

namespace NeoServiceLayer.Tee.Enclave.Exceptions
{
    /// <summary>
    /// Represents an exception that occurs when there is an error executing JavaScript code in the enclave.
    /// </summary>
    [Serializable]
    public class JavaScriptExecutionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptExecutionException"/> class.
        /// </summary>
        public JavaScriptExecutionException() : base("An error occurred while executing JavaScript code in the enclave")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptExecutionException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public JavaScriptExecutionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptExecutionException"/> class with a specified error message 
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public JavaScriptExecutionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptExecutionException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected JavaScriptExecutionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
} 