using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo.SecretsManagement.Service.Data;
using Neo.SecretsManagement.Service.Models;
using Neo.SecretsManagement.Service.Services;
using NUnit.Framework;
using Moq;

namespace Neo.SecretsManagement.Service.Tests.Services;

[TestFixture]
public class SecretServiceTests
{
    private DbContextOptions<SecretsDbContext> _dbOptions = null!;
    private Mock<IEncryptionService> _mockEncryptionService = null!;
    private Mock<IKeyManagementService> _mockKeyManagementService = null!;
    private Mock<IAuditService> _mockAuditService = null!;
    private Mock<ISecretPolicyService> _mockPolicyService = null!;
    private Mock<ILogger<SecretService>> _mockLogger = null!;
    private SecretServiceOptions _options = null!;

    [SetUp]
    public void Setup()
    {
        _dbOptions = new DbContextOptionsBuilder<SecretsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockKeyManagementService = new Mock<IKeyManagementService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockPolicyService = new Mock<ISecretPolicyService>();
        _mockLogger = new Mock<ILogger<SecretService>>();

        _options = new SecretServiceOptions
        {
            DefaultEncryptionKeyId = "test-key-id",
            MaxSecretsPerUser = 1000,
            MaxSecretSize = 64 * 1024,
            DefaultExpirationDays = 365,
            EnableVersioning = true,
            MaxVersionsPerSecret = 10
        };

        // Setup default mock behaviors
        _mockEncryptionService.Setup(x => x.EncryptAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("encrypted-value");
        
        _mockEncryptionService.Setup(x => x.DecryptAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("decrypted-value");

        _mockAuditService.Setup(x => x.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockPolicyService.Setup(x => x.EvaluatePolicyAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecretOperation>(), It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(true);
    }

    [Test]
    public async Task CreateSecretAsync_ValidRequest_ReturnsSecretResponse()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateSecretService(context);

        var request = new CreateSecretRequest
        {
            Name = "Test Secret",
            Path = "/app/test-secret",
            Value = "secret-value",
            Type = SecretType.Generic,
            Description = "Test description"
        };

        // Act
        var result = await service.CreateSecretAsync(request, "test-user");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo(request.Name));
        Assert.That(result.Path, Is.EqualTo(request.Path));
        Assert.That(result.Type, Is.EqualTo(request.Type));
        Assert.That(result.CurrentVersion, Is.EqualTo(1));
        Assert.That(result.Status, Is.EqualTo(SecretStatus.Active));

        // Verify secret was saved to database
        var savedSecret = await context.Secrets.FirstOrDefaultAsync(s => s.Path == request.Path);
        Assert.That(savedSecret, Is.Not.Null);
        Assert.That(savedSecret.CreatedBy, Is.EqualTo("test-user"));

        // Verify encryption was called
        _mockEncryptionService.Verify(x => x.EncryptAsync(request.Value, _options.DefaultEncryptionKeyId), Times.Once);
        
        // Verify audit logging
        _mockAuditService.Verify(x => x.LogAsync(
            "test-user", "create", "secret", It.IsAny<string>(), request.Path, true, null,
            It.IsAny<Dictionary<string, object>>(), null, null), Times.Once);
    }

    [Test]
    public async Task CreateSecretAsync_DuplicatePath_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateSecretService(context);

        // Create first secret
        var secret = new Secret
        {
            Name = "Existing Secret",
            Path = "/app/existing",
            EncryptedValue = "encrypted",
            KeyId = "key-id",
            Type = SecretType.Generic,
            Status = SecretStatus.Active,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "test-user",
            CurrentVersion = 1
        };
        context.Secrets.Add(secret);
        await context.SaveChangesAsync();

        var request = new CreateSecretRequest
        {
            Name = "Duplicate Secret",
            Path = "/app/existing", // Same path
            Value = "secret-value",
            Type = SecretType.Generic
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateSecretAsync(request, "test-user"));
        
        Assert.That(ex.Message, Contains.Substring("already exists"));
    }

    [Test]
    public async Task GetSecretAsync_ExistingSecret_ReturnsSecret()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateSecretService(context);

        var secret = new Secret
        {
            Id = Guid.NewGuid(),
            Name = "Test Secret",
            Path = "/app/test",
            EncryptedValue = "encrypted-value",
            KeyId = "key-id",
            Type = SecretType.Generic,
            Status = SecretStatus.Active,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "test-user",
            CurrentVersion = 1,
            Tags = new Dictionary<string, string> { { "env", "test" } }
        };
        context.Secrets.Add(secret);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetSecretAsync("/app/test", "test-user", includeValue: true);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo(secret.Name));
        Assert.That(result.Path, Is.EqualTo(secret.Path));
        Assert.That(result.Value, Is.EqualTo("decrypted-value"));

        // Verify decryption was called
        _mockEncryptionService.Verify(x => x.DecryptAsync("encrypted-value", "key-id"), Times.Once);
        
        // Verify policy evaluation
        _mockPolicyService.Verify(x => x.EvaluatePolicyAsync(
            "/app/test", "test-user", SecretOperation.Read, It.IsAny<Dictionary<string, object>>()), Times.Once);
    }

    [Test]
    public async Task GetSecretAsync_NonExistentSecret_ReturnsNull()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateSecretService(context);

        // Act
        var result = await service.GetSecretAsync("/app/nonexistent", "test-user");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetSecretAsync_WithoutIncludeValue_DoesNotDecrypt()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateSecretService(context);

        var secret = new Secret
        {
            Id = Guid.NewGuid(),
            Name = "Test Secret",
            Path = "/app/test",
            EncryptedValue = "encrypted-value",
            KeyId = "key-id",
            Type = SecretType.Generic,
            Status = SecretStatus.Active,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "test-user",
            CurrentVersion = 1
        };
        context.Secrets.Add(secret);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetSecretAsync("/app/test", "test-user", includeValue: false);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.Null);

        // Verify decryption was NOT called
        _mockEncryptionService.Verify(x => x.DecryptAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task UpdateSecretAsync_ValidRequest_UpdatesSecret()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateSecretService(context);

        var secret = new Secret
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            Path = "/app/test",
            EncryptedValue = "original-encrypted",
            KeyId = "key-id",
            Type = SecretType.Generic,
            Status = SecretStatus.Active,
            Description = "Original description",
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "test-user",
            CurrentVersion = 1
        };
        context.Secrets.Add(secret);
        await context.SaveChangesAsync();

        var updateRequest = new UpdateSecretRequest
        {
            Value = "new-secret-value",
            Description = "Updated description",
            Tags = new Dictionary<string, string> { { "updated", "true" } }
        };

        // Act
        var result = await service.UpdateSecretAsync("/app/test", updateRequest, "test-user");

        // Assert
        Assert.That(result, Is.True);

        var updatedSecret = await context.Secrets.FirstAsync(s => s.Path == "/app/test");
        Assert.That(updatedSecret.Description, Is.EqualTo("Updated description"));
        Assert.That(updatedSecret.Tags, Contains.Key("updated"));
        Assert.That(updatedSecret.UpdatedBy, Is.EqualTo("test-user"));
        Assert.That(updatedSecret.CurrentVersion, Is.EqualTo(2));

        // Verify new version was created
        var versions = await context.SecretVersions.Where(v => v.SecretId == secret.Id).ToListAsync();
        Assert.That(versions, Has.Count.EqualTo(1));
        Assert.That(versions[0].Version, Is.EqualTo(1));

        // Verify encryption was called with new value
        _mockEncryptionService.Verify(x => x.EncryptAsync("new-secret-value", "key-id"), Times.Once);
    }

    [Test]
    public async Task DeleteSecretAsync_ExistingSecret_MarksAsDeleted()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateSecretService(context);

        var secret = new Secret
        {
            Id = Guid.NewGuid(),
            Name = "Test Secret",
            Path = "/app/test",
            EncryptedValue = "encrypted-value",
            KeyId = "key-id",
            Type = SecretType.Generic,
            Status = SecretStatus.Active,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "test-user",
            CurrentVersion = 1
        };
        context.Secrets.Add(secret);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteSecretAsync("/app/test", "test-user");

        // Assert
        Assert.That(result, Is.True);

        var deletedSecret = await context.Secrets.FirstAsync(s => s.Path == "/app/test");
        Assert.That(deletedSecret.Status, Is.EqualTo(SecretStatus.Deleted));
        Assert.That(deletedSecret.UpdatedBy, Is.EqualTo("test-user"));
    }

    [Test]
    public async Task ListSecretsAsync_WithFilters_ReturnsFilteredSecrets()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateSecretService(context);

        var secrets = new[]
        {
            new Secret
            {
                Id = Guid.NewGuid(),
                Name = "App Secret",
                Path = "/app/secret1",
                Type = SecretType.Generic,
                Status = SecretStatus.Active,
                CreatedBy = "test-user",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "test-user",
                CurrentVersion = 1
            },
            new Secret
            {
                Id = Guid.NewGuid(),
                Name = "DB Password",
                Path = "/app/db/password",
                Type = SecretType.Password,
                Status = SecretStatus.Active,
                CreatedBy = "test-user",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "test-user",
                CurrentVersion = 1
            },
            new Secret
            {
                Id = Guid.NewGuid(),
                Name = "API Key",
                Path = "/services/api-key",
                Type = SecretType.ApiKey,
                Status = SecretStatus.Active,
                CreatedBy = "other-user",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "other-user",
                CurrentVersion = 1
            }
        };

        context.Secrets.AddRange(secrets);
        await context.SaveChangesAsync();

        var request = new ListSecretsRequest
        {
            PathPrefix = "/app",
            Type = SecretType.Password,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await service.ListSecretsAsync(request, "test-user");

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Path, Is.EqualTo("/app/db/password"));
        Assert.That(result[0].Type, Is.EqualTo(SecretType.Password));
    }

    [Test]
    public async Task ShareSecretAsync_ValidRequest_CreatesSecretShare()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateSecretService(context);

        var secret = new Secret
        {
            Id = Guid.NewGuid(),
            Name = "Shared Secret",
            Path = "/app/shared",
            EncryptedValue = "encrypted-value",
            KeyId = "key-id",
            Type = SecretType.Generic,
            Status = SecretStatus.Active,
            CreatedBy = "owner-user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "owner-user",
            CurrentVersion = 1
        };
        context.Secrets.Add(secret);
        await context.SaveChangesAsync();

        var shareRequest = new ShareSecretRequest
        {
            SharedWithUserId = "recipient-user",
            Permissions = SecretPermissions.Read,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        // Act
        var result = await service.ShareSecretAsync("/app/shared", shareRequest, "owner-user");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SecretId, Is.EqualTo(secret.Id));
        Assert.That(result.SharedWithUserId, Is.EqualTo("recipient-user"));
        Assert.That(result.Permissions, Is.EqualTo(SecretPermissions.Read));

        // Verify share was saved to database
        var share = await context.SecretShares.FirstAsync(s => s.Id == result.Id);
        Assert.That(share.SharedByUserId, Is.EqualTo("owner-user"));
    }

    [Test]
    public async Task GetSecretStatisticsAsync_ReturnsCorrectStatistics()
    {
        // Arrange
        using var context = new SecretsDbContext(_dbOptions);
        var service = CreateSecretService(context);

        var secrets = new[]
        {
            CreateTestSecret("secret1", "/app/secret1", SecretType.Generic, "test-user"),
            CreateTestSecret("secret2", "/app/secret2", SecretType.Password, "test-user"),
            CreateTestSecret("secret3", "/app/secret3", SecretType.ApiKey, "other-user", DateTime.UtcNow.AddDays(-10)),
            CreateTestSecret("secret4", "/app/secret4", SecretType.Generic, "test-user", DateTime.UtcNow.AddDays(5)) // Expiring
        };

        context.Secrets.AddRange(secrets);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetSecretStatisticsAsync("test-user");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.TotalSecrets, Is.EqualTo(3)); // Only user's secrets
        Assert.That(result.ActiveSecrets, Is.EqualTo(3));
        Assert.That(result.ExpiredSecrets, Is.EqualTo(0));
        Assert.That(result.ExpiringSecrets, Is.EqualTo(1));
        Assert.That(result.SecretsByType, Contains.Key(SecretType.Generic.ToString()));
        Assert.That(result.SecretsByType[SecretType.Generic.ToString()], Is.EqualTo(2));
    }

    private SecretService CreateSecretService(SecretsDbContext context)
    {
        return new SecretService(
            context,
            _mockEncryptionService.Object,
            _mockKeyManagementService.Object,
            _mockAuditService.Object,
            _mockPolicyService.Object,
            _mockLogger.Object,
            Options.Create(_options)
        );
    }

    private static Secret CreateTestSecret(string name, string path, SecretType type, string createdBy, DateTime? expiresAt = null)
    {
        return new Secret
        {
            Id = Guid.NewGuid(),
            Name = name,
            Path = path,
            EncryptedValue = "encrypted-value",
            KeyId = "key-id",
            Type = type,
            Status = SecretStatus.Active,
            ExpiresAt = expiresAt,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = createdBy,
            CurrentVersion = 1
        };
    }
}

public class SecretServiceOptions
{
    public string DefaultEncryptionKeyId { get; set; } = string.Empty;
    public int MaxSecretsPerUser { get; set; }
    public int MaxSecretSize { get; set; }
    public int DefaultExpirationDays { get; set; }
    public bool EnableVersioning { get; set; }
    public int MaxVersionsPerSecret { get; set; }
}