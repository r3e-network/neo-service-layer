using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Core.Health
{
    /// <summary>
    /// Health check for database connectivity
    /// </summary>
    public class ProfessionalDatabaseHealthCheck : IProfessionalHealthCheck
    {
        private readonly DbContext _context;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        /// <summary>
        /// Initializes a new instance of DatabaseHealthCheck
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="logger">The logger</param>
        public DatabaseHealthCheck(DbContext context, ILogger<DatabaseHealthCheck> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public string Name => "Database";

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Test database connectivity with a simple query
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
                
                if (!canConnect)
                {
                    stopwatch.Stop();
                    return HealthCheckResult.Unhealthy(
                        "Cannot connect to database",
                        duration: stopwatch.Elapsed);
                }

                // Test with a simple query
                await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
                
                stopwatch.Stop();
                var duration = stopwatch.Elapsed;
                
                // Consider degraded if response time is high
                if (duration.TotalSeconds > 5)
                {
                    _logger.LogWarning("Database health check took {Duration}ms - performance may be degraded", 
                        duration.TotalMilliseconds);
                    
                    return HealthCheckResult.Degraded(
                        $"Database responding slowly ({duration.TotalMilliseconds:F0}ms)",
                        data: new { ResponseTime = duration },
                        duration: duration);
                }

                return HealthCheckResult.Healthy(
                    $"Database is healthy ({duration.TotalMilliseconds:F0}ms)",
                    data: new { ResponseTime = duration },
                    duration: duration);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Database health check failed");
                
                return HealthCheckResult.Unhealthy(
                    "Database health check failed",
                    ex,
                    duration: stopwatch.Elapsed);
            }
        }
    }
}