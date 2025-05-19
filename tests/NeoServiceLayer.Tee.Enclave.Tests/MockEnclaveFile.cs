using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    /// <summary>
    /// Creates a mock enclave file for testing purposes.
    /// </summary>
    /// <remarks>
    /// This class creates a mock enclave file that can be used for testing without requiring
    /// the actual SGX enclave. It creates a dummy file with the same name and location as
    /// the real enclave file, which allows the tests to run without the "Enclave file not found" error.
    /// </remarks>
    public class MockEnclaveFile : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _enclavePath;
        private readonly string _uniqueId;
        private bool _created;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockEnclaveFile"/> class.
        /// </summary>
        /// <param name="logger">The logger for logging information and errors.</param>
        /// <param name="enclavePath">The path where the mock enclave file should be created. If null, a default path will be used.</param>
        public MockEnclaveFile(ILogger logger, string enclavePath = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uniqueId = Guid.NewGuid().ToString("N");
            _enclavePath = enclavePath ?? GetDefaultEnclavePath();
        }

        /// <summary>
        /// Creates the mock enclave file.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CreateAsync()
        {
            try
            {
                // Create the directory if it doesn't exist
                string directory = Path.GetDirectoryName(_enclavePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    _logger.LogInformation("Creating directory: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                }

                // Create a dummy enclave file
                _logger.LogInformation("Creating mock enclave file: {Path}", _enclavePath);
                await File.WriteAllTextAsync(_enclavePath, "This is a mock enclave file for testing purposes.");
                _created = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create mock enclave file: {Path}", _enclavePath);
                throw;
            }
        }

        /// <summary>
        /// Gets the default path for the enclave file.
        /// </summary>
        /// <returns>The default path for the enclave file.</returns>
        private string GetDefaultEnclavePath()
        {
            // This should match the path expected by the OpenEnclaveInterface class
            // but with a unique ID to avoid conflicts
            return Path.Combine("..", "src", "NeoServiceLayer.Tee.Enclave", "bin", "Debug", "net9.0", $"liboe_enclave_{_uniqueId}.signed.so");
        }

        /// <summary>
        /// Gets the path to the mock enclave file.
        /// </summary>
        public string MockEnclavePath => _enclavePath;

        /// <summary>
        /// Deletes the mock enclave file.
        /// </summary>
        public void Dispose()
        {
            if (_created && File.Exists(_enclavePath))
            {
                try
                {
                    _logger.LogInformation("Deleting mock enclave file: {Path}", _enclavePath);
                    File.Delete(_enclavePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete mock enclave file: {Path}", _enclavePath);
                }
            }
        }
    }
}
