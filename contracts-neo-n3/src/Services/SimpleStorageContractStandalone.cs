using Neo.SmartContract.Framework;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NeoServiceLayer.Contracts.Services
{
    /// <summary>
    /// Simplified standalone storage contract for testing Neo N3 compilation
    /// </summary>
    [DisplayName("SimpleStorageContractStandalone")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Description", "Simple standalone storage contract")]
    [ManifestExtra("Version", "1.0.0")]
    [ContractPermission("*", "*")]
    public class SimpleStorageContractStandalone : SmartContract
    {
        // Storage keys
        private static readonly ByteString StorageItemPrefix = "item:";
        private static readonly ByteString StorageCountKey = "count";

        // Events
        [DisplayName("FileStored")]
        public static event Action<UInt160, ByteString, string> FileStored;

        [DisplayName("FileRetrieved")]
        public static event Action<UInt160, ByteString> FileRetrieved;

        // Contract deployment
        public static void _deploy(object data, bool update)
        {
            if (update) return;

            // Initialize storage count
            Storage.Put(Storage.CurrentContext, StorageCountKey, 0);
            Runtime.Log("SimpleStorageContractStandalone deployed successfully");
        }

        /// <summary>
        /// Store data with a key
        /// </summary>
        public static bool StoreData(ByteString key, ByteString data)
        {
            if (key == null || key.Length == 0)
                throw new ArgumentException("Key cannot be empty");

            if (data == null)
                throw new ArgumentException("Data cannot be null");

            var caller = Runtime.CallingScriptHash;
            var storageKey = StorageItemPrefix + key;

            // Store the data
            Storage.Put(Storage.CurrentContext, storageKey, data);

            // Increment count
            var count = GetStorageCount();
            Storage.Put(Storage.CurrentContext, StorageCountKey, count + 1);

            FileStored(caller, key, "data");
            Runtime.Log($"Data stored with key: {key}");

            return true;
        }

        /// <summary>
        /// Retrieve data by key
        /// </summary>
        public static ByteString RetrieveData(ByteString key)
        {
            if (key == null || key.Length == 0)
                throw new ArgumentException("Key cannot be empty");

            var caller = Runtime.CallingScriptHash;
            var storageKey = StorageItemPrefix + key;

            var data = Storage.Get(Storage.CurrentContext, storageKey);
            if (data == null)
                return null;

            FileRetrieved(caller, key);
            Runtime.Log($"Data retrieved with key: {key}");

            return data;
        }

        /// <summary>
        /// Delete data by key
        /// </summary>
        public static bool DeleteData(ByteString key)
        {
            if (key == null || key.Length == 0)
                throw new ArgumentException("Key cannot be empty");

            var storageKey = StorageItemPrefix + key;
            var existingData = Storage.Get(Storage.CurrentContext, storageKey);

            if (existingData != null)
            {
                Storage.Delete(Storage.CurrentContext, storageKey);

                // Decrement count
                var count = GetStorageCount();
                Storage.Put(Storage.CurrentContext, StorageCountKey, count - 1);

                Runtime.Log($"Data deleted with key: {key}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get total stored items count
        /// </summary>
        public static int GetStorageCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, StorageCountKey);
            return (int)(countBytes != null ? ((BigInteger)countBytes) : 0;
        }

        /// <summary>
        /// Check if key exists
        /// </summary>
        public static bool KeyExists(ByteString key)
        {
            if (key == null || key.Length == 0)
                return false;

            var storageKey = StorageItemPrefix + key;
            var data = Storage.Get(Storage.CurrentContext, storageKey);
            return data != null;
        }
    }
}