using System.Threading.Tasks;
using System.Threading;
using System;

namespace NeoServiceLayer.Services.Backup.Tests
{
    /// <summary>
    /// Test interface for encryption service
    /// </summary>
    public interface IEncryptionService
    {
        Task<byte[]> EncryptAsync(byte[] data, string key, CancellationToken cancellationToken = default);
        Task<byte[]> DecryptAsync(byte[] encryptedData, string key, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Test interface for compression service
    /// </summary>
    public interface ICompressionService
    {
        Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default);
        Task<byte[]> DecompressAsync(byte[] compressedData, CancellationToken cancellationToken = default);
    }
}