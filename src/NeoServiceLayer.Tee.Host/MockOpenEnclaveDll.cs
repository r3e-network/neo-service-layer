using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// A mock implementation of the OpenEnclave DLL for testing.
    /// </summary>
    /// <remarks>
    /// This class provides a simulation of the OpenEnclave DLL for testing purposes.
    /// It implements all the native methods that would normally be provided by the oehost DLL.
    /// 
    /// To use this class, call <see cref="Initialize"/> before any OpenEnclave operations are performed.
    /// This will set up the necessary hooks to intercept calls to the native methods.
    /// </remarks>
    public static class MockOpenEnclaveDll
    {
        private static ILogger _logger;
        private static bool _initialized;
        private static readonly Dictionary<IntPtr, MockEnclave> _enclaves = new Dictionary<IntPtr, MockEnclave>();
        private static int _nextEnclaveId = 1;

        /// <summary>
        /// Initializes the mock OpenEnclave DLL.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        public static void Initialize(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _initialized = true;
            _logger.LogInformation("Mock OpenEnclave DLL initialized");
        }

        /// <summary>
        /// Creates a mock enclave.
        /// </summary>
        /// <param name="enclavePath">The path to the enclave file.</param>
        /// <param name="type">The enclave type.</param>
        /// <param name="flags">The enclave flags.</param>
        /// <param name="config">The enclave configuration.</param>
        /// <param name="configSize">The size of the enclave configuration.</param>
        /// <param name="enclaveId">The enclave ID.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        public static int oe_create_enclave(
            string enclavePath,
            int type,
            int flags,
            IntPtr config,
            int configSize,
            out IntPtr enclaveId)
        {
            if (!_initialized)
            {
                enclaveId = IntPtr.Zero;
                return -1;
            }

            _logger.LogInformation("Creating mock enclave: {Path}, Type: {Type}, Flags: {Flags}", enclavePath, type, flags);

            // Create a new mock enclave
            var mockEnclave = new MockEnclave
            {
                Id = new IntPtr(_nextEnclaveId++),
                Path = enclavePath,
                Type = type,
                Flags = flags,
                MrEnclave = new byte[32],
                MrSigner = new byte[32],
                ProductId = 1,
                SecurityVersion = 1,
                Attributes = 0
            };

            // Initialize the MRENCLAVE and MRSIGNER values
            new Random().NextBytes(mockEnclave.MrEnclave);
            new Random().NextBytes(mockEnclave.MrSigner);

            // Add the enclave to the dictionary
            _enclaves[mockEnclave.Id] = mockEnclave;

            // Return the enclave ID
            enclaveId = mockEnclave.Id;
            return 0;
        }

        /// <summary>
        /// Terminates a mock enclave.
        /// </summary>
        /// <param name="enclaveId">The enclave ID.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        public static int oe_terminate_enclave(IntPtr enclaveId)
        {
            if (!_initialized)
            {
                return -1;
            }

            _logger.LogInformation("Terminating mock enclave: {Id}", enclaveId);

            // Remove the enclave from the dictionary
            if (_enclaves.ContainsKey(enclaveId))
            {
                _enclaves.Remove(enclaveId);
                return 0;
            }

            return -1;
        }

        /// <summary>
        /// Gets a report from a mock enclave.
        /// </summary>
        /// <param name="enclaveId">The enclave ID.</param>
        /// <param name="flags">The report flags.</param>
        /// <param name="reportData">The report data.</param>
        /// <param name="reportDataSize">The size of the report data.</param>
        /// <param name="report">The report.</param>
        /// <param name="reportSize">The size of the report.</param>
        /// <returns>0 if successful, non-zero otherwise.</returns>
        public static int oe_get_report(
            IntPtr enclaveId,
            int flags,
            IntPtr reportData,
            int reportDataSize,
            out IntPtr report,
            out int reportSize)
        {
            if (!_initialized)
            {
                report = IntPtr.Zero;
                reportSize = 0;
                return -1;
            }

            _logger.LogInformation("Getting report from mock enclave: {Id}, Flags: {Flags}", enclaveId, flags);

            // Check if the enclave exists
            if (!_enclaves.TryGetValue(enclaveId, out var mockEnclave))
            {
                report = IntPtr.Zero;
                reportSize = 0;
                return -1;
            }

            // Create a mock report
            var mockReport = new byte[256];
            new Random().NextBytes(mockReport);

            // Include the MRENCLAVE and MRSIGNER in the report
            Array.Copy(mockEnclave.MrEnclave, 0, mockReport, 32, 32);
            Array.Copy(mockEnclave.MrSigner, 0, mockReport, 64, 32);

            // Include the report data if provided
            if (reportData != IntPtr.Zero && reportDataSize > 0)
            {
                var reportDataBytes = new byte[reportDataSize];
                Marshal.Copy(reportData, reportDataBytes, 0, reportDataSize);
                Array.Copy(reportDataBytes, 0, mockReport, 96, Math.Min(reportDataSize, 64));
            }

            // Allocate memory for the report
            report = Marshal.AllocHGlobal(mockReport.Length);
            Marshal.Copy(mockReport, 0, report, mockReport.Length);
            reportSize = mockReport.Length;

            return 0;
        }

        /// <summary>
        /// Frees a report.
        /// </summary>
        /// <param name="report">The report to free.</param>
        public static void oe_free_report(IntPtr report)
        {
            if (!_initialized)
            {
                return;
            }

            _logger.LogInformation("Freeing mock report: {Report}", report);

            // Free the memory
            if (report != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(report);
            }
        }

        /// <summary>
        /// A mock enclave.
        /// </summary>
        private class MockEnclave
        {
            public IntPtr Id { get; set; }
            public string Path { get; set; }
            public int Type { get; set; }
            public int Flags { get; set; }
            public byte[] MrEnclave { get; set; }
            public byte[] MrSigner { get; set; }
            public int ProductId { get; set; }
            public int SecurityVersion { get; set; }
            public int Attributes { get; set; }
        }
    }
}
