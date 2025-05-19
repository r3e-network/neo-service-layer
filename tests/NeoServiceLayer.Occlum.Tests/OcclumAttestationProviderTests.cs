using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Occlum;
using NeoServiceLayer.Tee.Host.RemoteAttestation;
using Xunit;

namespace NeoServiceLayer.Occlum.Tests
{
    /// <summary>
    /// Tests for the OcclumAttestationProvider class.
    /// </summary>
    [Trait("Category", "Occlum")]
    public class OcclumAttestationProviderTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OcclumAttestationProviderTests> _logger;
        private readonly IOcclumManager _occlumManager;
        private readonly OcclumAttestationProvider _attestationProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="OcclumAttestationProviderTests"/> class.
        /// </summary>
        public OcclumAttestationProviderTests()
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
            _logger = _serviceProvider.GetRequiredService<ILogger<OcclumAttestationProviderTests>>();

            // Get Occlum manager
            _occlumManager = _serviceProvider.GetRequiredService<IOcclumManager>();

            // Initialize Occlum manager
            _occlumManager.InitializeAsync().Wait();

            // Create attestation provider
            var attestationLogger = _serviceProvider.GetRequiredService<ILogger<OcclumAttestationProvider>>();
            _attestationProvider = new OcclumAttestationProvider(attestationLogger, _occlumManager);

            // Set simulation mode
            Environment.SetEnvironmentVariable("OCCLUM_SIMULATION", "1");
        }

        /// <summary>
        /// Tests that an attestation report can be generated.
        /// </summary>
        [Fact]
        public void GetAttestationReport_ShouldSucceed()
        {
            // Arrange
            byte[] reportData = Encoding.UTF8.GetBytes("Report data");

            // Act
            byte[] report = _attestationProvider.GetAttestationReport(reportData);

            // Assert
            Assert.NotNull(report);
            Assert.NotEmpty(report);
        }

        /// <summary>
        /// Tests that an attestation report can be verified.
        /// </summary>
        [Fact]
        public void VerifyAttestationReport_ShouldSucceed()
        {
            // Arrange
            byte[] reportData = Encoding.UTF8.GetBytes("Report data");
            byte[] report = _attestationProvider.GetAttestationReport(reportData);

            // Act
            bool result = _attestationProvider.VerifyAttestationReport(report);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests that data can be sealed and unsealed.
        /// </summary>
        [Fact]
        public void SealAndUnsealData_ShouldSucceed()
        {
            // Arrange
            byte[] data = Encoding.UTF8.GetBytes("Hello, world!");

            // Act
            byte[] sealedData = _attestationProvider.SealData(data);
            byte[] unsealedData = _attestationProvider.UnsealData(sealedData);

            // Assert
            Assert.NotNull(sealedData);
            Assert.NotEmpty(sealedData);
            Assert.NotNull(unsealedData);
            Assert.NotEmpty(unsealedData);
            Assert.Equal(data, unsealedData);
        }

        /// <summary>
        /// Tests that enclave measurements can be retrieved.
        /// </summary>
        [Fact]
        public void GetEnclaveMeasurements_ShouldSucceed()
        {
            // Act
            var measurements = _attestationProvider.GetEnclaveMeasurements();

            // Assert
            Assert.NotNull(measurements);
            Assert.NotNull(measurements.MrEnclave);
            Assert.NotNull(measurements.MrSigner);
            Assert.NotEmpty(measurements.MrEnclave);
            Assert.NotEmpty(measurements.MrSigner);
        }
    }
}
