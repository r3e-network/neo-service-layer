using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host.Services;

/// <summary>
/// Abstract Account operations for the Enclave Manager.
/// </summary>
public partial class EnclaveManager
{
    /// <inheritdoc/>
    public Task<string> CreateAbstractAccountAsync(string accountId, string accountDataJson, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating abstract account {AccountId} in enclave", accountId);

            // Use the real enclave Abstract Account function
            string result = _enclaveWrapper.CreateAbstractAccount(accountId, accountDataJson);

            _logger.LogDebug("Abstract account {AccountId} created successfully", accountId);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating abstract account {AccountId}", accountId);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> SignAndExecuteTransactionAsync(string accountId, string transactionDataJson, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Signing and executing transaction for account {AccountId}", accountId);

            // Use the real enclave transaction signing and execution function
            string result = _enclaveWrapper.SignAbstractAccountTransaction(accountId, transactionDataJson);

            _logger.LogDebug("Transaction signed and executed for account {AccountId}", accountId);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing and executing transaction for account {AccountId}", accountId);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> AddAccountGuardianAsync(string accountId, string guardianDataJson, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Adding guardian to account {AccountId}", accountId);

            // Use the real enclave guardian addition function
            string result = _enclaveWrapper.AddAbstractAccountGuardian(accountId, guardianDataJson);

            _logger.LogDebug("Guardian added to account {AccountId}", accountId);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding guardian to account {AccountId}", accountId);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> InitiateAccountRecoveryAsync(string accountId, string recoveryDataJson, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Initiating account recovery for {AccountId}", accountId);

            // Use the real enclave account recovery initiation function
            string result = _enclaveWrapper.ExecuteJavaScript($"initiateAccountRecovery('{accountId}', {recoveryDataJson})", "");

            _logger.LogDebug("Account recovery initiated for {AccountId}", accountId);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating account recovery for {AccountId}", accountId);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> CompleteAccountRecoveryAsync(string recoveryId, string recoveryDataJson, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Completing account recovery {RecoveryId}", recoveryId);

            // Use the real enclave account recovery completion function
            string result = _enclaveWrapper.ExecuteJavaScript($"completeAccountRecovery('{recoveryId}', {recoveryDataJson})", "");

            _logger.LogDebug("Account recovery {RecoveryId} completed", recoveryId);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing account recovery {RecoveryId}", recoveryId);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> CreateSessionKeyAsync(string accountId, string sessionKeyDataJson, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating session key for account {AccountId}", accountId);

            // Use the real enclave session key creation function
            string result = _enclaveWrapper.ExecuteJavaScript($"createSessionKey('{accountId}', {sessionKeyDataJson})", "");

            _logger.LogDebug("Session key created for account {AccountId}", accountId);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session key for account {AccountId}", accountId);
            throw;
        }
    }
}
