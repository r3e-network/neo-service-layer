using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.JavaScriptExecution;
using NeoServiceLayer.Tee.Host.Occlum;
using Xunit;

namespace NeoServiceLayer.Occlum.Tests
{
    /// <summary>
    /// Tests for the OcclumJavaScriptExecution class.
    /// </summary>
    [Trait("Category", "Occlum")]
    public class OcclumJavaScriptExecutionTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OcclumJavaScriptExecutionTests> _logger;
        private readonly IOcclumManager _occlumManager;
        private readonly OcclumJavaScriptExecution _jsExecution;

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumJavaScriptExecutionTests"/> class.
        /// </summary>
        public OcclumJavaScriptExecutionTests()
        {
            // Create service collection
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add Occlum manager
            services.AddSingleton<IOcclumManager, OcclumManager>();

            // Build service provider
            _serviceProvider = services.BuildServiceProvider();

            // Get logger
            _logger = _serviceProvider.GetRequiredService<ILogger<OcclumJavaScriptExecutionTests>>();

            // Get Occlum manager
            _occlumManager = _serviceProvider.GetRequiredService<IOcclumManager>();

            // Create JavaScript execution
            _jsExecution = new OcclumJavaScriptExecution(_logger, _occlumManager);

            // Set simulation mode
            Environment.SetEnvironmentVariable("OCCLUM_SIMULATION", "1");
        }

        /// <summary>
        /// Tests that the OcclumJavaScriptExecution can be initialized.
        /// </summary>
        [Fact]
        public async Task InitializeAsync_ShouldSucceed()
        {
            // Act
            await _jsExecution.InitializeAsync();

            // Assert
            Assert.True(_occlumManager.IsOcclumSupportEnabled());
        }

        /// <summary>
        /// Tests that JavaScript code can be executed.
        /// </summary>
        [Fact]
        public async Task ExecuteJavaScriptAsync_ShouldSucceed()
        {
            // Arrange
            await _jsExecution.InitializeAsync();

            string code = @"
                function main(input, secrets, functionId, userId) {
                    return {
                        message: 'Hello, world!',
                        input: input,
                        functionId: functionId,
                        userId: userId
                    };
                }
            ";
            string input = @"{ ""name"": ""Test"" }";
            string secrets = @"{ ""apiKey"": ""test-api-key"" }";
            string functionId = "test-function";
            string userId = "test-user";

            // Act
            string result = await _jsExecution.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Hello, world!", result);
            Assert.Contains("Test", result);
            Assert.Contains("test-function", result);
            Assert.Contains("test-user", result);
        }

        /// <summary>
        /// Tests that JavaScript code with errors is handled correctly.
        /// </summary>
        [Fact]
        public async Task ExecuteJavaScriptAsync_WithError_ShouldReturnError()
        {
            // Arrange
            await _jsExecution.InitializeAsync();

            string code = @"
                function main(input, secrets, functionId, userId) {
                    throw new Error('Test error');
                }
            ";
            string input = @"{ ""name"": ""Test"" }";
            string secrets = @"{ ""apiKey"": ""test-api-key"" }";
            string functionId = "test-function";
            string userId = "test-user";

            // Act & Assert
            await Assert.ThrowsAsync<JavaScriptExecutionException>(() =>
                _jsExecution.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId));
        }

        /// <summary>
        /// Tests that JavaScript code with syntax errors is handled correctly.
        /// </summary>
        [Fact]
        public async Task ExecuteJavaScriptAsync_WithSyntaxError_ShouldReturnError()
        {
            // Arrange
            await _jsExecution.InitializeAsync();

            string code = @"
                function main(input, secrets, functionId, userId) {
                    return {
                        message: 'Hello, world!'
                    ;
                }
            ";
            string input = @"{ ""name"": ""Test"" }";
            string secrets = @"{ ""apiKey"": ""test-api-key"" }";
            string functionId = "test-function";
            string userId = "test-user";

            // Act & Assert
            await Assert.ThrowsAsync<JavaScriptExecutionException>(() =>
                _jsExecution.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId));
        }
    }
}
