using Neo.SmartContract.Framework;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.Numerics;

namespace NeoServiceLayer.Contracts
{
    [DisplayName("TestContract")]
    [ManifestExtra("Author", "Neo Service Layer Team")]
    [ManifestExtra("Description", "Test contract for compilation")]
    [ManifestExtra("Version", "1.0.0")]
    public class TestContract : SmartContract
    {
        public static string GetName()
        {
            return "TestContract";
        }

        public static BigInteger Add(BigInteger a, BigInteger b)
        {
            return a + b;
        }

        public static void _deploy(object data, bool update)
        {
            if (update) return;
            Runtime.Log("TestContract deployed successfully");
        }
    }
}