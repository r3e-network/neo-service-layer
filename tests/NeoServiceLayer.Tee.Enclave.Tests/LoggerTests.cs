using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Shared.Models;
using System.Text.Json;
using System.Collections.Generic;
using NeoServiceLayer.Enclave;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Collection("SimulationMode")]
    [Trait("Category", "Logger")]
    public class LoggerTests
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;

        public LoggerTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact]
        public void Logger_Initialization_ShouldSucceed()
        {
            // Arrange & Act
            var logger = NeoServiceLayer.Enclave.Logger.getInstance();
            bool result = logger.initialize(
                NeoServiceLayer.Enclave.LogLevel.DEBUG,
                true,
                Path.Combine(Path.GetTempPath(), "enclave_test.log"),
                1024 * 1024,
                3);

            // Assert
            Assert.True(result);
            Assert.Equal(NeoServiceLayer.Enclave.LogLevel.DEBUG, logger.getLogLevel());
        }

        [Fact]
        public void Logger_LogLevels_ShouldFilterCorrectly()
        {
            // Arrange
            var logger = NeoServiceLayer.Enclave.Logger.getInstance();
            logger.initialize(NeoServiceLayer.Enclave.LogLevel.INFO);
            
            // Set up a callback to capture log messages
            List<string> capturedLogs = new List<string>();
            logger.setLogCallback((level, message) => {
                capturedLogs.Add($"{level}: {message}");
                _output.WriteLine($"Captured log: {level}: {message}");
            });
            
            // Act - Log at different levels
            logger.trace("TestComponent", "This is a trace message");
            logger.debug("TestComponent", "This is a debug message");
            logger.info("TestComponent", "This is an info message");
            logger.warning("TestComponent", "This is a warning message");
            logger.error("TestComponent", "This is an error message");
            logger.critical("TestComponent", "This is a critical message");
            
            // Assert
            Assert.DoesNotContain(capturedLogs, log => log.Contains("This is a trace message"));
            Assert.DoesNotContain(capturedLogs, log => log.Contains("This is a debug message"));
            Assert.Contains(capturedLogs, log => log.Contains("This is an info message"));
            Assert.Contains(capturedLogs, log => log.Contains("This is a warning message"));
            Assert.Contains(capturedLogs, log => log.Contains("This is an error message"));
            Assert.Contains(capturedLogs, log => log.Contains("This is a critical message"));
        }

        [Fact]
        public void Logger_ChangeLogLevel_ShouldAffectFiltering()
        {
            // Arrange
            var logger = NeoServiceLayer.Enclave.Logger.getInstance();
            logger.initialize(NeoServiceLayer.Enclave.LogLevel.WARNING);
            
            // Set up a callback to capture log messages
            List<string> capturedLogs = new List<string>();
            logger.setLogCallback((level, message) => {
                capturedLogs.Add($"{level}: {message}");
                _output.WriteLine($"Captured log: {level}: {message}");
            });
            
            // Act - Log at info level (should be filtered out)
            logger.info("TestComponent", "This info message should be filtered out");
            
            // Change log level
            logger.setLogLevel(NeoServiceLayer.Enclave.LogLevel.INFO);
            
            // Log at info level again (should now be included)
            logger.info("TestComponent", "This info message should be included");
            
            // Assert
            Assert.DoesNotContain(capturedLogs, log => log.Contains("should be filtered out"));
            Assert.Contains(capturedLogs, log => log.Contains("should be included"));
        }

        [Fact]
        public void Logger_LogFormat_ShouldIncludeRequiredElements()
        {
            // Arrange
            var logger = NeoServiceLayer.Enclave.Logger.getInstance();
            logger.initialize(NeoServiceLayer.Enclave.LogLevel.DEBUG);
            
            // Set up a callback to capture log messages
            string capturedLog = null;
            logger.setLogCallback((level, message) => {
                capturedLog = message;
                _output.WriteLine($"Captured log: {message}");
            });
            
            // Act
            logger.info("FormatTest", "This is a test message");
            
            // Assert
            Assert.NotNull(capturedLog);
            
            // Check for timestamp format (YYYY-MM-DD HH:MM:SS.mmm)
            Assert.Matches(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}", capturedLog);
            
            // Check for log level
            Assert.Contains("[INFO]", capturedLog);
            
            // Check for component name
            Assert.Contains("[FormatTest]", capturedLog);
            
            // Check for message
            Assert.Contains("This is a test message", capturedLog);
        }
    }
}
