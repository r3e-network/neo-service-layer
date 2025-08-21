using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Oracle;

namespace NeoServiceLayer.Services.Oracle;

/// <summary>
/// Hosted service worker for Oracle service lifecycle management.
/// </summary>
public class OracleServiceWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OracleServiceWorker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleServiceWorker"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger.</param>
    public OracleServiceWorker(
        IServiceProvider serviceProvider,
        ILogger<OracleServiceWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Oracle Service Worker starting...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var oracleService = scope.ServiceProvider.GetRequiredService<IOracleService>();

            // Initialize the Oracle service
            var initialized = await oracleService.InitializeAsync();
            if (!initialized)
            {
                _logger.LogError("Failed to initialize Oracle service");
                return;
            }

            _logger.LogInformation("Oracle service initialized successfully");

            // Start the Oracle service
            var started = await oracleService.StartAsync();
            if (!started)
            {
                _logger.LogError("Failed to start Oracle service");
                return;
            }

            _logger.LogInformation("Oracle service started successfully");

            // Wait for cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Oracle Service Worker is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oracle Service Worker encountered an error");
        }
    }

    /// <inheritdoc/>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Oracle Service Worker stopping...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var oracleService = scope.ServiceProvider.GetRequiredService<IOracleService>();

            // Stop the Oracle service gracefully
            await oracleService.StopAsync();
            _logger.LogInformation("Oracle service stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Oracle service");
        }

        await base.StopAsync(cancellationToken);
    }
}