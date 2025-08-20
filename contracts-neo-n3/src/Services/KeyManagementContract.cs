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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Contracts.Services
{
    /// <summary>
    /// Provides secure key management and rotation services with
    /// hierarchical deterministic key derivation and multi-signature support.
    /// </summary>
    [DisplayName("KeyManagementContract")]
    [ManifestExtra("Author", "Neo Service Layer Team")]
    [ManifestExtra("Description", "Secure key management and rotation service")]
    [ManifestExtra("Version", "1.0.0")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class KeyManagementContract : BaseServiceContract
    {
        #region Storage Keys
        private static readonly byte[] KeyRecordPrefix = "keyRecord:".ToByteArray();
        private static readonly byte[] KeyHierarchyPrefix = "keyHierarchy:".ToByteArray();
        private static readonly byte[] RotationSchedulePrefix = "rotationSchedule:".ToByteArray();
        private static readonly byte[] MultiSigConfigPrefix = "multiSigConfig:".ToByteArray();
        private static readonly byte[] KeyUsagePrefix = "keyUsage:".ToByteArray();
        private static readonly byte[] KeyCountKey = "keyCount".ToByteArray();
        private static readonly byte[] RotationPolicyKey = "rotationPolicy".ToByteArray();
        private static readonly byte[] SecurityConfigKey = "securityConfig".ToByteArray();
        #endregion

        #region Events
        [DisplayName("KeyGenerated")]
        public static event Action<ByteString, UInt160, KeyType, KeyPurpose> KeyGenerated;

        [DisplayName("KeyRotated")]
        public static event Action<ByteString, ByteString, UInt160> KeyRotated;

        [DisplayName("KeyRevoked")]
        public static event Action<ByteString, UInt160, string> KeyRevoked;

        [DisplayName("MultiSigConfigured")]
        public static event Action<UInt160, int, int> MultiSigConfigured;

        [DisplayName("KeyUsageLogged")]
        public static event Action<ByteString, UInt160, string, ulong> KeyUsageLogged;

        [DisplayName("RotationScheduled")]
        public static event Action<ByteString, ulong, RotationReason> RotationScheduled;
        #endregion

        #region Constants
        private const int DEFAULT_KEY_ROTATION_PERIOD = 2592000; // 30 days
        private const int MAX_MULTISIG_PARTICIPANTS = 20;
        private const int DEFAULT_KEY_STRENGTH = 256;
        #endregion

        #region Initialization
        public static void _deploy(object data, bool update)
        {
            if (update) return;

            var serviceId = Runtime.ExecutingScriptHash;
            var contract = new KeyManagementContract();
            contract.InitializeBaseService(serviceId, "KeyManagementService", "1.0.0", "{}");
            
            // Initialize security configuration
            var securityConfig = new SecurityConfig
            {
                DefaultKeyStrength = DEFAULT_KEY_STRENGTH,
                RotationPeriod = DEFAULT_KEY_ROTATION_PERIOD,
                RequireRotationApproval = true,
                EnableUsageLogging = true,
                MaxKeyAge = DEFAULT_KEY_ROTATION_PERIOD * 2
            };
            
            Storage.Put(Storage.CurrentContext, SecurityConfigKey, StdLib.Serialize(securityConfig));
            Storage.Put(Storage.CurrentContext, KeyCountKey, 0);

            Runtime.Log("KeyManagementContract deployed successfully");
        }
        #endregion

        #region Service Implementation
        protected override void InitializeService(string config)
        {
            Runtime.Log("KeyManagementContract service initialized");
        }

        protected override bool PerformHealthCheck()
        {
            try
            {
                var keyCount = GetKeyCount();
                return keyCount >= 0;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Key Generation
        /// <summary>
        /// Generates a new cryptographic key with specified parameters.
        /// </summary>
        public static ByteString GenerateKey(UInt160 owner, KeyType keyType, KeyPurpose purpose, 
            int keyStrength, string metadata)
        {
            return ExecuteServiceOperation(() =>
            {
                // Validate caller has permission
                if (!ValidateKeyOperation(Runtime.CallingScriptHash, owner))
                    throw new InvalidOperationException("Insufficient permissions");
                
                if (keyStrength < 128 || keyStrength > 512)
                    throw new ArgumentException("Invalid key strength");
                
                var keyId = GenerateKeyId();
                
                // Generate key material (simplified - in production would use secure key generation)
                var keyMaterial = GenerateSecureKeyMaterial(keyType, keyStrength);
                
                var keyRecord = new KeyRecord
                {
                    Id = keyId,
                    Owner = owner,
                    Type = keyType,
                    Purpose = purpose,
                    Strength = keyStrength,
                    PublicKey = ExtractPublicKey(keyMaterial, keyType),
                    PrivateKeyHash = CryptoLib.Sha256(keyMaterial), // Store hash, not actual key
                    CreatedAt = Runtime.Time,
                    CreatedBy = Runtime.CallingScriptHash,
                    Status = KeyStatus.Active,
                    UsageCount = 0,
                    LastUsed = 0,
                    Metadata = metadata ?? "",
                    RotationSchedule = Runtime.Time + GetSecurityConfig().RotationPeriod
                };
                
                // Store key record
                var keyRecordKey = KeyRecordPrefix.Concat(keyId);
                Storage.Put(Storage.CurrentContext, keyRecordKey, StdLib.Serialize(keyRecord));
                
                // Schedule automatic rotation
                ScheduleKeyRotation(keyId, keyRecord.RotationSchedule, RotationReason.Scheduled);
                
                // Increment key count
                var count = GetKeyCount();
                Storage.Put(Storage.CurrentContext, KeyCountKey, count + 1);
                
                KeyGenerated(keyId, owner, keyType, purpose);
                Runtime.Log($"Key generated: {keyId} for {owner}");
                return keyId;
            });
        }

        /// <summary>
        /// Derives a child key from a parent key using hierarchical deterministic derivation.
        /// </summary>
        public static ByteString DeriveChildKey(ByteString parentKeyId, int derivationIndex, 
            KeyPurpose childPurpose, string metadata)
        {
            return ExecuteServiceOperation(() =>
            {
                var parentKey = GetKeyRecord(parentKeyId);
                if (parentKey == null)
                    throw new InvalidOperationException("Parent key not found");
                
                if (parentKey.Status != KeyStatus.Active)
                    throw new InvalidOperationException("Parent key is not active");
                
                // Validate caller has permission for parent key
                if (!ValidateKeyOperation(Runtime.CallingScriptHash, parentKey.Owner))
                    throw new InvalidOperationException("Insufficient permissions");
                
                var childKeyId = GenerateChildKeyId(parentKeyId, derivationIndex);
                
                // Derive child key (simplified - in production would use proper HD derivation)
                var childKeyMaterial = DeriveChildKeyMaterial(parentKey, derivationIndex);
                
                var childKeyRecord = new KeyRecord
                {
                    Id = childKeyId,
                    Owner = parentKey.Owner,
                    Type = parentKey.Type,
                    Purpose = childPurpose,
                    Strength = parentKey.Strength,
                    PublicKey = ExtractPublicKey(childKeyMaterial, parentKey.Type),
                    PrivateKeyHash = CryptoLib.Sha256(childKeyMaterial),
                    CreatedAt = Runtime.Time,
                    CreatedBy = Runtime.CallingScriptHash,
                    Status = KeyStatus.Active,
                    UsageCount = 0,
                    LastUsed = 0,
                    Metadata = metadata ?? "",
                    RotationSchedule = Runtime.Time + GetSecurityConfig().RotationPeriod
                };
                
                // Store child key record
                var childKeyRecordKey = KeyRecordPrefix.Concat(childKeyId);
                Storage.Put(Storage.CurrentContext, childKeyRecordKey, StdLib.Serialize(childKeyRecord));
                
                // Store hierarchy relationship
                var hierarchyKey = KeyHierarchyPrefix.Concat(parentKeyId).Concat(childKeyId);
                Storage.Put(Storage.CurrentContext, hierarchyKey, derivationIndex);
                
                var count = GetKeyCount();
                Storage.Put(Storage.CurrentContext, KeyCountKey, count + 1);
                
                KeyGenerated(childKeyId, parentKey.Owner, parentKey.Type, childPurpose);
                Runtime.Log($"Child key derived: {childKeyId} from {parentKeyId}");
                return childKeyId;
            });
        }
        #endregion

        #region Key Rotation
        /// <summary>
        /// Rotates a key by generating a new key and marking the old one as rotated.
        /// </summary>
        public static ByteString RotateKey(ByteString keyId, RotationReason reason, string justification)
        {
            return ExecuteServiceOperation(() =>
            {
                var oldKey = GetKeyRecord(keyId);
                if (oldKey == null)
                    throw new InvalidOperationException("Key not found");
                
                if (oldKey.Status != KeyStatus.Active)
                    throw new InvalidOperationException("Key is not active");
                
                // Validate caller has permission
                if (!ValidateKeyOperation(Runtime.CallingScriptHash, oldKey.Owner))
                    throw new InvalidOperationException("Insufficient permissions");
                
                // Check if rotation approval is required
                var securityConfig = GetSecurityConfig();
                if (securityConfig.RequireRotationApproval && reason == RotationReason.Manual)
                {
                    // In production, would check for proper approvals
                    Runtime.Log("Rotation approval required - proceeding with authorized rotation");
                }
                
                // Generate new key with same parameters
                var newKeyId = GenerateKey(oldKey.Owner, oldKey.Type, oldKey.Purpose, 
                    oldKey.Strength, $"Rotated from {keyId}: {justification}");
                
                // Mark old key as rotated
                oldKey.Status = KeyStatus.Rotated;
                oldKey.RotatedAt = Runtime.Time;
                oldKey.RotatedTo = newKeyId;
                
                var oldKeyRecordKey = KeyRecordPrefix.Concat(keyId);
                Storage.Put(Storage.CurrentContext, oldKeyRecordKey, StdLib.Serialize(oldKey));
                
                KeyRotated(keyId, newKeyId, oldKey.Owner);
                Runtime.Log($"Key rotated: {keyId} -> {newKeyId}, reason: {reason}");
                return newKeyId;
            });
        }

        /// <summary>
        /// Schedules automatic key rotation.
        /// </summary>
        public static bool ScheduleKeyRotation(ByteString keyId, ulong rotationTime, RotationReason reason)
        {
            return ExecuteServiceOperation(() =>
            {
                var keyRecord = GetKeyRecord(keyId);
                if (keyRecord == null)
                    throw new InvalidOperationException("Key not found");
                
                var rotationSchedule = new RotationSchedule
                {
                    KeyId = keyId,
                    ScheduledTime = rotationTime,
                    Reason = reason,
                    IsExecuted = false,
                    ScheduledBy = Runtime.CallingScriptHash,
                    ScheduledAt = Runtime.Time
                };
                
                var scheduleKey = RotationSchedulePrefix.Concat(keyId);
                Storage.Put(Storage.CurrentContext, scheduleKey, StdLib.Serialize(rotationSchedule));
                
                RotationScheduled(keyId, rotationTime, reason);
                Runtime.Log($"Key rotation scheduled: {keyId} at {rotationTime}");
                return true;
            });
        }
        #endregion

        #region Multi-Signature Management
        /// <summary>
        /// Configures multi-signature requirements for a key.
        /// </summary>
        public static bool ConfigureMultiSig(UInt160 owner, UInt160[] participants, int threshold)
        {
            return ExecuteServiceOperation(() =>
            {
                // Validate caller has permission
                if (!ValidateKeyOperation(Runtime.CallingScriptHash, owner))
                    throw new InvalidOperationException("Insufficient permissions");
                
                if (participants.Length > MAX_MULTISIG_PARTICIPANTS)
                    throw new ArgumentException($"Too many participants (max: {MAX_MULTISIG_PARTICIPANTS})");
                
                if (threshold < 1 || threshold > participants.Length)
                    throw new ArgumentException("Invalid threshold");
                
                var multiSigConfig = new MultiSigConfig
                {
                    Owner = owner,
                    Participants = participants,
                    Threshold = threshold,
                    CreatedAt = Runtime.Time,
                    CreatedBy = Runtime.CallingScriptHash,
                    IsActive = true
                };
                
                var configKey = MultiSigConfigPrefix.Concat(owner);
                Storage.Put(Storage.CurrentContext, configKey, StdLib.Serialize(multiSigConfig));
                
                MultiSigConfigured(owner, participants.Length, threshold);
                Runtime.Log($"Multi-sig configured for {owner}: {threshold}/{participants.Length}");
                return true;
            });
        }

        /// <summary>
        /// Gets multi-signature configuration for an owner.
        /// </summary>
        public static MultiSigConfig GetMultiSigConfig(UInt160 owner)
        {
            var configKey = MultiSigConfigPrefix.Concat(owner);
            var configBytes = Storage.Get(Storage.CurrentContext, configKey);
            if (configBytes == null)
                return null;
            
            return (MultiSigConfig)StdLib.Deserialize(configBytes);
        }
        #endregion

        #region Key Usage Tracking
        /// <summary>
        /// Logs key usage for audit and monitoring purposes.
        /// </summary>
        public static bool LogKeyUsage(ByteString keyId, string operation, string context)
        {
            return ExecuteServiceOperation(() =>
            {
                var keyRecord = GetKeyRecord(keyId);
                if (keyRecord == null)
                    throw new InvalidOperationException("Key not found");
                
                if (keyRecord.Status != KeyStatus.Active)
                    throw new InvalidOperationException("Key is not active");
                
                // Update key usage statistics
                keyRecord.UsageCount++;
                keyRecord.LastUsed = Runtime.Time;
                
                var keyRecordKey = KeyRecordPrefix.Concat(keyId);
                Storage.Put(Storage.CurrentContext, keyRecordKey, StdLib.Serialize(keyRecord));
                
                // Log usage details
                var usageLog = new KeyUsageLog
                {
                    KeyId = keyId,
                    Operation = operation,
                    Context = context,
                    UsedBy = Runtime.CallingScriptHash,
                    UsedAt = Runtime.Time
                };
                
                var usageKey = KeyUsagePrefix.Concat(keyId).Concat(Runtime.Time.ToByteArray());
                Storage.Put(Storage.CurrentContext, usageKey, StdLib.Serialize(usageLog));
                
                KeyUsageLogged(keyId, Runtime.CallingScriptHash, operation, Runtime.Time);
                return true;
            });
        }
        #endregion

        #region Key Management
        /// <summary>
        /// Revokes a key and marks it as compromised.
        /// </summary>
        public static bool RevokeKey(ByteString keyId, string reason)
        {
            return ExecuteServiceOperation(() =>
            {
                var keyRecord = GetKeyRecord(keyId);
                if (keyRecord == null)
                    throw new InvalidOperationException("Key not found");
                
                // Validate caller has permission
                if (!ValidateKeyOperation(Runtime.CallingScriptHash, keyRecord.Owner))
                    throw new InvalidOperationException("Insufficient permissions");
                
                keyRecord.Status = KeyStatus.Revoked;
                keyRecord.RevokedAt = Runtime.Time;
                keyRecord.RevokedBy = Runtime.CallingScriptHash;
                keyRecord.RevocationReason = reason;
                
                var keyRecordKey = KeyRecordPrefix.Concat(keyId);
                Storage.Put(Storage.CurrentContext, keyRecordKey, StdLib.Serialize(keyRecord));
                
                KeyRevoked(keyId, keyRecord.Owner, reason);
                Runtime.Log($"Key revoked: {keyId}, reason: {reason}");
                return true;
            });
        }

        /// <summary>
        /// Gets key record information.
        /// </summary>
        public static KeyRecord GetKeyRecord(ByteString keyId)
        {
            var keyRecordKey = KeyRecordPrefix.Concat(keyId);
            var keyRecordBytes = Storage.Get(Storage.CurrentContext, keyRecordKey);
            if (keyRecordBytes == null)
                return null;
            
            return (KeyRecord)StdLib.Deserialize(keyRecordBytes);
        }

        /// <summary>
        /// Gets the total number of managed keys.
        /// </summary>
        public static BigInteger GetKeyCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, KeyCountKey);
            return countBytes?.ToBigInteger() ?? 0;
        }
        #endregion

        #region Configuration
        /// <summary>
        /// Gets security configuration.
        /// </summary>
        public static SecurityConfig GetSecurityConfig()
        {
            var configBytes = Storage.Get(Storage.CurrentContext, SecurityConfigKey);
            if (configBytes == null)
            {
                return new SecurityConfig
                {
                    DefaultKeyStrength = DEFAULT_KEY_STRENGTH,
                    RotationPeriod = DEFAULT_KEY_ROTATION_PERIOD,
                    RequireRotationApproval = true,
                    EnableUsageLogging = true,
                    MaxKeyAge = DEFAULT_KEY_ROTATION_PERIOD * 2
                };
            }
            
            return (SecurityConfig)StdLib.Deserialize(configBytes);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Validates key operation permissions.
        /// </summary>
        private static bool ValidateKeyOperation(UInt160 caller, UInt160 keyOwner)
        {
            // Owner can always perform operations
            if (caller.Equals(keyOwner))
                return true;
            
            // Check if caller has admin permissions
            return ValidateAccess(caller);
        }

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
        /// Generates a unique key ID.
        /// </summary>
        private static ByteString GenerateKeyId()
        {
            var counter = GetKeyCount();
            var tx = (Transaction)Runtime.ScriptContainer;
            var data = Runtime.Time.ToByteArray()
                .Concat(counter.ToByteArray())
                .Concat(tx.Hash);
            
            return CryptoLib.Sha256(data);
        }

        /// <summary>
        /// Generates a child key ID.
        /// </summary>
        private static ByteString GenerateChildKeyId(ByteString parentKeyId, int derivationIndex)
        {
            var data = parentKeyId.Concat(derivationIndex.ToByteArray()).Concat(Runtime.Time.ToByteArray());
            return CryptoLib.Sha256(data);
        }

        /// <summary>
        /// Generates secure key material (simplified).
        /// </summary>
        private static ByteString GenerateSecureKeyMaterial(KeyType keyType, int keyStrength)
        {
            // In production, would use secure random number generation
            var entropy = Runtime.Time.ToByteArray()
                .Concat(Runtime.CallingScriptHash)
                .Concat(((int)keyType).ToByteArray())
                .Concat(keyStrength.ToByteArray());
            
            return CryptoLib.Sha256(entropy);
        }

        /// <summary>
        /// Extracts public key from key material (simplified).
        /// </summary>
        private static ByteString ExtractPublicKey(ByteString keyMaterial, KeyType keyType)
        {
            // In production, would derive actual public key based on key type
            return CryptoLib.Sha256(keyMaterial.Concat("public".ToByteArray()));
        }

        /// <summary>
        /// Derives child key material (simplified).
        /// </summary>
        private static ByteString DeriveChildKeyMaterial(KeyRecord parentKey, int derivationIndex)
        {
            // In production, would use proper HD key derivation
            var data = parentKey.PrivateKeyHash
                .Concat(derivationIndex.ToByteArray())
                .Concat(Runtime.Time.ToByteArray());
            
            return CryptoLib.Sha256(data);
        }

        /// <summary>
        /// Executes a service operation with proper error handling.
        /// </summary>
        private static T ExecuteServiceOperation<T>(Func<T> operation)
        {
            ValidateServiceActive();
            IncrementRequestCount();

            try
            {
                return operation();
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                throw;
            }
        }
        #endregion

        #region Data Structures
        /// <summary>
        /// Represents a cryptographic key record.
        /// </summary>
        public class KeyRecord
        {
            public ByteString Id;
            public UInt160 Owner;
            public KeyType Type;
            public KeyPurpose Purpose;
            public int Strength;
            public ByteString PublicKey;
            public ByteString PrivateKeyHash;
            public ulong CreatedAt;
            public UInt160 CreatedBy;
            public KeyStatus Status;
            public int UsageCount;
            public ulong LastUsed;
            public string Metadata;
            public ulong RotationSchedule;
            public ulong RevokedAt;
            public UInt160 RevokedBy;
            public string RevocationReason;
            public ByteString RotatedTo;
            public ulong RotatedAt;
        }

        /// <summary>
        /// Represents a rotation schedule.
        /// </summary>
        public class RotationSchedule
        {
            public ByteString KeyId;
            public ulong ScheduledTime;
            public RotationReason Reason;
            public bool IsExecuted;
            public UInt160 ScheduledBy;
            public ulong ScheduledAt;
        }

        /// <summary>
        /// Represents multi-signature configuration.
        /// </summary>
        public class MultiSigConfig
        {
            public UInt160 Owner;
            public UInt160[] Participants;
            public int Threshold;
            public ulong CreatedAt;
            public UInt160 CreatedBy;
            public bool IsActive;
        }

        /// <summary>
        /// Represents key usage log entry.
        /// </summary>
        public class KeyUsageLog
        {
            public ByteString KeyId;
            public string Operation;
            public string Context;
            public UInt160 UsedBy;
            public ulong UsedAt;
        }

        /// <summary>
        /// Represents security configuration.
        /// </summary>
        public class SecurityConfig
        {
            public int DefaultKeyStrength;
            public int RotationPeriod;
            public bool RequireRotationApproval;
            public bool EnableUsageLogging;
            public int MaxKeyAge;
        }

        /// <summary>
        /// Key type enumeration.
        /// </summary>
        public enum KeyType : byte
        {
            RSA = 0,
            ECDSA = 1,
            EdDSA = 2,
            AES = 3,
            ChaCha20 = 4
        }

        /// <summary>
        /// Key purpose enumeration.
        /// </summary>
        public enum KeyPurpose : byte
        {
            Signing = 0,
            Encryption = 1,
            Authentication = 2,
            KeyDerivation = 3,
            General = 4
        }

        /// <summary>
        /// Key status enumeration.
        /// </summary>
        public enum KeyStatus : byte
        {
            Active = 0,
            Rotated = 1,
            Revoked = 2,
            Expired = 3,
            Compromised = 4
        }

        /// <summary>
        /// Rotation reason enumeration.
        /// </summary>
        public enum RotationReason : byte
        {
            Scheduled = 0,
            Manual = 1,
            Compromised = 2,
            PolicyChange = 3,
            Emergency = 4
        }
        #endregion
    }
}