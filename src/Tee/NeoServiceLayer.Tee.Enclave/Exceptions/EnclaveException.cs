using System;
using System.Runtime.Serialization;

namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Exception thrown when enclave operations fail.
/// </summary>
[Serializable]
public class EnclaveException : Exception
{
    /// <summary>
    /// Gets the error code associated with the enclave failure.
    /// </summary>
    public int ErrorCode { get; }

    /// <summary>
    /// Gets the enclave operation that failed.
    /// </summary>
    public string? Operation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveException"/> class.
    /// </summary>
    public EnclaveException()
        : base("An enclave operation failed.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public EnclaveException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public EnclaveException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveException"/> class with error details.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code from the enclave.</param>
    /// <param name="operation">The operation that failed.</param>
    public EnclaveException(string message, int errorCode, string? operation = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveException"/> class with error details and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code from the enclave.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public EnclaveException(string message, int errorCode, string? operation, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
    /// <param name="context">The StreamingContext that contains contextual information.</param>
    protected EnclaveException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        ErrorCode = info.GetInt32(nameof(ErrorCode));
        Operation = info.GetString(nameof(Operation));
    }

    /// <summary>
    /// Sets the SerializationInfo with information about the exception.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
    /// <param name="context">The StreamingContext that contains contextual information.</param>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ErrorCode), ErrorCode);
        info.AddValue(nameof(Operation), Operation);
    }
}