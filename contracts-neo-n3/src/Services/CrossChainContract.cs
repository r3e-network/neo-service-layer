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
    /// Provides cross-chain bridge functionality for asset transfers and
    /// message passing between different blockchain networks.
    /// </summary>
    [DisplayName("CrossChainContract")]
    [ManifestExtra("Author", "Neo Service Layer Team")]
    [ManifestExtra("Description", "Cross-chain bridge and asset transfer service")]
    [ManifestExtra("Version", "1.0.0")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class CrossChainContract : BaseServiceContract
    {
        #region Storage Keys
        private static readonly byte[] BridgeRequestPrefix = "bridgeRequest:".ToByteArray();
        private static readonly byte[] ChainConfigPrefix = "chainConfig:".ToByteArray();
        private static readonly byte[] ValidatorPrefix = "validator:".ToByteArray();
        private static readonly byte[] AssetMappingPrefix = "assetMapping:".ToByteArray();
        private static readonly byte[] RequestCounterKey = "requestCounter".ToByteArray();
        private static readonly byte[] BridgeFeeKey = "bridgeFee".ToByteArray();
        private static readonly byte[] MinValidatorsKey = "minValidators".ToByteArray();
        private static readonly byte[] ValidatorCountKey = "validatorCount".ToByteArray();
        private static readonly byte[] ChainCountKey = "chainCount".ToByteArray();
        #endregion

        #region Events
        [DisplayName("BridgeRequestCreated")]
        public static event Action<UInt160, ByteString, string, string, BigInteger, UInt160> BridgeRequestCreated;

        [DisplayName("BridgeRequestSigned")]
        public static event Action<ByteString, UInt160, int> BridgeRequestSigned;

        [DisplayName("BridgeRequestExecuted")]
        public static event Action<ByteString, string, bool> BridgeRequestExecuted;

        [DisplayName("ChainAdded")]
        public static event Action<string, string, bool> ChainAdded;

        [DisplayName("ValidatorAdded")]
        public static event Action<UInt160, string> ValidatorAdded;

        [DisplayName("AssetMapped")]
        public static event Action<string, UInt160, string, UInt160> AssetMapped;
        #endregion

        #region Constants
        private const long DEFAULT_BRIDGE_FEE = 5000000; // 0.05 GAS
        private const int DEFAULT_MIN_VALIDATORS = 3;
        private const int MAX_VALIDATORS = 21;
        private const int MAX_CHAINS = 50;
        #endregion

        #region Initialization
        public static void _deploy(object data, bool update)
        {
            if (update) return;

            var serviceId = Runtime.ExecutingScriptHash;
            var contract = new CrossChainContract();
            contract.InitializeBaseService(serviceId, "CrossChainService", "1.0.0", "{}");
            
            Storage.Put(Storage.CurrentContext, BridgeFeeKey, DEFAULT_BRIDGE_FEE);
            Storage.Put(Storage.CurrentContext, MinValidatorsKey, DEFAULT_MIN_VALIDATORS);
            Storage.Put(Storage.CurrentContext, RequestCounterKey, 0);
            Storage.Put(Storage.CurrentContext, ValidatorCountKey, 0);
            Storage.Put(Storage.CurrentContext, ChainCountKey, 0);

            Runtime.Log("CrossChainContract deployed successfully");
        }
        #endregion

        #region Service Implementation
        protected override void InitializeService(string config)
        {
            Runtime.Log("CrossChainContract service initialized");
        }

        protected override bool PerformHealthCheck()
        {
            try
            {
                var validatorCount = GetValidatorCount();
                var minValidators = GetMinValidators();
                return validatorCount >= minValidators;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Bridge Operations
        /// <summary>
        /// Creates a cross-chain bridge request for asset transfer.
        /// </summary>
        public static ByteString CreateBridgeRequest(string targetChain, string targetAddress, 
            UInt160 assetContract, BigInteger amount, string memo)
        {
            return ExecuteServiceOperation(() =>
            {
                var caller = Runtime.CallingScriptHash;
                var requestId = GenerateRequestId();
                var fee = GetBridgeFee();
                
                // Validate inputs
                if (string.IsNullOrEmpty(targetChain))
                    throw new ArgumentException("Target chain cannot be empty");
                if (string.IsNullOrEmpty(targetAddress))
                    throw new ArgumentException("Target address cannot be empty");
                if (amount <= 0)
                    throw new ArgumentException("Amount must be positive");
                
                // Verify target chain is supported
                var chainConfig = GetChainConfig(targetChain);
                if (chainConfig == null || !chainConfig.IsActive)
                    throw new InvalidOperationException("Target chain not supported");
                
                // Verify asset mapping exists
                var assetMapping = GetAssetMapping(targetChain, assetContract);
                if (assetMapping == null)
                    throw new InvalidOperationException("Asset not supported on target chain");
                
                // Create bridge request
                var request = new BridgeRequest
                {
                    Id = requestId,
                    Requester = caller,
                    SourceChain = "neo-n3",
                    TargetChain = targetChain,
                    TargetAddress = targetAddress,
                    AssetContract = assetContract,
                    Amount = amount,
                    Fee = fee,
                    Memo = memo,
                    CreatedAt = Runtime.Time,
                    Status = BridgeStatus.Pending,
                    Signatures = new UInt160[0],
                    ExecutionTxHash = ""
                };
                
                // Store request
                var requestKey = BridgeRequestPrefix.Concat(requestId);
                Storage.Put(Storage.CurrentContext, requestKey, StdLib.Serialize(request));
                
                // Lock assets (simplified - in production would transfer to bridge contract)
                // TransferAssetToBridge(caller, assetContract, amount);
                
                BridgeRequestCreated(caller, requestId, targetChain, targetAddress, amount, assetContract);
                Runtime.Log($"Bridge request created: {requestId} to {targetChain}");
                return requestId;
            });
        }

        /// <summary>
        /// Signs a bridge request (validator only).
        /// </summary>
        public static bool SignBridgeRequest(ByteString requestId, string signature)
        {
            return ExecuteServiceOperation(() =>
            {
                var caller = Runtime.CallingScriptHash;
                
                // Verify caller is a validator
                if (!IsValidator(caller))
                    throw new InvalidOperationException("Only validators can sign bridge requests");
                
                var request = GetBridgeRequest(requestId);
                if (request == null)
                    throw new InvalidOperationException("Bridge request not found");
                
                if (request.Status != BridgeStatus.Pending)
                    throw new InvalidOperationException("Request not in pending status");
                
                // Check if validator already signed
                foreach (var existingSignature in request.Signatures)
                {
                    if (existingSignature.Equals(caller))
                        throw new InvalidOperationException("Validator already signed");
                }
                
                // Add signature
                var newSignatures = new UInt160[request.Signatures.Length + 1];
                for (int i = 0; i < request.Signatures.Length; i++)
                {
                    newSignatures[i] = request.Signatures[i];
                }
                newSignatures[request.Signatures.Length] = caller;
                request.Signatures = newSignatures;
                
                // Check if enough signatures collected
                var minValidators = GetMinValidators();
                if (request.Signatures.Length >= minValidators)
                {
                    request.Status = BridgeStatus.Approved;
                }
                
                // Store updated request
                var requestKey = BridgeRequestPrefix.Concat(requestId);
                Storage.Put(Storage.CurrentContext, requestKey, StdLib.Serialize(request));
                
                BridgeRequestSigned(requestId, caller, request.Signatures.Length);
                Runtime.Log($"Bridge request signed: {requestId} by {caller}");
                return true;
            });
        }

        /// <summary>
        /// Executes an approved bridge request.
        /// </summary>
        public static bool ExecuteBridgeRequest(ByteString requestId, string executionTxHash)
        {
            return ExecuteServiceOperation(() =>
            {
                var caller = Runtime.CallingScriptHash;
                
                // Verify caller is a validator
                if (!IsValidator(caller))
                    throw new InvalidOperationException("Only validators can execute bridge requests");
                
                var request = GetBridgeRequest(requestId);
                if (request == null)
                    throw new InvalidOperationException("Bridge request not found");
                
                if (request.Status != BridgeStatus.Approved)
                    throw new InvalidOperationException("Request not approved");
                
                // Mark as executed
                request.Status = BridgeStatus.Executed;
                request.ExecutionTxHash = executionTxHash;
                
                var requestKey = BridgeRequestPrefix.Concat(requestId);
                Storage.Put(Storage.CurrentContext, requestKey, StdLib.Serialize(request));
                
                BridgeRequestExecuted(requestId, executionTxHash, true);
                Runtime.Log($"Bridge request executed: {requestId}");
                return true;
            });
        }

        /// <summary>
        /// Gets bridge request information.
        /// </summary>
        public static BridgeRequest GetBridgeRequest(ByteString requestId)
        {
            var requestKey = BridgeRequestPrefix.Concat(requestId);
            var requestBytes = Storage.Get(Storage.CurrentContext, requestKey);
            if (requestBytes == null)
                return null;
            
            return (BridgeRequest)StdLib.Deserialize(requestBytes);
        }
        #endregion

        #region Chain Management
        /// <summary>
        /// Adds a new supported blockchain.
        /// </summary>
        public static bool AddChain(string chainId, string chainName, string endpoint, bool isActive)
        {
            return ExecuteServiceOperation(() =>
            {
                // Verify caller has admin permissions
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                var chainCount = GetChainCount();
                if (chainCount >= MAX_CHAINS)
                    throw new InvalidOperationException("Maximum chains limit reached");
                
                var chainConfig = new ChainConfig
                {
                    Id = chainId,
                    Name = chainName,
                    Endpoint = endpoint,
                    IsActive = isActive,
                    AddedAt = Runtime.Time,
                    BlockHeight = 0,
                    LastUpdate = Runtime.Time
                };
                
                var chainKey = ChainConfigPrefix.Concat(chainId.ToByteArray());
                Storage.Put(Storage.CurrentContext, chainKey, StdLib.Serialize(chainConfig));
                Storage.Put(Storage.CurrentContext, ChainCountKey, chainCount + 1);
                
                ChainAdded(chainId, chainName, isActive);
                Runtime.Log($"Chain added: {chainId} - {chainName}");
                return true;
            });
        }

        /// <summary>
        /// Gets chain configuration.
        /// </summary>
        public static ChainConfig GetChainConfig(string chainId)
        {
            var chainKey = ChainConfigPrefix.Concat(chainId.ToByteArray());
            var chainBytes = Storage.Get(Storage.CurrentContext, chainKey);
            if (chainBytes == null)
                return null;
            
            return (ChainConfig)StdLib.Deserialize(chainBytes);
        }

        /// <summary>
        /// Gets the total number of supported chains.
        /// </summary>
        public static int GetChainCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, ChainCountKey);
            return (int)(countBytes?.ToBigInteger() ?? 0);
        }
        #endregion

        #region Validator Management
        /// <summary>
        /// Adds a new bridge validator.
        /// </summary>
        public static bool AddValidator(UInt160 validatorAddress, string validatorInfo)
        {
            return ExecuteServiceOperation(() =>
            {
                // Verify caller has admin permissions
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                var validatorCount = GetValidatorCount();
                if (validatorCount >= MAX_VALIDATORS)
                    throw new InvalidOperationException("Maximum validators limit reached");
                
                if (IsValidator(validatorAddress))
                    throw new InvalidOperationException("Address is already a validator");
                
                var validator = new ValidatorInfo
                {
                    Address = validatorAddress,
                    Info = validatorInfo,
                    AddedAt = Runtime.Time,
                    IsActive = true,
                    RequestsSigned = 0,
                    LastActivity = Runtime.Time
                };
                
                var validatorKey = ValidatorPrefix.Concat(validatorAddress);
                Storage.Put(Storage.CurrentContext, validatorKey, StdLib.Serialize(validator));
                Storage.Put(Storage.CurrentContext, ValidatorCountKey, validatorCount + 1);
                
                ValidatorAdded(validatorAddress, validatorInfo);
                Runtime.Log($"Validator added: {validatorAddress}");
                return true;
            });
        }

        /// <summary>
        /// Checks if an address is a validator.
        /// </summary>
        public static bool IsValidator(UInt160 address)
        {
            var validatorKey = ValidatorPrefix.Concat(address);
            var validatorBytes = Storage.Get(Storage.CurrentContext, validatorKey);
            if (validatorBytes == null)
                return false;
            
            var validator = (ValidatorInfo)StdLib.Deserialize(validatorBytes);
            return validator.IsActive;
        }

        /// <summary>
        /// Gets the total number of validators.
        /// </summary>
        public static int GetValidatorCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, ValidatorCountKey);
            return (int)(countBytes?.ToBigInteger() ?? 0);
        }

        /// <summary>
        /// Gets the minimum number of validators required.
        /// </summary>
        public static int GetMinValidators()
        {
            var minBytes = Storage.Get(Storage.CurrentContext, MinValidatorsKey);
            return (int)(minBytes?.ToBigInteger() ?? DEFAULT_MIN_VALIDATORS);
        }
        #endregion

        #region Asset Mapping
        /// <summary>
        /// Maps an asset between chains.
        /// </summary>
        public static bool MapAsset(string targetChain, UInt160 sourceAsset, string targetAssetId, UInt160 targetAsset)
        {
            return ExecuteServiceOperation(() =>
            {
                // Verify caller has admin permissions
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                var mapping = new AssetMapping
                {
                    SourceChain = "neo-n3",
                    TargetChain = targetChain,
                    SourceAsset = sourceAsset,
                    TargetAssetId = targetAssetId,
                    TargetAsset = targetAsset,
                    IsActive = true,
                    CreatedAt = Runtime.Time
                };
                
                var mappingKey = AssetMappingPrefix.Concat(targetChain.ToByteArray()).Concat(sourceAsset);
                Storage.Put(Storage.CurrentContext, mappingKey, StdLib.Serialize(mapping));
                
                AssetMapped(targetChain, sourceAsset, targetAssetId, targetAsset);
                Runtime.Log($"Asset mapped: {sourceAsset} -> {targetChain}:{targetAssetId}");
                return true;
            });
        }

        /// <summary>
        /// Gets asset mapping for a specific chain and asset.
        /// </summary>
        public static AssetMapping GetAssetMapping(string targetChain, UInt160 sourceAsset)
        {
            var mappingKey = AssetMappingPrefix.Concat(targetChain.ToByteArray()).Concat(sourceAsset);
            var mappingBytes = Storage.Get(Storage.CurrentContext, mappingKey);
            if (mappingBytes == null)
                return null;
            
            return (AssetMapping)StdLib.Deserialize(mappingBytes);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Gets the bridge fee.
        /// </summary>
        private static BigInteger GetBridgeFee()
        {
            var feeBytes = Storage.Get(Storage.CurrentContext, BridgeFeeKey);
            return feeBytes?.ToBigInteger() ?? DEFAULT_BRIDGE_FEE;
        }

        /// <summary>
        /// Generates a unique request ID.
        /// </summary>
        private static ByteString GenerateRequestId()
        {
            var counter = GetRequestCounter();
            Storage.Put(Storage.CurrentContext, RequestCounterKey, counter + 1);
            
            var tx = (Transaction)Runtime.ScriptContainer;
            var data = Runtime.Time.ToByteArray()
                .Concat(counter.ToByteArray())
                .Concat(tx.Hash);
            
            return CryptoLib.Sha256(data);
        }

        /// <summary>
        /// Gets the current request counter.
        /// </summary>
        private static BigInteger GetRequestCounter()
        {
            var counterBytes = Storage.Get(Storage.CurrentContext, RequestCounterKey);
            return counterBytes?.ToBigInteger() ?? 0;
        }

        /// <summary>
        /// Executes a service operation with proper error handling.
        /// </summary>
        private static T ExecuteServiceOperation<T>(Func<T> operation)
        {
            ValidateServiceActive();
            IncrementRequestCount();

            try
            {
                return operation();
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                throw;
            }
        }
        #endregion

        #region Data Structures
        /// <summary>
        /// Represents a cross-chain bridge request.
        /// </summary>
        public class BridgeRequest
        {
            public ByteString Id;
            public UInt160 Requester;
            public string SourceChain;
            public string TargetChain;
            public string TargetAddress;
            public UInt160 AssetContract;
            public BigInteger Amount;
            public BigInteger Fee;
            public string Memo;
            public ulong CreatedAt;
            public BridgeStatus Status;
            public UInt160[] Signatures;
            public string ExecutionTxHash;
        }

        /// <summary>
        /// Represents a blockchain configuration.
        /// </summary>
        public class ChainConfig
        {
            public string Id;
            public string Name;
            public string Endpoint;
            public bool IsActive;
            public ulong AddedAt;
            public BigInteger BlockHeight;
            public ulong LastUpdate;
        }

        /// <summary>
        /// Represents a bridge validator.
        /// </summary>
        public class ValidatorInfo
        {
            public UInt160 Address;
            public string Info;
            public ulong AddedAt;
            public bool IsActive;
            public int RequestsSigned;
            public ulong LastActivity;
        }

        /// <summary>
        /// Represents an asset mapping between chains.
        /// </summary>
        public class AssetMapping
        {
            public string SourceChain;
            public string TargetChain;
            public UInt160 SourceAsset;
            public string TargetAssetId;
            public UInt160 TargetAsset;
            public bool IsActive;
            public ulong CreatedAt;
        }

        /// <summary>
        /// Bridge request status enumeration.
        /// </summary>
        public enum BridgeStatus : byte
        {
            Pending = 0,
            Approved = 1,
            Executed = 2,
            Failed = 3,
            Cancelled = 4
        }
        #endregion
    }
}