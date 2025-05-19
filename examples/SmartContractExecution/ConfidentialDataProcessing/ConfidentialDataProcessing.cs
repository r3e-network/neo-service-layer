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
    [DisplayName("ConfidentialDataProcessing")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Email", "info@neoservicelayer.io")]
    [ManifestExtra("Description", "Confidential Data Processing Example for Neo Service Layer")]
    public class ConfidentialDataProcessing : SmartContract
    {
        // Events
        [DisplayName("DataSubmitted")]
        public static event Action<UInt160, string, byte[]> OnDataSubmitted;

        [DisplayName("DataProcessed")]
        public static event Action<string, string> OnDataProcessed;

        [DisplayName("ResultRetrieved")]
        public static event Action<UInt160, string> OnResultRetrieved;

        // Storage keys
        private static readonly byte[] OwnerKey = "owner".ToByteArray();
        private static readonly byte[] DataPrefix = "data".ToByteArray();
        private static readonly byte[] ResultPrefix = "result".ToByteArray();
        private static readonly byte[] ServiceLayerKey = "serviceLayer".ToByteArray();
        private static readonly byte[] FeeKey = "fee".ToByteArray();

        // Data processing types
        public enum ProcessingType : byte
        {
            MachineLearning = 0,
            DataAnalytics = 1,
            PrivateSetIntersection = 2,
            SecureAggregation = 3,
            ConfidentialComputation = 4
        }

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

        // Set the fee for data processing
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

        // Submit encrypted data for processing
        public static string SubmitData(byte[] encryptedData, ProcessingType processingType)
        {
            // Validate parameters
            if (encryptedData is null || encryptedData.Length == 0)
                throw new Exception("Invalid encrypted data");
                
            // Generate data ID
            string dataId = GenerateDataId();
            
            // Store data submission
            var dataSubmission = new DataSubmission
            {
                Submitter = Runtime.CallingScriptHash,
                ProcessingType = processingType,
                EncryptedData = encryptedData,
                Timestamp = Runtime.Time,
                Status = DataStatus.Submitted
            };
            
            byte[] dataKey = DataPrefix.Concat(dataId.ToByteArray());
            Storage.Put(Storage.CurrentContext, dataKey, StdLib.Serialize(dataSubmission));
            
            // Collect fee
            BigInteger fee = Fee();
            if (fee > 0)
            {
                // Transfer GAS from caller to contract
                if (!GAS.Transfer(Runtime.CallingScriptHash, Runtime.ExecutingScriptHash, fee))
                    throw new Exception("Failed to transfer fee");
            }
            
            // Notify data submitted event
            OnDataSubmitted(Runtime.CallingScriptHash, dataId, encryptedData);
            
            return dataId;
        }

        // Process the submitted data
        public static bool ProcessData(string dataId)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(dataId))
                throw new Exception("Invalid data ID");
                
            // Get data submission
            byte[] dataKey = DataPrefix.Concat(dataId.ToByteArray());
            byte[] dataBytes = Storage.Get(Storage.CurrentContext, dataKey);
            if (dataBytes is null)
                throw new Exception("Data submission not found");
                
            // Deserialize data submission
            DataSubmission dataSubmission = (DataSubmission)StdLib.Deserialize(dataBytes);
            
            // Check if data is already processed
            if (dataSubmission.Status == DataStatus.Processed)
                return true;
                
            // Call Neo Service Layer to process the data
            bool success = (bool)Contract.Call(
                ServiceLayerAddress(),
                "processConfidentialData",
                CallFlags.All,
                new object[] { dataId, (byte)dataSubmission.ProcessingType, dataSubmission.EncryptedData }
            );
            
            if (success)
            {
                // Update data submission status
                dataSubmission.Status = DataStatus.Processed;
                Storage.Put(Storage.CurrentContext, dataKey, StdLib.Serialize(dataSubmission));
                
                // Notify data processed event
                OnDataProcessed(dataId, ((byte)dataSubmission.ProcessingType).ToString());
            }
            
            return success;
        }

        // Retrieve the processed result
        public static byte[] RetrieveResult(string dataId)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(dataId))
                throw new Exception("Invalid data ID");
                
            // Get data submission
            byte[] dataKey = DataPrefix.Concat(dataId.ToByteArray());
            byte[] dataBytes = Storage.Get(Storage.CurrentContext, dataKey);
            if (dataBytes is null)
                throw new Exception("Data submission not found");
                
            // Deserialize data submission
            DataSubmission dataSubmission = (DataSubmission)StdLib.Deserialize(dataBytes);
            
            // Check if data is processed
            if (dataSubmission.Status != DataStatus.Processed)
                throw new Exception("Data not yet processed");
                
            // Check if caller is the submitter
            if (!Runtime.CheckWitness(dataSubmission.Submitter))
                throw new Exception("Only the submitter can retrieve the result");
                
            // Get result from Neo Service Layer
            byte[] result = (byte[])Contract.Call(
                ServiceLayerAddress(),
                "getConfidentialDataResult",
                CallFlags.All,
                new object[] { dataId, dataSubmission.Submitter }
            );
            
            // Notify result retrieved event
            OnResultRetrieved(dataSubmission.Submitter, dataId);
            
            return result;
        }

        // Get data submission information
        public static DataInfo GetDataInfo(string dataId)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(dataId))
                throw new Exception("Invalid data ID");
                
            // Get data submission
            byte[] dataKey = DataPrefix.Concat(dataId.ToByteArray());
            byte[] dataBytes = Storage.Get(Storage.CurrentContext, dataKey);
            if (dataBytes is null)
                throw new Exception("Data submission not found");
                
            // Deserialize data submission
            DataSubmission dataSubmission = (DataSubmission)StdLib.Deserialize(dataBytes);
            
            // Create data info
            return new DataInfo
            {
                Submitter = dataSubmission.Submitter,
                ProcessingType = (byte)dataSubmission.ProcessingType,
                Timestamp = dataSubmission.Timestamp,
                Status = (byte)dataSubmission.Status
            };
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

        private static string GenerateDataId()
        {
            // Generate a unique data ID based on tx hash and timestamp
            return $"{Runtime.GetTransaction().Hash}-{Runtime.Time}";
        }

        // Data structures
        public enum DataStatus : byte
        {
            Submitted = 0,
            Processed = 1
        }

        public class DataSubmission
        {
            public UInt160 Submitter;
            public ProcessingType ProcessingType;
            public byte[] EncryptedData;
            public BigInteger Timestamp;
            public DataStatus Status;
        }

        public class DataInfo
        {
            public UInt160 Submitter;
            public byte ProcessingType;
            public BigInteger Timestamp;
            public byte Status;
        }
    }
}
