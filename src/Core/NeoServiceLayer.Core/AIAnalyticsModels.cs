// This file contains additional AI/Analytics models
// The main models are defined in AIAnalyticsTypes.cs

using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core;

/// <summary>
/// Represents a detected fraud pattern.
/// </summary>
public class FraudPattern
{
    /// <summary>
    /// Gets or sets the pattern identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the pattern name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the pattern description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the confidence score for this pattern.
    /// </summary>
    public double Confidence { get; set; }
    
    /// <summary>
    /// Gets or sets the severity level.
    /// </summary>
    public string Severity { get; set; } = "Medium";
}

/// <summary>
/// Represents a risk factor identified in pattern recognition.
/// </summary>
public class RiskFactor
{
    /// <summary>
    /// Gets or sets the factor identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the factor name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the risk score.
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public string Level { get; set; } = "Medium";
}