using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Interface for enclave network service operations.
/// </summary>
public interface IEnclaveNetworkService
{
    /// <summary>
    /// Fetches data from a URL within the enclave environment.
    /// </summary>
    /// <param name="url">The URL to fetch data from.</param>
    /// <param name="headers">Optional HTTP headers.</param>
    /// <param name="processingScript">Optional JavaScript processing script.</param>
    /// <param name="outputFormat">Output format (default: json).</param>
    /// <returns>The fetched and processed data.</returns>
    Task<string> FetchDataAsync(string url, string? headers = null, string? processingScript = null, string? outputFormat = "json");

    /// <summary>
    /// Validates that an endpoint is secure and accessible.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns>True if the endpoint is valid and secure.</returns>
    Task<bool> ValidateEndpointAsync(string url);

    /// <summary>
    /// Gets network statistics from the enclave.
    /// </summary>
    /// <returns>Network statistics in JSON format.</returns>
    Task<string> GetNetworkStatsAsync();
}