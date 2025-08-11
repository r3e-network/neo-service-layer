using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Services.SocialRecovery.Configuration;

/// <summary>
/// Configuration options for the Social Recovery Service.
/// </summary>
public class SocialRecoveryOptions
{
    /// <summary>
    /// Maximum number of guardians allowed per account.
    /// </summary>
    [Range(1, 100)]
    public int MaxGuardiansPerAccount { get; set; } = 20;

    /// <summary>
    /// Minimum recovery threshold (number of guardian confirmations required).
    /// </summary>
    [Range(1, 10)]
    public int MinRecoveryThreshold { get; set; } = 2;

    /// <summary>
    /// Maximum recovery threshold allowed.
    /// </summary>
    [Range(1, 20)]
    public int MaxRecoveryThreshold { get; set; } = 10;

    /// <summary>
    /// Default recovery timeout period.
    /// </summary>
    public TimeSpan RecoveryTimeout { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Emergency recovery timeout (faster for urgent situations).
    /// </summary>
    public TimeSpan EmergencyRecoveryTimeout { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Minimum reputation score required for guardians.
    /// </summary>
    [Range(0, 10000)]
    public int MinGuardianReputation { get; set; } = 1000;

    /// <summary>
    /// Minimum stake amount required for guardian enrollment (in smallest unit).
    /// </summary>
    public long MinGuardianStake { get; set; } = 1000_00000000; // 1000 GAS

    /// <summary>
    /// Maximum time allowed for guardian confirmation.
    /// </summary>
    public TimeSpan GuardianConfirmationTimeout { get; set; } = TimeSpan.FromHours(72);

    /// <summary>
    /// Allowed recovery strategies.
    /// </summary>
    public string[] AllowedRecoveryStrategies { get; set; } = { "social-recovery", "emergency-recovery", "multi-factor-recovery" };

    /// <summary>
    /// Whether to allow network guardians (untrusted guardians from the network).
    /// </summary>
    public bool AllowNetworkGuardians { get; set; } = true;

    /// <summary>
    /// Whether to require attestation for recovery operations.
    /// </summary>
    public bool RequireAttestation { get; set; } = true;

    /// <summary>
    /// Reputation penalty for failed guardian actions.
    /// </summary>
    [Range(1, 5000)]
    public int ReputationPenalty { get; set; } = 1000;

    /// <summary>
    /// Reputation reward for successful guardian actions.
    /// </summary>
    [Range(1, 1000)]
    public int ReputationReward { get; set; } = 100;

    /// <summary>
    /// Whether to enable persistent storage.
    /// </summary>
    public bool EnablePersistentStorage { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent recovery operations.
    /// </summary>
    [Range(1, 1000)]
    public int MaxConcurrentRecoveries { get; set; } = 100;

    /// <summary>
    /// Blockchain-specific contract addresses.
    /// </summary>
    public Dictionary<string, string> ContractAddresses { get; set; } = new()
    {
        ["neo-n3"] = "0x0000000000000000000000000000000000000000",
        ["neo-x"] = "0x0000000000000000000000000000000000000000"
    };
}

/// <summary>
/// Account recovery configuration stored per account.
/// </summary>
public class AccountRecoveryConfig
{
    /// <summary>
    /// Account address this configuration applies to.
    /// </summary>
    public string AccountAddress { get; set; } = string.Empty;

    /// <summary>
    /// Preferred recovery strategy ID.
    /// </summary>
    public string PreferredStrategy { get; set; } = "social-recovery";

    /// <summary>
    /// Recovery threshold (number of confirmations required).
    /// </summary>
    public int RecoveryThreshold { get; set; } = 3;

    /// <summary>
    /// Whether to allow network guardians.
    /// </summary>
    public bool AllowNetworkGuardians { get; set; } = true;

    /// <summary>
    /// Minimum guardian reputation required.
    /// </summary>
    public int MinGuardianReputation { get; set; } = 5000;

    /// <summary>
    /// List of trusted guardian addresses.
    /// </summary>
    public List<string> TrustedGuardians { get; set; } = new();

    /// <summary>
    /// Custom recovery timeout for this account.
    /// </summary>
    public TimeSpan? CustomTimeout { get; set; }

    /// <summary>
    /// Whether emergency recovery is enabled.
    /// </summary>
    public bool EmergencyRecoveryEnabled { get; set; } = false;

    /// <summary>
    /// Configuration creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Configuration last modified timestamp.
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this configuration is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}