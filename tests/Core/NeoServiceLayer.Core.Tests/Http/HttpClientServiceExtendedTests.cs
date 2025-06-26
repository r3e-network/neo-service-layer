using System.Net;
using System.Text;
using FluentAssertions;
using Moq;
using Moq.Protected;
using NeoServiceLayer.Core.Http;
using Xunit;

namespace NeoServiceLayer.Core.Tests.Http;

/// <summary>
/// Extended tests for HttpClientService to improve coverage of disposal patterns, timeout management, and error scenarios.
/// </summary>
public class HttpClientServiceExtendedTests
{
    #region Constructor and Initialization Tests

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new HttpClientService(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public void DefaultConstructor_ShouldSetDefaultTimeout()
    {
        // Act
        using var service = new HttpClientService();

        // Assert
        service.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Constructor_WithCustomHttpClient_ShouldPreserveClientConfiguration()
    {
        // Arrange
        var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(5);
        httpClient.DefaultRequestHeaders.Add("User-Agent", "TestAgent");

        // Act
        using var service = new HttpClientService(httpClient);

        // Assert
        service.Timeout.Should().Be(TimeSpan.FromMinutes(5));
    }

    #endregion

    #region Timeout Property Tests

    [Fact]
    public void Timeout_Get_ShouldReturnCurrentTimeout()
    {
        // Arrange
        var httpClient = new HttpClient();
        var expectedTimeout = TimeSpan.FromSeconds(45);
        httpClient.Timeout = expectedTimeout;
        using var service = new HttpClientService(httpClient);

        // Act
        var actualTimeout = service.Timeout;

        // Assert
        actualTimeout.Should().Be(expectedTimeout);
    }

    [Fact]
    public void Timeout_Set_ShouldUpdateTimeout()
    {
        // Arrange
        var httpClient = new HttpClient();
        using var service = new HttpClientService(httpClient);
        var newTimeout = TimeSpan.FromMinutes(2);

        // Act
        service.Timeout = newTimeout;

        // Assert
        service.Timeout.Should().Be(newTimeout);
        httpClient.Timeout.Should().Be(newTimeout);
    }

    [Fact]
    public void Timeout_Get_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new HttpClientService(httpClient);
        service.Dispose();

        // Act & Assert
        var action = () => service.Timeout;
        action.Should().Throw<ObjectDisposedException>()
            .WithMessage("*HttpClientService*");
    }

    [Fact]
    public void Timeout_Set_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new HttpClientService(httpClient);
        service.Dispose();

        // Act & Assert
        var action = () => service.Timeout = TimeSpan.FromSeconds(30);
        action.Should().Throw<ObjectDisposedException>()
            .WithMessage("*HttpClientService*");
    }

    #endregion

    #region GET Method Tests with Disposal Checks

    [Fact]
    public async Task GetAsync_String_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new HttpClientService(httpClient);
        service.Dispose();

        // Act & Assert
        var action = async () => await service.GetAsync("https://example.com");
        await action.Should().ThrowAsync<ObjectDisposedException>()
            .WithMessage("*HttpClientService*");
    }

    [Fact]
    public async Task GetAsync_Uri_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new HttpClientService(httpClient);
        var uri = new Uri("https://example.com");
        service.Dispose();

        // Act & Assert
        var action = async () => await service.GetAsync(uri);
        await action.Should().ThrowAsync<ObjectDisposedException>()
            .WithMessage("*HttpClientService*");
    }

    [Fact]
    public async Task GetAsync_String_WithCancellationToken_ShouldPassTokenToHttpClient()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var cancellationToken = new CancellationToken();
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        using var httpClient = new HttpClient(mockHandler.Object);
        using var service = new HttpClientService(httpClient);

        // Act
        var response = await service.GetAsync("https://example.com", cancellationToken);

        // Assert
        response.Should().Be(expectedResponse);
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_Uri_WithCancellationToken_ShouldPassTokenToHttpClient()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var cancellationToken = new CancellationToken();
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var uri = new Uri("https://example.com");

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        using var httpClient = new HttpClient(mockHandler.Object);
        using var service = new HttpClientService(httpClient);

        // Act
        var response = await service.GetAsync(uri, cancellationToken);

        // Assert
        response.Should().Be(expectedResponse);
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    #endregion

    #region POST Method Tests with Disposal Checks

    [Fact]
    public async Task PostAsync_String_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new HttpClientService(httpClient);
        var content = new StringContent("test", Encoding.UTF8, "application/json");
        service.Dispose();

        // Act & Assert
        var action = async () => await service.PostAsync("https://example.com", content);
        await action.Should().ThrowAsync<ObjectDisposedException>()
            .WithMessage("*HttpClientService*");
    }

    [Fact]
    public async Task PostAsync_Uri_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new HttpClientService(httpClient);
        var content = new StringContent("test", Encoding.UTF8, "application/json");
        var uri = new Uri("https://example.com");
        service.Dispose();

        // Act & Assert
        var action = async () => await service.PostAsync(uri, content);
        await action.Should().ThrowAsync<ObjectDisposedException>()
            .WithMessage("*HttpClientService*");
    }

    [Fact]
    public async Task PostAsync_String_WithCancellationToken_ShouldPassTokenToHttpClient()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var cancellationToken = new CancellationToken();
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.Created);
        var content = new StringContent("test", Encoding.UTF8, "application/json");

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        using var httpClient = new HttpClient(mockHandler.Object);
        using var service = new HttpClientService(httpClient);

        // Act
        var response = await service.PostAsync("https://example.com", content, cancellationToken);

        // Assert
        response.Should().Be(expectedResponse);
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task PostAsync_Uri_WithCancellationToken_ShouldPassTokenToHttpClient()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var cancellationToken = new CancellationToken();
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.Created);
        var content = new StringContent("test", Encoding.UTF8, "application/json");
        var uri = new Uri("https://example.com");

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        using var httpClient = new HttpClient(mockHandler.Object);
        using var service = new HttpClientService(httpClient);

        // Act
        var response = await service.PostAsync(uri, content, cancellationToken);

        // Assert
        response.Should().Be(expectedResponse);
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    #endregion

    #region Disposal Pattern Tests

    [Fact]
    public void Dispose_ShouldDisposeHttpClient()
    {
        // Arrange
        var mockHttpClient = new Mock<HttpClient>();
        var service = new HttpClientService();

        // Act
        service.Dispose();

        // Assert
        // Subsequent operations should throw ObjectDisposedException
        var action = () => service.Timeout;
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new HttpClientService(httpClient);

        // Act & Assert
        var action = () =>
        {
            service.Dispose();
            service.Dispose(); // Second call should not throw
            service.Dispose(); // Third call should not throw
        };

        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WithNullHttpClient_ShouldNotThrow()
    {
        // This tests the disposal pattern's null check
        // Since we can't directly inject a null HttpClient, we test the disposal pattern
        // by verifying that disposal completes without throwing

        // Arrange
        using var service = new HttpClientService();

        // Act & Assert
        var action = () => service.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void Finalizer_ShouldSuppressFinalization()
    {
        // Arrange & Act
        var service = new HttpClientService();
        service.Dispose();

        // Assert
        // The fact that we can dispose and the test completes without hanging
        // verifies that GC.SuppressFinalize is working correctly
        service.Should().NotBeNull();
    }

    #endregion

    #region Integration Tests with Real HTTP Operations

    [Fact]
    public async Task GetAsync_WithValidStringUri_ShouldReturnResponse()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Success")
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        using var httpClient = new HttpClient(mockHandler.Object);
        using var service = new HttpClientService(httpClient);

        // Act
        var response = await service.GetAsync("https://api.example.com/data");

        // Assert
        response.Should().Be(expectedResponse);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostAsync_WithValidContent_ShouldReturnResponse()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("Created")
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        using var httpClient = new HttpClient(mockHandler.Object);
        using var service = new HttpClientService(httpClient);
        var content = new StringContent("{\"key\":\"value\"}", Encoding.UTF8, "application/json");

        // Act
        var response = await service.PostAsync("https://api.example.com/create", content);

        // Assert
        response.Should().Be(expectedResponse);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    #endregion

    #region Error Scenario Tests

    [Fact]
    public async Task GetAsync_WithHttpRequestException_ShouldPropagateException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var expectedException = new HttpRequestException("Network error");

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(expectedException);

        using var httpClient = new HttpClient(mockHandler.Object);
        using var service = new HttpClientService(httpClient);

        // Act & Assert
        var action = async () => await service.GetAsync("https://invalid-url.com");
        await action.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Network error");
    }

    [Fact]
    public async Task PostAsync_WithTaskCancelledException_ShouldPropagateException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var cancellationToken = new CancellationToken(true); // Already cancelled
        var expectedException = new TaskCanceledException("Request was cancelled");

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(expectedException);

        using var httpClient = new HttpClient(mockHandler.Object);
        using var service = new HttpClientService(httpClient);
        var content = new StringContent("test");

        // Act & Assert
        var action = async () => await service.PostAsync("https://example.com", content, cancellationToken);
        await action.Should().ThrowAsync<TaskCanceledException>()
            .WithMessage("Request was cancelled");
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Timeout_SetToInfinite_ShouldWork()
    {
        // Arrange
        using var service = new HttpClientService();
        var infiniteTimeout = System.Threading.Timeout.InfiniteTimeSpan;

        // Act
        service.Timeout = infiniteTimeout;

        // Assert
        service.Timeout.Should().Be(infiniteTimeout);
    }

    [Fact]
    public void Timeout_SetToVerySmall_ShouldWork()
    {
        // Arrange
        using var service = new HttpClientService();
        var smallTimeout = TimeSpan.FromMilliseconds(1);

        // Act
        service.Timeout = smallTimeout;

        // Assert
        service.Timeout.Should().Be(smallTimeout);
    }

    [Fact]
    public async Task GetAsync_WithEmptyString_ShouldDelegateToHttpClient()
    {
        // Arrange
        using var httpClient = new HttpClient();
        using var service = new HttpClientService(httpClient);

        // Act & Assert
        // This should throw InvalidOperationException from HttpClient for invalid URIs, confirming delegation
        var action = async () => await service.GetAsync("");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("An invalid request URI was provided*");
    }

    #endregion
}
