using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Shared.Models;
using Task = System.Threading.Tasks.Task;

namespace NeoServiceLayer.Tee.Host.Services
{
    /// <summary>
    /// Interface for the TEE host service.
    /// </summary>
    public interface ITeeHostService
    {
        /// <summary>
        /// Initializes the TEE.
        /// </summary>
        /// <returns>True if the TEE was initialized successfully, false otherwise.</returns>
        Task<bool> InitializeTeeAsync();

        /// <summary>
        /// Executes a task in the TEE.
        /// </summary>
        /// <param name="task">The task to execute.</param>
        /// <returns>The result of the task execution.</returns>
        Task<Dictionary<string, object>> ExecuteTaskAsync(Core.Models.Task task);

        /// <summary>
        /// Sends a message to the TEE.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The response message from the TEE.</returns>
        Task<TeeMessage> SendMessageAsync(TeeMessage message);

        /// <summary>
        /// Gets the current attestation proof from the TEE.
        /// </summary>
        /// <returns>The current attestation proof.</returns>
        Task<AttestationProof> GetAttestationProofAsync();

        /// <summary>
        /// Verifies an attestation proof.
        /// </summary>
        /// <param name="attestationProof">The attestation proof to verify.</param>
        /// <returns>True if the attestation proof is valid, false otherwise.</returns>
        Task<bool> VerifyAttestationProofAsync(AttestationProof attestationProof);

        /// <summary>
        /// Gets the status of the TEE asynchronously.
        /// </summary>
        /// <returns>The status of the TEE.</returns>
        Task<Core.Models.TeeStatus> GetStatusAsync();
    }
}
