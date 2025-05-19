using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// Validator for TEE enclave settings.
    /// </summary>
    public class TeeEnclaveSettingsValidator
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeeEnclaveSettingsValidator"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public TeeEnclaveSettingsValidator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates the specified settings.
        /// </summary>
        /// <param name="settings">The settings to validate.</param>
        /// <returns>A list of validation errors, or an empty list if the settings are valid.</returns>
        public List<string> Validate(TeeEnclaveSettings settings)
        {
            var errors = new List<string>();

            if (settings == null)
            {
                errors.Add("Settings cannot be null.");
                return errors;
            }

            // Validate TEE type
            if (string.IsNullOrEmpty(settings.Type))
            {
                errors.Add("TEE type cannot be null or empty.");
            }
            else if (settings.Type != "SGX" && settings.Type != "OpenEnclave")
            {
                errors.Add($"Invalid TEE type: {settings.Type}. Supported types are: SGX, OpenEnclave.");
            }

            // Validate enclave settings
            if (settings.EnclaveSettings == null)
            {
                errors.Add("Enclave settings cannot be null.");
            }
            else
            {
                // Validate enclave path
                if (string.IsNullOrEmpty(settings.EnclaveSettings.EnclavePath))
                {
                    errors.Add("Enclave path cannot be null or empty.");
                }
                else if (!settings.EnclaveSettings.SimulationMode && !File.Exists(settings.EnclaveSettings.EnclavePath))
                {
                    errors.Add($"Enclave file not found: {settings.EnclaveSettings.EnclavePath}");
                }

                // Validate Occlum settings
                if (settings.EnclaveSettings.OcclumSupport)
                {
                    if (string.IsNullOrEmpty(settings.EnclaveSettings.OcclumInstanceDir))
                    {
                        errors.Add("Occlum instance directory cannot be null or empty when Occlum support is enabled.");
                    }

                    if (string.IsNullOrEmpty(settings.EnclaveSettings.OcclumLogLevel))
                    {
                        errors.Add("Occlum log level cannot be null or empty when Occlum support is enabled.");
                    }
                }
            }

            // Validate JavaScript engine settings
            if (settings.JavaScriptEngine == null)
            {
                errors.Add("JavaScript engine settings cannot be null.");
            }
            else
            {
                if (settings.JavaScriptEngine.MaxMemoryMB <= 0)
                {
                    errors.Add($"Invalid maximum memory: {settings.JavaScriptEngine.MaxMemoryMB}. Must be greater than zero.");
                }

                if (settings.JavaScriptEngine.MaxExecutionTimeMs <= 0)
                {
                    errors.Add($"Invalid maximum execution time: {settings.JavaScriptEngine.MaxExecutionTimeMs}. Must be greater than zero.");
                }
            }

            // Validate user secrets settings
            if (settings.UserSecrets == null)
            {
                errors.Add("User secrets settings cannot be null.");
            }
            else
            {
                if (settings.UserSecrets.MaxSecretsPerUser <= 0)
                {
                    errors.Add($"Invalid maximum secrets per user: {settings.UserSecrets.MaxSecretsPerUser}. Must be greater than zero.");
                }

                if (settings.UserSecrets.MaxSecretSizeBytes <= 0)
                {
                    errors.Add($"Invalid maximum secret size: {settings.UserSecrets.MaxSecretSizeBytes}. Must be greater than zero.");
                }
            }

            // Validate gas accounting settings
            if (settings.GasAccounting == null)
            {
                errors.Add("Gas accounting settings cannot be null.");
            }
            else
            {
                if (settings.GasAccounting.EnableGasAccounting)
                {
                    if (settings.GasAccounting.GasLimitPerExecution <= 0)
                    {
                        errors.Add($"Invalid gas limit per execution: {settings.GasAccounting.GasLimitPerExecution}. Must be greater than zero.");
                    }

                    if (settings.GasAccounting.GasPriceMultiplier <= 0)
                    {
                        errors.Add($"Invalid gas price multiplier: {settings.GasAccounting.GasPriceMultiplier}. Must be greater than zero.");
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Validates the specified settings and throws an exception if they are invalid.
        /// </summary>
        /// <param name="settings">The settings to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the settings are invalid.</exception>
        public void ValidateAndThrow(TeeEnclaveSettings settings)
        {
            var errors = Validate(settings);
            if (errors.Count > 0)
            {
                var errorMessage = string.Join(Environment.NewLine, errors);
                _logger.LogError("Invalid TEE enclave settings: {ErrorMessage}", errorMessage);
                throw new ArgumentException(errorMessage, nameof(settings));
            }
        }
    }
}
