using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.SmartContracts;
using NeoServiceLayer.Services.SmartContracts.NeoN3;
using NeoServiceLayer.Services.SmartContracts.NeoX;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.TestInfrastructure;
using NeoServiceLayer.Tee.Host.Tests;
using Xunit;

namespace NeoServiceLayer.Services.SmartContracts.Tests;

internal class SimpleServiceConfiguration : IServiceConfiguration
{
    private readonly Dictionary<string, object> _values = new();

    public T? GetValue<T>(string key) => _values.TryGetValue(key, out var value) ? (T)value : default;
    public T GetValue<T>(string key, T defaultValue) => GetValue<T>(key) ?? defaultValue;
    public void SetValue<T>(string key, T value) => _values[key] = value!;
    public bool ContainsKey(string key) => _values.ContainsKey(key);
    public bool RemoveKey(string key) => _values.Remove(key);
    public IEnumerable<string> GetAllKeys() => _values.Keys;
    public IServiceConfiguration? GetSection(string sectionName) => null;
    public string GetConnectionString(string name) => string.Empty;
}

/// <summary>
/// Simple integration tests for the SmartContractsService.
/// </summary>
public class SmartContractsServiceTestsSimple : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly SmartContractsService _service;

    public SmartContractsServiceTestsSimple()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        // Add mock dependencies
        services.AddSingleton<IServiceConfiguration>(new SimpleServiceConfiguration());
        services.AddSingleton<IEnclaveWrapper, TestEnclaveWrapper>();
        services.AddSingleton<IEnclaveManager, EnclaveManager>();
        
        // Add managers
        services.AddScoped<NeoN3SmartContractManager>();
        services.AddScoped<NeoXSmartContractManager>();
        
        // Add service
        services.AddScoped<SmartContractsService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _service = _serviceProvider.GetRequiredService<SmartContractsService>();
    }

    [Fact]
    public async Task Service_ShouldInitialize()
    {
        // Act
        var result = await _service.InitializeAsync();
        
        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ServiceProperties_ShouldHaveCorrectValues()
    {
        // Assert
        _service.Name.Should().Be("SmartContracts");
        _service.Description.Should().Contain("Smart");
        _service.Version.Should().NotBeNullOrEmpty();
        _service.Capabilities.Should().Contain(typeof(ISmartContractsService));
    }

    [Fact]
    public async Task GetHealthAsync_ShouldReturnNotReady_WhenNotStarted()
    {
        // Act
        var health = await _service.GetHealthAsync();
        
        // Assert
        health.Should().Be(ServiceHealth.NotRunning);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        await _service.InitializeAsync();
        
        // Act
        var stats = await _service.GetStatisticsAsync();
        
        // Assert
        stats.Should().NotBeNull();
        stats.ByBlockchain.Should().NotBeNull();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}