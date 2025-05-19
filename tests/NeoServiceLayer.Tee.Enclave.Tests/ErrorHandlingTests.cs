using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using NeoServiceLayer.Tee.Host;
using NeoServiceLayer.Tee.Host.Exceptions;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "ErrorHandling")]
    public class ErrorHandlingTests : IDisposable
    {
        private readonly ILogger<MockOpenEnclaveInterface> _logger;
        private readonly MockOpenEnclaveInterface _oeInterface;
        private readonly string _enclavePath;
        private readonly bool _skipTests;

        public ErrorHandlingTests()
        {
            // Create a real logger for better diagnostics
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<MockOpenEnclaveInterface>();

            // Get the enclave path from environment variable or use a default
            _enclavePath = Environment.GetEnvironmentVariable("OE_ENCLAVE_PATH") ?? "liboe_enclave.signed.so";

            // Set simulation mode for testing
            Environment.SetEnvironmentVariable("OE_SIMULATION", "1");

            try
            {
                // Initialize the mock DLLs
                MockDllInitializer.Initialize(_logger);

                // Create a mock enclave file
                var mockEnclaveFileLogger = loggerFactory.CreateLogger<MockEnclaveFile>();
                var mockEnclaveFile = new MockEnclaveFile(mockEnclaveFileLogger);
                mockEnclaveFile.CreateAsync().Wait();

                // Create the MockOpenEnclaveInterface
                _oeInterface = new MockOpenEnclaveInterface(_logger, _enclavePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Open Enclave interface");
                _skipTests = true;
            }
        }

        public void Dispose()
        {
            _oeInterface?.Dispose();
        }

        [Fact]
        public void GetRandomBytes_WithVeryLargeLength_HandlesGracefully()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            int length = 10 * 1024 * 1024; // 10MB

            // Act & Assert
            // This should either succeed or throw a specific exception, not crash
            try
            {
                byte[] randomBytes = _oeInterface.GetRandomBytes(length);
                Assert.Equal(length, randomBytes.Length);
            }
            catch (EnclaveOperationException ex)
            {
                // This is acceptable - the enclave might reject very large requests
                _logger.LogInformation("GetRandomBytes with large length threw exception: {Message}", ex.Message);
            }
        }

        [Fact]
        public void SignData_WithVeryLargeData_HandlesGracefully()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            byte[] largeData = new byte[10 * 1024 * 1024]; // 10MB
            new Random().NextBytes(largeData);

            // Act & Assert
            // This should either succeed or throw a specific exception, not crash
            try
            {
                byte[] signature = _oeInterface.SignData(largeData);
                Assert.NotNull(signature);
                Assert.True(signature.Length > 0);
            }
            catch (EnclaveOperationException ex)
            {
                // This is acceptable - the enclave might reject very large requests
                _logger.LogInformation("SignData with large data threw exception: {Message}", ex.Message);
            }
        }

        [Fact]
        public void VerifySignature_WithInvalidSignature_ReturnsFalse()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            byte[] data = Encoding.UTF8.GetBytes("Test data");
            byte[] invalidSignature = new byte[64]; // Empty signature
            new Random().NextBytes(invalidSignature);

            // Act
            bool isValid = _oeInterface.VerifySignature(data, invalidSignature);

            // Assert
            Assert.False(isValid, "Verification should fail with invalid signature");
        }

        [Fact]
        public void UnsealData_WithInvalidSealedData_ThrowsException()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            byte[] invalidSealedData = new byte[100];
            new Random().NextBytes(invalidSealedData);

            // Act & Assert
            Assert.Throws<EnclaveOperationException>(() => _oeInterface.UnsealData(invalidSealedData));
        }

        [Fact]
        public async Task ExecuteJavaScript_WithSyntaxError_ThrowsException()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            string invalidCode = @"
                function main(input) {
                    // Syntax error - missing closing brace
                    if (true) {
                        return { result: 'error' };
                }
            ";
            string input = @"{}";
            string secrets = @"{}";
            string functionId = "error_test_function";
            string userId = "error_test_user";

            // Act & Assert
            await Assert.ThrowsAsync<EnclaveOperationException>(() =>
                _oeInterface.ExecuteJavaScriptAsync(invalidCode, input, secrets, functionId, userId));
        }

        [Fact]
        public async Task ExecuteJavaScript_WithRuntimeError_ThrowsException()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            string errorCode = @"
                function main(input) {
                    // Runtime error - undefined variable
                    return { result: undefinedVariable };
                }
            ";
            string input = @"{}";
            string secrets = @"{}";
            string functionId = "runtime_error_test_function";
            string userId = "runtime_error_test_user";

            // Act & Assert
            await Assert.ThrowsAsync<EnclaveOperationException>(() =>
                _oeInterface.ExecuteJavaScriptAsync(errorCode, input, secrets, functionId, userId));
        }

        [Fact]
        public async Task ExecuteJavaScript_WithInfiniteLoop_TimesOut()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            string infiniteLoopCode = @"
                function main(input) {
                    // Infinite loop
                    while (true) {}
                    return { result: 'never reached' };
                }
            ";
            string input = @"{}";
            string secrets = @"{}";
            string functionId = "timeout_test_function";
            string userId = "timeout_test_user";

            // Act & Assert
            // This should throw an exception due to timeout
            await Assert.ThrowsAsync<EnclaveOperationException>(() =>
                _oeInterface.ExecuteJavaScriptAsync(infiniteLoopCode, input, secrets, functionId, userId));
        }

        [Fact]
        public async Task ExecuteJavaScript_WithExcessiveMemoryUsage_ThrowsException()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            string memoryHogCode = @"
                function main(input) {
                    // Try to allocate a huge array
                    const hugeArray = new Array(1000000000).fill(0);
                    return { result: 'success' };
                }
            ";
            string input = @"{}";
            string secrets = @"{}";
            string functionId = "memory_test_function";
            string userId = "memory_test_user";

            // Act & Assert
            // This should throw an exception due to memory limits
            await Assert.ThrowsAsync<EnclaveOperationException>(() =>
                _oeInterface.ExecuteJavaScriptAsync(memoryHogCode, input, secrets, functionId, userId));
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Act & Assert
            _oeInterface.Dispose();
            _oeInterface.Dispose(); // Should not throw
        }

        [Fact]
        public void MethodsCalledAfterDispose_ThrowObjectDisposedException()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            var interface_to_dispose = new MockOpenEnclaveInterface(_logger, _enclavePath);
            interface_to_dispose.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => interface_to_dispose.GetRandomBytes(10));
            Assert.Throws<ObjectDisposedException>(() => interface_to_dispose.GetMrEnclave());
            Assert.Throws<ObjectDisposedException>(() => interface_to_dispose.SignData(new byte[10]));
        }
    }
}
