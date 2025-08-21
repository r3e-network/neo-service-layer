using System;
using System.Collections.Generic;
using NeoServiceLayer.Core.Domain;

namespace NeoServiceLayer.Core.ConfidentialComputing
{
    /// <summary>
    /// Represents a confidential computation that can be executed within SGX
    /// </summary>
    /// <typeparam name="TInput">Input data type</typeparam>
    /// <typeparam name="TOutput">Output data type</typeparam>
    public class ConfidentialComputation<TInput, TOutput>
    {
        /// <summary>
        /// Unique identifier for the computation
        /// </summary>
        public string ComputationId { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable name for the computation
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of what this computation does
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// JavaScript template to execute (from PrivacyComputingJavaScriptTemplates)
        /// </summary>
        public string ScriptTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Additional parameters for the computation
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// Security requirements for the computation
        /// </summary>
        public ConfidentialSecurityRequirements SecurityRequirements { get; set; } = new();

        /// <summary>
        /// Resource limits for the computation
        /// </summary>
        public ComputationResourceLimits ResourceLimits { get; set; } = new();

        /// <summary>
        /// Timeout for computation execution
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Result of a confidential computation
    /// </summary>
    /// <typeparam name="TOutput">Output data type</typeparam>
    public class ConfidentialComputationResult<TOutput>
    {
        /// <summary>
        /// Whether the computation succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The computation result data
        /// </summary>
        public TOutput? Result { get; set; }

        /// <summary>
        /// Error message if computation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Execution metrics
        /// </summary>
        public ComputationMetrics Metrics { get; set; } = new();

        /// <summary>
        /// Attestation proving the computation was executed in SGX
        /// </summary>
        public AttestationProof? Attestation { get; set; }

        /// <summary>
        /// Additional metadata about the computation
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of JavaScript execution within SGX enclave
    /// </summary>
    public class ConfidentialScriptResult
    {
        /// <summary>
        /// Whether the script executed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Script output as JSON string
        /// </summary>
        public string? Output { get; set; }

        /// <summary>
        /// Error message if script failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Memory used by the script execution
        /// </summary>
        public long MemoryUsedBytes { get; set; }

        /// <summary>
        /// Attestation proving script was executed in SGX
        /// </summary>
        public AttestationProof? Attestation { get; set; }
    }

    /// <summary>
    /// Configuration options for a confidential session
    /// </summary>
    public class ConfidentialSessionOptions
    {
        /// <summary>
        /// Session name for identification
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Session timeout
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Maximum memory allocation for the session
        /// </summary>
        public long MaxMemoryBytes { get; set; } = 100 * 1024 * 1024; // 100MB

        /// <summary>
        /// Security requirements for the session
        /// </summary>
        public ConfidentialSecurityRequirements SecurityRequirements { get; set; } = new();

        /// <summary>
        /// Whether to enable persistent storage within the session
        /// </summary>
        public bool EnablePersistentStorage { get; set; } = true;

        /// <summary>
        /// Additional session metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Security requirements for confidential operations
    /// </summary>
    public class ConfidentialSecurityRequirements
    {
        /// <summary>
        /// Minimum required security level
        /// </summary>
        public SecurityLevel MinimumSecurityLevel { get; set; } = SecurityLevel.High;

        /// <summary>
        /// Whether remote attestation is required
        /// </summary>
        public bool RequireRemoteAttestation { get; set; } = true;

        /// <summary>
        /// Required enclave measurement (MRENCLAVE)
        /// </summary>
        public string? RequiredEnclaveHash { get; set; }

        /// <summary>
        /// Required signer measurement (MRSIGNER)
        /// </summary>
        public string? RequiredSignerHash { get; set; }

        /// <summary>
        /// Minimum allowed enclave security version number
        /// </summary>
        public uint MinimumSecurityVersion { get; set; } = 1;

        /// <summary>
        /// Whether debug mode is allowed
        /// </summary>
        public bool AllowDebugMode { get; set; } = false;
    }

    /// <summary>
    /// Resource limits for computations
    /// </summary>
    public class ComputationResourceLimits
    {
        /// <summary>
        /// Maximum memory usage in bytes
        /// </summary>
        public long MaxMemoryBytes { get; set; } = 50 * 1024 * 1024; // 50MB

        /// <summary>
        /// Maximum execution time
        /// </summary>
        public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Maximum number of CPU cycles (0 = no limit)
        /// </summary>
        public long MaxCpuCycles { get; set; } = 0;

        /// <summary>
        /// Maximum number of file operations
        /// </summary>
        public int MaxFileOperations { get; set; } = 100;

        /// <summary>
        /// Maximum network requests allowed
        /// </summary>
        public int MaxNetworkRequests { get; set; } = 10;
    }

    /// <summary>
    /// Metrics collected during computation execution
    /// </summary>
    public class ComputationMetrics
    {
        /// <summary>
        /// Execution start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Execution end time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Total execution duration
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Peak memory usage during execution
        /// </summary>
        public long PeakMemoryBytes { get; set; }

        /// <summary>
        /// Number of CPU cycles used
        /// </summary>
        public long CpuCycles { get; set; }

        /// <summary>
        /// Number of file operations performed
        /// </summary>
        public int FileOperations { get; set; }

        /// <summary>
        /// Number of network requests made
        /// </summary>
        public int NetworkRequests { get; set; }

        /// <summary>
        /// Gas cost estimate for blockchain operations
        /// </summary>
        public long GasEstimate { get; set; }
    }

    /// <summary>
    /// Result of encryption operation
    /// </summary>
    public class ConfidentialEncryptionResult
    {
        /// <summary>
        /// Whether encryption succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Encrypted data
        /// </summary>
        public byte[] EncryptedData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Initialization vector used
        /// </summary>
        public byte[] InitializationVector { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Authentication tag for integrity verification
        /// </summary>
        public byte[] AuthenticationTag { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Key identifier used for encryption
        /// </summary>
        public string KeyId { get; set; } = string.Empty;

        /// <summary>
        /// Encryption algorithm used
        /// </summary>
        public string Algorithm { get; set; } = "AES-256-GCM";

        /// <summary>
        /// Error message if encryption failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// SGX attestation report
    /// </summary>
    public class AttestationReport
    {
        /// <summary>
        /// Attestation report version
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Sign type (linkable or unlinkable)
        /// </summary>
        public int SignType { get; set; }

        /// <summary>
        /// Enclave measurement (MRENCLAVE)
        /// </summary>
        public string EnclaveHash { get; set; } = string.Empty;

        /// <summary>
        /// Signer measurement (MRSIGNER)
        /// </summary>
        public string SignerHash { get; set; } = string.Empty;

        /// <summary>
        /// Security version number
        /// </summary>
        public uint SecurityVersion { get; set; }

        /// <summary>
        /// Platform instance ID
        /// </summary>
        public string PlatformInstanceId { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is a debug enclave
        /// </summary>
        public bool IsDebugEnclave { get; set; }

        /// <summary>
        /// Quote signature
        /// </summary>
        public byte[] Signature { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Raw attestation data
        /// </summary>
        public byte[] RawData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Timestamp when attestation was generated
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Proof that an operation was executed within SGX
    /// </summary>
    public class AttestationProof
    {
        /// <summary>
        /// Unique proof identifier
        /// </summary>
        public string ProofId { get; set; } = string.Empty;

        /// <summary>
        /// Hash of the operation input
        /// </summary>
        public string InputHash { get; set; } = string.Empty;

        /// <summary>
        /// Hash of the operation output
        /// </summary>
        public string OutputHash { get; set; } = string.Empty;

        /// <summary>
        /// SGX attestation report
        /// </summary>
        public AttestationReport AttestationReport { get; set; } = new();

        /// <summary>
        /// Cryptographic signature of the proof
        /// </summary>
        public byte[] Signature { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// When the proof was generated
        /// </summary>
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Health status of the confidential computing service
    /// </summary>
    public class ConfidentialComputingHealth
    {
        /// <summary>
        /// Overall health status
        /// </summary>
        public HealthStatus Status { get; set; } = HealthStatus.Unknown;

        /// <summary>
        /// Whether SGX hardware is available
        /// </summary>
        public bool SgxHardwareAvailable { get; set; }

        /// <summary>
        /// Whether enclave is initialized
        /// </summary>
        public bool EnclaveInitialized { get; set; }

        /// <summary>
        /// Current enclave mode (Hardware/Simulation)
        /// </summary>
        public string EnclaveMode { get; set; } = "Unknown";

        /// <summary>
        /// Number of active sessions
        /// </summary>
        public int ActiveSessions { get; set; }

        /// <summary>
        /// Current memory usage percentage
        /// </summary>
        public double MemoryUsagePercent { get; set; }

        /// <summary>
        /// Service uptime
        /// </summary>
        public TimeSpan Uptime { get; set; }

        /// <summary>
        /// Last attestation time
        /// </summary>
        public DateTime? LastAttestationTime { get; set; }

        /// <summary>
        /// Health check timestamp
        /// </summary>
        public DateTime CheckedAt { get; set; }

        /// <summary>
        /// Additional health details
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = new();
    }

    /// <summary>
    /// Security levels for confidential operations
    /// </summary>
    public enum SecurityLevel
    {
        /// <summary>
        /// Low security - simulation mode acceptable
        /// </summary>
        Low = 1,

        /// <summary>
        /// Medium security - hardware mode preferred
        /// </summary>
        Medium = 2,

        /// <summary>
        /// High security - hardware mode required
        /// </summary>
        High = 3,

        /// <summary>
        /// Maximum security - hardware mode with attestation required
        /// </summary>
        Maximum = 4
    }

    /// <summary>
    /// Health status enumeration
    /// </summary>
    public enum HealthStatus
    {
        /// <summary>
        /// Service is healthy and fully operational
        /// </summary>
        Healthy,

        /// <summary>
        /// Service has minor issues but is operational
        /// </summary>
        Degraded,

        /// <summary>
        /// Service is not operational
        /// </summary>
        Unhealthy,

        /// <summary>
        /// Health status is unknown
        /// </summary>
        Unknown
    }
}