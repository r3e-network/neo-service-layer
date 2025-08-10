using System.Numerics;

namespace NeoServiceLayer.Core.SmartContracts;

/// <summary>
/// Represents the result of a smart contract deployment.
/// </summary>
public class ContractDeploymentResult
{
    /// <summary>
    /// Gets or sets the deployed contract hash.
    /// </summary>
    public required string ContractHash { get; set; }

    /// <summary>
    /// Gets or sets the transaction hash of the deployment.
    /// </summary>
    public required string TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets the block number where the contract was deployed.
    /// </summary>
    public long BlockNumber { get; set; }

    /// <summary>
    /// Gets or sets the gas consumed for deployment.
    /// </summary>
    public long GasConsumed { get; set; }

    /// <summary>
    /// Gets or sets whether the deployment was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets any error message if deployment failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the deployed contract manifest.
    /// </summary>
    public string? ContractManifest { get; set; }
}

/// <summary>
/// Represents the result of a smart contract invocation.
/// </summary>
public class ContractInvocationResult
{
    /// <summary>
    /// Gets or sets the transaction hash of the invocation.
    /// </summary>
    public required string TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets the block number where the invocation was included.
    /// </summary>
    public long BlockNumber { get; set; }

    /// <summary>
    /// Gets or sets the gas consumed for the invocation.
    /// </summary>
    public long GasConsumed { get; set; }

    /// <summary>
    /// Gets or sets the return value from the contract method.
    /// </summary>
    public object? ReturnValue { get; set; }

    /// <summary>
    /// Gets or sets whether the invocation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets any error message if invocation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the contract execution state.
    /// </summary>
    public string? ExecutionState { get; set; }

    /// <summary>
    /// Gets or sets the events emitted by the contract.
    /// </summary>
    public List<ContractEvent> Events { get; set; } = new();
}

/// <summary>
/// Represents a contract event.
/// </summary>
public class ContractEvent
{
    /// <summary>
    /// Gets or sets the event name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the contract hash that emitted the event.
    /// </summary>
    public required string ContractHash { get; set; }

    /// <summary>
    /// Gets or sets the event parameters.
    /// </summary>
    public List<object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the block number where the event was emitted.
    /// </summary>
    public long BlockNumber { get; set; }

    /// <summary>
    /// Gets or sets the transaction hash that triggered the event.
    /// </summary>
    public required string TransactionHash { get; set; }
}

/// <summary>
/// Represents contract metadata information.
/// </summary>
public class ContractMetadata
{
    /// <summary>
    /// Gets or sets the contract hash.
    /// </summary>
    public required string ContractHash { get; set; }

    /// <summary>
    /// Gets or sets the contract name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the contract version.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the contract author.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the contract description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the contract manifest.
    /// </summary>
    public string? Manifest { get; set; }

    /// <summary>
    /// Gets or sets the contract ABI.
    /// </summary>
    public string? Abi { get; set; }

    /// <summary>
    /// Gets or sets the block number where the contract was deployed.
    /// </summary>
    public long DeployedBlockNumber { get; set; }

    /// <summary>
    /// Gets or sets the deployment transaction hash.
    /// </summary>
    public required string DeploymentTxHash { get; set; }

    /// <summary>
    /// Gets or sets when the contract was deployed.
    /// </summary>
    public DateTime DeployedAt { get; set; }

    /// <summary>
    /// Gets or sets the contract methods.
    /// </summary>
    public List<ContractMethod> Methods { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the contract is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Represents a contract method.
/// </summary>
public class ContractMethod
{
    /// <summary>
    /// Gets or sets the method name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the method parameters.
    /// </summary>
    public List<ContractParameter> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the return type.
    /// </summary>
    public string? ReturnType { get; set; }

    /// <summary>
    /// Gets or sets whether the method is safe (read-only).
    /// </summary>
    public bool IsSafe { get; set; }

    /// <summary>
    /// Gets or sets whether the method is payable.
    /// </summary>
    public bool IsPayable { get; set; }
}

/// <summary>
/// Represents a contract parameter.
/// </summary>
public class ContractParameter
{
    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the parameter type.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the parameter value.
    /// </summary>
    public object? Value { get; set; }
}

/// <summary>
/// Options for contract deployment.
/// </summary>
public class ContractDeploymentOptions
{
    /// <summary>
    /// Gets or sets the contract name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the contract version.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the contract author.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the contract description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the gas limit for deployment.
    /// </summary>
    public long? GasLimit { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to wait for transaction confirmation.
    /// </summary>
    public bool WaitForConfirmation { get; set; } = true;
}

/// <summary>
/// Options for contract invocation.
/// </summary>
public class ContractInvocationOptions
{
    /// <summary>
    /// Gets or sets the gas limit for the invocation.
    /// </summary>
    public long? GasLimit { get; set; }

    /// <summary>
    /// Gets or sets the value to send with the invocation (for payable methods).
    /// </summary>
    public BigInteger? Value { get; set; }

    /// <summary>
    /// Gets or sets whether to wait for transaction confirmation.
    /// </summary>
    public bool WaitForConfirmation { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of confirmations to wait for.
    /// </summary>
    public int ConfirmationCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the timeout for waiting for confirmation.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Interface for managing smart contracts across different blockchain networks.
/// </summary>
public interface ISmartContractManager
{
    /// <summary>
    /// Gets the blockchain type this manager supports.
    /// </summary>
    BlockchainType BlockchainType { get; }

    /// <summary>
    /// Deploys a smart contract to the blockchain.
    /// </summary>
    /// <param name="contractCode">The compiled contract bytecode.</param>
    /// <param name="constructorParameters">Parameters for the contract constructor.</param>
    /// <param name="options">Deployment options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deployment result.</returns>
    Task<ContractDeploymentResult> DeployContractAsync(
        byte[] contractCode,
        object[]? constructorParameters = null,
        ContractDeploymentOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a method on a deployed smart contract.
    /// </summary>
    /// <param name="contractHash">The hash of the contract to invoke.</param>
    /// <param name="method">The method name to invoke.</param>
    /// <param name="parameters">The method parameters.</param>
    /// <param name="options">Invocation options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The invocation result.</returns>
    Task<ContractInvocationResult> InvokeContractAsync(
        string contractHash,
        string method,
        object[]? parameters = null,
        ContractInvocationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a read-only method on a smart contract without creating a transaction.
    /// </summary>
    /// <param name="contractHash">The hash of the contract to call.</param>
    /// <param name="method">The method name to call.</param>
    /// <param name="parameters">The method parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The call result.</returns>
    Task<object?> CallContractAsync(
        string contractHash,
        string method,
        object[]? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata information about a deployed contract.
    /// </summary>
    /// <param name="contractHash">The hash of the contract.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The contract metadata.</returns>
    Task<ContractMetadata?> GetContractMetadataAsync(
        string contractHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all contracts deployed by the current account.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of contract metadata.</returns>
    Task<IEnumerable<ContractMetadata>> ListDeployedContractsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events emitted by a contract within a block range.
    /// </summary>
    /// <param name="contractHash">The hash of the contract.</param>
    /// <param name="eventName">The name of the event (optional).</param>
    /// <param name="fromBlock">The starting block number.</param>
    /// <param name="toBlock">The ending block number (optional, defaults to latest).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of contract events.</returns>
    Task<IEnumerable<ContractEvent>> GetContractEventsAsync(
        string contractHash,
        string? eventName = null,
        long? fromBlock = null,
        long? toBlock = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates the gas cost for a contract invocation.
    /// </summary>
    /// <param name="contractHash">The hash of the contract.</param>
    /// <param name="method">The method name to invoke.</param>
    /// <param name="parameters">The method parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The estimated gas cost.</returns>
    Task<long> EstimateGasAsync(
        string contractHash,
        string method,
        object[]? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a contract (if supported by the blockchain).
    /// </summary>
    /// <param name="contractHash">The hash of the contract to update.</param>
    /// <param name="newContractCode">The new contract bytecode.</param>
    /// <param name="options">Update options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The update result.</returns>
    Task<ContractDeploymentResult> UpdateContractAsync(
        string contractHash,
        byte[] newContractCode,
        ContractDeploymentOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Destroys a contract (if supported by the blockchain).
    /// </summary>
    /// <param name="contractHash">The hash of the contract to destroy.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the contract was destroyed successfully.</returns>
    Task<bool> DestroyContractAsync(
        string contractHash,
        CancellationToken cancellationToken = default);
}
