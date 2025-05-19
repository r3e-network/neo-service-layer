using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Events;

namespace NeoServiceLayer.Tee.Host.Events
{
    /// <summary>
    /// Background service for processing pending events.
    /// </summary>
    public class PendingEventProcessor : BackgroundService
    {
        private readonly ILogger<PendingEventProcessor> _logger;
        private readonly IEventSystem _eventSystem;
        private readonly TimeSpan _interval;

        /// <summary>
        /// Initializes a new instance of the PendingEventProcessor class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventSystem">The event system.</param>
        public PendingEventProcessor(
            ILogger<PendingEventProcessor> logger,
            IEventSystem eventSystem)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _interval = TimeSpan.FromSeconds(10); // Process pending events every 10 seconds
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Pending event processor started");

            try
            {
                // Initialize event system
                if (!await _eventSystem.InitializeAsync())
                {
                    _logger.LogError("Failed to initialize event system");
                    return;
                }

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // Process pending events
                        int processedCount = await _eventSystem.ProcessPendingEventsAsync();
                        if (processedCount > 0)
                        {
                            _logger.LogInformation("Processed {ProcessedCount} pending events", processedCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing pending events");
                    }

                    // Wait for the next interval
                    await Task.Delay(_interval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in pending event processor");
            }

            _logger.LogInformation("Pending event processor stopped");
        }
    }
}
