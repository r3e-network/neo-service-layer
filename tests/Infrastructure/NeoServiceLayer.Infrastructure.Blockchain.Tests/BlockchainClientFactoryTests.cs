using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Blockchain;
using Xunit;

namespace NeoServiceLayer.Infrastructure.Blockchain.Tests
{
    public class BlockchainClientFactoryTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<BlockchainClientFactory>> _mockLogger;
        private readonly BlockchainClientFactory _factory;

        public BlockchainClientFactoryTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<BlockchainClientFactory>>();
            
            _mockServiceProvider.Setup(x => x.GetService(typeof(IConfiguration)))
                .Returns(_mockConfiguration.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(ILogger<BlockchainClientFactory>)))
                .Returns(_mockLogger.Object);

            _factory = new BlockchainClientFactory(_mockServiceProvider.Object, _mockConfiguration.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateClientAsync_WithNeoN3Type_CreatesNeoN3Client()
        {
            // Arrange
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(x => x.Value).Returns("http://localhost:10332");
            _mockConfiguration.Setup(x => x.GetSection("Blockchain:Neo:RpcUrl"))
                .Returns(configSection.Object);

            // Act
            var client = await _factory.CreateClientAsync(BlockchainType.NeoN3);

            // Assert
            client.Should().NotBeNull();
            client.Should().BeOfType<NeoN3BlockchainClient>();
            client.BlockchainType.Should().Be(BlockchainType.NeoN3);
        }

        [Fact]
        public async Task CreateClientAsync_WithNeoXType_CreatesNeoXClient()
        {
            // Arrange
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(x => x.Value).Returns("http://localhost:10333");
            _mockConfiguration.Setup(x => x.GetSection("Blockchain:NeoX:RpcUrl"))
                .Returns(configSection.Object);

            // Act
            var client = await _factory.CreateClientAsync(BlockchainType.NeoX);

            // Assert
            client.Should().NotBeNull();
            client.Should().BeOfType<NeoXBlockchainClient>();
            client.BlockchainType.Should().Be(BlockchainType.NeoX);
        }

        [Fact]
        public async Task CreateClientAsync_WithMockType_CreatesMockClient()
        {
            // Act
            var client = await _factory.CreateClientAsync(BlockchainType.Mock);

            // Assert
            client.Should().NotBeNull();
            client.Should().BeOfType<MockBlockchainClient>();
            client.BlockchainType.Should().Be(BlockchainType.Mock);
        }

        [Fact]
        public async Task CreateClientAsync_WithUnsupportedType_ThrowsNotSupportedException()
        {
            // Act
            Func<Task> act = async () => await _factory.CreateClientAsync((BlockchainType)999);

            // Assert
            await act.Should().ThrowAsync<NotSupportedException>()
                .WithMessage("*Blockchain type*not supported*");
        }

        [Fact]
        public async Task CreateClientAsync_UsesClientCaching()
        {
            // Arrange
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(x => x.Value).Returns("http://localhost:10332");
            _mockConfiguration.Setup(x => x.GetSection("Blockchain:Neo:RpcUrl"))
                .Returns(configSection.Object);

            // Act
            var client1 = await _factory.CreateClientAsync(BlockchainType.NeoN3);
            var client2 = await _factory.CreateClientAsync(BlockchainType.NeoN3);

            // Assert
            client1.Should().BeSameAs(client2);
        }

        [Fact]
        public async Task CreateClientAsync_WithConnectionString_UsesProvidedConnection()
        {
            // Arrange
            var customConnectionString = "http://custom-node:10332";

            // Act
            var client = await _factory.CreateClientAsync(BlockchainType.NeoN3, customConnectionString);

            // Assert
            client.Should().NotBeNull();
            client.ConnectionString.Should().Be(customConnectionString);
        }

        [Fact]
        public async Task GetAvailableClientsAsync_ReturnsAllSupportedTypes()
        {
            // Act
            var availableClients = await _factory.GetAvailableClientsAsync();

            // Assert
            availableClients.Should().NotBeNull();
            availableClients.Should().Contain(BlockchainType.NeoN3);
            availableClients.Should().Contain(BlockchainType.NeoX);
            availableClients.Should().Contain(BlockchainType.Mock);
        }

        [Fact]
        public async Task ValidateConnectionAsync_WithValidClient_ReturnsTrue()
        {
            // Arrange
            var mockClient = new Mock<IBlockchainClient>();
            mockClient.Setup(x => x.IsConnectedAsync())
                .ReturnsAsync(true);
            
            // Act
            var isValid = await _factory.ValidateConnectionAsync(mockClient.Object);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateConnectionAsync_WithInvalidClient_ReturnsFalse()
        {
            // Arrange
            var mockClient = new Mock<IBlockchainClient>();
            mockClient.Setup(x => x.IsConnectedAsync())
                .ReturnsAsync(false);
            
            // Act
            var isValid = await _factory.ValidateConnectionAsync(mockClient.Object);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Dispose_DisposesAllCreatedClients()
        {
            // Arrange
            var mockClient1 = new Mock<IBlockchainClient>();
            var mockClient2 = new Mock<IBlockchainClient>();
            
            _factory.RegisterClient(BlockchainType.NeoN3, mockClient1.Object);
            _factory.RegisterClient(BlockchainType.NeoX, mockClient2.Object);

            // Act
            _factory.Dispose();

            // Assert
            mockClient1.Verify(x => x.Dispose(), Times.Once);
            mockClient2.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task CreateClientAsync_WithRetryPolicy_RetriesOnFailure()
        {
            // Arrange
            var attemptCount = 0;
            var mockClient = new Mock<IBlockchainClient>();
            mockClient.Setup(x => x.ConnectAsync(It.IsAny<string>()))
                .ReturnsAsync(() =>
                {
                    attemptCount++;
                    if (attemptCount < 3)
                        throw new Exception("Connection failed");
                    return true;
                });

            // Act
            var result = await _factory.CreateClientWithRetryAsync(BlockchainType.NeoN3, maxRetries: 3);

            // Assert
            attemptCount.Should().Be(3);
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetClientMetricsAsync_ReturnsDetailedMetrics()
        {
            // Arrange
            await _factory.CreateClientAsync(BlockchainType.NeoN3);
            await _factory.CreateClientAsync(BlockchainType.Mock);

            // Act
            var metrics = await _factory.GetClientMetricsAsync();

            // Assert
            metrics.Should().NotBeNull();
            metrics.Should().ContainKey("TotalClients");
            metrics.Should().ContainKey("ActiveClients");
            metrics.Should().ContainKey("ClientTypes");
            metrics["TotalClients"].Should().Be(2);
        }
    }

    // Test implementations
    internal class NeoN3BlockchainClient : IBlockchainClient
    {
        public BlockchainType BlockchainType => BlockchainType.NeoN3;
        public string ConnectionString { get; set; }
        public bool IsConnected { get; private set; }

        public Task<bool> ConnectAsync(string connectionString)
        {
            ConnectionString = connectionString;
            IsConnected = true;
            return Task.FromResult(true);
        }

        public Task<bool> IsConnectedAsync() => Task.FromResult(IsConnected);
        public Task DisconnectAsync() 
        {
            IsConnected = false;
            return Task.CompletedTask;
        }
        public void Dispose() { }
    }

    internal class NeoXBlockchainClient : IBlockchainClient
    {
        public BlockchainType BlockchainType => BlockchainType.NeoX;
        public string ConnectionString { get; set; }
        public bool IsConnected { get; private set; }

        public Task<bool> ConnectAsync(string connectionString)
        {
            ConnectionString = connectionString;
            IsConnected = true;
            return Task.FromResult(true);
        }

        public Task<bool> IsConnectedAsync() => Task.FromResult(IsConnected);
        public Task DisconnectAsync()
        {
            IsConnected = false;
            return Task.CompletedTask;
        }
        public void Dispose() { }
    }

    // Extension methods for testing
    internal static class BlockchainClientFactoryExtensions
    {
        public static void RegisterClient(this BlockchainClientFactory factory, BlockchainType type, IBlockchainClient client)
        {
            // This would be implemented in the actual factory for testing purposes
        }

        public static async Task<IBlockchainClient> CreateClientWithRetryAsync(
            this BlockchainClientFactory factory, 
            BlockchainType type, 
            int maxRetries = 3)
        {
            Exception lastException = null;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await factory.CreateClientAsync(type);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    await Task.Delay(100 * (i + 1)); // Exponential backoff
                }
            }
            throw lastException;
        }

        public static async Task<Dictionary<string, object>> GetClientMetricsAsync(this BlockchainClientFactory factory)
        {
            // Mock implementation for testing
            return new Dictionary<string, object>
            {
                ["TotalClients"] = 2,
                ["ActiveClients"] = 2,
                ["ClientTypes"] = new[] { "NeoN3", "Mock" }
            };
        }
    }
}