using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using NeoServiceLayer.Services.AbstractAccount;
using NeoServiceLayer.Services.AbstractAccount.Models;
using NeoServiceLayer.TestInfrastructure;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.AbstractAccount.Tests;

public class AbstractAccountServiceTests : TestBase
{
    private readonly Mock<ILogger<AbstractAccountService>> _loggerMock;
    private readonly AbstractAccountService _service;

    public AbstractAccountServiceTests()
    {
        _loggerMock = new Mock<ILogger<AbstractAccountService>>();
        _service = new AbstractAccountService(_loggerMock.Object, MockEnclaveWrapper.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act & Assert
        _service.Should().NotBeNull();
        _service.ServiceName.Should().Be("AbstractAccountService");
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
            GuardianAddresses = new[] { GenerateTestAddress(blockchainType) },
            RequiredConfirmations = 1,
            SessionKeyDuration = TimeSpan.FromHours(24)
        };

        // Act
        var result = await _service.CreateAccountAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccountId.Should().NotBeNullOrEmpty();
        result.Address.Should().NotBeNullOrEmpty();
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
        var request = new ExecuteTransactionRequest
        {
            AccountId = "test-account-id",
            To = GenerateTestAddress(blockchainType),
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
        var request = new BatchTransactionRequest
        {
            AccountId = "test-account-id",
            Transactions = new[]
            {
                new TransactionRequest
                {
                    To = GenerateTestAddress(blockchainType),
                    Value = 1.0m,
                    Data = "0x1234"
                },
                new TransactionRequest
                {
                    To = GenerateTestAddress(blockchainType),
                    Value = 2.0m,
                    Data = "0x5678"
                }
            }
        };

        // Act
        var result = await _service.ExecuteBatchTransactionAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.TransactionHashes.Should().HaveCount(2);
        result.TransactionHashes.Should().OnlyContain(hash => !string.IsNullOrEmpty(hash));
        VerifyLoggerCalled(_loggerMock, LogLevel.Debug);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task AddGuardianAsync_WithValidRequest_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        var request = new AddGuardianRequest
        {
            AccountId = "test-account-id",
            GuardianAddress = GenerateTestAddress(blockchainType),
            GuardianName = "Emergency Guardian",
            RequiredConfirmations = 2
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
        var request = new InitiateRecoveryRequest
        {
            AccountId = "test-account-id",
            NewOwnerPublicKey = "02c663ba46afa8349f020eb9e8f9e1dc1c8e877b9d239e9110d1fdd7152e7c59dd",
            GuardianSignatures = new[]
            {
                new GuardianSignature
                {
                    GuardianAddress = GenerateTestAddress(blockchainType),
                    Signature = "signature1"
                }
            }
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
            GuardianSignatures = new[]
            {
                new GuardianSignature
                {
                    GuardianAddress = GenerateTestAddress(blockchainType),
                    Signature = "signature1"
                },
                new GuardianSignature
                {
                    GuardianAddress = GenerateTestAddress(blockchainType),
                    Signature = "signature2"
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
        var request = new CreateSessionKeyRequest
        {
            AccountId = "test-account-id",
            Duration = TimeSpan.FromHours(24),
            Permissions = new[] { "transfer", "approve" },
            SpendingLimit = 1000m
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
        var request = new RevokeSessionKeyRequest
        {
            AccountId = "test-account-id",
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
        var accountId = "test-account-id";

        // Act
        var result = await _service.GetAccountInfoAsync(accountId, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.AccountId.Should().Be(accountId);
        result.Address.Should().NotBeNullOrEmpty();
        VerifyLoggerCalled(_loggerMock, LogLevel.Debug);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task GetTransactionHistoryAsync_WithValidRequest_ShouldReturnHistory(BlockchainType blockchainType)
    {
        // Arrange
        var request = new TransactionHistoryRequest
        {
            AccountId = "test-account-id",
            PageSize = 10,
            PageIndex = 0,
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GetTransactionHistoryAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
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
        _service.IsInitialized.Should().BeTrue();
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
        _service.HasCapability<IAbstractAccountService>().Should().BeTrue();
    }
}
