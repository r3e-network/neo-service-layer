using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Tee.Host.Models
{
    /// <summary>
    /// Represents an event from a blockchain.
    /// </summary>
    public class BlockchainEvent
    {
        /// <summary>
        /// Gets or sets the unique identifier for the event.
        /// </summary>
        public string EventId { get; set; }

        /// <summary>
        /// Gets or sets the type of the event.
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Gets or sets the blockchain-specific identifier for the event.
        /// For example, in Ethereum, this might be the transaction hash.
        /// </summary>
        public string BlockchainEventId { get; set; }

        /// <summary>
        /// Gets or sets the name of the blockchain this event came from.
        /// </summary>
        public string BlockchainName { get; set; }

        /// <summary>
        /// Gets or sets the blockchain-specific network identifier.
        /// For example, in Ethereum, this might be "mainnet", "ropsten", etc.
        /// </summary>
        public string NetworkId { get; set; }

        /// <summary>
        /// Gets or sets the block number this event occurred in.
        /// </summary>
        public long BlockNumber { get; set; }

        /// <summary>
        /// Gets or sets the block hash this event occurred in.
        /// </summary>
        public string BlockHash { get; set; }

        /// <summary>
        /// Gets or sets the transaction hash this event was emitted in.
        /// </summary>
        public string TransactionHash { get; set; }

        /// <summary>
        /// Gets or sets the timestamp (in seconds since the Unix epoch) when this event occurred.
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the address of the contract that emitted this event.
        /// </summary>
        public string ContractAddress { get; set; }

        /// <summary>
        /// Gets or sets the data associated with this event.
        /// This is typically a JSON string, but the exact format depends on the event type.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Gets or sets the topics associated with this event.
        /// In Ethereum, these are the indexed parameters of the event.
        /// </summary>
        public List<string> Topics { get; set; }

        /// <summary>
        /// Gets or sets additional metadata associated with this event.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this event has been processed.
        /// </summary>
        public bool Processed { get; set; }

        /// <summary>
        /// Gets or sets the timestamp (in seconds since the Unix epoch) when this event was processed.
        /// </summary>
        public long ProcessedTimestamp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether processing this event resulted in an error.
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// Gets or sets the error message if processing this event resulted in an error.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockchainEvent"/> class.
        /// </summary>
        public BlockchainEvent()
        {
            EventId = Guid.NewGuid().ToString("N");
            Topics = new List<string>();
            Metadata = new Dictionary<string, string>();
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
} 