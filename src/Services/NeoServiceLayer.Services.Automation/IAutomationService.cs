using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Automation;

/// <summary>
/// Interface for the Automation Service that provides smart contract automation and scheduling capabilities.
/// </summary>
public interface IAutomationService : IService
{
    /// <summary>
    /// Creates a new automation job.
    /// </summary>
    /// <param name="request">The job creation request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The unique job identifier.</returns>
    Task<string> CreateJobAsync(AutomationJobRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets the status of an automation job.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The job status.</returns>
    Task<AutomationJobStatus> GetJobStatusAsync(string jobId, BlockchainType blockchainType);

    /// <summary>
    /// Cancels an automation job.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the job was cancelled successfully.</returns>
    Task<bool> CancelJobAsync(string jobId, BlockchainType blockchainType);

    /// <summary>
    /// Pauses an automation job.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the job was paused successfully.</returns>
    Task<bool> PauseJobAsync(string jobId, BlockchainType blockchainType);

    /// <summary>
    /// Resumes a paused automation job.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the job was resumed successfully.</returns>
    Task<bool> ResumeJobAsync(string jobId, BlockchainType blockchainType);

    /// <summary>
    /// Gets all automation jobs for a specific address.
    /// </summary>
    /// <param name="address">The owner address.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>A collection of automation jobs.</returns>
    Task<IEnumerable<AutomationJob>> GetJobsAsync(string address, BlockchainType blockchainType);

    /// <summary>
    /// Updates an existing automation job.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="update">The job update request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the job was updated successfully.</returns>
    Task<bool> UpdateJobAsync(string jobId, AutomationJobUpdate update, BlockchainType blockchainType);

    /// <summary>
    /// Gets the execution history for an automation job.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>A collection of job executions.</returns>
    Task<IEnumerable<AutomationExecution>> GetExecutionHistoryAsync(string jobId, BlockchainType blockchainType);
} 