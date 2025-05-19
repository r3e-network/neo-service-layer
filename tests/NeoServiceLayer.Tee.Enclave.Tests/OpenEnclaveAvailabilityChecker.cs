using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    /// <summary>
    /// Utility class to check if the OpenEnclave SDK is available on the system.
    /// </summary>
    public static class OpenEnclaveAvailabilityChecker
    {
        private static bool? _isAvailable;
        private static string _sdkPath;
        private static string _errorMessage;

        /// <summary>
        /// Checks if the OpenEnclave SDK is available on the system.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <returns>True if the OpenEnclave SDK is available, false otherwise.</returns>
        public static bool IsAvailable(ILogger logger)
        {
            if (_isAvailable.HasValue)
            {
                return _isAvailable.Value;
            }

            try
            {
                logger.LogInformation("Checking if OpenEnclave SDK is available...");

                // Check if we're running on Windows
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _errorMessage = "OpenEnclave SDK is only supported on Windows in this implementation.";
                    logger.LogWarning(_errorMessage);
                    _isAvailable = false;
                    return false;
                }

                // Check for the OpenEnclave SDK installation
                string[] possiblePaths = new[]
                {
                    Environment.GetEnvironmentVariable("OPEN_ENCLAVE_SDK"),
                    Environment.GetEnvironmentVariable("OPEN_ENCLAVE_SDK_PATH"),
                    @"C:\openenclave\sdk",
                    @"C:\Program Files\Open Enclave SDK",
                    @"C:\OpenEnclave"
                };

                foreach (var path in possiblePaths)
                {
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }

                    if (Directory.Exists(path))
                    {
                        // Check for oehost.dll or oedebugrt.dll
                        bool hasBin = Directory.Exists(Path.Combine(path, "bin"));
                        bool hasLib = Directory.Exists(Path.Combine(path, "lib"));

                        if (hasBin && (
                            File.Exists(Path.Combine(path, "bin", "oehost.dll")) ||
                            File.Exists(Path.Combine(path, "bin", "oedebugrt.dll")) ||
                            File.Exists(Path.Combine(path, "bin", "oeedger8r.exe"))))
                        {
                            _sdkPath = path;
                            _isAvailable = true;
                            logger.LogInformation("OpenEnclave SDK found at: {Path} (bin directory)", _sdkPath);
                            return true;
                        }

                        // Check for the OpenEnclave SDK tools
                        if (hasBin && Directory.GetFiles(Path.Combine(path, "bin"), "oe*.exe").Length > 0)
                        {
                            _sdkPath = path;
                            _isAvailable = true;
                            logger.LogInformation("OpenEnclave SDK found at: {Path} (bin directory with tools)", _sdkPath);
                            return true;
                        }

                        if (hasLib && (
                            File.Exists(Path.Combine(path, "lib", "openenclave", "host", "oehost.lib")) ||
                            Directory.Exists(Path.Combine(path, "lib", "openenclave", "enclave"))))
                        {
                            _sdkPath = path;
                            _isAvailable = true;
                            logger.LogInformation("OpenEnclave SDK found at: {Path} (lib directory)", _sdkPath);
                            return true;
                        }
                    }
                }

                // Check if the DLL is in the system path
                try
                {
                    // Try to load the DLL
                    IntPtr dllHandle = IntPtr.Zero;

                    // Try different DLLs that might be available
                    string[] dlls = new[] { "oehost", "oedebugrt" };

                    foreach (var dll in dlls)
                    {
                        try
                        {
                            dllHandle = NativeLibrary.Load(dll);
                            if (dllHandle != IntPtr.Zero)
                            {
                                NativeLibrary.Free(dllHandle);
                                _isAvailable = true;
                                logger.LogInformation("OpenEnclave SDK found in system path (loaded {Dll})", dll);
                                return true;
                            }
                        }
                        catch (Exception dllEx)
                        {
                            logger.LogDebug(dllEx, "Failed to load {Dll} from system path", dll);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Failed to check for OpenEnclave DLLs in system path");
                }

                _errorMessage = "OpenEnclave SDK not found. Please install it or set the OPEN_ENCLAVE_SDK_PATH environment variable.";
                logger.LogWarning(_errorMessage);
                _isAvailable = false;
                return false;
            }
            catch (Exception ex)
            {
                _errorMessage = $"Error checking for OpenEnclave SDK: {ex.Message}";
                logger.LogError(ex, _errorMessage);
                _isAvailable = false;
                return false;
            }
        }

        /// <summary>
        /// Gets the path to the OpenEnclave SDK installation.
        /// </summary>
        /// <returns>The path to the OpenEnclave SDK installation, or null if not available.</returns>
        public static string GetSdkPath()
        {
            return _sdkPath;
        }

        /// <summary>
        /// Gets the error message if the OpenEnclave SDK is not available.
        /// </summary>
        /// <returns>The error message, or null if the SDK is available.</returns>
        public static string GetErrorMessage()
        {
            return _errorMessage;
        }
    }
}
