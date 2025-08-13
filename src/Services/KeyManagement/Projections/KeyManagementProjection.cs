using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Events;
using NeoServiceLayer.Infrastructure.CQRS.Projections;
using NeoServiceLayer.Services.KeyManagement.Events;
using NeoServiceLayer.Services.KeyManagement.QueryHandlers;
using NeoServiceLayer.Services.KeyManagement.ReadModels;

namespace NeoServiceLayer.Services.KeyManagement.Projections
{
    /// <summary>
    /// Projection that builds read models from key management events
    /// </summary>
    public class KeyManagementProjection : ProjectionBase
    {
        private readonly IKeyReadModelStore _store;
        private readonly ILogger<KeyManagementProjection> _logger;
        private long _currentPosition;

        public KeyManagementProjection(
            IKeyReadModelStore store,
            ILogger<KeyManagementProjection> logger)
            : base("KeyManagementProjection")
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task HandleAsync(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken = default)
        {
            if (!CanHandle(domainEvent))
                return;

            try
            {
                switch (domainEvent)
                {
                    case KeyGeneratedEvent e:
                        await HandleKeyGenerated(e, cancellationToken);
                        break;

                    case KeyActivatedEvent e:
                        await HandleKeyActivated(e, cancellationToken);
                        break;

                    case KeyRevokedEvent e:
                        await HandleKeyRevoked(e, cancellationToken);
                        break;

                    case KeyRotatedEvent e:
                        await HandleKeyRotated(e, cancellationToken);
                        break;

                    case KeyUsedEvent e:
                        await HandleKeyUsed(e, cancellationToken);
                        break;

                    case KeyAccessGrantedEvent e:
                        await HandleKeyAccessGranted(e, cancellationToken);
                        break;

                    case KeyAccessRevokedEvent e:
                        await HandleKeyAccessRevoked(e, cancellationToken);
                        break;

                    case KeyMetadataUpdatedEvent e:
                        await HandleKeyMetadataUpdated(e, cancellationToken);
                        break;

                    default:
                        _logger.LogWarning(
                            "Unhandled event type {EventType} in projection",
                            domainEvent.GetType().Name);
                        break;
                }

                // Update position after successful handling
                _currentPosition = domainEvent.AggregateVersion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to handle event {EventType} for aggregate {AggregateId}",
                    domainEvent.GetType().Name, domainEvent.AggregateId);
                throw;
            }
        }

        private async Task HandleKeyGenerated(KeyGeneratedEvent e, CancellationToken cancellationToken)
        {
            var model = new KeyReadModel
            {
                KeyId = e.KeyId,
                KeyType = e.KeyType,
                Algorithm = e.Algorithm,
                Status = "Created",
                PublicKey = e.PublicKey,
                HasPrivateKey = e.HasPrivateKey,
                CreatedAt = e.OccurredAt,
                CreatedBy = e.InitiatedBy,
                ExpiresAt = e.ExpiresAt,
                IsExpired = e.ExpiresAt.HasValue && e.ExpiresAt.Value <= DateTime.UtcNow,
                UsageCount = 0,
                AuthorizedUsers = new() { e.InitiatedBy },
                Metadata = e.Metadata ?? new(),
                Version = e.AggregateVersion
            };

            await _store.SaveAsync(model, cancellationToken);

            _logger.LogInformation(
                "Created read model for key {KeyId} of type {KeyType}",
                e.KeyId, e.KeyType);
        }

        private async Task HandleKeyActivated(KeyActivatedEvent e, CancellationToken cancellationToken)
        {
            var model = await _store.GetByIdAsync(e.KeyId, cancellationToken);
            if (model == null)
            {
                _logger.LogWarning("Read model not found for key {KeyId}", e.KeyId);
                return;
            }

            model.Status = "Active";
            model.ActivatedAt = e.OccurredAt;
            model.Version = e.AggregateVersion;

            await _store.UpdateAsync(model, cancellationToken);

            _logger.LogInformation("Activated key {KeyId} in read model", e.KeyId);
        }

        private async Task HandleKeyRevoked(KeyRevokedEvent e, CancellationToken cancellationToken)
        {
            var model = await _store.GetByIdAsync(e.KeyId, cancellationToken);
            if (model == null)
            {
                _logger.LogWarning("Read model not found for key {KeyId}", e.KeyId);
                return;
            }

            model.Status = "Revoked";
            model.RevokedAt = e.OccurredAt;
            model.RevocationReason = e.Reason;
            model.Version = e.AggregateVersion;

            await _store.UpdateAsync(model, cancellationToken);

            _logger.LogInformation(
                "Revoked key {KeyId} in read model with reason: {Reason}",
                e.KeyId, e.Reason);
        }

        private async Task HandleKeyRotated(KeyRotatedEvent e, CancellationToken cancellationToken)
        {
            var model = await _store.GetByIdAsync(e.KeyId, cancellationToken);
            if (model == null)
            {
                _logger.LogWarning("Read model not found for key {KeyId}", e.KeyId);
                return;
            }

            model.PublicKey = e.NewPublicKey;
            model.HasPrivateKey = e.HasNewPrivateKey;
            model.Version = e.AggregateVersion;

            await _store.UpdateAsync(model, cancellationToken);

            _logger.LogInformation("Rotated key {KeyId} in read model", e.KeyId);
        }

        private async Task HandleKeyUsed(KeyUsedEvent e, CancellationToken cancellationToken)
        {
            var model = await _store.GetByIdAsync(e.KeyId, cancellationToken);
            if (model == null)
            {
                _logger.LogWarning("Read model not found for key {KeyId}", e.KeyId);
                return;
            }

            model.UsageCount++;
            model.LastUsedAt = e.OccurredAt;
            model.Version = e.AggregateVersion;

            await _store.UpdateAsync(model, cancellationToken);

            _logger.LogDebug(
                "Updated usage count for key {KeyId} to {UsageCount}",
                e.KeyId, model.UsageCount);
        }

        private async Task HandleKeyAccessGranted(KeyAccessGrantedEvent e, CancellationToken cancellationToken)
        {
            var model = await _store.GetByIdAsync(e.KeyId, cancellationToken);
            if (model == null)
            {
                _logger.LogWarning("Read model not found for key {KeyId}", e.KeyId);
                return;
            }

            if (!model.AuthorizedUsers.Contains(e.UserId))
            {
                model.AuthorizedUsers.Add(e.UserId);
                model.Version = e.AggregateVersion;
                await _store.UpdateAsync(model, cancellationToken);
            }

            _logger.LogInformation(
                "Granted access to key {KeyId} for user {UserId} in read model",
                e.KeyId, e.UserId);
        }

        private async Task HandleKeyAccessRevoked(KeyAccessRevokedEvent e, CancellationToken cancellationToken)
        {
            var model = await _store.GetByIdAsync(e.KeyId, cancellationToken);
            if (model == null)
            {
                _logger.LogWarning("Read model not found for key {KeyId}", e.KeyId);
                return;
            }

            model.AuthorizedUsers.Remove(e.UserId);
            model.Version = e.AggregateVersion;

            await _store.UpdateAsync(model, cancellationToken);

            _logger.LogInformation(
                "Revoked access to key {KeyId} for user {UserId} in read model",
                e.KeyId, e.UserId);
        }

        private async Task HandleKeyMetadataUpdated(KeyMetadataUpdatedEvent e, CancellationToken cancellationToken)
        {
            var model = await _store.GetByIdAsync(e.KeyId, cancellationToken);
            if (model == null)
            {
                _logger.LogWarning("Read model not found for key {KeyId}", e.KeyId);
                return;
            }

            if (e.NewValue == null)
                model.Metadata.Remove(e.Key);
            else
                model.Metadata[e.Key] = e.NewValue;

            model.Version = e.AggregateVersion;

            await _store.UpdateAsync(model, cancellationToken);

            _logger.LogDebug(
                "Updated metadata key {Key} for key {KeyId} in read model",
                e.Key, e.KeyId);
        }

        public override async Task<long> GetPositionAsync(CancellationToken cancellationToken = default)
        {
            // In a real implementation, this would load from persistent storage
            return await Task.FromResult(_currentPosition);
        }

        public override async Task SavePositionAsync(long position, CancellationToken cancellationToken = default)
        {
            // In a real implementation, this would save to persistent storage
            _currentPosition = position;
            await Task.CompletedTask;
        }

        public override async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            // In a real implementation, this would clear the read model store
            _currentPosition = 0;
            await Task.CompletedTask;
            
            _logger.LogInformation("Reset projection {ProjectionName}", ProjectionName);
        }

        protected override Type[] GetHandledEventTypes()
        {
            return new[]
            {
                typeof(KeyGeneratedEvent),
                typeof(KeyActivatedEvent),
                typeof(KeyRevokedEvent),
                typeof(KeyRotatedEvent),
                typeof(KeyUsedEvent),
                typeof(KeyAccessGrantedEvent),
                typeof(KeyAccessRevokedEvent),
                typeof(KeyMetadataUpdatedEvent)
            };
        }
    }
}