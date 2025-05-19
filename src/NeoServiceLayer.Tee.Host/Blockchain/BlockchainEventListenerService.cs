using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Blockchain;

namespace NeoServiceLayer.Tee.Host.Blockchain
{
    /// <summary>
    /// Hosted service for the blockchain event listener.
    /// </summary>
    public class BlockchainEventListenerService : IHostedService
    {
        private readonly ILogger<BlockchainEventListenerService> _logger;
        private readonly IBlockchainEventListener _eventListener;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the BlockchainEventListenerService class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventListener">The blockchain event listener.</param>
        /// <param name="configuration">The configuration.</param>
        public BlockchainEventListenerService(
            ILogger<BlockchainEventListenerService> logger,
            IBlockchainEventListener eventListener,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventListener = eventListener ?? throw new ArgumentNullException(nameof(eventListener));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting blockchain event listener service");

                // Initialize the event listener
                if (!await _eventListener.InitializeAsync())
                {
                    _logger.LogError("Failed to initialize blockchain event listener");
                    return;
                }

                // Configure the event listener
                var pollingIntervalMs = _configuration.GetValue<int>("Blockchain:PollingIntervalMs", 15000);
                var maxBlocksPerPoll = _configuration.GetValue<int>("Blockchain:MaxBlocksPerPoll", 10);
                var requiredConfirmations = _configuration.GetValue<int>("Blockchain:RequiredConfirmations", 1);

                _eventListener.SetPollingIntervalMs(pollingIntervalMs);
                _eventListener.SetMaxBlocksPerPoll(maxBlocksPerPoll);
                _eventListener.SetRequiredConfirmations(requiredConfirmations);

                // Start the event listener
                if (!await _eventListener.StartAsync())
                {
                    _logger.LogError("Failed to start blockchain event listener");
                    return;
                }

                _logger.LogInformation("Blockchain event listener service started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting blockchain event listener service");
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Stopping blockchain event listener service");

                // Stop the event listener
                if (_eventListener.IsRunning())
                {
                    if (!await _eventListener.StopAsync())
                    {
                        _logger.LogError("Failed to stop blockchain event listener");
                        return;
                    }
                }

                _logger.LogInformation("Blockchain event listener service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping blockchain event listener service");
            }
        }
    }
}
