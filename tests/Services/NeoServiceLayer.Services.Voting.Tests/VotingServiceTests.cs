using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Services.Voting;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;

namespace NeoServiceLayer.Services.Voting.Tests;

/// <summary>
/// Unit tests for the Voting Service.
/// </summary>
public class VotingServiceTests : IDisposable
{
    private readonly Mock<ILogger<VotingService>> _mockLogger;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly Mock<IStorageService> _mockStorageService;
    private readonly VotingService _votingService;

    public VotingServiceTests()
    {
        _mockLogger = new Mock<ILogger<VotingService>>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();
        _mockStorageService = new Mock<IStorageService>();
        _votingService = new VotingService(_mockLogger.Object, _mockEnclaveManager.Object, _mockStorageService.Object);

        // Initialize the enclave for testing
        _votingService.InitializeEnclaveAsync().Wait();
    }

    [Fact]
    public async Task CreateVotingStrategyAsync_ValidRequest_ReturnsStrategyId()
    {
        // Arrange
        var request = new VotingStrategyRequest
        {
            Name = "Test Strategy",
            Description = "Test voting strategy",
            OwnerAddress = "test-owner-address",
            StrategyType = VotingStrategyType.StabilityFocused,
            Rules = new VotingRules
            {
                MaxCandidates = 21,
                OnlyActiveNodes = true
            }
        };

        _mockStorageService.Setup(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>(), It.IsAny<BlockchainType>()))
            .ReturnsAsync(new StorageMetadata());

        // Act
        var result = await _votingService.CreateVotingStrategyAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNullOrEmpty();
        _mockStorageService.Verify(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>(), BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task CreateVotingStrategyAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _votingService.CreateVotingStrategyAsync(null!, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task CreateVotingStrategyAsync_UnsupportedBlockchain_ThrowsNotSupportedException()
    {
        // Arrange
        var request = new VotingStrategyRequest
        {
            Name = "Test Strategy",
            OwnerAddress = "test-owner-address"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _votingService.CreateVotingStrategyAsync(request, BlockchainType.NeoX));
    }

    [Fact]
    public async Task ExecuteVotingAsync_ValidStrategy_ReturnsTrue()
    {
        // Arrange
        var request = new VotingStrategyRequest
        {
            Name = "Test Strategy",
            OwnerAddress = "test-owner-address",
            StrategyType = VotingStrategyType.StabilityFocused,
            Rules = new VotingRules { MaxCandidates = 5 }
        };

        _mockStorageService.Setup(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>(), It.IsAny<BlockchainType>()))
            .ReturnsAsync(new StorageMetadata());

        var strategyId = await _votingService.CreateVotingStrategyAsync(request, BlockchainType.NeoN3);

        // Act
        var result = await _votingService.ExecuteVotingAsync(strategyId, "test-voter-address", BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteVotingAsync_NonExistentStrategy_ReturnsFalse()
    {
        // Act
        var result = await _votingService.ExecuteVotingAsync("non-existent-strategy", "test-voter-address", BlockchainType.NeoN3);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetVotingStrategyAsync_ExistingStrategy_ReturnsStrategy()
    {
        // Arrange
        var request = new VotingStrategyRequest
        {
            Name = "Test Strategy",
            OwnerAddress = "test-owner-address",
            StrategyType = VotingStrategyType.Automatic
        };

        _mockStorageService.Setup(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>(), It.IsAny<BlockchainType>()))
            .ReturnsAsync(new StorageMetadata());

        var strategyId = await _votingService.CreateVotingStrategyAsync(request, BlockchainType.NeoN3);

        // Act
        var result = await _votingService.GetVotingStrategiesAsync(request.OwnerAddress, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Test Strategy");
        result.First().StrategyType.Should().Be(VotingStrategyType.Automatic);
    }

    [Fact]
    public async Task GetVotingStrategiesAsync_NoStrategies_ReturnsEmptyCollection()
    {
        // Act
        var result = await _votingService.GetVotingStrategiesAsync("test-owner", BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateVotingStrategyAsync_ExistingStrategy_ReturnsTrue()
    {
        // Arrange
        var request = new VotingStrategyRequest
        {
            Name = "Test Strategy",
            OwnerAddress = "test-owner-address"
        };

        _mockStorageService.Setup(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>(), It.IsAny<BlockchainType>()))
            .ReturnsAsync(new StorageMetadata());

        var strategyId = await _votingService.CreateVotingStrategyAsync(request, BlockchainType.NeoN3);

        var update = new VotingStrategyUpdate
        {
            Name = "Updated Strategy",
            Description = "Updated description"
        };

        // Act
        var result = await _votingService.UpdateVotingStrategyAsync(strategyId, update, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateVotingStrategyAsync_NonExistentStrategy_ReturnsFalse()
    {
        // Arrange
        var update = new VotingStrategyUpdate
        {
            Name = "Updated Strategy"
        };

        // Act
        var result = await _votingService.UpdateVotingStrategyAsync("non-existent-strategy", update, BlockchainType.NeoN3);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteVotingStrategyAsync_ExistingStrategy_ReturnsTrue()
    {
        // Arrange
        var request = new VotingStrategyRequest
        {
            Name = "Test Strategy",
            OwnerAddress = "test-owner-address"
        };

        _mockStorageService.Setup(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>(), It.IsAny<BlockchainType>()))
            .ReturnsAsync(new StorageMetadata());

        var strategyId = await _votingService.CreateVotingStrategyAsync(request, BlockchainType.NeoN3);

        // Act
        var result = await _votingService.DeleteVotingStrategyAsync(strategyId, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteVotingStrategyAsync_NonExistentStrategy_ReturnsFalse()
    {
        // Act
        var result = await _votingService.DeleteVotingStrategyAsync("non-existent-strategy", BlockchainType.NeoN3);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetVotingRecommendationAsync_ValidPreferences_ReturnsRecommendation()
    {
        // Arrange
        var preferences = new VotingPreferences
        {
            Priority = VotingPriority.Stability
        };

        // Act
        var result = await _votingService.GetVotingRecommendationAsync(preferences, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.RecommendedCandidates.Should().NotBeNull();
        result.ConfidenceScore.Should().BeInRange(0, 1);
    }

    [Fact]
    public async Task GetCandidatesAsync_ReturnsCandidates()
    {
        // Act
        var result = await _votingService.GetCandidatesAsync(BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEnumerable<CandidateInfo>>();
    }

    [Fact]
    public void ServiceInfo_HasCorrectProperties()
    {
        // Assert
        _votingService.Name.Should().Be("VotingService");
        _votingService.Description.Should().Be("Neo N3 council member voting assistance service");
        _votingService.Version.Should().Be("1.0.0");
        _votingService.SupportedBlockchains.Should().Contain(BlockchainType.NeoN3);
        _votingService.SupportedBlockchains.Should().NotContain(BlockchainType.NeoX);
    }

    [Fact]
    public void SupportsBlockchain_NeoN3_ReturnsTrue()
    {
        // Act & Assert
        _votingService.SupportsBlockchain(BlockchainType.NeoN3).Should().BeTrue();
    }

    [Fact]
    public void SupportsBlockchain_NeoX_ReturnsFalse()
    {
        // Act & Assert
        _votingService.SupportsBlockchain(BlockchainType.NeoX).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    public async Task ExecuteVotingAsync_EmptyStrategyId_ThrowsArgumentException(string strategyId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _votingService.ExecuteVotingAsync(strategyId, "test-voter", BlockchainType.NeoN3));
    }

    [Fact]
    public async Task ExecuteVotingAsync_NullStrategyId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _votingService.ExecuteVotingAsync(null!, "test-voter", BlockchainType.NeoN3));
    }

    [Theory]
    [InlineData("")]
    public async Task ExecuteVotingAsync_EmptyVoterAddress_ThrowsArgumentException(string voterAddress)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _votingService.ExecuteVotingAsync("test-strategy", voterAddress, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task ExecuteVotingAsync_NullVoterAddress_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _votingService.ExecuteVotingAsync("test-strategy", null!, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task UpdateVotingStrategyAsync_NullUpdate_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _votingService.UpdateVotingStrategyAsync("test-strategy", null!, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task GetVotingRecommendationAsync_NullPreferences_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _votingService.GetVotingRecommendationAsync(null!, BlockchainType.NeoN3));
    }

    public void Dispose()
    {
        _votingService?.Dispose();
    }
}
