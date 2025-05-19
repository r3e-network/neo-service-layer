using System;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Interfaces
{
    /// <summary>
    /// Base interface for all Trusted Execution Environment (TEE) interactions.
    /// This interface provides a unified abstraction over different TEE implementations
    /// such as Intel SGX and Open Enclave.
    /// </summary>
    public interface ITeeInterface : IDisposable
    {
        /// <summary>
        /// Initializes the TEE interface.
        /// </summary>
        void Initialize();

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
        /// <param name="data">The data to verify.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        bool VerifySignature(byte[] data, byte[] signature);

        /// <summary>
        /// Seals data to the enclave.
        /// </summary>
        /// <param name="data">The data to seal.</param>
        /// <returns>The sealed data.</returns>
        byte[] SealData(byte[] data);

        /// <summary>
        /// Unseals data that was sealed to the enclave.
        /// </summary>
        /// <param name="sealedData">The sealed data to unseal.</param>
        /// <returns>The unsealed data.</returns>
        byte[] UnsealData(byte[] sealedData);

        /// <summary>
        /// Gets an attestation report from the enclave.
        /// </summary>
        /// <param name="reportData">The report data to include in the attestation.</param>
        /// <returns>The attestation report.</returns>
        byte[] GetAttestationReport(byte[] reportData);

        /// <summary>
        /// Verifies an attestation report.
        /// </summary>
        /// <param name="report">The attestation report to verify.</param>
        /// <param name="expectedMrEnclave">The expected MRENCLAVE value, or null to skip this check.</param>
        /// <param name="expectedMrSigner">The expected MRSIGNER value, or null to skip this check.</param>
        /// <returns>True if the attestation report is valid, false otherwise.</returns>
        bool VerifyAttestationReport(byte[] report, byte[] expectedMrEnclave, byte[] expectedMrSigner);

        /// <summary>
        /// Executes JavaScript code in the enclave.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data as a JSON string.</param>
        /// <param name="secrets">The secrets data as a JSON string.</param>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>The result of the execution as a JSON string.</returns>
        Task<string> ExecuteJavaScriptAsync(string code, string input, string secrets, string functionId, string userId);

        /// <summary>
        /// Executes JavaScript code in the enclave with gas accounting.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="input">The input data as a JSON string.</param>
        /// <param name="secrets">The secrets data as a JSON string.</param>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="gasUsed">Output parameter for the amount of gas used.</param>
        /// <returns>The result of the execution as a JSON string.</returns>
        Task<string> ExecuteJavaScriptWithGasAsync(string code, string input, string secrets, string functionId, string userId, out ulong gasUsed);

        /// <summary>
        /// Stores a user secret in the enclave.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretName">The secret name.</param>
        /// <param name="secretValue">The secret value.</param>
        /// <returns>True if the secret was stored successfully, false otherwise.</returns>
        Task<bool> StoreUserSecretAsync(string userId, string secretName, string secretValue);

        /// <summary>
        /// Gets a user secret from the enclave.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretName">The secret name.</param>
        /// <returns>The secret value, or null if the secret does not exist.</returns>
        Task<string> GetUserSecretAsync(string userId, string secretName);

        /// <summary>
        /// Deletes a user secret from the enclave.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="secretName">The secret name.</param>
        /// <returns>True if the secret was deleted successfully, false otherwise.</returns>
        Task<bool> DeleteUserSecretAsync(string userId, string secretName);

        /// <summary>
        /// Lists all user secrets in the enclave.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>An array of secret names.</returns>
        Task<string[]> ListUserSecretsAsync(string userId);

        /// <summary>
        /// Stores data in persistent storage.
        /// </summary>
        /// <param name="key">The key to store the data under.</param>
        /// <param name="data">The data to store.</param>
        /// <returns>True if the data was stored successfully, false otherwise.</returns>
        Task<bool> StorePersistentDataAsync(string key, byte[] data);

        /// <summary>
        /// Retrieves data from persistent storage.
        /// </summary>
        /// <param name="key">The key to retrieve the data for.</param>
        /// <returns>The retrieved data, or null if the key does not exist.</returns>
        Task<byte[]> RetrievePersistentDataAsync(string key);

        /// <summary>
        /// Deletes data from persistent storage.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        /// <returns>True if the data was deleted successfully, false otherwise.</returns>
        Task<bool> DeletePersistentDataAsync(string key);

        /// <summary>
        /// Checks if data exists in persistent storage.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        Task<bool> PersistentDataExistsAsync(string key);

        /// <summary>
        /// Lists all keys in persistent storage.
        /// </summary>
        /// <returns>An array of keys.</returns>
        Task<string[]> ListPersistentDataAsync();

        #region Event Trigger Methods

        /// <summary>
        /// Registers a trigger for a specific event.
        /// </summary>
        /// <param name="eventType">The type of event to trigger on.</param>
        /// <param name="functionId">The ID of the function to execute.</param>
        /// <param name="userId">The ID of the user who owns the function.</param>
        /// <param name="condition">The condition under which the trigger should fire (JSON string).</param>
        /// <returns>The ID of the registered trigger, or empty string if registration failed.</returns>
        Task<string> RegisterTriggerAsync(string eventType, string functionId, string userId, string condition);

        /// <summary>
        /// Unregisters a trigger.
        /// </summary>
        /// <param name="triggerId">The ID of the trigger to unregister.</param>
        /// <returns>True if the trigger was unregistered, false otherwise.</returns>
        Task<bool> UnregisterTriggerAsync(string triggerId);

        /// <summary>
        /// Gets all triggers for a specific event type.
        /// </summary>
        /// <param name="eventType">The type of event.</param>
        /// <returns>An array of trigger IDs.</returns>
        Task<string[]> GetTriggersForEventAsync(string eventType);

        /// <summary>
        /// Gets information about a specific trigger.
        /// </summary>
        /// <param name="triggerId">The ID of the trigger.</param>
        /// <returns>A JSON string containing information about the trigger, or empty string if not found.</returns>
        Task<string> GetTriggerInfoAsync(string triggerId);

        /// <summary>
        /// Processes a blockchain event.
        /// </summary>
        /// <param name="eventData">The event data (JSON string).</param>
        /// <returns>The number of triggers executed.</returns>
        Task<int> ProcessBlockchainEventAsync(string eventData);

        /// <summary>
        /// Processes scheduled triggers.
        /// </summary>
        /// <param name="currentTime">The current time in seconds since the epoch.</param>
        /// <returns>The number of triggers executed.</returns>
        Task<int> ProcessScheduledTriggersAsync(ulong currentTime);

        #endregion

        #region Randomness Service Methods

        /// <summary>
        /// Generates a random number between min and max (inclusive).
        /// </summary>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="userId">The ID of the user requesting the random number.</param>
        /// <param name="requestId">The ID of the request.</param>
        /// <returns>The generated random number.</returns>
        Task<ulong> GenerateRandomNumberAsync(ulong min, ulong max, string userId, string requestId);

        /// <summary>
        /// Verifies a random number.
        /// </summary>
        /// <param name="randomNumber">The random number to verify.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="userId">The ID of the user who requested the random number.</param>
        /// <param name="requestId">The ID of the request.</param>
        /// <param name="proof">The proof of the random number.</param>
        /// <returns>True if the random number is valid, false otherwise.</returns>
        Task<bool> VerifyRandomNumberAsync(ulong randomNumber, ulong min, ulong max, string userId, string requestId, string proof);

        /// <summary>
        /// Gets the proof for a random number.
        /// </summary>
        /// <param name="randomNumber">The random number.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="userId">The ID of the user who requested the random number.</param>
        /// <param name="requestId">The ID of the request.</param>
        /// <returns>The proof of the random number.</returns>
        Task<string> GetRandomNumberProofAsync(ulong randomNumber, ulong min, ulong max, string userId, string requestId);

        /// <summary>
        /// Generates a random seed.
        /// </summary>
        /// <param name="userId">The ID of the user requesting the seed.</param>
        /// <param name="requestId">The ID of the request.</param>
        /// <returns>The generated seed.</returns>
        Task<string> GenerateSeedAsync(string userId, string requestId);

        #endregion

        #region Compliance Service Methods

        /// <summary>
        /// Verifies a JavaScript function for compliance.
        /// </summary>
        /// <param name="code">The JavaScript code to verify.</param>
        /// <param name="userId">The ID of the user who owns the function.</param>
        /// <param name="functionId">The ID of the function.</param>
        /// <param name="complianceRules">The compliance rules to check against (JSON string).</param>
        /// <returns>A JSON string containing the verification result.</returns>
        Task<string> VerifyComplianceAsync(string code, string userId, string functionId, string complianceRules);

        /// <summary>
        /// Gets the compliance rules for a specific jurisdiction.
        /// </summary>
        /// <param name="jurisdiction">The jurisdiction code (e.g., "US", "EU", "JP").</param>
        /// <returns>A JSON string containing the compliance rules.</returns>
        Task<string> GetComplianceRulesAsync(string jurisdiction);

        /// <summary>
        /// Sets the compliance rules for a specific jurisdiction.
        /// </summary>
        /// <param name="jurisdiction">The jurisdiction code (e.g., "US", "EU", "JP").</param>
        /// <param name="rules">The compliance rules (JSON string).</param>
        /// <returns>True if the rules were set successfully, false otherwise.</returns>
        Task<bool> SetComplianceRulesAsync(string jurisdiction, string rules);

        /// <summary>
        /// Gets the compliance status for a specific function.
        /// </summary>
        /// <param name="functionId">The ID of the function.</param>
        /// <param name="jurisdiction">The jurisdiction code (e.g., "US", "EU", "JP").</param>
        /// <returns>A JSON string containing the compliance status.</returns>
        Task<string> GetComplianceStatusAsync(string functionId, string jurisdiction);

        /// <summary>
        /// Verifies a user's identity.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="identityData">The identity data (JSON string).</param>
        /// <param name="jurisdiction">The jurisdiction code (e.g., "US", "EU", "JP").</param>
        /// <returns>A JSON string containing the verification result.</returns>
        Task<string> VerifyIdentityAsync(string userId, string identityData, string jurisdiction);

        /// <summary>
        /// Gets the identity verification status for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="jurisdiction">The jurisdiction code (e.g., "US", "EU", "JP").</param>
        /// <returns>A JSON string containing the identity verification status.</returns>
        Task<string> GetIdentityStatusAsync(string userId, string jurisdiction);

        #endregion
    }
}
