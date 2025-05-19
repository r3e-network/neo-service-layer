using System;

namespace NeoServiceLayer.Tee.Host.Exceptions
{
    /// <summary>
    /// Base exception for RPC errors.
    /// </summary>
    public class RpcException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RpcException"/> class.
        /// </summary>
        public RpcException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RpcException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public RpcException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RpcException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public RpcException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when an RPC call times out.
    /// </summary>
    public class RpcTimeoutException : RpcException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RpcTimeoutException"/> class.
        /// </summary>
        public RpcTimeoutException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RpcTimeoutException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public RpcTimeoutException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RpcTimeoutException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public RpcTimeoutException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when an RPC method is not found.
    /// </summary>
    public class RpcMethodNotFoundException : RpcException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RpcMethodNotFoundException"/> class.
        /// </summary>
        public RpcMethodNotFoundException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RpcMethodNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public RpcMethodNotFoundException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RpcMethodNotFoundException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public RpcMethodNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when authentication fails for an RPC call.
    /// </summary>
    public class RpcAuthenticationException : RpcException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RpcAuthenticationException"/> class.
        /// </summary>
        public RpcAuthenticationException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RpcAuthenticationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public RpcAuthenticationException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RpcAuthenticationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public RpcAuthenticationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when authorization fails for an RPC call.
    /// </summary>
    public class RpcAuthorizationException : RpcException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RpcAuthorizationException"/> class.
        /// </summary>
        public RpcAuthorizationException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RpcAuthorizationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public RpcAuthorizationException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RpcAuthorizationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public RpcAuthorizationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when an RPC call is canceled.
    /// </summary>
    public class RpcCanceledException : RpcException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RpcCanceledException"/> class.
        /// </summary>
        public RpcCanceledException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RpcCanceledException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public RpcCanceledException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RpcCanceledException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public RpcCanceledException(string message, Exception innerException) : base(message, innerException) { }
    }
}
