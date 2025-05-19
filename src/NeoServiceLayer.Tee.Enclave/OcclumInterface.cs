using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NeoServiceLayer.Tee.Enclave
{
    /// <summary>
    /// Occlum LibOS integration for the Neo Service Layer.
    /// </summary>
    public class OcclumInterface : IOcclumInterface, IDisposable
    {
        private readonly ILogger<OcclumInterface> _logger;
        private readonly IPersistentStorageService _storageService;
        private readonly OcclumOptions _options;
        private bool _initialized = false;
        private bool _disposed = false;
        private string _instanceId;

        // Native methods for Occlum-specific operations
        [DllImport("occlum_pal", CallingConvention = CallingConvention.Cdecl)]
        private static extern int occlum_pal_init([MarshalAs(UnmanagedType.LPStr)] string instance_dir);

        [DllImport("occlum_pal", CallingConvention = CallingConvention.Cdecl)]
        private static extern int occlum_pal_create_process([MarshalAs(UnmanagedType.LPStr)] string path, 
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] argv,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] env,
            out IntPtr proc);

        [DllImport("occlum_pal", CallingConvention = CallingConvention.Cdecl)]
        private static extern int occlum_pal_exec(IntPtr proc);

        [DllImport("occlum_pal", CallingConvention = CallingConvention.Cdecl)]
        private static extern int occlum_pal_get_exit_status(IntPtr proc, out int exit_status);

        [DllImport("occlum_pal", CallingConvention = CallingConvention.Cdecl)]
        private static extern int occlum_pal_destroy_process(IntPtr proc);

        [DllImport("occlum_pal", CallingConvention = CallingConvention.Cdecl)]
        private static extern int occlum_pal_get_random([Out] byte[] buf, int len);

        [DllImport("occlum_pal", CallingConvention = CallingConvention.Cdecl)]
        private static extern int occlum_pal_destroy();

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumInterface"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The Occlum options.</param>
        /// <param name="storageFactory">The storage factory.</param>
        public OcclumInterface(
            ILogger<OcclumInterface> logger,
            IOptions<OcclumOptions> options,
            Func<IPersistentStorageService> storageFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            
            if (storageFactory == null)
            {
                throw new ArgumentNullException(nameof(storageFactory));
            }
            
            _storageService = storageFactory();
            
            // Generate a unique instance ID
            _instanceId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Initializes the Occlum environment.
        /// </summary>
        /// <returns>True if initialization is successful, false otherwise.</returns>
        public async Task<bool> InitializeAsync()
        {
            if (_initialized)
            {
                return true;
            }

            try
            {
                _logger.LogInformation("Initializing Occlum with instance directory: {InstanceDir}", _options.InstanceDir);

                // Check if we're in simulation mode
                bool isSimulationMode = Environment.GetEnvironmentVariable("OCCLUM_SIMULATION") == "1";
                
                if (!isSimulationMode)
                {
                    // Initialize the Occlum PAL
                    int result = occlum_pal_init(_options.InstanceDir);
                    if (result != 0)
                    {
                        _logger.LogError("Failed to initialize Occlum PAL: error code {Result}", result);
                        return false;
                    }
                }
                else
                {
                    _logger.LogInformation("Running in simulation mode, skipping Occlum PAL initialization");
                }

                // Initialize storage
                var storageOptions = new PersistentStorageOptions
                {
                    StoragePath = Path.Combine(_options.InstanceDir, "data"),
                    EnableEncryption = true,
                    EncryptionKey = GenerateEncryptionKey(),
                    EnableCompression = true,
                    CompressionLevel = 6,
                    CreateIfNotExists = true,
                    EnableCaching = true,
                    EnableAutoFlush = true
                };

                await _storageService.InitializeAsync(storageOptions);
                
                _initialized = true;
                _logger.LogInformation("Occlum initialized successfully");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Occlum");
                return false;
            }
        }

        /// <summary>
        /// Executes a command in the Occlum instance.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="args">The command arguments.</param>
        /// <returns>The command output.</returns>
        public async Task<string> ExecuteCommandAsync(string command, string[] args)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentNullException(nameof(command));
            }

            EnsureInitialized();

            try
            {
                // Prepare the arguments array for Occlum
                string[] argv = new string[args.Length + 1];
                argv[0] = command;
                Array.Copy(args, 0, argv, 1, args.Length);

                // Set up environment variables
                string[] env = new[]
                {
                    "PATH=/bin:/usr/bin",
                    "LD_LIBRARY_PATH=/lib:/usr/lib",
                    "OCCLUM=yes"
                };

                bool isSimulationMode = Environment.GetEnvironmentVariable("OCCLUM_SIMULATION") == "1";
                
                if (isSimulationMode)
                {
                    // In simulation mode, use Process to execute the command
                    _logger.LogInformation("Running in simulation mode, executing command: {Command}", command);
                    
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = string.Join(" ", args),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    var process = Process.Start(processStartInfo);
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode != 0)
                    {
                        _logger.LogWarning("Command exited with non-zero code: {ExitCode}. Error: {Error}", 
                            process.ExitCode, error);
                    }
                    
                    return output;
                }
                else
                {
                    // In real mode, use Occlum PAL to execute the command
                    IntPtr proc = IntPtr.Zero;
                    int result = occlum_pal_create_process(command, argv, env, out proc);
                    
                    if (result != 0 || proc == IntPtr.Zero)
                    {
                        throw new InvalidOperationException($"Failed to create Occlum process: error code {result}");
                    }
                    
                    try
                    {
                        result = occlum_pal_exec(proc);
                        if (result != 0)
                        {
                            throw new InvalidOperationException($"Failed to execute Occlum process: error code {result}");
                        }
                        
                        int exitStatus;
                        result = occlum_pal_get_exit_status(proc, out exitStatus);
                        if (result != 0)
                        {
                            throw new InvalidOperationException($"Failed to get exit status: error code {result}");
                        }
                        
                        if (exitStatus != 0)
                        {
                            _logger.LogWarning("Command exited with non-zero code: {ExitCode}", exitStatus);
                        }
                        
                        // TODO: In a real implementation, we would need to capture the output of the command
                        // This would likely require setting up pipes or redirecting output to a file
                        // For now, we just return a placeholder
                        return $"Command executed with exit code: {exitStatus}";
                    }
                    finally
                    {
                        if (proc != IntPtr.Zero)
                        {
                            occlum_pal_destroy_process(proc);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing command in Occlum: {Command}", command);
                throw;
            }
        }

        /// <summary>
        /// Gets the instance ID of the Occlum instance.
        /// </summary>
        /// <returns>The instance ID.</returns>
        public string GetInstanceId()
        {
            EnsureInitialized();
            return _instanceId;
        }

        /// <summary>
        /// Records execution metrics for a JavaScript function.
        /// </summary>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="gasUsed">The amount of GAS used.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RecordExecutionMetricsAsync(string functionId, string userId, long gasUsed)
        {
            EnsureInitialized();
            
            try
            {
                var metrics = new ExecutionMetrics
                {
                    FunctionId = functionId,
                    UserId = userId,
                    GasUsed = gasUsed,
                    Timestamp = DateTime.UtcNow
                };
                
                string key = $"metrics:{functionId}:{userId}:{DateTime.UtcNow.Ticks}";
                string json = System.Text.Json.JsonSerializer.Serialize(metrics);
                byte[] data = Encoding.UTF8.GetBytes(json);
                
                await _storageService.WriteAsync(key, data);
                
                _logger.LogInformation("Recorded execution metrics for function {FunctionId}, user {UserId}, gas used {GasUsed}", 
                    functionId, userId, gasUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording execution metrics for function {FunctionId}, user {UserId}", 
                    functionId, userId);
            }
        }

        /// <summary>
        /// Records execution failure for a JavaScript function.
        /// </summary>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RecordExecutionFailureAsync(string functionId, string userId, string errorMessage)
        {
            EnsureInitialized();
            
            try
            {
                var failure = new ExecutionFailure
                {
                    FunctionId = functionId,
                    UserId = userId,
                    ErrorMessage = errorMessage,
                    Timestamp = DateTime.UtcNow
                };
                
                string key = $"failures:{functionId}:{userId}:{DateTime.UtcNow.Ticks}";
                string json = System.Text.Json.JsonSerializer.Serialize(failure);
                byte[] data = Encoding.UTF8.GetBytes(json);
                
                await _storageService.WriteAsync(key, data);
                
                _logger.LogInformation("Recorded execution failure for function {FunctionId}, user {UserId}: {ErrorMessage}", 
                    functionId, userId, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording execution failure for function {FunctionId}, user {UserId}", 
                    functionId, userId);
            }
        }

        /// <summary>
        /// Verifies the integrity of the Occlum instance.
        /// </summary>
        /// <returns>True if the instance is valid, false otherwise.</returns>
        public Task<bool> VerifyIntegrityAsync()
        {
            EnsureInitialized();
            
            // In a real implementation, this would check the integrity of the Occlum instance
            // For now, we just return true
            return Task.FromResult(true);
        }

        /// <summary>
        /// Generates a random value using the Occlum random source.
        /// </summary>
        /// <param name="size">The size of the random value in bytes.</param>
        /// <returns>The random bytes.</returns>
        public Task<byte[]> GenerateRandomBytesAsync(int size)
        {
            EnsureInitialized();
            
            if (size <= 0)
            {
                throw new ArgumentException("Size must be positive", nameof(size));
            }
            
            try
            {
                byte[] randomBytes = new byte[size];
                bool isSimulationMode = Environment.GetEnvironmentVariable("OCCLUM_SIMULATION") == "1";
                
                if (isSimulationMode)
                {
                    // In simulation mode, use .NET random number generator
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(randomBytes);
                    }
                }
                else
                {
                    // In real mode, use Occlum PAL to generate random bytes
                    int result = occlum_pal_get_random(randomBytes, size);
                    if (result != 0)
                    {
                        throw new InvalidOperationException($"Failed to generate random bytes: error code {result}");
                    }
                }
                
                return Task.FromResult(randomBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating random bytes");
                throw;
            }
        }

        /// <summary>
        /// Terminates the Occlum instance.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task TerminateAsync()
        {
            if (!_initialized)
            {
                return Task.CompletedTask;
            }
            
            try
            {
                bool isSimulationMode = Environment.GetEnvironmentVariable("OCCLUM_SIMULATION") == "1";
                
                if (!isSimulationMode)
                {
                    // In real mode, destroy the Occlum PAL
                    int result = occlum_pal_destroy();
                    if (result != 0)
                    {
                        _logger.LogWarning("Failed to destroy Occlum PAL: error code {Result}", result);
                    }
                }
                
                _initialized = false;
                _logger.LogInformation("Occlum terminated");
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating Occlum");
                throw;
            }
        }

        /// <summary>
        /// Disposes resources used by the Occlum interface.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources used by the Occlum interface.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            
            if (disposing)
            {
                // Terminate Occlum if initialized
                if (_initialized)
                {
                    TerminateAsync().GetAwaiter().GetResult();
                }
                
                // Dispose storage service if not null
                (_storageService as IDisposable)?.Dispose();
            }
            
            _disposed = true;
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Occlum interface is not initialized. Call InitializeAsync first.");
            }
        }

        private byte[] GenerateEncryptionKey()
        {
            byte[] key = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }
    }

    /// <summary>
    /// Options for Occlum.
    /// </summary>
    public class OcclumOptions
    {
        /// <summary>
        /// Gets or sets the Occlum instance directory.
        /// </summary>
        public string InstanceDir { get; set; }

        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        public string LogLevel { get; set; }

        /// <summary>
        /// Gets or sets the Node.js path.
        /// </summary>
        public string NodeJsPath { get; set; }

        /// <summary>
        /// Gets or sets the temporary directory.
        /// </summary>
        public string TempDir { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable debug mode.
        /// </summary>
        public bool EnableDebugMode { get; set; }

        /// <summary>
        /// Gets or sets the maximum memory in MB.
        /// </summary>
        public int MaxMemoryMB { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of threads.
        /// </summary>
        public int MaxThreads { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of processes.
        /// </summary>
        public int MaxProcesses { get; set; }

        /// <summary>
        /// Gets or sets the maximum execution time in seconds.
        /// </summary>
        public int MaxExecutionTimeSeconds { get; set; }
    }
    
    /// <summary>
    /// Execution metrics.
    /// </summary>
    public class ExecutionMetrics
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
        /// Gets or sets the amount of GAS used.
        /// </summary>
        public long GasUsed { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// Execution failure.
    /// </summary>
    public class ExecutionFailure
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
        /// Gets or sets the error message.
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
} 