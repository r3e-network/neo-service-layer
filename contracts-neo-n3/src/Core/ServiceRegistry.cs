using Neo.SmartContract.Framework;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;

namespace NeoServiceLayer.Contracts.Core
{
    [ContractSourceCode("https://github.com/yourusername/neo-service-layer")]
    public class ServiceRegistry : SmartContract
    {
        private static readonly ByteString ServiceMapPrefix = "serviceMap:";
        private static readonly ByteString ServiceCountKey = "serviceCount";
        private static readonly ByteString OwnerKey = "owner";
        
        [DisplayName("ServiceRegistered")]
        public static event Action<string, UInt160> ServiceRegistered;
        
        [DisplayName("ServiceUpdated")]
        public static event Action<string, UInt160> ServiceUpdated;
        
        [DisplayName("OwnerChanged")]
        public static event Action<UInt160, UInt160> OwnerChanged;

        public static void _deploy(object data, bool update)
        {
            if (!update)
            {
                Storage.Put(Storage.CurrentContext, OwnerKey, ((Transaction)Runtime.ScriptContainer).Sender);
                Storage.Put(Storage.CurrentContext, ServiceCountKey, 0);
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
            ServiceUpdated("ownership", newOwner);
        }

        public static bool RegisterService(string serviceName, UInt160 contractAddress)
        {
            Assert(serviceName != null && serviceName.Length > 0, "Service name required");
            Assert(contractAddress != null && contractAddress.IsValid, "Invalid contract address");
            
            ByteString owner = GetOwner();
            Assert(Runtime.CheckWitness((UInt160)owner), "Only owner can register services");
            
            ByteString key = ServiceMapPrefix.Concat(serviceName);
            Storage.Put(Storage.CurrentContext, key, contractAddress);
            
            BigInteger count = (BigInteger)Storage.Get(Storage.CurrentContext, ServiceCountKey);
            Storage.Put(Storage.CurrentContext, ServiceCountKey, count + 1);
            
            ServiceRegistered(serviceName, contractAddress);
            return true;
        }

        public static UInt160 GetService(string serviceName)
        {
            Assert(serviceName != null && serviceName.Length > 0, "Service name required");
            
            ByteString key = ServiceMapPrefix.Concat(serviceName);
            ByteString value = Storage.Get(Storage.CurrentContext, key);
            
            if (value != null && value.Length == 20)
            {
                return (UInt160)value;
            }
            return null;
        }

        public static bool UpdateService(string serviceName, UInt160 newAddress)
        {
            Assert(serviceName != null && serviceName.Length > 0, "Service name required");
            Assert(newAddress != null && newAddress.IsValid, "Invalid contract address");
            
            ByteString owner = GetOwner();
            Assert(Runtime.CheckWitness((UInt160)owner), "Only owner can update services");
            
            ByteString key = ServiceMapPrefix.Concat(serviceName);
            ByteString existing = Storage.Get(Storage.CurrentContext, key);
            Assert(existing != null, "Service not found");
            
            Storage.Put(Storage.CurrentContext, key, newAddress);
            ServiceUpdated(serviceName, newAddress);
            return true;
        }

        public static BigInteger GetServiceCount()
        {
            return (BigInteger)Storage.Get(Storage.CurrentContext, ServiceCountKey);
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
    }
}
