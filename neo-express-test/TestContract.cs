using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;

namespace TestContract
{
    [DisplayName("TestContract")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Email", "test@neoservicelayer.com")]
    [ManifestExtra("Description", "Test contract for Neo Express")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class TestContract : SmartContract
    {
        private static readonly UInt160 Owner = (UInt160)"NUf4zTGLB9APRe4Jdh85HEaHS5kY6rg1gr".ToScriptHash(53);
        private const byte Prefix_TotalSupply = 0x00;
        private const byte Prefix_Balance = 0x01;

        [DisplayName("Transfer")]
        public static event Action<UInt160, UInt160, BigInteger> OnTransfer;

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (!update)
            {
                // Initialize contract on first deployment
                Storage.Put(Storage.CurrentContext, new ByteString(new byte[] { Prefix_TotalSupply }), 1000000);
                Storage.Put(Storage.CurrentContext, new ByteString(new byte[] { Prefix_Balance }).Concat(Owner), 1000000);
            }
        }

        public static string Name() => "TestToken";
        public static string Symbol() => "TST";
        public static byte Decimals() => 8;
        public static BigInteger TotalSupply() => (BigInteger)Storage.Get(Storage.CurrentContext, new ByteString(new byte[] { Prefix_TotalSupply }));

        public static BigInteger BalanceOf(UInt160 account)
        {
            if (!account.IsValid)
                throw new Exception("Invalid account");
            return (BigInteger)Storage.Get(Storage.CurrentContext, new ByteString(new byte[] { Prefix_Balance }).Concat(account));
        }

        public static bool Transfer(UInt160 from, UInt160 to, BigInteger amount, object data)
        {
            if (!from.IsValid || !to.IsValid)
                throw new Exception("Invalid address");
            if (amount <= 0)
                throw new Exception("Invalid amount");
            if (!Runtime.CheckWitness(from))
                return false;

            var fromKey = new ByteString(new byte[] { Prefix_Balance }).Concat(from);
            var fromBalance = (BigInteger)Storage.Get(Storage.CurrentContext, fromKey);
            
            if (fromBalance < amount)
                return false;

            if (from == to)
                return true;

            Storage.Put(Storage.CurrentContext, fromKey, fromBalance - amount);
            
            var toKey = new ByteString(new byte[] { Prefix_Balance }).Concat(to);
            var toBalance = (BigInteger)Storage.Get(Storage.CurrentContext, toKey);
            Storage.Put(Storage.CurrentContext, toKey, toBalance + amount);

            OnTransfer(from, to, amount);
            
            if (ContractManagement.GetContract(to) != null)
                Contract.Call(to, "onNEP17Payment", CallFlags.All, from, amount, data);

            return true;
        }

        [DisplayName("getValue")]
        public static string GetValue(string key)
        {
            return Storage.Get(Storage.CurrentContext, key);
        }

        [DisplayName("setValue")]
        public static bool SetValue(string key, string value)
        {
            if (!Runtime.CheckWitness(Owner))
                return false;
            Storage.Put(Storage.CurrentContext, key, value);
            return true;
        }

        [DisplayName("hello")]
        public static string Hello(string name)
        {
            return "Hello, " + name + "!";
        }
    }
}