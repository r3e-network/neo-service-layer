using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Core.Interfaces
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
        System.Threading.Tasks.Task<bool> InitializeTeeAsync();

        /// <summary>
        /// Executes a task in the TEE.
        /// </summary>
        /// <param name="task">The task to execute.</param>
        /// <returns>The result of the task execution.</returns>
        System.Threading.Tasks.Task<Dictionary<string, object>> ExecuteTaskAsync(NeoServiceLayer.Core.Models.Task task);

        /// <summary>
        /// Sends a message to the TEE.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The response message from the TEE.</returns>
        System.Threading.Tasks.Task<TeeMessage> SendMessageAsync(TeeMessage message);

        /// <summary>
        /// Gets the attestation proof from the TEE.
        /// </summary>
        /// <returns>The attestation proof.</returns>
        System.Threading.Tasks.Task<AttestationProof> GetAttestationProofAsync();

        /// <summary>
        /// Verifies an attestation proof.
        /// </summary>
        /// <param name="attestationProof">The attestation proof to verify.</param>
        /// <returns>True if the attestation proof is valid, false otherwise.</returns>
        System.Threading.Tasks.Task<bool> VerifyAttestationProofAsync(AttestationProof attestationProof);

        /// <summary>
        /// Gets the status of the TEE.
        /// </summary>
        /// <returns>The TEE status.</returns>
        System.Threading.Tasks.Task<Core.Models.TeeStatus> GetStatusAsync();
    }
}
