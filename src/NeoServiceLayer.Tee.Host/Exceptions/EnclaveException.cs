using System;
using System.Runtime.Serialization;

namespace NeoServiceLayer.Tee.Host.Exceptions
{
    /// <summary>
    /// Base exception for all enclave-related exceptions.
    /// </summary>
    [Serializable]
    public class EnclaveException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveException"/> class.
        /// </summary>
        public EnclaveException() : base("An error occurred in the enclave.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public EnclaveException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public EnclaveException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected EnclaveException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception thrown when an enclave operation fails.
    /// </summary>
    [Serializable]
    public class EnclaveOperationException : EnclaveException
    {
        /// <summary>
        /// Gets the error code returned by the enclave operation.
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveOperationException"/> class.
        /// </summary>
        public EnclaveOperationException() : base("An enclave operation failed.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveOperationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public EnclaveOperationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveOperationException"/> class with a specified error message
        /// and error code.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="errorCode">The error code returned by the enclave operation.</param>
        public EnclaveOperationException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveOperationException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public EnclaveOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveOperationException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected EnclaveOperationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ErrorCode = info.GetInt32(nameof(ErrorCode));
        }

        /// <summary>
        /// Sets the <see cref="SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ErrorCode), ErrorCode);
        }
    }

    /// <summary>
    /// Exception thrown when an enclave is not initialized.
    /// </summary>
    [Serializable]
    public class EnclaveNotInitializedException : EnclaveException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveNotInitializedException"/> class.
        /// </summary>
        public EnclaveNotInitializedException() : base("The enclave is not initialized.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveNotInitializedException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public EnclaveNotInitializedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveNotInitializedException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public EnclaveNotInitializedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveNotInitializedException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected EnclaveNotInitializedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception thrown when Occlum is not initialized.
    /// </summary>
    [Serializable]
    public class OcclumNotInitializedException : EnclaveException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumNotInitializedException"/> class.
        /// </summary>
        public OcclumNotInitializedException() : base("Occlum is not initialized.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumNotInitializedException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public OcclumNotInitializedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumNotInitializedException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public OcclumNotInitializedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumNotInitializedException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected OcclumNotInitializedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
