using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace NeoServiceLayer.Contracts.Services
{
    [ContractPermission("*", "*")]
    public class HybridTestContract : SmartContract
    {
        private static readonly StorageMap TestStorage = new(Storage.CurrentContext, "test");

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (update) return;
            TestStorage.Put("initialized", "true");
            TestStorage.Put("version", "1.0.0");
        }

        [DisplayName("setValue")]
        public static bool SetValue(string key, string value)
        {
            if (key is null || value is null) return false;
            TestStorage.Put(key, value);
            return true;
        }

        [DisplayName("getValue")]
        [Safe]
        public static string GetValue(string key)
        {
            if (key is null) return "";
            return TestStorage.Get(key) ?? "";
        }

        [DisplayName("getVersion")]
        [Safe]
        public static string GetVersion()
        {
            return "1.0.0";
        }
    }
}