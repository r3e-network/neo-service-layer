using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Tee.Host.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Base class for services that require enclave operations.
/// </summary>
public abstract class EnclaveServiceBase : ServiceBase, IEnclaveService
{
    private bool _isEnclaveInitialized;
    protected readonly IEnclaveManager? _enclaveManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveServiceBase"/> class.
    /// </summary>
    /// <param name="name">The name of the service.</param>
    /// <param name="description">The description of the service.</param>
    /// <param name="version">The version of the service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="enclaveManager">The enclave manager (optional).</param>
    protected EnclaveServiceBase(string name, string description, string version, ILogger logger, IEnclaveManager? enclaveManager = null)
        : base(name, description, version, logger)
    {
        _isEnclaveInitialized = false;
        _enclaveManager = enclaveManager;
    }

    /// <inheritdoc/>
    public bool IsEnclaveInitialized => _isEnclaveInitialized;

    /// <inheritdoc/>
    public virtual async Task<bool> InitializeEnclaveAsync()
    {
        if (_isEnclaveInitialized)
        {
            return true;
        }

        try
        {
            var result = await OnInitializeEnclaveAsync();
            if (result)
            {
                _isEnclaveInitialized = true;
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new EnclaveInitializationException("Failed to initialize enclave.", ex);
        }
    }

    /// <inheritdoc/>
    public override async Task<bool> InitializeAsync()
    {
        var baseResult = await base.InitializeAsync();
        if (!baseResult)
        {
            return false;
        }

        return await InitializeEnclaveAsync();
    }

    /// <summary>
    /// Called when the enclave is being initialized.
    /// </summary>
    /// <returns>True if the enclave was initialized successfully, false otherwise.</returns>
    protected abstract Task<bool> OnInitializeEnclaveAsync();

    /// <summary>
    /// Executes an operation within the enclave.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <returns>The result of the operation.</returns>
    protected virtual async Task<T> ExecuteInEnclaveAsync<T>(Func<Task<T>> operation)
    {
        if (!_isEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized. Call InitializeEnclaveAsync first.");
        }

        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing operation in enclave for service {ServiceName}", Name);
            throw;
        }
    }

    /// <summary>
    /// Ensures the enclave manager is available.
    /// </summary>
    /// <returns>The enclave manager.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the enclave manager is not available.</exception>
    protected IEnclaveManager EnsureEnclaveManager()
    {
        return _enclaveManager ?? throw new InvalidOperationException("Enclave manager is not available. Ensure it is injected during service construction.");
    }

    /// <summary>
    /// Executes an operation within the enclave without a return value.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async Task ExecuteInEnclaveAsync(Func<Task> operation)
    {
        if (!_isEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized. Call InitializeEnclaveAsync first.");
        }

        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing operation in enclave for service {ServiceName}", Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<string?> GetAttestationAsync()
    {
        if (!_isEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized. Call InitializeEnclaveAsync first.");
        }

        // Default implementation - can be overridden by derived classes
        try
        {
            if (_enclaveManager != null)
            {
                // Generate a challenge for attestation (in production, this would be provided by the requester)
                var challenge = Guid.NewGuid().ToString("N");
                var attestation = await _enclaveManager.GetAttestationReportAsync(challenge);
                return attestation;
            }

            // Return null if no enclave manager is available
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting attestation for service {ServiceName}", Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<bool> ValidateEnclaveAsync()
    {
        if (!_isEnclaveInitialized)
        {
            return false;
        }

        try
        {
            // Basic validation - can be overridden by derived classes
            if (_enclaveManager != null)
            {
                // Try to get attestation as a validation check
                var attestation = await GetAttestationAsync();
                return !string.IsNullOrEmpty(attestation);
            }

            return _isEnclaveInitialized;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating enclave for service {ServiceName}", Name);
            return false;
        }
    }

    /// <inheritdoc/>
    public virtual bool HasEnclaveCapabilities => _enclaveManager != null;
}

/// <summary>
/// Exception thrown when enclave initialization fails.
/// </summary>
public class EnclaveInitializationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveInitializationException"/> class.
    /// </summary>
    public EnclaveInitializationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveInitializationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public EnclaveInitializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveInitializationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public EnclaveInitializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
