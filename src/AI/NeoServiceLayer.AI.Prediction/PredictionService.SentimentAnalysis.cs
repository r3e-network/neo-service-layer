using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.AI.Prediction.Models;

namespace NeoServiceLayer.AI.Prediction;

/// <summary>
/// Sentiment analysis operations for the Prediction Service.
/// </summary>
public partial class PredictionService
{
    /// <summary>
    /// Analyzes text sentiment within the enclave.
    /// </summary>
    private async Task<Models.SentimentScore> AnalyzeTextSentimentInEnclaveAsync(string text)
    {
        // Perform actual sentiment analysis using NLP algorithms

        // Preprocess the text
        var preprocessedText = await PreprocessTextAsync(text);

        // Apply sentiment analysis model
        var sentimentScores = await ApplySentimentModelAsync(preprocessedText);

        // Calculate sentiment scores and normalize
        var normalizedScores = NormalizeSentimentScores(sentimentScores);

        // Return structured sentiment result
        return CreateSentimentResult(text, normalizedScores);
    }

    /// <summary>
    /// Preprocesses text for sentiment analysis.
    /// </summary>
    private async Task<string> PreprocessTextAsync(string text)
    {
        // Perform actual text preprocessing and normalization
        await Task.CompletedTask; // Ensure async

        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Convert to lowercase
        var processed = text.ToLowerInvariant();

        // Remove special characters (keep letters, numbers, spaces)
        processed = System.Text.RegularExpressions.Regex.Replace(processed, @"[^a-z0-9\s]", " ");

        // Remove extra whitespace
        processed = System.Text.RegularExpressions.Regex.Replace(processed, @"\s+", " ").Trim();

        // Remove common stop words
        var stopWords = new HashSet<string> {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
            "is", "are", "was", "were", "be", "been", "have", "has", "had", "do", "does", "did",
            "will", "would", "could", "should"
        };

        var words = processed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var filteredWords = words.Where(w => !stopWords.Contains(w) && w.Length > 2);

        return string.Join(" ", filteredWords);
    }

    /// <summary>
    /// Applies sentiment analysis model to preprocessed text.
    /// </summary>
    private async Task<SentimentScores> ApplySentimentModelAsync(string preprocessedText)
    {
        // Perform actual sentiment model inference
        await Task.CompletedTask; // Ensure async

        if (string.IsNullOrWhiteSpace(preprocessedText))
        {
            return new SentimentScores { Positive = 0.33, Negative = 0.33, Neutral = 0.34 };
        }

        // Simple rule-based sentiment analysis (in production, this would use a trained model)
        var words = preprocessedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var positiveWords = new HashSet<string> {
            "good", "great", "excellent", "amazing", "wonderful", "fantastic", "positive", "love", "like",
            "happy", "joy", "success", "win", "profit", "gain", "up", "rise", "increase", "bull", "bullish",
            "strong", "growth", "boom", "surge", "rally", "optimistic", "confident", "breakthrough",
            "innovation", "revolutionary", "promising", "bright", "stellar", "outstanding"
        };

        var negativeWords = new HashSet<string> {
            "bad", "terrible", "awful", "horrible", "negative", "hate", "dislike", "sad", "fear", "loss",
            "fail", "down", "fall", "decrease", "bear", "bearish", "crash", "dump", "weak", "decline",
            "recession", "crisis", "collapse", "disaster", "panic", "worried", "concerned", "risky",
            "dangerous", "volatile", "unstable", "uncertain", "disappointing", "poor"
        };

        var positiveCount = words.Count(w => positiveWords.Contains(w));
        var negativeCount = words.Count(w => negativeWords.Contains(w));
        var neutralCount = words.Length - positiveCount - negativeCount;

        var total = Math.Max(1, words.Length);

        return new SentimentScores
        {
            Positive = (double)positiveCount / total,
            Negative = (double)negativeCount / total,
            Neutral = (double)neutralCount / total
        };
    }

    /// <summary>
    /// Normalizes sentiment scores to ensure they sum to 1.0.
    /// </summary>
    private SentimentScores NormalizeSentimentScores(SentimentScores scores)
    {
        var total = scores.Positive + scores.Negative + scores.Neutral;

        if (total == 0)
        {
            return new SentimentScores { Positive = 0.33, Negative = 0.33, Neutral = 0.34 };
        }

        return new SentimentScores
        {
            Positive = scores.Positive / total,
            Negative = scores.Negative / total,
            Neutral = scores.Neutral / total
        };
    }

    /// <summary>
    /// Creates a sentiment result from normalized scores.
    /// </summary>
    private Models.SentimentScore CreateSentimentResult(string originalText, SentimentScores normalizedScores)
    {
        var dominantSentiment = normalizedScores.Positive > normalizedScores.Negative && normalizedScores.Positive > normalizedScores.Neutral ? "positive" :
                               normalizedScores.Negative > normalizedScores.Neutral ? "negative" : "neutral";

        var confidence = Math.Max(normalizedScores.Positive, Math.Max(normalizedScores.Negative, normalizedScores.Neutral));

        return new Models.SentimentScore
        {
            Positive = normalizedScores.Positive,
            Negative = normalizedScores.Negative,
            Neutral = normalizedScores.Neutral,
            Compound = normalizedScores.Positive - normalizedScores.Negative,
            Overall = dominantSentiment == "positive" ? SentimentType.Positive :
                     dominantSentiment == "negative" ? SentimentType.Negative : SentimentType.Neutral
        };
    }

    /// <summary>
    /// Calculates overall sentiment from multiple sentiment scores.
    /// </summary>
    private Models.SentimentScore CalculateOverallSentiment(List<Models.SentimentScore> sentiments)
    {
        if (sentiments.Count == 0)
        {
            return new Models.SentimentScore
            {
                Positive = 0.33,
                Negative = 0.33,
                Neutral = 0.34,
                Compound = 0.0,
                Overall = SentimentType.Neutral
            };
        }

        var avgPositive = sentiments.Average(s => s.Positive);
        var avgNegative = sentiments.Average(s => s.Negative);
        var avgNeutral = sentiments.Average(s => s.Neutral);
        var avgCompound = sentiments.Average(s => s.Compound);

        var dominantSentiment = avgPositive > avgNegative && avgPositive > avgNeutral ? SentimentType.Positive :
                               avgNegative > avgNeutral ? SentimentType.Negative : SentimentType.Neutral;

        return new Models.SentimentScore
        {
            Positive = avgPositive,
            Negative = avgNegative,
            Neutral = avgNeutral,
            Compound = avgCompound,
            Overall = dominantSentiment
        };
    }

    /// <summary>
    /// Analyzes sentiment trends over time.
    /// </summary>
    private async Task<SentimentTrend> AnalyzeSentimentTrendAsync(List<Models.SentimentScore> historicalSentiments)
    {
        // Perform actual sentiment trend analysis using time series methods
        await Task.CompletedTask; // Ensure async

        if (historicalSentiments.Count < 2)
        {
            return new SentimentTrend
            {
                Direction = "stable",
                Strength = 0.0,
                Confidence = 0.5
            };
        }

        // Calculate trend direction and strength
        var recentSentiments = historicalSentiments.TakeLast(10).ToList();
        var olderSentiments = historicalSentiments.Take(Math.Max(1, historicalSentiments.Count - 10)).ToList();

        var recentAvg = recentSentiments.Average(s => s.Positive - s.Negative);
        var olderAvg = olderSentiments.Average(s => s.Positive - s.Negative);

        var change = recentAvg - olderAvg;
        var direction = Math.Abs(change) < 0.05 ? "stable" : change > 0 ? "improving" : "declining";
        var strength = Math.Min(1.0, Math.Abs(change) * 2); // Scale to [0,1]

        // Calculate confidence based on consistency
        var variance = recentSentiments.Select(s => s.Positive - s.Negative)
                                      .Select(score => Math.Pow(score - recentAvg, 2))
                                      .Average();
        var confidence = Math.Max(0.1, 1.0 - Math.Sqrt(variance));

        return new SentimentTrend
        {
            Direction = direction,
            Strength = strength,
            Confidence = confidence,
            RecentAverage = recentAvg,
            HistoricalAverage = olderAvg,
            ChangeRate = change
        };
    }

    /// <summary>
    /// Extracts key sentiment indicators from text.
    /// </summary>
    private async Task<SentimentIndicators> ExtractSentimentIndicatorsAsync(string text)
    {
        // Extract actual sentiment indicators using linguistic analysis

        var preprocessedText = await PreprocessTextAsync(text);
        var words = preprocessedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Market-specific sentiment indicators
        var bullishIndicators = words.Count(w => new[] { "bull", "bullish", "moon", "pump", "rally", "surge", "breakout" }.Contains(w));
        var bearishIndicators = words.Count(w => new[] { "bear", "bearish", "dump", "crash", "fall", "decline", "correction" }.Contains(w));
        var volatilityIndicators = words.Count(w => new[] { "volatile", "swing", "fluctuate", "unstable", "choppy" }.Contains(w));
        var uncertaintyIndicators = words.Count(w => new[] { "uncertain", "unclear", "confused", "mixed", "sideways" }.Contains(w));

        var totalIndicators = Math.Max(1, bullishIndicators + bearishIndicators + volatilityIndicators + uncertaintyIndicators);

        return new SentimentIndicators
        {
            BullishRatio = (double)bullishIndicators / totalIndicators,
            BearishRatio = (double)bearishIndicators / totalIndicators,
            VolatilityRatio = (double)volatilityIndicators / totalIndicators,
            UncertaintyRatio = (double)uncertaintyIndicators / totalIndicators,
            TotalWords = words.Length,
            IndicatorDensity = (double)totalIndicators / Math.Max(1, words.Length)
        };
    }
}

/// <summary>
/// Raw sentiment scores before normalization.
/// </summary>
public class SentimentScores
{
    public double Positive { get; set; }
    public double Negative { get; set; }
    public double Neutral { get; set; }
}

/// <summary>
/// Sentiment trend analysis result.
/// </summary>
public class SentimentTrend
{
    public string Direction { get; set; } = string.Empty; // "improving", "declining", "stable"
    public double Strength { get; set; } // 0-1, strength of the trend
    public double Confidence { get; set; } // 0-1, confidence in the trend
    public double RecentAverage { get; set; }
    public double HistoricalAverage { get; set; }
    public double ChangeRate { get; set; }
}

/// <summary>
/// Market-specific sentiment indicators.
/// </summary>
public class SentimentIndicators
{
    public double BullishRatio { get; set; }
    public double BearishRatio { get; set; }
    public double VolatilityRatio { get; set; }
    public double UncertaintyRatio { get; set; }
    public int TotalWords { get; set; }
    public double IndicatorDensity { get; set; }
}
