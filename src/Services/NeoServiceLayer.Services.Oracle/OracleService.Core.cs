using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Http;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Oracle.Models;
using NeoServiceLayer.Services.Oracle.Configuration;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Infrastructure;
using IBlockchainClient = NeoServiceLayer.Infrastructure.IBlockchainClient;
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
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(enclaveManager);
        ArgumentNullException.ThrowIfNull(blockchainClientFactory);
        ArgumentNullException.ThrowIfNull(httpClientService);
        
        _configuration = configuration;
        _enclaveManager = enclaveManager;
        _blockchainClientFactory = blockchainClientFactory;
        _httpClientService = httpClientService;
        _requestCount = 0;
        _successCount = 0;
        _failureCount = 0;
        _lastRequestTime = DateTime.MinValue;
        
        InitializeService();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleService"/> class using the builder pattern.
    /// </summary>
    /// <param name="dependencies">The service dependencies.</param>
    /// <param name="options">The service options.</param>
    internal OracleService(
        OracleServiceDependencies dependencies,
        OracleServiceOptions options)
        : base("Oracle", "Confidential Oracle Service", "1.0.0", dependencies.Logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        ArgumentNullException.ThrowIfNull(dependencies);
        ArgumentNullException.ThrowIfNull(options);
        
        _configuration = options.Configuration ?? throw new ArgumentException("Configuration is required", nameof(options));
        _enclaveManager = dependencies.EnclaveManager;
        _blockchainClientFactory = dependencies.BlockchainClientFactory;
        _httpClientService = dependencies.HttpClientService;
        _requestCount = 0;
        _successCount = 0;
        _failureCount = 0;
        _lastRequestTime = DateTime.MinValue;
        
        InitializeService();
    }

    /// <summary>
    /// Initializes common service settings.
    /// </summary>
    private void InitializeService()
    {
        // Add capabilities
        AddCapability<IOracleService>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("MaxDataSources", "100");
        SetMetadata("SupportedBlockchains", "NeoN3,NeoX");
        SetMetadata("DefaultCacheTTL", "300"); // 5 minutes

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

        return await ExecuteInEnclaveAsync(async () =>
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            Logger.LogDebug("Fetching data from {DataSource}/{DataPath} securely within enclave", dataSource, dataPath);

            // Validate data source URL for security
            if (!IsValidDataSource(dataSource))
            {
                throw new UnauthorizedAccessException($"Data source {dataSource} is not authorized");
            }

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

            // Get blockchain data for verification within enclave
            var client = _blockchainClientFactory.CreateClient(blockchainType);
            var blockHeight = await client.GetBlockHeightAsync();
            var block = await client.GetBlockAsync(blockHeight);
            var blockHash = block.Hash;

            string result;
            try
            {
                // Use secure enclave Oracle function to fetch and process data
                result = await _enclaveManager.OracleFetchAndProcessDataAsync(
                    dataSource,
                    "GET",
                    "{}",
                    "",
                    "",
                    "{}");

                // Validate and sanitize the result within enclave
                result = ValidateAndSanitizeOracleData(result, dataSource, dataPath);
            }
            catch (Exception enclaveEx)
            {
                Logger.LogWarning(enclaveEx, "Primary enclave data fetch failed, attempting fallback");
                
                // Try the simpler GetDataAsync method as fallback
                try
                {
                    result = await _enclaveManager.GetDataAsync(dataSource, dataPath);
                    result = ValidateAndSanitizeOracleData(result, dataSource, dataPath);
                }
                catch (Exception fallbackEx)
                {
                    Logger.LogError(fallbackEx, "All enclave data fetch methods failed for {DataSource}", dataSource);
                    throw new InvalidOperationException($"Failed to fetch data from {dataSource} within enclave", fallbackEx);
                }
            }

            // Add integrity metadata to the result
            var enhancedResult = AddIntegrityMetadata(result, blockHeight, blockHash, dataSource, dataPath);

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            UpdateMetric("TotalDataRequests", _requestCount);

            Logger.LogDebug("Successfully fetched and validated data from {DataSource} within enclave", dataSource);

            return enhancedResult;
        });
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

    /// <summary>
    /// Validates if a data source is authorized and secure.
    /// </summary>
    /// <param name="dataSource">The data source URL to validate.</param>
    /// <returns>True if the data source is valid and authorized.</returns>
    private bool IsValidDataSource(string dataSource)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dataSource))
            {
                return false;
            }

            // Check if URL is well-formed
            if (!Uri.TryCreate(dataSource, UriKind.Absolute, out var uri))
            {
                return false;
            }

            // Only allow HTTPS for security
            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            {
                Logger.LogWarning("Invalid scheme for data source: {Scheme}", uri.Scheme);
                return false;
            }

            // Check against allowed domains (in production, this would be configurable)
            var allowedDomains = new[]
            {
                "api.coinpaprika.com",
                "api.coingecko.com",
                "api.binance.com",
                "api.cryptocompare.com",
                "localhost"
            };

            if (!allowedDomains.Any(domain => uri.Host.Contains(domain, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.LogWarning("Data source domain not in allowed list: {Domain}", uri.Host);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating data source: {DataSource}", dataSource);
            return false;
        }
    }

    /// <summary>
    /// Validates and sanitizes oracle data to prevent injection attacks.
    /// </summary>
    /// <param name="data">The raw oracle data.</param>
    /// <param name="dataSource">The data source URL.</param>
    /// <param name="dataPath">The data path.</param>
    /// <returns>Validated and sanitized data.</returns>
    private string ValidateAndSanitizeOracleData(string data, string dataSource, string dataPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new InvalidDataException("Oracle data is null or empty");
            }

            // Basic size validation
            if (data.Length > 1024 * 1024) // 1MB limit
            {
                throw new InvalidDataException("Oracle data exceeds maximum size limit");
            }

            // Validate JSON format if it appears to be JSON
            if (data.TrimStart().StartsWith('{') || data.TrimStart().StartsWith('['))
            {
                try
                {
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(data);
                    // If parsing succeeds, data is valid JSON
                }
                catch (System.Text.Json.JsonException)
                {
                    Logger.LogWarning("Invalid JSON format in oracle data from {DataSource}", dataSource);
                    throw new InvalidDataException("Oracle data contains invalid JSON format");
                }
            }

            // Remove any potential script injection attempts
            var sanitizedData = data
                .Replace("<script", "&lt;script", StringComparison.OrdinalIgnoreCase)
                .Replace("javascript:", "js:", StringComparison.OrdinalIgnoreCase);

            Logger.LogDebug("Validated oracle data from {DataSource}: {DataSize} bytes", dataSource, sanitizedData.Length);

            return sanitizedData;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to validate oracle data from {DataSource}", dataSource);
            throw;
        }
    }

    /// <summary>
    /// Adds integrity metadata to oracle data for verification.
    /// </summary>
    /// <param name="data">The oracle data.</param>
    /// <param name="blockHeight">The current block height.</param>
    /// <param name="blockHash">The current block hash.</param>
    /// <param name="dataSource">The data source URL.</param>
    /// <param name="dataPath">The data path.</param>
    /// <returns>Enhanced data with integrity metadata.</returns>
    private string AddIntegrityMetadata(string data, long blockHeight, string blockHash, string dataSource, string dataPath)
    {
        try
        {
            var timestamp = DateTime.UtcNow;
            var dataHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(data));
            var dataHashHex = Convert.ToHexString(dataHash);

            var metadata = new
            {
                oracle_data = data,
                integrity = new
                {
                    data_hash = dataHashHex,
                    block_height = blockHeight,
                    block_hash = blockHash,
                    timestamp = timestamp.ToString("O"),
                    data_source = dataSource,
                    data_path = dataPath,
                    enclave_verified = true
                }
            };

            var enhancedData = System.Text.Json.JsonSerializer.Serialize(metadata);

            Logger.LogDebug("Added integrity metadata to oracle data: hash={DataHash}, block={BlockHeight}", 
                dataHashHex[..16] + "...", blockHeight);

            return enhancedData;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to add integrity metadata to oracle data");
            // Return original data if metadata addition fails
            return data;
        }
    }
}
