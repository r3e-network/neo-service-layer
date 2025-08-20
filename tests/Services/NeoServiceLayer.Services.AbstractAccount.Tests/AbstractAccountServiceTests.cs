extern alias EnclaveStorageAlias;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.AbstractAccount;
using NeoServiceLayer.Services.AbstractAccount.Models;
using NeoServiceLayer.TestInfrastructure;
using NeoServiceLayer.Tee.Host.Services;
using EnclaveStorageAlias::NeoServiceLayer.Services.EnclaveStorage;
using EnclaveModels = EnclaveStorageAlias::NeoServiceLayer.Services.EnclaveStorage.Models;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using FluentAssertions;


namespace NeoServiceLayer.Services.AbstractAccount.Tests;

public class AbstractAccountServiceTests : TestBase
{
    private readonly Mock<ILogger<AbstractAccountService>> _loggerMock;
    private readonly Mock<IEnclaveManager> _enclaveManagerMock;
    private readonly Mock<IEnclaveStorageService> _enclaveStorageMock;
    private readonly AbstractAccountService _service;

    public AbstractAccountServiceTests()
    {
        _loggerMock = new Mock<ILogger<AbstractAccountService>>();
        _enclaveManagerMock = new Mock<IEnclaveManager>();
        _enclaveStorageMock = new Mock<IEnclaveStorageService>();
        
        // Setup enclave manager
        SetupEnclaveManager();
        
        // Setup enclave storage
        SetupEnclaveStorage();
        
        _service = new AbstractAccountService(_loggerMock.Object, _enclaveManagerMock.Object, _enclaveStorageMock.Object);

        // Initialize the service to ensure enclave is ready
        InitializeServiceAsync().GetAwaiter().GetResult();
    }
    
    private void SetupEnclaveManager()
    {
        // Setup enclave initialization
        _enclaveManagerMock
            .Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
            
        _enclaveManagerMock
            .Setup(x => x.InitializeAsync(null, default))
            .Returns(Task.CompletedTask);
        
        _enclaveManagerMock
            .Setup(x => x.InitializeEnclaveAsync())
            .ReturnsAsync(true);
            
        _enclaveManagerMock
            .Setup(x => x.IsInitialized)
            .Returns(true);
        
        // Setup ExecuteJavaScriptAsync with two string parameters (template and paramsJson)
        _enclaveManagerMock
            .Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string template, string paramsJson) => 
            {
                // Return success for abstract account operations
                return "{\"success\": true, \"accountId\": \"test-account-id\", \"address\": \"0xtest123\"}";
            });
            
        // Setup CreateAbstractAccountAsync method
        _enclaveManagerMock
            .Setup(x => x.CreateAbstractAccountAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string accountId, string accountData, CancellationToken ct) =>
            {
                // Return a valid account creation response
                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    success = true,
                    accountAddress = "0xtest123456789",
                    masterPublicKey = "03c663ba46afa8349f020eb9e8f9e1dc1c8e877b9d239e9110d1fdd7152e7c59dd",
                    transactionHash = "0xtxhash123456"
                });
            });
            
        // Setup other abstract account operations
        _enclaveManagerMock
            .Setup(x => x.AddGuardianAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"success\": true, \"transactionHash\": \"0xguardian123\"}");
            
        _enclaveManagerMock
            .Setup(x => x.ExecuteTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"success\": true, \"transactionHash\": \"0xtransaction123\"}");
    }

    private void SetupEnclaveStorage()
    {
        // Setup enclave storage mock
        _enclaveStorageMock
            .Setup(x => x.InitializeAsync())
            .ReturnsAsync(true);
            
        _enclaveStorageMock
            .Setup(x => x.IsInitialized)
            .Returns(true);
            
        // Setup SealDataAsync
        _enclaveStorageMock
            .Setup(x => x.SealDataAsync(It.IsAny<EnclaveModels.SealDataRequest>(), It.IsAny<BlockchainType>()))
            .ReturnsAsync((EnclaveModels.SealDataRequest request, BlockchainType blockchain) =>
            {
                return new EnclaveModels.SealDataResult
                {
                    Success = true,
                    SealedData = new byte[] { 1, 2, 3, 4, 5 },
                    Metadata = new Dictionary<string, string> { ["sealed"] = "true" }
                };
            });
            
        // Setup UnsealDataAsync
        _enclaveStorageMock
            .Setup(x => x.UnsealDataAsync(It.IsAny<string>(), It.IsAny<BlockchainType>()))
            .ReturnsAsync((string key, BlockchainType blockchain) =>
            {
                return new EnclaveModels.UnsealDataResult
                {
                    Success = true,
                    Data = System.Text.Encoding.UTF8.GetBytes("{}"),
                    Metadata = new Dictionary<string, string> { ["unsealed"] = "true" }
                };
            });
    }
    
    private async Task InitializeServiceAsync()
    {
        await _service.InitializeAsync();
    }

    private async Task<string> CreateTestAccountAsync(BlockchainType blockchainType)
    {
        var request = new CreateAccountRequest
        {
            OwnerPublicKey = "03c663ba46afa8349f020eb9e8f9e1dc1c8e877b9d239e9110d1fdd7152e7c59dd",
            InitialGuardians = new[] { GenerateTestAddress(blockchainType) },
            RecoveryThreshold = 1,
            EnableGaslessTransactions = true,
            AccountName = "Test Account",
            InitialBalance = 0
        };

        var result = await _service.CreateAccountAsync(request, blockchainType);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to create test account: {result.ErrorMessage}");
        }
        return result.AccountId;
    }

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act & Assert
        _service.Should().NotBeNull();
        _service.Name.Should().Be("AbstractAccountService");
        _service.Description.Should().Be("Account abstraction and smart wallet functionality");
        _service.Version.Should().Be("1.0.0");
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task CreateAccountAsync_WithValidRequest_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            OwnerPublicKey = "03c663ba46afa8349f020eb9e8f9e1dc1c8e877b9d239e9110d1fdd7152e7c59dd",
            InitialGuardians = new[] { GenerateTestAddress(blockchainType) },
            RecoveryThreshold = 1,
            EnableGaslessTransactions = true,
            AccountName = "Test Account",
            InitialBalance = 0
        };

        // Act
        var result = await _service.CreateAccountAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccountId.Should().NotBeNullOrEmpty();
        result.AccountAddress.Should().NotBeNullOrEmpty();
        VerifyLoggerCalled(_loggerMock, LogLevel.Debug);
    }

    [Fact]
    public async Task CreateAccountAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.CreateAccountAsync(null!, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task CreateAccountAsync_WithUnsupportedBlockchain_ShouldThrowNotSupportedException()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            OwnerPublicKey = "03c663ba46afa8349f020eb9e8f9e1dc1c8e877b9d239e9110d1fdd7152e7c59dd"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.CreateAccountAsync(request, (BlockchainType)999));
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task ExecuteTransactionAsync_WithValidRequest_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        var accountId = await CreateTestAccountAsync(blockchainType);
        var request = new ExecuteTransactionRequest
        {
            AccountId = accountId,
            ToAddress = GenerateTestAddress(blockchainType),
            Value = 1.5m,
            Data = "0x1234",
            GasLimit = 21000
        };

        // Act
        var result = await _service.ExecuteTransactionAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.TransactionHash.Should().NotBeNullOrEmpty();
        VerifyLoggerCalled(_loggerMock, LogLevel.Debug);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task ExecuteBatchTransactionAsync_WithValidRequest_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        var accountId = await CreateTestAccountAsync(blockchainType);
        var request = new BatchTransactionRequest
        {
            AccountId = accountId,
            Transactions = new List<ExecuteTransactionRequest>
            {
                new ExecuteTransactionRequest
                {
                    AccountId = accountId,
                    ToAddress = GenerateTestAddress(blockchainType),
                    Value = 1.0m,
                    Data = "0x1234"
                },
                new ExecuteTransactionRequest
                {
                    AccountId = accountId,
                    ToAddress = GenerateTestAddress(blockchainType),
                    Value = 2.0m,
                    Data = "0x5678"
                }
            }
        };

        // Act
        var result = await _service.ExecuteBatchTransactionAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.AllSuccessful.Should().BeTrue();
        result.Results.Should().HaveCount(2);
        result.Results.Should().OnlyContain(r => r.Success);
        VerifyLoggerCalled(_loggerMock, LogLevel.Debug);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task AddGuardianAsync_WithValidRequest_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        var accountId = await CreateTestAccountAsync(blockchainType);
        var request = new AddGuardianRequest
        {
            AccountId = accountId,
            GuardianAddress = GenerateTestAddress(blockchainType),
            GuardianName = "Emergency Guardian"
        };

        // Act
        var result = await _service.AddGuardianAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.GuardianId.Should().NotBeNullOrEmpty();
        VerifyLoggerCalled(_loggerMock, LogLevel.Debug);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task InitiateRecoveryAsync_WithValidRequest_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        var accountId = await CreateTestAccountAsync(blockchainType);
        var request = new InitiateRecoveryRequest
        {
            AccountId = accountId,
            NewOwnerPublicKey = "02c663ba46afa8349f020eb9e8f9e1dc1c8e877b9d239e9110d1fdd7152e7c59dd",
            Reason = "Test recovery"
        };

        // Act
        var result = await _service.InitiateRecoveryAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RecoveryId.Should().NotBeNullOrEmpty();
        VerifyLoggerCalled(_loggerMock, LogLevel.Debug);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task CompleteRecoveryAsync_WithValidRequest_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        var request = new CompleteRecoveryRequest
        {
            RecoveryId = "test-recovery-id",
            GuardianSignatures = new List<GuardianSignature>
            {
                new GuardianSignature
                {
                    GuardianAddress = GenerateTestAddress(blockchainType),
                    Signature = "signature1",
                    SignedAt = DateTime.UtcNow
                },
                new GuardianSignature
                {
                    GuardianAddress = GenerateTestAddress(blockchainType),
                    Signature = "signature2",
                    SignedAt = DateTime.UtcNow
                }
            }
        };

        // Act
        var result = await _service.CompleteRecoveryAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.TransactionHash.Should().NotBeNullOrEmpty();
        VerifyLoggerCalled(_loggerMock, LogLevel.Debug);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task CreateSessionKeyAsync_WithValidRequest_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        var accountId = await CreateTestAccountAsync(blockchainType);
        var request = new CreateSessionKeyRequest
        {
            AccountId = accountId,
            Permissions = new SessionKeyPermissions
            {
                MaxTransactionValue = 1000m,
                AllowedContracts = new List<string> { GenerateTestAddress(blockchainType) },
                AllowedFunctions = new List<string> { "transfer", "approve" },
                MaxTransactionsPerDay = 100,
                AllowGaslessTransactions = true
            },
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            Name = "Test Session Key"
        };

        // Act
        var result = await _service.CreateSessionKeyAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SessionKeyId.Should().NotBeNullOrEmpty();
        result.PublicKey.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        VerifyLoggerCalled(_loggerMock, LogLevel.Debug);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task RevokeSessionKeyAsync_WithValidRequest_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        var accountId = await CreateTestAccountAsync(blockchainType);
        var request = new RevokeSessionKeyRequest
        {
            AccountId = accountId,
            SessionKeyId = "test-session-key-id"
        };

        // Act
        var result = await _service.RevokeSessionKeyAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        VerifyLoggerCalled(_loggerMock, LogLevel.Debug);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task GetAccountInfoAsync_WithValidAccountId_ShouldReturnAccountInfo(BlockchainType blockchainType)
    {
        // Arrange
        var accountId = await CreateTestAccountAsync(blockchainType);

        // Act
        var result = await _service.GetAccountInfoAsync(accountId, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.AccountId.Should().Be(accountId);
        result.AccountAddress.Should().NotBeNullOrEmpty();
        VerifyLoggerCalled(_loggerMock, LogLevel.Debug);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task GetTransactionHistoryAsync_WithValidRequest_ShouldReturnHistory(BlockchainType blockchainType)
    {
        // Arrange
        var accountId = await CreateTestAccountAsync(blockchainType);
        var request = new TransactionHistoryRequest
        {
            AccountId = accountId,
            Limit = 10,
            Offset = 0,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GetTransactionHistoryAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Transactions.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterOrEqualTo(0);
        VerifyLoggerCalled(_loggerMock, LogLevel.Debug);
    }

    [Fact]
    public async Task Service_ShouldInitializeAndStartSuccessfully()
    {
        // Act
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Assert
        _service.IsEnclaveInitialized.Should().BeTrue();
        _service.IsRunning.Should().BeTrue();
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Fact]
    public async Task Service_ShouldStopSuccessfully()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        await _service.StopAsync();

        // Assert
        _service.IsRunning.Should().BeFalse();
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Fact]
    public void Service_ShouldSupportCorrectBlockchainTypes()
    {
        // Act & Assert
        _service.SupportsBlockchain(BlockchainType.NeoN3).Should().BeTrue();
        _service.SupportsBlockchain(BlockchainType.NeoX).Should().BeTrue();
        _service.SupportsBlockchain((BlockchainType)999).Should().BeFalse();
    }

    [Fact]
    public void Service_ShouldHaveCorrectCapabilities()
    {
        // Act & Assert
        _service.Capabilities.Should().Contain(typeof(IAbstractAccountService));
    }
}
