using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;


namespace NeoServiceLayer.Services.Authentication.Infrastructure
{
    /// <summary>
    /// Manages Redis connection lifecycle and health monitoring
    /// </summary>
    public class RedisConnectionManager : IHostedService, IDisposable
    {
        private readonly ILogger<RedisConnectionManager> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private IConnectionMultiplexer _connection;
        private Timer _healthCheckTimer;
        private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(1);
        private int _reconnectAttempts = 0;
        private readonly int _maxReconnectAttempts = 5;

        public RedisConnectionManager(
            ILogger<RedisConnectionManager> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _connectionString = GetConnectionString();
        }

        public IConnectionMultiplexer Connection => _connection;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await ConnectAsync();
                StartHealthCheck();
                _logger.LogInformation("Redis connection manager started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Redis connection manager");
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                StopHealthCheck();

                if (_connection != null)
                {
                    await _connection.CloseAsync();
                    _connection.Dispose();
                }

                _logger.LogInformation("Redis connection manager stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping Redis connection manager");
            }
        }

        private async Task ConnectAsync()
        {
            try
            {
                var options = ConfigurationOptions.Parse(_connectionString);

                // Configure connection options
                options.AbortOnConnectFail = false;
                options.ConnectRetry = 3;
                options.ConnectTimeout = 5000;
                options.SyncTimeout = 5000;
                options.AsyncTimeout = 5000;
                options.KeepAlive = 60;
                options.ReconnectRetryPolicy = new ExponentialRetry(5000);

                // Add event handlers
                _connection = await ConnectionMultiplexer.ConnectAsync(options);

                _connection.ConnectionFailed += OnConnectionFailed;
                _connection.ConnectionRestored += OnConnectionRestored;
                _connection.ErrorMessage += OnErrorMessage;
                _connection.InternalError += OnInternalError;

                // Test connection
                var db = _connection.GetDatabase();
                await db.PingAsync();

                _reconnectAttempts = 0;
                _logger.LogInformation("Successfully connected to Redis at {Endpoints}",
                    string.Join(", ", options.EndPoints));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Redis");
                throw;
            }
        }

        private async Task ReconnectAsync()
        {
            if (_reconnectAttempts >= _maxReconnectAttempts)
            {
                _logger.LogError("Max reconnection attempts reached. Redis connection failed permanently.");
                return;
            }

            _reconnectAttempts++;
            _logger.LogWarning("Attempting to reconnect to Redis (attempt {Attempt}/{Max})",
                _reconnectAttempts, _maxReconnectAttempts);

            try
            {
                if (_connection != null)
                {
                    await _connection.CloseAsync();
                    _connection.Dispose();
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, _reconnectAttempts))); // Exponential backoff
                await ConnectAsync();

                _logger.LogInformation("Successfully reconnected to Redis");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reconnection attempt {Attempt} failed", _reconnectAttempts);

                // Schedule another reconnection attempt
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(30));
                    await ReconnectAsync();
                });
            }
        }

        private void StartHealthCheck()
        {
            _healthCheckTimer = new Timer(
                async _ => await PerformHealthCheckAsync(),
                null,
                _healthCheckInterval,
                _healthCheckInterval);
        }

        private void StopHealthCheck()
        {
            _healthCheckTimer?.Change(Timeout.Infinite, 0);
            _healthCheckTimer?.Dispose();
        }

        private async Task PerformHealthCheckAsync()
        {
            try
            {
                if (_connection == null || !_connection.IsConnected)
                {
                    _logger.LogWarning("Redis connection is not healthy. Attempting to reconnect...");
                    await ReconnectAsync();
                    return;
                }

                var db = _connection.GetDatabase();
                var latency = await db.PingAsync();

                if (latency > TimeSpan.FromSeconds(1))
                {
                    _logger.LogWarning("Redis latency is high: {Latency}ms", latency.TotalMilliseconds);
                }

                // Get server statistics
                var endpoints = _connection.GetEndPoints();
                foreach (var endpoint in endpoints)
                {
                    var server = _connection.GetServer(endpoint);
                    if (server.IsConnected)
                    {
                        var info = await server.InfoAsync();
                        LogServerMetrics(endpoint.ToString(), info);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
            }
        }

        private void LogServerMetrics(string endpoint, IGrouping<string, KeyValuePair<string, string>>[] info)
        {
            try
            {
                // Log key metrics
                var memorySection = info.FirstOrDefault(g => g.Key == "Memory");
                if (memorySection != null)
                {
                    var usedMemory = memorySection.FirstOrDefault(kv => kv.Key == "used_memory_human").Value;
                    var maxMemory = memorySection.FirstOrDefault(kv => kv.Key == "maxmemory_human").Value;

                    _logger.LogDebug("Redis [{Endpoint}] Memory: {Used}/{Max}",
                        endpoint, usedMemory, maxMemory);
                }

                var statsSection = info.FirstOrDefault(g => g.Key == "Stats");
                if (statsSection != null)
                {
                    var totalConnections = statsSection.FirstOrDefault(kv => kv.Key == "total_connections_received").Value;
                    var totalCommands = statsSection.FirstOrDefault(kv => kv.Key == "total_commands_processed").Value;

                    _logger.LogDebug("Redis [{Endpoint}] Stats: Connections={Connections}, Commands={Commands}",
                        endpoint, totalConnections, totalCommands);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging server metrics");
            }
        }

        private string GetConnectionString()
        {
            var redisConfig = _configuration.GetSection("Redis");

            if (redisConfig.Exists())
            {
                var host = redisConfig["Host"] ?? "localhost";
                var port = redisConfig["Port"] ?? "6379";
                var password = redisConfig["Password"];
                var ssl = redisConfig.GetValue<bool>("Ssl", false);
                var database = redisConfig.GetValue<int>("Database", 0);

                var connectionString = $"{host}:{port},abortConnect=false,defaultDatabase={database}";

                if (!string.IsNullOrEmpty(password))
                {
                    connectionString += $",password={password}";
                }

                if (ssl)
                {
                    connectionString += ",ssl=true";
                }

                return connectionString;
            }

            // Fall back to connection string
            return _configuration.GetConnectionString("Redis") ?? "localhost:6379";
        }

        private void OnConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            _logger.LogError("Redis connection failed: {FailureType} - {Exception}",
                e.FailureType, e.Exception?.Message);

            _ = Task.Run(async () => await ReconnectAsync());
        }

        private void OnConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            _logger.LogInformation("Redis connection restored to {Endpoint}", e.EndPoint);
            _reconnectAttempts = 0;
        }

        private void OnErrorMessage(object sender, RedisErrorEventArgs e)
        {
            _logger.LogError("Redis error: {Message}", e.Message);
        }

        private void OnInternalError(object sender, InternalErrorEventArgs e)
        {
            _logger.LogError(e.Exception, "Redis internal error: {Origin}", e.Origin);
        }

        public void Dispose()
        {
            StopHealthCheck();
            _connection?.Dispose();
        }
    }

    /// <summary>
    /// Redis connection factory for dependency injection
    /// </summary>
    public class RedisConnectionFactory
    {
        private readonly RedisConnectionManager _connectionManager;

        public RedisConnectionFactory(RedisConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public IConnectionMultiplexer GetConnection()
        {
            return _connectionManager.Connection;
        }
    }
}