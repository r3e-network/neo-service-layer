using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.TestInfrastructure;
using IBlockchainClient = NeoServiceLayer.Infrastructure.IBlockchainClient;
using IBlockchainClientFactory = NeoServiceLayer.Infrastructure.IBlockchainClientFactory;

namespace NeoServiceLayer.ServiceFramework.Tests;

/// <summary>
/// Base class for service tests providing common setup and utilities.
/// </summary>
public abstract class TestBase
{
    protected Mock<ILogger> MockLogger { get; private set; }
    protected Mock<IServiceConfiguration> MockConfiguration { get; private set; }
    protected Mock<IEnclaveManager> MockEnclaveManager { get; private set; }
    protected Mock<IBlockchainClientFactory> MockBlockchainClientFactory { get; private set; }
    protected Mock<IBlockchainClient> MockBlockchainClient { get; private set; }

    protected TestBase()
    {
        MockLogger = new Mock<ILogger>();
        MockConfiguration = new Mock<IServiceConfiguration>();
        MockEnclaveManager = new Mock<IEnclaveManager>();
        MockBlockchainClientFactory = new Mock<IBlockchainClientFactory>();
        MockBlockchainClient = new Mock<IBlockchainClient>();
        
        SetupMockConfiguration();
        SetupMockEnclaveManager();
        SetupMockBlockchainClient();
    }

    protected virtual void SetupMockConfiguration()
    {
        // Setup default configuration values
        MockConfiguration.Setup(x => x.GetValue<string>("TestKey")).Returns("TestValue");
        MockConfiguration.Setup(x => x.GetValue<string>(It.IsAny<string>())).Returns("DefaultValue");
        MockConfiguration.Setup(x => x.GetValue<int>(It.IsAny<string>())).Returns(10);
        MockConfiguration.Setup(x => x.GetValue<bool>(It.IsAny<string>())).Returns(true);
        MockConfiguration.Setup(x => x.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns<string, string>((key, defaultValue) => defaultValue);
        MockConfiguration.Setup(x => x.GetValue(It.IsAny<string>(), It.IsAny<int>())).Returns<string, int>((key, defaultValue) => defaultValue);
        MockConfiguration.Setup(x => x.GetValue(It.IsAny<string>(), It.IsAny<bool>())).Returns<string, bool>((key, defaultValue) => defaultValue);
        
        // Setup service-specific configuration
        MockConfiguration.Setup(x => x.GetValue("Oracle:MaxConcurrentRequests", "10")).Returns("10");
        MockConfiguration.Setup(x => x.GetValue("Oracle:DefaultTimeout", "30000")).Returns("30000");
        MockConfiguration.Setup(x => x.GetValue("Storage:ChunkSize", "1048576")).Returns("1048576");
        MockConfiguration.Setup(x => x.GetValue("Storage:MaxFileSize", "104857600")).Returns("104857600");
    }

    protected virtual void SetupMockEnclaveManager()
    {
        // Setup enclave manager to simulate successful initialization
        MockEnclaveManager.Setup(x => x.InitializeEnclaveAsync()).ReturnsAsync(true);
        MockEnclaveManager.Setup(x => x.DestroyEnclaveAsync()).ReturnsAsync(true);
        
        // Setup JavaScript execution
        MockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"result\": \"test_execution_result\"}");
        
        // Setup data retrieval
        MockEnclaveManager.Setup(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"value\": 42, \"timestamp\": \"2025-01-22T10:00:00Z\"}");
        
        // Setup random number generation
        MockEnclaveManager.Setup(x => x.GenerateRandomAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(42);
        
        // Setup encryption/decryption
        MockEnclaveManager.Setup(x => x.EncryptAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .ReturnsAsync((byte[] data, byte[] key) => data.Concat(new byte[] { 0xFF }).ToArray());
        
        MockEnclaveManager.Setup(x => x.DecryptAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .ReturnsAsync((byte[] data, byte[] key) => data.Take(data.Length - 1).ToArray());
        
        // Setup signing/verification
        MockEnclaveManager.Setup(x => x.SignAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03, 0x04 });
        
        MockEnclaveManager.Setup(x => x.VerifyAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .ReturnsAsync(true);
    }

    protected virtual void SetupMockBlockchainClient()
    {
        // Setup blockchain client factory
        MockBlockchainClientFactory.Setup(x => x.CreateClient(It.IsAny<BlockchainType>()))
            .Returns(MockBlockchainClient.Object);
        
        // Setup blockchain client operations
        MockBlockchainClient.Setup(x => x.GetBlockHeightAsync()).ReturnsAsync(1000);
        MockBlockchainClient.Setup(x => x.GetBlockHashAsync(It.IsAny<long>()))
            .ReturnsAsync("0x1234567890abcdef");
        MockBlockchainClient.Setup(x => x.SendTransactionAsync(It.IsAny<Transaction>()))
            .ReturnsAsync("0xabcdef1234567890");
    }

    protected T CreateService<T>() where T : class
    {
        // This method should be overridden in derived test classes
        throw new NotImplementedException("CreateService must be implemented in derived test classes");
    }

    protected void VerifyLoggerCalled<T>(Mock<ILogger<T>> logger, LogLevel level, string message)
    {
        logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    protected async Task<T> InitializeServiceAsync<T>(T service) where T : IService
    {
        await service.InitializeAsync();
        await service.StartAsync();
        return service;
    }

    protected async Task CleanupServiceAsync<T>(T service) where T : IService
    {
        if (service.IsRunning)
        {
            await service.StopAsync();
        }
    }
}
