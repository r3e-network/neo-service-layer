namespace NeoServiceLayer.Advanced.FairOrdering.Models;

/// <summary>
/// Represents risk level.
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Minimal risk level.
    /// </summary>
    Minimal,

    /// <summary>
    /// Low risk level.
    /// </summary>
    Low,

    /// <summary>
    /// Medium risk level.
    /// </summary>
    Medium,

    /// <summary>
    /// High risk level.
    /// </summary>
    High,

    /// <summary>
    /// Critical risk level.
    /// </summary>
    Critical
}

/// <summary>
/// Represents pool status.
/// </summary>
public enum PoolStatus
{
    /// <summary>
    /// Pool is active and accepting transactions.
    /// </summary>
    Active,

    /// <summary>
    /// Pool is paused.
    /// </summary>
    Paused,

    /// <summary>
    /// Pool is draining (not accepting new transactions).
    /// </summary>
    Draining,

    /// <summary>
    /// Pool is inactive.
    /// </summary>
    Inactive
}

/// <summary>
/// Represents ordering algorithm.
/// </summary>
public enum OrderingAlgorithm
{
    /// <summary>
    /// First-in-first-out ordering.
    /// </summary>
    FIFO,

    /// <summary>
    /// First-come-first-served ordering.
    /// </summary>
    FirstComeFirstServed,

    /// <summary>
    /// Fair queue ordering.
    /// </summary>
    FairQueue,

    /// <summary>
    /// Priority-based ordering.
    /// </summary>
    Priority,

    /// <summary>
    /// Priority-based fair ordering.
    /// </summary>
    PriorityFair,

    /// <summary>
    /// Random fair ordering.
    /// </summary>
    RandomFair,

    /// <summary>
    /// Weighted fair queuing.
    /// </summary>
    WeightedFairQueuing,

    /// <summary>
    /// Time-based fair ordering.
    /// </summary>
    TimeBased,

    /// <summary>
    /// MEV-resistant ordering.
    /// </summary>
    MevResistant
}

/// <summary>
/// Represents fairness level.
/// </summary>
public enum FairnessLevel
{
    /// <summary>
    /// Basic fairness level.
    /// </summary>
    Basic,

    /// <summary>
    /// Standard fairness level.
    /// </summary>
    Standard,

    /// <summary>
    /// High fairness level.
    /// </summary>
    High,

    /// <summary>
    /// Maximum fairness level.
    /// </summary>
    Maximum
}

/// <summary>
/// Represents MEV protection level.
/// </summary>
public enum MevProtectionLevel
{
    /// <summary>
    /// No MEV protection.
    /// </summary>
    None,

    /// <summary>
    /// Basic MEV protection.
    /// </summary>
    Basic,

    /// <summary>
    /// Standard MEV protection.
    /// </summary>
    Standard,

    /// <summary>
    /// High MEV protection.
    /// </summary>
    High,

    /// <summary>
    /// Maximum MEV protection.
    /// </summary>
    Maximum
}

/// <summary>
/// Represents transaction status.
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// Transaction is being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Transaction has been ordered.
    /// </summary>
    Ordered,

    /// <summary>
    /// Transaction has been executed.
    /// </summary>
    Executed,

    /// <summary>
    /// Transaction has been confirmed.
    /// </summary>
    Confirmed,

    /// <summary>
    /// Transaction has failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Transaction has been rejected.
    /// </summary>
    Rejected,

    /// <summary>
    /// Transaction has been cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Represents MEV analysis depth.
/// </summary>
public enum MevAnalysisDepth
{
    /// <summary>
    /// Basic analysis.
    /// </summary>
    Basic,

    /// <summary>
    /// Standard analysis.
    /// </summary>
    Standard,

    /// <summary>
    /// Deep analysis.
    /// </summary>
    Deep,

    /// <summary>
    /// Comprehensive analysis.
    /// </summary>
    Comprehensive
}

/// <summary>
/// Represents MEV opportunity type.
/// </summary>
public enum MevOpportunityType
{
    /// <summary>
    /// Arbitrage opportunity.
    /// </summary>
    Arbitrage,

    /// <summary>
    /// Front-running opportunity.
    /// </summary>
    FrontRunning,

    /// <summary>
    /// Back-running opportunity.
    /// </summary>
    BackRunning,

    /// <summary>
    /// Sandwich attack opportunity.
    /// </summary>
    SandwichAttack,

    /// <summary>
    /// Liquidation opportunity.
    /// </summary>
    Liquidation,

    /// <summary>
    /// Flash loan opportunity.
    /// </summary>
    FlashLoan,

    /// <summary>
    /// Price manipulation opportunity.
    /// </summary>
    PriceManipulation,

    /// <summary>
    /// Other MEV opportunity.
    /// </summary>
    Other
}

/// <summary>
/// Represents batch status.
/// </summary>
public enum BatchStatus
{
    /// <summary>
    /// Batch is being formed.
    /// </summary>
    Forming,

    /// <summary>
    /// Batch is ready for processing.
    /// </summary>
    Ready,

    /// <summary>
    /// Batch is being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Batch has been completed.
    /// </summary>
    Completed,

    /// <summary>
    /// Batch processing failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Batch was cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Represents ordering priority.
/// </summary>
public enum OrderingPriority
{
    /// <summary>
    /// Lowest priority.
    /// </summary>
    Lowest = 1,

    /// <summary>
    /// Low priority.
    /// </summary>
    Low = 2,

    /// <summary>
    /// Normal priority.
    /// </summary>
    Normal = 3,

    /// <summary>
    /// High priority.
    /// </summary>
    High = 4,

    /// <summary>
    /// Highest priority.
    /// </summary>
    Highest = 5,

    /// <summary>
    /// Critical priority.
    /// </summary>
    Critical = 6
}

/// <summary>
/// Represents ordering strategy.
/// </summary>
public enum OrderingStrategy
{
    /// <summary>
    /// Time-based ordering.
    /// </summary>
    TimeBased,

    /// <summary>
    /// Fee-based ordering.
    /// </summary>
    FeeBased,

    /// <summary>
    /// Fairness-based ordering.
    /// </summary>
    FairnessBased,

    /// <summary>
    /// MEV-resistant ordering.
    /// </summary>
    MevResistant,

    /// <summary>
    /// Hybrid ordering strategy.
    /// </summary>
    Hybrid,

    /// <summary>
    /// Custom ordering strategy.
    /// </summary>
    Custom
}

/// <summary>
/// Represents protection mechanism.
/// </summary>
public enum ProtectionMechanism
{
    /// <summary>
    /// Time delay protection.
    /// </summary>
    TimeDelay,

    /// <summary>
    /// Randomization protection.
    /// </summary>
    Randomization,

    /// <summary>
    /// Commit-reveal protection.
    /// </summary>
    CommitReveal,

    /// <summary>
    /// Threshold encryption protection.
    /// </summary>
    ThresholdEncryption,

    /// <summary>
    /// Verifiable delay function protection.
    /// </summary>
    VerifiableDelayFunction,

    /// <summary>
    /// Batch auction protection.
    /// </summary>
    BatchAuction,

    /// <summary>
    /// Private mempool protection.
    /// </summary>
    PrivateMempool
}
