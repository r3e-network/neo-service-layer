using Neo.SmartContract.Framework;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;
using NeoServiceLayer.Contracts.Core;

namespace NeoServiceLayer.Contracts.ProductionReady
{
    /// <summary>
    /// Production-ready NEP-17 token with advanced features
    /// Features: Minting controls, burning, pausable transfers, blacklisting, fee collection
    /// </summary>
    [DisplayName("ProductionNEP17Token")]
    [ManifestExtra("Author", "Neo Service Layer Team")]
    [ManifestExtra("Description", "Enterprise-grade NEP-17 token with governance features")]
    [ManifestExtra("Version", "1.0.0")]
    [ManifestExtra("License", "MIT")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class ProductionNEP17Token : ReentrancyGuard
    {
        #region Token Configuration
        private const string TOKEN_SYMBOL = "PNEP";
        private const string TOKEN_NAME = "Production NEP-17 Token";
        private const byte TOKEN_DECIMALS = 8;
        private static readonly BigInteger INITIAL_SUPPLY = 1_000_000_00000000; // 1M tokens with 8 decimals
        private static readonly BigInteger MAX_SUPPLY = 100_000_000_00000000; // 100M max supply

        // Fee configuration (in basis points, 1 bp = 0.01%)
        private const int DEFAULT_TRANSFER_FEE_BP = 0; // 0% default
        private const int MAX_TRANSFER_FEE_BP = 1000; // 10% maximum
        #endregion

        #region Storage Keys
        private static readonly ByteString OWNER_KEY = "owner";
        private static readonly ByteString PAUSED_KEY = "paused";
        private static readonly ByteString TOTAL_SUPPLY_KEY = "totalSupply";
        private static readonly ByteString TRANSFER_FEE_KEY = "transferFee";
        private static readonly ByteString FEE_COLLECTOR_KEY = "feeCollector";
        private static readonly ByteString MINTING_PAUSED_KEY = "mintingPaused";
        private static readonly ByteString BURNING_PAUSED_KEY = "burningPaused";

        private static readonly ByteString BALANCE_PREFIX = "balance:";
        private static readonly ByteString ALLOWANCE_PREFIX = "allowance:";
        private static readonly ByteString BLACKLIST_PREFIX = "blacklist:";
        private static readonly ByteString MINTER_PREFIX = "minter:";
        private static readonly ByteString ADMIN_PREFIX = "admin:";
        #endregion

        #region Events
        [DisplayName("Transfer")]
        public static event Action<UInt160, UInt160, BigInteger> OnTransfer;

        [DisplayName("Approval")]
        public static event Action<UInt160, UInt160, BigInteger> OnApproval;

        [DisplayName("Mint")]
        public static event Action<UInt160, UInt160, BigInteger> OnMint;

        [DisplayName("Burn")]
        public static event Action<UInt160, BigInteger> OnBurn;

        [DisplayName("OwnershipTransferred")]
        public static event Action<UInt160, UInt160> OwnershipTransferred;

        [DisplayName("ContractPaused")]
        public static event Action<bool> ContractPaused;

        [DisplayName("BlacklistUpdated")]
        public static event Action<UInt160, bool> BlacklistUpdated;

        [DisplayName("MinterUpdated")]
        public static event Action<UInt160, bool> MinterUpdated;

        [DisplayName("AdminUpdated")]
        public static event Action<UInt160, bool> AdminUpdated;

        [DisplayName("TransferFeeUpdated")]
        public static event Action<int, int> TransferFeeUpdated;

        [DisplayName("FeeCollectorUpdated")]
        public static event Action<UInt160, UInt160> FeeCollectorUpdated;
        #endregion

        #region Initialization
        public static void _deploy(object data, bool update)
        {
            if (update)
            {
                Runtime.Log("ProductionNEP17Token updated successfully");
                return;
            }

            var owner = (UInt160)data ?? Runtime.CallingScriptHash;

            if (owner == null || owner.IsZero)
                throw new InvalidOperationException("Invalid owner address");

            // Initialize contract state
            Storage.Put(Storage.CurrentContext, OWNER_KEY, owner);
            Storage.Put(Storage.CurrentContext, PAUSED_KEY, (byte)0);
            Storage.Put(Storage.CurrentContext, TOTAL_SUPPLY_KEY, INITIAL_SUPPLY);
            Storage.Put(Storage.CurrentContext, TRANSFER_FEE_KEY, DEFAULT_TRANSFER_FEE_BP);
            Storage.Put(Storage.CurrentContext, FEE_COLLECTOR_KEY, owner);
            Storage.Put(Storage.CurrentContext, MINTING_PAUSED_KEY, (byte)0);
            Storage.Put(Storage.CurrentContext, BURNING_PAUSED_KEY, (byte)0);

            // Give initial supply to owner
            var ownerBalanceKey = BALANCE_PREFIX + owner;
            Storage.Put(Storage.CurrentContext, ownerBalanceKey, INITIAL_SUPPLY);

            // Set owner as admin and minter
            Storage.Put(Storage.CurrentContext, ADMIN_PREFIX + owner, (byte)1);
            Storage.Put(Storage.CurrentContext, MINTER_PREFIX + owner, (byte)1);

            OnTransfer(null, owner, INITIAL_SUPPLY);
            OwnershipTransferred(UInt160.Zero, owner);

            Runtime.Log("ProductionNEP17Token deployed successfully");
        }
        #endregion

        #region NEP-17 Standard Implementation
        [Safe]
        public static string symbol()
        {
            return TOKEN_SYMBOL;
        }

        [Safe]
        public static string name()
        {
            return TOKEN_NAME;
        }

        [Safe]
        public static byte decimals()
        {
            return TOKEN_DECIMALS;
        }

        [Safe]
        public static BigInteger totalSupply()
        {
            var supply = Storage.Get(Storage.CurrentContext, TOTAL_SUPPLY_KEY);
            return supply != null ? (BigInteger)supply : 0;
        }

        [Safe]
        public static BigInteger balanceOf(UInt160 account)
        {
            if (account == null || account.IsZero)
                return 0;

            var balanceKey = BALANCE_PREFIX + account;
            var balance = Storage.Get(Storage.CurrentContext, balanceKey);
            return balance != null ? (BigInteger)balance : 0;
        }

        public static bool transfer(UInt160 from, UInt160 to, BigInteger amount, object data)
        {
            return InternalTransfer(from, to, amount, data, true);
        }
        #endregion

        #region Transfer Implementation
        private static bool InternalTransfer(UInt160 from, UInt160 to, BigInteger amount, object data, bool checkWitness)
        {
            // Wrap entire transfer operation in reentrancy guard
            return ExecuteNonReentrant(() => 
            {
                ValidateNotPaused();

            if (from == null || from.IsZero)
                throw new ArgumentException("Invalid from address");

            if (to == null || to.IsZero)
                throw new ArgumentException("Invalid to address");

            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative");

            if (amount == 0)
                return true;

            // Check blacklist
            if (IsBlacklisted(from))
                throw new InvalidOperationException("From address is blacklisted");

            if (IsBlacklisted(to))
                throw new InvalidOperationException("To address is blacklisted");

            // Check witness if required
            if (checkWitness && !Runtime.CheckWitness(from))
                return false;

            // Get balances
            var fromBalance = balanceOf(from);
            if (fromBalance < amount)
                return false;

            // Calculate transfer fee
            var fee = CalculateTransferFee(amount);
            var netAmount = amount - fee;

            if (netAmount <= 0)
                return false;

            var toBalance = balanceOf(to);

            // Update balances
            var fromBalanceKey = BALANCE_PREFIX + from;
            var toBalanceKey = BALANCE_PREFIX + to;

            Storage.Put(Storage.CurrentContext, fromBalanceKey, fromBalance - amount);
            Storage.Put(Storage.CurrentContext, toBalanceKey, toBalance + netAmount);

            // Handle transfer fee
            if (fee > 0)
            {
                var feeCollector = GetFeeCollector();
                var collectorBalance = balanceOf(feeCollector);
                var collectorBalanceKey = BALANCE_PREFIX + feeCollector;
                Storage.Put(Storage.CurrentContext, collectorBalanceKey, collectorBalance + fee);

                OnTransfer(from, feeCollector, fee);
            }

            // Emit transfer event
            OnTransfer(from, to, netAmount);

            // Call onNEP17Payment if receiver is a contract
            if (ContractManagement.GetContract(to) != null)
            {
                Contract.Call(to, "onNEP17Payment", CallFlags.All, from, netAmount, data);
            }

            return true;
            });
        }

        private static BigInteger CalculateTransferFee(BigInteger amount)
        {
            var feeBasisPoints = GetTransferFee();
            if (feeBasisPoints == 0)
                return 0;

            return (amount * feeBasisPoints) / 10000;
        }
        #endregion

        #region Approval System (ERC-20 Style)
        public static bool approve(UInt160 spender, BigInteger amount)
        {
            ValidateNotPaused();

            if (spender == null || spender.IsZero)
                throw new ArgumentException("Invalid spender address");

            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative");

            var owner = Runtime.CallingScriptHash;
            if (!Runtime.CheckWitness(owner))
                return false;

            var allowanceKey = ALLOWANCE_PREFIX + owner + spender;
            Storage.Put(Storage.CurrentContext, allowanceKey, amount);

            OnApproval(owner, spender, amount);
            return true;
        }

        [Safe]
        public static BigInteger allowance(UInt160 owner, UInt160 spender)
        {
            if (owner == null || owner.IsZero || spender == null || spender.IsZero)
                return 0;

            var allowanceKey = ALLOWANCE_PREFIX + owner + spender;
            var allowanceAmount = Storage.Get(Storage.CurrentContext, allowanceKey);
            return allowanceAmount != null ? (BigInteger)allowanceAmount : 0;
        }

        public static bool transferFrom(UInt160 from, UInt160 to, BigInteger amount, object data)
        {
            ValidateNotPaused();

            var spender = Runtime.CallingScriptHash;
            if (!Runtime.CheckWitness(spender))
                return false;

            var currentAllowance = allowance(from, spender);
            if (currentAllowance < amount)
                return false;

            // Update allowance
            var allowanceKey = ALLOWANCE_PREFIX + from + spender;
            Storage.Put(Storage.CurrentContext, allowanceKey, currentAllowance - amount);

            // Perform transfer without checking witness (already verified)
            return InternalTransfer(from, to, amount, data, false);
        }
        #endregion

        #region Minting and Burning
        public static bool mint(UInt160 to, BigInteger amount)
        {
            ValidateNotPaused();
            ValidateMinter();

            if (to == null || to.IsZero)
                throw new ArgumentException("Invalid to address");

            if (amount <= 0)
                throw new ArgumentException("Amount must be positive");

            if (IsMintingPaused())
                throw new InvalidOperationException("Minting is currently paused");

            if (IsBlacklisted(to))
                throw new InvalidOperationException("Cannot mint to blacklisted address");

            var currentSupply = totalSupply();
            var newSupply = currentSupply + amount;

            if (newSupply > MAX_SUPPLY)
                throw new InvalidOperationException("Would exceed maximum supply");

            var toBalance = balanceOf(to);

            // Update balances and total supply
            var toBalanceKey = BALANCE_PREFIX + to;
            Storage.Put(Storage.CurrentContext, toBalanceKey, toBalance + amount);
            Storage.Put(Storage.CurrentContext, TOTAL_SUPPLY_KEY, newSupply);

            OnTransfer(null, to, amount);
            OnMint(Runtime.CallingScriptHash, to, amount);

            return true;
        }

        public static bool burn(BigInteger amount)
        {
            ValidateNotPaused();

            if (amount <= 0)
                throw new ArgumentException("Amount must be positive");

            if (IsBurningPaused())
                throw new InvalidOperationException("Burning is currently paused");

            var from = Runtime.CallingScriptHash;
            if (!Runtime.CheckWitness(from))
                return false;

            if (IsBlacklisted(from))
                throw new InvalidOperationException("Blacklisted address cannot burn");

            var fromBalance = balanceOf(from);
            if (fromBalance < amount)
                return false;

            var currentSupply = totalSupply();

            // Update balance and total supply
            var fromBalanceKey = BALANCE_PREFIX + from;
            Storage.Put(Storage.CurrentContext, fromBalanceKey, fromBalance - amount);
            Storage.Put(Storage.CurrentContext, TOTAL_SUPPLY_KEY, currentSupply - amount);

            OnTransfer(from, null, amount);
            OnBurn(from, amount);

            return true;
        }

        public static bool burnFrom(UInt160 from, BigInteger amount)
        {
            ValidateNotPaused();

            if (from == null || from.IsZero)
                throw new ArgumentException("Invalid from address");

            if (amount <= 0)
                throw new ArgumentException("Amount must be positive");

            if (IsBurningPaused())
                throw new InvalidOperationException("Burning is currently paused");

            var spender = Runtime.CallingScriptHash;
            if (!Runtime.CheckWitness(spender))
                return false;

            if (IsBlacklisted(from))
                throw new InvalidOperationException("Cannot burn from blacklisted address");

            var currentAllowance = allowance(from, spender);
            if (currentAllowance < amount)
                return false;

            var fromBalance = balanceOf(from);
            if (fromBalance < amount)
                return false;

            var currentSupply = totalSupply();

            // Update allowance, balance and total supply
            var allowanceKey = ALLOWANCE_PREFIX + from + spender;
            Storage.Put(Storage.CurrentContext, allowanceKey, currentAllowance - amount);

            var fromBalanceKey = BALANCE_PREFIX + from;
            Storage.Put(Storage.CurrentContext, fromBalanceKey, fromBalance - amount);
            Storage.Put(Storage.CurrentContext, TOTAL_SUPPLY_KEY, currentSupply - amount);

            OnTransfer(from, null, amount);
            OnBurn(from, amount);

            return true;
        }
        #endregion

        #region Access Control
        [Safe]
        public static UInt160 GetOwner()
        {
            var ownerBytes = Storage.Get(Storage.CurrentContext, OWNER_KEY);
            return ownerBytes != null ? (UInt160)ownerBytes : UInt160.Zero;
        }

        public static bool TransferOwnership(UInt160 newOwner)
        {
            ValidateOwner();

            if (newOwner == null || newOwner.IsZero)
                throw new ArgumentException("Invalid new owner address");

            var currentOwner = GetOwner();
            Storage.Put(Storage.CurrentContext, OWNER_KEY, newOwner);

            // Transfer admin and minter roles
            Storage.Put(Storage.CurrentContext, ADMIN_PREFIX + newOwner, (byte)1);
            Storage.Put(Storage.CurrentContext, MINTER_PREFIX + newOwner, (byte)1);
            Storage.Delete(Storage.CurrentContext, ADMIN_PREFIX + currentOwner);
            Storage.Delete(Storage.CurrentContext, MINTER_PREFIX + currentOwner);

            OwnershipTransferred(currentOwner, newOwner);
            return true;
        }

        public static bool SetPaused(bool paused)
        {
            ValidateAdmin();

            Storage.Put(Storage.CurrentContext, PAUSED_KEY, paused ? (byte)1  : (byte)0);
            ContractPaused(paused);
            return true;
        }

        [Safe]
        public static bool IsPaused()
        {
            var pausedBytes = Storage.Get(Storage.CurrentContext, PAUSED_KEY);
            return pausedBytes != null && pausedBytes[0] == 1;
        }

        public static bool SetMintingPaused(bool paused)
        {
            ValidateAdmin();

            Storage.Put(Storage.CurrentContext, MINTING_PAUSED_KEY, paused ? (byte)1  : (byte)0);
            return true;
        }

        [Safe]
        public static bool IsMintingPaused()
        {
            var pausedBytes = Storage.Get(Storage.CurrentContext, MINTING_PAUSED_KEY);
            return pausedBytes != null && pausedBytes[0] == 1;
        }

        public static bool SetBurningPaused(bool paused)
        {
            ValidateAdmin();

            Storage.Put(Storage.CurrentContext, BURNING_PAUSED_KEY, paused ? (byte)1  : (byte)0);
            return true;
        }

        [Safe]
        public static bool IsBurningPaused()
        {
            var pausedBytes = Storage.Get(Storage.CurrentContext, BURNING_PAUSED_KEY);
            return pausedBytes != null && pausedBytes[0] == 1;
        }

        public static bool SetAdmin(UInt160 admin, bool isAdmin)
        {
            ValidateOwner();

            if (admin == null || admin.IsZero)
                throw new ArgumentException("Invalid admin address");

            var adminKey = ADMIN_PREFIX + admin;
            if (isAdmin)
                Storage.Put(Storage.CurrentContext, adminKey, (byte)1);
            else
                Storage.Delete(Storage.CurrentContext, adminKey);

            AdminUpdated(admin, isAdmin);
            return true;
        }

        [Safe]
        public static bool IsAdmin(UInt160 address)
        {
            if (address == null || address.IsZero)
                return false;

            // Owner is always admin
            if (address.Equals(GetOwner()))
                return true;

            var adminKey = ADMIN_PREFIX + address;
            var adminBytes = Storage.Get(Storage.CurrentContext, adminKey);
            return adminBytes != null && adminBytes[0] == 1;
        }

        public static bool SetMinter(UInt160 minter, bool isMinter)
        {
            ValidateAdmin();

            if (minter == null || minter.IsZero)
                throw new ArgumentException("Invalid minter address");

            var minterKey = MINTER_PREFIX + minter;
            if (isMinter)
                Storage.Put(Storage.CurrentContext, minterKey, (byte)1);
            else
                Storage.Delete(Storage.CurrentContext, minterKey);

            MinterUpdated(minter, isMinter);
            return true;
        }

        [Safe]
        public static bool IsMinter(UInt160 address)
        {
            if (address == null || address.IsZero)
                return false;

            var minterKey = MINTER_PREFIX + address;
            var minterBytes = Storage.Get(Storage.CurrentContext, minterKey);
            return minterBytes != null && minterBytes[0] == 1;
        }

        private static void ValidateOwner()
        {
            if (!Runtime.CheckWitness(GetOwner()))
                throw new UnauthorizedAccessException("Only owner can perform this action");
        }

        private static void ValidateAdmin()
        {
            var caller = Runtime.CallingScriptHash;
            if (!IsAdmin(caller))
                throw new UnauthorizedAccessException("Admin role required");

            if (!Runtime.CheckWitness(caller))
                throw new UnauthorizedAccessException("Invalid witness");
        }

        private static void ValidateMinter()
        {
            var caller = Runtime.CallingScriptHash;
            if (!IsMinter(caller))
                throw new UnauthorizedAccessException("Minter role required");

            if (!Runtime.CheckWitness(caller))
                throw new UnauthorizedAccessException("Invalid witness");
        }

        private static void ValidateNotPaused()
        {
            if (IsPaused())
                throw new InvalidOperationException("Contract is currently paused");
        }
        #endregion

        #region Blacklist Management
        public static bool SetBlacklisted(UInt160 address, bool blacklisted)
        {
            ValidateAdmin();

            if (address == null || address.IsZero)
                throw new ArgumentException("Invalid address");

            // Cannot blacklist owner
            if (address.Equals(GetOwner()))
                throw new InvalidOperationException("Cannot blacklist owner");

            var blacklistKey = BLACKLIST_PREFIX + address;
            if (blacklisted)
                Storage.Put(Storage.CurrentContext, blacklistKey, (byte)1);
            else
                Storage.Delete(Storage.CurrentContext, blacklistKey);

            BlacklistUpdated(address, blacklisted);
            return true;
        }

        [Safe]
        public static bool IsBlacklisted(UInt160 address)
        {
            if (address == null || address.IsZero)
                return false;

            var blacklistKey = BLACKLIST_PREFIX + address;
            var blacklistBytes = Storage.Get(Storage.CurrentContext, blacklistKey);
            return blacklistBytes != null && blacklistBytes[0] == 1;
        }
        #endregion

        #region Fee Management
        public static bool SetTransferFee(int feeBasicPoints)
        {
            ValidateAdmin();

            if (feeBasicPoints < 0 || feeBasicPoints > MAX_TRANSFER_FEE_BP)
                throw new ArgumentException("Invalid fee basis points");

            var oldFee = GetTransferFee();
            Storage.Put(Storage.CurrentContext, TRANSFER_FEE_KEY, feeBasicPoints);

            TransferFeeUpdated(oldFee, feeBasicPoints);
            return true;
        }

        [Safe]
        public static int GetTransferFee()
        {
            var feeBytes = Storage.Get(Storage.CurrentContext, TRANSFER_FEE_KEY);
            return (int)(feeBytes != null ? (BigInteger)feeBytes : DEFAULT_TRANSFER_FEE_BP);
        }

        public static bool SetFeeCollector(UInt160 collector)
        {
            ValidateAdmin();

            if (collector == null || collector.IsZero)
                throw new ArgumentException("Invalid collector address");

            var oldCollector = GetFeeCollector();
            Storage.Put(Storage.CurrentContext, FEE_COLLECTOR_KEY, collector);

            FeeCollectorUpdated(oldCollector, collector);
            return true;
        }

        [Safe]
        public static UInt160 GetFeeCollector()
        {
            var collectorBytes = Storage.Get(Storage.CurrentContext, FEE_COLLECTOR_KEY);
            return collectorBytes != null ? (UInt160)collectorBytes : GetOwner();
        }
        #endregion

        #region Utility Methods
        [Safe]
        public static BigInteger GetMaxSupply()
        {
            return MAX_SUPPLY;
        }

        [Safe]
        public static string GetContractInfo()
        {
            return "ProductionNEP17Token v1.0.0 - Enterprise-grade NEP-17 token with governance features";
        }

        [Safe]
        public static object GetTokenInfo()
        {
            return new object[]
            {
                TOKEN_NAME,
                TOKEN_SYMBOL,
                TOKEN_DECIMALS,
                totalSupply(),
                MAX_SUPPLY,
                GetOwner(),
                IsPaused(),
                GetTransferFee(),
                GetFeeCollector()
            };
        }
        #endregion
    }
}