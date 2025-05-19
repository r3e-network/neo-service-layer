using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Data.Repositories;
using NeoServiceLayer.Infrastructure.Services;
using NeoServiceLayer.Shared.Models;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace NeoServiceLayer.Infrastructure.Tests.Services
{
    public class JavaScriptExecutionServiceTests
    {
        private readonly Mock<NeoServiceLayer.Tee.Host.Services.ITeeHostService> _mockTeeHostService;
        private readonly Mock<IUserSecretService> _mockUserSecretService;
        private readonly Mock<IGasAccountingService> _mockGasAccountingService;
        private readonly Mock<NeoServiceLayer.Infrastructure.Data.Repositories.IRepository<JavaScriptFunction, string>> _mockFunctionRepository;
        private readonly Mock<ILogger<JavaScriptExecutionService>> _mockLogger;
        private readonly JavaScriptExecutionService _javaScriptExecutionService;

        public JavaScriptExecutionServiceTests()
        {
            _mockTeeHostService = new Mock<NeoServiceLayer.Tee.Host.Services.ITeeHostService>();
            _mockUserSecretService = new Mock<IUserSecretService>();
            _mockGasAccountingService = new Mock<IGasAccountingService>();
            _mockFunctionRepository = new Mock<NeoServiceLayer.Infrastructure.Data.Repositories.IRepository<JavaScriptFunction, string>>();
            _mockLogger = new Mock<ILogger<JavaScriptExecutionService>>();

            // Create adapters that implement the Core interfaces
            var teeHostServiceAdapter = new Mocks.TeeHostServiceAdapter(_mockTeeHostService.Object);
            var repositoryAdapter = new Mocks.RepositoryAdapter<JavaScriptFunction, string>(_mockFunctionRepository.Object);

            _javaScriptExecutionService = new JavaScriptExecutionService(
                teeHostServiceAdapter,
                _mockUserSecretService.Object,
                _mockGasAccountingService.Object,
                repositoryAdapter,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CreateFunctionAsync_ValidFunction_ReturnsCreatedFunction()
        {
            // Arrange
            var function = new JavaScriptFunction
            {
                Name = "TestFunction",
                Description = "Test function description",
                Code = "function test() { return 42; }",
                RequiredSecrets = new List<string> { "API_KEY" },
                GasLimit = 1000000,
                OwnerId = "user123"
            };

            _mockFunctionRepository.Setup(x => x.AddAsync(It.IsAny<JavaScriptFunction>()))
                .ReturnsAsync((JavaScriptFunction f) => f);

            // Act
            var result = await _javaScriptExecutionService.CreateFunctionAsync(function);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(function.Name, result.Name);
            Assert.Equal(function.Description, result.Description);
            Assert.Equal(function.Code, result.Code);
            Assert.Equal(function.RequiredSecrets, result.RequiredSecrets);
            Assert.Equal(function.GasLimit, result.GasLimit);
            Assert.Equal(function.OwnerId, result.OwnerId);
            Assert.NotNull(result.Id);
            Assert.Equal(JavaScriptFunctionStatus.Active, result.Status);
            Assert.True(result.CreatedAt > DateTime.MinValue);
            Assert.True(result.UpdatedAt > DateTime.MinValue);

            _mockFunctionRepository.Verify(x => x.AddAsync(It.IsAny<JavaScriptFunction>()), Times.Once);
        }

        [Fact]
        public async Task GetFunctionAsync_ExistingFunction_ReturnsFunction()
        {
            // Arrange
            var functionId = "func123";
            var function = new JavaScriptFunction
            {
                Id = functionId,
                Name = "TestFunction",
                Description = "Test function description",
                Code = "function test() { return 42; }",
                RequiredSecrets = new List<string> { "API_KEY" },
                GasLimit = 1000000,
                OwnerId = "user123",
                Status = JavaScriptFunctionStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockFunctionRepository.Setup(x => x.GetByIdAsync(functionId))
                .ReturnsAsync(function);

            // Act
            var result = await _javaScriptExecutionService.GetFunctionAsync(functionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(function.Id, result.Id);
            Assert.Equal(function.Name, result.Name);
            Assert.Equal(function.Description, result.Description);
            Assert.Equal(function.Code, result.Code);
            Assert.Equal(function.RequiredSecrets, result.RequiredSecrets);
            Assert.Equal(function.GasLimit, result.GasLimit);
            Assert.Equal(function.OwnerId, result.OwnerId);
            Assert.Equal(function.Status, result.Status);
            Assert.Equal(function.CreatedAt, result.CreatedAt);
            Assert.Equal(function.UpdatedAt, result.UpdatedAt);

            _mockFunctionRepository.Verify(x => x.GetByIdAsync(functionId), Times.Once);
        }

        [Fact]
        public async Task ExecuteFunctionAsync_ValidFunction_ReturnsResult()
        {
            // Arrange
            var functionId = "func123";
            var userId = "user123";
            var input = JsonDocument.Parse("{\"value\": 42}");
            var function = new JavaScriptFunction
            {
                Id = functionId,
                Name = "TestFunction",
                Description = "Test function description",
                Code = "function test(input) { return input.value * 2; }",
                RequiredSecrets = new List<string> { "API_KEY" },
                GasLimit = 1000000,
                OwnerId = userId,
                Status = JavaScriptFunctionStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var secrets = new Dictionary<string, string>
            {
                { "API_KEY", "test-api-key" }
            };

            var teeResponse = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.JavaScriptExecution,
                Data = "{\"result\": 84, \"gas_used\": 500000}",
                CreatedAt = DateTime.UtcNow
            };

            _mockFunctionRepository.Setup(x => x.GetByIdAsync(functionId))
                .ReturnsAsync(function);

            _mockGasAccountingService.Setup(x => x.HasEnoughGasAsync(userId, function.GasLimit))
                .ReturnsAsync(true);

            _mockUserSecretService.Setup(x => x.GetSecretValuesByNamesAsync(function.RequiredSecrets, userId))
                .ReturnsAsync(secrets);

            _mockTeeHostService.Setup(x => x.SendMessageAsync(It.IsAny<TeeMessage>()))
                .ReturnsAsync(teeResponse);

            _mockGasAccountingService.Setup(x => x.RecordGasUsageAsync(userId, functionId, It.IsAny<string>(), 500000))
                .ReturnsAsync(new GasAccounting());

            // Act
            var result = await _javaScriptExecutionService.ExecuteFunctionAsync(functionId, input, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(84, result.RootElement.GetProperty("result").GetInt32());
            Assert.Equal(500000, result.RootElement.GetProperty("gas_used").GetInt64());

            _mockFunctionRepository.Verify(x => x.GetByIdAsync(functionId), Times.Once);
            _mockGasAccountingService.Verify(x => x.HasEnoughGasAsync(userId, function.GasLimit), Times.Once);
            _mockUserSecretService.Verify(x => x.GetSecretValuesByNamesAsync(function.RequiredSecrets, userId), Times.Once);
            _mockTeeHostService.Verify(x => x.SendMessageAsync(It.IsAny<TeeMessage>()), Times.Once);
            _mockGasAccountingService.Verify(x => x.RecordGasUsageAsync(userId, functionId, It.IsAny<string>(), 500000), Times.Once);
        }
    }
}
