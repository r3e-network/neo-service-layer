using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Core.Configuration
{
    /// <summary>
    /// Validates security-related configuration settings
    /// </summary>
    public class SecurityConfigurationValidator : IConfigurationValidator
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SecurityConfigurationValidator> _logger;

        /// <summary>
        /// Initializes a new instance of SecurityConfigurationValidator
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="logger">The logger</param>
        public SecurityConfigurationValidator(
            IConfiguration configuration, 
            ILogger<SecurityConfigurationValidator> logger)
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
                // Validate JWT settings
                ValidateJwtConfiguration(errors, warnings);

                // Validate password policy settings
                ValidatePasswordPolicyConfiguration(errors, warnings);

                // Validate encryption settings
                ValidateEncryptionConfiguration(errors, warnings);

                // Validate HTTPS settings
                ValidateHttpsConfiguration(errors, warnings);

                // Validate CORS settings
                ValidateCorsConfiguration(errors, warnings);

                // Validate rate limiting settings
                ValidateRateLimitingConfiguration(errors, warnings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during security configuration validation");
                errors.Add($"Security configuration validation failed: {ex.Message}");
            }

            return errors.Count == 0 
                ? ConfigurationValidationResult.Success(warnings)
                : ConfigurationValidationResult.Failure(errors, warnings);
        }

        private void ValidateJwtConfiguration(List<string> errors, List<string> warnings)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            
            var secretKey = jwtSection.GetValue<string>("SecretKey");
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                errors.Add("JWT secret key is missing");
            }
            else if (secretKey.Length < 32)
            {
                errors.Add("JWT secret key is too short (minimum 32 characters required)");
            }
            else if (secretKey == "your-secret-key" || secretKey == "supersecret")
            {
                errors.Add("JWT secret key is using a default/example value");
            }

            var expiration = jwtSection.GetValue<int?>("ExpirationMinutes");
            if (!expiration.HasValue)
            {
                warnings.Add("JWT expiration time not configured - using default");
            }
            else if (expiration.Value > 1440) // 24 hours
            {
                warnings.Add($"JWT expiration time is very long ({expiration.Value} minutes) - consider shorter duration");
            }

            var issuer = jwtSection.GetValue<string>("Issuer");
            if (string.IsNullOrWhiteSpace(issuer))
            {
                warnings.Add("JWT issuer not configured");
            }

            var audience = jwtSection.GetValue<string>("Audience");
            if (string.IsNullOrWhiteSpace(audience))
            {
                warnings.Add("JWT audience not configured");
            }
        }

        private void ValidatePasswordPolicyConfiguration(List<string> errors, List<string> warnings)
        {
            var passwordSection = _configuration.GetSection("PasswordPolicy");
            
            var minLength = passwordSection.GetValue<int?>("MinimumLength");
            if (minLength.HasValue && minLength.Value < 8)
            {
                errors.Add($"Password minimum length ({minLength.Value}) is too short (minimum 8 required)");
            }

            var maxFailedAttempts = passwordSection.GetValue<int?>("MaxFailedAttempts");
            if (maxFailedAttempts.HasValue && maxFailedAttempts.Value <= 0)
            {
                errors.Add("Password max failed attempts must be positive");
            }

            var lockoutDuration = passwordSection.GetValue<int?>("LockoutDurationMinutes");
            if (lockoutDuration.HasValue && lockoutDuration.Value <= 0)
            {
                errors.Add("Password lockout duration must be positive");
            }
        }

        private void ValidateEncryptionConfiguration(List<string> errors, List<string> warnings)
        {
            var encryptionSection = _configuration.GetSection("Encryption");
            
            var encryptionKey = encryptionSection.GetValue<string>("Key");
            if (string.IsNullOrWhiteSpace(encryptionKey))
            {
                warnings.Add("Encryption key not configured - some features may not work");
            }
            else if (encryptionKey.Length < 32)
            {
                errors.Add("Encryption key is too short (minimum 32 characters required)");
            }
        }

        private void ValidateHttpsConfiguration(List<string> errors, List<string> warnings)
        {
            var httpsSection = _configuration.GetSection("Https");
            
            var requireHttps = httpsSection.GetValue<bool?>("RequireHttps");
            var environment = _configuration.GetValue<string>("Environment");
            
            if (requireHttps == false && 
                !string.IsNullOrEmpty(environment) && 
                (environment.Equals("Production", StringComparison.OrdinalIgnoreCase) ||
                 environment.Equals("Staging", StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"HTTPS should be required in {environment} environment");
            }
        }

        private void ValidateCorsConfiguration(List<string> errors, List<string> warnings)
        {
            var corsSection = _configuration.GetSection("Cors");
            
            var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>();
            if (allowedOrigins?.Any(o => o == "*") == true)
            {
                var environment = _configuration.GetValue<string>("Environment");
                if (!string.IsNullOrEmpty(environment) && 
                    environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("CORS should not allow all origins (*) in production");
                }
                else
                {
                    warnings.Add("CORS allows all origins (*) - ensure this is intentional");
                }
            }
        }

        private void ValidateRateLimitingConfiguration(List<string> errors, List<string> warnings)
        {
            var rateLimitSection = _configuration.GetSection("RateLimit");
            
            var enabled = rateLimitSection.GetValue<bool?>("Enabled");
            if (enabled != true)
            {
                warnings.Add("Rate limiting is not enabled - consider enabling for production");
                return;
            }

            var requestsPerMinute = rateLimitSection.GetValue<int?>("RequestsPerMinute");
            if (requestsPerMinute.HasValue && requestsPerMinute.Value <= 0)
            {
                errors.Add("Rate limit requests per minute must be positive");
            }
        }
    }
}