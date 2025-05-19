using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Data.Repositories;
using NeoServiceLayer.Infrastructure.Services;
using NeoServiceLayer.Shared.Models;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;

namespace NeoServiceLayer.Infrastructure.Tests.Services
{
    public class KeyManagementServiceTests
    {
        private readonly Mock<NeoServiceLayer.Tee.Host.Services.ITeeHostService> _mockTeeHostService;
        private readonly Mock<ITeeAccountRepository> _mockTeeAccountRepository;
        private readonly Mock<ILogger<KeyManagementService>> _mockLogger;
        private readonly KeyManagementService _keyManagementService;

        public KeyManagementServiceTests()
        {
            _mockTeeHostService = new Mock<NeoServiceLayer.Tee.Host.Services.ITeeHostService>();
            _mockTeeAccountRepository = new Mock<ITeeAccountRepository>();
            _mockLogger = new Mock<ILogger<KeyManagementService>>();

            // Create an adapter that implements Core.Interfaces.ITeeHostService
            var teeHostServiceAdapter = new Mocks.TeeHostServiceAdapter(_mockTeeHostService.Object);

            _keyManagementService = new KeyManagementService(
                teeHostServiceAdapter,
                _mockTeeAccountRepository.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAccountAsync_ValidAccount_ReturnsCreatedAccount()
        {
            // Arrange
            var account = new TeeAccount
            {
                Name = "Test Account",
                Type = AccountType.ECDSA,
                UserId = "user123",
                IsExportable = false
            };

            var responseData = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "public_key", "public_key_data" },
                { "address", "address_data" },
                { "attestation_proof", "attestation_proof_data" }
            });

            var response = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.KeyManagement,
                Data = responseData,
                CreatedAt = DateTime.UtcNow
            };

            _mockTeeHostService.Setup(x => x.SendMessageAsync(It.IsAny<TeeMessage>()))
                .ReturnsAsync(response);

            _mockTeeAccountRepository.Setup(x => x.AddAccountAsync(It.IsAny<TeeAccount>()))
                .ReturnsAsync(account);

            // Act
            var result = await _keyManagementService.CreateAccountAsync(account);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(account.Id, result.Id);
            Assert.Equal(account.Name, result.Name);
            Assert.Equal(account.Type, result.Type);
            Assert.Equal(account.UserId, result.UserId);
            Assert.Equal(account.IsExportable, result.IsExportable);
            Assert.Equal("public_key_data", result.PublicKey);
            Assert.Equal("address_data", result.Address);
            Assert.Equal("attestation_proof_data", result.AttestationProof);

            // Verify repository was called
            _mockTeeAccountRepository.Verify(x => x.AddAccountAsync(It.Is<TeeAccount>(a =>
                a.Id == account.Id &&
                a.PublicKey == "public_key_data" &&
                a.Address == "address_data" &&
                a.AttestationProof == "attestation_proof_data")),
                Times.Once);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAccountAsync_ExistingAccountId_ReturnsAccount()
        {
            // Arrange
            var accountId = Guid.NewGuid().ToString();
            var account = new TeeAccount
            {
                Id = accountId,
                Name = "Test Account",
                Type = AccountType.ECDSA,
                UserId = "user123",
                PublicKey = "public_key_data",
                Address = "address_data",
                AttestationProof = "attestation_proof_data",
                IsExportable = false
            };

            _mockTeeAccountRepository.Setup(x => x.GetAccountByIdAsync(accountId))
                .ReturnsAsync(account);

            // Act
            var result = await _keyManagementService.GetAccountAsync(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(account.Id, result.Id);
            Assert.Equal(account.Name, result.Name);
            Assert.Equal(account.Type, result.Type);
            Assert.Equal(account.UserId, result.UserId);
            Assert.Equal(account.IsExportable, result.IsExportable);
            Assert.Equal(account.PublicKey, result.PublicKey);
            Assert.Equal(account.Address, result.Address);
            Assert.Equal(account.AttestationProof, result.AttestationProof);

            // Verify repository was called
            _mockTeeAccountRepository.Verify(x => x.GetAccountByIdAsync(accountId), Times.Once);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAccountAsync_NonExistingAccountId_ReturnsNull()
        {
            // Arrange
            var accountId = "non-existing-id";

            _mockTeeAccountRepository.Setup(x => x.GetAccountByIdAsync(accountId))
                .ReturnsAsync((TeeAccount)null);

            // Act
            var result = await _keyManagementService.GetAccountAsync(accountId);

            // Assert
            Assert.Null(result);

            // Verify repository was called
            _mockTeeAccountRepository.Verify(x => x.GetAccountByIdAsync(accountId), Times.Once);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAccountsAsync_ValidUserId_ReturnsUserAccounts()
        {
            // Arrange
            var userId = "user123";
            var accounts = new List<TeeAccount>
            {
                new TeeAccount
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Test Account 1",
                    Type = AccountType.ECDSA,
                    UserId = userId,
                    PublicKey = "public_key_data_1",
                    Address = "address_data_1",
                    IsExportable = false
                },
                new TeeAccount
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Test Account 2",
                    Type = AccountType.ED25519,
                    UserId = userId,
                    PublicKey = "public_key_data_2",
                    Address = "address_data_2",
                    IsExportable = true
                }
            };

            _mockTeeAccountRepository.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(accounts);

            // Act
            var result = await _keyManagementService.GetAccountsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, a => a.Name == "Test Account 1" && a.Type == AccountType.ECDSA);
            Assert.Contains(result, a => a.Name == "Test Account 2" && a.Type == AccountType.ED25519);

            // Verify repository was called
            _mockTeeAccountRepository.Verify(x => x.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAccountsAsync_ValidUserIdWithTypeFilter_ReturnsFilteredUserAccounts()
        {
            // Arrange
            var userId = "user123";
            var accounts = new List<TeeAccount>
            {
                new TeeAccount
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Test Account 1",
                    Type = AccountType.ECDSA,
                    UserId = userId,
                    PublicKey = "public_key_data_1",
                    Address = "address_data_1",
                    IsExportable = false
                },
                new TeeAccount
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Test Account 2",
                    Type = AccountType.ED25519,
                    UserId = userId,
                    PublicKey = "public_key_data_2",
                    Address = "address_data_2",
                    IsExportable = true
                }
            };

            _mockTeeAccountRepository.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(accounts);

            // Act
            var result = await _keyManagementService.GetAccountsAsync(userId, AccountType.ECDSA);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains(result, a => a.Name == "Test Account 1" && a.Type == AccountType.ECDSA);
            Assert.DoesNotContain(result, a => a.Name == "Test Account 2" && a.Type == AccountType.ED25519);

            // Verify repository was called
            _mockTeeAccountRepository.Verify(x => x.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async System.Threading.Tasks.Task SignAsync_ValidAccountIdAndData_ReturnsSignature()
        {
            // Arrange
            var accountId = Guid.NewGuid().ToString();
            var account = new TeeAccount
            {
                Id = accountId,
                Name = "Test Account",
                Type = AccountType.ECDSA,
                UserId = "user123",
                PublicKey = "public_key_data",
                Address = "address_data",
                IsExportable = false
            };

            var signResponseData = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "signature", "signature_data" }
            });

            var signResponse = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.KeyManagement,
                Data = signResponseData,
                CreatedAt = DateTime.UtcNow
            };

            _mockTeeAccountRepository.Setup(x => x.GetAccountByIdAsync(accountId))
                .ReturnsAsync(account);

            _mockTeeHostService.Setup(x => x.SendMessageAsync(It.IsAny<TeeMessage>()))
                .ReturnsAsync(signResponse);

            var data = Encoding.UTF8.GetBytes("test data");

            // Act
            var result = await _keyManagementService.SignAsync(accountId, data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("signature_data", result);

            // Verify repository was called
            _mockTeeAccountRepository.Verify(x => x.GetAccountByIdAsync(accountId), Times.Once);

            // Verify TEE host service was called with correct parameters
            _mockTeeHostService.Verify(x => x.SendMessageAsync(It.Is<TeeMessage>(m =>
                m.Type == TeeMessageType.KeyManagement &&
                m.Data.Contains("sign") &&
                m.Data.Contains(accountId))),
                Times.Once);
        }

        [Fact]
        public async System.Threading.Tasks.Task VerifyAsync_ValidSignature_ReturnsTrue()
        {
            // Arrange
            var accountId = Guid.NewGuid().ToString();
            var account = new TeeAccount
            {
                Id = accountId,
                Name = "Test Account",
                Type = AccountType.ECDSA,
                UserId = "user123",
                PublicKey = "public_key_data",
                Address = "address_data",
                IsExportable = false
            };

            var verifyResponse = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.KeyManagement,
                Data = "true",
                CreatedAt = DateTime.UtcNow
            };

            _mockTeeAccountRepository.Setup(x => x.GetAccountByIdAsync(accountId))
                .ReturnsAsync(account);

            _mockTeeHostService.Setup(x => x.SendMessageAsync(It.IsAny<TeeMessage>()))
                .ReturnsAsync(verifyResponse);

            var data = Encoding.UTF8.GetBytes("test data");
            var signature = "valid_signature";

            // Act
            var result = await _keyManagementService.VerifyAsync(accountId, data, signature);

            // Assert
            Assert.True(result);

            // Verify repository was called
            _mockTeeAccountRepository.Verify(x => x.GetAccountByIdAsync(accountId), Times.Once);

            // Verify TEE host service was called with correct parameters
            _mockTeeHostService.Verify(x => x.SendMessageAsync(It.Is<TeeMessage>(m =>
                m.Type == TeeMessageType.KeyManagement &&
                m.Data.Contains("verify") &&
                m.Data.Contains(accountId) &&
                m.Data.Contains(signature))),
                Times.Once);
        }
    }
}
