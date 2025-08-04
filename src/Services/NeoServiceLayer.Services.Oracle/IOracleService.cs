using NeoServiceLayer.Core;
using NeoServiceLayer.RPC.Server.Attributes;
using NeoServiceLayer.Services.Oracle.Models;

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
    [JsonRpcMethod("oracle.fetchdata", Description = "Fetches data from an external source")]
    Task<OracleResponse> FetchDataAsync(OracleRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Fetches data from multiple external sources in a single batch.
    /// </summary>
    /// <param name="requests">The oracle requests.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The oracle responses.</returns>
    [JsonRpcMethod("oracle.fetchdatabatch", Description = "Fetches data from multiple external sources in a batch")]
    Task<IEnumerable<OracleResponse>> FetchDataBatchAsync(IEnumerable<OracleRequest> requests, BlockchainType blockchainType);

    /// <summary>
    /// Verifies the authenticity of fetched data.
    /// </summary>
    /// <param name="response">The oracle response.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the data is valid, false otherwise.</returns>
    [JsonRpcMethod("oracle.verifydata", Description = "Verifies the authenticity of fetched data")]
    Task<bool> VerifyDataAsync(OracleResponse response, BlockchainType blockchainType);

    /// <summary>
    /// Gets the list of supported data sources.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of supported data sources.</returns>
    [JsonRpcMethod("oracle.getsupporteddatasources", Description = "Gets the list of supported data sources")]
    Task<IEnumerable<string>> GetSupportedDataSourcesAsync(BlockchainType blockchainType);

    // New methods for controller compatibility

    /// <summary>
    /// Subscribes to an Oracle data feed.
    /// </summary>
    /// <param name="request">The subscription request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The subscription result.</returns>
    [JsonRpcMethod("oracle.subscribe", Description = "Subscribes to an Oracle data feed")]
    Task<OracleSubscriptionResult> SubscribeAsync(OracleSubscriptionRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Unsubscribes from an Oracle data feed.
    /// </summary>
    /// <param name="request">The unsubscribe request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The subscription result.</returns>
    [JsonRpcMethod("oracle.unsubscribe", Description = "Unsubscribes from an Oracle data feed")]
    Task<OracleSubscriptionResult> UnsubscribeAsync(OracleUnsubscribeRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets data from an Oracle data source.
    /// </summary>
    /// <param name="request">The data request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The data result.</returns>
    [JsonRpcMethod("oracle.getdata", Description = "Gets data from an Oracle data source")]
    Task<OracleDataResult> GetDataAsync(OracleDataRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Creates a new data source.
    /// </summary>
    /// <param name="request">The create data source request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The data source result.</returns>
    [JsonRpcMethod("oracle.createdatasource", Description = "Creates a new data source")]
    Task<DataSourceResult> CreateDataSourceAsync(CreateDataSourceRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Updates an existing data source.
    /// </summary>
    /// <param name="request">The update data source request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The data source result.</returns>
    [JsonRpcMethod("oracle.updatedatasource", Description = "Updates an existing data source")]
    Task<DataSourceResult> UpdateDataSourceAsync(UpdateDataSourceRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Deletes a data source.
    /// </summary>
    /// <param name="request">The delete data source request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The data source result.</returns>
    [JsonRpcMethod("oracle.deletedatasource", Description = "Deletes a data source")]
    Task<DataSourceResult> DeleteDataSourceAsync(DeleteDataSourceRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets subscriptions with pagination.
    /// </summary>
    /// <param name="request">The list subscriptions request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The subscriptions result.</returns>
    [JsonRpcMethod("oracle.getsubscriptions", Description = "Gets subscriptions with pagination")]
    Task<ListSubscriptionsResult> GetSubscriptionsAsync(ListSubscriptionsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets data sources with pagination.
    /// </summary>
    /// <param name="request">The list data sources request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The data sources result.</returns>
    [JsonRpcMethod("oracle.getdatasources", Description = "Gets data sources with pagination")]
    Task<ListDataSourcesResult> GetDataSourcesAsync(ListDataSourcesRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Processes a batch of Oracle requests.
    /// </summary>
    /// <param name="request">The batch Oracle request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The batch Oracle result.</returns>
    [JsonRpcMethod("oracle.batchrequest", Description = "Processes a batch of Oracle requests")]
    Task<BatchOracleResult> BatchRequestAsync(BatchOracleRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets subscription status and metrics.
    /// </summary>
    /// <param name="request">The Oracle status request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The Oracle status result.</returns>
    [JsonRpcMethod("oracle.getsubscriptionstatus", Description = "Gets subscription status and metrics")]
    Task<OracleStatusResult> GetSubscriptionStatusAsync(OracleStatusRequest request, BlockchainType blockchainType);
}
