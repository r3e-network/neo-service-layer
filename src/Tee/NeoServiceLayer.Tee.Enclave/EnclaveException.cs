namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Exception thrown when an enclave operation fails.
/// </summary>
public class EnclaveException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public EnclaveException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public EnclaveException(string message, Exception innerException) : base(message, innerException)
    {
    }
} 