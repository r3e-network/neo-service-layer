using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Configuration;
using NeoServiceLayer.Tee.Host.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Base class for AI-powered services with model management capabilities.
/// </summary>
public abstract class AIServiceBase : EnclaveBlockchainServiceBase
{
    private readonly Dictionary<string, ModelInfo> _registeredModels = new();
    private readonly object _modelsLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AIServiceBase"/> class.
    /// </summary>
    /// <param name="name">The name of the service.</param>
    /// <param name="description">The description of the service.</param>
    /// <param name="version">The version of the service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The service configuration.</param>
    protected AIServiceBase(string name, string description, string version, ILogger logger, IServiceConfiguration? configuration = null)
        : base(name, description, version, logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        Configuration = configuration;
        AddCapability<IAIService>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIServiceBase"/> class with enclave manager.
    /// </summary>
    /// <param name="name">The name of the service.</param>
    /// <param name="description">The description of the service.</param>
    /// <param name="version">The version of the service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="supportedBlockchains">The supported blockchain types.</param>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="configuration">The service configuration.</param>
    protected AIServiceBase(string name, string description, string version, ILogger logger, IEnumerable<BlockchainType> supportedBlockchains, IEnclaveManager? enclaveManager, IServiceConfiguration? configuration = null)
        : base(name, description, version, logger, supportedBlockchains, enclaveManager)
    {
        Configuration = configuration;
        AddCapability<IAIService>();
    }

    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    protected IServiceConfiguration? Configuration { get; }

    /// <summary>
    /// Gets the registered models.
    /// </summary>
    protected IEnumerable<ModelInfo> RegisteredModels
    {
        get
        {
            lock (_modelsLock)
            {
                return _registeredModels.Values.ToList();
            }
        }
    }

    /// <summary>
    /// Registers a new AI model.
    /// </summary>
    /// <param name="modelInfo">The model information.</param>
    /// <returns>The model ID.</returns>
    protected virtual string RegisterModel(ModelInfo modelInfo)
    {
        ArgumentNullException.ThrowIfNull(modelInfo);

        var modelId = Guid.NewGuid().ToString();
        modelInfo.Id = modelId;
        modelInfo.RegisteredAt = DateTime.UtcNow;

        lock (_modelsLock)
        {
            _registeredModels[modelId] = modelInfo;
        }

        Logger.LogInformation("Model {ModelId} registered for service {ServiceName}", modelId, Name);
        return modelId;
    }

    /// <summary>
    /// Gets a registered model by ID.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <returns>The model information, or null if not found.</returns>
    protected virtual ModelInfo? GetModel(string modelId)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);

        lock (_modelsLock)
        {
            return _registeredModels.TryGetValue(modelId, out var model) ? model : null;
        }
    }

    /// <summary>
    /// Unregisters a model.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <returns>True if the model was unregistered, false otherwise.</returns>
    protected virtual bool UnregisterModel(string modelId)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);

        lock (_modelsLock)
        {
            if (_registeredModels.Remove(modelId))
            {
                Logger.LogInformation("Model {ModelId} unregistered from service {ServiceName}", modelId, Name);
                return true;
            }
        }

        Logger.LogWarning("Model {ModelId} not found for unregistration in service {ServiceName}", modelId, Name);
        return false;
    }

    /// <summary>
    /// Executes AI inference within the enclave.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="modelId">The model ID.</param>
    /// <param name="request">The inference request.</param>
    /// <param name="inferenceFunc">The inference function to execute in the enclave.</param>
    /// <returns>The inference result.</returns>
    protected virtual async Task<TResult> ExecuteInferenceAsync<TRequest, TResult>(
        string modelId,
        TRequest request,
        Func<string, TRequest, Task<TResult>> inferenceFunc)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(inferenceFunc);

        // Validate model exists
        var model = GetModel(modelId);
        if (model == null)
        {
            throw new ArgumentException($"Model {modelId} not found", nameof(modelId));
        }

        // Execute inference in enclave
        return await ExecuteInEnclaveAsync(async () =>
        {
            Logger.LogDebug("Executing inference for model {ModelId} in service {ServiceName}", modelId, Name);
            var result = await inferenceFunc(modelId, request);
            Logger.LogDebug("Inference completed for model {ModelId} in service {ServiceName}", modelId, Name);
            return result;
        });
    }

    /// <summary>
    /// Validates model input data.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="inputData">The input data.</param>
    /// <returns>True if the input is valid, false otherwise.</returns>
    protected virtual bool ValidateModelInput(string modelId, object[] inputData)
    {
        var model = GetModel(modelId);
        if (model == null)
        {
            return false;
        }

        // Basic validation - can be extended based on model schema
        return inputData != null && inputData.Length > 0;
    }

    /// <summary>
    /// Gets model performance metrics.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <returns>The model metrics.</returns>
    protected virtual Dictionary<string, object> GetModelMetrics(string modelId)
    {
        var model = GetModel(modelId);
        if (model == null)
        {
            return new Dictionary<string, object>();
        }

        return new Dictionary<string, object>
        {
            ["modelId"] = modelId,
            ["registeredAt"] = model.RegisteredAt,
            ["inferenceCount"] = model.InferenceCount,
            ["lastUsed"] = model.LastUsed,
            ["averageInferenceTime"] = model.AverageInferenceTimeMs
        };
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing AI service {ServiceName}", Name);

        // Initialize base functionality
        // Note: OnInitializeAsync is abstract, so we implement AI-specific initialization here

        // Load pre-trained models if configured
        await LoadPretrainedModelsAsync();

        Logger.LogInformation("AI service {ServiceName} initialized successfully", Name);
        return true;
    }

    /// <summary>
    /// Loads pre-trained models during service initialization.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task LoadPretrainedModelsAsync()
    {
        // Override in derived classes to load specific models
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        // Implement AI-specific health check
        // Check if models are loaded and accessible
        var modelCount = RegisteredModels.Count();
        if (modelCount == 0)
        {
            Logger.LogWarning("No models registered in AI service {ServiceName}", Name);
            return Task.FromResult(ServiceHealth.Degraded);
        }

        return Task.FromResult(ServiceHealth.Healthy);
    }

    /// <inheritdoc/>
    protected override Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting AI service {ServiceName}", Name);
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping AI service {ServiceName}", Name);
        return Task.FromResult(true);
    }
}

/// <summary>
/// Interface marker for AI services.
/// </summary>
public interface IAIService
{
}

/// <summary>
/// Model information for AI services.
/// </summary>
public class ModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Format { get; set; } = "onnx";
    public string[] InputSchema { get; set; } = Array.Empty<string>();
    public string[] OutputSchema { get; set; } = Array.Empty<string>();
    public DateTime RegisteredAt { get; set; }
    public DateTime LastUsed { get; set; }
    public long InferenceCount { get; set; }
    public double AverageInferenceTimeMs { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
