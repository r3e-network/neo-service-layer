using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.JavaScript;

namespace NeoServiceLayer.Tee.Host.JavaScript
{
    /// <summary>
    /// Manager for JavaScript execution.
    /// </summary>
    public class JavaScriptManager : IJavaScriptManager
    {
        private readonly ILogger<JavaScriptManager> _logger;
        private readonly IJavaScriptEngineFactory _engineFactory;
        private readonly IJavaScriptEngine _engine;
        private readonly SemaphoreSlim _semaphore;
        private bool _initialized;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the JavaScriptManager class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="engineFactory">The JavaScript engine factory.</param>
        public JavaScriptManager(ILogger<JavaScriptManager> logger, IJavaScriptEngineFactory engineFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _engineFactory = engineFactory ?? throw new ArgumentNullException(nameof(engineFactory));
            _engine = _engineFactory.CreateEngine(_engineFactory.GetDefaultEngineType());
            _semaphore = new SemaphoreSlim(1, 1);
            _initialized = false;
            _disposed = false;
        }

        /// <inheritdoc/>
        public async Task<bool> InitializeAsync()
        {
            CheckDisposed();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (_initialized)
                    {
                        return true;
                    }

                    // Initialize the JavaScript engine
                    if (!await _engine.InitializeAsync())
                    {
                        _logger.LogError("Failed to initialize JavaScript engine");
                        return false;
                    }

                    _initialized = true;
                    _logger.LogInformation("JavaScript manager initialized successfully");
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing JavaScript manager");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExecuteAsync(JavaScriptExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            CheckDisposed();
            CheckInitialized();

            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Execute the JavaScript code
                    string result = await _engine.ExecuteWithGasAsync(
                        context.Code,
                        context.Input,
                        context.Secrets,
                        context.FunctionId,
                        context.UserId,
                        context.GasLimit,
                        out ulong gasUsed);

                    // Check if the result contains an error
                    if (result.Contains("\"error\":"))
                    {
                        context.SetError(result, gasUsed);
                        return false;
                    }

                    // Set the result
                    context.SetResult(result, gasUsed);
                    return true;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code: {Error}", ex.Message);
                context.SetError(ex.Message, _engine.GetGasUsed());
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<JavaScriptExecutionContext> ExecuteAsync(string code, string input, string secrets, string functionId, string userId, ulong gasLimit)
        {
            JavaScriptExecutionContext context = new JavaScriptExecutionContext(functionId, userId, code, input, secrets, gasLimit);
            await ExecuteAsync(context);
            return context;
        }

        /// <inheritdoc/>
        public async Task<bool> VerifyCodeHashAsync(string code, string hash)
        {
            CheckDisposed();
            CheckInitialized();

            return await _engine.VerifyCodeHashAsync(code, hash);
        }

        /// <inheritdoc/>
        public async Task<string> CalculateCodeHashAsync(string code)
        {
            CheckDisposed();
            CheckInitialized();

            return await _engine.CalculateCodeHashAsync(code);
        }

        /// <inheritdoc/>
        public IJavaScriptEngine GetEngine()
        {
            CheckDisposed();
            CheckInitialized();

            return _engine;
        }

        /// <inheritdoc/>
        public IJavaScriptEngineFactory GetEngineFactory()
        {
            CheckDisposed();

            return _engineFactory;
        }

        /// <inheritdoc/>
        public IReadOnlyList<string> GetAvailableApis()
        {
            CheckDisposed();
            CheckInitialized();

            return _engine.GetAvailableApis();
        }

        /// <inheritdoc/>
        public string GetEngineName()
        {
            CheckDisposed();
            CheckInitialized();

            return _engine.GetEngineName();
        }

        /// <inheritdoc/>
        public string GetEngineVersion()
        {
            CheckDisposed();
            CheckInitialized();

            return _engine.GetEngineVersion();
        }

        /// <summary>
        /// Checks if the manager is initialized.
        /// </summary>
        private void CheckInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("JavaScript manager is not initialized");
            }
        }

        /// <summary>
        /// Checks if the manager is disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(JavaScriptManager));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the manager.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _engine.Dispose();
                    _semaphore.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the manager.
        /// </summary>
        ~JavaScriptManager()
        {
            Dispose(false);
        }
    }
}
