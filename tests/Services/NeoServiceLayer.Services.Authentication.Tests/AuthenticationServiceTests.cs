using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Infrastructure.Security;
using NeoServiceLayer.Services.Authentication;
using NeoServiceLayer.Services.Authentication.Models;
using NeoServiceLayer.Services.Authentication.Repositories;
using Xunit;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Tests
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<ILogger<AuthenticationService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ISecurityLogger> _securityLoggerMock;
        private readonly AuthenticationService _authService;

        public AuthenticationServiceTests()
        {
            _loggerMock = new Mock<ILogger<AuthenticationService>>();
            _configurationMock = new Mock<IConfiguration>();
            _cacheMock = new Mock<IDistributedCache>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _securityLoggerMock = new Mock<ISecurityLogger>();
            
            // Setup configuration
            _configurationMock.Setup(x => x["Authentication:JwtSecret"])
                .Returns("ThisIsAVerySecureSecretKeyForTestingPurposesOnly123456789");
            _configurationMock.Setup(x => x["Authentication:Issuer"])
                .Returns("TestIssuer");
            _configurationMock.Setup(x => x["Authentication:Audience"])
                .Returns("TestAudience");
            _configurationMock.Setup(x => x.GetSection("Authentication:AccessTokenExpiryMinutes").Value)
                .Returns("15");
            _configurationMock.Setup(x => x.GetSection("Authentication:RefreshTokenExpiryDays").Value)
                .Returns("30");
            _configurationMock.Setup(x => x.GetSection("Authentication:MaxFailedAttempts").Value)
                .Returns("5");
            _configurationMock.Setup(x => x.GetSection("Authentication:LockoutDurationMinutes").Value)
                .Returns("30");
            
            _authService = new AuthenticationService(
                _loggerMock.Object,
                _configurationMock.Object,
                _cacheMock.Object,
                _userRepositoryMock.Object,
                _securityLoggerMock.Object);
        }

        [Fact]
        public async Task AuthenticateAsync_ValidCredentials_ReturnsSuccess()
        {
            // Arrange
            var username = "testuser";
            var password = "ValidPassword123!";
            var user = CreateTestUser(username, password, false);
            
            _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync(user);
            
            // Act
            var result = await _authService.AuthenticateAsync(username, password);
            
            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);
            Assert.Equal(user.Id, result.UserId);
            Assert.Equal(user.Roles, result.Roles);
        }

        [Fact]
        public async Task AuthenticateAsync_InvalidPassword_ReturnsFail()
        {
            // Arrange
            var username = "testuser";
            var correctPassword = "ValidPassword123!";
            var wrongPassword = "WrongPassword123!";
            var user = CreateTestUser(username, correctPassword, false);
            
            _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync(user);
            
            // Act
            var result = await _authService.AuthenticateAsync(username, wrongPassword);
            
            // Assert
            Assert.False(result.Success);
            Assert.Equal(AuthenticationErrorCode.InvalidCredentials, result.ErrorCode);
            _securityLoggerMock.Verify(x => x.LogSecurityEventAsync("InvalidPassword", username, It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_UserNotFound_ReturnsFail()
        {
            // Arrange
            var username = "nonexistent";
            var password = "Password123!";
            
            _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync((User)null);
            
            // Act
            var result = await _authService.AuthenticateAsync(username, password);
            
            // Assert
            Assert.False(result.Success);
            Assert.Equal(AuthenticationErrorCode.InvalidCredentials, result.ErrorCode);
        }

        [Fact]
        public async Task AuthenticateAsync_AccountLocked_ReturnsAccountLocked()
        {
            // Arrange
            var username = "lockeduser";
            var password = "ValidPassword123!";
            var user = CreateTestUser(username, password, false);
            user.IsLocked = true;
            
            _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync(user);
            
            // Act
            var result = await _authService.AuthenticateAsync(username, password);
            
            // Assert
            Assert.False(result.Success);
            Assert.Equal(AuthenticationErrorCode.AccountLocked, result.ErrorCode);
            _securityLoggerMock.Verify(x => x.LogSecurityEventAsync("LockedAccountAccess", username, It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_MfaEnabled_RequiresMfa()
        {
            // Arrange
            var username = "mfauser";
            var password = "ValidPassword123!";
            var user = CreateTestUser(username, password, true);
            
            _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync(user);
            
            // Act
            var result = await _authService.AuthenticateAsync(username, password);
            
            // Assert
            Assert.False(result.Success);
            Assert.True(result.RequiresMfa);
            Assert.NotNull(result.MfaToken);
            Assert.Equal(AuthenticationErrorCode.MfaRequired, result.ErrorCode);
        }

        [Fact]
        public async Task GenerateTokenPairAsync_ValidInput_ReturnsValidTokens()
        {
            // Arrange
            var userId = "user123";
            var roles = new[] { "User", "Admin" };
            
            // Act
            var tokenPair = await _authService.GenerateTokenPairAsync(userId, roles);
            
            // Assert
            Assert.NotNull(tokenPair);
            Assert.NotNull(tokenPair.AccessToken);
            Assert.NotNull(tokenPair.RefreshToken);
            Assert.True(tokenPair.AccessTokenExpiry > DateTime.UtcNow);
            Assert.True(tokenPair.RefreshTokenExpiry > tokenPair.AccessTokenExpiry);
        }

        [Fact]
        public async Task ValidatePasswordStrengthAsync_StrongPassword_ReturnsTrue()
        {
            // Arrange
            var strongPassword = "MyStr0ng!Password123";
            
            // Act
            var result = await _authService.ValidatePasswordStrengthAsync(strongPassword);
            
            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidatePasswordStrengthAsync_WeakPassword_ReturnsFalse()
        {
            // Arrange
            var weakPasswords = new[]
            {
                "short",          // Too short
                "nouppercase123!", // No uppercase
                "NOLOWERCASE123!", // No lowercase
                "NoNumbers!",      // No digits
                "NoSpecialChar123" // No special characters
            };
            
            // Act & Assert
            foreach (var password in weakPasswords)
            {
                var result = await _authService.ValidatePasswordStrengthAsync(password);
                Assert.False(result, $"Password '{password}' should be considered weak");
            }
        }

        [Fact]
        public async Task SetupMfaAsync_TotpType_ReturnsValidSetup()
        {
            // Arrange
            var userId = "user123";
            var user = new User { Id = userId, Username = "testuser" };
            
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.UpdateMfaSettingsAsync(userId, It.IsAny<MfaSettings>()))
                .Returns(Task.CompletedTask);
            
            // Act
            var result = await _authService.SetupMfaAsync(userId, MfaType.Totp);
            
            // Assert
            Assert.True(result.Success);
            Assert.Equal(MfaType.Totp, result.Type);
            Assert.NotNull(result.Secret);
            Assert.NotNull(result.QrCodeUrl);
            Assert.NotNull(result.BackupCodes);
            Assert.True(result.BackupCodes.Length > 0);
        }

        [Fact]
        public async Task ChangePasswordAsync_ValidCurrentPassword_ReturnsTrue()
        {
            // Arrange
            var userId = "user123";
            var currentPassword = "CurrentPassword123!";
            var newPassword = "NewPassword456!";
            var user = CreateTestUser("testuser", currentPassword, false);
            user.Id = userId;
            
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.UpdatePasswordAsync(userId, It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            // Act
            var result = await _authService.ChangePasswordAsync(userId, currentPassword, newPassword);
            
            // Assert
            Assert.True(result);
            _userRepositoryMock.Verify(x => x.UpdatePasswordAsync(userId, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _securityLoggerMock.Verify(x => x.LogSecurityEventAsync("PasswordChanged", user.Username, It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_InvalidCurrentPassword_ReturnsFalse()
        {
            // Arrange
            var userId = "user123";
            var correctPassword = "CurrentPassword123!";
            var wrongPassword = "WrongPassword123!";
            var newPassword = "NewPassword456!";
            var user = CreateTestUser("testuser", correctPassword, false);
            user.Id = userId;
            
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            
            // Act
            var result = await _authService.ChangePasswordAsync(userId, wrongPassword, newPassword);
            
            // Assert
            Assert.False(result);
            _userRepositoryMock.Verify(x => x.UpdatePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        // Helper method to create test user
        private User CreateTestUser(string username, string password, bool mfaEnabled)
        {
            var (hash, salt) = HashPassword(password);
            return new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                Email = $"{username}@test.com",
                PasswordHash = hash,
                PasswordSalt = salt,
                Roles = new[] { "User" },
                IsLocked = false,
                MfaEnabled = mfaEnabled,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            };
        }

        private (string hash, string salt) HashPassword(string password)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256();
            var salt = Convert.ToBase64String(hmac.Key);
            var hash = Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
            return (hash, salt);
        }
    }
}