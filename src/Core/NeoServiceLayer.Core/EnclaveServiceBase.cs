using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Core;

/// <summary>
/// Base class for services that require enclave operations.
/// This implementation avoids circular dependencies by not directly depending on Tee.Host.
/// </summary>
public abstract class EnclaveServiceBase : ServiceBase, IEnclaveService
{
    private bool _enclaveInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveServiceBase"/> class.
    /// </summary>
    protected EnclaveServiceBase(string name, string description, string version, ILogger logger)
        : base(name, description, version, logger)
    {
        HasEnclaveCapabilities = true;

        // Add enclave capability
        AddCapability<IEnclaveService>();

        // Set metadata
        SetMetadata("HasEnclaveCapabilities", true);
        SetMetadata("EnclaveType", "SGX"); // Default, can be overridden
    }

    /// <inheritdoc/>
    public bool HasEnclaveCapabilities { get; }

    /// <inheritdoc/>
    public bool IsEnclaveInitialized => _enclaveInitialized;

    /// <inheritdoc/>
    public virtual async Task<bool> InitializeEnclaveAsync()
    {
        if (_enclaveInitialized)
        {
            Logger.LogDebug("Enclave already initialized for service {ServiceName}", Name);
            return true;
        }

        try
        {
            Logger.LogInformation("Initializing enclave for service {ServiceName}", Name);

            var result = await OnInitializeEnclaveAsync();

            if (result)
            {
                _enclaveInitialized = true;
                SetMetadata("EnclaveInitializedAt", DateTime.UtcNow);
                Logger.LogInformation("Enclave initialized successfully for service {ServiceName}", Name);
            }
            else
            {
                Logger.LogError("Failed to initialize enclave for service {ServiceName}", Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing enclave for service {ServiceName}", Name);
            return false;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<string?> GetAttestationAsync()
    {
        if (!_enclaveInitialized)
        {
            Logger.LogWarning("Cannot get attestation - enclave not initialized for service {ServiceName}", Name);
            return null;
        }

        try
        {
            return await OnGetAttestationAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting attestation for service {ServiceName}", Name);
            return null;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<bool> ValidateEnclaveAsync()
    {
        if (!_enclaveInitialized)
        {
            return false;
        }

        try
        {
            return await OnValidateEnclaveAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating enclave for service {ServiceName}", Name);
            return false;
        }
    }

    /// <summary>
    /// Override the base initialization to include enclave initialization.
    /// </summary>
    public override async Task<bool> InitializeAsync()
    {
        // First initialize the base service
        var baseResult = await base.InitializeAsync();
        if (!baseResult)
        {
            return false;
        }

        // Then initialize the enclave if the service supports it
        if (HasEnclaveCapabilities)
        {
            return await InitializeEnclaveAsync();
        }

        return true;
    }

    /// <summary>
    /// Called when the enclave should be initialized.
    /// Override this method to implement enclave-specific initialization logic.
    /// </summary>
    /// <returns>True if enclave initialization was successful; otherwise, false.</returns>
    protected abstract Task<bool> OnInitializeEnclaveAsync();

    /// <summary>
    /// Called when attestation information should be retrieved.
    /// Override this method to implement enclave-specific attestation logic.
    /// </summary>
    /// <returns>Attestation data as a JSON string, or null if not available.</returns>
    protected abstract Task<string?> OnGetAttestationAsync();

    /// <summary>
    /// Called when the enclave should be validated.
    /// Override this method to implement enclave-specific validation logic.
    /// </summary>
    /// <returns>True if the enclave is valid and secure; otherwise, false.</returns>
    protected abstract Task<bool> OnValidateEnclaveAsync();

    /// <summary>
    /// Override disposal to clean up enclave resources.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _enclaveInitialized)
        {
            try
            {
                OnDisposeEnclave();
                _enclaveInitialized = false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error disposing enclave resources for service {ServiceName}", Name);
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Called when enclave resources should be disposed.
    /// Override this method to implement enclave-specific cleanup logic.
    /// </summary>
    protected virtual void OnDisposeEnclave()
    {
        // Default implementation does nothing
    }
}
