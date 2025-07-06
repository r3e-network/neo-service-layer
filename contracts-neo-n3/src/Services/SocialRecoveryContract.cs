using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.Numerics;

namespace NeoServiceLayer.Contracts.Services
{
    [DisplayName("SocialRecoveryContract")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Description", "Decentralized social recovery network for abstract accounts")]
    [ContractPermission("*", "onNEP17Payment", "transfer")]
    [SupportedStandards("NEP-17")]
    public class SocialRecoveryContract : BaseServiceContract
    {
        #region Constants
        private const string SERVICE_ID = "socialrecovery";
        private const string SERVICE_NAME = "Social Recovery Network";
        private const string SERVICE_VERSION = "1.0.0";
        
        // Reputation thresholds
        private const int MIN_REPUTATION_SCORE = 100;
        private const int MAX_REPUTATION_SCORE = 10000;
        private const int REPUTATION_DECAY_RATE = 10; // Points per month
        
        // Recovery parameters
        private const int MIN_GUARDIANS = 3;
        private const int MAX_GUARDIANS = 20;
        private const int RECOVERY_TIMEOUT = 7 * 24 * 3600; // 7 days in seconds
        private const int EMERGENCY_RECOVERY_TIMEOUT = 24 * 3600; // 24 hours
        
        // Staking parameters
        private const long MIN_GUARDIAN_STAKE = 100_00000000; // 100 GAS
        private const long SLASH_PERCENTAGE = 10; // 10% slash for malicious behavior
        #endregion

        #region Storage Keys
        private static readonly byte[] GUARDIAN_REGISTRY_PREFIX = "guardian_".ToByteArray();
        private static readonly byte[] RECOVERY_REQUEST_PREFIX = "recovery_".ToByteArray();
        private static readonly byte[] TRUST_GRAPH_PREFIX = "trust_".ToByteArray();
        private static readonly byte[] ACCOUNT_RECOVERY_PREFIX = "account_recovery_".ToByteArray();
        private static readonly byte[] GUARDIAN_STATS_PREFIX = "guardian_stats_".ToByteArray();
        private static readonly byte[] RECOVERY_STRATEGY_PREFIX = "strategy_".ToByteArray();
        #endregion

        #region Data Structures
        public class Guardian
        {
            public UInt160 Address { get; set; }
            public BigInteger ReputationScore { get; set; }
            public BigInteger SuccessfulRecoveries { get; set; }
            public BigInteger FailedAttempts { get; set; }
            public BigInteger StakedAmount { get; set; }
            public BigInteger LastActivityTime { get; set; }
            public bool IsActive { get; set; }
            public BigInteger TotalEndorsements { get; set; }
        }

        public class RecoveryRequest
        {
            public ByteString RecoveryId { get; set; }
            public UInt160 AccountAddress { get; set; }
            public UInt160 NewOwner { get; set; }
            public UInt160 Initiator { get; set; }
            public string RecoveryStrategy { get; set; }
            public BigInteger RequiredConfirmations { get; set; }
            public BigInteger CurrentConfirmations { get; set; }
            public Map<UInt160, bool> GuardianConfirmations { get; set; }
            public BigInteger InitiatedAt { get; set; }
            public BigInteger ExpiresAt { get; set; }
            public bool IsExecuted { get; set; }
            public bool IsEmergency { get; set; }
            public BigInteger RecoveryFee { get; set; }
        }

        public class TrustRelation
        {
            public UInt160 Truster { get; set; }
            public UInt160 Trustee { get; set; }
            public BigInteger TrustLevel { get; set; } // 0-100
            public BigInteger EstablishedAt { get; set; }
            public BigInteger LastInteraction { get; set; }
        }

        public class RecoveryStrategy
        {
            public string StrategyId { get; set; }
            public string Name { get; set; }
            public BigInteger MinGuardians { get; set; }
            public BigInteger TimeoutPeriod { get; set; }
            public bool RequiresReputation { get; set; }
            public BigInteger MinReputationRequired { get; set; }
            public bool AllowsEmergency { get; set; }
        }

        public class AccountRecoveryConfig
        {
            public UInt160 AccountAddress { get; set; }
            public string PreferredStrategy { get; set; }
            public Map<UInt160, bool> TrustedGuardians { get; set; }
            public BigInteger RecoveryThreshold { get; set; }
            public bool AllowNetworkGuardians { get; set; }
            public BigInteger MinGuardianReputation { get; set; }
        }
        #endregion

        #region Events
        [DisplayName("GuardianEnrolled")]
        public static event Action<UInt160, BigInteger, BigInteger> OnGuardianEnrolled;

        [DisplayName("RecoveryInitiated")]
        public static event Action<ByteString, UInt160, UInt160, string> OnRecoveryInitiated;

        [DisplayName("RecoveryConfirmed")]
        public static event Action<ByteString, UInt160, BigInteger> OnRecoveryConfirmed;

        [DisplayName("RecoveryExecuted")]
        public static event Action<ByteString, UInt160, UInt160> OnRecoveryExecuted;

        [DisplayName("GuardianSlashed")]
        public static event Action<UInt160, BigInteger, string> OnGuardianSlashed;

        [DisplayName("TrustEstablished")]
        public static event Action<UInt160, UInt160, BigInteger> OnTrustEstablished;

        [DisplayName("ReputationUpdated")]
        public static event Action<UInt160, BigInteger, BigInteger> OnReputationUpdated;
        #endregion

        #region Contract Methods
        public static new string GetServiceId() => SERVICE_ID;
        public static new string GetServiceName() => SERVICE_NAME;
        public static new string GetServiceVersion() => SERVICE_VERSION;

        // Guardian enrollment with staking
        public static bool EnrollGuardian(UInt160 guardian, BigInteger stakeAmount)
        {
            if (!Runtime.CheckWitness(guardian)) return false;
            if (stakeAmount < MIN_GUARDIAN_STAKE) return false;

            // Transfer stake from guardian
            if (!TransferToken(GAS.Hash, guardian, Runtime.ExecutingScriptHash, stakeAmount))
                return false;

            var existingGuardian = GetGuardian(guardian);
            if (existingGuardian != null)
            {
                // Update existing guardian
                existingGuardian.StakedAmount += stakeAmount;
                existingGuardian.IsActive = true;
                existingGuardian.LastActivityTime = Runtime.Time;
            }
            else
            {
                // Create new guardian
                existingGuardian = new Guardian
                {
                    Address = guardian,
                    ReputationScore = MIN_REPUTATION_SCORE,
                    SuccessfulRecoveries = 0,
                    FailedAttempts = 0,
                    StakedAmount = stakeAmount,
                    LastActivityTime = Runtime.Time,
                    IsActive = true,
                    TotalEndorsements = 0
                };
            }

            SaveGuardian(existingGuardian);
            OnGuardianEnrolled(guardian, stakeAmount, existingGuardian.ReputationScore);
            return true;
        }

        // Initiate recovery request
        public static ByteString InitiateRecovery(
            UInt160 accountAddress,
            UInt160 newOwner,
            string strategyId,
            bool isEmergency,
            BigInteger recoveryFee)
        {
            if (!Runtime.CheckWitness(Runtime.EntryScriptHash)) return null;

            var strategy = GetRecoveryStrategy(strategyId);
            if (strategy == null) return null;

            if (isEmergency && !strategy.AllowsEmergency) return null;

            var initiator = Runtime.EntryScriptHash;
            var guardian = GetGuardian(initiator);
            
            // Check if initiator is a valid guardian for this recovery
            if (strategy.RequiresReputation && 
                (guardian == null || guardian.ReputationScore < strategy.MinReputationRequired))
                return null;

            var recoveryId = GenerateRecoveryId(accountAddress, newOwner, Runtime.Time);
            var timeout = isEmergency ? EMERGENCY_RECOVERY_TIMEOUT : strategy.TimeoutPeriod;

            var request = new RecoveryRequest
            {
                RecoveryId = recoveryId,
                AccountAddress = accountAddress,
                NewOwner = newOwner,
                Initiator = initiator,
                RecoveryStrategy = strategyId,
                RequiredConfirmations = CalculateRequiredConfirmations(accountAddress, strategy),
                CurrentConfirmations = 1,
                GuardianConfirmations = new Map<UInt160, bool>(),
                InitiatedAt = Runtime.Time,
                ExpiresAt = Runtime.Time + timeout,
                IsExecuted = false,
                IsEmergency = isEmergency,
                RecoveryFee = recoveryFee
            };

            request.GuardianConfirmations[initiator] = true;
            SaveRecoveryRequest(request);

            OnRecoveryInitiated(recoveryId, accountAddress, newOwner, strategyId);
            IncrementRequestCount();
            
            return recoveryId;
        }

        // Confirm recovery request
        public static bool ConfirmRecovery(ByteString recoveryId)
        {
            var confirmer = Runtime.EntryScriptHash;
            if (!Runtime.CheckWitness(confirmer)) return false;

            var request = GetRecoveryRequest(recoveryId);
            if (request == null || request.IsExecuted) return false;
            if (Runtime.Time > request.ExpiresAt) return false;

            // Check if already confirmed
            if (request.GuardianConfirmations.HasKey(confirmer) && 
                request.GuardianConfirmations[confirmer])
                return false;

            var guardian = GetGuardian(confirmer);
            if (guardian == null || !guardian.IsActive) return false;

            // Check reputation requirements
            var strategy = GetRecoveryStrategy(request.RecoveryStrategy);
            if (strategy.RequiresReputation && 
                guardian.ReputationScore < strategy.MinReputationRequired)
                return false;

            // Add confirmation with reputation weighting
            var weight = CalculateConfirmationWeight(guardian, request);
            request.CurrentConfirmations += weight;
            request.GuardianConfirmations[confirmer] = true;

            SaveRecoveryRequest(request);
            OnRecoveryConfirmed(recoveryId, confirmer, request.CurrentConfirmations);

            // Execute if threshold met
            if (request.CurrentConfirmations >= request.RequiredConfirmations)
            {
                return ExecuteRecovery(recoveryId);
            }

            return true;
        }

        // Execute recovery
        private static bool ExecuteRecovery(ByteString recoveryId)
        {
            var request = GetRecoveryRequest(recoveryId);
            if (request == null || request.IsExecuted) return false;

            // Call the abstract account to execute recovery
            var result = Contract.Call(
                request.AccountAddress,
                "executeRecovery",
                CallFlags.All,
                request.NewOwner,
                recoveryId
            );

            if ((bool)result)
            {
                request.IsExecuted = true;
                SaveRecoveryRequest(request);

                // Update guardian stats
                UpdateGuardianStats(request, true);

                // Distribute recovery fees
                DistributeRecoveryFees(request);

                OnRecoveryExecuted(recoveryId, request.AccountAddress, request.NewOwner);
                return true;
            }

            return false;
        }

        // Establish trust relationship
        public static bool EstablishTrust(UInt160 trustee, BigInteger trustLevel)
        {
            var truster = Runtime.EntryScriptHash;
            if (!Runtime.CheckWitness(truster)) return false;
            if (trustLevel < 0 || trustLevel > 100) return false;

            var guardian = GetGuardian(trustee);
            if (guardian == null || !guardian.IsActive) return false;

            var relation = new TrustRelation
            {
                Truster = truster,
                Trustee = trustee,
                TrustLevel = trustLevel,
                EstablishedAt = Runtime.Time,
                LastInteraction = Runtime.Time
            };

            SaveTrustRelation(relation);
            
            // Update endorsement count
            if (trustLevel >= 70) // High trust threshold
            {
                guardian.TotalEndorsements += 1;
                SaveGuardian(guardian);
            }

            OnTrustEstablished(truster, trustee, trustLevel);
            return true;
        }

        // Slash malicious guardian
        public static bool SlashGuardian(UInt160 guardian, string reason)
        {
            // Only contract owner or governance can slash
            if (!IsAuthorized()) return false;

            var guardianData = GetGuardian(guardian);
            if (guardianData == null) return false;

            var slashAmount = guardianData.StakedAmount * SLASH_PERCENTAGE / 100;
            guardianData.StakedAmount -= slashAmount;
            guardianData.ReputationScore = Math.Max(0, guardianData.ReputationScore - 500);
            guardianData.FailedAttempts += 1;

            if (guardianData.StakedAmount < MIN_GUARDIAN_STAKE)
            {
                guardianData.IsActive = false;
            }

            SaveGuardian(guardianData);
            OnGuardianSlashed(guardian, slashAmount, reason);
            
            return true;
        }

        // Update recovery strategy configuration
        public static bool ConfigureAccountRecovery(
            UInt160 accountAddress,
            string preferredStrategy,
            BigInteger recoveryThreshold,
            bool allowNetworkGuardians,
            BigInteger minGuardianReputation)
        {
            if (!Runtime.CheckWitness(accountAddress)) return false;

            var config = new AccountRecoveryConfig
            {
                AccountAddress = accountAddress,
                PreferredStrategy = preferredStrategy,
                RecoveryThreshold = recoveryThreshold,
                AllowNetworkGuardians = allowNetworkGuardians,
                MinGuardianReputation = minGuardianReputation,
                TrustedGuardians = new Map<UInt160, bool>()
            };

            SaveAccountConfig(config);
            return true;
        }

        // Add trusted guardian for an account
        public static bool AddTrustedGuardian(UInt160 accountAddress, UInt160 guardian)
        {
            if (!Runtime.CheckWitness(accountAddress)) return false;

            var config = GetAccountConfig(accountAddress);
            if (config == null) return false;

            config.TrustedGuardians[guardian] = true;
            SaveAccountConfig(config);
            
            return true;
        }
        #endregion

        #region Helper Methods
        private static BigInteger CalculateRequiredConfirmations(
            UInt160 accountAddress, 
            RecoveryStrategy strategy)
        {
            var config = GetAccountConfig(accountAddress);
            if (config != null && config.RecoveryThreshold > 0)
            {
                return config.RecoveryThreshold;
            }
            return strategy.MinGuardians;
        }

        private static BigInteger CalculateConfirmationWeight(
            Guardian guardian, 
            RecoveryRequest request)
        {
            // Base weight is 1
            BigInteger weight = 1;

            // Add reputation bonus (up to 2x for max reputation)
            var reputationBonus = guardian.ReputationScore * 100 / MAX_REPUTATION_SCORE;
            weight = weight * (100 + reputationBonus) / 100;

            // Check trust relationships
            var trustRelation = GetTrustRelation(request.AccountAddress, guardian.Address);
            if (trustRelation != null && trustRelation.TrustLevel >= 50)
            {
                weight = weight * 150 / 100; // 1.5x for trusted guardians
            }

            return weight;
        }

        private static void UpdateGuardianStats(RecoveryRequest request, bool success)
        {
            foreach (var confirmation in request.GuardianConfirmations)
            {
                if (confirmation.Value)
                {
                    var guardian = GetGuardian(confirmation.Key);
                    if (guardian != null)
                    {
                        if (success)
                        {
                            guardian.SuccessfulRecoveries += 1;
                            guardian.ReputationScore = Math.Min(
                                MAX_REPUTATION_SCORE, 
                                guardian.ReputationScore + 50
                            );
                        }
                        else
                        {
                            guardian.FailedAttempts += 1;
                            guardian.ReputationScore = Math.Max(
                                0, 
                                guardian.ReputationScore - 100
                            );
                        }
                        guardian.LastActivityTime = Runtime.Time;
                        SaveGuardian(guardian);
                        
                        OnReputationUpdated(
                            guardian.Address, 
                            guardian.ReputationScore,
                            success ? 1 : -1
                        );
                    }
                }
            }
        }

        private static void DistributeRecoveryFees(RecoveryRequest request)
        {
            if (request.RecoveryFee <= 0) return;

            var participantCount = 0;
            foreach (var confirmation in request.GuardianConfirmations)
            {
                if (confirmation.Value) participantCount++;
            }

            if (participantCount == 0) return;

            var feePerGuardian = request.RecoveryFee / participantCount;
            
            foreach (var confirmation in request.GuardianConfirmations)
            {
                if (confirmation.Value)
                {
                    TransferToken(GAS.Hash, Runtime.ExecutingScriptHash, confirmation.Key, feePerGuardian);
                }
            }
        }

        private static ByteString GenerateRecoveryId(
            UInt160 account, 
            UInt160 newOwner, 
            BigInteger timestamp)
        {
            var data = account.Concat(newOwner).Concat(timestamp.ToByteArray());
            return CryptoLib.Sha256(data);
        }

        private static bool TransferToken(
            UInt160 token, 
            UInt160 from, 
            UInt160 to, 
            BigInteger amount)
        {
            return (bool)Contract.Call(
                token, 
                "transfer", 
                CallFlags.All, 
                from, to, amount, null
            );
        }
        #endregion

        #region Storage Methods
        private static void SaveGuardian(Guardian guardian)
        {
            var key = GUARDIAN_REGISTRY_PREFIX.Concat(guardian.Address);
            Storage.Put(Storage.CurrentContext, key, StdLib.Serialize(guardian));
        }

        private static Guardian GetGuardian(UInt160 address)
        {
            var key = GUARDIAN_REGISTRY_PREFIX.Concat(address);
            var data = Storage.Get(Storage.CurrentContext, key);
            return data != null ? (Guardian)StdLib.Deserialize(data) : null;
        }

        private static void SaveRecoveryRequest(RecoveryRequest request)
        {
            var key = RECOVERY_REQUEST_PREFIX.Concat(request.RecoveryId);
            Storage.Put(Storage.CurrentContext, key, StdLib.Serialize(request));
        }

        private static RecoveryRequest GetRecoveryRequest(ByteString recoveryId)
        {
            var key = RECOVERY_REQUEST_PREFIX.Concat(recoveryId);
            var data = Storage.Get(Storage.CurrentContext, key);
            return data != null ? (RecoveryRequest)StdLib.Deserialize(data) : null;
        }

        private static void SaveTrustRelation(TrustRelation relation)
        {
            var key = TRUST_GRAPH_PREFIX
                .Concat(relation.Truster)
                .Concat(relation.Trustee);
            Storage.Put(Storage.CurrentContext, key, StdLib.Serialize(relation));
        }

        private static TrustRelation GetTrustRelation(UInt160 truster, UInt160 trustee)
        {
            var key = TRUST_GRAPH_PREFIX.Concat(truster).Concat(trustee);
            var data = Storage.Get(Storage.CurrentContext, key);
            return data != null ? (TrustRelation)StdLib.Deserialize(data) : null;
        }

        private static void SaveAccountConfig(AccountRecoveryConfig config)
        {
            var key = ACCOUNT_RECOVERY_PREFIX.Concat(config.AccountAddress);
            Storage.Put(Storage.CurrentContext, key, StdLib.Serialize(config));
        }

        private static AccountRecoveryConfig GetAccountConfig(UInt160 accountAddress)
        {
            var key = ACCOUNT_RECOVERY_PREFIX.Concat(accountAddress);
            var data = Storage.Get(Storage.CurrentContext, key);
            return data != null ? (AccountRecoveryConfig)StdLib.Deserialize(data) : null;
        }

        private static void SaveRecoveryStrategy(RecoveryStrategy strategy)
        {
            var key = RECOVERY_STRATEGY_PREFIX.Concat(strategy.StrategyId.ToByteArray());
            Storage.Put(Storage.CurrentContext, key, StdLib.Serialize(strategy));
        }

        private static RecoveryStrategy GetRecoveryStrategy(string strategyId)
        {
            var key = RECOVERY_STRATEGY_PREFIX.Concat(strategyId.ToByteArray());
            var data = Storage.Get(Storage.CurrentContext, key);
            return data != null ? (RecoveryStrategy)StdLib.Deserialize(data) : null;
        }
        #endregion

        #region Query Methods
        public static object[] GetActiveGuardians()
        {
            var guardians = new List<UInt160>();
            var iterator = Storage.Find(Storage.CurrentContext, GUARDIAN_REGISTRY_PREFIX, FindOptions.KeysOnly);
            
            while (iterator.Next())
            {
                var key = (ByteString)iterator.Value;
                var address = key.Substring(GUARDIAN_REGISTRY_PREFIX.Length);
                var guardian = GetGuardian((UInt160)address);
                
                if (guardian != null && guardian.IsActive)
                {
                    guardians.Add(guardian.Address);
                }
            }
            
            return guardians.ToArray();
        }

        public static BigInteger GetActiveGuardianCount()
        {
            BigInteger count = 0;
            var iterator = Storage.Find(Storage.CurrentContext, GUARDIAN_REGISTRY_PREFIX);
            
            while (iterator.Next())
            {
                var guardian = (Guardian)StdLib.Deserialize(iterator.Value);
                if (guardian.IsActive)
                {
                    count++;
                }
            }
            
            return count;
        }

        public static object[] GetActiveRecoveriesForAccount(UInt160 accountAddress)
        {
            var recoveries = new List<ByteString>();
            var iterator = Storage.Find(Storage.CurrentContext, RECOVERY_REQUEST_PREFIX);
            
            while (iterator.Next())
            {
                var request = (RecoveryRequest)StdLib.Deserialize(iterator.Value);
                if (request.AccountAddress == accountAddress && 
                    !request.IsExecuted && 
                    Runtime.Time <= request.ExpiresAt)
                {
                    recoveries.Add(request.RecoveryId);
                }
            }
            
            return recoveries.ToArray();
        }

        public static object[] GetTrustRelationsForGuardian(UInt160 guardian)
        {
            var relations = new List<object[]>();
            var prefixAsTruster = TRUST_GRAPH_PREFIX.Concat(guardian);
            var iterator = Storage.Find(Storage.CurrentContext, prefixAsTruster);
            
            while (iterator.Next())
            {
                var relation = (TrustRelation)StdLib.Deserialize(iterator.Value);
                relations.Add(new object[] 
                {
                    relation.Truster,
                    relation.Trustee,
                    relation.TrustLevel,
                    relation.EstablishedAt,
                    relation.LastInteraction
                });
            }
            
            return relations.ToArray();
        }

        public static bool CancelRecovery(ByteString recoveryId)
        {
            var request = GetRecoveryRequest(recoveryId);
            if (request == null || request.IsExecuted) return false;
            
            // Only initiator or account owner can cancel
            if (!Runtime.CheckWitness(request.Initiator) && 
                !Runtime.CheckWitness(request.AccountAddress))
                return false;
            
            // Mark as executed to prevent further actions
            request.IsExecuted = true;
            SaveRecoveryRequest(request);
            
            // Refund recovery fee to initiator if any
            if (request.RecoveryFee > 0)
            {
                TransferToken(GAS.Hash, Runtime.ExecutingScriptHash, request.Initiator, request.RecoveryFee);
            }
            
            return true;
        }

        public static bool VerifyAttestation(UInt160 subject, ByteString attestationType, ByteString proof)
        {
            // In production, this would verify against an attestation service
            // For now, check if the attestation exists in storage
            var key = "attestation_".ToByteArray()
                .Concat(subject)
                .Concat(attestationType);
            
            var storedProof = Storage.Get(Storage.CurrentContext, key);
            return storedProof != null && storedProof == proof;
        }
        #endregion

        #region Initialization
        public static void Initialize()
        {
            // Initialize default recovery strategies
            var standardStrategy = new RecoveryStrategy
            {
                StrategyId = "standard",
                Name = "Standard Guardian Recovery",
                MinGuardians = 3,
                TimeoutPeriod = 7 * 24 * 3600, // 7 days
                RequiresReputation = true,
                MinReputationRequired = 100,
                AllowsEmergency = false
            };
            SaveRecoveryStrategy(standardStrategy);
            
            var emergencyStrategy = new RecoveryStrategy
            {
                StrategyId = "emergency",
                Name = "Emergency Recovery",
                MinGuardians = 5,
                TimeoutPeriod = 24 * 3600, // 24 hours
                RequiresReputation = true,
                MinReputationRequired = 500,
                AllowsEmergency = true
            };
            SaveRecoveryStrategy(emergencyStrategy);
            
            var multifactorStrategy = new RecoveryStrategy
            {
                StrategyId = "multifactor",
                Name = "Multi-Factor Recovery",
                MinGuardians = 2,
                TimeoutPeriod = 3 * 24 * 3600, // 3 days
                RequiresReputation = true,
                MinReputationRequired = 200,
                AllowsEmergency = false
            };
            SaveRecoveryStrategy(multifactorStrategy);
            
            // Initialize service
            InitializeService();
        }
        #endregion
    }
}