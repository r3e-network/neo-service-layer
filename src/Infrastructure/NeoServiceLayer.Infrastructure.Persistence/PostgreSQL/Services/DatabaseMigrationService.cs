using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Contexts;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Services
{
    /// <summary>
    /// Background service for handling database migrations on startup
    /// </summary>
    public class DatabaseMigrationService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseMigrationService> _logger;

        public DatabaseMigrationService(
            IServiceProvider serviceProvider,
            ILogger<DatabaseMigrationService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Database Migration Service starting...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<NeoServiceDbContext>();

            try
            {
                // Check database connection
                if (!await context.Database.CanConnectAsync(cancellationToken))
                {
                    _logger.LogWarning("Cannot connect to PostgreSQL database. Waiting for database to be available...");
                    
                    // Retry connection with exponential backoff
                    var retryCount = 0;
                    var maxRetries = 10;
                    var delay = TimeSpan.FromSeconds(2);

                    while (retryCount < maxRetries && !cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(delay, cancellationToken);
                        
                        if (await context.Database.CanConnectAsync(cancellationToken))
                        {
                            _logger.LogInformation("Successfully connected to PostgreSQL database");
                            break;
                        }

                        retryCount++;
                        delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60));
                        _logger.LogWarning("Database connection attempt {Attempt}/{MaxAttempts} failed. Retrying in {Delay} seconds...",
                            retryCount, maxRetries, delay.TotalSeconds);
                    }

                    if (retryCount >= maxRetries)
                    {
                        throw new InvalidOperationException($"Failed to connect to PostgreSQL database after {maxRetries} attempts");
                    }
                }

                // Apply pending migrations
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
                
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("Found {Count} pending migrations. Applying...", pendingMigrations.Count());
                    
                    foreach (var migration in pendingMigrations)
                    {
                        _logger.LogInformation("Applying migration: {Migration}", migration);
                    }

                    await context.Database.MigrateAsync(cancellationToken);
                    _logger.LogInformation("All migrations applied successfully");
                }
                else
                {
                    _logger.LogInformation("Database is up to date. No migrations to apply");
                }

                // Seed initial data if needed
                await SeedInitialDataAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database migration");
                throw;
            }

            _logger.LogInformation("Database Migration Service started successfully");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Database Migration Service stopping...");
            return Task.CompletedTask;
        }

        private async Task SeedInitialDataAsync(NeoServiceDbContext context, CancellationToken cancellationToken)
        {
            try
            {
                // Seed default sealing policies if they don't exist
                if (!await context.SealingPolicies.AnyAsync(cancellationToken))
                {
                    _logger.LogInformation("Seeding default sealing policies...");

                    var policies = new[]
                    {
                        new SealingPolicy
                        {
                            Name = "Default_MRSIGNER",
                            PolicyType = "MRSIGNER",
                            Description = "Default MRSIGNER policy - binds to enclave signer",
                            ExpirationHours = 24,
                            AllowUnseal = true,
                            RequireAttestation = false,
                            PolicyRules = "{\"policyType\":\"MRSIGNER\",\"expirationHours\":24,\"requireAttestation\":false}"
                        },
                        new SealingPolicy
                        {
                            Name = "Default_MRENCLAVE",
                            PolicyType = "MRENCLAVE",
                            Description = "Default MRENCLAVE policy - binds to specific enclave measurement",
                            ExpirationHours = 12,
                            AllowUnseal = true,
                            RequireAttestation = true,
                            PolicyRules = "{\"policyType\":\"MRENCLAVE\",\"expirationHours\":12,\"requireAttestation\":true}"
                        },
                        new SealingPolicy
                        {
                            Name = "HighSecurity",
                            PolicyType = "CUSTOM",
                            Description = "High security policy with strict attestation requirements",
                            ExpirationHours = 6,
                            AllowUnseal = true,
                            RequireAttestation = true,
                            PolicyRules = "{\"policyType\":\"CUSTOM\",\"expirationHours\":6,\"requireAttestation\":true,\"minISVSvn\":1}"
                        }
                    };

                    await context.SealingPolicies.AddRangeAsync(policies, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogInformation("Seeded {Count} default sealing policies", policies.Length);
                }

                // Add more seed data as needed for other entities
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding initial data");
                // Don't throw - seeding is not critical for startup
            }
        }
    }
}