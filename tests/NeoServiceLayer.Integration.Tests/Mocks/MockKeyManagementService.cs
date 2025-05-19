using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Integration.Tests.Mocks
{
    public class MockKeyManagementService : IKeyManagementServiceExtended
    {
        private readonly ILogger<MockKeyManagementService> _logger;
        private readonly List<TeeAccount> _accounts = new List<TeeAccount>();

        public MockKeyManagementService(ILogger<MockKeyManagementService> logger)
        {
            _logger = logger;

            // Add some sample accounts
            _accounts.Add(new TeeAccount
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "user123",
                Type = AccountType.Wallet,
                Name = "Neo Account 1",
                PublicKey = "03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c",
                Address = "NZHf1NJvz1tvELGLWZjhpb3NqZJFFUYpxT",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            });

            _accounts.Add(new TeeAccount
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "user123",
                Type = AccountType.ECDSA,
                Name = "Ethereum Account 1",
                PublicKey = "0x0472ec0185ebb8202f3d4ddb0226998889663cf2709afc16d0c5c0c6cd9ef5add7a4346b69963da75b4a3c4ec9e97575f4c77d5695859a7c5f3fba7db77cf46c1c",
                Address = "0x742d35Cc6634C0532925a3b844Bc454e4438f44e",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            });
        }

        public async Task<TeeAccount> CreateAccountAsync(TeeAccount account)
        {
            _logger.LogInformation("Creating account of type {AccountType} for user {UserId}", account.Type, account.UserId);

            if (string.IsNullOrEmpty(account.Id))
            {
                account.Id = Guid.NewGuid().ToString();
            }

            if (account.CreatedAt == default)
            {
                account.CreatedAt = DateTime.UtcNow;
            }

            // Generate mock keys based on account type if not provided
            if (string.IsNullOrEmpty(account.PublicKey) || string.IsNullOrEmpty(account.Address))
            {
                switch (account.Type)
                {
                    case AccountType.Wallet:
                        account.PublicKey = "03" + Guid.NewGuid().ToString("N").Substring(0, 64);
                        account.Address = "N" + Guid.NewGuid().ToString("N").Substring(0, 33);
                        break;
                    case AccountType.ECDSA:
                        account.PublicKey = "0x04" + Guid.NewGuid().ToString("N").Substring(0, 128);
                        account.Address = "0x" + Guid.NewGuid().ToString("N").Substring(0, 40);
                        break;
                    default:
                        account.PublicKey = Guid.NewGuid().ToString("N");
                        account.Address = Guid.NewGuid().ToString("N").Substring(0, 40);
                        break;
                }
            }

            _accounts.Add(account);

            return account;
        }

        public async Task<TeeAccount> GetAccountAsync(string accountId)
        {
            _logger.LogInformation("Getting account {AccountId}", accountId);

            var account = _accounts.FirstOrDefault(a => a.Id == accountId);

            if (account == null)
            {
                // Create a mock account if not found
                account = new TeeAccount
                {
                    Id = accountId,
                    UserId = "user123", // Default user ID
                    Type = AccountType.Wallet,
                    Name = "Auto-generated Account",
                    PublicKey = "03" + Guid.NewGuid().ToString("N").Substring(0, 64),
                    Address = "N" + Guid.NewGuid().ToString("N").Substring(0, 33),
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    IsExportable = true
                };

                // Add to the accounts list for future reference
                _accounts.Add(account);
            }

            return account;
        }

        public async Task<IEnumerable<TeeAccount>> GetAccountsAsync(string userId, AccountType? type = null)
        {
            _logger.LogInformation("Getting accounts for user {UserId} with type {AccountType}", userId, type);

            var query = _accounts.Where(a => a.UserId == userId);

            if (type.HasValue)
            {
                query = query.Where(a => a.Type == type.Value);
            }

            var result = query.ToList();

            // If no accounts found for this user, create some default ones
            if (result.Count == 0)
            {
                // Create a Neo wallet account
                var neoAccount = new TeeAccount
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Type = AccountType.Wallet,
                    Name = "Neo Account",
                    PublicKey = "03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c",
                    Address = "NZHf1NJvz1tvELGLWZjhpb3NqZJFFUYpxT",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    IsExportable = true
                };

                // Create an ECDSA account
                var ecdsaAccount = new TeeAccount
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Type = AccountType.ECDSA,
                    Name = "Ethereum Account",
                    PublicKey = "0x0472ec0185ebb8202f3d4ddb0226998889663cf2709afc16d0c5c0c6cd9ef5add7a4346b69963da75b4a3c4ec9e97575f4c77d5695859a7c5f3fba7db77cf46c1c",
                    Address = "0x742d35Cc6634C0532925a3b844Bc454e4438f44e",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    IsExportable = true
                };

                // Add to the accounts list for future reference
                _accounts.Add(neoAccount);
                _accounts.Add(ecdsaAccount);

                // Filter by type if specified
                if (type.HasValue)
                {
                    result = _accounts.Where(a => a.UserId == userId && a.Type == type.Value).ToList();
                }
                else
                {
                    result = _accounts.Where(a => a.UserId == userId).ToList();
                }
            }

            return result;
        }

        public async Task<string> SignAsync(string accountId, byte[] data, string hashAlgorithm = "SHA256")
        {
            _logger.LogInformation("Signing data with account {AccountId} using {HashAlgorithm}", accountId, hashAlgorithm);

            // Return a mock signature as a base64 string
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        public async Task<bool> VerifyAsync(string accountId, byte[] data, string signature, string hashAlgorithm = "SHA256")
        {
            _logger.LogInformation("Verifying signature with account {AccountId} using {HashAlgorithm}", accountId, hashAlgorithm);

            // Always return true for mock implementation
            return true;
        }

        public async Task<byte[]> EncryptAsync(string accountId, byte[] data)
        {
            _logger.LogInformation("Encrypting data with account {AccountId}", accountId);

            // Return mock encrypted data (original data with a prefix)
            var result = new byte[data.Length + 8];
            Array.Copy(Encoding.UTF8.GetBytes("ENCRYPTED"), 0, result, 0, 8);
            Array.Copy(data, 0, result, 8, data.Length);
            return result;
        }

        public async Task<byte[]> DecryptAsync(string accountId, byte[] encryptedData)
        {
            _logger.LogInformation("Decrypting data with account {AccountId}", accountId);

            // Return mock decrypted data (remove the prefix if it exists)
            if (encryptedData.Length > 8 &&
                Encoding.UTF8.GetString(encryptedData, 0, 8) == "ENCRYPTED")
            {
                var result = new byte[encryptedData.Length - 8];
                Array.Copy(encryptedData, 8, result, 0, result.Length);
                return result;
            }

            // If no prefix, just return the original data
            return encryptedData;
        }

        public async Task<byte[]> ExportAccountAsync(string accountId, string password)
        {
            _logger.LogInformation("Exporting account {AccountId}", accountId);

            var account = _accounts.FirstOrDefault(a => a.Id == accountId);
            if (account == null)
            {
                throw new ArgumentException($"Account with ID {accountId} not found", nameof(accountId));
            }

            // Return mock exported account data
            var accountJson = $"{{\"id\":\"{account.Id}\",\"type\":\"{account.Type}\",\"name\":\"{account.Name}\",\"publicKey\":\"{account.PublicKey}\",\"address\":\"{account.Address}\"}}";
            return Encoding.UTF8.GetBytes(accountJson);
        }

        public async Task<TeeAccount> ImportAccountAsync(byte[] accountData, string password, string userId)
        {
            _logger.LogInformation("Importing account for user {UserId}", userId);

            // Create a mock account from the imported data
            var account = new TeeAccount
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Type = AccountType.Wallet, // Default to Wallet
                Name = "Imported Account",
                PublicKey = "03" + Guid.NewGuid().ToString("N").Substring(0, 64),
                Address = "N" + Guid.NewGuid().ToString("N").Substring(0, 33),
                CreatedAt = DateTime.UtcNow,
                IsExportable = true
            };

            _accounts.Add(account);

            return account;
        }

        // The following methods are not part of the IKeyManagementService interface
        // but are kept for backward compatibility with existing tests

        public async Task<bool> DeleteAccountAsync(string accountId)
        {
            _logger.LogInformation("Deleting account {AccountId}", accountId);

            var account = _accounts.FirstOrDefault(a => a.Id == accountId);
            if (account != null)
            {
                _accounts.Remove(account);
                return true;
            }

            return false;
        }

        public async Task<TeeAccount> CreateAccountAsync(string userId, AccountType type, string name)
        {
            _logger.LogInformation("Creating account of type {AccountType} for user {UserId}", type, userId);

            var account = new TeeAccount
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Type = type,
                Name = name,
                CreatedAt = DateTime.UtcNow
            };

            return await CreateAccountAsync(account);
        }
    }
}
