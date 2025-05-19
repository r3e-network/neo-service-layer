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
    [DisplayName("VerifiableRandomness")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Email", "info@neoservicelayer.io")]
    [ManifestExtra("Description", "Verifiable Randomness Example for Neo Service Layer")]
    public class VerifiableRandomness : SmartContract
    {
        // Events
        [DisplayName("RandomNumberRequested")]
        public static event Action<UInt160, string, BigInteger, BigInteger> OnRandomNumberRequested;

        [DisplayName("RandomNumberGenerated")]
        public static event Action<string, BigInteger, byte[]> OnRandomNumberGenerated;

        [DisplayName("RandomNumbersGenerated")]
        public static event Action<string, BigInteger[], byte[]> OnRandomNumbersGenerated;

        // Storage keys
        private static readonly byte[] OwnerKey = "owner".ToByteArray();
        private static readonly byte[] RequestPrefix = "request".ToByteArray();
        private static readonly byte[] ResultPrefix = "result".ToByteArray();
        private static readonly byte[] ServiceLayerKey = "serviceLayer".ToByteArray();
        private static readonly byte[] FeeKey = "fee".ToByteArray();

        // Custom properties
        [DisplayName("ServiceLayerAddress")]
        public static UInt160 ServiceLayerAddress()
        {
            return (UInt160)Storage.Get(Storage.CurrentContext, ServiceLayerKey);
        }

        [DisplayName("Fee")]
        public static BigInteger Fee()
        {
            return (BigInteger)Storage.Get(Storage.CurrentContext, FeeKey);
        }

        // Constructor
        public static void _deploy(object data, bool update)
        {
            if (!update)
            {
                // Set initial owner to the contract deployer
                Storage.Put(Storage.CurrentContext, OwnerKey, Runtime.CallingScriptHash);
                
                // Set initial fee to 0.1 GAS
                Storage.Put(Storage.CurrentContext, FeeKey, 10_000_000);
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

        // Set the fee for random number generation
        public static void SetFee(BigInteger fee)
        {
            // Only owner can set fee
            if (!IsOwner()) throw new Exception("No authorization");
            
            // Validate fee
            if (fee < 0)
                throw new Exception("Fee cannot be negative");
                
            // Store fee
            Storage.Put(Storage.CurrentContext, FeeKey, fee);
        }

        // Request a random number
        public static string RequestRandomNumber(BigInteger min, BigInteger max, string seed = null)
        {
            // Validate parameters
            if (min >= max)
                throw new Exception("Min must be less than max");
                
            // Generate request ID
            string requestId = GenerateRequestId();
            
            // Store request data
            var requestData = new RandomNumberRequest
            {
                Requester = Runtime.CallingScriptHash,
                Min = min,
                Max = max,
                Seed = seed,
                Timestamp = Runtime.Time
            };
            
            byte[] requestKey = RequestPrefix.Concat(requestId.ToByteArray());
            Storage.Put(Storage.CurrentContext, requestKey, StdLib.Serialize(requestData));
            
            // Collect fee
            BigInteger fee = Fee();
            if (fee > 0)
            {
                // Transfer GAS from caller to contract
                if (!GAS.Transfer(Runtime.CallingScriptHash, Runtime.ExecutingScriptHash, fee))
                    throw new Exception("Failed to transfer fee");
            }
            
            // Notify random number requested event
            OnRandomNumberRequested(Runtime.CallingScriptHash, requestId, min, max);
            
            return requestId;
        }

        // Generate a random number
        public static BigInteger GenerateRandomNumber(string requestId)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(requestId))
                throw new Exception("Invalid request ID");
                
            // Get request data
            byte[] requestKey = RequestPrefix.Concat(requestId.ToByteArray());
            byte[] requestBytes = Storage.Get(Storage.CurrentContext, requestKey);
            if (requestBytes is null)
                throw new Exception("Request does not exist");
                
            // Check if result already exists
            byte[] resultKey = ResultPrefix.Concat(requestId.ToByteArray());
            byte[] resultBytes = Storage.Get(Storage.CurrentContext, resultKey);
            if (resultBytes != null)
            {
                // Return existing result
                RandomNumberResult result = (RandomNumberResult)StdLib.Deserialize(resultBytes);
                return result.Number;
            }
            
            // Deserialize request data
            RandomNumberRequest request = (RandomNumberRequest)StdLib.Deserialize(requestBytes);
            
            // Call Neo Service Layer to generate random number
            object[] response = (object[])Contract.Call(
                ServiceLayerAddress(),
                "generateRandomNumber",
                CallFlags.All,
                new object[] { request.Min, request.Max, request.Seed }
            );
            
            BigInteger randomNumber = (BigInteger)response[0];
            byte[] proof = (byte[])response[1];
            
            // Store result
            var result = new RandomNumberResult
            {
                Number = randomNumber,
                Proof = proof,
                Timestamp = Runtime.Time
            };
            
            Storage.Put(Storage.CurrentContext, resultKey, StdLib.Serialize(result));
            
            // Notify random number generated event
            OnRandomNumberGenerated(requestId, randomNumber, proof);
            
            return randomNumber;
        }

        // Generate multiple random numbers
        public static BigInteger[] GenerateRandomNumbers(string requestId, BigInteger count)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(requestId))
                throw new Exception("Invalid request ID");
            if (count <= 0)
                throw new Exception("Count must be positive");
                
            // Get request data
            byte[] requestKey = RequestPrefix.Concat(requestId.ToByteArray());
            byte[] requestBytes = Storage.Get(Storage.CurrentContext, requestKey);
            if (requestBytes is null)
                throw new Exception("Request does not exist");
                
            // Check if result already exists
            byte[] resultKey = ResultPrefix.Concat(requestId.ToByteArray());
            byte[] resultBytes = Storage.Get(Storage.CurrentContext, resultKey);
            if (resultBytes != null)
            {
                // Return existing result
                RandomNumbersResult result = (RandomNumbersResult)StdLib.Deserialize(resultBytes);
                return result.Numbers;
            }
            
            // Deserialize request data
            RandomNumberRequest request = (RandomNumberRequest)StdLib.Deserialize(requestBytes);
            
            // Call Neo Service Layer to generate random numbers
            object[] response = (object[])Contract.Call(
                ServiceLayerAddress(),
                "generateRandomNumbers",
                CallFlags.All,
                new object[] { count, request.Min, request.Max, request.Seed }
            );
            
            BigInteger[] randomNumbers = (BigInteger[])response[0];
            byte[] proof = (byte[])response[1];
            
            // Store result
            var result = new RandomNumbersResult
            {
                Numbers = randomNumbers,
                Proof = proof,
                Timestamp = Runtime.Time
            };
            
            Storage.Put(Storage.CurrentContext, resultKey, StdLib.Serialize(result));
            
            // Notify random numbers generated event
            OnRandomNumbersGenerated(requestId, randomNumbers, proof);
            
            return randomNumbers;
        }

        // Verify a random number proof
        public static bool VerifyRandomNumberProof(string requestId)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(requestId))
                throw new Exception("Invalid request ID");
                
            // Get result data
            byte[] resultKey = ResultPrefix.Concat(requestId.ToByteArray());
            byte[] resultBytes = Storage.Get(Storage.CurrentContext, resultKey);
            if (resultBytes is null)
                throw new Exception("Result does not exist");
                
            // Get request data
            byte[] requestKey = RequestPrefix.Concat(requestId.ToByteArray());
            byte[] requestBytes = Storage.Get(Storage.CurrentContext, requestKey);
            if (requestBytes is null)
                throw new Exception("Request does not exist");
                
            // Deserialize data
            RandomNumberRequest request = (RandomNumberRequest)StdLib.Deserialize(requestBytes);
            
            // Check if it's a single number or multiple numbers
            try
            {
                RandomNumberResult result = (RandomNumberResult)StdLib.Deserialize(resultBytes);
                
                // Call Neo Service Layer to verify proof
                return (bool)Contract.Call(
                    ServiceLayerAddress(),
                    "verifyRandomnessProof",
                    CallFlags.All,
                    new object[] { new BigInteger[] { result.Number }, result.Proof, request.Seed }
                );
            }
            catch
            {
                // Try as multiple numbers
                RandomNumbersResult result = (RandomNumbersResult)StdLib.Deserialize(resultBytes);
                
                // Call Neo Service Layer to verify proof
                return (bool)Contract.Call(
                    ServiceLayerAddress(),
                    "verifyRandomnessProof",
                    CallFlags.All,
                    new object[] { result.Numbers, result.Proof, request.Seed }
                );
            }
        }

        // Withdraw fees
        public static bool WithdrawFees(UInt160 to)
        {
            // Only owner can withdraw fees
            if (!IsOwner()) throw new Exception("No authorization");
            
            // Validate parameters
            if (to is null || !to.IsValid)
                throw new Exception("Invalid to address");
                
            // Get contract balance
            BigInteger balance = GAS.BalanceOf(Runtime.ExecutingScriptHash);
            if (balance <= 0)
                return false;
                
            // Transfer GAS to owner
            return GAS.Transfer(Runtime.ExecutingScriptHash, to, balance);
        }

        // Helper methods
        private static bool IsOwner()
        {
            UInt160 owner = (UInt160)Storage.Get(Storage.CurrentContext, OwnerKey);
            return Runtime.CheckWitness(owner);
        }

        private static string GenerateRequestId()
        {
            // Generate a unique request ID based on tx hash and timestamp
            return $"{Runtime.GetTransaction().Hash}-{Runtime.Time}";
        }

        // Data structures
        public class RandomNumberRequest
        {
            public UInt160 Requester;
            public BigInteger Min;
            public BigInteger Max;
            public string Seed;
            public BigInteger Timestamp;
        }

        public class RandomNumberResult
        {
            public BigInteger Number;
            public byte[] Proof;
            public BigInteger Timestamp;
        }

        public class RandomNumbersResult
        {
            public BigInteger[] Numbers;
            public byte[] Proof;
            public BigInteger Timestamp;
        }
    }
}
