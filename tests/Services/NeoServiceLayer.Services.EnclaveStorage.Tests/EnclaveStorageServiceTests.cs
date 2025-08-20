extern alias EnclaveStorageAlias;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Configuration;
using NeoServiceLayer.ServiceFramework;
using EnclaveStorageAlias::NeoServiceLayer.Services.EnclaveStorage;
using EnclaveModels = EnclaveStorageAlias::NeoServiceLayer.Services.EnclaveStorage.Models;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Text;

namespace NeoServiceLayer.Services.EnclaveStorage.Tests
{
    public class EnclaveStorageServiceTests
    {
        private readonly Mock<ILogger<EnclaveStorageService>> _mockLogger;
        private readonly Mock<NeoServiceLayer.Core.Configuration.IServiceConfiguration> _mockConfiguration;
        private readonly Mock<IEnclaveManager> _mockEnclaveManager;
        private readonly EnclaveStorageService _service;

        public EnclaveStorageServiceTests()
        {
            _mockLogger = new Mock<ILogger<EnclaveStorageService>>();
            _mockConfiguration = new Mock<NeoServiceLayer.Core.Configuration.IServiceConfiguration>();
            _mockEnclaveManager = new Mock<IEnclaveManager>();

            _mockConfiguration.Setup(x => x.GetValue(It.IsAny<string>(), It.IsAny<long>()))
                .Returns(1073741824L); // 1GB

            _mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);

            _service = new EnclaveStorageService(_mockEnclaveManager.Object, _mockConfiguration.Object, _mockLogger.Object);
        }

        [Fact]
        public void Service_ShouldHaveCorrectName()
        {
            // Assert
            _service.Name.Should().Be("EnclaveStorage");
        }

        [Fact]
        public void Service_ShouldHaveCorrectDescription()
        {
            // Assert
            _service.Description.Should().Contain("Secure Enclave Storage Service");
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
            _service.Capabilities.Should().Contain(typeof(IEnclaveStorageService));
        }

        [Fact]
        public async Task SealDataAsync_ShouldValidateParameters()
        {
            // Arrange
            var request = new EnclaveModels.SealDataRequest
            {
                Key = "",
                Data = new byte[] { 1, 2, 3, 4, 5 },
                Policy = new EnclaveModels.SealingPolicy { Type = EnclaveModels.SealingPolicyType.MrEnclave }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.SealDataAsync(request, BlockchainType.NeoN3));
        }

        [Fact]
        public async Task SealDataAsync_ShouldValidateDataNotEmpty()
        {
            // Arrange
            var request = new EnclaveModels.SealDataRequest
            {
                Key = "test-key",
                Data = Array.Empty<byte>(),
                Policy = new EnclaveModels.SealingPolicy { Type = EnclaveModels.SealingPolicyType.MrEnclave }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.SealDataAsync(request, BlockchainType.NeoN3));
        }

        [Fact]
        public async Task UnsealDataAsync_ShouldThrowForNonExistentKey()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UnsealDataAsync("non-existent-key", BlockchainType.NeoN3));
        }

        [Fact]
        public async Task DeleteSealedDataAsync_ShouldReturnFalseForNonExistentKey()
        {
            // Act
            var result = await _service.DeleteSealedDataAsync("non-existent-key", BlockchainType.NeoN3);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Deleted.Should().BeFalse();
        }

        [Fact]
        public async Task SealDataAsync_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var testData = Encoding.UTF8.GetBytes("test data content");
            var sealRequest = new EnclaveModels.SealDataRequest
            {
                Key = "test-key",
                Data = testData,
                Policy = new EnclaveModels.SealingPolicy { Type = EnclaveModels.SealingPolicyType.MrEnclave, ExpirationHours = 24 }
            };

            // Act
            var sealResult = await _service.SealDataAsync(sealRequest, BlockchainType.NeoN3);

            // Assert
            sealResult.Should().NotBeNull();
            sealResult.Success.Should().BeTrue();
            sealResult.StorageId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetStorageStatisticsAsync_ShouldNotThrow()
        {
            // Act
            var result = await _service.GetStorageStatisticsAsync(BlockchainType.NeoN3);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().BeGreaterOrEqualTo(0);
            result.TotalSize.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public async Task BackupSealedDataAsync_ShouldNotThrowWithValidRequest()
        {
            // Arrange
            var request = new EnclaveModels.BackupRequest
            {
                BackupLocation = "/backup/test",
                IncludeMetadata = true
            };

            // Act
            var result = await _service.BackupSealedDataAsync(request, BlockchainType.NeoN3);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.BackupId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ListSealedItemsAsync_ShouldNotThrow()
        {
            // Arrange
            var request = new EnclaveModels.ListSealedItemsRequest
            {
                Service = "TestService",
                PageSize = 10
            };

            // Act
            var result = await _service.ListSealedItemsAsync(request, BlockchainType.NeoN3);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().NotBeNull();
        }

        [Fact]
        public void Service_ShouldValidateUnsupportedBlockchain()
        {
            // Assert
            _service.SupportsBlockchain((BlockchainType)999).Should().BeFalse();
        }
    }
}
