using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NeoServiceLayer.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when a resource is not found.
    /// </summary>
    [Serializable]
    public class NotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class.
        /// </summary>
        public NotFoundException() : base("The requested resource was not found.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public NotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public NotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected NotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a validation error occurs.
    /// </summary>
    [Serializable]
    public class ValidationException : Exception
    {
        /// <summary>
        /// Gets the validation errors.
        /// </summary>
        public IDictionary<string, string[]> Errors { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        public ValidationException() : base("One or more validation errors occurred.")
        {
            Errors = new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ValidationException(string message) : base(message)
        {
            Errors = new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public ValidationException(string message, Exception innerException) : base(message, innerException)
        {
            Errors = new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message and validation errors.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="errors">The validation errors.</param>
        public ValidationException(string message, IDictionary<string, string[]> errors) : base(message)
        {
            Errors = errors;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected ValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Errors = new Dictionary<string, string[]>();
        }
    }

    /// <summary>
    /// Exception thrown when access to a resource is forbidden.
    /// </summary>
    [Serializable]
    public class ForbiddenException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ForbiddenException"/> class.
        /// </summary>
        public ForbiddenException() : base("Access to the requested resource is forbidden.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForbiddenException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ForbiddenException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForbiddenException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public ForbiddenException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForbiddenException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected ForbiddenException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a conflict occurs.
    /// </summary>
    [Serializable]
    public class ConflictException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class.
        /// </summary>
        public ConflictException() : base("A conflict occurred while processing the request.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ConflictException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public ConflictException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected ConflictException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a rate limit is exceeded.
    /// </summary>
    [Serializable]
    public class RateLimitExceededException : Exception
    {
        /// <summary>
        /// Gets the retry after time in seconds.
        /// </summary>
        public int RetryAfterSeconds { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class.
        /// </summary>
        public RateLimitExceededException() : base("Rate limit exceeded. Please try again later.")
        {
            RetryAfterSeconds = 60;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public RateLimitExceededException(string message) : base(message)
        {
            RetryAfterSeconds = 60;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class with a specified error message and retry after time.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="retryAfterSeconds">The retry after time in seconds.</param>
        public RateLimitExceededException(string message, int retryAfterSeconds) : base(message)
        {
            RetryAfterSeconds = retryAfterSeconds;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public RateLimitExceededException(string message, Exception innerException) : base(message, innerException)
        {
            RetryAfterSeconds = 60;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected RateLimitExceededException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            RetryAfterSeconds = info.GetInt32(nameof(RetryAfterSeconds));
        }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(RetryAfterSeconds), RetryAfterSeconds);
        }
    }
}
