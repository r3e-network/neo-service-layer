using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NeoServiceLayer.Examples
{
    [DisplayName("ConfidentialToken")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Email", "info@neoservicelayer.io")]
    [ManifestExtra("Description", "Confidential Token Example for Neo Service Layer")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class ConfidentialToken : SmartContract
    {
        // Events
        [DisplayName("Transfer")]
        public static event Action<UInt160, UInt160, BigInteger> OnTransfer;

        [DisplayName("ConfidentialTransfer")]
        public static event Action<UInt160, UInt160, byte[]> OnConfidentialTransfer;

        // Storage keys
        private static readonly byte[] OwnerKey = "owner".ToByteArray();
        private static readonly byte[] TotalSupplyKey = "totalSupply".ToByteArray();
        private static readonly byte[] BalancePrefix = "balance".ToByteArray();
        private static readonly byte[] EncryptedBalancePrefix = "encryptedBalance".ToByteArray();
        private static readonly byte[] ServiceLayerKey = "serviceLayer".ToByteArray();

        // NEP-17 properties
        [DisplayName("Symbol")]
        public static string Symbol() => "CTOK";

        [DisplayName("Decimals")]
        public static byte Decimals() => 8;

        // Custom properties
        [DisplayName("ServiceLayerAddress")]
        public static UInt160 ServiceLayerAddress()
        {
            return (UInt160)Storage.Get(Storage.CurrentContext, ServiceLayerKey);
        }

        // Constructor
        public static void _deploy(object data, bool update)
        {
            if (!update)
            {
                // Set initial owner to the contract deployer
                Storage.Put(Storage.CurrentContext, OwnerKey, Runtime.CallingScriptHash);
                
                // Set initial total supply to 0
                Storage.Put(Storage.CurrentContext, TotalSupplyKey, 0);
            }
        }

        // Initialize the contract with the Neo Service Layer address
        public static void Initialize(UInt160 serviceLayerAddress)
        {
            // Only owner can initialize
            if (!IsOwner()) throw new Exception("No authorization");
            
            // Validate service layer address
            if (serviceLayerAddress is null || !serviceLayerAddress.IsValid)
                throw new Exception("Invalid service layer address");
                
            // Store service layer address
            Storage.Put(Storage.CurrentContext, ServiceLayerKey, serviceLayerAddress);
        }

        // NEP-17 methods
        [DisplayName("balanceOf")]
        public static BigInteger BalanceOf(UInt160 account)
        {
            if (account is null || !account.IsValid)
                throw new Exception("Invalid account");
                
            return (BigInteger)Storage.Get(Storage.CurrentContext, BalancePrefix.Concat(account));
        }

        [DisplayName("totalSupply")]
        public static BigInteger TotalSupply()
        {
            return (BigInteger)Storage.Get(Storage.CurrentContext, TotalSupplyKey);
        }

        [DisplayName("transfer")]
        public static bool Transfer(UInt160 from, UInt160 to, BigInteger amount, object data)
        {
            // Validate parameters
            if (from is null || !from.IsValid)
                throw new Exception("Invalid from address");
            if (to is null || !to.IsValid)
                throw new Exception("Invalid to address");
            if (amount <= 0)
                throw new Exception("Amount must be positive");
                
            // Check if the sender is the owner of the tokens
            if (!Runtime.CheckWitness(from))
                return false;
                
            // Get sender balance
            BigInteger fromBalance = BalanceOf(from);
            
            // Check if the sender has enough tokens
            if (fromBalance < amount)
                return false;
                
            // Update balances
            if (fromBalance == amount)
                Storage.Delete(Storage.CurrentContext, BalancePrefix.Concat(from));
            else
                Storage.Put(Storage.CurrentContext, BalancePrefix.Concat(from), fromBalance - amount);
                
            // Add tokens to the recipient's balance
            BigInteger toBalance = BalanceOf(to);
            Storage.Put(Storage.CurrentContext, BalancePrefix.Concat(to), toBalance + amount);
            
            // Notify transfer event
            OnTransfer(from, to, amount);
            
            // Call onNEP17Payment if the receiver is a contract
            if (ContractManagement.GetContract(to) != null)
                Contract.Call(to, "onNEP17Payment", CallFlags.All, new object[] { from, amount, data });
                
            return true;
        }

        // Confidential methods
        [DisplayName("confidentialTransfer")]
        public static bool ConfidentialTransfer(UInt160 from, UInt160 to, byte[] encryptedAmount)
        {
            // Validate parameters
            if (from is null || !from.IsValid)
                throw new Exception("Invalid from address");
            if (to is null || !to.IsValid)
                throw new Exception("Invalid to address");
            if (encryptedAmount is null || encryptedAmount.Length == 0)
                throw new Exception("Invalid encrypted amount");
                
            // Check if the sender is the owner of the tokens
            if (!Runtime.CheckWitness(from))
                return false;
                
            // Call Neo Service Layer to process the confidential transfer
            bool success = (bool)Contract.Call(
                ServiceLayerAddress(),
                "processConfidentialTransfer",
                CallFlags.All,
                new object[] { from, to, encryptedAmount }
            );
            
            if (success)
            {
                // Notify confidential transfer event
                OnConfidentialTransfer(from, to, encryptedAmount);
            }
            
            return success;
        }

        // Admin methods
        [DisplayName("mint")]
        public static bool Mint(UInt160 to, BigInteger amount)
        {
            // Only owner can mint
            if (!IsOwner()) throw new Exception("No authorization");
            
            // Validate parameters
            if (to is null || !to.IsValid)
                throw new Exception("Invalid to address");
            if (amount <= 0)
                throw new Exception("Amount must be positive");
                
            // Update total supply
            BigInteger totalSupply = TotalSupply();
            Storage.Put(Storage.CurrentContext, TotalSupplyKey, totalSupply + amount);
            
            // Update recipient balance
            BigInteger toBalance = BalanceOf(to);
            Storage.Put(Storage.CurrentContext, BalancePrefix.Concat(to), toBalance + amount);
            
            // Notify transfer event
            OnTransfer(null, to, amount);
            
            return true;
        }

        [DisplayName("burn")]
        public static bool Burn(UInt160 from, BigInteger amount)
        {
            // Validate parameters
            if (from is null || !from.IsValid)
                throw new Exception("Invalid from address");
            if (amount <= 0)
                throw new Exception("Amount must be positive");
                
            // Check if the sender is the owner of the tokens
            if (!Runtime.CheckWitness(from))
                return false;
                
            // Get sender balance
            BigInteger fromBalance = BalanceOf(from);
            
            // Check if the sender has enough tokens
            if (fromBalance < amount)
                return false;
                
            // Update sender balance
            if (fromBalance == amount)
                Storage.Delete(Storage.CurrentContext, BalancePrefix.Concat(from));
            else
                Storage.Put(Storage.CurrentContext, BalancePrefix.Concat(from), fromBalance - amount);
                
            // Update total supply
            BigInteger totalSupply = TotalSupply();
            Storage.Put(Storage.CurrentContext, TotalSupplyKey, totalSupply - amount);
            
            // Notify transfer event
            OnTransfer(from, null, amount);
            
            return true;
        }

        // Helper methods
        private static bool IsOwner()
        {
            UInt160 owner = (UInt160)Storage.Get(Storage.CurrentContext, OwnerKey);
            return Runtime.CheckWitness(owner);
        }
    }
}
