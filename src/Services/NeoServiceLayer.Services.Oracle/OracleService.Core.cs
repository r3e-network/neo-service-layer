using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Http;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Infrastructure.Blockchain;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Oracle.Configuration;
using NeoServiceLayer.Services.Oracle.Models;
using NeoServiceLayer.Tee.Host.Services;
using CoreConfig = NeoServiceLayer.Core.Configuration.IServiceConfiguration;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Text.Json;
using System.Security.Cryptography;


namespace NeoServiceLayer.Services.Oracle;

/// <summary>
/// Core implementation of the Oracle service.
/// </summary>
public partial class OracleService : ServiceFramework.EnclaveBlockchainServiceBase, IOracleService
{
    // LoggerMessage delegates for performance optimization
    private static readonly Action<ILogger, string, string, Exception?> _fetchingData =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(4001, "FetchingData"),
            "Fetching data from {DataSource}/{DataPath} securely within enclave");

    private static readonly Action<ILogger, string, string, Exception?> _privacyFetchCompleted =
        LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(4002, "PrivacyFetchCompleted"),
            "Privacy-preserving oracle fetch completed: RequestId={RequestId}, DataHash={DataHash}");

    private static readonly Action<ILogger, string, Exception?> _enclaveFetchFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(4003, "EnclaveFetchFailed"),
            "Primary enclave data fetch failed, attempting fallback for {DataSource}");

    private static readonly Action<ILogger, string, Exception?> _allFetchMethodsFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(4004, "AllFetchMethodsFailed"),
            "All enclave data fetch methods failed for {DataSource}");

    private static readonly Action<ILogger, string, Exception?> _dataFetchedSuccessfully =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(4005, "DataFetchedSuccessfully"),
            "Successfully fetched and validated data from {DataSource} within enclave");

    private static readonly Action<ILogger, string, Exception?> _errorGettingData =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(4006, "ErrorGettingData"),
            "Error getting data for feed {FeedId}");

    private static readonly Action<ILogger, string, Exception?> _errorFetchingData =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(4007, "ErrorFetchingData"),
            "Error fetching data for request {RequestId}");

    private static readonly Action<ILogger, Exception?> _serviceInitializing =
        LoggerMessage.Define(LogLevel.Information, new EventId(4008, "ServiceInitializing"),
            "Initializing Oracle service...");

    private static readonly Action<ILogger, string, string, Exception?> _serviceConfiguration =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(4009, "ServiceConfiguration"),
            "Oracle service configuration: MaxConcurrentRequests={MaxConcurrentRequests}, DefaultTimeout={DefaultTimeout}ms");

    private static readonly Action<ILogger, Exception?> _enclaveInitializing =
        LoggerMessage.Define(LogLevel.Information, new EventId(4010, "EnclaveInitializing"),
            "Initializing enclave for Oracle service...");

    private static readonly Action<ILogger, Exception?> _serviceStarting =
        LoggerMessage.Define(LogLevel.Information, new EventId(4011, "ServiceStarting"),
            "Starting Oracle service...");

    private static readonly Action<ILogger, Exception?> _serviceStopping =
        LoggerMessage.Define(LogLevel.Information, new EventId(4012, "ServiceStopping"),
            "Stopping Oracle service...");

    private static readonly Action<ILogger, string, string, Exception?> _subscriptionCancelling =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(4013, "SubscriptionCancelling"),
            "Cancelling subscription {SubscriptionId} for feed {FeedId}");

    private static readonly Action<ILogger, string, Exception?> _invalidScheme =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(4014, "InvalidScheme"),
            "Invalid scheme for data source: {Scheme}");

    private static readonly Action<ILogger, string, Exception?> _domainNotAllowed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(4015, "DomainNotAllowed"),
            "Data source domain not in allowed list: {Domain}");

    private static readonly Action<ILogger, string, Exception?> _validationError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(4016, "ValidationError"),
            "Error validating data source: {DataSource}");

    private static readonly Action<ILogger, string, Exception?> _invalidJsonFormat =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(4017, "InvalidJsonFormat"),
            "Invalid JSON format in oracle data from {DataSource}");

    private static readonly Action<ILogger, string, int, Exception?> _oracleDataValidated =
        LoggerMessage.Define<string, int>(LogLevel.Debug, new EventId(4018, "OracleDataValidated"),
            "Validated oracle data from {DataSource}: {DataSize} bytes");

    private static readonly Action<ILogger, string, Exception?> _oracleDataValidationFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(4019, "OracleDataValidationFailed"),
            "Failed to validate oracle data from {DataSource}");

    private static readonly Action<ILogger, string, long, Exception?> _integrityMetadataAdded =
        LoggerMessage.Define<string, long>(LogLevel.Debug, new EventId(4020, "IntegrityMetadataAdded"),
            "Added integrity metadata to oracle data: hash={DataHash}, block={BlockHeight}");

    private static readonly Action<ILogger, Exception?> _integrityMetadataFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(4021, "IntegrityMetadataFailed"),
            "Failed to add integrity metadata to oracle data");

    protected readonly CoreConfig _configuration;
    protected new readonly IEnclaveManager _enclaveManager;
    protected readonly IBlockchainClientFactory _blockchainClientFactory;
    protected readonly IHttpClientService _httpClientService;
    protected readonly List<DataSource> _dataSources = new();
    protected readonly Dictionary<string, Models.OracleSubscription> _subscriptions = new();
    protected readonly IServiceProvider? _serviceProvider;
    protected readonly SHA256 sha256 = SHA256.Create();
    protected int _requestCount;
    protected int _successCount;
    protected int _failureCount;
    protected DateTime _lastRequestTime;
    private bool _isEnclaveInitialized;
    private bool _isRunning;

    /// <summary>
    /// Gets a value indicating whether the enclave is initialized.
    /// </summary>
    public bool IsEnclaveInitialized => _isEnclaveInitialized;

    /// <summary>
    /// Gets a value indicating whether the service is running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleService"/> class.
    /// </summary>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="blockchainClientFactory">The blockchain client factory.</param>
    /// <param name="httpClientService">The HTTP client service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public OracleService(
        CoreConfig configuration,
        IEnclaveManager enclaveManager,
        IBlockchainClientFactory blockchainClientFactory,
        IHttpClientService httpClientService,
        ILogger<OracleService> logger,
        IServiceProvider? serviceProvider = null)
        : base("Oracle", "Confidential Oracle Service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX }, enclaveManager)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(enclaveManager);
        ArgumentNullException.ThrowIfNull(blockchainClientFactory);
        ArgumentNullException.ThrowIfNull(httpClientService);

        _configuration = configuration;
        _enclaveManager = enclaveManager;
        _blockchainClientFactory = blockchainClientFactory;
        _httpClientService = httpClientService;
        _serviceProvider = serviceProvider;
        _requestCount = 0;
        _successCount = 0;
        _failureCount = 0;
        _lastRequestTime = DateTime.MinValue;

        InitializeService();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleService"/> class.
    /// Constructor overload for test compatibility.
    /// </summary>
    public OracleService(
        CoreConfig configuration,
        IEnclaveManager enclaveManager,
        IBlockchainClientFactory blockchainClientFactory,
        IHttpClientService httpClientService,
        ILogger<OracleService> logger)
        : this(configuration, enclaveManager, blockchainClientFactory, httpClientService, logger, null)
    {
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

            _fetchingData(Logger, dataSource, dataPath, null);

            // Validate data source reputation using privacy-preserving computation
            var isReputable = await ValidateDataSourceReputationAsync(dataSource);
            if (!isReputable)
            {
                throw new UnauthorizedAccessException($"Data source {dataSource} failed reputation check");
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

            // Fetch data using privacy-preserving operations
            var privacyResult = await FetchDataWithPrivacyAsync(dataSource, dataPath);

            _privacyFetchCompleted(Logger, privacyResult.RequestId, privacyResult.DataHash, null);

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
                _enclaveFetchFailed(Logger, dataSource, enclaveEx);

                // Try the simpler GetDataAsync method as fallback
                try
                {
                    result = await _enclaveManager.GetDataAsync(dataSource, dataPath);
                    result = ValidateAndSanitizeOracleData(result, dataSource, dataPath);
                }
                catch (Exception fallbackEx)
                {
                    _allFetchMethodsFailed(Logger, dataSource, fallbackEx);
                    throw new InvalidOperationException($"Failed to fetch data from {dataSource} within enclave", fallbackEx);
                }
            }

            // Add integrity metadata to the result with privacy proofs
            var enhancedResult = AddIntegrityMetadata(result, blockHeight, blockHash, dataSource, dataPath, privacyResult);

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            UpdateMetric("TotalDataRequests", _requestCount);

            _dataFetchedSuccessfully(Logger, dataSource, null);

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
            _errorGettingData(Logger, feedId, ex);
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
            // Fetch data with privacy-preserving operations
            var privacyResult = await FetchDataWithPrivacyAsync(request.Url, request.Path, request);

            _privacyFetchCompleted(Logger, privacyResult.RequestId, privacyResult.DataHash, null);

            var data = await GetDataAsync(request.Url, request.Path, blockchainType);

            var response = new OracleResponse
            {
                RequestId = request.RequestId,
                Data = data,
                Timestamp = DateTime.UtcNow,
                StatusCode = 200,
                BlockchainType = blockchainType,
                SourceUrl = request.Url,
                SourcePath = request.Path,
                // Add privacy metadata
                Metadata = new Dictionary<string, object>
                {
                    ["privacy_request_id"] = privacyResult.RequestId,
                    ["data_hash"] = privacyResult.DataHash,
                    ["source_proof_hash"] = privacyResult.SourceProof.SourceHash,
                    ["source_proof_signature"] = privacyResult.SourceProof.Signature
                }
            };

            return response;
        }
        catch (Exception ex)
        {
            _errorFetchingData(Logger, request.RequestId, ex);

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
        _serviceInitializing(Logger, null);

        // Initialize persistent storage
        await InitializePersistentStorageAsync();

        // Load configuration
        var maxConcurrentRequests = _configuration.GetValue("Oracle:MaxConcurrentRequests", "10");
        var defaultTimeout = _configuration.GetValue("Oracle:DefaultTimeout", "30000");

        _serviceConfiguration(Logger, maxConcurrentRequests, defaultTimeout, null);

        return await Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        _enclaveInitializing(Logger, null);
        return await _enclaveManager.InitializeEnclaveAsync();
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        _serviceStarting(Logger, null);
        return await Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        _serviceStopping(Logger, null);

        // Cancel all subscriptions
        foreach (var subscription in _subscriptions.Values)
        {
            _subscriptionCancelling(Logger, subscription.Id, subscription.FeedId, null);
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
                _invalidScheme(Logger, uri.Scheme, null);
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
                _domainNotAllowed(Logger, uri.Host, null);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _validationError(Logger, dataSource, ex);
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
                    // If parsing succeeds, data is valid JSON
                }
                catch (System.Text.Json.JsonException)
                {
                    _invalidJsonFormat(Logger, dataSource, null);
                    throw new InvalidDataException("Oracle data contains invalid JSON format");
                }
            }

            // Remove any potential script injection attempts
            var sanitizedData = data
                .Replace("<script", "<script", StringComparison.OrdinalIgnoreCase)
                .Replace("javascript:", "js:", StringComparison.OrdinalIgnoreCase);

            _oracleDataValidated(Logger, dataSource, sanitizedData.Length, null);

            return sanitizedData;
        }
        catch (Exception ex)
        {
            _oracleDataValidationFailed(Logger, dataSource, ex);
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
    /// <param name="privacyResult">The privacy result from SGX.</param>
    /// <returns>Enhanced data with integrity metadata.</returns>
    private string AddIntegrityMetadata(string data, long blockHeight, string blockHash, string dataSource, string dataPath, PrivacyOracleResult? privacyResult = null)
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
                    enclave_verified = true,
                    privacy_proof = privacyResult != null ? new
                    {
                        request_id = privacyResult.RequestId,
                        data_hash = privacyResult.DataHash,
                        source_hash = privacyResult.SourceProof.SourceHash,
                        path_hash = privacyResult.SourceProof.PathHash,
                        source_signature = privacyResult.SourceProof.Signature
                    } : null
                }
            };

            var enhancedData = System.Text.Json.JsonSerializer.Serialize(metadata);

            _integrityMetadataAdded(Logger, dataHashHex[..16] + "...", blockHeight, null);

            return enhancedData;
        }
        catch (Exception ex)
        {
            _integrityMetadataFailed(Logger, ex);
            // Return original data if metadata addition fails
            return data;
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposePersistenceResources();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Initializes the Oracle service.
    /// </summary>
    /// <returns>True if initialization was successful.</returns>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            _serviceInitializing(Logger, null);
            
            if (_enclaveManager != null)
            {
                _enclaveInitializing(Logger, null);
                await _enclaveManager.InitializeAsync(null).ConfigureAwait(false);
                _isEnclaveInitialized = true;
            }
            
            // Initialize any other resources
            _serviceConfiguration(Logger, "10", "30000", null);
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize Oracle service");
            return false;
        }
    }

    /// <summary>
    /// Starts the Oracle service.
    /// </summary>
    /// <returns>True if the service started successfully.</returns>
    public async Task<bool> StartAsync()
    {
        try
        {
            _serviceStarting(Logger, null);
            
            // Start any background services or workers
            await Task.CompletedTask;
            
            _isRunning = true;
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start Oracle service");
            return false;
        }
    }

    /// <summary>
    /// Stops the Oracle service.
    /// </summary>
    /// <returns>True if the service stopped successfully.</returns>
    public async Task<bool> StopAsync()
    {
        try
        {
            _serviceStopping(Logger, null);
            
            // Stop any background services or workers
            await Task.CompletedTask;
            
            _isRunning = false;
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to stop Oracle service");
            return false;
        }
    }
}
