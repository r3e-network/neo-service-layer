using System;

namespace NeoServiceLayer.Tee.Shared.JavaScript
{
    /// <summary>
    /// Interface for a factory that creates JavaScript engines.
    /// </summary>
    public interface IJavaScriptEngineFactory
    {
        /// <summary>
        /// Creates a JavaScript engine.
        /// </summary>
        /// <param name="engineType">The type of JavaScript engine to create.</param>
        /// <returns>The created JavaScript engine.</returns>
        IJavaScriptEngine CreateEngine(JavaScriptEngineType engineType);

        /// <summary>
        /// Creates a JavaScript engine with the specified options.
        /// </summary>
        /// <param name="engineType">The type of JavaScript engine to create.</param>
        /// <param name="options">The options for the JavaScript engine.</param>
        /// <returns>The created JavaScript engine.</returns>
        IJavaScriptEngine CreateEngine(JavaScriptEngineType engineType, JavaScriptEngineOptions options);

        /// <summary>
        /// Gets the default JavaScript engine type.
        /// </summary>
        /// <returns>The default JavaScript engine type.</returns>
        JavaScriptEngineType GetDefaultEngineType();

        /// <summary>
        /// Gets the available JavaScript engine types.
        /// </summary>
        /// <returns>An array of available JavaScript engine types.</returns>
        JavaScriptEngineType[] GetAvailableEngineTypes();
    }

    /// <summary>
    /// Types of JavaScript engines.
    /// </summary>
    public enum JavaScriptEngineType
    {
        /// <summary>
        /// QuickJS JavaScript engine.
        /// </summary>
        QuickJS,

        /// <summary>
        /// V8 JavaScript engine.
        /// </summary>
        V8,

        /// <summary>
        /// ClearScript JavaScript engine.
        /// </summary>
        ClearScript,

        /// <summary>
        /// Simple JavaScript engine for testing.
        /// </summary>
        Simple
    }

    /// <summary>
    /// Options for JavaScript engines.
    /// </summary>
    public class JavaScriptEngineOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to enable debugging.
        /// </summary>
        public bool EnableDebugging { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable gas accounting.
        /// </summary>
        public bool EnableGasAccounting { get; set; } = true;

        /// <summary>
        /// Gets or sets the default gas limit.
        /// </summary>
        public ulong DefaultGasLimit { get; set; } = 10000000; // 10 million gas units

        /// <summary>
        /// Gets or sets a value indicating whether to enable secure APIs.
        /// </summary>
        public bool EnableSecureApis { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to enable storage APIs.
        /// </summary>
        public bool EnableStorageApis { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to enable crypto APIs.
        /// </summary>
        public bool EnableCryptoApis { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to enable network APIs.
        /// </summary>
        public bool EnableNetworkApis { get; set; } = false;

        /// <summary>
        /// Gets or sets the memory limit in bytes.
        /// </summary>
        public ulong MemoryLimit { get; set; } = 8 * 1024 * 1024; // 8 MB

        /// <summary>
        /// Gets or sets the execution timeout in milliseconds.
        /// </summary>
        public int ExecutionTimeoutMs { get; set; } = 5000; // 5 seconds

        /// <summary>
        /// Gets or sets a value indicating whether to enable code caching.
        /// </summary>
        public bool EnableCodeCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum code cache size.
        /// </summary>
        public int MaxCodeCacheSize { get; set; } = 100;
    }
}
