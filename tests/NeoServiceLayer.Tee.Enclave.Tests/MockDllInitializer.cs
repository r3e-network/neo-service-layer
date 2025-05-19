using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    /// <summary>
    /// Initializes mock DLLs for testing.
    /// </summary>
    /// <remarks>
    /// This class provides methods to initialize mock DLLs for testing purposes.
    /// It creates mock implementations of the native DLLs used by the OpenEnclaveInterface.
    /// </remarks>
    public static class MockDllInitializer
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Initializes the mock DLLs.
        /// </summary>
        /// <param name="logger">The logger for logging information and errors.</param>
        public static void Initialize(ILogger logger)
        {
            if (_initialized)
            {
                return;
            }

            lock (_lock)
            {
                if (_initialized)
                {
                    return;
                }

                logger.LogInformation("Initializing mock DLLs");

                // Set the environment variable to indicate that we're using mock DLLs
                Environment.SetEnvironmentVariable("OE_SIMULATION", "1");

                // Register the mock DLL functions
                RegisterMockDllFunctions();

                _initialized = true;
                logger.LogInformation("Mock DLLs initialized successfully");
            }
        }

        /// <summary>
        /// Registers the mock DLL functions.
        /// </summary>
        private static void RegisterMockDllFunctions()
        {
            // In a real implementation, we would register mock implementations of the native DLL functions
            // For now, we'll just rely on the simulation mode flag to use mock implementations
        }
    }
}
