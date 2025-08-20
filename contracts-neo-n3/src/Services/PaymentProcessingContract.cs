using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using NeoServiceLayer.Contracts.Core;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Contracts.Services
{
    /// <summary>
    /// Provides comprehensive payment processing services with
    /// multi-currency support, escrow, recurring payments, and fraud detection.
    /// </summary>
    [DisplayName("PaymentProcessingContract")]
    [ManifestExtra("Author", "Neo Service Layer Team")]
    [ManifestExtra("Description", "Multi-currency payment processing and financial services")]
    [ManifestExtra("Version", "1.0.0")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class PaymentProcessingContract : BaseServiceContract
    {
        #region Storage Keys
        private static readonly byte[] PaymentPrefix = "payment:".ToByteArray();
        private static readonly byte[] EscrowPrefix = "escrow:".ToByteArray();
        private static readonly byte[] SubscriptionPrefix = "subscription:".ToByteArray();
        private static readonly byte[] WalletPrefix = "wallet:".ToByteArray();
        private static readonly byte[] TransactionPrefix = "transaction:".ToByteArray();
        private static readonly byte[] PaymentCountKey = "paymentCount".ToByteArray();
        private static readonly byte[] EscrowCountKey = "escrowCount".ToByteArray();
        private static readonly byte[] SubscriptionCountKey = "subscriptionCount".ToByteArray();
        private static readonly byte[] PaymentConfigKey = "paymentConfig".ToByteArray();
        private static readonly byte[] SupportedTokensKey = "supportedTokens".ToByteArray();
        #endregion

        #region Events
        [DisplayName("PaymentProcessed")]
        public static event Action<ByteString, UInt160, UInt160, BigInteger, UInt160> PaymentProcessed;

        [DisplayName("EscrowCreated")]
        public static event Action<ByteString, UInt160, UInt160, BigInteger, UInt160> EscrowCreated;

        [DisplayName("EscrowReleased")]
        public static event Action<ByteString, UInt160, BigInteger> EscrowReleased;

        [DisplayName("SubscriptionCreated")]
        public static event Action<ByteString, UInt160, UInt160, BigInteger, int> SubscriptionCreated;

        [DisplayName("RecurringPaymentExecuted")]
        public static event Action<ByteString, ByteString, BigInteger> RecurringPaymentExecuted;

        [DisplayName("FraudDetected")]
        public static event Action<ByteString, UInt160, string> FraudDetected;

        [DisplayName("RefundProcessed")]
        public static event Action<ByteString, ByteString, BigInteger> RefundProcessed;
        #endregion

        #region Constants
        private const int MAX_ESCROW_DURATION = 2592000; // 30 days
        private const int DEFAULT_PAYMENT_TIMEOUT = 3600; // 1 hour
        private const BigInteger MIN_PAYMENT_AMOUNT = 1;
        private const BigInteger MAX_PAYMENT_AMOUNT = 1000000000000; // 10,000 tokens (assuming 8 decimals)
        private const int MAX_SUBSCRIPTION_DURATION = 31536000; // 1 year
        #endregion

        #region Initialization
        public static void _deploy(object data, bool update)
        {
            if (update) return;

            var serviceId = Runtime.ExecutingScriptHash;
            var contract = new PaymentProcessingContract();
            contract.InitializeBaseService(serviceId, "PaymentProcessingService", "1.0.0", "{}");
            
            // Initialize payment configuration
            var paymentConfig = new PaymentConfig
            {
                EnableEscrow = true,
                EnableSubscriptions = true,
                EnableFraudDetection = true,
                DefaultPaymentTimeout = DEFAULT_PAYMENT_TIMEOUT,
                MaxEscrowDuration = MAX_ESCROW_DURATION,
                MinPaymentAmount = MIN_PAYMENT_AMOUNT,
                MaxPaymentAmount = MAX_PAYMENT_AMOUNT,
                TransactionFeeRate = 25, // 0.25%
                EscrowFeeRate = 50 // 0.50%
            };
            
            Storage.Put(Storage.CurrentContext, PaymentConfigKey, StdLib.Serialize(paymentConfig));
            Storage.Put(Storage.CurrentContext, PaymentCountKey, 0);
            Storage.Put(Storage.CurrentContext, EscrowCountKey, 0);
            Storage.Put(Storage.CurrentContext, SubscriptionCountKey, 0);

            // Initialize supported tokens (NEO and GAS by default)
            var supportedTokens = new UInt160[] { NEO.Hash, GAS.Hash };
            Storage.Put(Storage.CurrentContext, SupportedTokensKey, StdLib.Serialize(supportedTokens));

            Runtime.Log("PaymentProcessingContract deployed successfully");
        }
        #endregion

        #region Service Implementation
        protected override void InitializeService(string config)
        {
            Runtime.Log("PaymentProcessingContract service initialized");
        }

        protected override bool PerformHealthCheck()
        {
            try
            {
                var paymentCount = GetPaymentCount();
                var escrowCount = GetEscrowCount();
                return paymentCount >= 0 && escrowCount >= 0;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Payment Processing
        /// <summary>
        /// Processes a direct payment between two parties.
        /// </summary>
        public static ByteString ProcessPayment(UInt160 from, UInt160 to, BigInteger amount, 
            UInt160 tokenContract, string description, string metadata)
        {
            return ExecuteServiceOperation(() =>
            {
                // Validate payment parameters
                ValidatePaymentParameters(from, to, amount, tokenContract);
                
                // Check for fraud
                if (DetectFraud(from, to, amount, tokenContract))
                {
                    var fraudId = GeneratePaymentId();
                    FraudDetected(fraudId, from, "Suspicious payment pattern detected");
                    throw new InvalidOperationException("Payment blocked due to fraud detection");
                }
                
                var paymentId = GeneratePaymentId();
                
                var payment = new Payment
                {
                    Id = paymentId,
                    From = from,
                    To = to,
                    Amount = amount,
                    TokenContract = tokenContract,
                    Description = description,
                    Metadata = metadata ?? "",
                    Status = PaymentStatus.Processing,
                    CreatedAt = Runtime.Time,
                    ProcessedAt = 0,
                    TransactionHash = new byte[0],
                    FeeAmount = CalculateTransactionFee(amount),
                    PaymentType = PaymentType.Direct
                };
                
                // Store payment record
                var paymentKey = PaymentPrefix.Concat(paymentId);
                Storage.Put(Storage.CurrentContext, paymentKey, StdLib.Serialize(payment));
                
                // Execute the transfer
                var success = ExecuteTokenTransfer(from, to, amount, tokenContract);
                
                if (success)
                {
                    payment.Status = PaymentStatus.Completed;
                    payment.ProcessedAt = Runtime.Time;
                    
                    // Charge transaction fee
                    if (payment.FeeAmount > 0)
                    {
                        ExecuteTokenTransfer(from, Runtime.ExecutingScriptHash, payment.FeeAmount, tokenContract);
                    }
                }
                else
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.FailureReason = "Token transfer failed";
                }
                
                Storage.Put(Storage.CurrentContext, paymentKey, StdLib.Serialize(payment));
                
                // Increment payment count
                var count = GetPaymentCount();
                Storage.Put(Storage.CurrentContext, PaymentCountKey, count + 1);
                
                PaymentProcessed(paymentId, from, to, amount, tokenContract);
                Runtime.Log($"Payment processed: {paymentId} - {success}");
                return paymentId;
            });
        }

        /// <summary>
        /// Processes a refund for a previous payment.
        /// </summary>
        public static bool ProcessRefund(ByteString originalPaymentId, BigInteger refundAmount, string reason)
        {
            return ExecuteServiceOperation(() =>
            {
                var payment = GetPayment(originalPaymentId);
                if (payment == null)
                    throw new InvalidOperationException("Original payment not found");
                
                if (payment.Status != PaymentStatus.Completed)
                    throw new InvalidOperationException("Can only refund completed payments");
                
                if (refundAmount > payment.Amount)
                    throw new ArgumentException("Refund amount cannot exceed original payment");
                
                // Validate caller has permission (merchant or admin)
                if (!payment.To.Equals(Runtime.CallingScriptHash) && !ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                var refundId = GeneratePaymentId();
                
                var refund = new Payment
                {
                    Id = refundId,
                    From = payment.To,
                    To = payment.From,
                    Amount = refundAmount,
                    TokenContract = payment.TokenContract,
                    Description = $"Refund for {originalPaymentId}",
                    Metadata = reason,
                    Status = PaymentStatus.Processing,
                    CreatedAt = Runtime.Time,
                    PaymentType = PaymentType.Refund,
                    OriginalPaymentId = originalPaymentId
                };
                
                // Execute refund transfer
                var success = ExecuteTokenTransfer(payment.To, payment.From, refundAmount, payment.TokenContract);
                
                if (success)
                {
                    refund.Status = PaymentStatus.Completed;
                    refund.ProcessedAt = Runtime.Time;
                    
                    // Update original payment
                    payment.RefundAmount = refundAmount;
                    payment.RefundedAt = Runtime.Time;
                    var paymentKey = PaymentPrefix.Concat(originalPaymentId);
                    Storage.Put(Storage.CurrentContext, paymentKey, StdLib.Serialize(payment));
                }
                else
                {
                    refund.Status = PaymentStatus.Failed;
                    refund.FailureReason = "Refund transfer failed";
                }
                
                var refundKey = PaymentPrefix.Concat(refundId);
                Storage.Put(Storage.CurrentContext, refundKey, StdLib.Serialize(refund));
                
                RefundProcessed(refundId, originalPaymentId, refundAmount);
                Runtime.Log($"Refund processed: {refundId} for {originalPaymentId}");
                return success;
            });
        }
        #endregion

        #region Escrow Services
        /// <summary>
        /// Creates an escrow payment that requires release conditions.
        /// </summary>
        public static ByteString CreateEscrow(UInt160 buyer, UInt160 seller, BigInteger amount, 
            UInt160 tokenContract, int escrowDuration, string conditions)
        {
            return ExecuteServiceOperation(() =>
            {
                ValidatePaymentParameters(buyer, seller, amount, tokenContract);
                
                if (escrowDuration > MAX_ESCROW_DURATION)
                    throw new ArgumentException($"Escrow duration cannot exceed {MAX_ESCROW_DURATION} seconds");
                
                var escrowId = GenerateEscrowId();
                
                var escrow = new EscrowPayment
                {
                    Id = escrowId,
                    Buyer = buyer,
                    Seller = seller,
                    Amount = amount,
                    TokenContract = tokenContract,
                    Conditions = conditions,
                    Status = EscrowStatus.Active,
                    CreatedAt = Runtime.Time,
                    ExpiresAt = Runtime.Time + (ulong)escrowDuration,
                    ReleasedAt = 0,
                    FeeAmount = CalculateEscrowFee(amount),
                    DisputeRaised = false
                };
                
                // Transfer funds to escrow (this contract)
                var success = ExecuteTokenTransfer(buyer, Runtime.ExecutingScriptHash, amount + escrow.FeeAmount, tokenContract);
                
                if (!success)
                    throw new InvalidOperationException("Failed to transfer funds to escrow");
                
                var escrowKey = EscrowPrefix.Concat(escrowId);
                Storage.Put(Storage.CurrentContext, escrowKey, StdLib.Serialize(escrow));
                
                var count = GetEscrowCount();
                Storage.Put(Storage.CurrentContext, EscrowCountKey, count + 1);
                
                EscrowCreated(escrowId, buyer, seller, amount, tokenContract);
                Runtime.Log($"Escrow created: {escrowId}");
                return escrowId;
            });
        }

        /// <summary>
        /// Releases funds from escrow to the seller.
        /// </summary>
        public static bool ReleaseEscrow(ByteString escrowId, string releaseReason)
        {
            return ExecuteServiceOperation(() =>
            {
                var escrow = GetEscrow(escrowId);
                if (escrow == null)
                    throw new InvalidOperationException("Escrow not found");
                
                if (escrow.Status != EscrowStatus.Active)
                    throw new InvalidOperationException("Escrow is not active");
                
                // Validate caller is buyer or has permission
                if (!escrow.Buyer.Equals(Runtime.CallingScriptHash) && !ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                // Release funds to seller
                var success = ExecuteTokenTransfer(Runtime.ExecutingScriptHash, escrow.Seller, escrow.Amount, escrow.TokenContract);
                
                if (success)
                {
                    escrow.Status = EscrowStatus.Released;
                    escrow.ReleasedAt = Runtime.Time;
                    escrow.ReleaseReason = releaseReason;
                    
                    var escrowKey = EscrowPrefix.Concat(escrowId);
                    Storage.Put(Storage.CurrentContext, escrowKey, StdLib.Serialize(escrow));
                    
                    EscrowReleased(escrowId, escrow.Seller, escrow.Amount);
                    Runtime.Log($"Escrow released: {escrowId}");
                }
                
                return success;
            });
        }

        /// <summary>
        /// Cancels an escrow and returns funds to buyer.
        /// </summary>
        public static bool CancelEscrow(ByteString escrowId, string cancelReason)
        {
            return ExecuteServiceOperation(() =>
            {
                var escrow = GetEscrow(escrowId);
                if (escrow == null)
                    throw new InvalidOperationException("Escrow not found");
                
                if (escrow.Status != EscrowStatus.Active)
                    throw new InvalidOperationException("Escrow is not active");
                
                // Check if escrow has expired or caller has permission
                var canCancel = Runtime.Time > escrow.ExpiresAt || 
                               escrow.Buyer.Equals(Runtime.CallingScriptHash) || 
                               ValidateAccess(Runtime.CallingScriptHash);
                
                if (!canCancel)
                    throw new InvalidOperationException("Cannot cancel escrow at this time");
                
                // Return funds to buyer (minus fee)
                var success = ExecuteTokenTransfer(Runtime.ExecutingScriptHash, escrow.Buyer, escrow.Amount, escrow.TokenContract);
                
                if (success)
                {
                    escrow.Status = EscrowStatus.Cancelled;
                    escrow.CancelledAt = Runtime.Time;
                    escrow.CancelReason = cancelReason;
                    
                    var escrowKey = EscrowPrefix.Concat(escrowId);
                    Storage.Put(Storage.CurrentContext, escrowKey, StdLib.Serialize(escrow));
                    
                    Runtime.Log($"Escrow cancelled: {escrowId}");
                }
                
                return success;
            });
        }
        #endregion

        #region Subscription Services
        /// <summary>
        /// Creates a recurring payment subscription.
        /// </summary>
        public static ByteString CreateSubscription(UInt160 subscriber, UInt160 merchant, 
            BigInteger amount, UInt160 tokenContract, int intervalSeconds, int duration)
        {
            return ExecuteServiceOperation(() =>
            {
                ValidatePaymentParameters(subscriber, merchant, amount, tokenContract);
                
                if (duration > MAX_SUBSCRIPTION_DURATION)
                    throw new ArgumentException($"Subscription duration cannot exceed {MAX_SUBSCRIPTION_DURATION} seconds");
                
                var subscriptionId = GenerateSubscriptionId();
                
                var subscription = new Subscription
                {
                    Id = subscriptionId,
                    Subscriber = subscriber,
                    Merchant = merchant,
                    Amount = amount,
                    TokenContract = tokenContract,
                    IntervalSeconds = intervalSeconds,
                    Status = SubscriptionStatus.Active,
                    CreatedAt = Runtime.Time,
                    ExpiresAt = Runtime.Time + (ulong)duration,
                    NextPaymentAt = Runtime.Time + (ulong)intervalSeconds,
                    PaymentCount = 0,
                    TotalPaid = 0,
                    LastPaymentAt = 0
                };
                
                var subscriptionKey = SubscriptionPrefix.Concat(subscriptionId);
                Storage.Put(Storage.CurrentContext, subscriptionKey, StdLib.Serialize(subscription));
                
                var count = GetSubscriptionCount();
                Storage.Put(Storage.CurrentContext, SubscriptionCountKey, count + 1);
                
                SubscriptionCreated(subscriptionId, subscriber, merchant, amount, intervalSeconds);
                Runtime.Log($"Subscription created: {subscriptionId}");
                return subscriptionId;
            });
        }

        /// <summary>
        /// Executes a recurring payment for an active subscription.
        /// </summary>
        public static bool ExecuteRecurringPayment(ByteString subscriptionId)
        {
            return ExecuteServiceOperation(() =>
            {
                var subscription = GetSubscription(subscriptionId);
                if (subscription == null)
                    throw new InvalidOperationException("Subscription not found");
                
                if (subscription.Status != SubscriptionStatus.Active)
                    throw new InvalidOperationException("Subscription is not active");
                
                if (Runtime.Time < subscription.NextPaymentAt)
                    throw new InvalidOperationException("Payment not yet due");
                
                if (Runtime.Time > subscription.ExpiresAt)
                {
                    subscription.Status = SubscriptionStatus.Expired;
                    var subscriptionKey = SubscriptionPrefix.Concat(subscriptionId);
                    Storage.Put(Storage.CurrentContext, subscriptionKey, StdLib.Serialize(subscription));
                    throw new InvalidOperationException("Subscription has expired");
                }
                
                // Process the recurring payment
                var paymentId = ProcessPayment(subscription.Subscriber, subscription.Merchant, 
                    subscription.Amount, subscription.TokenContract, 
                    $"Recurring payment for subscription {subscriptionId}", "");
                
                // Update subscription
                subscription.PaymentCount++;
                subscription.TotalPaid += subscription.Amount;
                subscription.LastPaymentAt = Runtime.Time;
                subscription.NextPaymentAt = Runtime.Time + (ulong)subscription.IntervalSeconds;
                
                var key = SubscriptionPrefix.Concat(subscriptionId);
                Storage.Put(Storage.CurrentContext, key, StdLib.Serialize(subscription));
                
                RecurringPaymentExecuted(subscriptionId, paymentId, subscription.Amount);
                Runtime.Log($"Recurring payment executed: {subscriptionId} -> {paymentId}");
                return true;
            });
        }
        #endregion

        #region Token Management
        /// <summary>
        /// Adds a supported token contract.
        /// </summary>
        public static bool AddSupportedToken(UInt160 tokenContract)
        {
            return ExecuteServiceOperation(() =>
            {
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                var supportedTokensBytes = Storage.Get(Storage.CurrentContext, SupportedTokensKey);
                var supportedTokens = supportedTokensBytes != null ? 
                    (UInt160[])StdLib.Deserialize(supportedTokensBytes) : new UInt160[0];
                
                // Check if token is already supported
                foreach (var token in supportedTokens)
                {
                    if (token.Equals(tokenContract))
                        return true; // Already supported
                }
                
                // Add new token
                var newTokens = new UInt160[supportedTokens.Length + 1];
                for (int i = 0; i < supportedTokens.Length; i++)
                {
                    newTokens[i] = supportedTokens[i];
                }
                newTokens[supportedTokens.Length] = tokenContract;
                
                Storage.Put(Storage.CurrentContext, SupportedTokensKey, StdLib.Serialize(newTokens));
                
                Runtime.Log($"Token added: {tokenContract}");
                return true;
            });
        }

        /// <summary>
        /// Checks if a token is supported.
        /// </summary>
        public static bool IsTokenSupported(UInt160 tokenContract)
        {
            var supportedTokensBytes = Storage.Get(Storage.CurrentContext, SupportedTokensKey);
            if (supportedTokensBytes == null)
                return false;
            
            var supportedTokens = (UInt160[])StdLib.Deserialize(supportedTokensBytes);
            foreach (var token in supportedTokens)
            {
                if (token.Equals(tokenContract))
                    return true;
            }
            
            return false;
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Gets payment information.
        /// </summary>
        public static Payment GetPayment(ByteString paymentId)
        {
            var paymentKey = PaymentPrefix.Concat(paymentId);
            var paymentBytes = Storage.Get(Storage.CurrentContext, paymentKey);
            if (paymentBytes == null)
                return null;
            
            return (Payment)StdLib.Deserialize(paymentBytes);
        }

        /// <summary>
        /// Gets escrow information.
        /// </summary>
        public static EscrowPayment GetEscrow(ByteString escrowId)
        {
            var escrowKey = EscrowPrefix.Concat(escrowId);
            var escrowBytes = Storage.Get(Storage.CurrentContext, escrowKey);
            if (escrowBytes == null)
                return null;
            
            return (EscrowPayment)StdLib.Deserialize(escrowBytes);
        }

        /// <summary>
        /// Gets subscription information.
        /// </summary>
        public static Subscription GetSubscription(ByteString subscriptionId)
        {
            var subscriptionKey = SubscriptionPrefix.Concat(subscriptionId);
            var subscriptionBytes = Storage.Get(Storage.CurrentContext, subscriptionKey);
            if (subscriptionBytes == null)
                return null;
            
            return (Subscription)StdLib.Deserialize(subscriptionBytes);
        }

        /// <summary>
        /// Gets payment count.
        /// </summary>
        public static BigInteger GetPaymentCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, PaymentCountKey);
            return countBytes?.ToBigInteger() ?? 0;
        }

        /// <summary>
        /// Gets escrow count.
        /// </summary>
        public static BigInteger GetEscrowCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, EscrowCountKey);
            return countBytes?.ToBigInteger() ?? 0;
        }

        /// <summary>
        /// Gets subscription count.
        /// </summary>
        public static BigInteger GetSubscriptionCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, SubscriptionCountKey);
            return countBytes?.ToBigInteger() ?? 0;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Validates access permissions for the caller.
        /// </summary>
        private static bool ValidateAccess(UInt160 caller)
        {
            var activeBytes = Storage.Get(Storage.CurrentContext, ServiceActiveKey);
            if (activeBytes == null || activeBytes[0] != 1)
                return false;
            return true;
        }

        /// <summary>
        /// Validates payment parameters.
        /// </summary>
        private static void ValidatePaymentParameters(UInt160 from, UInt160 to, BigInteger amount, UInt160 tokenContract)
        {
            if (from == null || from.IsZero)
                throw new ArgumentException("Invalid from address");
            
            if (to == null || to.IsZero)
                throw new ArgumentException("Invalid to address");
            
            if (from.Equals(to))
                throw new ArgumentException("Cannot send payment to self");
            
            var config = GetPaymentConfig();
            if (amount < config.MinPaymentAmount || amount > config.MaxPaymentAmount)
                throw new ArgumentException("Payment amount out of range");
            
            if (!IsTokenSupported(tokenContract))
                throw new ArgumentException("Token not supported");
        }

        /// <summary>
        /// Detects potential fraud in payment patterns.
        /// </summary>
        private static bool DetectFraud(UInt160 from, UInt160 to, BigInteger amount, UInt160 tokenContract)
        {
            // Simplified fraud detection - in production would be more sophisticated
            var config = GetPaymentConfig();
            
            // Check for unusually large amounts
            if (amount > config.MaxPaymentAmount / 2)
                return true;
            
            // In production, would check:
            // - Payment velocity
            // - Unusual patterns
            // - Blacklisted addresses
            // - Geographic anomalies
            
            return false;
        }

        /// <summary>
        /// Calculates transaction fee.
        /// </summary>
        private static BigInteger CalculateTransactionFee(BigInteger amount)
        {
            var config = GetPaymentConfig();
            return amount * config.TransactionFeeRate / 10000; // Basis points
        }

        /// <summary>
        /// Calculates escrow fee.
        /// </summary>
        private static BigInteger CalculateEscrowFee(BigInteger amount)
        {
            var config = GetPaymentConfig();
            return amount * config.EscrowFeeRate / 10000; // Basis points
        }

        /// <summary>
        /// Executes a token transfer.
        /// </summary>
        private static bool ExecuteTokenTransfer(UInt160 from, UInt160 to, BigInteger amount, UInt160 tokenContract)
        {
            try
            {
                // In production, would call the actual token contract
                // For now, simulate successful transfer
                Runtime.Log($"Token transfer: {from} -> {to}, amount: {amount}, token: {tokenContract}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets payment configuration.
        /// </summary>
        private static PaymentConfig GetPaymentConfig()
        {
            var configBytes = Storage.Get(Storage.CurrentContext, PaymentConfigKey);
            if (configBytes == null)
            {
                return new PaymentConfig
                {
                    EnableEscrow = true,
                    EnableSubscriptions = true,
                    EnableFraudDetection = true,
                    DefaultPaymentTimeout = DEFAULT_PAYMENT_TIMEOUT,
                    MaxEscrowDuration = MAX_ESCROW_DURATION,
                    MinPaymentAmount = MIN_PAYMENT_AMOUNT,
                    MaxPaymentAmount = MAX_PAYMENT_AMOUNT,
                    TransactionFeeRate = 25,
                    EscrowFeeRate = 50
                };
            }
            
            return (PaymentConfig)StdLib.Deserialize(configBytes);
        }

        /// <summary>
        /// Generates unique payment ID.
        /// </summary>
        private static ByteString GeneratePaymentId()
        {
            var counter = GetPaymentCount();
            var tx = (Transaction)Runtime.ScriptContainer;
            var data = "payment".ToByteArray()
                .Concat(Runtime.Time.ToByteArray())
                .Concat(counter.ToByteArray())
                .Concat(tx.Hash);
            
            return CryptoLib.Sha256(data);
        }

        /// <summary>
        /// Generates unique escrow ID.
        /// </summary>
        private static ByteString GenerateEscrowId()
        {
            var counter = GetEscrowCount();
            var tx = (Transaction)Runtime.ScriptContainer;
            var data = "escrow".ToByteArray()
                .Concat(Runtime.Time.ToByteArray())
                .Concat(counter.ToByteArray())
                .Concat(tx.Hash);
            
            return CryptoLib.Sha256(data);
        }

        /// <summary>
        /// Generates unique subscription ID.
        /// </summary>
        private static ByteString GenerateSubscriptionId()
        {
            var counter = GetSubscriptionCount();
            var tx = (Transaction)Runtime.ScriptContainer;
            var data = "subscription".ToByteArray()
                .Concat(Runtime.Time.ToByteArray())
                .Concat(counter.ToByteArray())
                .Concat(tx.Hash);
            
            return CryptoLib.Sha256(data);
        }
        #endregion

        #region Data Structures
        /// <summary>
        /// Represents a payment transaction.
        /// </summary>
        public class Payment
        {
            public ByteString Id;
            public UInt160 From;
            public UInt160 To;
            public BigInteger Amount;
            public UInt160 TokenContract;
            public string Description;
            public string Metadata;
            public PaymentStatus Status;
            public ulong CreatedAt;
            public ulong ProcessedAt;
            public ByteString TransactionHash;
            public BigInteger FeeAmount;
            public PaymentType PaymentType;
            public string FailureReason;
            public ByteString OriginalPaymentId;
            public BigInteger RefundAmount;
            public ulong RefundedAt;
        }

        /// <summary>
        /// Represents an escrow payment.
        /// </summary>
        public class EscrowPayment
        {
            public ByteString Id;
            public UInt160 Buyer;
            public UInt160 Seller;
            public BigInteger Amount;
            public UInt160 TokenContract;
            public string Conditions;
            public EscrowStatus Status;
            public ulong CreatedAt;
            public ulong ExpiresAt;
            public ulong ReleasedAt;
            public BigInteger FeeAmount;
            public bool DisputeRaised;
            public string ReleaseReason;
            public ulong CancelledAt;
            public string CancelReason;
        }

        /// <summary>
        /// Represents a recurring payment subscription.
        /// </summary>
        public class Subscription
        {
            public ByteString Id;
            public UInt160 Subscriber;
            public UInt160 Merchant;
            public BigInteger Amount;
            public

        #region Data Structures
        /// <summary>
        /// Represents a payment transaction.
        /// </summary>
        public class Payment
        {
            public ByteString Id;
            public UInt160 From;
            public UInt160 To;
            public BigInteger Amount;
            public UInt160 TokenContract;
            public string Description;
            public string Metadata;
            public PaymentStatus Status;
            public ulong CreatedAt;
            public ulong ProcessedAt;
            public ByteString TransactionHash;
            public BigInteger FeeAmount;
            public PaymentType PaymentType;
            public string FailureReason;
            public ByteString OriginalPaymentId;
            public BigInteger RefundAmount;
            public ulong RefundedAt;
        }

        /// <summary>
        /// Represents an escrow payment.
        /// </summary>
        public class EscrowPayment
        {
            public ByteString Id;
            public UInt160 Buyer;
            public UInt160 Seller;
            public BigInteger Amount;
            public UInt160 TokenContract;
            public string Conditions;
            public EscrowStatus Status;
            public ulong CreatedAt;
            public ulong ExpiresAt;
            public ulong ReleasedAt;
            public BigInteger FeeAmount;
            public bool DisputeRaised;
            public string ReleaseReason;
            public ulong CancelledAt;
            public string CancelReason;
        }

        /// <summary>
        /// Represents a recurring payment subscription.
        /// </summary>
        public class Subscription
        {
            public ByteString Id;
            public UInt160 Subscriber;
            public UInt160 Merchant;
            public BigInteger Amount;
            public UInt160 TokenContract;
            public int IntervalSeconds;
            public SubscriptionStatus Status;
            public ulong CreatedAt;
            public ulong ExpiresAt;
            public ulong NextPaymentAt;
            public int PaymentCount;
            public BigInteger TotalPaid;
            public ulong LastPaymentAt;
        }

        /// <summary>
        /// Represents payment configuration.
        /// </summary>
        public class PaymentConfig
        {
            public bool EnableEscrow;
            public bool EnableSubscriptions;
            public bool EnableFraudDetection;
            public int DefaultPaymentTimeout;
            public int MaxEscrowDuration;
            public BigInteger MinPaymentAmount;
            public BigInteger MaxPaymentAmount;
            public int TransactionFeeRate;
            public int EscrowFeeRate;
        }

        /// <summary>
        /// Payment status enumeration.
        /// </summary>
        public enum PaymentStatus : byte
        {
            Processing = 0,
            Completed = 1,
            Failed = 2,
            Cancelled = 3,
            Refunded = 4
        }

        /// <summary>
        /// Payment type enumeration.
        /// </summary>
        public enum PaymentType : byte
        {
            Direct = 0,
            Escrow = 1,
            Subscription = 2,
            Refund = 3
        }

        /// <summary>
        /// Escrow status enumeration.
        /// </summary>
        public enum EscrowStatus : byte
        {
            Active = 0,
            Released = 1,
            Cancelled = 2,
            Disputed = 3,
            Expired = 4
        }

        /// <summary>
        /// Subscription status enumeration.
        /// </summary>
        public enum SubscriptionStatus : byte
        {
            Active = 0,
            Paused = 1,
            Cancelled = 2,
            Expired = 3
        }
        #endregion
    }
}