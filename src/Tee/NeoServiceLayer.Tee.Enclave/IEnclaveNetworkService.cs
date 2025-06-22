namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Interface for enclave network services.
/// </summary>
public interface IEnclaveNetworkService
{
    /// <summary>
    /// Fetches data from a URL securely within the enclave.
    /// </summary>
    /// <param name="url">The URL to fetch data from.</param>
    /// <param name="headers">Optional HTTP headers.</param>
    /// <param name="processingScript">Optional JavaScript processing script.</param>
    /// <param name="outputFormat">The desired output format.</param>
    /// <returns>The fetched and processed data as JSON string.</returns>
    Task<string> FetchDataAsync(string url, string? headers = null, string? processingScript = null, string? outputFormat = "json");

    /// <summary>
    /// Validates a network endpoint for security.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns>True if the endpoint is considered safe.</returns>
    Task<bool> ValidateEndpointAsync(string url);

    /// <summary>
    /// Gets network statistics from the enclave.
    /// </summary>
    /// <returns>Network statistics as JSON string.</returns>
    Task<string> GetNetworkStatsAsync();
}
