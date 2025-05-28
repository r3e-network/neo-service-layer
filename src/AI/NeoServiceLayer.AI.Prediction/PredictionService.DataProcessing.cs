using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.AI.Prediction.Models;

namespace NeoServiceLayer.AI.Prediction;

/// <summary>
/// Data processing operations for the Prediction Service.
/// </summary>
public partial class PredictionService
{
    /// <summary>
    /// Analyzes input data quality for confidence calculation.
    /// </summary>
    private async Task<DataQualityMetrics> AnalyzeInputDataQualityAsync(Dictionary<string, object> inputData)
    {
        // Perform actual data quality analysis using statistical methods
        await Task.CompletedTask; // Ensure async

        var metrics = new DataQualityMetrics();

        // Check data completeness
        var totalFields = inputData.Count;
        var nonNullFields = inputData.Values.Count(v => v != null);
        metrics.Completeness = totalFields > 0 ? (double)nonNullFields / totalFields : 0.0;

        // Check data consistency
        var numericFields = inputData.Values.OfType<double>().Count() +
                           inputData.Values.OfType<int>().Count() +
                           inputData.Values.OfType<decimal>().Count();
        metrics.Consistency = totalFields > 0 ? (double)numericFields / totalFields : 0.0;

        // Check for outliers in numeric data
        var numericValues = ExtractNumericValues(inputData);
        metrics.OutlierRatio = CalculateOutlierRatio(numericValues);

        // Calculate overall quality score
        metrics.OverallQuality = (metrics.Completeness + metrics.Consistency + (1.0 - metrics.OutlierRatio)) / 3.0;

        return metrics;
    }

    /// <summary>
    /// Extracts numeric values from input data dictionary.
    /// </summary>
    private double[] ExtractNumericValues(Dictionary<string, object> inputData)
    {
        var values = new List<double>();

        foreach (var kvp in inputData)
        {
            switch (kvp.Value)
            {
                case double d:
                    values.Add(d);
                    break;
                case int i:
                    values.Add(i);
                    break;
                case decimal dec:
                    values.Add((double)dec);
                    break;
                case float f:
                    values.Add(f);
                    break;
                case string s when double.TryParse(s, out var parsed):
                    values.Add(parsed);
                    break;
            }
        }

        return values.ToArray();
    }

    /// <summary>
    /// Calculates the ratio of outliers in numeric data.
    /// </summary>
    private double CalculateOutlierRatio(double[] values)
    {
        if (values.Length < 3) return 0.0;

        var mean = values.Average();
        var stdDev = Math.Sqrt(values.Sum(x => Math.Pow(x - mean, 2)) / values.Length);

        if (stdDev == 0) return 0.0;

        var outliers = values.Count(x => Math.Abs(x - mean) > 2 * stdDev);
        return (double)outliers / values.Length;
    }

    /// <summary>
    /// Calculates prediction variance for uncertainty estimation.
    /// </summary>
    private double CalculatePredictionVariance(Dictionary<string, object> prediction)
    {
        var numericValues = ExtractNumericValues(prediction);
        if (numericValues.Length < 2) return 0.0;

        var mean = numericValues.Average();
        return numericValues.Sum(x => Math.Pow(x - mean, 2)) / numericValues.Length;
    }

    /// <summary>
    /// Calculates model complexity factor for uncertainty adjustment.
    /// </summary>
    private double CalculateComplexityFactor(PredictionModel model)
    {
        // Simple complexity estimation based on model characteristics
        var baseComplexity = model.PredictionType switch
        {
            Models.PredictionType.Price => 0.3,
            Models.PredictionType.Volatility => 0.4,
            Models.PredictionType.MarketTrend => 0.2,
            Models.PredictionType.Sentiment => 0.3,
            _ => 0.25
        };

        // Adjust based on model confidence threshold (lower threshold = higher complexity penalty)
        var accuracyAdjustment = (1.0 - model.MinConfidenceThreshold) * 0.2;

        return Math.Min(1.0, baseComplexity + accuracyAdjustment);
    }

    /// <summary>
    /// Normalizes features to [0, 1] range.
    /// </summary>
    private double[] NormalizeFeatures(double[] features)
    {
        if (features.Length == 0) return features;

        var min = features.Min();
        var max = features.Max();
        var range = max - min;

        if (range == 0) return features.Select(_ => 0.5).ToArray();

        return features.Select(f => (f - min) / range).ToArray();
    }

    /// <summary>
    /// Extracts fraud detection features from input data.
    /// </summary>
    private double[] ExtractFraudDetectionFeatures(Dictionary<string, object> inputData)
    {
        var features = new List<double>();

        // Transaction amount (normalized)
        if (inputData.TryGetValue("amount", out var amountObj) && double.TryParse(amountObj.ToString(), out var amount))
        {
            features.Add(Math.Log10(Math.Max(1, amount)) / 6.0); // Log scale, normalized to ~[0,1]
        }
        else
        {
            features.Add(0.5); // Default value
        }

        // Transaction frequency
        if (inputData.TryGetValue("frequency", out var freqObj) && double.TryParse(freqObj.ToString(), out var frequency))
        {
            features.Add(Math.Min(1.0, frequency / 100.0)); // Normalize to [0,1]
        }
        else
        {
            features.Add(0.1); // Default low frequency
        }

        // Time pattern (hour of day)
        if (inputData.TryGetValue("hour", out var hourObj) && int.TryParse(hourObj.ToString(), out var hour))
        {
            features.Add(hour / 24.0); // Normalize to [0,1]
        }
        else
        {
            features.Add(0.5); // Default mid-day
        }

        // Address age (days since first seen)
        if (inputData.TryGetValue("address_age", out var ageObj) && double.TryParse(ageObj.ToString(), out var age))
        {
            features.Add(Math.Min(1.0, age / 365.0)); // Normalize to [0,1] for up to 1 year
        }
        else
        {
            features.Add(0.0); // Default new address
        }

        // Geographic risk score
        if (inputData.TryGetValue("geo_risk", out var geoObj) && double.TryParse(geoObj.ToString(), out var geoRisk))
        {
            features.Add(Math.Min(1.0, Math.Max(0.0, geoRisk)));
        }
        else
        {
            features.Add(0.2); // Default low risk
        }

        return features.ToArray();
    }

    /// <summary>
    /// Extracts behavior analysis features from input data.
    /// </summary>
    private double[] ExtractBehaviorAnalysisFeatures(Dictionary<string, object> inputData)
    {
        var features = new List<double>();

        // Transaction count in period
        if (inputData.TryGetValue("tx_count", out var countObj) && double.TryParse(countObj.ToString(), out var txCount))
        {
            features.Add(Math.Min(1.0, txCount / 1000.0)); // Normalize to [0,1]
        }
        else
        {
            features.Add(0.1);
        }

        // Average transaction value
        if (inputData.TryGetValue("avg_value", out var avgObj) && double.TryParse(avgObj.ToString(), out var avgValue))
        {
            features.Add(Math.Log10(Math.Max(1, avgValue)) / 6.0);
        }
        else
        {
            features.Add(0.3);
        }

        // Unique counterparties
        if (inputData.TryGetValue("unique_counterparties", out var uniqueObj) && double.TryParse(uniqueObj.ToString(), out var unique))
        {
            features.Add(Math.Min(1.0, unique / 100.0));
        }
        else
        {
            features.Add(0.2);
        }

        // Time variance (regularity of transactions)
        if (inputData.TryGetValue("time_variance", out var varianceObj) && double.TryParse(varianceObj.ToString(), out var variance))
        {
            features.Add(Math.Min(1.0, variance / 24.0)); // Hours variance normalized
        }
        else
        {
            features.Add(0.5);
        }

        // Weekend activity ratio
        if (inputData.TryGetValue("weekend_ratio", out var weekendObj) && double.TryParse(weekendObj.ToString(), out var weekendRatio))
        {
            features.Add(Math.Min(1.0, Math.Max(0.0, weekendRatio)));
        }
        else
        {
            features.Add(0.3);
        }

        return features.ToArray();
    }

    /// <summary>
    /// Extracts anomaly detection features from input data.
    /// </summary>
    private double[] ExtractAnomalyDetectionFeatures(Dictionary<string, object> inputData)
    {
        var features = new List<double>();

        // Statistical features
        if (inputData.TryGetValue("mean", out var meanObj) && double.TryParse(meanObj.ToString(), out var mean))
        {
            features.Add(mean);
        }
        else
        {
            features.Add(0.0);
        }

        if (inputData.TryGetValue("std_dev", out var stdObj) && double.TryParse(stdObj.ToString(), out var stdDev))
        {
            features.Add(stdDev);
        }
        else
        {
            features.Add(1.0);
        }

        if (inputData.TryGetValue("skewness", out var skewObj) && double.TryParse(skewObj.ToString(), out var skewness))
        {
            features.Add(skewness);
        }
        else
        {
            features.Add(0.0);
        }

        if (inputData.TryGetValue("kurtosis", out var kurtObj) && double.TryParse(kurtObj.ToString(), out var kurtosis))
        {
            features.Add(kurtosis);
        }
        else
        {
            features.Add(3.0); // Normal distribution kurtosis
        }

        // Temporal features
        if (inputData.TryGetValue("trend", out var trendObj) && double.TryParse(trendObj.ToString(), out var trend))
        {
            features.Add(trend);
        }
        else
        {
            features.Add(0.0);
        }

        return features.ToArray();
    }

    /// <summary>
    /// Extracts generic features from input data when specific extraction is not available.
    /// </summary>
    private double[] ExtractGenericFeatures(Dictionary<string, object> inputData)
    {
        var numericValues = ExtractNumericValues(inputData);

        if (numericValues.Length == 0)
        {
            return new double[] { 0.5, 0.5, 0.5 }; // Default neutral features
        }

        // Use basic statistical features
        var mean = numericValues.Average();
        var stdDev = numericValues.Length > 1 ?
            Math.Sqrt(numericValues.Sum(x => Math.Pow(x - mean, 2)) / (numericValues.Length - 1)) : 0;
        var range = numericValues.Max() - numericValues.Min();

        return new double[] { mean, stdDev, range };
    }
}

/// <summary>
/// Data quality metrics for input validation.
/// </summary>
public class DataQualityMetrics
{
    public double Completeness { get; set; }
    public double Consistency { get; set; }
    public double OutlierRatio { get; set; }
    public double OverallQuality { get; set; }
}
