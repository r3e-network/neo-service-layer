using NeoServiceLayer.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.ComponentModel.DataAnnotations;


namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// Request model for deploying a smart contract.
/// </summary>
public class DeployContractRequest
{
    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    [Required]
    public BlockchainType Blockchain { get; set; }

    /// <summary>
    /// Gets or sets the contract name.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contract script (Base64 encoded).
    /// </summary>
    [Required]
    public string Script { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contract manifest (JSON).
    /// </summary>
    [Required]
    public string Manifest { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the constructor parameters.
    /// </summary>
    public object[]? ConstructorParameters { get; set; }

    /// <summary>
    /// Gets or sets the contract version.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the contract author.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the contract email.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the contract description.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request model for invoking a smart contract method.
/// </summary>
public class InvokeContractRequest
{
    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    [Required]
    public BlockchainType Blockchain { get; set; }

    /// <summary>
    /// Gets or sets the contract hash.
    /// </summary>
    [Required]
    public string ContractHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method name.
    /// </summary>
    [Required]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method parameters.
    /// </summary>
    public object[]? Params { get; set; }
}

/// <summary>
/// Request model for calling a smart contract method (read-only).
/// </summary>
public class CallContractRequest
{
    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    [Required]
    public BlockchainType Blockchain { get; set; }

    /// <summary>
    /// Gets or sets the contract hash.
    /// </summary>
    [Required]
    public string ContractHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method name.
    /// </summary>
    [Required]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method parameters.
    /// </summary>
    public object[]? Params { get; set; }
}

/// <summary>
/// Request model for getting contract events.
/// </summary>
public class GetContractEventsRequest
{
    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    [Required]
    public BlockchainType Blockchain { get; set; }

    /// <summary>
    /// Gets or sets the contract hash.
    /// </summary>
    [Required]
    public string ContractHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event name (optional).
    /// </summary>
    public string? EventName { get; set; }

    /// <summary>
    /// Gets or sets the starting block number.
    /// </summary>
    public long? FromBlock { get; set; }

    /// <summary>
    /// Gets or sets the ending block number.
    /// </summary>
    public long? ToBlock { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of results.
    /// </summary>
    public int MaxResults { get; set; } = 100;
}
