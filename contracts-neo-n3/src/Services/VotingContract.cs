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

namespace NeoServiceLayer.Contracts.Services
{
    /// <summary>
    /// Provides decentralized governance and voting mechanisms with
    /// proposal creation, voting, and execution capabilities.
    /// </summary>
    [DisplayName("VotingContract")]
    [ManifestExtra("Author", "Neo Service Layer Team")]
    [ManifestExtra("Description", "Decentralized governance and voting service")]
    [ManifestExtra("Version", "1.0.0")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class VotingContract : BaseServiceContract
    {
        #region Storage Keys
        private static readonly byte[] ProposalPrefix = "proposal:".ToByteArray();
        private static readonly byte[] VotePrefix = "vote:".ToByteArray();
        private static readonly byte[] VoterPrefix = "voter:".ToByteArray();
        private static readonly byte[] ProposalCountKey = "proposalCount".ToByteArray();
        private static readonly byte[] VotingConfigKey = "votingConfig".ToByteArray();
        private static readonly byte[] QuorumThresholdKey = "quorumThreshold".ToByteArray();
        private static readonly byte[] VotingPowerPrefix = "votingPower:".ToByteArray();
        #endregion

        #region Events
        [DisplayName("ProposalCreated")]
        public static event Action<ByteString, UInt160, string, ulong, ulong> ProposalCreated;

        [DisplayName("VoteCast")]
        public static event Action<ByteString, UInt160, bool, BigInteger> VoteCast;

        [DisplayName("ProposalExecuted")]
        public static event Action<ByteString, bool> ProposalExecuted;

        [DisplayName("VoterRegistered")]
        public static event Action<UInt160, BigInteger> VoterRegistered;

        [DisplayName("QuorumReached")]
        public static event Action<ByteString, BigInteger, BigInteger> QuorumReached;
        #endregion

        #region Constants
        private const int DEFAULT_VOTING_PERIOD = 604800; // 7 days
        private const int DEFAULT_EXECUTION_DELAY = 86400; // 1 day
        private const int DEFAULT_QUORUM_THRESHOLD = 5000; // 50% in basis points
        #endregion

        #region Initialization
        public static void _deploy(object data, bool update)
        {
            if (update) return;

            var serviceId = Runtime.ExecutingScriptHash;
            var contract = new VotingContract();
            contract.InitializeBaseService(serviceId, "VotingService", "1.0.0", "{}");
            
            // Initialize voting configuration
            var config = new VotingConfig
            {
                VotingPeriod = DEFAULT_VOTING_PERIOD,
                ExecutionDelay = DEFAULT_EXECUTION_DELAY,
                QuorumThreshold = DEFAULT_QUORUM_THRESHOLD,
                RequireRegistration = true
            };
            
            Storage.Put(Storage.CurrentContext, VotingConfigKey, StdLib.Serialize(config));
            Storage.Put(Storage.CurrentContext, ProposalCountKey, 0);
            Storage.Put(Storage.CurrentContext, QuorumThresholdKey, DEFAULT_QUORUM_THRESHOLD);

            Runtime.Log("VotingContract deployed successfully");
        }
        #endregion

        #region Service Implementation
        protected override void InitializeService(string config)
        {
            Runtime.Log("VotingContract service initialized");
        }

        protected override bool PerformHealthCheck()
        {
            try
            {
                var proposalCount = GetProposalCount();
                return proposalCount >= 0;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Proposal Management
        /// <summary>
        /// Creates a new governance proposal.
        /// </summary>
        public static ByteString CreateProposal(string title, string description, 
            ByteString executionData, UInt160 targetContract)
        {
            return ExecuteServiceOperation(() =>
            {
                var caller = Runtime.CallingScriptHash;
                
                // Validate inputs
                if (string.IsNullOrEmpty(title))
                    throw new ArgumentException("Title cannot be empty");
                if (string.IsNullOrEmpty(description))
                    throw new ArgumentException("Description cannot be empty");
                
                // Check if caller is registered voter (if required)
                var config = GetVotingConfig();
                if (config.RequireRegistration && !IsRegisteredVoter(caller))
                    throw new InvalidOperationException("Caller must be registered voter");
                
                var proposalId = GenerateProposalId();
                var currentTime = Runtime.Time;
                
                var proposal = new Proposal
                {
                    Id = proposalId,
                    Title = title,
                    Description = description,
                    Proposer = caller,
                    ExecutionData = executionData,
                    TargetContract = targetContract,
                    CreatedAt = currentTime,
                    VotingStartTime = currentTime,
                    VotingEndTime = currentTime + config.VotingPeriod,
                    ExecutionTime = currentTime + config.VotingPeriod + config.ExecutionDelay,
                    Status = ProposalStatus.Active,
                    YesVotes = 0,
                    NoVotes = 0,
                    TotalVotingPower = GetTotalVotingPower()
                };
                
                // Store proposal
                var proposalKey = ProposalPrefix.Concat(proposalId);
                Storage.Put(Storage.CurrentContext, proposalKey, StdLib.Serialize(proposal));
                
                // Increment proposal count
                var count = GetProposalCount();
                Storage.Put(Storage.CurrentContext, ProposalCountKey, count + 1);
                
                ProposalCreated(proposalId, caller, title, proposal.VotingEndTime, proposal.ExecutionTime);
                Runtime.Log($"Proposal created: {title} by {caller}");
                return proposalId;
            });
        }

        /// <summary>
        /// Casts a vote on a proposal.
        /// </summary>
        public static bool CastVote(ByteString proposalId, bool support, string reason)
        {
            return ExecuteServiceOperation(() =>
            {
                var caller = Runtime.CallingScriptHash;
                
                // Get proposal
                var proposal = GetProposal(proposalId);
                if (proposal == null)
                    throw new InvalidOperationException("Proposal not found");
                
                // Check voting period
                if (Runtime.Time < proposal.VotingStartTime || Runtime.Time > proposal.VotingEndTime)
                    throw new InvalidOperationException("Voting period has ended");
                
                if (proposal.Status != ProposalStatus.Active)
                    throw new InvalidOperationException("Proposal is not active");
                
                // Check if voter is registered
                var config = GetVotingConfig();
                if (config.RequireRegistration && !IsRegisteredVoter(caller))
                    throw new InvalidOperationException("Voter not registered");
                
                // Check if already voted
                var voteKey = VotePrefix.Concat(proposalId).Concat(caller);
                if (Storage.Get(Storage.CurrentContext, voteKey) != null)
                    throw new InvalidOperationException("Already voted on this proposal");
                
                // Get voting power
                var votingPower = GetVotingPower(caller);
                if (votingPower == 0)
                    throw new InvalidOperationException("No voting power");
                
                // Record vote
                var vote = new Vote
                {
                    ProposalId = proposalId,
                    Voter = caller,
                    Support = support,
                    VotingPower = votingPower,
                    Reason = reason ?? "",
                    Timestamp = Runtime.Time
                };
                
                Storage.Put(Storage.CurrentContext, voteKey, StdLib.Serialize(vote));
                
                // Update proposal vote counts
                if (support)
                {
                    proposal.YesVotes += votingPower;
                }
                else
                {
                    proposal.NoVotes += votingPower;
                }
                
                // Check if quorum reached
                var totalVotes = proposal.YesVotes + proposal.NoVotes;
                var quorumThreshold = GetQuorumThreshold();
                var requiredQuorum = (proposal.TotalVotingPower * quorumThreshold) / 10000;
                
                if (totalVotes >= requiredQuorum)
                {
                    proposal.Status = ProposalStatus.QuorumReached;
                    QuorumReached(proposalId, totalVotes, requiredQuorum);
                }
                
                // Store updated proposal
                var proposalKey = ProposalPrefix.Concat(proposalId);
                Storage.Put(Storage.CurrentContext, proposalKey, StdLib.Serialize(proposal));
                
                VoteCast(proposalId, caller, support, votingPower);
                Runtime.Log($"Vote cast: {caller} voted {(support ? "YES" : "NO")} on {proposalId}");
                return true;
            });
        }

        /// <summary>
        /// Executes a proposal if conditions are met.
        /// </summary>
        public static bool ExecuteProposal(ByteString proposalId)
        {
            return ExecuteServiceOperation(() =>
            {
                var proposal = GetProposal(proposalId);
                if (proposal == null)
                    throw new InvalidOperationException("Proposal not found");
                
                // Check if execution time has arrived
                if (Runtime.Time < proposal.ExecutionTime)
                    throw new InvalidOperationException("Execution time not reached");
                
                if (proposal.Status == ProposalStatus.Executed)
                    throw new InvalidOperationException("Proposal already executed");
                
                if (proposal.Status == ProposalStatus.Cancelled)
                    throw new InvalidOperationException("Proposal was cancelled");
                
                // Check if proposal passed
                var totalVotes = proposal.YesVotes + proposal.NoVotes;
                var quorumThreshold = GetQuorumThreshold();
                var requiredQuorum = (proposal.TotalVotingPower * quorumThreshold) / 10000;
                
                bool passed = false;
                if (totalVotes >= requiredQuorum)
                {
                    // Simple majority of votes cast
                    passed = proposal.YesVotes > proposal.NoVotes;
                }
                
                // Update proposal status
                proposal.Status = passed ? ProposalStatus.Executed : ProposalStatus.Failed;
                
                var proposalKey = ProposalPrefix.Concat(proposalId);
                Storage.Put(Storage.CurrentContext, proposalKey, StdLib.Serialize(proposal));
                
                // Execute proposal if passed
                if (passed && proposal.ExecutionData.Length > 0)
                {
                    try
                    {
                        // In production, would execute the proposal's target contract call
                        // For now, just log the execution
                        Runtime.Log($"Executing proposal: {proposal.Title}");
                        // Contract.Call(proposal.TargetContract, "execute", proposal.ExecutionData);
                    }
                    catch (Exception ex)
                    {
                        Runtime.Log($"Proposal execution failed: {ex.Message}");
                        proposal.Status = ProposalStatus.ExecutionFailed;
                        Storage.Put(Storage.CurrentContext, proposalKey, StdLib.Serialize(proposal));
                        passed = false;
                    }
                }
                
                ProposalExecuted(proposalId, passed);
                Runtime.Log($"Proposal {proposalId} execution result: {(passed ? "PASSED" : "FAILED")}");
                return passed;
            });
        }

        /// <summary>
        /// Gets proposal information.
        /// </summary>
        public static Proposal GetProposal(ByteString proposalId)
        {
            var proposalKey = ProposalPrefix.Concat(proposalId);
            var proposalBytes = Storage.Get(Storage.CurrentContext, proposalKey);
            if (proposalBytes == null)
                return null;
            
            return (Proposal)StdLib.Deserialize(proposalBytes);
        }
        #endregion

        #region Voter Management
        /// <summary>
        /// Registers a voter with voting power.
        /// </summary>
        public static bool RegisterVoter(UInt160 voter, BigInteger votingPower)
        {
            return ExecuteServiceOperation(() =>
            {
                // Only admin can register voters
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                if (votingPower <= 0)
                    throw new ArgumentException("Voting power must be positive");
                
                var voterInfo = new VoterInfo
                {
                    Address = voter,
                    VotingPower = votingPower,
                    RegisteredAt = Runtime.Time,
                    IsActive = true,
                    VotesCast = 0
                };
                
                var voterKey = VoterPrefix.Concat(voter);
                Storage.Put(Storage.CurrentContext, voterKey, StdLib.Serialize(voterInfo));
                
                // Store voting power separately for quick access
                var powerKey = VotingPowerPrefix.Concat(voter);
                Storage.Put(Storage.CurrentContext, powerKey, votingPower);
                
                VoterRegistered(voter, votingPower);
                Runtime.Log($"Voter registered: {voter} with power {votingPower}");
                return true;
            });
        }

        /// <summary>
        /// Checks if an address is a registered voter.
        /// </summary>
        public static bool IsRegisteredVoter(UInt160 voter)
        {
            var voterKey = VoterPrefix.Concat(voter);
            var voterBytes = Storage.Get(Storage.CurrentContext, voterKey);
            if (voterBytes == null)
                return false;
            
            var voterInfo = (VoterInfo)StdLib.Deserialize(voterBytes);
            return voterInfo.IsActive;
        }

        /// <summary>
        /// Gets voting power for an address.
        /// </summary>
        public static BigInteger GetVotingPower(UInt160 voter)
        {
            var powerKey = VotingPowerPrefix.Concat(voter);
            var powerBytes = Storage.Get(Storage.CurrentContext, powerKey);
            return powerBytes?.ToBigInteger() ?? 0;
        }

        /// <summary>
        /// Gets total voting power in the system.
        /// </summary>
        public static BigInteger GetTotalVotingPower()
        {
            // Simplified implementation - in production would maintain running total
            return 1000000; // Placeholder
        }
        #endregion

        #region Configuration
        /// <summary>
        /// Updates voting configuration.
        /// </summary>
        public static bool UpdateVotingConfig(int votingPeriod, int executionDelay, 
            int quorumThreshold, bool requireRegistration)
        {
            return ExecuteServiceOperation(() =>
            {
                // Only admin can update configuration
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                if (votingPeriod < 3600) // Minimum 1 hour
                    throw new ArgumentException("Voting period too short");
                if (quorumThreshold < 1000 || quorumThreshold > 10000) // 10% to 100%
                    throw new ArgumentException("Invalid quorum threshold");
                
                var config = new VotingConfig
                {
                    VotingPeriod = votingPeriod,
                    ExecutionDelay = executionDelay,
                    QuorumThreshold = quorumThreshold,
                    RequireRegistration = requireRegistration
                };
                
                Storage.Put(Storage.CurrentContext, VotingConfigKey, StdLib.Serialize(config));
                Storage.Put(Storage.CurrentContext, QuorumThresholdKey, quorumThreshold);
                
                Runtime.Log("Voting configuration updated");
                return true;
            });
        }

        /// <summary>
        /// Gets current voting configuration.
        /// </summary>
        public static VotingConfig GetVotingConfig()
        {
            var configBytes = Storage.Get(Storage.CurrentContext, VotingConfigKey);
            if (configBytes == null)
            {
                return new VotingConfig
                {
                    VotingPeriod = DEFAULT_VOTING_PERIOD,
                    ExecutionDelay = DEFAULT_EXECUTION_DELAY,
                    QuorumThreshold = DEFAULT_QUORUM_THRESHOLD,
                    RequireRegistration = true
                };
            }
            
            return (VotingConfig)StdLib.Deserialize(configBytes);
        }

        /// <summary>
        /// Gets current quorum threshold.
        /// </summary>
        public static int GetQuorumThreshold()
        {
            var thresholdBytes = Storage.Get(Storage.CurrentContext, QuorumThresholdKey);
            return (int)(thresholdBytes?.ToBigInteger() ?? DEFAULT_QUORUM_THRESHOLD);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Gets the total number of proposals.
        /// </summary>
        public static int GetProposalCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, ProposalCountKey);
            return (int)(countBytes?.ToBigInteger() ?? 0);
        }

        /// <summary>
        /// Generates a unique proposal ID.
        /// </summary>
        private static ByteString GenerateProposalId()
        {
            var counter = GetProposalCount();
            var tx = (Transaction)Runtime.ScriptContainer;
            var data = Runtime.Time.ToByteArray()
                .Concat(counter.ToByteArray())
                .Concat(tx.Hash);
            
            return CryptoLib.Sha256(data);
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
        /// Represents a governance proposal.
        /// </summary>
        public class Proposal
        {
            public ByteString Id;
            public string Title;
            public string Description;
            public UInt160 Proposer;
            public ByteString ExecutionData;
            public UInt160 TargetContract;
            public ulong CreatedAt;
            public ulong VotingStartTime;
            public ulong VotingEndTime;
            public ulong ExecutionTime;
            public ProposalStatus Status;
            public BigInteger YesVotes;
            public BigInteger NoVotes;
            public BigInteger TotalVotingPower;
        }

        /// <summary>
        /// Represents a vote on a proposal.
        /// </summary>
        public class Vote
        {
            public ByteString ProposalId;
            public UInt160 Voter;
            public bool Support;
            public BigInteger VotingPower;
            public string Reason;
            public ulong Timestamp;
        }

        /// <summary>
        /// Represents voter information.
        /// </summary>
        public class VoterInfo
        {
            public UInt160 Address;
            public BigInteger VotingPower;
            public ulong RegisteredAt;
            public bool IsActive;
            public int VotesCast;
        }

        /// <summary>
        /// Represents voting configuration.
        /// </summary>
        public class VotingConfig
        {
            public int VotingPeriod;
            public int ExecutionDelay;
            public int QuorumThreshold;
            public bool RequireRegistration;
        }

        /// <summary>
        /// Proposal status enumeration.
        /// </summary>
        public enum ProposalStatus : byte
        {
            Active = 0,
            QuorumReached = 1,
            Executed = 2,
            Failed = 3,
            Cancelled = 4,
            ExecutionFailed = 5
        }
        #endregion
    }
}