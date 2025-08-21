using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Core.Configuration
{
    /// <summary>
    /// Service for orchestrating configuration validation
    /// </summary>
    public class ConfigurationValidationService
    {
        private readonly IEnumerable<IConfigurationValidator> _validators;
        private readonly ILogger<ConfigurationValidationService> _logger;

        /// <summary>
        /// Initializes a new instance of ConfigurationValidationService
        /// </summary>
        /// <param name="validators">The configuration validators</param>
        /// <param name="logger">The logger</param>
        public ConfigurationValidationService(
            IEnumerable<IConfigurationValidator> validators,
            ILogger<ConfigurationValidationService> logger)
        {
            _validators = validators ?? throw new ArgumentNullException(nameof(validators));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates all configuration using registered validators
        /// </summary>
        /// <returns>Overall configuration validation result</returns>
        public OverallConfigurationValidationResult ValidateAll()
        {
            var validatorList = _validators.ToList();
            var results = new Dictionary<string, ConfigurationValidationResult>();
            var allErrors = new List<string>();
            var allWarnings = new List<string>();

            _logger.LogDebug("Running {ValidatorCount} configuration validators", validatorList.Count);

            foreach (var validator in validatorList)
            {
                var validatorName = validator.GetType().Name.Replace("ConfigurationValidator", "");
                
                try
                {
                    _logger.LogDebug("Running configuration validator: {ValidatorName}", validatorName);
                    var result = validator.Validate();
                    results[validatorName] = result;

                    allErrors.AddRange(result.Errors);
                    allWarnings.AddRange(result.Warnings);

                    _logger.LogDebug(
                        "Configuration validator {ValidatorName} completed - Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                        validatorName, result.IsValid, result.Errors.Count, result.Warnings.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Configuration validator {ValidatorName} threw an exception", validatorName);
                    
                    var errorResult = ConfigurationValidationResult.Failure(
                        new[] { $"Validator {validatorName} failed: {ex.Message}" });
                    
                    results[validatorName] = errorResult;
                    allErrors.Add($"Validator {validatorName} failed: {ex.Message}");
                }
            }

            var isValid = allErrors.Count == 0;
            var overallResult = new OverallConfigurationValidationResult(
                isValid, 
                results, 
                allErrors, 
                allWarnings);

            _logger.LogInformation(
                "Configuration validation completed - Valid: {IsValid}, Total Errors: {ErrorCount}, Total Warnings: {WarningCount}",
                isValid, allErrors.Count, allWarnings.Count);

            if (!isValid)
            {
                _logger.LogWarning("Configuration validation failed with errors: {Errors}", 
                    string.Join("; ", allErrors));
            }

            if (allWarnings.Any())
            {
                _logger.LogWarning("Configuration validation has warnings: {Warnings}", 
                    string.Join("; ", allWarnings));
            }

            return overallResult;
        }

        /// <summary>
        /// Validates configuration and throws an exception if validation fails
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        public void ValidateAndThrow()
        {
            var result = ValidateAll();
            
            if (!result.IsValid)
            {
                var errorMessage = "Configuration validation failed with errors:\n" + 
                                 string.Join("\n", result.AllErrors);
                
                if (result.AllWarnings.Any())
                {
                    errorMessage += "\n\nWarnings:\n" + string.Join("\n", result.AllWarnings);
                }
                
                throw new InvalidOperationException(errorMessage);
            }
        }
    }

    /// <summary>
    /// Overall configuration validation result
    /// </summary>
    public class OverallConfigurationValidationResult
    {
        /// <summary>
        /// Gets whether all configurations are valid
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the individual validation results by validator name
        /// </summary>
        public IReadOnlyDictionary<string, ConfigurationValidationResult> ValidatorResults { get; }

        /// <summary>
        /// Gets all errors from all validators
        /// </summary>
        public IReadOnlyList<string> AllErrors { get; }

        /// <summary>
        /// Gets all warnings from all validators
        /// </summary>
        public IReadOnlyList<string> AllWarnings { get; }

        /// <summary>
        /// Initializes a new instance of OverallConfigurationValidationResult
        /// </summary>
        /// <param name="isValid">Whether all configurations are valid</param>
        /// <param name="validatorResults">Individual validator results</param>
        /// <param name="allErrors">All errors</param>
        /// <param name="allWarnings">All warnings</param>
        public OverallConfigurationValidationResult(
            bool isValid,
            IReadOnlyDictionary<string, ConfigurationValidationResult> validatorResults,
            IEnumerable<string> allErrors,
            IEnumerable<string> allWarnings)
        {
            IsValid = isValid;
            ValidatorResults = validatorResults ?? throw new ArgumentNullException(nameof(validatorResults));
            AllErrors = allErrors?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
            AllWarnings = allWarnings?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
        }
    }
}