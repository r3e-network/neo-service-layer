using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Services.Configuration;
using NeoServiceLayer.Services.Configuration.Models;
using NeoServiceLayer.Tee.Host.Services;

class Program
{
    static async Task Main(string[] args)
    {
        var testStoragePath = Path.Combine(Path.GetTempPath(), $"debug-config-test-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("ENCLAVE_MASTER_KEY", "test-encryption-key-for-debug");
        
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        // Add persistent storage
        services.AddSingleton<IPersistentStorageProvider>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<OcclumFileStorageProvider>>();
            return new OcclumFileStorageProvider(testStoragePath, logger);
        });
        
        // Mock enclave manager
        var mockEnclaveManager = new Mock<IEnclaveManager>();
        mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);
        mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                          .Returns(Task.CompletedTask);
        mockEnclaveManager.Setup(x => x.InitializeEnclaveAsync()).ReturnsAsync(true);
        
        // Setup JavaScript operations
        mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                          .Returns<string, CancellationToken>((script, ct) =>
                          {
                              Console.WriteLine($"JavaScript call: {script}");
                              if (script.Contains("validateConfigurationValue"))
                                  return Task.FromResult("true");
                              if (script.Contains("initializeConfigurationEncryption"))
                                  return Task.FromResult("true");
                              if (script.Contains("startConfigurationMonitoring"))
                                  return Task.FromResult("true");
                              if (script.Contains("stopConfigurationMonitoring"))
                                  return Task.FromResult("true");
                              if (script.Contains("configurationHealthCheck"))
                                  return Task.FromResult("true");
                              return Task.FromResult("{}");
                          });
                          
        // Setup storage operations
        mockEnclaveManager.Setup(x => x.StorageStoreDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                          .Returns<string, string, string, CancellationToken>((key, data, encKey, ct) =>
                          {
                              Console.WriteLine($"Storage store: {key}");
                              return Task.FromResult("{\"success\": true}");
                          });
                          
        mockEnclaveManager.Setup(x => x.StorageListKeysAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync("[]");
        
        services.AddSingleton(mockEnclaveManager.Object);
        
        // Add service configuration
        services.AddSingleton<IServiceConfiguration>(provider =>
        {
            var mockConfig = new Mock<IServiceConfiguration>();
            mockConfig.Setup(x => x.GetValue(It.IsAny<string>(), It.IsAny<string>()))
                     .Returns((string key, string defaultValue) => defaultValue);
            return mockConfig.Object;
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Initialize storage
        var storageProvider = serviceProvider.GetRequiredService<IPersistentStorageProvider>();
        await storageProvider.InitializeAsync();
        
        // Create ConfigurationService
        var configService = new ConfigurationService(
            serviceProvider.GetRequiredService<ILogger<ConfigurationService>>(),
            mockEnclaveManager.Object,
            serviceProvider.GetRequiredService<IServiceConfiguration>(),
            storageProvider);
        
        try
        {
            Console.WriteLine("Initializing ConfigurationService...");
            await configService.InitializeAsync();
            await configService.StartAsync();
            Console.WriteLine("ConfigurationService started successfully");
            
            // Test setting a configuration
            Console.WriteLine("\nSetting configuration...");
            var setRequest = new SetConfigurationRequest
            {
                Key = "test.setting",
                Value = "test.value"
            };
            var setResult = await configService.SetConfigurationAsync(setRequest, BlockchainType.NeoN3);
            Console.WriteLine($"Set result - Success: {setResult.Success}, Version: {setResult.NewVersion}");
            
            // Test getting the configuration immediately
            Console.WriteLine("\nGetting configuration immediately...");
            var getRequest = new GetConfigurationRequest
            {
                Key = "test.setting"
            };
            var getResult = await configService.GetConfigurationAsync(getRequest, BlockchainType.NeoN3);
            Console.WriteLine($"Get result - Found: {getResult.Found}, Success: {getResult.Success}, Value: '{getResult.Value}'");
            
            // List storage keys to see what's persisted
            Console.WriteLine("\nListing storage keys...");
            var allKeys = await storageProvider.ListKeysAsync();
            Console.WriteLine($"Storage keys: [{string.Join(", ", allKeys)}]");
            
            // Stop and restart service
            Console.WriteLine("\nStopping ConfigurationService...");
            await configService.StopAsync();
            
            Console.WriteLine("Creating new ConfigurationService instance...");
            var newConfigService = new ConfigurationService(
                serviceProvider.GetRequiredService<ILogger<ConfigurationService>>(),
                mockEnclaveManager.Object,
                serviceProvider.GetRequiredService<IServiceConfiguration>(),
                storageProvider);
            
            await newConfigService.InitializeAsync();
            await newConfigService.StartAsync();
            Console.WriteLine("New ConfigurationService started successfully");
            
            // Test getting the configuration after restart
            Console.WriteLine("\nGetting configuration after restart...");
            var getResult2 = await newConfigService.GetConfigurationAsync(getRequest, BlockchainType.NeoN3);
            Console.WriteLine($"Get result after restart - Found: {getResult2.Found}, Success: {getResult2.Success}, Value: '{getResult2.Value}'");
            
            if (getResult2.Found && getResult2.Value == "test.value")
            {
                Console.WriteLine("✅ SUCCESS: Configuration survived restart!");
            }
            else
            {
                Console.WriteLine("❌ FAILED: Configuration did not survive restart");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
        }
        finally
        {
            serviceProvider.Dispose();
            if (Directory.Exists(testStoragePath))
            {
                Directory.Delete(testStoragePath, true);
            }
        }
    }
}