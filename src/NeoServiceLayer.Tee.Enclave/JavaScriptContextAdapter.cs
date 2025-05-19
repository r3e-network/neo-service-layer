using System;
using System.Collections.Generic;
using System.Text.Json;
using NeoServiceLayer.Tee.Shared.JavaScript;

namespace NeoServiceLayer.Tee.Enclave
{
    /// <summary>
    /// Adapter for converting between the shared JavaScriptExecutionContext and the enclave-specific context.
    /// </summary>
    public static class JavaScriptContextAdapter
    {
        /// <summary>
        /// Creates an enclave-specific execution context from a shared context.
        /// </summary>
        /// <param name="sharedContext">The shared execution context.</param>
        /// <returns>An enclave-specific execution context.</returns>
        public static EnclaveJavaScriptContext FromSharedContext(Shared.JavaScript.JavaScriptExecutionContext sharedContext)
        {
            if (sharedContext == null)
            {
                throw new ArgumentNullException(nameof(sharedContext));
            }

            var context = new EnclaveJavaScriptContext
            {
                FunctionId = sharedContext.FunctionId,
                UserId = sharedContext.UserId,
                Input = string.IsNullOrEmpty(sharedContext.Input) 
                    ? JsonDocument.Parse("{}") 
                    : JsonDocument.Parse(sharedContext.Input),
                GasLimit = (long)sharedContext.GasLimit
            };

            // Parse secrets from JSON string to dictionary
            if (!string.IsNullOrEmpty(sharedContext.Secrets))
            {
                var secretsDoc = JsonDocument.Parse(sharedContext.Secrets);
                var enumerator = secretsDoc.RootElement.EnumerateObject();
                
                foreach (var property in enumerator)
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        context.AddSecret(property.Name, property.Value.GetString());
                    }
                }
            }

            // Add metadata as options
            foreach (var kvp in sharedContext.Metadata)
            {
                context.SetOption(kvp.Key, kvp.Value);
            }

            return context;
        }

        /// <summary>
        /// Updates a shared context with the results from an enclave-specific context.
        /// </summary>
        /// <param name="sharedContext">The shared context to update.</param>
        /// <param name="enclaveContext">The enclave-specific context with results.</param>
        /// <param name="result">The execution result as a JsonDocument.</param>
        /// <param name="gasUsed">The amount of gas used.</param>
        /// <param name="error">The error message, if any.</param>
        public static void UpdateSharedContext(
            Shared.JavaScript.JavaScriptExecutionContext sharedContext,
            EnclaveJavaScriptContext enclaveContext,
            JsonDocument result,
            long gasUsed,
            string error = null)
        {
            if (sharedContext == null)
            {
                throw new ArgumentNullException(nameof(sharedContext));
            }

            if (enclaveContext == null)
            {
                throw new ArgumentNullException(nameof(enclaveContext));
            }

            if (string.IsNullOrEmpty(error))
            {
                // Successful execution
                sharedContext.SetResult(
                    result != null ? result.RootElement.ToString() : "{}",
                    (ulong)gasUsed);
            }
            else
            {
                // Failed execution
                sharedContext.SetError(error, (ulong)gasUsed);
            }

            // Add any options back as metadata
            foreach (var kvp in enclaveContext.Options)
            {
                sharedContext.Metadata[kvp.Key] = kvp.Value?.ToString();
            }
        }
    }

    /// <summary>
    /// Represents the execution context for JavaScript code within the enclave.
    /// </summary>
    public class EnclaveJavaScriptContext
    {
        /// <summary>
        /// Gets or sets the function ID.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the input data for the JavaScript function.
        /// </summary>
        public JsonDocument Input { get; set; }

        /// <summary>
        /// Gets or sets the secrets available to the JavaScript function.
        /// </summary>
        public Dictionary<string, string> Secrets { get; set; }

        /// <summary>
        /// Gets or sets the maximum execution time in milliseconds.
        /// </summary>
        public int MaxExecutionTimeMs { get; set; } = 60000; // Default: 60 seconds

        /// <summary>
        /// Gets or sets the gas limit.
        /// </summary>
        public long GasLimit { get; set; } = 1_000_000; // Default: 1 million gas units

        /// <summary>
        /// Gets or sets a value indicating whether to enable debug mode.
        /// </summary>
        public bool EnableDebugMode { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to enable tracing.
        /// </summary>
        public bool EnableTracing { get; set; } = false;

        /// <summary>
        /// Gets or sets additional options for the JavaScript execution.
        /// </summary>
        public Dictionary<string, object> Options { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the creation time of this context.
        /// </summary>
        public DateTime CreationTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveJavaScriptContext"/> class.
        /// </summary>
        public EnclaveJavaScriptContext()
        {
            Secrets = new Dictionary<string, string>();
        }

        /// <summary>
        /// Adds a secret to the execution context.
        /// </summary>
        /// <param name="name">The secret name.</param>
        /// <param name="value">The secret value.</param>
        public void AddSecret(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Secrets[name] = value;
        }

        /// <summary>
        /// Sets an option for the execution context.
        /// </summary>
        /// <param name="name">The option name.</param>
        /// <param name="value">The option value.</param>
        public void SetOption(string name, object value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Options[name] = value;
        }

        /// <summary>
        /// Gets an option value from the execution context.
        /// </summary>
        /// <typeparam name="T">The type of the option value.</typeparam>
        /// <param name="name">The option name.</param>
        /// <param name="defaultValue">The default value to return if the option doesn't exist.</param>
        /// <returns>The option value, or the default value if the option doesn't exist.</returns>
        public T GetOption<T>(string name, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (Options.TryGetValue(name, out var value) && value is T typedValue)
            {
                return typedValue;
            }

            return defaultValue;
        }
    }
} 