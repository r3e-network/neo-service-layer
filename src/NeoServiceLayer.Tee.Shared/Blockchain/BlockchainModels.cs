using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Tee.Shared.Blockchain
{
    /// <summary>
    /// Represents a blockchain block.
    /// </summary>
    public class BlockchainBlock
    {
        /// <summary>
        /// Gets or sets the block hash.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Gets or sets the block height.
        /// </summary>
        public ulong Height { get; set; }

        /// <summary>
        /// Gets or sets the block timestamp.
        /// </summary>
        public ulong Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the block size.
        /// </summary>
        public uint Size { get; set; }

        /// <summary>
        /// Gets or sets the block version.
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        /// Gets or sets the block merkle root.
        /// </summary>
        public string MerkleRoot { get; set; }

        /// <summary>
        /// Gets or sets the block previous hash.
        /// </summary>
        public string PreviousHash { get; set; }

        /// <summary>
        /// Gets or sets the block next hash.
        /// </summary>
        public string NextHash { get; set; }

        /// <summary>
        /// Gets or sets the block transaction count.
        /// </summary>
        public uint TransactionCount { get; set; }

        /// <summary>
        /// Gets or sets the block transaction hashes.
        /// </summary>
        public string[] TransactionHashes { get; set; }

        /// <summary>
        /// Gets or sets the block transactions.
        /// </summary>
        public BlockchainTransaction[] Transactions { get; set; }

        /// <summary>
        /// Gets or sets the block confirmations.
        /// </summary>
        public ulong Confirmations { get; set; }

        /// <summary>
        /// Gets or sets the block metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a blockchain transaction.
    /// </summary>
    public class BlockchainTransaction
    {
        /// <summary>
        /// Gets or sets the transaction hash.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Gets or sets the block hash.
        /// </summary>
        public string BlockHash { get; set; }

        /// <summary>
        /// Gets or sets the block height.
        /// </summary>
        public ulong BlockHeight { get; set; }

        /// <summary>
        /// Gets or sets the transaction timestamp.
        /// </summary>
        public ulong Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the transaction sender.
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// Gets or sets the transaction size.
        /// </summary>
        public uint Size { get; set; }

        /// <summary>
        /// Gets or sets the transaction version.
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        /// Gets or sets the transaction nonce.
        /// </summary>
        public ulong Nonce { get; set; }

        /// <summary>
        /// Gets or sets the transaction signers.
        /// </summary>
        public string[] Signers { get; set; }

        /// <summary>
        /// Gets or sets the transaction script.
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// Gets or sets the transaction system fee.
        /// </summary>
        public string SystemFee { get; set; }

        /// <summary>
        /// Gets or sets the transaction network fee.
        /// </summary>
        public string NetworkFee { get; set; }

        /// <summary>
        /// Gets or sets the transaction attributes.
        /// </summary>
        public TransactionAttribute[] Attributes { get; set; }

        /// <summary>
        /// Gets or sets the transaction witnesses.
        /// </summary>
        public TransactionWitness[] Witnesses { get; set; }

        /// <summary>
        /// Gets or sets the transaction confirmations.
        /// </summary>
        public ulong Confirmations { get; set; }

        /// <summary>
        /// Gets or sets the transaction metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a transaction attribute.
    /// </summary>
    public class TransactionAttribute
    {
        /// <summary>
        /// Gets or sets the attribute type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the attribute value.
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// Represents a transaction witness.
    /// </summary>
    public class TransactionWitness
    {
        /// <summary>
        /// Gets or sets the witness invocation script.
        /// </summary>
        public string InvocationScript { get; set; }

        /// <summary>
        /// Gets or sets the witness verification script.
        /// </summary>
        public string VerificationScript { get; set; }
    }

    /// <summary>
    /// Represents a blockchain event.
    /// </summary>
    public class BlockchainEvent
    {
        /// <summary>
        /// Gets or sets the event ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the transaction hash.
        /// </summary>
        public string TxHash { get; set; }

        /// <summary>
        /// Gets or sets the block hash.
        /// </summary>
        public string BlockHash { get; set; }

        /// <summary>
        /// Gets or sets the block height.
        /// </summary>
        public ulong BlockHeight { get; set; }

        /// <summary>
        /// Gets or sets the event timestamp.
        /// </summary>
        public ulong Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the contract hash.
        /// </summary>
        public string ContractHash { get; set; }

        /// <summary>
        /// Gets or sets the event name.
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets the event state.
        /// </summary>
        public object[] State { get; set; }

        /// <summary>
        /// Gets or sets the event metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the BlockchainEvent class.
        /// </summary>
        public BlockchainEvent()
        {
            Id = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Represents a contract invocation result.
    /// </summary>
    public class ContractInvocationResult
    {
        /// <summary>
        /// Gets or sets the invocation state.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the gas consumed.
        /// </summary>
        public string GasConsumed { get; set; }

        /// <summary>
        /// Gets or sets the exception.
        /// </summary>
        public string Exception { get; set; }

        /// <summary>
        /// Gets or sets the stack.
        /// </summary>
        public object[] Stack { get; set; }

        /// <summary>
        /// Gets or sets the notifications.
        /// </summary>
        public object[] Notifications { get; set; }

        /// <summary>
        /// Gets or sets the invocation metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a blockchain subscription.
    /// </summary>
    public class BlockchainSubscription
    {
        /// <summary>
        /// Gets or sets the subscription ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the contract hash.
        /// </summary>
        public string ContractHash { get; set; }

        /// <summary>
        /// Gets or sets the event name.
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets the starting block height.
        /// </summary>
        public ulong FromBlock { get; set; }

        /// <summary>
        /// Gets or sets the last processed block height.
        /// </summary>
        public ulong LastProcessedBlock { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the subscription is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the subscription metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the BlockchainSubscription class.
        /// </summary>
        public BlockchainSubscription()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            IsActive = true;
        }
    }
}
