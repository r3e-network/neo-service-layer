using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Tee.Host.Services;

/// <summary>
/// Oracle operations for the Enclave Manager.
/// </summary>
public partial class EnclaveManager
{
    /// <inheritdoc/>
    public Task<string> OracleFetchAndProcessDataAsync(
        string url,
        string processingScript,
        string outputFormat,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching and processing data from URL: {Url}", url);

            // Use the real enclave Oracle function
            string result = _enclaveWrapper.FetchOracleData(url, null, processingScript, outputFormat);

            _logger.LogDebug("Data fetched successfully from URL: {Url}", url);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching and processing data.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> OracleGetPriceDataAsync(string symbol, string source, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting price data for symbol: {Symbol} from source: {Source}", symbol, source);

            string jsonPayload = $@"{{
                ""symbol"": ""{symbol}"",
                ""source"": ""{source}""
            }}";

            return CallEnclaveFunctionAsync("oracleGetPriceData", jsonPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price data.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> OracleAggregateDataAsync(string[] sources, string aggregationMethod, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Aggregating data from {SourceCount} sources using method: {AggregationMethod}", sources.Length, aggregationMethod);

            string sourcesJson = string.Join(",", sources.Select(s => $"\"{s}\""));
            string jsonPayload = $@"{{
                ""sources"": [{sourcesJson}],
                ""aggregationMethod"": ""{aggregationMethod}""
            }}";

            return CallEnclaveFunctionAsync("oracleAggregateData", jsonPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating data.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> OracleValidateDataAsync(string data, string validationRules, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating data with rules: {ValidationRules}", validationRules);

            string jsonPayload = $@"{{
                ""data"": ""{data}"",
                ""validationRules"": ""{validationRules}""
            }}";

            return CallEnclaveFunctionAsync("oracleValidateData", jsonPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating data.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> StorageStoreDataAsync(string key, string data, string encryptionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Storing data with key: {Key}", key);

            string jsonPayload = $@"{{
                ""key"": ""{key}"",
                ""data"": ""{data}"",
                ""encryptionKey"": ""{encryptionKey}""
            }}";

            return CallEnclaveFunctionAsync("storageStoreData", jsonPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing data.");
            throw;
        }
    }

    // Note: StorageRetrieveDataAsync and StorageDeleteDataAsync are implemented in the main EnclaveManager.cs file

    /// <inheritdoc/>
    public Task<string> StorageListKeysAsync(string prefix, int skip, int take, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Listing keys with prefix: {Prefix}, skip: {Skip}, take: {Take}", prefix, skip, take);

            string jsonPayload = $@"{{
                ""prefix"": ""{prefix}"",
                ""skip"": {skip},
                ""take"": {take}
            }}";

            return CallEnclaveFunctionAsync("storageListKeys", jsonPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing keys.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> ComplianceCheckTransactionAsync(string transactionData, string rules, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking transaction compliance with rules: {Rules}", rules);

            string jsonPayload = $@"{{
                ""transactionData"": ""{transactionData}"",
                ""rules"": ""{rules}""
            }}";

            return CallEnclaveFunctionAsync("complianceCheckTransaction", jsonPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking transaction compliance.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> ComplianceGenerateReportAsync(string reportType, string parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating compliance report of type: {ReportType}", reportType);

            string jsonPayload = $@"{{
                ""reportType"": ""{reportType}"",
                ""parameters"": ""{parameters}""
            }}";

            return CallEnclaveFunctionAsync("complianceGenerateReport", jsonPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating compliance report.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> EventSubscribeAsync(string eventType, string filter, string callbackUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Subscribing to event type: {EventType} with filter: {Filter}", eventType, filter);

            string jsonPayload = $@"{{
                ""eventType"": ""{eventType}"",
                ""filter"": ""{filter}"",
                ""callbackUrl"": ""{callbackUrl}""
            }}";

            return CallEnclaveFunctionAsync("eventSubscribe", jsonPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to event.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> EventUnsubscribeAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Unsubscribing from subscription: {SubscriptionId}", subscriptionId);

            string jsonPayload = $@"{{
                ""subscriptionId"": ""{subscriptionId}""
            }}";

            string result = CallEnclaveFunctionAsync("eventUnsubscribe", jsonPayload, cancellationToken).Result;
            return Task.FromResult(bool.Parse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from event.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> EventGetSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting event subscriptions");

            string jsonPayload = "{}";

            return CallEnclaveFunctionAsync("eventGetSubscriptions", jsonPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event subscriptions.");
            throw;
        }
    }
}
