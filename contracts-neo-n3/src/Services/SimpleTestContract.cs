using Neo.SmartContract.Framework;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace NeoServiceLayer.Contracts.Services
{
    public class SimpleTestContract : SmartContract
    {
        private static readonly StorageMap TestStorage = new(Storage.CurrentContext, "test");

        public static void _deploy(object data, bool update)
        {
            if (update) return;

            TestStorage.Put("initialized", "true");
            TestStorage.Put("version", "1.0.0");
        }

        public static bool SetValue(string key, string value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                return false;

            TestStorage.Put(key, value);
            return true;
        }

        public static string GetValue(string key)
        {
            if (string.IsNullOrEmpty(key))
                return "";

            return TestStorage.Get(key) ?? "";
        }

        public static string GetVersion()
        {
            return TestStorage.Get("version") ?? "1.0.0";
        }

        public static string GetStatus()
        {
            var initialized = TestStorage.Get("initialized");
            return initialized != null ? "initialized" : "not_initialized";
        }
    }
}