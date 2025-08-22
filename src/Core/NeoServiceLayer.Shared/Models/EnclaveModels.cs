using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Shared.Models;

/// <summary>
/// Sealing policy type for SGX/TEE sealed data.
/// </summary>
public enum SealingPolicyType
{
    /// <summary>
    /// Per-session sealing (data is only accessible within the same enclave session).
    /// </summary>
    PerSession,
    
    /// <summary>
    /// Persistent sealing (data persists across enclave restarts).
    /// </summary>
    Persistent,
    
    /// <summary>
    /// MRSIGNER-based sealing (data is accessible to any enclave signed with the same key).
    /// </summary>
    MrSigner,
    
    /// <summary>
    /// MRENCLAVE-based sealing (data is only accessible to the exact same enclave).
    /// </summary>
    MrEnclave
}

/// <summary>
/// Sealing policy configuration for SGX/TEE sealed data.
/// </summary>
public class SealingPolicy
{
    /// <summary>
    /// Gets or sets the sealing policy type.
    /// </summary>
    public SealingPolicyType Type { get; set; } = SealingPolicyType.MrEnclave;
    
    /// <summary>
    /// Gets or sets the expiration hours for the sealed data.
    /// </summary>
    public int ExpirationHours { get; set; } = 8760; // 1 year default
}