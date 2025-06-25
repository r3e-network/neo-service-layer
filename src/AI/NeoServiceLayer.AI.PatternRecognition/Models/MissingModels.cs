using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.AI.PatternRecognition.Models;


/// <summary>
/// Represents helper methods for holiday checking.
/// </summary>
public static class HolidayHelper
{
    /// <summary>
    /// Checks if a date is a holiday.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>True if the date is a holiday.</returns>
    public static bool IsHoliday(DateTime date)
    {
        // Simple implementation - check for common holidays
        var month = date.Month;
        var day = date.Day;

        // New Year's Day
        if (month == 1 && day == 1) return true;

        // Christmas Day
        if (month == 12 && day == 25) return true;

        // Independence Day (US)
        if (month == 7 && day == 4) return true;

        // Add more holidays as needed
        return false;
    }
}

/// <summary>
/// Represents analysis result types.
/// </summary>
public enum AnalysisResultType
{
    /// <summary>
    /// Pattern analysis result.
    /// </summary>
    Pattern,

    /// <summary>
    /// Anomaly detection result.
    /// </summary>
    Anomaly,

    /// <summary>
    /// Behavior analysis result.
    /// </summary>
    Behavior,

    /// <summary>
    /// Risk assessment result.
    /// </summary>
    Risk,

    /// <summary>
    /// Classification result.
    /// </summary>
    Classification,

    /// <summary>
    /// Fraud detection result.
    /// </summary>
    Fraud
}

/// <summary>
/// Represents model performance status.
/// </summary>
public enum ModelPerformanceStatus
{
    /// <summary>
    /// Excellent performance.
    /// </summary>
    Excellent,

    /// <summary>
    /// Good performance.
    /// </summary>
    Good,

    /// <summary>
    /// Fair performance.
    /// </summary>
    Fair,

    /// <summary>
    /// Poor performance.
    /// </summary>
    Poor,

    /// <summary>
    /// Needs retraining.
    /// </summary>
    NeedsRetraining
}

/// <summary>
/// Extension methods for fraud detection operations.
/// </summary>
public static class FraudDetectionExtensions
{
    /// <summary>
    /// Calculates fraud score from risk factors.
    /// </summary>
    /// <param name="riskFactors">The risk factors.</param>
    /// <returns>The fraud score.</returns>
    public static double CalculateFraudScore(this Dictionary<string, double> riskFactors)
    {
        if (riskFactors.Count == 0) return 0.0;

        return Math.Min(1.0, riskFactors.Values.Average());
    }

    /// <summary>
    /// Determines if result indicates fraud.
    /// </summary>
    /// <param name="fraudScore">The fraud score.</param>
    /// <param name="threshold">The threshold for fraud.</param>
    /// <returns>True if fraudulent.</returns>
    public static bool IsFraudulent(this double fraudScore, double threshold = 0.7)
    {
        return fraudScore > threshold;
    }
}

/// <summary>
/// Helper methods for anomaly detection.
/// </summary>
public static class AnomalyDetectionHelper
{
    /// <summary>
    /// Converts Core.AnomalyDetectionRequest to AI version.
    /// </summary>
    /// <param name="coreRequest">The core request.</param>
    /// <returns>AI-specific request.</returns>
    public static AnomalyDetectionRequest ConvertFromCore(Core.Models.AnomalyDetectionRequest coreRequest)
    {
        // Extract data points from the dictionary data
        var dataPoints = new List<double>();
        var featureNames = new List<string>();

        foreach (var kvp in coreRequest.Data)
        {
            if (double.TryParse(kvp.Value?.ToString(), out var value))
            {
                dataPoints.Add(value);
                featureNames.Add(kvp.Key);
            }
        }

        return new AnomalyDetectionRequest
        {
            DataPoints = dataPoints.ToArray(),
            FeatureNames = featureNames.ToArray(),
            Parameters = coreRequest.Parameters,
            Metadata = new Dictionary<string, object>
            {
                ["threshold"] = coreRequest.Threshold,
                ["window_size"] = coreRequest.WindowSize
            }
        };
    }
}

/// <summary>
/// Helper methods for fraud detection requests.
/// </summary>
public static class FraudDetectionHelper
{
    /// <summary>
    /// Converts Core.FraudDetectionRequest to AI version.
    /// </summary>
    /// <param name="coreRequest">The core request.</param>
    /// <returns>AI-specific request.</returns>
    public static FraudDetectionRequest ConvertFromCore(Core.Models.FraudDetectionRequest coreRequest)
    {
        var aiRequest = new FraudDetectionRequest
        {
            TransactionId = coreRequest.TransactionId,
            TransactionData = coreRequest.TransactionData,
            Parameters = coreRequest.Parameters,
            Threshold = coreRequest.Threshold,
            Metadata = new Dictionary<string, object>
            {
                ["sensitivity"] = coreRequest.Sensitivity.ToString(),
                ["include_historical"] = coreRequest.IncludeHistoricalAnalysis
            }
        };

        // Extract specific fields from transaction data
        if (coreRequest.TransactionData.TryGetValue("amount", out var amountObj)
            && decimal.TryParse(amountObj?.ToString(), out var amount))
        {
            aiRequest.TransactionAmount = amount;
        }

        if (coreRequest.TransactionData.TryGetValue("sender_address", out var senderObj))
        {
            aiRequest.SenderAddress = senderObj?.ToString() ?? string.Empty;
        }

        if (coreRequest.TransactionData.TryGetValue("recipient_address", out var recipientObj))
        {
            aiRequest.RecipientAddress = recipientObj?.ToString() ?? string.Empty;
        }

        if (coreRequest.TransactionData.TryGetValue("timestamp", out var timestampObj)
            && DateTime.TryParse(timestampObj?.ToString(), out var timestamp))
        {
            aiRequest.TransactionTime = timestamp;
        }

        // Extract feature flags from transaction data
        if (coreRequest.TransactionData.TryGetValue("is_new_address", out var isNewAddressObj)
            && bool.TryParse(isNewAddressObj?.ToString(), out var isNewAddress))
        {
            aiRequest.IsNewAddress = isNewAddress;
        }

        if (coreRequest.TransactionData.TryGetValue("high_frequency", out var highFrequencyObj)
            && bool.TryParse(highFrequencyObj?.ToString(), out var highFrequency))
        {
            aiRequest.HighFrequency = highFrequency;
        }

        if (coreRequest.TransactionData.TryGetValue("unusual_time_pattern", out var unusualTimeObj)
            && bool.TryParse(unusualTimeObj?.ToString(), out var unusualTime))
        {
            aiRequest.UnusualTimePattern = unusualTime;
        }

        if (coreRequest.TransactionData.TryGetValue("transaction_count", out var transactionCountObj)
            && int.TryParse(transactionCountObj?.ToString(), out var transactionCount))
        {
            aiRequest.TransactionCount = transactionCount;
        }

        if (coreRequest.TransactionData.TryGetValue("time_window", out var timeWindowObj)
            && timeWindowObj is TimeSpan timeWindow)
        {
            aiRequest.TimeWindow = timeWindow;
        }

        return aiRequest;
    }

    /// <summary>
    /// Converts AI FraudDetectionRequest to Core version.
    /// </summary>
    /// <param name="aiRequest">The AI request.</param>
    /// <returns>Core-specific request.</returns>
    public static Core.Models.FraudDetectionRequest ConvertToCore(FraudDetectionRequest aiRequest)
    {
        var coreRequest = new Core.Models.FraudDetectionRequest
        {
            TransactionId = aiRequest.TransactionId,
            TransactionData = new Dictionary<string, object>(aiRequest.TransactionData),
            Parameters = aiRequest.Parameters,
            Threshold = aiRequest.Threshold
        };

        // Ensure transaction data includes all AI-specific fields
        coreRequest.TransactionData["amount"] = aiRequest.TransactionAmount;
        coreRequest.TransactionData["sender_address"] = aiRequest.SenderAddress;
        coreRequest.TransactionData["recipient_address"] = aiRequest.RecipientAddress;
        coreRequest.TransactionData["timestamp"] = aiRequest.TransactionTime;

        return coreRequest;
    }
}
