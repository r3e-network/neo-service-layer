using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.SmartContracts.Models;

/// <summary>
/// Request to deploy a smart contract
/// </summary>
public class DeployContractRequest
{
    public string Code { get; set; } = string.Empty;
    public string Manifest { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Result of contract deployment
/// </summary>
public class DeploymentResult
{
    public bool Success { get; set; }
    public string ContractHash { get; set; } = string.Empty;
    public string TransactionHash { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public DateTime DeployedAt { get; set; }
}

/// <summary>
/// Request to invoke a smart contract
/// </summary>
public class InvokeContractRequest
{
    public string ContractHash { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public List<object> Parameters { get; set; } = new();
    public string Signer { get; set; } = string.Empty;
}

/// <summary>
/// Result of contract invocation
/// </summary>
public class InvocationResult
{
    public bool Success { get; set; }
    public object Result { get; set; } = new();
    public string TransactionHash { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public long GasConsumed { get; set; }
}

/// <summary>
/// Result of contract call
/// </summary>
public class ContractCallResult
{
    public bool Success { get; set; }
    public object ReturnValue { get; set; } = new();
    public string Exception { get; set; } = string.Empty;
    public long GasConsumed { get; set; }
    public List<string> Notifications { get; set; } = new();
}

/// <summary>
/// Smart contract information
/// </summary>
public class ContractInfo
{
    public string Hash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime DeployedAt { get; set; }
    public List<string> Methods { get; set; } = new();
}

