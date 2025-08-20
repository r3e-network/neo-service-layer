using System;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.AI.Prediction;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Tee.Host.Tests;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.AI.Prediction.Tests;

/// <summary>
/// Shared test fixture for AI Prediction tests to optimize performance.
/// This fixture shares expensive setup across all tests in the collection.
/// </summary>
public class OptimizedPredictionTestFixture : IDisposable
{
    public Mock<ILogger<PredictionService>> MockLogger { get; }
    public Mock<IServiceConfiguration> MockServiceConfiguration { get; }
    public Mock<IPersistentStorageProvider> MockStorageProvider { get; }
    public IEnclaveManager EnclaveManager { get; }
    public TestEnclaveWrapper TestEnclaveWrapper { get; }
    
    // Cached test data
    public double[][] SmallTestDataset { get; }
    public double[][] MediumTestDataset { get; }
    public object CachedModel { get; private set; }
    
    public OptimizedPredictionTestFixture()
    {
        // Initialize mocks once for all tests
        MockLogger = new Mock<ILogger<PredictionService>>();
        MockServiceConfiguration = new Mock<IServiceConfiguration>();
        MockStorageProvider = new Mock<IPersistentStorageProvider>();
        
        // Setup configuration once
        SetupConfiguration();
        SetupStorageProvider();
        
        // Create shared enclave manager
        var enclaveManagerLogger = new Mock<ILogger<EnclaveManager>>();
        TestEnclaveWrapper = new TestEnclaveWrapper();
        EnclaveManager = new EnclaveManager(enclaveManagerLogger.Object, TestEnclaveWrapper);
        
        // Pre-generate test datasets
        SmallTestDataset = GenerateTestData(100);   // Small dataset for unit tests
        MediumTestDataset = GenerateTestData(500);  // Medium dataset for integration tests
        
        // Pre-initialize enclave manager
        EnclaveManager.InitializeAsync().Wait();
    }
    
    private void SetupConfiguration()
    {
        var configSection = new Mock<Microsoft.Extensions.Configuration.IConfigurationSection>();
        configSection.Setup(x => x.Value).Returns("test-value");
        configSection.Setup(x => x["MaxPredictions"]).Returns("100");
        configSection.Setup(x => x["EnableCaching"]).Returns("true");
        
        MockServiceConfiguration.Setup(x => x.GetSection(It.IsAny<string>()))
            .Returns(configSection.Object);
    }
    
    private void SetupStorageProvider()
    {
        MockStorageProvider.Setup(x => x.StoreAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>()))
            .ReturnsAsync(true);
        MockStorageProvider.Setup(x => x.RetrieveAsync(It.IsAny<string>()))
            .ReturnsAsync((byte[]?)null);
    }
    
    private double[][] GenerateTestData(int size)
    {
        var random = new Random(42); // Fixed seed for reproducibility
        var data = new double[size][];
        
        for (int i = 0; i < size; i++)
        {
            data[i] = new double[10]; // 10 features
            for (int j = 0; j < 10; j++)
            {
                data[i][j] = random.NextDouble();
            }
        }
        
        return data;
    }
    
    public void Dispose()
    {
        // Cleanup if needed
        TestEnclaveWrapper?.Dispose();
    }
}

/// <summary>
/// Collection definition for AI Prediction tests to enable parallel execution
/// </summary>
[CollectionDefinition("AI Prediction Tests")]
public class AIPredictionTestCollection : ICollectionFixture<OptimizedPredictionTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}