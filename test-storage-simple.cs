using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

// Simple test to validate basic file operations work
class SimpleStorageTest
{
    static async Task Main(string[] args)
    {
        try
        {
            var testDir = "/tmp/storage-test";
            var testFile = Path.Combine(testDir, "test.dat");
            
            // Create directory
            Directory.CreateDirectory(testDir);
            
            // Test data
            var testData = "Hello, Persistent Storage Implementation!";
            var testBytes = Encoding.UTF8.GetBytes(testData);
            
            // Write data
            await File.WriteAllBytesAsync(testFile, testBytes);
            Console.WriteLine("✅ Write operation completed");
            
            // Read data
            var readBytes = await File.ReadAllBytesAsync(testFile);
            var readData = Encoding.UTF8.GetString(readBytes);
            
            if (readData == testData)
            {
                Console.WriteLine("✅ Read operation verified successfully");
            }
            else
            {
                Console.WriteLine("❌ Read operation failed - data mismatch");
                return;
            }
            
            // Check file exists
            if (File.Exists(testFile))
            {
                Console.WriteLine("✅ File existence check passed");
            }
            else
            {
                Console.WriteLine("❌ File existence check failed");
                return;
            }
            
            // Delete file
            File.Delete(testFile);
            
            if (!File.Exists(testFile))
            {
                Console.WriteLine("✅ File deletion verified");
            }
            else
            {
                Console.WriteLine("❌ File deletion failed");
                return;
            }
            
            // Cleanup
            Directory.Delete(testDir, true);
            
            Console.WriteLine("🎉 All basic file operations working correctly!");
            Console.WriteLine("Persistent storage infrastructure is ready for testing.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            Environment.Exit(1);
        }
    }
}