using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NeoServiceLayer.Contracts.ProductionReady
{
    /// <summary>
    /// Production-ready secure storage contract for Neo N3
    /// Features: Access control, audit trails, versioning, encryption support
    /// </summary>
    [DisplayName("SecureStorageContract")]
    [ManifestExtra("Author", "Neo Service Layer Team")]
    [ManifestExtra("Description", "Enterprise-grade secure storage with access control")]
    [ManifestExtra("Version", "1.0.0")]
    [ManifestExtra("License", "MIT")]
    [ContractPermission("*", "*")]
    public class SecureStorageContract : SmartContract
    {
        #region Constants
        private const byte PERMISSION_READ = 1;
        private const byte PERMISSION_WRITE = 2;
        private const byte PERMISSION_DELETE = 4;
        private const byte PERMISSION_ADMIN = 8;

        private const int MAX_KEY_LENGTH = 64;
        private const int MAX_VALUE_SIZE = 1048576; // 1MB
        private const int MAX_METADATA_SIZE = 1024;

        private static readonly UInt160 ZERO_HASH = UInt160.Zero;
        #endregion

        #region Storage Keys
        private static readonly ByteString OWNER_KEY = "owner";
        private static readonly ByteString PAUSED_KEY = "paused";
        private static readonly ByteString ITEM_COUNT_KEY = "itemCount";
        private static readonly ByteString VERSION_KEY = "version";

        private static readonly ByteString ITEM_PREFIX = "item:";
        private static readonly ByteString META_PREFIX = "meta:";
        private static readonly ByteString ACCESS_PREFIX = "access:";
        private static readonly ByteString AUDIT_PREFIX = "audit:";
        #endregion

        #region Events
        [DisplayName("ContractOwnershipTransferred")]
        public static event Action<UInt160, UInt160> ContractOwnershipTransferred;

        [DisplayName("ContractPaused")]
        public static event Action<UInt160, bool> ContractPaused;

        [DisplayName("ItemStored")]
        public static event Action<UInt160, ByteString, int, bool> ItemStored;

        [DisplayName("ItemRetrieved")]
        public static event Action<UInt160, ByteString, UInt160> ItemRetrieved;

        [DisplayName("ItemDeleted")]
        public static event Action<UInt160, ByteString, UInt160> ItemDeleted;

        [DisplayName("PermissionGranted")]
        public static event Action<UInt160, UInt160, ByteString, byte> PermissionGranted;

        [DisplayName("PermissionRevoked")]
        public static event Action<UInt160, UInt160, ByteString, byte> PermissionRevoked;

        [DisplayName("AuditEvent")]
        public static event Action<UInt160, string, ByteString, string> AuditEvent;
        #endregion

        #region Data Structures
        public class StorageItem
        {
            public ByteString Key;
            public UInt160 Owner;
            public int Size;
            public bool IsEncrypted;
            public ulong CreatedAt;
            public ulong ModifiedAt;
            public UInt160 ModifiedBy;
            public int Version;
            public string Checksum;
        }

        public class ItemMetadata
        {
            public string ContentType;
            public string Description;
            public string Tags;
            public bool IsPublic;
            public ulong ExpiresAt;
        }
        #endregion

        #region Initialization
        public static void _deploy(object data, bool update)
        {
            if (update)
            {
                // Handle contract updates
                var currentVersion = GetVersion();
                if (currentVersion < 1)
                {
                    Storage.Put(Storage.CurrentContext, VERSION_KEY, 1);
                }
                Runtime.Log("SecureStorageContract updated successfully");
                return;
            }

            // Initial deployment
            var deployer = (UInt160)data ?? Runtime.CallingScriptHash;

            if (deployer == null || deployer.IsZero)
                throw new InvalidOperationException("Invalid deployer address");

            Storage.Put(Storage.CurrentContext, OWNER_KEY, deployer);
            Storage.Put(Storage.CurrentContext, PAUSED_KEY, (byte)0);
            Storage.Put(Storage.CurrentContext, ITEM_COUNT_KEY, 0);
            Storage.Put(Storage.CurrentContext, VERSION_KEY, 1);

            ContractOwnershipTransferred(UInt160.Zero, deployer);
            Runtime.Log("SecureStorageContract deployed successfully");
        }
        #endregion

        #region Access Control
        [Safe]
        public static UInt160 GetOwner()
        {
            var ownerBytes = Storage.Get(Storage.CurrentContext, OWNER_KEY);
            return ownerBytes != null ? (UInt160)ownerBytes : UInt160.Zero;
        }

        public static bool TransferOwnership(UInt160 newOwner)
        {
            ValidateNotPaused();

            if (newOwner == null || newOwner.IsZero)
                throw new ArgumentException("Invalid new owner address");

            var currentOwner = GetOwner();
            if (!Runtime.CheckWitness(currentOwner))
                throw new UnauthorizedAccessException("Only owner can transfer ownership");

            Storage.Put(Storage.CurrentContext, OWNER_KEY, newOwner);
            ContractOwnershipTransferred(currentOwner, newOwner);

            LogAuditEvent("OWNERSHIP_TRANSFERRED", newOwner, $"Ownership transferred to {newOwner}");
            return true;
        }

        public static bool SetPaused(bool paused)
        {
            ValidateOwner();

            Storage.Put(Storage.CurrentContext, PAUSED_KEY, paused ? (byte)1  : (byte)0);
            ContractPaused(Runtime.CallingScriptHash, paused);

            LogAuditEvent("CONTRACT_PAUSED", Runtime.CallingScriptHash, $"Contract paused: {paused}");
            return true;
        }

        [Safe]
        public static bool IsPaused()
        {
            var pausedBytes = Storage.Get(Storage.CurrentContext, PAUSED_KEY);
            return pausedBytes != null && pausedBytes[0] == 1;
        }

        public static bool GrantPermission(UInt160 user, ByteString key, byte permissions)
        {
            ValidateNotPaused();

            if (user == null || user.IsZero)
                throw new ArgumentException("Invalid user address");

            if (key == null || key.Length == 0 || key.Length > MAX_KEY_LENGTH)
                throw new ArgumentException("Invalid key");

            if (permissions == 0 || permissions > 15)
                throw new ArgumentException("Invalid permissions");

            var caller = Runtime.CallingScriptHash;
            if (!HasPermission(caller, key, PERMISSION_ADMIN) && !IsOwner(caller))
                throw new UnauthorizedAccessException("Insufficient permissions");

            var accessKey = ACCESS_PREFIX + key + user;
            Storage.Put(Storage.CurrentContext, accessKey, permissions);

            PermissionGranted(caller, user, key, permissions);
            LogAuditEvent("PERMISSION_GRANTED", user, $"Granted permissions {permissions} for key {key}");

            return true;
        }

        public static bool RevokePermission(UInt160 user, ByteString key)
        {
            ValidateNotPaused();

            if (user == null || user.IsZero)
                throw new ArgumentException("Invalid user address");

            if (key == null || key.Length == 0)
                throw new ArgumentException("Invalid key");

            var caller = Runtime.CallingScriptHash;
            if (!HasPermission(caller, key, PERMISSION_ADMIN) && !IsOwner(caller))
                throw new UnauthorizedAccessException("Insufficient permissions");

            var accessKey = ACCESS_PREFIX + key + user;
            Storage.Delete(Storage.CurrentContext, accessKey);

            PermissionRevoked(caller, user, key, 0);
            LogAuditEvent("PERMISSION_REVOKED", user, $"Revoked permissions for key {key}");

            return true;
        }

        [Safe]
        public static byte GetPermissions(UInt160 user, ByteString key)
        {
            if (user == null || user.IsZero || key == null || key.Length == 0)
                return 0;

            if (IsOwner(user))
                return PERMISSION_READ | PERMISSION_WRITE | PERMISSION_DELETE | PERMISSION_ADMIN;

            var accessKey = ACCESS_PREFIX + key + user;
            var permissionBytes = Storage.Get(Storage.CurrentContext, accessKey);
            return permissionBytes != null ? permissionBytes[0]  : (byte)0;
        }

        [Safe]
        public static bool HasPermission(UInt160 user, ByteString key, byte requiredPermission)
        {
            var userPermissions = GetPermissions(user, key);
            return (userPermissions & requiredPermission) == requiredPermission;
        }

        private static bool IsOwner(UInt160 address)
        {
            return GetOwner().Equals(address);
        }

        private static void ValidateOwner()
        {
            if (!Runtime.CheckWitness(GetOwner()))
                throw new UnauthorizedAccessException("Only contract owner can perform this action");
        }

        private static void ValidateNotPaused()
        {
            if (IsPaused())
                throw new InvalidOperationException("Contract is currently paused");
        }
        #endregion

        #region Storage Operations
        public static bool StoreItem(ByteString key, ByteString value, ItemMetadata metadata, bool isEncrypted)
        {
            ValidateNotPaused();

            if (key == null || key.Length == 0 || key.Length > MAX_KEY_LENGTH)
                throw new ArgumentException("Invalid key length");

            if (value == null || value.Length == 0 || value.Length > MAX_VALUE_SIZE)
                throw new ArgumentException("Invalid value size");

            if (metadata == null)
                throw new ArgumentException("Metadata is required");

            var caller = Runtime.CallingScriptHash;
            var itemKey = ITEM_PREFIX + key;
            var existingItem = Storage.Get(Storage.CurrentContext, itemKey);

            // Check permissions for updates
            if (existingItem != null)
            {
                if (!HasPermission(caller, key, PERMISSION_WRITE))
                    throw new UnauthorizedAccessException("Write permission required");
            }
            else
            {
                // New item - caller becomes owner with full permissions
                GrantPermission(caller, key, PERMISSION_READ | PERMISSION_WRITE | PERMISSION_DELETE | PERMISSION_ADMIN);
            }

            // Create storage item
            var item = new StorageItem
            {
                Key = key,
                Owner = caller,
                Size = value.Length,
                IsEncrypted = isEncrypted,
                CreatedAt = existingItem == null ? Runtime.Time  : ((StorageItem)StdLib.Deserialize(existingItem)).CreatedAt,
                ModifiedAt = Runtime.Time,
                ModifiedBy = caller,
                Version = existingItem == null ? 1 : ((StorageItem)StdLib.Deserialize(existingItem)).Version + 1;
                Checksum = CalculateChecksum(value);

                return Key = key,
                Owner = caller,
                Size = value.Length,
                IsEncrypted = isEncrypted,
                CreatedAt = existingItem == null ? Runtime.Time  : ((StorageItem)StdLib.Deserialize(existingItem)).CreatedAt,
                ModifiedAt = Runtime.Time,
                ModifiedBy = caller,
                Version = existingItem == null ? 1 : ((StorageItem)StdLib.Deserialize(existingItem)).Version + 1;
                Checksum = CalculateChecksum(value);
}
            // Store item and metadata
            Storage.Put(Storage.CurrentContext, itemKey, StdLib.Serialize(item));
            Storage.Put(Storage.CurrentContext, META_PREFIX + key, StdLib.Serialize(metadata));

            // Store the actual data
            Storage.Put(Storage.CurrentContext, "data:" + key, value);

            // Update item count if new item
            if (existingItem == null);
            {
                var count = GetItemCount();
                Storage.Put(Storage.CurrentContext, ITEM_COUNT_KEY, count + 1);
            }

            ItemStored(caller, key, value.Length, isEncrypted);
            LogAuditEvent("ITEM_STORED", key, $"Item stored/updated by {caller}");

            return true;
        }

        [Safe]
        public static ByteString RetrieveItem(ByteString key)
        {
            if (key == null || key.Length == 0)
                throw new ArgumentException("Invalid key");

            var caller = Runtime.CallingScriptHash;
            if (!HasPermission(caller, key, PERMISSION_READ))
                throw new UnauthorizedAccessException("Read permission required");

            var dataKey = "data:" + key;
            var value = Storage.Get(Storage.CurrentContext, dataKey);

            if (value == null)
                return null;

            ItemRetrieved(caller, key, caller);
            LogAuditEvent("ITEM_RETRIEVED", key, $"Item retrieved by {caller}");

            return value;
        }

        public static bool DeleteItem(ByteString key)
        {
            ValidateNotPaused();

            if (key == null || key.Length == 0)
                throw new ArgumentException("Invalid key");

            var caller = Runtime.CallingScriptHash;
            if (!HasPermission(caller, key, PERMISSION_DELETE))
                throw new UnauthorizedAccessException("Delete permission required");

            var itemKey = ITEM_PREFIX + key;
            var existingItem = Storage.Get(Storage.CurrentContext, itemKey);

            if (existingItem == null)
                throw new InvalidOperationException("Item not found");

            // Delete all related data
            Storage.Delete(Storage.CurrentContext, itemKey);
            Storage.Delete(Storage.CurrentContext, META_PREFIX + key);
            Storage.Delete(Storage.CurrentContext, "data:" + key);

            // Update item count
            var count = GetItemCount();
            Storage.Put(Storage.CurrentContext, ITEM_COUNT_KEY, count - 1);

            ItemDeleted(caller, key, caller);
            LogAuditEvent("ITEM_DELETED", key, $"Item deleted by {caller}");

            return true;
        }

        [Safe]
        public static StorageItem GetItemInfo(ByteString key)
        {
            if (key == null || key.Length == 0)
                return null;

            var caller = Runtime.CallingScriptHash;
            if (!HasPermission(caller, key, PERMISSION_READ))
                return null;

            var itemKey = ITEM_PREFIX + key;
            var itemBytes = Storage.Get(Storage.CurrentContext, itemKey);

            return itemBytes != null ? (StorageItem)StdLib.Deserialize(itemBytes) : null;
        }

        [Safe]
        public static ItemMetadata GetItemMetadata(ByteString key)
        {
            if (key == null || key.Length == 0)
                return null;

            var caller = Runtime.CallingScriptHash;
            if (!HasPermission(caller, key, PERMISSION_READ))
                return null;

            var metaKey = META_PREFIX + key;
            var metaBytes = Storage.Get(Storage.CurrentContext, metaKey);

            return metaBytes != null ? (ItemMetadata)StdLib.Deserialize(metaBytes) : null;
        }

        [Safe]
        public static bool ItemExists(ByteString key)
        {
            if (key == null || key.Length == 0)
                return false;

            var itemKey = ITEM_PREFIX + key;
            return Storage.Get(Storage.CurrentContext, itemKey) != null;
        }

        [Safe]
        public static int GetItemCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, ITEM_COUNT_KEY);
            return (int)(countBytes != null ? (BigInteger)countBytes : 0;
        }

        [Safe]
        public static int GetVersion()
        {
            var versionBytes = Storage.Get(Storage.CurrentContext, VERSION_KEY);
            return (int)(versionBytes != null ? (BigInteger)versionBytes : 0;
        }
        #endregion

        #region Utility Methods
        private static string CalculateChecksum(ByteString data)
        {
            var hash = CryptoLib.Sha256(data);
            return StdLib.Base64Encode(hash);
        }

        private static void LogAuditEvent(string action, ByteString target, string details)
        {
            var caller = Runtime.CallingScriptHash;
            var auditKey = AUDIT_PREFIX + Runtime.Time + caller;
            var auditData = action + "|" + details + "|" + Runtime.Time;

            Storage.Put(Storage.CurrentContext, auditKey, auditData);
            AuditEvent(caller, action, target, details);
        }

        [Safe]
        public static string GetContractInfo()
        {
            return "SecureStorageContract v1.0.0 - Enterprise-grade secure storage with access control";
        }
        #endregion
    }
}