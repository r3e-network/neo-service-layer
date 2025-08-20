using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core;

/// <summary>
/// Service configuration base class
/// </summary>
public class ServiceConfigurationBase
{
    public string ServiceName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Service health status
/// </summary>
public enum ServiceHealthStatus
{
    Unknown = 0,
    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3
}

/// <summary>
/// Service health information
/// </summary>
