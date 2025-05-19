using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Services;
using NeoServiceLayer.Infrastructure.Data.Repositories;
using NeoServiceLayer.Shared.Models;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;

namespace NeoServiceLayer.Infrastructure.Tests.Services
{
    public class AttestationServiceTests
    {
        private readonly Mock<NeoServiceLayer.Tee.Host.Services.ITeeHostService> _mockTeeHostService;
        private readonly Mock<IAttestationProofRepository> _mockAttestationProofRepository;
        private readonly Mock<ILogger<AttestationService>> _mockLogger;
        private readonly AttestationService _attestationService;

        public AttestationServiceTests()
        {
            _mockTeeHostService = new Mock<NeoServiceLayer.Tee.Host.Services.ITeeHostService>();
            _mockAttestationProofRepository = new Mock<IAttestationProofRepository>();
            _mockLogger = new Mock<ILogger<AttestationService>>();

            // Create an adapter that implements Core.Interfaces.ITeeHostService
            var teeHostServiceAdapter = new Mocks.TeeHostServiceAdapter(_mockTeeHostService.Object);

            _attestationService = new AttestationService(teeHostServiceAdapter, _mockAttestationProofRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async System.Threading.Tasks.Task GenerateAttestationProofAsync_Success_ReturnsAttestationProof()
        {
            // Arrange
            var attestationProof = new AttestationProof
            {
                Id = Guid.NewGuid().ToString(),
                Report = "report-data",
                Signature = "signature-data",
                MrEnclave = "mrenclave-value",
                MrSigner = "mrsigner-value",
                ProductId = "product-id",
                SecurityVersion = "1.0.0",
                Attributes = "attributes",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _mockTeeHostService.Setup(x => x.GetAttestationProofAsync())
                .ReturnsAsync(attestationProof);

            // Act
            var result = await _attestationService.GenerateAttestationProofAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(attestationProof.Id, result.Id);
            Assert.Equal(attestationProof.Report, result.Report);
            Assert.Equal(attestationProof.Signature, result.Signature);
            Assert.Equal(attestationProof.MrEnclave, result.MrEnclave);
            Assert.Equal(attestationProof.MrSigner, result.MrSigner);
            Assert.Equal(attestationProof.ProductId, result.ProductId);
            Assert.Equal(attestationProof.SecurityVersion, result.SecurityVersion);
            Assert.Equal(attestationProof.Attributes, result.Attributes);
            Assert.Equal(attestationProof.CreatedAt, result.CreatedAt);
            Assert.Equal(attestationProof.ExpiresAt, result.ExpiresAt);
        }

        [Fact]
        public async System.Threading.Tasks.Task VerifyAttestationProofAsync_ValidProof_ReturnsTrue()
        {
            // Arrange
            var attestationProof = new AttestationProof
            {
                Id = Guid.NewGuid().ToString(),
                Report = "report-data",
                Signature = "signature-data",
                MrEnclave = "mrenclave-value",
                MrSigner = "mrsigner-value",
                ProductId = "product-id",
                SecurityVersion = "1.0.0",
                Attributes = "attributes",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _mockTeeHostService.Setup(x => x.VerifyAttestationProofAsync(It.IsAny<AttestationProof>()))
                .ReturnsAsync(true);

            // Act
            var result = await _attestationService.VerifyAttestationProofAsync(attestationProof);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async System.Threading.Tasks.Task VerifyAttestationProofAsync_InvalidProof_ReturnsFalse()
        {
            // Arrange
            var attestationProof = new AttestationProof
            {
                Id = Guid.NewGuid().ToString(),
                Report = "report-data",
                Signature = "invalid-signature",
                MrEnclave = "mrenclave-value",
                MrSigner = "mrsigner-value",
                ProductId = "product-id",
                SecurityVersion = "1.0.0",
                Attributes = "attributes",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _mockTeeHostService.Setup(x => x.VerifyAttestationProofAsync(It.IsAny<AttestationProof>()))
                .ReturnsAsync(false);

            // Act
            var result = await _attestationService.VerifyAttestationProofAsync(attestationProof);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetCurrentAttestationProofAsync_NoCurrentProof_GeneratesNewProof()
        {
            // Arrange
            var attestationProof = new AttestationProof
            {
                Id = "9ec08b46-1c3f-454f-8f6d-dc0813900771", // Fixed ID for testing
                Report = "report-data",
                Signature = "signature-data",
                MrEnclave = "mrenclave-value",
                MrSigner = "mrsigner-value",
                ProductId = "product-id",
                SecurityVersion = "1.0.0",
                Attributes = "attributes",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _mockTeeHostService.Setup(x => x.GetAttestationProofAsync())
                .ReturnsAsync(attestationProof);

            // Act
            var result = await _attestationService.GetCurrentAttestationProofAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(attestationProof.Id, result.Id);
            Assert.Equal(attestationProof.Report, result.Report);
            Assert.Equal(attestationProof.Signature, result.Signature);
            Assert.Equal(attestationProof.MrEnclave, result.MrEnclave);
            Assert.Equal(attestationProof.MrSigner, result.MrSigner);
            Assert.Equal(attestationProof.ProductId, result.ProductId);
            Assert.Equal(attestationProof.SecurityVersion, result.SecurityVersion);
            Assert.Equal(attestationProof.Attributes, result.Attributes);
            Assert.Equal(attestationProof.CreatedAt, result.CreatedAt);
            Assert.Equal(attestationProof.ExpiresAt, result.ExpiresAt);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAttestationProofAsync_ExistingProofId_ReturnsProof()
        {
            // Arrange
            var attestationProof = new AttestationProof
            {
                Id = Guid.NewGuid().ToString(),
                Report = "report-data",
                Signature = "signature-data",
                MrEnclave = "mrenclave-value",
                MrSigner = "mrsigner-value",
                ProductId = "product-id",
                SecurityVersion = "1.0.0",
                Attributes = "attributes",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _mockTeeHostService.Setup(x => x.GetAttestationProofAsync())
                .ReturnsAsync(attestationProof);

            // Setup mock repository to return the proof
            _mockAttestationProofRepository.Setup(x => x.GetByIdAsync(attestationProof.Id))
                .ReturnsAsync(attestationProof);

            // Act
            var result = await _attestationService.GetAttestationProofAsync(attestationProof.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(attestationProof.Id, result.Id);
            Assert.Equal(attestationProof.Report, result.Report);
            Assert.Equal(attestationProof.Signature, result.Signature);
            Assert.Equal(attestationProof.MrEnclave, result.MrEnclave);
            Assert.Equal(attestationProof.MrSigner, result.MrSigner);
            Assert.Equal(attestationProof.ProductId, result.ProductId);
            Assert.Equal(attestationProof.SecurityVersion, result.SecurityVersion);
            Assert.Equal(attestationProof.Attributes, result.Attributes);
            Assert.Equal(attestationProof.CreatedAt, result.CreatedAt);
            Assert.Equal(attestationProof.ExpiresAt, result.ExpiresAt);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAttestationProofAsync_NonExistingProofId_ReturnsNull()
        {
            // Arrange
            var proofId = "non-existing-id";

            // Act
            var result = await _attestationService.GetAttestationProofAsync(proofId);

            // Assert
            Assert.Null(result);
        }
    }
}
