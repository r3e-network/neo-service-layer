using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Automation.Models;
using System.Linq;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Automation.Services
{
    /// <summary>
    /// Interface for managing automation jobs lifecycle.
    /// </summary>
    public interface IJobManagementService
    {
        /// <summary>
        /// Creates a new automation job.
        /// </summary>
        Task<string> CreateJobAsync(AutomationJobRequest request, BlockchainType blockchainType);

        /// <summary>
        /// Gets the status of a job.
        /// </summary>
        Task<AutomationJobStatus> GetJobStatusAsync(string jobId, BlockchainType blockchainType);

        /// <summary>
        /// Cancels a job.
        /// </summary>
        Task<bool> CancelJobAsync(string jobId, BlockchainType blockchainType);

        /// <summary>
        /// Pauses a job.
        /// </summary>
        Task<bool> PauseJobAsync(string jobId, BlockchainType blockchainType);

        /// <summary>
        /// Resumes a paused job.
        /// </summary>
        Task<bool> ResumeJobAsync(string jobId, BlockchainType blockchainType);

        /// <summary>
        /// Gets all jobs for an address.
        /// </summary>
        Task<IEnumerable<AutomationJob>> GetJobsAsync(string address, BlockchainType blockchainType);

        /// <summary>
        /// Updates a job configuration.
        /// </summary>
        Task<bool> UpdateJobAsync(string jobId, AutomationJobUpdate update, BlockchainType blockchainType);

        /// <summary>
        /// Gets job execution history.
        /// </summary>
        Task<IEnumerable<AutomationExecution>> GetExecutionHistoryAsync(string jobId, BlockchainType blockchainType);

        /// <summary>
        /// Stores a job in persistent storage.
        /// </summary>
        Task StoreJobAsync(AutomationJob job);

        /// <summary>
        /// Loads a job from persistent storage.
        /// </summary>
        Task<AutomationJob?> LoadJobAsync(string jobId);

        /// <summary>
        /// Deletes old execution history.
        /// </summary>
        Task CleanupOldExecutionsAsync(TimeSpan retentionPeriod);
    }
}
