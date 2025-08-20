using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Automation.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Automation;

/// <summary>
/// Interface for the Automation Service that provides smart contract automation and scheduling capabilities.
/// </summary>
public interface IAutomationService : IService
{
    /// <summary>
    /// Creates a new automation.
    /// </summary>
    /// <param name="request">The automation creation request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The automation creation response.</returns>
    Task<CreateAutomationResponse> CreateAutomationAsync(CreateAutomationRequest request, BlockchainType blockchainType);

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

    /// <summary>
    /// Updates an existing automation.
    /// </summary>
    /// <param name="request">The update automation request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The update automation response.</returns>
    Task<UpdateAutomationResponse> UpdateAutomationAsync(UpdateAutomationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Deletes an automation.
    /// </summary>
    /// <param name="automationId">The automation identifier.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The delete automation response.</returns>
    Task<DeleteAutomationResponse> DeleteAutomationAsync(string automationId, BlockchainType blockchainType);

    /// <summary>
    /// Gets a list of automations based on filter criteria.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>A collection of automation information.</returns>
    Task<IEnumerable<AutomationInfo>> GetAutomationsAsync(AutomationFilter filter, BlockchainType blockchainType);

    /// <summary>
    /// Gets detailed information about a specific automation.
    /// </summary>
    /// <param name="automationId">The automation identifier.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The automation information.</returns>
    Task<AutomationInfo> GetAutomationAsync(string automationId, BlockchainType blockchainType);

    /// <summary>
    /// Executes an automation manually.
    /// </summary>
    /// <param name="automationId">The automation identifier.</param>
    /// <param name="context">The execution context.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The execution result.</returns>
    Task<ExecutionResult> ExecuteAutomationAsync(string automationId, Models.ExecutionContext context, BlockchainType blockchainType);

    /// <summary>
    /// Gets the execution history for an automation.
    /// </summary>
    /// <param name="request">The execution history request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The execution history response.</returns>
    Task<ExecutionHistoryResponse> GetExecutionHistoryAsync(ExecutionHistoryRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Pauses an automation.
    /// </summary>
    /// <param name="automationId">The automation identifier.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The pause response.</returns>
    Task<PauseResumeResponse> PauseAutomationAsync(string automationId, BlockchainType blockchainType);

    /// <summary>
    /// Resumes a paused automation.
    /// </summary>
    /// <param name="automationId">The automation identifier.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The resume response.</returns>
    Task<PauseResumeResponse> ResumeAutomationAsync(string automationId, BlockchainType blockchainType);

    /// <summary>
    /// Validates automation configuration.
    /// </summary>
    /// <param name="request">The validation request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The validation response.</returns>
    Task<ValidationResponse> ValidateAutomationAsync(ValidationRequest request, BlockchainType blockchainType);
}
