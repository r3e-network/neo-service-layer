using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Infrastructure.Persistence.Migrations;

public interface IDatabaseMigrationService
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
    Task<bool> IsDatabaseReadyAsync(CancellationToken cancellationToken = default);
    Task SeedDataAsync(CancellationToken cancellationToken = default);
}

public class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseMigrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _logger.LogInformation("Starting database migration");

            // Get pending migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);

            if (pendingMigrations.Any())
            {
                _logger.LogInformation("Found {Count} pending migrations", pendingMigrations.Count());

                // Apply migrations
                await context.Database.MigrateAsync(cancellationToken);

                _logger.LogInformation("Database migration completed successfully");
            }
            else
            {
                _logger.LogInformation("No pending migrations found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }
    }

    public async Task<bool> IsDatabaseReadyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Try to open connection
            return await context.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database is not ready");
            return false;
        }
    }

    public async Task SeedDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _logger.LogInformation("Starting database seeding");

            // Add seed data logic here
            await SeedDefaultDataAsync(context, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task SeedDefaultDataAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        // Seed default service configurations
        if (!await context.ServiceConfigurations.AnyAsync(cancellationToken))
        {
            context.ServiceConfigurations.AddRange(
                new ServiceConfigurationEntity { ServiceName = "StorageService", IsEnabled = true, CreatedAt = DateTime.UtcNow },
                new ServiceConfigurationEntity { ServiceName = "KeyManagementService", IsEnabled = true, CreatedAt = DateTime.UtcNow },
                new ServiceConfigurationEntity { ServiceName = "NotificationService", IsEnabled = true, CreatedAt = DateTime.UtcNow },
                new ServiceConfigurationEntity { ServiceName = "OracleService", IsEnabled = true, CreatedAt = DateTime.UtcNow },
                new ServiceConfigurationEntity { ServiceName = "ComputeService", IsEnabled = true, CreatedAt = DateTime.UtcNow }
            );
        }

        // Seed default API keys (disabled by default)
        if (!await context.ApiKeys.AnyAsync(cancellationToken))
        {
            context.ApiKeys.Add(new ApiKey
            {
                Id = Guid.NewGuid(),
                Name = "Default API Key",
                Key = GenerateApiKey(),
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(1)
            });
        }
    }

    private string GenerateApiKey()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

// Hosted service to run migrations on startup
public class DatabaseMigrationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationHostedService> _logger;

    public DatabaseMigrationHostedService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseMigrationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();

            // Wait for database to be ready
            var retries = 0;
            while (!await migrationService.IsDatabaseReadyAsync(cancellationToken))
            {
                if (retries++ > 30) // 30 seconds timeout
                {
                    throw new Exception("Database is not ready after 30 seconds");
                }

                _logger.LogInformation("Waiting for database to be ready... (attempt {Attempt})", retries);
                await Task.Delay(1000, cancellationToken);
            }

            // Run migrations
            await migrationService.MigrateAsync(cancellationToken);

            // Seed data
            await migrationService.SeedDataAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate database");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
