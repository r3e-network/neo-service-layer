using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Tee.Enclave.Models;

/// <summary>
/// Request for attestation
/// </summary>
public class AttestationRequest
{
    public string Quote { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Result of attestation
/// </summary>
public class AttestationResult
{
    public bool Success { get; set; }
    public string AttestationToken { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Dictionary<string, object> Claims { get; set; } = new();
}

/// <summary>
/// Request to verify attestation
/// </summary>
public class AttestationVerificationRequest
{
    public string AttestationToken { get; set; } = string.Empty;
    public string ExpectedMrEnclave { get; set; } = string.Empty;
    public string ExpectedMrSigner { get; set; } = string.Empty;
    public List<string> TrustedIssuers { get; set; } = new();
}

/// <summary>
/// Attestation details
/// </summary>
public class AttestationDetails
{
    public string AttestationId { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public string MrEnclave { get; set; } = string.Empty;
    public string MrSigner { get; set; } = string.Empty;
    public string PlatformInfo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsValid { get; set; }
}

/// <summary>
/// Attestation summary
/// </summary>
public class AttestationSummary
{
    public int TotalAttestations { get; set; }
    public int ValidAttestations { get; set; }
    public int ExpiredAttestations { get; set; }
    public DateTime LastAttestation { get; set; }
}

/// <summary>
/// Platform configuration
/// </summary>
public class PlatformConfiguration
{
    public string SgxVersion { get; set; } = string.Empty;
    public string PlatformSvn { get; set; } = string.Empty;
    public string CpuSvn { get; set; } = string.Empty;
    public List<string> SupportedFeatures { get; set; } = new();
}

/// <summary>
/// Enclave measurements
/// </summary>
public class EnclaveMeasurements
{
    public string MrEnclave { get; set; } = string.Empty;
    public string MrSigner { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string SecurityVersion { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; set; } = new();
}

/// <summary>
/// Enclave policy
/// </summary>
public class EnclavePolicy
{
    public string PolicyId { get; set; } = string.Empty;
    public List<string> AllowedMrEnclaves { get; set; } = new();
    public List<string> AllowedMrSigners { get; set; } = new();
    public int MinSecurityVersion { get; set; }
    public bool RequireDebugDisabled { get; set; } = true;
}

/// <summary>
/// Attestation service health
/// </summary>
public class AttestationServiceHealth
{
    public bool IsHealthy { get; set; }
    public string ServiceVersion { get; set; } = string.Empty;
    public DateTime LastHealthCheck { get; set; }
    public List<string> AvailableFeatures { get; set; } = new();
    public string Status { get; set; } = string.Empty;
}
