using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.NetworkSecurity.Models;

/// <summary>
/// Request to create a secure channel.
/// </summary>
public class CreateChannelRequest
{
    /// <summary>
    /// Gets or sets the channel name.
    /// </summary>
    public string ChannelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target endpoint.
    /// </summary>
    public string TargetEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the network protocol.
    /// </summary>
    public NetworkProtocol Protocol { get; set; }

    /// <summary>
    /// Gets or sets the authentication configuration.
    /// </summary>
    public AuthenticationConfig? Authentication { get; set; }

    /// <summary>
    /// Gets or sets the encryption policy.
    /// </summary>
    public EncryptionPolicy? EncryptionPolicy { get; set; }
}

/// <summary>
/// Network protocols supported.
/// </summary>
public enum NetworkProtocol
{
    /// <summary>HTTPS protocol.</summary>
    Https,
    /// <summary>WebSocket protocol.</summary>
    WebSocket,
    /// <summary>TCP protocol.</summary>
    Tcp
}

/// <summary>
/// Authentication configuration.
/// </summary>
public class AuthenticationConfig
{
    /// <summary>
    /// Gets or sets the authentication type.
    /// </summary>
    public string Type { get; set; } = "MUTUAL_TLS";

    /// <summary>
    /// Gets or sets the client certificate.
    /// </summary>
    public string? ClientCertificate { get; set; }
}

/// <summary>
/// Encryption policy configuration.
/// </summary>
public class EncryptionPolicy
{
    /// <summary>
    /// Gets or sets the encryption algorithm.
    /// </summary>
    public string Algorithm { get; set; } = "AES-256-GCM";

    /// <summary>
    /// Gets or sets the key rotation hours.
    /// </summary>
    public int KeyRotationHours { get; set; } = 24;
}

/// <summary>
/// Secure channel response.
/// </summary>
public class SecureChannelResponse
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the channel ID.
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the channel status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the channel endpoint.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public key.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration time.
    /// </summary>
    public DateTime ValidUntil { get; set; }
}

/// <summary>
/// Network message to send.
/// </summary>
public class NetworkMessage
{
    /// <summary>
    /// Gets or sets the message payload.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the timeout in milliseconds.
    /// </summary>
    public int Timeout { get; set; } = 5000;
}

/// <summary>
/// Message response.
/// </summary>
public class MessageResponse
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the message ID.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the response payload.
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the latency in milliseconds.
    /// </summary>
    public int Latency { get; set; }

    /// <summary>
    /// Gets or sets whether the message was encrypted.
    /// </summary>
    public bool Encrypted { get; set; }
}

/// <summary>
/// Firewall rule set.
/// </summary>
public class FirewallRuleSet
{
    /// <summary>
    /// Gets or sets the firewall rules.
    /// </summary>
    public List<FirewallRule> Rules { get; set; } = new();

    /// <summary>
    /// Gets or sets the default action.
    /// </summary>
    public FirewallAction DefaultAction { get; set; }
}

/// <summary>
/// Individual firewall rule.
/// </summary>
public class FirewallRule
{
    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule action.
    /// </summary>
    public FirewallAction Action { get; set; }

    /// <summary>
    /// Gets or sets the source address.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination address.
    /// </summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    public string Port { get; set; } = "*";

    /// <summary>
    /// Gets or sets the protocol.
    /// </summary>
    public string Protocol { get; set; } = "*";
}

/// <summary>
/// Firewall actions.
/// </summary>
public enum FirewallAction
{
    /// <summary>Allow traffic.</summary>
    Allow,
    /// <summary>Deny traffic.</summary>
    Deny
}

/// <summary>
/// Firewall configuration result.
/// </summary>
public class FirewallConfigurationResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the number of rules applied.
    /// </summary>
    public int RulesApplied { get; set; }

    /// <summary>
    /// Gets or sets any error messages.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Monitoring request parameters.
/// </summary>
public class MonitoringRequest
{
    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the channel ID filter.
    /// </summary>
    public string? ChannelId { get; set; }
}

/// <summary>
/// Network monitoring data.
/// </summary>
public class NetworkMonitoringData
{
    /// <summary>
    /// Gets or sets the network statistics.
    /// </summary>
    public NetworkStatistics Statistics { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of active channels.
    /// </summary>
    public int ActiveChannels { get; set; }

    /// <summary>
    /// Gets or sets the security events.
    /// </summary>
    public List<SecurityEvent> SecurityEvents { get; set; } = new();
}

/// <summary>
/// Network statistics.
/// </summary>
public class NetworkStatistics
{
    /// <summary>
    /// Gets or sets the total requests.
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Gets or sets the successful requests.
    /// </summary>
    public long SuccessfulRequests { get; set; }

    /// <summary>
    /// Gets or sets the failed requests.
    /// </summary>
    public long FailedRequests { get; set; }

    /// <summary>
    /// Gets or sets the average latency.
    /// </summary>
    public double AverageLatency { get; set; }

    /// <summary>
    /// Gets or sets the bandwidth used in bytes.
    /// </summary>
    public long BandwidthUsed { get; set; }
}

/// <summary>
/// Security event information.
/// </summary>
public class SecurityEvent
{
    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source address.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action taken.
    /// </summary>
    public string Action { get; set; } = string.Empty;
}

/// <summary>
/// Channel status information.
/// </summary>
public class ChannelStatus
{
    /// <summary>
    /// Gets or sets the channel ID.
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the channel is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last activity time.
    /// </summary>
    public DateTime LastActivity { get; set; }

    /// <summary>
    /// Gets or sets the message count.
    /// </summary>
    public long MessageCount { get; set; }

    /// <summary>
    /// Gets or sets the error count.
    /// </summary>
    public long ErrorCount { get; set; }
}