using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Integration.Tests.Configuration
{
    /// <summary>
    /// Integration tests for configuration validation
    /// </summary>
    public class ConfigurationValidationIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public ConfigurationValidationIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ConfigurationValidationService_WithValidConfiguration_ShouldSucceed()
        {
            // Arrange
            var configuration = CreateValidConfiguration();
            var services = CreateServicesWithConfiguration(configuration);
            var serviceProvider = services.BuildServiceProvider();
            
            var validationService = serviceProvider.GetRequiredService<ConfigurationValidationService>();

            // Act
            var result = validationService.ValidateAll();

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.AllErrors);
        }

        [Fact]
        public void ConfigurationValidationService_WithInvalidConfiguration_ShouldFail()
        {
            // Arrange
            var configuration = CreateInvalidConfiguration();
            var services = CreateServicesWithConfiguration(configuration);
            var serviceProvider = services.BuildServiceProvider();
            
            var validationService = serviceProvider.GetRequiredService<ConfigurationValidationService>();

            // Act
            var result = validationService.ValidateAll();

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.AllErrors);
        }

        [Fact]
        public void ConfigurationValidationService_ValidateAndThrow_WithInvalidConfig_ShouldThrow()
        {
            // Arrange
            var configuration = CreateInvalidConfiguration();
            var services = CreateServicesWithConfiguration(configuration);
            var serviceProvider = services.BuildServiceProvider();
            
            var validationService = serviceProvider.GetRequiredService<ConfigurationValidationService>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => validationService.ValidateAndThrow());
        }

        [Fact]
        public void DatabaseConfigurationValidator_WithValidConnectionString_ShouldSucceed()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=TestDb;Trusted_Connection=true;Encrypt=true",
                    ["Database:CommandTimeout"] = "30",
                    ["Database:MaxRetries"] = "3",
                    ["Environment"] = "Development"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(builder => builder.AddXUnit(_output));
            services.AddSingleton<IConfigurationValidator, DatabaseConfigurationValidator>();

            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetRequiredService<IConfigurationValidator>();

            // Act
            var result = validator.Validate();

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void SecurityConfigurationValidator_WithValidJwtConfig_ShouldSucceed()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SecretKey"] = "ThisIsAVeryLongSecretKeyThatMeetsTheMinimumRequirement32Characters",
                    ["Jwt:ExpirationMinutes"] = "60",
                    ["Jwt:Issuer"] = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience",
                    ["Environment"] = "Development"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(builder => builder.AddXUnit(_output));
            services.AddSingleton<IConfigurationValidator, SecurityConfigurationValidator>();

            var serviceProvider = services.BuildServiceProvider();
            var validator = serviceProvider.GetRequiredService<IConfigurationValidator>();

            // Act
            var result = validator.Validate();

            // Assert
            Assert.True(result.IsValid);
        }

        private static IConfiguration CreateValidConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Database settings
                    ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=TestDb;Trusted_Connection=true;Encrypt=true",
                    ["Database:CommandTimeout"] = "30",
                    ["Database:MaxRetries"] = "3",
                    ["Database:AutoMigrate"] = "false",
                    
                    // JWT settings
                    ["Jwt:SecretKey"] = "ThisIsAVeryLongSecretKeyThatMeetsTheMinimumRequirement32Characters",
                    ["Jwt:ExpirationMinutes"] = "60",
                    ["Jwt:Issuer"] = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience",
                    
                    // Password policy
                    ["PasswordPolicy:MinimumLength"] = "12",
                    ["PasswordPolicy:MaxFailedAttempts"] = "5",
                    ["PasswordPolicy:LockoutDurationMinutes"] = "15",
                    
                    // HTTPS
                    ["Https:RequireHttps"] = "true",
                    
                    // Rate limiting
                    ["RateLimit:Enabled"] = "true",
                    ["RateLimit:RequestsPerMinute"] = "100",
                    
                    // Environment
                    ["Environment"] = "Development"
                })
                .Build();
        }

        private static IConfiguration CreateInvalidConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Missing connection string
                    ["ConnectionStrings:DefaultConnection"] = "",
                    
                    // Invalid JWT settings
                    ["Jwt:SecretKey"] = "short", // Too short
                    ["Jwt:ExpirationMinutes"] = "0", // Invalid
                    
                    // Invalid password policy
                    ["PasswordPolicy:MinimumLength"] = "4", // Too short
                    ["PasswordPolicy:MaxFailedAttempts"] = "-1", // Invalid
                    
                    // Environment
                    ["Environment"] = "Production"
                })
                .Build();
        }

        private static IServiceCollection CreateServicesWithConfiguration(IConfiguration configuration)
        {
            var services = new ServiceCollection();
            services.AddSingleton(configuration);
            services.AddLogging();
            services.AddConfigurationValidation();
            
            return services;
        }
    }
}