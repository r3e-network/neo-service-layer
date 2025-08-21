using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Services
{
    /// <summary>
    /// Background service for cleaning up expired SGX sealed data and attestations
    /// </summary>
    public class SgxCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SgxCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval;

        public SgxCleanupService(
            IServiceProvider serviceProvider,
            ILogger<SgxCleanupService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cleanupInterval = TimeSpan.FromHours(1); // Run cleanup every hour
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SGX Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);
                    
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await PerformCleanupAsync(stoppingToken);
                    }
                }
                catch (TaskCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during SGX cleanup execution");
                    // Continue running even if cleanup fails
                }
            }

            _logger.LogInformation("SGX Cleanup Service stopped");
        }

        private async Task PerformCleanupAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting SGX data cleanup");

            using var scope = _serviceProvider.CreateScope();
            var sgxStore = scope.ServiceProvider.GetRequiredService<ISgxConfidentialStore>();

            try
            {
                await sgxStore.CleanupExpiredDataAsync();
                _logger.LogInformation("SGX cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform SGX cleanup");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SGX Cleanup Service is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}