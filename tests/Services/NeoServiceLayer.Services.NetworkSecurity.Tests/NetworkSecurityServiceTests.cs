using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using CoreConfig = NeoServiceLayer.Core.Configuration;
using CoreIServiceConfiguration = NeoServiceLayer.Core.Configuration.IServiceConfiguration;
using NeoServiceLayer.Services.NetworkSecurity;
using NeoServiceLayer.Services.NetworkSecurity.Models;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.NetworkSecurity.Tests
{
    public class NetworkSecurityServiceTests
    {
        private readonly Mock<ILogger<NetworkSecurityService>> _mockLogger;
        private readonly Mock<CoreIServiceConfiguration> _mockConfiguration;
        private readonly Mock<IEnclaveManager> _mockEnclaveManager;
        private readonly NetworkSecurityService _service;

        public NetworkSecurityServiceTests()
        {
            _mockLogger = new Mock<ILogger<NetworkSecurityService>>();
            _mockConfiguration = new Mock<CoreIServiceConfiguration>();
            _mockEnclaveManager = new Mock<IEnclaveManager>();

            _mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);

            _service = new NetworkSecurityService(_mockEnclaveManager.Object, _mockConfiguration.Object, _mockLogger.Object);
        }

        [Fact]
        public void Service_ShouldHaveCorrectName()
        {
            // Assert
            _service.Name.Should().Be("NetworkSecurity");
        }

        [Fact]
        public void Service_ShouldHaveCorrectDescription()
        {
            // Assert
            _service.Description.Should().Contain("Secure Network Communication Service");
        }

        [Fact]
        public void Service_ShouldHaveCorrectVersion()
        {
            // Assert
            _service.Version.Should().Be("1.0.0");
        }

        [Fact]
        public void Service_ShouldSupportCorrectBlockchains()
        {
            // Assert
            _service.SupportsBlockchain(BlockchainType.NeoN3).Should().BeTrue();
            _service.SupportsBlockchain(BlockchainType.NeoX).Should().BeTrue();
        }

        [Fact]
        public void Service_ShouldHaveCorrectCapabilities()
        {
            // Assert
            _service.Capabilities.Should().Contain(typeof(INetworkSecurityService));
        }

        [Fact]
        public async Task CreateSecureChannelAsync_ShouldValidateRequest()
        {
            // Arrange
            var request = new CreateChannelRequest
            {
                ChannelName = "",
                TargetEndpoint = "https://api.example.com",
                Protocol = NetworkProtocol.Https
            };

            // Act & Assert
            var result = await _service.CreateSecureChannelAsync(request, BlockchainType.NeoN3);
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task SendMessageAsync_ShouldValidateChannelId()
        {
            // Arrange
            var message = new NetworkMessage
            {
                Payload = "test message",
                Headers = new Dictionary<string, string>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.SendMessageAsync("non-existent-channel", message, BlockchainType.NeoN3));
        }

        [Fact]
        public async Task SendMessageAsync_ShouldValidateMessage()
        {
            // Arrange
            var message = new NetworkMessage
            {
                Payload = "test message",
                Headers = new Dictionary<string, string>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.SendMessageAsync("test-channel", message, BlockchainType.NeoN3));
        }

        [Fact]
        public async Task ConfigureFirewallAsync_ShouldValidateRules()
        {
            // Arrange
            var ruleSet = new FirewallRuleSet
            {
                Rules = new List<FirewallRule>(),
                DefaultAction = FirewallAction.Deny
            };

            // Act
            var result = await _service.ConfigureFirewallAsync(ruleSet, BlockchainType.NeoN3);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task MonitorNetworkAsync_ShouldValidateRequest()
        {
            // Arrange
            var request = new MonitoringRequest
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                EndTime = DateTime.UtcNow
            };

            // Act
            var result = await _service.MonitorNetworkAsync(request, BlockchainType.NeoN3);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task CloseChannelAsync_ShouldValidateChannelId()
        {
            // Act
            var result = await _service.CloseChannelAsync("non-existent-channel", BlockchainType.NeoN3);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetChannelStatusAsync_ShouldValidateChannelId()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GetChannelStatusAsync("non-existent-channel", BlockchainType.NeoN3));
        }

        [Fact]
        public async Task CreateSecureChannelAsync_WithValidRequest_ShouldNotThrow()
        {
            // Arrange
            var request = new CreateChannelRequest
            {
                ChannelName = "test-channel",
                TargetEndpoint = "https://api.example.com",
                Protocol = NetworkProtocol.Https
            };

            // Act
            var result = await _service.CreateSecureChannelAsync(request, BlockchainType.NeoN3);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.ChannelId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ConfigureFirewallAsync_WithValidRules_ShouldNotThrow()
        {
            // Arrange
            var ruleSet = new FirewallRuleSet
            {
                Rules = new List<FirewallRule>
                {
                    new FirewallRule
                    {
                        Name = "Allow HTTPS",
                        Action = FirewallAction.Allow,
                        Source = "0.0.0.0/0",
                        Destination = "10.0.0.0/8",
                        Port = "443",
                        Protocol = "TCP"
                    }
                },
                DefaultAction = FirewallAction.Deny
            };

            // Act
            var result = await _service.ConfigureFirewallAsync(ruleSet, BlockchainType.NeoN3);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task MonitorNetworkAsync_WithValidRequest_ShouldNotThrow()
        {
            // Arrange
            var request = new MonitoringRequest
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                EndTime = DateTime.UtcNow
            };

            // Act
            var result = await _service.MonitorNetworkAsync(request, BlockchainType.NeoN3);

            // Assert
            result.Should().NotBeNull();
            result.Statistics.Should().NotBeNull();
            result.SecurityEvents.Should().NotBeNull();
        }
    }
}
