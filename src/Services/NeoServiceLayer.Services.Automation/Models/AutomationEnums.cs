namespace NeoServiceLayer.Services.Automation.Models;

/// <summary>
/// Represents the status of an automation job.
/// </summary>
public enum AutomationJobStatus
{
    /// <summary>
    /// Job has been created but not yet activated.
    /// </summary>
    Created = 0,

    /// <summary>
    /// Job is active and will execute according to its trigger.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Job is paused and will not execute.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// Job has been cancelled and will not execute.
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Job has completed all its executions.
    /// </summary>
    Completed = 4,

    /// <summary>
    /// Job has failed and cannot continue.
    /// </summary>
    Failed = 5,

    /// <summary>
    /// Job has expired and will not execute.
    /// </summary>
    Expired = 6,

    /// <summary>
    /// Job not found.
    /// </summary>
    NotFound = 7
}

/// <summary>
/// Represents the status of an automation job execution.
/// </summary>
public enum AutomationExecutionStatus
{
    /// <summary>
    /// Execution is currently in progress.
    /// </summary>
    InProgress = 0,
    
    /// <summary>
    /// Execution is currently executing (alias for InProgress).
    /// </summary>
    Executing = InProgress,

    /// <summary>
    /// Execution completed successfully.
    /// </summary>
    Success = 1,
    
    /// <summary>
    /// Execution completed (alias for Success).
    /// </summary>
    Completed = Success,

    /// <summary>
    /// Execution failed with an error.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Execution was cancelled before completion.
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Execution timed out.
    /// </summary>
    Timeout = 4,

    /// <summary>
    /// Execution is waiting for retry.
    /// </summary>
    Retrying = 5,

    /// <summary>
    /// Execution is pending (waiting to start).
    /// </summary>
    Pending = 6
}

/// <summary>
/// Represents the type of trigger for an automation job.
/// </summary>
public enum AutomationTriggerType
{
    /// <summary>
    /// Time-based trigger using cron schedule.
    /// </summary>
    Cron = 0,

    /// <summary>
    /// Event-based trigger responding to blockchain events.
    /// </summary>
    Event = 1,

    /// <summary>
    /// Condition-based trigger that checks conditions periodically.
    /// </summary>
    Condition = 2,

    /// <summary>
    /// Manual trigger that requires explicit execution.
    /// </summary>
    Manual = 3,

    /// <summary>
    /// Webhook trigger that responds to HTTP requests.
    /// </summary>
    Webhook = 4,

    /// <summary>
    /// Oracle data trigger that responds to external data changes.
    /// </summary>
    Oracle = 5,

    /// <summary>
    /// Block height trigger that executes at specific block heights.
    /// </summary>
    BlockHeight = 6,

    /// <summary>
    /// Price trigger that executes when price conditions are met.
    /// </summary>
    Price = 7,

    /// <summary>
    /// Schedule trigger that executes based on a defined schedule.
    /// </summary>
    Schedule = 8,

    /// <summary>
    /// Time trigger that executes at specific times.
    /// </summary>
    Time = 9
}

/// <summary>
/// Represents the type of action for an automation.
/// </summary>
public enum AutomationActionType
{
    /// <summary>
    /// Smart contract action that executes a smart contract method.
    /// </summary>
    SmartContract = 0,

    /// <summary>
    /// HTTP webhook action that sends HTTP requests.
    /// </summary>
    Webhook = 1,

    /// <summary>
    /// Email notification action.
    /// </summary>
    Email = 2,

    /// <summary>
    /// SMS notification action.
    /// </summary>
    Sms = 3,

    /// <summary>
    /// Database update action.
    /// </summary>
    DatabaseUpdate = 4,

    /// <summary>
    /// File system operation action.
    /// </summary>
    FileOperation = 5,

    /// <summary>
    /// Custom script execution action.
    /// </summary>
    Script = 6,

    /// <summary>
    /// Multi-action that executes multiple actions in sequence.
    /// </summary>
    MultiAction = 7,

    /// <summary>
    /// Token transfer action.
    /// </summary>
    TokenTransfer = 8,

    /// <summary>
    /// NFT minting action.
    /// </summary>
    NftMint = 9,

    /// <summary>
    /// HTTP webhook action.
    /// </summary>
    HttpWebhook = 10
}

/// <summary>
/// Represents the type of condition for automation.
/// </summary>
public enum AutomationConditionType
{
    /// <summary>
    /// Time-based condition.
    /// </summary>
    Time = 0,

    /// <summary>
    /// Blockchain condition (block height, transaction, etc.).
    /// </summary>
    Blockchain = 1,

    /// <summary>
    /// Price condition based on market data.
    /// </summary>
    Price = 2,

    /// <summary>
    /// Oracle data condition.
    /// </summary>
    Oracle = 3,

    /// <summary>
    /// Custom logic condition.
    /// </summary>
    Custom = 4,

    /// <summary>
    /// Token balance condition.
    /// </summary>
    Balance = 5,

    /// <summary>
    /// Smart contract state condition.
    /// </summary>
    ContractState = 6
}
