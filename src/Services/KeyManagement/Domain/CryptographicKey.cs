using System;
using System.Collections.Generic;
using NeoServiceLayer.Core.Aggregates;
using NeoServiceLayer.Core.Events;
using NeoServiceLayer.ServiceFramework;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Services.KeyManagement.Domain
{
    /// <summary>
    /// Aggregate root for cryptographic keys
    /// </summary>
    public class CryptographicKey : AggregateRoot
    {
        private string _keyType = string.Empty;
        private string _algorithm = string.Empty;
        private KeyStatus _status = KeyStatus.Created;
        private string _publicKey = string.Empty;
        private string? _encryptedPrivateKey;
        private DateTime? _activatedAt;
        private DateTime? _revokedAt;
        private string? _revocationReason;
        private HashSet<string> _authorizedUsers = new();
        private int _usageCount;
        private DateTime? _lastUsedAt;
        private DateTime? _expiresAt;
        private Dictionary<string, string> _metadata = new();

        // For Entity Framework or serialization
        public CryptographicKey() : base()
        {
        }

        // Domain constructor
        public CryptographicKey(
            string keyId,
            string keyType,
            string algorithm,
            string publicKey,
            string? encryptedPrivateKey,
            string createdBy,
            DateTime? expiresAt = null,
            Dictionary<string, string>? metadata = null) : base()
        {
            if (string.IsNullOrWhiteSpace(keyId))
                throw new ArgumentException("Key ID cannot be empty", nameof(keyId));
            if (string.IsNullOrWhiteSpace(keyType))
                throw new ArgumentException("Key type cannot be empty", nameof(keyType));
            if (string.IsNullOrWhiteSpace(algorithm))
                throw new ArgumentException("Algorithm cannot be empty", nameof(algorithm));
            if (string.IsNullOrWhiteSpace(publicKey))
                throw new ArgumentException("Public key cannot be empty", nameof(publicKey));

            var @event = new KeyGeneratedEvent(
                keyId,
                keyType,
                algorithm,
                publicKey,
                encryptedPrivateKey != null,
                createdBy,
                expiresAt,
                metadata);

            RaiseEvent(@event);
        }

        // Public properties for queries
        public string KeyType => _keyType;
        public string Algorithm => _algorithm;
        public KeyStatus Status => _status;
        public string PublicKey => _publicKey;
        public DateTime? ActivatedAt => _activatedAt;
        public DateTime? RevokedAt => _revokedAt;
        public string? RevocationReason => _revocationReason;
        public IReadOnlyCollection<string> AuthorizedUsers => _authorizedUsers;
        public int UsageCount => _usageCount;
        public DateTime? LastUsedAt => _lastUsedAt;
        public DateTime? ExpiresAt => _expiresAt;
        public IReadOnlyDictionary<string, string> Metadata => _metadata;
        public bool IsExpired => _expiresAt.HasValue && _expiresAt.Value <= DateTime.UtcNow;

        // Domain methods
        public void Activate(string activatedBy)
        {
            if (_status != KeyStatus.Created)
                throw new InvalidOperationException($"Cannot activate key in {_status} status");

            if (IsExpired)
                throw new InvalidOperationException("Cannot activate expired key");

            RaiseEvent(new KeyActivatedEvent(Id, activatedBy));
        }

        public void Revoke(string revokedBy, string reason)
        {
            if (_status == KeyStatus.Revoked)
                throw new InvalidOperationException("Key is already revoked");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Revocation reason is required", nameof(reason));

            RaiseEvent(new KeyRevokedEvent(Id, revokedBy, reason));
        }

        public void Rotate(string newPublicKey, string? newEncryptedPrivateKey, string rotatedBy)
        {
            if (_status != KeyStatus.Active)
                throw new InvalidOperationException($"Cannot rotate key in {_status} status");

            if (string.IsNullOrWhiteSpace(newPublicKey))
                throw new ArgumentException("New public key is required", nameof(newPublicKey));

            RaiseEvent(new KeyRotatedEvent(
                Id,
                _publicKey,
                newPublicKey,
                newEncryptedPrivateKey != null,
                rotatedBy));
        }

        public void RecordUsage(string usedBy, string operation)
        {
            if (_status != KeyStatus.Active)
                throw new InvalidOperationException($"Cannot use key in {_status} status");

            if (IsExpired)
                throw new InvalidOperationException("Cannot use expired key");

            if (!_authorizedUsers.Contains(usedBy) && !_authorizedUsers.Contains("*"))
                throw new UnauthorizedAccessException($"User {usedBy} is not authorized to use this key");

            RaiseEvent(new KeyUsedEvent(Id, usedBy, operation));
        }

        public void GrantAccess(string userId, string grantedBy)
        {
            if (_status == KeyStatus.Revoked)
                throw new InvalidOperationException("Cannot grant access to revoked key");

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required", nameof(userId));

            if (_authorizedUsers.Contains(userId))
                return; // Already authorized

            RaiseEvent(new KeyAccessGrantedEvent(Id, userId, grantedBy));
        }

        public void RevokeAccess(string userId, string revokedBy)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required", nameof(userId));

            if (!_authorizedUsers.Contains(userId))
                return; // Not authorized

            RaiseEvent(new KeyAccessRevokedEvent(Id, userId, revokedBy));
        }

        public void UpdateMetadata(string key, string value, string updatedBy)
        {
            if (_status == KeyStatus.Revoked)
                throw new InvalidOperationException("Cannot update metadata of revoked key");

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Metadata key is required", nameof(key));

            var oldValue = _metadata.TryGetValue(key, out var existing) ? existing : null;
            if (oldValue == value)
                return; // No change

            RaiseEvent(new KeyMetadataUpdatedEvent(Id, key, oldValue, value, updatedBy));
        }

        // Event handlers
        protected override void RegisterEventHandlers()
        {
            RegisterHandler<KeyGeneratedEvent>(Apply);
            RegisterHandler<KeyActivatedEvent>(Apply);
            RegisterHandler<KeyRevokedEvent>(Apply);
            RegisterHandler<KeyRotatedEvent>(Apply);
            RegisterHandler<KeyUsedEvent>(Apply);
            RegisterHandler<KeyAccessGrantedEvent>(Apply);
            RegisterHandler<KeyAccessRevokedEvent>(Apply);
            RegisterHandler<KeyMetadataUpdatedEvent>(Apply);
        }

        private void Apply(KeyGeneratedEvent @event)
        {
            Id = @event.KeyId;
            _keyType = @event.KeyType;
            _algorithm = @event.Algorithm;
            _publicKey = @event.PublicKey;
            _encryptedPrivateKey = @event.HasPrivateKey ? "encrypted" : null;
            _status = KeyStatus.Created;
            _expiresAt = @event.ExpiresAt;
            _metadata = @event.Metadata ?? new Dictionary<string, string>();
            _authorizedUsers = new HashSet<string> { @event.InitiatedBy };
            CreatedAt = @event.OccurredAt;
            CreatedBy = @event.InitiatedBy;
        }

        private void Apply(KeyActivatedEvent @event)
        {
            _status = KeyStatus.Active;
            _activatedAt = @event.OccurredAt;
        }

        private void Apply(KeyRevokedEvent @event)
        {
            _status = KeyStatus.Revoked;
            _revokedAt = @event.OccurredAt;
            _revocationReason = @event.Reason;
        }

        private void Apply(KeyRotatedEvent @event)
        {
            _publicKey = @event.NewPublicKey;
            _encryptedPrivateKey = @event.HasNewPrivateKey ? "encrypted" : null;
        }

        private void Apply(KeyUsedEvent @event)
        {
            _usageCount++;
            _lastUsedAt = @event.OccurredAt;
        }

        private void Apply(KeyAccessGrantedEvent @event)
        {
            _authorizedUsers.Add(@event.UserId);
        }

        private void Apply(KeyAccessRevokedEvent @event)
        {
            _authorizedUsers.Remove(@event.UserId);
        }

        private void Apply(KeyMetadataUpdatedEvent @event)
        {
            if (@event.NewValue == null)
                _metadata.Remove(@event.Key);
            else
                _metadata[@event.Key] = @event.NewValue;
        }

        protected override void ValidateInvariants()
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new InvalidOperationException("Key ID is required");

            if (string.IsNullOrWhiteSpace(_keyType))
                throw new InvalidOperationException("Key type is required");

            if (string.IsNullOrWhiteSpace(_algorithm))
                throw new InvalidOperationException("Algorithm is required");

            if (string.IsNullOrWhiteSpace(_publicKey))
                throw new InvalidOperationException("Public key is required");

            if (_status == KeyStatus.Active && _activatedAt == null)
                throw new InvalidOperationException("Active key must have activation date");

            if (_status == KeyStatus.Revoked && string.IsNullOrWhiteSpace(_revocationReason))
                throw new InvalidOperationException("Revoked key must have revocation reason");
        }

        protected override object GetSnapshotData()
        {
            return new
            {
                base.GetSnapshotData(),
                KeyType = _keyType,
                Algorithm = _algorithm,
                Status = _status,
                PublicKey = _publicKey,
                EncryptedPrivateKey = _encryptedPrivateKey,
                ActivatedAt = _activatedAt,
                RevokedAt = _revokedAt,
                RevocationReason = _revocationReason,
                AuthorizedUsers = _authorizedUsers.ToList(),
                UsageCount = _usageCount,
                LastUsedAt = _lastUsedAt,
                ExpiresAt = _expiresAt,
                Metadata = _metadata
            };
        }
    }

    public enum KeyStatus
    {
        Created,
        Active,
        Revoked,
        Expired
    }
}