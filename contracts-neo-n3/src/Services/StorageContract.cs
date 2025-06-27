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
    /// Provides decentralized storage management using the Neo Service Layer's
    /// secure storage infrastructure with encryption and access control.
    /// </summary>
    [DisplayName("StorageContract")]
    [ManifestExtra("Author", "Neo Service Layer Team")]
    [ManifestExtra("Description", "Decentralized storage management service")]
    [ManifestExtra("Version", "1.0.0")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class StorageContract : BaseServiceContract
    {
        #region Storage Keys
        private static readonly byte[] StorageItemPrefix = "storageItem:".ToByteArray();
        private static readonly byte[] StorageMetadataPrefix = "metadata:".ToByteArray();
        private static readonly byte[] AccessControlPrefix = "access:".ToByteArray();
        private static readonly byte[] StorageCountKey = "storageCount".ToByteArray();
        private static readonly byte[] StorageFeeKey = "storageFee".ToByteArray();
        private static readonly byte[] MaxFileSizeKey = "maxFileSize".ToByteArray();
        private static readonly byte[] EncryptionEnabledKey = "encryptionEnabled".ToByteArray();
        #endregion

        #region Events
        [DisplayName("FileStored")]
        public static event Action<UInt160, ByteString, string, BigInteger, bool> FileStored;

        [DisplayName("FileRetrieved")]
        public static event Action<UInt160, ByteString, UInt160> FileRetrieved;

        [DisplayName("FileDeleted")]
        public static event Action<UInt160, ByteString> FileDeleted;

        [DisplayName("AccessGranted")]
        public static event Action<UInt160, ByteString, UInt160> AccessGranted;

        [DisplayName("AccessRevoked")]
        public static event Action<UInt160, ByteString, UInt160> AccessRevoked;
        #endregion

        #region Constants
        private const long DEFAULT_STORAGE_FEE = 1000000; // 0.01 GAS per KB
        private const int DEFAULT_MAX_FILE_SIZE = 1048576; // 1MB
        #endregion

        #region Initialization
        public static void _deploy(object data, bool update)
        {
            if (update) return;

            var serviceId = Runtime.ExecutingScriptHash;
            var contract = new StorageContract();
            contract.InitializeBaseService(serviceId, "StorageService", "1.0.0", "{}");
            
            Storage.Put(Storage.CurrentContext, StorageFeeKey, DEFAULT_STORAGE_FEE);
            Storage.Put(Storage.CurrentContext, MaxFileSizeKey, DEFAULT_MAX_FILE_SIZE);
            Storage.Put(Storage.CurrentContext, EncryptionEnabledKey, true);
            Storage.Put(Storage.CurrentContext, StorageCountKey, 0);

            Runtime.Log("StorageContract deployed successfully");
        }
        #endregion

        #region Service Implementation
        protected override void InitializeService(string config)
        {
            Runtime.Log("StorageContract service initialized");
        }

        protected override bool PerformHealthCheck()
        {
            try
            {
                var storageCount = GetStorageCount();
                return storageCount >= 0;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Storage Operations
        /// <summary>
        /// Stores a file with metadata and access control.
        /// </summary>
        public static ByteString StoreFile(string filename, ByteString data, string contentType, bool isEncrypted, UInt160[] allowedUsers)
        {
            return ExecuteServiceOperation(() =>
            {
                var caller = Runtime.CallingScriptHash;
                var fileId = GenerateFileId(caller, filename);
                
                // Validate file size
                var maxSize = GetMaxFileSize();
                if (data.Length > maxSize)
                    throw new ArgumentException($"File size exceeds maximum allowed: {maxSize} bytes");
                
                // Calculate storage fee
                var fee = CalculateStorageFee(data.Length);
                
                // Create storage item
                var storageItem = new StorageItem
                {
                    Id = fileId,
                    Owner = caller,
                    Filename = filename,
                    ContentType = contentType,
                    Size = data.Length,
                    IsEncrypted = isEncrypted,
                    CreatedAt = Runtime.Time,
                    AccessCount = 0
                };
                
                // Store file data and metadata
                var itemKey = StorageItemPrefix.Concat(fileId);
                Storage.Put(Storage.CurrentContext, itemKey, data);
                
                var metadataKey = StorageMetadataPrefix.Concat(fileId);
                Storage.Put(Storage.CurrentContext, metadataKey, StdLib.Serialize(storageItem));
                
                // Set access control
                foreach (var user in allowedUsers)
                {
                    var accessKey = AccessControlPrefix.Concat(fileId).Concat(user);
                    Storage.Put(Storage.CurrentContext, accessKey, true);
                }
                
                // Increment storage count
                var count = GetStorageCount();
                Storage.Put(Storage.CurrentContext, StorageCountKey, count + 1);
                
                FileStored(caller, fileId, filename, fee, isEncrypted);
                Runtime.Log($"File stored: {filename} with ID {fileId}");
                return fileId;
            });
        }

        /// <summary>
        /// Retrieves a stored file.
        /// </summary>
        public static ByteString RetrieveFile(ByteString fileId)
        {
            return ExecuteServiceOperation(() =>
            {
                var caller = Runtime.CallingScriptHash;
                
                // Check access permissions
                if (!HasFileAccess(fileId, caller))
                    throw new InvalidOperationException("Access denied");
                
                // Get file data
                var itemKey = StorageItemPrefix.Concat(fileId);
                var data = Storage.Get(Storage.CurrentContext, itemKey);
                if (data == null)
                    throw new InvalidOperationException("File not found");
                
                // Update access count
                UpdateAccessCount(fileId);
                
                FileRetrieved(caller, fileId, caller);
                Runtime.Log($"File retrieved: {fileId}");
                return data;
            });
        }

        /// <summary>
        /// Deletes a stored file.
        /// </summary>
        public static bool DeleteFile(ByteString fileId)
        {
            return ExecuteServiceOperation(() =>
            {
                var caller = Runtime.CallingScriptHash;
                var metadata = GetFileMetadata(fileId);
                
                if (metadata == null)
                    throw new InvalidOperationException("File not found");
                
                // Only owner can delete
                if (!metadata.Owner.Equals(caller))
                    throw new InvalidOperationException("Only file owner can delete");
                
                // Delete file data and metadata
                var itemKey = StorageItemPrefix.Concat(fileId);
                Storage.Delete(Storage.CurrentContext, itemKey);
                
                var metadataKey = StorageMetadataPrefix.Concat(fileId);
                Storage.Delete(Storage.CurrentContext, metadataKey);
                
                // Clean up access control entries
                // (Simplified - in production would iterate through all access entries)
                
                // Decrement storage count
                var count = GetStorageCount();
                Storage.Put(Storage.CurrentContext, StorageCountKey, count - 1);
                
                FileDeleted(caller, fileId);
                Runtime.Log($"File deleted: {fileId}");
                return true;
            });
        }
        #endregion

        #region Access Control
        /// <summary>
        /// Grants access to a file for a specific user.
        /// </summary>
        public static bool GrantFileAccess(ByteString fileId, UInt160 user)
        {
            return ExecuteServiceOperation(() =>
            {
                var caller = Runtime.CallingScriptHash;
                var metadata = GetFileMetadata(fileId);
                
                if (metadata == null)
                    throw new InvalidOperationException("File not found");
                
                // Only owner can grant access
                if (!metadata.Owner.Equals(caller))
                    throw new InvalidOperationException("Only file owner can grant access");
                
                var accessKey = AccessControlPrefix.Concat(fileId).Concat(user);
                Storage.Put(Storage.CurrentContext, accessKey, true);
                
                AccessGranted(caller, fileId, user);
                Runtime.Log($"Access granted to {user} for file {fileId}");
                return true;
            });
        }

        /// <summary>
        /// Revokes access to a file for a specific user.
        /// </summary>
        public static bool RevokeFileAccess(ByteString fileId, UInt160 user)
        {
            return ExecuteServiceOperation(() =>
            {
                var caller = Runtime.CallingScriptHash;
                var metadata = GetFileMetadata(fileId);
                
                if (metadata == null)
                    throw new InvalidOperationException("File not found");
                
                // Only owner can revoke access
                if (!metadata.Owner.Equals(caller))
                    throw new InvalidOperationException("Only file owner can revoke access");
                
                var accessKey = AccessControlPrefix.Concat(fileId).Concat(user);
                Storage.Delete(Storage.CurrentContext, accessKey);
                
                AccessRevoked(caller, fileId, user);
                Runtime.Log($"Access revoked from {user} for file {fileId}");
                return true;
            });
        }

        /// <summary>
        /// Checks if a user has access to a file.
        /// </summary>
        public static bool HasFileAccess(ByteString fileId, UInt160 user)
        {
            var metadata = GetFileMetadata(fileId);
            if (metadata == null)
                return false;
            
            // Owner always has access
            if (metadata.Owner.Equals(user))
                return true;
            
            // Check explicit access grant
            var accessKey = AccessControlPrefix.Concat(fileId).Concat(user);
            return Storage.Get(Storage.CurrentContext, accessKey) != null;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Gets file metadata.
        /// </summary>
        public static StorageItem GetFileMetadata(ByteString fileId)
        {
            var metadataKey = StorageMetadataPrefix.Concat(fileId);
            var metadataBytes = Storage.Get(Storage.CurrentContext, metadataKey);
            if (metadataBytes == null)
                return null;
            
            return (StorageItem)StdLib.Deserialize(metadataBytes);
        }

        /// <summary>
        /// Gets the total number of stored files.
        /// </summary>
        public static int GetStorageCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, StorageCountKey);
            return (int)(countBytes?.ToBigInteger() ?? 0);
        }

        /// <summary>
        /// Gets the maximum file size allowed.
        /// </summary>
        public static int GetMaxFileSize()
        {
            var sizeBytes = Storage.Get(Storage.CurrentContext, MaxFileSizeKey);
            return (int)(sizeBytes?.ToBigInteger() ?? DEFAULT_MAX_FILE_SIZE);
        }

        /// <summary>
        /// Calculates storage fee based on file size.
        /// </summary>
        private static BigInteger CalculateStorageFee(int fileSize)
        {
            var feePerKB = GetStorageFee();
            var sizeInKB = (fileSize + 1023) / 1024; // Round up to nearest KB
            return feePerKB * sizeInKB;
        }

        /// <summary>
        /// Gets the storage fee per KB.
        /// </summary>
        private static BigInteger GetStorageFee()
        {
            var feeBytes = Storage.Get(Storage.CurrentContext, StorageFeeKey);
            return feeBytes?.ToBigInteger() ?? DEFAULT_STORAGE_FEE;
        }

        /// <summary>
        /// Generates a unique file ID.
        /// </summary>
        private static ByteString GenerateFileId(UInt160 owner, string filename)
        {
            var data = owner.ToByteArray()
                .Concat(filename.ToByteArray())
                .Concat(Runtime.Time.ToByteArray());
            return CryptoLib.Sha256(data);
        }

        /// <summary>
        /// Updates the access count for a file.
        /// </summary>
        private static void UpdateAccessCount(ByteString fileId)
        {
            var metadata = GetFileMetadata(fileId);
            if (metadata != null)
            {
                metadata.AccessCount++;
                var metadataKey = StorageMetadataPrefix.Concat(fileId);
                Storage.Put(Storage.CurrentContext, metadataKey, StdLib.Serialize(metadata));
            }
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
        /// Represents a stored file item.
        /// </summary>
        public class StorageItem
        {
            public ByteString Id;
            public UInt160 Owner;
            public string Filename;
            public string ContentType;
            public int Size;
            public bool IsEncrypted;
            public ulong CreatedAt;
            public int AccessCount;
        }
        #endregion
    }
}