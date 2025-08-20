using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Tee.Enclave.Models;

/// <summary>
/// Represents a trained machine learning model stored in the enclave.
/// </summary>
public class TrainedModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the model.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the name of the model.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model type (e.g., "neural_network", "decision_tree").
    /// </summary>
    public string ModelType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the serialized model data.
    /// </summary>
    public byte[] ModelData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the model metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the accuracy metrics.
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Gets or sets whether the model is encrypted.
    /// </summary>
    public bool IsEncrypted { get; set; } = true;

    /// <summary>
    /// Gets or sets the model status.
    /// </summary>
    public ModelStatus Status { get; set; } = ModelStatus.Ready;

    /// <summary>
    /// Gets or sets the model ID (alias for Id).
    /// </summary>
    public string ModelId 
    { 
        get => Id; 
        set => Id = value; 
    }

    /// <summary>
    /// Gets or sets when the model was trained (alias for CreatedAt).
    /// </summary>
    public DateTime TrainedAt 
    { 
        get => CreatedAt; 
        set => CreatedAt = value; 
    }
}

/// <summary>
/// Represents the status of a trained model.
/// </summary>
public enum ModelStatus
{
    /// <summary>
    /// Model is being trained.
    /// </summary>
    Training,

    /// <summary>
    /// Model is ready for use.
    /// </summary>
    Ready,

    /// <summary>
    /// Model is being validated.
    /// </summary>
    Validating,

    /// <summary>
    /// Model is archived.
    /// </summary>
    Archived,

    /// <summary>
    /// Model has failed.
    /// </summary>
    Failed
}