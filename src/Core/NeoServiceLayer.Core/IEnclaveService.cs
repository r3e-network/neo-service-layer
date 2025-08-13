using System.Threading.Tasks;

namespace NeoServiceLayer.Core;

/// <summary>
/// Interface for services that require enclave operations.
/// This interface is separate from core service functionality to avoid circular dependencies.
/// </summary>
public interface IEnclaveService : IService
{
    /// <summary>
    /// Gets a value indicating whether the service has enclave capabilities.
    /// </summary>
    bool HasEnclaveCapabilities { get; }

    /// <summary>
    /// Gets a value indicating whether the service's enclave is initialized.
    /// </summary>
    bool IsEnclaveInitialized { get; }

    /// <summary>
    /// Initializes the enclave for this service.
    /// This is called after regular service initialization if enclave capabilities are required.
    /// </summary>
    /// <returns>True if enclave initialization was successful; otherwise, false.</returns>
    Task<bool> InitializeEnclaveAsync();

    /// <summary>
    /// Gets attestation information for this service's enclave.
    /// </summary>
    /// <returns>Attestation data as a JSON string, or null if not available.</returns>
    Task<string?> GetAttestationAsync();

    /// <summary>
    /// Validates that the enclave is properly initialized and secure.
    /// </summary>
    /// <returns>True if the enclave is valid and secure; otherwise, false.</returns>
    Task<bool> ValidateEnclaveAsync();
}
