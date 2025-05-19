using System;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// Helper class to initialize the mock DLLs.
    /// </summary>
    /// <remarks>
    /// This class provides a way to initialize the mock DLLs for testing purposes.
    /// It should be called before any OpenEnclave operations are performed.
    /// </remarks>
    public static class MockDllInitializer
    {
        private static bool _initialized;

        /// <summary>
        /// Initializes the mock DLLs.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        public static void Initialize(ILogger logger)
        {
            if (_initialized)
            {
                return;
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            // Check if we're running in simulation mode
            if (Environment.GetEnvironmentVariable("OE_SIMULATION") != "1" &&
                Environment.GetEnvironmentVariable("SGX_SIMULATION") != "1")
            {
                logger.LogInformation("Not running in simulation mode, skipping mock DLL initialization");
                return;
            }

            // Initialize the mock DLLs
            MockOpenEnclaveDll.Initialize(logger);
            MockNeoServiceLayerEnclaveDll.Initialize(logger);

            _initialized = true;
            logger.LogInformation("Mock DLLs initialized");
        }

        /// <summary>
        /// Checks if the mock DLLs are initialized.
        /// </summary>
        /// <returns>True if the mock DLLs are initialized, false otherwise.</returns>
        public static bool IsInitialized()
        {
            return _initialized;
        }
    }
}
