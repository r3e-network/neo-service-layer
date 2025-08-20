using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Api;
using NeoServiceLayer.Services.Authentication.Infrastructure;
using NeoServiceLayer.Services.Authentication.Models;
using NeoServiceLayer.Services.Authentication.Repositories;
using NeoServiceLayer.Services.Authentication.Services;
using Newtonsoft.Json;
using Xunit;
using System.Threading;


namespace NeoServiceLayer.Authentication.Integration.Tests
{
    public class AuthenticationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly AuthenticationDbContext _dbContext;

        public AuthenticationIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AuthenticationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database for testing
                    services.AddDbContext<AuthenticationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });

                    // Add test configuration
                    var configuration = new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string>
                        {
                            ["Jwt:Secret"] = "TestSecretKeyThatIsLongEnoughForJWTTokenGenerationAndValidation2024",
                            ["Jwt:Issuer"] = "TestIssuer",
                            ["Jwt:Audience"] = "TestAudience",
                            ["Jwt:AccessTokenExpirationMinutes"] = "15",
                            ["Jwt:RefreshTokenExpirationDays"] = "7",
                            ["Authentication:MaxFailedAttempts"] = "5",
                            ["Authentication:LockoutDurationMinutes"] = "30",
                            ["Authentication:RateLimitPerMinute"] = "10",
                            ["Authentication:Require2FA"] = "false"
                        })
                        .Build();

                    services.AddSingleton<IConfiguration>(configuration);
                });
            });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            _scope = _factory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
            
            // Ensure database is created
            _dbContext.Database.EnsureCreated();
            
            // Seed initial data
            SeedTestData();
        }

        private void SeedTestData()
        {
            var passwordHasher = new PasswordHasher();

            // Create test roles
            var adminRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = "Admin",
                Description = "Administrator role",
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow
            };

            var userRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = "User",
                Description = "Standard user role",
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Roles.AddRange(adminRole, userRole);

            // Create test users
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@test.com",
                PasswordHash = passwordHasher.HashPassword("Admin123!@#"),
                EmailVerified = true,
                TwoFactorEnabled = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var normalUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "user@test.com",
                PasswordHash = passwordHasher.HashPassword("User123!@#"),
                EmailVerified = true,
                TwoFactorEnabled = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var unverifiedUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "unverified",
                Email = "unverified@test.com",
                PasswordHash = passwordHasher.HashPassword("Unverified123!@#"),
                EmailVerified = false,
                EmailVerificationToken = "verification-token-123",
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var twoFactorUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "2fauser",
                Email = "2fa@test.com",
                PasswordHash = passwordHasher.HashPassword("TwoFactor123!@#"),
                EmailVerified = true,
                TwoFactorEnabled = true,
                TwoFactorSecret = "JBSWY3DPEHPK3PXP",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.AddRange(adminUser, normalUser, unverifiedUser, twoFactorUser);

            // Assign roles to users
            _dbContext.UserRoles.Add(new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = "System"
            });

            _dbContext.UserRoles.Add(new UserRole
            {
                UserId = normalUser.Id,
                RoleId = userRole.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = "System"
            });

            _dbContext.SaveChanges();
        }

        #region Authentication Tests

        [Fact]
        public async Task Login_ValidCredentials_ReturnsTokens()
        {
            // Arrange
            var request = new
            {
                username = "testuser",
                password = "User123!@#"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/login", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(responseContent);

            ((string)result.accessToken).Should().NotBeNullOrEmpty();
            ((string)result.refreshToken).Should().NotBeNullOrEmpty();
            ((string)result.tokenType).Should().Be("Bearer");
            ((string)result.username).Should().Be("testuser");
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var request = new
            {
                username = "testuser",
                password = "WrongPassword"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/login", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("Invalid credentials");
        }

        [Fact]
        public async Task Login_UnverifiedEmail_ReturnsForbidden()
        {
            // Arrange
            var request = new
            {
                username = "unverified",
                password = "Unverified123!@#"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/login", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("Email address not verified");
        }

        [Fact]
        public async Task Login_TwoFactorEnabled_RequiresCode()
        {
            // Arrange
            var request = new
            {
                username = "2fauser",
                password = "TwoFactor123!@#"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/login", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);

            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(responseContent);

            ((bool)result.requiresTwoFactor).Should().BeTrue();
            ((string)result.message).Should().Contain("Two-factor authentication required");
        }

        #endregion

        #region Token Tests

        [Fact]
        public async Task RefreshToken_ValidToken_ReturnsNewTokens()
        {
            // Arrange - Login first to get tokens
            var loginRequest = new
            {
                username = "testuser",
                password = "User123!@#"
            };

            var loginContent = new StringContent(
                JsonConvert.SerializeObject(loginRequest),
                Encoding.UTF8,
                "application/json");

            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
            var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
            dynamic loginResult = JsonConvert.DeserializeObject(loginResponseContent);

            string refreshToken = loginResult.refreshToken;

            // Act - Refresh tokens
            var refreshRequest = new
            {
                refreshToken = refreshToken
            };

            var refreshContent = new StringContent(
                JsonConvert.SerializeObject(refreshRequest),
                Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync("/api/auth/refresh", refreshContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(responseContent);

            ((string)result.accessToken).Should().NotBeNullOrEmpty();
            ((string)result.refreshToken).Should().NotBeNullOrEmpty();
            ((string)result.refreshToken).Should().NotBe(refreshToken); // Should be rotated
        }

        [Fact]
        public async Task RefreshToken_InvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var request = new
            {
                refreshToken = "invalid-refresh-token"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/refresh", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Protected Endpoint Tests

        [Fact]
        public async Task ProtectedEndpoint_WithValidToken_ReturnsSuccess()
        {
            // Arrange - Login first to get access token
            var loginRequest = new
            {
                username = "testuser",
                password = "User123!@#"
            };

            var loginContent = new StringContent(
                JsonConvert.SerializeObject(loginRequest),
                Encoding.UTF8,
                "application/json");

            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
            var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
            dynamic loginResult = JsonConvert.DeserializeObject(loginResponseContent);

            string accessToken = loginResult.accessToken;

            // Act - Access protected endpoint
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _client.GetAsync("/api/auth/user/profile");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(responseContent);

            ((string)result.username).Should().Be("testuser");
            ((string)result.email).Should().Be("user@test.com");
        }

        [Fact]
        public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/auth/user/profile");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ProtectedEndpoint_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await _client.GetAsync("/api/auth/user/profile");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Registration Tests

        [Fact]
        public async Task Register_ValidData_CreatesUser()
        {
            // Arrange
            var request = new
            {
                username = "newuser",
                email = "newuser@test.com",
                password = "NewUser123!@#",
                confirmPassword = "NewUser123!@#"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/register", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(responseContent);

            ((string)result.message).Should().Contain("Registration successful");
            ((bool)result.emailVerificationRequired).Should().BeTrue();

            // Verify user was created in database
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == "newuser");
            
            user.Should().NotBeNull();
            user.Email.Should().Be("newuser@test.com");
            user.EmailVerified.Should().BeFalse();
        }

        [Fact]
        public async Task Register_DuplicateUsername_ReturnsBadRequest()
        {
            // Arrange
            var request = new
            {
                username = "testuser", // Already exists
                email = "another@test.com",
                password = "NewUser123!@#",
                confirmPassword = "NewUser123!@#"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/register", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("Username already exists");
        }

        [Fact]
        public async Task Register_WeakPassword_ReturnsBadRequest()
        {
            // Arrange
            var request = new
            {
                username = "weakpassuser",
                email = "weak@test.com",
                password = "weak", // Too weak
                confirmPassword = "weak"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/register", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("Password does not meet requirements");
        }

        #endregion

        #region Password Reset Tests

        [Fact]
        public async Task ForgotPassword_ValidEmail_SendsResetToken()
        {
            // Arrange
            var request = new
            {
                email = "user@test.com"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/forgot-password", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(responseContent);

            ((string)result.message).Should().Contain("Password reset instructions sent");

            // Verify reset token was generated
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == "user@test.com");
            
            user.PasswordResetToken.Should().NotBeNullOrEmpty();
            user.PasswordResetTokenExpiry.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task ResetPassword_ValidToken_UpdatesPassword()
        {
            // Arrange - First request password reset
            var forgotRequest = new
            {
                email = "user@test.com"
            };

            var forgotContent = new StringContent(
                JsonConvert.SerializeObject(forgotRequest),
                Encoding.UTF8,
                "application/json");

            await _client.PostAsync("/api/auth/forgot-password", forgotContent);

            // Get reset token from database
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == "user@test.com");
            var resetToken = user.PasswordResetToken;

            // Act - Reset password
            var resetRequest = new
            {
                token = resetToken,
                newPassword = "NewPassword123!@#",
                confirmPassword = "NewPassword123!@#"
            };

            var resetContent = new StringContent(
                JsonConvert.SerializeObject(resetRequest),
                Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync("/api/auth/reset-password", resetContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify can login with new password
            var loginRequest = new
            {
                username = "testuser",
                password = "NewPassword123!@#"
            };

            var loginContent = new StringContent(
                JsonConvert.SerializeObject(loginRequest),
                Encoding.UTF8,
                "application/json");

            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Logout Tests

        [Fact]
        public async Task Logout_ValidToken_InvalidatesToken()
        {
            // Arrange - Login first
            var loginRequest = new
            {
                username = "testuser",
                password = "User123!@#"
            };

            var loginContent = new StringContent(
                JsonConvert.SerializeObject(loginRequest),
                Encoding.UTF8,
                "application/json");

            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
            var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
            dynamic loginResult = JsonConvert.DeserializeObject(loginResponseContent);

            string accessToken = loginResult.accessToken;
            string refreshToken = loginResult.refreshToken;

            // Act - Logout
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);

            var logoutRequest = new
            {
                refreshToken = refreshToken
            };

            var logoutContent = new StringContent(
                JsonConvert.SerializeObject(logoutRequest),
                Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync("/api/auth/logout", logoutContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify token is blacklisted and cannot be used
            var profileResponse = await _client.GetAsync("/api/auth/user/profile");
            profileResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Rate Limiting Tests

        [Fact]
        public async Task Login_ExceedsRateLimit_ReturnsTooManyRequests()
        {
            // Arrange
            var request = new
            {
                username = "ratelimituser",
                password = "wrong"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json");

            // Act - Make multiple requests to exceed rate limit
            for (int i = 0; i < 11; i++) // Rate limit is 10 per minute
            {
                await _client.PostAsync("/api/auth/login", content);
            }

            var response = await _client.PostAsync("/api/auth/login", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("Too many attempts");
        }

        #endregion

        #region Account Lockout Tests

        [Fact]
        public async Task Login_ExceedsFailedAttempts_LocksAccount()
        {
            // Arrange
            var validRequest = new
            {
                username = "testuser",
                password = "User123!@#"
            };

            var invalidRequest = new
            {
                username = "testuser",
                password = "WrongPassword"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(invalidRequest),
                Encoding.UTF8,
                "application/json");

            // Act - Make multiple failed attempts
            for (int i = 0; i < 6; i++) // Max attempts is 5
            {
                await _client.PostAsync("/api/auth/login", content);
            }

            // Try with correct password
            var validContent = new StringContent(
                JsonConvert.SerializeObject(validRequest),
                Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync("/api/auth/login", validContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("Account is locked");
        }

        #endregion

        public void Dispose()
        {
            _dbContext?.Dispose();
            _scope?.Dispose();
            _client?.Dispose();
        }
    }
}