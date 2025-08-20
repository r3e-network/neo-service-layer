using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoServiceLayer.AI.PatternRecognition.Models;

/// <summary>
/// Represents a known fraud pattern.
/// </summary>
public class FraudPattern
{
    /// <summary>
    /// Gets or sets the pattern ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern type.
    /// </summary>
    public string PatternType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confidence threshold.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the severity score.
    /// </summary>
    public double Severity { get; set; }
    
    /// <summary>
    /// Gets or sets the amount range for this pattern.
    /// </summary>
    public (decimal Min, decimal Max) AmountRange { get; set; }
    
    /// <summary>
    /// Gets or sets the time pattern information.
    /// </summary>
    public Dictionary<string, object> TimePattern { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the frequency pattern for this fraud detection.
    /// </summary>
    public FrequencyPattern? FrequencyPattern { get; set; }
    
    /// <summary>
    /// Gets or sets the address patterns for this fraud detection.
    /// </summary>
    public List<AddressPattern> AddressPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the pattern indicators.
    /// </summary>
    public List<string> Indicators { get; set; } = new();

    /// <summary>
    /// Gets or sets when the pattern was detected.
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the typical amounts for this pattern.
    /// </summary>
    public List<decimal> TypicalAmounts { get; set; } = new();
}

/// <summary>
/// Represents an amount range for pattern matching.
/// </summary>
public class AmountRange
{
    /// <summary>
    /// Gets or sets the minimum amount.
    /// </summary>
    public decimal MinAmount { get; set; }

    /// <summary>
    /// Gets or sets the maximum amount.
    /// </summary>
    public decimal MaxAmount { get; set; }

    /// <summary>
    /// Gets or sets the minimum amount (alias for MinAmount).
    /// </summary>
    public decimal Min
    {
        get => MinAmount;
        set => MinAmount = value;
    }

    /// <summary>
    /// Gets or sets the maximum amount (alias for MaxAmount).
    /// </summary>
    public decimal Max
    {
        get => MaxAmount;
        set => MaxAmount = value;
    }
}

/// <summary>
/// Represents a fraud-specific time pattern.
/// </summary>
public class FraudTimePattern
{
    /// <summary>
    /// Gets or sets the time pattern ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the suspicious hours.
    /// </summary>
    public List<int> SuspiciousHours { get; set; } = new();

    /// <summary>
    /// Gets or sets the time zone.
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Extension methods for working with time patterns stored in dictionaries.
/// </summary>
public static class TimePatternExtensions
{
    /// <summary>
    /// Gets the suspicious hours from a time pattern dictionary.
    /// </summary>
    /// <param name="timePattern">The time pattern dictionary.</param>
    /// <returns>List of suspicious hours, or null if not found.</returns>
    public static List<int>? GetSuspiciousHours(this Dictionary<string, object>? timePattern)
    {
        if (timePattern == null) return null;
        
        if (timePattern.TryGetValue("SuspiciousHours", out var hoursObj))
        {
            return hoursObj switch
            {
                List<int> hours => hours,
                int[] hourArray => hourArray.ToList(),
                IEnumerable<object> objects => objects.OfType<int>().ToList(),
                _ => null
            };
        }
        
        return null;
    }
}

/// <summary>
/// Represents an address pattern for fraud detection.
/// </summary>
public class AddressPattern
{
    /// <summary>
    /// Gets or sets the pattern ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the suspicious addresses.
    /// </summary>
    public List<string> SuspiciousAddresses { get; set; } = new();

    /// <summary>
    /// Gets or sets the pattern type.
    /// </summary>
    public string PatternType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// Checks if an address matches this pattern.
    /// </summary>
    /// <param name="address">The address to check.</param>
    /// <returns>True if the address matches the pattern.</returns>
    public bool Matches(string address)
    {
        return SuspiciousAddresses.Contains(address, StringComparer.OrdinalIgnoreCase) ||
               SuspiciousAddresses.Any(pattern => address.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Pattern matching types.
/// </summary>
public enum PatternMatchType
{
    /// <summary>
    /// Exact match.
    /// </summary>
    Exact,

    /// <summary>
    /// Partial match.
    /// </summary>
    Partial,

    /// <summary>
    /// Fuzzy match.
    /// </summary>
    Fuzzy,

    /// <summary>
    /// Regular expression match.
    /// </summary>
    Regex
}

/// <summary>
/// Represents a frequency pattern for fraud detection.
/// </summary>
public class FrequencyPattern
{
    /// <summary>
    /// Gets or sets the pattern ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the minimum number of transactions.
    /// </summary>
    public int MinTransactions { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of transactions.
    /// </summary>
    public int MaxTransactions { get; set; }
    
    /// <summary>
    /// Gets or sets the time window for the frequency analysis.
    /// </summary>
    public TimeSpan TimeWindow { get; set; } = TimeSpan.FromHours(24);
    
    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

