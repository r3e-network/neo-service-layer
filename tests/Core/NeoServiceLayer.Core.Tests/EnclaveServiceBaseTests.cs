using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using Xunit;

namespace NeoServiceLayer.Core.Tests;

public class EnclaveServiceBaseTests : IDisposable
{
    private readonly Mock<ILogger<TestEnclaveService>> _mockLogger;
    private readonly TestEnclaveService _service;

    public EnclaveServiceBaseTests()
    {
        _mockLogger = new Mock<ILogger<TestEnclaveService>>();
        _service = new TestEnclaveService(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_SetsEnclaveProperties()
    {
        _service.HasEnclaveCapabilities.Should().BeTrue();
        _service.IsEnclaveInitialized.Should().BeFalse();
        _service.GetCapabilities().Should().Contain(typeof(IEnclaveService));
        _service.GetMetadata("HasEnclaveCapabilities").Should().Be(true);
        _service.GetMetadata("EnclaveType").Should().Be("SGX");
    }

    [Fact]
    public async Task InitializeEnclaveAsync_WhenNotInitialized_InitializesSuccessfully()
    {
        var result = await _service.InitializeEnclaveAsync();

        result.Should().BeTrue();
        _service.IsEnclaveInitialized.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeEnclaveAsync_WhenAlreadyInitialized_ReturnsTrue()
    {
        await _service.InitializeEnclaveAsync();

        var result = await _service.InitializeEnclaveAsync();

        result.Should().BeTrue();
        _service.IsEnclaveInitialized.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateEnclaveAsync_WhenInitialized_ReturnsTrue()
    {
        await _service.InitializeEnclaveAsync();

        var result = await _service.ValidateEnclaveAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateEnclaveAsync_WhenNotInitialized_ReturnsFalse()
    {
        var result = await _service.ValidateEnclaveAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetEnclaveStatusAsync_ReturnsCompleteStatus()
    {
        await _service.InitializeEnclaveAsync();

        var status = await _service.GetEnclaveStatusAsync();

        status.Should().NotBeNull();
        status.Should().ContainKey("Initialized");
        status.Should().ContainKey("Type");
        status.Should().ContainKey("Capabilities");
        status["Initialized"].Should().Be(true);
        status["Type"].Should().Be("SGX");
    }

    [Fact]
    public async Task SecureComputeAsync_WhenInitialized_PerformsComputation()
    {
        await _service.InitializeEnclaveAsync();
        var data = System.Text.Encoding.UTF8.GetBytes("test data");
        var parameters = new Dictionary<string, object> { ["operation"] = "encrypt" };

        var result = await _service.SecureComputeAsync("test-operation", data, parameters);

        result.Should().NotBeNull();
        result.Should().NotBeEquivalentTo(data); // Should be transformed
    }

    [Fact]
    public async Task SecureComputeAsync_WhenNotInitialized_ThrowsInvalidOperationException()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("test data");

        Func<Task> act = async () => await _service.SecureComputeAsync("test", data);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public async Task VerifyAttestationAsync_WithValidData_ReturnsTrue()
    {
        await _service.InitializeEnclaveAsync();
        var attestationData = System.Text.Encoding.UTF8.GetBytes("valid attestation");

        var result = await _service.VerifyAttestationAsync(attestationData);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAttestationAsync_WithInvalidData_ReturnsFalse()
    {
        await _service.InitializeEnclaveAsync();
        byte[] attestationData = null;

        var result = await _service.VerifyAttestationAsync(attestationData);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAttestationAsync_WhenInitialized_ReturnsAttestationData()
    {
        await _service.InitializeEnclaveAsync();

        var result = await _service.GenerateAttestationAsync();

        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateAttestationAsync_WhenNotInitialized_ThrowsInvalidOperationException()
    {
        Func<Task> act = async () => await _service.GenerateAttestationAsync();
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public async Task GetHealthAsync_IncludesEnclaveStatus()
    {
        await _service.InitializeAsync();
        await _service.InitializeEnclaveAsync();
        await _service.StartAsync();

        var health = await _service.GetHealthAsync();

        health.Should().ContainKey("EnclaveInitialized");
        health["EnclaveInitialized"].Should().Be(true);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }

    private class TestEnclaveService : EnclaveServiceBase
    {
        public TestEnclaveService(ILogger<TestEnclaveService> logger)
            : base("TestEnclaveService", "A test enclave service", "1.0.0", logger)
        {
        }

        protected override Task<bool> OnInitializeAsync()
        {
            Logger.LogInformation("Test enclave service initialized");
            return Task.FromResult(true);
        }

        protected override Task<bool> OnStartAsync()
        {
            Logger.LogInformation("Test enclave service started");
            return Task.FromResult(true);
        }

        protected override Task<bool> OnStopAsync()
        {
            Logger.LogInformation("Test enclave service stopped");
            return Task.FromResult(true);
        }

        protected override Task<bool> OnValidateEnclaveAsync()
        {
            Logger.LogInformation("Test enclave service validated");
            return Task.FromResult(true);
        }

        protected override Task<ServiceHealth> OnGetHealthAsync()
        {
            return Task.FromResult(ServiceHealth.Healthy);
        }

        protected override Task<bool> OnInitializeEnclaveAsync()
        {
            Logger.LogInformation("Test enclave initialized");
            return Task.FromResult(true);
        }

        protected override Task<string?> OnGetAttestationAsync()
        {
            return Task.FromResult<string?>("test-attestation-string");
        }

    }
}