using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Shared.Models.Attestation;
using NeoServiceLayer.Tee.Host.RemoteAttestation;
using Xunit;

namespace NeoServiceLayer.Tee.Enclave.Tests.RemoteAttestation
{
    [Trait("Category", "OpenEnclave")]
    public class AttestationProviderTests
    {
        private readonly Mock<ILogger<MockAttestationProvider>> _loggerMock;
        private readonly IAttestationProvider _attestationProvider;

        public AttestationProviderTests()
        {
            _loggerMock = new Mock<ILogger<MockAttestationProvider>>();
            _attestationProvider = new MockAttestationProvider(_loggerMock.Object);
        }

        [Fact]
        public async Task GenerateAttestationProofAsync_WithNoReportData_ReturnsValidProof()
        {
            // Act
            var proof = await _attestationProvider.GenerateAttestationProofAsync();

            // Assert
            Assert.NotNull(proof);
            Assert.Equal("OpenEnclave", proof.EnclaveType);
            Assert.NotNull(proof.Report);
            Assert.NotEmpty(proof.Report);
            Assert.True(proof.Timestamp <= DateTime.UtcNow);
            Assert.True(proof.Timestamp >= DateTime.UtcNow.AddMinutes(-1));
            Assert.NotNull(proof.AdditionalData);
            Assert.True(proof.AdditionalData.ContainsKey("ReportSize"));
            Assert.True(proof.AdditionalData.ContainsKey("ReportFormat"));
            Assert.True(proof.AdditionalData.ContainsKey("IsMock"));
            Assert.Equal("true", proof.AdditionalData["IsMock"]);
        }

        [Fact]
        public async Task GenerateAttestationProofAsync_WithReportData_IncludesReportDataHash()
        {
            // Arrange
            byte[] reportData = Encoding.UTF8.GetBytes("Test report data");

            // Act
            var proof = await _attestationProvider.GenerateAttestationProofAsync(reportData);

            // Assert
            Assert.NotNull(proof);
            Assert.NotNull(proof.AdditionalData);
            Assert.True(proof.AdditionalData.ContainsKey("ReportDataHash"));
            Assert.NotEmpty(proof.AdditionalData["ReportDataHash"]);
        }

        [Fact]
        public async Task VerifyAttestationProofAsync_WithValidProof_ReturnsTrue()
        {
            // Arrange
            var proof = await _attestationProvider.GenerateAttestationProofAsync();

            // Act
            bool isValid = await _attestationProvider.VerifyAttestationProofAsync(proof);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public async Task VerifyAttestationProofAsync_WithNullProof_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _attestationProvider.VerifyAttestationProofAsync(null));
        }

        [Fact]
        public void Dispose_DoesNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => _attestationProvider.Dispose());
            Assert.Null(exception);
        }
    }

    [Trait("Category", "OpenEnclave")]
    public class AttestationProviderFactoryTests
    {
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;
        private readonly Mock<ILogger<OpenEnclaveAttestationProvider>> _loggerMock;
        private readonly AttestationProviderFactory _factory;

        public AttestationProviderFactoryTests()
        {
            _loggerMock = new Mock<ILogger<OpenEnclaveAttestationProvider>>();
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);
            _factory = new AttestationProviderFactory(_loggerFactoryMock.Object);
        }

        [Fact]
        public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AttestationProviderFactory(null));
        }

        [Fact]
        public void CreateAttestationProvider_WithZeroEnclaveId_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _factory.CreateAttestationProvider(IntPtr.Zero));
        }

        [Fact]
        public void CreateAttestationProvider_WithValidEnclaveId_ReturnsAttestationProvider()
        {
            // Arrange
            IntPtr enclaveId = new IntPtr(1);

            // Act
            var provider = _factory.CreateAttestationProvider(enclaveId);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<OpenEnclaveAttestationProvider>(provider);
        }

        [Fact]
        public void CreateMockAttestationProvider_ReturnsAttestationProvider()
        {
            // Act
            var provider = _factory.CreateMockAttestationProvider();

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<MockAttestationProvider>(provider);
        }
    }
}
