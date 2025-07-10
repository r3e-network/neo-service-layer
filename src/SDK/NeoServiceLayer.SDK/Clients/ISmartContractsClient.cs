using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.SmartContracts;

namespace NeoServiceLayer.SDK.Clients;

/// <summary>
/// Client interface for Smart Contracts service
/// </summary>
public interface ISmartContractsClient
{
    /// <summary>
    /// Deploy a new smart contract
    /// </summary>
    Task<ContractDeploymentResult> DeployContractAsync(
        string contractCode,
        Dictionary<string, object> constructorParameters,
        DeploymentOptions options,
        string blockchainType = "neon3",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoke a smart contract method
    /// </summary>
    Task<ContractInvocationResult> InvokeContractAsync(
        string contractHash,
        string method,
        object[] parameters,
        InvocationOptions options,
        string blockchainType = "neon3",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get contract state
    /// </summary>
    Task<ContractState> GetContractStateAsync(
        string contractHash,
        string blockchainType = "neon3",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List deployed contracts
    /// </summary>
    Task<IEnumerable<DeployedContract>> GetDeployedContractsAsync(
        string blockchainType = "all",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate contract code
    /// </summary>
    Task<ContractValidationResult> ValidateContractAsync(
        string contractCode,
        string blockchainType = "neon3",
        CancellationToken cancellationToken = default);
}

// Client models
public class DeploymentOptions
{
    public string NetworkFee { get; set; }
    public string SystemFee { get; set; }
    public string Account { get; set; }
}

public class InvocationOptions
{
    public string NetworkFee { get; set; }
    public string SystemFee { get; set; }
    public string Account { get; set; }
    public bool ReadOnly { get; set; }
}

public class ContractState
{
    public string ContractHash { get; set; }
    public string Blockchain { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
    public IEnumerable<ContractMethod> Methods { get; set; }
    public IEnumerable<string> SupportedStandards { get; set; }
    public ContractUsageStats UsageStats { get; set; }
}

public class ContractMethod
{
    public string Name { get; set; }
    public string[] Parameters { get; set; }
    public string ReturnType { get; set; }
    public int Offset { get; set; }
}

public class ContractUsageStats
{
    public int InvocationCount { get; set; }
    public decimal TotalGasConsumed { get; set; }
    public DateTime LastInvoked { get; set; }
}

public class DeployedContract
{
    public string ContractHash { get; set; }
    public string Blockchain { get; set; }
    public string Name { get; set; }
    public DateTime DeployedAt { get; set; }
    public string DeployedBy { get; set; }
}

public class ContractValidationResult
{
    public bool IsValid { get; set; }
    public string[] Errors { get; set; }
    public string[] Warnings { get; set; }
    public GasEstimate GasEstimate { get; set; }
    public int SecurityScore { get; set; }
    public string Blockchain { get; set; }
}

public class GasEstimate
{
    public string Deployment { get; set; }
    public string Invocation { get; set; }
}

/// <summary>
/// Implementation of Smart Contracts client
/// </summary>
public class SmartContractsClient : BaseServiceClient, ISmartContractsClient
{
    public SmartContractsClient(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory, "neo-service-smart-contracts")
    {
    }

    public async Task<ContractDeploymentResult> DeployContractAsync(
        string contractCode,
        Dictionary<string, object> constructorParameters,
        DeploymentOptions options,
        string blockchainType = "neon3",
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            contractCode,
            constructorParameters,
            options,
            blockchainType
        };

        return await PostAsync<ContractDeploymentResult>("deploy", request, cancellationToken);
    }

    public async Task<ContractInvocationResult> InvokeContractAsync(
        string contractHash,
        string method,
        object[] parameters,
        InvocationOptions options,
        string blockchainType = "neon3",
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            contractHash,
            method,
            parameters,
            options,
            blockchainType
        };

        return await PostAsync<ContractInvocationResult>("invoke", request, cancellationToken);
    }

    public async Task<ContractState> GetContractStateAsync(
        string contractHash,
        string blockchainType = "neon3",
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<ContractState>($"{contractHash}/state?blockchain={blockchainType}", cancellationToken);
    }

    public async Task<IEnumerable<DeployedContract>> GetDeployedContractsAsync(
        string blockchainType = "all",
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<DeployedContract>>($"deployed?blockchain={blockchainType}", cancellationToken);
    }

    public async Task<ContractValidationResult> ValidateContractAsync(
        string contractCode,
        string blockchainType = "neon3",
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            contractCode,
            blockchainType
        };

        return await PostAsync<ContractValidationResult>("validate", request, cancellationToken);
    }
}
