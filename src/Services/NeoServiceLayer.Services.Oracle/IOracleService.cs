using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Oracle.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Oracle;

/// <summary>
/// Interface for the Oracle service.
/// </summary>
public interface IOracleService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Initializes the Oracle service.
    /// </summary>
    /// <returns>True if initialization was successful.</returns>
    Task<bool> InitializeAsync();

    /// <summary>
    /// Starts the Oracle service.
    /// </summary>
    /// <returns>True if the service started successfully.</returns>
    Task<bool> StartAsync();

    /// <summary>
    /// Stops the Oracle service.
    /// </summary>
    /// <returns>True if the service stopped successfully.</returns>
    Task<bool> StopAsync();

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

    // New methods for controller compatibility

    /// <summary>
    /// Subscribes to an Oracle data feed.
    /// </summary>
    /// <param name="request">The subscription request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The subscription result.</returns>
    Task<OracleSubscriptionResult> SubscribeAsync(OracleSubscriptionRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Unsubscribes from an Oracle data feed.
    /// </summary>
    /// <param name="request">The unsubscribe request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The subscription result.</returns>
    Task<OracleSubscriptionResult> UnsubscribeAsync(OracleUnsubscribeRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets data from an Oracle data source.
    /// </summary>
    /// <param name="request">The data request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The data result.</returns>
    Task<OracleDataResult> GetDataAsync(OracleDataRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Creates a new data source.
    /// </summary>
    /// <param name="request">The create data source request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The data source result.</returns>
    Task<DataSourceResult> CreateDataSourceAsync(CreateDataSourceRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Updates an existing data source.
    /// </summary>
    /// <param name="request">The update data source request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The data source result.</returns>
    Task<DataSourceResult> UpdateDataSourceAsync(UpdateDataSourceRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Deletes a data source.
    /// </summary>
    /// <param name="request">The delete data source request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The data source result.</returns>
    Task<DataSourceResult> DeleteDataSourceAsync(DeleteDataSourceRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets subscriptions with pagination.
    /// </summary>
    /// <param name="request">The list subscriptions request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The subscriptions result.</returns>
    Task<ListSubscriptionsResult> GetSubscriptionsAsync(ListSubscriptionsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets data sources with pagination.
    /// </summary>
    /// <param name="request">The list data sources request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The data sources result.</returns>
    Task<ListDataSourcesResult> GetDataSourcesAsync(ListDataSourcesRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Processes a batch of Oracle requests.
    /// </summary>
    /// <param name="request">The batch Oracle request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The batch Oracle result.</returns>
    Task<BatchOracleResult> BatchRequestAsync(BatchOracleRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets subscription status and metrics.
    /// </summary>
    /// <param name="request">The Oracle status request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The Oracle status result.</returns>
    Task<OracleStatusResult> GetSubscriptionStatusAsync(OracleStatusRequest request, BlockchainType blockchainType);
}
