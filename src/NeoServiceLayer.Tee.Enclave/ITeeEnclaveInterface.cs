using System;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Enclave
{
    /// <summary>
    /// Interface for Trusted Execution Environment (TEE) enclave operations.
    /// This interface provides a unified abstraction over different TEE implementations
    /// such as Intel SGX and Open Enclave.
    /// </summary>
    public interface ITeeEnclaveInterface : IDisposable
    {
        /// <summary>
        /// Gets the enclave ID.
        /// </summary>
        /// <returns>The enclave ID.</returns>
        IntPtr GetEnclaveId();

        /// <summary>
        /// Gets the enclave measurement (MRENCLAVE).
        /// </summary>
        /// <returns>The enclave measurement.</returns>
        byte[] GetMrEnclave();

        /// <summary>
        /// Gets the signer measurement (MRSIGNER).
        /// </summary>
        /// <returns>The signer measurement.</returns>
        byte[] GetMrSigner();

        /// <summary>
        /// Gets random bytes from the enclave.
        /// </summary>
        /// <param name="length">The number of random bytes to get.</param>
        /// <returns>The random bytes.</returns>
        byte[] GetRandomBytes(int length);

        /// <summary>
        /// Signs data using the enclave's private key.
        /// </summary>
        /// <param name="data">The data to sign.</param>
        /// <returns>The signature.</returns>
        byte[] SignData(byte[] data);

        /// <summary>
        /// Verifies a signature using the enclave's public key.
        /// </summary>
        /// <param name="data">The data that was signed.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        bool VerifySignature(byte[] data, byte[] signature);

        /// <summary>
        /// Seals data using the enclave's sealing key.
        /// </summary>
        /// <param name="data">The data to seal.</param>
        /// <returns>The sealed data.</returns>
        byte[] SealData(byte[] data);

        /// <summary>
        /// Unseals data using the enclave's sealing key.
        /// </summary>
        /// <param name="sealedData">The sealed data.</param>
        /// <returns>The unsealed data.</returns>
        byte[] UnsealData(byte[] sealedData);

        /// <summary>
        /// Executes JavaScript code in the enclave.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data as JSON.</param>
        /// <param name="secrets">The secrets as JSON.</param>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>The result as JSON.</returns>
        Task<string> ExecuteJavaScriptAsync(string code, string input, string secrets, string functionId, string userId);

        /// <summary>
        /// Records execution metrics.
        /// </summary>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="gasUsed">The amount of gas used.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RecordExecutionMetricsAsync(string functionId, string userId, long gasUsed);

        /// <summary>
        /// Records execution failure.
        /// </summary>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RecordExecutionFailureAsync(string functionId, string userId, string errorMessage);

        /// <summary>
        /// Stores a user secret.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretName">The secret name.</param>
        /// <param name="secretValue">The secret value.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StoreUserSecretAsync(string userId, string secretName, string secretValue);

        /// <summary>
        /// Gets a user secret.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretName">The secret name.</param>
        /// <returns>The secret value.</returns>
        Task<string> GetUserSecretAsync(string userId, string secretName);

        /// <summary>
        /// Deletes a user secret.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretName">The secret name.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteUserSecretAsync(string userId, string secretName);

        /// <summary>
        /// Gets the attestation report.
        /// </summary>
        /// <param name="reportData">The report data.</param>
        /// <returns>The attestation report.</returns>
        byte[] GetAttestationReport(byte[] reportData);

        /// <summary>
        /// Gets the attestation report asynchronously.
        /// </summary>
        /// <param name="reportData">The report data.</param>
        /// <returns>The attestation report.</returns>
        Task<byte[]> GetAttestationReportAsync(byte[] reportData);

        /// <summary>
        /// Verifies an attestation report.
        /// </summary>
        /// <param name="report">The attestation report to verify.</param>
        /// <returns>True if the report is valid, false otherwise.</returns>
        Task<bool> VerifyAttestationReportAsync(byte[] report);

        /// <summary>
        /// Gets the enclave measurement (MRENCLAVE) asynchronously.
        /// </summary>
        /// <returns>The enclave measurement.</returns>
        Task<byte[]> GetMrEnclaveAsync();

        /// <summary>
        /// Gets the signer measurement (MRSIGNER) asynchronously.
        /// </summary>
        /// <returns>The signer measurement.</returns>
        Task<byte[]> GetMrSignerAsync();

        /// <summary>
        /// Stores persistent data.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="data">The data.</param>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        Task<bool> StorePersistentDataAsync(string key, byte[] data);

        /// <summary>
        /// Retrieves persistent data.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The data.</returns>
        Task<byte[]> RetrievePersistentDataAsync(string key);

        /// <summary>
        /// Removes persistent data.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        Task<bool> RemovePersistentDataAsync(string key);

        /// <summary>
        /// Begins a transaction.
        /// </summary>
        /// <returns>The transaction ID.</returns>
        Task<ulong> BeginTransactionAsync();

        /// <summary>
        /// Stores data in a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="key">The key.</param>
        /// <param name="data">The data.</param>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        Task<bool> StoreInTransactionAsync(ulong transactionId, string key, byte[] data);

        /// <summary>
        /// Commits a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        Task<bool> CommitTransactionAsync(ulong transactionId);

        /// <summary>
        /// Rolls back a transaction.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        Task<bool> RollbackTransactionAsync(ulong transactionId);

        /// <summary>
        /// Stores a JavaScript function.
        /// </summary>
        /// <param name="functionId">The function ID.</param>
        /// <param name="code">The JavaScript code.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        Task<bool> StoreJavaScriptFunctionAsync(string functionId, string code, string userId);

        /// <summary>
        /// Verifies compliance of JavaScript code.
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="functionId">The function ID.</param>
        /// <param name="rules">The compliance rules as JSON.</param>
        /// <returns>The compliance verification result as JSON.</returns>
        Task<string> VerifyComplianceAsync(string code, string userId, string functionId, string rules);

        /// <summary>
        /// Gets the compliance status of a function.
        /// </summary>
        /// <param name="functionId">The function ID.</param>
        /// <param name="jurisdiction">The jurisdiction.</param>
        /// <returns>The compliance status as JSON.</returns>
        Task<string> GetComplianceStatusAsync(string functionId, string jurisdiction);

        /// <summary>
        /// Sets compliance rules for a jurisdiction.
        /// </summary>
        /// <param name="jurisdiction">The jurisdiction.</param>
        /// <param name="rules">The compliance rules as JSON.</param>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        Task<bool> SetComplianceRulesAsync(string jurisdiction, string rules);

        /// <summary>
        /// Gets compliance rules for a jurisdiction.
        /// </summary>
        /// <param name="jurisdiction">The jurisdiction.</param>
        /// <returns>The compliance rules as JSON.</returns>
        Task<string> GetComplianceRulesAsync(string jurisdiction);

        /// <summary>
        /// Verifies a user's identity.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="identityData">The identity data as JSON.</param>
        /// <param name="jurisdiction">The jurisdiction.</param>
        /// <returns>The verification result as JSON.</returns>
        Task<string> VerifyIdentityAsync(string userId, string identityData, string jurisdiction);

        /// <summary>
        /// Gets a user's identity status.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="jurisdiction">The jurisdiction.</param>
        /// <returns>The identity status as JSON.</returns>
        Task<string> GetIdentityStatusAsync(string userId, string jurisdiction);

        /// <summary>
        /// Registers a trigger.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="condition">The trigger condition as JSON.</param>
        /// <returns>The trigger ID.</returns>
        Task<string> RegisterTriggerAsync(string eventType, string functionId, string userId, string condition);

        /// <summary>
        /// Unregisters a trigger.
        /// </summary>
        /// <param name="triggerId">The trigger ID.</param>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        Task<bool> UnregisterTriggerAsync(string triggerId);

        /// <summary>
        /// Processes a blockchain event.
        /// </summary>
        /// <param name="eventData">The event data as JSON.</param>
        /// <returns>The number of triggers processed.</returns>
        Task<int> ProcessBlockchainEventAsync(string eventData);

        /// <summary>
        /// Corrupts storage data for testing purposes.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="data">The corrupted data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CorruptStorageDataInternalAsync(string key, byte[] data);

        /// <summary>
        /// Tampers with storage data for testing purposes.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="data">The tampered data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task TamperWithStorageDataInternalAsync(string key, byte[] data);

        /// <summary>
        /// Simulates an enclave restart for testing purposes.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SimulateEnclaveRestartInternalAsync();
    }
}
