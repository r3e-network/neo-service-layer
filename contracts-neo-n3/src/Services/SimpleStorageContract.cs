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
    /// Simple storage contract for testing Neo N3 compilation
    /// </summary>
    [DisplayName("SimpleStorageContract")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Description", "Simple storage contract")]
    [ManifestExtra("Version", "1.0.0")]
    [ContractPermission("*", "*")]
    public class SimpleStorageContract : SmartContract
    {
        // Storage prefix
        private static readonly ByteString StoragePrefix = "storage:";

        // Events
        [DisplayName("DataStored")]
        public static event Action<UInt160, ByteString, ByteString> DataStored;

        [DisplayName("DataRetrieved")]
        public static event Action<UInt160, ByteString> DataRetrieved;

        // Contract deployment
        public static void _deploy(object data, bool update)
        {
            if (update) return;
            Runtime.Log("SimpleStorageContract deployed successfully");
        }

        /// <summary>
        /// Store data with a key
        /// </summary>
        public static bool Store(ByteString key, ByteString value)
        {
            if (key == null || key.Length == 0)
                throw new ArgumentException("Key cannot be empty");

            if (value == null)
                throw new ArgumentException("Value cannot be null");

            var caller = Runtime.CallingScriptHash;
            var storageKey = StoragePrefix + key;

            Storage.Put(Storage.CurrentContext, storageKey, value);

            DataStored(caller, key, value);
            Runtime.Log($"Data stored with key: {key}");

            return true;
        }

        /// <summary>
        /// Retrieve data by key
        /// </summary>
        public static ByteString Retrieve(ByteString key)
        {
            if (key == null || key.Length == 0)
                throw new ArgumentException("Key cannot be empty");

            var caller = Runtime.CallingScriptHash;
            var storageKey = StoragePrefix + key;

            var value = Storage.Get(Storage.CurrentContext, storageKey);
            if (value == null)
                return null;

            DataRetrieved(caller, key);
            Runtime.Log($"Data retrieved with key: {key}");

            return value;
        }

        /// <summary>
        /// Delete data by key
        /// </summary>
        public static bool Delete(ByteString key)
        {
            if (key == null || key.Length == 0)
                throw new ArgumentException("Key cannot be empty");

            var storageKey = StoragePrefix + key;
            Storage.Delete(Storage.CurrentContext, storageKey);

            Runtime.Log($"Data deleted with key: {key}");
            return true;
        }

        /// <summary>
        /// Check if key exists
        /// </summary>
        public static bool Exists(ByteString key)
        {
            if (key == null || key.Length == 0)
                return false;

            var storageKey = StoragePrefix + key;
            var value = Storage.Get(Storage.CurrentContext, storageKey);
            return value != null;
        }
    }
}