using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Host;
using NeoServiceLayer.Tee.Host.Interfaces;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;

namespace NeoServiceLayer.Tests
{
    public class TeeServiceTests
    {
        private readonly Mock<ILogger<TeeService>> _loggerMock;
        private readonly Mock<ITeeClient> _teeClientMock;
        private readonly TeeService _service;

        public TeeServiceTests()
        {
            _loggerMock = new Mock<ILogger<TeeService>>();
            _teeClientMock = new Mock<ITeeClient>();
            _service = new TeeService(_loggerMock.Object, _teeClientMock.Object);
        }

        [Fact]
        public async Task InitializeAsync_ShouldReturnTrue_WhenClientInitializesSuccessfully()
        {
            // Arrange
            _teeClientMock.Setup(x => x.InitializeAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.InitializeAsync();

            // Assert
            Assert.True(result);
            _teeClientMock.Verify(x => x.InitializeAsync(), Times.Once);
        }

        [Fact]
        public async Task GetStatusAsync_ShouldReturnStatus_WhenClientReturnsStatus()
        {
            // Arrange
            var expectedStatus = "{\"status\":\"ok\"}";
            _teeClientMock.Setup(x => x.GetStatusAsync())
                .ReturnsAsync(expectedStatus);

            // Act
            var result = await _service.GetStatusAsync();

            // Assert
            Assert.Equal(expectedStatus, result);
            _teeClientMock.Verify(x => x.GetStatusAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateJavaScriptContextAsync_ShouldReturnContextId_WhenClientCreatesContext()
        {
            // Arrange
            ulong expectedContextId = 123;
            _teeClientMock.Setup(x => x.CreateJavaScriptContextAsync())
                .ReturnsAsync(expectedContextId);

            // Act
            var result = await _service.CreateJavaScriptContextAsync();

            // Assert
            Assert.Equal(expectedContextId, result);
            _teeClientMock.Verify(x => x.CreateJavaScriptContextAsync(), Times.Once);
        }

        [Fact]
        public async Task DestroyJavaScriptContextAsync_ShouldReturnTrue_WhenClientDestroysContext()
        {
            // Arrange
            ulong contextId = 123;
            _teeClientMock.Setup(x => x.DestroyJavaScriptContextAsync(contextId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DestroyJavaScriptContextAsync(contextId);

            // Assert
            Assert.True(result);
            _teeClientMock.Verify(x => x.DestroyJavaScriptContextAsync(contextId), Times.Once);
        }

        [Fact]
        public async Task ExecuteJavaScriptAsync_ShouldReturnResult_WhenClientExecutesJavaScript()
        {
            // Arrange
            var code = "function main(input) { return input; }";
            var input = "{\"name\":\"World\"}";
            var secrets = "{}";
            var functionId = "test-function-1";
            var userId = "test-user-1";
            var expectedResult = "{\"result\":\"Hello, World\"}";

            _teeClientMock.Setup(x => x.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId);

            // Assert
            Assert.Equal(expectedResult, result);
            _teeClientMock.Verify(x => x.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId), Times.Once);
        }

        [Fact]
        public async Task InitializeJavaScriptExecutorAsync_ShouldReturnTrue_WhenClientInitializesExecutor()
        {
            // Arrange
            var response = "{\"success\":true}";
            _teeClientMock.Setup(x => x.SendMessageAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _service.InitializeJavaScriptExecutorAsync();

            // Assert
            Assert.True(result);
            _teeClientMock.Verify(x => x.SendMessageAsync(It.Is<string>(s => 
                JsonSerializer.Deserialize<JsonElement>(s).GetProperty("message_type").GetInt32() == (int)EnclaveMessageType.INITIALIZE_JS_EXECUTOR)), 
                Times.Once);
        }

        [Fact]
        public async Task ExecuteJavaScriptCodeAsync_ShouldReturnResult_WhenClientExecutesCode()
        {
            // Arrange
            var code = "console.log('Hello, World'); return 42;";
            var filename = "test.js";
            var expectedResult = "{\"result\":42}";

            _teeClientMock.Setup(x => x.SendMessageAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.ExecuteJavaScriptCodeAsync(code, filename);

            // Assert
            Assert.Equal(expectedResult, result);
            _teeClientMock.Verify(x => x.SendMessageAsync(It.Is<string>(s => 
                JsonSerializer.Deserialize<JsonElement>(s).GetProperty("message_type").GetInt32() == (int)EnclaveMessageType.EXECUTE_JS_CODE_NEW &&
                JsonSerializer.Deserialize<JsonElement>(s).GetProperty("code").GetString() == code &&
                JsonSerializer.Deserialize<JsonElement>(s).GetProperty("filename").GetString() == filename)), 
                Times.Once);
        }

        [Fact]
        public async Task ExecuteJavaScriptFunctionAsync_ShouldReturnResult_WhenClientExecutesFunction()
        {
            // Arrange
            var functionName = "add";
            var args = new List<string> { "5", "7" };
            var expectedResult = "{\"result\":12}";

            _teeClientMock.Setup(x => x.SendMessageAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.ExecuteJavaScriptFunctionAsync(functionName, args);

            // Assert
            Assert.Equal(expectedResult, result);
            _teeClientMock.Verify(x => x.SendMessageAsync(It.Is<string>(s => 
                JsonSerializer.Deserialize<JsonElement>(s).GetProperty("message_type").GetInt32() == (int)EnclaveMessageType.EXECUTE_JS_FUNCTION &&
                JsonSerializer.Deserialize<JsonElement>(s).GetProperty("function_name").GetString() == functionName)), 
                Times.Once);
        }

        [Fact]
        public async Task CollectJavaScriptGarbageAsync_ShouldReturnTrue_WhenClientCollectsGarbage()
        {
            // Arrange
            var response = "{\"success\":true}";
            _teeClientMock.Setup(x => x.SendMessageAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _service.CollectJavaScriptGarbageAsync();

            // Assert
            Assert.True(result);
            _teeClientMock.Verify(x => x.SendMessageAsync(It.Is<string>(s => 
                JsonSerializer.Deserialize<JsonElement>(s).GetProperty("message_type").GetInt32() == (int)EnclaveMessageType.COLLECT_JS_GARBAGE)), 
                Times.Once);
        }

        [Fact]
        public async Task ShutdownJavaScriptExecutorAsync_ShouldReturnTrue_WhenClientShutdownsExecutor()
        {
            // Arrange
            var response = "{\"success\":true}";
            _teeClientMock.Setup(x => x.SendMessageAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _service.ShutdownJavaScriptExecutorAsync();

            // Assert
            Assert.True(result);
            _teeClientMock.Verify(x => x.SendMessageAsync(It.Is<string>(s => 
                JsonSerializer.Deserialize<JsonElement>(s).GetProperty("message_type").GetInt32() == (int)EnclaveMessageType.SHUTDOWN_JS_EXECUTOR)), 
                Times.Once);
        }
    }
}
