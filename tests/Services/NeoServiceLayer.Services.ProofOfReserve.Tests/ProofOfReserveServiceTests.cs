using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using NeoServiceLayer.Services.ProofOfReserve;
using NeoServiceLayer.TestInfrastructure;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.ProofOfReserve.Tests;

public class ProofOfReserveServiceTests : TestBase
{
    private readonly Mock<ILogger<ProofOfReserveService>> _loggerMock;
    private readonly ProofOfReserveService _service;

    public ProofOfReserveServiceTests()
    {
        _loggerMock = new Mock<ILogger<ProofOfReserveService>>();
        _service = new ProofOfReserveService(_loggerMock.Object, MockEnclaveWrapper.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act & Assert
        _service.Should().NotBeNull();
        _service.ServiceName.Should().Be("ProofOfReserveService");
        _service.Description.Should().Be("Proof of reserves and asset backing verification");
        _service.Version.Should().Be("1.0.0");
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task RegisterAssetAsync_WithValidRequest_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        var request = new AssetRegistrationRequest
        {
            AssetSymbol = "TEST",
            AssetName = "Test Token",
            ReserveAddresses = new[] { GenerateTestAddress(blockchainType) },
            MinReserveRatio = 1.0m,
            TotalSupply = 1000000m
        };

        // Act
        var result = await _service.RegisterAssetAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AssetId.Should().NotBeNullOrEmpty();
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Fact]
    public async Task RegisterAssetAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.RegisterAssetAsync(null!, BlockchainType.NeoN3));
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task GenerateProofAsync_WithValidRequest_ShouldReturnProof(BlockchainType blockchainType)
    {
        // Arrange
        var request = new ProofGenerationRequest
        {
            AssetId = "test-asset-id",
            ProofType = ProofType.MerkleProof,
            IncludeTransactionHistory = true,
            IncludeSignatures = true
        };

        // Act
        var result = await _service.GenerateProofAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ProofData.Should().NotBeNullOrEmpty();
        result.ProofHash.Should().NotBeNullOrEmpty();
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task VerifyProofAsync_WithValidProof_ShouldReturnValid(BlockchainType blockchainType)
    {
        // Arrange
        var proofData = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9...";
        var assetId = "test-asset-id";

        // Act
        var result = await _service.VerifyProofAsync(proofData, assetId, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.AssetId.Should().Be(assetId);
        result.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task GetReserveStatusAsync_WithValidAssetId_ShouldReturnStatus(BlockchainType blockchainType)
    {
        // Arrange
        var assetId = "test-asset-id";

        // First register an asset
        var registrationRequest = new AssetRegistrationRequest
        {
            AssetSymbol = "TEST",
            AssetName = "Test Token",
            ReserveAddresses = new[] { GenerateTestAddress(blockchainType) },
            MinReserveRatio = 1.0m,
            TotalSupply = 1000000m
        };
        var registrationResult = await _service.RegisterAssetAsync(registrationRequest, blockchainType);
        assetId = registrationResult.AssetId;

        // Act
        var result = await _service.GetReserveStatusAsync(assetId, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.AssetId.Should().Be(assetId);
        result.AssetSymbol.Should().Be("TEST");
        result.ReserveRatio.Should().BeGreaterOrEqualTo(0);
        result.Health.Should().BeOneOf(ReserveHealthStatus.Healthy, ReserveHealthStatus.Warning, ReserveHealthStatus.Critical, ReserveHealthStatus.Undercollateralized);
        VerifyLoggerCalled(_loggerMock, LogLevel.Debug);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task UpdateReserveDataAsync_WithValidData_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        var request = new ReserveUpdateRequest
        {
            AssetId = "test-asset-id",
            NewReserveAmount = 1500000m,
            UpdateReason = "Monthly reserve audit",
            AuditorSignature = "signature-data"
        };

        // Act
        var result = await _service.UpdateReserveDataAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.UpdateId.Should().NotBeNullOrEmpty();
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task SetAlertThresholdAsync_WithValidThreshold_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        var assetId = "test-asset-id";
        var threshold = 0.9m; // 90%
        var alertType = ReserveAlertType.LowReserveRatio;

        // Act
        var result = await _service.SetAlertThresholdAsync(assetId, threshold, alertType, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AlertId.Should().NotBeNullOrEmpty();
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task GetActiveAlertsAsync_ShouldReturnAlerts(BlockchainType blockchainType)
    {
        // Act
        var result = await _service.GetActiveAlertsAsync(blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEnumerable<ReserveAlert>>();
        VerifyLoggerCalled(_loggerMock, LogLevel.Debug);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task GetReserveHistoryAsync_WithValidRequest_ShouldReturnHistory(BlockchainType blockchainType)
    {
        // Arrange
        var assetId = "test-asset-id";
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;

        // Act
        var result = await _service.GetReserveHistoryAsync(assetId, from, to, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEnumerable<ReserveSnapshot>>();
        VerifyLoggerCalled(_loggerMock, LogLevel.Debug);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task GenerateAuditReportAsync_WithValidRequest_ShouldReturnReport(BlockchainType blockchainType)
    {
        // Arrange
        var request = new AuditReportRequest
        {
            AssetId = "test-asset-id",
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            IncludeTransactionDetails = true,
            IncludeComplianceCheck = true
        };

        // Act
        var result = await _service.GenerateAuditReportAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ReportId.Should().NotBeNullOrEmpty();
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
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
        _service.HasCapability<IProofOfReserveService>().Should().BeTrue();
    }

    [Fact]
    public void GetCacheStatistics_ShouldReturnStatistics()
    {
        // Act
        var stats = _service.GetCacheStatistics();

        // Assert - Can be null if cache is not initialized
        if (stats != null)
        {
            stats.Should().BeOfType<CacheStatistics>();
        }
    }

    [Fact]
    public void GetCacheHitRatio_ShouldReturnValidRatio()
    {
        // Act
        var hitRatio = _service.GetCacheHitRatio();

        // Assert
        hitRatio.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public async Task WarmUpCacheAsync_ShouldExecuteWithoutError()
    {
        // Act & Assert
        await _service.WarmUpCacheAsync();
        // Should not throw exceptions
    }

    [Fact]
    public void ResetCacheStatistics_ShouldExecuteWithoutError()
    {
        // Act & Assert
        _service.ResetCacheStatistics();
        // Should not throw exceptions
    }
}
