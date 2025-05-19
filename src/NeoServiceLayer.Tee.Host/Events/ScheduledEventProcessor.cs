using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Events;

namespace NeoServiceLayer.Tee.Host.Events
{
    /// <summary>
    /// Background service for processing scheduled events.
    /// </summary>
    public class ScheduledEventProcessor : BackgroundService
    {
        private readonly ILogger<ScheduledEventProcessor> _logger;
        private readonly IEventSystem _eventSystem;
        private readonly TimeSpan _interval;

        /// <summary>
        /// Initializes a new instance of the ScheduledEventProcessor class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventSystem">The event system.</param>
        public ScheduledEventProcessor(
            ILogger<ScheduledEventProcessor> logger,
            IEventSystem eventSystem)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _interval = TimeSpan.FromSeconds(60); // Process scheduled events every minute
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scheduled event processor started");

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
                        // Get current time
                        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                        // Create schedule event
                        var eventData = JsonSerializer.Serialize(new
                        {
                            current_time = (ulong)currentTime
                        });

                        // Publish schedule event
                        await _eventSystem.PublishEventAsync(
                            EventType.Schedule,
                            "system",
                            eventData);

                        _logger.LogDebug("Published schedule event with current time {CurrentTime}", currentTime);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing scheduled events");
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
                _logger.LogError(ex, "Error in scheduled event processor");
            }

            _logger.LogInformation("Scheduled event processor stopped");
        }
    }
}
