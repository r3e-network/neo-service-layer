using NeoServiceLayer.Services.AbstractAccount.Models;
using NeoServiceLayer.Services.Core.SGX;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.AbstractAccount;

/// <summary>
/// Enclave operations for the Abstract Account Service.
/// </summary>
public partial class AbstractAccountService
{
    /// <summary>
    /// Creates an account in the enclave.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="request">The account creation request.</param>
    /// <returns>The account creation result.</returns>
    private async Task<(string AccountAddress, string MasterPublicKey, string TransactionHash)> CreateAccountInEnclaveAsync(
        string accountId, CreateAccountRequest request)
    {
        // Create account using privacy-preserving JavaScript in SGX enclave
        var accountData = new
        {
            address = request.OwnerPublicKey,
            type = "multi-sig",
            threshold = request.RecoveryThreshold,
            guardians = request.InitialGuardians
        };

        var operation = new
        {
            type = "create",
            gasLimit = 100000 // Default gas limit
        };

        var witnesses = request.InitialGuardians.Select((g, i) => new
        {
            data = g,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            signature = GenerateSimpleSignature(g, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString())
        }).ToList();

        var jsParams = new
        {
            accountData,
            operation,
            witnesses
        };

        string paramsJson = JsonSerializer.Serialize(jsParams);

        // Execute privacy-preserving account creation in SGX
#pragma warning disable CS8602 // Dereference of a possibly null reference
        string result = await _enclaveManager.ExecuteJavaScriptAsync(
            PrivacyComputingJavaScriptTemplates.AbstractAccountOperations,
            paramsJson);
#pragma warning restore CS8602

        if (string.IsNullOrEmpty(result))
            throw new InvalidOperationException("Enclave returned null or empty result");

        var resultJson = JsonSerializer.Deserialize<JsonElement>(result);

        if (!resultJson.TryGetProperty("success", out var success) || !success.GetBoolean())
        {
            throw new InvalidOperationException("Failed to create account in enclave");
        }

        // The JS template returns anonymized data, so we need to create the actual account
        // and then return the real values while keeping the privacy-preserving computation
        string actualResult = await _enclaveManager.CreateAbstractAccountAsync(accountId, JsonSerializer.Serialize(accountData), CancellationToken.None);

        var actualResultJson = JsonSerializer.Deserialize<JsonElement>(actualResult);

        string accountAddress = actualResultJson.GetProperty("account_address").GetString() ?? "";
        string masterPublicKey = actualResultJson.GetProperty("master_public_key").GetString() ?? "";
        string transactionHash = actualResultJson.GetProperty("transaction_hash").GetString() ?? "";

        return (accountAddress, masterPublicKey, transactionHash);
    }

    /// <summary>
    /// Generates a simple signature for witness validation.
    /// </summary>
    private string GenerateSimpleSignature(string data, string timestamp)
    {
        // This is a placeholder - in production, this would use proper crypto
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data + timestamp).Take(16).ToArray());
    }

    /// <summary>
    /// Executes a transaction in the enclave.
    /// </summary>
    /// <param name="request">The transaction execution request.</param>
    /// <returns>The transaction result.</returns>
    private async Task<TransactionResult> ExecuteTransactionInEnclaveAsync(ExecuteTransactionRequest request)
    {
        // First, run privacy-preserving validation in SGX
        var accountData = new
        {
            address = request.AccountId,
            type = "multi-sig",
            threshold = 2 // This should be retrieved from account info
        };

        var operation = new
        {
            type = "execute",
            toAddress = request.ToAddress,
            value = request.Value,
            data = request.Data,
            gasLimit = request.GasLimit > 0 ? request.GasLimit : 100000,
            useSessionKey = request.UseSessionKey,
            sessionKeyId = request.SessionKeyId
        };

        var witnesses = new[]
        {
            new
            {
                data = request.SessionKeyId ?? request.AccountId,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                signature = GenerateSimpleSignature(request.SessionKeyId ?? request.AccountId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString())
            }
        };

        var jsParams = new
        {
            accountData,
            operation,
            witnesses
        };

        string paramsJson = JsonSerializer.Serialize(jsParams);

        // Execute privacy-preserving transaction validation in SGX
#pragma warning disable CS8602 // Dereference of a possibly null reference
        string validationResult = await _enclaveManager.ExecuteJavaScriptAsync(
            PrivacyComputingJavaScriptTemplates.AbstractAccountOperations,
            paramsJson);
#pragma warning restore CS8602

        if (string.IsNullOrEmpty(validationResult))
            throw new InvalidOperationException("Enclave validation returned null or empty result");

        var validationJson = JsonSerializer.Deserialize<JsonElement>(validationResult);

        if (!validationJson.TryGetProperty("success", out var validationSuccess) || !validationSuccess.GetBoolean())
        {
            return new TransactionResult
            {
                TransactionHash = "",
                Success = false,
                ErrorMessage = "Transaction validation failed in privacy-preserving computation",
                GasUsed = 0,
                ExecutedAt = DateTime.UtcNow,
                Metadata = request.Metadata
            };
        }

        // If validation passes, execute the actual transaction
        var transactionData = new
        {
            accountId = request.AccountId,
            toAddress = request.ToAddress,
            value = request.Value,
            data = request.Data,
            gasLimit = request.GasLimit,
            useSessionKey = request.UseSessionKey,
            sessionKeyId = request.SessionKeyId,
            metadata = request.Metadata
        };

        string transactionDataJson = JsonSerializer.Serialize(transactionData);
        string result = await _enclaveManager.SignAndExecuteTransactionAsync(request.AccountId, transactionDataJson, CancellationToken.None);

        if (string.IsNullOrEmpty(result))
            throw new InvalidOperationException("Enclave returned null or empty result");

        var resultJson = JsonSerializer.Deserialize<JsonElement>(result);

        bool success = resultJson.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
        string transactionHash = resultJson.TryGetProperty("transaction_hash", out var hashProp) ?
            hashProp.GetString() ?? "" : "";
        long gasUsed = resultJson.TryGetProperty("gas_used", out var gasProp) ?
            gasProp.GetInt64() : 0;
        string? errorMessage = resultJson.TryGetProperty("error", out var errorProp) ?
            errorProp.GetString() : null;

        // Extract gas estimate from validation result if available
        if (validationJson.TryGetProperty("gasEstimate", out var gasEstimateProp))
        {
            gasUsed = Math.Max(gasUsed, gasEstimateProp.GetInt64());
        }

        return new TransactionResult
        {
            TransactionHash = transactionHash,
            Success = success,
            ErrorMessage = errorMessage,
            GasUsed = gasUsed,
            ExecutedAt = DateTime.UtcNow,
            Metadata = request.Metadata
        };
    }

    /// <summary>
    /// Adds a guardian in the enclave.
    /// </summary>
    /// <param name="request">The add guardian request.</param>
    /// <returns>The guardian result.</returns>
    private async Task<GuardianResult> AddGuardianInEnclaveAsync(AddGuardianRequest request)
    {
        var guardianData = new
        {
            accountId = request.AccountId,
            guardianAddress = request.GuardianAddress,
            guardianName = request.GuardianName,
            metadata = request.Metadata
        };

        string guardianDataJson = JsonSerializer.Serialize(guardianData);
#pragma warning disable CS8602 // Dereference of a possibly null reference
        string result = await _enclaveManager.AddAccountGuardianAsync(request.AccountId, guardianDataJson, CancellationToken.None);
#pragma warning restore CS8602

        if (string.IsNullOrEmpty(result))
            throw new InvalidOperationException("Enclave returned null or empty result");

        var resultJson = JsonSerializer.Deserialize<JsonElement>(result);

        bool success = resultJson.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
        string guardianId = resultJson.TryGetProperty("guardian_id", out var idProp) ?
            idProp.GetString() ?? "" : "";
        string transactionHash = resultJson.TryGetProperty("transaction_hash", out var hashProp) ?
            hashProp.GetString() ?? "" : "";
        string? errorMessage = resultJson.TryGetProperty("error", out var errorProp) ?
            errorProp.GetString() : null;

        return new GuardianResult
        {
            GuardianId = guardianId,
            Success = success,
            ErrorMessage = errorMessage,
            TransactionHash = transactionHash,
            Timestamp = DateTime.UtcNow,
            Metadata = request.Metadata
        };
    }

    /// <summary>
    /// Initiates account recovery in the enclave.
    /// </summary>
    /// <param name="request">The recovery request.</param>
    /// <returns>The recovery result.</returns>
    private async Task<RecoveryResult> InitiateRecoveryInEnclaveAsync(InitiateRecoveryRequest request)
    {
        var recoveryData = new
        {
            accountId = request.AccountId,
            newOwnerPublicKey = request.NewOwnerPublicKey,
            reason = request.Reason,
            metadata = request.Metadata
        };

        string recoveryDataJson = JsonSerializer.Serialize(recoveryData);
#pragma warning disable CS8602 // Dereference of a possibly null reference
        string result = await _enclaveManager.InitiateAccountRecoveryAsync(request.AccountId, recoveryDataJson, CancellationToken.None);
#pragma warning restore CS8602

        if (string.IsNullOrEmpty(result))
            throw new InvalidOperationException("Enclave returned null or empty result");

        var resultJson = JsonSerializer.Deserialize<JsonElement>(result);

        bool success = resultJson.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
        string recoveryId = resultJson.TryGetProperty("recovery_id", out var idProp) ?
            idProp.GetString() ?? "" : "";
        string? errorMessage = resultJson.TryGetProperty("error", out var errorProp) ?
            errorProp.GetString() : null;

        return new RecoveryResult
        {
            RecoveryId = recoveryId,
            Success = success,
            ErrorMessage = errorMessage,
            Status = success ? RecoveryStatus.Initiated : RecoveryStatus.Failed,
            Timestamp = DateTime.UtcNow,
            Metadata = request.Metadata
        };
    }

    /// <summary>
    /// Completes account recovery in the enclave.
    /// </summary>
    /// <param name="request">The complete recovery request.</param>
    /// <returns>The recovery result.</returns>
    private async Task<RecoveryResult> CompleteRecoveryInEnclaveAsync(CompleteRecoveryRequest request)
    {
        var recoveryData = new
        {
            recoveryId = request.RecoveryId,
            guardianSignatures = request.GuardianSignatures.Select(gs => new
            {
                guardianAddress = gs.GuardianAddress,
                signature = gs.Signature,
                signedAt = gs.SignedAt
            }).ToArray(),
            metadata = request.Metadata
        };

        string recoveryDataJson = JsonSerializer.Serialize(recoveryData);
#pragma warning disable CS8602 // Dereference of a possibly null reference
        string result = await _enclaveManager.CompleteAccountRecoveryAsync(request.RecoveryId, recoveryDataJson, CancellationToken.None);
#pragma warning restore CS8602

        if (string.IsNullOrEmpty(result))
            throw new InvalidOperationException("Enclave returned null or empty result");

        var resultJson = JsonSerializer.Deserialize<JsonElement>(result);

        bool success = resultJson.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
        string transactionHash = resultJson.TryGetProperty("transaction_hash", out var hashProp) ?
            hashProp.GetString() ?? "" : "";
        string? errorMessage = resultJson.TryGetProperty("error", out var errorProp) ?
            errorProp.GetString() : null;

        return new RecoveryResult
        {
            RecoveryId = request.RecoveryId,
            Success = success,
            ErrorMessage = errorMessage,
            TransactionHash = transactionHash,
            Status = success ? RecoveryStatus.Completed : RecoveryStatus.Failed,
            Timestamp = DateTime.UtcNow,
            Metadata = request.Metadata
        };
    }

    /// <summary>
    /// Creates a session key in the enclave.
    /// </summary>
    /// <param name="request">The session key creation request.</param>
    /// <returns>The session key result.</returns>
    private async Task<SessionKeyResult> CreateSessionKeyInEnclaveAsync(CreateSessionKeyRequest request)
    {
        var sessionKeyData = new
        {
            accountId = request.AccountId,
            permissions = new
            {
                maxTransactionValue = request.Permissions.MaxTransactionValue,
                allowedContracts = request.Permissions.AllowedContracts,
                allowedFunctions = request.Permissions.AllowedFunctions,
                maxTransactionsPerDay = request.Permissions.MaxTransactionsPerDay,
                allowGaslessTransactions = request.Permissions.AllowGaslessTransactions
            },
            expiresAt = request.ExpiresAt,
            name = request.Name,
            metadata = request.Metadata
        };

        string sessionKeyDataJson = JsonSerializer.Serialize(sessionKeyData);
#pragma warning disable CS8602 // Dereference of a possibly null reference
        string result = await _enclaveManager.CreateSessionKeyAsync(request.AccountId, sessionKeyDataJson, CancellationToken.None);
#pragma warning restore CS8602

        if (string.IsNullOrEmpty(result))
            throw new InvalidOperationException("Enclave returned null or empty result");

        var resultJson = JsonSerializer.Deserialize<JsonElement>(result);

        bool success = resultJson.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
        string sessionKeyId = resultJson.TryGetProperty("session_key_id", out var idProp) ?
            idProp.GetString() ?? "" : "";
        string publicKey = resultJson.TryGetProperty("public_key", out var keyProp) ?
            keyProp.GetString() ?? "" : "";
        string? errorMessage = resultJson.TryGetProperty("error", out var errorProp) ?
            errorProp.GetString() : null;

        return new SessionKeyResult
        {
            SessionKeyId = sessionKeyId,
            PublicKey = publicKey,
            Success = success,
            ErrorMessage = errorMessage,
            Status = success ? SessionKeyStatus.Active : SessionKeyStatus.Suspended,
            ExpiresAt = request.ExpiresAt,
            Timestamp = DateTime.UtcNow,
            Metadata = request.Metadata
        };
    }
}
