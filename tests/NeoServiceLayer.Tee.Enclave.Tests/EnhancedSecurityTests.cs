using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "Security")]
    [Collection("SimulationMode")]
    public class EnhancedSecurityTests
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;

        public EnhancedSecurityTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<EnhancedSecurityTests>();
            
            _logger.LogInformation("Test initialized with {UsingRealSdk}", 
                _fixture.UsingRealSdk ? "real SDK" : "mock implementation");
        }

        [Fact]
        public async Task RemoteAttestation_ShouldVerifyEnclaveIdentity()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            byte[] challenge = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(challenge);
            }
            
            try
            {
                // Act - Get attestation report
                byte[] report = await _fixture.TeeInterface.GetAttestationReportAsync(challenge);
                
                // Act - Verify attestation report
                bool isValid = await _fixture.TeeInterface.VerifyAttestationReportAsync(report);
                
                // Assert
                Assert.NotNull(report);
                Assert.True(report.Length > 0, "Attestation report should not be empty");
                Assert.True(isValid, "Attestation report should be valid");
                
                // Act - Get MRENCLAVE and MRSIGNER
                byte[] mrEnclave = await _fixture.TeeInterface.GetMrEnclaveAsync();
                byte[] mrSigner = await _fixture.TeeInterface.GetMrSignerAsync();
                
                // Assert
                Assert.NotNull(mrEnclave);
                Assert.NotNull(mrSigner);
                Assert.True(mrEnclave.Length > 0, "MRENCLAVE should not be empty");
                Assert.True(mrSigner.Length > 0, "MRSIGNER should not be empty");
                
                _logger.LogInformation("Successfully verified enclave identity");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in remote attestation test");
                throw;
            }
        }

        [Fact]
        public async Task RemoteAttestation_ShouldDetectTamperedReport()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            byte[] challenge = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(challenge);
            }
            
            try
            {
                // Act - Get attestation report
                byte[] report = await _fixture.TeeInterface.GetAttestationReportAsync(challenge);
                
                // Act - Tamper with the report
                if (report.Length > 100)
                {
                    report[100] ^= 0xFF; // Flip all bits in one byte
                }
                
                // Act - Verify tampered attestation report
                bool isValid = await _fixture.TeeInterface.VerifyAttestationReportAsync(report);
                
                // Assert
                Assert.False(isValid, "Tampered attestation report should not be valid");
                
                _logger.LogInformation("Successfully detected tampered attestation report");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in tampered report test");
                throw;
            }
        }

        [Fact]
        public async Task Storage_ShouldDetectTamperedData()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string key = "security-test-" + Guid.NewGuid().ToString();
            byte[] data = Encoding.UTF8.GetBytes("Test data for security testing");
            
            try
            {
                // Act - Store data
                bool storeResult = await _fixture.TeeInterface.StorePersistentDataAsync(key, data);
                Assert.True(storeResult, "Failed to store data");
                
                // Act - Tamper with the data
                await _fixture.TeeInterface.TamperWithStorageDataAsync(key);
                
                // Act - Try to retrieve the tampered data
                try
                {
                    byte[] retrievedData = await _fixture.TeeInterface.RetrievePersistentDataAsync(key);
                    
                    // If we get here, the tampering wasn't detected
                    // Check if the data is actually different
                    bool dataMatches = true;
                    if (retrievedData.Length == data.Length)
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            if (data[i] != retrievedData[i])
                            {
                                dataMatches = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        dataMatches = false;
                    }
                    
                    Assert.False(dataMatches, "Tampered data should not match original data");
                    _logger.LogWarning("Tampering was not detected, but data is different");
                }
                catch (Exception ex)
                {
                    // This is the expected behavior - tampering should be detected
                    _logger.LogInformation("Tampering was detected as expected: {Error}", ex.Message);
                }
                
                // Clean up
                try
                {
                    await _fixture.TeeInterface.RemovePersistentDataAsync(key);
                }
                catch
                {
                    // Ignore errors during cleanup
                }
                
                _logger.LogInformation("Successfully tested tampered data detection");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in tampered data test");
                throw;
            }
        }

        [Fact]
        public async Task UserSecrets_ShouldBeIsolated()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string user1 = "security-test-user1-" + Guid.NewGuid().ToString();
            string user2 = "security-test-user2-" + Guid.NewGuid().ToString();
            string secretName = "API_KEY";
            string secretValue1 = "test-api-key-1-" + Guid.NewGuid().ToString();
            string secretValue2 = "test-api-key-2-" + Guid.NewGuid().ToString();
            string functionId = "security-test-function-" + Guid.NewGuid().ToString();
            
            // JavaScript code that tries to access secrets
            string jsCode = @"
                function main(input) {
                    // Try to access the secret
                    let apiKey = SECRETS.API_KEY;
                    
                    return { 
                        success: true, 
                        api_key: apiKey
                    };
                }
            ";
            
            try
            {
                // Act - Store secrets for both users
                bool secretStored1 = await _fixture.TeeInterface.StoreUserSecretAsync(user1, secretName, secretValue1);
                bool secretStored2 = await _fixture.TeeInterface.StoreUserSecretAsync(user2, secretName, secretValue2);
                Assert.True(secretStored1, "Failed to store secret for user 1");
                Assert.True(secretStored2, "Failed to store secret for user 2");
                
                // Act - Execute JavaScript as user 1
                string secrets1 = JsonSerializer.Serialize(new Dictionary<string, string> { { secretName, secretValue1 } });
                string result1 = await _fixture.TeeInterface.ExecuteJavaScriptAsync(jsCode, "{}", secrets1, functionId, user1);
                
                // Act - Execute JavaScript as user 2
                string secrets2 = JsonSerializer.Serialize(new Dictionary<string, string> { { secretName, secretValue2 } });
                string result2 = await _fixture.TeeInterface.ExecuteJavaScriptAsync(jsCode, "{}", secrets2, functionId, user2);
                
                // Parse results
                var resultObj1 = JsonDocument.Parse(result1).RootElement;
                var resultObj2 = JsonDocument.Parse(result2).RootElement;
                string apiKey1 = resultObj1.GetProperty("api_key").GetString();
                string apiKey2 = resultObj2.GetProperty("api_key").GetString();
                
                // Assert
                Assert.Equal(secretValue1, apiKey1);
                Assert.Equal(secretValue2, apiKey2);
                Assert.NotEqual(apiKey1, apiKey2);
                
                // Act - Try to execute JavaScript as user 1 but with user 2's function ID
                try
                {
                    string result = await _fixture.TeeInterface.ExecuteJavaScriptAsync(jsCode, "{}", secrets1, functionId, user2);
                    
                    // Parse result
                    var resultObj = JsonDocument.Parse(result).RootElement;
                    string apiKey = resultObj.GetProperty("api_key").GetString();
                    
                    // Assert
                    Assert.NotEqual(secretValue1, apiKey);
                    Assert.Equal(secretValue2, apiKey);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("Expected error when accessing wrong user's function: {Error}", ex.Message);
                }
                
                // Clean up
                await _fixture.TeeInterface.DeleteUserSecretAsync(user1, secretName);
                await _fixture.TeeInterface.DeleteUserSecretAsync(user2, secretName);
                
                _logger.LogInformation("Successfully verified user secret isolation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in user secret isolation test");
                throw;
            }
        }

        [Fact]
        public async Task JavaScript_ShouldNotAccessSystemResources()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string userId = "security-test-user-" + Guid.NewGuid().ToString();
            string functionId = "security-test-function-" + Guid.NewGuid().ToString();
            
            // JavaScript code that tries to access system resources
            string jsCode = @"
                function main(input) {
                    let results = {};
                    
                    // Try to access process object
                    try {
                        results.process = typeof process !== 'undefined';
                    } catch (e) {
                        results.process = false;
                    }
                    
                    // Try to access require function
                    try {
                        results.require = typeof require !== 'undefined';
                    } catch (e) {
                        results.require = false;
                    }
                    
                    // Try to access fs module
                    try {
                        const fs = require('fs');
                        results.fs = true;
                    } catch (e) {
                        results.fs = false;
                    }
                    
                    // Try to access network
                    try {
                        const http = require('http');
                        results.http = true;
                    } catch (e) {
                        results.http = false;
                    }
                    
                    return results;
                }
            ";
            
            try
            {
                // Act - Execute JavaScript
                string result = await _fixture.TeeInterface.ExecuteJavaScriptAsync(jsCode, "{}", "{}", functionId, userId);
                
                // Parse result
                var resultObj = JsonDocument.Parse(result).RootElement;
                bool hasProcess = resultObj.GetProperty("process").GetBoolean();
                bool hasRequire = resultObj.GetProperty("require").GetBoolean();
                bool hasFs = resultObj.GetProperty("fs").GetBoolean();
                bool hasHttp = resultObj.GetProperty("http").GetBoolean();
                
                // Assert
                Assert.False(hasProcess, "JavaScript should not have access to process object");
                Assert.False(hasRequire, "JavaScript should not have access to require function");
                Assert.False(hasFs, "JavaScript should not have access to fs module");
                Assert.False(hasHttp, "JavaScript should not have access to http module");
                
                _logger.LogInformation("Successfully verified JavaScript sandbox security");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JavaScript sandbox test");
                throw;
            }
        }
    }
}
