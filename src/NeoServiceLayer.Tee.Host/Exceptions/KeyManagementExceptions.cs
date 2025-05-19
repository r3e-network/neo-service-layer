using System;

namespace NeoServiceLayer.Tee.Host.Exceptions
{
    /// <summary>
    /// Exception thrown when an error occurs during key sealing.
    /// </summary>
    public class SealingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SealingException"/> class.
        /// </summary>
        public SealingException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SealingException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SealingException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SealingException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public SealingException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when an error occurs during key unsealing.
    /// </summary>
    public class UnsealingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnsealingException"/> class.
        /// </summary>
        public UnsealingException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsealingException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UnsealingException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsealingException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public UnsealingException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when an error occurs during data signing.
    /// </summary>
    public class SigningException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SigningException"/> class.
        /// </summary>
        public SigningException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SigningException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SigningException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SigningException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public SigningException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when an error occurs during signature verification.
    /// </summary>
    public class VerificationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VerificationException"/> class.
        /// </summary>
        public VerificationException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="VerificationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public VerificationException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="VerificationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public VerificationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when an error occurs during enclave creation.
    /// </summary>
    public class EnclaveCreationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveCreationException"/> class.
        /// </summary>
        public EnclaveCreationException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveCreationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public EnclaveCreationException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveCreationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public EnclaveCreationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when an error occurs during enclave termination.
    /// </summary>
    public class EnclaveTerminationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveTerminationException"/> class.
        /// </summary>
        public EnclaveTerminationException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveTerminationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public EnclaveTerminationException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveTerminationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public EnclaveTerminationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
