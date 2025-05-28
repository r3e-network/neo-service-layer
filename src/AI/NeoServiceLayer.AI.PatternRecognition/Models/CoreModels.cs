namespace NeoServiceLayer.AI.PatternRecognition.Models;

/// <summary>
/// Represents a detected pattern.
/// </summary>
public class DetectedPattern
{
    /// <summary>
    /// Gets or sets the pattern ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern type.
    /// </summary>
    public PatternRecognitionType Type { get; set; }

    /// <summary>
    /// Gets or sets the match score (0-1).
    /// </summary>
    public double MatchScore { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0-1).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the pattern features.
    /// </summary>
    public Dictionary<string, object> Features { get; set; } = new();

    /// <summary>
    /// Gets or sets when the pattern was detected.
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the data point index.
    /// </summary>
    public int DataPointIndex { get; set; }

    /// <summary>
    /// Gets or sets the anomaly type.
    /// </summary>
    public AnomalyType AnomalyType { get; set; }
}

/// <summary>
/// Represents a detected anomaly.
/// </summary>
public class DetectedAnomaly
{
    /// <summary>
    /// Gets or sets the anomaly ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the anomaly type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the anomaly description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity score (0-1).
    /// </summary>
    public double Severity { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0-1).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the data point that triggered the anomaly.
    /// </summary>
    public Dictionary<string, object> DataPoint { get; set; } = new();

    /// <summary>
    /// Gets or sets when the anomaly was detected.
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the data point index.
    /// </summary>
    public int DataPointIndex { get; set; }

    /// <summary>
    /// Gets or sets the anomaly type.
    /// </summary>
    public AnomalyType AnomalyType { get; set; }
}