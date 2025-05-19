using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Integration.Tests.Mocks
{
    /// <summary>
    /// A mock health check service that always returns healthy for testing.
    /// </summary>
    public class MockHealthCheck : IHealthCheck
    {
        private readonly ILogger<MockHealthCheck> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockHealthCheck"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public MockHealthCheck(ILogger<MockHealthCheck> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Checks the health of the service.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The health check result.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Performing mock health check - always returns healthy");

            var data = new Dictionary<string, object>
            {
                { "BlockchainHeight", 12345 },
                { "BlockchainConnection", "Connected" },
                { "TeeStatus", "Running" },
                { "TeeEnclaveId", "mock-enclave-id" },
                { "MemoryUsage", 1024 * 1024 * 100 }, // 100 MB
                { "CpuUsage", 5.0 }, // 5%
                { "Timestamp", DateTime.UtcNow }
            };

            // Always return healthy status for testing
            return Task.FromResult(HealthCheckResult.Healthy("Neo Service Layer is healthy", data));
        }
    }
}
