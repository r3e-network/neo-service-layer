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
    /// Simple NEP-17 token contract for testing Neo N3 compilation
    /// </summary>
    [DisplayName("SimpleTokenContract")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Description", "Simple NEP-17 token")]
    [ManifestExtra("Version", "1.0.0")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class SimpleTokenContract : SmartContract
    {
        // Token details
        private const string Symbol = "STK";
        private const string Name = "SimpleToken";
        private const byte Decimals = 8;
        private static readonly BigInteger TotalSupply = new BigInteger(10000000000000000); // 100M tokens with 8 decimals

        // Storage keys
        private static readonly ByteString BalancePrefix = "balance:";
        private static readonly ByteString TotalSupplyKey = "totalSupply";

        // Events (NEP-17 standard)
        [DisplayName("Transfer")]
        public static event Action<UInt160, UInt160, BigInteger> OnTransfer;

        // Contract deployment
        public static void _deploy(object data, bool update)
        {
            if (update) return;

            // Initialize total supply
            Storage.Put(Storage.CurrentContext, TotalSupplyKey, TotalSupply);

            // Give all tokens to contract deployer (use contract address for simplicity)
            var deployer = Runtime.ExecutingScriptHash;

            var deployerBalanceKey = BalancePrefix + deployer;
            Storage.Put(Storage.CurrentContext, deployerBalanceKey, TotalSupply);

            OnTransfer(null, deployer, TotalSupply);
            Runtime.Log("SimpleTokenContract deployed successfully");
        }

        /// <summary>
        /// Get token symbol (NEP-17)
        /// </summary>
        public static string symbol()
        {
            return Symbol;
        }

        /// <summary>
        /// Get token name (NEP-17)
        /// </summary>
        public static string name()
        {
            return Name;
        }

        /// <summary>
        /// Get token decimals (NEP-17)
        /// </summary>
        public static byte decimals()
        {
            return Decimals;
        }

        /// <summary>
        /// Get total token supply (NEP-17)
        /// </summary>
        public static BigInteger totalSupply()
        {
            var supply = Storage.Get(Storage.CurrentContext, TotalSupplyKey);
            return supply != null ? (BigInteger)supply : 0;
        }

        /// <summary>
        /// Get balance of an address (NEP-17)
        /// </summary>
        public static BigInteger balanceOf(UInt160 account)
        {
            if (account == null || account.IsZero)
                throw new ArgumentException("Invalid account");

            var balanceKey = BalancePrefix + account;
            var balance = Storage.Get(Storage.CurrentContext, balanceKey);
            return balance != null ? (BigInteger)balance : 0;
        }

        /// <summary>
        /// Transfer tokens (NEP-17)
        /// </summary>
        public static bool transfer(UInt160 from, UInt160 to, BigInteger amount, object data)
        {
            if (from == null || from.IsZero)
                throw new ArgumentException("Invalid from address");

            if (to == null || to.IsZero)
                throw new ArgumentException("Invalid to address");

            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative");

            if (amount == 0)
                return true;

            // Check permission
            if (!Runtime.CheckWitness(from))
                return false;

            // Get balances
            var fromBalance = balanceOf(from);
            if (fromBalance < amount)
                return false;

            var toBalance = balanceOf(to);

            // Update balances
            var fromBalanceKey = BalancePrefix + from;
            var toBalanceKey = BalancePrefix + to;

            Storage.Put(Storage.CurrentContext, fromBalanceKey, fromBalance - amount);
            Storage.Put(Storage.CurrentContext, toBalanceKey, toBalance + amount);

            // Emit transfer event
            OnTransfer(from, to, amount);

            // Call onNEP17Payment if receiver is a contract
            if (ContractManagement.GetContract(to) != null)
            {
                Contract.Call(to, "onNEP17Payment", CallFlags.All, from, amount, data);
            }

            Runtime.Log($"Transferred {amount} tokens from {from} to {to}");
            return true;
        }

        /// <summary>
        /// Mint new tokens (only contract owner)
        /// </summary>
        public static bool mint(UInt160 to, BigInteger amount)
        {
            if (to == null || to.IsZero)
                throw new ArgumentException("Invalid to address");

            if (amount <= 0)
                throw new ArgumentException("Amount must be positive");

            // Only contract can mint (simplified - in production would have owner checks)
            if (!Runtime.CheckWitness(Runtime.ExecutingScriptHash))
                return false;

            var toBalance = balanceOf(to);
            var currentSupply = totalSupply();

            // Update balance and total supply
            var toBalanceKey = BalancePrefix + to;
            Storage.Put(Storage.CurrentContext, toBalanceKey, toBalance + amount);
            Storage.Put(Storage.CurrentContext, TotalSupplyKey, currentSupply + amount);

            OnTransfer(null, to, amount);
            Runtime.Log($"Minted {amount} tokens to {to}");

            return true;
        }

        /// <summary>
        /// Burn tokens
        /// </summary>
        public static bool burn(UInt160 from, BigInteger amount)
        {
            if (from == null || from.IsZero)
                throw new ArgumentException("Invalid from address");

            if (amount <= 0)
                throw new ArgumentException("Amount must be positive");

            // Check permission
            if (!Runtime.CheckWitness(from))
                return false;

            var fromBalance = balanceOf(from);
            if (fromBalance < amount)
                return false;

            var currentSupply = totalSupply();

            // Update balance and total supply
            var fromBalanceKey = BalancePrefix + from;
            Storage.Put(Storage.CurrentContext, fromBalanceKey, fromBalance - amount);
            Storage.Put(Storage.CurrentContext, TotalSupplyKey, currentSupply - amount);

            OnTransfer(from, null, amount);
            Runtime.Log($"Burned {amount} tokens from {from}");

            return true;
        }
    }
}