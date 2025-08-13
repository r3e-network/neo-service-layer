using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace NeoServiceLayer.Infrastructure.EventSourcing
{
    /// <summary>
    /// Initializes event sourcing infrastructure on startup
    /// </summary>
    public class EventSourcingInitializer : IHostedService
    {
        private readonly ILogger&lt;EventSourcingInitializer&gt; _logger;
        private readonly EventStoreConfiguration _eventStoreConfiguration;

        public EventSourcingInitializer(
            ILogger&lt;EventSourcingInitializer&gt; logger,
            IOptions&lt;EventStoreConfiguration&gt; eventStoreConfiguration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventStoreConfiguration = eventStoreConfiguration?.Value ?? throw new ArgumentNullException(nameof(eventStoreConfiguration));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Initializing event sourcing infrastructure");

            try
            {
                await CreateEventStoreDatabaseTablesAsync(cancellationToken);
                _logger.LogInformation("Event sourcing infrastructure initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize event sourcing infrastructure");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Event sourcing infrastructure shutdown");
            return Task.CompletedTask;
        }

        private async Task CreateEventStoreDatabaseTablesAsync(CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_eventStoreConfiguration.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            // Create event_store table
            var eventStoreTableSql = @"
                CREATE TABLE IF NOT EXISTS event_store (
                    sequence_number BIGSERIAL PRIMARY KEY,
                    event_id UUID NOT NULL UNIQUE,
                    aggregate_id VARCHAR(255) NOT NULL,
                    aggregate_type VARCHAR(255) NOT NULL,
                    aggregate_version BIGINT NOT NULL,
                    event_type VARCHAR(255) NOT NULL,
                    event_data JSONB NOT NULL,
                    metadata JSONB NOT NULL DEFAULT '{}',
                    occurred_at TIMESTAMP WITH TIME ZONE NOT NULL,
                    causation_id UUID NULL,
                    correlation_id UUID NULL,
                    initiated_by VARCHAR(255) NOT NULL,
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                );

                CREATE INDEX IF NOT EXISTS ix_event_store_aggregate_id ON event_store (aggregate_id);
                CREATE INDEX IF NOT EXISTS ix_event_store_aggregate_type ON event_store (aggregate_type);
                CREATE INDEX IF NOT EXISTS ix_event_store_event_type ON event_store (event_type);
                CREATE INDEX IF NOT EXISTS ix_event_store_occurred_at ON event_store (occurred_at);
                CREATE INDEX IF NOT EXISTS ix_event_store_correlation_id ON event_store (correlation_id);
                CREATE UNIQUE INDEX IF NOT EXISTS ix_event_store_aggregate_version ON event_store (aggregate_id, aggregate_version);
            ";

            await using var eventStoreCommand = new NpgsqlCommand(eventStoreTableSql, connection);
            await eventStoreCommand.ExecuteNonQueryAsync(cancellationToken);

            // Create event_snapshots table
            var snapshotsTableSql = @"
                CREATE TABLE IF NOT EXISTS event_snapshots (
                    aggregate_id VARCHAR(255) PRIMARY KEY,
                    aggregate_type VARCHAR(255) NOT NULL,
                    version BIGINT NOT NULL,
                    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
                    snapshot_data JSONB NOT NULL
                );

                CREATE INDEX IF NOT EXISTS ix_event_snapshots_aggregate_type ON event_snapshots (aggregate_type);
                CREATE INDEX IF NOT EXISTS ix_event_snapshots_version ON event_snapshots (version);
            ";

            await using var snapshotsCommand = new NpgsqlCommand(snapshotsTableSql, connection);
            await snapshotsCommand.ExecuteNonQueryAsync(cancellationToken);

            // Create event processing checkpoint table for tracking processed events
            var checkpointTableSql = @"
                CREATE TABLE IF NOT EXISTS event_processing_checkpoints (
                    handler_name VARCHAR(255) PRIMARY KEY,
                    last_processed_sequence BIGINT NOT NULL DEFAULT 0,
                    last_processed_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    error_count INTEGER NOT NULL DEFAULT 0,
                    last_error TEXT NULL,
                    last_error_at TIMESTAMP WITH TIME ZONE NULL
                );
            ";

            await using var checkpointCommand = new NpgsqlCommand(checkpointTableSql, connection);
            await checkpointCommand.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Event store database tables created/verified successfully");
        }
    }
}