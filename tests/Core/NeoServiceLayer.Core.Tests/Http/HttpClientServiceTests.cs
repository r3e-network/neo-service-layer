using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using NeoServiceLayer.Core.Http;
using Moq.Protected;
using ItExpr = Moq.It;

namespace NeoServiceLayer.Core.Tests.Http;

/// <summary>
/// Comprehensive tests for HttpClientService covering HTTP operations, lifecycle management, and error scenarios.
/// </summary>
public class HttpClientServiceTests : IDisposable
{
    private readonly IFixture _fixture;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private HttpClientService _httpClientService;

    public HttpClientServiceTests()
    {
        _fixture = new Fixture();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientService = new HttpClientService(_httpClient);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithoutParameters_ShouldCreateDefaultHttpClient()
    {
        // Act
        using var service = new HttpClientService();

        // Assert
        service.Should().NotBeNull();
        service.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Constructor_WithHttpClient_ShouldUseProvidedClient()
    {
        // Arrange
        var customTimeout = TimeSpan.FromMinutes(2);
        _httpClient.Timeout = customTimeout;

        // Act
        using var service = new HttpClientService(_httpClient);

        // Assert
        service.Should().NotBeNull();
        service.Timeout.Should().Be(customTimeout);
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new HttpClientService(null!);
        action.Should().Throw<ArgumentNullException>()
              .And.ParamName.Should().Be("httpClient");
    }

    #endregion

    #region Timeout Property Tests

    [Fact]
    public void Timeout_Get_ShouldReturnHttpClientTimeout()
    {
        // Arrange
        var expectedTimeout = TimeSpan.FromMinutes(5);
        _httpClient.Timeout = expectedTimeout;

        // Act
        var actualTimeout = _httpClientService.Timeout;

        // Assert
        actualTimeout.Should().Be(expectedTimeout);
    }

    [Fact]
    public void Timeout_Set_ShouldUpdateHttpClientTimeout()
    {
        // Arrange
        var newTimeout = TimeSpan.FromMinutes(10);

        // Act
        _httpClientService.Timeout = newTimeout;

        // Assert
        _httpClient.Timeout.Should().Be(newTimeout);
        _httpClientService.Timeout.Should().Be(newTimeout);
    }

    #endregion

    #region GET String URI Tests

    [Fact]
    public async Task GetAsync_WithStringUri_ShouldReturnResponse()
    {
        // Arrange
        var requestUri = "https://api.example.com/test";
        var expectedContent = "Test response";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedContent, Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == requestUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _httpClientService.GetAsync(requestUri);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be(expectedContent);
    }

    [Fact]
    public async Task GetAsync_WithStringUri_AndCancellationToken_ShouldPassToken()
    {
        // Arrange
        var requestUri = "https://api.example.com/test";
        var cancellationToken = new CancellationToken();
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == requestUri),
                ItExpr.Is<CancellationToken>(ct => ct == cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _httpClientService.GetAsync(requestUri, cancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAsync_WithStringUri_WhenCancelled_ShouldThrowTaskCancelledException()
    {
        // Arrange
        var requestUri = "https://api.example.com/test";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        // Act & Assert
        var action = () => _httpClientService.GetAsync(requestUri, cts.Token);
        await action.Should().ThrowAsync<TaskCanceledException>();
    }

    #endregion

    #region GET Uri Tests

    [Fact]
    public async Task GetAsync_WithUri_ShouldReturnResponse()
    {
        // Arrange
        var requestUri = new Uri("https://api.example.com/test");
        var expectedContent = "Test response";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedContent)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri == requestUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _httpClientService.GetAsync(requestUri);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be(expectedContent);
    }

    [Fact]
    public async Task GetAsync_WithUri_AndCancellationToken_ShouldPassToken()
    {
        // Arrange
        var requestUri = new Uri("https://api.example.com/test");
        var cancellationToken = new CancellationToken();
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri == requestUri),
                ItExpr.Is<CancellationToken>(ct => ct == cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _httpClientService.GetAsync(requestUri, cancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region POST String URI Tests

    [Fact]
    public async Task PostAsync_WithStringUri_ShouldReturnResponse()
    {
        // Arrange
        var requestUri = "https://api.example.com/post";
        var requestContent = new StringContent("test data", Encoding.UTF8, "application/json");
        var expectedContent = "Post response";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(expectedContent)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.ToString() == requestUri && 
                    req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _httpClientService.PostAsync(requestUri, requestContent);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be(expectedContent);
    }

    [Fact]
    public async Task PostAsync_WithStringUri_AndCancellationToken_ShouldPassToken()
    {
        // Arrange
        var requestUri = "https://api.example.com/post";
        var requestContent = new StringContent("test data");
        var cancellationToken = new CancellationToken();
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.ToString() == requestUri &&
                    req.Method == HttpMethod.Post),
                ItExpr.Is<CancellationToken>(ct => ct == cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _httpClientService.PostAsync(requestUri, requestContent, cancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region POST Uri Tests

    [Fact]
    public async Task PostAsync_WithUri_ShouldReturnResponse()
    {
        // Arrange
        var requestUri = new Uri("https://api.example.com/post");
        var requestContent = new StringContent("test data", Encoding.UTF8, "application/json");
        var expectedContent = "Post response";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(expectedContent)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri == requestUri && 
                    req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _httpClientService.PostAsync(requestUri, requestContent);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be(expectedContent);
    }

    [Fact]
    public async Task PostAsync_WithUri_AndCancellationToken_ShouldPassToken()
    {
        // Arrange
        var requestUri = new Uri("https://api.example.com/post");
        var requestContent = new StringContent("test data");
        var cancellationToken = new CancellationToken();
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri == requestUri &&
                    req.Method == HttpMethod.Post),
                ItExpr.Is<CancellationToken>(ct => ct == cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _httpClientService.PostAsync(requestUri, requestContent, cancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetAsync_WithHttpRequestException_ShouldPropagateException()
    {
        // Arrange
        var requestUri = "https://api.example.com/error";
        var expectedException = new HttpRequestException("Network error");

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(expectedException);

        // Act & Assert
        var action = () => _httpClientService.GetAsync(requestUri);
        await action.Should().ThrowAsync<HttpRequestException>()
                   .WithMessage("Network error");
    }

    [Fact]
    public async Task PostAsync_WithHttpRequestException_ShouldPropagateException()
    {
        // Arrange
        var requestUri = "https://api.example.com/error";
        var requestContent = new StringContent("test");
        var expectedException = new HttpRequestException("Server error");

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(expectedException);

        // Act & Assert
        var action = () => _httpClientService.PostAsync(requestUri, requestContent);
        await action.Should().ThrowAsync<HttpRequestException>()
                   .WithMessage("Server error");
    }

    [Fact]
    public async Task GetAsync_WithTimeout_ShouldThrowTaskCancelledException()
    {
        // Arrange
        var requestUri = "https://api.example.com/slow";
        var timeoutException = new TaskCanceledException("Request timeout");

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(timeoutException);

        // Act & Assert
        var action = () => _httpClientService.GetAsync(requestUri);
        await action.Should().ThrowAsync<TaskCanceledException>();
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public async Task GetAsync_AfterDisposal_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _httpClientService.Dispose();

        // Act & Assert
        var action = () => _httpClientService.GetAsync("https://api.example.com/test");
        await action.Should().ThrowAsync<ObjectDisposedException>()
                   .WithMessage("*HttpClientService*");
    }

    [Fact]
    public async Task PostAsync_AfterDisposal_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _httpClientService.Dispose();
        var content = new StringContent("test");

        // Act & Assert
        var action = () => _httpClientService.PostAsync("https://api.example.com/test", content);
        await action.Should().ThrowAsync<ObjectDisposedException>()
                   .WithMessage("*HttpClientService*");
    }

    [Fact]
    public void Timeout_AfterDisposal_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _httpClientService.Dispose();

        // Act & Assert
        var action = () => _httpClientService.Timeout;
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void SetTimeout_AfterDisposal_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _httpClientService.Dispose();

        // Act & Assert
        var action = () => _httpClientService.Timeout = TimeSpan.FromMinutes(1);
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_MultipleCallsShouldNotThrow()
    {
        // Act & Assert
        _httpClientService.Dispose();
        _httpClientService.Dispose(); // Should not throw
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task HttpClientService_WithRealHttpClient_ShouldWork()
    {
        // Arrange
        using var realHttpClient = new HttpClient();
        using var service = new HttpClientService(realHttpClient);
        
        // Note: This is a mock test since we can't make real HTTP calls in unit tests
        // In a real scenario, you'd use a test server or HttpClientFactory with DI

        // Act & Assert
        service.Should().NotBeNull();
        service.Timeout.Should().Be(realHttpClient.Timeout);
    }

    [Fact]
    public void HttpClientService_WithDependencyInjection_ShouldConfigureCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient<IHttpClientService, HttpClientService>();
        
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var httpClientService = serviceProvider.GetRequiredService<IHttpClientService>();

        // Assert
        httpClientService.Should().NotBeNull();
        httpClientService.Should().BeOfType<HttpClientService>();
    }

    #endregion

    #region Test Cleanup

    public void Dispose()
    {
        _httpClientService?.Dispose();
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
} 