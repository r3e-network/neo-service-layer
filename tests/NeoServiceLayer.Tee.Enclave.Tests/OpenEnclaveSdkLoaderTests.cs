using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Host;
using Xunit;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "OpenEnclave")]
    public class OpenEnclaveSdkLoaderTests
    {
        private readonly Mock<ILogger> _loggerMock;
        private readonly OpenEnclaveSdkLoader _sdkLoader;

        public OpenEnclaveSdkLoaderTests()
        {
            _loggerMock = new Mock<ILogger>();
            _sdkLoader = new OpenEnclaveSdkLoader(_loggerMock.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OpenEnclaveSdkLoader(null));
        }

        [Fact]
        public void Initialize_WhenSdkIsAvailable_ReturnsTrue()
        {
            // Arrange
            if (!OpenEnclaveAvailabilityChecker.IsAvailable(_loggerMock.Object))
            {
                // Skip the test if the SDK is not available
                return;
            }

            // Act
            bool result = _sdkLoader.Initialize();

            // Assert
            Assert.True(result);
        }

        [Fact(Skip = "This test requires the OpenEnclave SDK with DLLs that export the expected functions")]
        public void GetFunctionPointer_AfterInitialization_ReturnsNonZeroPointer()
        {
            // Arrange
            if (!OpenEnclaveAvailabilityChecker.IsAvailable(_loggerMock.Object))
            {
                // Skip the test if the SDK is not available
                return;
            }

            _sdkLoader.Initialize();

            // Act
            // Try different function names that might be available
            string[] functionNames = new[] { "oe_create_enclave", "oe_terminate_enclave", "oe_get_report", "oeedger8r_get_public_key" };
            bool foundAnyFunction = false;

            foreach (var functionName in functionNames)
            {
                IntPtr result = _sdkLoader.GetFunctionPointer(functionName);
                if (result != IntPtr.Zero)
                {
                    foundAnyFunction = true;
                    break;
                }
            }

            // Assert
            // We're just checking if any function was found, not a specific one
            Assert.True(foundAnyFunction, "No OpenEnclave SDK functions were found");
        }

        [Fact]
        public void GetFunctionPointer_WithInvalidFunctionName_ReturnsZeroPointer()
        {
            // Arrange
            if (!OpenEnclaveAvailabilityChecker.IsAvailable(_loggerMock.Object))
            {
                // Skip the test if the SDK is not available
                return;
            }

            _sdkLoader.Initialize();

            // Act
            IntPtr result = _sdkLoader.GetFunctionPointer("definitely_non_existent_function_name_123456789");

            // Assert
            Assert.Equal(IntPtr.Zero, result);
        }

        [Fact]
        public void GetFunctionPointer_BeforeInitialization_ReturnsZeroPointer()
        {
            // Act
            IntPtr result = _sdkLoader.GetFunctionPointer("oeedger8r");

            // Assert
            Assert.Equal(IntPtr.Zero, result);
        }

        [Fact]
        public void Dispose_AfterInitialization_DoesNotThrow()
        {
            // Arrange
            if (!OpenEnclaveAvailabilityChecker.IsAvailable(_loggerMock.Object))
            {
                // Skip the test if the SDK is not available
                return;
            }

            _sdkLoader.Initialize();

            // Act & Assert
            var exception = Record.Exception(() => _sdkLoader.Dispose());
            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_BeforeInitialization_DoesNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => _sdkLoader.Dispose());
            Assert.Null(exception);
        }
    }
}
