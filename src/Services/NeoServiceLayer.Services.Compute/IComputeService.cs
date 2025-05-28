using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Compute;

/// <summary>
/// Interface for the Compute service.
/// </summary>
public interface IComputeService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Executes a computation.
    /// </summary>
    /// <param name="computationId">The computation ID.</param>
    /// <param name="parameters">The computation parameters.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The computation result.</returns>
    Task<ComputationResult> ExecuteComputationAsync(string computationId, IDictionary<string, string> parameters, BlockchainType blockchainType);

    /// <summary>
    /// Gets the status of a computation.
    /// </summary>
    /// <param name="computationId">The computation ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The computation status.</returns>
    Task<ComputationStatus> GetComputationStatusAsync(string computationId, BlockchainType blockchainType);

    /// <summary>
    /// Registers a computation.
    /// </summary>
    /// <param name="computationId">The computation ID.</param>
    /// <param name="computationCode">The computation code.</param>
    /// <param name="computationType">The computation type (e.g., "JavaScript", "WebAssembly").</param>
    /// <param name="description">The computation description.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the computation was registered successfully, false otherwise.</returns>
    Task<bool> RegisterComputationAsync(string computationId, string computationCode, string computationType, string description, BlockchainType blockchainType);

    /// <summary>
    /// Unregisters a computation.
    /// </summary>
    /// <param name="computationId">The computation ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the computation was unregistered successfully, false otherwise.</returns>
    Task<bool> UnregisterComputationAsync(string computationId, BlockchainType blockchainType);

    /// <summary>
    /// Lists registered computations.
    /// </summary>
    /// <param name="skip">The number of computations to skip.</param>
    /// <param name="take">The number of computations to take.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of computation metadata.</returns>
    Task<IEnumerable<ComputationMetadata>> ListComputationsAsync(int skip, int take, BlockchainType blockchainType);

    /// <summary>
    /// Gets computation metadata.
    /// </summary>
    /// <param name="computationId">The computation ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The computation metadata.</returns>
    Task<ComputationMetadata> GetComputationMetadataAsync(string computationId, BlockchainType blockchainType);

    /// <summary>
    /// Verifies a computation result.
    /// </summary>
    /// <param name="result">The computation result to verify.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the result is valid, false otherwise.</returns>
    Task<bool> VerifyComputationResultAsync(ComputationResult result, BlockchainType blockchainType);
}

/// <summary>
/// Computation metadata.
/// </summary>
public class ComputationMetadata
{
    /// <summary>
    /// Gets or sets the computation ID.
    /// </summary>
    public string ComputationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the computation type.
    /// </summary>
    public string ComputationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the computation description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the computation code.
    /// </summary>
    public string? ComputationCode { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last used date.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets the execution count.
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Gets or sets the average execution time in milliseconds.
    /// </summary>
    public double AverageExecutionTimeMs { get; set; }
}

/// <summary>
/// Computation result.
/// </summary>
public class ComputationResult
{
    /// <summary>
    /// Gets or sets the computation ID.
    /// </summary>
    public string ComputationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the result ID.
    /// </summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the result data.
    /// </summary>
    public string ResultData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution time in milliseconds.
    /// </summary>
    public double ExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the proof.
    /// </summary>
    public string Proof { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameters used for the computation.
    /// </summary>
    public IDictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
}
