using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Tee.Host.Services;

/// <summary>
/// Core operations for the Enclave Manager.
/// </summary>
public partial class EnclaveManager
{
    /// <inheritdoc/>
    public Task<string> GetAttestationReportAsync(string challengeHex, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting attestation report with challenge: {ChallengeHex}", challengeHex);

            string jsonPayload = $@"{{
                ""challengeHex"": ""{challengeHex}""
            }}";

            return CallEnclaveFunctionAsync("getAttestationReport", jsonPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attestation report.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> SignDataAsync(string data, string privateKeyHex)
    {
        try
        {
            _logger.LogDebug("Signing data with private key");

            // Convert string data to bytes
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            // Convert hex private key to bytes
            byte[] keyBytes = Convert.FromHexString(privateKeyHex);

            // Sign the data
            byte[] signatureBytes = _enclaveWrapper.Sign(dataBytes, keyBytes);

            // Convert signature to hex string
            string signatureHex = Convert.ToHexString(signatureBytes).ToLowerInvariant();

            return Task.FromResult(signatureHex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing data.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> VerifySignatureAsync(string data, string signatureHex, string publicKeyHex)
    {
        try
        {
            _logger.LogDebug("Verifying signature with public key");

            // Convert string data to bytes
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            // Convert hex signature to bytes
            byte[] signatureBytes = Convert.FromHexString(signatureHex);

            // Convert hex public key to bytes
            byte[] keyBytes = Convert.FromHexString(publicKeyHex);

            // Verify the signature
            bool result = _enclaveWrapper.Verify(dataBytes, signatureBytes, keyBytes);

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signature.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> EncryptDataAsync(string data, string keyHex)
    {
        try
        {
            _logger.LogDebug("Encrypting data with key");

            // Convert string data to bytes
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            // Convert hex key to bytes
            byte[] keyBytes = Convert.FromHexString(keyHex);

            // Encrypt the data
            byte[] encryptedBytes = _enclaveWrapper.Encrypt(dataBytes, keyBytes);

            // Convert encrypted data to hex string
            string encryptedHex = Convert.ToHexString(encryptedBytes).ToLowerInvariant();

            return Task.FromResult(encryptedHex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting data.");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> DecryptDataAsync(string encryptedData, string keyHex)
    {
        try
        {
            _logger.LogDebug("Decrypting data with key");

            // Convert hex encrypted data to bytes
            byte[] encryptedBytes = Convert.FromHexString(encryptedData);

            // Convert hex key to bytes
            byte[] keyBytes = Convert.FromHexString(keyHex);

            // Decrypt the data
            byte[] decryptedBytes = _enclaveWrapper.Decrypt(encryptedBytes, keyBytes);

            // Convert decrypted data to string
            string decryptedData = Encoding.UTF8.GetString(decryptedBytes);

            return Task.FromResult(decryptedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting data.");
            throw;
        }
    }

    /// <inheritdoc/>
    [Obsolete("Prefer OracleFetchAndProcessDataAsync for oracle tasks, or a more specific enclave function.")]
    public Task<string> FetchDataFromUrlAsync(string url, string headersJson)
    {
        try
        {
            _logger.LogDebug("Fetching data from URL: {Url}", url);

            string jsonPayload = $@"{{
                ""url"": ""{url}"",
                ""headersJson"": {headersJson}
            }}";

            return CallEnclaveFunctionAsync("fetchDataFromUrl", jsonPayload, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching data from URL.");
            throw;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DestroyEnclaveAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the enclave manager.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the enclave manager.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _enclaveWrapper.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="EnclaveManager"/> class.
    /// </summary>
    ~EnclaveManager()
    {
        Dispose(false);
    }
}
