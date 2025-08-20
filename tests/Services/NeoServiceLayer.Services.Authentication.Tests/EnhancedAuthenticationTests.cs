using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Services.Authentication.Infrastructure;
using NeoServiceLayer.Services.Authentication.Models;
using NeoServiceLayer.Services.Authentication.Repositories;
using NeoServiceLayer.Services.Authentication.Services;
using Xunit;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Tests
{
    public class EnhancedAuthenticationTests
    {
        private readonly Mock<ILogger<ComprehensiveAuthenticationService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IEnhancedJwtTokenService> _tokenServiceMock;
        private readonly Mock<ITwoFactorService> _twoFactorServiceMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly IMemoryCache _memoryCache;
        private readonly ComprehensiveAuthenticationService _authService;

        public EnhancedAuthenticationTests()
        {
            _loggerMock = new Mock<ILogger<ComprehensiveAuthenticationService>>();
            _configurationMock = new Mock<IConfiguration>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _tokenServiceMock = new Mock<IEnhancedJwtTokenService>();
            _twoFactorServiceMock = new Mock<ITwoFactorService>();
            _auditServiceMock = new Mock<IAuditService>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            // Setup configuration
            _configurationMock.Setup(x => x.GetSection("Authentication:MaxFailedAttempts").Value).Returns("5");
            _configurationMock.Setup(x => x.GetSection("Authentication:LockoutDurationMinutes").Value).Returns("30");
            _configurationMock.Setup(x => x.GetSection("Authentication:RateLimitPerMinute").Value).Returns("10");
            _configurationMock.Setup(x => x.GetSection("Authentication:Require2FA").Value).Returns("false");

            var userStore = new Mock<IUserReadModelStore>();
            
            _authService = new ComprehensiveAuthenticationService(
                _loggerMock.Object,
                _configurationMock.Object,
                userStore.Object,
                _passwordHasherMock.Object,
                _tokenServiceMock.Object,
                _twoFactorServiceMock.Object,
                _memoryCache,
                _auditServiceMock.Object);
        }

        #region Authentication Tests

        [Fact]
        public async Task AuthenticateAsync_ValidCredentials_ReturnsSuccess()
        {
            // Arrange
            var username = "testuser";
            var password = "Test123!@#";
            var ipAddress = "192.168.1.1";
            var userAgent = "TestAgent";
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = "test@example.com",
                PasswordHash = "hashedPassword",
                EmailVerified = true,
                TwoFactorEnabled = false,
                Roles = new List<string> { "User" }
            };

            _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync(user);
            
            _passwordHasherMock.Setup(x => x.VerifyPassword(password, user.PasswordHash))
                .Returns(true);
            
            _tokenServiceMock.Setup(x => x.GenerateTokensAsync(
                    It.IsAny<Guid>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new AuthenticationTokens
                {
                    AccessToken = "access_token",
                    RefreshToken = "refresh_token",
                    AccessTokenExpiration = DateTime.UtcNow.AddMinutes(15),
                    RefreshTokenExpiration = DateTime.UtcNow.AddDays(7),
                    TokenType = "Bearer"
                });

            // Act
            var result = await _authService.AuthenticateAsync(username, password, ipAddress, userAgent);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.UserId.Should().Be(user.Id);
            result.Username.Should().Be(username);
            result.Tokens.Should().NotBeNull();
            result.Tokens.AccessToken.Should().NotBeNullOrEmpty();
            
            _auditServiceMock.Verify(x => x.LogAuthenticationAttemptAsync(
                username, ipAddress, true, "Authentication successful"), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_InvalidPassword_ReturnsFailure()
        {
            // Arrange
            var username = "testuser";
            var password = "WrongPassword";
            var ipAddress = "192.168.1.1";
            var userAgent = "TestAgent";
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = "hashedPassword",
                EmailVerified = true
            };

            _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync(user);
            
            _passwordHasherMock.Setup(x => x.VerifyPassword(password, user.PasswordHash))
                .Returns(false);

            // Act
            var result = await _authService.AuthenticateAsync(username, password, ipAddress, userAgent);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be(AuthenticationErrorCode.InvalidCredentials);
            result.Error.Should().Contain("Invalid credentials");
            
            _auditServiceMock.Verify(x => x.LogAuthenticationAttemptAsync(
                username, ipAddress, false, "Invalid password"), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_UserNotFound_ReturnsFailure()
        {
            // Arrange
            var username = "nonexistent";
            var password = "Test123!@#";
            var ipAddress = "192.168.1.1";
            var userAgent = "TestAgent";

            _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync((User)null);
            _userRepositoryMock.Setup(x => x.GetByEmailAsync(username))
                .ReturnsAsync((User)null);

            // Act
            var result = await _authService.AuthenticateAsync(username, password, ipAddress, userAgent);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be(AuthenticationErrorCode.InvalidCredentials);
            result.Error.Should().Be("Invalid credentials");
        }

        [Fact]
        public async Task AuthenticateAsync_EmailNotVerified_ReturnsFailure()
        {
            // Arrange
            var username = "testuser";
            var password = "Test123!@#";
            var ipAddress = "192.168.1.1";
            var userAgent = "TestAgent";
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = "hashedPassword",
                EmailVerified = false
            };

            _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync(user);
            
            _passwordHasherMock.Setup(x => x.VerifyPassword(password, user.PasswordHash))
                .Returns(true);

            // Act
            var result = await _authService.AuthenticateAsync(username, password, ipAddress, userAgent);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be(AuthenticationErrorCode.EmailNotVerified);
            result.Error.Should().Contain("Email address not verified");
        }

        [Fact]
        public async Task AuthenticateAsync_TwoFactorRequired_ReturnsChallenge()
        {
            // Arrange
            var username = "testuser";
            var password = "Test123!@#";
            var ipAddress = "192.168.1.1";
            var userAgent = "TestAgent";
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = "hashedPassword",
                EmailVerified = true,
                TwoFactorEnabled = true,
                TwoFactorSecret = "secret"
            };

            _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync(user);
            
            _passwordHasherMock.Setup(x => x.VerifyPassword(password, user.PasswordHash))
                .Returns(true);

            // Act
            var result = await _authService.AuthenticateAsync(username, password, ipAddress, userAgent, null);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.RequiresTwoFactor.Should().BeTrue();
            result.ErrorCode.Should().Be(AuthenticationErrorCode.TwoFactorRequired);
        }

        #endregion

        #region Token Service Tests

        [Fact]
        public async Task GenerateTokensAsync_ValidUser_ReturnsTokens()
        {
            // Arrange
            var tokenService = new Mock<IEnhancedJwtTokenService>();
            var userId = Guid.NewGuid();
            var username = "testuser";
            var email = "test@example.com";
            var roles = new List<string> { "User", "Admin" };

            tokenService.Setup(x => x.GenerateTokensAsync(
                    userId, username, email, roles, null))
                .ReturnsAsync(new AuthenticationTokens
                {
                    AccessToken = "access_token",
                    RefreshToken = "refresh_token",
                    AccessTokenExpiration = DateTime.UtcNow.AddMinutes(15),
                    RefreshTokenExpiration = DateTime.UtcNow.AddDays(7),
                    TokenType = "Bearer"
                });

            // Act
            var tokens = await tokenService.Object.GenerateTokensAsync(userId, username, email, roles);

            // Assert
            tokens.Should().NotBeNull();
            tokens.AccessToken.Should().NotBeNullOrEmpty();
            tokens.RefreshToken.Should().NotBeNullOrEmpty();
            tokens.TokenType.Should().Be("Bearer");
            tokens.AccessTokenExpiration.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task ValidateAccessTokenAsync_ValidToken_ReturnsSuccess()
        {
            // Arrange
            var tokenService = new Mock<IEnhancedJwtTokenService>();
            var token = "valid_token";
            var userId = Guid.NewGuid();

            tokenService.Setup(x => x.ValidateAccessTokenAsync(token))
                .ReturnsAsync(new TokenValidationResult
                {
                    IsValid = true,
                    UserId = userId,
                    Username = "testuser",
                    Email = "test@example.com",
                    Roles = new List<string> { "User" }
                });

            // Act
            var result = await tokenService.Object.ValidateAccessTokenAsync(token);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.UserId.Should().Be(userId);
            result.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task ValidateAccessTokenAsync_ExpiredToken_ReturnsFailure()
        {
            // Arrange
            var tokenService = new Mock<IEnhancedJwtTokenService>();
            var token = "expired_token";

            tokenService.Setup(x => x.ValidateAccessTokenAsync(token))
                .ReturnsAsync(new TokenValidationResult
                {
                    IsValid = false,
                    Error = "Token has expired"
                });

            // Act
            var result = await tokenService.Object.ValidateAccessTokenAsync(token);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Error.Should().Be("Token has expired");
        }

        #endregion

        #region Password Hasher Tests

        [Fact]
        public void HashPassword_ValidPassword_ReturnsHash()
        {
            // Arrange
            var hasher = new PasswordHasher();
            var password = "Test123!@#";

            // Act
            var hash = hasher.HashPassword(password);

            // Assert
            hash.Should().NotBeNullOrEmpty();
            hash.Should().NotBe(password);
        }

        [Fact]
        public void VerifyPassword_CorrectPassword_ReturnsTrue()
        {
            // Arrange
            var hasher = new PasswordHasher();
            var password = "Test123!@#";
            var hash = hasher.HashPassword(password);

            // Act
            var result = hasher.VerifyPassword(password, hash);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void VerifyPassword_IncorrectPassword_ReturnsFalse()
        {
            // Arrange
            var hasher = new PasswordHasher();
            var password = "Test123!@#";
            var wrongPassword = "Wrong123!@#";
            var hash = hasher.HashPassword(password);

            // Act
            var result = hasher.VerifyPassword(wrongPassword, hash);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Two-Factor Authentication Tests

        [Fact]
        public void GenerateSecret_ReturnsValidSecret()
        {
            // Arrange
            var twoFactorService = new Mock<ITwoFactorService>();
            var expectedSecret = "JBSWY3DPEHPK3PXP";

            twoFactorService.Setup(x => x.GenerateSecret())
                .Returns(expectedSecret);

            // Act
            var secret = twoFactorService.Object.GenerateSecret();

            // Assert
            secret.Should().NotBeNullOrEmpty();
            secret.Should().Be(expectedSecret);
        }

        [Fact]
        public void ValidateTotp_ValidCode_ReturnsTrue()
        {
            // Arrange
            var twoFactorService = new Mock<ITwoFactorService>();
            var secret = "JBSWY3DPEHPK3PXP";
            var code = "123456";

            twoFactorService.Setup(x => x.ValidateTotp(secret, code))
                .Returns(true);

            // Act
            var result = twoFactorService.Object.ValidateTotp(secret, code);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GenerateBackupCodes_ReturnsValidCodes()
        {
            // Arrange
            var twoFactorService = new Mock<ITwoFactorService>();
            var expectedCodes = new List<string>
            {
                "1234-5678",
                "2345-6789",
                "3456-7890",
                "4567-8901"
            };

            twoFactorService.Setup(x => x.GenerateBackupCodes(4))
                .Returns(expectedCodes);

            // Act
            var codes = twoFactorService.Object.GenerateBackupCodes(4);

            // Assert
            codes.Should().NotBeNull();
            codes.Should().HaveCount(4);
            codes.Should().AllSatisfy(code => code.Should().Match("????-????"));
        }

        #endregion

        #region Rate Limiting Tests

        [Fact]
        public async Task AuthenticateAsync_ExceedsRateLimit_ReturnsRateLimitError()
        {
            // Arrange
            var ipAddress = "192.168.1.1";
            var userAgent = "TestAgent";
            
            // Fill up the rate limit cache
            var cacheKey = $"rate_limit_{ipAddress}";
            _memoryCache.Set(cacheKey, 11, TimeSpan.FromMinutes(1)); // Exceed limit of 10

            // Act
            var result = await _authService.AuthenticateAsync("user", "pass", ipAddress, userAgent);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be(AuthenticationErrorCode.RateLimitExceeded);
            result.Error.Should().Contain("Too many attempts");
        }

        #endregion

        #region Account Lockout Tests

        [Fact]
        public async Task AuthenticateAsync_AccountLocked_ReturnsLockedError()
        {
            // Arrange
            var username = "testuser";
            var password = "Test123!@#";
            var ipAddress = "192.168.1.1";
            var userAgent = "TestAgent";
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = "hashedPassword",
                EmailVerified = true
            };

            _userRepositoryMock.Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync(user);
            
            // Lock the account
            var lockoutKey = $"lockout_{user.Id}";
            _memoryCache.Set(lockoutKey, true, TimeSpan.FromMinutes(30));

            // Act
            var result = await _authService.AuthenticateAsync(username, password, ipAddress, userAgent);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorCode.Should().Be(AuthenticationErrorCode.AccountLocked);
            result.Error.Should().Contain("Account is locked");
        }

        #endregion
    }
}