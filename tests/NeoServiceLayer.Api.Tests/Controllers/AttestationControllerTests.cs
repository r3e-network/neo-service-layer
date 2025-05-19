using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Api.Controllers;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Shared.Models;
using Xunit;

namespace NeoServiceLayer.Api.Tests.Controllers
{
    public class AttestationControllerTests
    {
        private readonly Mock<IAttestationService> _mockAttestationService;
        private readonly Mock<ILogger<AttestationController>> _mockLogger;
        private readonly AttestationController _controller;

        public AttestationControllerTests()
        {
            _mockAttestationService = new Mock<IAttestationService>();
            _mockLogger = new Mock<ILogger<AttestationController>>();
            _controller = new AttestationController(_mockAttestationService.Object, _mockLogger.Object);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAttestation_Success_ReturnsOkResult()
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

            _mockAttestationService.Setup(x => x.GetCurrentAttestationProofAsync())
                .ReturnsAsync(attestationProof);

            // Act
            var result = await _controller.GetAttestation();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<NeoServiceLayer.Api.Models.ApiResponse<AttestationProof>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(attestationProof.Id, response.Data.Id);
            Assert.Equal(attestationProof.Report, response.Data.Report);
            Assert.Equal(attestationProof.Signature, response.Data.Signature);
            Assert.Equal(attestationProof.MrEnclave, response.Data.MrEnclave);
            Assert.Equal(attestationProof.MrSigner, response.Data.MrSigner);
            Assert.Equal(attestationProof.ProductId, response.Data.ProductId);
            Assert.Equal(attestationProof.SecurityVersion, response.Data.SecurityVersion);
            Assert.Equal(attestationProof.Attributes, response.Data.Attributes);
            Assert.Equal(attestationProof.CreatedAt, response.Data.CreatedAt);
            Assert.Equal(attestationProof.ExpiresAt, response.Data.ExpiresAt);
        }

        [Fact]
        public async System.Threading.Tasks.Task VerifyAttestation_ValidProof_ReturnsOkResult()
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

            _mockAttestationService.Setup(x => x.VerifyAttestationProofAsync(It.IsAny<AttestationProof>()))
                .ReturnsAsync(true);

            // Convert to AttestationProofResponse
            var attestationProofResponse = new Api.Models.AttestationProofResponse
            {
                Id = attestationProof.Id,
                Report = attestationProof.Report,
                Signature = attestationProof.Signature,
                MrEnclave = attestationProof.MrEnclave,
                MrSigner = attestationProof.MrSigner,
                ProductId = attestationProof.ProductId,
                SecurityVersion = attestationProof.SecurityVersion,
                Attributes = attestationProof.Attributes,
                CreatedAt = attestationProof.CreatedAt,
                ExpiresAt = attestationProof.ExpiresAt
            };

            // Act
            var result = await _controller.VerifyAttestation(attestationProofResponse);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<NeoServiceLayer.Api.Models.ApiResponse<Api.Models.VerifyAttestationResponse>>(okResult.Value);
            Assert.True(response.Success);
            Assert.True(response.Data.IsValid);
        }

        [Fact]
        public async System.Threading.Tasks.Task VerifyAttestation_InvalidProof_ReturnsOkResultWithInvalidFlag()
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

            _mockAttestationService.Setup(x => x.VerifyAttestationProofAsync(It.IsAny<AttestationProof>()))
                .ReturnsAsync(false);

            // Convert to AttestationProofResponse
            var attestationProofResponse = new Api.Models.AttestationProofResponse
            {
                Id = attestationProof.Id,
                Report = attestationProof.Report,
                Signature = attestationProof.Signature,
                MrEnclave = attestationProof.MrEnclave,
                MrSigner = attestationProof.MrSigner,
                ProductId = attestationProof.ProductId,
                SecurityVersion = attestationProof.SecurityVersion,
                Attributes = attestationProof.Attributes,
                CreatedAt = attestationProof.CreatedAt,
                ExpiresAt = attestationProof.ExpiresAt
            };

            // Act
            var result = await _controller.VerifyAttestation(attestationProofResponse);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<NeoServiceLayer.Api.Models.ApiResponse<Api.Models.VerifyAttestationResponse>>(okResult.Value);
            Assert.True(response.Success);
            // The controller returns the value from the attestation service, which we mocked to return false
            Assert.False(response.Data.IsValid);
        }

        [Fact]
        public async System.Threading.Tasks.Task VerifyAttestation_NullProof_ReturnsBadRequestResult()
        {
            // Act
            var result = await _controller.VerifyAttestation(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<NeoServiceLayer.Api.Models.ApiResponse<Api.Models.VerifyAttestationResponse>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("VALIDATION_ERROR", response.Error.Code);
            Assert.Contains("required", response.Error.Message);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAttestationProof_ExistingProofId_ReturnsOkResult()
        {
            // Arrange
            var attestationProofId = Guid.NewGuid().ToString();
            var attestationProof = new AttestationProof
            {
                Id = attestationProofId,
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

            _mockAttestationService.Setup(x => x.GetAttestationProofAsync(attestationProofId))
                .ReturnsAsync(attestationProof);

            // Act
            var result = await _controller.GetAttestationProof(attestationProofId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<NeoServiceLayer.Api.Models.ApiResponse<AttestationProof>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(attestationProofId, response.Data.Id);
            Assert.Equal(attestationProof.Report, response.Data.Report);
            Assert.Equal(attestationProof.Signature, response.Data.Signature);
            Assert.Equal(attestationProof.MrEnclave, response.Data.MrEnclave);
            Assert.Equal(attestationProof.MrSigner, response.Data.MrSigner);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAttestationProof_NonExistingProofId_ReturnsNotFoundResult()
        {
            // Arrange
            var attestationProofId = Guid.NewGuid().ToString();

            _mockAttestationService.Setup(x => x.GetAttestationProofAsync(attestationProofId))
                .ReturnsAsync((AttestationProof)null);

            // Act
            var result = await _controller.GetAttestationProof(attestationProofId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<NeoServiceLayer.Api.Models.ApiResponse<AttestationProof>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal("NOT_FOUND", response.Error.Code);
            Assert.Contains(attestationProofId, response.Error.Message);
        }
    }
}
