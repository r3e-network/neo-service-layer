using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Contexts;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Services;

namespace NeoServiceLayer.Infrastructure.Persistence.Tests.PostgreSQL
{
    public class SgxConfidentialStoreTests : IDisposable
    {
        private readonly NeoServiceDbContext _context;
        private readonly SgxConfidentialStore _store;
        private readonly Mock<ILogger<SgxConfidentialStore>> _loggerMock;

        public SgxConfidentialStoreTests()
        {
            var options = new DbContextOptionsBuilder<NeoServiceDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new NeoServiceDbContext(options);
            _loggerMock = new Mock<ILogger<SgxConfidentialStore>>();
            _store = new SgxConfidentialStore(_context, _loggerMock.Object);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task SealDataAsync_ValidInput_CreatesNewSealedItem()
        {
            // Arrange
            var key = "test-key";
            var data = Encoding.UTF8.GetBytes("sensitive data");
            var serviceName = "TestService";

            // Act
            var storageId = await _store.SealDataAsync(key, data, serviceName);

            // Assert
            Assert.NotNull(storageId);
            Assert.NotEmpty(storageId);

            var sealedItem = await _context.SealedDataItems
                .FirstOrDefaultAsync(s => s.Key == key && s.ServiceName == serviceName);
            
            Assert.NotNull(sealedItem);
            Assert.Equal(key, sealedItem.Key);
            Assert.Equal(serviceName, sealedItem.ServiceName);
            Assert.Equal(data, sealedItem.SealedData);
            Assert.Equal("MRSIGNER", sealedItem.PolicyType);
        }

        [Fact]
        public async Task SealDataAsync_ExistingKey_UpdatesSealedItem()
        {
            // Arrange
            var key = "test-key";
            var originalData = Encoding.UTF8.GetBytes("original data");
            var updatedData = Encoding.UTF8.GetBytes("updated data");
            var serviceName = "TestService";

            // Act
            await _store.SealDataAsync(key, originalData, serviceName);
            await _store.SealDataAsync(key, updatedData, serviceName);

            // Assert
            var sealedItems = await _context.SealedDataItems
                .Where(s => s.Key == key && s.ServiceName == serviceName)
                .ToListAsync();
            
            Assert.Single(sealedItems);
            Assert.Equal(updatedData, sealedItems[0].SealedData);
            Assert.True(sealedItems[0].AccessCount > 0);
        }

        [Fact]
        public async Task UnsealDataAsync_ExistingKey_ReturnsData()
        {
            // Arrange
            var key = "test-key";
            var data = Encoding.UTF8.GetBytes("sensitive data");
            var serviceName = "TestService";
            await _store.SealDataAsync(key, data, serviceName);

            // Act
            var unsealedData = await _store.UnsealDataAsync(key, serviceName);

            // Assert
            Assert.NotNull(unsealedData);
            Assert.Equal(data, unsealedData);
        }

        [Fact]
        public async Task UnsealDataAsync_NonExistentKey_ReturnsNull()
        {
            // Arrange
            var key = "non-existent-key";
            var serviceName = "TestService";

            // Act
            var unsealedData = await _store.UnsealDataAsync(key, serviceName);

            // Assert
            Assert.Null(unsealedData);
        }

        [Fact]
        public async Task UnsealDataAsync_ExpiredData_ReturnsNull()
        {
            // Arrange
            var key = "test-key";
            var data = Encoding.UTF8.GetBytes("sensitive data");
            var serviceName = "TestService";
            
            // Create expired data directly in context
            var sealedItem = new SealedDataItem
            {
                Key = key,
                ServiceName = serviceName,
                StorageId = "test-storage-id",
                SealedData = data,
                OriginalSize = data.Length,
                SealedSize = data.Length,
                PolicyType = "MRSIGNER",
                ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
                Fingerprint = "test-fingerprint"
            };
            
            await _context.SealedDataItems.AddAsync(sealedItem);
            await _context.SaveChangesAsync();

            // Act
            var unsealedData = await _store.UnsealDataAsync(key, serviceName);

            // Assert
            Assert.Null(unsealedData);
        }

        [Fact]
        public async Task DeleteSealedDataAsync_ExistingKey_ReturnsTrue()
        {
            // Arrange
            var key = "test-key";
            var data = Encoding.UTF8.GetBytes("sensitive data");
            var serviceName = "TestService";
            await _store.SealDataAsync(key, data, serviceName);

            // Act
            var result = await _store.DeleteSealedDataAsync(key, serviceName);

            // Assert
            Assert.True(result);
            
            var sealedItem = await _context.SealedDataItems
                .FirstOrDefaultAsync(s => s.Key == key && s.ServiceName == serviceName);
            Assert.Null(sealedItem);
        }

        [Fact]
        public async Task DeleteSealedDataAsync_NonExistentKey_ReturnsFalse()
        {
            // Arrange
            var key = "non-existent-key";
            var serviceName = "TestService";

            // Act
            var result = await _store.DeleteSealedDataAsync(key, serviceName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ListSealedDataAsync_ReturnsCorrectItems()
        {
            // Arrange
            var serviceName = "TestService";
            var otherServiceName = "OtherService";
            
            await _store.SealDataAsync("key1", Encoding.UTF8.GetBytes("data1"), serviceName);
            await _store.SealDataAsync("key2", Encoding.UTF8.GetBytes("data2"), serviceName);
            await _store.SealDataAsync("key3", Encoding.UTF8.GetBytes("data3"), otherServiceName);

            // Act
            var items = await _store.ListSealedDataAsync(serviceName);

            // Assert
            Assert.Equal(2, items.Count());
            Assert.All(items, item => Assert.Equal(serviceName, item.Key.StartsWith("key") ? serviceName : item.Key));
        }

        [Fact]
        public async Task StoreAttestationAsync_ValidInput_CreatesAttestation()
        {
            // Arrange
            var attestationId = "test-attestation";
            var quote = new byte[] { 1, 2, 3 };
            var report = new byte[] { 4, 5, 6 };
            var mrenclave = "test-mrenclave";
            var mrsigner = "test-mrsigner";

            // Act
            var result = await _store.StoreAttestationAsync(attestationId, quote, report, mrenclave, mrsigner);

            // Assert
            Assert.True(result);
            
            var attestation = await _context.EnclaveAttestations
                .FirstOrDefaultAsync(a => a.AttestationId == attestationId);
            
            Assert.NotNull(attestation);
            Assert.Equal(quote, attestation.Quote);
            Assert.Equal(report, attestation.Report);
            Assert.Equal(mrenclave, attestation.MRENCLAVE);
            Assert.Equal(mrsigner, attestation.MRSIGNER);
            Assert.Equal("Pending", attestation.Status);
        }

        [Fact]
        public async Task GetAttestationAsync_ExistingId_ReturnsAttestation()
        {
            // Arrange
            var attestationId = "test-attestation";
            var quote = new byte[] { 1, 2, 3 };
            var report = new byte[] { 4, 5, 6 };
            await _store.StoreAttestationAsync(attestationId, quote, report, "mrenclave", "mrsigner");

            // Act
            var attestation = await _store.GetAttestationAsync(attestationId);

            // Assert
            Assert.NotNull(attestation);
            Assert.Equal(attestationId, attestation.AttestationId);
        }

        [Fact]
        public async Task VerifyAttestationAsync_ExistingId_UpdatesStatus()
        {
            // Arrange
            var attestationId = "test-attestation";
            await _store.StoreAttestationAsync(attestationId, new byte[] { 1 }, new byte[] { 2 }, "mrenclave", "mrsigner");

            // Act
            var result = await _store.VerifyAttestationAsync(attestationId, "Valid", "Verification successful");

            // Assert
            Assert.True(result);
            
            var attestation = await _context.EnclaveAttestations
                .FirstOrDefaultAsync(a => a.AttestationId == attestationId);
            
            Assert.NotNull(attestation);
            Assert.Equal("Valid", attestation.Status);
            Assert.NotNull(attestation.VerifiedAt);
            Assert.Equal("Verification successful", attestation.VerificationResult);
        }

        [Fact]
        public async Task CleanupExpiredDataAsync_RemovesExpiredItems()
        {
            // Arrange
            // Create expired and non-expired items
            var expiredItem = new SealedDataItem
            {
                Key = "expired-key",
                ServiceName = "TestService",
                StorageId = "expired-storage",
                SealedData = new byte[] { 1 },
                OriginalSize = 1,
                SealedSize = 1,
                PolicyType = "MRSIGNER",
                ExpiresAt = DateTime.UtcNow.AddHours(-1),
                Fingerprint = "expired"
            };

            var validItem = new SealedDataItem
            {
                Key = "valid-key",
                ServiceName = "TestService",
                StorageId = "valid-storage",
                SealedData = new byte[] { 2 },
                OriginalSize = 1,
                SealedSize = 1,
                PolicyType = "MRSIGNER",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Fingerprint = "valid"
            };

            await _context.SealedDataItems.AddRangeAsync(expiredItem, validItem);
            await _context.SaveChangesAsync();

            // Act
            await _store.CleanupExpiredDataAsync();

            // Assert
            var remainingItems = await _context.SealedDataItems.ToListAsync();
            Assert.Single(remainingItems);
            Assert.Equal("valid-key", remainingItems[0].Key);
        }

        [Fact]
        public async Task CreatePolicyAsync_ValidInput_CreatesPolicy()
        {
            // Arrange
            var name = "TestPolicy";
            var policyType = "MRENCLAVE";
            var expirationHours = 48;
            var requireAttestation = true;

            // Act
            var policy = await _store.CreatePolicyAsync(name, policyType, expirationHours, requireAttestation);

            // Assert
            Assert.NotNull(policy);
            Assert.Equal(name, policy.Name);
            Assert.Equal(policyType, policy.PolicyType);
            Assert.Equal(expirationHours, policy.ExpirationHours);
            Assert.Equal(requireAttestation, policy.RequireAttestation);
            Assert.True(policy.IsActive);
        }

        [Fact]
        public async Task GetPolicyAsync_ExistingPolicy_ReturnsPolicy()
        {
            // Arrange
            var policyType = "MRENCLAVE";
            await _store.CreatePolicyAsync("TestPolicy", policyType, 24, true);

            // Act
            var policy = await _store.GetPolicyAsync(policyType);

            // Assert
            Assert.NotNull(policy);
            Assert.Equal(policyType, policy.PolicyType);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task SealDataAsync_InvalidKey_ThrowsArgumentException(string invalidKey)
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("data");
            var serviceName = "TestService";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _store.SealDataAsync(invalidKey, data, serviceName));
        }

        [Fact]
        public async Task SealDataAsync_EmptyData_ThrowsArgumentException()
        {
            // Arrange
            var key = "test-key";
            var data = Array.Empty<byte>();
            var serviceName = "TestService";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _store.SealDataAsync(key, data, serviceName));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task SealDataAsync_InvalidServiceName_ThrowsArgumentException(string invalidServiceName)
        {
            // Arrange
            var key = "test-key";
            var data = Encoding.UTF8.GetBytes("data");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _store.SealDataAsync(key, data, invalidServiceName));
        }
    }
}