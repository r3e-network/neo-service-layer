using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neo.SecretsManagement.Service.Data;
using Neo.SecretsManagement.Service.Models;
using Neo.SecretsManagement.Service.Services;
using NUnit.Framework;
using Moq;

namespace Neo.SecretsManagement.Service.Tests.Services;

[TestFixture]
public class AuditServiceTests
{
    private DbContextOptions<SecretsDbContext> _dbOptions = null!;
    private Mock<ILogger<AuditService>> _mockLogger = null!;

    [SetUp]
    public void Setup()
    {
        _dbOptions = new DbContextOptionsBuilder<SecretsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _mockLogger = new Mock<ILogger<AuditService>>();
    }

    [Test]
    public async Task LogAsync_ValidEntry_CreatesAuditLog()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateAuditService(context);

        // Act
        await service.LogAsync(
            userId: "test-user",
            operation: "create",
            resourceType: "secret",
            resourceId: "secret-123",
            resourcePath: "/app/test-secret",
            success: true,
            errorMessage: null,
            details: new Dictionary<string, object> { { "test_key", "test_value" } },
            clientIp: "192.168.1.100",
            userAgent: "TestAgent/1.0"
        );

        // Assert
        var auditLogs = await context.AuditLogs.ToListAsync();
        Assert.That(auditLogs, Has.Count.EqualTo(1));

        var log = auditLogs[0];
        Assert.That(log.UserId, Is.EqualTo("test-user"));
        Assert.That(log.Operation, Is.EqualTo("create"));
        Assert.That(log.ResourceType, Is.EqualTo("secret"));
        Assert.That(log.ResourceId, Is.EqualTo("secret-123"));
        Assert.That(log.ResourcePath, Is.EqualTo("/app/test-secret"));
        Assert.That(log.Success, Is.True);
        Assert.That(log.ClientIpAddress, Is.EqualTo("192.168.1.100"));
        Assert.That(log.UserAgent, Is.EqualTo("TestAgent/1.0"));
        Assert.That(log.ServiceName, Is.EqualTo("secrets-management"));
        Assert.That(log.Details, Contains.Key("test_key"));
        Assert.That(log.Timestamp, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test]
    public async Task LogAsync_WithNullOptionalParameters_CreatesAuditLogWithDefaults()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateAuditService(context);

        // Act
        await service.LogAsync(
            userId: "test-user",
            operation: "read",
            resourceType: "secret",
            resourceId: "secret-123"
        );

        // Assert
        var auditLog = await context.AuditLogs.FirstAsync();
        Assert.That(auditLog.ResourcePath, Is.Null);
        Assert.That(auditLog.Success, Is.True); // Default
        Assert.That(auditLog.ErrorMessage, Is.Null);
        Assert.That(auditLog.ClientIpAddress, Is.EqualTo("unknown"));
        Assert.That(auditLog.UserAgent, Is.EqualTo("unknown"));
        Assert.That(auditLog.Details, Is.Not.Null);
        Assert.That(auditLog.Details, Is.Empty);
    }

    [Test]
    public async Task LogAsync_WithFailure_LogsErrorDetails()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateAuditService(context);

        // Act
        await service.LogAsync(
            userId: "test-user",
            operation: "delete",
            resourceType: "secret",
            resourceId: "secret-123",
            success: false,
            errorMessage: "Access denied"
        );

        // Assert
        var auditLog = await context.AuditLogs.FirstAsync();
        Assert.That(auditLog.Success, Is.False);
        Assert.That(auditLog.ErrorMessage, Is.EqualTo("Access denied"));
    }

    [Test]
    public async Task LogAsync_WithException_DoesNotThrow()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        context.Dispose(); // Dispose context to cause exception
        
        var service = CreateAuditService(context);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await service.LogAsync(
            userId: "test-user",
            operation: "test",
            resourceType: "test",
            resourceId: "test-123"
        ));

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create audit log entry")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetAuditLogsAsync_WithoutFilters_ReturnsAllLogs()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateAuditService(context);

        // Create test audit logs
        var testLogs = new[]
        {
            CreateTestAuditLog("user1", "create", DateTime.UtcNow.AddHours(-3)),
            CreateTestAuditLog("user2", "read", DateTime.UtcNow.AddHours(-2)),
            CreateTestAuditLog("user1", "update", DateTime.UtcNow.AddHours(-1))
        };

        context.AuditLogs.AddRange(testLogs);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAuditLogsAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        
        // Should be ordered by timestamp descending (most recent first)
        Assert.That(result[0].Operation, Is.EqualTo("update"));
        Assert.That(result[1].Operation, Is.EqualTo("read"));
        Assert.That(result[2].Operation, Is.EqualTo("create"));
    }

    [Test]
    public async Task GetAuditLogsAsync_WithUserIdFilter_ReturnsFilteredLogs()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateAuditService(context);

        var testLogs = new[]
        {
            CreateTestAuditLog("user1", "create"),
            CreateTestAuditLog("user2", "read"),
            CreateTestAuditLog("user1", "update")
        };

        context.AuditLogs.AddRange(testLogs);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAuditLogsAsync(userId: "user1");

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.All(l => l.UserId == "user1"), Is.True);
    }

    [Test]
    public async Task GetAuditLogsAsync_WithOperationFilter_ReturnsFilteredLogs()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateAuditService(context);

        var testLogs = new[]
        {
            CreateTestAuditLog("user1", "create"),
            CreateTestAuditLog("user2", "read"),
            CreateTestAuditLog("user1", "create")
        };

        context.AuditLogs.AddRange(testLogs);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAuditLogsAsync(operation: "create");

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.All(l => l.Operation == "create"), Is.True);
    }

    [Test]
    public async Task GetAuditLogsAsync_WithDateRange_ReturnsFilteredLogs()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateAuditService(context);

        var baseTime = DateTime.UtcNow;
        var testLogs = new[]
        {
            CreateTestAuditLog("user1", "create", baseTime.AddDays(-3)), // Outside range
            CreateTestAuditLog("user2", "read", baseTime.AddDays(-1)),   // Inside range
            CreateTestAuditLog("user1", "update", baseTime.AddHours(-1)) // Inside range
        };

        context.AuditLogs.AddRange(testLogs);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAuditLogsAsync(
            fromDate: baseTime.AddDays(-2),
            toDate: baseTime
        );

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.All(l => l.Timestamp >= baseTime.AddDays(-2)), Is.True);
        Assert.That(result.All(l => l.Timestamp <= baseTime), Is.True);
    }

    [Test]
    public async Task GetAuditLogsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateAuditService(context);

        // Create 15 test logs
        var testLogs = Enumerable.Range(1, 15)
            .Select(i => CreateTestAuditLog($"user{i}", "operation", DateTime.UtcNow.AddMinutes(-i)))
            .ToArray();

        context.AuditLogs.AddRange(testLogs);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAuditLogsAsync(skip: 5, take: 5);

        // Assert
        Assert.That(result, Has.Count.EqualTo(5));
        
        // Verify pagination - should get logs 6-10 (0-indexed, ordered by timestamp desc)
        Assert.That(result[0].UserId, Is.EqualTo("user6"));
        Assert.That(result[4].UserId, Is.EqualTo("user10"));
    }

    [Test]
    public async Task GetAuditLogCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateAuditService(context);

        var testLogs = new[]
        {
            CreateTestAuditLog("user1", "create"),
            CreateTestAuditLog("user2", "read"),
            CreateTestAuditLog("user1", "update")
        };

        context.AuditLogs.AddRange(testLogs);
        await context.SaveChangesAsync();

        // Act
        var totalCount = await service.GetAuditLogCountAsync();
        var user1Count = await service.GetAuditLogCountAsync(userId: "user1");

        // Assert
        Assert.That(totalCount, Is.EqualTo(3));
        Assert.That(user1Count, Is.EqualTo(2));
    }

    [Test]
    public async Task CleanupOldLogsAsync_RemovesOldLogs()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateAuditService(context);

        var testLogs = new[]
        {
            CreateTestAuditLog("user1", "create", DateTime.UtcNow.AddDays(-40)), // Should be deleted
            CreateTestAuditLog("user2", "read", DateTime.UtcNow.AddDays(-20)),   // Should be kept
            CreateTestAuditLog("user1", "update", DateTime.UtcNow.AddDays(-10))  // Should be kept
        };

        context.AuditLogs.AddRange(testLogs);
        await context.SaveChangesAsync();

        // Act
        await service.CleanupOldLogsAsync(retentionDays: 30);

        // Assert
        var remainingLogs = await context.AuditLogs.ToListAsync();
        Assert.That(remainingLogs, Has.Count.EqualTo(2));
        Assert.That(remainingLogs.All(l => l.Timestamp >= DateTime.UtcNow.AddDays(-30)), Is.True);

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cleaned up 1 audit log entries")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetOperationStatisticsAsync_ReturnsCorrectStatistics()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateAuditService(context);

        var baseTime = DateTime.UtcNow;
        var testLogs = new[]
        {
            CreateTestAuditLog("user1", "create", baseTime.AddHours(-1)),
            CreateTestAuditLog("user2", "create", baseTime.AddHours(-1)),
            CreateTestAuditLog("user1", "read", baseTime.AddHours(-1)),
            CreateTestAuditLog("user2", "update", baseTime.AddHours(-1)),
            CreateTestAuditLog("user3", "create", baseTime.AddDays(-2)) // Outside range
        };

        context.AuditLogs.AddRange(testLogs);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetOperationStatisticsAsync(
            fromDate: baseTime.AddDays(-1),
            toDate: baseTime
        );

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result["create"], Is.EqualTo(2));
        Assert.That(result["read"], Is.EqualTo(1));
        Assert.That(result["update"], Is.EqualTo(1));
        Assert.That(result.ContainsKey("delete"), Is.False); // Should not include operations not in range
    }

    [Test]
    public async Task GetUserActivityStatisticsAsync_ReturnsCorrectStatistics()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateAuditService(context);

        var baseTime = DateTime.UtcNow;
        var testLogs = new[]
        {
            CreateTestAuditLog("user1", "create", baseTime.AddHours(-1)),
            CreateTestAuditLog("user1", "read", baseTime.AddHours(-1)),
            CreateTestAuditLog("user2", "create", baseTime.AddHours(-1)),
            CreateTestAuditLog("user3", "update", baseTime.AddDays(-2)) // Outside range
        };

        context.AuditLogs.AddRange(testLogs);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUserActivityStatisticsAsync(
            fromDate: baseTime.AddDays(-1),
            toDate: baseTime
        );

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result["user1"], Is.EqualTo(2));
        Assert.That(result["user2"], Is.EqualTo(1));
        Assert.That(result.ContainsKey("user3"), Is.False); // Should not include users not in range
    }

    [Test]
    public async Task GetUserActivityStatisticsAsync_LimitsToTop50Users()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateAuditService(context);

        // Create logs for 60 users (more than the limit of 50)
        var testLogs = Enumerable.Range(1, 60)
            .Select(i => CreateTestAuditLog($"user{i:D2}", "operation"))
            .ToArray();

        context.AuditLogs.AddRange(testLogs);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUserActivityStatisticsAsync(
            fromDate: DateTime.UtcNow.AddDays(-1),
            toDate: DateTime.UtcNow
        );

        // Assert
        Assert.That(result, Has.Count.EqualTo(50)); // Should be limited to 50
    }

    private AuditService CreateAuditService(SecretsDbContext context)
    {
        return new AuditService(context, _mockLogger.Object);
    }

    private static AuditLogEntry CreateTestAuditLog(
        string userId, 
        string operation, 
        DateTime? timestamp = null)
    {
        return new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = timestamp ?? DateTime.UtcNow,
            UserId = userId,
            ServiceName = "secrets-management",
            Operation = operation,
            ResourceType = "test_resource",
            ResourceId = Guid.NewGuid().ToString(),
            Success = true,
            ClientIpAddress = "127.0.0.1",
            UserAgent = "TestAgent/1.0",
            Details = new Dictionary<string, object>()
        };
    }
}