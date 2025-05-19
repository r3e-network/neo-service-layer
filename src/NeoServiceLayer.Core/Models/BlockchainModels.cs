using System;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents a transaction on the Neo N3 blockchain.
    /// </summary>
    public class BlockchainTransaction
    {
        /// <summary>
        /// Gets or sets the transaction hash.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Gets or sets the block index.
        /// </summary>
        public uint BlockIndex { get; set; }

        /// <summary>
        /// Gets or sets the block time.
        /// </summary>
        public ulong BlockTime { get; set; }

        /// <summary>
        /// Gets or sets the sender address.
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// Gets or sets the transaction size.
        /// </summary>
        public uint Size { get; set; }

        /// <summary>
        /// Gets or sets the transaction version.
        /// </summary>
        public byte Version { get; set; }

        /// <summary>
        /// Gets or sets the transaction nonce.
        /// </summary>
        public uint Nonce { get; set; }

        /// <summary>
        /// Gets or sets the transaction signers.
        /// </summary>
        public string[] Signers { get; set; }

        /// <summary>
        /// Gets or sets the transaction script.
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// Gets or sets the system fee.
        /// </summary>
        public string SystemFee { get; set; }

        /// <summary>
        /// Gets or sets the network fee.
        /// </summary>
        public string NetworkFee { get; set; }

        /// <summary>
        /// Gets or sets the transaction attributes.
        /// </summary>
        public TransactionAttribute[] Attributes { get; set; }
    }

    /// <summary>
    /// Represents a transaction attribute.
    /// </summary>
    public class TransactionAttribute
    {
        /// <summary>
        /// Gets or sets the attribute type.
        /// </summary>
        public byte Type { get; set; }

        /// <summary>
        /// Gets or sets the attribute data.
        /// </summary>
        public string Data { get; set; }
    }

    /// <summary>
    /// Represents the result of a contract invocation.
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
        /// Gets or sets the exception message, if any.
        /// </summary>
        public string Exception { get; set; }

        /// <summary>
        /// Gets or sets the stack items returned by the invocation.
        /// </summary>
        public object[] Stack { get; set; }
    }

    /// <summary>
    /// Represents an event emitted by a smart contract.
    /// </summary>
    public class BlockchainEvent
    {
        /// <summary>
        /// Gets or sets the transaction hash.
        /// </summary>
        public string TxHash { get; set; }

        /// <summary>
        /// Gets or sets the block index.
        /// </summary>
        public uint BlockIndex { get; set; }

        /// <summary>
        /// Gets or sets the event name.
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets the event state.
        /// </summary>
        public object[] State { get; set; }
    }
}
