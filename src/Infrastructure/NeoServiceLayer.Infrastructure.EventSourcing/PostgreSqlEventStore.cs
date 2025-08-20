using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NeoServiceLayer.Core.Events;


namespace NeoServiceLayer.Infrastructure.EventSourcing
{
    /// <summary>
    /// PostgreSQL implementation of the event store
    /// </summary>
    public class PostgreSqlEventStore : IEventStore
    {
        private readonly ILogger<PostgreSqlEventStore> _logger;
        private readonly EventStoreConfiguration _configuration;
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions;

        public PostgreSqlEventStore(
            ILogger<PostgreSqlEventStore> logger,
            IOptions<EventStoreConfiguration> configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            _connectionString = _configuration.ConnectionString;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task AppendEventsAsync(
            string aggregateId,
            long expectedVersion,
            IEnumerable<IDomainEvent> events,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(aggregateId));

            var eventList = events.ToList();
            if (!eventList.Any())
                return;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                // Check current version for optimistic concurrency control
                var currentVersion = await GetAggregateVersionInternalAsync(
                    connection, transaction, aggregateId, cancellationToken);

                if (currentVersion != expectedVersion)
                {
                    throw new OptimisticConcurrencyException(
                        $"Expected version {expectedVersion} but current version is {currentVersion} for aggregate {aggregateId}");
                }

                // Insert events
                var insertSql = @"
                    INSERT INTO event_store (
                        event_id, aggregate_id, aggregate_type, aggregate_version,
                        event_type, event_data, metadata, occurred_at,
                        causation_id, correlation_id, initiated_by
                    ) VALUES (
                        @EventId, @AggregateId, @AggregateType, @AggregateVersion,
                        @EventType, @EventData, @Metadata, @OccurredAt,
                        @CausationId, @CorrelationId, @InitiatedBy
                    )";

                long version = currentVersion;
                foreach (var domainEvent in eventList)
                {
                    version++;

                    await using var command = new NpgsqlCommand(insertSql, connection, transaction);
                    command.Parameters.AddWithValue("@EventId", domainEvent.EventId);
                    command.Parameters.AddWithValue("@AggregateId", domainEvent.AggregateId);
                    command.Parameters.AddWithValue("@AggregateType", domainEvent.AggregateType);
                    command.Parameters.AddWithValue("@AggregateVersion", version);
                    command.Parameters.AddWithValue("@EventType", domainEvent.EventType);
                    command.Parameters.AddWithValue("@EventData", JsonSerializer.Serialize(domainEvent, _jsonOptions));
                    command.Parameters.AddWithValue("@Metadata", JsonSerializer.Serialize(domainEvent.Metadata, _jsonOptions));
                    command.Parameters.AddWithValue("@OccurredAt", domainEvent.OccurredAt);
                    command.Parameters.AddWithValue("@CausationId", domainEvent.CausationId as object ?? DBNull.Value);
                    command.Parameters.AddWithValue("@CorrelationId", domainEvent.CorrelationId as object ?? DBNull.Value);
                    command.Parameters.AddWithValue("@InitiatedBy", domainEvent.InitiatedBy);

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Appended {EventCount} events for aggregate {AggregateId} at version {Version}",
                    eventList.Count, aggregateId, version);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex,
                    "Failed to append events for aggregate {AggregateId}", aggregateId);
                throw;
            }
        }

        public async Task<IEnumerable<IDomainEvent>> GetEventsAsync(
            string aggregateId,
            long fromVersion = 0,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(aggregateId));

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = @"
                SELECT event_id, aggregate_id, aggregate_type, aggregate_version,
                       event_type, event_data, metadata, occurred_at,
                       causation_id, correlation_id, initiated_by
                FROM event_store
                WHERE aggregate_id = @AggregateId
                  AND aggregate_version > @FromVersion
                ORDER BY aggregate_version";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@AggregateId", aggregateId);
            command.Parameters.AddWithValue("@FromVersion", fromVersion);

            var events = new List<IDomainEvent>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var eventType = reader.GetString(reader.GetOrdinal("event_type"));
                var eventData = reader.GetString(reader.GetOrdinal("event_data"));

                // For now, store as generic event - in production, you'd deserialize to specific types
                var genericEvent = new GenericDomainEvent(
                    reader.GetGuid(reader.GetOrdinal("event_id")),
                    reader.GetDateTime(reader.GetOrdinal("occurred_at")),
                    reader.GetString(reader.GetOrdinal("aggregate_id")),
                    reader.GetString(reader.GetOrdinal("aggregate_type")),
                    reader.GetInt64(reader.GetOrdinal("aggregate_version")),
                    eventType,
                    reader.IsDBNull(reader.GetOrdinal("causation_id")) ? null : reader.GetGuid(reader.GetOrdinal("causation_id")),
                    reader.IsDBNull(reader.GetOrdinal("correlation_id")) ? null : reader.GetGuid(reader.GetOrdinal("correlation_id")),
                    reader.GetString(reader.GetOrdinal("initiated_by")),
                    eventData,
                    JsonSerializer.Deserialize<Dictionary<string, object>>(
                        reader.GetString(reader.GetOrdinal("metadata")), _jsonOptions) ?? new Dictionary<string, object>()
                );

                events.Add(genericEvent);
            }

            return events;
        }

        public async Task<IEnumerable<IDomainEvent>> GetEventsByTypeAsync(
            string eventType,
            DateTime? fromTimestamp = null,
            DateTime? toTimestamp = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(eventType))
                throw new ArgumentException("Event type cannot be null or empty", nameof(eventType));

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var whereClause = "WHERE event_type = @EventType";
            var parameters = new List<NpgsqlParameter>
            {
                new("@EventType", eventType)
            };

            if (fromTimestamp.HasValue)
            {
                whereClause += " AND occurred_at >= @FromTimestamp";
                parameters.Add(new NpgsqlParameter("@FromTimestamp", fromTimestamp.Value));
            }

            if (toTimestamp.HasValue)
            {
                whereClause += " AND occurred_at <= @ToTimestamp";
                parameters.Add(new NpgsqlParameter("@ToTimestamp", toTimestamp.Value));
            }

            var sql = $@"
                SELECT event_id, aggregate_id, aggregate_type, aggregate_version,
                       event_type, event_data, metadata, occurred_at,
                       causation_id, correlation_id, initiated_by
                FROM event_store
                {whereClause}
                ORDER BY occurred_at, aggregate_version";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            var events = new List<IDomainEvent>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var genericEvent = new GenericDomainEvent(
                    reader.GetGuid(reader.GetOrdinal("event_id")),
                    reader.GetDateTime(reader.GetOrdinal("occurred_at")),
                    reader.GetString(reader.GetOrdinal("aggregate_id")),
                    reader.GetString(reader.GetOrdinal("aggregate_type")),
                    reader.GetInt64(reader.GetOrdinal("aggregate_version")),
                    reader.GetString(reader.GetOrdinal("event_type")),
                    reader.IsDBNull(reader.GetOrdinal("causation_id")) ? null : reader.GetGuid(reader.GetOrdinal("causation_id")),
                    reader.IsDBNull(reader.GetOrdinal("correlation_id")) ? null : reader.GetGuid(reader.GetOrdinal("correlation_id")),
                    reader.GetString(reader.GetOrdinal("initiated_by")),
                    reader.GetString(reader.GetOrdinal("event_data")),
                    JsonSerializer.Deserialize<Dictionary<string, object>>(
                        reader.GetString(reader.GetOrdinal("metadata")), _jsonOptions) ?? new Dictionary<string, object>()
                );

                events.Add(genericEvent);
            }

            return events;
        }

        public async Task<IEnumerable<IDomainEvent>> GetEventsByCorrelationAsync(
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = @"
                SELECT event_id, aggregate_id, aggregate_type, aggregate_version,
                       event_type, event_data, metadata, occurred_at,
                       causation_id, correlation_id, initiated_by
                FROM event_store
                WHERE correlation_id = @CorrelationId
                ORDER BY occurred_at, aggregate_version";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CorrelationId", correlationId);

            var events = new List<IDomainEvent>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var genericEvent = new GenericDomainEvent(
                    reader.GetGuid(reader.GetOrdinal("event_id")),
                    reader.GetDateTime(reader.GetOrdinal("occurred_at")),
                    reader.GetString(reader.GetOrdinal("aggregate_id")),
                    reader.GetString(reader.GetOrdinal("aggregate_type")),
                    reader.GetInt64(reader.GetOrdinal("aggregate_version")),
                    reader.GetString(reader.GetOrdinal("event_type")),
                    reader.IsDBNull(reader.GetOrdinal("causation_id")) ? null : reader.GetGuid(reader.GetOrdinal("causation_id")),
                    reader.IsDBNull(reader.GetOrdinal("correlation_id")) ? null : reader.GetGuid(reader.GetOrdinal("correlation_id")),
                    reader.GetString(reader.GetOrdinal("initiated_by")),
                    reader.GetString(reader.GetOrdinal("event_data")),
                    JsonSerializer.Deserialize<Dictionary<string, object>>(
                        reader.GetString(reader.GetOrdinal("metadata")), _jsonOptions) ?? new Dictionary<string, object>()
                );

                events.Add(genericEvent);
            }

            return events;
        }

        public async Task<long> GetAggregateVersionAsync(
            string aggregateId,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            return await GetAggregateVersionInternalAsync(connection, null, aggregateId, cancellationToken);
        }

        public async Task SaveSnapshotAsync(
            string aggregateId,
            long version,
            object snapshot,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = @"
                INSERT INTO event_snapshots (aggregate_id, aggregate_type, version, created_at, snapshot_data)
                VALUES (@AggregateId, @AggregateType, @Version, @CreatedAt, @SnapshotData)
                ON CONFLICT (aggregate_id)
                DO UPDATE SET
                    version = EXCLUDED.version,
                    created_at = EXCLUDED.created_at,
                    snapshot_data = EXCLUDED.snapshot_data
                WHERE event_snapshots.version < EXCLUDED.version";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@AggregateId", aggregateId);
            command.Parameters.AddWithValue("@AggregateType", snapshot.GetType().Name);
            command.Parameters.AddWithValue("@Version", version);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@SnapshotData", JsonSerializer.Serialize(snapshot, _jsonOptions));

            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation(
                "Saved snapshot for aggregate {AggregateId} at version {Version}",
                aggregateId, version);
        }

        public async Task<EventSnapshot?> GetLatestSnapshotAsync(
            string aggregateId,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = @"
                SELECT aggregate_id, aggregate_type, version, created_at, snapshot_data
                FROM event_snapshots
                WHERE aggregate_id = @AggregateId
                ORDER BY version DESC
                LIMIT 1";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@AggregateId", aggregateId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                return new EventSnapshot
                {
                    AggregateId = reader.GetString(reader.GetOrdinal("aggregate_id")),
                    AggregateType = reader.GetString(reader.GetOrdinal("aggregate_type")),
                    Version = reader.GetInt64(reader.GetOrdinal("version")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    SerializedData = reader.GetString(reader.GetOrdinal("snapshot_data"))
                };
            }

            return null;
        }

        private async Task<long> GetAggregateVersionInternalAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction? transaction,
            string aggregateId,
            CancellationToken cancellationToken)
        {
            var sql = @"
                SELECT COALESCE(MAX(aggregate_version), 0)
                FROM event_store
                WHERE aggregate_id = @AggregateId";

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@AggregateId", aggregateId);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        }
    }

    /// <summary>
    /// Generic domain event for deserialization from event store
    /// </summary>
    internal class GenericDomainEvent : IDomainEvent
    {
        public GenericDomainEvent(
            Guid eventId,
            DateTime occurredAt,
            string aggregateId,
            string aggregateType,
            long aggregateVersion,
            string eventType,
            Guid? causationId,
            Guid? correlationId,
            string initiatedBy,
            string serializedData,
            IDictionary<string, object> metadata)
        {
            EventId = eventId;
            OccurredAt = occurredAt;
            AggregateId = aggregateId;
            AggregateType = aggregateType;
            AggregateVersion = aggregateVersion;
            EventType = eventType;
            CausationId = causationId;
            CorrelationId = correlationId;
            InitiatedBy = initiatedBy;
            SerializedData = serializedData;
            Metadata = metadata;
        }

        public Guid EventId { get; }
        public DateTime OccurredAt { get; }
        public string AggregateId { get; }
        public string AggregateType { get; }
        public long AggregateVersion { get; }
        public string EventType { get; }
        public Guid? CausationId { get; }
        public Guid? CorrelationId { get; }
        public string InitiatedBy { get; }
        public string SerializedData { get; }
        public IDictionary<string, object> Metadata { get; }
    }

    /// <summary>
    /// Exception thrown when optimistic concurrency control fails
    /// </summary>
    public class OptimisticConcurrencyException : Exception
    {
        public OptimisticConcurrencyException(string message) : base(message)
        {
        }

        public OptimisticConcurrencyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}