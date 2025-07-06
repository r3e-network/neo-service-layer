using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace NeoServiceLayer.Services.Abstractions
{
    /// <summary>
    /// Service for managing decentralized social recovery network
    /// </summary>
    public interface ISocialRecoveryService
    {
        /// <summary>
        /// Enrolls a new guardian in the social recovery network
        /// </summary>
        Task<GuardianInfo> EnrollGuardianAsync(string address, BigInteger stakeAmount, string blockchain = "neo-n3");

        /// <summary>
        /// Initiates a recovery request for an account
        /// </summary>
        Task<RecoveryRequest> InitiateRecoveryAsync(
            string accountAddress,
            string newOwner,
            string strategyId,
            bool isEmergency,
            BigInteger recoveryFee,
            List<AuthFactor> authFactors = null,
            string blockchain = "neo-n3");

        /// <summary>
        /// Confirms a recovery request as a guardian
        /// </summary>
        Task<bool> ConfirmRecoveryAsync(string recoveryId, string blockchain = "neo-n3");

        /// <summary>
        /// Establishes trust relationship with another guardian
        /// </summary>
        Task<bool> EstablishTrustAsync(string trustee, int trustLevel, string blockchain = "neo-n3");

        /// <summary>
        /// Gets information about a guardian
        /// </summary>
        Task<GuardianInfo> GetGuardianInfoAsync(string address, string blockchain = "neo-n3");

        /// <summary>
        /// Gets information about a recovery request
        /// </summary>
        Task<RecoveryInfo> GetRecoveryInfoAsync(string recoveryId, string blockchain = "neo-n3");

        /// <summary>
        /// Gets available recovery strategies
        /// </summary>
        Task<List<RecoveryStrategy>> GetAvailableStrategiesAsync(string blockchain = "neo-n3");

        /// <summary>
        /// Gets network statistics
        /// </summary>
        Task<NetworkStats> GetNetworkStatsAsync(string blockchain = "neo-n3");

        /// <summary>
        /// Adds multi-factor authentication to an account
        /// </summary>
        Task<bool> AddAuthFactorAsync(string factorType, string factorHash, string blockchain = "neo-n3");

        /// <summary>
        /// Slashes a malicious guardian
        /// </summary>
        Task<bool> SlashGuardianAsync(string guardian, string reason, string blockchain = "neo-n3");

        /// <summary>
        /// Configures account recovery preferences
        /// </summary>
        Task<bool> ConfigureAccountRecoveryAsync(
            string accountAddress,
            string preferredStrategy,
            BigInteger recoveryThreshold,
            bool allowNetworkGuardians,
            BigInteger minGuardianReputation,
            string blockchain = "neo-n3");

        /// <summary>
        /// Adds a trusted guardian for an account
        /// </summary>
        Task<bool> AddTrustedGuardianAsync(string accountAddress, string guardian, string blockchain = "neo-n3");

        /// <summary>
        /// Gets the trust level between two addresses
        /// </summary>
        Task<int> GetTrustLevelAsync(string truster, string trustee, string blockchain = "neo-n3");

        /// <summary>
        /// Verifies multi-factor authentication
        /// </summary>
        Task<bool> VerifyMultiFactorAuthAsync(
            string accountAddress,
            List<AuthFactor> authFactors,
            string blockchain = "neo-n3");

        /// <summary>
        /// Gets active recovery requests for an account
        /// </summary>
        Task<List<RecoveryRequest>> GetActiveRecoveriesAsync(string accountAddress, string blockchain = "neo-n3");

        /// <summary>
        /// Cancels a recovery request
        /// </summary>
        Task<bool> CancelRecoveryAsync(string recoveryId, string blockchain = "neo-n3");

        /// <summary>
        /// Updates guardian reputation based on performance
        /// </summary>
        Task<bool> UpdateGuardianReputationAsync(string guardian, int change, string blockchain = "neo-n3");

        /// <summary>
        /// Gets trust relationships for a guardian
        /// </summary>
        Task<List<TrustRelation>> GetTrustRelationshipsAsync(string guardian, string blockchain = "neo-n3");
    }

    public class GuardianInfo
    {
        public string Address { get; set; }
        public BigInteger ReputationScore { get; set; }
        public BigInteger SuccessfulRecoveries { get; set; }
        public BigInteger FailedAttempts { get; set; }
        public BigInteger StakedAmount { get; set; }
        public bool IsActive { get; set; }
        public BigInteger TotalEndorsements { get; set; }
        public double TrustScore => CalculateTrustScore();

        private double CalculateTrustScore()
        {
            if (SuccessfulRecoveries + FailedAttempts == 0) return 0;
            var successRate = (double)SuccessfulRecoveries / (double)(SuccessfulRecoveries + FailedAttempts);
            var reputationFactor = (double)ReputationScore / 10000.0; // Max reputation
            var stakeFactor = Math.Min((double)StakedAmount / 1000_00000000, 1.0); // Normalize to 1000 GAS
            
            return (successRate * 0.4 + reputationFactor * 0.4 + stakeFactor * 0.2) * 100;
        }
    }

    public class RecoveryRequest
    {
        public string RecoveryId { get; set; }
        public string AccountAddress { get; set; }
        public string NewOwner { get; set; }
        public string Initiator { get; set; }
        public string StrategyId { get; set; }
        public BigInteger RequiredConfirmations { get; set; }
        public BigInteger CurrentConfirmations { get; set; }
        public DateTime InitiatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsEmergency { get; set; }
        public BigInteger RecoveryFee { get; set; }
        public RecoveryStatus Status { get; set; }
        public List<string> ConfirmedGuardians { get; set; } = new();
    }

    public class RecoveryInfo
    {
        public string RecoveryId { get; set; }
        public string AccountAddress { get; set; }
        public string NewOwner { get; set; }
        public BigInteger CurrentConfirmations { get; set; }
        public BigInteger RequiredConfirmations { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExecuted { get; set; }
        public bool IsEmergency { get; set; }
        public BigInteger RecoveryFee { get; set; }
        public double Progress => RequiredConfirmations > 0 ? 
            (double)CurrentConfirmations / (double)RequiredConfirmations * 100 : 0;
    }

    public class RecoveryStrategy
    {
        public string StrategyId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MinGuardians { get; set; }
        public TimeSpan TimeoutPeriod { get; set; }
        public bool RequiresReputation { get; set; }
        public int MinReputationRequired { get; set; }
        public bool AllowsEmergency { get; set; }
        public bool RequiresAttestation { get; set; }
        public List<string> RequiredAttestations { get; set; } = new();
    }

    public class AuthFactor
    {
        public string FactorType { get; set; }
        public string FactorHash { get; set; }
        public byte[] Proof { get; set; }
        public DateTime AddedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class NetworkStats
    {
        public int TotalGuardians { get; set; }
        public long TotalRecoveries { get; set; }
        public long SuccessfulRecoveries { get; set; }
        public BigInteger TotalStaked { get; set; }
        public double AverageReputationScore { get; set; }
        public double SuccessRate => TotalRecoveries > 0 ? 
            (double)SuccessfulRecoveries / TotalRecoveries * 100 : 0;
    }

    public enum RecoveryStatus
    {
        Pending,
        InProgress,
        Executed,
        Expired,
        Failed
    }

    public class TrustRelation
    {
        public string Truster { get; set; }
        public string Trustee { get; set; }
        public int TrustLevel { get; set; } // 0-100
        public DateTime EstablishedAt { get; set; }
        public DateTime LastInteraction { get; set; }
        public bool IsActive => (DateTime.UtcNow - LastInteraction).TotalDays < 180; // Active if interaction within 6 months
    }
}