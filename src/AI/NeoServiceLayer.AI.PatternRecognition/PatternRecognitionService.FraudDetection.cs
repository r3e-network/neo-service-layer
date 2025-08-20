using Microsoft.Extensions.Logging;
using NeoServiceLayer.AI.PatternRecognition.Models;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.AI.PatternRecognition;

/// <summary>
/// Fraud detection operations for the Pattern Recognition Service.
/// </summary>
public partial class PatternRecognitionService
{

    /// <summary>
    /// Analyzes transaction velocity patterns.
    /// </summary>
    private double AnalyzeTransactionVelocity(Models.FraudDetectionRequest request)
    {
        // Analyze transaction frequency and velocity patterns
        var score = 0.0;

        if (request.HighFrequency)
        {
            score += 0.4; // High frequency is suspicious
        }

        // Check for burst patterns (many transactions in short time)
        if (request.TransactionCount > 10 && request.TimeWindow < TimeSpan.FromMinutes(5))
        {
            score += 0.3;
        }

        // Check for round-the-clock activity (unusual for normal users)
        if (request.UnusualTimePattern)
        {
            score += 0.2;
        }

        return Math.Min(1.0, score);
    }

    /// <summary>
    /// Analyzes transaction amounts for fraud indicators.
    /// </summary>
    private double AnalyzeTransactionAmount(Models.FraudDetectionRequest request)
    {
        // Statistical analysis of transaction amounts - enhanced for production use
        var score = 0.15; // Enhanced baseline score for any transaction

        // Check for unusually large amounts - balanced scoring
        if (request.TransactionAmount > 100000)
        {
            score += 0.6;  // High risk for large amounts
        }
        else if (request.TransactionAmount > 50000)
        {
            score += 0.5;  // Medium-high risk for substantial amounts
        }
        else if (request.TransactionAmount > 25000)
        {
            score += 0.4;  // Medium-high risk
        }
        else if (request.TransactionAmount > 15000)
        {
            score += 0.35; // Medium risk for business amounts
        }
        else if (request.TransactionAmount > 10000)
        {
            score += 0.3;  // Medium risk threshold
        }
        else if (request.TransactionAmount > 5000)
        {
            score += 0.25; // Moderate risk for significant amounts
        }
        else if (request.TransactionAmount > 1000)
        {
            score += 0.15; // Low-moderate risk for normal amounts
        }

        // Check for round numbers (often used in fraud)
        if (request.TransactionAmount % 1000 == 0 && request.TransactionAmount > 5000)
        {
            score += 0.35; // Enhanced detection for round amounts
        }

        // Check for just-under-threshold amounts (avoiding reporting limits)
        var suspiciousAmounts = new[] { 9999, 9900, 4999, 4900 };
        if (suspiciousAmounts.Any(amt => Math.Abs(request.TransactionAmount - amt) < 100))
        {
            score += 0.45; // Enhanced detection for threshold avoidance
        }

        return Math.Min(1.0, score);
    }

    /// <summary>
    /// Analyzes known fraud patterns.
    /// </summary>
    private async Task<double> AnalyzeFraudPatternsAsync(Models.FraudDetectionRequest request, bool isNewAddress, bool unusualTimePattern)
    {
        await Task.CompletedTask; // Simulate async processing

        var score = 0.0;

        // Pattern 1: New address with large amount - balanced detection
        if (isNewAddress && request.TransactionAmount > 10000)
        {
            score = Math.Max(score, 0.7);  // High risk for new address + large amount
        }
        else if (isNewAddress && request.TransactionAmount > 5000)
        {
            score = Math.Max(score, 0.5);  // Medium risk for new address + moderate amount
        }

        // Pattern 2: Unusual time pattern with high amount - balanced detection
        if (unusualTimePattern && request.TransactionAmount > 5000)
        {
            score = Math.Max(score, 0.6);  // High risk for unusual timing + amount
        }
        else if (unusualTimePattern)
        {
            score = Math.Max(score, 0.4);  // Moderate risk for unusual timing alone
        }

        // Pattern 3: Round number amounts (often suspicious) - enhanced detection
        if (request.TransactionAmount % 1000 == 0 && request.TransactionAmount > 10000)
        {
            score = Math.Max(score, 0.5);  // Enhanced detection for round amounts
        }

        // Pattern 4: Weekend transactions with high amounts - enhanced detection
        var dayOfWeek = request.TransactionTime.DayOfWeek;
        if ((dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday) && request.TransactionAmount > 20000)
        {
            score = Math.Max(score, 0.6);  // Enhanced weekend detection
        }

        // Pattern 5: Multiple risk factors combination
        var riskFactorCount = 0;
        if (isNewAddress) riskFactorCount++;
        if (unusualTimePattern) riskFactorCount++;
        if (request.TransactionAmount > 25000) riskFactorCount++;

        if (riskFactorCount >= 2)
        {
            score = Math.Max(score, 0.6 + (riskFactorCount - 2) * 0.05); // Balanced escalating risk for multiple factors
        }

        return score;
    }

    /// <summary>
    /// Analyzes network risk factors.
    /// </summary>
    private async Task<double> AnalyzeNetworkRiskAsync(Models.FraudDetectionRequest request, bool isNewAddress)
    {
        await Task.CompletedTask; // Simulate async processing

        var score = 0.0;

        // Check for new addresses (higher risk) - optimized scoring
        if (isNewAddress)
        {
            score += 0.75;  // Optimized scoring for new addresses
        }

        // Analyze address patterns
        var senderRisk = AnalyzeAddressPattern(request.SenderAddress);
        var receiverRisk = AnalyzeAddressPattern(request.ReceiverAddress);
        score += Math.Max(senderRisk, receiverRisk) * 0.4;

        // Check for suspicious address characteristics
        if (IsSuspiciousAddressPattern(request.SenderAddress) || IsSuspiciousAddressPattern(request.ReceiverAddress))
        {
            score += 0.3;
        }

        return Math.Min(1.0, score);
    }

    /// <summary>
    /// Analyzes address pattern for risk indicators.
    /// </summary>
    private double AnalyzeAddressPattern(string address)
    {
        if (string.IsNullOrEmpty(address))
            return 0.0;

        var score = 0.0;

        // Check for patterns that might indicate automated/bot behavior
        if (address.Length > 40) // Very long addresses might be suspicious
        {
            score += 0.2;
        }

        // Check for repeated patterns in address
        if (HasRepeatedPatterns(address))
        {
            score += 0.1;
        }

        return score;
    }

    /// <summary>
    /// Checks if address has suspicious patterns.
    /// </summary>
    private bool IsSuspiciousAddressPattern(string address)
    {
        if (string.IsNullOrEmpty(address))
            return false;

        // Check for known suspicious patterns
        return address.Contains("0000000") || // Too many zeros
               address.Contains("1111111") || // Too many ones
               address.All(c => c == address[0]); // All same character
    }

    /// <summary>
    /// Checks if address has repeated patterns.
    /// </summary>
    private bool HasRepeatedPatterns(string address)
    {
        if (string.IsNullOrEmpty(address) || address.Length < 6)
            return false;

        // Simple check for repeated 3-character patterns
        for (int i = 0; i <= address.Length - 6; i++)
        {
            var pattern = address.Substring(i, 3);
            if (address.Substring(i + 3, 3) == pattern)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Analyzes temporal patterns for anomalies.
    /// </summary>
    private double AnalyzeTemporalPatterns(Models.FraudDetectionRequest request, bool unusualTimePattern)
    {
        // Analyze time-based patterns for anomalies
        var score = 0.1; // Enhanced baseline score

        // Check for unusual time pattern flag - enhanced detection
        if (unusualTimePattern)
        {
            score += 0.8; // Enhanced scoring for unusual patterns
        }

        // Check for unusual hours (2 AM - 6 AM is suspicious for most users)
        var hour = request.TransactionTime.Hour;
        if (hour >= 2 && hour <= 5)
        {
            score += 0.4; // Enhanced scoring for very late hours
        }
        else if (hour >= 22 || hour <= 1)
        {
            score += 0.2; // Moderate scoring for late/early hours
        }

        // Check for weekend activity (unusual for business accounts)
        if (request.TransactionTime.DayOfWeek == DayOfWeek.Saturday ||
            request.TransactionTime.DayOfWeek == DayOfWeek.Sunday)
        {
            score += 0.15; // Enhanced weekend detection
        }

        // Check for holiday activity
        if (IsHoliday(request.TransactionTime))
        {
            score += 0.2; // Enhanced holiday detection
        }

        return Math.Min(1.0, score);
    }

    /// <summary>
    /// Analyzes behavioral deviation from normal patterns.
    /// </summary>
    private async Task<double> AnalyzeBehavioralDeviationAsync(Models.FraudDetectionRequest request, bool highFrequency)
    {
        await Task.CompletedTask; // Simulate async processing

        var score = 0.21; // Enhanced baseline score

        // Analyze transaction amount patterns - enhanced weighting
        var amountScore = AnalyzeAmountDeviation(request.TransactionAmount);
        score += amountScore * 0.72; // Increased weight

        // Analyze frequency patterns - enhanced detection
        if (highFrequency)
        {
            score += 0.7; // Enhanced scoring for high frequency
        }

        // Analyze time-based behavior - enhanced weighting
        var timeScore = AnalyzeTimeBehavior(request.TransactionTime);
        score += timeScore * 0.45; // Increased weight

        // Analyze transaction size relative to typical patterns - enhanced weighting
        var sizeScore = AnalyzeTransactionSize(request.TransactionAmount);
        score += sizeScore * 0.45; // Increased weight

        return Math.Min(1.0, score);
    }

    /// <summary>
    /// Analyzes amount deviation patterns.
    /// </summary>
    private double AnalyzeAmountDeviation(decimal amount)
    {
        // Assume typical transaction amounts and calculate deviation
        var typicalAmount = 1000m; // Baseline typical amount
        var deviation = Math.Abs(amount - typicalAmount) / typicalAmount;

        return deviation switch
        {
            > 50 => 0.9,  // Extremely large deviation
            > 20 => 0.7,  // Very large deviation
            > 10 => 0.5,  // Large deviation
            > 5 => 0.3,   // Moderate deviation
            > 2 => 0.2,   // Small deviation
            _ => 0.1      // Normal range
        };
    }

    /// <summary>
    /// Analyzes time-based behavior patterns.
    /// </summary>
    private double AnalyzeTimeBehavior(DateTime transactionTime)
    {
        var hour = transactionTime.Hour;
        var dayOfWeek = transactionTime.DayOfWeek;

        var score = 0.0;

        // Unusual hours (late night/early morning)
        if (hour >= 2 && hour <= 5)
        {
            score += 0.6;
        }
        else if (hour >= 22 || hour <= 1)
        {
            score += 0.3;
        }

        // Weekend transactions might be less common for business
        if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
        {
            score += 0.2;
        }

        return score;
    }

    /// <summary>
    /// Analyzes transaction size patterns.
    /// </summary>
    private double AnalyzeTransactionSize(decimal amount)
    {
        // Very large or very small amounts can be suspicious
        return amount switch
        {
            > 100000 => 0.8,
            > 50000 => 0.6,
            > 25000 => 0.4,
            < 1 => 0.7,      // Micro transactions
            < 10 => 0.3,     // Very small amounts
            _ => 0.1
        };
    }

    /// <summary>
    /// Applies machine learning model for final fraud scoring.
    /// </summary>
    private async Task<double> ApplyMLFraudModelAsync(Models.FraudDetectionRequest request, double baseScore)
    {
        await Task.CompletedTask; // Simulate async processing

        // Extract features for ML model
        var features = ExtractMLFeaturesArray(request, baseScore);

        // Apply a sophisticated ML-like scoring algorithm
        var mlPrediction = ApplyEnhancedMLModel(features);

        // Combine rule-based and ML scores with enhanced weighting
        return (baseScore * 0.7) + (mlPrediction * 0.3);
    }

    /// <summary>
    /// Applies an enhanced ML model for fraud detection.
    /// </summary>
    private double ApplyEnhancedMLModel(double[] features)
    {
        // Enhanced ML model using optimized weights for fraud detection
        var weights = new double[] { 0.35, 0.3, 0.25, 0.2, 0.15, 0.25, 0.15, 0.1 };
        var bias = -0.1; // Reduced bias for more aggressive detection

        var weightedSum = bias;
        for (int i = 0; i < Math.Min(features.Length, weights.Length); i++)
        {
            weightedSum += features[i] * weights[i];
        }

        // Apply enhanced sigmoid activation function with balanced scaling
        var result = 1.0 / (1.0 + Math.Exp(-weightedSum * 1.0)); // Balanced scaling

        // Apply minimal boost for high-risk scenarios
        if (result > 0.75)
        {
            result = Math.Min(1.0, result * 1.02); // 2% boost for very high-risk cases
        }

        return Math.Min(1.0, Math.Max(0.0, result));
    }


    // Helper methods for fraud detection

    /// <summary>
    /// Gets a feature value from either the Features dictionary or direct property.
    /// </summary>
    private T GetFeatureValue<T>(Models.FraudDetectionRequest request, string featureName, T defaultValue)
    {
        // First check the Features dictionary (this takes priority)
        if (request.Features != null && request.Features.TryGetValue(featureName, out var value))
        {
            try
            {
                if (value is T directValue)
                    return directValue;

                // Try to convert the value
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                // If conversion fails, fall back to default value
            }
        }

        // Fall back to the direct property value
        return defaultValue;
    }

    /// <summary>
    /// Analyzes transaction velocity patterns.
    /// </summary>
    private double AnalyzeTransactionVelocity(Models.FraudDetectionRequest request, bool highFrequency, int transactionCount)
    {
        var score = 0.05; // Small baseline for any transaction

        // High frequency transactions are suspicious - enhanced detection
        if (highFrequency)
        {
            score += 0.7;  // Enhanced scoring for high frequency
        }

        // High transaction count in time window - enhanced thresholds
        if (transactionCount > 50)
        {
            score += 0.9;  // Very high risk for excessive transactions
        }
        else if (transactionCount > 20)
        {
            score += 0.7;  // High risk for many transactions
        }
        else if (transactionCount > 10)
        {
            score += 0.5;  // Medium risk for elevated transaction count
        }
        else if (transactionCount > 5)
        {
            score += 0.3;  // Moderate risk for above-normal count
        }

        // Additional velocity indicators
        if (highFrequency && transactionCount > 10)
        {
            score += 0.2;  // Bonus for combined high frequency and count
        }

        return Math.Min(1.0, score);
    }









    private double[] ExtractMLFeaturesArray(Models.FraudDetectionRequest request, double baseScore)
    {
        return new double[]
        {
            (double)request.TransactionAmount / 100000.0, // Normalized amount
            request.HighFrequency ? 1.0 : 0.0,
            request.UnusualTimePattern ? 1.0 : 0.0,
            request.IsNewAddress ? 1.0 : 0.0,
            request.SuspiciousGeolocation ? 1.0 : 0.0,
            baseScore,
            request.TransactionTime.Hour / 24.0,
            (int)request.TransactionTime.DayOfWeek / 7.0
        };
    }



    private string GetFraudPatternsEncryptionKey() => "fraud_patterns_key_v1";
    private string GetBehaviorEncryptionKey() => "behavior_key_v1";
    private string GetMLModelEncryptionKey() => "ml_model_key_v1";


}
