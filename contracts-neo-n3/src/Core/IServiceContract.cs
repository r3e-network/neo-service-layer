using Neo.SmartContract.Framework;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;

namespace NeoServiceLayer.Contracts.Core
{
    [ContractSourceCode("https://github.com/yourusername/neo-service-layer")]
    public class IServiceContract : SmartContract
    {
        private static readonly ByteString ConfigPrefix = "config:";
        private static readonly ByteString OwnerKey = "owner";
        private static readonly ByteString VersionKey = "version";
        
        [DisplayName("ConfigUpdated")]
        public static event Action<string, ByteString> ConfigUpdated;
        
        [DisplayName("OwnerChanged")]
        public static event Action<UInt160, UInt160> OwnerChanged;

        public static void _deploy(object data, bool update)
        {
            if (!update)
            {
                Storage.Put(Storage.CurrentContext, OwnerKey, ((Transaction)Runtime.ScriptContainer).Sender);
                Storage.Put(Storage.CurrentContext, VersionKey, "1.0.0");
            }
        }

        public static ByteString GetOwner()
        {
            return Storage.Get(Storage.CurrentContext, OwnerKey);
        }

        public static void TransferOwnership(UInt160 newOwner)
        {
            ByteString currentOwner = GetOwner();
            Assert(Runtime.CheckWitness((UInt160)currentOwner), "Only owner can transfer ownership");
            Assert(newOwner != null && newOwner.IsValid, "Invalid new owner");
            
            Storage.Put(Storage.CurrentContext, OwnerKey, newOwner);
            OwnerChanged((UInt160)currentOwner, newOwner);
        }

        public static ByteString GetVersion()
        {
            return Storage.Get(Storage.CurrentContext, VersionKey);
        }

        public static bool SetConfig(string key, ByteString value)
        {
            Assert(key != null && key.Length > 0, "Key required");
            Assert(value != null, "Value required");
            
            ByteString owner = GetOwner();
            Assert(Runtime.CheckWitness((UInt160)owner), "Only owner can set config");
            
            ByteString configKey = ConfigPrefix.Concat(key);
            Storage.Put(Storage.CurrentContext, configKey, value);
            
            ConfigUpdated(key, value);
            return true;
        }

        public static ByteString GetConfig(string key)
        {
            Assert(key != null && key.Length > 0, "Key required");
            
            ByteString configKey = ConfigPrefix.Concat(key);
            return Storage.Get(Storage.CurrentContext, configKey);
        }

        public static bool Store(string key, ByteString value)
        {
            Assert(key != null && key.Length > 0, "Key required");
            Assert(value != null, "Value required");
            
            ByteString owner = GetOwner();
            Assert(Runtime.CheckWitness((UInt160)owner), "Unauthorized");
            
            Storage.Put(Storage.CurrentContext, key, value);
            return true;
        }

        public static ByteString Retrieve(string key)
        {
            Assert(key != null && key.Length > 0, "Key required");
            return Storage.Get(Storage.CurrentContext, key);
        }

        public static bool Delete(string key)
        {
            Assert(key != null && key.Length > 0, "Key required");
            
            ByteString owner = GetOwner();
            Assert(Runtime.CheckWitness((UInt160)owner), "Unauthorized");
            
            Storage.Delete(Storage.CurrentContext, key);
            return true;
        }

        public static bool IsAuthorized(UInt160 address)
        {
            ByteString owner = GetOwner();
            return address == (UInt160)owner || Runtime.CheckWitness(address);
        }
    }
}
