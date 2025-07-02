using NeoServiceLayer.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using System.Text;

// Set test encryption key
Environment.SetEnvironmentVariable("ENCLAVE_MASTER_KEY", "test-encryption-key-for-debug");

// Create a simple console logger
var loggerFactory = LoggerFactory.Create(builder => 
    builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
var logger = loggerFactory.CreateLogger<OcclumFileStorageProvider>();

// Create test storage path
var testStoragePath = Path.Combine(Path.GetTempPath(), $"debug-storage-{Guid.NewGuid():N}");
Console.WriteLine($"Test storage path: {testStoragePath}");

// Create provider and initialize
var provider = new OcclumFileStorageProvider(testStoragePath, logger);
await provider.InitializeAsync();

// Store some test data
var testData = new Dictionary<string, byte[]>
{
    { "test_key1", Encoding.UTF8.GetBytes("test_data1") },
    { "test_key2", Encoding.UTF8.GetBytes("test_data2") },
    { "test_key3", Encoding.UTF8.GetBytes("test_data3") }
};

var options = new StorageOptions();
foreach (var item in testData)
{
    var result = await provider.StoreAsync(item.Key, item.Value, options);
    Console.WriteLine($"Stored {item.Key}: {result}");
}

// Verify data exists before backup
Console.WriteLine("\n--- Before backup ---");
foreach (var key in testData.Keys)
{
    var data = await provider.RetrieveAsync(key);
    Console.WriteLine($"Retrieve {key}: {(data != null ? Encoding.UTF8.GetString(data) : "NULL")}");
}

// List all keys
var allKeys = await provider.ListKeysAsync();
Console.WriteLine($"All keys before backup: [{string.Join(", ", allKeys)}]");

// Create backup
var backupPath = Path.Combine(Path.GetTempPath(), $"debug-backup-{Guid.NewGuid():N}.zip");
Console.WriteLine($"\nBackup path: {backupPath}");
var backupResult = await provider.BackupAsync(backupPath);
Console.WriteLine($"Backup result: {backupResult}");

// Check what's in the storage directory
Console.WriteLine($"\n--- Storage directory contents ---");
if (Directory.Exists(testStoragePath))
{
    var files = Directory.GetFiles(testStoragePath, "*", SearchOption.AllDirectories);
    foreach (var file in files)
    {
        Console.WriteLine($"File: {Path.GetRelativePath(testStoragePath, file)} ({new FileInfo(file).Length} bytes)");
    }
}

// Delete all data to simulate loss
foreach (var key in testData.Keys)
{
    await provider.DeleteAsync(key);
}

// Verify data is gone
Console.WriteLine("\n--- After deletion ---");
foreach (var key in testData.Keys)
{
    var data = await provider.RetrieveAsync(key);
    Console.WriteLine($"Retrieve {key}: {(data != null ? Encoding.UTF8.GetString(data) : "NULL")}");
}

var allKeysAfterDeletion = await provider.ListKeysAsync();
Console.WriteLine($"All keys after deletion: [{string.Join(", ", allKeysAfterDeletion)}]");

// Restore from backup
Console.WriteLine($"\n--- Restoring from backup ---");
var restoreResult = await provider.RestoreAsync(backupPath);
Console.WriteLine($"Restore result: {restoreResult}");

// Check storage directory after restore
Console.WriteLine($"\n--- Storage directory contents after restore ---");
if (Directory.Exists(testStoragePath))
{
    var files = Directory.GetFiles(testStoragePath, "*", SearchOption.AllDirectories);
    foreach (var file in files)
    {
        Console.WriteLine($"File: {Path.GetRelativePath(testStoragePath, file)} ({new FileInfo(file).Length} bytes)");
    }
}

// Verify restored data
Console.WriteLine("\n--- After restore ---");
foreach (var key in testData.Keys)
{
    var data = await provider.RetrieveAsync(key);
    Console.WriteLine($"Retrieve {key}: {(data != null ? Encoding.UTF8.GetString(data) : "NULL")}");
}

var allKeysAfterRestore = await provider.ListKeysAsync();
Console.WriteLine($"All keys after restore: [{string.Join(", ", allKeysAfterRestore)}]");

// Cleanup
provider.Dispose();
Console.WriteLine("\nDebug complete.");