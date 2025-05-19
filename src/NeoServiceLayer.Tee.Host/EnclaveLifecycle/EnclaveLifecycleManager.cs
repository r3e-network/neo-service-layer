using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Shared.Models;
using NeoServiceLayer.Tee.Host.Exceptions;

namespace NeoServiceLayer.Tee.Host.EnclaveLifecycle
{
    /// <summary>
    /// Manages the lifecycle of enclaves, including creation, initialization, and termination.
    /// </summary>
    public class EnclaveLifecycleManager : IEnclaveLifecycleManager, IDisposable
    {
        private readonly ILogger<EnclaveLifecycleManager> _logger;
        private readonly IEnclaveInterfaceFactory _enclaveInterfaceFactory;
        private readonly ConcurrentDictionary<string, ITeeInterface> _enclaves;
        private readonly SemaphoreSlim _enclaveSemaphore;
        private readonly EnclaveLifecycleOptions _options;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnclaveLifecycleManager"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="enclaveInterfaceFactory">The factory to use for creating enclave interfaces.</param>
        /// <param name="options">The options for the enclave lifecycle manager.</param>
        public EnclaveLifecycleManager(
            ILogger<EnclaveLifecycleManager> logger,
            IEnclaveInterfaceFactory enclaveInterfaceFactory,
            EnclaveLifecycleOptions options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _enclaveInterfaceFactory = enclaveInterfaceFactory ?? throw new ArgumentNullException(nameof(enclaveInterfaceFactory));
            _options = options ?? new EnclaveLifecycleOptions();
            _enclaves = new ConcurrentDictionary<string, ITeeInterface>();
            _enclaveSemaphore = new SemaphoreSlim(_options.MaxConcurrentEnclaveCreations, _options.MaxConcurrentEnclaveCreations);
            _disposed = false;
        }

        /// <summary>
        /// Creates and initializes an enclave with the specified ID.
        /// </summary>
        /// <param name="enclaveId">The ID of the enclave to create.</param>
        /// <param name="enclavePath">The path to the enclave file.</param>
        /// <param name="simulationMode">Whether to create the enclave in simulation mode.</param>
        /// <returns>The created enclave interface.</returns>
        public async Task<ITeeInterface> CreateEnclaveAsync(string enclaveId, string enclavePath, bool simulationMode = false)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(enclaveId))
            {
                throw new ArgumentException("Enclave ID cannot be null or empty", nameof(enclaveId));
            }

            if (string.IsNullOrEmpty(enclavePath))
            {
                throw new ArgumentException("Enclave path cannot be null or empty", nameof(enclavePath));
            }

            if (!File.Exists(enclavePath))
            {
                throw new FileNotFoundException("Enclave file not found", enclavePath);
            }

            // Check if the enclave already exists
            if (_enclaves.TryGetValue(enclaveId, out var existingEnclave))
            {
                _logger.LogInformation("Enclave {EnclaveId} already exists, returning existing instance", enclaveId);
                return existingEnclave;
            }

            // Acquire a semaphore to limit concurrent enclave creations
            await _enclaveSemaphore.WaitAsync();
            try
            {
                // Check again after acquiring the semaphore
                if (_enclaves.TryGetValue(enclaveId, out existingEnclave))
                {
                    _logger.LogInformation("Enclave {EnclaveId} already exists, returning existing instance", enclaveId);
                    return existingEnclave;
                }

                _logger.LogInformation("Creating enclave {EnclaveId} from {EnclavePath}", enclaveId, enclavePath);

                // Set simulation mode if requested
                if (simulationMode)
                {
                    Environment.SetEnvironmentVariable("OCCLUM_SIMULATION", "1");
                    _logger.LogInformation("Creating enclave in simulation mode");
                }
                else
                {
                    Environment.SetEnvironmentVariable("OCCLUM_SIMULATION", "0");
                    _logger.LogInformation("Creating enclave in hardware mode");
                }

                // Create the enclave
                ITeeInterface enclaveInterface;
                try
                {
                    enclaveInterface = _enclaveInterfaceFactory.CreateOcclumInterface(enclavePath, simulationMode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create enclave {EnclaveId}", enclaveId);
                    throw new EnclaveCreationException($"Failed to create enclave {enclaveId}", ex);
                }

                // Add the enclave to the dictionary
                if (!_enclaves.TryAdd(enclaveId, enclaveInterface))
                {
                    // Another thread added the enclave while we were creating it
                    enclaveInterface.Dispose();
                    enclaveInterface = _enclaves[enclaveId];
                    _logger.LogInformation("Enclave {EnclaveId} was created by another thread, returning that instance", enclaveId);
                }
                else
                {
                    _logger.LogInformation("Enclave {EnclaveId} created successfully", enclaveId);
                }

                return enclaveInterface;
            }
            finally
            {
                _enclaveSemaphore.Release();
            }
        }

        /// <summary>
        /// Gets an existing enclave with the specified ID.
        /// </summary>
        /// <param name="enclaveId">The ID of the enclave to get.</param>
        /// <returns>The enclave interface, or null if the enclave does not exist.</returns>
        public Task<ITeeInterface> GetEnclaveAsync(string enclaveId)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(enclaveId))
            {
                throw new ArgumentException("Enclave ID cannot be null or empty", nameof(enclaveId));
            }

            if (_enclaves.TryGetValue(enclaveId, out var enclaveInterface))
            {
                _logger.LogDebug("Found enclave {EnclaveId}", enclaveId);
                return Task.FromResult(enclaveInterface);
            }

            _logger.LogWarning("Enclave {EnclaveId} not found", enclaveId);
            return Task.FromResult<ITeeInterface>(null);
        }

        /// <summary>
        /// Terminates and removes an enclave with the specified ID.
        /// </summary>
        /// <param name="enclaveId">The ID of the enclave to terminate.</param>
        /// <returns>True if the enclave was terminated, false if the enclave does not exist.</returns>
        public async Task<bool> TerminateEnclaveAsync(string enclaveId)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(enclaveId))
            {
                throw new ArgumentException("Enclave ID cannot be null or empty", nameof(enclaveId));
            }

            // Acquire a semaphore to limit concurrent enclave terminations
            await _enclaveSemaphore.WaitAsync();
            try
            {
                if (_enclaves.TryRemove(enclaveId, out var enclaveInterface))
                {
                    _logger.LogInformation("Terminating enclave {EnclaveId}", enclaveId);
                    try
                    {
                        enclaveInterface.Dispose();
                        _logger.LogInformation("Enclave {EnclaveId} terminated successfully", enclaveId);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to terminate enclave {EnclaveId}", enclaveId);
                        throw new EnclaveTerminationException($"Failed to terminate enclave {enclaveId}", ex);
                    }
                }

                _logger.LogWarning("Enclave {EnclaveId} not found for termination", enclaveId);
                return false;
            }
            finally
            {
                _enclaveSemaphore.Release();
            }
        }

        /// <summary>
        /// Gets all active enclaves.
        /// </summary>
        /// <returns>A dictionary of enclave IDs to enclave interfaces.</returns>
        public Task<IReadOnlyDictionary<string, ITeeInterface>> GetAllEnclavesAsync()
        {
            CheckDisposed();
            return Task.FromResult<IReadOnlyDictionary<string, ITeeInterface>>(_enclaves);
        }

        /// <summary>
        /// Terminates all active enclaves.
        /// </summary>
        /// <returns>The number of enclaves terminated.</returns>
        public async Task<int> TerminateAllEnclavesAsync()
        {
            CheckDisposed();

            int count = 0;
            var enclaveIds = new List<string>(_enclaves.Keys);

            foreach (var enclaveId in enclaveIds)
            {
                if (await TerminateEnclaveAsync(enclaveId))
                {
                    count++;
                }
            }

            _logger.LogInformation("Terminated {Count} enclaves", count);
            return count;
        }

        /// <summary>
        /// Disposes the enclave lifecycle manager.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the enclave lifecycle manager.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Terminate all enclaves
                    TerminateAllEnclavesAsync().GetAwaiter().GetResult();

                    // Dispose the semaphore
                    _enclaveSemaphore.Dispose();
                }

                _disposed = true;
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EnclaveLifecycleManager));
            }
        }
    }
}
