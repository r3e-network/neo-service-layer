using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core.Models;

/// <summary>
/// Represents a blockchain contract event.
/// </summary>
public class ContractEvent
{
    public string EventName { get; set; } = string.Empty;
    public string ContractHash { get; set; } = string.Empty;
    public string TransactionHash { get; set; } = string.Empty;
    public uint BlockIndex { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime Timestamp { get; set; }
    
    // Additional properties for compatibility 
    public string BlockHash { get; set; } = string.Empty;
    public long BlockHeight { get; set; }
}