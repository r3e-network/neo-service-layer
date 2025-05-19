using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "Integration")]
    [Collection("SimulationMode")]
    public class ComponentInteractionTests
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;

        public ComponentInteractionTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<ComponentInteractionTests>();
            
            _logger.LogInformation("Test initialized with {UsingRealSdk}", 
                _fixture.UsingRealSdk ? "real SDK" : "mock implementation");
        }

        [Fact]
        public async Task JavaScriptWithSecrets_ShouldStoreAndRetrieveFromStorage()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string userId = "integration-test-user-" + Guid.NewGuid().ToString();
            string secretName = "API_KEY";
            string secretValue = "test-api-key-" + Guid.NewGuid().ToString();
            string functionId = "storage-integration-test-" + Guid.NewGuid().ToString();
            
            // JavaScript code that stores data in storage using the secret as part of the key
            string jsCode = @"
                function main(input) {
                    // Use the secret API key as part of the storage key
                    const storageKey = 'data-' + SECRETS.API_KEY;
                    
                    // Store data in storage
                    const dataToStore = {
                        timestamp: new Date().toISOString(),
                        value: input.value,
                        source: 'integration-test'
                    };
                    
                    // Convert to string for storage
                    const dataString = JSON.stringify(dataToStore);
                    
                    // Store in storage
                    STORAGE.set(storageKey, dataString);
                    
                    // Retrieve from storage to verify
                    const retrievedData = STORAGE.get(storageKey);
                    const parsedData = JSON.parse(retrievedData);
                    
                    return {
                        success: true,
                        stored_key: storageKey,
                        original_data: dataToStore,
                        retrieved_data: parsedData,
                        match: JSON.stringify(dataToStore) === retrievedData
                    };
                }
            ";
            
            string input = JsonSerializer.Serialize(new { value = 42 });
            
            try
            {
                // Act - Store user secret
                bool secretStored = await _fixture.TeeInterface.StoreUserSecretAsync(userId, secretName, secretValue);
                Assert.True(secretStored, "Failed to store user secret");
                
                // Act - Store JavaScript function
                bool functionStored = await _fixture.TeeInterface.StoreJavaScriptFunctionAsync(functionId, jsCode, userId);
                Assert.True(functionStored, "Failed to store JavaScript function");
                
                // Act - Execute JavaScript function
                string secrets = JsonSerializer.Serialize(new Dictionary<string, string> { { secretName, secretValue } });
                string result = await _fixture.TeeInterface.ExecuteJavaScriptAsync(jsCode, input, secrets, functionId, userId);
                
                // Parse result
                var resultObj = JsonDocument.Parse(result).RootElement;
                bool success = resultObj.GetProperty("success").GetBoolean();
                string storedKey = resultObj.GetProperty("stored_key").GetString();
                bool dataMatch = resultObj.GetProperty("match").GetBoolean();
                
                // Assert
                Assert.True(success, "JavaScript execution should succeed");
                Assert.Contains(secretValue, storedKey);
                Assert.True(dataMatch, "Retrieved data should match stored data");
                
                _logger.LogInformation("Integration test result: {Result}", result);
                
                // Clean up
                await _fixture.TeeInterface.DeleteUserSecretAsync(userId, secretName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in integration test");
                throw;
            }
        }

        [Fact]
        public async Task EventTriggerWithRandomness_ShouldExecuteJavaScript()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string userId = "integration-test-user-" + Guid.NewGuid().ToString();
            string functionId = "randomness-trigger-test-" + Guid.NewGuid().ToString();
            string eventType = "blockchain";
            string condition = @"{""event_type"": ""transfer"", ""contract_address"": ""0x1234567890abcdef""}";
            
            // JavaScript code that uses randomness service
            string jsCode = @"
                function main(input) {
                    // Get event data
                    const event = input.event;
                    const trigger = input.trigger;
                    
                    // Generate a random number using the randomness service
                    const min = 1;
                    const max = 100;
                    const requestId = 'request-' + trigger.id;
                    
                    // In a real implementation, we would call the randomness service
                    // For this test, we'll simulate it
                    const randomNumber = Math.floor(Math.random() * (max - min + 1)) + min;
                    
                    // Store the result
                    const storageKey = 'random-result-' + trigger.id;
                    const resultData = {
                        timestamp: new Date().toISOString(),
                        event_type: event.type,
                        contract: event.contract,
                        random_number: randomNumber,
                        trigger_id: trigger.id
                    };
                    
                    // Store in storage
                    STORAGE.set(storageKey, JSON.stringify(resultData));
                    
                    return resultData;
                }
            ";
            
            try
            {
                // Act - Store JavaScript function
                bool functionStored = await _fixture.TeeInterface.StoreJavaScriptFunctionAsync(functionId, jsCode, userId);
                Assert.True(functionStored, "Failed to store JavaScript function");
                
                // Act - Register trigger
                string triggerId = await _fixture.TeeInterface.RegisterTriggerAsync(eventType, functionId, userId, condition);
                Assert.NotEmpty(triggerId, "Failed to register trigger");
                
                // Act - Process blockchain event
                string eventData = @"{
                    ""type"": ""transfer"",
                    ""contract"": ""0x1234567890abcdef"",
                    ""from"": ""0xabcdef1234567890"",
                    ""to"": ""0x0987654321fedcba"",
                    ""amount"": 100
                }";
                int processedCount = await _fixture.TeeInterface.ProcessBlockchainEventAsync(eventData);
                
                // Assert
                Assert.Equal(1, processedCount);
                
                _logger.LogInformation("Processed {Count} triggers", processedCount);
                
                // Clean up
                await _fixture.TeeInterface.UnregisterTriggerAsync(triggerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in integration test");
                throw;
            }
        }

        [Fact]
        public async Task ComplianceWithSecrets_ShouldEnforceRules()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string userId = "integration-test-user-" + Guid.NewGuid().ToString();
            string functionId = "compliance-test-" + Guid.NewGuid().ToString();
            string jurisdiction = "test-jurisdiction-" + Guid.NewGuid().ToString();
            
            // Set compliance rules
            string rules = @"{
                ""prohibited_apis"": [""eval"", ""Function""],
                ""prohibited_data"": [""password"", ""credit_card""],
                ""allow_network_access"": false,
                ""max_gas"": 1000000
            }";
            
            // JavaScript code that attempts to use prohibited APIs
            string nonCompliantCode = @"
                function main(input) {
                    // Using prohibited API
                    eval('let x = 10;');
                    
                    // Accessing secret
                    const apiKey = SECRETS.API_KEY;
                    
                    return { result: 'Done' };
                }
            ";
            
            // JavaScript code that complies with rules
            string compliantCode = @"
                function main(input) {
                    // Not using prohibited APIs
                    let x = 10;
                    
                    // Accessing secret
                    const apiKey = SECRETS.API_KEY;
                    
                    return { result: 'Done', api_key: apiKey };
                }
            ";
            
            try
            {
                // Act - Set compliance rules
                bool rulesSet = await _fixture.TeeInterface.SetComplianceRulesAsync(jurisdiction, rules);
                Assert.True(rulesSet, "Failed to set compliance rules");
                
                // Act - Store user secret
                bool secretStored = await _fixture.TeeInterface.StoreUserSecretAsync(userId, "API_KEY", "test-api-key");
                Assert.True(secretStored, "Failed to store user secret");
                
                // Act - Verify non-compliant code
                string nonCompliantResult = await _fixture.TeeInterface.VerifyComplianceAsync(nonCompliantCode, userId, functionId, rules);
                var nonCompliantObj = JsonDocument.Parse(nonCompliantResult).RootElement;
                bool nonCompliant = nonCompliantObj.GetProperty("compliant").GetBoolean();
                
                // Act - Verify compliant code
                string compliantResult = await _fixture.TeeInterface.VerifyComplianceAsync(compliantCode, userId, functionId, rules);
                var compliantObj = JsonDocument.Parse(compliantResult).RootElement;
                bool compliant = compliantObj.GetProperty("compliant").GetBoolean();
                
                // Assert
                Assert.False(nonCompliant, "Non-compliant code should be detected");
                Assert.True(compliant, "Compliant code should pass verification");
                
                _logger.LogInformation("Non-compliant result: {Result}", nonCompliantResult);
                _logger.LogInformation("Compliant result: {Result}", compliantResult);
                
                // Clean up
                await _fixture.TeeInterface.DeleteUserSecretAsync(userId, "API_KEY");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in integration test");
                throw;
            }
        }
    }
}
