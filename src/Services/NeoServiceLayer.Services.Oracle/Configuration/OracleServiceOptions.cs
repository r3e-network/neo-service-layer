using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Http;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using IBlockchainClientFactory = NeoServiceLayer.Infrastructure.IBlockchainClientFactory;

namespace NeoServiceLayer.Services.Oracle.Configuration;

/// <summary>
/// Configuration options for the Oracle service.
/// </summary>
public class OracleServiceOptions
{
    /// <summary>
    /// Gets or sets the service configuration.
    /// </summary>
    public IServiceConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of data sources.
    /// </summary>
    public int MaxDataSources { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of subscriptions.
    /// </summary>
    public int MaxSubscriptions { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the request timeout in milliseconds.
    /// </summary>
    public int RequestTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the cache expiration time in minutes.
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the allowed domains for data sources.
    /// </summary>
    public string[] AllowedDomains { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets whether to enable data source verification.
    /// </summary>
    public bool EnableDataSourceVerification { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable response caching.
    /// </summary>
    public bool EnableResponseCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum batch size for oracle requests.
    /// </summary>
    public int MaxBatchSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the rate limit per minute.
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 100;
}

/// <summary>
/// Represents the dependencies required by the Oracle service.
/// </summary>
public class OracleServiceDependencies
{
    /// <summary>
    /// Gets or sets the enclave manager.
    /// </summary>
    public IEnclaveManager? EnclaveManager { get; set; }

    /// <summary>
    /// Gets or sets the blockchain client factory.
    /// </summary>
    public IBlockchainClientFactory? BlockchainClientFactory { get; set; }

    /// <summary>
    /// Gets or sets the HTTP client service.
    /// </summary>
    public IHttpClientService? HttpClientService { get; set; }

    /// <summary>
    /// Gets or sets the logger.
    /// </summary>
    public ILogger<OracleService>? Logger { get; set; }
}

/// <summary>
/// Builder for configuring Oracle service dependencies and options.
/// </summary>
public class OracleServiceBuilder
{
    private readonly OracleServiceDependencies _dependencies = new();
    private readonly OracleServiceOptions _options = new();

    /// <summary>
    /// Sets the enclave manager dependency.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <returns>The builder instance.</returns>
    public OracleServiceBuilder WithEnclaveManager(IEnclaveManager enclaveManager)
    {
        ArgumentNullException.ThrowIfNull(enclaveManager);
        _dependencies.EnclaveManager = enclaveManager;
        return this;
    }

    /// <summary>
    /// Sets the blockchain client factory dependency.
    /// </summary>
    /// <param name="blockchainClientFactory">The blockchain client factory.</param>
    /// <returns>The builder instance.</returns>
    public OracleServiceBuilder WithBlockchainClientFactory(IBlockchainClientFactory blockchainClientFactory)
    {
        ArgumentNullException.ThrowIfNull(blockchainClientFactory);
        _dependencies.BlockchainClientFactory = blockchainClientFactory;
        return this;
    }

    /// <summary>
    /// Sets the HTTP client service dependency.
    /// </summary>
    /// <param name="httpClientService">The HTTP client service.</param>
    /// <returns>The builder instance.</returns>
    public OracleServiceBuilder WithHttpClientService(IHttpClientService httpClientService)
    {
        ArgumentNullException.ThrowIfNull(httpClientService);
        _dependencies.HttpClientService = httpClientService;
        return this;
    }

    /// <summary>
    /// Sets the logger dependency.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The builder instance.</returns>
    public OracleServiceBuilder WithLogger(ILogger<OracleService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _dependencies.Logger = logger;
        return this;
    }

    /// <summary>
    /// Sets the service configuration.
    /// </summary>
    /// <param name="configuration">The service configuration.</param>
    /// <returns>The builder instance.</returns>
    public OracleServiceBuilder WithConfiguration(IServiceConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _options.Configuration = configuration;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of data sources.
    /// </summary>
    /// <param name="maxDataSources">The maximum number of data sources.</param>
    /// <returns>The builder instance.</returns>
    public OracleServiceBuilder WithMaxDataSources(int maxDataSources)
    {
        if (maxDataSources <= 0)
            throw new ArgumentException("Max data sources must be greater than zero", nameof(maxDataSources));
        
        _options.MaxDataSources = maxDataSources;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of subscriptions.
    /// </summary>
    /// <param name="maxSubscriptions">The maximum number of subscriptions.</param>
    /// <returns>The builder instance.</returns>
    public OracleServiceBuilder WithMaxSubscriptions(int maxSubscriptions)
    {
        if (maxSubscriptions <= 0)
            throw new ArgumentException("Max subscriptions must be greater than zero", nameof(maxSubscriptions));
        
        _options.MaxSubscriptions = maxSubscriptions;
        return this;
    }

    /// <summary>
    /// Sets the request timeout.
    /// </summary>
    /// <param name="timeoutMs">The timeout in milliseconds.</param>
    /// <returns>The builder instance.</returns>
    public OracleServiceBuilder WithRequestTimeout(int timeoutMs)
    {
        if (timeoutMs <= 0)
            throw new ArgumentException("Timeout must be greater than zero", nameof(timeoutMs));
        
        _options.RequestTimeoutMs = timeoutMs;
        return this;
    }

    /// <summary>
    /// Sets the allowed domains for data sources.
    /// </summary>
    /// <param name="allowedDomains">The allowed domains.</param>
    /// <returns>The builder instance.</returns>
    public OracleServiceBuilder WithAllowedDomains(params string[] allowedDomains)
    {
        ArgumentNullException.ThrowIfNull(allowedDomains);
        _options.AllowedDomains = allowedDomains;
        return this;
    }

    /// <summary>
    /// Enables or disables data source verification.
    /// </summary>
    /// <param name="enabled">Whether to enable verification.</param>
    /// <returns>The builder instance.</returns>
    public OracleServiceBuilder WithDataSourceVerification(bool enabled)
    {
        _options.EnableDataSourceVerification = enabled;
        return this;
    }

    /// <summary>
    /// Enables or disables response caching.
    /// </summary>
    /// <param name="enabled">Whether to enable caching.</param>
    /// <returns>The builder instance.</returns>
    public OracleServiceBuilder WithResponseCaching(bool enabled)
    {
        _options.EnableResponseCaching = enabled;
        return this;
    }

    /// <summary>
    /// Sets the cache expiration time.
    /// </summary>
    /// <param name="minutes">The expiration time in minutes.</param>
    /// <returns>The builder instance.</returns>
    public OracleServiceBuilder WithCacheExpiration(int minutes)
    {
        if (minutes <= 0)
            throw new ArgumentException("Cache expiration must be greater than zero", nameof(minutes));
        
        _options.CacheExpirationMinutes = minutes;
        return this;
    }

    /// <summary>
    /// Sets the maximum batch size.
    /// </summary>
    /// <param name="batchSize">The maximum batch size.</param>
    /// <returns>The builder instance.</returns>
    public OracleServiceBuilder WithMaxBatchSize(int batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than zero", nameof(batchSize));
        
        _options.MaxBatchSize = batchSize;
        return this;
    }

    /// <summary>
    /// Sets the rate limit per minute.
    /// </summary>
    /// <param name="rateLimit">The rate limit per minute.</param>
    /// <returns>The builder instance.</returns>
    public OracleServiceBuilder WithRateLimit(int rateLimit)
    {
        if (rateLimit <= 0)
            throw new ArgumentException("Rate limit must be greater than zero", nameof(rateLimit));
        
        _options.RateLimitPerMinute = rateLimit;
        return this;
    }

    /// <summary>
    /// Builds the Oracle service with the configured dependencies and options.
    /// </summary>
    /// <returns>The configured Oracle service.</returns>
    public OracleService Build()
    {
        ValidateDependencies();
        return new OracleService(_dependencies, _options);
    }

    /// <summary>
    /// Gets the configured dependencies.
    /// </summary>
    /// <returns>The dependencies.</returns>
    public OracleServiceDependencies GetDependencies()
    {
        ValidateDependencies();
        return _dependencies;
    }

    /// <summary>
    /// Gets the configured options.
    /// </summary>
    /// <returns>The options.</returns>
    public OracleServiceOptions GetOptions()
    {
        return _options;
    }

    private void ValidateDependencies()
    {
        if (_dependencies.EnclaveManager == null)
            throw new InvalidOperationException("EnclaveManager dependency is required");
        
        if (_dependencies.BlockchainClientFactory == null)
            throw new InvalidOperationException("BlockchainClientFactory dependency is required");
        
        if (_dependencies.HttpClientService == null)
            throw new InvalidOperationException("HttpClientService dependency is required");
        
        if (_dependencies.Logger == null)
            throw new InvalidOperationException("Logger dependency is required");
    }
}