using System;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using Microsoft.EntityFrameworkCore;
using NeoServiceLayer.Infrastructure.Data;
using NeoServiceLayer.Infrastructure.Data.Repositories;
using NeoServiceLayer.Infrastructure.Data.Entities;
using NeoServiceLayer.Shared.Models;
using Xunit;

namespace NeoServiceLayer.Infrastructure.Tests.Data.Repositories
{
    public class VerificationResultRepositoryTests
    {
        private readonly DbContextOptions<NeoServiceLayerDbContext> _options;

        public VerificationResultRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<NeoServiceLayerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task AddVerificationResultAsync_ShouldAddVerificationResult()
        {
            // Arrange
            var verificationResult = new VerificationResult
            {
                VerificationId = Guid.NewGuid().ToString(),
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                Metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "key1", "value1" },
                    { "key2", 123 }
                },
                Reason = "Pending verification"
            };
            string verificationType = "KYC";
            string identityData = "{\"name\":\"John Doe\"}";

            // Act
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var repository = new VerificationResultRepository(context);
                await repository.AddVerificationResultAsync(verificationResult, verificationType, identityData);
            }

            // Assert
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var savedResult = await context.VerificationResults.FindAsync(verificationResult.VerificationId);
                Assert.NotNull(savedResult);
                Assert.Equal(verificationResult.Status, savedResult.Status);
                Assert.Equal(verificationType, savedResult.VerificationType);
                Assert.Equal(identityData, savedResult.IdentityData);
            }
        }

        [Fact]
        public async Task GetVerificationResultByIdAsync_ShouldReturnVerificationResult()
        {
            // Arrange
            var verificationResult = new VerificationResult
            {
                VerificationId = Guid.NewGuid().ToString(),
                Status = "pending",
                VerificationType = "KYC",
                IdentityData = "{\"name\":\"John Doe\"}",
                CreatedAt = DateTime.UtcNow,
                Metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "key1", "value1" },
                    { "key2", 123 }
                },
                Reason = "Pending verification"
            };

            using (var context = new NeoServiceLayerDbContext(_options))
            {
                context.VerificationResults.Add(VerificationResultEntity.FromDomainModel(verificationResult, verificationResult.VerificationType, verificationResult.IdentityData));
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var repository = new VerificationResultRepository(context);
                var result = await repository.GetVerificationResultByIdAsync(verificationResult.VerificationId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(verificationResult.VerificationId, result.VerificationId);
                Assert.Equal(verificationResult.Status, result.Status);
                Assert.Equal(verificationResult.VerificationType, result.VerificationType);
                Assert.Equal(verificationResult.IdentityData, result.IdentityData);
            }
        }

        [Fact]
        public async Task UpdateVerificationResultAsync_ShouldUpdateVerificationResult()
        {
            // Arrange
            var verificationResult = new VerificationResult
            {
                VerificationId = Guid.NewGuid().ToString(),
                Status = "pending",
                VerificationType = "KYC",
                IdentityData = "{\"name\":\"John Doe\"}",
                CreatedAt = DateTime.UtcNow,
                Metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "key1", "value1" },
                    { "key2", 123 }
                },
                Reason = "Pending verification"
            };

            using (var context = new NeoServiceLayerDbContext(_options))
            {
                context.VerificationResults.Add(VerificationResultEntity.FromDomainModel(verificationResult, verificationResult.VerificationType, verificationResult.IdentityData));
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var repository = new VerificationResultRepository(context);
                var resultToUpdate = await repository.GetVerificationResultByIdAsync(verificationResult.VerificationId);
                resultToUpdate.Status = "completed";
                resultToUpdate.Verified = true;
                resultToUpdate.Score = 0.95;
                resultToUpdate.Reason = "Verification successful";
                resultToUpdate.ProcessedAt = DateTime.UtcNow;

                await repository.UpdateVerificationResultAsync(resultToUpdate);
            }

            // Assert
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var updatedResult = await context.VerificationResults.FindAsync(verificationResult.VerificationId);
                Assert.NotNull(updatedResult);
                Assert.Equal("completed", updatedResult.Status);
                Assert.Equal(true, updatedResult.Verified);
                Assert.Equal(0.95, updatedResult.Score);
                Assert.Equal("Verification successful", updatedResult.Reason);
                Assert.NotNull(updatedResult.ProcessedAt);
            }
        }
    }
}
