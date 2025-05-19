using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host;
using NeoServiceLayer.Tee.Host.Occlum;
using Xunit;

namespace NeoServiceLayer.Occlum.Tests
{
    /// <summary>
    /// Integration tests for Occlum.
    /// </summary>
    [Trait("Category", "Occlum")]
    public class OcclumIntegrationTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OcclumIntegrationTests> _logger;
        private readonly IOcclumManager _occlumManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumIntegrationTests"/> class.
        /// </summary>
        public OcclumIntegrationTests()
        {
            // Create service collection
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add Occlum manager
            services.AddSingleton<IOcclumManager, OcclumManager>();

            // Build service provider
            _serviceProvider = services.BuildServiceProvider();

            // Get logger
            _logger = _serviceProvider.GetRequiredService<ILogger<OcclumIntegrationTests>>();

            // Get Occlum manager
            _occlumManager = _serviceProvider.GetRequiredService<IOcclumManager>();

            // Set simulation mode
            Environment.SetEnvironmentVariable("OCCLUM_SIMULATION", "1");
        }

        /// <summary>
        /// Tests that Occlum can be initialized.
        /// </summary>
        [Fact]
        public async Task InitializeAsync_ShouldSucceed()
        {
            // Act
            await _occlumManager.InitializeAsync();

            // Assert
            Assert.True(_occlumManager.IsOcclumSupportEnabled());
        }

        /// <summary>
        /// Tests that a command can be executed in Occlum.
        /// </summary>
        [Fact]
        public async Task ExecuteCommandAsync_ShouldSucceed()
        {
            // Arrange
            await _occlumManager.InitializeAsync();

            // Act
            int exitCode = await _occlumManager.ExecuteCommandAsync("/bin/ls", new string[] { "-la" });

            // Assert
            Assert.Equal(0, exitCode);
        }

        /// <summary>
        /// Tests that JavaScript code can be executed in Occlum.
        /// </summary>
        [Fact]
        public async Task ExecuteJavaScriptCodeAsync_ShouldSucceed()
        {
            // Arrange
            await _occlumManager.InitializeAsync();
            string code = "console.log('Hello, world!'); process.exit(0);";

            // Act
            int exitCode = await _occlumManager.ExecuteJavaScriptCodeAsync(code, Array.Empty<string>());

            // Assert
            Assert.Equal(0, exitCode);
        }

        /// <summary>
        /// Tests that a JavaScript file can be executed in Occlum.
        /// </summary>
        [Fact]
        public async Task ExecuteJavaScriptFileAsync_ShouldSucceed()
        {
            // Arrange
            await _occlumManager.InitializeAsync();
            string tempDir = Path.Combine(Path.GetTempPath(), "occlum_tests");
            Directory.CreateDirectory(tempDir);
            string scriptPath = Path.Combine(tempDir, "test.js");
            File.WriteAllText(scriptPath, "console.log('Hello, world!'); process.exit(0);");

            try
            {
                // Act
                int exitCode = await _occlumManager.ExecuteJavaScriptFileAsync(scriptPath, Array.Empty<string>());

                // Assert
                Assert.Equal(0, exitCode);
            }
            finally
            {
                // Clean up
                File.Delete(scriptPath);
                Directory.Delete(tempDir);
            }
        }
    }
}
