using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Production SGX enclave wrapper implementation.
/// This class is specifically designed for hardware SGX environments.
/// </summary>
public class ProductionSGXEnclaveWrapper : IEnclaveWrapper
{
    private readonly ILogger<ProductionSGXEnclaveWrapper> _logger;
    private readonly OcclumEnclaveWrapper _occlumWrapper;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductionSGXEnclaveWrapper"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ProductionSGXEnclaveWrapper(ILogger<ProductionSGXEnclaveWrapper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create an Occlum wrapper with the same logger type cast
        var occlumLogger = logger as ILogger<OcclumEnclaveWrapper> ??
                          new LoggerWrapper<OcclumEnclaveWrapper>(logger);
        _occlumWrapper = new OcclumEnclaveWrapper(occlumLogger);
        _disposed = false;
    }

    /// <inheritdoc/>
    public bool Initialize()
    {
        _logger.LogInformation("Initializing production SGX enclave...");
        return _occlumWrapper.Initialize();
    }

    /// <inheritdoc/>
    public string ExecuteJavaScript(string functionCode, string args)
    {
        return _occlumWrapper.ExecuteJavaScript(functionCode, args);
    }

    /// <inheritdoc/>
    public string ExecuteComputation(string computationId, string computationCode, string parameters)
    {
        return _occlumWrapper.ExecuteComputation(computationId, computationCode, parameters);
    }

    /// <inheritdoc/>
    public string GetData(string dataSource, string dataPath)
    {
        return _occlumWrapper.GetData(dataSource, dataPath);
    }

    /// <inheritdoc/>
    public string FetchOracleData(string url, string? headers = null, string? processingScript = null, string? outputFormat = "json")
    {
        return _occlumWrapper.FetchOracleData(url, headers, processingScript, outputFormat);
    }

    /// <inheritdoc/>
    public int GenerateRandom(int min, int max)
    {
        return _occlumWrapper.GenerateRandom(min, max);
    }

    /// <inheritdoc/>
    public byte[] GenerateRandomBytes(int length)
    {
        return _occlumWrapper.GenerateRandomBytes(length);
    }

    /// <inheritdoc/>
    public byte[] Encrypt(byte[] data, byte[] key)
    {
        return _occlumWrapper.Encrypt(data, key);
    }

    /// <inheritdoc/>
    public byte[] Decrypt(byte[] data, byte[] key)
    {
        return _occlumWrapper.Decrypt(data, key);
    }

    /// <inheritdoc/>
    public byte[] Sign(byte[] data, byte[] key)
    {
        return _occlumWrapper.Sign(data, key);
    }

    /// <inheritdoc/>
    public bool Verify(byte[] data, byte[] signature, byte[] key)
    {
        return _occlumWrapper.Verify(data, signature, key);
    }

    /// <inheritdoc/>
    public string GenerateKey(string keyId, string keyType, string keyUsage, bool exportable, string description)
    {
        return _occlumWrapper.GenerateKey(keyId, keyType, keyUsage, exportable, description);
    }

    /// <inheritdoc/>
    public string StoreData(string key, byte[] data, string encryptionKey, bool compress = false)
    {
        return _occlumWrapper.StoreData(key, data, encryptionKey, compress);
    }

    /// <inheritdoc/>
    public byte[] RetrieveData(string key, string encryptionKey)
    {
        return _occlumWrapper.RetrieveData(key, encryptionKey);
    }

    /// <inheritdoc/>
    public string DeleteData(string key)
    {
        return _occlumWrapper.DeleteData(key);
    }

    /// <inheritdoc/>
    public string GetStorageMetadata(string key)
    {
        return _occlumWrapper.GetStorageMetadata(key);
    }

    /// <inheritdoc/>
    public string TrainAIModel(string modelId, string modelType, double[] trainingData, string parameters = "{}")
    {
        return _occlumWrapper.TrainAIModel(modelId, modelType, trainingData, parameters);
    }

    /// <inheritdoc/>
    public (double[] predictions, string metadata) PredictWithAIModel(string modelId, double[] inputData)
    {
        return _occlumWrapper.PredictWithAIModel(modelId, inputData);
    }

    /// <inheritdoc/>
    public string CreateAbstractAccount(string accountId, string accountData)
    {
        return _occlumWrapper.CreateAbstractAccount(accountId, accountData);
    }

    /// <inheritdoc/>
    public string SignAbstractAccountTransaction(string accountId, string transactionData)
    {
        return _occlumWrapper.SignAbstractAccountTransaction(accountId, transactionData);
    }

    /// <inheritdoc/>
    public string AddAbstractAccountGuardian(string accountId, string guardianData)
    {
        return _occlumWrapper.AddAbstractAccountGuardian(accountId, guardianData);
    }

    /// <inheritdoc/>
    public string GetAttestationReport()
    {
        return _occlumWrapper.GetAttestationReport();
    }

    /// <inheritdoc/>
    public byte[] SealData(byte[] data)
    {
        return _occlumWrapper.SealData(data);
    }

    /// <inheritdoc/>
    public byte[] UnsealData(byte[] sealedData)
    {
        return _occlumWrapper.UnsealData(sealedData);
    }

    /// <inheritdoc/>
    public long GetTrustedTime()
    {
        return _occlumWrapper.GetTrustedTime();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _occlumWrapper?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Logger wrapper to adapt between different logger types.
/// </summary>
/// <typeparam name="T">The target logger category type.</typeparam>
internal class LoggerWrapper<T> : ILogger<T>
{
    private readonly ILogger _logger;

    public LoggerWrapper(ILogger logger)
    {
        _logger = logger;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _logger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}
