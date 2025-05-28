using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Oracle;

/// <summary>
/// Interface for the Oracle service.
/// </summary>
public interface IOracleService : IEnclaveService, IBlockchainService, IDataFeedService
{
    /// <summary>
    /// Fetches data from an external source.
    /// </summary>
    /// <param name="request">The oracle request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The oracle response.</returns>
    Task<OracleResponse> FetchDataAsync(OracleRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Fetches data from multiple external sources in a single batch.
    /// </summary>
    /// <param name="requests">The oracle requests.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The oracle responses.</returns>
    Task<IEnumerable<OracleResponse>> FetchDataBatchAsync(IEnumerable<OracleRequest> requests, BlockchainType blockchainType);

    /// <summary>
    /// Verifies the authenticity of fetched data.
    /// </summary>
    /// <param name="response">The oracle response.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the data is valid, false otherwise.</returns>
    Task<bool> VerifyDataAsync(OracleResponse response, BlockchainType blockchainType);

    /// <summary>
    /// Gets the list of supported data sources.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of supported data sources.</returns>
    Task<IEnumerable<string>> GetSupportedDataSourcesAsync(BlockchainType blockchainType);
}
