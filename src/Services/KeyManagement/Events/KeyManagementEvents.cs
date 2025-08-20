using System;
using System.Collections.Generic;
using NeoServiceLayer.Core.Events;
using NeoServiceLayer.ServiceFramework;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Services.KeyManagement.Events
{
    public class KeyGeneratedEvent : DomainEventBase
    {
        public KeyGeneratedEvent(
            string keyId,
            string keyType,
            string algorithm,
            string publicKey,
            bool hasPrivateKey,
            string generatedBy,
            DateTime? expiresAt,
            Dictionary<string, string>? metadata)
            : base(keyId, "CryptographicKey", 1, generatedBy)
        {
            KeyId = keyId;
            KeyType = keyType;
            Algorithm = algorithm;
            PublicKey = publicKey;
            HasPrivateKey = hasPrivateKey;
            ExpiresAt = expiresAt;
            Metadata = metadata;
        }

        public string KeyId { get; }
        public string KeyType { get; }
        public string Algorithm { get; }
        public string PublicKey { get; }
        public bool HasPrivateKey { get; }
        public DateTime? ExpiresAt { get; }
        public Dictionary<string, string>? Metadata { get; }
    }

    public class KeyActivatedEvent : DomainEventBase
    {
        public KeyActivatedEvent(string keyId, string activatedBy)
            : base(keyId, "CryptographicKey", 0, activatedBy)
        {
            KeyId = keyId;
        }

        public string KeyId { get; }
    }

    public class KeyRevokedEvent : DomainEventBase
    {
        public KeyRevokedEvent(string keyId, string revokedBy, string reason)
            : base(keyId, "CryptographicKey", 0, revokedBy)
        {
            KeyId = keyId;
            Reason = reason;
        }

        public string KeyId { get; }
        public string Reason { get; }
    }

    public class KeyRotatedEvent : DomainEventBase
    {
        public KeyRotatedEvent(
            string keyId,
            string oldPublicKey,
            string newPublicKey,
            bool hasNewPrivateKey,
            string rotatedBy)
            : base(keyId, "CryptographicKey", 0, rotatedBy)
        {
            KeyId = keyId;
            OldPublicKey = oldPublicKey;
            NewPublicKey = newPublicKey;
            HasNewPrivateKey = hasNewPrivateKey;
        }

        public string KeyId { get; }
        public string OldPublicKey { get; }
        public string NewPublicKey { get; }
        public bool HasNewPrivateKey { get; }
    }

    public class KeyUsedEvent : DomainEventBase
    {
        public KeyUsedEvent(string keyId, string usedBy, string operation)
            : base(keyId, "CryptographicKey", 0, usedBy)
        {
            KeyId = keyId;
            Operation = operation;
        }

        public string KeyId { get; }
        public string Operation { get; }
    }

    public class KeyAccessGrantedEvent : DomainEventBase
    {
        public KeyAccessGrantedEvent(string keyId, string userId, string grantedBy)
            : base(keyId, "CryptographicKey", 0, grantedBy)
        {
            KeyId = keyId;
            UserId = userId;
        }

        public string KeyId { get; }
        public string UserId { get; }
    }

    public class KeyAccessRevokedEvent : DomainEventBase
    {
        public KeyAccessRevokedEvent(string keyId, string userId, string revokedBy)
            : base(keyId, "CryptographicKey", 0, revokedBy)
        {
            KeyId = keyId;
            UserId = userId;
        }

        public string KeyId { get; }
        public string UserId { get; }
    }

    public class KeyMetadataUpdatedEvent : DomainEventBase
    {
        public KeyMetadataUpdatedEvent(
            string keyId,
            string key,
            string? oldValue,
            string? newValue,
            string updatedBy)
            : base(keyId, "CryptographicKey", 0, updatedBy)
        {
            KeyId = keyId;
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public string KeyId { get; }
        public string Key { get; }
        public string? OldValue { get; }
        public string? NewValue { get; }
    }
}