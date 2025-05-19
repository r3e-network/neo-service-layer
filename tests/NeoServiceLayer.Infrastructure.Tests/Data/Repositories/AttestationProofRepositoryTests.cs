using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using Microsoft.EntityFrameworkCore;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Data;
using NeoServiceLayer.Infrastructure.Data.Repositories;
using NeoServiceLayer.Infrastructure.Data.Entities;
using Xunit;

namespace NeoServiceLayer.Infrastructure.Tests.Data.Repositories
{
    public class AttestationProofRepositoryTests
    {
        private readonly DbContextOptions<NeoServiceLayerDbContext> _options;

        public AttestationProofRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<NeoServiceLayerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task AddAttestationProofAsync_ShouldAddAttestationProof()
        {
            // Arrange
            var attestationProof = new AttestationProof
            {
                Id = Guid.NewGuid().ToString(),
                Report = "report-data",
                Signature = "signature-data",
                MrEnclave = "mr-enclave-value",
                MrSigner = "mr-signer-value",
                ProductId = "product-id",
                SecurityVersion = "1.0",
                Attributes = "attributes-data",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Metadata = new Dictionary<string, object>
                {
                    { "key1", "value1" },
                    { "key2", 123 }
                }
            };

            // Act
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var repository = new AttestationProofRepository(context);
                await repository.AddAttestationProofAsync(attestationProof);
            }

            // Assert
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var savedProof = await context.AttestationProofs.FindAsync(attestationProof.Id);
                Assert.NotNull(savedProof);
                Assert.Equal(attestationProof.Report, savedProof.Report);
                Assert.Equal(attestationProof.Signature, savedProof.Signature);
                Assert.Equal(attestationProof.MrEnclave, savedProof.MrEnclave);
                Assert.Equal(attestationProof.MrSigner, savedProof.MrSigner);
                Assert.Equal(attestationProof.ProductId, savedProof.ProductId);
                Assert.Equal(attestationProof.SecurityVersion, savedProof.SecurityVersion);
                Assert.Equal(attestationProof.Attributes, savedProof.Attributes);
            }
        }

        [Fact]
        public async Task GetAttestationProofByIdAsync_ShouldReturnAttestationProof()
        {
            // Arrange
            var attestationProof = new AttestationProof
            {
                Id = Guid.NewGuid().ToString(),
                Report = "report-data",
                Signature = "signature-data",
                MrEnclave = "mr-enclave-value",
                MrSigner = "mr-signer-value",
                ProductId = "product-id",
                SecurityVersion = "1.0",
                Attributes = "attributes-data",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Metadata = new Dictionary<string, object>
                {
                    { "key1", "value1" },
                    { "key2", 123 }
                }
            };

            using (var context = new NeoServiceLayerDbContext(_options))
            {
                context.AttestationProofs.Add(AttestationProofEntity.FromDomainModel(attestationProof));
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var repository = new AttestationProofRepository(context);
                var result = await repository.GetAttestationProofByIdAsync(attestationProof.Id);

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
            }
        }

        [Fact]
        public async Task GetLatestAttestationProofAsync_ShouldReturnLatestProof()
        {
            // Arrange
            var proofs = new List<AttestationProof>
            {
                new AttestationProof
                {
                    Id = Guid.NewGuid().ToString(),
                    Report = "report-data-1",
                    Signature = "signature-data-1",
                    MrEnclave = "mr-enclave-value",
                    MrSigner = "mr-signer-value",
                    ProductId = "product-id",
                    SecurityVersion = "1.0",
                    Attributes = "attributes-data",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    ExpiresAt = DateTime.UtcNow.AddDays(28),
                    Metadata = new Dictionary<string, object>
                    {
                        { "key1", "value1" },
                        { "key2", 123 }
                    }
                },
                new AttestationProof
                {
                    Id = Guid.NewGuid().ToString(),
                    Report = "report-data-2",
                    Signature = "signature-data-2",
                    MrEnclave = "mr-enclave-value",
                    MrSigner = "mr-signer-value",
                    ProductId = "product-id",
                    SecurityVersion = "1.0",
                    Attributes = "attributes-data",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    Metadata = new Dictionary<string, object>
                    {
                        { "key1", "value1" },
                        { "key2", 123 }
                    }
                },
                new AttestationProof
                {
                    Id = Guid.NewGuid().ToString(),
                    Report = "report-data-3",
                    Signature = "signature-data-3",
                    MrEnclave = "mr-enclave-value-different",
                    MrSigner = "mr-signer-value",
                    ProductId = "product-id",
                    SecurityVersion = "1.0",
                    Attributes = "attributes-data",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    ExpiresAt = DateTime.UtcNow.AddDays(29),
                    Metadata = new Dictionary<string, object>
                    {
                        { "key1", "value1" },
                        { "key2", 123 }
                    }
                }
            };

            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var entities = proofs.Select(p => AttestationProofEntity.FromDomainModel(p)).ToList();
                context.AttestationProofs.AddRange(entities);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var repository = new AttestationProofRepository(context);
                var result = await repository.GetLatestAttestationProofAsync("mr-enclave-value");

                // Assert
                Assert.NotNull(result);
                Assert.Equal("report-data-2", result.Report);
                Assert.Equal("signature-data-2", result.Signature);
            }
        }
    }
}
