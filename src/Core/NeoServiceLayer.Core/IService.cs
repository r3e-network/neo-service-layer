using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Core;

/// <summary>
/// Interface for all services in the Neo Service Layer.
/// </summary>
public interface IService
{
    /// <summary>
    /// Gets the name of the service.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the service.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the version of the service.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets a value indicating whether the service is running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the service dependencies.
    /// </summary>
    IEnumerable<object> Dependencies { get; }

    /// <summary>
    /// Gets the service capabilities.
    /// </summary>
    IEnumerable<Type> Capabilities { get; }

    /// <summary>
    /// Gets the service metadata.
    /// </summary>
    IDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Initializes the service.
    /// </summary>
    /// <returns>True if initialization was successful, false otherwise.</returns>
    Task<bool> InitializeAsync();

    /// <summary>
    /// Starts the service.
    /// </summary>
    /// <returns>True if the service was started successfully, false otherwise.</returns>
    Task<bool> StartAsync();

    /// <summary>
    /// Stops the service.
    /// </summary>
    /// <returns>True if the service was stopped successfully, false otherwise.</returns>
    Task<bool> StopAsync();

    /// <summary>
    /// Gets the health status of the service.
    /// </summary>
    /// <returns>The health status of the service.</returns>
    Task<ServiceHealth> GetHealthAsync();

    /// <summary>
    /// Gets the service metrics.
    /// </summary>
    /// <returns>The service metrics.</returns>
    Task<IDictionary<string, object>> GetMetricsAsync();

    /// <summary>
    /// Validates the service dependencies.
    /// </summary>
    /// <param name="availableServices">The available services.</param>
    /// <returns>True if all required dependencies are satisfied, false otherwise.</returns>
    Task<bool> ValidateDependenciesAsync(IEnumerable<IService> availableServices);
}

/// <summary>
/// Interface for services that require enclave operations.
/// </summary>
public interface IEnclaveService : IService
{
    /// <summary>
    /// Gets a value indicating whether the enclave is initialized.
    /// </summary>
    bool IsEnclaveInitialized { get; }

    /// <summary>
    /// Initializes the enclave.
    /// </summary>
    /// <returns>True if the enclave was initialized successfully, false otherwise.</returns>
    Task<bool> InitializeEnclaveAsync();
}

/// <summary>
/// Interface for services that support blockchain operations.
/// </summary>
public interface IBlockchainService : IService
{
    /// <summary>
    /// Gets the supported blockchain types.
    /// </summary>
    IEnumerable<BlockchainType> SupportedBlockchains { get; }

    /// <summary>
    /// Checks if a specific blockchain type is supported.
    /// </summary>
    /// <param name="blockchainType">The blockchain type to check.</param>
    /// <returns>True if the blockchain type is supported, false otherwise.</returns>
    bool SupportsBlockchain(BlockchainType blockchainType);
}

/// <summary>
/// Interface for AI-powered prediction and forecasting services.
/// </summary>
public interface IPredictionService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Generates a prediction based on input data.
    /// </summary>
    /// <param name="request">The prediction request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The prediction result.</returns>
    Task<PredictionResult> PredictAsync(PredictionRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Analyzes sentiment from text data.
    /// </summary>
    /// <param name="request">The sentiment analysis request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The sentiment analysis result.</returns>
    Task<SentimentResult> AnalyzeSentimentAsync(SentimentAnalysisRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Registers a new prediction model.
    /// </summary>
    /// <param name="registration">The model registration details.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The model ID.</returns>
    Task<string> RegisterModelAsync(ModelRegistration registration, BlockchainType blockchainType);
}

/// <summary>
/// Interface for AI-powered pattern detection and classification services.
/// </summary>
public interface IPatternRecognitionService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Detects fraud in transaction data.
    /// </summary>
    /// <param name="request">The fraud detection request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The fraud detection result.</returns>
    Task<FraudDetectionResult> DetectFraudAsync(FraudDetectionRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Detects anomalies in data patterns.
    /// </summary>
    /// <param name="request">The anomaly detection request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The anomaly detection result.</returns>
    Task<AnomalyDetectionResult> DetectAnomaliesAsync(AnomalyDetectionRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Classifies data into predefined categories.
    /// </summary>
    /// <param name="request">The classification request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The classification result.</returns>
    Task<ClassificationResult> ClassifyDataAsync(ClassificationRequest request, BlockchainType blockchainType);
}

/// <summary>
/// Interface for zero-knowledge proof services.
/// </summary>
public interface IZeroKnowledgeService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Generates a zero-knowledge proof.
    /// </summary>
    /// <param name="request">The proof generation request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The proof result.</returns>
    Task<ProofResult> GenerateProofAsync(ProofRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Verifies a zero-knowledge proof.
    /// </summary>
    /// <param name="verification">The proof verification request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the proof is valid.</returns>
    Task<bool> VerifyProofAsync(ProofVerification verification, BlockchainType blockchainType);
}

/// <summary>
/// Interface for fair ordering and MEV protection services.
/// </summary>
public interface IFairOrderingService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Submits a transaction with fair ordering protection.
    /// </summary>
    /// <param name="request">The fair transaction request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The transaction ID.</returns>
    Task<string> SubmitFairTransactionAsync(FairTransactionRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Analyzes fairness risk for a transaction.
    /// </summary>
    /// <param name="request">The transaction analysis request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The fairness analysis result.</returns>
    Task<FairnessAnalysisResult> AnalyzeFairnessRiskAsync(TransactionAnalysisRequest request, BlockchainType blockchainType);
}

/// <summary>
/// Enum representing the status of a computation.
/// </summary>
public enum ComputationStatus
{
    /// <summary>
    /// The computation is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// The computation is running.
    /// </summary>
    Running,

    /// <summary>
    /// The computation completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// The computation failed.
    /// </summary>
    Failed,

    /// <summary>
    /// The computation was cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Enum representing the health status of a service.
/// </summary>
public enum ServiceHealth
{
    /// <summary>
    /// The service is healthy and functioning normally.
    /// </summary>
    Healthy,

    /// <summary>
    /// The service is degraded but still functioning.
    /// </summary>
    Degraded,

    /// <summary>
    /// The service is unhealthy and not functioning properly.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// The service is not running.
    /// </summary>
    NotRunning
}

/// <summary>
/// Enum representing the supported blockchain types.
/// </summary>
public enum BlockchainType
{
    /// <summary>
    /// Neo N3 blockchain.
    /// </summary>
    NeoN3,

    /// <summary>
    /// NeoX blockchain (EVM-compatible).
    /// </summary>
    NeoX
}
