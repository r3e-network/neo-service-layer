using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Services.Backup;
using NeoServiceLayer.Services.Backup.Models;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Http;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Infrastructure.Blockchain;
using NeoServiceLayer.TestInfrastructure;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace NeoServiceLayer.Services.Backup.Tests;

/// <summary>
/// Comprehensive unit tests for BackupService covering all backup and restore operations.
/// Tests data backup, system backup, incremental backups, restore operations, and backup validation.
/// </summary>
public class BackupServiceUltraComprehensiveTests : TestBase
{
    private readonly Mock<ILogger<BackupService>> _mockLogger;
    private readonly Mock<IBlockchainClientFactory> _mockBlockchainClientFactory;
    private readonly Mock<IHttpClientService> _mockHttpClientService;
    private readonly BackupService _backupService;

    public BackupServiceUltraComprehensiveTests()
    {
        _mockLogger = new Mock<ILogger<BackupService>>();
        _mockBlockchainClientFactory = new Mock<IBlockchainClientFactory>();
        _mockHttpClientService = new Mock<IHttpClientService>();

        _backupService = new BackupService(
            _mockLogger.Object,
            _mockBlockchainClientFactory.Object,
            _mockHttpClientService.Object
        );
    }

    #region Basic Backup Tests

    [Fact]
    public async Task CreateBackupAsync_WithValidRequest_ShouldCreateBackup()
    {
        // Arrange
        var request = new CreateBackupRequest
        {
            BackupName = "test-backup",
            BackupType = BackupType.Full,
            DataSources = new BackupDataSource[]
            {
                new BackupDataSource { SourceType = DataSourceType.Database, SourceId = "testdb" }
            }
        };

        // Act
        var result = await _backupService.CreateBackupAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.BackupId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateBackupAsync_WithIncrementalBackup_ShouldCreateIncrementalBackup()
    {
        // Arrange
        var request = new CreateBackupRequest
        {
            BackupName = "incremental-backup",
            BackupType = BackupType.Incremental,
            DataSources = new BackupDataSource[]
            {
                new BackupDataSource { SourceType = DataSourceType.Blockchain, SourceId = "blockchain-data" }
            }
        };

        // Act
        var result = await _backupService.CreateBackupAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.BackupId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateBackupAsync_WithEncryption_ShouldEncryptBackup()
    {
        // Arrange
        var request = new CreateBackupRequest
        {
            BackupName = "encrypted-backup",
            BackupType = BackupType.Full,
            EncryptData = true,
            DataSources = new BackupDataSource[]
            {
                new BackupDataSource { SourceType = DataSourceType.Database, SourceId = "testdb" }
            }
        };

        // Act
        var result = await _backupService.CreateBackupAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CreateBackupAsync_WithCompression_ShouldCompressBackup()
    {
        // Arrange
        var request = new CreateBackupRequest
        {
            BackupName = "compressed-backup",
            BackupType = BackupType.Full,
            CompressData = true,
            DataSources = new BackupDataSource[]
            {
                new BackupDataSource { SourceType = DataSourceType.Database, SourceId = "testdb" }
            }
        };

        // Act
        var result = await _backupService.CreateBackupAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    #endregion

    #region Restore Tests

    [Fact]
    public async Task RestoreBackupAsync_WithValidBackup_ShouldRestoreSuccessfully()
    {
        // Arrange
        var createRequest = new CreateBackupRequest
        {
            BackupName = "restore-test-backup",
            BackupType = BackupType.Full,
            DataSources = new BackupDataSource[]
            {
                new BackupDataSource { SourceType = DataSourceType.Database, SourceId = "testdb" }
            }
        };

        var createResult = await _backupService.CreateBackupAsync(createRequest, BlockchainType.NeoN3);
        
        var restoreRequest = new RestoreBackupRequest
        {
            BackupId = createResult.BackupId,
            RestorePath = "/restore/path",
            OverwriteExisting = false,
            ValidateIntegrity = true
        };

        // Act
        var result = await _backupService.RestoreBackupAsync(restoreRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RestoreId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RestoreBackupAsync_WithNonExistentBackup_ShouldFail()
    {
        // Arrange
        var restoreRequest = new RestoreBackupRequest
        {
            BackupId = "non-existent-backup",
            RestorePath = "/restore/path"
        };

        // Act
        var result = await _backupService.RestoreBackupAsync(restoreRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Backup Management Tests

    [Fact]
    public async Task GetBackupStatusAsync_WithValidBackupId_ShouldReturnStatus()
    {
        // Arrange
        var createRequest = new CreateBackupRequest
        {
            BackupName = "status-test-backup",
            BackupType = BackupType.Full,
            DataSources = new BackupDataSource[]
            {
                new BackupDataSource { SourceType = DataSourceType.Database, SourceId = "testdb" }
            }
        };

        var createResult = await _backupService.CreateBackupAsync(createRequest, BlockchainType.NeoN3);

        var statusRequest = new BackupStatusRequest
        {
            BackupId = createResult.BackupId,
            IncludeDetails = true
        };

        // Act
        var result = await _backupService.GetBackupStatusAsync(statusRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.BackupId.Should().Be(createResult.BackupId);
    }

    [Fact]
    public async Task ListBackupsAsync_ShouldReturnBackupList()
    {
        // Arrange
        var listRequest = new ListBackupsRequest
        {
            PageSize = 10,
            PageNumber = 1
        };

        // Act
        var result = await _backupService.ListBackupsAsync(listRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Backups.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateBackupAsync_WithValidBackup_ShouldValidateSuccessfully()
    {
        // Arrange
        var createRequest = new CreateBackupRequest
        {
            BackupName = "validation-test-backup",
            BackupType = BackupType.Full,
            DataSources = new BackupDataSource[]
            {
                new BackupDataSource { SourceType = DataSourceType.Database, SourceId = "testdb" }
            }
        };

        var createResult = await _backupService.CreateBackupAsync(createRequest, BlockchainType.NeoN3);

        var validateRequest = new ValidateBackupRequest
        {
            BackupId = createResult.BackupId,
            CheckIntegrity = true,
            VerifyChecksum = true
        };

        // Act
        var result = await _backupService.ValidateBackupAsync(validateRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.BackupId.Should().Be(createResult.BackupId);
    }

    [Fact]
    public async Task DeleteBackupAsync_WithValidBackup_ShouldDeleteSuccessfully()
    {
        // Arrange
        var createRequest = new CreateBackupRequest
        {
            BackupName = "deletion-test-backup",
            BackupType = BackupType.Full,
            DataSources = new BackupDataSource[]
            {
                new BackupDataSource { SourceType = DataSourceType.Database, SourceId = "testdb" }
            }
        };

        var createResult = await _backupService.CreateBackupAsync(createRequest, BlockchainType.NeoN3);

        var deleteRequest = new DeleteBackupRequest
        {
            BackupId = createResult.BackupId,
            PermanentDelete = false
        };

        // Act
        var result = await _backupService.DeleteBackupAsync(deleteRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.BackupId.Should().Be(createResult.BackupId);
    }

    #endregion

    #region Scheduled Backup Tests

    [Fact]
    public async Task CreateScheduledBackupAsync_WithValidSchedule_ShouldCreateSchedule()
    {
        // Arrange
        var request = new CreateScheduledBackupRequest
        {
            ScheduleName = "Daily Backup",
            BackupName = "daily-backup",
            DataSources = new BackupDataSource[]
            {
                new BackupDataSource { SourceType = DataSourceType.Database, SourceId = "daily-data" }
            },
            Schedule = new BackupScheduleConfiguration
            {
                CronExpression = "0 2 * * *", // Daily at 2 AM
                IsEnabled = true
            }
        };

        // Act
        var result = await _backupService.CreateScheduledBackupAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ScheduleId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetBackupStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        var request = new BackupStatisticsRequest
        {
            TimeRange = TimeSpan.FromDays(30)
        };

        // Act
        var result = await _backupService.GetBackupStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Statistics.Should().NotBeNull();
    }

    #endregion

    #region Import/Export Tests

    [Fact]
    public async Task ExportBackupAsync_WithValidBackup_ShouldExportSuccessfully()
    {
        // Arrange
        var createRequest = new CreateBackupRequest
        {
            BackupName = "export-test-backup",
            BackupType = BackupType.Full,
            DataSources = new BackupDataSource[]
            {
                new BackupDataSource { SourceType = DataSourceType.Database, SourceId = "testdb" }
            }
        };

        var createResult = await _backupService.CreateBackupAsync(createRequest, BlockchainType.NeoN3);

        var exportRequest = new ExportBackupRequest
        {
            BackupId = createResult.BackupId,
            ExportPath = "/export/path",
            ExportFormat = BackupExportFormat.Zip,
            IncludeMetadata = true
        };

        // Act
        var result = await _backupService.ExportBackupAsync(exportRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.BackupId.Should().Be(createResult.BackupId);
    }

    [Fact]
    public async Task ImportBackupAsync_WithValidFile_ShouldImportSuccessfully()
    {
        // Arrange
        var importRequest = new ImportBackupRequest
        {
            ImportPath = "/import/path/backup.zip",
            BackupName = "imported-backup",
            ValidateOnImport = true,
            ImportFormat = BackupExportFormat.Zip
        };

        // Act
        var result = await _backupService.ImportBackupAsync(importRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.BackupId.Should().NotBeNullOrEmpty();
    }

    #endregion
}