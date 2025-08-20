using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Automation;
using NeoServiceLayer.Services.Automation.Models;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.TestInfrastructure;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using FluentAssertions;


namespace NeoServiceLayer.Services.Automation.Tests;

public class AutomationServiceTests : TestBase
{
    private readonly Mock<ILogger<AutomationService>> _loggerMock;
    private readonly Mock<IEnclaveManager> _enclaveManagerMock;
    private readonly AutomationService _service;

    public AutomationServiceTests()
    {
        _loggerMock = new Mock<ILogger<AutomationService>>();
        _enclaveManagerMock = new Mock<IEnclaveManager>();
        _enclaveManagerMock.Setup(x => x.IsInitialized).Returns(true);
        _service = new AutomationService(_loggerMock.Object, _enclaveManagerMock.Object);

        // Initialize the service synchronously for tests
        _service.InitializeAsync().GetAwaiter().GetResult();
        _service.StartAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act & Assert
        _service.Should().NotBeNull();
        _service.Name.Should().Be("AutomationService");
        _service.Description.Should().Be("Smart contract automation and scheduling service");
        _service.Version.Should().Be("1.0.0");
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task CreateAutomationAsync_WithValidRequest_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange
        var request = new CreateAutomationRequest
        {
            Name = "Daily Balance Check",
            Description = "Check account balance daily at 9 AM",
            TriggerType = AutomationTriggerType.Schedule,
            TriggerConfiguration = "{\"cron\": \"0 9 * * *\"}",
            ActionType = AutomationActionType.SmartContract,
            ActionConfiguration = "{\"contractAddress\": \"0x1234\", \"method\": \"checkBalance\"}",
            IsActive = true
        };

        // Act
        var result = await _service.CreateAutomationAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AutomationId.Should().NotBeNullOrEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Fact]
    public async Task CreateAutomationAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.CreateAutomationAsync(null!, BlockchainType.NeoN3));
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task UpdateAutomationAsync_WithValidRequest_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange - First create an automation
        var createRequest = new CreateAutomationRequest
        {
            Name = "Test Automation",
            Description = "Test automation for updating",
            TriggerType = AutomationTriggerType.Schedule,
            TriggerConfiguration = "{\"cron\": \"0 0 * * *\"}",
            ActionConfiguration = "{\"type\": \"Transfer\", \"amount\": 100}"
        };
        var createResult = await _service.CreateAutomationAsync(createRequest, blockchainType);
        var automationId = createResult.AutomationId;

        var request = new UpdateAutomationRequest
        {
            AutomationId = automationId,
            Name = "Updated Automation",
            IsActive = false,
            TriggerConfiguration = "{\"cron\": \"0 10 * * *\"}"
        };

        // Act
        var result = await _service.UpdateAutomationAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task DeleteAutomationAsync_WithValidId_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange - First create an automation
        var createRequest = new CreateAutomationRequest
        {
            Name = "Test Automation",
            Description = "Test automation for deletion",
            TriggerType = AutomationTriggerType.Schedule,
            TriggerConfiguration = "{\"cron\": \"0 0 * * *\"}",
            ActionConfiguration = "{\"type\": \"Transfer\", \"amount\": 100}"
        };
        var createResult = await _service.CreateAutomationAsync(createRequest, blockchainType);
        var automationId = createResult.AutomationId;

        // Act
        var result = await _service.DeleteAutomationAsync(automationId, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task GetAutomationsAsync_ShouldReturnAutomationList(BlockchainType blockchainType)
    {
        // Arrange
        var filter = new AutomationFilter
        {
            IsActive = true,
            TriggerType = AutomationTriggerType.Schedule
        };

        // Act
        var result = await _service.GetAutomationsAsync(filter, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEnumerable<AutomationInfo>>();
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task GetAutomationAsync_WithValidId_ShouldReturnAutomation(BlockchainType blockchainType)
    {
        // Arrange - First create an automation
        var createRequest = new CreateAutomationRequest
        {
            Name = "Test Automation",
            Description = "Test automation for retrieval",
            TriggerType = AutomationTriggerType.Schedule,
            TriggerConfiguration = "{\"cron\": \"0 0 * * *\"}",
            ActionConfiguration = "{\"type\": \"Transfer\", \"amount\": 100}"
        };
        var createResult = await _service.CreateAutomationAsync(createRequest, blockchainType);
        var automationId = createResult.AutomationId;

        // Act
        var result = await _service.GetAutomationAsync(automationId, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.AutomationId.Should().Be(automationId);
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task ExecuteAutomationAsync_WithValidId_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange - First create an automation
        var createRequest = new CreateAutomationRequest
        {
            Name = "Test Automation",
            Description = "Test automation for execution",
            TriggerType = AutomationTriggerType.Schedule,
            TriggerConfiguration = "{\"cron\": \"0 0 * * *\"}",
            ActionConfiguration = "{\"type\": \"Transfer\", \"amount\": 100}"
        };
        var createResult = await _service.CreateAutomationAsync(createRequest, blockchainType);
        var automationId = createResult.AutomationId;

        var context = new NeoServiceLayer.Services.Automation.Models.ExecutionContext
        {
            UserId = "test-user",
            Parameters = new Dictionary<string, object> { { "amount", 100 } }
        };

        // Act
        var result = await _service.ExecuteAutomationAsync(automationId, context, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ExecutionId.Should().NotBeNullOrEmpty();
        result.ExecutedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task GetExecutionHistoryAsync_WithValidRequest_ShouldReturnHistory(BlockchainType blockchainType)
    {
        // Arrange - First create an automation
        var createRequest = new CreateAutomationRequest
        {
            Name = "Test Automation",
            Description = "Test automation for execution history",
            TriggerType = AutomationTriggerType.Schedule,
            TriggerConfiguration = "{\"cron\": \"0 0 * * *\"}",
            ActionConfiguration = "{\"type\": \"Transfer\", \"amount\": 100}"
        };
        var createResult = await _service.CreateAutomationAsync(createRequest, blockchainType);
        var automationId = createResult.AutomationId;

        var request = new ExecutionHistoryRequest
        {
            AutomationId = automationId,
            FromDate = DateTime.UtcNow.AddDays(-7),
            ToDate = DateTime.UtcNow,
            PageSize = 20,
            PageIndex = 0
        };

        // Act
        var result = await _service.GetExecutionHistoryAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Executions.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterOrEqualTo(0);
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task PauseAutomationAsync_WithValidId_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange - First create an automation
        var createRequest = new CreateAutomationRequest
        {
            Name = "Test Automation",
            Description = "Test automation for pausing",
            TriggerType = AutomationTriggerType.Schedule,
            TriggerConfiguration = "{\"cron\": \"0 12 * * *\"}",
            ActionType = AutomationActionType.SmartContract,
            ActionConfiguration = "{\"contractAddress\": \"0x5678\", \"method\": \"testMethod\"}",
            IsActive = true
        };

        var createResult = await _service.CreateAutomationAsync(createRequest, blockchainType);
        var automationId = createResult.AutomationId;

        // Act
        var result = await _service.PauseAutomationAsync(automationId, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task ResumeAutomationAsync_WithValidId_ShouldReturnSuccess(BlockchainType blockchainType)
    {
        // Arrange - First create an automation and pause it
        var createRequest = new CreateAutomationRequest
        {
            Name = "Test Automation",
            Description = "Test automation for resuming",
            TriggerType = AutomationTriggerType.Schedule,
            TriggerConfiguration = "{\"cron\": \"0 0 * * *\"}",
            ActionConfiguration = "{\"type\": \"Transfer\", \"amount\": 100}",
            IsActive = true // Make sure it's active so it can be paused
        };
        var createResult = await _service.CreateAutomationAsync(createRequest, blockchainType);
        var automationId = createResult.AutomationId;

        // Pause the automation first so we can resume it
        await _service.PauseAutomationAsync(automationId, blockchainType);

        // Act
        var result = await _service.ResumeAutomationAsync(automationId, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task ValidateAutomationAsync_WithValidConfiguration_ShouldReturnValid(BlockchainType blockchainType)
    {
        // Arrange
        var request = new ValidationRequest
        {
            TriggerType = AutomationTriggerType.Schedule,
            TriggerConfiguration = "{\"cron\": \"0 9 * * *\"}",
            ActionType = AutomationActionType.SmartContract,
            ActionConfiguration = "{\"contractAddress\": \"0x1234\", \"method\": \"checkBalance\"}"
        };

        // Act
        var result = await _service.ValidateAutomationAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ValidationErrors.Should().BeEmpty();
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Fact]
    public async Task Service_ShouldInitializeAndStartSuccessfully()
    {
        // Act
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Assert
        _service.IsEnclaveInitialized.Should().BeTrue();
        _service.IsRunning.Should().BeTrue();
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Fact]
    public async Task Service_ShouldStopSuccessfully()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        await _service.StopAsync();

        // Assert
        _service.IsRunning.Should().BeFalse();
        VerifyLoggerCalled(_loggerMock, LogLevel.Information);
    }

    [Fact]
    public void Service_ShouldSupportCorrectBlockchainTypes()
    {
        // Act & Assert
        _service.SupportsBlockchain(BlockchainType.NeoN3).Should().BeTrue();
        _service.SupportsBlockchain(BlockchainType.NeoX).Should().BeTrue();
        _service.SupportsBlockchain((BlockchainType)999).Should().BeFalse();
    }

    [Fact]
    public void Service_ShouldHaveCorrectCapabilities()
    {
        // Act & Assert
        _service.Capabilities.Should().Contain(typeof(IAutomationService));
    }
}
