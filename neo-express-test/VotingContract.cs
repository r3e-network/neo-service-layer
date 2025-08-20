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


namespace VotingContract
{
    [DisplayName("VotingContract")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Description", "Decentralized voting and governance")]
    [ManifestExtra("Version", "1.0.0")]
    public class VotingContract : SmartContract
    {
        private const byte ProposalPrefix = 0x01;
        private const byte VotePrefix = 0x02;
        private const byte VoterPrefix = 0x03;
        
        [DisplayName("ProposalCreated")]
        public static event Action<UInt160, string, ulong> OnProposalCreated;

        [DisplayName("VoteCast")]
        public static event Action<UInt160, ByteString, bool> OnVoteCast;

        [DisplayName("ProposalExecuted")]
        public static event Action<ByteString, bool> OnProposalExecuted;

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (!update)
            {
                Runtime.Log("VotingContract deployed successfully");
            }
        }

        [DisplayName("createProposal")]
        public static ByteString CreateProposal(string title, string description, ulong endTime)
        {
            var proposer = Runtime.ExecutingScriptHash;
            var random = Runtime.GetRandom();
            var proposalId = CryptoLib.Sha256(random.ToByteArray());
            var proposalKey = ((ByteString)new byte[] { ProposalPrefix }).Concat(proposalId);
            
            var proposal = new Proposal
            {
                Id = proposalId,
                Title = title,
                Description = description,
                Proposer = proposer,
                EndTime = endTime,
                VotesFor = 0,
                VotesAgainst = 0,
                Executed = false
            };

            Storage.Put(Storage.CurrentContext, proposalKey, StdLib.Serialize(proposal));
            OnProposalCreated(proposer, title, endTime);
            
            return proposalId;
        }

        [DisplayName("vote")]
        public static bool Vote(ByteString proposalId, bool support)
        {
            var voter = Runtime.ExecutingScriptHash;
            var proposalKey = ((ByteString)new byte[] { ProposalPrefix }).Concat(proposalId);
            var voteKey = ((ByteString)new byte[] { VotePrefix }).Concat(proposalId).Concat(voter);
            
            // Check if already voted
            var existingVote = Storage.Get(Storage.CurrentContext, voteKey);
            if (existingVote != null)
                return false;
            
            // Get proposal
            var proposalBytes = Storage.Get(Storage.CurrentContext, proposalKey);
            if (proposalBytes == null)
                return false;
            
            var proposal = (Proposal)StdLib.Deserialize(proposalBytes);
            
            // Check if voting period is active
            if (Runtime.Time > proposal.EndTime)
                return false;
            
            // Cast vote
            Storage.Put(Storage.CurrentContext, voteKey, support ? 1 : 0);
            
            // Update proposal vote counts
            if (support)
                proposal.VotesFor++;
            else
                proposal.VotesAgainst++;
            
            Storage.Put(Storage.CurrentContext, proposalKey, StdLib.Serialize(proposal));
            OnVoteCast(voter, proposalId, support);
            
            return true;
        }

        [DisplayName("getProposal")]
        public static object GetProposal(ByteString proposalId)
        {
            var proposalKey = ((ByteString)new byte[] { ProposalPrefix }).Concat(proposalId);
            var proposalBytes = Storage.Get(Storage.CurrentContext, proposalKey);
            
            if (proposalBytes == null)
                return null;
            
            return StdLib.Deserialize(proposalBytes);
        }

        [DisplayName("executeProposal")]
        public static bool ExecuteProposal(ByteString proposalId)
        {
            var proposalKey = ((ByteString)new byte[] { ProposalPrefix }).Concat(proposalId);
            var proposalBytes = Storage.Get(Storage.CurrentContext, proposalKey);
            
            if (proposalBytes == null)
                return false;
            
            var proposal = (Proposal)StdLib.Deserialize(proposalBytes);
            
            // Check if voting period ended
            if (Runtime.Time <= proposal.EndTime)
                return false;
            
            // Check if already executed
            if (proposal.Executed)
                return false;
            
            // Simple majority rule
            var passed = proposal.VotesFor > proposal.VotesAgainst;
            
            // Mark as executed
            proposal.Executed = true;
            Storage.Put(Storage.CurrentContext, proposalKey, StdLib.Serialize(proposal));
            
            OnProposalExecuted(proposalId, passed);
            return passed;
        }
    }

    public class Proposal
    {
        public ByteString Id;
        public string Title;
        public string Description;
        public UInt160 Proposer;
        public ulong EndTime;
        public int VotesFor;
        public int VotesAgainst;
        public bool Executed;
    }
}