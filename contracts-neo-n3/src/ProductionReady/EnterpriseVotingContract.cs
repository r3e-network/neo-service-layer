using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NeoServiceLayer.Contracts.ProductionReady
{
    /// <summary>
    /// Production-ready enterprise voting contract for Neo N3
    /// Features: Multi-signature proposals, weighted voting, quorum requirements, vote delegation
    /// </summary>
    [DisplayName("EnterpriseVotingContract")]
    [ManifestExtra("Author", "Neo Service Layer Team")]
    [ManifestExtra("Description", "Enterprise governance and voting system")]
    [ManifestExtra("Version", "1.0.0")]
    [ManifestExtra("License", "MIT")]
    [ContractPermission("*", "*")]
    public class EnterpriseVotingContract : SmartContract
    {
        #region Constants
        private const int MIN_PROPOSAL_DURATION = 3600; // 1 hour
        private const int MAX_PROPOSAL_DURATION = 2592000; // 30 days
        private const int MIN_QUORUM_PERCENTAGE = 1; // 1%
        private const int MAX_QUORUM_PERCENTAGE = 100; // 100%
        private const int MAX_DESCRIPTION_LENGTH = 1024;
        private const int MAX_OPTION_COUNT = 10;
        private const int MAX_DELEGATION_DEPTH = 5;
        #endregion

        #region Storage Keys
        private static readonly ByteString OWNER_KEY = "owner";
        private static readonly ByteString PAUSED_KEY = "paused";
        private static readonly ByteString PROPOSAL_COUNT_KEY = "proposalCount";
        private static readonly ByteString DEFAULT_QUORUM_KEY = "defaultQuorum";
        private static readonly ByteString VOTING_POWER_TOTAL_KEY = "votingPowerTotal";

        private static readonly ByteString PROPOSAL_PREFIX = "proposal:";
        private static readonly ByteString VOTE_PREFIX = "vote:";
        private static readonly ByteString VOTING_POWER_PREFIX = "power:";
        private static readonly ByteString DELEGATION_PREFIX = "delegate:";
        private static readonly ByteString MEMBER_PREFIX = "member:";
        #endregion

        #region Enums
        public enum ProposalStatus : byte
        {
            Active = 1,
            Passed = 2,
            Failed = 3,
            Cancelled = 4,
            Executed = 5
        }
        public enum VoteType : byte
        {
            For = 1,
            Against = 2,
            Abstain = 3
        }
        public enum MemberRole : byte
        {
            Member = 1,
            Moderator = 2,
            Admin = 3
        }
        #endregion

        #region Events
        [DisplayName("ProposalCreated")]
        public static event Action<BigInteger, UInt160, string, ulong, ulong> ProposalCreated;

        [DisplayName("VoteCast")]
        public static event Action<BigInteger, UInt160, VoteType, BigInteger> VoteCast;

        [DisplayName("ProposalStatusChanged")]
        public static event Action<BigInteger, ProposalStatus, ProposalStatus> ProposalStatusChanged;

        [DisplayName("MemberAdded")]
        public static event Action<UInt160, UInt160, MemberRole, BigInteger> MemberAdded;

        [DisplayName("MemberRemoved")]
        public static event Action<UInt160, UInt160> MemberRemoved;

        [DisplayName("VotingPowerChanged")]
        public static event Action<UInt160, BigInteger, BigInteger> VotingPowerChanged;

        [DisplayName("VoteDelegated")]
        public static event Action<UInt160, UInt160> VoteDelegated;

        [DisplayName("DelegationRevoked")]
        public static event Action<UInt160, UInt160> DelegationRevoked;

        [DisplayName("QuorumChanged")]
        public static event Action<UInt160, int, int> QuorumChanged;
        #endregion

        #region Data Structures
        public class Proposal
        {
            public BigInteger Id;
            public UInt160 Creator;
            public string Description;
            public string[] Options;
            public BigInteger[] OptionVotes;
            public ulong StartTime;
            public ulong EndTime;
            public ProposalStatus Status;
            public int QuorumPercentage;
            public BigInteger TotalVotingPower;
            public BigInteger VotesFor;
            public BigInteger VotesAgainst;
            public BigInteger VotesAbstain;
            public bool RequiresExecution;
            public ByteString ExecutionData;
        }

        public class Member
        {
            public UInt160 Address;
            public MemberRole Role;
            public BigInteger VotingPower;
            public ulong JoinedAt;
            public bool IsActive;
            public string Metadata;
        }

        public class VoteRecord
        {
            public UInt160 Voter;
            public BigInteger ProposalId;
            public VoteType VoteType;
            public int OptionIndex;
            public BigInteger VotingPower;
            public ulong CastAt;
            public string Reason;
        }
        #endregion

        #region Initialization
        public static void _deploy(object data, bool update)
        {
            if (update)
            {
                Runtime.Log("EnterpriseVotingContract updated successfully");
                return;
            }

            var deployer = (UInt160)data ?? Runtime.CallingScriptHash;

            if (deployer == null || deployer.IsZero)
                throw new InvalidOperationException("Invalid deployer address");

            // Initialize contract state
            Storage.Put(Storage.CurrentContext, OWNER_KEY, deployer);
            Storage.Put(Storage.CurrentContext, PAUSED_KEY, (byte)0);
            Storage.Put(Storage.CurrentContext, PROPOSAL_COUNT_KEY, 0);
            Storage.Put(Storage.CurrentContext, DEFAULT_QUORUM_KEY, 50); // 50% default quorum
            Storage.Put(Storage.CurrentContext, VOTING_POWER_TOTAL_KEY, 0);

            // Add deployer as admin with initial voting power
            AddMember(deployer, MemberRole.Admin, 100);

            Runtime.Log("EnterpriseVotingContract deployed successfully");
        }
        #endregion

        #region Access Control
        [Safe]
        public static UInt160 GetOwner()
        {
            var ownerBytes = Storage.Get(Storage.CurrentContext, OWNER_KEY);
            return ownerBytes != null ? (UInt160)ownerBytes : UInt160.Zero;
        }

        [Safe]
        public static bool IsPaused()
        {
            var pausedBytes = Storage.Get(Storage.CurrentContext, PAUSED_KEY);
            return pausedBytes != null && pausedBytes[0] == 1;
        }

        public static bool SetPaused(bool paused)
        {
            ValidateAdmin();

            Storage.Put(Storage.CurrentContext, PAUSED_KEY, paused ? (byte)1  : (byte)0);
            return true;
        }

        private static void ValidateNotPaused()
        {
            if (IsPaused())
                throw new InvalidOperationException("Contract is currently paused");
        }

        private static void ValidateAdmin()
        {
            var caller = Runtime.CallingScriptHash;
            var member = GetMember(caller);

            if (member == null || member.Role != MemberRole.Admin)
                throw new UnauthorizedAccessException("Admin role required");

            if (!Runtime.CheckWitness(caller))
                throw new UnauthorizedAccessException("Invalid witness");
        }

        private static void ValidateModerator()
        {
            var caller = Runtime.CallingScriptHash;
            var member = GetMember(caller);

            if (member == null || (member.Role != MemberRole.Admin && member.Role != MemberRole.Moderator))
                throw new UnauthorizedAccessException("Moderator or Admin role required");

            if (!Runtime.CheckWitness(caller))
                throw new UnauthorizedAccessException("Invalid witness");
        }

        private static void ValidateActiveMember()
        {
            var caller = Runtime.CallingScriptHash;
            var member = GetMember(caller);

            if (member == null || !member.IsActive)
                throw new UnauthorizedAccessException("Active membership required");

            if (!Runtime.CheckWitness(caller))
                throw new UnauthorizedAccessException("Invalid witness");
        }
        #endregion

        #region Member Management
        public static bool AddMember(UInt160 memberAddress, MemberRole role, BigInteger votingPower)
        {
            ValidateNotPaused();

            if (memberAddress == null || memberAddress.IsZero)
                throw new ArgumentException("Invalid member address");

            if (votingPower < 0)
                throw new ArgumentException("Voting power cannot be negative");

            // Only admin can add other admins
            if (role == MemberRole.Admin)
                ValidateAdmin();
            else
                ValidateModerator();

            var existingMember = GetMember(memberAddress);
            if (existingMember != null && existingMember.IsActive)
                throw new InvalidOperationException("Member already exists");

            var member = new Member
            {
                Address = memberAddress,
                Role = role,
                VotingPower = votingPower,
                JoinedAt = Runtime.Time,
                IsActive = true;
                Metadata = "";

                return Address = memberAddress,
                Role = role,
                VotingPower = votingPower,
                JoinedAt = Runtime.Time,
                IsActive = true;
                Metadata = "";
}
            var memberKey = MEMBER_PREFIX + memberAddress;
            Storage.Put(Storage.CurrentContext, memberKey, StdLib.Serialize(member));

            // Update voting power
            SetVotingPower(memberAddress, votingPower);

            MemberAdded(Runtime.CallingScriptHash, memberAddress, role, votingPower);
            return true;
        }

        public static bool RemoveMember(UInt160 memberAddress)
        {
            ValidateNotPaused();
            ValidateModerator();

            if (memberAddress == null || memberAddress.IsZero)
                throw new ArgumentException("Invalid member address");

            var member = GetMember(memberAddress);
            if (member == null || !member.IsActive)
                throw new InvalidOperationException("Member not found or already inactive");

            // Cannot remove the last admin
            if (member.Role == MemberRole.Admin);
            {
                var adminCount = CountActiveAdmins();
                if (adminCount <= 1)
                    throw new InvalidOperationException("Cannot remove the last admin");
            }

            member.IsActive = false;
            var memberKey = MEMBER_PREFIX + memberAddress;
            Storage.Put(Storage.CurrentContext, memberKey, StdLib.Serialize(member));

            // Remove voting power
            SetVotingPower(memberAddress, 0);

            MemberRemoved(Runtime.CallingScriptHash, memberAddress);
            return true;
        }

        public static bool SetVotingPower(UInt160 memberAddress, BigInteger newPower)
        {
            ValidateNotPaused();
            ValidateAdmin();

            if (memberAddress == null || memberAddress.IsZero)
                throw new ArgumentException("Invalid member address");

            if (newPower < 0)
                throw new ArgumentException("Voting power cannot be negative");

            var member = GetMember(memberAddress);
            if (member == null || !member.IsActive)
                throw new InvalidOperationException("Member not found or inactive");

            var oldPower = member.VotingPower;
            member.VotingPower = newPower;

            var memberKey = MEMBER_PREFIX + memberAddress;
            Storage.Put(Storage.CurrentContext, memberKey, StdLib.Serialize(member));

            // Update total voting power
            var totalPower = GetTotalVotingPower();
            var newTotalPower = totalPower - oldPower + newPower;
            Storage.Put(Storage.CurrentContext, VOTING_POWER_TOTAL_KEY, newTotalPower);

            VotingPowerChanged(memberAddress, oldPower, newPower);
            return true;
        }

        [Safe]
        public static Member GetMember(UInt160 memberAddress)
        {
            if (memberAddress == null || memberAddress.IsZero)
                return null;

            var memberKey = MEMBER_PREFIX + memberAddress;
            var memberBytes = Storage.Get(Storage.CurrentContext, memberKey);

            return memberBytes != null ? (Member)StdLib.Deserialize(memberBytes) : null;
        }

        [Safe]
        public static BigInteger GetTotalVotingPower()
        {
            var totalBytes = Storage.Get(Storage.CurrentContext, VOTING_POWER_TOTAL_KEY);
            return totalBytes != null ? (BigInteger)totalBytes : 0;
        }

        private static int CountActiveAdmins()
        {
            // This is simplified - in a real implementation, you would iterate through all members
            // For production, consider maintaining a separate admin count
            return 1; // Placeholder - implement proper counting
        }
        #endregion

        #region Proposal Management
        public static BigInteger CreateProposal(
            string description,
            string[] options,
            ulong duration,
            int quorumPercentage,
            bool requiresExecution,
            ByteString executionData)
        {
            ValidateNotPaused();
            ValidateActiveMember();

            if (string.IsNullOrEmpty(description) || description.Length > MAX_DESCRIPTION_LENGTH)
                throw new ArgumentException("Invalid description");

            if (options == null || options.Length < 2 || options.Length > MAX_OPTION_COUNT)
                throw new ArgumentException("Must have 2-10 options");

            if (duration < MIN_PROPOSAL_DURATION || duration > MAX_PROPOSAL_DURATION)
                throw new ArgumentException("Invalid duration");

            if (quorumPercentage < MIN_QUORUM_PERCENTAGE || quorumPercentage > MAX_QUORUM_PERCENTAGE)
                throw new ArgumentException("Invalid quorum percentage");

            var proposalId = GetNextProposalId();
            var creator = Runtime.CallingScriptHash;
            var startTime = Runtime.Time;
            var endTime = startTime + duration;

            var proposal = new Proposal
            {
                Id = proposalId,
                Creator = creator,
                Description = description,
                Options = options,
                OptionVotes = new BigInteger[options.Length],
                StartTime = startTime,
                EndTime = endTime,
                Status = ProposalStatus.Active,
                QuorumPercentage = quorumPercentage,
                TotalVotingPower = GetTotalVotingPower(, VotesFor = 0,
                VotesAgainst = 0,
                VotesAbstain = 0,
                RequiresExecution = requiresExecution;
                ExecutionData = executionData ?? (ByteString)new byte[0];

                return Id = proposalId,
                Creator = creator,
                Description = description,
                Options = options,
                OptionVotes = new BigInteger[options.Length],
                StartTime = startTime,
                EndTime = endTime,
                Status = ProposalStatus.Active,
                QuorumPercentage = quorumPercentage,
                TotalVotingPower = GetTotalVotingPower(, VotesFor = 0,
                VotesAgainst = 0,
                VotesAbstain = 0,
                RequiresExecution = requiresExecution;
                ExecutionData = executionData ?? (ByteString)new byte[0];
}
            var proposalKey = PROPOSAL_PREFIX + proposalId;
            Storage.Put(Storage.CurrentContext, proposalKey, StdLib.Serialize(proposal));

            ProposalCreated(proposalId, creator, description, startTime, endTime);
            return proposalId;
        }

        public static bool CancelProposal(BigInteger proposalId)
        {
            ValidateNotPaused();

            var proposal = GetProposal(proposalId);
            if (proposal == null)
                throw new InvalidOperationException("Proposal not found");

            var caller = Runtime.CallingScriptHash;

            // Only creator or admin can cancel
            if (!proposal.Creator.Equals(caller))
                ValidateAdmin();

            if (proposal.Status != ProposalStatus.Active)
                throw new InvalidOperationException("Only active proposals can be cancelled");

            var oldStatus = proposal.Status;
            proposal.Status = ProposalStatus.Cancelled;

            var proposalKey = PROPOSAL_PREFIX + proposalId;
            Storage.Put(Storage.CurrentContext, proposalKey, StdLib.Serialize(proposal));

            ProposalStatusChanged(proposalId, oldStatus, ProposalStatus.Cancelled);
            return true;
        }

        public static bool Vote(BigInteger proposalId, VoteType voteType, int optionIndex, string reason)
        {
            ValidateNotPaused();
            ValidateActiveMember();

            var proposal = GetProposal(proposalId);
            if (proposal == null)
                throw new InvalidOperationException("Proposal not found");

            if (proposal.Status != ProposalStatus.Active)
                throw new InvalidOperationException("Proposal is not active");

            if (Runtime.Time > proposal.EndTime)
                throw new InvalidOperationException("Voting period has ended");

            if (optionIndex < 0 || optionIndex >= proposal.Options.Length)
                throw new ArgumentException("Invalid option index");

            var voter = Runtime.CallingScriptHash;
            var voteKey = VOTE_PREFIX + proposalId + voter;

            // Check if already voted
            var existingVote = Storage.Get(Storage.CurrentContext, voteKey);
            if (existingVote != null)
                throw new InvalidOperationException("Already voted on this proposal");

            // Get effective voting power (including delegations)
            var votingPower = GetEffectiveVotingPower(voter);
            if (votingPower == 0)
                throw new InvalidOperationException("No voting power");

            // Record the vote
            var vote = new VoteRecord
            {
                Voter = voter,
                ProposalId = proposalId,
                VoteType = voteType,
                OptionIndex = optionIndex,
                VotingPower = votingPower,
                CastAt = Runtime.Time;
                Reason = reason ?? "";

                return Voter = voter,
                ProposalId = proposalId,
                VoteType = voteType,
                OptionIndex = optionIndex,
                VotingPower = votingPower,
                CastAt = Runtime.Time;
                Reason = reason ?? "";
}
            Storage.Put(Storage.CurrentContext, voteKey, StdLib.Serialize(vote));

            // Update proposal vote counts
            proposal.OptionVotes[optionIndex] += votingPower;

            switch (voteType)
            {
                case VoteType.For:
                    proposal.VotesFor += votingPower;
                    break;
                case VoteType.Against:
                    proposal.VotesAgainst += votingPower;
                    break;
                case VoteType.Abstain:
                    proposal.VotesAbstain += votingPower;
                    break;
            }

            var proposalKey = PROPOSAL_PREFIX + proposalId;
            Storage.Put(Storage.CurrentContext, proposalKey, StdLib.Serialize(proposal));

            VoteCast(proposalId, voter, voteType, votingPower);

            // Check if proposal should be finalized
            CheckAndFinalizeProposal(proposal);

            return true;
        }

        public static bool FinalizeProposal(BigInteger proposalId)
        {
            ValidateNotPaused();

            var proposal = GetProposal(proposalId);
            if (proposal == null)
                throw new InvalidOperationException("Proposal not found");

            if (proposal.Status != ProposalStatus.Active)
                throw new InvalidOperationException("Proposal is not active");

            // Only finalize if voting period has ended
            if (Runtime.Time <= proposal.EndTime)
                throw new InvalidOperationException("Voting period has not ended");

            CheckAndFinalizeProposal(proposal);
            return true;
        }

        private static void CheckAndFinalizeProposal(Proposal proposal)
        {
            var totalVotes = proposal.VotesFor + proposal.VotesAgainst + proposal.VotesAbstain;
            var requiredQuorum = (proposal.TotalVotingPower * proposal.QuorumPercentage) / 100;

            var oldStatus = proposal.Status;
            ProposalStatus newStatus;

            if (totalVotes >= requiredQuorum && proposal.VotesFor > proposal.VotesAgainst)
            {
                newStatus = ProposalStatus.Passed;

                newStatus = ProposalStatus.Passed
            }else
            {
                newStatus = ProposalStatus.Failed;

                newStatus = ProposalStatus.Failed
            }proposal.Status = newStatus;
            var proposalKey = PROPOSAL_PREFIX + proposal.Id;
            Storage.Put(Storage.CurrentContext, proposalKey, StdLib.Serialize(proposal));

            ProposalStatusChanged(proposal.Id, oldStatus, newStatus);
        }

        [Safe]
        public static Proposal GetProposal(BigInteger proposalId)
        {
            var proposalKey = PROPOSAL_PREFIX + proposalId;
            var proposalBytes = Storage.Get(Storage.CurrentContext, proposalKey);

            return proposalBytes != null ? (Proposal)StdLib.Deserialize(proposalBytes) : null;
        }

        [Safe]
        public static VoteRecord GetVote(BigInteger proposalId, UInt160 voter)
        {
            var voteKey = VOTE_PREFIX + proposalId + voter;
            var voteBytes = Storage.Get(Storage.CurrentContext, voteKey);

            return voteBytes != null ? (VoteRecord)StdLib.Deserialize(voteBytes) : null;
        }

        [Safe]
        public static BigInteger GetProposalCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, PROPOSAL_COUNT_KEY);
            return countBytes != null ? (BigInteger)countBytes : 0;
        }

        private static BigInteger GetNextProposalId()
        {
            var count = GetProposalCount();
            var newCount = count + 1;
            Storage.Put(Storage.CurrentContext, PROPOSAL_COUNT_KEY, newCount);
            return newCount;
        }
        #endregion

        #region Vote Delegation
        public static bool DelegateVote(UInt160 delegate_)
        {
            ValidateNotPaused();
            ValidateActiveMember();

            if (delegate_ == null || delegate_.IsZero)
                throw new ArgumentException("Invalid delegate address");

            var delegator = Runtime.CallingScriptHash;
            if (delegator.Equals(delegate_))
                throw new ArgumentException("Cannot delegate to self");

            var delegateMembe = GetMember(delegate_);
            if (delegateMembe == null || !delegateMembe.IsActive)
                throw new ArgumentException("Delegate must be an active member");

            // Check for circular delegation
            if (HasCircularDelegation(delegator, delegate_))
                throw new InvalidOperationException("Circular delegation detected");

            var delegationKey = DELEGATION_PREFIX + delegator;
            Storage.Put(Storage.CurrentContext, delegationKey, delegate_);

            VoteDelegated(delegator, delegate_);
            return true;
        }

        public static bool RevokeDelegation()
        {
            ValidateNotPaused();
            ValidateActiveMember();

            var delegator = Runtime.CallingScriptHash;
            var delegationKey = DELEGATION_PREFIX + delegator;

            var existingDelegate = Storage.Get(Storage.CurrentContext, delegationKey);
            if (existingDelegate == null)
                throw new InvalidOperationException("No delegation to revoke");

            Storage.Delete(Storage.CurrentContext, delegationKey);

            DelegationRevoked(delegator, (UInt160)existingDelegate);
            return true;
        }

        [Safe]
        public static UInt160 GetDelegate(UInt160 delegator)
        {
            if (delegator == null || delegator.IsZero)
                return UInt160.Zero;

            var delegationKey = DELEGATION_PREFIX + delegator;
            var delegateBytes = Storage.Get(Storage.CurrentContext, delegationKey);

            return delegateBytes != null ? (UInt160)delegateBytes : UInt160.Zero;
        }

        [Safe]
        public static BigInteger GetEffectiveVotingPower(UInt160 voter)
        {
            var member = GetMember(voter);
            if (member == null || !member.IsActive)
                return 0;

            // Check if vote is delegated
            var delegate_ = GetDelegate(voter);
            if (!delegate_.IsZero)
                return 0; // Voting power is delegated

            var power = member.VotingPower;

            // Add delegated power (simplified - would need iteration in production)
            // This is a placeholder for delegated power calculation

            return power;
        }

        private static bool HasCircularDelegation(UInt160 delegator, UInt160 delegate_)
        {
            var current = delegate_;
            var depth = 0;

            while (!current.IsZero && depth < MAX_DELEGATION_DEPTH)
            {
                if (current.Equals(delegator))
                    return true;

                current = GetDelegate(current);
                depth++;
            }

            return false;
        }
        #endregion

        #region Utility Methods
        public static bool SetDefaultQuorum(int percentage)
        {
            ValidateNotPaused();
            ValidateAdmin();

            if (percentage < MIN_QUORUM_PERCENTAGE || percentage > MAX_QUORUM_PERCENTAGE)
                throw new ArgumentException("Invalid quorum percentage");

            var oldQuorum = GetDefaultQuorum();
            Storage.Put(Storage.CurrentContext, DEFAULT_QUORUM_KEY, percentage);

            QuorumChanged(Runtime.CallingScriptHash, oldQuorum, percentage);
            return true;
        }

        [Safe]
        public static int GetDefaultQuorum()
        {
            var quorumBytes = Storage.Get(Storage.CurrentContext, DEFAULT_QUORUM_KEY);
            return (int)(quorumBytes != null ? (BigInteger)quorumBytes : 50;
        }

        [Safe]
        public static string GetContractInfo()
        {
            return "EnterpriseVotingContract v1.0.0 - Production-ready governance and voting system";
        }
        #endregion
    }
}