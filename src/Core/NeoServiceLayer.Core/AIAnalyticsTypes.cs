using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core;

// Fraud Detection Types
public class FraudDetectionRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class FraudDetectionResult
{
    public bool IsFraudulent { get; set; }
    public double RiskScore { get; set; }
    public List<string> RiskFactors { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
}

// Anomaly Detection Types
public class AnomalyDetectionRequest
{
    public string DataSource { get; set; } = string.Empty;
    public Dictionary<string, object> DataPoints { get; set; } = new();
    public DateTime TimeRange { get; set; }
    public double Threshold { get; set; } = 0.5;
}

public class AnomalyDetectionResult
{
    public bool IsAnomaly { get; set; }
    public double AnomalyScore { get; set; }
    public string AnomalyType { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}

// Classification Types
public class ClassificationRequest
{
    public string Data { get; set; } = string.Empty;
    public string ClassificationModel { get; set; } = string.Empty;
    public Dictionary<string, object> Features { get; set; } = new();
}

public class ClassificationResult
{
    public string PredictedClass { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public Dictionary<string, double> ClassProbabilities { get; set; } = new();
}

// Zero Knowledge Proof Types
public class ProofRequest
{
    public string ProofType { get; set; } = string.Empty;
    public Dictionary<string, object> Claims { get; set; } = new();
    public string Circuit { get; set; } = string.Empty;
}

public class ProofResult
{
    public string Proof { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public Dictionary<string, object> PublicInputs { get; set; } = new();
}

public class ProofVerification
{
    public string ProofId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string VerificationError { get; set; } = string.Empty;
}

// Fair Transaction Types
public class FairTransactionRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public List<string> Participants { get; set; } = new();
    public Dictionary<string, object> TransactionData { get; set; } = new();
}

public class TransactionAnalysisRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public string AnalysisType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class FairnessAnalysisResult
{
    public bool IsFair { get; set; }
    public double FairnessScore { get; set; }
    public List<string> FairnessViolations { get; set; } = new();
    public Dictionary<string, object> AnalysisDetails { get; set; } = new();
}
