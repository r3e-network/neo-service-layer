using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Implementation of enclave storage service.
/// </summary>
public class EnclaveStorageService : IEnclaveStorageService
{
    private readonly IEnclaveWrapper _enclaveWrapper;
    private readonly ILogger<EnclaveStorageService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveStorageService"/> class.
    /// </summary>
    /// <param name="enclaveWrapper">The enclave wrapper.</param>
    /// <param name="logger">The logger.</param>
    public EnclaveStorageService(IEnclaveWrapper enclaveWrapper, ILogger<EnclaveStorageService> logger)
    {
        ArgumentNullException.ThrowIfNull(enclaveWrapper);
        ArgumentNullException.ThrowIfNull(logger);

        _enclaveWrapper = enclaveWrapper;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<string> StoreAsync(string key, byte[] data, string encryptionKey, bool compress = false)
    {
        try
        {
            _logger.LogDebug("Storing data with key: {Key}", key);
            string result = _enclaveWrapper.StoreData(key, data, encryptionKey, compress);
            _logger.LogDebug("Data stored successfully with key: {Key}", key);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing data with key: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<byte[]> RetrieveAsync(string key, string encryptionKey)
    {
        try
        {
            _logger.LogDebug("Retrieving data with key: {Key}", key);
            byte[] result = _enclaveWrapper.RetrieveData(key, encryptionKey);
            _logger.LogDebug("Data retrieved successfully with key: {Key}", key);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data with key: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> DeleteAsync(string key)
    {
        try
        {
            _logger.LogDebug("Deleting data with key: {Key}", key);
            string result = _enclaveWrapper.DeleteData(key);
            _logger.LogDebug("Data deleted successfully with key: {Key}", key);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting data with key: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> GetMetadataAsync(string key)
    {
        try
        {
            _logger.LogDebug("Getting metadata for key: {Key}", key);
            string result = _enclaveWrapper.GetStorageMetadata(key);
            _logger.LogDebug("Metadata retrieved successfully for key: {Key}", key);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata for key: {Key}", key);
            throw;
        }
    }
}
