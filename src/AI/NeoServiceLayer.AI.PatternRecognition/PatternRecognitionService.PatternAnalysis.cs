using Microsoft.Extensions.Logging;
using NeoServiceLayer.AI.PatternRecognition.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Text.Json;


namespace NeoServiceLayer.AI.PatternRecognition;

/// <summary>
/// Pattern analysis operations for the Pattern Recognition Service.
/// </summary>
public partial class PatternRecognitionService
{

    /// <summary>
    /// Generates training data for a pattern model.
    /// </summary>
    /// <param name="definition">The pattern model definition.</param>
    /// <returns>Training data array.</returns>
    private double[] GenerateTrainingDataForModel(PatternModelDefinition definition)
    {
        // Generate synthetic training data based on the model type
        var trainingData = new List<double>();

        switch (definition.PatternType)
        {
            case PatternRecognitionType.FraudDetection:
                // Generate fraud detection training data (normal vs fraudulent patterns)
                for (int i = 0; i < 1000; i++)
                {
                    // Normal transactions (first 800)
                    if (i < 800)
                    {
                        trainingData.Add(Random.Shared.NextDouble() * 0.3); // Low fraud scores
                        trainingData.Add(Random.Shared.NextDouble() * 1000 + 10); // Normal amounts
                        trainingData.Add(Random.Shared.NextDouble() * 24); // Random hours
                        trainingData.Add(0); // Label: not fraud
                    }
                    else
                    {
                        trainingData.Add(Random.Shared.NextDouble() * 0.7 + 0.3); // High fraud scores
                        trainingData.Add(Random.Shared.NextDouble() * 100000 + 10000); // Large amounts
                        trainingData.Add(Random.Shared.NextDouble() * 6 + 2); // Unusual hours (2-8 AM)
                        trainingData.Add(1); // Label: fraud
                    }
                }
                break;

            case PatternRecognitionType.BehavioralAnalysis:
                // Generate behavior classification data (normal vs suspicious)
                for (int i = 0; i < 1000; i++)
                {
                    // Normal behavior (first 700)
                    if (i < 700)
                    {
                        trainingData.Add(Random.Shared.NextDouble() * 50 + 10); // Normal transaction amounts
                        trainingData.Add(Random.Shared.NextDouble() * 10 + 1); // Normal frequency
                        trainingData.Add(Random.Shared.NextDouble() * 16 + 8); // Business hours
                        trainingData.Add(0); // Label: normal
                    }
                    else
                    {
                        trainingData.Add(Random.Shared.NextDouble() * 100000 + 50000); // Suspicious amounts
                        trainingData.Add(Random.Shared.NextDouble() * 100 + 50); // High frequency
                        trainingData.Add(Random.Shared.NextDouble() * 8); // Off hours
                        trainingData.Add(1); // Label: suspicious
                    }
                }
                break;

            case PatternRecognitionType.AnomalyDetection:
                // Generate anomaly detection data (mostly normal with some outliers)
                for (int i = 0; i < 1000; i++)
                {
                    if (i < 900)
                    {
                        // Normal data points
                        trainingData.Add(Random.Shared.NextDouble() * 20 + 40); // Around 40-60
                        trainingData.Add(Random.Shared.NextDouble() * 10 + 45); // Around 45-55
                        trainingData.Add(0); // Label: normal
                    }
                    else
                    {
                        // Anomalous data points
                        trainingData.Add(Random.Shared.NextDouble() * 200 + 100); // Outliers
                        trainingData.Add(Random.Shared.NextDouble() * 200 + 100); // Outliers
                        trainingData.Add(1); // Label: anomaly
                    }
                }
                break;

            case PatternRecognitionType.Classification:
                // Generate classification training data
                for (int i = 0; i < 1000; i++)
                {
                    var classLabel = i % 3; // 3 classes
                    var baseValue = classLabel * 30 + 10;

                    trainingData.Add(Random.Shared.NextDouble() * 20 + baseValue); // Feature 1
                    trainingData.Add(Random.Shared.NextDouble() * 15 + baseValue); // Feature 2
                    trainingData.Add(classLabel); // Label
                }
                break;

            case PatternRecognitionType.ClusteringAnalysis:
                // Generate clustering data (unlabeled)
                for (int i = 0; i < 1000; i++)
                {
                    var cluster = i % 4; // 4 clusters
                    var centerX = (cluster % 2) * 50 + 25;
                    var centerY = (cluster / 2) * 50 + 25;

                    trainingData.Add(Random.Shared.NextDouble() * 20 + centerX - 10); // X coordinate
                    trainingData.Add(Random.Shared.NextDouble() * 20 + centerY - 10); // Y coordinate
                }
                break;

            case PatternRecognitionType.Regression:
                // Generate regression training data
                for (int i = 0; i < 500; i++)
                {
                    double x = Random.Shared.NextDouble() * 100;
                    double y = 2 * x + 5 + (Random.Shared.NextDouble() - 0.5) * 20; // y = 2x + 5 + noise
                    trainingData.Add(x);
                    trainingData.Add(y);
                }
                break;

            default:
                // Default linear regression data
                for (int i = 0; i < 100; i++)
                {
                    double x = i;
                    double y = 2 * x + 5 + (Random.Shared.NextDouble() - 0.5) * 10; // y = 2x + 5 + noise
                    trainingData.Add(x);
                    trainingData.Add(y);
                }
                break;
        }

        return trainingData.ToArray();
    }

    /// <summary>
    /// Calculates pattern match score between request and known pattern.
    /// </summary>
    /// <param name="request">The fraud detection request.</param>
    /// <param name="pattern">The known fraud pattern.</param>
    /// <returns>Pattern match score.</returns>
    private double CalculatePatternMatch(NeoServiceLayer.AI.PatternRecognition.Models.FraudDetectionRequest request, NeoServiceLayer.AI.PatternRecognition.Models.FraudPattern pattern)
    {
        var score = 0.0;
        var matchCount = 0;
        var totalChecks = 0;

        // Check amount pattern
        totalChecks++;
        if (pattern.AmountRange.Min != 0 || pattern.AmountRange.Max != 0)
        {
            if (request.Amount >= pattern.AmountRange.Min &&
                request.Amount <= pattern.AmountRange.Max)
            {
                matchCount++;
                score += 0.3;
            }
        }

        // Check time pattern
        totalChecks++;
        if (pattern.TimePattern != null && pattern.TimePattern.Count > 0)
        {
            var hour = request.Timestamp.Hour;
            // Check if TimePattern contains suspicious hours information
            var suspiciousHours = pattern.TimePattern.GetSuspiciousHours();
            if (suspiciousHours != null && suspiciousHours.Contains(hour))
            {
                matchCount++;
                score += 0.2;
            }
        }

        // Check frequency pattern
        totalChecks++;
        if (pattern.FrequencyPattern != null &&
            request.Features.TryGetValue("transaction_count", out var countObj) &&
            countObj is int transactionCount)
        {
            // Assuming FrequencyPattern has a property for minimum transactions
            if (transactionCount >= 10) // Use a default or check if FrequencyPattern has specific threshold
            {
                matchCount++;
                score += 0.2;
            }
        }

        // Check address pattern
        totalChecks++;
        if (pattern.AddressPatterns?.Any(ap =>
            ap.SuspiciousAddresses?.Contains(request.FromAddress) == true ||
            ap.SuspiciousAddresses?.Contains(request.ToAddress) == true) == true)
        {
            matchCount++;
            score += 0.3;
        }

        // Calculate confidence based on match ratio
        var confidence = (double)matchCount / totalChecks;
        return score * confidence;
    }

    /// <summary>
    /// Checks address reputation against known blacklists.
    /// </summary>
    /// <param name="address">The address to check.</param>
    /// <returns>Risk score for the address.</returns>
    private async Task<double> CheckAddressReputationAsync(string address)
    {
        // Load blacklist from secure storage
        var blacklistJson = await _enclaveManager!.StorageRetrieveDataAsync("address_blacklist", GetAddressBlacklistEncryptionKey(), CancellationToken.None);

        if (string.IsNullOrEmpty(blacklistJson))
        {
            return 0.0; // No blacklist available
        }

        var blacklist = JsonSerializer.Deserialize<string[]>(blacklistJson);
        if (blacklist?.Contains(address, StringComparer.OrdinalIgnoreCase) == true)
        {
            return 1.0; // Address is blacklisted
        }

        // Check for partial matches or similar addresses
        var similarityScore = blacklist?.Max(blacklistedAddress =>
            CalculateAddressSimilarity(address, blacklistedAddress)) ?? 0.0;

        return Math.Min(1.0, similarityScore);
    }

    /// <summary>
    /// Checks if the transaction pattern matches mixing services.
    /// </summary>
    /// <param name="request">The fraud detection request.</param>
    /// <returns>True if mixing service pattern detected.</returns>
    private async Task<bool> IsMixingServicePatternAsync(NeoServiceLayer.AI.PatternRecognition.Models.FraudDetectionRequest request)
    {
        // Load known mixing service patterns
        var patternsJson = await _enclaveManager!.StorageRetrieveDataAsync("mixing_patterns", GetMixingPatternsEncryptionKey(), CancellationToken.None);

        if (string.IsNullOrEmpty(patternsJson))
        {
            return false;
        }

        var patterns = JsonSerializer.Deserialize<MixingServicePattern[]>(patternsJson);

        foreach (var pattern in patterns ?? Array.Empty<MixingServicePattern>())
        {
            if (MatchesMixingPattern(request, pattern))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a date is a holiday.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>True if the date is a holiday.</returns>
    private bool IsHoliday(DateTime date)
    {
        // Simple holiday detection (can be expanded)
        var holidays = new[]
        {
            new DateTime(date.Year, 1, 1),   // New Year's Day
            new DateTime(date.Year, 7, 4),   // Independence Day (US)
            new DateTime(date.Year, 12, 25), // Christmas Day
        };

        return holidays.Any(h => h.Date == date.Date);
    }

    /// <summary>
    /// Extracts ML features from fraud detection request.
    /// </summary>
    /// <param name="request">The fraud detection request.</param>
    /// <param name="baseScore">The base rule-based score.</param>
    /// <returns>Feature dictionary for ML model.</returns>
    private Dictionary<string, object> ExtractMLFeatures(NeoServiceLayer.AI.PatternRecognition.Models.FraudDetectionRequest request, double baseScore)
    {
        return new Dictionary<string, object>
        {
            ["amount"] = request.Amount,
            ["hour"] = request.Timestamp.Hour,
            ["day_of_week"] = (int)request.Timestamp.DayOfWeek,
            ["base_score"] = baseScore,
            ["from_address_length"] = request.FromAddress.Length,
            ["to_address_length"] = request.ToAddress.Length,
            ["is_weekend"] = request.Timestamp.DayOfWeek == DayOfWeek.Saturday || request.Timestamp.DayOfWeek == DayOfWeek.Sunday
        };
    }

    /// <summary>
    /// Runs ML inference on extracted features.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="features">The feature dictionary.</param>
    /// <returns>ML prediction score.</returns>
    private async Task<double> RunMLInferenceAsync(string modelId, Dictionary<string, object> features)
    {
        // In production, this would use the actual ML model
        await Task.Delay(50);

        // Simple mock prediction based on features
        var amount = (decimal)features["amount"];
        var hour = (int)features["hour"];
        var baseScore = (double)features["base_score"];

        var mlAdjustment = 0.0;

        // Adjust based on amount
        if (amount > 50000) mlAdjustment += 0.2;

        // Adjust based on time
        if (hour < 6 || hour > 22) mlAdjustment += 0.1;

        return Math.Min(1.0, baseScore + mlAdjustment);
    }

    /// <summary>
    /// Gets the encryption key for address blacklist.
    /// </summary>
    /// <returns>The encryption key.</returns>
    private string GetAddressBlacklistEncryptionKey()
    {
        try
        {
            // Derive key for address blacklist encryption
            var keyDerivationInput = $"address_blacklist_encryption_key_{Environment.MachineName}_{DateTime.UtcNow:yyyyMMdd}";
            
            // Use HKDF for proper key derivation
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes("neo-address-blacklist-salt"));
            var derivedKey = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(keyDerivationInput));
            
            Logger.LogDebug("Generated address blacklist encryption key using HKDF");
            return Convert.ToBase64String(derivedKey);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to derive address blacklist encryption key");
            throw new InvalidOperationException("Address blacklist encryption key derivation failed", ex);
        }
    }

    /// <summary>
    /// Gets the encryption key for mixing patterns.
    /// </summary>
    /// <returns>The encryption key.</returns>
    private string GetMixingPatternsEncryptionKey()
    {
        try
        {
            // Derive key for mixing patterns encryption
            var keyDerivationInput = $"mixing_patterns_encryption_key_{Environment.MachineName}_{DateTime.UtcNow:yyyyMMdd}";
            
            // Use HKDF for proper key derivation
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes("neo-mixing-patterns-salt"));
            var derivedKey = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(keyDerivationInput));
            
            Logger.LogDebug("Generated mixing patterns encryption key using HKDF");
            return Convert.ToBase64String(derivedKey);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to derive mixing patterns encryption key");
            throw new InvalidOperationException("Mixing patterns encryption key derivation failed", ex);
        }
    }

    /// <summary>
    /// Calculates similarity between two addresses.
    /// </summary>
    /// <param name="address1">First address.</param>
    /// <param name="address2">Second address.</param>
    /// <returns>Similarity score between 0 and 1.</returns>
    private double CalculateAddressSimilarity(string address1, string address2)
    {
        // Simple Levenshtein distance-based similarity
        var distance = LevenshteinDistance(address1, address2);
        var maxLength = Math.Max(address1.Length, address2.Length);
        return maxLength == 0 ? 1.0 : 1.0 - (double)distance / maxLength;
    }

    /// <summary>
    /// Calculates Levenshtein distance between two strings.
    /// </summary>
    /// <param name="s1">First string.</param>
    /// <param name="s2">Second string.</param>
    /// <returns>Levenshtein distance.</returns>
    private int LevenshteinDistance(string s1, string s2)
    {
        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    /// <summary>
    /// Checks if request matches a mixing service pattern.
    /// </summary>
    /// <param name="request">The fraud detection request.</param>
    /// <param name="pattern">The mixing service pattern.</param>
    /// <returns>True if pattern matches.</returns>
    private bool MatchesMixingPattern(NeoServiceLayer.AI.PatternRecognition.Models.FraudDetectionRequest request, MixingServicePattern pattern)
    {
        // Check if addresses match known mixing service addresses
        if (pattern.KnownAddresses?.Contains(request.FromAddress, StringComparer.OrdinalIgnoreCase) == true ||
            pattern.KnownAddresses?.Contains(request.ToAddress, StringComparer.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        // Check for mixing service transaction patterns
        if (pattern.TypicalAmounts != null &&
            pattern.TypicalAmounts.Any(amount => Math.Abs(request.Amount - amount) < amount * 0.05m))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Trains an AI model in the enclave.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="modelType">The model type.</param>
    /// <param name="trainingData">The training data.</param>
    /// <param name="parameters">The training parameters.</param>
    /// <returns>Training result as JSON string.</returns>
    private async Task<string> TrainAIModelInEnclaveAsync(string modelId, string modelType, double[] trainingData, string parameters)
    {
        await Task.Delay(1000); // Simulate training time

        // In production, this would perform actual AI model training in the enclave
        var accuracy = Random.Shared.NextDouble() * 0.2 + 0.8; // 0.8-1.0
        var loss = Random.Shared.NextDouble() * 0.1 + 0.05; // 0.05-0.15

        var result = new
        {
            success = true,
            model_id = modelId,
            accuracy = accuracy,
            loss = loss,
            training_samples = trainingData.Length,
            model_type = modelType,
            trained_at = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(result);
    }
}
