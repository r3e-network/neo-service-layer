using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Shared.Constants;
using Xunit;

namespace NeoServiceLayer.Core.Tests;

public class ServiceBaseTests : IDisposable
{
    private readonly Mock<ILogger<TestService>> _mockLogger;
    private readonly TestService _service;

    public ServiceBaseTests()
    {
        _mockLogger = new Mock<ILogger<TestService>>();
        _service = new TestService(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        _service.Name.Should().Be("TestService");
        _service.Description.Should().Be("A test service");
        _service.Version.Should().Be("1.0.0");
        _service.ServiceType.Should().Be("TestService");
        _service.IsRunning.Should().BeFalse();
        _service.Health.Should().Be("Unknown");
        _service.Status.Should().Be("NotStarted");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Action act = () => new TestService(null!);
        act.Should().Throw<ArgumentNullException>().WithMessage("*logger*");
    }

    [Fact]
    public async Task InitializeAsync_WhenCalled_SetsInitializedStatus()
    {
        var result = await _service.InitializeAsync();

        result.Should().BeTrue();
        _service.Status.Should().Be("Initialized");
    }

    [Fact]
    public async Task StartAsync_WhenInitialized_StartsSuccessfully()
    {
        await _service.InitializeAsync();
        
        var result = await _service.StartAsync();

        result.Should().BeTrue();
        _service.IsRunning.Should().BeTrue();
        _service.Status.Should().Be("Running");
    }

    [Fact]
    public async Task StartAsync_WhenNotInitialized_ThrowsInvalidOperationException()
    {
        Func<Task> act = async () => await _service.StartAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*must be initialized*");
    }

    [Fact]
    public async Task StopAsync_WhenRunning_StopsSuccessfully()
    {
        await _service.InitializeAsync();
        await _service.StartAsync();
        
        var result = await _service.StopAsync();

        result.Should().BeTrue();
        _service.IsRunning.Should().BeFalse();
        _service.Status.Should().Be("Stopped");
    }

    [Fact]
    public void AddCapability_WithValidCapability_AddsToCapabilities()
    {
        _service.AddCapability<IService>();

        _service.GetCapabilities().Should().Contain(typeof(IService));
    }

    [Fact]
    public void HasCapability_WithExistingCapability_ReturnsTrue()
    {
        _service.AddCapability<IService>();

        var hasCapability = _service.HasCapability<IService>();

        hasCapability.Should().BeTrue();
    }

    [Fact]
    public void HasCapability_WithNonExistingCapability_ReturnsFalse()
    {
        var hasCapability = _service.HasCapability<IDisposable>();

        hasCapability.Should().BeFalse();
    }

    [Fact]
    public void SetMetadata_WithValidKeyValue_SetsMetadata()
    {
        _service.SetMetadata("TestKey", "TestValue");

        var metadata = _service.GetMetadata();
        metadata.Should().ContainKey("TestKey");
        metadata["TestKey"].Should().Be("TestValue");
    }

    [Fact]
    public void GetMetadata_WithKey_ReturnsCorrectValue()
    {
        _service.SetMetadata("TestKey", "TestValue");

        var value = _service.GetMetadata("TestKey");

        value.Should().Be("TestValue");
    }

    [Fact]
    public void GetMetadata_WithNonExistentKey_ReturnsNull()
    {
        var value = _service.GetMetadata("NonExistentKey");

        value.Should().BeNull();
    }

    [Fact]
    public async Task GetHealthAsync_WhenRunning_ReturnsHealthyStatus()
    {
        await _service.InitializeAsync();
        await _service.StartAsync();

        var health = await _service.GetHealthAsync();

        health.Should().NotBeNull();
        health["Status"].Should().Be("Running");
        health["IsRunning"].Should().Be(true);
    }

    [Fact]
    public void GetServiceInfo_ReturnsCompleteServiceInformation()
    {
        _service.SetMetadata("TestKey", "TestValue");

        var info = _service.GetServiceInfo();

        info.Should().NotBeNull();
        info["Name"].Should().Be("TestService");
        info["Description"].Should().Be("A test service");
        info["Version"].Should().Be("1.0.0");
        info["Status"].Should().Be("NotStarted");
        info.Should().ContainKey("Metadata");
    }

    [Fact]
    public void Dispose_WhenCalled_DisposesResourcesProperly()
    {
        _service.SetMetadata("TestKey", "TestValue");

        _service.Dispose();

        _service.Status.Should().Be("Disposed");
    }

    public void Dispose()
    {
        _service?.Dispose();
    }

    private class TestService : ServiceBase
    {
        public TestService(ILogger<TestService> logger)
            : base("TestService", "A test service", "1.0.0", logger)
        {
        }

        protected override Task<bool> OnInitializeAsync()
        {
            Logger.LogInformation("Test service initialized");
            return Task.FromResult(true);
        }

        protected override Task<bool> OnStartAsync()
        {
            Logger.LogInformation("Test service started");
            return Task.FromResult(true);
        }

        protected override Task<bool> OnStopAsync()
        {
            Logger.LogInformation("Test service stopped");
            return Task.FromResult(true);
        }

        protected override Task<ServiceHealth> OnGetHealthAsync()
        {
            return Task.FromResult(ServiceHealth.Healthy);
        }
    }
}