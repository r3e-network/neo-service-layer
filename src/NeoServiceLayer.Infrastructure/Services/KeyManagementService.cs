using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Data.Repositories;
using NeoServiceLayer.Shared.Models;
using ITeeHostService = NeoServiceLayer.Core.Interfaces.ITeeHostService;

namespace NeoServiceLayer.Infrastructure.Services
{
    /// <summary>
    /// Implementation of the key management service.
    /// </summary>
    public class KeyManagementService : IKeyManagementService
    {
        private readonly ITeeHostService _teeHostService;
        private readonly ITeeAccountRepository _teeAccountRepository;
        private readonly ILogger<KeyManagementService> _logger;

        /// <summary>
        /// Initializes a new instance of the KeyManagementService class.
        /// </summary>
        /// <param name="teeHostService">The TEE host service.</param>
        /// <param name="teeAccountRepository">The TEE account repository.</param>
        /// <param name="logger">The logger.</param>
        public KeyManagementService(
            ITeeHostService teeHostService,
            ITeeAccountRepository teeAccountRepository,
            ILogger<KeyManagementService> logger)
        {
            _teeHostService = teeHostService ?? throw new ArgumentNullException(nameof(teeHostService));
            _teeAccountRepository = teeAccountRepository ?? throw new ArgumentNullException(nameof(teeAccountRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<TeeAccount> CreateAccountAsync(TeeAccount account)
        {
            _logger.LogInformation("Creating account of type {AccountType}", account.Type);

            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            if (string.IsNullOrEmpty(account.UserId))
            {
                throw new ArgumentException("User ID is required", nameof(account));
            }

            try
            {
                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.KeyManagement, JsonSerializer.Serialize(new
                {
                    Action = "create_account",
                    Account = account
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(response.Data);

                // Update account with response data
                account.PublicKey = result["public_key"];
                account.Address = result["address"];
                account.AttestationProof = result["attestation_proof"];

                // Store account in the database
                await _teeAccountRepository.AddAccountAsync(account);

                _logger.LogInformation("Account {AccountId} created successfully", account.Id);

                return account;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating account");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TeeAccount> GetAccountAsync(string accountId)
        {
            _logger.LogInformation("Getting account {AccountId}", accountId);

            if (string.IsNullOrEmpty(accountId))
            {
                throw new ArgumentException("Account ID is required", nameof(accountId));
            }

            try
            {
                var account = await _teeAccountRepository.GetAccountByIdAsync(accountId);

                if (account != null)
                {
                    _logger.LogInformation("Account {AccountId} retrieved successfully", accountId);
                    return account;
                }

                _logger.LogWarning("Account {AccountId} not found", accountId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account {AccountId}", accountId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TeeAccount>> GetAccountsAsync(string userId, AccountType? type = null)
        {
            _logger.LogInformation("Getting accounts for user {UserId} with type {Type}", userId, type);

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID is required", nameof(userId));
            }

            try
            {
                // Get accounts from the database
                var accounts = await _teeAccountRepository.GetByUserIdAsync(userId);

                // Filter by type if specified
                if (type.HasValue)
                {
                    accounts = accounts.Where(a => a.Type == type.Value).ToList();
                }

                _logger.LogInformation("Retrieved {Count} accounts for user {UserId}", accounts.Count(), userId);

                return accounts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accounts for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> SignAsync(string accountId, byte[] data, string hashAlgorithm = "SHA256")
        {
            _logger.LogInformation("Signing data using account {AccountId}", accountId);

            if (string.IsNullOrEmpty(accountId))
            {
                throw new ArgumentException("Account ID is required", nameof(accountId));
            }

            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data is required", nameof(data));
            }

            try
            {
                // Check if account exists
                var account = await _teeAccountRepository.GetAccountByIdAsync(accountId);
                if (account == null)
                {
                    _logger.LogWarning("Account {AccountId} not found", accountId);
                    throw new ArgumentException($"Account with ID {accountId} not found", nameof(accountId));
                }

                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.KeyManagement, JsonSerializer.Serialize(new
                {
                    Action = "sign",
                    AccountId = accountId,
                    Data = Convert.ToBase64String(data),
                    HashAlgorithm = hashAlgorithm
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(response.Data);

                _logger.LogInformation("Data signed successfully using account {AccountId}", accountId);

                return result["signature"];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing data using account {AccountId}", accountId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> VerifyAsync(string accountId, byte[] data, string signature, string hashAlgorithm = "SHA256")
        {
            _logger.LogInformation("Verifying signature using account {AccountId}", accountId);

            if (string.IsNullOrEmpty(accountId))
            {
                throw new ArgumentException("Account ID is required", nameof(accountId));
            }

            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data is required", nameof(data));
            }

            if (string.IsNullOrEmpty(signature))
            {
                throw new ArgumentException("Signature is required", nameof(signature));
            }

            try
            {
                // Check if account exists
                var account = await _teeAccountRepository.GetAccountByIdAsync(accountId);
                if (account == null)
                {
                    _logger.LogWarning("Account {AccountId} not found", accountId);
                    throw new ArgumentException($"Account with ID {accountId} not found", nameof(accountId));
                }

                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.KeyManagement, JsonSerializer.Serialize(new
                {
                    Action = "verify",
                    AccountId = accountId,
                    Data = Convert.ToBase64String(data),
                    Signature = signature,
                    HashAlgorithm = hashAlgorithm
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var result = JsonSerializer.Deserialize<bool>(response.Data);

                _logger.LogInformation("Signature verification completed with result: {IsValid}", result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying signature using account {AccountId}", accountId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> EncryptAsync(string accountId, byte[] data)
        {
            _logger.LogInformation("Encrypting data using account {AccountId}", accountId);

            if (string.IsNullOrEmpty(accountId))
            {
                throw new ArgumentException("Account ID is required", nameof(accountId));
            }

            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data is required", nameof(data));
            }

            try
            {
                // Check if account exists
                var account = await _teeAccountRepository.GetAccountByIdAsync(accountId);
                if (account == null)
                {
                    _logger.LogWarning("Account {AccountId} not found", accountId);
                    throw new ArgumentException($"Account with ID {accountId} not found", nameof(accountId));
                }

                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.KeyManagement, JsonSerializer.Serialize(new
                {
                    Action = "encrypt",
                    AccountId = accountId,
                    Data = Convert.ToBase64String(data)
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(response.Data);

                _logger.LogInformation("Data encrypted successfully using account {AccountId}", accountId);

                return Convert.FromBase64String(result["encrypted_data"]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting data using account {AccountId}", accountId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> DecryptAsync(string accountId, byte[] encryptedData)
        {
            _logger.LogInformation("Decrypting data using account {AccountId}", accountId);

            if (string.IsNullOrEmpty(accountId))
            {
                throw new ArgumentException("Account ID is required", nameof(accountId));
            }

            if (encryptedData == null || encryptedData.Length == 0)
            {
                throw new ArgumentException("Encrypted data is required", nameof(encryptedData));
            }

            try
            {
                // Check if account exists
                var account = await _teeAccountRepository.GetAccountByIdAsync(accountId);
                if (account == null)
                {
                    _logger.LogWarning("Account {AccountId} not found", accountId);
                    throw new ArgumentException($"Account with ID {accountId} not found", nameof(accountId));
                }

                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.KeyManagement, JsonSerializer.Serialize(new
                {
                    Action = "decrypt",
                    AccountId = accountId,
                    EncryptedData = Convert.ToBase64String(encryptedData)
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(response.Data);

                _logger.LogInformation("Data decrypted successfully using account {AccountId}", accountId);

                return Convert.FromBase64String(result["decrypted_data"]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting data using account {AccountId}", accountId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> ExportAccountAsync(string accountId, string password)
        {
            _logger.LogInformation("Exporting account {AccountId}", accountId);

            if (string.IsNullOrEmpty(accountId))
            {
                throw new ArgumentException("Account ID is required", nameof(accountId));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password is required", nameof(password));
            }

            try
            {
                // Check if account exists
                var account = await _teeAccountRepository.GetAccountByIdAsync(accountId);
                if (account == null)
                {
                    _logger.LogWarning("Account {AccountId} not found", accountId);
                    throw new ArgumentException($"Account with ID {accountId} not found", nameof(accountId));
                }

                // Check if account is exportable
                if (!account.IsExportable)
                {
                    _logger.LogWarning("Account {AccountId} is not exportable", accountId);
                    throw new InvalidOperationException($"Account with ID {accountId} is not exportable");
                }

                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.KeyManagement, JsonSerializer.Serialize(new
                {
                    Action = "export_account",
                    AccountId = accountId,
                    Password = password
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(response.Data);

                _logger.LogInformation("Account {AccountId} exported successfully", accountId);

                return Convert.FromBase64String(result["account_data"]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting account {AccountId}", accountId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TeeAccount> ImportAccountAsync(byte[] accountData, string password, string userId)
        {
            _logger.LogInformation("Importing account for user {UserId}", userId);

            if (accountData == null || accountData.Length == 0)
            {
                throw new ArgumentException("Account data is required", nameof(accountData));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password is required", nameof(password));
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID is required", nameof(userId));
            }

            try
            {
                // Create a message to send to the TEE
                var message = TeeMessage.Create(TeeMessageType.KeyManagement, JsonSerializer.Serialize(new
                {
                    Action = "import_account",
                    AccountData = Convert.ToBase64String(accountData),
                    Password = password,
                    UserId = userId
                }));

                // Send the message to the TEE
                var response = await _teeHostService.SendMessageAsync(message);

                // Parse the response
                var account = JsonSerializer.Deserialize<TeeAccount>(response.Data);

                // Store account in the database
                await _teeAccountRepository.AddAccountAsync(account);

                _logger.LogInformation("Account {AccountId} imported successfully for user {UserId}", account.Id, userId);

                return account;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing account for user {UserId}", userId);
                throw;
            }
        }
    }
}
