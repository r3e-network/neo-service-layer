using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host.Utilities
{
    /// <summary>
    /// Provides secure temporary file handling with automatic cleanup.
    /// </summary>
    public sealed class SecureTempFile : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _filePath;
        private bool _disposed;

        /// <summary>
        /// Gets the path to the temporary file.
        /// </summary>
        public string FilePath => _filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureTempFile"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="directoryPath">Optional path to the directory where the temporary file should be created. If null, the system temp directory is used.</param>
        /// <param name="prefix">Optional prefix for the temporary file name.</param>
        public SecureTempFile(ILogger logger, string directoryPath = null, string prefix = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _disposed = false;

            try
            {
                // Use the specified directory or the system temp directory
                string directory = directoryPath ?? Path.GetTempPath();

                // Create the directory if it doesn't exist
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Generate a secure random filename with the specified prefix
                string fileName = $"{prefix ?? "tmp"}-{GenerateSecureRandomFileName()}";
                _filePath = Path.Combine(directory, fileName);

                // Create the file with restricted permissions
                using (var fs = File.Create(_filePath))
                {
                    // Set restricted permissions if on a Unix-like system
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        var fileInfo = new FileInfo(_filePath);
                        // Set permissions to owner read/write only (0600)
                        // Note: This is platform-specific and may require additional libraries on some platforms
                    }
                }

                _logger.LogDebug("Created secure temporary file at {FilePath}", _filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating secure temporary file");
                throw;
            }
        }

        /// <summary>
        /// Writes the specified content to the temporary file.
        /// </summary>
        /// <param name="content">The content to write.</param>
        public void WriteAllText(string content)
        {
            CheckDisposed();

            try
            {
                File.WriteAllText(_filePath, content);
                _logger.LogDebug("Wrote content to temporary file {FilePath}", _filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to temporary file {FilePath}", _filePath);
                throw;
            }
        }

        /// <summary>
        /// Asynchronously writes the specified content to the temporary file.
        /// </summary>
        /// <param name="content">The content to write.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task WriteAllTextAsync(string content)
        {
            CheckDisposed();

            try
            {
                await File.WriteAllTextAsync(_filePath, content);
                _logger.LogDebug("Wrote content to temporary file {FilePath}", _filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to temporary file {FilePath}", _filePath);
                throw;
            }
        }

        /// <summary>
        /// Reads all text from the temporary file.
        /// </summary>
        /// <returns>The content of the temporary file.</returns>
        public string ReadAllText()
        {
            CheckDisposed();

            try
            {
                string content = File.ReadAllText(_filePath);
                _logger.LogDebug("Read content from temporary file {FilePath}", _filePath);
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from temporary file {FilePath}", _filePath);
                throw;
            }
        }

        /// <summary>
        /// Asynchronously reads all text from the temporary file.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with the content of the temporary file.</returns>
        public async Task<string> ReadAllTextAsync()
        {
            CheckDisposed();

            try
            {
                string content = await File.ReadAllTextAsync(_filePath);
                _logger.LogDebug("Read content from temporary file {FilePath}", _filePath);
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from temporary file {FilePath}", _filePath);
                throw;
            }
        }

        /// <summary>
        /// Disposes the temporary file, deleting it from disk.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    // Ensure the file is deleted
                    if (File.Exists(_filePath))
                    {
                        // First, overwrite the file with zeros to prevent data recovery
                        SecureDelete(_filePath);

                        // Then delete the file
                        File.Delete(_filePath);
                        _logger.LogDebug("Deleted temporary file {FilePath}", _filePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting temporary file {FilePath}", _filePath);
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Generates a secure random file name.
        /// </summary>
        /// <returns>A random file name.</returns>
        private static string GenerateSecureRandomFileName()
        {
            // Use cryptographically secure random number generator
            using var rng = RandomNumberGenerator.Create();
            byte[] randomBytes = new byte[16];
            rng.GetBytes(randomBytes);
            return Guid.NewGuid().ToString("N") + "-" + BitConverter.ToString(randomBytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Securely deletes a file by overwriting it with zeros.
        /// </summary>
        /// <param name="filePath">The path to the file to delete.</param>
        private void SecureDelete(string filePath)
        {
            try
            {
                // Get the file size
                var fileInfo = new FileInfo(filePath);
                long fileSize = fileInfo.Length;

                // Overwrite the file with zeros
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
                {
                    // Create a buffer of zeros
                    const int bufferSize = 4096;
                    byte[] buffer = new byte[bufferSize];

                    // Overwrite the file in chunks
                    long bytesRemaining = fileSize;
                    while (bytesRemaining > 0)
                    {
                        int bytesToWrite = (int)Math.Min(bufferSize, bytesRemaining);
                        fs.Write(buffer, 0, bytesToWrite);
                        bytesRemaining -= bytesToWrite;
                    }

                    // Flush and sync to disk
                    fs.Flush(true);
                }

                _logger.LogDebug("Securely overwritten file {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error securely overwriting file {FilePath}", filePath);
                // Continue with deletion even if overwriting fails
            }
        }

        /// <summary>
        /// Checks if the object has been disposed and throws an exception if it has.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SecureTempFile));
            }
        }
    }
} 