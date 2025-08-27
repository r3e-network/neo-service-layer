using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neo.SecretsManagement.Service.Controllers;
using Neo.SecretsManagement.Service.Models;
using Neo.SecretsManagement.Service.Services;
using NUnit.Framework;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Neo.SecretsManagement.Service.Tests.Controllers;

[TestFixture]
public class SecretsControllerTests
{
    private Mock<ISecretService> _mockSecretService = null!;
    private Mock<IAuditService> _mockAuditService = null!;
    private Mock<ILogger<SecretsController>> _mockLogger = null!;
    private SecretsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockSecretService = new Mock<ISecretService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<SecretsController>>();

        _controller = new SecretsController(
            _mockSecretService.Object,
            _mockAuditService.Object,
            _mockLogger.Object);

        // Setup HTTP context and user claims
        SetupHttpContext("test-user-id");

        // Setup default audit service behavior
        _mockAuditService.Setup(x => x.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    [Test]
    public async Task CreateSecret_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateSecretRequest
        {
            Name = "Test Secret",
            Path = "/app/test-secret",
            Value = "secret-value",
            Type = SecretType.Generic,
            Description = "Test description"
        };

        var secretResponse = new SecretResponse
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Path = request.Path,
            Type = request.Type,
            Description = request.Description,
            CurrentVersion = 1,
            Status = SecretStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user-id"
        };

        _mockSecretService.Setup(x => x.CreateSecretAsync(request, "test-user-id"))
            .ReturnsAsync(secretResponse);

        // Act
        var result = await _controller.CreateSecret(request);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        
        var createdResult = result.Result as CreatedAtActionResult;
        Assert.That(createdResult!.Value, Is.EqualTo(secretResponse));
        Assert.That(createdResult.ActionName, Is.EqualTo(nameof(SecretsController.GetSecret)));

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogAsync(
            "test-user-id", "create", "secret", secretResponse.Id.ToString(),
            request.Path, true, null, It.IsAny<Dictionary<string, object>>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task CreateSecret_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateSecretRequest
        {
            Name = "Test Secret",
            Path = "/app/test-secret",
            Value = "secret-value",
            Type = SecretType.Generic
        };

        _mockSecretService.Setup(x => x.CreateSecretAsync(It.IsAny<CreateSecretRequest>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act
        var result = await _controller.CreateSecret(request);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        
        var objectResult = result.Result as ObjectResult;
        Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task GetSecret_ExistingSecret_ReturnsOk()
    {
        // Arrange
        const string secretPath = "/app/test-secret";
        var secretResponse = new SecretResponse
        {
            Id = Guid.NewGuid(),
            Name = "Test Secret",
            Path = secretPath,
            Type = SecretType.Generic,
            Value = "decrypted-value",
            CurrentVersion = 1,
            Status = SecretStatus.Active
        };

        _mockSecretService.Setup(x => x.GetSecretAsync(secretPath, "test-user-id", true))
            .ReturnsAsync(secretResponse);

        // Act
        var result = await _controller.GetSecret(secretPath, includeValue: true);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult!.Value, Is.EqualTo(secretResponse));

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogAsync(
            "test-user-id", "read", "secret", secretResponse.Id.ToString(),
            secretPath, true, null, It.IsAny<Dictionary<string, object>>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task GetSecret_NonExistentSecret_ReturnsNotFound()
    {
        // Arrange
        const string secretPath = "/app/nonexistent";

        _mockSecretService.Setup(x => x.GetSecretAsync(secretPath, "test-user-id", false))
            .ReturnsAsync((SecretResponse?)null);

        // Act
        var result = await _controller.GetSecret(secretPath);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task GetSecret_UnauthorizedAccess_ReturnsForbid()
    {
        // Arrange
        const string secretPath = "/app/test-secret";

        _mockSecretService.Setup(x => x.GetSecretAsync(secretPath, "test-user-id", false))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _controller.GetSecret(secretPath);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task ListSecrets_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new ListSecretsRequest
        {
            PathPrefix = "/app",
            Type = SecretType.Generic,
            Skip = 0,
            Take = 10
        };

        var secrets = new List<SecretResponse>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Secret 1",
                Path = "/app/secret1",
                Type = SecretType.Generic
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Secret 2",
                Path = "/app/secret2",
                Type = SecretType.Generic
            }
        };

        _mockSecretService.Setup(x => x.ListSecretsAsync(request, "test-user-id"))
            .ReturnsAsync(secrets);

        // Act
        var result = await _controller.ListSecrets(request);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        
        var okResult = result.Result as OkObjectResult;
        var returnedSecrets = okResult!.Value as List<SecretResponse>;
        Assert.That(returnedSecrets, Has.Count.EqualTo(2));

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogAsync(
            "test-user-id", "list", "secrets", "multiple", "/app", true, null,
            It.IsAny<Dictionary<string, object>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task UpdateSecret_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        const string secretPath = "/app/test-secret";
        var request = new UpdateSecretRequest
        {
            Value = "new-value",
            Description = "Updated description"
        };

        _mockSecretService.Setup(x => x.UpdateSecretAsync(secretPath, request, "test-user-id"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateSecret(secretPath, request);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogAsync(
            "test-user-id", "update", "secret", "path-based", secretPath, true, null,
            It.IsAny<Dictionary<string, object>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task UpdateSecret_NonExistentSecret_ReturnsNotFound()
    {
        // Arrange
        const string secretPath = "/app/nonexistent";
        var request = new UpdateSecretRequest { Value = "new-value" };

        _mockSecretService.Setup(x => x.UpdateSecretAsync(secretPath, request, "test-user-id"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateSecret(secretPath, request);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task DeleteSecret_ExistingSecret_ReturnsNoContent()
    {
        // Arrange
        const string secretPath = "/app/test-secret";

        _mockSecretService.Setup(x => x.DeleteSecretAsync(secretPath, "test-user-id"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteSecret(secretPath);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogAsync(
            "test-user-id", "delete", "secret", "path-based", secretPath, true, null,
            It.IsAny<Dictionary<string, object>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ShareSecret_ValidRequest_ReturnsOk()
    {
        // Arrange
        const string secretPath = "/app/shared-secret";
        var request = new ShareSecretRequest
        {
            SharedWithUserId = "recipient-user",
            Permissions = SecretPermissions.Read,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        var shareResponse = new ShareSecretResponse
        {
            Id = Guid.NewGuid(),
            SecretId = Guid.NewGuid(),
            SharedWithUserId = request.SharedWithUserId,
            Permissions = request.Permissions,
            ExpiresAt = request.ExpiresAt
        };

        _mockSecretService.Setup(x => x.ShareSecretAsync(secretPath, request, "test-user-id"))
            .ReturnsAsync(shareResponse);

        // Act
        var result = await _controller.ShareSecret(secretPath, request);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult!.Value, Is.EqualTo(shareResponse));

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogAsync(
            "test-user-id", "share", "secret", shareResponse.SecretId.ToString(),
            secretPath, true, null, It.IsAny<Dictionary<string, object>>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task RevokeShare_ExistingShare_ReturnsNoContent()
    {
        // Arrange
        var shareId = Guid.NewGuid();

        _mockSecretService.Setup(x => x.RevokeShareAsync(shareId, "test-user-id"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RevokeShare(shareId);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogAsync(
            "test-user-id", "revoke_share", "secret_share", shareId.ToString(),
            null, true, null, It.IsAny<Dictionary<string, object>>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task RotateSecret_ValidRequest_ReturnsOk()
    {
        // Arrange
        const string secretPath = "/app/rotate-secret";
        var request = new RotateSecretRequest { NewValue = "new-rotated-value" };

        var rotatedSecret = new SecretResponse
        {
            Id = Guid.NewGuid(),
            Name = "Rotated Secret",
            Path = secretPath,
            CurrentVersion = 2,
            Status = SecretStatus.Active
        };

        _mockSecretService.Setup(x => x.RotateSecretAsync(secretPath, request, "test-user-id"))
            .ReturnsAsync(rotatedSecret);

        // Act
        var result = await _controller.RotateSecret(secretPath, request);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult!.Value, Is.EqualTo(rotatedSecret));

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogAsync(
            "test-user-id", "rotate", "secret", rotatedSecret.Id.ToString(),
            secretPath, true, null, It.IsAny<Dictionary<string, object>>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task GetSecretVersions_ValidPath_ReturnsOk()
    {
        // Arrange
        const string secretPath = "/app/versioned-secret";
        var versions = new List<SecretVersion>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SecretId = Guid.NewGuid(),
                Version = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                SecretId = Guid.NewGuid(),
                Version = 2,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockSecretService.Setup(x => x.GetSecretVersionsAsync(secretPath, "test-user-id"))
            .ReturnsAsync(versions);

        // Act
        var result = await _controller.GetSecretVersions(secretPath);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        
        var okResult = result.Result as OkObjectResult;
        var returnedVersions = okResult!.Value as List<SecretVersion>;
        Assert.That(returnedVersions, Has.Count.EqualTo(2));

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogAsync(
            "test-user-id", "list_versions", "secret", "path-based", secretPath,
            true, null, It.IsAny<Dictionary<string, object>>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task GetStatistics_ReturnsOk()
    {
        // Arrange
        var statistics = new SecretStatistics
        {
            TotalSecrets = 100,
            ActiveSecrets = 90,
            ExpiredSecrets = 5,
            ExpiringSecrets = 10,
            SecretsByType = new Dictionary<string, int>
            {
                { "Generic", 50 },
                { "Password", 30 },
                { "ApiKey", 20 }
            }
        };

        _mockSecretService.Setup(x => x.GetSecretStatisticsAsync("test-user-id"))
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetStatistics();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult!.Value, Is.EqualTo(statistics));
    }

    [Test]
    public async Task ValidateAccess_ValidRequest_ReturnsOk()
    {
        // Arrange
        const string secretPath = "/app/access-test";
        var request = new ValidateAccessRequest { Operation = SecretOperation.Read };

        _mockSecretService.Setup(x => x.ValidateAccessAsync(secretPath, "test-user-id", SecretOperation.Read))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ValidateAccess(secretPath, request);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value;
        
        // Use reflection to check the anonymous object
        var hasAccessProperty = response?.GetType().GetProperty("hasAccess");
        Assert.That(hasAccessProperty, Is.Not.Null);
        Assert.That(hasAccessProperty!.GetValue(response), Is.EqualTo(true));
    }

    private void SetupHttpContext(string userId)
    {
        var httpContext = new DefaultHttpContext();
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, "SecretWriter")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        httpContext.User = principal;
        httpContext.Request.Headers.Add("User-Agent", "TestAgent/1.0");
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }
}