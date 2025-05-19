using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Moq;
using NeoServiceLayer.TestHelpers;
using NeoServiceLayer.Tee.Host;
using Xunit;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    /// <summary>
    /// A test fixture for simulation mode tests that provides common setup and teardown functionality.
    /// </summary>
    /// <remarks>
    /// This fixture sets up the simulation environment for tests, including:
    /// - Setting the SGX_SIMULATION environment variable
    /// - Creating a mock enclave file
    /// - Setting up the necessary services and interfaces
    ///
    /// It also provides access to the mock interfaces and other resources needed for testing.
    /// </remarks>
    public class SimulationModeFixture : IDisposable
    {
        /// <summary>
        /// Gets or sets a value indicating whether to force using the mock implementation
        /// instead of attempting to use the real OpenEnclave SDK.
        /// </summary>
        public static bool ForceMockImplementation { get; set; } = false;

        private readonly ILogger _logger;
        public IConfiguration Configuration { get; }
        public IServiceProvider ServiceProvider { get; }
        public ILoggerFactory LoggerFactory { get; }
        public bool SimulationModeEnabled { get; }
        public string EnclavePath { get; }
        public string OcclumInstanceDir { get; }
        public MockEnclaveFixture MockEnclave { get; }
        public ITeeEnclaveInterface TeeInterface { get; }
        public ISgxEnclaveInterface SgxInterface { get; }
        public MockEnclaveFile MockEnclaveFile { get; }
        public IOpenEnclaveInterface OpenEnclaveInterface { get; }

        /// <summary>
        /// Gets a value indicating whether the real OpenEnclave SDK is being used.
        /// </summary>
        public bool UsingRealSdk { get; private set; }

        public SimulationModeFixture()
        {
            // Set up simulation mode
            Environment.SetEnvironmentVariable("SGX_SIMULATION", "1");
            Environment.SetEnvironmentVariable("OE_SIMULATION", "1");

            // Create mock enclave
            MockEnclave = new MockEnclaveFixture();
            EnclavePath = MockEnclave.MockEnclavePath;

            // Build configuration
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("simulation-test-config.json", optional: true)
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Enclave:Path", EnclavePath },
                    { "Enclave:OcclumInstanceDir", "/tmp/occlum_instance" },
                    { "Enclave:SimulationMode", "true" }
                })
                .AddEnvironmentVariables();

            Configuration = configBuilder.Build();

            // Get paths from configuration or environment variables
            OcclumInstanceDir = Environment.GetEnvironmentVariable("OCCLUM_INSTANCE_DIR")
                ?? Configuration["Tee:Occlum:InstanceDir"]
                ?? "../occlum_instance";

            SimulationModeEnabled = Environment.GetEnvironmentVariable("OE_SIMULATION") == "1"
                || (Configuration["Tee:Enclave:SimulationMode"] != null && bool.TryParse(Configuration["Tee:Enclave:SimulationMode"], out bool result) && result);

            // Set up services
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(Configuration.GetSection("Logging"));
                builder.AddConsole();
                // Debug logging is not available in .NET 9.0, so we use console logging instead
            });

            // Add configuration
            services.AddSingleton(Configuration);

            // Build service provider
            ServiceProvider = services.BuildServiceProvider();

            // Get logger factory
            LoggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();

            // Create logger for this class
            _logger = LoggerFactory.CreateLogger<SimulationModeFixture>();

            // Initialize the mock DLLs
            var mockDllLogger = LoggerFactory.CreateLogger("MockDlls");
            MockDllInitializer.Initialize(mockDllLogger);

            // Create the mock enclave file
            var logger = LoggerFactory.CreateLogger<MockEnclaveFile>();
            MockEnclaveFile = new MockEnclaveFile(logger);
            Task.Run(async () => await MockEnclaveFile.CreateAsync()).Wait();

            // Check if we should use the real OpenEnclave SDK
            bool useRealSdk = false;

            if (ForceMockImplementation)
            {
                _logger.LogInformation("Forcing use of mock implementation as requested");
            }
            else
            {
                // Check if the OpenEnclave SDK is available
                useRealSdk = OpenEnclaveAvailabilityChecker.IsAvailable(_logger);

                if (useRealSdk)
                {
                    // Create the real Open Enclave interface with simulation mode enabled
                    try
                    {
                        _logger.LogInformation("Attempting to create real OpenEnclaveInterface in simulation mode");

                        // Set the simulation mode environment variable
                        Environment.SetEnvironmentVariable("OE_SIMULATION", "1");

                        // Add the OpenEnclave SDK bin directory to the PATH if available
                        string sdkPath = OpenEnclaveAvailabilityChecker.GetSdkPath();
                        if (!string.IsNullOrEmpty(sdkPath))
                        {
                            string binPath = Path.Combine(sdkPath, "bin");
                            string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                            if (!currentPath.Contains(binPath))
                            {
                                Environment.SetEnvironmentVariable("PATH", $"{binPath};{currentPath}");
                                _logger.LogInformation("Added OpenEnclave SDK bin directory to PATH: {BinPath}", binPath);
                            }
                        }

                        var openEnclaveLogger = LoggerFactory.CreateLogger<OpenEnclaveInterface>();
                        OpenEnclaveInterface = new OpenEnclaveInterface(openEnclaveLogger, MockEnclaveFile.MockEnclavePath);
                        _logger.LogInformation("Successfully created real OpenEnclaveInterface in simulation mode");
                        UsingRealSdk = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create real OpenEnclaveInterface in simulation mode. Falling back to mock implementation.");
                        useRealSdk = false;
                    }
                }
                else
                {
                    _logger.LogWarning("OpenEnclave SDK not available: {ErrorMessage}. Using mock implementation.",
                        OpenEnclaveAvailabilityChecker.GetErrorMessage());
                }
            }

            if (!useRealSdk)
            {
                var openEnclaveLogger = LoggerFactory.CreateLogger<MockOpenEnclaveInterface>();
                OpenEnclaveInterface = new MockOpenEnclaveInterface(openEnclaveLogger);
                _logger.LogInformation("Created mock OpenEnclaveInterface");
                UsingRealSdk = false;
            }

            // Create and register the mock interfaces
            var mockSgxInterface = new MockSgxEnclaveInterface(
                LoggerFactory.CreateLogger<MockSgxEnclaveInterface>());

            // Register the interfaces
            TeeInterface = mockSgxInterface;
            SgxInterface = mockSgxInterface;
        }

        /// <summary>
        /// Disposes the resources used by the fixture.
        /// </summary>
        public void Dispose()
        {
            // Clean up resources
            MockEnclave.Dispose();
            MockEnclaveFile.Dispose();
            (OpenEnclaveInterface as IDisposable)?.Dispose();
            (ServiceProvider as IDisposable)?.Dispose();
            (LoggerFactory as IDisposable)?.Dispose();
        }

        /// <summary>
        /// Corrupts the storage data for a key to test error recovery.
        /// </summary>
        /// <param name="key">The key of the data to corrupt.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CorruptStorageDataAsync(string key)
        {
            if (UsingRealSdk)
            {
                // Get the current data
                byte[] data = await TeeInterface.RetrievePersistentDataAsync(key);

                // Corrupt the data by replacing it with invalid data
                byte[] corruptedData = new byte[data.Length];
                new Random().NextBytes(corruptedData);

                // Store the corrupted data directly, bypassing integrity checks
                await TeeInterface.CorruptStorageDataInternalAsync(key, corruptedData);

                _logger.LogInformation("Corrupted storage data for key: {Key}", key);
            }
            else
            {
                _logger.LogWarning("Cannot corrupt storage data in mock mode");
            }
        }

        /// <summary>
        /// Tampers with the storage data for a key to test integrity verification.
        /// </summary>
        /// <param name="key">The key of the data to tamper with.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task TamperWithStorageDataAsync(string key)
        {
            if (UsingRealSdk)
            {
                // Get the current data
                byte[] data = await TeeInterface.RetrievePersistentDataAsync(key);

                // Tamper with the data by modifying a few bytes
                if (data.Length > 10)
                {
                    data[5] ^= 0xFF;
                    data[6] ^= 0xFF;
                    data[7] ^= 0xFF;
                }

                // Store the tampered data directly, bypassing integrity checks
                await TeeInterface.TamperWithStorageDataInternalAsync(key, data);

                _logger.LogInformation("Tampered with storage data for key: {Key}", key);
            }
            else
            {
                _logger.LogWarning("Cannot tamper with storage data in mock mode");
            }
        }

        /// <summary>
        /// Simulates an enclave restart to test state recovery.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SimulateEnclaveRestartAsync()
        {
            if (UsingRealSdk)
            {
                // Simulate enclave restart by calling the internal method
                await TeeInterface.SimulateEnclaveRestartInternalAsync();

                _logger.LogInformation("Simulated enclave restart");
            }
            else
            {
                _logger.LogWarning("Cannot simulate enclave restart in mock mode");
            }
        }
    }

    /// <summary>
    /// Collection definition for simulation mode tests.
    /// </summary>
    /// <remarks>
    /// This class defines a test collection for simulation mode tests. It allows multiple test classes
    /// to share a single instance of the SimulationModeFixture, which improves test performance by
    /// avoiding the overhead of creating a new fixture for each test class.
    ///
    /// To use this collection, add the [Collection("SimulationMode")] attribute to your test class:
    ///
    /// [Collection("SimulationMode")]
    /// public class MyTests
    /// {
    ///     private readonly SimulationModeFixture _fixture;
    ///
    ///     public MyTests(SimulationModeFixture fixture)
    ///     {
    ///         _fixture = fixture;
    ///     }
    ///
    ///     [Fact]
    ///     public void MyTest()
    ///     {
    ///         // Use _fixture.TeeInterface, _fixture.SgxInterface, etc.
    ///     }
    /// }
    /// </remarks>
    [CollectionDefinition("SimulationMode")]
    public class SimulationModeCollection : ICollectionFixture<SimulationModeFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
