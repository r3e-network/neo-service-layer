using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.ConfidentialComputing
{
    /// <summary>
    /// Manages secure communication sessions with SGX enclaves
    /// Provides high-level abstraction for enclave interaction
    /// </summary>
    public interface IEnclaveSessionManager
    {
        /// <summary>
        /// Creates a new secure session with an enclave
        /// </summary>
        /// <param name="sessionOptions">Session configuration options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Session handle for communication</returns>
        Task<IEnclaveSession> CreateSessionAsync(
            ConfidentialSessionOptions sessionOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an existing session by ID
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Session handle or null if not found</returns>
        Task<IEnclaveSession?> GetSessionAsync(
            string sessionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all active sessions
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of active session information</returns>
        Task<IEnumerable<EnclaveSessionInfo>> ListActiveSessionsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Terminates a session and cleans up resources
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<SessionTerminationResult> TerminateSessionAsync(
            string sessionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets session statistics and metrics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Session manager statistics</returns>
        Task<SessionManagerStatistics> GetStatisticsAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents an active secure session with an SGX enclave
    /// </summary>
    public interface IEnclaveSession : IDisposable
    {
        /// <summary>
        /// Unique session identifier
        /// </summary>
        string SessionId { get; }

        /// <summary>
        /// Session name for identification
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Whether the session is active and ready for use
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Session creation timestamp
        /// </summary>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Last activity timestamp
        /// </summary>
        DateTime LastActivityAt { get; }

        /// <summary>
        /// Session security requirements
        /// </summary>
        ConfidentialSecurityRequirements SecurityRequirements { get; }

        /// <summary>
        /// Executes a confidential computation within the session
        /// </summary>
        /// <typeparam name="TInput">Input data type</typeparam>
        /// <typeparam name="TOutput">Output data type</typeparam>
        /// <param name="computation">Computation definition</param>
        /// <param name="input">Input data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Computation result</returns>
        Task<ConfidentialComputationResult<TOutput>> ExecuteAsync<TInput, TOutput>(
            ConfidentialComputation<TInput, TOutput> computation,
            TInput input,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes JavaScript code within the session
        /// </summary>
        /// <param name="script">JavaScript code to execute</param>
        /// <param name="parameters">Script parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Script execution result</returns>
        Task<ConfidentialScriptResult> ExecuteScriptAsync(
            string script,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores data securely within the session context
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="key">Storage key</param>
        /// <param name="data">Data to store</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Storage result</returns>
        Task<ConfidentialStorageResult> StoreDataAsync<T>(
            string key,
            T data,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves data from session storage
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="key">Storage key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Retrieved data</returns>
        Task<ConfidentialRetrievalResult<T>> RetrieveDataAsync<T>(
            string key,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates an attestation report for the current session
        /// </summary>
        /// <param name="challenge">Optional challenge for the attestation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Attestation report</returns>
        Task<AttestationProof> GenerateAttestationAsync(
            string? challenge = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets session metrics and usage statistics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Session metrics</returns>
        Task<SessionMetrics> GetMetricsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Extends the session timeout
        /// </summary>
        /// <param name="additionalTime">Additional time to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>New session expiry time</returns>
        Task<DateTime> ExtendSessionAsync(
            TimeSpan additionalTime,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Information about an enclave session
    /// </summary>
    public class EnclaveSessionInfo
    {
        /// <summary>
        /// Session identifier
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Session name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Session status
        /// </summary>
        public SessionStatus Status { get; set; } = SessionStatus.Unknown;

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last activity timestamp
        /// </summary>
        public DateTime LastActivityAt { get; set; }

        /// <summary>
        /// Session expiry time
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Memory usage in bytes
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// Number of operations performed
        /// </summary>
        public long OperationCount { get; set; }

        /// <summary>
        /// Security level of the session
        /// </summary>
        public SecurityLevel SecurityLevel { get; set; }

        /// <summary>
        /// Whether the session has active attestation
        /// </summary>
        public bool HasValidAttestation { get; set; }
    }

    /// <summary>
    /// Result of session termination
    /// </summary>
    public class SessionTerminationResult
    {
        /// <summary>
        /// Whether termination was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Session identifier that was terminated
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Error message if termination failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Final session statistics
        /// </summary>
        public SessionMetrics? FinalMetrics { get; set; }

        /// <summary>
        /// Cleanup operations performed
        /// </summary>
        public List<string> CleanupActions { get; set; } = new();
    }

    /// <summary>
    /// Session manager statistics
    /// </summary>
    public class SessionManagerStatistics
    {
        /// <summary>
        /// Total number of active sessions
        /// </summary>
        public int ActiveSessionCount { get; set; }

        /// <summary>
        /// Total sessions created since startup
        /// </summary>
        public long TotalSessionsCreated { get; set; }

        /// <summary>
        /// Total sessions terminated since startup
        /// </summary>
        public long TotalSessionsTerminated { get; set; }

        /// <summary>
        /// Total memory usage across all sessions
        /// </summary>
        public long TotalMemoryUsageBytes { get; set; }

        /// <summary>
        /// Average session lifetime
        /// </summary>
        public TimeSpan AverageSessionLifetime { get; set; }

        /// <summary>
        /// Peak concurrent sessions
        /// </summary>
        public int PeakConcurrentSessions { get; set; }

        /// <summary>
        /// Service uptime
        /// </summary>
        public TimeSpan Uptime { get; set; }

        /// <summary>
        /// Statistics collection timestamp
        /// </summary>
        public DateTime CollectedAt { get; set; }
    }

    /// <summary>
    /// Metrics for an individual session
    /// </summary>
    public class SessionMetrics
    {
        /// <summary>
        /// Session identifier
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Number of computations executed
        /// </summary>
        public long ComputationCount { get; set; }

        /// <summary>
        /// Number of script executions
        /// </summary>
        public long ScriptExecutionCount { get; set; }

        /// <summary>
        /// Number of storage operations
        /// </summary>
        public long StorageOperationCount { get; set; }

        /// <summary>
        /// Total computation time
        /// </summary>
        public TimeSpan TotalComputationTime { get; set; }

        /// <summary>
        /// Current memory usage in bytes
        /// </summary>
        public long CurrentMemoryUsageBytes { get; set; }

        /// <summary>
        /// Peak memory usage in bytes
        /// </summary>
        public long PeakMemoryUsageBytes { get; set; }

        /// <summary>
        /// Number of attestation requests
        /// </summary>
        public long AttestationCount { get; set; }

        /// <summary>
        /// Last error encountered (if any)
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// Error count since session creation
        /// </summary>
        public long ErrorCount { get; set; }

        /// <summary>
        /// Metrics collection timestamp
        /// </summary>
        public DateTime CollectedAt { get; set; }
    }

    /// <summary>
    /// Session status enumeration
    /// </summary>
    public enum SessionStatus
    {
        /// <summary>
        /// Session is being initialized
        /// </summary>
        Initializing,

        /// <summary>
        /// Session is active and ready
        /// </summary>
        Active,

        /// <summary>
        /// Session is idle (no recent activity)
        /// </summary>
        Idle,

        /// <summary>
        /// Session is being terminated
        /// </summary>
        Terminating,

        /// <summary>
        /// Session has been terminated
        /// </summary>
        Terminated,

        /// <summary>
        /// Session has encountered an error
        /// </summary>
        Error,

        /// <summary>
        /// Session status is unknown
        /// </summary>
        Unknown
    }
}