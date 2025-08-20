using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core.Models
{

    // Note: Transaction is defined in Transaction.cs

    // Note: Block is defined in Block.cs

    /// <summary>
    /// Represents core models used throughout the system
    /// </summary>
    public class CoreModels
    {
        public static readonly Dictionary<string, Type> ModelTypes = new()
        {
            { "Transaction", typeof(Transaction) },
            { "Block", typeof(Block) },
            { "BlockchainType", typeof(NeoServiceLayer.Core.BlockchainType) }
        };
    }
}