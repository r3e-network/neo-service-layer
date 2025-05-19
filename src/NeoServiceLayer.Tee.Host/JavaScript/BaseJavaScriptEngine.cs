using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.JavaScript;

namespace NeoServiceLayer.Tee.Host.JavaScript
{
    /// <summary>
    /// Base class for JavaScript engines.
    /// </summary>
    public abstract class BaseJavaScriptEngine : IJavaScriptEngine
    {
        /// <summary>
        /// The logger.
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// The gas accounting.
        /// </summary>
        protected readonly IGasAccounting GasAccounting;

        /// <summary>
        /// The JavaScript APIs.
        /// </summary>
        protected readonly List<IJavaScriptApi> JavaScriptApis;

        /// <summary>
        /// The JavaScript engine options.
        /// </summary>
        protected readonly JavaScriptEngineOptions Options;

        /// <summary>
        /// Whether the engine is initialized.
        /// </summary>
        protected bool Initialized;

        /// <summary>
        /// Whether the engine is disposed.
        /// </summary>
        protected bool Disposed;

        /// <summary>
        /// Initializes a new instance of the BaseJavaScriptEngine class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="gasAccounting">The gas accounting.</param>
        /// <param name="options">The JavaScript engine options.</param>
        protected BaseJavaScriptEngine(ILogger logger, IGasAccounting gasAccounting, JavaScriptEngineOptions options)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            GasAccounting = gasAccounting ?? throw new ArgumentNullException(nameof(gasAccounting));
            Options = options ?? new JavaScriptEngineOptions();
            JavaScriptApis = new List<IJavaScriptApi>();
            Initialized = false;
            Disposed = false;
        }

        /// <inheritdoc/>
        public abstract Task<bool> InitializeAsync();

        /// <inheritdoc/>
        public abstract Task<string> ExecuteAsync(string code, string input, string secrets, string functionId, string userId);

        /// <inheritdoc/>
        public virtual async Task<string> ExecuteWithGasAsync(string code, string input, string secrets, string functionId, string userId, ulong gasLimit, out ulong gasUsed)
        {
            // Set gas limit
            GasAccounting.SetGasLimit(gasLimit);

            // Reset gas used
            GasAccounting.ResetGasUsed();

            try
            {
                // Execute the code
                string result = await ExecuteAsync(code, input, secrets, functionId, userId);

                // Get gas used
                gasUsed = GasAccounting.GetGasUsed();

                // Record gas usage
                GasAccounting.RecordGasUsage(functionId, userId, gasUsed);

                return result;
            }
            catch (Exception ex)
            {
                // Get gas used
                gasUsed = GasAccounting.GetGasUsed();

                // Record gas usage
                GasAccounting.RecordGasUsage(functionId, userId, gasUsed);

                // Log the error
                Logger.LogError(ex, "Error executing JavaScript code: {Error}", ex.Message);

                // Return error as JSON
                return $"{{\"error\":\"{ex.Message}\"}}";
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> VerifyCodeHashAsync(string code, string hash)
        {
            string calculatedHash = await CalculateCodeHashAsync(code);
            return string.Equals(calculatedHash, hash, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public virtual async Task<string> CalculateCodeHashAsync(string code)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <inheritdoc/>
        public virtual void ResetGasUsed()
        {
            GasAccounting.ResetGasUsed();
        }

        /// <inheritdoc/>
        public virtual ulong GetGasUsed()
        {
            return GasAccounting.GetGasUsed();
        }

        /// <inheritdoc/>
        public virtual IReadOnlyList<string> GetAvailableApis()
        {
            List<string> apis = new List<string>();
            foreach (IJavaScriptApi api in JavaScriptApis)
            {
                apis.Add(api.Name);
            }
            return apis;
        }

        /// <inheritdoc/>
        public abstract string GetEngineName();

        /// <inheritdoc/>
        public abstract string GetEngineVersion();

        /// <summary>
        /// Adds a JavaScript API.
        /// </summary>
        /// <param name="api">The JavaScript API to add.</param>
        public virtual void AddApi(IJavaScriptApi api)
        {
            if (api == null)
            {
                throw new ArgumentNullException(nameof(api));
            }

            JavaScriptApis.Add(api);
            api.Register(this);
        }

        /// <summary>
        /// Removes a JavaScript API.
        /// </summary>
        /// <param name="api">The JavaScript API to remove.</param>
        public virtual void RemoveApi(IJavaScriptApi api)
        {
            if (api == null)
            {
                throw new ArgumentNullException(nameof(api));
            }

            JavaScriptApis.Remove(api);
            api.Unregister(this);
        }

        /// <summary>
        /// Checks if the engine is initialized.
        /// </summary>
        protected void CheckInitialized()
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("JavaScript engine is not initialized");
            }
        }

        /// <summary>
        /// Checks if the engine is disposed.
        /// </summary>
        protected void CheckDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the engine.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    foreach (IJavaScriptApi api in JavaScriptApis)
                    {
                        api.Unregister(this);
                    }
                    JavaScriptApis.Clear();
                }

                Disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the engine.
        /// </summary>
        ~BaseJavaScriptEngine()
        {
            Dispose(false);
        }
    }
}
