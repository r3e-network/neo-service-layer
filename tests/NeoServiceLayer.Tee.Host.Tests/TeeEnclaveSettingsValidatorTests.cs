using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Host;
using NeoServiceLayer.Tee.Host.Tests.Mocks;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests
{
    public class TeeEnclaveSettingsValidatorTests
    {
        private readonly Mock<ILogger> _loggerMock;
        private readonly TeeEnclaveSettingsValidator _validator;

        public TeeEnclaveSettingsValidatorTests()
        {
            _loggerMock = new Mock<ILogger>();
            _validator = new TeeEnclaveSettingsValidator(_loggerMock.Object);
        }

        [Fact]
        public void Validate_WithNullSettings_ReturnsError()
        {
            // Act
            var result = _validator.Validate(null!);

            // Assert
            Assert.Single(result);
            Assert.Equal("Settings cannot be null.", result[0]);
        }

        [Fact]
        public void Validate_WithValidSettings_ReturnsNoErrors()
        {
            // Arrange
            var settings = new TeeEnclaveSettings
            {
                Type = "OpenEnclave",
                EnclaveSettings = new EnclaveSettings
                {
                    EnclavePath = "bin/liboe_enclave.signed.so",
                    SimulationMode = true,
                    OcclumSupport = true,
                    OcclumInstanceDir = "/occlum_instance",
                    OcclumLogLevel = "info"
                },
                JavaScriptEngine = new JavaScriptEngineSettings
                {
                    MaxMemoryMB = 512,
                    MaxExecutionTimeMs = 5000
                },
                UserSecrets = new UserSecretsSettings
                {
                    MaxSecretsPerUser = 100,
                    MaxSecretSizeBytes = 4096
                },
                GasAccounting = new GasAccountingSettings
                {
                    EnableGasAccounting = true,
                    GasLimitPerExecution = 1000000,
                    GasPriceMultiplier = 1.0
                }
            };

            // Act
            var result = _validator.Validate(settings);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Validate_WithInvalidType_ReturnsError()
        {
            // Arrange
            var settings = new TeeEnclaveSettings
            {
                Type = "Invalid"
            };

            // Act
            var result = _validator.Validate(settings);

            // Assert
            Assert.Contains(result, error => error.Contains("Invalid TEE type"));
        }

        [Fact]
        public void Validate_WithNullType_ReturnsError()
        {
            // Arrange
            var settings = new TeeEnclaveSettings
            {
                Type = null!
            };

            // Act
            var result = _validator.Validate(settings);

            // Assert
            Assert.Contains(result, error => error.Contains("TEE type cannot be null or empty"));
        }

        [Fact]
        public void Validate_WithNullEnclaveSettings_ReturnsError()
        {
            // Arrange
            var settings = new TeeEnclaveSettings
            {
                Type = "OpenEnclave",
                EnclaveSettings = null!
            };

            // Act
            var result = _validator.Validate(settings);

            // Assert
            Assert.Contains(result, error => error.Contains("Enclave settings cannot be null"));
        }

        [Fact]
        public void Validate_WithInvalidJavaScriptEngineSettings_ReturnsErrors()
        {
            // Arrange
            var settings = new TeeEnclaveSettings
            {
                Type = "OpenEnclave",
                EnclaveSettings = new EnclaveSettings
                {
                    EnclavePath = "bin/liboe_enclave.signed.so",
                    SimulationMode = true
                },
                JavaScriptEngine = new JavaScriptEngineSettings
                {
                    MaxMemoryMB = 0,
                    MaxExecutionTimeMs = -1
                }
            };

            // Act
            var result = _validator.Validate(settings);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, error => error.Contains("Invalid maximum memory"));
            Assert.Contains(result, error => error.Contains("Invalid maximum execution time"));
        }

        [Fact]
        public void ValidateAndThrow_WithValidSettings_DoesNotThrow()
        {
            // Arrange
            var settings = new TeeEnclaveSettings
            {
                Type = "OpenEnclave",
                EnclaveSettings = new EnclaveSettings
                {
                    EnclavePath = "bin/liboe_enclave.signed.so",
                    SimulationMode = true
                }
            };

            // Act & Assert
            var exception = Record.Exception(() => _validator.ValidateAndThrow(settings));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateAndThrow_WithInvalidSettings_ThrowsArgumentException()
        {
            // Arrange
            var settings = new TeeEnclaveSettings
            {
                Type = "Invalid"
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.ValidateAndThrow(settings));
            Assert.Contains("Invalid TEE type", exception.Message);
        }
    }
}
