using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;

namespace SimpleContract
{
    [DisplayName("SimpleContract")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Email", "test@neoservicelayer.com")]
    [ManifestExtra("Description", "Simple test contract for Neo Express")]
    public class SimpleContract : SmartContract
    {
        private const byte STORAGE_PREFIX = 0x01;
        
        [DisplayName("DataStored")]
        public static event Action<string, string> OnDataStored;

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (!update)
            {
                // Initialize contract on first deployment
                Runtime.Log("Contract deployed successfully");
            }
        }

        [DisplayName("hello")]
        public static string Hello(string name)
        {
            return "Hello, " + name + "!";
        }

        [DisplayName("store")]
        public static void Store(string key, string value)
        {
            var storageKey = ((ByteString)new byte[] { STORAGE_PREFIX }).Concat((ByteString)key);
            Storage.Put(Storage.CurrentContext, storageKey, value);
            OnDataStored(key, value);
        }

        [DisplayName("retrieve")]
        public static string Retrieve(string key)
        {
            var storageKey = ((ByteString)new byte[] { STORAGE_PREFIX }).Concat((ByteString)key);
            return Storage.Get(Storage.CurrentContext, storageKey);
        }

        [DisplayName("add")]
        public static int Add(int a, int b)
        {
            return a + b;
        }

        [DisplayName("multiply")]
        public static int Multiply(int a, int b)
        {
            return a * b;
        }

        [DisplayName("getBlockHeight")]
        public static uint GetBlockHeight()
        {
            return Ledger.CurrentIndex;
        }

        [DisplayName("getTimestamp")]
        public static ulong GetTimestamp()
        {
            return Ledger.GetBlock(Ledger.CurrentIndex).Timestamp;
        }
    }
}