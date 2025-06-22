using Microsoft.Extensions.Logging;
using NeoServiceLayer.AI.PatternRecognition.Models;
using NeoServiceLayer.Core;
using CoreModels = NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.AI.PatternRecognition;

/// <summary>
/// Statistical analysis operations for the Pattern Recognition Service.
/// </summary>
public partial class PatternRecognitionService
{
    /// <summary>
    /// Detects anomalies using statistical methods.
    /// </summary>
    private async Task<DetectedAnomaly[]> DetectAnomaliesInEnclaveAsync(Models.AnomalyDetectionRequest request)
    {
        // Perform actual anomaly detection using statistical methods

        await Task.CompletedTask;

        // Apply statistical analysis to the data points
        var statistics = CalculateStatistics(request.DataPoints);
        var threshold = CalculateAnomalyThreshold(statistics);

        // Use statistical methods for anomaly detection
        var anomalies = DetectStatisticalAnomalies(request.DataPoints, statistics, threshold);

        return anomalies.ToArray();
    }

    /// <summary>
    /// Calculates statistical properties of data points.
    /// </summary>
    private DataStatistics CalculateStatistics(double[] dataPoints)
    {
        if (dataPoints.Length == 0)
            return new DataStatistics();

        var mean = dataPoints.Average();
        var variance = dataPoints.Sum(x => Math.Pow(x - mean, 2)) / dataPoints.Length;
        var standardDeviation = Math.Sqrt(variance);
        var median = CalculateMedian(dataPoints);
        var q1 = CalculatePercentile(dataPoints, 25);
        var q3 = CalculatePercentile(dataPoints, 75);
        var iqr = q3 - q1;

        return new DataStatistics
        {
            Mean = mean,
            Median = median,
            StandardDeviation = standardDeviation,
            Variance = variance,
            Q1 = q1,
            Q3 = q3,
            IQR = iqr,
            Min = dataPoints.Min(),
            Max = dataPoints.Max(),
            Count = dataPoints.Length
        };
    }

    /// <summary>
    /// Calculates anomaly detection threshold based on statistics.
    /// </summary>
    private AnomalyThreshold CalculateAnomalyThreshold(DataStatistics stats)
    {
        // Use multiple methods for threshold calculation
        var zScoreThreshold = 2.5; // Standard z-score threshold
        var iqrMultiplier = 1.5; // IQR multiplier for outlier detection

        return new AnomalyThreshold
        {
            ZScoreThreshold = zScoreThreshold,
            IQRLowerBound = stats.Q1 - (iqrMultiplier * stats.IQR),
            IQRUpperBound = stats.Q3 + (iqrMultiplier * stats.IQR),
            MeanPlusStdDev = stats.Mean + (2 * stats.StandardDeviation),
            MeanMinusStdDev = stats.Mean - (2 * stats.StandardDeviation)
        };
    }

    /// <summary>
    /// Detects statistical anomalies in data points.
    /// </summary>
    private List<DetectedAnomaly> DetectStatisticalAnomalies(double[] dataPoints, DataStatistics stats, AnomalyThreshold threshold)
    {
        var anomalies = new List<DetectedAnomaly>();

        for (int i = 0; i < dataPoints.Length; i++)
        {
            var value = dataPoints[i];
            var zScore = Math.Abs((value - stats.Mean) / stats.StandardDeviation);

            // Check for anomalies using multiple methods
            var isZScoreAnomaly = zScore > threshold.ZScoreThreshold;
            var isIQRAnomaly = value < threshold.IQRLowerBound || value > threshold.IQRUpperBound;
            var isStdDevAnomaly = value < threshold.MeanMinusStdDev || value > threshold.MeanPlusStdDev;

            if (isZScoreAnomaly || isIQRAnomaly || isStdDevAnomaly)
            {
                var anomalyType = DetermineAnomalyType(value, stats, threshold);
                var severity = CalculateAnomalySeverity(zScore, value, stats);
                var confidence = CalculateAnomalyConfidence(isZScoreAnomaly, isIQRAnomaly, isStdDevAnomaly);

                anomalies.Add(new DetectedAnomaly
                {
                    DataPointIndex = i,
                    AnomalyType = anomalyType,
                    Severity = severity,
                    Description = $"Statistical anomaly: value {value:F3}, z-score {zScore:F3}",
                    Confidence = confidence
                });
            }
        }

        return anomalies;
    }

    /// <summary>
    /// Calculates the median of a dataset.
    /// </summary>
    private double CalculateMedian(double[] values)
    {
        var sorted = values.OrderBy(x => x).ToArray();
        var mid = sorted.Length / 2;

        if (sorted.Length % 2 == 0)
            return (sorted[mid - 1] + sorted[mid]) / 2.0;
        else
            return sorted[mid];
    }

    /// <summary>
    /// Calculates a percentile of a dataset.
    /// </summary>
    private double CalculatePercentile(double[] values, double percentile)
    {
        var sorted = values.OrderBy(x => x).ToArray();
        var index = (percentile / 100.0) * (sorted.Length - 1);

        if (index == Math.Floor(index))
            return sorted[(int)index];
        else
        {
            var lower = sorted[(int)Math.Floor(index)];
            var upper = sorted[(int)Math.Ceiling(index)];
            var fraction = index - Math.Floor(index);
            return lower + (fraction * (upper - lower));
        }
    }

    /// <summary>
    /// Determines the type of anomaly based on statistical analysis.
    /// </summary>
    private Models.AnomalyType DetermineAnomalyType(double value, DataStatistics stats, AnomalyThreshold threshold)
    {
        if (value > stats.Max * 0.9) return Models.AnomalyType.HighValue;
        if (value < stats.Min * 1.1) return Models.AnomalyType.LowValue;
        if (Math.Abs(value - stats.Mean) > 3 * stats.StandardDeviation) return Models.AnomalyType.Outlier;
        return Models.AnomalyType.Statistical;
    }

    /// <summary>
    /// Calculates the severity of an anomaly.
    /// </summary>
    private double CalculateAnomalySeverity(double zScore, double value, DataStatistics stats)
    {
        // Normalize z-score to 0-1 range for severity
        var normalizedZScore = Math.Min(zScore / 5.0, 1.0); // Cap at z-score of 5

        // Consider distance from median as additional factor
        var medianDistance = Math.Abs(value - stats.Median) / (stats.Max - stats.Min);

        return Math.Min((normalizedZScore + medianDistance) / 2.0, 1.0);
    }

    /// <summary>
    /// Calculates confidence in anomaly detection.
    /// </summary>
    private double CalculateAnomalyConfidence(bool isZScoreAnomaly, bool isIQRAnomaly, bool isStdDevAnomaly)
    {
        var methodCount = (isZScoreAnomaly ? 1 : 0) + (isIQRAnomaly ? 1 : 0) + (isStdDevAnomaly ? 1 : 0);

        return methodCount switch
        {
            3 => 0.95, // All methods agree
            2 => 0.80, // Two methods agree
            1 => 0.65, // One method detects
            _ => 0.50  // Fallback
        };
    }

    /// <summary>
    /// Statistical properties of a dataset.
    /// </summary>
    private class DataStatistics
    {
        public double Mean { get; set; }
        public double Median { get; set; }
        public double StandardDeviation { get; set; }
        public double Variance { get; set; }
        public double Q1 { get; set; }
        public double Q3 { get; set; }
        public double IQR { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// Thresholds for anomaly detection.
    /// </summary>
    private class AnomalyThreshold
    {
        public double ZScoreThreshold { get; set; }
        public double IQRLowerBound { get; set; }
        public double IQRUpperBound { get; set; }
        public double MeanPlusStdDev { get; set; }
        public double MeanMinusStdDev { get; set; }
    }
}

/// <summary>
/// Extension methods for statistical calculations.
/// </summary>
public static class StatisticalExtensions
{
    /// <summary>
    /// Calculates the standard deviation of a collection of TimeSpan values.
    /// </summary>
    public static double StandardDeviation(this IEnumerable<TimeSpan> values)
    {
        var timeSpans = values.ToArray();
        if (timeSpans.Length <= 1) return 0;

        var mean = timeSpans.Average(t => t.TotalHours);
        var variance = timeSpans.Sum(t => Math.Pow(t.TotalHours - mean, 2)) / (timeSpans.Length - 1);
        return Math.Sqrt(variance);
    }
}
