using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using Xunit;

namespace NeoServiceLayer.ServiceFramework.Tests;

/// <summary>
/// Tests for the ServiceBase class.
/// </summary>
public class ServiceBaseTests
{
    private readonly Mock<ILogger<TestService>> _loggerMock;
    private readonly TestService _service;

    public ServiceBaseTests()
    {
        _loggerMock = new Mock<ILogger<TestService>>();
        _service = new TestService(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Assert
        Assert.Equal("TestService", _service.Name);
        Assert.Equal("Test service for unit tests", _service.Description);
        Assert.Equal("1.0.0", _service.Version);
        Assert.False(_service.IsRunning);
        Assert.Contains(typeof(IService), _service.Capabilities);
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnTrue_WhenOnInitializeAsyncReturnsTrue()
    {
        // Arrange
        _service.SetOnInitializeAsyncResult(true);

        // Act
        bool result = await _service.InitializeAsync();

        // Assert
        Assert.True(result);
        Assert.True(_service.OnInitializeAsyncCalled);
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnFalse_WhenOnInitializeAsyncReturnsFalse()
    {
        // Arrange
        _service.SetOnInitializeAsyncResult(false);

        // Act
        bool result = await _service.InitializeAsync();

        // Assert
        Assert.False(result);
        Assert.True(_service.OnInitializeAsyncCalled);
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnFalse_WhenOnInitializeAsyncThrowsException()
    {
        // Arrange
        _service.SetOnInitializeAsyncException(new Exception("Test exception"));

        // Act
        bool result = await _service.InitializeAsync();

        // Assert
        Assert.False(result);
        Assert.True(_service.OnInitializeAsyncCalled);
    }

    [Fact]
    public async Task StartAsync_ShouldReturnTrue_WhenOnStartAsyncReturnsTrue()
    {
        // Arrange
        _service.SetOnStartAsyncResult(true);

        // Act
        bool result = await _service.StartAsync();

        // Assert
        Assert.True(result);
        Assert.True(_service.IsRunning);
        Assert.True(_service.OnStartAsyncCalled);
    }

    [Fact]
    public async Task StartAsync_ShouldReturnFalse_WhenOnStartAsyncReturnsFalse()
    {
        // Arrange
        _service.SetOnStartAsyncResult(false);

        // Act
        bool result = await _service.StartAsync();

        // Assert
        Assert.False(result);
        Assert.False(_service.IsRunning);
        Assert.True(_service.OnStartAsyncCalled);
    }

    [Fact]
    public async Task StartAsync_ShouldReturnFalse_WhenOnStartAsyncThrowsException()
    {
        // Arrange
        _service.SetOnStartAsyncException(new Exception("Test exception"));

        // Act
        bool result = await _service.StartAsync();

        // Assert
        Assert.False(result);
        Assert.False(_service.IsRunning);
        Assert.True(_service.OnStartAsyncCalled);
    }

    [Fact]
    public async Task StartAsync_ShouldReturnTrue_WhenServiceIsAlreadyRunning()
    {
        // Arrange
        _service.SetOnStartAsyncResult(true);
        await _service.StartAsync();
        _service.ResetFlags();

        // Act
        bool result = await _service.StartAsync();

        // Assert
        Assert.True(result);
        Assert.True(_service.IsRunning);
        Assert.False(_service.OnStartAsyncCalled); // Should not be called again
    }

    [Fact]
    public async Task StopAsync_ShouldReturnTrue_WhenOnStopAsyncReturnsTrue()
    {
        // Arrange
        _service.SetOnStartAsyncResult(true);
        _service.SetOnStopAsyncResult(true);
        await _service.StartAsync();
        _service.ResetFlags();

        // Act
        bool result = await _service.StopAsync();

        // Assert
        Assert.True(result);
        Assert.False(_service.IsRunning);
        Assert.True(_service.OnStopAsyncCalled);
    }

    [Fact]
    public async Task StopAsync_ShouldReturnFalse_WhenOnStopAsyncReturnsFalse()
    {
        // Arrange
        _service.SetOnStartAsyncResult(true);
        _service.SetOnStopAsyncResult(false);
        await _service.StartAsync();
        _service.ResetFlags();

        // Act
        bool result = await _service.StopAsync();

        // Assert
        Assert.False(result);
        Assert.True(_service.IsRunning); // Service should still be running
        Assert.True(_service.OnStopAsyncCalled);
    }

    [Fact]
    public async Task StopAsync_ShouldReturnFalse_WhenOnStopAsyncThrowsException()
    {
        // Arrange
        _service.SetOnStartAsyncResult(true);
        _service.SetOnStopAsyncException(new Exception("Test exception"));
        await _service.StartAsync();
        _service.ResetFlags();

        // Act
        bool result = await _service.StopAsync();

        // Assert
        Assert.False(result);
        Assert.True(_service.IsRunning); // Service should still be running
        Assert.True(_service.OnStopAsyncCalled);
    }

    [Fact]
    public async Task StopAsync_ShouldReturnTrue_WhenServiceIsNotRunning()
    {
        // Arrange
        // Service is not started

        // Act
        bool result = await _service.StopAsync();

        // Assert
        Assert.True(result);
        Assert.False(_service.IsRunning);
        Assert.False(_service.OnStopAsyncCalled); // Should not be called
    }

    [Fact]
    public async Task GetHealthAsync_ShouldReturnNotRunning_WhenServiceIsNotRunning()
    {
        // Arrange
        // Service is not started

        // Act
        ServiceHealth health = await _service.GetHealthAsync();

        // Assert
        Assert.Equal(ServiceHealth.NotRunning, health);
        Assert.False(_service.OnGetHealthAsyncCalled); // Should not be called
    }

    [Fact]
    public async Task GetHealthAsync_ShouldReturnHealthFromOnGetHealthAsync_WhenServiceIsRunning()
    {
        // Arrange
        _service.SetOnStartAsyncResult(true);
        _service.SetOnGetHealthAsyncResult(ServiceHealth.Healthy);
        await _service.StartAsync();
        _service.ResetFlags();

        // Act
        ServiceHealth health = await _service.GetHealthAsync();

        // Assert
        Assert.Equal(ServiceHealth.Healthy, health);
        Assert.True(_service.OnGetHealthAsyncCalled);
    }

    [Fact]
    public async Task GetHealthAsync_ShouldReturnUnhealthy_WhenOnGetHealthAsyncThrowsException()
    {
        // Arrange
        _service.SetOnStartAsyncResult(true);
        _service.SetOnGetHealthAsyncException(new Exception("Test exception"));
        await _service.StartAsync();
        _service.ResetFlags();

        // Act
        ServiceHealth health = await _service.GetHealthAsync();

        // Assert
        Assert.Equal(ServiceHealth.Unhealthy, health);
        Assert.True(_service.OnGetHealthAsyncCalled);
    }

    [Fact]
    public async Task GetMetricsAsync_ShouldReturnMetrics()
    {
        // Arrange
        _service.UpdateTestMetric("TestMetric", 42);

        // Act
        var metrics = await _service.GetMetricsAsync();

        // Assert
        Assert.NotNull(metrics);
        Assert.Contains("TestMetric", metrics.Keys);
        Assert.Equal(42, metrics["TestMetric"]);
        Assert.True(_service.OnUpdateMetricsAsyncCalled);
    }

    [Fact]
    public async Task ValidateDependenciesAsync_ShouldReturnTrue_WhenAllDependenciesAreValid()
    {
        // Arrange
        var dependencyService = new TestService(_loggerMock.Object, "DependencyService", "1.0.0");
        _service.AddTestRequiredDependency("DependencyService", "1.0.0");

        // Act
        bool result = await _service.ValidateDependenciesAsync(new[] { dependencyService });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateDependenciesAsync_ShouldReturnFalse_WhenRequiredDependencyIsMissing()
    {
        // Arrange
        _service.AddTestRequiredDependency("MissingService", "1.0.0");

        // Act
        bool result = await _service.ValidateDependenciesAsync(Array.Empty<IService>());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateDependenciesAsync_ShouldReturnTrue_WhenOptionalDependencyIsMissing()
    {
        // Arrange
        _service.AddTestOptionalDependency("MissingService", "1.0.0");

        // Act
        bool result = await _service.ValidateDependenciesAsync(Array.Empty<IService>());

        // Assert
        Assert.True(result);
    }
}

/// <summary>
/// Test implementation of ServiceBase for unit tests.
/// </summary>
public class TestService : ServiceBase
{
    public bool OnInitializeAsyncCalled { get; private set; }
    public bool OnStartAsyncCalled { get; private set; }
    public bool OnStopAsyncCalled { get; private set; }
    public bool OnGetHealthAsyncCalled { get; private set; }
    public bool OnUpdateMetricsAsyncCalled { get; private set; }

    private bool _onInitializeAsyncResult = true;
    private bool _onStartAsyncResult = true;
    private bool _onStopAsyncResult = true;
    private ServiceHealth _onGetHealthAsyncResult = ServiceHealth.Healthy;

    private Exception? _onInitializeAsyncException;
    private Exception? _onStartAsyncException;
    private Exception? _onStopAsyncException;
    private Exception? _onGetHealthAsyncException;

    public TestService(ILogger<TestService> logger, string name = "TestService", string version = "1.0.0")
        : base(name, "Test service for unit tests", version, logger)
    {
    }

    public void ResetFlags()
    {
        OnInitializeAsyncCalled = false;
        OnStartAsyncCalled = false;
        OnStopAsyncCalled = false;
        OnGetHealthAsyncCalled = false;
        OnUpdateMetricsAsyncCalled = false;
    }

    public void SetOnInitializeAsyncResult(bool result)
    {
        _onInitializeAsyncResult = result;
        _onInitializeAsyncException = null;
    }

    public void SetOnInitializeAsyncException(Exception exception)
    {
        _onInitializeAsyncException = exception;
    }

    public void SetOnStartAsyncResult(bool result)
    {
        _onStartAsyncResult = result;
        _onStartAsyncException = null;
    }

    public void SetOnStartAsyncException(Exception exception)
    {
        _onStartAsyncException = exception;
    }

    public void SetOnStopAsyncResult(bool result)
    {
        _onStopAsyncResult = result;
        _onStopAsyncException = null;
    }

    public void SetOnStopAsyncException(Exception exception)
    {
        _onStopAsyncException = exception;
    }

    public void SetOnGetHealthAsyncResult(ServiceHealth result)
    {
        _onGetHealthAsyncResult = result;
        _onGetHealthAsyncException = null;
    }

    public void SetOnGetHealthAsyncException(Exception exception)
    {
        _onGetHealthAsyncException = exception;
    }

    public void UpdateTestMetric(string key, object value)
    {
        UpdateMetric(key, value);
    }

    public void AddTestRequiredDependency(string serviceName, string minimumVersion)
    {
        AddRequiredDependency(serviceName, minimumVersion);
    }

    public void AddTestOptionalDependency(string serviceName, string minimumVersion)
    {
        AddOptionalDependency(serviceName, minimumVersion);
    }

    protected override Task<bool> OnInitializeAsync()
    {
        OnInitializeAsyncCalled = true;

        if (_onInitializeAsyncException != null)
        {
            throw _onInitializeAsyncException;
        }

        return Task.FromResult(_onInitializeAsyncResult);
    }

    protected override Task<bool> OnStartAsync()
    {
        OnStartAsyncCalled = true;

        if (_onStartAsyncException != null)
        {
            throw _onStartAsyncException;
        }

        return Task.FromResult(_onStartAsyncResult);
    }

    protected override Task<bool> OnStopAsync()
    {
        OnStopAsyncCalled = true;

        if (_onStopAsyncException != null)
        {
            throw _onStopAsyncException;
        }

        return Task.FromResult(_onStopAsyncResult);
    }

    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        OnGetHealthAsyncCalled = true;

        if (_onGetHealthAsyncException != null)
        {
            throw _onGetHealthAsyncException;
        }

        return Task.FromResult(_onGetHealthAsyncResult);
    }

    protected override Task OnUpdateMetricsAsync()
    {
        OnUpdateMetricsAsyncCalled = true;
        return Task.CompletedTask;
    }
}
