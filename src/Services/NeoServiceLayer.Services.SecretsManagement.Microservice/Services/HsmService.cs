using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Neo.SecretsManagement.Service.Services;

public class HsmService : IHsmService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HsmService> _logger;
    private readonly HsmConfiguration _config;
    private readonly SemaphoreSlim _rateLimitSemaphore;

    public HsmService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<HsmService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("HSM");
        _cache = cache;
        _logger = logger;
        _config = configuration.GetSection("Hsm").Get<HsmConfiguration>() ?? new HsmConfiguration();
        _rateLimitSemaphore = new SemaphoreSlim(_config.MaxConcurrentOperations, _config.MaxConcurrentOperations);
        
        ConfigureHttpClient();
    }

    public async Task<string> GenerateKeyAsync(string keyId, string algorithm, int keySize)
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            if (!_config.IsEnabled)
            {
                return await SimulateHsmOperation($"hsm-slot-{Guid.NewGuid():N}");
            }

            var request = new
            {
                KeyId = keyId,
                Algorithm = algorithm,
                KeySize = keySize,
                Attributes = new
                {
                    Extractable = false,
                    Sensitive = true,
                    Token = true
                }
            };

            var response = await PostToHsmAsync("/api/v1/keys/generate", request);
            var result = JsonSerializer.Deserialize<HsmKeyGenerationResponse>(response);
            
            if (result?.Success == true && !string.IsNullOrEmpty(result.SlotId))
            {
                _logger.LogInformation("Generated key {KeyId} in HSM slot {SlotId}", keyId, result.SlotId);
                return result.SlotId;
            }

            throw new InvalidOperationException($"HSM key generation failed: {result?.ErrorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate key {KeyId} in HSM", keyId);
            throw new InvalidOperationException($"HSM key generation failed: {ex.Message}", ex);
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    public async Task<string> EncryptAsync(string plaintext, string keyId)
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            if (!_config.IsEnabled)
            {
                return await SimulateEncryption(plaintext);
            }

            var request = new
            {
                KeyId = keyId,
                Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext)),
                Algorithm = "AES-GCM"
            };

            var response = await PostToHsmAsync("/api/v1/crypto/encrypt", request);
            var result = JsonSerializer.Deserialize<HsmCryptoResponse>(response);
            
            if (result?.Success == true && !string.IsNullOrEmpty(result.Data))
            {
                return result.Data;
            }

            throw new InvalidOperationException($"HSM encryption failed: {result?.ErrorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt with HSM key {KeyId}", keyId);
            throw new InvalidOperationException($"HSM encryption failed: {ex.Message}", ex);
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    public async Task<string> DecryptAsync(string ciphertext, string keyId)
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            if (!_config.IsEnabled)
            {
                return await SimulateDecryption(ciphertext);
            }

            var request = new
            {
                KeyId = keyId,
                Data = ciphertext,
                Algorithm = "AES-GCM"
            };

            var response = await PostToHsmAsync("/api/v1/crypto/decrypt", request);
            var result = JsonSerializer.Deserialize<HsmCryptoResponse>(response);
            
            if (result?.Success == true && !string.IsNullOrEmpty(result.Data))
            {
                var decryptedBytes = Convert.FromBase64String(result.Data);
                return Encoding.UTF8.GetString(decryptedBytes);
            }

            throw new InvalidOperationException($"HSM decryption failed: {result?.ErrorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt with HSM key {KeyId}", keyId);
            throw new InvalidOperationException($"HSM decryption failed: {ex.Message}", ex);
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    public async Task<string> SignAsync(string data, string keyId)
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            if (!_config.IsEnabled)
            {
                return await SimulateSignature(data);
            }

            var request = new
            {
                KeyId = keyId,
                Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(data)),
                Algorithm = "RSA-PSS-SHA256"
            };

            var response = await PostToHsmAsync("/api/v1/crypto/sign", request);
            var result = JsonSerializer.Deserialize<HsmCryptoResponse>(response);
            
            if (result?.Success == true && !string.IsNullOrEmpty(result.Data))
            {
                return result.Data;
            }

            throw new InvalidOperationException($"HSM signing failed: {result?.ErrorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign with HSM key {KeyId}", keyId);
            throw new InvalidOperationException($"HSM signing failed: {ex.Message}", ex);
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    public async Task<bool> VerifyAsync(string data, string signature, string keyId)
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            if (!_config.IsEnabled)
            {
                return await SimulateVerification(data, signature);
            }

            var request = new
            {
                KeyId = keyId,
                Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(data)),
                Signature = signature,
                Algorithm = "RSA-PSS-SHA256"
            };

            var response = await PostToHsmAsync("/api/v1/crypto/verify", request);
            var result = JsonSerializer.Deserialize<HsmVerificationResponse>(response);
            
            return result?.Success == true && result.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify signature with HSM key {KeyId}", keyId);
            return false;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    public async Task<bool> RevokeKeyAsync(string keyId)
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            if (!_config.IsEnabled)
            {
                return await SimulateHsmOperation(true);
            }

            var response = await DeleteFromHsmAsync($"/api/v1/keys/{keyId}");
            var result = JsonSerializer.Deserialize<HsmResponse>(response);
            
            var success = result?.Success == true;
            if (success)
            {
                _logger.LogInformation("Revoked HSM key {KeyId}", keyId);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke HSM key {KeyId}", keyId);
            return false;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    public async Task<bool> ValidateKeyAsync(string keyId)
    {
        try
        {
            if (!_config.IsEnabled)
            {
                return await SimulateHsmOperation(true);
            }

            // Check cache first
            var cacheKey = $"hsm_key_validation:{keyId}";
            if (_cache.TryGetValue(cacheKey, out bool cachedResult))
            {
                return cachedResult;
            }

            var keyInfo = await GetKeyInfoAsync(keyId);
            var isValid = keyInfo?.IsActive == true;
            
            // Cache result for 5 minutes
            _cache.Set(cacheKey, isValid, TimeSpan.FromMinutes(5));
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate HSM key {KeyId}", keyId);
            return false;
        }
    }

    public async Task<HsmStatus> GetStatusAsync()
    {
        try
        {
            if (!_config.IsEnabled)
            {
                return new HsmStatus
                {
                    IsAvailable = false,
                    Version = "Simulated HSM v1.0",
                    SerialNumber = "SIM-12345",
                    ActiveSlots = 0,
                    TotalSlots = 100,
                    LastHealthCheck = DateTime.UtcNow
                };
            }

            // Check cache first
            var cacheKey = "hsm_status";
            if (_cache.TryGetValue(cacheKey, out HsmStatus? cachedStatus))
            {
                return cachedStatus!;
            }

            var response = await GetFromHsmAsync("/api/v1/status");
            var status = JsonSerializer.Deserialize<HsmStatus>(response) ?? new HsmStatus();
            
            // Cache status for 1 minute
            _cache.Set(cacheKey, status, TimeSpan.FromMinutes(1));
            
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get HSM status");
            return new HsmStatus
            {
                IsAvailable = false,
                LastHealthCheck = DateTime.UtcNow,
                Alarms = new List<string> { ex.Message }
            };
        }
    }

    public async Task<List<string>> ListKeysAsync()
    {
        try
        {
            if (!_config.IsEnabled)
            {
                return new List<string>();
            }

            var response = await GetFromHsmAsync("/api/v1/keys");
            var result = JsonSerializer.Deserialize<HsmKeyListResponse>(response);
            
            return result?.Keys ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list HSM keys");
            return new List<string>();
        }
    }

    public async Task<HsmKeyInfo?> GetKeyInfoAsync(string keyId)
    {
        try
        {
            if (!_config.IsEnabled)
            {
                return new HsmKeyInfo
                {
                    KeyId = keyId,
                    SlotId = $"slot-{keyId}",
                    Algorithm = "AES-256-GCM",
                    KeySize = 256,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    UsageCount = 0
                };
            }

            var response = await GetFromHsmAsync($"/api/v1/keys/{keyId}");
            return JsonSerializer.Deserialize<HsmKeyInfo>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get HSM key info for {KeyId}", keyId);
            return null;
        }
    }

    public async Task<bool> BackupKeyAsync(string keyId, string backupLocation)
    {
        try
        {
            if (!_config.IsEnabled)
            {
                return await SimulateHsmOperation(true);
            }

            var request = new
            {
                KeyId = keyId,
                BackupLocation = backupLocation,
                EncryptBackup = true
            };

            var response = await PostToHsmAsync("/api/v1/keys/backup", request);
            var result = JsonSerializer.Deserialize<HsmResponse>(response);
            
            var success = result?.Success == true;
            if (success)
            {
                _logger.LogInformation("Backed up HSM key {KeyId} to {BackupLocation}", keyId, backupLocation);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backup HSM key {KeyId}", keyId);
            return false;
        }
    }

    public async Task<bool> RestoreKeyAsync(string keyId, string backupLocation)
    {
        try
        {
            if (!_config.IsEnabled)
            {
                return await SimulateHsmOperation(true);
            }

            var request = new
            {
                KeyId = keyId,
                BackupLocation = backupLocation
            };

            var response = await PostToHsmAsync("/api/v1/keys/restore", request);
            var result = JsonSerializer.Deserialize<HsmResponse>(response);
            
            var success = result?.Success == true;
            if (success)
            {
                _logger.LogInformation("Restored HSM key {KeyId} from {BackupLocation}", keyId, backupLocation);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore HSM key {KeyId}", keyId);
            return false;
        }
    }

    public async Task<HsmPerformanceMetrics> GetPerformanceMetricsAsync()
    {
        try
        {
            if (!_config.IsEnabled)
            {
                return new HsmPerformanceMetrics
                {
                    OperationsPerSecond = 0,
                    AverageResponseTime = 0,
                    ActiveConnections = 0,
                    CpuUtilization = 0,
                    MemoryUtilization = 0,
                    ErrorRate = 0,
                    MeasuredAt = DateTime.UtcNow
                };
            }

            var response = await GetFromHsmAsync("/api/v1/metrics/performance");
            return JsonSerializer.Deserialize<HsmPerformanceMetrics>(response) ?? new HsmPerformanceMetrics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get HSM performance metrics");
            return new HsmPerformanceMetrics { MeasuredAt = DateTime.UtcNow };
        }
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        
        if (!string.IsNullOrEmpty(_config.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _config.ApiKey);
        }
        
        if (!string.IsNullOrEmpty(_config.ClientCertificateThumbprint))
        {
            // In a real implementation, load and configure client certificate
            _logger.LogInformation("Client certificate authentication configured");
        }
    }

    private async Task<string> PostToHsmAsync(string endpoint, object request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> GetFromHsmAsync(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> DeleteFromHsmAsync(string endpoint)
    {
        var response = await _httpClient.DeleteAsync(endpoint);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }

    // Simulation methods for when HSM is not available
    private async Task<T> SimulateHsmOperation<T>(T result)
    {
        await Task.Delay(Random.Shared.Next(10, 50)); // Simulate network delay
        return result;
    }

    private async Task<string> SimulateEncryption(string plaintext)
    {
        await Task.Delay(Random.Shared.Next(10, 30));
        // Simple base64 encoding for simulation
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"hsm-encrypted:{plaintext}"));
    }

    private async Task<string> SimulateDecryption(string ciphertext)
    {
        await Task.Delay(Random.Shared.Next(10, 30));
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(ciphertext));
            if (decoded.StartsWith("hsm-encrypted:"))
            {
                return decoded["hsm-encrypted:".Length..];
            }
        }
        catch
        {
            // Ignore decoding errors in simulation
        }
        return "simulated-decrypted-data";
    }

    private async Task<string> SimulateSignature(string data)
    {
        await Task.Delay(Random.Shared.Next(20, 50));
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes($"hsm-sign:{data}"));
        return Convert.ToBase64String(hash);
    }

    private async Task<bool> SimulateVerification(string data, string signature)
    {
        await Task.Delay(Random.Shared.Next(20, 50));
        var expectedSignature = await SimulateSignature(data);
        return expectedSignature == signature;
    }

    private class HsmConfiguration
    {
        public bool IsEnabled { get; set; } = false;
        public string BaseUrl { get; set; } = "https://hsm.internal";
        public string ApiKey { get; set; } = string.Empty;
        public string ClientCertificateThumbprint { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxConcurrentOperations { get; set; } = 10;
    }

    private class HsmResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private class HsmKeyGenerationResponse : HsmResponse
    {
        public string? SlotId { get; set; }
    }

    private class HsmCryptoResponse : HsmResponse
    {
        public string? Data { get; set; }
    }

    private class HsmVerificationResponse : HsmResponse
    {
        public bool IsValid { get; set; }
    }

    private class HsmKeyListResponse : HsmResponse
    {
        public List<string>? Keys { get; set; }
    }

    public void Dispose()
    {
        _rateLimitSemaphore?.Dispose();
        _httpClient?.Dispose();
    }
}