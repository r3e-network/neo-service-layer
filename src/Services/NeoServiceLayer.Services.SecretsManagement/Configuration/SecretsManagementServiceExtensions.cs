using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.SecretsManagement;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for configuring the Secrets Management service.
/// </summary>
public static class SecretsManagementServiceExtensions
{
    /// <summary>
    /// Adds the Secrets Management service to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSecretsManagement(
        this IServiceCollection services,
        Action<SecretsManagementOptions>? configureOptions = null)
    {
        // Configure options
        var options = new SecretsManagementOptions();
        configureOptions?.Invoke(options);

        services.AddSingleton(options);

        // Register external providers based on configuration
        if (options.EnableAzureKeyVault)
        {
            services.AddSingleton<IExternalSecretProvider, AzureKeyVaultProvider>();
        }

        if (options.EnableAwsSecretsManager)
        {
            services.AddSingleton<IExternalSecretProvider, AwsSecretsManagerProvider>();
        }

        // Register the main service
        services.AddSingleton<ISecretsManagementService, SecretsManagementService>();
        services.AddSingleton<ISecretsManager>(provider =>
            (ISecretsManager)provider.GetRequiredService<ISecretsManagementService>());

        return services;
    }

    /// <summary>
    /// Adds Azure Key Vault as an external secret provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="keyVaultUrl">The Azure Key Vault URL.</param>
    /// <param name="tenantId">Optional tenant ID for service principal authentication.</param>
    /// <param name="clientId">Optional client ID for service principal authentication.</param>
    /// <param name="clientSecret">Optional client secret for service principal authentication.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureKeyVault(
        this IServiceCollection services,
        string keyVaultUrl,
        string? tenantId = null,
        string? clientId = null,
        string? clientSecret = null)
    {
        services.AddSingleton<IExternalSecretProvider>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<AzureKeyVaultProvider>>();
            var azureProvider = new AzureKeyVaultProvider(logger);

            var configuration = new Dictionary<string, string>
            {
                ["KeyVaultUrl"] = keyVaultUrl
            };

            if (!string.IsNullOrEmpty(tenantId))
                configuration["TenantId"] = tenantId;
            if (!string.IsNullOrEmpty(clientId))
                configuration["ClientId"] = clientId;
            if (!string.IsNullOrEmpty(clientSecret))
                configuration["ClientSecret"] = clientSecret;

            azureProvider.ConfigureAsync(configuration).GetAwaiter().GetResult();
            return azureProvider;
        });

        return services;
    }

    /// <summary>
    /// Adds AWS Secrets Manager as an external secret provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="region">The AWS region.</param>
    /// <param name="serviceUrl">Optional service URL for local testing.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAwsSecretsManager(
        this IServiceCollection services,
        string region,
        string? serviceUrl = null)
    {
        services.AddSingleton<IExternalSecretProvider>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<AwsSecretsManagerProvider>>();
            var awsProvider = new AwsSecretsManagerProvider(logger);

            var configuration = new Dictionary<string, string>
            {
                ["Region"] = region
            };

            if (!string.IsNullOrEmpty(serviceUrl))
                configuration["ServiceUrl"] = serviceUrl;

            awsProvider.ConfigureAsync(configuration).GetAwaiter().GetResult();
            return awsProvider;
        });

        return services;
    }
}

/// <summary>
/// Configuration options for the Secrets Management service.
/// </summary>
public class SecretsManagementOptions
{
    /// <summary>
    /// Gets or sets whether to enable Azure Key Vault integration.
    /// </summary>
    public bool EnableAzureKeyVault { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable AWS Secrets Manager integration.
    /// </summary>
    public bool EnableAwsSecretsManager { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable HashiCorp Vault integration.
    /// </summary>
    public bool EnableHashiCorpVault { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable Google Secret Manager integration.
    /// </summary>
    public bool EnableGoogleSecretManager { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable Kubernetes Secrets integration.
    /// </summary>
    public bool EnableKubernetesSecrets { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of secrets that can be stored.
    /// </summary>
    public int MaxSecretCount { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the default expiration time for secrets in days.
    /// </summary>
    public int DefaultExpirationDays { get; set; } = 365;

    /// <summary>
    /// Gets or sets whether to enable audit logging for secret access.
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable automatic secret rotation.
    /// </summary>
    public bool EnableAutomaticRotation { get; set; } = false;

    /// <summary>
    /// Gets or sets the interval in days for automatic secret rotation.
    /// </summary>
    public int AutoRotationIntervalDays { get; set; } = 90;

    /// <summary>
    /// Gets or sets whether to enable backup of secrets.
    /// </summary>
    public bool EnableBackup { get; set; } = true;

    /// <summary>
    /// Gets or sets the backup interval in hours.
    /// </summary>
    public int BackupIntervalHours { get; set; } = 24;
}
