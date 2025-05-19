using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.TestHelpers;
using NeoServiceLayer.Tee.Host;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    /// <summary>
    /// A fixture that provides a mock enclave for testing.
    /// </summary>
    public class MockEnclaveFixture : IDisposable
    {
        public Mock<ITeeEnclaveInterface> MockTeeInterface { get; }
        public string MockEnclavePath { get; }

        public MockEnclaveFixture()
        {
            // Create a mock enclave interface
            MockTeeInterface = new Mock<ITeeEnclaveInterface>();

            // Set up the mock interface with basic functionality
            MockTeeInterface.Setup(x => x.GetEnclaveId()).Returns(new IntPtr(1234));
            MockTeeInterface.Setup(x => x.GetMrEnclave()).Returns(new byte[32] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 });
            MockTeeInterface.Setup(x => x.GetMrSigner()).Returns(new byte[32] { 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 });
            MockTeeInterface.Setup(x => x.GetRandomBytes(It.IsAny<int>())).Returns((int length) => new byte[length]);
            MockTeeInterface.Setup(x => x.SignData(It.IsAny<byte[]>())).Returns((byte[] data) => new byte[64]);
            MockTeeInterface.Setup(x => x.VerifySignature(It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(true);
            MockTeeInterface.Setup(x => x.RecordExecutionMetricsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>())).Returns(Task.CompletedTask);
            MockTeeInterface.Setup(x => x.RecordExecutionFailureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTeeInterface.Setup(x => x.SealData(It.IsAny<byte[]>())).Returns((byte[] data) => {
                byte[] sealedData = new byte[data.Length + 16];
                Array.Copy(data, 0, sealedData, 16, data.Length);
                return sealedData;
            });
            MockTeeInterface.Setup(x => x.UnsealData(It.IsAny<byte[]>())).Returns((byte[] sealedData) => {
                byte[] data = new byte[sealedData.Length - 16];
                Array.Copy(sealedData, 16, data, 0, data.Length);
                return data;
            });
            MockTeeInterface.Setup(x => x.ExecuteJavaScriptAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>()))
                .ReturnsAsync((string code, string input, string secrets, string functionId, string userId) => {
                    // Simple mock implementation that returns the input value multiplied by 2
                    try {
                        var inputJson = System.Text.Json.JsonDocument.Parse(input);
                        if (inputJson.RootElement.TryGetProperty("value", out var valueElement) && valueElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            int value = valueElement.GetInt32();
                            return $"{{\"result\": {value * 2}}}";
                        }
                        return "{\"result\": \"success\"}";
                    }
                    catch {
                        return "{\"error\": \"Failed to execute JavaScript\"}";
                    }
                });

            // Create a mock enclave file path
            string tempDir = Path.Combine(Path.GetTempPath(), "MockEnclave");
            Directory.CreateDirectory(tempDir);
            MockEnclavePath = Path.Combine(tempDir, "mock_enclave.signed.so");
            
            // Create an empty file to simulate the enclave
            File.WriteAllText(MockEnclavePath, "MOCK ENCLAVE");
        }

        public void Dispose()
        {
            // Clean up the mock enclave file
            try
            {
                if (File.Exists(MockEnclavePath))
                {
                    File.Delete(MockEnclavePath);
                }
                
                string tempDir = Path.GetDirectoryName(MockEnclavePath);
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
        }
    }
}
