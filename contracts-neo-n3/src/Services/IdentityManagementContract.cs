using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using NeoServiceLayer.Contracts.Core;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NeoServiceLayer.Contracts.Services
{
    /// <summary>
    /// Provides comprehensive identity management services with
    /// decentralized identity (DID), verifiable credentials, and reputation systems.
    /// </summary>
    [DisplayName("IdentityManagementContract")]
    [ManifestExtra("Author", "Neo Service Layer Team")]
    [ManifestExtra("Description", "Decentralized identity management and verification service")]
    [ManifestExtra("Version", "1.0.0")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class IdentityManagementContract : BaseServiceContract
    {
        #region Storage Keys
        private static readonly byte[] IdentityPrefix = "identity:".ToByteArray();
        private static readonly byte[] CredentialPrefix = "credential:".ToByteArray();
        private static readonly byte[] ReputationPrefix = "reputation:".ToByteArray();
        private static readonly byte[] VerifierPrefix = "verifier:".ToByteArray();
        private static readonly byte[] SchemaPrefix = "schema:".ToByteArray();
        private static readonly byte[] IdentityCountKey = "identityCount".ToByteArray();
        private static readonly byte[] CredentialCountKey = "credentialCount".ToByteArray();
        private static readonly byte[] VerifierCountKey = "verifierCount".ToByteArray();
        private static readonly byte[] IdentityConfigKey = "identityConfig".ToByteArray();
        #endregion

        #region Events
        [DisplayName("IdentityCreated")]
        public static event Action<UInt160, ByteString, string> IdentityCreated;

        [DisplayName("CredentialIssued")]
        public static event Action<ByteString, UInt160, UInt160, string> CredentialIssued;

        [DisplayName("CredentialVerified")]
        public static event Action<ByteString, UInt160, bool> CredentialVerified;

        [DisplayName("ReputationUpdated")]
        public static event Action<UInt160, int, string> ReputationUpdated;

        [DisplayName("VerifierRegistered")]
        public static event Action<UInt160, string, VerifierType> VerifierRegistered;

        [DisplayName("IdentityLinked")]
        public static event Action<UInt160, UInt160, string> IdentityLinked;
        #endregion

        #region Constants
        private const int MAX_CREDENTIALS_PER_IDENTITY = 100;
        private const int MAX_VERIFIERS = 1000;
        private const int DEFAULT_REPUTATION_SCORE = 50;
        private const int MAX_REPUTATION_SCORE = 100;
        private const int MIN_REPUTATION_SCORE = 0;
        #endregion

        #region Initialization
        public static void _deploy(object data, bool update)
        {
            if (update) return;

            var serviceId = Runtime.ExecutingScriptHash;
            var contract = new IdentityManagementContract();
            contract.InitializeBaseService(serviceId, "IdentityManagementService", "1.0.0", "{}");
            
            // Initialize identity configuration
            var identityConfig = new IdentityConfig
            {
                RequireVerification = true,
                EnableReputation = true,
                DefaultReputationScore = DEFAULT_REPUTATION_SCORE,
                MaxCredentialsPerIdentity = MAX_CREDENTIALS_PER_IDENTITY,
                CredentialExpiryPeriod = 31536000, // 1 year
                EnableSelfSovereignIdentity = true
            };
            
            Storage.Put(Storage.CurrentContext, IdentityConfigKey, StdLib.Serialize(identityConfig));
            Storage.Put(Storage.CurrentContext, IdentityCountKey, 0);
            Storage.Put(Storage.CurrentContext, CredentialCountKey, 0);
            Storage.Put(Storage.CurrentContext, VerifierCountKey, 0);

            Runtime.Log("IdentityManagementContract deployed successfully");
        }
        #endregion

        #region Service Implementation
        protected override void InitializeService(string config)
        {
            Runtime.Log("IdentityManagementContract service initialized");
        }

        protected override bool PerformHealthCheck()
        {
            try
            {
                var identityCount = GetIdentityCount();
                var verifierCount = GetVerifierCount();
                return identityCount >= 0 && verifierCount >= 0;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Identity Management
        /// <summary>
        /// Creates a new decentralized identity (DID).
        /// </summary>
        public static ByteString CreateIdentity(UInt160 owner, string didDocument, 
            IdentityType identityType, string metadata)
        {
            return ExecuteServiceOperation(() =>
            {
                // Validate caller has permission
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                if (string.IsNullOrEmpty(didDocument))
                    throw new ArgumentException("DID document cannot be empty");
                
                var identityId = GenerateIdentityId();
                
                var identity = new Identity
                {
                    Id = identityId,
                    Owner = owner,
                    DidDocument = didDocument,
                    Type = identityType,
                    Status = IdentityStatus.Active,
                    CreatedAt = Runtime.Time,
                    CreatedBy = Runtime.CallingScriptHash,
                    UpdatedAt = Runtime.Time,
                    Metadata = metadata ?? "",
                    CredentialCount = 0,
                    ReputationScore = GetIdentityConfig().DefaultReputationScore,
                    VerificationLevel = VerificationLevel.Basic
                };
                
                // Store identity
                var identityKey = IdentityPrefix.Concat(identityId);
                Storage.Put(Storage.CurrentContext, identityKey, StdLib.Serialize(identity));
                
                // Initialize reputation
                InitializeReputation(identityId, owner);
                
                // Increment identity count
                var count = GetIdentityCount();
                Storage.Put(Storage.CurrentContext, IdentityCountKey, count + 1);
                
                IdentityCreated(owner, identityId, didDocument);
                Runtime.Log($"Identity created: {identityId} for {owner}");
                return identityId;
            });
        }

        /// <summary>
        /// Updates an existing identity's DID document.
        /// </summary>
        public static bool UpdateIdentity(ByteString identityId, string didDocument, string metadata)
        {
            return ExecuteServiceOperation(() =>
            {
                var identity = GetIdentity(identityId);
                if (identity == null)
                    throw new InvalidOperationException("Identity not found");
                
                // Validate caller is owner or has permission
                if (!identity.Owner.Equals(Runtime.CallingScriptHash) && !ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                if (!string.IsNullOrEmpty(didDocument))
                    identity.DidDocument = didDocument;
                
                if (metadata != null)
                    identity.Metadata = metadata;
                
                identity.UpdatedAt = Runtime.Time;
                
                var identityKey = IdentityPrefix.Concat(identityId);
                Storage.Put(Storage.CurrentContext, identityKey, StdLib.Serialize(identity));
                
                Runtime.Log($"Identity updated: {identityId}");
                return true;
            });
        }

        /// <summary>
        /// Links two identities together (e.g., personal and business identities).
        /// </summary>
        public static bool LinkIdentities(ByteString primaryIdentityId, ByteString secondaryIdentityId, 
            string linkType)
        {
            return ExecuteServiceOperation(() =>
            {
                var primaryIdentity = GetIdentity(primaryIdentityId);
                var secondaryIdentity = GetIdentity(secondaryIdentityId);
                
                if (primaryIdentity == null || secondaryIdentity == null)
                    throw new InvalidOperationException("One or both identities not found");
                
                // Validate caller owns both identities or has permission
                if ((!primaryIdentity.Owner.Equals(Runtime.CallingScriptHash) || 
                     !secondaryIdentity.Owner.Equals(Runtime.CallingScriptHash)) && 
                    !ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                var linkKey = IdentityPrefix.Concat(primaryIdentityId).Concat("link:".ToByteArray()).Concat(secondaryIdentityId);
                var linkData = new IdentityLink
                {
                    PrimaryIdentity = primaryIdentityId,
                    SecondaryIdentity = secondaryIdentityId,
                    LinkType = linkType,
                    CreatedAt = Runtime.Time,
                    CreatedBy = Runtime.CallingScriptHash,
                    IsActive = true
                };
                
                Storage.Put(Storage.CurrentContext, linkKey, StdLib.Serialize(linkData));
                
                IdentityLinked(primaryIdentity.Owner, secondaryIdentity.Owner, linkType);
                Runtime.Log($"Identities linked: {primaryIdentityId} -> {secondaryIdentityId}");
                return true;
            });
        }
        #endregion

        #region Credential Management
        /// <summary>
        /// Issues a verifiable credential to an identity.
        /// </summary>
        public static ByteString IssueCredential(ByteString identityId, UInt160 issuer, 
            string credentialType, string credentialData, ulong expiryTime)
        {
            return ExecuteServiceOperation(() =>
            {
                var identity = GetIdentity(identityId);
                if (identity == null)
                    throw new InvalidOperationException("Identity not found");
                
                // Validate issuer is registered verifier
                if (!IsRegisteredVerifier(issuer))
                    throw new InvalidOperationException("Issuer is not a registered verifier");
                
                if (identity.CredentialCount >= GetIdentityConfig().MaxCredentialsPerIdentity)
                    throw new InvalidOperationException("Maximum credentials per identity exceeded");
                
                var credentialId = GenerateCredentialId();
                
                var credential = new VerifiableCredential
                {
                    Id = credentialId,
                    IdentityId = identityId,
                    Issuer = issuer,
                    Type = credentialType,
                    Data = credentialData,
                    IssuedAt = Runtime.Time,
                    ExpiresAt = expiryTime,
                    Status = CredentialStatus.Active,
                    VerificationCount = 0,
                    LastVerified = 0,
                    Signature = GenerateCredentialSignature(credentialId, issuer, credentialData)
                };
                
                // Store credential
                var credentialKey = CredentialPrefix.Concat(credentialId);
                Storage.Put(Storage.CurrentContext, credentialKey, StdLib.Serialize(credential));
                
                // Update identity credential count
                identity.CredentialCount++;
                var identityKey = IdentityPrefix.Concat(identityId);
                Storage.Put(Storage.CurrentContext, identityKey, StdLib.Serialize(identity));
                
                // Increment credential count
                var count = GetCredentialCount();
                Storage.Put(Storage.CurrentContext, CredentialCountKey, count + 1);
                
                CredentialIssued(credentialId, identity.Owner, issuer, credentialType);
                Runtime.Log($"Credential issued: {credentialId} to {identityId}");
                return credentialId;
            });
        }

        /// <summary>
        /// Verifies a credential's authenticity and validity.
        /// </summary>
        public static bool VerifyCredential(ByteString credentialId, UInt160 verifier)
        {
            return ExecuteServiceOperation(() =>
            {
                var credential = GetCredential(credentialId);
                if (credential == null)
                    throw new InvalidOperationException("Credential not found");
                
                // Check if credential is expired
                if (credential.ExpiresAt > 0 && Runtime.Time > credential.ExpiresAt)
                {
                    CredentialVerified(credentialId, verifier, false);
                    return false;
                }
                
                // Check if credential is active
                if (credential.Status != CredentialStatus.Active)
                {
                    CredentialVerified(credentialId, verifier, false);
                    return false;
                }
                
                // Verify signature (simplified - in production would use proper cryptographic verification)
                var expectedSignature = GenerateCredentialSignature(credentialId, credential.Issuer, credential.Data);
                var isValid = credential.Signature.Equals(expectedSignature);
                
                // Update verification statistics
                credential.VerificationCount++;
                credential.LastVerified = Runtime.Time;
                
                var credentialKey = CredentialPrefix.Concat(credentialId);
                Storage.Put(Storage.CurrentContext, credentialKey, StdLib.Serialize(credential));
                
                CredentialVerified(credentialId, verifier, isValid);
                Runtime.Log($"Credential verified: {credentialId} - {isValid}");
                return isValid;
            });
        }

        /// <summary>
        /// Revokes a credential.
        /// </summary>
        public static bool RevokeCredential(ByteString credentialId, string reason)
        {
            return ExecuteServiceOperation(() =>
            {
                var credential = GetCredential(credentialId);
                if (credential == null)
                    throw new InvalidOperationException("Credential not found");
                
                // Validate caller is issuer or has permission
                if (!credential.Issuer.Equals(Runtime.CallingScriptHash) && !ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                credential.Status = CredentialStatus.Revoked;
                credential.RevokedAt = Runtime.Time;
                credential.RevokedBy = Runtime.CallingScriptHash;
                credential.RevocationReason = reason;
                
                var credentialKey = CredentialPrefix.Concat(credentialId);
                Storage.Put(Storage.CurrentContext, credentialKey, StdLib.Serialize(credential));
                
                Runtime.Log($"Credential revoked: {credentialId}, reason: {reason}");
                return true;
            });
        }
        #endregion

        #region Verifier Management
        /// <summary>
        /// Registers a new credential verifier.
        /// </summary>
        public static bool RegisterVerifier(UInt160 verifierAddress, string name, 
            VerifierType verifierType, string metadata)
        {
            return ExecuteServiceOperation(() =>
            {
                // Validate caller has permission
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                if (GetVerifierCount() >= MAX_VERIFIERS)
                    throw new InvalidOperationException("Maximum verifiers exceeded");
                
                var verifier = new CredentialVerifier
                {
                    Address = verifierAddress,
                    Name = name,
                    Type = verifierType,
                    Status = VerifierStatus.Active,
                    RegisteredAt = Runtime.Time,
                    RegisteredBy = Runtime.CallingScriptHash,
                    Metadata = metadata ?? "",
                    CredentialsIssued = 0,
                    ReputationScore = DEFAULT_REPUTATION_SCORE
                };
                
                var verifierKey = VerifierPrefix.Concat(verifierAddress);
                Storage.Put(Storage.CurrentContext, verifierKey, StdLib.Serialize(verifier));
                
                var count = GetVerifierCount();
                Storage.Put(Storage.CurrentContext, VerifierCountKey, count + 1);
                
                VerifierRegistered(verifierAddress, name, verifierType);
                Runtime.Log($"Verifier registered: {verifierAddress} - {name}");
                return true;
            });
        }

        /// <summary>
        /// Checks if an address is a registered verifier.
        /// </summary>
        public static bool IsRegisteredVerifier(UInt160 verifierAddress)
        {
            var verifierKey = VerifierPrefix.Concat(verifierAddress);
            var verifierBytes = Storage.Get(Storage.CurrentContext, verifierKey);
            if (verifierBytes == null)
                return false;
            
            var verifier = (CredentialVerifier)StdLib.Deserialize(verifierBytes);
            return verifier.Status == VerifierStatus.Active;
        }
        #endregion

        #region Reputation System
        /// <summary>
        /// Updates an identity's reputation score.
        /// </summary>
        public static bool UpdateReputation(ByteString identityId, int scoreChange, string reason)
        {
            return ExecuteServiceOperation(() =>
            {
                var identity = GetIdentity(identityId);
                if (identity == null)
                    throw new InvalidOperationException("Identity not found");
                
                // Validate caller has permission
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                var newScore = identity.ReputationScore + scoreChange;
                if (newScore > MAX_REPUTATION_SCORE)
                    newScore = MAX_REPUTATION_SCORE;
                if (newScore < MIN_REPUTATION_SCORE)
                    newScore = MIN_REPUTATION_SCORE;
                
                identity.ReputationScore = newScore;
                identity.UpdatedAt = Runtime.Time;
                
                var identityKey = IdentityPrefix.Concat(identityId);
                Storage.Put(Storage.CurrentContext, identityKey, StdLib.Serialize(identity));
                
                // Log reputation change
                var reputationKey = ReputationPrefix.Concat(identityId).Concat(Runtime.Time.ToByteArray());
                var reputationEntry = new ReputationEntry
                {
                    IdentityId = identityId,
                    PreviousScore = identity.ReputationScore - scoreChange,
                    NewScore = newScore,
                    Change = scoreChange,
                    Reason = reason,
                    UpdatedBy = Runtime.CallingScriptHash,
                    UpdatedAt = Runtime.Time
                };
                
                Storage.Put(Storage.CurrentContext, reputationKey, StdLib.Serialize(reputationEntry));
                
                ReputationUpdated(identity.Owner, newScore, reason);
                Runtime.Log($"Reputation updated: {identityId} -> {newScore}");
                return true;
            });
        }

        /// <summary>
        /// Gets an identity's reputation score.
        /// </summary>
        public static int GetReputationScore(ByteString identityId)
        {
            var identity = GetIdentity(identityId);
            return identity?.ReputationScore ?? 0;
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Gets identity information.
        /// </summary>
        public static Identity GetIdentity(ByteString identityId)
        {
            var identityKey = IdentityPrefix.Concat(identityId);
            var identityBytes = Storage.Get(Storage.CurrentContext, identityKey);
            if (identityBytes == null)
                return null;
            
            return (Identity)StdLib.Deserialize(identityBytes);
        }

        /// <summary>
        /// Gets credential information.
        /// </summary>
        public static VerifiableCredential GetCredential(ByteString credentialId)
        {
            var credentialKey = CredentialPrefix.Concat(credentialId);
            var credentialBytes = Storage.Get(Storage.CurrentContext, credentialKey);
            if (credentialBytes == null)
                return null;
            
            return (VerifiableCredential)StdLib.Deserialize(credentialBytes);
        }

        /// <summary>
        /// Gets verifier information.
        /// </summary>
        public static CredentialVerifier GetVerifier(UInt160 verifierAddress)
        {
            var verifierKey = VerifierPrefix.Concat(verifierAddress);
            var verifierBytes = Storage.Get(Storage.CurrentContext, verifierKey);
            if (verifierBytes == null)
                return null;
            
            return (CredentialVerifier)StdLib.Deserialize(verifierBytes);
        }

        /// <summary>
        /// Gets identity count.
        /// </summary>
        public static BigInteger GetIdentityCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, IdentityCountKey);
            return countBytes?.ToBigInteger() ?? 0;
        }

        /// <summary>
        /// Gets credential count.
        /// </summary>
        public static BigInteger GetCredentialCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, CredentialCountKey);
            return countBytes?.ToBigInteger() ?? 0;
        }

        /// <summary>
        /// Gets verifier count.
        /// </summary>
        public static BigInteger GetVerifierCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, VerifierCountKey);
            return countBytes?.ToBigInteger() ?? 0;
        }
        #endregion

        #region Configuration
        /// <summary>
        /// Gets identity configuration.
        /// </summary>
        public static IdentityConfig GetIdentityConfig()
        {
            var configBytes = Storage.Get(Storage.CurrentContext, IdentityConfigKey);
            if (configBytes == null)
            {
                return new IdentityConfig
                {
                    RequireVerification = true,
                    EnableReputation = true,
                    DefaultReputationScore = DEFAULT_REPUTATION_SCORE,
                    MaxCredentialsPerIdentity = MAX_CREDENTIALS_PER_IDENTITY,
                    CredentialExpiryPeriod = 31536000,
                    EnableSelfSovereignIdentity = true
                };
            }
            
            return (IdentityConfig)StdLib.Deserialize(configBytes);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Validates access permissions for the caller.
        /// </summary>
        private static bool ValidateAccess(UInt160 caller)
        {
            // Check if service is active first
            var activeBytes = Storage.Get(Storage.CurrentContext, ServiceActiveKey);
            if (activeBytes == null || activeBytes[0] != 1)
                return false;

            // For now, allow access if service is active
            // In production, would integrate with ServiceRegistry for role-based access
            return true;
        }

        /// <summary>
        /// Generates a unique identity ID.
        /// </summary>
        private static ByteString GenerateIdentityId()
        {
            var counter = GetIdentityCount();
            var tx = (Transaction)Runtime.ScriptContainer;
            var data = "identity".ToByteArray()
                .Concat(Runtime.Time.ToByteArray())
                .Concat(counter.ToByteArray())
                .Concat(tx.Hash);
            
            return CryptoLib.Sha256(data);
        }

        /// <summary>
        /// Generates a unique credential ID.
        /// </summary>
        private static ByteString GenerateCredentialId()
        {
            var counter = GetCredentialCount();
            var tx = (Transaction)Runtime.ScriptContainer;
            var data = "credential".ToByteArray()
                .Concat(Runtime.Time.ToByteArray())
                .Concat(counter.ToByteArray())
                .Concat(tx.Hash);
            
            return CryptoLib.Sha256(data);
        }

        /// <summary>
        /// Generates a credential signature (simplified).
        /// </summary>
        private static ByteString GenerateCredentialSignature(ByteString credentialId, UInt160 issuer, string data)
        {
            // In production, would use proper cryptographic signing
            var signatureData = credentialId.Concat(issuer).Concat(data.ToByteArray());
            return CryptoLib.Sha256(signatureData);
        }

        /// <summary>
        /// Initializes reputation for a new identity.
        /// </summary>
        private static void InitializeReputation(ByteString identityId, UInt160 owner)
        {
            var reputationKey = ReputationPrefix.Concat(identityId).Concat(Runtime.Time.ToByteArray());
            var reputationEntry = new ReputationEntry
            {
                IdentityId = identityId,
                PreviousScore = 0,
                NewScore = DEFAULT_REPUTATION_SCORE,
                Change = DEFAULT_REPUTATION_SCORE,
                Reason = "Initial reputation score",
                UpdatedBy = Runtime.CallingScriptHash,
                UpdatedAt = Runtime.Time
            };
            
            Storage.Put(Storage.CurrentContext, reputationKey, StdLib.Serialize(reputationEntry));
        }
        #endregion

        #region Data Structures
        /// <summary>
        /// Represents a decentralized identity.
        /// </summary>
        public class Identity
        {
            public ByteString Id;
            public UInt160 Owner;
            public string DidDocument;
            public IdentityType Type;
            public IdentityStatus Status;
            public ulong CreatedAt;
            public UInt160 CreatedBy;
            public ulong UpdatedAt;
            public string Metadata;
            public int CredentialCount;
            public int ReputationScore;
            public VerificationLevel VerificationLevel;
        }

        /// <summary>
        /// Represents a verifiable credential.
        /// </summary>
        public class VerifiableCredential
        {
            public ByteString Id;
            public ByteString IdentityId;
            public UInt160 Issuer;
            public string Type;
            public string Data;
            public ulong IssuedAt;
            public ulong ExpiresAt;
            public CredentialStatus Status;
            public int VerificationCount;
            public ulong LastVerified;
            public ByteString Signature;
            public ulong RevokedAt;
            public UInt160 RevokedBy;
            public string RevocationReason;
        }

        /// <summary>
        /// Represents a credential verifier.
        /// </summary>
        public class CredentialVerifier
        {
            public UInt160 Address;
            public string Name;
            public VerifierType Type;
            public VerifierStatus Status;
            public ulong RegisteredAt;
            public UInt160 RegisteredBy;
            public string Metadata;
            public int CredentialsIssued;
            public int ReputationScore;
        }

        /// <summary>
        /// Represents an identity link.
        /// </summary>
        public class IdentityLink
        {
            public ByteString PrimaryIdentity;
            public ByteString SecondaryIdentity;
            public string LinkType;
            public ulong CreatedAt;
            public UInt160 CreatedBy;
            public bool IsActive;
        }

        /// <summary>
        /// Represents a reputation entry.
        /// </summary>
        public class ReputationEntry
        {
            public ByteString IdentityId;
            public int PreviousScore;
            public int NewScore;
            public int Change;
            public string Reason;
            public UInt160 UpdatedBy;
            public ulong UpdatedAt;
        }

        /// <summary>
        /// Represents identity configuration.
        /// </summary>
        public class IdentityConfig
        {
            public bool RequireVerification;
            public bool EnableReputation;
            public int DefaultReputationScore;
            public int MaxCredentialsPerIdentity;
            public int CredentialExpiryPeriod;
            public bool EnableSelfSovereignIdentity;
        }

        /// <summary>
        /// Identity type enumeration.
        /// </summary>
        public enum IdentityType : byte
        {
            Individual = 0,
            Organization = 1,
            Device = 2,
            Service = 3,
            Government = 4
        }

        /// <summary>
        /// Identity status enumeration.
        /// </summary>
        public enum IdentityStatus : byte
        {
            Active = 0,
            Suspended = 1,
            Revoked = 2,
            Pending = 3
        }

        /// <summary>
        /// Verification level enumeration.
        /// </summary>
        public enum VerificationLevel : byte
        {
            Basic = 0,
            Enhanced = 1,
            Premium = 2,
            Enterprise = 3
        }

        /// <summary>
        /// Credential status enumeration.
        /// </summary>
        public enum CredentialStatus : byte
        {
            Active = 0,
            Expired = 1,
            Revoked = 2,
            Suspended = 3
        }

        /// <summary>
        /// Verifier type enumeration.
        /// </summary>
        public enum VerifierType : byte
        {
            Government = 0,
            Educational = 1,
            Financial = 2,
            Healthcare = 3,
            Corporate = 4,
            Community = 5
        }

        /// <summary>
        /// Verifier status enumeration.
        /// </summary>
        public enum VerifierStatus : byte
        {
            Active = 0,
            Suspended = 1,
            Revoked = 2,
            Pending = 3
        }
        #endregion
    }
}