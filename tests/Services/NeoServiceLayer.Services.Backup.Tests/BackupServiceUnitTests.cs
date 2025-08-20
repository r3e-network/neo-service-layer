using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Blockchain;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Services.Backup;
using NeoServiceLayer.Services.Backup.Models;
using NeoServiceLayer.Shared.Configuration;
using Xunit;

namespace NeoServiceLayer.Services.Backup.Tests;

public class BackupServiceUnitTests : IDisposable
{
    private readonly Mock<ILogger<BackupService>> _mockLogger;
    private readonly Mock<IPersistentStorageProvider> _mockStorageProvider;
    private readonly Mock<IServiceConfiguration> _mockConfig;
    private readonly Mock<IHealthCheckService> _mockHealthCheck;
    private readonly Mock<ITelemetryCollector> _mockTelemetry;
    private readonly Mock<IHttpClientService> _mockHttpClient;
    private readonly Mock<IBlockchainClientFactory> _mockBlockchainFactory;
    private readonly Mock<ISecretsManager> _mockSecretsManager;
    private readonly BackupService _backupService;

    public BackupServiceUnitTests()
    {
        _mockLogger = new Mock<ILogger<BackupService>>();
        _mockStorageProvider = new Mock<IPersistentStorageProvider>();
        _mockConfig = new Mock<IServiceConfiguration>();
        _mockHealthCheck = new Mock<IHealthCheckService>();
        _mockTelemetry = new Mock<ITelemetryCollector>();
        _mockHttpClient = new Mock<IHttpClientService>();
        _mockBlockchainFactory = new Mock<IBlockchainClientFactory>();
        _mockSecretsManager = new Mock<ISecretsManager>();

        _mockConfig.Setup(x => x.GetSetting("BackupService:MaxBackupSize", "1GB"))
               .Returns("1GB");
        _mockConfig.Setup(x => x.GetSetting("BackupService:RetentionDays", "30"))
               .Returns("30");

        _backupService = new BackupService(
            _mockLogger.Object,
            _mockStorageProvider.Object,
            _mockConfig.Object,
            _mockHealthCheck.Object,
            _mockTelemetry.Object,
            _mockHttpClient.Object,
            _mockBlockchainFactory.Object,
            _mockSecretsManager.Object);
    }

    [Fact]
    public async Task InitializeAsync_InitializesSuccessfully()
    {
        var result = await _backupService.InitializeAsync();

        result.Should().BeTrue();
        _backupService.Name.Should().Be("BackupService");
        _backupService.ServiceType.Should().Be("BackupService");
    }

    [Fact]
    public async Task CreateBackupAsync_WithValidRequest_CreatesBackupSuccessfully()
    {
        // Arrange
        await _backupService.InitializeAsync();
        await _backupService.StartAsync();

        var request = new CreateBackupRequest
        {
            BackupId = "test-backup-001",
            DataType = BackupDataType.KeyStore,
            SourcePath = "/data/keystore",
            Description = "Test backup",
            IncludeMetadata = true,
            CompressionLevel = CompressionLevel.Standard
        };

        var expectedBackup = new BackupMetadata
        {
            BackupId = request.BackupId,
            DataType = request.DataType,
            Status = BackupStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };

        _mockStorageProvider.Setup(x => x.StoreAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _backupService.CreateBackupAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.BackupId.Should().Be(request.BackupId);
        result.Status.Should().Be(BackupStatus.InProgress);
        result.DataType.Should().Be(request.DataType);

        _mockStorageProvider.Verify(x => x.StoreAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()), 
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateBackupAsync_WithInvalidRequest_ThrowsArgumentException()
    {
        // Arrange
        await _backupService.InitializeAsync();
        await _backupService.StartAsync();

        var request = new CreateBackupRequest
        {
            BackupId = "", // Invalid empty ID
            DataType = BackupDataType.KeyStore,
            SourcePath = "/data/keystore"
        };

        // Act & Assert
        Func<Task> act = async () => await _backupService.CreateBackupAsync(request, BlockchainType.NeoN3);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetBackupStatusAsync_WithExistingBackup_ReturnsStatus()
    {
        // Arrange
        await _backupService.InitializeAsync();
        await _backupService.StartAsync();

        var backupId = "test-backup-001";
        var expectedMetadata = new BackupMetadata
        {
            BackupId = backupId,
            Status = BackupStatus.Completed,
            DataType = BackupDataType.KeyStore,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow,
            Size = 1024
        };

        _mockStorageProvider.Setup(x => x.ExistsAsync($"backups/metadata/{backupId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"backups/metadata/{backupId}"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(expectedMetadata));

        // Act
        var result = await _backupService.GetBackupStatusAsync(backupId, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.BackupId.Should().Be(backupId);
        result.Status.Should().Be(BackupStatus.Completed);
        result.DataType.Should().Be(BackupDataType.KeyStore);
    }

    [Fact]
    public async Task GetBackupStatusAsync_WithNonExistingBackup_ReturnsNull()
    {
        // Arrange
        await _backupService.InitializeAsync();
        await _backupService.StartAsync();

        var backupId = "non-existent-backup";

        _mockStorageProvider.Setup(x => x.ExistsAsync($"backups/metadata/{backupId}"))
            .ReturnsAsync(false);

        // Act
        var result = await _backupService.GetBackupStatusAsync(backupId, BlockchainType.NeoN3);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RestoreBackupAsync_WithValidBackup_RestoresSuccessfully()
    {
        // Arrange
        await _backupService.InitializeAsync();
        await _backupService.StartAsync();

        var request = new RestoreBackupRequest
        {
            BackupId = "test-backup-001",
            TargetPath = "/restore/keystore",
            OverwriteExisting = true,
            ValidateIntegrity = true
        };

        var backupData = new byte[] { 1, 2, 3, 4, 5 };
        var metadata = new BackupMetadata
        {
            BackupId = request.BackupId,
            Status = BackupStatus.Completed,
            DataType = BackupDataType.KeyStore,
            Size = backupData.Length
        };

        _mockStorageProvider.Setup(x => x.ExistsAsync($"backups/metadata/{request.BackupId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"backups/metadata/{request.BackupId}"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(metadata));
        _mockStorageProvider.Setup(x => x.GetAsync($"backups/data/{request.BackupId}"))
            .ReturnsAsync(backupData);

        // Act
        var result = await _backupService.RestoreBackupAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.BackupId.Should().Be(request.BackupId);
        result.Status.Should().Be(RestoreStatus.Completed);
        result.RestoredPath.Should().Be(request.TargetPath);
    }

    [Fact]
    public async Task DeleteBackupAsync_WithExistingBackup_DeletesSuccessfully()
    {
        // Arrange
        await _backupService.InitializeAsync();
        await _backupService.StartAsync();

        var backupId = "test-backup-001";

        _mockStorageProvider.Setup(x => x.ExistsAsync($"backups/metadata/{backupId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.DeleteAsync($"backups/metadata/{backupId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.DeleteAsync($"backups/data/{backupId}"))
            .ReturnsAsync(true);

        // Act
        var result = await _backupService.DeleteBackupAsync(backupId, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();

        _mockStorageProvider.Verify(x => x.DeleteAsync($"backups/metadata/{backupId}"), Times.Once);
        _mockStorageProvider.Verify(x => x.DeleteAsync($"backups/data/{backupId}"), Times.Once);
    }

    [Fact]
    public async Task ListBackupsAsync_ReturnsBackupList()
    {
        // Arrange
        await _backupService.InitializeAsync();
        await _backupService.StartAsync();

        var filter = new BackupFilter
        {
            DataType = BackupDataType.KeyStore,
            Status = BackupStatus.Completed,
            CreatedAfter = DateTime.UtcNow.AddDays(-7)
        };

        var backupIds = new[] { "backup-001", "backup-002" };
        var metadata1 = new BackupMetadata
        {
            BackupId = "backup-001",
            Status = BackupStatus.Completed,
            DataType = BackupDataType.KeyStore,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };
        var metadata2 = new BackupMetadata
        {
            BackupId = "backup-002",
            Status = BackupStatus.Completed,
            DataType = BackupDataType.KeyStore,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _mockStorageProvider.Setup(x => x.ListKeysAsync("backups/metadata/", It.IsAny<string>()))
            .ReturnsAsync(backupIds.Select(id => $"backups/metadata/{id}"));
        _mockStorageProvider.Setup(x => x.GetAsync("backups/metadata/backup-001"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(metadata1));
        _mockStorageProvider.Setup(x => x.GetAsync("backups/metadata/backup-002"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(metadata2));

        // Act
        var result = await _backupService.ListBackupsAsync(filter, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Count().Should().Be(2);
        result.All(b => b.DataType == BackupDataType.KeyStore).Should().BeTrue();
        result.All(b => b.Status == BackupStatus.Completed).Should().BeTrue();
    }

    [Fact]
    public async Task ValidateBackupAsync_WithValidBackup_ReturnsValidResult()
    {
        // Arrange
        await _backupService.InitializeAsync();
        await _backupService.StartAsync();

        var backupId = "test-backup-001";
        var backupData = new byte[] { 1, 2, 3, 4, 5 };
        var metadata = new BackupMetadata
        {
            BackupId = backupId,
            Status = BackupStatus.Completed,
            DataType = BackupDataType.KeyStore,
            Size = backupData.Length,
            Checksum = "test-checksum"
        };

        _mockStorageProvider.Setup(x => x.ExistsAsync($"backups/metadata/{backupId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"backups/metadata/{backupId}"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(metadata));
        _mockStorageProvider.Setup(x => x.GetAsync($"backups/data/{backupId}"))
            .ReturnsAsync(backupData);

        // Act
        var result = await _backupService.ValidateBackupAsync(backupId, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.BackupId.Should().Be(backupId);
        result.IsValid.Should().BeTrue();
        result.ValidationResults.Should().NotBeEmpty();
    }

    [Fact]
    public void Dispose_DisposesResourcesProperly()
    {
        _backupService.Dispose();
        _backupService.Status.Should().Be("Disposed");
    }

    public void Dispose()
    {
        _backupService?.Dispose();
    }
}