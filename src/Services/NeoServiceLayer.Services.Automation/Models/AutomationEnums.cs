namespace NeoServiceLayer.Services.Automation;

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
    Expired = 6
}

/// <summary>
/// Represents the status of an automation job execution.
/// </summary>
public enum AutomationExecutionStatus
{
    /// <summary>
    /// Execution is currently in progress.
    /// </summary>
    Executing = 0,

    /// <summary>
    /// Execution completed successfully.
    /// </summary>
    Completed = 1,

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
    TimedOut = 4,

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
    Time = 0,

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
    Schedule = 8
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
    HttpWebhook = 1,

    /// <summary>
    /// Email notification action.
    /// </summary>
    EmailNotification = 2,

    /// <summary>
    /// SMS notification action.
    /// </summary>
    SmsNotification = 3,

    /// <summary>
    /// Database update action.
    /// </summary>
    DatabaseUpdate = 4,

    /// <summary>
    /// File system operation action.
    /// </summary>
    FileSystemOperation = 5,

    /// <summary>
    /// Custom script execution action.
    /// </summary>
    CustomScript = 6,

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
    NftMint = 9
}
