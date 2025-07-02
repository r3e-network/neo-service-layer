using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Tee.Host.Services;
using Moq;
using System.Text;

// Set test encryption key
Environment.SetEnvironmentVariable("ENCLAVE_MASTER_KEY", "test-encryption-key-for-debug");

// Create a simple console logger
var loggerFactory = LoggerFactory.Create(builder => 
    builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

// Create mock storage and metadata
var mockStorage = new Dictionary<string, string>();
var mockMetadata = new Dictionary<string, string>();

// Setup mock enclave manager
var mockEnclaveManager = new Mock<IEnclaveManager>();

// Setup basic enclave operations
mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);
mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
mockEnclaveManager.Setup(x => x.InitializeEnclaveAsync()).ReturnsAsync(true);

// Setup storage operations with proper state management
mockEnclaveManager.Setup(x => x.StorageStoreDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns<string, string, string, CancellationToken>((key, data, encKey, ct) =>
                  {
                      Console.WriteLine($"Mock storing: {key} = {data}");
                      mockStorage[key] = data;
                      return Task.FromResult("{\"success\": true}");
                  });

mockEnclaveManager.Setup(x => x.StorageRetrieveDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns<string, string, CancellationToken>((key, encKey, ct) => 
                  {
                      Console.WriteLine($"Mock retrieving: {key}");
                      if (mockStorage.TryGetValue(key, out var data))
                      {
                          Console.WriteLine($"Mock found: {key} = {data}");
                          return Task.FromResult(data);
                      }
                      Console.WriteLine($"Mock NOT found: {key}");
                      throw new KeyNotFoundException($"Key '{key}' not found in mock storage");
                  });

// Setup JavaScript operations with proper metadata management
mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns<string, CancellationToken>((script, ct) =>
                  {
                      Console.WriteLine($"Mock JS: {script}");
                      if (script.Contains("storeMetadata"))
                      {
                          // Extract key and metadata from script
                          var keyStart = script.IndexOf("'") + 1;
                          var keyEnd = script.IndexOf("'", keyStart);
                          var key = script.Substring(keyStart, keyEnd - keyStart);
                          var metadataStart = script.IndexOf('{');
                          var metadata = script.Substring(metadataStart);
                          Console.WriteLine($"Mock storing metadata: {key} = {metadata}");
                          mockMetadata[key] = metadata;
                          return Task.FromResult("true");
                      }
                      if (script.Contains("getMetadata"))
                      {
                          // Extract key from script
                          var keyStart = script.IndexOf("'") + 1;
                          var keyEnd = script.IndexOf("'", keyStart);
                          var key = script.Substring(keyStart, keyEnd - keyStart);
                          Console.WriteLine($"Mock getting metadata: {key}");
                          if (mockMetadata.TryGetValue(key, out var metadata))
                          {
                              Console.WriteLine($"Mock found metadata: {key} = {metadata}");
                              return Task.FromResult(metadata);
                          }
                          Console.WriteLine($"Mock metadata NOT found: {key}");
                          return Task.FromResult("null");
                      }
                      return Task.FromResult("{}");
                  });

// Add mock configuration
var mockConfig = new Mock<IServiceConfiguration>();
mockConfig.Setup(x => x.GetValue(It.IsAny<string>(), It.IsAny<string>()))
         .Returns((string key, string defaultValue) => defaultValue);

services.AddSingleton(mockEnclaveManager.Object);
services.AddSingleton(mockConfig.Object);
services.AddSingleton<IStorageService, StorageService>();

var serviceProvider = services.BuildServiceProvider();
var storageService = serviceProvider.GetRequiredService<IStorageService>();

Console.WriteLine("Initializing StorageService...");
await storageService.InitializeAsync();
await storageService.StartAsync();

Console.WriteLine("Storing data...");
const string key = "persistent_test_key";
var data = Encoding.UTF8.GetBytes("persistent_test_data");
var options = new NeoServiceLayer.Services.Storage.StorageOptions { Encrypt = false, Compress = false };

var metadata = await storageService.StoreDataAsync(key, data, options, BlockchainType.NeoN3);
Console.WriteLine($"Stored metadata: {System.Text.Json.JsonSerializer.Serialize(metadata)}");

Console.WriteLine("Retrieving data...");
var retrievedData = await storageService.GetDataAsync(key, BlockchainType.NeoN3);

Console.WriteLine($"Original data: {Encoding.UTF8.GetString(data)} ({data.Length} bytes)");
Console.WriteLine($"Retrieved data: {Encoding.UTF8.GetString(retrievedData)} ({retrievedData.Length} bytes)");
Console.WriteLine($"Data match: {data.SequenceEqual(retrievedData)}");