using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NeoServiceLayer.Infrastructure.Observability.Logging;
using NeoServiceLayer.Services.Authentication;
using Xunit;

namespace NeoServiceLayer.Services.Authentication.Tests
{
    public class TokenServiceTests
    {
        private readonly Mock<ILogger<TokenService>> _loggerMock;
        private readonly Mock<IStructuredLoggerFactory> _structuredLoggerFactoryMock;
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly IConfiguration _configuration;
        private readonly TokenService _tokenService;

        public TokenServiceTests()
        {
            _loggerMock = new Mock<ILogger<TokenService>>();
            _structuredLoggerFactoryMock = new Mock<IStructuredLoggerFactory>();
            _cacheMock = new Mock<IDistributedCache>();

            var configData = new Dictionary<string, string>
            {
                ["Authentication:JwtSecret"] = "ThisIsAVerySecretKeyForTestingPurposesOnly12345",
                ["Authentication:Issuer"] = "TestIssuer",
                ["Authentication:Audience"] = "TestAudience",
                ["Authentication:AccessTokenExpiryMinutes"] = "15",
                ["Authentication:RefreshTokenExpiryDays"] = "30"
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            var structuredLoggerMock = new Mock<IStructuredLogger>();
            _structuredLoggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
                .Returns(structuredLoggerMock.Object);

            _tokenService = new TokenService(
                _loggerMock.Object,
                _structuredLoggerFactoryMock.Object,
                _configuration,
                _cacheMock.Object);
        }

        [Fact]
        public async Task GenerateTokenPairAsync_ShouldReturnValidTokenPair()
        {
            // Arrange
            var userId = "test-user-123";
            var roles = new[] { "user", "admin" };
            var additionalClaims = new Dictionary<string, string> { ["custom"] = "claim" };

            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default))
                .Returns(Task.CompletedTask);

            // Act
            var tokenPair = await _tokenService.GenerateTokenPairAsync(userId, roles, additionalClaims);

            // Assert
            Assert.NotNull(tokenPair);
            Assert.NotEmpty(tokenPair.AccessToken);
            Assert.NotEmpty(tokenPair.RefreshToken);
            Assert.True(tokenPair.AccessTokenExpiry > DateTime.UtcNow);
            Assert.True(tokenPair.RefreshTokenExpiry > DateTime.UtcNow);
            Assert.True(tokenPair.RefreshTokenExpiry > tokenPair.AccessTokenExpiry);

            // Verify token contains expected claims
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.ReadJwtToken(tokenPair.AccessToken);
            
            Assert.Equal(userId, jwt.Subject);
            Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "user");
            Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "admin");
            Assert.Contains(jwt.Claims, c => c.Type == "custom" && c.Value == "claim");
        }

        [Fact]
        public async Task ValidateTokenAsync_WithValidToken_ShouldReturnTrue()
        {
            // Arrange
            var userId = "test-user";
            var roles = new[] { "user" };
            
            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default))
                .Returns(Task.CompletedTask);

            _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                .ReturnsAsync((byte[])null);

            var tokenPair = await _tokenService.GenerateTokenPairAsync(userId, roles);

            // Act
            var isValid = await _tokenService.ValidateTokenAsync(tokenPair.AccessToken);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithInvalidToken_ShouldReturnFalse()
        {
            // Arrange
            var invalidToken = "invalid.token.here";

            _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                .ReturnsAsync((byte[])null);

            // Act
            var isValid = await _tokenService.ValidateTokenAsync(invalidToken);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithBlacklistedToken_ShouldReturnFalse()
        {
            // Arrange
            var userId = "test-user";
            var roles = new[] { "user" };
            
            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default))
                .Returns(Task.CompletedTask);

            var tokenPair = await _tokenService.GenerateTokenPairAsync(userId, roles);

            // Setup blacklist check to return token is blacklisted
            _cacheMock.Setup(c => c.GetAsync($"blacklist:{tokenPair.AccessToken}", default))
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("revoked"));

            // Act
            var isValid = await _tokenService.ValidateTokenAsync(tokenPair.AccessToken);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithValidRefreshToken_ShouldReturnNewTokenPair()
        {
            // Arrange
            var userId = "test-user";
            var roles = new[] { "user" };
            
            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default))
                .Returns(Task.CompletedTask);

            var originalTokenPair = await _tokenService.GenerateTokenPairAsync(userId, roles);

            var refreshTokenData = new
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Used = false
            };

            _cacheMock.Setup(c => c.GetAsync($"refresh_token:{originalTokenPair.RefreshToken}", default))
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(refreshTokenData)));

            // Act
            var newTokenPair = await _tokenService.RefreshTokenAsync(originalTokenPair.RefreshToken);

            // Assert
            Assert.NotNull(newTokenPair);
            Assert.NotEmpty(newTokenPair.AccessToken);
            Assert.NotEmpty(newTokenPair.RefreshToken);
            Assert.NotEqual(originalTokenPair.AccessToken, newTokenPair.AccessToken);
            Assert.NotEqual(originalTokenPair.RefreshToken, newTokenPair.RefreshToken);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithInvalidRefreshToken_ShouldThrowException()
        {
            // Arrange
            var invalidRefreshToken = "invalid-refresh-token";

            _cacheMock.Setup(c => c.GetAsync($"refresh_token:{invalidRefreshToken}", default))
                .ReturnsAsync((byte[])null);

            // Act & Assert
            await Assert.ThrowsAsync<SecurityTokenException>(async () =>
                await _tokenService.RefreshTokenAsync(invalidRefreshToken));
        }

        [Fact]
        public async Task RefreshTokenAsync_WithExpiredRefreshToken_ShouldThrowException()
        {
            // Arrange
            var expiredRefreshToken = "expired-refresh-token";
            
            var refreshTokenData = new
            {
                UserId = "test-user",
                CreatedAt = DateTime.UtcNow.AddDays(-31),
                ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
                Used = false
            };

            _cacheMock.Setup(c => c.GetAsync($"refresh_token:{expiredRefreshToken}", default))
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(refreshTokenData)));

            _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), default))
                .Returns(Task.CompletedTask);

            // Act & Assert
            await Assert.ThrowsAsync<SecurityTokenException>(async () =>
                await _tokenService.RefreshTokenAsync(expiredRefreshToken));
        }

        [Fact]
        public async Task RefreshTokenAsync_WithUsedRefreshToken_ShouldThrowException()
        {
            // Arrange
            var usedRefreshToken = "used-refresh-token";
            
            var refreshTokenData = new
            {
                UserId = "test-user",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Used = true, // Already used
                UsedAt = DateTime.UtcNow.AddMinutes(-5)
            };

            _cacheMock.Setup(c => c.GetAsync($"refresh_token:{usedRefreshToken}", default))
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(refreshTokenData)));

            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default))
                .Returns(Task.CompletedTask);

            // Act & Assert
            await Assert.ThrowsAsync<SecurityTokenException>(async () =>
                await _tokenService.RefreshTokenAsync(usedRefreshToken));
        }

        [Fact]
        public async Task RevokeTokenAsync_ShouldAddTokenToBlacklist()
        {
            // Arrange
            var userId = "test-user";
            var roles = new[] { "user" };
            
            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default))
                .Returns(Task.CompletedTask);

            var tokenPair = await _tokenService.GenerateTokenPairAsync(userId, roles);

            // Act
            await _tokenService.RevokeTokenAsync(tokenPair.AccessToken);

            // Assert
            _cacheMock.Verify(c => c.SetAsync(
                $"blacklist:{tokenPair.AccessToken}",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default), Times.Once);
        }

        [Fact]
        public async Task IsTokenBlacklistedAsync_WithBlacklistedToken_ShouldReturnTrue()
        {
            // Arrange
            var token = "blacklisted-token";
            
            _cacheMock.Setup(c => c.GetAsync($"blacklist:{token}", default))
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("revoked"));

            // Act
            var isBlacklisted = await _tokenService.IsTokenBlacklistedAsync(token);

            // Assert
            Assert.True(isBlacklisted);
        }

        [Fact]
        public async Task IsTokenBlacklistedAsync_WithNonBlacklistedToken_ShouldReturnFalse()
        {
            // Arrange
            var token = "valid-token";
            
            _cacheMock.Setup(c => c.GetAsync($"blacklist:{token}", default))
                .ReturnsAsync((byte[])null);

            // Act
            var isBlacklisted = await _tokenService.IsTokenBlacklistedAsync(token);

            // Assert
            Assert.False(isBlacklisted);
        }

        [Fact]
        public void GetUserIdFromToken_WithValidToken_ShouldReturnUserId()
        {
            // Arrange
            var userId = "test-user-123";
            var roles = new[] { "user" };
            
            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default))
                .Returns(Task.CompletedTask);

            var tokenPair = _tokenService.GenerateTokenPairAsync(userId, roles).Result;

            // Act
            var extractedUserId = _tokenService.GetUserIdFromToken(tokenPair.AccessToken);

            // Assert
            Assert.Equal(userId, extractedUserId);
        }

        [Fact]
        public void GetUserIdFromToken_WithInvalidToken_ShouldReturnNull()
        {
            // Arrange
            var invalidToken = "invalid.token.here";

            // Act
            var userId = _tokenService.GetUserIdFromToken(invalidToken);

            // Assert
            Assert.Null(userId);
        }

        [Fact]
        public void ValidateAndGetPrincipal_WithValidToken_ShouldReturnPrincipal()
        {
            // Arrange
            var userId = "test-user";
            var roles = new[] { "user", "admin" };
            
            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default))
                .Returns(Task.CompletedTask);

            var tokenPair = _tokenService.GenerateTokenPairAsync(userId, roles).Result;

            // Act
            var principal = _tokenService.ValidateAndGetPrincipal(tokenPair.AccessToken);

            // Assert
            Assert.NotNull(principal);
            Assert.Equal(userId, principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.Role && c.Value == "user");
            Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.Role && c.Value == "admin");
        }

        [Fact]
        public void ValidateAndGetPrincipal_WithInvalidToken_ShouldReturnNull()
        {
            // Arrange
            var invalidToken = "invalid.token.here";

            // Act
            var principal = _tokenService.ValidateAndGetPrincipal(invalidToken);

            // Assert
            Assert.Null(principal);
        }
    }
}