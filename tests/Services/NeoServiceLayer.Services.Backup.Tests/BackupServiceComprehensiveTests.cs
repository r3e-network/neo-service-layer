using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Http;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Infrastructure.Blockchain;
using NeoServiceLayer.Services.Backup;
using NeoServiceLayer.Services.Backup.Models;
using NeoServiceLayer.TestInfrastructure;
using System.Text.Json;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace NeoServiceLayer.Services.Backup.Tests;

/// <summary>
/// Comprehensive test suite for BackupService covering all major functionality
/// </summary>
public class BackupServiceComprehensiveTests : TestBase
{
    private readonly Mock<ILogger<BackupService>> _loggerMock;
    private readonly Mock<IBlockchainClientFactory> _blockchainClientFactoryMock;
    private readonly Mock<IHttpClientService> _httpClientServiceMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly BackupService _service;

    public BackupServiceComprehensiveTests()
    {
        _loggerMock = new Mock<ILogger<BackupService>>();
        _blockchainClientFactoryMock = new Mock<IBlockchainClientFactory>();
        _httpClientServiceMock = new Mock<IHttpClientService>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _service = new BackupService(
            _loggerMock.Object, 
            _blockchainClientFactoryMock.Object, 
            _httpClientServiceMock.Object,
            _serviceProviderMock.Object);
    }

    #region Service Initialization Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange & Act & Assert
        _service.Should().NotBeNull();
        // Remove property checks that don't exist in the actual service
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new BackupService(
            null!, 
            _blockchainClientFactoryMock.Object, 
            _httpClientServiceMock.Object);
            
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullBlockchainClientFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new BackupService(
            _loggerMock.Object, 
            null!, 
            _httpClientServiceMock.Object);
            
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("blockchainClientFactory");
    }

    [Fact]
    public void Constructor_WithNullHttpClientService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new BackupService(
            _loggerMock.Object, 
            _blockchainClientFactoryMock.Object, 
            null!);
            
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("httpClientService");
    }

    #endregion

    #region Backup Creation Tests

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
                new BackupDataSource { SourceType = DataSourceType.Blockchain, SourceId = "blockchain-data" },
                new BackupDataSource { SourceType = DataSourceType.Database, SourceId = "user-data" }
            }
        };

        // Act
        var result = await _service.CreateBackupAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.BackupId.Should().NotBeNullOrEmpty();
        result.BackupSizeBytes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateBackupAsync_WithInvalidBackupName_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateBackupRequest
        {
            BackupName = "", // Invalid empty name
            BackupType = BackupType.Full,
            DataSources = new BackupDataSource[] 
            {
                new BackupDataSource { SourceType = DataSourceType.Blockchain, SourceId = "blockchain-data" }
            }
        };

        // Act
        var result = await _service.CreateBackupAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("BackupName cannot be empty");
    }

    [Fact]
    public async Task CreateBackupAsync_WithDuplicateBackupName_ShouldReturnFailure()
    {
        // Arrange
        var backupName = "duplicate-backup";
        var request1 = new CreateBackupRequest
        {
            BackupName = backupName,
            BackupType = BackupType.Full,
            DataSources = new BackupDataSource[] 
            {
                new BackupDataSource { SourceType = DataSourceType.Blockchain, SourceId = "blockchain-data" }
            }
        };

        var request2 = new CreateBackupRequest
        {
            BackupName = backupName, // Same name
            BackupType = BackupType.Incremental,
            DataSources = new BackupDataSource[] 
            {
                new BackupDataSource { SourceType = DataSourceType.Database, SourceId = "user-data" }
            }
        };

        // Act
        await _service.CreateBackupAsync(request1, BlockchainType.NeoN3);
        var result = await _service.CreateBackupAsync(request2, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
    }

    #endregion

    #region Backup Restore Tests

    [Fact]
    public async Task RestoreBackupAsync_WithValidBackup_ShouldRestoreSuccessfully()
    {
        // Arrange
        var backupName = "test-backup";
        
        // First create a backup
        var createRequest = new CreateBackupRequest
        {
            BackupName = backupName,
            BackupType = BackupType.Full,
            DataSources = new BackupDataSource[] 
            {
                new BackupDataSource { SourceType = DataSourceType.Blockchain, SourceId = "blockchain-data" }
            }
        };
        var createResult = await _service.CreateBackupAsync(createRequest, BlockchainType.NeoN3);

        var restoreRequest = new RestoreBackupRequest
        {
            BackupId = createResult.BackupId,
            RestorePath = "restored-blockchain-data",
            OverwriteExisting = true,
            ValidateIntegrity = true
        };

        // Act
        var result = await _service.RestoreBackupAsync(restoreRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RestoreId.Should().NotBeNullOrEmpty();
        result.ItemsRestored.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RestoreBackupAsync_WithNonExistentBackup_ShouldReturnFailure()
    {
        // Arrange
        var restoreRequest = new RestoreBackupRequest
        {
            BackupId = "non-existent-backup-id",
            RestorePath = "target-location"
        };

        // Act
        var result = await _service.RestoreBackupAsync(restoreRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Backup not found");
    }

    #endregion

    #region Backup Scheduling Tests

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
        var result = await _service.CreateScheduledBackupAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ScheduleId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateScheduledBackupAsync_WithInvalidCronExpression_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateScheduledBackupRequest
        {
            ScheduleName = "Invalid Backup",
            BackupName = "invalid-backup",
            DataSources = new BackupDataSource[] 
            {
                new BackupDataSource { SourceType = DataSourceType.Database, SourceId = "data" }
            },
            Schedule = new BackupScheduleConfiguration
            {
                CronExpression = "invalid-cron", // Invalid cron expression
                IsEnabled = true
            }
        };

        // Act
        var result = await _service.CreateScheduledBackupAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid cron expression");
    }

    #endregion

    #region Backup Validation Tests

    [Fact]
    public async Task ValidateBackupAsync_WithValidBackup_ShouldValidateSuccessfully()
    {
        // Arrange
        var createRequest = new CreateBackupRequest
        {
            BackupName = "validation-test",
            BackupType = BackupType.Full,
            DataSources = new BackupDataSource[] 
            {
                new BackupDataSource { SourceType = DataSourceType.Database, SourceId = "testdb" }
            }
        };

        var createResult = await _service.CreateBackupAsync(createRequest, BlockchainType.NeoN3);

        var validateRequest = new ValidateBackupRequest
        {
            BackupId = createResult.BackupId,
            CheckIntegrity = true,
            VerifyChecksum = true
        };

        // Act
        var result = await _service.ValidateBackupAsync(validateRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.BackupId.Should().Be(createResult.BackupId);
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Backup Management Tests

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
        var result = await _service.ListBackupsAsync(listRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Backups.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteBackupAsync_WithValidBackup_ShouldDeleteSuccessfully()
    {
        // Arrange
        var createRequest = new CreateBackupRequest
        {
            BackupName = "deletion-test",
            BackupType = BackupType.Full,
            DataSources = new BackupDataSource[] 
            {
                new BackupDataSource { SourceType = DataSourceType.Database, SourceId = "testdb" }
            }
        };

        var createResult = await _service.CreateBackupAsync(createRequest, BlockchainType.NeoN3);

        var deleteRequest = new DeleteBackupRequest
        {
            BackupId = createResult.BackupId,
            PermanentDelete = false
        };

        // Act
        var result = await _service.DeleteBackupAsync(deleteRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.BackupId.Should().Be(createResult.BackupId);
    }

    [Fact]
    public async Task GetBackupStatusAsync_WithValidBackupId_ShouldReturnStatus()
    {
        // Arrange
        var createRequest = new CreateBackupRequest
        {
            BackupName = "status-test",
            BackupType = BackupType.Full,
            DataSources = new BackupDataSource[] 
            {
                new BackupDataSource { SourceType = DataSourceType.Database, SourceId = "testdb" }
            }
        };

        var createResult = await _service.CreateBackupAsync(createRequest, BlockchainType.NeoN3);

        var statusRequest = new BackupStatusRequest
        {
            BackupId = createResult.BackupId,
            IncludeDetails = true
        };

        // Act
        var result = await _service.GetBackupStatusAsync(statusRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.BackupId.Should().Be(createResult.BackupId);
        result.Status.Should().NotBe(BackupStatus.Failed);
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
        var result = await _service.GetBackupStatisticsAsync(request, BlockchainType.NeoN3);

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
            BackupName = "export-test",
            BackupType = BackupType.Full,
            DataSources = new BackupDataSource[] 
            {
                new BackupDataSource { SourceType = DataSourceType.Database, SourceId = "testdb" }
            }
        };

        var createResult = await _service.CreateBackupAsync(createRequest, BlockchainType.NeoN3);

        var exportRequest = new ExportBackupRequest
        {
            BackupId = createResult.BackupId,
            ExportPath = "/export/path",
            ExportFormat = BackupExportFormat.Zip,
            IncludeMetadata = true
        };

        // Act
        var result = await _service.ExportBackupAsync(exportRequest, BlockchainType.NeoN3);

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
        var result = await _service.ImportBackupAsync(importRequest, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.BackupId.Should().NotBeNullOrEmpty();
    }

    #endregion
}