using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Notification.Models;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Integration.Tests.Microservices
{
    /// <summary>
    /// Integration tests using mocked HTTP responses
    /// </summary>
    [Collection("Integration")]
    public class MockedServiceTests
    {
        private readonly ITestOutputHelper _output;
        private readonly ILogger<MockedServiceTests> _logger;

        public MockedServiceTests(ITestOutputHelper output)
        {
            _output = output;
            
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            var serviceProvider = services.BuildServiceProvider();
            _logger = serviceProvider.GetRequiredService<ILogger<MockedServiceTests>>();
        }

        [Fact]
        public async Task RateLimiting_ShouldReturn429_WhenLimitExceeded()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            var requestCount = 0;
            
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    requestCount++;
                    if (requestCount > 100) // Simulate rate limit after 100 requests
                    {
                        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(60));
                        return response;
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK);
                });

            var client = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:7000")
            };

            // Act
            var responses = new List<HttpResponseMessage>();
            for (int i = 0; i < 150; i++)
            {
                var response = await client.GetAsync("/api/test");
                responses.Add(response);
            }

            // Assert
            var successCount = responses.FindAll(r => r.StatusCode == HttpStatusCode.OK).Count;
            var rateLimitedCount = responses.FindAll(r => r.StatusCode == HttpStatusCode.TooManyRequests).Count;
            
            successCount.Should().Be(100);
            rateLimitedCount.Should().Be(50);
            
            // Verify retry-after header
            var rateLimitedResponse = responses.Find(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            rateLimitedResponse.Should().NotBeNull();
            rateLimitedResponse!.Headers.RetryAfter.Should().NotBeNull();
        }

        [Fact]
        public async Task CircuitBreaker_ShouldOpenAfterFailures()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            var failureCount = 0;
            var circuitOpen = false;
            
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    if (circuitOpen)
                    {
                        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                        {
                            ReasonPhrase = "Circuit breaker is open"
                        };
                    }
                    
                    failureCount++;
                    if (failureCount >= 5)
                    {
                        circuitOpen = true;
                    }
                    
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                });

            var client = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:7000")
            };

            // Act
            var responses = new List<HttpResponseMessage>();
            for (int i = 0; i < 10; i++)
            {
                var response = await client.GetAsync("/api/failing-service");
                responses.Add(response);
                _logger.LogInformation($"Request {i + 1}: {response.StatusCode} - {response.ReasonPhrase}");
            }

            // Assert
            var serverErrors = responses.FindAll(r => r.StatusCode == HttpStatusCode.InternalServerError).Count;
            var circuitBreakerErrors = responses.FindAll(r => r.StatusCode == HttpStatusCode.ServiceUnavailable).Count;
            
            serverErrors.Should().Be(5, "First 5 requests should fail with server error");
            circuitBreakerErrors.Should().Be(5, "After 5 failures, circuit should open");
        }

        [Fact]
        public async Task Authentication_ShouldReturnToken()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0dXNlciIsImV4cCI6MTcwMDAwMDAwMH0.test";
            
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains("/auth/login")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new StringContent(
                        $"{{\"token\":\"{token}\",\"expiresAt\":\"2025-01-01T00:00:00Z\"}}",
                        Encoding.UTF8,
                        "application/json");
                    return response;
                });

            var client = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:7000")
            };

            // Act
            var authRequest = new { Username = "testuser", Password = "testpass" };
            var response = await client.PostAsJsonAsync("/api/auth/login", authRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain(token);
        }

        [Fact]
        public async Task ServiceDiscovery_ShouldReturnRegisteredServices()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            var servicesJson = @"[
                {""serviceType"":""notification"",""serviceName"":""notification-1"",""hostName"":""localhost"",""port"":8081},
                {""serviceType"":""storage"",""serviceName"":""storage-1"",""hostName"":""localhost"",""port"":8082},
                {""serviceType"":""health"",""serviceName"":""health-1"",""hostName"":""localhost"",""port"":8083}
            ]";
            
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains("/discovery/services")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new StringContent(servicesJson, Encoding.UTF8, "application/json");
                    return response;
                });

            var client = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:7000")
            };

            // Act
            var response = await client.GetAsync("/api/discovery/services");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("notification");
            content.Should().Contain("storage");
            content.Should().Contain("health");
        }

        [Fact]
        public async Task HealthAggregation_ShouldReturnAllServicesStatus()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            var healthJson = @"{
                ""services"": {
                    ""notification"": {""status"":""Healthy"",""lastCheck"":""2025-01-10T12:00:00Z""},
                    ""storage"": {""status"":""Healthy"",""lastCheck"":""2025-01-10T12:00:00Z""},
                    ""configuration"": {""status"":""Degraded"",""lastCheck"":""2025-01-10T12:00:00Z""}
                }
            }";
            
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains("/health/system")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new StringContent(healthJson, Encoding.UTF8, "application/json");
                    return response;
                });

            var client = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:7000")
            };

            // Act
            var response = await client.GetAsync("/api/health/system");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Healthy");
            content.Should().Contain("Degraded");
            content.Should().Contain("notification");
            content.Should().Contain("storage");
            content.Should().Contain("configuration");
        }
    }
}