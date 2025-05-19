using System;

namespace NeoServiceLayer.Tee.Host.Exceptions
{
    /// <summary>
    /// Base exception for Occlum operations.
    /// </summary>
    public class OcclumException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumException"/> class.
        /// </summary>
        public OcclumException() : base("An error occurred in the Occlum enclave")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public OcclumException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public OcclumException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when there is an initialization error in Occlum.
    /// </summary>
    public class OcclumInitializationException : OcclumException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumInitializationException"/> class.
        /// </summary>
        public OcclumInitializationException() : base("Failed to initialize Occlum enclave")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumInitializationException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public OcclumInitializationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumInitializationException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public OcclumInitializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when there is an execution error in Occlum.
    /// </summary>
    public class OcclumExecutionException : OcclumException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumExecutionException"/> class.
        /// </summary>
        public OcclumExecutionException() : base("Failed to execute command in Occlum enclave")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumExecutionException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public OcclumExecutionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumExecutionException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public OcclumExecutionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when there is a configuration error in Occlum.
    /// </summary>
    public class OcclumConfigurationException : OcclumException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumConfigurationException"/> class.
        /// </summary>
        public OcclumConfigurationException() : base("Failed to configure Occlum enclave")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumConfigurationException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public OcclumConfigurationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumConfigurationException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public OcclumConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when an error occurs during Occlum attestation.
    /// </summary>
    public class OcclumAttestationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumAttestationException"/> class.
        /// </summary>
        public OcclumAttestationException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumAttestationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public OcclumAttestationException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumAttestationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public OcclumAttestationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when an error occurs during Occlum sealing or unsealing.
    /// </summary>
    public class OcclumSealingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumSealingException"/> class.
        /// </summary>
        public OcclumSealingException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumSealingException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public OcclumSealingException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumSealingException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public OcclumSealingException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when an error occurs during Occlum JavaScript execution.
    /// </summary>
    public class OcclumJavaScriptException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumJavaScriptException"/> class.
        /// </summary>
        public OcclumJavaScriptException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumJavaScriptException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public OcclumJavaScriptException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumJavaScriptException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public OcclumJavaScriptException(string message, Exception innerException) : base(message, innerException) { }
    }
}
