using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core.Http;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Services.Backup;
using NeoServiceLayer.TestInfrastructure;
using Xunit;

namespace NeoServiceLayer.Services.Backup.Tests;

public class BackupServiceTests : TestBase
{
    private readonly Mock<ILogger<BackupService>> _loggerMock;
    private readonly Mock<IBlockchainClientFactory> _blockchainClientFactoryMock;
    private readonly Mock<IHttpClientService> _httpClientServiceMock;
    private readonly BackupService _service;

    public BackupServiceTests()
    {
        _loggerMock = new Mock<ILogger<BackupService>>();
        _blockchainClientFactoryMock = new Mock<IBlockchainClientFactory>();
        _httpClientServiceMock = new Mock<IHttpClientService>();

        // BackupService expects IBlockchainClientFactory and IHttpClientService
        _service = new BackupService(_loggerMock.Object, _blockchainClientFactoryMock.Object, _httpClientServiceMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act & Assert
        _service.Should().NotBeNull();
    }

    // TODO: Add comprehensive tests for all service methods
    // TODO: Add enclave integration tests
    // TODO: Add error handling tests
    // TODO: Add performance tests
}
