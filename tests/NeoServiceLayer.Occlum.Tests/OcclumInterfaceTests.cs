using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Shared.Models;
using NeoServiceLayer.Tee.Host;
using Xunit;

namespace NeoServiceLayer.Occlum.Tests
{
    /// <summary>
    /// Tests for the OcclumInterface class.
    /// </summary>
    [Trait("Category", "Occlum")]
    public class OcclumInterfaceTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OcclumInterfaceTests> _logger;
        private readonly ITeeInterfaceFactory _teeInterfaceFactory;
        private readonly string _enclavePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumInterfaceTests"/> class.
        /// </summary>
        public OcclumInterfaceTests()
        {
            // Create service collection
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Register services
            TeeInterfaceFactory.RegisterServices(services, null);

            // Build service provider
            _serviceProvider = services.BuildServiceProvider();

            // Get logger
            _logger = _serviceProvider.GetRequiredService<ILogger<OcclumInterfaceTests>>();

            // Get TEE interface factory
            _teeInterfaceFactory = _serviceProvider.GetRequiredService<ITeeInterfaceFactory>();

            // Set enclave path
            _enclavePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Enclave", "libenclave.so");

            // Set simulation mode
            Environment.SetEnvironmentVariable("OCCLUM_SIMULATION", "1");
        }

        /// <summary>
        /// Tests that the OcclumInterface can be created.
        /// </summary>
        [Fact]
        public void CreateOcclumInterface_ShouldSucceed()
        {
            // Act
            using var teeInterface = _teeInterfaceFactory.CreateTeeInterface(_enclavePath, true);

            // Assert
            Assert.NotNull(teeInterface);
            Assert.IsType<OcclumInterface>(teeInterface);
        }

        /// <summary>
        /// Tests that the OcclumInterface can be initialized.
        /// </summary>
        [Fact]
        public async Task InitializeAsync_ShouldSucceed()
        {
            // Arrange
            using var teeInterface = _teeInterfaceFactory.CreateTeeInterface(_enclavePath, true);

            // Act
            bool result = await teeInterface.InitializeAsync();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests that JavaScript code can be executed.
        /// </summary>
        [Fact]
        public async Task ExecuteJavaScriptAsync_ShouldSucceed()
        {
            // Arrange
            using var teeInterface = _teeInterfaceFactory.CreateTeeInterface(_enclavePath, true);
            await teeInterface.InitializeAsync();

            string code = @"
                function main(input, secrets, functionId, userId) {
                    return {
                        message: 'Hello, world!',
                        input: input,
                        functionId: functionId,
                        userId: userId
                    };
                }
            ";
            string input = @"{ ""name"": ""Test"" }";
            string secrets = @"{ ""apiKey"": ""test-api-key"" }";
            string functionId = "test-function";
            string userId = "test-user";

            // Act
            JavaScriptExecutionResult result = await teeInterface.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Hello, world!", result.Result);
            Assert.Contains("Test", result.Result);
            Assert.Contains("test-function", result.Result);
            Assert.Contains("test-user", result.Result);
        }

        /// <summary>
        /// Tests that data can be sealed and unsealed.
        /// </summary>
        [Fact]
        public async Task SealAndUnsealData_ShouldSucceed()
        {
            // Arrange
            using var teeInterface = _teeInterfaceFactory.CreateTeeInterface(_enclavePath, true);
            await teeInterface.InitializeAsync();

            byte[] data = Encoding.UTF8.GetBytes("Hello, world!");

            // Act
            byte[] sealedData = teeInterface.SealData(data);
            byte[] unsealedData = teeInterface.UnsealData(sealedData);

            // Assert
            Assert.NotNull(sealedData);
            Assert.NotEmpty(sealedData);
            Assert.NotNull(unsealedData);
            Assert.NotEmpty(unsealedData);
            Assert.Equal(data, unsealedData);
        }

        /// <summary>
        /// Tests that an attestation report can be generated.
        /// </summary>
        [Fact]
        public async Task GetAttestationReport_ShouldSucceed()
        {
            // Arrange
            using var teeInterface = _teeInterfaceFactory.CreateTeeInterface(_enclavePath, true);
            await teeInterface.InitializeAsync();

            byte[] reportData = Encoding.UTF8.GetBytes("Report data");

            // Act
            byte[] report = teeInterface.GetAttestationReport(reportData);

            // Assert
            Assert.NotNull(report);
            Assert.NotEmpty(report);
        }
    }
}
