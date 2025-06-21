using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Services.Voting.Storage;
using System.Text.Json;
using Xunit;

namespace NeoServiceLayer.Services.Voting.Tests.Storage;

/// <summary>
/// Unit tests for the VotingStorageHelper class.
/// </summary>
public class VotingStorageHelperTests
{
    private readonly Mock<IStorageService> _mockStorageService;
    private readonly Mock<ILogger<VotingStorageHelper>> _mockLogger;
    private readonly VotingStorageHelper _storageHelper;

    public VotingStorageHelperTests()
    {
        _mockStorageService = new Mock<IStorageService>();
        _mockLogger = new Mock<ILogger<VotingStorageHelper>>();
        _storageHelper = new VotingStorageHelper(_mockStorageService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task PersistVotingStrategiesAsync_ValidStrategies_SavesSuccessfully()
    {
        // Arrange
        var strategies = new Dictionary<string, VotingStrategy>
        {
            ["strategy1"] = new VotingStrategy
            {
                Id = "strategy1",
                Name = "Test Strategy",
                StrategyType = VotingStrategyType.StabilityFocused,
                IsActive = true,
                Rules = new VotingRules
                {
                    MaxCandidates = 21
                }
            }
        };

        _mockStorageService.Setup(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>(), It.IsAny<BlockchainType>()))
            .ReturnsAsync(new StorageMetadata());

        // Act
        await _storageHelper.PersistVotingStrategiesAsync(strategies);

        // Assert
        _mockStorageService.Verify(x => x.StoreDataAsync(
            "voting:strategies",
            It.IsAny<byte[]>(),
            It.IsAny<StorageOptions>(),
            BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task LoadVotingStrategiesAsync_ExistingData_ReturnsStrategies()
    {
        // Arrange
        var strategies = new Dictionary<string, VotingStrategy>
        {
            ["strategy1"] = new VotingStrategy
            {
                Id = "strategy1",
                Name = "Test Strategy",
                StrategyType = VotingStrategyType.StabilityFocused,
                IsActive = true,
                Rules = new VotingRules
                {
                    MaxCandidates = 21
                }
            }
        };

        var data = JsonSerializer.SerializeToUtf8Bytes(strategies);
        _mockStorageService.Setup(x => x.RetrieveDataAsync("voting:strategies", BlockchainType.NeoN3))
            .Returns(Task.FromResult(data));

        // Act
        var result = await _storageHelper.LoadVotingStrategiesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result["strategy1"].Id.Should().Be("strategy1");
        result["strategy1"].Name.Should().Be("Test Strategy");
    }

    [Fact]
    public async Task LoadVotingStrategiesAsync_NoData_ReturnsEmptyDictionary()
    {
        // Arrange
        _mockStorageService.Setup(x => x.RetrieveDataAsync("voting:strategies", BlockchainType.NeoN3))
            .ThrowsAsync(new KeyNotFoundException("No data found"));

        // Act
        var result = await _storageHelper.LoadVotingStrategiesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PersistCandidatesAsync_ValidCandidates_SavesSuccessfully()
    {
        // Arrange
        var candidates = new Dictionary<string, CandidateInfo>
        {
            ["test-public-key"] = new CandidateInfo
            {
                PublicKey = "test-public-key",
                Name = "Test Candidate",
                IsActive = true,
                VotesReceived = 1000,
                Rank = 1,
                AdditionalInfo = new Dictionary<string, object>
                {
                    ["Website"] = "https://example.com"
                }
            }
        };

        _mockStorageService.Setup(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>(), It.IsAny<BlockchainType>()))
            .ReturnsAsync(new StorageMetadata());

        // Act
        await _storageHelper.PersistCandidatesAsync(candidates);

        // Assert
        _mockStorageService.Verify(x => x.StoreDataAsync(
            "voting:candidates",
            It.IsAny<byte[]>(),
            It.IsAny<StorageOptions>(),
            BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task LoadCandidatesAsync_ExistingData_ReturnsCandidates()
    {
        // Arrange
        var candidates = new Dictionary<string, CandidateInfo>
        {
            ["test-public-key"] = new CandidateInfo
            {
                PublicKey = "test-public-key",
                Name = "Test Candidate",
                IsActive = true,
                VotesReceived = 1000,
                Rank = 1,
                AdditionalInfo = new Dictionary<string, object>
                {
                    ["Website"] = "https://example.com"
                }
            }
        };

        var data = JsonSerializer.SerializeToUtf8Bytes(candidates);
        _mockStorageService.Setup(x => x.RetrieveDataAsync("voting:candidates", BlockchainType.NeoN3))
            .Returns(Task.FromResult(data));

        // Act
        var result = await _storageHelper.LoadCandidatesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result["test-public-key"].PublicKey.Should().Be("test-public-key");
        result["test-public-key"].Name.Should().Be("Test Candidate");
    }

    [Fact]
    public async Task PersistVotingResultsAsync_ValidResults_SavesSuccessfully()
    {
        // Arrange
        var results = new Dictionary<string, VotingResult>
        {
            ["result1"] = new VotingResult
            {
                ExecutionId = "result1",
                StrategyId = "strategy1",
                VoterAddress = "voter1",
                Success = true,
                SelectedCandidates = new[] { "candidate1", "candidate2" },
                TransactionHash = "0x123456",
                ExecutionDetails = new Dictionary<string, object> { ["GasUsed"] = 1000000 }
            }
        };

        _mockStorageService.Setup(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>(), It.IsAny<BlockchainType>()))
            .ReturnsAsync(new StorageMetadata());

        // Act
        await _storageHelper.PersistVotingResultsAsync(results);

        // Assert
        _mockStorageService.Verify(x => x.StoreDataAsync(
            "voting:results",
            It.IsAny<byte[]>(),
            It.IsAny<StorageOptions>(),
            BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task LoadVotingResultsAsync_ExistingData_ReturnsResults()
    {
        // Arrange
        var results = new Dictionary<string, VotingResult>
        {
            ["result1"] = new VotingResult
            {
                ExecutionId = "result1",
                StrategyId = "strategy1",
                VoterAddress = "voter1",
                Success = true,
                SelectedCandidates = new[] { "candidate1", "candidate2" },
                TransactionHash = "0x123456",
                ExecutionDetails = new Dictionary<string, object> { ["GasUsed"] = 1000000 }
            }
        };

        var data = JsonSerializer.SerializeToUtf8Bytes(results);
        _mockStorageService.Setup(x => x.RetrieveDataAsync("voting:results", BlockchainType.NeoN3))
            .Returns(Task.FromResult(data));

        // Act
        var result = await _storageHelper.LoadVotingResultsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result["result1"].ExecutionId.Should().Be("result1");
        result["result1"].StrategyId.Should().Be("strategy1");
    }
}
