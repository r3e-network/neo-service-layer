namespace NeoServiceLayer.AI.PatternRecognition.Models;

/// <summary>
/// Represents a known fraud pattern.
/// </summary>
public class FraudPattern
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AmountRange? AmountRange { get; set; }
    public TimePattern? TimePattern { get; set; }
    internal FrequencyPattern? FrequencyPattern { get; set; }
    public TimePattern[] TimePatterns { get; set; } = Array.Empty<TimePattern>();
    public AddressPattern[] AddressPatterns { get; set; } = Array.Empty<AddressPattern>();
    public string[] KnownAddresses { get; set; } = Array.Empty<string>();
    public double Severity { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents an amount range for pattern matching.
/// </summary>
public class AmountRange
{
    public decimal MinAmount { get; set; }
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

    public bool Contains(decimal amount)
    {
        return amount >= MinAmount && amount <= MaxAmount;
    }
}

/// <summary>
/// Represents a fraud-specific time pattern.
/// </summary>
public class FraudTimePattern
{
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    public DayOfWeek[] DaysOfWeek { get; set; } = Array.Empty<DayOfWeek>();
    public bool IsHolidayPattern { get; set; }

    /// <summary>
    /// Gets or sets the suspicious hours.
    /// </summary>
    public int[] SuspiciousHours { get; set; } = Array.Empty<int>();

    public bool Matches(DateTime dateTime)
    {
        var hour = dateTime.Hour;
        var dayOfWeek = dateTime.DayOfWeek;

        var hourMatches = hour >= StartHour && hour <= EndHour;
        var dayMatches = DaysOfWeek.Length == 0 || DaysOfWeek.Contains(dayOfWeek);

        return hourMatches && dayMatches;
    }
}

/// <summary>
/// Represents an address pattern for fraud detection.
/// </summary>
public class AddressPattern
{
    public string Pattern { get; set; } = string.Empty;
    public PatternType Type { get; set; }
    public bool IsBlacklisted { get; set; }

    public bool Matches(string address)
    {
        return Type switch
        {
            PatternType.Exact => address.Equals(Pattern, StringComparison.OrdinalIgnoreCase),
            PatternType.Prefix => address.StartsWith(Pattern, StringComparison.OrdinalIgnoreCase),
            PatternType.Suffix => address.EndsWith(Pattern, StringComparison.OrdinalIgnoreCase),
            PatternType.Contains => address.Contains(Pattern, StringComparison.OrdinalIgnoreCase),
            PatternType.Regex => System.Text.RegularExpressions.Regex.IsMatch(address, Pattern),
            _ => false
        };
    }
}

/// <summary>
/// Pattern matching types.
/// </summary>
public enum PatternType
{
    Exact,
    Prefix,
    Suffix,
    Contains,
    Regex
}



