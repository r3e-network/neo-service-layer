using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host;
using NeoServiceLayer.Tee.Shared;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Integration.Tests
{
    [Trait("Category", "Integration")]
    public class EnclaveIntegrationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;
        private readonly TeeEnclaveHost _enclaveHost;
        private readonly ITeeEnclaveInterface _enclaveInterface;

        public EnclaveIntegrationTests(ITestOutputHelper output)
        {
            _output = output;

            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<EnclaveIntegrationTests>();

            // Initialize the enclave host in simulation mode
            _logger.LogInformation("Initializing enclave host in simulation mode");
            _enclaveHost = new TeeEnclaveHost(loggerFactory, simulationMode: true);
            _enclaveInterface = _enclaveHost.GetEnclaveInterface();
        }

        public void Dispose()
        {
            _enclaveHost?.Dispose();
        }

        [Fact]
        public async Task EnclaveHost_ShouldInitializeSuccessfully()
        {
            // Assert
            Assert.NotNull(_enclaveHost);
            Assert.NotNull(_enclaveInterface);

            // Get enclave measurements
            var mrEnclave = await _enclaveInterface.GetMrEnclaveAsync();
            var mrSigner = await _enclaveInterface.GetMrSignerAsync();

            // Assert
            Assert.NotNull(mrEnclave);
            Assert.NotNull(mrSigner);
            Assert.True(mrEnclave.Length > 0, "MRENCLAVE should not be empty");
            Assert.True(mrSigner.Length > 0, "MRSIGNER should not be empty");

            _logger.LogInformation("MRENCLAVE: {MrEnclave}", Convert.ToBase64String(mrEnclave));
            _logger.LogInformation("MRSIGNER: {MrSigner}", Convert.ToBase64String(mrSigner));
        }

        [Fact]
        public async Task EnclaveHost_ShouldExecuteJavaScript()
        {
            // Arrange
            string jsCode = @"
                function main(input) {
                    return { result: 'Hello, ' + input.name };
                }
            ";
            string input = @"{ ""name"": ""World"" }";
            string secrets = @"{}";
            string functionId = "test-function";
            string userId = "test-user";

            // Act
            var result = await _enclaveInterface.ExecuteJavaScriptAsync(
                jsCode, input, secrets, functionId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success, "JavaScript execution should succeed");
            Assert.Contains("Hello, World", result.Result);

            _logger.LogInformation("JavaScript execution result: {Result}", result.Result);
            _logger.LogInformation("Gas used: {GasUsed}", result.GasUsed);
        }

        [Fact]
        public async Task EnclaveHost_ShouldStoreAndRetrieveSecrets()
        {
            // Arrange
            string userId = "test-user-" + Guid.NewGuid().ToString();
            string secretName = "test-secret";
            string secretValue = "This is a test secret value";

            // Act - Store secret
            bool storeResult = await _enclaveInterface.StoreUserSecretAsync(userId, secretName, secretValue);

            // Act - Retrieve secret
            string retrievedSecret = await _enclaveInterface.GetUserSecretAsync(userId, secretName);

            // Act - Get secret names
            var secretNames = await _enclaveInterface.GetUserSecretNamesAsync(userId);

            // Act - Delete secret
            bool deleteResult = await _enclaveInterface.DeleteUserSecretAsync(userId, secretName);

            // Act - Try to retrieve deleted secret
            string deletedSecret = await _enclaveInterface.GetUserSecretAsync(userId, secretName);

            // Assert
            Assert.True(storeResult, "Storing secret should succeed");
            Assert.Equal(secretValue, retrievedSecret);
            Assert.Contains(secretName, secretNames);
            Assert.True(deleteResult, "Deleting secret should succeed");
            Assert.Empty(deletedSecret);

            _logger.LogInformation("Secret test passed for user {UserId}", userId);
        }

        [Fact]
        public async Task EnclaveHost_ShouldStoreAndRetrievePersistentData()
        {
            // Arrange
            string key = "test-key-" + Guid.NewGuid().ToString();
            byte[] data = Encoding.UTF8.GetBytes("This is test data for storage");

            // Act - Store data
            bool storeResult = await _enclaveInterface.StorePersistentDataAsync(key, data);

            // Act - Retrieve data
            byte[] retrievedData = await _enclaveInterface.RetrievePersistentDataAsync(key);

            // Act - Check if key exists
            bool keyExists = await _enclaveInterface.PersistentDataExistsAsync(key);

            // Act - Delete data
            bool deleteResult = await _enclaveInterface.RemovePersistentDataAsync(key);

            // Act - Check if key exists after deletion
            bool keyExistsAfterDeletion = await _enclaveInterface.PersistentDataExistsAsync(key);

            // Assert
            Assert.True(storeResult, "Storing data should succeed");
            Assert.Equal(data, retrievedData);
            Assert.True(keyExists, "Key should exist after storing data");
            Assert.True(deleteResult, "Deleting data should succeed");
            Assert.False(keyExistsAfterDeletion, "Key should not exist after deletion");

            _logger.LogInformation("Storage test passed for key {Key}", key);
        }

        [Fact]
        public async Task EnclaveHost_ShouldTrackGasUsage()
        {
            // Arrange
            string jsCode = @"
                function main(input) {
                    // Perform some operations that use gas
                    let result = 0;
                    for (let i = 0; i < 1000; i++) {
                        result += i;
                    }
                    return { result: result };
                }
            ";
            string input = @"{}";
            string secrets = @"{}";
            string functionId = "test-function-gas";
            string userId = "test-user";

            // Act - Reset gas used
            await _enclaveInterface.ResetGasUsedAsync();

            // Act - Get initial gas used
            ulong initialGasUsed = await _enclaveInterface.GetGasUsedAsync();

            // Act - Execute JavaScript
            var result = await _enclaveInterface.ExecuteJavaScriptAsync(
                jsCode, input, secrets, functionId, userId);

            // Act - Get gas used after execution
            ulong finalGasUsed = await _enclaveInterface.GetGasUsedAsync();

            // Assert
            Assert.Equal(0UL, initialGasUsed);
            Assert.True(finalGasUsed > 0UL, "Gas used should be greater than zero after execution");
            Assert.Equal(result.GasUsed, finalGasUsed);

            _logger.LogInformation("Gas used: {GasUsed}", finalGasUsed);
        }

        [Fact]
        public async Task EnclaveHost_ShouldGenerateRandomBytes()
        {
            // Arrange
            int length = 32;

            // Act
            byte[] randomBytes1 = await _enclaveInterface.GetRandomBytesAsync(length);
            byte[] randomBytes2 = await _enclaveInterface.GetRandomBytesAsync(length);

            // Assert
            Assert.NotNull(randomBytes1);
            Assert.NotNull(randomBytes2);
            Assert.Equal(length, randomBytes1.Length);
            Assert.Equal(length, randomBytes2.Length);
            Assert.NotEqual(randomBytes1, randomBytes2); // Should be different

            _logger.LogInformation("Random bytes 1: {RandomBytes1}", Convert.ToBase64String(randomBytes1));
            _logger.LogInformation("Random bytes 2: {RandomBytes2}", Convert.ToBase64String(randomBytes2));
        }

        [Fact]
        public async Task EnclaveHost_ShouldSignAndVerifyData()
        {
            // Arrange
            byte[] data = Encoding.UTF8.GetBytes("This is a test message to sign");

            // Act
            byte[] signature = await _enclaveInterface.SignDataAsync(data);
            bool isValid = await _enclaveInterface.VerifySignatureAsync(data, signature);

            // Tamper with the data
            byte[] tamperedData = Encoding.UTF8.GetBytes("This is a tampered test message");
            bool isInvalid = await _enclaveInterface.VerifySignatureAsync(tamperedData, signature);

            // Assert
            Assert.NotNull(signature);
            Assert.True(signature.Length > 0, "Signature should not be empty");
            Assert.True(isValid, "Signature should be valid for the original data");
            Assert.False(isInvalid, "Signature should be invalid for tampered data");

            _logger.LogInformation("Signature: {Signature}", Convert.ToBase64String(signature));
        }

        [Fact]
        public async Task EnclaveHost_ShouldSealAndUnsealData()
        {
            // Arrange
            byte[] data = Encoding.UTF8.GetBytes("This is a test message to seal");

            // Act
            byte[] sealedData = await _enclaveInterface.SealDataAsync(data);
            byte[] unsealedData = await _enclaveInterface.UnsealDataAsync(sealedData);

            // Assert
            Assert.NotNull(sealedData);
            Assert.NotNull(unsealedData);
            Assert.True(sealedData.Length > data.Length, "Sealed data should be larger than original data");
            Assert.Equal(data, unsealedData);

            _logger.LogInformation("Original data: {OriginalData}", Convert.ToBase64String(data));
            _logger.LogInformation("Sealed data: {SealedData}", Convert.ToBase64String(sealedData));
            _logger.LogInformation("Unsealed data: {UnsealedData}", Convert.ToBase64String(unsealedData));
        }

        #region Event Trigger Tests

        [Fact]
        public async Task EnclaveHost_ShouldRegisterAndUnregisterTrigger()
        {
            // Arrange
            string eventType = "blockchain";
            string functionId = "test-function-" + Guid.NewGuid().ToString();
            string userId = "test-user";
            string condition = @"{""event_type"": ""transfer"", ""contract_address"": ""0x1234567890abcdef""}";

            // Act - Register trigger
            string triggerId = await _enclaveInterface.RegisterTriggerAsync(eventType, functionId, userId, condition);

            // Act - Get triggers for event
            var triggers = await _enclaveInterface.GetTriggersForEventAsync(eventType);

            // Act - Get trigger info
            string triggerInfo = await _enclaveInterface.GetTriggerInfoAsync(triggerId);

            // Act - Unregister trigger
            bool unregisterResult = await _enclaveInterface.UnregisterTriggerAsync(triggerId);

            // Act - Get triggers for event after unregistering
            var triggersAfterUnregister = await _enclaveInterface.GetTriggersForEventAsync(eventType);

            // Assert
            Assert.NotEmpty(triggerId);
            Assert.Contains(triggerId, triggers);
            Assert.NotEmpty(triggerInfo);
            Assert.True(unregisterResult);
            Assert.DoesNotContain(triggerId, triggersAfterUnregister);

            _logger.LogInformation("Trigger ID: {TriggerId}", triggerId);
            _logger.LogInformation("Trigger info: {TriggerInfo}", triggerInfo);
        }

        [Fact]
        public async Task EnclaveHost_ShouldProcessBlockchainEvent()
        {
            // Arrange
            string eventType = "blockchain";
            string functionId = "test-function-" + Guid.NewGuid().ToString();
            string userId = "test-user";
            string condition = @"{""event_type"": ""transfer"", ""contract_address"": ""0x1234567890abcdef""}";
            string jsCode = @"
                function main(input) {
                    return {
                        event_processed: true,
                        event_type: input.event.type,
                        contract: input.event.contract,
                        trigger_id: input.trigger.id
                    };
                }
            ";

            // Act - Store JavaScript function
            await _enclaveInterface.StoreJavaScriptFunctionAsync(functionId, jsCode, userId);

            // Act - Register trigger
            string triggerId = await _enclaveInterface.RegisterTriggerAsync(eventType, functionId, userId, condition);

            // Act - Process blockchain event
            string eventData = @"{
                ""type"": ""transfer"",
                ""contract"": ""0x1234567890abcdef"",
                ""from"": ""0xabcdef1234567890"",
                ""to"": ""0x0987654321fedcba"",
                ""amount"": 100
            }";
            int processedCount = await _enclaveInterface.ProcessBlockchainEventAsync(eventData);

            // Act - Clean up
            await _enclaveInterface.UnregisterTriggerAsync(triggerId);

            // Assert
            Assert.Equal(1, processedCount);

            _logger.LogInformation("Processed {Count} triggers", processedCount);
        }

        [Fact]
        public async Task EnclaveHost_ShouldProcessScheduledTriggers()
        {
            // Arrange
            string eventType = "schedule";
            string functionId = "test-function-" + Guid.NewGuid().ToString();
            string userId = "test-user";
            string condition = @"{""interval_seconds"": 60}";
            string jsCode = @"
                function main(input) {
                    return {
                        scheduled_execution: true,
                        timestamp: input.timestamp,
                        trigger_id: input.trigger.id
                    };
                }
            ";

            // Act - Store JavaScript function
            await _enclaveInterface.StoreJavaScriptFunctionAsync(functionId, jsCode, userId);

            // Act - Register trigger
            string triggerId = await _enclaveInterface.RegisterTriggerAsync(eventType, functionId, userId, condition);

            // Act - Process scheduled triggers
            ulong currentTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 120; // 2 minutes in the future
            int processedCount = await _enclaveInterface.ProcessScheduledTriggersAsync(currentTime);

            // Act - Clean up
            await _enclaveInterface.UnregisterTriggerAsync(triggerId);

            // Assert
            Assert.Equal(1, processedCount);

            _logger.LogInformation("Processed {Count} scheduled triggers", processedCount);
        }

        #endregion

        #region Randomness Service Tests

        [Fact]
        public async Task EnclaveHost_ShouldGenerateAndVerifyRandomNumber()
        {
            // Arrange
            ulong min = 1;
            ulong max = 100;
            string userId = "test-user";
            string requestId = Guid.NewGuid().ToString();

            // Act - Generate random number
            ulong randomNumber = await _enclaveInterface.GenerateRandomNumberAsync(min, max, userId, requestId);

            // Act - Get proof
            string proof = await _enclaveInterface.GetRandomNumberProofAsync(randomNumber, min, max, userId, requestId);

            // Act - Verify random number
            bool isValid = await _enclaveInterface.VerifyRandomNumberAsync(randomNumber, min, max, userId, requestId, proof);

            // Assert
            Assert.InRange(randomNumber, min, max);
            Assert.NotEmpty(proof);
            Assert.True(isValid);

            _logger.LogInformation("Random number: {RandomNumber}", randomNumber);
            _logger.LogInformation("Proof: {Proof}", proof);
        }

        [Fact]
        public async Task EnclaveHost_ShouldDetectTamperedRandomNumber()
        {
            // Arrange
            ulong min = 1;
            ulong max = 100;
            string userId = "test-user";
            string requestId = Guid.NewGuid().ToString();

            // Act - Generate random number
            ulong randomNumber = await _enclaveInterface.GenerateRandomNumberAsync(min, max, userId, requestId);

            // Act - Get proof
            string proof = await _enclaveInterface.GetRandomNumberProofAsync(randomNumber, min, max, userId, requestId);

            // Act - Verify tampered random number
            ulong tamperedNumber = randomNumber + 1;
            bool isValid = await _enclaveInterface.VerifyRandomNumberAsync(tamperedNumber, min, max, userId, requestId, proof);

            // Assert
            Assert.False(isValid);

            _logger.LogInformation("Original random number: {RandomNumber}", randomNumber);
            _logger.LogInformation("Tampered random number: {TamperedNumber}", tamperedNumber);
        }

        [Fact]
        public async Task EnclaveHost_ShouldGenerateSeed()
        {
            // Arrange
            string userId = "test-user";
            string requestId = Guid.NewGuid().ToString();

            // Act - Generate seed
            string seed = await _enclaveInterface.GenerateSeedAsync(userId, requestId);

            // Assert
            Assert.NotEmpty(seed);

            _logger.LogInformation("Seed: {Seed}", seed);
        }

        #endregion

        #region Compliance Service Tests

        [Fact]
        public async Task EnclaveHost_ShouldVerifyCompliantCode()
        {
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
            string result = await _enclaveInterface.VerifyComplianceAsync(code, userId, functionId, complianceRules);

            // Act - Get compliance status
            string status = await _enclaveInterface.GetComplianceStatusAsync(functionId, "global");

            // Parse result
            var resultObj = System.Text.Json.JsonDocument.Parse(result).RootElement;
            var compliant = resultObj.GetProperty("compliant").GetBoolean();

            // Assert
            Assert.True(compliant);
            Assert.NotEmpty(status);

            _logger.LogInformation("Compliance result: {Result}", result);
            _logger.LogInformation("Compliance status: {Status}", status);
        }

        [Fact]
        public async Task EnclaveHost_ShouldDetectNonCompliantCode()
        {
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
            string result = await _enclaveInterface.VerifyComplianceAsync(code, userId, functionId, complianceRules);

            // Parse result
            var resultObj = System.Text.Json.JsonDocument.Parse(result).RootElement;
            var compliant = resultObj.GetProperty("compliant").GetBoolean();
            var violations = resultObj.GetProperty("violations");

            // Assert
            Assert.False(compliant);
            Assert.True(violations.GetArrayLength() > 0);

            _logger.LogInformation("Compliance result: {Result}", result);
        }

        [Fact]
        public async Task EnclaveHost_ShouldGetAndSetComplianceRules()
        {
            // Arrange
            string jurisdiction = "test-jurisdiction-" + Guid.NewGuid().ToString();
            string rules = @"{
                ""prohibited_apis"": [""eval"", ""Function""],
                ""prohibited_data"": [""password""],
                ""allow_network_access"": true,
                ""max_gas"": 500000
            }";

            // Act - Set compliance rules
            bool setResult = await _enclaveInterface.SetComplianceRulesAsync(jurisdiction, rules);

            // Act - Get compliance rules
            string retrievedRules = await _enclaveInterface.GetComplianceRulesAsync(jurisdiction);

            // Parse rules
            var rulesObj = System.Text.Json.JsonDocument.Parse(retrievedRules).RootElement;
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
        public async Task EnclaveHost_ShouldVerifyIdentity()
        {
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
            string result = await _enclaveInterface.VerifyIdentityAsync(userId, identityData, jurisdiction);

            // Act - Get identity status
            string status = await _enclaveInterface.GetIdentityStatusAsync(userId, jurisdiction);

            // Parse result
            var resultObj = System.Text.Json.JsonDocument.Parse(result).RootElement;
            var verified = resultObj.GetProperty("verified").GetBoolean();

            // Assert
            Assert.True(verified);
            Assert.NotEmpty(status);

            _logger.LogInformation("Identity verification result: {Result}", result);
            _logger.LogInformation("Identity status: {Status}", status);
        }

        #endregion
    }

    // Logger provider for xUnit
    public class XunitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public XunitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XunitLogger(_testOutputHelper, categoryName);
        }

        public void Dispose()
        {
        }
    }

    public class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _categoryName;

        public XunitLogger(ITestOutputHelper testOutputHelper, string categoryName)
        {
            _testOutputHelper = testOutputHelper;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            try
            {
                _testOutputHelper.WriteLine($"{logLevel}: {_categoryName} - {formatter(state, exception)}");
                if (exception != null)
                {
                    _testOutputHelper.WriteLine($"Exception: {exception}");
                }
            }
            catch
            {
                // Ignore exceptions from the test output helper
            }
        }
    }
}
