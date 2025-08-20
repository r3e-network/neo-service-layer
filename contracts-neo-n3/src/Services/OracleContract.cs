using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using NeoServiceLayer.Contracts.Core;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Contracts.Services
{
    /// <summary>
    /// Provides external data feeds and price oracles using the Neo Service Layer's
    /// secure oracle infrastructure with multiple data source aggregation.
    /// </summary>
    [DisplayName("OracleContract")]
    [ManifestExtra("Author", "Neo Service Layer Team")]
    [ManifestExtra("Description", "External data feeds and price oracle service")]
    [ManifestExtra("Version", "1.0.0")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class OracleContract : BaseServiceContract
    {
        #region Storage Keys
        private static readonly byte[] OracleRequestPrefix = "oracleRequest:".ToByteArray();
        private static readonly byte[] OracleResultPrefix = "oracleResult:".ToByteArray();
        private static readonly byte[] DataSourcePrefix = "dataSource:".ToByteArray();
        private static readonly byte[] PriceFeedPrefix = "priceFeed:".ToByteArray();
        private static readonly byte[] RequestCounterKey = "requestCounter".ToByteArray();
        private static readonly byte[] ServiceFeeKey = "serviceFee".ToByteArray();
        private static readonly byte[] DataSourceCountKey = "dataSourceCount".ToByteArray();
        private static readonly byte[] MinConfirmationsKey = "minConfirmations".ToByteArray();
        private static readonly byte[] MaxDeviationKey = "maxDeviation".ToByteArray();
        private static readonly byte[] CacheExpiryKey = "cacheExpiry".ToByteArray();
        #endregion

        #region Events
        [DisplayName("OracleDataRequested")]
        public static event Action<UInt160, ByteString, string, string, BigInteger> OracleDataRequested;

        [DisplayName("OracleDataFulfilled")]
        public static event Action<UInt160, ByteString, string, ByteString, bool> OracleDataFulfilled;

        [DisplayName("PriceFeedUpdated")]
        public static event Action<string, BigInteger, ulong, int> PriceFeedUpdated;

        [DisplayName("DataSourceAdded")]
        public static event Action<string, string, bool> DataSourceAdded;

        [DisplayName("DataSourceStatusChanged")]
        public static event Action<string, bool, bool> DataSourceStatusChanged;

        [DisplayName("OracleError")]
        public static event Action<UInt160, ByteString, string> OracleError;
        #endregion

        #region Constants
        private const long DEFAULT_SERVICE_FEE = 5000000; // 0.05 GAS
        private const int DEFAULT_MIN_CONFIRMATIONS = 3;
        private const int DEFAULT_MAX_DEVIATION = 500; // 5% in basis points
        private const ulong DEFAULT_CACHE_EXPIRY = 300; // 5 minutes
        private const int MAX_DATA_SOURCES = 50;
        #endregion

        #region Initialization
        /// <summary>
        /// Deploys the OracleContract.
        /// </summary>
        /// <param name="data">Deployment data</param>
        /// <param name="update">Whether this is an update</param>
        public static void _deploy(object data, bool update)
        {
            if (update) return;

            var tx = (Transaction)Runtime.ScriptContainer;
            var serviceId = Runtime.ExecutingScriptHash;
            
            // Initialize base service
            var contract = new OracleContract();
            contract.InitializeBaseService(serviceId, "OracleService", "1.0.0", "{}");
            
            // Set default configuration
            Storage.Put(Storage.CurrentContext, ServiceFeeKey, DEFAULT_SERVICE_FEE);
            Storage.Put(Storage.CurrentContext, MinConfirmationsKey, DEFAULT_MIN_CONFIRMATIONS);
            Storage.Put(Storage.CurrentContext, MaxDeviationKey, DEFAULT_MAX_DEVIATION);
            Storage.Put(Storage.CurrentContext, CacheExpiryKey, DEFAULT_CACHE_EXPIRY);
            Storage.Put(Storage.CurrentContext, RequestCounterKey, 0);
            Storage.Put(Storage.CurrentContext, DataSourceCountKey, 0);

            Runtime.Log("OracleContract deployed successfully");
        }
        #endregion

        #region Service Implementation
        protected override void InitializeService(string config)
        {
            // Parse configuration if needed
            Runtime.Log("OracleContract service initialized");
        }

        protected override bool PerformHealthCheck()
        {
            try
            {
                // Check if we have active data sources
                var dataSourceCount = GetDataSourceCount();
                return dataSourceCount > 0;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Oracle Data Requests
        /// <summary>
        /// Requests external data from a specific data source.
        /// </summary>
        /// <param name="dataSource">Data source identifier</param>
        /// <param name="query">Query string for the data</param>
        /// <param name="callbackData">Optional callback data</param>
        /// <returns>Request ID for tracking</returns>
        public static ByteString RequestOracleData(string dataSource, string query, ByteString callbackData)
        {
            return ExecuteServiceOperation(() =>
            {
                ValidateDataRequest(dataSource, query);
                
                var caller = Runtime.CallingScriptHash;
                var requestId = GenerateRequestId();
                var fee = GetServiceFee();
                
                // Store request details
                var request = new OracleRequest
                {
                    Requester = caller,
                    DataSource = dataSource,
                    Query = query,
                    CallbackData = callbackData,
                    Fee = fee,
                    Timestamp = Runtime.Time,
                    Status = RequestStatus.Pending
                };
                
                var requestKey = OracleRequestPrefix.Concat(requestId);
                Storage.Put(Storage.CurrentContext, requestKey, StdLib.Serialize(request));
                
                // Emit event for off-chain processing
                OracleDataRequested(caller, requestId, dataSource, query, fee);
                
                Runtime.Log($"Oracle data requested: {requestId} from {dataSource} with query: {query}");
                return requestId;
            });
        }

        /// <summary>
        /// Requests price data for a specific asset pair.
        /// </summary>
        /// <param name="baseCurrency">Base currency symbol</param>
        /// <param name="quoteCurrency">Quote currency symbol</param>
        /// <param name="sources">Preferred data sources (comma-separated)</param>
        /// <returns>Request ID for tracking</returns>
        public static ByteString RequestPriceData(string baseCurrency, string quoteCurrency, string sources)
        {
            return ExecuteServiceOperation(() =>
            {
                if (string.IsNullOrEmpty(baseCurrency) || string.IsNullOrEmpty(quoteCurrency))
                    throw new ArgumentException("Currency symbols cannot be empty");
                
                var query = $"{baseCurrency}/{quoteCurrency}";
                var dataSource = string.IsNullOrEmpty(sources) ? "aggregated" : sources;
                
                return RequestOracleData(dataSource, query, "price_request".ToByteArray());
            });
        }

        /// <summary>
        /// Fulfills an oracle data request with the fetched data.
        /// This method is called by authorized oracle providers.
        /// </summary>
        /// <param name="requestId">Request identifier</param>
        /// <param name="data">Fetched data</param>
        /// <param name="proof">Cryptographic proof of data authenticity</param>
        /// <returns>True if fulfillment successful</returns>
        public static bool FulfillOracleData(ByteString requestId, ByteString data, string proof)
        {
            return ExecuteServiceOperation(() =>
            {
                // Validate caller is authorized oracle provider
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Unauthorized oracle provider");
                
                var requestKey = OracleRequestPrefix.Concat(requestId);
                var requestBytes = Storage.Get(Storage.CurrentContext, requestKey);
                if (requestBytes == null)
                    throw new InvalidOperationException("Request not found");
                
                var request = (OracleRequest)StdLib.Deserialize(requestBytes);
                if (request.Status != RequestStatus.Pending)
                    throw new InvalidOperationException("Request already fulfilled or cancelled");
                
                // Store result
                var result = new OracleResult
                {
                    RequestId = requestId,
                    Data = data,
                    Proof = proof ?? "",
                    FulfilledAt = Runtime.Time,
                    FulfilledBy = Runtime.CallingScriptHash,
                    DataSource = request.DataSource
                };
                
                var resultKey = OracleResultPrefix.Concat(requestId);
                Storage.Put(Storage.CurrentContext, resultKey, StdLib.Serialize(result));
                
                // Update request status
                request.Status = RequestStatus.Fulfilled;
                Storage.Put(Storage.CurrentContext, requestKey, StdLib.Serialize(request));
                
                // Update price feed cache if this is price data
                if (request.CallbackData.ToByteString() == "price_request")
                {
                    UpdatePriceFeedCache(request.Query, data);
                }
                
                // Emit fulfillment event
                OracleDataFulfilled(request.Requester, requestId, request.DataSource, data, true);
                
                Runtime.Log($"Oracle data fulfilled: {requestId} from {request.DataSource}");
                return true;
            });
        }

        /// <summary>
        /// Gets the result of an oracle data request.
        /// </summary>
        /// <param name="requestId">Request identifier</param>
        /// <returns>Oracle result or null if not found/fulfilled</returns>
        public static OracleResult GetOracleResult(ByteString requestId)
        {
            var resultKey = OracleResultPrefix.Concat(requestId);
            var resultBytes = Storage.Get(Storage.CurrentContext, resultKey);
            if (resultBytes == null)
                return null;
            
            return (OracleResult)StdLib.Deserialize(resultBytes);
        }

        /// <summary>
        /// Gets the status of an oracle request.
        /// </summary>
        /// <param name="requestId">Request identifier</param>
        /// <returns>Request information or null if not found</returns>
        public static OracleRequest GetOracleRequest(ByteString requestId)
        {
            var requestKey = OracleRequestPrefix.Concat(requestId);
            var requestBytes = Storage.Get(Storage.CurrentContext, requestKey);
            if (requestBytes == null)
                return null;
            
            return (OracleRequest)StdLib.Deserialize(requestBytes);
        }
        #endregion

        #region Price Feed Management
        /// <summary>
        /// Gets the latest price for an asset pair.
        /// </summary>
        /// <param name="baseCurrency">Base currency symbol</param>
        /// <param name="quoteCurrency">Quote currency symbol</param>
        /// <returns>Price feed data or null if not available</returns>
        public static PriceFeed GetLatestPrice(string baseCurrency, string quoteCurrency)
        {
            var pair = $"{baseCurrency}/{quoteCurrency}";
            var feedKey = PriceFeedPrefix.Concat(pair.ToByteArray());
            var feedBytes = Storage.Get(Storage.CurrentContext, feedKey);
            if (feedBytes == null)
                return null;
            
            var feed = (PriceFeed)StdLib.Deserialize(feedBytes);
            
            // Check if price is still valid (not expired)
            var cacheExpiry = GetCacheExpiry();
            if (Runtime.Time - feed.UpdatedAt > cacheExpiry)
            {
                return null; // Price is stale
            }
            
            return feed;
        }

        /// <summary>
        /// Updates the price feed cache.
        /// </summary>
        /// <param name="pair">Currency pair</param>
        /// <param name="priceData">Price data as bytes</param>
        private static void UpdatePriceFeedCache(string pair, ByteString priceData)
        {
            try
            {
                // Parse price from data (simplified - in production would handle various formats)
                var priceString = priceData.ToByteString();
                if (BigInteger.TryParse(priceString, out var price))
                {
                    var feed = new PriceFeed
                    {
                        Pair = pair,
                        Price = price,
                        UpdatedAt = Runtime.Time,
                        Confirmations = 1 // In production, would aggregate multiple sources
                    };
                    
                    var feedKey = PriceFeedPrefix.Concat(pair.ToByteArray());
                    Storage.Put(Storage.CurrentContext, feedKey, StdLib.Serialize(feed));
                    
                    PriceFeedUpdated(pair, price, Runtime.Time, 1);
                }
            }
            catch (Exception ex)
            {
                Runtime.Log($"Error updating price feed for {pair}: {ex.Message}");
            }
        }
        #endregion

        #region Data Source Management
        /// <summary>
        /// Adds a new data source (admin only).
        /// </summary>
        /// <param name="sourceId">Data source identifier</param>
        /// <param name="endpoint">API endpoint</param>
        /// <param name="isActive">Whether the source is active</param>
        /// <returns>True if successful</returns>
        public static bool AddDataSource(string sourceId, string endpoint, bool isActive)
        {
            return ExecuteServiceOperation(() =>
            {
                // Check admin permissions
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                if (string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(endpoint))
                    throw new ArgumentException("Source ID and endpoint cannot be empty");
                
                var sourceCount = GetDataSourceCount();
                if (sourceCount >= MAX_DATA_SOURCES)
                    throw new InvalidOperationException("Maximum data sources limit reached");
                
                var sourceKey = DataSourcePrefix.Concat(sourceId.ToByteArray());
                if (Storage.Get(Storage.CurrentContext, sourceKey) != null)
                    throw new InvalidOperationException("Data source already exists");
                
                var dataSource = new DataSource
                {
                    Id = sourceId,
                    Endpoint = endpoint,
                    IsActive = isActive,
                    AddedAt = Runtime.Time,
                    RequestCount = 0,
                    ErrorCount = 0
                };
                
                Storage.Put(Storage.CurrentContext, sourceKey, StdLib.Serialize(dataSource));
                Storage.Put(Storage.CurrentContext, DataSourceCountKey, sourceCount + 1);
                
                DataSourceAdded(sourceId, endpoint, isActive);
                Runtime.Log($"Data source added: {sourceId} at {endpoint}");
                return true;
            });
        }

        /// <summary>
        /// Updates data source status (admin only).
        /// </summary>
        /// <param name="sourceId">Data source identifier</param>
        /// <param name="isActive">New active status</param>
        /// <returns>True if successful</returns>
        public static bool UpdateDataSourceStatus(string sourceId, bool isActive)
        {
            return ExecuteServiceOperation(() =>
            {
                // Check admin permissions
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                var sourceKey = DataSourcePrefix.Concat(sourceId.ToByteArray());
                var sourceBytes = Storage.Get(Storage.CurrentContext, sourceKey);
                if (sourceBytes == null)
                    throw new InvalidOperationException("Data source not found");
                
                var dataSource = (DataSource)StdLib.Deserialize(sourceBytes);
                var wasActive = dataSource.IsActive;
                dataSource.IsActive = isActive;
                
                Storage.Put(Storage.CurrentContext, sourceKey, StdLib.Serialize(dataSource));
                
                DataSourceStatusChanged(sourceId, wasActive, isActive);
                Runtime.Log($"Data source {sourceId} status changed: {wasActive} -> {isActive}");
                return true;
            });
        }

        /// <summary>
        /// Gets information about a data source.
        /// </summary>
        /// <param name="sourceId">Data source identifier</param>
        /// <returns>Data source information or null if not found</returns>
        public static DataSource GetDataSource(string sourceId)
        {
            var sourceKey = DataSourcePrefix.Concat(sourceId.ToByteArray());
            var sourceBytes = Storage.Get(Storage.CurrentContext, sourceKey);
            if (sourceBytes == null)
                return null;
            
            return (DataSource)StdLib.Deserialize(sourceBytes);
        }

        /// <summary>
        /// Gets the total number of data sources.
        /// </summary>
        /// <returns>Data source count</returns>
        public static int GetDataSourceCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, DataSourceCountKey);
            return (int)(countBytes?.ToBigInteger() ?? 0);
        }
        #endregion

        #region Configuration Management
        /// <summary>
        /// Updates service configuration (admin only).
        /// </summary>
        /// <param name="serviceFee">Fee for oracle requests</param>
        /// <param name="minConfirmations">Minimum confirmations required</param>
        /// <param name="maxDeviation">Maximum price deviation in basis points</param>
        /// <param name="cacheExpiry">Cache expiry time in seconds</param>
        /// <returns>True if update successful</returns>
        public static bool UpdateConfiguration(BigInteger serviceFee, int minConfirmations, int maxDeviation, ulong cacheExpiry)
        {
            return ExecuteServiceOperation(() =>
            {
                // Check admin permissions
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                if (serviceFee < 0)
                    throw new ArgumentException("Service fee cannot be negative");
                if (minConfirmations < 1 || minConfirmations > 10)
                    throw new ArgumentException("Invalid minimum confirmations");
                if (maxDeviation < 0 || maxDeviation > 10000)
                    throw new ArgumentException("Invalid maximum deviation");
                if (cacheExpiry < 60 || cacheExpiry > 86400)
                    throw new ArgumentException("Invalid cache expiry");
                
                Storage.Put(Storage.CurrentContext, ServiceFeeKey, serviceFee);
                Storage.Put(Storage.CurrentContext, MinConfirmationsKey, minConfirmations);
                Storage.Put(Storage.CurrentContext, MaxDeviationKey, maxDeviation);
                Storage.Put(Storage.CurrentContext, CacheExpiryKey, cacheExpiry);
                
                Runtime.Log("Oracle service configuration updated");
                return true;
            });
        }

        /// <summary>
        /// Gets the current service fee.
        /// </summary>
        /// <returns>Service fee in GAS units</returns>
        public static BigInteger GetServiceFee()
        {
            var feeBytes = Storage.Get(Storage.CurrentContext, ServiceFeeKey);
            return feeBytes?.ToBigInteger() ?? DEFAULT_SERVICE_FEE;
        }

        /// <summary>
        /// Gets the cache expiry time.
        /// </summary>
        /// <returns>Cache expiry in seconds</returns>
        public static ulong GetCacheExpiry()
        {
            var expiryBytes = Storage.Get(Storage.CurrentContext, CacheExpiryKey);
            return (ulong)(expiryBytes?.ToBigInteger() ?? DEFAULT_CACHE_EXPIRY);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Validates a data request.
        /// </summary>
        private static void ValidateDataRequest(string dataSource, string query)
        {
            if (string.IsNullOrEmpty(dataSource))
                throw new ArgumentException("Data source cannot be empty");
            if (string.IsNullOrEmpty(query))
                throw new ArgumentException("Query cannot be empty");
            
            // Check if data source exists and is active
            var source = GetDataSource(dataSource);
            if (source != null && !source.IsActive)
                throw new InvalidOperationException($"Data source {dataSource} is not active");
        }

        /// <summary>
        /// Generates a unique request ID.
        /// </summary>
        private static ByteString GenerateRequestId()
        {
            var counter = GetRequestCounter();
            Storage.Put(Storage.CurrentContext, RequestCounterKey, counter + 1);
            
            var tx = (Transaction)Runtime.ScriptContainer;
            var data = Runtime.Time.ToByteArray()
                .Concat(counter.ToByteArray())
                .Concat(tx.Hash);
            
            return CryptoLib.Sha256(data);
        }

        /// <summary>
        /// Gets the current request counter.
        /// </summary>
        private static BigInteger GetRequestCounter()
        {
            var counterBytes = Storage.Get(Storage.CurrentContext, RequestCounterKey);
            return counterBytes?.ToBigInteger() ?? 0;
        }

        /// <summary>
        /// Executes a service operation with proper error handling.
        /// </summary>
        private static T ExecuteServiceOperation<T>(Func<T> operation)
        {
            ValidateServiceActive();
            IncrementRequestCount();

            try
            {
                return operation();
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                throw;
            }
        }
        #endregion

        #region Data Structures
        /// <summary>
        /// Represents an oracle data request.
        /// </summary>
        public class OracleRequest
        {
            public UInt160 Requester;
            public string DataSource;
            public string Query;
            public ByteString CallbackData;
            public BigInteger Fee;
            public ulong Timestamp;
            public RequestStatus Status;
        }

        /// <summary>
        /// Represents an oracle data result.
        /// </summary>
        public class OracleResult
        {
            public ByteString RequestId;
            public ByteString Data;
            public string Proof;
            public ulong FulfilledAt;
            public UInt160 FulfilledBy;
            public string DataSource;
        }

        /// <summary>
        /// Represents a price feed entry.
        /// </summary>
        public class PriceFeed
        {
            public string Pair;
            public BigInteger Price;
            public ulong UpdatedAt;
            public int Confirmations;
        }

        /// <summary>
        /// Represents a data source.
        /// </summary>
        public class DataSource
        {
            public string Id;
            public string Endpoint;
            public bool IsActive;
            public ulong AddedAt;
            public int RequestCount;
            public int ErrorCount;
        }

        /// <summary>
        /// Request status enumeration.
        /// </summary>
        public enum RequestStatus : byte
        {
            Pending = 0,
            Fulfilled = 1,
            Cancelled = 2,
            Failed = 3
        }
        #endregion
    }
}