using System;
using System.Collections.Generic;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.ServiceFramework;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Services.KeyManagement.Commands
{
    public class GenerateKeyCommand : CommandBase<string>
    {
        public GenerateKeyCommand(
            string keyType,
            string algorithm,
            int keySize,
            string requestedBy,
            DateTime? expiresAt = null,
            Dictionary<string, string>? metadata = null)
            : base(requestedBy)
        {
            KeyType = keyType ?? throw new ArgumentNullException(nameof(keyType));
            Algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
            KeySize = keySize;
            ExpiresAt = expiresAt;
            Metadata = metadata ?? new Dictionary<string, string>();
        }

        public string KeyType { get; }
        public string Algorithm { get; }
        public int KeySize { get; }
        public DateTime? ExpiresAt { get; }
        public Dictionary<string, string> Metadata { get; }
    }

    public class ActivateKeyCommand : CommandBase
    {
        public ActivateKeyCommand(string keyId, string activatedBy)
            : base(activatedBy)
        {
            KeyId = keyId ?? throw new ArgumentNullException(nameof(keyId));
        }

        public string KeyId { get; }
    }

    public class RevokeKeyCommand : CommandBase
    {
        public RevokeKeyCommand(string keyId, string reason, string revokedBy)
            : base(revokedBy)
        {
            KeyId = keyId ?? throw new ArgumentNullException(nameof(keyId));
            Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        }

        public string KeyId { get; }
        public string Reason { get; }
    }

    public class RotateKeyCommand : CommandBase<string>
    {
        public RotateKeyCommand(string keyId, string rotatedBy)
            : base(rotatedBy)
        {
            KeyId = keyId ?? throw new ArgumentNullException(nameof(keyId));
        }

        public string KeyId { get; }
    }

    public class SignDataCommand : CommandBase<string>
    {
        public SignDataCommand(string keyId, byte[] data, string signedBy)
            : base(signedBy)
        {
            KeyId = keyId ?? throw new ArgumentNullException(nameof(keyId));
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public string KeyId { get; }
        public byte[] Data { get; }
    }

    public class VerifySignatureCommand : CommandBase<bool>
    {
        public VerifySignatureCommand(
            string keyId,
            byte[] data,
            string signature,
            string verifiedBy)
            : base(verifiedBy)
        {
            KeyId = keyId ?? throw new ArgumentNullException(nameof(keyId));
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Signature = signature ?? throw new ArgumentNullException(nameof(signature));
        }

        public string KeyId { get; }
        public byte[] Data { get; }
        public string Signature { get; }
    }

    public class GrantKeyAccessCommand : CommandBase
    {
        public GrantKeyAccessCommand(string keyId, string userId, string grantedBy)
            : base(grantedBy)
        {
            KeyId = keyId ?? throw new ArgumentNullException(nameof(keyId));
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        }

        public string KeyId { get; }
        public string UserId { get; }
    }

    public class RevokeKeyAccessCommand : CommandBase
    {
        public RevokeKeyAccessCommand(string keyId, string userId, string revokedBy)
            : base(revokedBy)
        {
            KeyId = keyId ?? throw new ArgumentNullException(nameof(keyId));
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        }

        public string KeyId { get; }
        public string UserId { get; }
    }
}