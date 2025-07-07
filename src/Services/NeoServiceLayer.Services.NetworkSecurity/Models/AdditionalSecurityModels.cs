using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.NetworkSecurity.Models;

/// <summary>
/// Request to create a firewall rule
/// </summary>
public class CreateFirewallRuleRequest
{
    public string RuleName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // ALLOW, DENY, DROP
    public string Protocol { get; set; } = string.Empty; // TCP, UDP, ICMP
    public string SourceIp { get; set; } = string.Empty;
    public string DestinationIp { get; set; } = string.Empty;
    public int? SourcePort { get; set; }
    public int? DestinationPort { get; set; }
    public int Priority { get; set; } = 1;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Request to update a firewall rule
/// </summary>
public class UpdateFirewallRuleRequest
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string SourceIp { get; set; } = string.Empty;
    public string DestinationIp { get; set; } = string.Empty;
    public int? SourcePort { get; set; }
    public int? DestinationPort { get; set; }
    public int Priority { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Result of firewall rule operation
/// </summary>
public class FirewallRuleResult
{
    public bool Success { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> RuleDetails { get; set; } = new();
}

/// <summary>
/// Filter for firewall rules
/// </summary>
public class FirewallRuleFilter
{
    public string? RuleName { get; set; }
    public string? Action { get; set; }
    public string? Protocol { get; set; }
    public bool? IsEnabled { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Paginated response
/// </summary>
public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}

/// <summary>
/// Request to create secure channel
/// </summary>
public class SecureChannelRequest
{
    public string ChannelName { get; set; } = string.Empty;
    public string TargetEndpoint { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public Dictionary<string, object> SecurityParameters { get; set; } = new();
}

/// <summary>
/// Result of secure channel operation
/// </summary>
public class SecureChannelResult
{
    public bool Success { get; set; }
    public string ChannelId { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> ChannelDetails { get; set; } = new();
}

/// <summary>
/// Secure channel information
/// </summary>
public class SecureChannelInfo
{
    public string ChannelId { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TargetEndpoint { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsed { get; set; }
}

/// <summary>
/// Result of security audit
/// </summary>
public class SecurityAuditResult
{
    public string AuditId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalChecks { get; set; }
    public int PassedChecks { get; set; }
    public int FailedChecks { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// Traffic statistics
/// </summary>
public class TrafficStatistics
{
    public long TotalRequests { get; set; }
    public long BlockedRequests { get; set; }
    public long AllowedRequests { get; set; }
    public double BlockRate { get; set; }
    public Dictionary<string, long> RequestsByProtocol { get; set; } = new();
    public Dictionary<string, long> TopSourceIps { get; set; } = new();
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

/// <summary>
/// Security threat information
/// </summary>
public class SecurityThreat
{
    public string ThreatId { get; set; } = string.Empty;
    public string ThreatType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string SourceIp { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public bool IsBlocked { get; set; }
    public Dictionary<string, object> ThreatDetails { get; set; } = new();
}

/// <summary>
/// Filter for security threats
/// </summary>
public class ThreatFilter
{
    public string? ThreatType { get; set; }
    public string? Severity { get; set; }
    public string? SourceIp { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsBlocked { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Request to block IP address
/// </summary>
public class BlockIpRequest
{
    public string IpAddress { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public string BlockType { get; set; } = "TEMPORARY"; // TEMPORARY, PERMANENT
}
