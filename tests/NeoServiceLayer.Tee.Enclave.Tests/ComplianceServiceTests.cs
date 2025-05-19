using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Enclave;
using Xunit;
using Xunit.Abstractions;
using System.Text.Json;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "OpenEnclave")]
    [Collection("SimulationMode")]
    public class ComplianceServiceTests
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;

        public ComplianceServiceTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<ComplianceServiceTests>();
            
            _logger.LogInformation("Test initialized with {UsingRealSdk}", 
                _fixture.UsingRealSdk ? "real SDK" : "mock implementation");
        }

        [Fact]
        public async Task ComplianceService_ShouldVerifyCompliantCode()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string code = @"
                function main(input) {
                    let result = 0;
                    for (let i = 0; i < 100; i++) {
                        result += i;
                    }
                    return { result: result };
                }
            ";
            string userId = "test-user";
            string functionId = "test-function-" + Guid.NewGuid().ToString();
            string complianceRules = @"{
                ""jurisdiction"": ""global"",
                ""prohibited_apis"": [""eval"", ""Function"", ""setTimeout"", ""setInterval"", ""XMLHttpRequest"", ""fetch""],
                ""prohibited_data"": [""password"", ""credit_card"", ""ssn"", ""passport""],
                ""allow_network_access"": false,
                ""max_gas"": 1000000
            }";
            
            // Act - Verify compliance
            string result = await _fixture.TeeInterface.VerifyComplianceAsync(code, userId, functionId, complianceRules);
            
            // Act - Get compliance status
            string status = await _fixture.TeeInterface.GetComplianceStatusAsync(functionId, "global");
            
            // Parse result
            var resultObj = JsonDocument.Parse(result).RootElement;
            var compliant = resultObj.GetProperty("compliant").GetBoolean();
            
            // Assert
            Assert.True(compliant);
            Assert.NotEmpty(status);
            
            _logger.LogInformation("Compliance result: {Result}", result);
            _logger.LogInformation("Compliance status: {Status}", status);
        }

        [Fact]
        public async Task ComplianceService_ShouldDetectNonCompliantCode()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string code = @"
                function main(input) {
                    // Using prohibited API
                    eval('let x = 10;');
                    
                    // Accessing prohibited data
                    let password = input.password;
                    
                    // Network access
                    let xhr = new XMLHttpRequest();
                    xhr.open('GET', 'https://example.com');
                    
                    return { result: 'Done' };
                }
            ";
            string userId = "test-user";
            string functionId = "test-function-" + Guid.NewGuid().ToString();
            string complianceRules = @"{
                ""jurisdiction"": ""global"",
                ""prohibited_apis"": [""eval"", ""Function"", ""setTimeout"", ""setInterval"", ""XMLHttpRequest"", ""fetch""],
                ""prohibited_data"": [""password"", ""credit_card"", ""ssn"", ""passport""],
                ""allow_network_access"": false,
                ""max_gas"": 1000000
            }";
            
            // Act - Verify compliance
            string result = await _fixture.TeeInterface.VerifyComplianceAsync(code, userId, functionId, complianceRules);
            
            // Act - Get compliance status
            string status = await _fixture.TeeInterface.GetComplianceStatusAsync(functionId, "global");
            
            // Parse result
            var resultObj = JsonDocument.Parse(result).RootElement;
            var compliant = resultObj.GetProperty("compliant").GetBoolean();
            var violations = resultObj.GetProperty("violations");
            
            // Assert
            Assert.False(compliant);
            Assert.True(violations.GetArrayLength() > 0);
            Assert.NotEmpty(status);
            
            _logger.LogInformation("Compliance result: {Result}", result);
            _logger.LogInformation("Compliance status: {Status}", status);
        }

        [Fact]
        public async Task ComplianceService_ShouldGetAndSetComplianceRules()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string jurisdiction = "test-jurisdiction-" + Guid.NewGuid().ToString();
            string rules = @"{
                ""prohibited_apis"": [""eval"", ""Function""],
                ""prohibited_data"": [""password""],
                ""allow_network_access"": true,
                ""max_gas"": 500000
            }";
            
            // Act - Set compliance rules
            bool setResult = await _fixture.TeeInterface.SetComplianceRulesAsync(jurisdiction, rules);
            
            // Act - Get compliance rules
            string retrievedRules = await _fixture.TeeInterface.GetComplianceRulesAsync(jurisdiction);
            
            // Parse rules
            var rulesObj = JsonDocument.Parse(retrievedRules).RootElement;
            var prohibitedApis = rulesObj.GetProperty("prohibited_apis");
            var maxGas = rulesObj.GetProperty("max_gas").GetUInt64();
            
            // Assert
            Assert.True(setResult);
            Assert.NotEmpty(retrievedRules);
            Assert.Equal(2, prohibitedApis.GetArrayLength());
            Assert.Equal(500000UL, maxGas);
            
            _logger.LogInformation("Retrieved rules: {Rules}", retrievedRules);
        }

        [Fact]
        public async Task ComplianceService_ShouldVerifyIdentity()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string userId = "test-user-" + Guid.NewGuid().ToString();
            string identityData = @"{
                ""name"": ""John Doe"",
                ""email"": ""john.doe@example.com"",
                ""address"": ""123 Main St, Anytown, USA"",
                ""phone"": ""+1-555-123-4567""
            }";
            string jurisdiction = "US";
            
            // Act - Verify identity
            string result = await _fixture.TeeInterface.VerifyIdentityAsync(userId, identityData, jurisdiction);
            
            // Act - Get identity status
            string status = await _fixture.TeeInterface.GetIdentityStatusAsync(userId, jurisdiction);
            
            // Parse result
            var resultObj = JsonDocument.Parse(result).RootElement;
            var verified = resultObj.GetProperty("verified").GetBoolean();
            
            // Assert
            Assert.True(verified);
            Assert.NotEmpty(status);
            
            _logger.LogInformation("Identity verification result: {Result}", result);
            _logger.LogInformation("Identity status: {Status}", status);
        }

        [Fact]
        public async Task ComplianceService_ShouldDetectInvalidIdentity()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string userId = "test-user-" + Guid.NewGuid().ToString();
            string identityData = @"{
                ""name"": ""John Doe"",
                ""email"": ""john.doe@example.com""
                // Missing required fields: address and phone
            }";
            string jurisdiction = "US";
            
            // Act - Verify identity
            string result = await _fixture.TeeInterface.VerifyIdentityAsync(userId, identityData, jurisdiction);
            
            // Act - Get identity status
            string status = await _fixture.TeeInterface.GetIdentityStatusAsync(userId, jurisdiction);
            
            // Parse result
            var resultObj = JsonDocument.Parse(result).RootElement;
            var verified = resultObj.GetProperty("verified").GetBoolean();
            var violations = resultObj.GetProperty("violations");
            
            // Assert
            Assert.False(verified);
            Assert.True(violations.GetArrayLength() > 0);
            Assert.NotEmpty(status);
            
            _logger.LogInformation("Identity verification result: {Result}", result);
            _logger.LogInformation("Identity status: {Status}", status);
        }
    }
}
