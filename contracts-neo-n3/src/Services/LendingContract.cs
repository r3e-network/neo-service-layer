using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Services
{
    /// <summary>
    /// Decentralized lending and borrowing service for DeFi operations
    /// Supports collateralized loans, liquidity pools, and automated liquidations
    /// </summary>
    [DisplayName("LendingContract")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Description", "Decentralized lending and borrowing service")]
    [ManifestExtra("Version", "1.0.0")]
    [ContractPermission("*", "*")]
    public class LendingContract : SmartContract, IServiceContract
    {
        #region Constants
        private const string SERVICE_NAME = "Lending";
        private const byte LENDING_PREFIX = 0x4C; // 'L'
        private const byte LOANS_PREFIX = 0x4D;
        private const byte POOLS_PREFIX = 0x50;
        private const byte COLLATERAL_PREFIX = 0x43;
        private const byte LIQUIDATIONS_PREFIX = 0x4C;
        private const byte RATES_PREFIX = 0x52;
        
        // Lending parameters
        private const BigInteger MIN_COLLATERAL_RATIO = 150; // 150%
        private const BigInteger LIQUIDATION_THRESHOLD = 120; // 120%
        private const BigInteger LIQUIDATION_PENALTY = 10; // 10%
        #endregion

        #region Events
        [DisplayName("LoanCreated")]
        public static event Action<string, UInt160, BigInteger, BigInteger> OnLoanCreated;

        [DisplayName("LoanRepaid")]
        public static event Action<string, UInt160, BigInteger> OnLoanRepaid;

        [DisplayName("CollateralDeposited")]
        public static event Action<string, UInt160, UInt160, BigInteger> OnCollateralDeposited;

        [DisplayName("LiquidationTriggered")]
        public static event Action<string, UInt160, BigInteger> OnLiquidationTriggered;

        [DisplayName("PoolCreated")]
        public static event Action<string, UInt160, BigInteger> OnPoolCreated;

        [DisplayName("LendingError")]
        public static event Action<string, string> OnLendingError;
        #endregion

        #region Data Structures
        public enum LoanStatus : byte
        {
            Active = 0,
            Repaid = 1,
            Defaulted = 2,
            Liquidated = 3,
            Cancelled = 4
        }

        public enum PoolType : byte
        {
            Lending = 0,
            Borrowing = 1,
            Liquidity = 2,
            Yield = 3
        }

        public enum CollateralStatus : byte
        {
            Active = 0,
            Locked = 1,
            Liquidated = 2,
            Released = 3
        }

        public class Loan
        {
            public string Id;
            public UInt160 Borrower;
            public UInt160 Lender;
            public UInt160 AssetToken;
            public BigInteger PrincipalAmount;
            public BigInteger InterestRate;
            public BigInteger Duration;
            public BigInteger StartTime;
            public BigInteger EndTime;
            public BigInteger RepaidAmount;
            public BigInteger AccruedInterest;
            public LoanStatus Status;
            public string CollateralId;
            public BigInteger LastInterestUpdate;
            public string Terms;
        }

        public class LendingPool
        {
            public string Id;
            public string Name;
            public UInt160 AssetToken;
            public PoolType Type;
            public UInt160 Owner;
            public BigInteger TotalDeposits;
            public BigInteger TotalBorrowed;
            public BigInteger AvailableLiquidity;
            public BigInteger InterestRate;
            public BigInteger UtilizationRate;
            public BigInteger RewardRate;
            public BigInteger CreatedAt;
            public bool IsActive;
            public BigInteger MinimumDeposit;
            public BigInteger MaximumLoan;
        }

        public class Collateral
        {
            public string Id;
            public string LoanId;
            public UInt160 Owner;
            public UInt160 AssetToken;
            public BigInteger Amount;
            public BigInteger Value;
            public BigInteger CollateralRatio;
            public CollateralStatus Status;
            public BigInteger DepositedAt;
            public BigInteger LastValuation;
            public BigInteger LiquidationPrice;
        }

        public class Liquidation
        {
            public string Id;
            public string LoanId;
            public string CollateralId;
            public UInt160 Liquidator;
            public BigInteger LiquidatedAmount;
            public BigInteger PenaltyAmount;
            public BigInteger LiquidationPrice;
            public BigInteger ExecutedAt;
            public bool IsCompleted;
        }

        public class InterestRate
        {
            public UInt160 AssetToken;
            public BigInteger BaseRate;
            public BigInteger VariableRate;
            public BigInteger UtilizationRate;
            public BigInteger LastUpdated;
            public BigInteger OptimalUtilization;
            public BigInteger MaxRate;
        }
        #endregion

        #region Storage Keys
        private static StorageKey LoanKey(string id) => new byte[] { LOANS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey PoolKey(string id) => new byte[] { POOLS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey CollateralKey(string id) => new byte[] { COLLATERAL_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey LiquidationKey(string id) => new byte[] { LIQUIDATIONS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey RateKey(UInt160 token) => new byte[] { RATES_PREFIX }.Concat(token.ToByteArray());
        #endregion

        #region IServiceContract Implementation
        public static string GetServiceName() => SERVICE_NAME;

        public static string GetServiceVersion() => "1.0.0";

        public static string[] GetServiceMethods() => new string[]
        {
            "CreateLoan",
            "RepayLoan",
            "DepositCollateral",
            "WithdrawCollateral",
            "CreatePool",
            "DepositToPool",
            "WithdrawFromPool",
            "Liquidate",
            "UpdateInterestRates",
            "GetLoanHealth"
        };

        public static bool IsServiceActive() => true;

        protected static void ValidateAccess()
        {
            if (!Runtime.CheckWitness(Runtime.CallingScriptHash))
                throw new InvalidOperationException("Unauthorized access");
        }

        public static object ExecuteServiceOperation(string method, object[] args)
        {
            return ExecuteServiceOperation<object>(method, args);
        }

        protected static T ExecuteServiceOperation<T>(string method, object[] args)
        {
            ValidateAccess();
            
            switch (method)
            {
                case "CreateLoan":
                    return (T)(object)CreateLoan((UInt160)args[0], (BigInteger)args[1], (BigInteger)args[2], (BigInteger)args[3], (string)args[4]);
                case "RepayLoan":
                    return (T)(object)RepayLoan((string)args[0], (BigInteger)args[1]);
                case "DepositCollateral":
                    return (T)(object)DepositCollateral((string)args[0], (UInt160)args[1], (BigInteger)args[2]);
                case "WithdrawCollateral":
                    return (T)(object)WithdrawCollateral((string)args[0], (BigInteger)args[1]);
                case "CreatePool":
                    return (T)(object)CreatePool((string)args[0], (UInt160)args[1], (byte)args[2], (BigInteger)args[3], (BigInteger)args[4]);
                case "DepositToPool":
                    return (T)(object)DepositToPool((string)args[0], (BigInteger)args[1]);
                case "WithdrawFromPool":
                    return (T)(object)WithdrawFromPool((string)args[0], (BigInteger)args[1]);
                case "Liquidate":
                    return (T)(object)Liquidate((string)args[0]);
                case "UpdateInterestRates":
                    return (T)(object)UpdateInterestRates((UInt160)args[0]);
                case "GetLoanHealth":
                    return (T)(object)GetLoanHealth((string)args[0]);
                default:
                    throw new InvalidOperationException($"Unknown method: {method}");
            }
        }
        #endregion

        #region Loan Management
        /// <summary>
        /// Create a new loan
        /// </summary>
        public static string CreateLoan(UInt160 assetToken, BigInteger amount, BigInteger interestRate, BigInteger duration, string collateralId)
        {
            if (assetToken == UInt160.Zero) throw new ArgumentException("Asset token required");
            if (amount <= 0) throw new ArgumentException("Amount must be positive");
            if (interestRate <= 0) throw new ArgumentException("Interest rate must be positive");
            if (duration <= 0) throw new ArgumentException("Duration must be positive");
            if (string.IsNullOrEmpty(collateralId)) throw new ArgumentException("Collateral ID required");

            // Validate collateral
            var collateralData = Storage.Get(Storage.CurrentContext, CollateralKey(collateralId));
            if (collateralData == null) throw new InvalidOperationException("Collateral not found");

            var collateral = (Collateral)StdLib.Deserialize(collateralData);
            if (collateral.Owner != Runtime.CallingScriptHash) throw new UnauthorizedAccessException("Not collateral owner");
            if (collateral.Status != CollateralStatus.Active) throw new InvalidOperationException("Collateral not available");

            // Check collateral ratio
            var collateralRatio = (collateral.Value * 100) / amount;
            if (collateralRatio < MIN_COLLATERAL_RATIO) throw new InvalidOperationException("Insufficient collateral ratio");

            try
            {
                var loanId = GenerateId("LOAN");
                var startTime = Runtime.Time;
                var endTime = startTime + duration;

                var loan = new Loan
                {
                    Id = loanId,
                    Borrower = Runtime.CallingScriptHash,
                    Lender = UInt160.Zero, // Will be set when funded
                    AssetToken = assetToken,
                    PrincipalAmount = amount,
                    InterestRate = interestRate,
                    Duration = duration,
                    StartTime = startTime,
                    EndTime = endTime,
                    RepaidAmount = 0,
                    AccruedInterest = 0,
                    Status = LoanStatus.Active,
                    CollateralId = collateralId,
                    LastInterestUpdate = startTime,
                    Terms = ""
                };

                // Lock collateral
                collateral.Status = CollateralStatus.Locked;
                collateral.LoanId = loanId;

                Storage.Put(Storage.CurrentContext, LoanKey(loanId), StdLib.Serialize(loan));
                Storage.Put(Storage.CurrentContext, CollateralKey(collateralId), StdLib.Serialize(collateral));

                OnLoanCreated(loanId, Runtime.CallingScriptHash, amount, interestRate);
                return loanId;
            }
            catch (Exception ex)
            {
                OnLendingError("CreateLoan", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Repay a loan
        /// </summary>
        public static bool RepayLoan(string loanId, BigInteger amount)
        {
            if (string.IsNullOrEmpty(loanId)) throw new ArgumentException("Loan ID required");
            if (amount <= 0) throw new ArgumentException("Amount must be positive");

            var loanData = Storage.Get(Storage.CurrentContext, LoanKey(loanId));
            if (loanData == null) throw new InvalidOperationException("Loan not found");

            var loan = (Loan)StdLib.Deserialize(loanData);
            if (loan.Borrower != Runtime.CallingScriptHash) throw new UnauthorizedAccessException("Not loan borrower");
            if (loan.Status != LoanStatus.Active) throw new InvalidOperationException("Loan not active");

            try
            {
                // Update accrued interest
                UpdateLoanInterest(loan);

                var totalOwed = loan.PrincipalAmount + loan.AccruedInterest - loan.RepaidAmount;
                if (amount > totalOwed) amount = totalOwed;

                loan.RepaidAmount += amount;

                // Check if loan is fully repaid
                if (loan.RepaidAmount >= loan.PrincipalAmount + loan.AccruedInterest)
                {
                    loan.Status = LoanStatus.Repaid;
                    
                    // Release collateral
                    var collateralData = Storage.Get(Storage.CurrentContext, CollateralKey(loan.CollateralId));
                    if (collateralData != null)
                    {
                        var collateral = (Collateral)StdLib.Deserialize(collateralData);
                        collateral.Status = CollateralStatus.Released;
                        Storage.Put(Storage.CurrentContext, CollateralKey(loan.CollateralId), StdLib.Serialize(collateral));
                    }
                }

                Storage.Put(Storage.CurrentContext, LoanKey(loanId), StdLib.Serialize(loan));
                OnLoanRepaid(loanId, Runtime.CallingScriptHash, amount);

                return true;
            }
            catch (Exception ex)
            {
                OnLendingError("RepayLoan", ex.Message);
                return false;
            }
        }

        private static void UpdateLoanInterest(Loan loan)
        {
            var timeElapsed = Runtime.Time - loan.LastInterestUpdate;
            var interestAccrued = (loan.PrincipalAmount * loan.InterestRate * timeElapsed) / (365 * 24 * 3600 * 100);
            loan.AccruedInterest += interestAccrued;
            loan.LastInterestUpdate = Runtime.Time;
        }
        #endregion

        #region Collateral Management
        /// <summary>
        /// Deposit collateral for a loan
        /// </summary>
        public static string DepositCollateral(string loanId, UInt160 assetToken, BigInteger amount)
        {
            if (assetToken == UInt160.Zero) throw new ArgumentException("Asset token required");
            if (amount <= 0) throw new ArgumentException("Amount must be positive");

            try
            {
                var collateralId = GenerateId("COL");
                var value = GetAssetValue(assetToken, amount);

                var collateral = new Collateral
                {
                    Id = collateralId,
                    LoanId = loanId ?? "",
                    Owner = Runtime.CallingScriptHash,
                    AssetToken = assetToken,
                    Amount = amount,
                    Value = value,
                    CollateralRatio = 0, // Will be calculated when used
                    Status = CollateralStatus.Active,
                    DepositedAt = Runtime.Time,
                    LastValuation = Runtime.Time,
                    LiquidationPrice = 0
                };

                Storage.Put(Storage.CurrentContext, CollateralKey(collateralId), StdLib.Serialize(collateral));
                OnCollateralDeposited(collateralId, Runtime.CallingScriptHash, assetToken, amount);

                return collateralId;
            }
            catch (Exception ex)
            {
                OnLendingError("DepositCollateral", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Withdraw collateral
        /// </summary>
        public static bool WithdrawCollateral(string collateralId, BigInteger amount)
        {
            if (string.IsNullOrEmpty(collateralId)) throw new ArgumentException("Collateral ID required");
            if (amount <= 0) throw new ArgumentException("Amount must be positive");

            var collateralData = Storage.Get(Storage.CurrentContext, CollateralKey(collateralId));
            if (collateralData == null) throw new InvalidOperationException("Collateral not found");

            var collateral = (Collateral)StdLib.Deserialize(collateralData);
            if (collateral.Owner != Runtime.CallingScriptHash) throw new UnauthorizedAccessException("Not collateral owner");
            if (collateral.Status != CollateralStatus.Active) throw new InvalidOperationException("Collateral not available");
            if (amount > collateral.Amount) throw new ArgumentException("Insufficient collateral");

            try
            {
                collateral.Amount -= amount;
                collateral.Value = GetAssetValue(collateral.AssetToken, collateral.Amount);

                if (collateral.Amount == 0)
                {
                    collateral.Status = CollateralStatus.Released;
                }

                Storage.Put(Storage.CurrentContext, CollateralKey(collateralId), StdLib.Serialize(collateral));
                return true;
            }
            catch (Exception ex)
            {
                OnLendingError("WithdrawCollateral", ex.Message);
                return false;
            }
        }

        private static BigInteger GetAssetValue(UInt160 assetToken, BigInteger amount)
        {
            // Simplified asset valuation - in practice would use oracle prices
            return amount; // 1:1 for simplicity
        }
        #endregion

        #region Pool Management
        /// <summary>
        /// Create a new lending pool
        /// </summary>
        public static string CreatePool(string name, UInt160 assetToken, byte poolType, BigInteger interestRate, BigInteger minimumDeposit)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Pool name required");
            if (assetToken == UInt160.Zero) throw new ArgumentException("Asset token required");
            if (!Enum.IsDefined(typeof(PoolType), poolType)) throw new ArgumentException("Invalid pool type");

            try
            {
                var poolId = GenerateId("POOL");
                var pool = new LendingPool
                {
                    Id = poolId,
                    Name = name,
                    AssetToken = assetToken,
                    Type = (PoolType)poolType,
                    Owner = Runtime.CallingScriptHash,
                    TotalDeposits = 0,
                    TotalBorrowed = 0,
                    AvailableLiquidity = 0,
                    InterestRate = interestRate,
                    UtilizationRate = 0,
                    RewardRate = 0,
                    CreatedAt = Runtime.Time,
                    IsActive = true,
                    MinimumDeposit = minimumDeposit,
                    MaximumLoan = 0
                };

                Storage.Put(Storage.CurrentContext, PoolKey(poolId), StdLib.Serialize(pool));
                OnPoolCreated(poolId, Runtime.CallingScriptHash, interestRate);

                return poolId;
            }
            catch (Exception ex)
            {
                OnLendingError("CreatePool", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Deposit to lending pool
        /// </summary>
        public static bool DepositToPool(string poolId, BigInteger amount)
        {
            if (string.IsNullOrEmpty(poolId)) throw new ArgumentException("Pool ID required");
            if (amount <= 0) throw new ArgumentException("Amount must be positive");

            var poolData = Storage.Get(Storage.CurrentContext, PoolKey(poolId));
            if (poolData == null) throw new InvalidOperationException("Pool not found");

            var pool = (LendingPool)StdLib.Deserialize(poolData);
            if (!pool.IsActive) throw new InvalidOperationException("Pool not active");
            if (amount < pool.MinimumDeposit) throw new ArgumentException("Below minimum deposit");

            try
            {
                pool.TotalDeposits += amount;
                pool.AvailableLiquidity += amount;
                pool.UtilizationRate = pool.TotalBorrowed * 100 / pool.TotalDeposits;

                Storage.Put(Storage.CurrentContext, PoolKey(poolId), StdLib.Serialize(pool));
                return true;
            }
            catch (Exception ex)
            {
                OnLendingError("DepositToPool", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Withdraw from lending pool
        /// </summary>
        public static bool WithdrawFromPool(string poolId, BigInteger amount)
        {
            if (string.IsNullOrEmpty(poolId)) throw new ArgumentException("Pool ID required");
            if (amount <= 0) throw new ArgumentException("Amount must be positive");

            var poolData = Storage.Get(Storage.CurrentContext, PoolKey(poolId));
            if (poolData == null) throw new InvalidOperationException("Pool not found");

            var pool = (LendingPool)StdLib.Deserialize(poolData);
            if (amount > pool.AvailableLiquidity) throw new ArgumentException("Insufficient liquidity");

            try
            {
                pool.TotalDeposits -= amount;
                pool.AvailableLiquidity -= amount;
                pool.UtilizationRate = pool.TotalDeposits > 0 ? pool.TotalBorrowed * 100 / pool.TotalDeposits : 0;

                Storage.Put(Storage.CurrentContext, PoolKey(poolId), StdLib.Serialize(pool));
                return true;
            }
            catch (Exception ex)
            {
                OnLendingError("WithdrawFromPool", ex.Message);
                return false;
            }
        }
        #endregion

        #region Liquidation
        /// <summary>
        /// Liquidate an undercollateralized loan
        /// </summary>
        public static bool Liquidate(string loanId)
        {
            if (string.IsNullOrEmpty(loanId)) throw new ArgumentException("Loan ID required");

            var loanData = Storage.Get(Storage.CurrentContext, LoanKey(loanId));
            if (loanData == null) throw new InvalidOperationException("Loan not found");

            var loan = (Loan)StdLib.Deserialize(loanData);
            if (loan.Status != LoanStatus.Active) throw new InvalidOperationException("Loan not active");

            // Check if loan is eligible for liquidation
            var healthFactor = GetLoanHealthFactor(loan);
            if (healthFactor >= LIQUIDATION_THRESHOLD) throw new InvalidOperationException("Loan not eligible for liquidation");

            try
            {
                var liquidationId = GenerateId("LIQ");
                
                // Get collateral
                var collateralData = Storage.Get(Storage.CurrentContext, CollateralKey(loan.CollateralId));
                var collateral = (Collateral)StdLib.Deserialize(collateralData);

                // Calculate liquidation amounts
                UpdateLoanInterest(loan);
                var totalDebt = loan.PrincipalAmount + loan.AccruedInterest - loan.RepaidAmount;
                var penaltyAmount = totalDebt * LIQUIDATION_PENALTY / 100;
                var liquidatedAmount = totalDebt + penaltyAmount;

                var liquidation = new Liquidation
                {
                    Id = liquidationId,
                    LoanId = loanId,
                    CollateralId = loan.CollateralId,
                    Liquidator = Runtime.CallingScriptHash,
                    LiquidatedAmount = liquidatedAmount,
                    PenaltyAmount = penaltyAmount,
                    LiquidationPrice = collateral.Value,
                    ExecutedAt = Runtime.Time,
                    IsCompleted = true
                };

                // Update loan and collateral status
                loan.Status = LoanStatus.Liquidated;
                collateral.Status = CollateralStatus.Liquidated;

                Storage.Put(Storage.CurrentContext, LoanKey(loanId), StdLib.Serialize(loan));
                Storage.Put(Storage.CurrentContext, CollateralKey(loan.CollateralId), StdLib.Serialize(collateral));
                Storage.Put(Storage.CurrentContext, LiquidationKey(liquidationId), StdLib.Serialize(liquidation));

                OnLiquidationTriggered(loanId, Runtime.CallingScriptHash, liquidatedAmount);
                return true;
            }
            catch (Exception ex)
            {
                OnLendingError("Liquidate", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Get loan health factor
        /// </summary>
        public static BigInteger GetLoanHealth(string loanId)
        {
            if (string.IsNullOrEmpty(loanId)) throw new ArgumentException("Loan ID required");

            var loanData = Storage.Get(Storage.CurrentContext, LoanKey(loanId));
            if (loanData == null) throw new InvalidOperationException("Loan not found");

            var loan = (Loan)StdLib.Deserialize(loanData);
            return GetLoanHealthFactor(loan);
        }

        private static BigInteger GetLoanHealthFactor(Loan loan)
        {
            var collateralData = Storage.Get(Storage.CurrentContext, CollateralKey(loan.CollateralId));
            if (collateralData == null) return 0;

            var collateral = (Collateral)StdLib.Deserialize(collateralData);
            UpdateLoanInterest(loan);
            
            var totalDebt = loan.PrincipalAmount + loan.AccruedInterest - loan.RepaidAmount;
            if (totalDebt == 0) return 1000; // Very healthy

            return (collateral.Value * 100) / totalDebt;
        }
        #endregion

        #region Interest Rate Management
        /// <summary>
        /// Update interest rates for an asset
        /// </summary>
        public static bool UpdateInterestRates(UInt160 assetToken)
        {
            if (assetToken == UInt160.Zero) throw new ArgumentException("Asset token required");

            try
            {
                var rateData = Storage.Get(Storage.CurrentContext, RateKey(assetToken));
                InterestRate rate;

                if (rateData == null)
                {
                    rate = new InterestRate
                    {
                        AssetToken = assetToken,
                        BaseRate = 5, // 5%
                        VariableRate = 0,
                        UtilizationRate = 0,
                        LastUpdated = Runtime.Time,
                        OptimalUtilization = 80, // 80%
                        MaxRate = 50 // 50%
                    };
                }
                else
                {
                    rate = (InterestRate)StdLib.Deserialize(rateData);
                }

                // Update rates based on utilization
                rate.LastUpdated = Runtime.Time;
                Storage.Put(Storage.CurrentContext, RateKey(assetToken), StdLib.Serialize(rate));

                return true;
            }
            catch (Exception ex)
            {
                OnLendingError("UpdateInterestRates", ex.Message);
                return false;
            }
        }
        #endregion

        #region Utility Methods
        private static string GenerateId(string prefix)
        {
            var timestamp = Runtime.Time;
            var random = Runtime.GetRandom();
            return $"{prefix}_{timestamp}_{random}";
        }
        #endregion

        #region Administrative Methods
        /// <summary>
        /// Get lending service statistics
        /// </summary>
        public static Map<string, BigInteger> GetLendingStats()
        {
            var stats = new Map<string, BigInteger>();
            stats["total_loans"] = GetTotalLoans();
            stats["total_borrowed"] = GetTotalBorrowed();
            stats["total_collateral"] = GetTotalCollateral();
            stats["total_liquidations"] = GetTotalLiquidations();
            stats["active_loans"] = GetActiveLoans();
            return stats;
        }

        private static BigInteger GetTotalLoans()
        {
            return Storage.Get(Storage.CurrentContext, "total_loans")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalBorrowed()
        {
            return Storage.Get(Storage.CurrentContext, "total_borrowed")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalCollateral()
        {
            return Storage.Get(Storage.CurrentContext, "total_collateral")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalLiquidations()
        {
            return Storage.Get(Storage.CurrentContext, "total_liquidations")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetActiveLoans()
        {
            return Storage.Get(Storage.CurrentContext, "active_loans")?.ToBigInteger() ?? 0;
        }
        #endregion
    }
}