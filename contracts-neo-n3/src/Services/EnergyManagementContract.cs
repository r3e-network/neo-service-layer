using Neo;
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
    [DisplayName("EnergyManagementContract")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Description", "Energy management and tracking")]
    [ManifestExtra("Version", "1.0.0")]
    [ContractPermission("*", "*")]
    public class EnergyManagementContract : SmartContract
    {
        // Storage prefix for this contract
        private static readonly ByteString StoragePrefix = "energy:";
        private static readonly ByteString OwnerKey = "owner";
        private static readonly ByteString ConfigKey = "config";
        
        // Events
        [DisplayName("Deployed")]
        public static event Action<UInt160, string> Deployed;
        
        [DisplayName("DataStored")]
        public static event Action<UInt160, ByteString> DataStored;
        
        [DisplayName("OwnershipTransferred")]
        public static event Action<UInt160, UInt160> OwnershipTransferred;
        
        // Deploy the contract
        public static void _deploy(object data, bool update)
        {
            if (update) return;
            
            var tx = Runtime.Transaction;
            Storage.Put(Storage.CurrentContext, OwnerKey, tx.Sender);
            
            Deployed(tx.Sender, "EnergyManagementContract deployed");
        }
        
        // Get the contract owner
        public static UInt160 GetOwner()
        {
            return (UInt160)Storage.Get(Storage.CurrentContext, OwnerKey);
        }
        
        // Transfer ownership
        public static bool TransferOwnership(UInt160 newOwner)
        {
            if (!Runtime.CheckWitness(GetOwner()))
                throw new InvalidOperationException("Only owner can transfer ownership");
                
            var oldOwner = GetOwner();
            Storage.Put(Storage.CurrentContext, OwnerKey, newOwner);
            
            OwnershipTransferred(oldOwner, newOwner);
            return true;
        }
        
        // Store data with key
        public static bool Store(ByteString key, ByteString value)
        {
            if (!Runtime.CheckWitness(GetOwner()))
                throw new InvalidOperationException("Only owner can store data");
                
            var storageKey = StoragePrefix + key;
            Storage.Put(Storage.CurrentContext, storageKey, value);
            
            DataStored(GetOwner(), key);
            return true;
        }
        
        // Retrieve data by key
        public static ByteString Retrieve(ByteString key)
        {
            var storageKey = StoragePrefix + key;
            return Storage.Get(Storage.CurrentContext, storageKey);
        }
        
        // Delete data by key
        public static bool Delete(ByteString key)
        {
            if (!Runtime.CheckWitness(GetOwner()))
                throw new InvalidOperationException("Only owner can delete data");
                
            var storageKey = StoragePrefix + key;
            Storage.Delete(Storage.CurrentContext, storageKey);
            return true;
        }
        
        // Check if key exists
        public static bool Exists(ByteString key)
        {
            var storageKey = StoragePrefix + key;
            return Storage.Get(Storage.CurrentContext, storageKey) != null;
        }
        
        // Update configuration
        public static bool UpdateConfig(string config)
        {
            if (!Runtime.CheckWitness(GetOwner()))
                throw new InvalidOperationException("Only owner can update config");
                
            Storage.Put(Storage.CurrentContext, ConfigKey, config);
            return true;
        }
        
        // Get configuration
        public static string GetConfig()
        {
            var config = Storage.Get(Storage.CurrentContext, ConfigKey);
            return config != null ? (string)config : "";
        }
    }
}
