using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests.Storage.PersistentStorage
{
    public class StorageTransactionTests
    {
        private readonly Mock<ILogger<StorageTransaction>> _loggerMock;
        private readonly Mock<IPersistentStorageProvider> _providerMock;

        public StorageTransactionTests()
        {
            _loggerMock = new Mock<ILogger<StorageTransaction>>();
            _providerMock = new Mock<IPersistentStorageProvider>();
        }

        [Fact]
        public void Constructor_InitializesProperties()
        {
            // Act
            var transaction = new StorageTransaction(_loggerMock.Object, _providerMock.Object);

            // Assert
            Assert.NotNull(transaction.TransactionId);
            Assert.False(transaction.IsCommitted);
            Assert.False(transaction.IsRolledBack);
        }

        [Fact]
        public async Task CommitAsync_ExecutesOperations()
        {
            // Arrange
            var transaction = new StorageTransaction(_loggerMock.Object, _providerMock.Object);
            string key1 = "test_key_1";
            string key2 = "test_key_2";
            byte[] data1 = Encoding.UTF8.GetBytes("test_data_1");
            
            await transaction.WriteAsync(key1, data1);
            await transaction.DeleteAsync(key2);

            // Act
            await transaction.CommitAsync();

            // Assert
            _providerMock.Verify(p => p.WriteAsync(key1, data1), Times.Once);
            _providerMock.Verify(p => p.DeleteAsync(key2), Times.Once);
            _providerMock.Verify(p => p.FlushAsync(), Times.Once);
            Assert.True(transaction.IsCommitted);
            Assert.False(transaction.IsRolledBack);
        }

        [Fact]
        public async Task RollbackAsync_ClearsOperations()
        {
            // Arrange
            var transaction = new StorageTransaction(_loggerMock.Object, _providerMock.Object);
            string key = "test_key";
            byte[] data = Encoding.UTF8.GetBytes("test_data");
            
            await transaction.WriteAsync(key, data);

            // Act
            await transaction.RollbackAsync();

            // Assert
            _providerMock.Verify(p => p.WriteAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
            _providerMock.Verify(p => p.DeleteAsync(It.IsAny<string>()), Times.Never);
            Assert.False(transaction.IsCommitted);
            Assert.True(transaction.IsRolledBack);
        }

        [Fact]
        public async Task CommitAsync_AfterCommit_ThrowsException()
        {
            // Arrange
            var transaction = new StorageTransaction(_loggerMock.Object, _providerMock.Object);
            await transaction.CommitAsync();

            // Act & Assert
            await Assert.ThrowsAsync<StorageException>(() => transaction.CommitAsync());
        }

        [Fact]
        public async Task CommitAsync_AfterRollback_ThrowsException()
        {
            // Arrange
            var transaction = new StorageTransaction(_loggerMock.Object, _providerMock.Object);
            await transaction.RollbackAsync();

            // Act & Assert
            await Assert.ThrowsAsync<StorageException>(() => transaction.CommitAsync());
        }

        [Fact]
        public async Task RollbackAsync_AfterCommit_ThrowsException()
        {
            // Arrange
            var transaction = new StorageTransaction(_loggerMock.Object, _providerMock.Object);
            await transaction.CommitAsync();

            // Act & Assert
            await Assert.ThrowsAsync<StorageException>(() => transaction.RollbackAsync());
        }

        [Fact]
        public async Task RollbackAsync_AfterRollback_ThrowsException()
        {
            // Arrange
            var transaction = new StorageTransaction(_loggerMock.Object, _providerMock.Object);
            await transaction.RollbackAsync();

            // Act & Assert
            await Assert.ThrowsAsync<StorageException>(() => transaction.RollbackAsync());
        }

        [Fact]
        public async Task WriteAsync_AfterCommit_ThrowsException()
        {
            // Arrange
            var transaction = new StorageTransaction(_loggerMock.Object, _providerMock.Object);
            await transaction.CommitAsync();

            // Act & Assert
            await Assert.ThrowsAsync<StorageException>(() => transaction.WriteAsync("key", new byte[0]));
        }

        [Fact]
        public async Task DeleteAsync_AfterCommit_ThrowsException()
        {
            // Arrange
            var transaction = new StorageTransaction(_loggerMock.Object, _providerMock.Object);
            await transaction.CommitAsync();

            // Act & Assert
            await Assert.ThrowsAsync<StorageException>(() => transaction.DeleteAsync("key"));
        }

        [Fact]
        public void Dispose_AutomaticallyRollsBackUncommittedTransaction()
        {
            // Arrange
            var transaction = new StorageTransaction(_loggerMock.Object, _providerMock.Object);
            string key = "test_key";
            byte[] data = Encoding.UTF8.GetBytes("test_data");
            
            transaction.WriteAsync(key, data).GetAwaiter().GetResult();

            // Act
            transaction.Dispose();

            // Assert
            _providerMock.Verify(p => p.WriteAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
            _providerMock.Verify(p => p.DeleteAsync(It.IsAny<string>()), Times.Never);
        }
    }
}
