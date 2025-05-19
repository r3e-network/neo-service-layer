using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests
{
    public class OpenEnclaveHostCallbacksTests : IDisposable
    {
        private readonly Mock<ILogger<OpenEnclaveHostCallbacks>> _loggerMock;
        private readonly string _testDirectory;
        private readonly OpenEnclaveHostCallbacks _hostCallbacks;

        public OpenEnclaveHostCallbacksTests()
        {
            _loggerMock = new Mock<ILogger<OpenEnclaveHostCallbacks>>();
            
            // Create a test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), $"host_callbacks_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            
            // Create the host callbacks
            _hostCallbacks = new OpenEnclaveHostCallbacks(_loggerMock.Object, _testDirectory);
        }

        public void Dispose()
        {
            // Delete the test directory
            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, true);
                }
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        [Fact]
        public void HostPrintString_LogsMessage()
        {
            // Arrange
            string message = "Test message from enclave";
            
            // Act
            InvokeHostPrintString(message);
            
            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void HostSaveToFile_SavesDataToFile()
        {
            // Arrange
            string filename = "test_file.dat";
            byte[] data = Encoding.UTF8.GetBytes("Test data for saving to file");
            
            // Act
            InvokeHostSaveToFile(filename, data);
            
            // Assert
            string fullPath = Path.Combine(_testDirectory, filename);
            Assert.True(File.Exists(fullPath));
            byte[] readData = File.ReadAllBytes(fullPath);
            Assert.Equal(data, readData);
        }

        [Fact]
        public void HostSaveToFile_CreatesDirectoryIfNeeded()
        {
            // Arrange
            string filename = "subdir/test_file.dat";
            byte[] data = Encoding.UTF8.GetBytes("Test data for saving to file in subdirectory");
            
            // Act
            InvokeHostSaveToFile(filename, data);
            
            // Assert
            string fullPath = Path.Combine(_testDirectory, filename);
            Assert.True(File.Exists(fullPath));
            byte[] readData = File.ReadAllBytes(fullPath);
            Assert.Equal(data, readData);
        }

        [Fact]
        public void HostSaveToFile_DeletesFileWhenRequested()
        {
            // Arrange
            string filename = "test_file_to_delete.dat";
            byte[] data = Encoding.UTF8.GetBytes("Test data for file that will be deleted");
            
            // Create the file first
            InvokeHostSaveToFile(filename, data);
            string fullPath = Path.Combine(_testDirectory, filename);
            Assert.True(File.Exists(fullPath));
            
            // Act - Request deletion
            InvokeHostSaveToFile(filename + ".deleted", new byte[0]);
            
            // Assert
            Assert.False(File.Exists(fullPath));
        }

        [Fact]
        public void HostLoadFromFile_LoadsDataFromFile()
        {
            // Arrange
            string filename = "test_load_file.dat";
            byte[] data = Encoding.UTF8.GetBytes("Test data for loading from file");
            
            // Create the file first
            string fullPath = Path.Combine(_testDirectory, filename);
            File.WriteAllBytes(fullPath, data);
            
            // Act
            byte[] loadedData = InvokeHostLoadFromFile(filename, data.Length);
            
            // Assert
            Assert.Equal(data, loadedData);
        }

        [Fact]
        public void HostLoadFromFile_ReturnsEmptyWhenFileNotFound()
        {
            // Arrange
            string filename = "nonexistent_file.dat";
            
            // Act
            byte[] loadedData = InvokeHostLoadFromFile(filename, 100);
            
            // Assert
            Assert.Empty(loadedData);
        }

        [Fact]
        public void HostLoadFromFile_ReturnsEmptyWhenBufferTooSmall()
        {
            // Arrange
            string filename = "test_buffer_too_small.dat";
            byte[] data = Encoding.UTF8.GetBytes("Test data for buffer too small test");
            
            // Create the file first
            string fullPath = Path.Combine(_testDirectory, filename);
            File.WriteAllBytes(fullPath, data);
            
            // Act
            byte[] loadedData = InvokeHostLoadFromFile(filename, data.Length - 1); // Buffer too small
            
            // Assert
            Assert.Empty(loadedData);
        }

        [Fact]
        public void HostSendMetric_LogsMetric()
        {
            // Arrange
            string metricName = "test_metric";
            string metricValue = "42";
            
            // Act
            InvokeHostSendMetric(metricName, metricValue);
            
            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(metricName) && v.ToString().Contains(metricValue)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        // Helper methods to invoke the private methods using reflection
        private void InvokeHostPrintString(string message)
        {
            // Get the private method
            MethodInfo method = typeof(OpenEnclaveHostCallbacks).GetMethod(
                "HostPrintString",
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Invoke the method
            method.Invoke(_hostCallbacks, new object[] { message });
        }

        private void InvokeHostSaveToFile(string filename, byte[] data)
        {
            // Get the private method
            MethodInfo method = typeof(OpenEnclaveHostCallbacks).GetMethod(
                "HostSaveToFile",
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Pin the data to get a pointer
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                IntPtr dataPtr = handle.AddrOfPinnedObject();
                
                // Invoke the method
                method.Invoke(_hostCallbacks, new object[] { filename, dataPtr, data.Length });
            }
            finally
            {
                handle.Free();
            }
        }

        private byte[] InvokeHostLoadFromFile(string filename, int maxLen)
        {
            // Get the private method
            MethodInfo method = typeof(OpenEnclaveHostCallbacks).GetMethod(
                "HostLoadFromFile",
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Create a buffer to receive the data
            byte[] buffer = new byte[maxLen];
            
            // Pin the buffer to get a pointer
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                IntPtr bufferPtr = handle.AddrOfPinnedObject();
                
                // Invoke the method
                long bytesRead = (long)method.Invoke(_hostCallbacks, new object[] { filename, bufferPtr, maxLen });
                
                // Resize the buffer to the actual size
                if (bytesRead == 0)
                {
                    return Array.Empty<byte>();
                }
                
                byte[] result = new byte[bytesRead];
                Array.Copy(buffer, result, bytesRead);
                return result;
            }
            finally
            {
                handle.Free();
            }
        }

        private void InvokeHostSendMetric(string metricName, string metricValue)
        {
            // Get the private method
            MethodInfo method = typeof(OpenEnclaveHostCallbacks).GetMethod(
                "HostSendMetric",
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Invoke the method
            method.Invoke(_hostCallbacks, new object[] { metricName, metricValue });
        }
    }
}
