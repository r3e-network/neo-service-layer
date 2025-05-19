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
    [DisplayName("PrivateVoting")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Email", "info@neoservicelayer.io")]
    [ManifestExtra("Description", "Private Voting Example for Neo Service Layer")]
    public class PrivateVoting : SmartContract
    {
        // Events
        [DisplayName("VotingCreated")]
        public static event Action<string, UInt160, BigInteger, BigInteger> OnVotingCreated;

        [DisplayName("VoteCast")]
        public static event Action<string, UInt160> OnVoteCast;

        [DisplayName("VotingEnded")]
        public static event Action<string, BigInteger[]> OnVotingEnded;

        // Storage keys
        private static readonly byte[] OwnerKey = "owner".ToByteArray();
        private static readonly byte[] VotingPrefix = "voting".ToByteArray();
        private static readonly byte[] VotePrefix = "vote".ToByteArray();
        private static readonly byte[] ServiceLayerKey = "serviceLayer".ToByteArray();

        // Voting status
        private enum VotingStatus : byte
        {
            Active = 0,
            Ended = 1
        }

        // Custom properties
        [DisplayName("ServiceLayerAddress")]
        public static UInt160 ServiceLayerAddress()
        {
            return (UInt160)Storage.Get(Storage.CurrentContext, ServiceLayerKey);
        }

        // Constructor
        public static void _deploy(object data, bool update)
        {
            if (!update)
            {
                // Set initial owner to the contract deployer
                Storage.Put(Storage.CurrentContext, OwnerKey, Runtime.CallingScriptHash);
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

        // Create a new voting
        public static void CreateVoting(string votingId, BigInteger optionsCount, BigInteger endTime)
        {
            // Only owner can create voting
            if (!IsOwner()) throw new Exception("No authorization");
            
            // Validate parameters
            if (string.IsNullOrEmpty(votingId))
                throw new Exception("Invalid voting ID");
            if (optionsCount <= 1)
                throw new Exception("Options count must be greater than 1");
            if (endTime <= Runtime.Time)
                throw new Exception("End time must be in the future");
                
            // Check if voting already exists
            byte[] votingKey = VotingPrefix.Concat(votingId.ToByteArray());
            if (Storage.Get(Storage.CurrentContext, votingKey) != null)
                throw new Exception("Voting already exists");
                
            // Create voting data
            var votingData = new VotingData
            {
                Creator = Runtime.CallingScriptHash,
                OptionsCount = optionsCount,
                EndTime = endTime,
                Status = VotingStatus.Active
            };
            
            // Store voting data
            Storage.Put(Storage.CurrentContext, votingKey, StdLib.Serialize(votingData));
            
            // Notify voting created event
            OnVotingCreated(votingId, Runtime.CallingScriptHash, optionsCount, endTime);
        }

        // Cast a private vote
        public static bool CastVote(string votingId, byte[] encryptedVote)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(votingId))
                throw new Exception("Invalid voting ID");
            if (encryptedVote is null || encryptedVote.Length == 0)
                throw new Exception("Invalid encrypted vote");
                
            // Get voting data
            byte[] votingKey = VotingPrefix.Concat(votingId.ToByteArray());
            byte[] votingBytes = Storage.Get(Storage.CurrentContext, votingKey);
            if (votingBytes is null)
                throw new Exception("Voting does not exist");
                
            // Deserialize voting data
            VotingData votingData = (VotingData)StdLib.Deserialize(votingBytes);
            
            // Check if voting is active
            if (votingData.Status != VotingStatus.Active)
                throw new Exception("Voting is not active");
                
            // Check if voting has ended
            if (Runtime.Time > votingData.EndTime)
                throw new Exception("Voting has ended");
                
            // Check if the voter has already voted
            byte[] voteKey = VotePrefix.Concat(votingId.ToByteArray()).Concat(Runtime.CallingScriptHash);
            if (Storage.Get(Storage.CurrentContext, voteKey) != null)
                throw new Exception("Already voted");
                
            // Call Neo Service Layer to process the vote
            bool success = (bool)Contract.Call(
                ServiceLayerAddress(),
                "processPrivateVote",
                CallFlags.All,
                new object[] { votingId, Runtime.CallingScriptHash, encryptedVote, votingData.OptionsCount }
            );
            
            if (success)
            {
                // Store vote record (just a marker that the user has voted)
                Storage.Put(Storage.CurrentContext, voteKey, "voted");
                
                // Notify vote cast event
                OnVoteCast(votingId, Runtime.CallingScriptHash);
            }
            
            return success;
        }

        // End a voting and tally the results
        public static BigInteger[] EndVoting(string votingId)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(votingId))
                throw new Exception("Invalid voting ID");
                
            // Get voting data
            byte[] votingKey = VotingPrefix.Concat(votingId.ToByteArray());
            byte[] votingBytes = Storage.Get(Storage.CurrentContext, votingKey);
            if (votingBytes is null)
                throw new Exception("Voting does not exist");
                
            // Deserialize voting data
            VotingData votingData = (VotingData)StdLib.Deserialize(votingBytes);
            
            // Check if voting is already ended
            if (votingData.Status == VotingStatus.Ended)
                throw new Exception("Voting is already ended");
                
            // Check if voting end time has passed or if the caller is the owner
            if (Runtime.Time <= votingData.EndTime && !IsOwner())
                throw new Exception("Voting has not ended yet");
                
            // Call Neo Service Layer to tally the votes
            BigInteger[] results = (BigInteger[])Contract.Call(
                ServiceLayerAddress(),
                "tallyPrivateVotes",
                CallFlags.All,
                new object[] { votingId, votingData.OptionsCount }
            );
            
            // Update voting status
            votingData.Status = VotingStatus.Ended;
            Storage.Put(Storage.CurrentContext, votingKey, StdLib.Serialize(votingData));
            
            // Notify voting ended event
            OnVotingEnded(votingId, results);
            
            return results;
        }

        // Get voting information
        public static VotingInfo GetVotingInfo(string votingId)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(votingId))
                throw new Exception("Invalid voting ID");
                
            // Get voting data
            byte[] votingKey = VotingPrefix.Concat(votingId.ToByteArray());
            byte[] votingBytes = Storage.Get(Storage.CurrentContext, votingKey);
            if (votingBytes is null)
                throw new Exception("Voting does not exist");
                
            // Deserialize voting data
            VotingData votingData = (VotingData)StdLib.Deserialize(votingBytes);
            
            // Create voting info
            return new VotingInfo
            {
                Creator = votingData.Creator,
                OptionsCount = votingData.OptionsCount,
                EndTime = votingData.EndTime,
                Status = (byte)votingData.Status
            };
        }

        // Check if a user has voted
        public static bool HasVoted(string votingId, UInt160 voter)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(votingId))
                throw new Exception("Invalid voting ID");
            if (voter is null || !voter.IsValid)
                throw new Exception("Invalid voter address");
                
            // Check if the voter has voted
            byte[] voteKey = VotePrefix.Concat(votingId.ToByteArray()).Concat(voter);
            return Storage.Get(Storage.CurrentContext, voteKey) != null;
        }

        // Helper methods
        private static bool IsOwner()
        {
            UInt160 owner = (UInt160)Storage.Get(Storage.CurrentContext, OwnerKey);
            return Runtime.CheckWitness(owner);
        }

        // Data structures
        public class VotingData
        {
            public UInt160 Creator;
            public BigInteger OptionsCount;
            public BigInteger EndTime;
            public VotingStatus Status;
        }

        public class VotingInfo
        {
            public UInt160 Creator;
            public BigInteger OptionsCount;
            public BigInteger EndTime;
            public byte Status;
        }
    }
}
