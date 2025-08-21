using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Core.Configuration
{
    /// <summary>
    /// Validates database configuration settings
    /// </summary>
    public class DatabaseConfigurationValidator : IConfigurationValidator
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseConfigurationValidator> _logger;

        /// <summary>
        /// Initializes a new instance of DatabaseConfigurationValidator
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="logger">The logger</param>
        public DatabaseConfigurationValidator(
            IConfiguration configuration, 
            ILogger<DatabaseConfigurationValidator> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public ConfigurationValidationResult Validate()
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            try
            {
                // Validate connection string
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    errors.Add("Default database connection string is missing or empty");
                }
                else
                {
                    // Validate connection string format
                    if (!connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) &&
                        !connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add("Connection string appears to be invalid - missing server/data source");
                    }

                    // Check for security issues
                    if (connectionString.Contains("password=", StringComparison.OrdinalIgnoreCase) &&
                        !connectionString.Contains("Encrypt=true", StringComparison.OrdinalIgnoreCase))
                    {
                        warnings.Add("Database connection should use encryption (Encrypt=true)");
                    }

                    // Check for pooling configuration
                    if (!connectionString.Contains("Pooling=", StringComparison.OrdinalIgnoreCase))
                    {
                        warnings.Add("Connection pooling configuration not explicitly set");
                    }
                }

                // Validate timeout settings
                var commandTimeout = _configuration.GetValue<int?>("Database:CommandTimeout");
                if (commandTimeout.HasValue)
                {
                    if (commandTimeout.Value <= 0)
                    {
                        errors.Add("Database command timeout must be positive");
                    }
                    else if (commandTimeout.Value > 300)
                    {
                        warnings.Add($"Database command timeout is very high ({commandTimeout.Value}s) - consider reducing it");
                    }
                }
                else
                {
                    warnings.Add("Database command timeout not configured - using default");
                }

                // Validate retry policy
                var maxRetries = _configuration.GetValue<int?>("Database:MaxRetries");
                if (maxRetries.HasValue && maxRetries.Value < 0)
                {
                    errors.Add("Database max retries cannot be negative");
                }

                // Validate migration settings
                var autoMigrate = _configuration.GetValue<bool?>("Database:AutoMigrate");
                var environment = _configuration.GetValue<string>("Environment");
                
                if (autoMigrate == true && 
                    !string.IsNullOrEmpty(environment) && 
                    environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add("Auto-migration is enabled in production - consider manual migration control");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database configuration validation");
                errors.Add($"Configuration validation failed: {ex.Message}");
            }

            return errors.Count == 0 
                ? ConfigurationValidationResult.Success(warnings)
                : ConfigurationValidationResult.Failure(errors, warnings);
        }
    }
}