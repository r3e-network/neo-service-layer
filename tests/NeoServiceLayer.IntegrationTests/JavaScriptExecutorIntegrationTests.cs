using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Tee.Host.Interfaces;
using Xunit;

namespace NeoServiceLayer.IntegrationTests
{
    [Collection("Integration Tests")]
    public class JavaScriptExecutorIntegrationTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture _fixture;
        private readonly ILogger<JavaScriptExecutorIntegrationTests> _logger;
        private readonly IJavaScriptService _jsService;
        private readonly ITeeService _teeService;

        public JavaScriptExecutorIntegrationTests(TestFixture fixture)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            _logger = _fixture.ServiceProvider.GetRequiredService<ILogger<JavaScriptExecutorIntegrationTests>>();
            _jsService = _fixture.ServiceProvider.GetRequiredService<IJavaScriptService>();
            _teeService = _fixture.ServiceProvider.GetRequiredService<ITeeService>();
        }

        [Fact]
        public async Task InitializeJavaScriptExecutor_ShouldSucceed()
        {
            // Act
            var result = await _jsService.InitializeJavaScriptExecutorAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExecuteJavaScriptCode_ShouldReturnResult()
        {
            // Arrange
            await _jsService.InitializeJavaScriptExecutorAsync();
            var code = "console.log('Hello, World'); return 42;";
            var filename = "test.js";

            // Act
            var result = await _jsService.ExecuteJavaScriptCodeAsync(code, filename);

            // Assert
            Assert.Equal("42", result);
        }

        [Fact]
        public async Task ExecuteJavaScriptFunction_ShouldReturnResult()
        {
            // Arrange
            await _jsService.InitializeJavaScriptExecutorAsync();
            
            // First, define the function
            var defineCode = "function add(a, b) { return parseInt(a) + parseInt(b); }";
            await _jsService.ExecuteJavaScriptCodeAsync(defineCode, "define.js");
            
            // Then, execute the function
            var functionName = "add";
            var args = new List<string> { "5", "7" };

            // Act
            var result = await _jsService.ExecuteJavaScriptFunctionAsync(functionName, args);

            // Assert
            Assert.Equal("12", result);
        }

        [Fact]
        public async Task ExecuteJavaScriptWithStorage_ShouldPersistData()
        {
            // Arrange
            await _jsService.InitializeJavaScriptExecutorAsync();
            
            // First, store a value
            var storeCode = "storage.set('test-key', 'test-value'); return 'stored';";
            await _jsService.ExecuteJavaScriptCodeAsync(storeCode, "store.js");
            
            // Then, retrieve the value
            var retrieveCode = "return storage.get('test-key');";

            // Act
            var result = await _jsService.ExecuteJavaScriptCodeAsync(retrieveCode, "retrieve.js");

            // Assert
            Assert.Equal("test-value", result);
        }

        [Fact]
        public async Task ExecuteJavaScriptWithSecrets_ShouldPersistSecrets()
        {
            // Arrange
            await _jsService.InitializeJavaScriptExecutorAsync();
            
            // First, store a secret
            var storeCode = "SECRETS.set('test-secret-key', 'test-secret-value'); return 'stored';";
            await _jsService.ExecuteJavaScriptCodeAsync(storeCode, "store-secret.js");
            
            // Then, retrieve the secret
            var retrieveCode = "return SECRETS.get('test-secret-key');";

            // Act
            var result = await _jsService.ExecuteJavaScriptCodeAsync(retrieveCode, "retrieve-secret.js");

            // Assert
            Assert.Equal("test-secret-value", result);
        }

        [Fact]
        public async Task ExecuteJavaScriptWithGas_ShouldTrackGasUsage()
        {
            // Arrange
            await _jsService.InitializeJavaScriptExecutorAsync();
            
            // Get initial gas
            var getGasCode = "return gas.get();";
            var initialGas = await _jsService.ExecuteJavaScriptCodeAsync(getGasCode, "get-gas.js");
            
            // Use some gas
            var useGasCode = "gas.use(100); return gas.get();";

            // Act
            var remainingGas = await _jsService.ExecuteJavaScriptCodeAsync(useGasCode, "use-gas.js");

            // Assert
            Assert.True(long.Parse(initialGas) > long.Parse(remainingGas));
            Assert.True(long.Parse(initialGas) - long.Parse(remainingGas) >= 100);
        }

        [Fact]
        public async Task CollectJavaScriptGarbage_ShouldSucceed()
        {
            // Arrange
            await _jsService.InitializeJavaScriptExecutorAsync();

            // Act
            var result = await _jsService.CollectJavaScriptGarbageAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ShutdownJavaScriptExecutor_ShouldSucceed()
        {
            // Arrange
            await _jsService.InitializeJavaScriptExecutorAsync();

            // Act
            var result = await _jsService.ShutdownJavaScriptExecutorAsync();

            // Assert
            Assert.True(result);
        }
    }
}
