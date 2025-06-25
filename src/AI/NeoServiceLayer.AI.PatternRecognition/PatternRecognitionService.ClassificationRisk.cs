using Microsoft.Extensions.Logging;
using NeoServiceLayer.AI.PatternRecognition.Models;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.AI.PatternRecognition;

/// <summary>
/// Classification and risk assessment methods for the Pattern Recognition Service.
/// </summary>
public partial class PatternRecognitionService
{
    /// <inheritdoc/>
    public async Task<Models.ClassificationResult> ClassifyDataAsync(Models.ClassificationRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var classificationId = Guid.NewGuid().ToString();

            try
            {
                Logger.LogDebug("Classifying data {ClassificationId} with model {ModelId}",
                    classificationId, request.ModelId);

                var model = GetPatternModel(request.ModelId);

                // Perform classification within the enclave
                var classification = await ClassifyDataInEnclaveAsync(model, request.InputData);
                var confidence = await CalculateClassificationConfidenceAsync(model, classification);

                var result = new Models.ClassificationResult
                {
                    ClassificationId = classificationId,
                    ModelId = request.ModelId,
                    InputData = request.InputData,
                    Classification = classification,
                    Confidence = confidence,
                    ClassifiedAt = DateTime.UtcNow,
                    Success = true,
                    Metadata = request.Metadata
                };

                Logger.LogInformation("Data classification {ClassificationId}: {Classification} with confidence {Confidence:P2} on {Blockchain}",
                    classificationId, classification, confidence, blockchainType);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to classify data {ClassificationId}", classificationId);

                return new Models.ClassificationResult
                {
                    ClassificationId = classificationId,
                    ModelId = request.ModelId,
                    InputData = request.InputData,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ClassifiedAt = DateTime.UtcNow,
                    Metadata = request.Metadata
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<Models.RiskAssessmentResult> AssessRiskAsync(Models.RiskAssessmentRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var assessmentId = Guid.NewGuid().ToString();

            try
            {
                Logger.LogDebug("Assessing risk {AssessmentId} for entity {EntityId}",
                    assessmentId, request.EntityId);

                // Perform risk assessment within the enclave
                var riskScore = await CalculateRiskScoreInEnclaveAsync(request);
                var riskFactors = await IdentifyRiskFactorsInEnclaveAsync(request);

                // Process mitigating factors
                var mitigatingFactors = ProcessMitigatingFactors(request.MitigatingFactors);

                // Adjust risk score based on mitigating factors
                if (mitigatingFactors.Count > 0)
                {
                    var mitigationReduction = mitigatingFactors.Count * 0.02; // Each factor reduces risk by 2%
                    riskScore = Math.Max(0, riskScore - mitigationReduction);
                }

                var riskLevel = CalculateRiskLevel(riskScore);

                // Generate recommendations based on risk level and factors
                var recommendations = GenerateRiskRecommendations(riskLevel, riskFactors);

                var result = new Models.RiskAssessmentResult
                {
                    AssessmentId = assessmentId,
                    EntityId = request.EntityId,
                    EntityType = request.EntityType,
                    RiskScore = riskScore,
                    RiskLevel = riskLevel,
                    RiskFactors = riskFactors,
                    RiskBreakdown = new Dictionary<string, double>(riskFactors), // Copy risk factors to breakdown
                    MitigatingFactors = mitigatingFactors,
                    Recommendations = recommendations,
                    AssessedAt = DateTime.UtcNow,
                    Success = true,
                    Metadata = request.Metadata
                };

                Logger.LogInformation("Risk assessment {AssessmentId} for {EntityId}: Score {RiskScore:F3}, Level {RiskLevel} on {Blockchain}",
                    assessmentId, request.EntityId, riskScore, riskLevel, blockchainType);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to assess risk {AssessmentId}", assessmentId);

                return new Models.RiskAssessmentResult
                {
                    AssessmentId = assessmentId,
                    EntityId = request.EntityId,
                    EntityType = request.EntityType,
                    Success = false,
                    ErrorMessage = ex.Message,
                    AssessedAt = DateTime.UtcNow,
                    Metadata = request.Metadata
                };
            }
        });
    }

    /// <summary>
    /// Performs batch classification on multiple data points.
    /// </summary>
    /// <param name="requests">The classification requests.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Batch classification results.</returns>
    public async Task<BatchClassificationResult> ClassifyBatchAsync(IEnumerable<Models.ClassificationRequest> requests, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(requests);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        var batchId = Guid.NewGuid().ToString();
        var requestList = requests.ToList();

        try
        {
            Logger.LogDebug("Processing batch classification {BatchId} with {RequestCount} requests",
                batchId, requestList.Count);

            var results = new List<Models.ClassificationResult>();
            var tasks = requestList.Select(request => ClassifyDataAsync(request, blockchainType));

            var classificationResults = await Task.WhenAll(tasks);
            results.AddRange(classificationResults);

            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count - successCount;

            return new BatchClassificationResult
            {
                BatchId = batchId,
                TotalRequests = requestList.Count,
                SuccessfulClassifications = successCount,
                FailedClassifications = failureCount,
                Results = results,
                ProcessedAt = DateTime.UtcNow,
                Success = failureCount == 0
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process batch classification {BatchId}", batchId);

            return new BatchClassificationResult
            {
                BatchId = batchId,
                TotalRequests = requestList.Count,
                Success = false,
                ErrorMessage = ex.Message,
                ProcessedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Performs batch risk assessment on multiple entities.
    /// </summary>
    /// <param name="requests">The risk assessment requests.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Batch risk assessment results.</returns>
    public async Task<BatchRiskAssessmentResult> AssessRiskBatchAsync(IEnumerable<Models.RiskAssessmentRequest> requests, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(requests);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        var batchId = Guid.NewGuid().ToString();
        var requestList = requests.ToList();

        try
        {
            Logger.LogDebug("Processing batch risk assessment {BatchId} with {RequestCount} requests",
                batchId, requestList.Count);

            var results = new List<Models.RiskAssessmentResult>();
            var tasks = requestList.Select(request => AssessRiskAsync(request, blockchainType));

            var assessmentResults = await Task.WhenAll(tasks);
            results.AddRange(assessmentResults);

            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count - successCount;

            return new BatchRiskAssessmentResult
            {
                BatchId = batchId,
                TotalRequests = requestList.Count,
                SuccessfulAssessments = successCount,
                FailedAssessments = failureCount,
                Results = results,
                ProcessedAt = DateTime.UtcNow,
                Success = failureCount == 0
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process batch risk assessment {BatchId}", batchId);

            return new BatchRiskAssessmentResult
            {
                BatchId = batchId,
                TotalRequests = requestList.Count,
                Success = false,
                ErrorMessage = ex.Message,
                ProcessedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Calculates risk level from risk score.
    /// </summary>
    /// <param name="riskScore">The risk score (0-1).</param>
    /// <returns>Risk level enum.</returns>
    private Models.RiskLevel CalculateRiskLevel(double riskScore)
    {
        return riskScore switch
        {
            >= 0.9 => Models.RiskLevel.Critical,  // High threshold for Critical
            >= 0.6 => Models.RiskLevel.High,
            >= 0.4 => Models.RiskLevel.Medium,
            >= 0.15 => Models.RiskLevel.Low,      // Adjusted threshold for Low
            _ => Models.RiskLevel.Minimal
        };
    }

    /// <summary>
    /// Processes mitigating factors from the request.
    /// </summary>
    /// <param name="requestMitigatingFactors">The mitigating factors from the request.</param>
    /// <returns>Dictionary of mitigating factors with scores.</returns>
    private Dictionary<string, double> ProcessMitigatingFactors(Dictionary<string, bool>? requestMitigatingFactors)
    {
        var mitigatingFactors = new Dictionary<string, double>();

        if (requestMitigatingFactors == null)
            return mitigatingFactors;

        foreach (var factor in requestMitigatingFactors.Where(f => f.Value))
        {
            switch (factor.Key.ToLowerInvariant())
            {
                case "kyc_verified":
                    mitigatingFactors["KYC Verified"] = 0.95; // High confidence mitigation
                    break;
                case "established_relationship":
                    mitigatingFactors["Established Relationship"] = 0.85; // Strong mitigation
                    break;
                case "whitelisted":
                    mitigatingFactors["Whitelisted"] = 0.9; // Very high mitigation
                    break;
                case "verified_contract":
                    mitigatingFactors["Verified Contract"] = 0.8; // Good mitigation
                    break;
                default:
                    mitigatingFactors[factor.Key] = 0.7; // Default mitigation value
                    break;
            }
        }

        return mitigatingFactors;
    }

    /// <summary>
    /// Gets classification statistics for a model.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="timeRange">The time range to analyze.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Classification statistics.</returns>
    public async Task<ClassificationStatistics> GetClassificationStatisticsAsync(string modelId, TimeSpan timeRange, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        await Task.Delay(50); // Simulate processing time

        // In production, this would query actual classification history
        return new ClassificationStatistics
        {
            ModelId = modelId,
            TimeRange = timeRange,
            TotalClassifications = Random.Shared.Next(100, 1000),
            SuccessfulClassifications = Random.Shared.Next(90, 100),
            AverageConfidence = Random.Shared.NextDouble() * 0.3 + 0.7,
            ClassificationDistribution = new Dictionary<string, int>
            {
                ["Legitimate"] = Random.Shared.Next(50, 200),
                ["Suspicious"] = Random.Shared.Next(10, 50),
                ["Fraudulent"] = Random.Shared.Next(1, 20)
            },
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets risk assessment statistics.
    /// </summary>
    /// <param name="timeRange">The time range to analyze.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Risk assessment statistics.</returns>
    public async Task<RiskAssessmentStatistics> GetRiskAssessmentStatisticsAsync(TimeSpan timeRange, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        await Task.Delay(50); // Simulate processing time

        // In production, this would query actual risk assessment history
        return new RiskAssessmentStatistics
        {
            TimeRange = timeRange,
            TotalAssessments = Random.Shared.Next(100, 1000),
            SuccessfulAssessments = Random.Shared.Next(90, 100),
            AverageRiskScore = Random.Shared.NextDouble() * 0.5 + 0.2,
            RiskLevelDistribution = new Dictionary<string, int>
            {
                ["Minimal"] = Random.Shared.Next(30, 100),
                ["Low"] = Random.Shared.Next(20, 80),
                ["Medium"] = Random.Shared.Next(10, 50),
                ["High"] = Random.Shared.Next(5, 30),
                ["Critical"] = Random.Shared.Next(1, 10)
            },
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Generates risk recommendations based on risk level and factors.
    /// </summary>
    /// <param name="riskLevel">The calculated risk level.</param>
    /// <param name="riskFactors">Dictionary of risk factors.</param>
    /// <returns>List of recommendations.</returns>
    private static List<string> GenerateRiskRecommendations(Models.RiskLevel riskLevel, Dictionary<string, double> riskFactors)
    {
        var recommendations = new List<string>();

        // Base recommendations based on risk level
        switch (riskLevel)
        {
            case Models.RiskLevel.Critical:
                recommendations.Add("Immediate enhanced monitoring required");
                recommendations.Add("Block transaction pending manual review");
                recommendations.Add("Escalate to compliance team");
                break;

            case Models.RiskLevel.High:
                recommendations.Add("enhanced monitoring recommended");
                recommendations.Add("Additional verification steps required");
                recommendations.Add("Review transaction patterns");
                break;

            case Models.RiskLevel.Medium:
                recommendations.Add("Standard monitoring protocols");
                recommendations.Add("Periodic review recommended");
                break;

            case Models.RiskLevel.Low:
                recommendations.Add("Continue standard processing");
                recommendations.Add("Routine monitoring sufficient");
                break;

            case Models.RiskLevel.Minimal:
                recommendations.Add("No additional action required");
                break;
        }

        // Add specific recommendations based on risk factors
        if (riskFactors.ContainsKey("Amount Risk"))
            recommendations.Add("Monitor large transaction patterns");

        if (riskFactors.ContainsKey("Sender Reputation"))
            recommendations.Add("Verify sender identity and history");

        if (riskFactors.ContainsKey("Network Trust"))
            recommendations.Add("Review network connections and relationships");

        if (riskFactors.ContainsKey("Transaction Complexity"))
            recommendations.Add("Analyze transaction structure for compliance");

        return recommendations;
    }
}
