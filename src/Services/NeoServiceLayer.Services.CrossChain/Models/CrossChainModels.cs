using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.CrossChain.Models;

/// <summary>
/// Represents a cross-chain contract call request.
/// </summary>
public class CrossChainContractCallRequest
{
    /// <summary>
    /// Gets or sets the contract address.
    /// </summary>
    public string ContractAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target contract address.
    /// </summary>
    public string TargetContract { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method name.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method to call.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method parameters.
    /// </summary>
    public object[] Parameters { get; set; } = Array.Empty<object>();

    /// <summary>
    /// Gets or sets the gas limit.
    /// </summary>
    public long GasLimit { get; set; }

    /// <summary>
    /// Gets or sets the gas price.
    /// </summary>
    public decimal GasPrice { get; set; }

    /// <summary>
    /// Gets or sets the caller address.
    /// </summary>
    public string CallerAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents the result of a cross-chain execution.
/// </summary>
public class CrossChainExecutionResult
{
    /// <summary>
    /// Gets or sets the execution ID.
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the execution was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the return value.
    /// </summary>
    public object? ReturnValue { get; set; }

    /// <summary>
    /// Gets or sets the execution result.
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the gas consumed.
    /// </summary>
    public long GasConsumed { get; set; }

    /// <summary>
    /// Gets or sets the gas used.
    /// </summary>
    public long GasUsed { get; set; }

    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the block number.
    /// </summary>
    public long BlockNumber { get; set; }

    /// <summary>
    /// Gets or sets the error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the execution timestamp.
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}



/// <summary>
/// Represents a cross-chain pair configuration.
/// </summary>
public class CrossChainPair
{
    /// <summary>
    /// Gets or sets the pair ID.
    /// </summary>
    public string PairId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source chain.
    /// </summary>
    public BlockchainType SourceChain { get; set; }

    /// <summary>
    /// Gets or sets the target chain.
    /// </summary>
    public BlockchainType TargetChain { get; set; }

    /// <summary>
    /// Gets or sets whether the pair is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the pair is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the base fee for transactions.
    /// </summary>
    public decimal BaseFee { get; set; } = 0.001m;

    /// <summary>
    /// Gets or sets the estimated time in minutes.
    /// </summary>
    public int EstimatedTime { get; set; } = 5;

    /// <summary>
    /// Gets or sets the minimum transfer amount.
    /// </summary>
    public decimal MinTransferAmount { get; set; }

    /// <summary>
    /// Gets or sets the maximum transfer amount.
    /// </summary>
    public decimal MaxTransferAmount { get; set; }

    /// <summary>
    /// Gets or sets the transfer fee percentage.
    /// </summary>
    public decimal FeePercentage { get; set; }

    /// <summary>
    /// Gets or sets the required confirmations.
    /// </summary>
    public int RequiredConfirmations { get; set; }

    /// <summary>
    /// Gets or sets the supported tokens.
    /// </summary>
    public List<string> SupportedTokens { get; set; } = new();
}

/// <summary>
/// Represents a cross-chain transaction.
/// </summary>
public class CrossChainTransaction
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source blockchain.
    /// </summary>
    public BlockchainType SourceBlockchain { get; set; }

    /// <summary>
    /// Gets or sets the target blockchain.
    /// </summary>
    public BlockchainType TargetBlockchain { get; set; }

    /// <summary>
    /// Gets or sets the source chain (alias for SourceBlockchain).
    /// </summary>
    public BlockchainType SourceChain { get; set; }

    /// <summary>
    /// Gets or sets the target chain (alias for TargetBlockchain).
    /// </summary>
    public BlockchainType TargetChain { get; set; }

    /// <summary>
    /// Gets or sets the source transaction hash.
    /// </summary>
    public string SourceTransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target transaction hash.
    /// </summary>
    public string TargetTransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the from address.
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the to address.
    /// </summary>
    public string ToAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the token symbol.
    /// </summary>
    public string TokenSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction status.
    /// </summary>
    public CrossChainMessageState Status { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the completion timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the fee amount.
    /// </summary>
    public decimal Fee { get; set; }

    /// <summary>
    /// Gets or sets the transaction type.
    /// </summary>
    public CrossChainTransactionType Type { get; set; }

    /// <summary>
    /// Gets or sets the token contract address.
    /// </summary>
    public string TokenContract { get; set; } = string.Empty;
}

/// <summary>
/// Represents the state of a cross-chain message.
/// </summary>
public enum CrossChainMessageState
{
    /// <summary>
    /// Message is created.
    /// </summary>
    Created,

    /// <summary>
    /// Message is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// Message is confirmed.
    /// </summary>
    Confirmed,

    /// <summary>
    /// Message is being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Message processing completed.
    /// </summary>
    Completed,

    /// <summary>
    /// Message processing failed.
    /// </summary>
    Failed
}

/// <summary>
/// Represents the type of cross-chain transaction.
/// </summary>
public enum CrossChainTransactionType
{
    /// <summary>
    /// Token transfer transaction.
    /// </summary>
    TokenTransfer,

    /// <summary>
    /// Message transaction.
    /// </summary>
    Message,

    /// <summary>
    /// Contract call transaction.
    /// </summary>
    ContractCall
}



















