using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host.EnclaveLifecycle
{
    /// <summary>
    /// Interface for creating enclave interfaces.
    /// </summary>
    public interface IEnclaveInterfaceFactory
    {
        /// <summary>
        /// Creates an Occlum interface for the specified enclave.
        /// </summary>
        /// <param name="enclavePath">The path to the enclave file.</param>
        /// <param name="simulationMode">Whether to run in simulation mode.</param>
        /// <returns>The created enclave interface.</returns>
        ITeeInterface CreateOcclumInterface(string enclavePath, bool simulationMode);
    }

    /// <summary>
    /// Default implementation of the enclave interface factory.
    /// </summary>
    public class DefaultEnclaveInterfaceFactory : IEnclaveInterfaceFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultEnclaveInterfaceFactory"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory to use for creating loggers.</param>
        public DefaultEnclaveInterfaceFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Creates an Occlum interface for the specified enclave.
        /// </summary>
        /// <param name="enclavePath">The path to the enclave file.</param>
        /// <param name="simulationMode">Whether to run in simulation mode.</param>
        /// <returns>The created enclave interface.</returns>
        public ITeeInterface CreateOcclumInterface(string enclavePath, bool simulationMode)
        {
            if (string.IsNullOrEmpty(enclavePath))
            {
                throw new ArgumentException("Enclave path cannot be null or empty", nameof(enclavePath));
            }

            // Set simulation mode if requested
            if (simulationMode)
            {
                Environment.SetEnvironmentVariable("OCCLUM_SIMULATION", "1");
            }
            else
            {
                Environment.SetEnvironmentVariable("OCCLUM_SIMULATION", "0");
            }

            var logger = _loggerFactory.CreateLogger<OcclumInterface>();
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole())
                .BuildServiceProvider();
            return new OcclumInterface(logger, enclavePath, serviceProvider);
        }
    }
}
