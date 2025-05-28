using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Http;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Infrastructure;

// Use Infrastructure namespace for IBlockchainClientFactory
using IBlockchainClientFactory = NeoServiceLayer.Infrastructure.IBlockchainClientFactory;

namespace NeoServiceLayer.Services.Oracle;

/// <summary>
/// Core implementation of the Oracle service.
/// </summary>
public partial class OracleService : EnclaveBlockchainServiceBase, IOracleService
{
    protected readonly IServiceConfiguration _configuration;
    protected new readonly IEnclaveManager _enclaveManager;
    protected readonly IBlockchainClientFactory _blockchainClientFactory;
    protected readonly IHttpClientService _httpClientService;
    protected readonly List<DataSource> _dataSources = new();
    protected readonly Dictionary<string, OracleSubscription> _subscriptions = new();
    protected int _requestCount;
    protected int _successCount;
    protected int _failureCount;
    protected DateTime _lastRequestTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleService"/> class.
    /// </summary>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="blockchainClientFactory">The blockchain client factory.</param>
    /// <param name="httpClientService">The HTTP client service.</param>
    /// <param name="logger">The logger.</param>
    public OracleService(
        IServiceConfiguration configuration,
        IEnclaveManager enclaveManager,
        IBlockchainClientFactory blockchainClientFactory,
        IHttpClientService httpClientService,
        ILogger<OracleService> logger)
        : base("Oracle", "Confidential Oracle Service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        _configuration = configuration;
        _enclaveManager = enclaveManager;
        _blockchainClientFactory = blockchainClientFactory;
        _httpClientService = httpClientService;
        _requestCount = 0;
        _successCount = 0;
        _failureCount = 0;
        _lastRequestTime = DateTime.MinValue;

        // Add capabilities
        AddCapability<IOracleService>();
        AddCapability<IDataFeedService>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("MaxConcurrentRequests", _configuration.GetValue("Oracle:MaxConcurrentRequests", "10"));
        SetMetadata("DefaultTimeout", _configuration.GetValue("Oracle:DefaultTimeout", "30000"));
        SetMetadata("SupportedDataSources", "http,https,ipfs,blockchain");

        // Add dependencies
        AddRequiredDependency<IEnclaveService>("EnclaveManager", "1.0.0");
    }

    /// <inheritdoc/>
    public async Task<string> GetDataAsync(string dataSource, string dataPath, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Update data source access statistics
            lock (_dataSources)
            {
                var existingDataSource = _dataSources.FirstOrDefault(ds => ds.Url == dataSource && ds.BlockchainType == blockchainType);
                if (existingDataSource != null)
                {
                    existingDataSource.LastAccessedAt = DateTime.UtcNow;
                    existingDataSource.AccessCount++;
                }
            }

            // Get blockchain data for verification
            var client = _blockchainClientFactory.CreateClient(blockchainType);
            var blockHeight = await client.GetBlockHeightAsync();
            var blockHash = await client.GetBlockHashAsync(blockHeight);

            string result;
            if (IsEnclaveInitialized)
            {
                // Use the enclave Oracle function to get data securely
                result = await _enclaveManager.OracleFetchAndProcessDataAsync(
                    dataSource,
                    "GET",
                    "{}",
                    "",
                    "",
                    "{}");
            }
            else
            {
                // Fallback to mock data
                result = $"{{\"value\": 42, \"source\": \"{dataSource}\", \"path\": \"{dataPath}\", \"timestamp\": \"{DateTime.UtcNow}\"}}";
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error getting data from {DataSource}/{DataPath} for blockchain {BlockchainType}",
                dataSource, dataPath, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetDataAsync(string feedId, IDictionary<string, string> parameters)
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Extract parameters
            var dataSource = parameters.TryGetValue("dataSource", out var ds) ? ds : feedId;
            var dataPath = parameters.TryGetValue("dataPath", out var dp) ? dp : string.Empty;
            var blockchainType = parameters.TryGetValue("blockchain", out var bt) && Enum.TryParse<BlockchainType>(bt, true, out var bType)
                ? bType
                : BlockchainType.NeoN3;

            // Call the other method to get the data
            var result = await GetDataAsync(dataSource, dataPath, blockchainType);
            _successCount++;

            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error getting data for feed {FeedId}", feedId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<OracleResponse> FetchDataAsync(OracleRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        try
        {
            var data = await GetDataAsync(request.Url, request.Path, blockchainType);

            var response = new OracleResponse
            {
                RequestId = request.RequestId,
                Data = data,
                Timestamp = DateTime.UtcNow,
                StatusCode = 200,
                BlockchainType = blockchainType,
                SourceUrl = request.Url,
                SourcePath = request.Path
            };

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching data for request {RequestId}", request.RequestId);

            return new OracleResponse
            {
                RequestId = request.RequestId,
                StatusCode = 500,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow,
                BlockchainType = blockchainType,
                SourceUrl = request.Url,
                SourcePath = request.Path
            };
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing Oracle service...");

        // Load configuration
        var maxConcurrentRequests = _configuration.GetValue("Oracle:MaxConcurrentRequests", "10");
        var defaultTimeout = _configuration.GetValue("Oracle:DefaultTimeout", "30000");

        Logger.LogInformation("Oracle service configuration: MaxConcurrentRequests={MaxConcurrentRequests}, DefaultTimeout={DefaultTimeout}ms",
            maxConcurrentRequests, defaultTimeout);

        return await Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        Logger.LogInformation("Initializing enclave for Oracle service...");
        return await _enclaveManager.InitializeEnclaveAsync();
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting Oracle service...");
        return await Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping Oracle service...");

        // Cancel all subscriptions
        foreach (var subscription in _subscriptions.Values)
        {
            Logger.LogInformation("Cancelling subscription {SubscriptionId} for feed {FeedId}",
                subscription.Id, subscription.FeedId);
        }

        _subscriptions.Clear();
        UpdateMetric("SubscriptionCount", 0);

        return await Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override async Task<ServiceHealth> OnGetHealthAsync()
    {
        if (!IsEnclaveInitialized)
        {
            return ServiceHealth.Degraded;
        }

        // Check if there have been too many failures
        if (_requestCount > 0 && (double)_failureCount / _requestCount > 0.5)
        {
            return ServiceHealth.Degraded;
        }

        return await Task.FromResult(ServiceHealth.Healthy);
    }

    /// <inheritdoc/>
    protected override Task OnUpdateMetricsAsync()
    {
        UpdateMetric("RequestCount", _requestCount);
        UpdateMetric("SuccessCount", _successCount);
        UpdateMetric("FailureCount", _failureCount);
        UpdateMetric("SuccessRate", _requestCount > 0 ? (double)_successCount / _requestCount : 0);
        UpdateMetric("LastRequestTime", _lastRequestTime);
        UpdateMetric("DataSourceCount", _dataSources.Count);
        UpdateMetric("SubscriptionCount", _subscriptions.Count);
        UpdateMetric("ActiveSubscriptions", _subscriptions.Values
            .Where(s => (DateTime.UtcNow - s.LastUpdated) < TimeSpan.FromMinutes(10))
            .Count());

        return Task.CompletedTask;
    }
}
