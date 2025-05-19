using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Api.Models;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Tee.Host;
using NeoServiceLayer.Tee.Host.Occlum;
using Xunit;

namespace NeoServiceLayer.Api.Tests
{
    /// <summary>
    /// Integration tests for the API Gateway with Occlum function execution.
    /// </summary>
    [Trait("Category", "Integration")]
    [Trait("Category", "Occlum")]
    public class OcclumFunctionExecutionTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly ILogger<OcclumFunctionExecutionTests> _logger;
        private readonly bool _skipTests;

        /// <summary>
        /// Initializes a new instance of the OcclumFunctionExecutionTests class.
        /// </summary>
        /// <param name="factory">The web application factory.</param>
        public OcclumFunctionExecutionTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Configure services for testing
                    services.Configure<TeeEnclaveSettings>(options =>
                    {
                        options.SimulationMode = true;
                        options.OcclumSupport = true;
                        options.OcclumInstanceDir = "/occlum_instance";
                        options.OcclumLogLevel = "info";
                    });
                });
            });

            _client = _factory.CreateClient();

            // Get logger
            var loggerFactory = _factory.Services.GetRequiredService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger<OcclumFunctionExecutionTests>();

            // Check if Occlum support is enabled
            try
            {
                var occlumManager = _factory.Services.GetRequiredService<IOcclumManager>();
                if (!occlumManager.IsOcclumSupportEnabled())
                {
                    _logger.LogWarning("Occlum support is not enabled in the enclave. Tests will be skipped.");
                    _skipTests = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if Occlum support is enabled. Tests will be skipped.");
                _skipTests = true;
            }
        }

        [Fact]
        public async Task ExecuteJavaScript_SimpleFunction_ReturnsCorrectResult()
        {
            Skip.If(_skipTests, "Skipping test because Occlum support is not enabled");

            // Arrange
            var request = new JavaScriptExecutionRequest
            {
                Code = @"
                    function main(input) {
                        return { result: input.value * 2 };
                    }
                ",
                Input = @"{ ""value"": 42 }",
                Secrets = @"{ ""API_KEY"": ""test_key"" }",
                FunctionId = "test_function",
                UserId = "test_user"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/functions/execute", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<JavaScriptExecutionResponse>>(
                responseString,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.NotNull(apiResponse.Data.Result);

            var resultObj = JsonSerializer.Deserialize<JsonElement>(apiResponse.Data.Result);
            Assert.Equal(84, resultObj.GetProperty("result").GetInt32());
        }

        [Fact]
        public async Task ExecuteJavaScript_WithFileSystem_CanAccessFileSystem()
        {
            Skip.If(_skipTests, "Skipping test because Occlum support is not enabled");

            // Arrange
            var request = new JavaScriptExecutionRequest
            {
                Code = @"
                    const fs = require('fs');
                    
                    function main(input) {
                        // Write to a file
                        fs.writeFileSync('/tmp/test.txt', 'Hello, Occlum!');
                        
                        // Read from the file
                        const content = fs.readFileSync('/tmp/test.txt', 'utf8');
                        
                        return { 
                            result: input.value * 2,
                            fileContent: content,
                            fileExists: fs.existsSync('/tmp/test.txt')
                        };
                    }
                ",
                Input = @"{ ""value"": 42 }",
                Secrets = @"{ ""API_KEY"": ""test_key"" }",
                FunctionId = "test_function_filesystem",
                UserId = "test_user"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/functions/execute", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<JavaScriptExecutionResponse>>(
                responseString,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.NotNull(apiResponse.Data.Result);

            var resultObj = JsonSerializer.Deserialize<JsonElement>(apiResponse.Data.Result);
            Assert.Equal(84, resultObj.GetProperty("result").GetInt32());
            Assert.Equal("Hello, Occlum!", resultObj.GetProperty("fileContent").GetString());
            Assert.True(resultObj.GetProperty("fileExists").GetBoolean());
        }

        [Fact]
        public async Task ExecuteJavaScript_WithError_ReturnsErrorResponse()
        {
            Skip.If(_skipTests, "Skipping test because Occlum support is not enabled");

            // Arrange
            var request = new JavaScriptExecutionRequest
            {
                Code = @"
                    function main(input) {
                        throw new Error('Test error message');
                    }
                ",
                Input = @"{ ""value"": 42 }",
                Secrets = @"{ ""API_KEY"": ""test_key"" }",
                FunctionId = "test_function_error",
                UserId = "test_user"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/functions/execute", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(
                responseString,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(apiResponse);
            Assert.False(apiResponse.Success);
            Assert.NotNull(apiResponse.Error);
            Assert.Contains("Test error message", apiResponse.Error.Message);
        }
    }

    /// <summary>
    /// Request model for JavaScript execution.
    /// </summary>
    public class JavaScriptExecutionRequest
    {
        /// <summary>
        /// Gets or sets the JavaScript code to execute.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the input data for the JavaScript code.
        /// </summary>
        public string Input { get; set; }

        /// <summary>
        /// Gets or sets the secrets for the JavaScript code.
        /// </summary>
        public string Secrets { get; set; }

        /// <summary>
        /// Gets or sets the function ID.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string UserId { get; set; }
    }

    /// <summary>
    /// Response model for JavaScript execution.
    /// </summary>
    public class JavaScriptExecutionResponse
    {
        /// <summary>
        /// Gets or sets the result of the JavaScript execution.
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the gas used.
        /// </summary>
        public long GasUsed { get; set; }
    }
}
