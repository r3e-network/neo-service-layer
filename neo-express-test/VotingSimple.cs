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


namespace VotingSimple
{
    [DisplayName("VotingSimple")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Description", "Simple voting contract")]
    [ManifestExtra("Version", "1.0.0")]
    public class VotingSimple : SmartContract
    {
        private const byte ProposalPrefix = 0x01;
        private const byte VotePrefix = 0x02;
        
        [DisplayName("ProposalCreated")]
        public static event Action<string, UInt160, ulong> OnProposalCreated;

        [DisplayName("VoteCast")]
        public static event Action<string, UInt160, bool> OnVoteCast;

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (!update)
            {
                Runtime.Log("VotingSimple deployed successfully");
            }
        }

        [DisplayName("createProposal")]
        public static string CreateProposal(string title, string description, ulong endTime)
        {
            var proposer = Runtime.ExecutingScriptHash;
            var proposalId = title + "-" + Runtime.Time;
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
            OnProposalCreated(title, proposer, endTime);
            
            return proposalId;
        }

        [DisplayName("vote")]
        public static bool Vote(string proposalId, bool support)
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
            OnVoteCast(proposalId, voter, support);
            
            return true;
        }

        [DisplayName("getProposal")]
        public static object GetProposal(string proposalId)
        {
            var proposalKey = ((ByteString)new byte[] { ProposalPrefix }).Concat(proposalId);
            var proposalBytes = Storage.Get(Storage.CurrentContext, proposalKey);
            
            if (proposalBytes == null)
                return null;
            
            return StdLib.Deserialize(proposalBytes);
        }

        [DisplayName("getVoteCount")]
        public static object GetVoteCount(string proposalId)
        {
            var proposalKey = ((ByteString)new byte[] { ProposalPrefix }).Concat(proposalId);
            var proposalBytes = Storage.Get(Storage.CurrentContext, proposalKey);
            
            if (proposalBytes == null)
                return null;
            
            var proposal = (Proposal)StdLib.Deserialize(proposalBytes);
            return new { For = proposal.VotesFor, Against = proposal.VotesAgainst };
        }
    }

    public class Proposal
    {
        public string Id;
        public string Title;
        public string Description;
        public UInt160 Proposer;
        public ulong EndTime;
        public int VotesFor;
        public int VotesAgainst;
        public bool Executed;
    }
}