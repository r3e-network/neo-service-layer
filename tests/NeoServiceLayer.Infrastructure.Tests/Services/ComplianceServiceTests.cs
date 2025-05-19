using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Data.Repositories;
using NeoServiceLayer.Infrastructure.Services;
using NeoServiceLayer.Shared.Models;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;
using Task = System.Threading.Tasks.Task;
using SharedVerificationResult = NeoServiceLayer.Shared.Models.VerificationResult;

namespace NeoServiceLayer.Infrastructure.Tests.Services
{
    public class ComplianceServiceTests
    {
        private readonly Mock<NeoServiceLayer.Tee.Host.Services.ITeeHostService> _mockTeeHostService;
        private readonly Mock<IVerificationResultRepository> _mockVerificationResultRepository;
        private readonly Mock<ILogger<ComplianceService>> _mockLogger;
        private readonly ComplianceService _complianceService;

        public ComplianceServiceTests()
        {
            _mockTeeHostService = new Mock<NeoServiceLayer.Tee.Host.Services.ITeeHostService>();
            _mockVerificationResultRepository = new Mock<IVerificationResultRepository>();
            _mockLogger = new Mock<ILogger<ComplianceService>>();

            // Create an adapter that implements Core.Interfaces.ITeeHostService
            var teeHostServiceAdapter = new Mocks.TeeHostServiceAdapter(_mockTeeHostService.Object);

            _complianceService = new ComplianceService(
                teeHostServiceAdapter,
                _mockVerificationResultRepository.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task VerifyIdentityAsync_ValidData_ReturnsVerificationId()
        {
            // Arrange
            string identityData = "encrypted-identity-data";
            string verificationType = "kyc";
            string expectedVerificationId = Guid.NewGuid().ToString();

            var teeResponse = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.Compliance,
                Data = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "verification_id", expectedVerificationId }
                }),
                CreatedAt = DateTime.UtcNow
            };

            _mockTeeHostService.Setup(x => x.SendMessageAsync(It.IsAny<TeeMessage>()))
                .ReturnsAsync(teeResponse);

            // Act
            var result = await _complianceService.VerifyIdentityAsync(identityData, verificationType);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedVerificationId, result);

            // Verify TEE host service was called with correct parameters
            _mockTeeHostService.Verify(x => x.SendMessageAsync(It.Is<TeeMessage>(m =>
                m.Type == TeeMessageType.Compliance &&
                m.Data.Contains("verify_identity") &&
                m.Data.Contains(identityData) &&
                m.Data.Contains(verificationType))),
                Times.Once);

            // Verify repository was called to store the verification result
            _mockVerificationResultRepository.Verify(x => x.AddVerificationResultAsync(It.Is<SharedVerificationResult>(v =>
                v.VerificationId == expectedVerificationId &&
                v.Status == "pending"),
                It.IsAny<string>(),
                It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task VerifyIdentityAsync_NullIdentityData_ThrowsArgumentException()
        {
            // Arrange
            string identityData = null;
            string verificationType = "kyc";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _complianceService.VerifyIdentityAsync(identityData, verificationType));
        }

        [Fact]
        public async Task VerifyIdentityAsync_NullVerificationType_ThrowsArgumentException()
        {
            // Arrange
            string identityData = "encrypted-identity-data";
            string verificationType = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _complianceService.VerifyIdentityAsync(identityData, verificationType));
        }

        [Fact]
        public async Task GetVerificationResultAsync_ExistingVerificationId_ReturnsVerificationResult()
        {
            // Arrange
            string verificationId = Guid.NewGuid().ToString();
            var expectedResult = new SharedVerificationResult
            {
                VerificationId = verificationId,
                Status = "completed",
                Verified = true,
                Score = 0.95,
                Reason = "Verification successful",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                ProcessedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    { "verified", true },
                    { "score", 0.95 }
                }
            };

            _mockVerificationResultRepository.Setup(x => x.GetVerificationResultByIdAsync(verificationId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _complianceService.GetVerificationResultAsync(verificationId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(verificationId, result.VerificationId);
            Assert.Equal("completed", result.Status);
            Assert.Equal(expectedResult.Verified, result.Verified);
            Assert.Equal(expectedResult.Score, result.Score);
            Assert.Equal(expectedResult.Reason, result.Reason);
            Assert.Equal(expectedResult.CreatedAt, result.CreatedAt);
            Assert.Equal(expectedResult.ProcessedAt, result.ProcessedAt);
            Assert.Equal(expectedResult.Metadata, result.Metadata);

            // Verify repository was called
            _mockVerificationResultRepository.Verify(x => x.GetVerificationResultByIdAsync(verificationId), Times.Once);
        }

        [Fact]
        public async Task GetVerificationResultAsync_NonExistingVerificationId_ReturnsNull()
        {
            // Arrange
            string verificationId = Guid.NewGuid().ToString();

            _mockVerificationResultRepository.Setup(x => x.GetVerificationResultByIdAsync(verificationId))
                .ReturnsAsync((SharedVerificationResult)null);

            // Act
            var result = await _complianceService.GetVerificationResultAsync(verificationId);

            // Assert
            Assert.Null(result);

            // Verify repository was called
            _mockVerificationResultRepository.Verify(x => x.GetVerificationResultByIdAsync(verificationId), Times.Once);
        }

        [Fact]
        public async Task GetVerificationResultAsync_NullVerificationId_ThrowsArgumentException()
        {
            // Arrange
            string verificationId = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _complianceService.GetVerificationResultAsync(verificationId));
        }

        [Fact]
        public async Task CheckTransactionComplianceAsync_ValidData_ReturnsComplianceResult()
        {
            // Arrange
            string transactionData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                UserId = "user123",
                Amount = 5000.0,
                Currency = "USD",
                Type = "transfer",
                Destination = "user456",
                DestinationCountry = "US",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            })));

            var expectedResult = new ComplianceCheckResult
            {
                Compliant = true,
                Reason = "Transaction complies with all regulations",
                RiskScore = 0.05
            };

            var teeResponse = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.Compliance,
                Data = JsonSerializer.Serialize(expectedResult),
                CreatedAt = DateTime.UtcNow
            };

            _mockTeeHostService.Setup(x => x.SendMessageAsync(It.IsAny<TeeMessage>()))
                .ReturnsAsync(teeResponse);

            // Act
            var result = await _complianceService.CheckTransactionComplianceAsync(transactionData);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.Compliant, result.Compliant);
            Assert.Equal(expectedResult.Reason, result.Reason);
            Assert.Equal(expectedResult.RiskScore, result.RiskScore);

            // Verify TEE host service was called with correct parameters
            _mockTeeHostService.Verify(x => x.SendMessageAsync(It.Is<TeeMessage>(m =>
                m.Type == TeeMessageType.Compliance &&
                m.Data.Contains("check_transaction_compliance") &&
                m.Data.Contains(transactionData))),
                Times.Once);
        }

        [Fact]
        public async Task CheckTransactionComplianceAsync_NullTransactionData_ThrowsArgumentException()
        {
            // Arrange
            string transactionData = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _complianceService.CheckTransactionComplianceAsync(transactionData));
        }


    }
}
