using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Exceptions;

namespace NeoServiceLayer.Tee.Host.Occlum
{
    /// <summary>
    /// Manages Occlum instances within an enclave.
    /// </summary>
    public class OcclumManager : IOcclumManager, IDisposable
    {
        private readonly ILogger<OcclumManager> _logger;
        private readonly OcclumOptions _options;
        private bool _initialized;
        private bool _disposed;
        private Process _occlumProcess;

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumManager"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="options">The options for the Occlum manager.</param>
        public OcclumManager(
            ILogger<OcclumManager> logger,
            OcclumOptions options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new OcclumOptions();
            _initialized = false;
            _disposed = false;
        }

        /// <summary>
        /// Synchronously initializes the Occlum instance.
        /// </summary>
        public void Init()
        {
            CheckDisposed();

            if (_initialized)
            {
                _logger.LogInformation("Occlum instance already initialized");
                return;
            }

            _logger.LogInformation("Synchronously initializing Occlum instance in {InstanceDir} with log level {LogLevel}",
                _options.InstanceDir, _options.LogLevel);

            try
            {
                // Run the async initialization synchronously
                InitializeAsync().GetAwaiter().GetResult();
                _initialized = true;
                _logger.LogInformation("Occlum instance initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Occlum instance");
                throw new OcclumInitializationException("Failed to initialize Occlum instance", ex);
            }
        }

        /// <summary>
        /// Initializes the Occlum instance.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            CheckDisposed();

            if (_initialized)
            {
                _logger.LogInformation("Occlum instance already initialized");
                return;
            }

            _logger.LogInformation("Initializing Occlum instance in {InstanceDir} with log level {LogLevel}",
                _options.InstanceDir, _options.LogLevel);

            try
            {
                // Check if the Occlum instance directory exists
                if (!Directory.Exists(_options.InstanceDir))
                {
                    _logger.LogInformation("Creating Occlum instance directory: {InstanceDir}", _options.InstanceDir);
                    Directory.CreateDirectory(_options.InstanceDir);
                }

                // Check if the temp directory exists
                if (!Directory.Exists(_options.TempDir))
                {
                    _logger.LogInformation("Creating temp directory: {TempDir}", _options.TempDir);
                    Directory.CreateDirectory(_options.TempDir);
                }

                // Initialize the Occlum instance
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "occlum",
                    Arguments = $"init --log-level={_options.LogLevel}",
                    WorkingDirectory = _options.InstanceDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        throw new OcclumInitializationException("Failed to start Occlum init process");
                    }

                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        string error = await process.StandardError.ReadToEndAsync();
                        throw new OcclumInitializationException($"Occlum init failed with exit code {process.ExitCode}: {error}");
                    }
                }

                _initialized = true;
                _logger.LogInformation("Occlum instance initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Occlum instance");
                throw new OcclumInitializationException("Failed to initialize Occlum instance", ex);
            }
        }

        /// <summary>
        /// Initializes the Occlum instance with a specific directory and log level.
        /// </summary>
        /// <param name="instanceDir">The directory to initialize the Occlum instance in.</param>
        /// <param name="logLevel">The log level for the Occlum instance.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task InitializeInstanceAsync(string instanceDir, string logLevel)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(instanceDir))
            {
                throw new ArgumentException("Instance directory cannot be null or empty", nameof(instanceDir));
            }

            if (string.IsNullOrEmpty(logLevel))
            {
                logLevel = "info"; // Default log level
            }

            _logger.LogInformation("Initializing Occlum instance in {InstanceDir} with log level {LogLevel}",
                instanceDir, logLevel);

            try
            {
                // Create a new options object with the specified instance directory and log level
                var newOptions = new OcclumOptions
                {
                    InstanceDir = instanceDir,
                    LogLevel = logLevel,
                    NodeJsPath = _options.NodeJsPath,
                    TempDir = Path.Combine(instanceDir, "temp")
                };

                // Update the options
                await UpdateConfigurationAsync(newOptions);

                // Initialize the Occlum instance
                await InitializeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Occlum instance in {InstanceDir} with log level {LogLevel}",
                    instanceDir, logLevel);
                throw new OcclumInitializationException($"Failed to initialize Occlum instance in {instanceDir}", ex);
            }
        }

        /// <summary>
        /// Executes a command in the Occlum instance.
        /// </summary>
        /// <param name="path">The path to the executable.</param>
        /// <param name="args">The arguments for the executable.</param>
        /// <param name="env">The environment variables for the executable.</param>
        /// <returns>The exit code of the command.</returns>
        public async Task<int> ExecuteCommandAsync(string path, string[] args, string[] env = null)
        {
            CheckDisposed();

            if (!_initialized)
            {
                _logger.LogWarning("Occlum instance not initialized, initializing now");
                await InitializeAsync();
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            // Use empty environment if not provided
            env ??= Array.Empty<string>();

            _logger.LogInformation("Executing command {Path} with {ArgCount} arguments and {EnvCount} environment variables",
                path, args.Length, env.Length);

            try
            {
                // Build the command arguments
                string argsString = string.Join(" ", args);
                string envString = string.Join(" ", env);

                // Build the Occlum run command
                string occlumArgs = $"run {path} {argsString}";
                if (env.Length > 0)
                {
                    occlumArgs = $"run --env \"{envString}\" {path} {argsString}";
                }

                // Start the Occlum process
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "occlum",
                    Arguments = occlumArgs,
                    WorkingDirectory = _options.InstanceDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Set simulation mode if needed
                if (Environment.GetEnvironmentVariable("OCCLUM_SIMULATION") == "1")
                {
                    processStartInfo.EnvironmentVariables["OCCLUM_SIMULATION_MODE"] = "1";
                }

                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        throw new OcclumExecutionException($"Failed to start Occlum process for command {path}");
                    }

                    await process.WaitForExitAsync();
                    _logger.LogInformation("Command {Path} executed with exit code {ExitCode}", path, process.ExitCode);
                    return process.ExitCode;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute command {Path}", path);
                throw new OcclumExecutionException($"Failed to execute command {path}", ex);
            }
        }

        /// <summary>
        /// Executes a JavaScript file in the Occlum instance using Node.js.
        /// </summary>
        /// <param name="scriptPath">The path to the JavaScript file.</param>
        /// <param name="args">The arguments for the JavaScript file.</param>
        /// <param name="env">The environment variables for the JavaScript file.</param>
        /// <returns>The exit code of the command.</returns>
        public async Task<int> ExecuteJavaScriptFileAsync(string scriptPath, string[] args, string[] env = null)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(scriptPath))
            {
                throw new ArgumentException("Script path cannot be null or empty", nameof(scriptPath));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            // Use empty environment if not provided
            env ??= Array.Empty<string>();

            _logger.LogInformation("Executing JavaScript file {ScriptPath} with {ArgCount} arguments and {EnvCount} environment variables",
                scriptPath, args.Length, env.Length);

            try
            {
                // Create the Node.js arguments
                var nodeArgs = new List<string> { scriptPath };
                nodeArgs.AddRange(args);

                // Execute the JavaScript file using Node.js
                return await ExecuteCommandAsync(_options.NodeJsPath, nodeArgs.ToArray(), env);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute JavaScript file {ScriptPath}", scriptPath);
                throw new OcclumExecutionException($"Failed to execute JavaScript file {scriptPath}", ex);
            }
        }

        /// <summary>
        /// Executes JavaScript code in the Occlum instance using Node.js.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="args">The arguments for the JavaScript code.</param>
        /// <param name="env">The environment variables for the JavaScript code.</param>
        /// <returns>The exit code of the command.</returns>
        public async Task<int> ExecuteJavaScriptCodeAsync(string code, string[] args, string[] env = null)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Code cannot be null or empty", nameof(code));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            // Use empty environment if not provided
            env ??= Array.Empty<string>();

            _logger.LogInformation("Executing JavaScript code with {ArgCount} arguments and {EnvCount} environment variables",
                args.Length, env.Length);

            try
            {
                // Create a temporary file with the JavaScript code
                string tempDir = _options.TempDir;
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                string tempFile = Path.Combine(tempDir, $"temp-{Guid.NewGuid()}.js");
                await File.WriteAllTextAsync(tempFile, code);

                try
                {
                    // Execute the JavaScript file
                    return await ExecuteJavaScriptFileAsync(tempFile, args, env);
                }
                finally
                {
                    // Delete the temporary file
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute JavaScript code");
                throw new OcclumExecutionException("Failed to execute JavaScript code", ex);
            }
        }

        /// <summary>
        /// Checks if Occlum support is enabled in the enclave.
        /// </summary>
        /// <returns>True if Occlum support is enabled, false otherwise.</returns>
        public bool IsSupported()
        {
            CheckDisposed();

            try
            {
                // Check if the Occlum command is available
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "occlum",
                    Arguments = "version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        _logger.LogWarning("Failed to start Occlum version process");
                        return false;
                    }

                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if Occlum support is enabled");
                return false;
            }
        }

        /// <summary>
        /// Gets the version of Occlum.
        /// </summary>
        /// <returns>The version of Occlum.</returns>
        public string GetVersion()
        {
            CheckDisposed();

            try
            {
                // Get the Occlum version
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "occlum",
                    Arguments = "version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        _logger.LogWarning("Failed to start Occlum version process");
                        return "Unknown";
                    }

                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        _logger.LogWarning("Occlum version failed with exit code {ExitCode}", process.ExitCode);
                        return "Unknown";
                    }

                    string output = process.StandardOutput.ReadToEnd().Trim();
                    return output;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting Occlum version");
                return "Unknown";
            }
        }

        /// <summary>
        /// Updates the configuration of the Occlum instance.
        /// </summary>
        /// <param name="options">The new options to apply to the Occlum instance.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task UpdateConfigurationAsync(OcclumOptions options)
        {
            CheckDisposed();

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _logger.LogInformation("Updating Occlum instance configuration: InstanceDir={InstanceDir}, LogLevel={LogLevel}, NodeJsPath={NodeJsPath}, TempDir={TempDir}",
                options.InstanceDir, options.LogLevel, options.NodeJsPath, options.TempDir);

            try
            {
                // Update the options
                bool instanceDirChanged = _options.InstanceDir != options.InstanceDir;
                bool logLevelChanged = _options.LogLevel != options.LogLevel;

                // Update the options
                _options.InstanceDir = options.InstanceDir;
                _options.LogLevel = options.LogLevel;
                _options.NodeJsPath = options.NodeJsPath;
                _options.TempDir = options.TempDir;

                // If the instance directory changed, we need to reinitialize
                if (_initialized && (instanceDirChanged || logLevelChanged))
                {
                    _logger.LogInformation("Instance directory or log level changed, reinitializing Occlum instance");
                    _initialized = false;
                    await InitializeAsync();
                }

                _logger.LogInformation("Occlum instance configuration updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update Occlum instance configuration");
                throw new OcclumConfigurationException("Failed to update Occlum instance configuration", ex);
            }
        }

        /// <summary>
        /// Disposes the Occlum manager.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the Occlum manager.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose the Occlum process if it exists
                    _occlumProcess?.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Checks if the object has been disposed and throws an exception if it has.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OcclumManager));
            }
        }
    }
}
