using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.AbstractAccount.Models;

/// <summary>
/// User operation request for EIP-4337 compliance
/// </summary>
public class UserOperationRequest
{
    public string Sender { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string InitCode { get; set; } = string.Empty;
    public string CallData { get; set; } = string.Empty;
    public string CallGasLimit { get; set; } = string.Empty;
    public string VerificationGasLimit { get; set; } = string.Empty;
    public string PreVerificationGas { get; set; } = string.Empty;
    public string MaxFeePerGas { get; set; } = string.Empty;
    public string MaxPriorityFeePerGas { get; set; } = string.Empty;
    public string PaymasterAndData { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

/// <summary>
/// User operation result
/// </summary>
public class UserOperationResult
{
    public bool Success { get; set; }
    public string UserOpHash { get; set; } = string.Empty;
    public string TransactionHash { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public long GasUsed { get; set; }
    public DateTime ExecutedAt { get; set; }
}

/// <summary>
/// Account creation result
/// </summary>
public class AccountCreationResult
{
    public bool Success { get; set; }
    public string AccountAddress { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string TransactionHash { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Abstract account info
/// </summary>
public class AbstractAccount
{
    public string AccountId { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public AccountStatus Status { get; set; }
    public decimal Balance { get; set; }
    public List<GuardianInfo> Guardians { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// User operation data
/// </summary>
public class UserOperation
{
    public string Hash { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string CallData { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Gas estimation result
/// </summary>
public class GasEstimation
{
    public long CallGasLimit { get; set; }
    public long VerificationGasLimit { get; set; }
    public long PreVerificationGas { get; set; }
    public string MaxFeePerGas { get; set; } = string.Empty;
    public string MaxPriorityFeePerGas { get; set; } = string.Empty;
    public long TotalGas { get; set; }
    public decimal EstimatedCost { get; set; }
}
