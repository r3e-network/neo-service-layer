using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Api.Controllers;
using NeoServiceLayer.Services.Backup;
using NeoServiceLayer.Services.Backup.Models;
using Xunit;

namespace NeoServiceLayer.Api.Tests.Controllers;

public class BackupControllerUnitTests
{
    private readonly Mock<ILogger<BackupController>> _mockLogger;
    private readonly Mock<IBackupService> _mockBackupService;
    private readonly BackupController _controller;

    public BackupControllerUnitTests()
    {
        _mockLogger = new Mock<ILogger<BackupController>>();
        _mockBackupService = new Mock<IBackupService>();
        _controller = new BackupController(_mockLogger.Object, _mockBackupService.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var controller = new BackupController(_mockLogger.Object, _mockBackupService.Object);

        // Assert
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => new BackupController(null!, _mockBackupService.Object);
        act.Should().Throw<ArgumentNullException>().WithMessage("*logger*");
    }

    [Fact]
    public void Constructor_WithNullBackupService_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => new BackupController(_mockLogger.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithMessage("*backupService*");
    }

    [Fact]
    public async Task CreateBackupAsync_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateBackupRequest
        {
            BackupId = "backup-001",
            DataType = BackupDataType.KeyStore,
            SourcePath = "/data/keystore",
            Description = "Test backup"
        };

        var expectedBackup = new BackupMetadata
        {
            BackupId = request.BackupId,
            DataType = request.DataType,
            Status = BackupStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };

        _mockBackupService.Setup(x => x.CreateBackupAsync(request, It.IsAny<BlockchainType>()))
            .ReturnsAsync(expectedBackup);

        // Act
        var result = await _controller.CreateBackupAsync(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(expectedBackup);

        _mockBackupService.Verify(x => x.CreateBackupAsync(request, It.IsAny<BlockchainType>()), Times.Once);
    }

    [Fact]
    public async Task CreateBackupAsync_WithNullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.CreateBackupAsync(null!);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        
        var errorResponse = badRequestResult!.Value as dynamic;
        errorResponse!.error.Should().Be("Request cannot be null");
    }

    [Fact]
    public async Task CreateBackupAsync_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateBackupRequest
        {
            BackupId = "backup-001",
            DataType = BackupDataType.KeyStore,
            SourcePath = "/data/keystore"
        };

        _mockBackupService.Setup(x => x.CreateBackupAsync(request, It.IsAny<BlockchainType>()))
            .ThrowsAsync(new InvalidOperationException("Backup creation failed"));

        // Act
        var result = await _controller.CreateBackupAsync(request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        
        var errorResponse = objectResult.Value as dynamic;
        errorResponse!.error.Should().Be("Backup creation failed");
    }

    [Fact]
    public async Task GetBackupStatusAsync_WithExistingBackup_ReturnsOkResult()
    {
        // Arrange
        var backupId = "backup-001";
        var backupMetadata = new BackupMetadata
        {
            BackupId = backupId,
            Status = BackupStatus.Completed,
            DataType = BackupDataType.KeyStore,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow,
            Size = 1024
        };

        _mockBackupService.Setup(x => x.GetBackupStatusAsync(backupId, It.IsAny<BlockchainType>()))
            .ReturnsAsync(backupMetadata);

        // Act
        var result = await _controller.GetBackupStatusAsync(backupId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(backupMetadata);

        _mockBackupService.Verify(x => x.GetBackupStatusAsync(backupId, It.IsAny<BlockchainType>()), Times.Once);
    }

    [Fact]
    public async Task GetBackupStatusAsync_WithNonExistingBackup_ReturnsNotFound()
    {
        // Arrange
        var backupId = "non-existing-backup";

        _mockBackupService.Setup(x => x.GetBackupStatusAsync(backupId, It.IsAny<BlockchainType>()))
            .ReturnsAsync((BackupMetadata?)null);

        // Act
        var result = await _controller.GetBackupStatusAsync(backupId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        
        var errorResponse = notFoundResult!.Value as dynamic;
        errorResponse!.error.Should().Be($"Backup '{backupId}' not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public async Task GetBackupStatusAsync_WithEmptyBackupId_ReturnsBadRequest(string backupId)
    {
        // Act
        var result = await _controller.GetBackupStatusAsync(backupId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        
        var errorResponse = badRequestResult!.Value as dynamic;
        errorResponse!.error.Should().Be("BackupId cannot be empty");
    }

    [Fact]
    public async Task RestoreBackupAsync_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new RestoreBackupRequest
        {
            BackupId = "backup-001",
            TargetPath = "/restore/keystore",
            OverwriteExisting = true
        };

        var restoreResult = new RestoreBackupResult
        {
            BackupId = request.BackupId,
            Status = RestoreStatus.Completed,
            RestoredPath = request.TargetPath,
            RestoreStartedAt = DateTime.UtcNow.AddMinutes(-5),
            RestoreCompletedAt = DateTime.UtcNow
        };

        _mockBackupService.Setup(x => x.RestoreBackupAsync(request, It.IsAny<BlockchainType>()))
            .ReturnsAsync(restoreResult);

        // Act
        var result = await _controller.RestoreBackupAsync(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(restoreResult);

        _mockBackupService.Verify(x => x.RestoreBackupAsync(request, It.IsAny<BlockchainType>()), Times.Once);
    }

    [Fact]
    public async Task DeleteBackupAsync_WithExistingBackup_ReturnsOkResult()
    {
        // Arrange
        var backupId = "backup-to-delete";

        _mockBackupService.Setup(x => x.DeleteBackupAsync(backupId, It.IsAny<BlockchainType>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteBackupAsync(backupId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var response = okResult!.Value as dynamic;
        response!.message.Should().Be($"Backup '{backupId}' deleted successfully");

        _mockBackupService.Verify(x => x.DeleteBackupAsync(backupId, It.IsAny<BlockchainType>()), Times.Once);
    }

    [Fact]
    public async Task DeleteBackupAsync_WithNonExistingBackup_ReturnsNotFound()
    {
        // Arrange
        var backupId = "non-existing-backup";

        _mockBackupService.Setup(x => x.DeleteBackupAsync(backupId, It.IsAny<BlockchainType>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteBackupAsync(backupId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        
        var errorResponse = notFoundResult!.Value as dynamic;
        errorResponse!.error.Should().Be($"Backup '{backupId}' not found");
    }

    [Fact]
    public async Task ListBackupsAsync_WithValidFilter_ReturnsBackupList()
    {
        // Arrange
        var filter = new BackupFilter
        {
            DataType = BackupDataType.KeyStore,
            Status = BackupStatus.Completed
        };

        var backups = new List<BackupMetadata>
        {
            new BackupMetadata
            {
                BackupId = "backup-001",
                DataType = BackupDataType.KeyStore,
                Status = BackupStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new BackupMetadata
            {
                BackupId = "backup-002",
                DataType = BackupDataType.KeyStore,
                Status = BackupStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        _mockBackupService.Setup(x => x.ListBackupsAsync(filter, It.IsAny<BlockchainType>()))
            .ReturnsAsync(backups);

        // Act
        var result = await _controller.ListBackupsAsync(filter);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(backups);

        _mockBackupService.Verify(x => x.ListBackupsAsync(filter, It.IsAny<BlockchainType>()), Times.Once);
    }

    [Fact]
    public async Task ListBackupsAsync_WithNullFilter_UsesDefaultFilter()
    {
        // Arrange
        var backups = new List<BackupMetadata>
        {
            new BackupMetadata
            {
                BackupId = "backup-001",
                DataType = BackupDataType.KeyStore,
                Status = BackupStatus.Completed,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockBackupService.Setup(x => x.ListBackupsAsync(It.IsAny<BackupFilter>(), It.IsAny<BlockchainType>()))
            .ReturnsAsync(backups);

        // Act
        var result = await _controller.ListBackupsAsync(null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(backups);

        _mockBackupService.Verify(x => x.ListBackupsAsync(It.IsAny<BackupFilter>(), It.IsAny<BlockchainType>()), Times.Once);
    }

    [Fact]
    public async Task ValidateBackupAsync_WithValidBackup_ReturnsValidationResult()
    {
        // Arrange
        var backupId = "backup-001";
        var validationResult = new BackupValidationResult
        {
            BackupId = backupId,
            IsValid = true,
            ValidationResults = new Dictionary<string, object>
            {
                ["IntegrityCheck"] = "Passed",
                ["ChecksumValidation"] = "Passed",
                ["SizeVerification"] = "Passed"
            }
        };

        _mockBackupService.Setup(x => x.ValidateBackupAsync(backupId, It.IsAny<BlockchainType>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ValidateBackupAsync(backupId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(validationResult);

        _mockBackupService.Verify(x => x.ValidateBackupAsync(backupId, It.IsAny<BlockchainType>()), Times.Once);
    }
}