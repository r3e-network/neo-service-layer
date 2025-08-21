using System.Collections.Generic;

namespace NeoServiceLayer.Core.Configuration
{
    /// <summary>
    /// Interface for validating configuration
    /// </summary>
    public interface IConfigurationValidator
    {
        /// <summary>
        /// Validates the configuration
        /// </summary>
        /// <returns>Validation result</returns>
        ConfigurationValidationResult Validate();
    }

    /// <summary>
    /// Configuration validation result
    /// </summary>
    public class ConfigurationValidationResult
    {
        /// <summary>
        /// Gets whether the configuration is valid
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the validation errors
        /// </summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// Gets the validation warnings
        /// </summary>
        public IReadOnlyList<string> Warnings { get; }

        /// <summary>
        /// Initializes a new instance of ConfigurationValidationResult
        /// </summary>
        /// <param name="isValid">Whether the configuration is valid</param>
        /// <param name="errors">Validation errors</param>
        /// <param name="warnings">Validation warnings</param>
        public ConfigurationValidationResult(bool isValid, IEnumerable<string>? errors = null, IEnumerable<string>? warnings = null)
        {
            IsValid = isValid;
            Errors = (errors ?? new List<string>()).ToList().AsReadOnly();
            Warnings = (warnings ?? new List<string>()).ToList().AsReadOnly();
        }

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        /// <param name="warnings">Optional warnings</param>
        /// <returns>Successful result</returns>
        public static ConfigurationValidationResult Success(IEnumerable<string>? warnings = null) => 
            new ConfigurationValidationResult(true, null, warnings);

        /// <summary>
        /// Creates a failed validation result
        /// </summary>
        /// <param name="errors">Validation errors</param>
        /// <param name="warnings">Optional warnings</param>
        /// <returns>Failed result</returns>
        public static ConfigurationValidationResult Failure(IEnumerable<string> errors, IEnumerable<string>? warnings = null) => 
            new ConfigurationValidationResult(false, errors, warnings);
    }
}