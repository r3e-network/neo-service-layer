using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace NeoServiceLayer.Integration.Tests.Microservices
{
    /// <summary>
    /// Test configuration helper to determine if infrastructure is available
    /// </summary>
    public static class TestConfiguration
    {
        private static bool? _infrastructureAvailable;
        private static readonly object _lock = new object();

        /// <summary>
        /// Check if test infrastructure is available
        /// </summary>
        public static bool IsInfrastructureAvailable()
        {
            lock (_lock)
            {
                if (_infrastructureAvailable.HasValue)
                    return _infrastructureAvailable.Value;

                _infrastructureAvailable = CheckInfrastructure().GetAwaiter().GetResult();
                return _infrastructureAvailable.Value;
            }
        }

        private static async Task<bool> CheckInfrastructure()
        {
            try
            {
                // Check if API Gateway is running
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var response = await client.GetAsync("http://localhost:7000/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                // Also check if Consul is running
                try
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                    var response = await client.GetAsync("http://localhost:8500/v1/agent/self");
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Skip reason for tests that require infrastructure
        /// </summary>
        public static string SkipReason => IsInfrastructureAvailable()
            ? null
            : "Requires running microservices infrastructure (run docker-compose up first)";
    }

    /// <summary>
    /// Conditional fact attribute that checks for infrastructure
    /// </summary>
    public class InfrastructureFactAttribute : Xunit.FactAttribute
    {
        public InfrastructureFactAttribute()
        {
            if (!TestConfiguration.IsInfrastructureAvailable())
            {
                Skip = TestConfiguration.SkipReason;
            }
        }
    }

    /// <summary>
    /// Conditional theory attribute that checks for infrastructure
    /// </summary>
    public class InfrastructureTheoryAttribute : Xunit.TheoryAttribute
    {
        public InfrastructureTheoryAttribute()
        {
            if (!TestConfiguration.IsInfrastructureAvailable())
            {
                Skip = TestConfiguration.SkipReason;
            }
        }
    }
}
