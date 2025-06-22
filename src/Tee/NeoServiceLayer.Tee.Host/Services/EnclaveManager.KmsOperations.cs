using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host.Services;

/// <summary>
/// KMS operations for the Enclave Manager.
/// </summary>
public partial class EnclaveManager
{
    /// <inheritdoc/>
    public Task<string> KmsGenerateKeyAsync(string keyId, string keyType, string keyUsage, bool exportable, string description, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating key with ID: {KeyId}, type: {KeyType}", keyId, keyType);

            // Use the real enclave key generation function
            string result = _enclaveWrapper.GenerateKey(keyId, keyType, keyUsage, exportable, description);

            _logger.LogDebug("Key generated successfully with ID: {KeyId}", keyId);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating key.");
            throw;
        }
    }

    /// <summary>
    /// Overload without CancellationToken for easier testing.
    /// </summary>
    public Task<string> KmsGenerateKeyAsync(string keyId, string keyType, string keyUsage, bool exportable, string description)
    {
        return KmsGenerateKeyAsync(keyId, keyType, keyUsage, exportable, description, CancellationToken.None);
    }

    /// <inheritdoc/>
    public Task<string> KmsGetKeyMetadataAsync(string keyId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting key metadata for key ID: {KeyId}", keyId);

            string jsonPayload = $@"{{
                ""keyId"": ""{keyId}""
            }}";

            return CallEnclaveFunctionAsync("kmsGetKeyMetadata", jsonPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting key metadata.");
            throw;
        }
    }

    /// <summary>
    /// Overload without CancellationToken for easier testing.
    /// </summary>
    public Task<string> KmsGetKeyMetadataAsync(string keyId)
    {
        return KmsGetKeyMetadataAsync(keyId, CancellationToken.None);
    }

    /// <inheritdoc/>
    public Task<string> KmsListKeysAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Listing keys with skip: {Skip}, take: {Take}", skip, take);

            string jsonPayload = $@"{{
                ""skip"": {skip},
                ""take"": {take}
            }}";

            return CallEnclaveFunctionAsync("kmsListKeys", jsonPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing keys.");
            throw;
        }
    }

    /// <summary>
    /// Overload without CancellationToken for easier testing.
    /// </summary>
    public Task<string> KmsListKeysAsync(int skip, int take)
    {
        return KmsListKeysAsync(skip, take, CancellationToken.None);
    }

    /// <inheritdoc/>
    public Task<string> KmsSignDataAsync(string keyId, string dataHex, string signingAlgorithm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Signing data with key ID: {KeyId}", keyId);

            string jsonPayload = $@"{{
                ""keyId"": ""{keyId}"",
                ""dataHex"": ""{dataHex}"",
                ""signingAlgorithm"": ""{signingAlgorithm}""
            }}";

            return CallEnclaveFunctionAsync("kmsSignData", jsonPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing data.");
            throw;
        }
    }

    /// <summary>
    /// Overload without CancellationToken for easier testing.
    /// </summary>
    public Task<string> KmsSignDataAsync(string keyId, string dataHex, string signingAlgorithm)
    {
        return KmsSignDataAsync(keyId, dataHex, signingAlgorithm, CancellationToken.None);
    }

    /// <inheritdoc/>
    public Task<bool> KmsVerifySignatureAsync(string keyIdOrPublicKeyHex, string dataHex, string signatureHex, string signingAlgorithm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Verifying signature with key ID or public key: {KeyIdOrPublicKeyHex}", keyIdOrPublicKeyHex);

            string jsonPayload = $@"{{
                ""keyIdOrPublicKeyHex"": ""{keyIdOrPublicKeyHex}"",
                ""dataHex"": ""{dataHex}"",
                ""signatureHex"": ""{signatureHex}"",
                ""signingAlgorithm"": ""{signingAlgorithm}""
            }}";

            string result = CallEnclaveFunctionAsync("kmsVerifySignature", jsonPayload, cancellationToken).Result;
            return Task.FromResult(bool.Parse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signature.");
            throw;
        }
    }

    /// <summary>
    /// Overload without CancellationToken for easier testing.
    /// </summary>
    public Task<bool> KmsVerifySignatureAsync(string keyIdOrPublicKeyHex, string dataHex, string signatureHex, string signingAlgorithm)
    {
        return KmsVerifySignatureAsync(keyIdOrPublicKeyHex, dataHex, signatureHex, signingAlgorithm, CancellationToken.None);
    }

    /// <inheritdoc/>
    public Task<string> KmsEncryptDataAsync(string keyIdOrPublicKeyHex, string dataHex, string encryptionAlgorithm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Encrypting data with key ID or public key: {KeyIdOrPublicKeyHex}", keyIdOrPublicKeyHex);

            string jsonPayload = $@"{{
                ""keyIdOrPublicKeyHex"": ""{keyIdOrPublicKeyHex}"",
                ""dataHex"": ""{dataHex}"",
                ""encryptionAlgorithm"": ""{encryptionAlgorithm}""
            }}";

            return CallEnclaveFunctionAsync("kmsEncryptData", jsonPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting data.");
            throw;
        }
    }

    /// <summary>
    /// Overload without CancellationToken for easier testing.
    /// </summary>
    public Task<string> KmsEncryptDataAsync(string keyIdOrPublicKeyHex, string dataHex, string encryptionAlgorithm)
    {
        return KmsEncryptDataAsync(keyIdOrPublicKeyHex, dataHex, encryptionAlgorithm, CancellationToken.None);
    }

    /// <inheritdoc/>
    public Task<string> KmsDecryptDataAsync(string keyId, string encryptedDataHex, string encryptionAlgorithm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Decrypting data with key ID: {KeyId}", keyId);

            string jsonPayload = $@"{{
                ""keyId"": ""{keyId}"",
                ""encryptedDataHex"": ""{encryptedDataHex}"",
                ""encryptionAlgorithm"": ""{encryptionAlgorithm}""
            }}";

            return CallEnclaveFunctionAsync("kmsDecryptData", jsonPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting data.");
            throw;
        }
    }

    /// <summary>
    /// Overload without CancellationToken for easier testing.
    /// </summary>
    public Task<string> KmsDecryptDataAsync(string keyId, string encryptedDataHex, string encryptionAlgorithm)
    {
        return KmsDecryptDataAsync(keyId, encryptedDataHex, encryptionAlgorithm, CancellationToken.None);
    }

    /// <inheritdoc/>
    public Task<bool> KmsDeleteKeyAsync(string keyId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting key with ID: {KeyId}", keyId);

            string jsonPayload = $@"{{
                ""keyId"": ""{keyId}""
            }}";

            string result = CallEnclaveFunctionAsync("kmsDeleteKey", jsonPayload, cancellationToken).Result;
            return Task.FromResult(bool.Parse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting key.");
            throw;
        }
    }

    /// <summary>
    /// Overload without CancellationToken for easier testing.
    /// </summary>
    public Task<bool> KmsDeleteKeyAsync(string keyId)
    {
        return KmsDeleteKeyAsync(keyId, CancellationToken.None);
    }
}
