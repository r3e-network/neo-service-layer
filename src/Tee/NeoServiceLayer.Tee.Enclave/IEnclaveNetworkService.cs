/// <summary>
/// Interface for enclave network services.
/// </summary>
    /// <summary>
    /// Fetches data from a URL securely within the enclave.
    /// </summary>
    /// <param name="url">The URL to fetch data from.</param>
    /// <param name="headers">Optional HTTP headers.</param>
    /// <param name="processingScript">Optional JavaScript processing script.</param>
    /// <param name="outputFormat">The desired output format.</param>
    /// <returns>The fetched and processed data as JSON string.</returns>
    /// <summary>
    /// Validates a network endpoint for security.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns>True if the endpoint is considered safe.</returns>
    /// <summary>
    /// Gets network statistics from the enclave.
    /// </summary>
    /// <returns>Network statistics as JSON string.</returns>
