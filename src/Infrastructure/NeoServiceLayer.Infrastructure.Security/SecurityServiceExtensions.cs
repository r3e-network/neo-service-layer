using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.Security;

/// <summary>
/// Extension methods for SecurityService to provide additional encryption and compression functionality.
/// </summary>
public static class SecurityServiceExtensions
{
    /// <summary>
    /// Encrypts data asynchronously.
    /// </summary>
    public static async Task<byte[]> EncryptAsync(this ISecurityService securityService, byte[] data, string keyId = null)
    {
        var options = new EncryptionOptions { KeyId = keyId };
        var result = await securityService.EncryptDataAsync(data, options);
        
        if (!result.Success)
        {
            throw new SecurityException($"Encryption failed: {result.ErrorMessage}");
        }
        
        return result.EncryptedData ?? throw new SecurityException("Encryption returned null data");
    }

    /// <summary>
    /// Decrypts data asynchronously.
    /// </summary>
    public static async Task<byte[]> DecryptAsync(this ISecurityService securityService, byte[] encryptedData, string keyId)
    {
        var result = await securityService.DecryptDataAsync(encryptedData, keyId);
        
        if (!result.Success)
        {
            throw new SecurityException($"Decryption failed: {result.ErrorMessage}");
        }
        
        return result.DecryptedData ?? throw new SecurityException("Decryption returned null data");
    }

    /// <summary>
    /// Compresses data asynchronously using GZip.
    /// </summary>
    public static async Task<byte[]> CompressAsync(this ISecurityService securityService, byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return data;
        }

        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            await gzip.WriteAsync(data, 0, data.Length);
        }
        
        return output.ToArray();
    }

    /// <summary>
    /// Decompresses data asynchronously using GZip.
    /// </summary>
    public static async Task<byte[]> DecompressAsync(this ISecurityService securityService, byte[] compressedData)
    {
        if (compressedData == null || compressedData.Length == 0)
        {
            return compressedData;
        }

        using var input = new MemoryStream(compressedData);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(input, CompressionMode.Decompress))
        {
            await gzip.CopyToAsync(output);
        }
        
        return output.ToArray();
    }

    /// <summary>
    /// Encrypts a string asynchronously.
    /// </summary>
    public static async Task<string> EncryptStringAsync(this ISecurityService securityService, string plainText, string keyId = null)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        var data = Encoding.UTF8.GetBytes(plainText);
        var encrypted = await securityService.EncryptAsync(data, keyId);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// Decrypts a string asynchronously.
    /// </summary>
    public static async Task<string> DecryptStringAsync(this ISecurityService securityService, string encryptedText, string keyId)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            return encryptedText;
        }

        var encrypted = Convert.FromBase64String(encryptedText);
        var decrypted = await securityService.DecryptAsync(encrypted, keyId);
        return Encoding.UTF8.GetString(decrypted);
    }

    /// <summary>
    /// Compresses a string asynchronously.
    /// </summary>
    public static async Task<string> CompressStringAsync(this ISecurityService securityService, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var data = Encoding.UTF8.GetBytes(text);
        var compressed = await securityService.CompressAsync(data);
        return Convert.ToBase64String(compressed);
    }

    /// <summary>
    /// Decompresses a string asynchronously.
    /// </summary>
    public static async Task<string> DecompressStringAsync(this ISecurityService securityService, string compressedText)
    {
        if (string.IsNullOrEmpty(compressedText))
        {
            return compressedText;
        }

        var compressed = Convert.FromBase64String(compressedText);
        var decompressed = await securityService.DecompressAsync(compressed);
        return Encoding.UTF8.GetString(decompressed);
    }
}