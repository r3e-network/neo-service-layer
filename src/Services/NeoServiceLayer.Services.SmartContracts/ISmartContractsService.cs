using NeoServiceLayer.Core;
using NeoServiceLayer.Core.SmartContracts;
using NeoServiceLayer.ServiceFramework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.SmartContracts;

/// <summary>
/// Interface for the unified Smart Contracts service that manages both Neo N3 and Neo X contracts.
/// </summary>
public interface ISmartContractsService : IService
{
    /// <summary>
    /// Gets the smart contract manager for the specified blockchain type.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The smart contract manager for the blockchain.</returns>
    ISmartContractManager GetManager(BlockchainType blockchainType);

    /// <summary>
    /// Deploys a smart contract to the specified blockchain.
    /// </summary>
    /// <param name="blockchainType">The blockchain type to deploy to.</param>
    /// <param name="contractCode">The compiled contract bytecode.</param>
    /// <param name="constructorParameters">Parameters for the contract constructor.</param>
    /// <param name="options">Deployment options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deployment result.</returns>
    Task<ContractDeploymentResult> DeployContractAsync(
        BlockchainType blockchainType,
        byte[] contractCode,
        object[]? constructorParameters = null,
        ContractDeploymentOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a method on a deployed smart contract.
    /// </summary>
    /// <param name="blockchainType">The blockchain type where the contract is deployed.</param>
    /// <param name="contractHash">The hash of the contract to invoke.</param>
    /// <param name="method">The method name to invoke.</param>
    /// <param name="parameters">The method parameters.</param>
    /// <param name="options">Invocation options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The invocation result.</returns>
    Task<ContractInvocationResult> InvokeContractAsync(
        BlockchainType blockchainType,
        string contractHash,
        string method,
        object[]? parameters = null,
        ContractInvocationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a read-only method on a smart contract without creating a transaction.
    /// </summary>
    /// <param name="blockchainType">The blockchain type where the contract is deployed.</param>
    /// <param name="contractHash">The hash of the contract to call.</param>
    /// <param name="method">The method name to call.</param>
    /// <param name="parameters">The method parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The call result.</returns>
    Task<object?> CallContractAsync(
        BlockchainType blockchainType,
        string contractHash,
        string method,
        object[]? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata information about a deployed contract.
    /// </summary>
    /// <param name="blockchainType">The blockchain type where the contract is deployed.</param>
    /// <param name="contractHash">The hash of the contract.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The contract metadata.</returns>
    Task<ContractMetadata?> GetContractMetadataAsync(
        BlockchainType blockchainType,
        string contractHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all contracts deployed by the current account on all supported blockchains.
    /// </summary>
    /// <param name="blockchainType">The blockchain type to list contracts from (null for all).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of contract metadata grouped by blockchain.</returns>
    Task<Dictionary<BlockchainType, IEnumerable<ContractMetadata>>> ListAllDeployedContractsAsync(
        BlockchainType? blockchainType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events emitted by a contract within a block range.
    /// </summary>
    /// <param name="blockchainType">The blockchain type where the contract is deployed.</param>
    /// <param name="contractHash">The hash of the contract.</param>
    /// <param name="eventName">The name of the event (optional).</param>
    /// <param name="fromBlock">The starting block number.</param>
    /// <param name="toBlock">The ending block number (optional, defaults to latest).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of contract events.</returns>
    Task<IEnumerable<NeoServiceLayer.Core.SmartContracts.ContractEvent>> GetContractEventsAsync(
        BlockchainType blockchainType,
        string contractHash,
        string? eventName = null,
        long? fromBlock = null,
        long? toBlock = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates the gas cost for a contract invocation.
    /// </summary>
    /// <param name="blockchainType">The blockchain type where the contract is deployed.</param>
    /// <param name="contractHash">The hash of the contract.</param>
    /// <param name="method">The method name to invoke.</param>
    /// <param name="parameters">The method parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The estimated gas cost.</returns>
    Task<long> EstimateGasAsync(
        BlockchainType blockchainType,
        string contractHash,
        string method,
        object[]? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comprehensive statistics about smart contract usage across all blockchains.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Smart contract usage statistics.</returns>
    Task<SmartContractStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about smart contract usage across all supported blockchains.
/// </summary>
public class SmartContractStatistics
{
    /// <summary>
    /// Gets or sets the total number of contracts deployed across all blockchains.
    /// </summary>
    public int TotalContractsDeployed { get; set; }

    /// <summary>
    /// Gets or sets the total number of contract invocations across all blockchains.
    /// </summary>
    public long TotalInvocations { get; set; }

    /// <summary>
    /// Gets or sets the total gas consumed across all blockchains.
    /// </summary>
    public long TotalGasConsumed { get; set; }

    /// <summary>
    /// Gets or sets statistics broken down by blockchain type.
    /// </summary>
    public Dictionary<BlockchainType, BlockchainContractStats> ByBlockchain { get; set; } = new();

    /// <summary>
    /// Gets or sets the most active contracts (by invocation count).
    /// </summary>
    public List<ContractUsageInfo> MostActiveContracts { get; set; } = new();

    /// <summary>
    /// Gets or sets when these statistics were generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Statistics for a specific blockchain.
/// </summary>
public class BlockchainContractStats
{
    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the number of contracts deployed on this blockchain.
    /// </summary>
    public int ContractsDeployed { get; set; }

    /// <summary>
    /// Gets or sets the number of invocations on this blockchain.
    /// </summary>
    public long Invocations { get; set; }

    /// <summary>
    /// Gets or sets the total gas consumed on this blockchain.
    /// </summary>
    public long GasConsumed { get; set; }

    /// <summary>
    /// Gets or sets the success rate of contract operations.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the average gas consumption per invocation.
    /// </summary>
    public double AverageGasPerInvocation { get; set; }
}

/// <summary>
/// Usage information for a specific contract.
/// </summary>
public class ContractUsageInfo
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
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the number of invocations.
    /// </summary>
    public long InvocationCount { get; set; }

    /// <summary>
    /// Gets or sets the total gas consumed by this contract.
    /// </summary>
    public long TotalGasConsumed { get; set; }

    /// <summary>
    /// Gets or sets the last invocation time.
    /// </summary>
    public DateTime LastInvoked { get; set; }
}
