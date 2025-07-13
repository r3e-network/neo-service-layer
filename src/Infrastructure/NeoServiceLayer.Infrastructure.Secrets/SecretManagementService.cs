using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.Secrets;

public interface ISecretManagementService
{
    Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
    Task<T> GetSecretAsync<T>(string secretName, CancellationToken cancellationToken = default) where T : class;
    Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);
    Task SetSecretAsync<T>(string secretName, T secretValue, CancellationToken cancellationToken = default) where T : class;
    Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);
    Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetAllSecretsAsync(CancellationToken cancellationToken = default);
    Task RotateSecretAsync(string secretName, CancellationToken cancellationToken = default);
}

public class SecretManagementService : ISecretManagementService
{
    private readonly ISecretProvider _secretProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SecretManagementService> _logger;
    private readonly SecretManagementOptions _options;

    public SecretManagementService(
        ISecretProvider secretProvider,
        IMemoryCache cache,
        ILogger<SecretManagementService> logger,
        IOptions<SecretManagementOptions> options)
    {
        _secretProvider = secretProvider;
        _cache = cache;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
            throw new ArgumentNullException(nameof(secretName));

        var cacheKey = GetCacheKey(secretName);

        // Try to get from cache first
        if (_cache.TryGetValue<string>(cacheKey, out var cachedValue))
        {
            _logger.LogDebug("Secret {SecretName} retrieved from cache", secretName);
            return cachedValue;
        }

        // Get from provider
        try
        {
            var secretValue = await _secretProvider.GetSecretAsync(secretName, cancellationToken);

            if (!string.IsNullOrEmpty(secretValue))
            {
                // Cache the secret
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheExpirationMinutes),
                    SlidingExpiration = TimeSpan.FromMinutes(_options.CacheSlidingExpirationMinutes)
                };

                _cache.Set(cacheKey, secretValue, cacheOptions);
                _logger.LogDebug("Secret {SecretName} cached for {Minutes} minutes", secretName, _options.CacheExpirationMinutes);
            }

            return secretValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret {SecretName}", secretName);
            throw new SecretManagementException($"Failed to retrieve secret '{secretName}'", ex);
        }
    }

    public async Task<T> GetSecretAsync<T>(string secretName, CancellationToken cancellationToken = default) where T : class
    {
        var secretValue = await GetSecretAsync(secretName, cancellationToken);
        
        if (string.IsNullOrEmpty(secretValue))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(secretValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize secret {SecretName} to type {Type}", secretName, typeof(T).Name);
            throw new SecretManagementException($"Failed to deserialize secret '{secretName}'", ex);
        }
    }

    public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
            throw new ArgumentNullException(nameof(secretName));

        if (string.IsNullOrWhiteSpace(secretValue))
            throw new ArgumentNullException(nameof(secretValue));

        try
        {
            await _secretProvider.SetSecretAsync(secretName, secretValue, cancellationToken);
            
            // Invalidate cache
            var cacheKey = GetCacheKey(secretName);
            _cache.Remove(cacheKey);
            
            _logger.LogInformation("Secret {SecretName} has been updated", secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secret {SecretName}", secretName);
            throw new SecretManagementException($"Failed to set secret '{secretName}'", ex);
        }
    }

    public async Task SetSecretAsync<T>(string secretName, T secretValue, CancellationToken cancellationToken = default) where T : class
    {
        var jsonValue = JsonSerializer.Serialize(secretValue);
        await SetSecretAsync(secretName, jsonValue, cancellationToken);
    }

    public async Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
            throw new ArgumentNullException(nameof(secretName));

        try
        {
            await _secretProvider.DeleteSecretAsync(secretName, cancellationToken);
            
            // Invalidate cache
            var cacheKey = GetCacheKey(secretName);
            _cache.Remove(cacheKey);
            
            _logger.LogInformation("Secret {SecretName} has been deleted", secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret {SecretName}", secretName);
            throw new SecretManagementException($"Failed to delete secret '{secretName}'", ex);
        }
    }

    public async Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            var secret = await GetSecretAsync(secretName, cancellationToken);
            return !string.IsNullOrEmpty(secret);
        }
        catch
        {
            return false;
        }
    }

    public async Task<Dictionary<string, string>> GetAllSecretsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _secretProvider.GetAllSecretsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all secrets");
            throw new SecretManagementException("Failed to retrieve all secrets", ex);
        }
    }

    public async Task RotateSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
            throw new ArgumentNullException(nameof(secretName));

        try
        {
            // Generate new secret value based on type
            var newSecretValue = GenerateNewSecretValue(secretName);
            
            // Store the new secret
            await SetSecretAsync(secretName, newSecretValue, cancellationToken);
            
            _logger.LogInformation("Secret {SecretName} has been rotated", secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate secret {SecretName}", secretName);
            throw new SecretManagementException($"Failed to rotate secret '{secretName}'", ex);
        }
    }

    private string GetCacheKey(string secretName) => $"secret:{secretName}";

    private string GenerateNewSecretValue(string secretName)
    {
        // Determine the type of secret and generate accordingly
        if (secretName.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            return GenerateSecurePassword();
        }
        else if (secretName.Contains("key", StringComparison.OrdinalIgnoreCase) || 
                 secretName.Contains("token", StringComparison.OrdinalIgnoreCase))
        {
            return GenerateSecureToken();
        }
        else
        {
            // Default to a secure random string
            return GenerateSecureToken();
        }
    }

    private string GenerateSecurePassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?";
        var random = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(random);
        }

        var password = new StringBuilder(32);
        for (int i = 0; i < 32; i++)
        {
            password.Append(chars[random[i] % chars.Length]);
        }

        return password.ToString();
    }

    private string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }
}

// Secret Provider Interface
public interface ISecretProvider
{
    Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
    Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);
    Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetAllSecretsAsync(CancellationToken cancellationToken = default);
}

// Azure Key Vault Provider
public class AzureKeyVaultSecretProvider : ISecretProvider
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<AzureKeyVaultSecretProvider> _logger;

    public AzureKeyVaultSecretProvider(IConfiguration configuration, ILogger<AzureKeyVaultSecretProvider> logger)
    {
        _logger = logger;

        var keyVaultUri = configuration["AzureKeyVault:Uri"];
        if (string.IsNullOrEmpty(keyVaultUri))
        {
            throw new InvalidOperationException("Azure Key Vault URI is not configured");
        }

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = false,
            ExcludeManagedIdentityCredential = false,
            ExcludeSharedTokenCacheCredential = true,
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeAzureCliCredential = false,
            ExcludeInteractiveBrowserCredential = true
        });

        _secretClient = new SecretClient(new Uri(keyVaultUri), credential);
    }

    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            return response.Value.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret {SecretName} not found in Azure Key Vault", secretName);
            return null;
        }
    }

    public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
    {
        await _secretClient.SetSecretAsync(secretName, secretValue, cancellationToken);
    }

    public async Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        await _secretClient.StartDeleteSecretAsync(secretName, cancellationToken);
    }

    public async Task<Dictionary<string, string>> GetAllSecretsAsync(CancellationToken cancellationToken = default)
    {
        var secrets = new Dictionary<string, string>();

        await foreach (var secretProperties in _secretClient.GetPropertiesOfSecretsAsync(cancellationToken))
        {
            if (secretProperties.Enabled == true)
            {
                var secret = await _secretClient.GetSecretAsync(secretProperties.Name, cancellationToken: cancellationToken);
                secrets[secretProperties.Name] = secret.Value.Value;
            }
        }

        return secrets;
    }
}

// Local File Provider (for development only)
public class LocalFileSecretProvider : ISecretProvider
{
    private readonly string _secretsPath;
    private readonly ILogger<LocalFileSecretProvider> _logger;

    public LocalFileSecretProvider(IConfiguration configuration, ILogger<LocalFileSecretProvider> logger)
    {
        _secretsPath = configuration["LocalSecrets:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "secrets");
        _logger = logger;

        if (!Directory.Exists(_secretsPath))
        {
            Directory.CreateDirectory(_secretsPath);
        }
    }

    public Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        var filePath = GetSecretFilePath(secretName);
        
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Secret file {FilePath} not found", filePath);
            return Task.FromResult<string>(null);
        }

        return File.ReadAllTextAsync(filePath, cancellationToken);
    }

    public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
    {
        var filePath = GetSecretFilePath(secretName);
        await File.WriteAllTextAsync(filePath, secretValue, cancellationToken);
        
        // Set file permissions (Unix-like systems only)
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            File.SetUnixFileMode(filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    public Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        var filePath = GetSecretFilePath(secretName);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public Task<Dictionary<string, string>> GetAllSecretsAsync(CancellationToken cancellationToken = default)
    {
        var secrets = new Dictionary<string, string>();
        
        foreach (var file in Directory.GetFiles(_secretsPath, "*.secret"))
        {
            var secretName = Path.GetFileNameWithoutExtension(file);
            secrets[secretName] = File.ReadAllText(file);
        }

        return Task.FromResult(secrets);
    }

    private string GetSecretFilePath(string secretName)
    {
        var safeFileName = string.Join("_", secretName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_secretsPath, $"{safeFileName}.secret");
    }
}

// Configuration
public class SecretManagementOptions
{
    public string Provider { get; set; } = "AzureKeyVault";
    public int CacheExpirationMinutes { get; set; } = 60;
    public int CacheSlidingExpirationMinutes { get; set; } = 15;
    public bool EnableAutoRotation { get; set; } = false;
    public int RotationIntervalDays { get; set; } = 90;
}

// Exception
public class SecretManagementException : Exception
{
    public SecretManagementException(string message) : base(message) { }
    public SecretManagementException(string message, Exception innerException) : base(message, innerException) { }
}

// Extension methods
public static class SecretManagementExtensions
{
    public static IServiceCollection AddSecretManagement(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SecretManagementOptions>(configuration.GetSection("SecretManagement"));

        var provider = configuration["SecretManagement:Provider"] ?? "AzureKeyVault";

        switch (provider.ToLower())
        {
            case "azurekeyvault":
                services.AddSingleton<ISecretProvider, AzureKeyVaultSecretProvider>();
                break;
            case "local":
                services.AddSingleton<ISecretProvider, LocalFileSecretProvider>();
                break;
            default:
                throw new NotSupportedException($"Secret provider '{provider}' is not supported");
        }

        services.AddSingleton<ISecretManagementService, SecretManagementService>();
        services.AddHostedService<SecretRotationService>();

        return services;
    }
}

// Background service for automatic secret rotation
public class SecretRotationService : BackgroundService
{
    private readonly ISecretManagementService _secretManagement;
    private readonly ILogger<SecretRotationService> _logger;
    private readonly SecretManagementOptions _options;

    public SecretRotationService(
        ISecretManagementService secretManagement,
        ILogger<SecretRotationService> logger,
        IOptions<SecretManagementOptions> options)
    {
        _secretManagement = secretManagement;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableAutoRotation)
        {
            _logger.LogInformation("Automatic secret rotation is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RotateSecretsAsync(stoppingToken);
                
                // Wait for the next rotation interval
                await Task.Delay(TimeSpan.FromDays(_options.RotationIntervalDays), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during secret rotation");
                
                // Wait before retrying
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private async Task RotateSecretsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting automatic secret rotation");

        var secretsToRotate = new[]
        {
            "JWT_SECRET_KEY",
            "DB_PASSWORD",
            "REDIS_PASSWORD",
            "RABBITMQ_PASSWORD",
            "API_KEY"
        };

        foreach (var secretName in secretsToRotate)
        {
            try
            {
                await _secretManagement.RotateSecretAsync(secretName, cancellationToken);
                _logger.LogInformation("Successfully rotated secret {SecretName}", secretName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rotate secret {SecretName}", secretName);
            }
        }

        _logger.LogInformation("Completed automatic secret rotation");
    }
}