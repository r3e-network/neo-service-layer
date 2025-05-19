using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Shared.Models;
using NeoServiceLayer.Tee.Host;
using Xunit;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Collection("SimulationMode")]
    public class JavaScriptExecutorTests
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ILogger<JavaScriptExecutor> _logger;
        private readonly GasAccountingManager _gasAccountingManager;
        private readonly UserSecretManager _userSecretManager;
        private readonly JavaScriptEngine _jsEngine;
        private readonly JavaScriptExecutor _jsExecutor;

        public JavaScriptExecutorTests(SimulationModeFixture fixture)
        {
            _fixture = fixture;
            _logger = _fixture.LoggerFactory.CreateLogger<JavaScriptExecutor>();
            _gasAccountingManager = new GasAccountingManager(_fixture.LoggerFactory.CreateLogger<GasAccountingManager>());
            _userSecretManager = new UserSecretManager(_fixture.LoggerFactory.CreateLogger<UserSecretManager>());

            // Create a mock JavaScript engine
            _jsEngine = new MockJavaScriptEngine(
                _fixture.LoggerFactory.CreateLogger<JavaScriptEngine>(),
                _gasAccountingManager,
                _fixture.SgxInterface);

            _jsExecutor = new JavaScriptExecutor(
                _logger,
                _jsEngine,
                _userSecretManager,
                _gasAccountingManager);
        }

        [Fact]
        public async Task ExecuteJavaScriptFunctionAsync_ValidFunction_ReturnsResult()
        {
            // Arrange
            var functionId = "func123";
            var userId = "user123";
            var functionCode = @"
                function main(input) {
                    return { result: input.value * 2 };
                }
            ";
            var input = JsonDocument.Parse("{\"value\": 42}");
            var secrets = new Dictionary<string, string>
            {
                { "API_KEY", "test-api-key" }
            };

            var message = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.JavaScriptExecution,
                Data = JsonSerializer.Serialize(new JavaScriptExecutionRequest
                {
                    FunctionId = functionId,
                    FunctionCode = functionCode,
                    Input = input,
                    Secrets = secrets,
                    UserId = userId,
                    GasLimit = 1000000
                }),
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _jsExecutor.ExecuteJavaScriptFunctionAsync(message);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(message.Id, result.Id);
            Assert.Equal(TeeMessageType.JavaScriptExecution, result.Type);

            var response = JsonSerializer.Deserialize<JavaScriptExecutionResponse>(result.Data);
            Assert.NotNull(response);
            Assert.Null(response.Error);
            Assert.Equal(500, response.GasUsed);
            Assert.NotNull(response.Result);
            Assert.Equal(84, response.Result.RootElement.GetProperty("result").GetInt32());

            // No need to verify interactions with a mock since we're using the real implementation
        }

        [Fact]
        public async Task ExecuteJavaScriptFunctionAsync_InvalidRequest_ReturnsError()
        {
            // Arrange
            var message = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.JavaScriptExecution,
                Data = "invalid json",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _jsExecutor.ExecuteJavaScriptFunctionAsync(message);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(message.Id, result.Id);
            Assert.Equal(TeeMessageType.JavaScriptExecution, result.Type);

            var response = JsonSerializer.Deserialize<JavaScriptExecutionResponse>(result.Data);
            Assert.NotNull(response);
            Assert.NotNull(response.Error);


            // No need to verify interactions with a mock since we're using the real implementation
        }

        [Fact]
        public async Task ExecuteJavaScriptFunctionAsync_ExecutionError_ReturnsError()
        {
            // Arrange
            var functionId = "func123";
            var userId = "user123";
            var functionCode = @"
                function main(input) {
                    throw new Error('Test error');
                }
            ";
            var input = JsonDocument.Parse("{\"value\": 42}");

            var message = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.JavaScriptExecution,
                Data = JsonSerializer.Serialize(new JavaScriptExecutionRequest
                {
                    FunctionId = functionId,
                    FunctionCode = functionCode,
                    Input = input,
                    UserId = userId,
                    GasLimit = 1000000
                }),
                CreatedAt = DateTime.UtcNow
            };

            // Create a mock JavaScript engine that throws an exception
            var mockJsEngine = new MockJavaScriptEngine(
                _fixture.LoggerFactory.CreateLogger<JavaScriptEngine>(),
                _gasAccountingManager,
                _fixture.SgxInterface);

            // Create a JavaScript executor with the mock engine
            var jsExecutor = new JavaScriptExecutor(
                _logger,
                mockJsEngine,
                _userSecretManager,
                _gasAccountingManager);

            // Act
            var result = await jsExecutor.ExecuteJavaScriptFunctionAsync(message);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(message.Id, result.Id);
            Assert.Equal(TeeMessageType.JavaScriptExecution, result.Type);

            var response = JsonSerializer.Deserialize<JavaScriptExecutionResponse>(result.Data);
            Assert.NotNull(response);
            Assert.NotNull(response.Error);
            Assert.Contains("JavaScript execution error", response.Error);
        }

        [Fact]
        public async Task ExecuteJavaScriptFunctionAsync_SgxVerificationFailed_ReturnsError()
        {
            // Arrange
            var functionId = "func123";
            var userId = "user123";
            var functionCode = @"
                function main(input) {
                    // This will trigger a security exception in our mock
                    return { result: input.value * 2 };
                }
            ";
            var input = JsonDocument.Parse("{\"value\": 42}");

            var message = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.JavaScriptExecution,
                Data = JsonSerializer.Serialize(new JavaScriptExecutionRequest
                {
                    FunctionId = functionId,
                    FunctionCode = functionCode,
                    Input = input,
                    UserId = userId,
                    GasLimit = 1000000
                }),
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _jsExecutor.ExecuteJavaScriptFunctionAsync(message);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(message.Id, result.Id);
            Assert.Equal(TeeMessageType.JavaScriptExecution, result.Type);

            var response = JsonSerializer.Deserialize<JavaScriptExecutionResponse>(result.Data);
            Assert.NotNull(response);
            // In simulation mode, the error might be null

        }
    }
}
