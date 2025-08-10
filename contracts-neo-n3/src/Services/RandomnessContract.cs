using Neo.SmartContract.Framework;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
// using NeoServiceLayer.Contracts.Core;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NeoServiceLayer.Contracts.Services
{
    /// <summary>
    /// Provides secure random number generation using the Neo Service Layer's
    /// confidential computing infrastructure with Intel SGX.
    /// </summary>
    [DisplayName("RandomnessContract")]
    [ManifestExtra("Author", "Neo Service Layer Team")]
    [ManifestExtra("Description", "Secure random number generation service")]
    [ManifestExtra("Version", "1.0.0")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class RandomnessContract : SmartContract
    {
        #region Storage Keys
        private static readonly ByteString RandomnessRequestPrefix = "randomRequest:";
        private static readonly ByteString RandomnessResultPrefix = "randomResult:";
        private static readonly ByteString RequestCounterKey = "requestCounter";
        private static readonly ByteString ServiceFeeKey = "serviceFee";
        private static readonly ByteString MinRangeKey = "minRange";
        private static readonly ByteString MaxRangeKey = "maxRange";
        private static readonly ByteString MaxBatchSizeKey = "maxBatchSize";
        private static readonly ByteString OracleCallbackKey = "oracleCallback";
        private static readonly ByteString OwnerKey = "owner";
        #endregion

        #region Events
        [DisplayName("RandomnessRequested")]
        public static event Action<UInt160, ByteString, BigInteger, BigInteger, int> RandomnessRequested;
        
        [DisplayName("RandomnessFulfilled")]
        public static event Action<UInt160, ByteString, BigInteger[], bool> RandomnessFulfilled;
        
        [DisplayName("BatchRandomnessRequested")]
        public static event Action<UInt160, ByteString, BigInteger, BigInteger, int> BatchRandomnessRequested;
        
        [DisplayName("ServiceConfigurationUpdated")]
        public static event Action<BigInteger, BigInteger, BigInteger, int> ServiceConfigurationUpdated;
        
        [DisplayName("RandomnessError")]
        public static event Action<UInt160, ByteString, string> RandomnessError;
        #endregion

        #region Constants
        private const int DEFAULT_MAX_BATCH_SIZE = 100;
        private const long DEFAULT_SERVICE_FEE = 1000000; // 0.01 GAS
        private const long DEFAULT_MIN_RANGE = 1;
        private const long DEFAULT_MAX_RANGE = 1000000000;
        #endregion

        #region Initialization
        /// <summary>
        /// Deploys the RandomnessContract.
        /// </summary>
        /// <param name="data">Deployment data</param>
        /// <param name="update">Whether this is an update</param>
        public static void _deploy(object data, bool update)
        {
            if (update) return;

            var tx = Runtime.Transaction;
            var serviceId = Runtime.ExecutingScriptHash;
            
            // Set owner
            Storage.Put(Storage.CurrentContext, OwnerKey, tx.Sender);
            
            // Set default configuration
            Storage.Put(Storage.CurrentContext, ServiceFeeKey, DEFAULT_SERVICE_FEE);
            Storage.Put(Storage.CurrentContext, MaxRangeKey, DEFAULT_MAX_RANGE);
            Storage.Put(Storage.CurrentContext, MinRangeKey, DEFAULT_MIN_RANGE);
            Storage.Put(Storage.CurrentContext, MaxBatchSizeKey, DEFAULT_MAX_BATCH_SIZE);
            Storage.Put(Storage.CurrentContext, RequestCounterKey, 0);
            
            Runtime.Log("RandomnessContract deployed successfully");
        }
        #endregion

        #region Randomness Generation
        /// <summary>
        /// Requests a single random number within the specified range.
        /// </summary>
        /// <param name="minValue">Minimum value (inclusive)</param>
        /// <param name="maxValue">Maximum value (exclusive)</param>
        /// <returns>Request ID for tracking the request</returns>
        public static ByteString RequestRandomness(BigInteger minValue, BigInteger maxValue)
        {
            ValidateRandomnessRequest(minValue, maxValue, 1);
            
            var caller = Runtime.CallingScriptHash;
            var requestId = GenerateRequestId();
            
            // Store request details
            var request = new RandomnessRequest
            {
                Requester = caller,
                MinValue = minValue,
                MaxValue = maxValue,
                Count = 1,
                Timestamp = Runtime.Time,
                Status = RequestStatus.Pending
            };
            
            var requestKey = RandomnessRequestPrefix + requestId;
            Storage.Put(Storage.CurrentContext, requestKey, StdLib.Serialize(request));
            
            // Emit event for off-chain processing
            RandomnessRequested(caller, requestId, minValue, maxValue, 1);
            
            Runtime.Log($"Randomness requested: {requestId} for range [{minValue}, {maxValue})");
            
            return requestId;
        }

        /// <summary>
        /// Requests multiple random numbers within the specified range.
        /// </summary>
        /// <param name="minValue">Minimum value (inclusive)</param>
        /// <param name="maxValue">Maximum value (exclusive)</param>
        /// <param name="count">Number of random values to generate</param>
        /// <returns>Request ID for tracking the request</returns>
        public static ByteString RequestBatchRandomness(BigInteger minValue, BigInteger maxValue, int count)
        {
            ValidateRandomnessRequest(minValue, maxValue, count);
            
            var maxBatchSize = GetMaxBatchSize();
            if (count > maxBatchSize)
                throw new ArgumentException($"Batch size exceeds maximum allowed: {maxBatchSize}");
            
            var caller = Runtime.CallingScriptHash;
            var requestId = GenerateRequestId();
            
            // Store request details
            var request = new RandomnessRequest
            {
                Requester = caller,
                MinValue = minValue,
                MaxValue = maxValue,
                Count = count,
                Timestamp = Runtime.Time,
                Status = RequestStatus.Pending
            };
            
            var requestKey = RandomnessRequestPrefix + requestId;
            Storage.Put(Storage.CurrentContext, requestKey, StdLib.Serialize(request));
            
            // Emit event for off-chain processing
            BatchRandomnessRequested(caller, requestId, minValue, maxValue, count);
            
            Runtime.Log($"Batch randomness requested: {requestId} for {count} values in range [{minValue}, {maxValue})");
            
            return requestId;
        }

        /// <summary>
        /// Fulfills a randomness request with the generated values.
        /// This method is called by the authorized oracle/service provider.
        /// </summary>
        /// <param name="requestId">Request identifier</param>
        /// <param name="randomValues">Generated random values</param>
        /// <param name="proof">Cryptographic proof of randomness (optional)</param>
        /// <returns>True if fulfillment successful</returns>
        public static bool FulfillRandomness(ByteString requestId, BigInteger[] randomValues, string proof)
        {
            // Validate caller is authorized (in production, check oracle permissions)
            if (!Runtime.CheckWitness(GetOwner()))
                throw new InvalidOperationException("Unauthorized fulfillment attempt");
            
            var requestKey = RandomnessRequestPrefix + requestId;
            var requestBytes = Storage.Get(Storage.CurrentContext, requestKey);
            if (requestBytes == null)
                throw new InvalidOperationException("Request not found");
            
            var request = (RandomnessRequest)StdLib.Deserialize(requestBytes);
            if (request.Status != RequestStatus.Pending)
                throw new InvalidOperationException("Request already fulfilled or cancelled");
            
            // Validate random values
            if (randomValues.Length != request.Count)
                throw new ArgumentException("Random values count mismatch");
            
            foreach (var value in randomValues)
            {
                if (value < request.MinValue || value >= request.MaxValue)
                    throw new ArgumentException($"Random value {value} outside requested range [{request.MinValue}, {request.MaxValue})");
            }
            
            // Store results
            var result = new RandomnessResult
            {
                RequestId = requestId,
                Values = randomValues,
                Proof = proof ?? "",
                FulfilledAt = Runtime.Time,
                FulfilledBy = Runtime.CallingScriptHash
            };
            
            var resultKey = RandomnessResultPrefix + requestId;
            Storage.Put(Storage.CurrentContext, resultKey, StdLib.Serialize(result));
            
            // Update request status
            request.Status = RequestStatus.Fulfilled;
            Storage.Put(Storage.CurrentContext, requestKey, StdLib.Serialize(request));
            
            // Emit fulfillment event
            RandomnessFulfilled(request.Requester, requestId, randomValues, true);
            
            Runtime.Log($"Randomness fulfilled: {requestId} with {randomValues.Length} values");
            
            return true;
        }

        /// <summary>
        /// Gets the result of a randomness request.
        /// </summary>
        /// <param name="requestId">Request identifier</param>
        /// <returns>Randomness result or null if not found/fulfilled</returns>
        public static RandomnessResult GetRandomnessResult(ByteString requestId)
        {
            var resultKey = RandomnessResultPrefix + requestId;
            var resultBytes = Storage.Get(Storage.CurrentContext, resultKey);
            if (resultBytes == null)
                return null;
            return (RandomnessResult)StdLib.Deserialize(resultBytes);
        }

        /// <summary>
        /// Gets the status of a randomness request.
        /// </summary>
        /// <param name="requestId">Request identifier</param>
        /// <returns>Request information or null if not found</returns>
        public static RandomnessRequest GetRandomnessRequest(ByteString requestId)
        {
            var requestKey = RandomnessRequestPrefix + requestId;
            var requestBytes = Storage.Get(Storage.CurrentContext, requestKey);
            if (requestBytes == null)
                return null;
            return (RandomnessRequest)StdLib.Deserialize(requestBytes);
        }
        #endregion

        #region Configuration Management
        /// <summary>
        /// Updates service configuration (admin only).
        /// </summary>
        /// <param name="serviceFee">Fee for randomness requests</param>
        /// <param name="minRange">Minimum allowed range value</param>
        /// <param name="maxRange">Maximum allowed range value</param>
        /// <param name="maxBatchSize">Maximum batch size</param>
        /// <returns>True if update successful</returns>
        public static bool UpdateConfiguration(BigInteger serviceFee, BigInteger minRange, BigInteger maxRange, int maxBatchSize)
        {
            // Check admin permissions
            if (!Runtime.CheckWitness(GetOwner()))
                throw new InvalidOperationException("Insufficient permissions");
            
            if (serviceFee < 0)
                throw new ArgumentException("Service fee cannot be negative");
            
            if (minRange >= maxRange)
                throw new ArgumentException("Invalid range: min must be less than max");
            
            if (maxBatchSize <= 0 || maxBatchSize > 1000)
                throw new ArgumentException("Invalid batch size");
            
            Storage.Put(Storage.CurrentContext, ServiceFeeKey, serviceFee);
            Storage.Put(Storage.CurrentContext, MaxRangeKey, maxRange);
            Storage.Put(Storage.CurrentContext, MinRangeKey, minRange);
            Storage.Put(Storage.CurrentContext, MaxBatchSizeKey, maxBatchSize);
            
            ServiceConfigurationUpdated(serviceFee, minRange, maxRange, maxBatchSize);
            Runtime.Log("Randomness service configuration updated");
            
            return true;
        }

        /// <summary>
        /// Gets the current service fee.
        /// </summary>
        /// <returns>Service fee in GAS units</returns>
        public static BigInteger GetServiceFee()
        {
            var feeBytes = Storage.Get(Storage.CurrentContext, ServiceFeeKey);
            return feeBytes != null ? (BigInteger)feeBytes : DEFAULT_SERVICE_FEE;
        }

        /// <summary>
        /// Gets the maximum batch size.
        /// </summary>
        /// <returns>Maximum batch size</returns>
        public static int GetMaxBatchSize()
        {
            var sizeBytes = Storage.Get(Storage.CurrentContext, MaxBatchSizeKey);
            return sizeBytes != null ? (int)(BigInteger)sizeBytes : DEFAULT_MAX_BATCH_SIZE;
        }

        /// <summary>
        /// Gets the allowed range limits.
        /// </summary>
        /// <returns>Tuple of (minRange, maxRange)</returns>
        public static (BigInteger, BigInteger) GetRangeLimits()
        {
            var minBytes = Storage.Get(Storage.CurrentContext, MinRangeKey);
            var maxBytes = Storage.Get(Storage.CurrentContext, MaxRangeKey);
            var minRange = minBytes != null ? (BigInteger)minBytes : DEFAULT_MIN_RANGE;
            var maxRange = maxBytes != null ? (BigInteger)maxBytes : DEFAULT_MAX_RANGE;
            return (minRange, maxRange);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Validates a randomness request.
        /// </summary>
        private static void ValidateRandomnessRequest(BigInteger minValue, BigInteger maxValue, int count)
        {
            if (minValue >= maxValue)
                throw new ArgumentException("Invalid range: min must be less than max");
            
            if (count <= 0)
                throw new ArgumentException("Count must be positive");
            
            var (minRange, maxRange) = GetRangeLimits();
            if (minValue < minRange || maxValue > maxRange)
                throw new ArgumentException($"Range [{minValue}, {maxValue}) outside allowed limits [{minRange}, {maxRange})");
        }

        /// <summary>
        /// Generates a unique request ID.
        /// </summary>
        private static ByteString GenerateRequestId()
        {
            var counter = GetRequestCounter();
            Storage.Put(Storage.CurrentContext, RequestCounterKey, counter + 1);
            
            // Combine timestamp, counter and tx hash
            var tx = Runtime.Transaction;
            var data = ((BigInteger)Runtime.Time).ToString() + counter.ToString() + tx.Hash.ToString();
            return CryptoLib.Sha256(data);
        }

        /// <summary>
        /// Gets the current request counter.
        /// </summary>
        private static BigInteger GetRequestCounter()
        {
            var counterBytes = Storage.Get(Storage.CurrentContext, RequestCounterKey);
            return counterBytes != null ? ((BigInteger)counterBytes) : 0;
        }

        /// <summary>
        /// Gets the contract owner.
        /// </summary>
        public static UInt160 GetOwner()
        {
            return (UInt160)Storage.Get(Storage.CurrentContext, OwnerKey);
        }
        #endregion

        #region Data Structures
        /// <summary>
        /// Represents a randomness request.
        /// </summary>
        public class RandomnessRequest
        {
            public UInt160 Requester;
            public BigInteger MinValue;
            public BigInteger MaxValue;
            public int Count;
            public ulong Timestamp;
            public RequestStatus Status;
        }

        /// <summary>
        /// Represents a randomness result.
        /// </summary>
        public class RandomnessResult
        {
            public ByteString RequestId;
            public BigInteger[] Values;
            public string Proof;
            public ulong FulfilledAt;
            public UInt160 FulfilledBy;
        }

        /// <summary>
        /// Request status enumeration.
        /// </summary>
        public enum RequestStatus : byte
        {
            Pending = 0,
            Fulfilled = 1,
            Cancelled = 2,
            Failed = 3
        }
        #endregion
    }
}