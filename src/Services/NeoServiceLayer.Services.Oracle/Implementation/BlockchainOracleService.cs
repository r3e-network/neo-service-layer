using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Oracle.Models;
using NeoServiceLayer.Infrastructure.Caching;

namespace NeoServiceLayer.Services.Oracle.Implementation
{
    /// <summary>
    /// Oracle service for blockchain data feeds and external data integration
    /// </summary>
    public class BlockchainOracleService : ServiceBase, IOracleService
    {
        private readonly ILogger<BlockchainOracleService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOracleDataRepository _repository;
        private readonly ICacheService _cacheService;
        private readonly IMetricsCollector _metrics;
        private readonly IBlockchainClient _blockchainClient;
        
        private readonly ConcurrentDictionary<string, DataFeed> _activeFeeds;
        private readonly ConcurrentDictionary<string, FeedSubscription> _subscriptions;
        private readonly Dictionary<string, DataProvider> _providers;
        private readonly SemaphoreSlim _updateSemaphore;
        private CancellationTokenSource _cancellationTokenSource;
        private Timer _updateTimer;
        
        private readonly int _maxConcurrentUpdates;
        private readonly int _defaultUpdateIntervalSeconds;
        private readonly int _cacheExpirySeconds;

        public BlockchainOracleService(
            ILogger<BlockchainOracleService> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IOracleDataRepository repository,
            ICacheService cacheService,
            IMetricsCollector metrics,
            IBlockchainClient blockchainClient)
            : base("BlockchainOracleService", "Oracle service for data feeds and external data", "1.0.0", logger)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _repository = repository;
            _cacheService = cacheService;
            _metrics = metrics;
            _blockchainClient = blockchainClient;

            _activeFeeds = new ConcurrentDictionary<string, DataFeed>();
            _subscriptions = new ConcurrentDictionary<string, FeedSubscription>();
            _providers = new Dictionary<string, DataProvider>();
            
            _maxConcurrentUpdates = configuration.GetValue<int>("Oracle:MaxConcurrentUpdates", 10);
            _defaultUpdateIntervalSeconds = configuration.GetValue<int>("Oracle:DefaultUpdateInterval", 60);
            _cacheExpirySeconds = configuration.GetValue<int>("Oracle:CacheExpirySeconds", 30);
            _updateSemaphore = new SemaphoreSlim(_maxConcurrentUpdates, _maxConcurrentUpdates);

            InitializeProviders();
        }

        private void InitializeProviders()
        {
            // Initialize data providers from configuration
            _providers["chainlink"] = new DataProvider
            {
                Id = "chainlink",
                Name = "Chainlink",
                Type = ProviderType.Blockchain,
                BaseUrl = "https://api.chain.link/v1",
                ApiKey = _configuration["Oracle:Providers:Chainlink:ApiKey"],
                RateLimit = 100,
                Priority = 1
            };

            _providers["coingecko"] = new DataProvider
            {
                Id = "coingecko",
                Name = "CoinGecko",
                Type = ProviderType.PriceFeed,
                BaseUrl = "https://api.coingecko.com/api/v3",
                ApiKey = _configuration["Oracle:Providers:CoinGecko:ApiKey"],
                RateLimit = 50,
                Priority = 2
            };

            _providers["binance"] = new DataProvider
            {
                Id = "binance",
                Name = "Binance",
                Type = ProviderType.Exchange,
                BaseUrl = "https://api.binance.com/api/v3",
                RateLimit = 1200,
                Priority = 1
            };

            _providers["weather"] = new DataProvider
            {
                Id = "openweather",
                Name = "OpenWeatherMap",
                Type = ProviderType.Weather,
                BaseUrl = "https://api.openweathermap.org/data/2.5",
                ApiKey = _configuration["Oracle:Providers:OpenWeather:ApiKey"],
                RateLimit = 60,
                Priority = 1
            };
        }

        public async Task<DataFeed> CreateDataFeedAsync(CreateFeedRequest request)
        {
            try
            {
                // Validate request
                ValidateCreateFeedRequest(request);

                var feedId = Guid.NewGuid().ToString();
                var feed = new DataFeed
                {
                    Id = feedId,
                    Name = request.Name,
                    Description = request.Description,
                    FeedType = request.FeedType,
                    DataSource = request.DataSource,
                    UpdateInterval = request.UpdateInterval ?? _defaultUpdateIntervalSeconds,
                    AggregationMethod = request.AggregationMethod ?? AggregationMethod.Average,
                    Providers = SelectProviders(request.FeedType, request.PreferredProviders),
                    Parameters = request.Parameters ?? new Dictionary<string, object>(),
                    Status = FeedStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.UserId
                };

                // Store in repository
                await _repository.CreateFeedAsync(feed);
                _activeFeeds.TryAdd(feedId, feed);

                // Start feed updates
                await StartFeedUpdatesAsync(feed);

                _logger.LogInformation("Data feed {FeedId} created: {FeedName}", feedId, feed.Name);
                _metrics.IncrementCounter("oracle.feeds.created", 
                    new[] { ("type", feed.FeedType.ToString()) });

                return feed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating data feed");
                _metrics.IncrementCounter("oracle.feeds.create_error");
                throw;
            }
        }

        public async Task<FeedData> GetFeedDataAsync(string feedId, bool useCache = true)
        {
            try
            {
                // Check cache first
                if (useCache)
                {
                    var cachedData = await _cacheService.GetAsync<FeedData>($"oracle:feed:{feedId}");
                    if (cachedData != null)
                    {
                        _metrics.IncrementCounter("oracle.cache.hit");
                        return cachedData;
                    }
                }

                // Get feed configuration
                if (!_activeFeeds.TryGetValue(feedId, out var feed))
                {
                    feed = await _repository.GetFeedAsync(feedId);
                    if (feed == null)
                    {
                        throw new KeyNotFoundException($"Feed {feedId} not found");
                    }
                }

                // Fetch fresh data
                var feedData = await FetchFeedDataAsync(feed);

                // Cache the result
                await _cacheService.SetAsync($"oracle:feed:{feedId}", feedData, 
                    TimeSpan.FromSeconds(_cacheExpirySeconds));

                _metrics.IncrementCounter("oracle.data.fetched", 
                    new[] { ("feed", feedId), ("type", feed.FeedType.ToString()) });

                return feedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feed data for {FeedId}", feedId);
                _metrics.IncrementCounter("oracle.data.fetch_error");
                throw;
            }
        }

        public async Task<PriceData> GetPriceDataAsync(string symbol, string currency = "USD")
        {
            try
            {
                var cacheKey = $"oracle:price:{symbol}:{currency}";
                
                // Check cache
                var cachedPrice = await _cacheService.GetAsync<PriceData>(cacheKey);
                if (cachedPrice != null && (DateTime.UtcNow - cachedPrice.Timestamp).TotalSeconds < 10)
                {
                    return cachedPrice;
                }

                // Fetch from multiple sources
                var tasks = new List<Task<decimal?>>();
                
                if (_providers.ContainsKey("coingecko"))
                {
                    tasks.Add(FetchPriceFromCoinGeckoAsync(symbol, currency));
                }
                
                if (_providers.ContainsKey("binance"))
                {
                    tasks.Add(FetchPriceFromBinanceAsync(symbol, currency));
                }

                var prices = await Task.WhenAll(tasks);
                var validPrices = prices.Where(p => p.HasValue).Select(p => p.Value).ToList();

                if (!validPrices.Any())
                {
                    throw new InvalidOperationException($"No price data available for {symbol}/{currency}");
                }

                // Aggregate prices
                var aggregatedPrice = validPrices.Average();
                var priceData = new PriceData
                {
                    Symbol = symbol,
                    Currency = currency,
                    Price = aggregatedPrice,
                    High24h = validPrices.Max(),
                    Low24h = validPrices.Min(),
                    Volume24h = 0, // Would need to aggregate volume from sources
                    Change24h = 0, // Would need to calculate from historical data
                    Sources = validPrices.Count,
                    Timestamp = DateTime.UtcNow
                };

                // Cache the result
                await _cacheService.SetAsync(cacheKey, priceData, TimeSpan.FromSeconds(10));

                _logger.LogDebug("Price fetched for {Symbol}/{Currency}: {Price}", 
                    symbol, currency, aggregatedPrice);
                _metrics.RecordValue("oracle.price.value", (double)aggregatedPrice,
                    new[] { ("symbol", symbol), ("currency", currency) });

                return priceData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching price for {Symbol}/{Currency}", symbol, currency);
                _metrics.IncrementCounter("oracle.price.fetch_error");
                throw;
            }
        }

        public async Task<FeedSubscription> SubscribeToFeedAsync(SubscribeFeedRequest request)
        {
            try
            {
                var subscription = new FeedSubscription
                {
                    Id = Guid.NewGuid().ToString(),
                    FeedId = request.FeedId,
                    SubscriberId = request.SubscriberId,
                    CallbackUrl = request.CallbackUrl,
                    CallbackContract = request.CallbackContract,
                    Filters = request.Filters ?? new Dictionary<string, object>(),
                    NotificationThreshold = request.NotificationThreshold,
                    Status = SubscriptionStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.CreateSubscriptionAsync(subscription);
                _subscriptions.TryAdd(subscription.Id, subscription);

                _logger.LogInformation("Subscription {SubscriptionId} created for feed {FeedId}", 
                    subscription.Id, request.FeedId);
                _metrics.IncrementCounter("oracle.subscriptions.created");

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription");
                _metrics.IncrementCounter("oracle.subscriptions.create_error");
                throw;
            }
        }

        public async Task<bool> UnsubscribeFromFeedAsync(string subscriptionId)
        {
            try
            {
                if (_subscriptions.TryRemove(subscriptionId, out var subscription))
                {
                    subscription.Status = SubscriptionStatus.Cancelled;
                    await _repository.UpdateSubscriptionAsync(subscription);
                    
                    _logger.LogInformation("Subscription {SubscriptionId} cancelled", subscriptionId);
                    _metrics.IncrementCounter("oracle.subscriptions.cancelled");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", subscriptionId);
                return false;
            }
        }

        public async Task<bool> PublishDataOnChainAsync(string feedId, OnChainPublishRequest request)
        {
            try
            {
                var feedData = await GetFeedDataAsync(feedId, false);
                
                // Prepare on-chain transaction
                var txData = new
                {
                    feedId = feedId,
                    value = feedData.Value,
                    timestamp = feedData.Timestamp.ToUnixTimeSeconds(),
                    sources = feedData.Sources.Count,
                    signature = GenerateDataSignature(feedData)
                };

                // Call smart contract
                var txHash = await _blockchainClient.CallContractAsync(
                    request.ContractAddress,
                    "updateFeed",
                    txData);

                _logger.LogInformation("Feed data published on-chain: {FeedId}, Tx: {TxHash}", 
                    feedId, txHash);
                _metrics.IncrementCounter("oracle.onchain.published");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing feed {FeedId} on-chain", feedId);
                _metrics.IncrementCounter("oracle.onchain.publish_error");
                throw;
            }
        }

        private async Task<FeedData> FetchFeedDataAsync(DataFeed feed)
        {
            var dataPoints = new List<DataPoint>();

            // Fetch from all configured providers
            var tasks = feed.Providers.Select(providerId => 
                FetchFromProviderAsync(providerId, feed)).ToArray();
            
            var results = await Task.WhenAll(tasks);
            dataPoints.AddRange(results.Where(r => r != null));

            if (!dataPoints.Any())
            {
                throw new InvalidOperationException($"No data available for feed {feed.Id}");
            }

            // Aggregate data based on method
            var aggregatedValue = AggregateDataPoints(dataPoints, feed.AggregationMethod);

            return new FeedData
            {
                FeedId = feed.Id,
                Value = aggregatedValue,
                Sources = dataPoints.Select(dp => new DataSource
                {
                    ProviderId = dp.ProviderId,
                    Value = dp.Value,
                    Timestamp = dp.Timestamp
                }).ToList(),
                Timestamp = DateTime.UtcNow,
                Quality = CalculateDataQuality(dataPoints)
            };
        }

        private async Task<DataPoint> FetchFromProviderAsync(string providerId, DataFeed feed)
        {
            try
            {
                if (!_providers.TryGetValue(providerId, out var provider))
                {
                    return null;
                }

                var httpClient = _httpClientFactory.CreateClient(providerId);
                httpClient.BaseAddress = new Uri(provider.BaseUrl);

                if (!string.IsNullOrEmpty(provider.ApiKey))
                {
                    httpClient.DefaultRequestHeaders.Add("X-API-Key", provider.ApiKey);
                }

                // Build request URL based on feed type
                var requestUrl = BuildProviderRequestUrl(provider, feed);
                var response = await httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Provider {ProviderId} returned {StatusCode}", 
                        providerId, response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var value = ParseProviderResponse(provider, feed, content);

                return new DataPoint
                {
                    ProviderId = providerId,
                    Value = value,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching from provider {ProviderId}", providerId);
                return null;
            }
        }

        private async Task<decimal?> FetchPriceFromCoinGeckoAsync(string symbol, string currency)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("coingecko");
                var response = await httpClient.GetAsync(
                    $"https://api.coingecko.com/api/v3/simple/price?ids={symbol.ToLower()}&vs_currencies={currency.ToLower()}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(content);
                    return data[symbol.ToLower()][currency.ToLower()];
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch price from CoinGecko");
            }
            return null;
        }

        private async Task<decimal?> FetchPriceFromBinanceAsync(string symbol, string currency)
        {
            try
            {
                // Binance uses different symbol format (e.g., BTCUSDT)
                var pair = $"{symbol.ToUpper()}{currency.ToUpper()}T";
                var httpClient = _httpClientFactory.CreateClient("binance");
                var response = await httpClient.GetAsync(
                    $"https://api.binance.com/api/v3/ticker/price?symbol={pair}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(content);
                    return decimal.Parse((string)data.price);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch price from Binance");
            }
            return null;
        }

        private string BuildProviderRequestUrl(DataProvider provider, DataFeed feed)
        {
            return feed.FeedType switch
            {
                FeedType.Price => $"/price?symbol={feed.Parameters["symbol"]}&currency={feed.Parameters.GetValueOrDefault("currency", "USD")}",
                FeedType.Weather => $"/weather?q={feed.Parameters["location"]}&appid={provider.ApiKey}",
                FeedType.BlockchainData => $"/block/latest",
                FeedType.Custom => feed.Parameters.GetValueOrDefault("endpoint", "/").ToString(),
                _ => "/"
            };
        }

        private decimal ParseProviderResponse(DataProvider provider, DataFeed feed, string response)
        {
            try
            {
                dynamic data = JsonConvert.DeserializeObject(response);
                
                return feed.FeedType switch
                {
                    FeedType.Price => decimal.Parse(data.price.ToString()),
                    FeedType.Weather => decimal.Parse(data.main.temp.ToString()),
                    FeedType.BlockchainData => decimal.Parse(data.height.ToString()),
                    _ => decimal.Parse(data.value.ToString())
                };
            }
            catch
            {
                throw new InvalidOperationException($"Failed to parse response from provider {provider.Id}");
            }
        }

        private decimal AggregateDataPoints(List<DataPoint> dataPoints, AggregationMethod method)
        {
            var values = dataPoints.Select(dp => dp.Value).ToList();
            
            return method switch
            {
                AggregationMethod.Average => values.Average(),
                AggregationMethod.Median => GetMedian(values),
                AggregationMethod.Min => values.Min(),
                AggregationMethod.Max => values.Max(),
                AggregationMethod.Sum => values.Sum(),
                AggregationMethod.WeightedAverage => GetWeightedAverage(dataPoints),
                _ => values.Average()
            };
        }

        private decimal GetMedian(List<decimal> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            var mid = sorted.Count / 2;
            return sorted.Count % 2 == 0 
                ? (sorted[mid - 1] + sorted[mid]) / 2 
                : sorted[mid];
        }

        private decimal GetWeightedAverage(List<DataPoint> dataPoints)
        {
            var totalWeight = 0m;
            var weightedSum = 0m;

            foreach (var point in dataPoints)
            {
                // Weight based on provider priority
                var weight = _providers.ContainsKey(point.ProviderId) 
                    ? (decimal)(11 - _providers[point.ProviderId].Priority) 
                    : 1m;
                
                totalWeight += weight;
                weightedSum += point.Value * weight;
            }

            return totalWeight > 0 ? weightedSum / totalWeight : 0;
        }

        private double CalculateDataQuality(List<DataPoint> dataPoints)
        {
            if (dataPoints.Count == 0) return 0;
            
            // Quality based on number of sources and variance
            var sourceScore = Math.Min(dataPoints.Count / 3.0, 1.0) * 0.5;
            
            var values = dataPoints.Select(dp => (double)dp.Value).ToList();
            var mean = values.Average();
            var variance = values.Select(v => Math.Pow(v - mean, 2)).Average();
            var cv = mean != 0 ? Math.Sqrt(variance) / Math.Abs(mean) : 0;
            var varianceScore = Math.Max(0, 1 - cv) * 0.5;
            
            return sourceScore + varianceScore;
        }

        private string GenerateDataSignature(FeedData data)
        {
            // Generate cryptographic signature for on-chain verification
            var dataToSign = $"{data.FeedId}:{data.Value}:{data.Timestamp.ToUnixTimeSeconds()}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dataToSign));
            return Convert.ToBase64String(hash);
        }

        private void ValidateCreateFeedRequest(CreateFeedRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Feed name is required");
            
            if (request.UpdateInterval.HasValue && request.UpdateInterval.Value < 1)
                throw new ArgumentException("Update interval must be at least 1 second");
        }

        private List<string> SelectProviders(FeedType feedType, List<string> preferredProviders)
        {
            var eligibleProviders = _providers.Values
                .Where(p => IsProviderEligible(p, feedType))
                .OrderBy(p => p.Priority)
                .Select(p => p.Id)
                .ToList();

            if (preferredProviders?.Any() == true)
            {
                // Prioritize preferred providers
                var selected = preferredProviders.Intersect(eligibleProviders).ToList();
                selected.AddRange(eligibleProviders.Except(selected).Take(3 - selected.Count));
                return selected;
            }

            return eligibleProviders.Take(3).ToList();
        }

        private bool IsProviderEligible(DataProvider provider, FeedType feedType)
        {
            return feedType switch
            {
                FeedType.Price => provider.Type == ProviderType.PriceFeed || provider.Type == ProviderType.Exchange,
                FeedType.Weather => provider.Type == ProviderType.Weather,
                FeedType.BlockchainData => provider.Type == ProviderType.Blockchain,
                _ => true
            };
        }

        private async Task StartFeedUpdatesAsync(DataFeed feed)
        {
            // Schedule periodic updates
            _ = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(feed.UpdateInterval), _cancellationTokenSource.Token);
                        
                        await _updateSemaphore.WaitAsync();
                        try
                        {
                            var data = await FetchFeedDataAsync(feed);
                            await NotifySubscribersAsync(feed.Id, data);
                        }
                        finally
                        {
                            _updateSemaphore.Release();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating feed {FeedId}", feed.Id);
                    }
                }
            });
        }

        private async Task NotifySubscribersAsync(string feedId, FeedData data)
        {
            var subscriptions = _subscriptions.Values
                .Where(s => s.FeedId == feedId && s.Status == SubscriptionStatus.Active)
                .ToList();

            foreach (var subscription in subscriptions)
            {
                try
                {
                    // Check notification threshold
                    if (subscription.NotificationThreshold.HasValue)
                    {
                        var lastValue = await _cacheService.GetAsync<decimal>($"oracle:sub:{subscription.Id}:last");
                        if (lastValue != default && 
                            Math.Abs(data.Value - lastValue) < subscription.NotificationThreshold.Value)
                        {
                            continue;
                        }
                    }

                    // Send notification
                    if (!string.IsNullOrEmpty(subscription.CallbackUrl))
                    {
                        await SendWebhookNotificationAsync(subscription, data);
                    }
                    else if (!string.IsNullOrEmpty(subscription.CallbackContract))
                    {
                        await SendOnChainNotificationAsync(subscription, data);
                    }

                    // Update last notified value
                    await _cacheService.SetAsync($"oracle:sub:{subscription.Id}:last", data.Value, 
                        TimeSpan.FromHours(24));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error notifying subscription {SubscriptionId}", subscription.Id);
                }
            }
        }

        private async Task SendWebhookNotificationAsync(FeedSubscription subscription, FeedData data)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var notification = new
            {
                subscriptionId = subscription.Id,
                feedId = data.FeedId,
                value = data.Value,
                timestamp = data.Timestamp,
                sources = data.Sources.Count
            };

            var response = await httpClient.PostAsJsonAsync(subscription.CallbackUrl, notification);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Webhook notification failed for subscription {SubscriptionId}", 
                    subscription.Id);
            }
        }

        private async Task SendOnChainNotificationAsync(FeedSubscription subscription, FeedData data)
        {
            await _blockchainClient.CallContractAsync(
                subscription.CallbackContract,
                "oracleCallback",
                new { feedId = data.FeedId, value = data.Value, timestamp = data.Timestamp.ToUnixTimeSeconds() });
        }

        protected override async Task<ServiceHealth> OnGetHealthAsync()
        {
            try
            {
                var dbHealthy = await _repository.CheckHealthAsync();
                var cacheHealthy = await _cacheService.CheckHealthAsync();
                var providersHealthy = _providers.Any();

                if (dbHealthy && cacheHealthy && providersHealthy)
                    return ServiceHealth.Healthy;
                else if (dbHealthy && providersHealthy)
                    return ServiceHealth.Degraded;
                else
                    return ServiceHealth.Unhealthy;
            }
            catch
            {
                return ServiceHealth.Unhealthy;
            }
        }

        protected override Task<bool> OnInitializeAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _logger.LogInformation("Oracle Service initialized with {ProviderCount} providers", _providers.Count);
            return Task.FromResult(true);
        }

        protected override async Task<bool> OnStartAsync()
        {
            // Load active feeds
            var feeds = await _repository.GetActiveFeedsAsync();
            foreach (var feed in feeds)
            {
                _activeFeeds.TryAdd(feed.Id, feed);
                await StartFeedUpdatesAsync(feed);
            }

            _logger.LogInformation("Oracle Service started with {FeedCount} active feeds", _activeFeeds.Count);
            return true;
        }

        protected override Task<bool> OnStopAsync()
        {
            _cancellationTokenSource?.Cancel();
            _updateTimer?.Dispose();
            _logger.LogInformation("Oracle Service stopped");
            return Task.FromResult(true);
        }
    }

    internal static class DateTimeExtensions
    {
        public static long ToUnixTimeSeconds(this DateTime dateTime)
        {
            return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
        }
    }
}