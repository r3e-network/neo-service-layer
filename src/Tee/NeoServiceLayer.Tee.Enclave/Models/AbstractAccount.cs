using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Tee.Enclave.Models;

/// <summary>
/// Represents an abstract account for account abstraction features.
/// </summary>
public class AbstractAccount
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    public string AccountId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the account address.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account owner.
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account public key.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account nonce.
    /// </summary>
    public ulong Nonce { get; set; }

    /// <summary>
    /// Gets or sets the account balance.
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Gets or sets the account metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the account permissions.
    /// </summary>
    public List<AccountPermission> Permissions { get; set; } = new();

    /// <summary>
    /// Gets or sets the account recovery settings.
    /// </summary>
    public RecoverySettings Recovery { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last activity timestamp.
    /// </summary>
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents an account permission.
/// </summary>
public class AccountPermission
{
    /// <summary>
    /// Gets or sets the permission type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission target.
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission level.
    /// </summary>
    public PermissionLevel Level { get; set; } = PermissionLevel.Read;

    /// <summary>
    /// Gets or sets the expiration time.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Represents permission levels.
/// </summary>
public enum PermissionLevel
{
    /// <summary>
    /// Read-only access.
    /// </summary>
    Read,

    /// <summary>
    /// Write access.
    /// </summary>
    Write,

    /// <summary>
    /// Execute access.
    /// </summary>
    Execute,

    /// <summary>
    /// Full admin access.
    /// </summary>
    Admin
}

/// <summary>
/// Represents account recovery settings.
/// </summary>
public class RecoverySettings
{
    /// <summary>
    /// Gets or sets the recovery threshold.
    /// </summary>
    public int Threshold { get; set; } = 2;

    /// <summary>
    /// Gets or sets the recovery guardians.
    /// </summary>
    public List<string> Guardians { get; set; } = new();

    /// <summary>
    /// Gets or sets the recovery delay in seconds.
    /// </summary>
    public int RecoveryDelaySeconds { get; set; } = 86400; // 24 hours

    /// <summary>
    /// Gets or sets whether recovery is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}