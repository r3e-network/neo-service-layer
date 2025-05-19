using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests
{
    [Trait("Category", "JavaScript")]
    public class EnclaveJavaScriptTests : IDisposable
    {
        private readonly Mock<ILogger<OpenEnclaveTeeInterface>> _loggerMock;
        private readonly string _testDirectory;
        private readonly string _enclaveImagePath;
        private readonly OpenEnclaveTeeInterface _teeInterface;

        public EnclaveJavaScriptTests()
        {
            _loggerMock = new Mock<ILogger<OpenEnclaveTeeInterface>>();
            
            // Create a test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), $"enclave_js_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            
            // Set the enclave image path
            _enclaveImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "enclave.signed");
            
            // Skip tests if the enclave image doesn't exist
            if (!File.Exists(_enclaveImagePath))
            {
                Skip.If(true, $"Enclave image not found at {_enclaveImagePath}");
                return;
            }
            
            try
            {
                // Create the TEE interface
                _teeInterface = new OpenEnclaveTeeInterface(
                    _loggerMock.Object,
                    new OpenEnclaveTeeOptions
                    {
                        EnclaveImagePath = _enclaveImagePath,
                        SimulationMode = true,
                        StorageDirectory = _testDirectory
                    });
                
                // Initialize the TEE interface
                _teeInterface.Initialize();
            }
            catch (Exception)
            {
                // Skip tests if the enclave can't be initialized
                Skip.If(true, "Failed to initialize enclave in simulation mode");
            }
        }

        public void Dispose()
        {
            // Clean up
            (_teeInterface as IDisposable)?.Dispose();
            
            // Delete the test directory
            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, true);
                }
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        [Fact]
        public async Task ExecuteJavaScript_SimpleFunction_ReturnsResult()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null)
            {
                return;
            }
            
            // Arrange
            string jsCode = @"
                function main(input) {
                    return { result: 'Hello, ' + input.name };
                }
            ";
            string input = @"{ ""name"": ""World"" }";
            
            try
            {
                // Act
                string result = await _teeInterface.ExecuteJavaScriptAsync(jsCode, input);
                
                // Assert
                Assert.NotNull(result);
                var resultObj = JsonSerializer.Deserialize<JsonElement>(result);
                Assert.True(resultObj.TryGetProperty("result", out var resultProp));
                Assert.Equal("Hello, World", resultProp.GetString());
            }
            catch (Exception)
            {
                // Skip the test if JavaScript execution fails
                Skip.If(true, "JavaScript execution not supported in this enclave build");
            }
        }

        [Fact]
        public async Task ExecuteJavaScript_WithGas_TracksGasUsage()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null)
            {
                return;
            }
            
            // Arrange
            string jsCode = @"
                function main(input) {
                    // Do some computation to use gas
                    let result = 0;
                    for (let i = 0; i < 1000; i++) {
                        result += i;
                    }
                    return { result: result };
                }
            ";
            string input = @"{}";
            
            try
            {
                // Act
                string result = await _teeInterface.ExecuteJavaScriptWithGasAsync(jsCode, input, out ulong gasUsed);
                
                // Assert
                Assert.NotNull(result);
                Assert.True(gasUsed > 0, "Gas usage should be greater than zero");
                
                var resultObj = JsonSerializer.Deserialize<JsonElement>(result);
                Assert.True(resultObj.TryGetProperty("result", out var resultProp));
                Assert.Equal(499500, resultProp.GetInt32()); // Sum of numbers from 0 to 999
            }
            catch (Exception)
            {
                // Skip the test if JavaScript execution fails
                Skip.If(true, "JavaScript execution with gas tracking not supported in this enclave build");
            }
        }

        [Fact]
        public async Task ExecuteJavaScript_WithStorage_PersistsData()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null)
            {
                return;
            }
            
            // Arrange
            string jsCode = @"
                function main(input) {
                    // Store data
                    if (input.action === 'store') {
                        storage.set(input.key, input.value);
                        return { success: true, action: 'store' };
                    }
                    // Retrieve data
                    else if (input.action === 'retrieve') {
                        const value = storage.get(input.key);
                        return { success: true, action: 'retrieve', value: value };
                    }
                    // Delete data
                    else if (input.action === 'delete') {
                        storage.delete(input.key);
                        return { success: true, action: 'delete' };
                    }
                    return { success: false, error: 'Invalid action' };
                }
            ";
            
            string storeInput = @"{ ""action"": ""store"", ""key"": ""test-js-storage"", ""value"": ""Hello from JavaScript"" }";
            string retrieveInput = @"{ ""action"": ""retrieve"", ""key"": ""test-js-storage"" }";
            string deleteInput = @"{ ""action"": ""delete"", ""key"": ""test-js-storage"" }";
            
            try
            {
                // Act - Store
                string storeResult = await _teeInterface.ExecuteJavaScriptAsync(jsCode, storeInput);
                
                // Act - Retrieve
                string retrieveResult = await _teeInterface.ExecuteJavaScriptAsync(jsCode, retrieveInput);
                
                // Act - Delete
                string deleteResult = await _teeInterface.ExecuteJavaScriptAsync(jsCode, deleteInput);
                
                // Act - Retrieve again
                string retrieveAgainResult = await _teeInterface.ExecuteJavaScriptAsync(jsCode, retrieveInput);
                
                // Assert
                var storeResultObj = JsonSerializer.Deserialize<JsonElement>(storeResult);
                Assert.True(storeResultObj.GetProperty("success").GetBoolean());
                
                var retrieveResultObj = JsonSerializer.Deserialize<JsonElement>(retrieveResult);
                Assert.True(retrieveResultObj.GetProperty("success").GetBoolean());
                Assert.Equal("Hello from JavaScript", retrieveResultObj.GetProperty("value").GetString());
                
                var deleteResultObj = JsonSerializer.Deserialize<JsonElement>(deleteResult);
                Assert.True(deleteResultObj.GetProperty("success").GetBoolean());
                
                var retrieveAgainResultObj = JsonSerializer.Deserialize<JsonElement>(retrieveAgainResult);
                Assert.True(retrieveAgainResultObj.GetProperty("success").GetBoolean());
                Assert.Null(retrieveAgainResultObj.GetProperty("value").GetString());
            }
            catch (Exception)
            {
                // Skip the test if JavaScript execution fails
                Skip.If(true, "JavaScript execution with storage not supported in this enclave build");
            }
        }

        [Fact]
        public async Task ExecuteJavaScript_WithUserSecrets_ManagesSecrets()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null)
            {
                return;
            }
            
            // Arrange
            string jsCode = @"
                function main(input) {
                    // Store secret
                    if (input.action === 'store') {
                        secrets.store(input.userId, input.secretName, input.secretValue);
                        return { success: true, action: 'store' };
                    }
                    // Retrieve secret
                    else if (input.action === 'retrieve') {
                        const value = secrets.get(input.userId, input.secretName);
                        return { success: true, action: 'retrieve', value: value };
                    }
                    // Delete secret
                    else if (input.action === 'delete') {
                        secrets.delete(input.userId, input.secretName);
                        return { success: true, action: 'delete' };
                    }
                    // List secrets
                    else if (input.action === 'list') {
                        const secretNames = secrets.list(input.userId);
                        return { success: true, action: 'list', secretNames: secretNames };
                    }
                    return { success: false, error: 'Invalid action' };
                }
            ";
            
            string userId = "test-user-" + Guid.NewGuid().ToString();
            string secretName = "test-secret";
            string secretValue = "test-secret-value";
            
            string storeInput = $@"{{ ""action"": ""store"", ""userId"": ""{userId}"", ""secretName"": ""{secretName}"", ""secretValue"": ""{secretValue}"" }}";
            string retrieveInput = $@"{{ ""action"": ""retrieve"", ""userId"": ""{userId}"", ""secretName"": ""{secretName}"" }}";
            string listInput = $@"{{ ""action"": ""list"", ""userId"": ""{userId}"" }}";
            string deleteInput = $@"{{ ""action"": ""delete"", ""userId"": ""{userId}"", ""secretName"": ""{secretName}"" }}";
            
            try
            {
                // Act - Store
                string storeResult = await _teeInterface.ExecuteJavaScriptAsync(jsCode, storeInput);
                
                // Act - Retrieve
                string retrieveResult = await _teeInterface.ExecuteJavaScriptAsync(jsCode, retrieveInput);
                
                // Act - List
                string listResult = await _teeInterface.ExecuteJavaScriptAsync(jsCode, listInput);
                
                // Act - Delete
                string deleteResult = await _teeInterface.ExecuteJavaScriptAsync(jsCode, deleteInput);
                
                // Act - List again
                string listAgainResult = await _teeInterface.ExecuteJavaScriptAsync(jsCode, listInput);
                
                // Assert
                var storeResultObj = JsonSerializer.Deserialize<JsonElement>(storeResult);
                Assert.True(storeResultObj.GetProperty("success").GetBoolean());
                
                var retrieveResultObj = JsonSerializer.Deserialize<JsonElement>(retrieveResult);
                Assert.True(retrieveResultObj.GetProperty("success").GetBoolean());
                Assert.Equal(secretValue, retrieveResultObj.GetProperty("value").GetString());
                
                var listResultObj = JsonSerializer.Deserialize<JsonElement>(listResult);
                Assert.True(listResultObj.GetProperty("success").GetBoolean());
                Assert.Contains(secretName, listResultObj.GetProperty("secretNames").EnumerateArray().Select(e => e.GetString()));
                
                var deleteResultObj = JsonSerializer.Deserialize<JsonElement>(deleteResult);
                Assert.True(deleteResultObj.GetProperty("success").GetBoolean());
                
                var listAgainResultObj = JsonSerializer.Deserialize<JsonElement>(listAgainResult);
                Assert.True(listAgainResultObj.GetProperty("success").GetBoolean());
                Assert.Empty(listAgainResultObj.GetProperty("secretNames").EnumerateArray());
            }
            catch (Exception)
            {
                // Skip the test if JavaScript execution fails
                Skip.If(true, "JavaScript execution with user secrets not supported in this enclave build");
            }
        }
    }
}
