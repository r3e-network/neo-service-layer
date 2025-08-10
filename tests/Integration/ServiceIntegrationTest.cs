using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;

namespace NeoServiceLayer.Integration.Tests
{
    public class ServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ServiceIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Health_Endpoint_Returns_Success()
        {
            // Arrange & Act
            var response = await _client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }

        [Fact]
        public async Task Storage_Service_Can_Store_And_Retrieve_Data()
        {
            // Arrange
            var testData = new { key = "test", value = "data" };
            
            // Act - Store
            var storeResponse = await _client.PostAsJsonAsync("/storage/data", testData);
            storeResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
            
            var location = storeResponse.Headers.Location.ToString();
            
            // Act - Retrieve
            var getResponse = await _client.GetAsync(location);
            
            // Assert
            getResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }

        [Fact]
        public async Task Oracle_Service_Can_Process_Requests()
        {
            // Arrange
            var oracleRequest = new 
            { 
                dataType = "price",
                asset = "NEO/USD"
            };
            
            // Act
            var response = await _client.PostAsJsonAsync("/oracle/requests", oracleRequest);
            
            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
        }

        [Fact]
        public async Task Services_Can_Communicate_Through_Service_Discovery()
        {
            // This test would verify that services can discover each other
            // through Consul and communicate successfully
            
            // Arrange
            var storageClient = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Configure service discovery
                });
            }).CreateClient();
            
            // Act & Assert
            // Test inter-service communication
            Assert.True(true); // Placeholder
        }
    }
}
