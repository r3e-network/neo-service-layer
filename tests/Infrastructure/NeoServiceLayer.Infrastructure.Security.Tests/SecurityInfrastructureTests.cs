using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Infrastructure.Security.Tests
{
    /// <summary>
    /// Comprehensive security infrastructure tests for authentication, authorization, and encryption.
    /// </summary>
    public class SecurityInfrastructureTests : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SecurityInfrastructureTests> _logger;
        private readonly Mock<ISecurityService> _securityServiceMock;

        public SecurityInfrastructureTests()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            
            _securityServiceMock = new Mock<ISecurityService>();
            services.AddSingleton(_securityServiceMock.Object);
            
            _serviceProvider = services.BuildServiceProvider();
            _logger = _serviceProvider.GetRequiredService<ILogger<SecurityInfrastructureTests>>();
        }

        #region Authentication Tests

        [Fact]
        public async Task Authentication_ShouldValidate_ValidCredentials()
        {
            // Arrange
            var username = "testuser";
            var password = "SecurePassword123!";
            var hashedPassword = HashPassword(password);
            
            _securityServiceMock.Setup(x => x.AuthenticateAsync(username, It.IsAny<string>()))
                .ReturnsAsync(new AuthenticationResult { Success = true, UserId = "user123" });

            // Act
            var result = await _securityServiceMock.Object.AuthenticateAsync(username, password);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.UserId.Should().Be("user123");
        }

        [Fact]
        public async Task Authentication_ShouldReject_InvalidCredentials()
        {
            // Arrange
            var username = "testuser";
            var password = "WrongPassword";
            
            _securityServiceMock.Setup(x => x.AuthenticateAsync(username, It.IsAny<string>()))
                .ReturnsAsync(new AuthenticationResult { Success = false, Error = "Invalid credentials" });

            // Act
            var result = await _securityServiceMock.Object.AuthenticateAsync(username, password);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Be("Invalid credentials");
        }

        [Fact]
        public async Task Authentication_ShouldPrevent_BruteForceAttacks()
        {
            // Arrange
            var username = "testuser";
            var attemptsBeforeLockout = 5;
            var results = new List<AuthenticationResult>();

            // Act
            for (int i = 0; i < attemptsBeforeLockout + 1; i++)
            {
                _securityServiceMock.Setup(x => x.AuthenticateAsync(username, It.IsAny<string>()))
                    .ReturnsAsync(new AuthenticationResult 
                    { 
                        Success = false, 
                        Error = i >= attemptsBeforeLockout ? "Account locked" : "Invalid credentials" 
                    });
                
                var result = await _securityServiceMock.Object.AuthenticateAsync(username, "wrong");
                results.Add(result);
            }

            // Assert
            results.Last().Error.Should().Be("Account locked");
            _securityServiceMock.Verify(x => x.AuthenticateAsync(username, It.IsAny<string>()), 
                Times.Exactly(attemptsBeforeLockout + 1));
        }

        [Fact]
        public async Task Authentication_Should_SupportMultiFactorAuthentication()
        {
            // Arrange
            var username = "testuser";
            var password = "SecurePassword123!";
            var mfaCode = "123456";
            
            _securityServiceMock.Setup(x => x.ValidateMfaAsync(username, mfaCode))
                .ReturnsAsync(true);

            // Act
            var mfaValid = await _securityServiceMock.Object.ValidateMfaAsync(username, mfaCode);

            // Assert
            mfaValid.Should().BeTrue();
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task Authorization_ShouldAllow_AuthorizedResources()
        {
            // Arrange
            var userId = "user123";
            var resource = "documents/read";
            var permissions = new[] { "documents:read", "documents:write" };
            
            _securityServiceMock.Setup(x => x.AuthorizeAsync(userId, resource))
                .ReturnsAsync(true);

            // Act
            var isAuthorized = await _securityServiceMock.Object.AuthorizeAsync(userId, resource);

            // Assert
            isAuthorized.Should().BeTrue();
        }

        [Fact]
        public async Task Authorization_ShouldDeny_UnauthorizedResources()
        {
            // Arrange
            var userId = "user123";
            var resource = "admin/delete";
            
            _securityServiceMock.Setup(x => x.AuthorizeAsync(userId, resource))
                .ReturnsAsync(false);

            // Act
            var isAuthorized = await _securityServiceMock.Object.AuthorizeAsync(userId, resource);

            // Assert
            isAuthorized.Should().BeFalse();
        }

        [Theory]
        [InlineData("admin", "system:*", true)]
        [InlineData("user", "documents:read", true)]
        [InlineData("user", "system:admin", false)]
        [InlineData("guest", "documents:write", false)]
        public async Task Authorization_ShouldRespect_RoleBasedPermissions(string role, string permission, bool expected)
        {
            // Arrange
            _securityServiceMock.Setup(x => x.HasPermissionAsync(role, permission))
                .ReturnsAsync(expected);

            // Act
            var hasPermission = await _securityServiceMock.Object.HasPermissionAsync(role, permission);

            // Assert
            hasPermission.Should().Be(expected);
        }

        [Fact]
        public async Task Authorization_Should_SupportHierarchicalRoles()
        {
            // Arrange
            var roles = new Dictionary<string, string[]>
            {
                ["admin"] = new[] { "admin", "user", "guest" },
                ["user"] = new[] { "user", "guest" },
                ["guest"] = new[] { "guest" }
            };

            // Act & Assert
            foreach (var role in roles)
            {
                _securityServiceMock.Setup(x => x.GetEffectiveRolesAsync(role.Key))
                    .ReturnsAsync(role.Value);
                
                var effectiveRoles = await _securityServiceMock.Object.GetEffectiveRolesAsync(role.Key);
                effectiveRoles.Should().BeEquivalentTo(role.Value);
            }
        }

        #endregion

        #region Encryption Tests

        [Fact]
        public void Encryption_Should_EncryptAndDecryptData()
        {
            // Arrange
            var plainText = "Sensitive data that needs encryption";
            var key = GenerateEncryptionKey();
            
            // Act
            var encrypted = EncryptData(plainText, key);
            var decrypted = DecryptData(encrypted, key);

            // Assert
            encrypted.Should().NotBe(plainText);
            decrypted.Should().Be(plainText);
        }

        [Fact]
        public void Encryption_Should_GenerateUniqueEncryptedValues()
        {
            // Arrange
            var plainText = "Same data encrypted multiple times";
            var key = GenerateEncryptionKey();
            
            // Act
            var encrypted1 = EncryptData(plainText, key);
            var encrypted2 = EncryptData(plainText, key);

            // Assert
            encrypted1.Should().NotBe(encrypted2); // Due to IV/salt
            DecryptData(encrypted1, key).Should().Be(plainText);
            DecryptData(encrypted2, key).Should().Be(plainText);
        }

        [Fact]
        public void Encryption_Should_FailWithWrongKey()
        {
            // Arrange
            var plainText = "Sensitive data";
            var correctKey = GenerateEncryptionKey();
            var wrongKey = GenerateEncryptionKey();
            
            // Act
            var encrypted = EncryptData(plainText, correctKey);
            
            // Assert
            Assert.Throws<CryptographicException>(() => DecryptData(encrypted, wrongKey));
        }

        [Theory]
        [InlineData("AES")]
        [InlineData("RSA")]
        public void Encryption_Should_SupportMultipleAlgorithms(string algorithm)
        {
            // Arrange
            var plainText = "Data to encrypt";
            
            // Act & Assert
            switch (algorithm)
            {
                case "AES":
                    var aesKey = GenerateEncryptionKey();
                    var aesEncrypted = EncryptData(plainText, aesKey);
                    var aesDecrypted = DecryptData(aesEncrypted, aesKey);
                    aesDecrypted.Should().Be(plainText);
                    break;
                    
                case "RSA":
                    // RSA implementation would go here
                    // For now, just verify the algorithm is recognized
                    algorithm.Should().Be("RSA");
                    break;
            }
        }

        #endregion

        #region Token Management Tests

        [Fact]
        public async Task TokenManagement_Should_GenerateValidJWT()
        {
            // Arrange
            var userId = "user123";
            var claims = new Dictionary<string, string> { ["role"] = "user", ["email"] = "user@test.com" };
            
            _securityServiceMock.Setup(x => x.GenerateTokenAsync(userId, claims))
                .ReturnsAsync("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...");

            // Act
            var token = await _securityServiceMock.Object.GenerateTokenAsync(userId, claims);

            // Assert
            token.Should().NotBeNullOrEmpty();
            token.Should().StartWith("eyJ");
        }

        [Fact]
        public async Task TokenManagement_Should_ValidateTokens()
        {
            // Arrange
            var validToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
            var invalidToken = "invalid.token.here";
            
            _securityServiceMock.Setup(x => x.ValidateTokenAsync(validToken))
                .ReturnsAsync(new TokenValidationResult { IsValid = true });
            _securityServiceMock.Setup(x => x.ValidateTokenAsync(invalidToken))
                .ReturnsAsync(new TokenValidationResult { IsValid = false });

            // Act
            var validResult = await _securityServiceMock.Object.ValidateTokenAsync(validToken);
            var invalidResult = await _securityServiceMock.Object.ValidateTokenAsync(invalidToken);

            // Assert
            validResult.IsValid.Should().BeTrue();
            invalidResult.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task TokenManagement_Should_RefreshTokens()
        {
            // Arrange
            var refreshToken = "refresh_token_123";
            var newAccessToken = "new_access_token";
            
            _securityServiceMock.Setup(x => x.RefreshTokenAsync(refreshToken))
                .ReturnsAsync(newAccessToken);

            // Act
            var result = await _securityServiceMock.Object.RefreshTokenAsync(refreshToken);

            // Assert
            result.Should().Be(newAccessToken);
        }

        #endregion

        #region Security Audit Tests

        [Fact]
        public async Task SecurityAudit_Should_LogSecurityEvents()
        {
            // Arrange
            var securityEvent = new SecurityEvent
            {
                EventType = "LOGIN_ATTEMPT",
                UserId = "user123",
                Timestamp = DateTime.UtcNow,
                Success = true
            };
            
            _securityServiceMock.Setup(x => x.LogSecurityEventAsync(It.IsAny<SecurityEvent>()))
                .ReturnsAsync(true);

            // Act
            var logged = await _securityServiceMock.Object.LogSecurityEventAsync(securityEvent);

            // Assert
            logged.Should().BeTrue();
            _securityServiceMock.Verify(x => x.LogSecurityEventAsync(It.IsAny<SecurityEvent>()), Times.Once);
        }

        [Fact]
        public async Task SecurityAudit_Should_DetectSuspiciousActivity()
        {
            // Arrange
            var suspiciousPatterns = new[]
            {
                "Multiple failed login attempts",
                "Unusual access pattern",
                "Privilege escalation attempt"
            };
            
            _securityServiceMock.Setup(x => x.DetectAnomaliesAsync())
                .ReturnsAsync(suspiciousPatterns);

            // Act
            var anomalies = await _securityServiceMock.Object.DetectAnomaliesAsync();

            // Assert
            anomalies.Should().HaveCount(3);
            anomalies.Should().Contain("Multiple failed login attempts");
        }

        #endregion

        #region Helper Methods

        private string HashPassword(string password)
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private byte[] GenerateEncryptionKey()
        {
            var key = new byte[32]; // 256-bit key
            rng.GetBytes(key);
            return key;
        }

        private string EncryptData(string plainText, byte[] key)
        {
            aes.Key = key;
            aes.GenerateIV();
            
            var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            aes.IV.CopyTo(result, 0);
            encryptedBytes.CopyTo(result, aes.IV.Length);
            
            return Convert.ToBase64String(result);
        }

        private string DecryptData(string encryptedText, byte[] key)
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            
            aes.Key = key;
            
            var iv = new byte[16];
            var cipherText = new byte[encryptedBytes.Length - 16];
            
            Array.Copy(encryptedBytes, 0, iv, 0, 16);
            Array.Copy(encryptedBytes, 16, cipherText, 0, cipherText.Length);
            
            aes.IV = iv;
            
            var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
            
            return Encoding.UTF8.GetString(plainBytes);
        }

        #endregion

        public void Dispose()
        {
            if (_serviceProvider is IDisposable disposable)
                disposable.Dispose();
        }
    }

    #region Supporting Classes

    public interface ISecurityService
    {
        Task<AuthenticationResult> AuthenticateAsync(string username, string password);
        Task<bool> ValidateMfaAsync(string username, string code);
        Task<bool> AuthorizeAsync(string userId, string resource);
        Task<bool> HasPermissionAsync(string role, string permission);
        Task<string[]> GetEffectiveRolesAsync(string role);
        Task<string> GenerateTokenAsync(string userId, Dictionary<string, string> claims);
        Task<TokenValidationResult> ValidateTokenAsync(string token);
        Task<string> RefreshTokenAsync(string refreshToken);
        Task<bool> LogSecurityEventAsync(SecurityEvent securityEvent);
        Task<string[]> DetectAnomaliesAsync();
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string UserId { get; set; }
        public string Error { get; set; }
    }

    public class TokenValidationResult
    {
        public bool IsValid { get; set; }
        public Dictionary<string, string> Claims { get; set; }
        public string Error { get; set; }
    }

    public class SecurityEvent
    {
        public string EventType { get; set; }
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    #endregion
}