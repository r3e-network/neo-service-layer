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
    public class HealthControllerTests
    {
        private readonly Mock<IAttestationService> _mockAttestationService;
        private readonly Mock<ILogger<HealthController>> _mockLogger;
        private readonly HealthController _controller;

        public HealthControllerTests()
        {
            _mockAttestationService = new Mock<IAttestationService>();
            _mockLogger = new Mock<ILogger<HealthController>>();
            _controller = new HealthController(_mockAttestationService.Object, _mockLogger.Object);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetHealth_AttestationServiceAvailable_ReturnsOkResultWithHealthyStatus()
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
            var result = await _controller.GetHealth();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<HealthStatus>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("healthy", response.Data.Status);
            Assert.NotNull(response.Data.Version);
            Assert.NotNull(response.Data.Timestamp);
            Assert.NotNull(response.Data.Components);
            Assert.Equal("healthy", response.Data.Components["api"]);
            Assert.Equal("healthy", response.Data.Components["attestation"]);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetHealth_AttestationServiceUnavailable_ReturnsOkResultWithUnhealthyAttestationStatus()
        {
            // Arrange
            _mockAttestationService.Setup(x => x.GetCurrentAttestationProofAsync())
                .ReturnsAsync((AttestationProof)null);

            // Act
            var result = await _controller.GetHealth();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<HealthStatus>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("healthy", response.Data.Status);
            Assert.NotNull(response.Data.Version);
            Assert.NotNull(response.Data.Timestamp);
            Assert.NotNull(response.Data.Components);
            Assert.Equal("healthy", response.Data.Components["api"]);
            Assert.Equal("unhealthy", response.Data.Components["attestation"]);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetHealth_AttestationServiceThrowsException_ReturnsOkResultWithUnhealthyStatus()
        {
            // Arrange
            _mockAttestationService.Setup(x => x.GetCurrentAttestationProofAsync())
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetHealth();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<HealthStatus>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("healthy", response.Data.Status);
            Assert.NotNull(response.Data.Version);
            Assert.NotNull(response.Data.Timestamp);
            Assert.NotNull(response.Data.Components);
            Assert.Equal("healthy", response.Data.Components["api"]);
            Assert.Equal("unhealthy", response.Data.Components["attestation"]);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetDetailedHealth_AttestationServiceAvailable_ReturnsOkResultWithDetailedHealthyStatus()
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
            var result = await _controller.GetDetailedHealth();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<DetailedHealthStatus>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("healthy", response.Data.Status);
            Assert.NotNull(response.Data.Version);
            Assert.NotNull(response.Data.Timestamp);
            Assert.NotNull(response.Data.Components);
            Assert.Equal("healthy", response.Data.Components["api"].Status);
            Assert.Equal("healthy", response.Data.Components["attestation"].Status);
            Assert.NotNull(response.Data.Components["api"].Details);
            Assert.NotNull(response.Data.Components["attestation"].Details);
            Assert.Equal(attestationProof.MrEnclave, response.Data.Components["attestation"].Details["mrEnclave"]);
            Assert.Equal(attestationProof.MrSigner, response.Data.Components["attestation"].Details["mrSigner"]);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetDetailedHealth_AttestationServiceUnavailable_ReturnsOkResultWithDetailedUnhealthyAttestationStatus()
        {
            // Arrange
            _mockAttestationService.Setup(x => x.GetCurrentAttestationProofAsync())
                .ReturnsAsync((AttestationProof)null);

            // Act
            var result = await _controller.GetDetailedHealth();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<DetailedHealthStatus>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("healthy", response.Data.Status);
            Assert.NotNull(response.Data.Version);
            Assert.NotNull(response.Data.Timestamp);
            Assert.NotNull(response.Data.Components);
            Assert.Equal("healthy", response.Data.Components["api"].Status);
            Assert.Equal("unhealthy", response.Data.Components["attestation"].Status);
            Assert.NotNull(response.Data.Components["api"].Details);
            Assert.NotNull(response.Data.Components["attestation"].Details);
            Assert.Equal("No attestation proof available", response.Data.Components["attestation"].Details["error"]);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetDetailedHealth_AttestationServiceThrowsException_ReturnsOkResultWithDetailedUnhealthyStatus()
        {
            // Arrange
            _mockAttestationService.Setup(x => x.GetCurrentAttestationProofAsync())
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetDetailedHealth();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<DetailedHealthStatus>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("healthy", response.Data.Status);
            Assert.NotNull(response.Data.Version);
            Assert.NotNull(response.Data.Timestamp);
            Assert.NotNull(response.Data.Components);
            Assert.Equal("healthy", response.Data.Components["api"].Status);
            Assert.Equal("unhealthy", response.Data.Components["attestation"].Status);
            Assert.NotNull(response.Data.Components["api"].Details);
            Assert.NotNull(response.Data.Components["attestation"].Details);
            Assert.Equal("Test exception", response.Data.Components["attestation"].Details["error"]);
        }
    }
}
