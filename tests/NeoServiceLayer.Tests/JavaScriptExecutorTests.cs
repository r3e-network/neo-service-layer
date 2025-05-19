using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Core.Services;
using NeoServiceLayer.Tee.Host;
using NeoServiceLayer.Tee.Host.Interfaces;
using Xunit;

namespace NeoServiceLayer.Tests
{
    public class JavaScriptExecutorTests
    {
        private readonly Mock<ILogger<JavaScriptService>> _loggerMock;
        private readonly Mock<ITeeService> _teeServiceMock;
        private readonly JavaScriptService _service;

        public JavaScriptExecutorTests()
        {
            _loggerMock = new Mock<ILogger<JavaScriptService>>();
            _teeServiceMock = new Mock<ITeeService>();
            _service = new JavaScriptService(_loggerMock.Object, _teeServiceMock.Object);
        }

        [Fact]
        public async Task ExecuteJavaScript_ShouldReturnResult_WhenCodeIsValid()
        {
            // Arrange
            var code = @"
                function main(input) {
                    return { result: 'Hello, ' + input.name };
                }
            ";
            var input = new { name = "World" };
            var functionId = "test-function-1";
            var userId = "test-user-1";
            var expectedResult = "{ \"result\": \"Hello, World\" }";

            _teeServiceMock.Setup(x => x.ExecuteJavaScriptAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.ExecuteJavaScriptAsync(code, input, functionId, userId);

            // Assert
            Assert.Equal(expectedResult, result);
            _teeServiceMock.Verify(x => x.ExecuteJavaScriptAsync(
                code,
                It.IsAny<string>(),
                It.IsAny<string>(),
                functionId,
                userId), Times.Once);
        }

        [Fact]
        public async Task ExecuteJavaScript_ShouldThrowException_WhenTeeServiceFails()
        {
            // Arrange
            var code = "function main(input) { return input; }";
            var input = new { name = "World" };
            var functionId = "test-function-1";
            var userId = "test-user-1";

            _teeServiceMock.Setup(x => x.ExecuteJavaScriptAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("TEE service error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => 
                _service.ExecuteJavaScriptAsync(code, input, functionId, userId));
        }

        [Fact]
        public async Task InitializeJavaScriptExecutor_ShouldReturnTrue_WhenSuccessful()
        {
            // Arrange
            _teeServiceMock.Setup(x => x.InitializeJavaScriptExecutorAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.InitializeJavaScriptExecutorAsync();

            // Assert
            Assert.True(result);
            _teeServiceMock.Verify(x => x.InitializeJavaScriptExecutorAsync(), Times.Once);
        }

        [Fact]
        public async Task ExecuteJavaScriptCode_ShouldReturnResult_WhenCodeIsValid()
        {
            // Arrange
            var code = "console.log('Hello, World'); return 42;";
            var filename = "test.js";
            var expectedResult = "42";

            _teeServiceMock.Setup(x => x.ExecuteJavaScriptCodeAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.ExecuteJavaScriptCodeAsync(code, filename);

            // Assert
            Assert.Equal(expectedResult, result);
            _teeServiceMock.Verify(x => x.ExecuteJavaScriptCodeAsync(code, filename), Times.Once);
        }

        [Fact]
        public async Task ExecuteJavaScriptFunction_ShouldReturnResult_WhenFunctionIsValid()
        {
            // Arrange
            var functionName = "add";
            var args = new List<string> { "5", "7" };
            var expectedResult = "12";

            _teeServiceMock.Setup(x => x.ExecuteJavaScriptFunctionAsync(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.ExecuteJavaScriptFunctionAsync(functionName, args);

            // Assert
            Assert.Equal(expectedResult, result);
            _teeServiceMock.Verify(x => x.ExecuteJavaScriptFunctionAsync(functionName, args), Times.Once);
        }

        [Fact]
        public async Task CollectJavaScriptGarbage_ShouldReturnTrue_WhenSuccessful()
        {
            // Arrange
            _teeServiceMock.Setup(x => x.CollectJavaScriptGarbageAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.CollectJavaScriptGarbageAsync();

            // Assert
            Assert.True(result);
            _teeServiceMock.Verify(x => x.CollectJavaScriptGarbageAsync(), Times.Once);
        }

        [Fact]
        public async Task ShutdownJavaScriptExecutor_ShouldReturnTrue_WhenSuccessful()
        {
            // Arrange
            _teeServiceMock.Setup(x => x.ShutdownJavaScriptExecutorAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.ShutdownJavaScriptExecutorAsync();

            // Assert
            Assert.True(result);
            _teeServiceMock.Verify(x => x.ShutdownJavaScriptExecutorAsync(), Times.Once);
        }
    }
}
