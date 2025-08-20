using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Services.Authentication;
using NeoServiceLayer.Shared.Configuration;
using Xunit;

namespace NeoServiceLayer.Services.Authentication.Tests;

public class AuthenticationServiceUnitTests : IDisposable
{
    private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
    private readonly Mock<IPersistentStorageProvider> _mockStorageProvider;
    private readonly Mock<IServiceConfiguration> _mockConfig;
    private readonly Mock<IHealthCheckService> _mockHealthCheck;
    private readonly Mock<ITelemetryCollector> _mockTelemetry;
    private readonly Mock<IHttpClientService> _mockHttpClient;
    private readonly Mock<ISecretsManager> _mockSecretsManager;
    private readonly AuthenticationService _authService;

    public AuthenticationServiceUnitTests()
    {
        _mockLogger = new Mock<ILogger<AuthenticationService>>();
        _mockStorageProvider = new Mock<IPersistentStorageProvider>();
        _mockConfig = new Mock<IServiceConfiguration>();
        _mockHealthCheck = new Mock<IHealthCheckService>();
        _mockTelemetry = new Mock<ITelemetryCollector>();
        _mockHttpClient = new Mock<IHttpClientService>();
        _mockSecretsManager = new Mock<ISecretsManager>();

        _mockConfig.Setup(x => x.GetSetting("Authentication:TokenExpiration", "3600"))
               .Returns("3600");
        _mockConfig.Setup(x => x.GetSetting("Authentication:MaxLoginAttempts", "5"))
               .Returns("5");
        _mockSecretsManager.Setup(x => x.GetSecretAsync("JWT_SECRET_KEY"))
               .ReturnsAsync("test-secret-key-for-jwt-signing");

        _authService = new AuthenticationService(
            _mockLogger.Object,
            _mockStorageProvider.Object,
            _mockConfig.Object,
            _mockHealthCheck.Object,
            _mockTelemetry.Object,
            _mockHttpClient.Object,
            _mockSecretsManager.Object);
    }

    [Fact]
    public async Task InitializeAsync_InitializesSuccessfully()
    {
        var result = await _authService.InitializeAsync();

        result.Should().BeTrue();
        _authService.Name.Should().Be("AuthenticationService");
        _authService.ServiceType.Should().Be("AuthenticationService");
    }

    [Fact]
    public async Task RegisterUserAsync_WithValidData_RegistersUser()
    {
        // Arrange
        await _authService.InitializeAsync();
        await _authService.StartAsync();

        var username = "testuser";
        var email = "test@example.com";
        var password = "SecurePassword123!";

        _mockStorageProvider.Setup(x => x.ExistsAsync($"users/{username}"))
            .ReturnsAsync(false); // User doesn't exist
        _mockStorageProvider.Setup(x => x.StoreAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.RegisterUserAsync(username, email, password);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be(username);
        result.Email.Should().Be(email);
        result.IsActive.Should().BeTrue();

        _mockStorageProvider.Verify(x => x.StoreAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()), 
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RegisterUserAsync_WithExistingUser_ThrowsInvalidOperationException()
    {
        // Arrange
        await _authService.InitializeAsync();
        await _authService.StartAsync();

        var username = "existinguser";
        var email = "test@example.com";
        var password = "SecurePassword123!";

        _mockStorageProvider.Setup(x => x.ExistsAsync($"users/{username}"))
            .ReturnsAsync(true); // User already exists

        // Act & Assert
        Func<Task> act = async () => await _authService.RegisterUserAsync(username, email, password);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        await _authService.InitializeAsync();
        await _authService.StartAsync();

        var username = "testuser";
        var password = "SecurePassword123!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var userData = new Dictionary<string, object>
        {
            ["Username"] = username,
            ["Email"] = "test@example.com",
            ["PasswordHash"] = hashedPassword,
            ["IsActive"] = true,
            ["LoginAttempts"] = 0
        };

        _mockStorageProvider.Setup(x => x.ExistsAsync($"users/{username}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"users/{username}"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(userData));

        // Act
        var result = await _authService.AuthenticateAsync(username, password);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.Username.Should().Be(username);
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidCredentials_ReturnsNull()
    {
        // Arrange
        await _authService.InitializeAsync();
        await _authService.StartAsync();

        var username = "testuser";
        var password = "WrongPassword";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("CorrectPassword");

        var userData = new Dictionary<string, object>
        {
            ["Username"] = username,
            ["Email"] = "test@example.com",
            ["PasswordHash"] = hashedPassword,
            ["IsActive"] = true,
            ["LoginAttempts"] = 0
        };

        _mockStorageProvider.Setup(x => x.ExistsAsync($"users/{username}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"users/{username}"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(userData));

        // Act
        var result = await _authService.AuthenticateAsync(username, password);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        await _authService.InitializeAsync();
        await _authService.StartAsync();

        // First authenticate to get a valid token
        var username = "testuser";
        var password = "SecurePassword123!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var userData = new Dictionary<string, object>
        {
            ["Username"] = username,
            ["Email"] = "test@example.com",
            ["PasswordHash"] = hashedPassword,
            ["IsActive"] = true,
            ["LoginAttempts"] = 0
        };

        _mockStorageProvider.Setup(x => x.ExistsAsync($"users/{username}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"users/{username}"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(userData));

        var authResult = await _authService.AuthenticateAsync(username, password);

        // Act
        var isValid = await _authService.ValidateTokenAsync(authResult.Token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        await _authService.InitializeAsync();
        await _authService.StartAsync();

        var invalidToken = "invalid.token.here";

        // Act
        var isValid = await _authService.ValidateTokenAsync(invalidToken);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewToken()
    {
        // Arrange
        await _authService.InitializeAsync();
        await _authService.StartAsync();

        // First authenticate to get a valid token
        var username = "testuser";
        var password = "SecurePassword123!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var userData = new Dictionary<string, object>
        {
            ["Username"] = username,
            ["Email"] = "test@example.com",
            ["PasswordHash"] = hashedPassword,
            ["IsActive"] = true,
            ["LoginAttempts"] = 0
        };

        _mockStorageProvider.Setup(x => x.ExistsAsync($"users/{username}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"users/{username}"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(userData));

        var originalAuthResult = await _authService.AuthenticateAsync(username, password);

        // Act
        var refreshResult = await _authService.RefreshTokenAsync(originalAuthResult.Token);

        // Assert
        refreshResult.Should().NotBeNull();
        refreshResult.Token.Should().NotBe(originalAuthResult.Token);
        refreshResult.Username.Should().Be(username);
        refreshResult.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task LogoutAsync_WithValidToken_InvalidatesToken()
    {
        // Arrange
        await _authService.InitializeAsync();
        await _authService.StartAsync();

        var token = "valid.jwt.token";

        _mockStorageProvider.Setup(x => x.StoreAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.LogoutAsync(token);

        // Assert
        result.Should().BeTrue();

        // Should store token in blacklist
        _mockStorageProvider.Verify(x => x.StoreAsync(
            It.Is<string>(s => s.Contains("blacklist")), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()), 
            Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidCredentials_ChangesPassword()
    {
        // Arrange
        await _authService.InitializeAsync();
        await _authService.StartAsync();

        var username = "testuser";
        var currentPassword = "CurrentPassword123!";
        var newPassword = "NewPassword456!";
        var hashedCurrentPassword = BCrypt.Net.BCrypt.HashPassword(currentPassword);

        var userData = new Dictionary<string, object>
        {
            ["Username"] = username,
            ["Email"] = "test@example.com",
            ["PasswordHash"] = hashedCurrentPassword,
            ["IsActive"] = true,
            ["LoginAttempts"] = 0
        };

        _mockStorageProvider.Setup(x => x.ExistsAsync($"users/{username}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"users/{username}"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(userData));
        _mockStorageProvider.Setup(x => x.StoreAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.ChangePasswordAsync(username, currentPassword, newPassword);

        // Assert
        result.Should().BeTrue();

        _mockStorageProvider.Verify(x => x.StoreAsync(
            $"users/{username}", 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()), 
            Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidUser_SendsResetToken()
    {
        // Arrange
        await _authService.InitializeAsync();
        await _authService.StartAsync();

        var username = "testuser";
        var email = "test@example.com";

        var userData = new Dictionary<string, object>
        {
            ["Username"] = username,
            ["Email"] = email,
            ["IsActive"] = true
        };

        _mockStorageProvider.Setup(x => x.ExistsAsync($"users/{username}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"users/{username}"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(userData));
        _mockStorageProvider.Setup(x => x.StoreAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.ResetPasswordAsync(username);

        // Assert
        result.Should().BeTrue();

        // Should store reset token
        _mockStorageProvider.Verify(x => x.StoreAsync(
            It.Is<string>(s => s.Contains("reset-tokens")), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetUserInfoAsync_WithExistingUser_ReturnsUserInfo()
    {
        // Arrange
        await _authService.InitializeAsync();
        await _authService.StartAsync();

        var username = "testuser";
        var userData = new Dictionary<string, object>
        {
            ["Username"] = username,
            ["Email"] = "test@example.com",
            ["IsActive"] = true,
            ["CreatedAt"] = DateTime.UtcNow.ToString(),
            ["LastLoginAt"] = DateTime.UtcNow.ToString()
        };

        _mockStorageProvider.Setup(x => x.ExistsAsync($"users/{username}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"users/{username}"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(userData));

        // Act
        var result = await _authService.GetUserInfoAsync(username);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("Username");
        result.Should().ContainKey("Email");
        result.Should().ContainKey("IsActive");
        result["Username"].Should().Be(username);
        result["Email"].Should().Be("test@example.com");
        result["IsActive"].Should().Be(true);
    }

    [Fact]
    public void Dispose_DisposesResourcesProperly()
    {
        _authService.Dispose();
        _authService.Status.Should().Be("Disposed");
    }

    public void Dispose()
    {
        _authService?.Dispose();
    }
}