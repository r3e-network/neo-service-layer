using Microsoft.Extensions.Logging;
using NeoServiceLayer.AI.PatternRecognition.Models;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.AI.PatternRecognition;

/// <summary>
/// Behavior analysis operations for the Pattern Recognition Service.
/// </summary>
public partial class PatternRecognitionService
{

    /// <summary>
    /// Analyzes transaction patterns from behavior analysis request.
    /// </summary>
    private TransactionPatterns AnalyzeTransactionPatterns(BehaviorAnalysisRequest request)
    {
        // Analyze transaction frequency patterns
        var hourlyDistribution = new Dictionary<int, int>();
        var dailyDistribution = new Dictionary<DayOfWeek, int>();
        var amounts = new List<decimal>();
        var intervals = new List<TimeSpan>();

        DateTime? lastTransactionTime = null;

        foreach (var transaction in request.TransactionHistory)
        {
            // Extract timestamp from transaction data
            DateTime timestamp = DateTime.UtcNow; // Default value
            if (transaction.TryGetValue("timestamp", out var timestampObj))
            {
                if (timestampObj is DateTime dt)
                    timestamp = dt;
                else if (DateTime.TryParse(timestampObj.ToString(), out var parsedDt))
                    timestamp = parsedDt;
            }

            // Extract value from transaction data
            decimal value = 0;
            if (transaction.TryGetValue("value", out var valueObj) || transaction.TryGetValue("amount", out valueObj))
            {
                if (valueObj is decimal dec)
                    value = dec;
                else if (decimal.TryParse(valueObj.ToString(), out var parsedDec))
                    value = parsedDec;
            }

            // Analyze time patterns
            hourlyDistribution[timestamp.Hour] = hourlyDistribution.GetValueOrDefault(timestamp.Hour) + 1;
            dailyDistribution[timestamp.DayOfWeek] = dailyDistribution.GetValueOrDefault(timestamp.DayOfWeek) + 1;

            // Collect amounts
            amounts.Add(value);

            // Calculate intervals
            if (lastTransactionTime.HasValue)
            {
                intervals.Add(timestamp - lastTransactionTime.Value);
            }
            lastTransactionTime = timestamp;
        }

        return new TransactionPatterns
        {
            HourlyDistribution = hourlyDistribution,
            DailyDistribution = dailyDistribution,
            Amounts = amounts,
            TransactionIntervals = intervals,
            TotalTransactions = request.TransactionHistory.Count,
            AnalysisPeriod = request.AnalysisPeriod.Duration
        };
    }

    /// <summary>
    /// Identifies behavioral characteristics from transaction patterns.
    /// </summary>
    private BehaviorCharacteristics IdentifyBehaviorCharacteristics(TransactionPatterns patterns)
    {
        // Calculate frequency metrics
        var avgTransactionsPerDay = patterns.TotalTransactions / Math.Max(patterns.AnalysisPeriod.TotalDays, 1);
        var avgAmount = patterns.Amounts.Count > 0 ? patterns.Amounts.Average() : 0;
        var amountVariance = patterns.Amounts.Count > 1 ?
            patterns.Amounts.Sum(x => Math.Pow((double)(x - (decimal)avgAmount), 2)) / (patterns.Amounts.Count - 1) : 0;

        // Analyze time patterns
        var peakHours = patterns.HourlyDistribution
            .OrderByDescending(kvp => kvp.Value)
            .Take(3)
            .Select(kvp => kvp.Key)
            .ToArray();

        var weekendActivity = patterns.DailyDistribution.GetValueOrDefault(DayOfWeek.Saturday) +
                             patterns.DailyDistribution.GetValueOrDefault(DayOfWeek.Sunday);
        var weekdayActivity = patterns.TotalTransactions - weekendActivity;
        var weekendRatio = patterns.TotalTransactions > 0 ? (double)weekendActivity / patterns.TotalTransactions : 0;

        // Analyze transaction intervals
        var avgInterval = patterns.TransactionIntervals.Count > 0 ?
            TimeSpan.FromTicks((long)patterns.TransactionIntervals.Average(t => t.Ticks)) : TimeSpan.Zero;

        return new BehaviorCharacteristics
        {
            TransactionFrequency = avgTransactionsPerDay,
            AverageAmount = avgAmount,
            AmountVariance = amountVariance,
            PeakActivityHours = peakHours,
            WeekendActivityRatio = weekendRatio,
            AverageTransactionInterval = avgInterval,
            IsHighFrequencyTrader = avgTransactionsPerDay > 50,
            IsLargeValueTrader = avgAmount > 10000,
            HasRegularPattern = patterns.TransactionIntervals.Count > 0 &&
                               patterns.TransactionIntervals.StandardDeviation() < avgInterval.TotalHours * 0.5
        };
    }

    /// <summary>
    /// Compares behavior characteristics against normal patterns.
    /// </summary>
    private double CompareAgainstNormalPatterns(BehaviorCharacteristics characteristics)
    {
        var normalityScore = 1.0;

        // Check for unusual frequency patterns
        if (characteristics.TransactionFrequency > 100) // Very high frequency
            normalityScore -= 0.2;
        else if (characteristics.TransactionFrequency < 0.1) // Very low frequency
            normalityScore -= 0.1;

        // Check for unusual amount patterns
        if (characteristics.AverageAmount > 100000) // Very large amounts
            normalityScore -= 0.2;
        else if (characteristics.AmountVariance > Math.Pow((double)characteristics.AverageAmount * 2, 2)) // High variance
            normalityScore -= 0.15;

        // Check for unusual time patterns
        if (characteristics.WeekendActivityRatio > 0.8) // Mostly weekend activity
            normalityScore -= 0.1;
        else if (characteristics.WeekendActivityRatio < 0.05) // No weekend activity
            normalityScore -= 0.05;

        // Check for bot-like behavior
        if (characteristics.HasRegularPattern && characteristics.IsHighFrequencyTrader)
            normalityScore -= 0.3; // Potential bot behavior

        return Math.Max(normalityScore, 0.0);
    }

    /// <summary>
    /// Generates a comprehensive behavior profile.
    /// </summary>
    private BehaviorProfile GenerateBehaviorProfile(BehaviorAnalysisRequest request,
        TransactionPatterns patterns, BehaviorCharacteristics characteristics, double normalityScore)
    {
        return new BehaviorProfile
        {
            Address = request.Address,
            TransactionFrequency = (int)characteristics.TransactionFrequency,
            AverageTransactionAmount = (decimal)characteristics.AverageAmount,
            TransactionTimePatterns = CreateTimePatternsList(characteristics),
            AddressInteractions = CreateAddressInteractionsIntDictionary(request),
            UnusualTimePatterns = characteristics.WeekendActivityRatio > 0.7 || characteristics.WeekendActivityRatio < 0.1,
            SuspiciousAddressInteractions = HasSuspiciousInteractions(request),
            AnalyzedPeriod = request.AnalysisPeriod,
            ProfileGeneratedAt = DateTime.UtcNow,
            NormalityScore = normalityScore,
            RiskLevel = DetermineRiskLevel(normalityScore, characteristics)
        };
    }

    /// <summary>
    /// Creates time patterns list from characteristics.
    /// </summary>
    private List<Models.TimePattern> CreateTimePatternsList(BehaviorCharacteristics characteristics)
    {
        var patterns = new List<Models.TimePattern>();

        // Create a peak hours pattern
        if (characteristics.PeakActivityHours.Length > 0)
        {
            patterns.Add(new Models.TimePattern
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Peak Activity Hours",
                PeriodType = Models.TimePeriodType.Hourly,
                ActivityDistribution = characteristics.PeakActivityHours.ToDictionary(h => h.ToString(), h => 1.0),
                PeakActivityTimes = characteristics.PeakActivityHours.Select(h => TimeSpan.FromHours(h)).ToList()
            });
        }

        // Create a weekend activity pattern
        patterns.Add(new Models.TimePattern
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Weekend Activity",
            PeriodType = Models.TimePeriodType.Daily,
            ActivityDistribution = new Dictionary<string, double>
            {
                ["weekend_ratio"] = characteristics.WeekendActivityRatio,
                ["weekday_ratio"] = 1.0 - characteristics.WeekendActivityRatio
            }
        });

        return patterns;
    }

    /// <summary>
    /// Creates address interactions dictionary from request.
    /// </summary>
    private Dictionary<string, int> CreateAddressInteractionsIntDictionary(BehaviorAnalysisRequest request)
    {
        var addressCounts = new Dictionary<string, int>();

        foreach (var transaction in request.TransactionHistory)
        {
            // Extract recipient from transaction data
            string recipient = string.Empty;
            if (transaction.TryGetValue("recipient", out var recipientObj) ||
                transaction.TryGetValue("to", out recipientObj) ||
                transaction.TryGetValue("toAddress", out recipientObj))
            {
                recipient = recipientObj?.ToString() ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(recipient))
            {
                addressCounts[recipient] = addressCounts.GetValueOrDefault(recipient) + 1;
            }
        }

        return addressCounts;
    }

    /// <summary>
    /// Determines if there are suspicious address interactions.
    /// </summary>
    private bool HasSuspiciousInteractions(BehaviorAnalysisRequest request)
    {
        // Simple heuristics for suspicious interactions
        var addressCounts = new Dictionary<string, int>();

        foreach (var transaction in request.TransactionHistory)
        {
            // Extract recipient from transaction data
            string recipient = string.Empty;
            if (transaction.TryGetValue("recipient", out var recipientObj) ||
                transaction.TryGetValue("to", out recipientObj) ||
                transaction.TryGetValue("toAddress", out recipientObj))
            {
                recipient = recipientObj?.ToString() ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(recipient))
            {
                addressCounts[recipient] = addressCounts.GetValueOrDefault(recipient) + 1;
            }
        }

        // Check for potential mixing patterns (many small transactions to different addresses)
        var smallTransactionCount = request.TransactionHistory.Count(t =>
        {
            // Extract value from transaction data
            decimal value = 0;
            if (t.TryGetValue("value", out var valueObj) || t.TryGetValue("amount", out valueObj))
            {
                if (valueObj is decimal dec)
                    value = dec;
                else if (decimal.TryParse(valueObj.ToString(), out var parsedDec))
                    value = parsedDec;
            }
            return value < 1;
        });
        var uniqueRecipients = addressCounts.Count;

        return smallTransactionCount > 20 && uniqueRecipients > 15 &&
               (double)smallTransactionCount / request.TransactionHistory.Count > 0.8;
    }

    /// <summary>
    /// Determines risk level based on normality score and characteristics.
    /// </summary>
    private Models.RiskLevel DetermineRiskLevel(double normalityScore, BehaviorCharacteristics characteristics)
    {
        if (normalityScore < 0.3) return Models.RiskLevel.High;
        if (normalityScore < 0.6) return Models.RiskLevel.Medium;
        if (characteristics.IsHighFrequencyTrader && !characteristics.HasRegularPattern) return Models.RiskLevel.Medium;
        return Models.RiskLevel.Low;
    }

    /// <summary>
    /// Transaction patterns for behavior analysis.
    /// </summary>
    private class TransactionPatterns
    {
        public Dictionary<int, int> HourlyDistribution { get; set; } = new();
        public Dictionary<DayOfWeek, int> DailyDistribution { get; set; } = new();
        public List<decimal> Amounts { get; set; } = new();
        public List<TimeSpan> TransactionIntervals { get; set; } = new();
        public int TotalTransactions { get; set; }
        public TimeSpan AnalysisPeriod { get; set; }
    }

    /// <summary>
    /// Behavioral characteristics derived from transaction patterns.
    /// </summary>
    private class BehaviorCharacteristics
    {
        public double TransactionFrequency { get; set; }
        public decimal AverageAmount { get; set; }
        public double AmountVariance { get; set; }
        public int[] PeakActivityHours { get; set; } = Array.Empty<int>();
        public double WeekendActivityRatio { get; set; }
        public TimeSpan AverageTransactionInterval { get; set; }
        public bool IsHighFrequencyTrader { get; set; }
        public bool IsLargeValueTrader { get; set; }
        public bool HasRegularPattern { get; set; }
    }
}
