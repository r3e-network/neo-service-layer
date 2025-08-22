using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Services;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Extensions
{
    public static class ServiceExtensions
    {
        /// <summary>
        /// Adds PostgreSQL persistence services to the DI container
        /// </summary>
        public static IServiceCollection AddPostgreSQLPersistence(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add DbContext
            services.AddDbContext<NeoServiceLayerDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("PostgreSQL") 
                    ?? throw new InvalidOperationException("PostgreSQL connection string not configured");
                    
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(NeoServiceLayerDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                    npgsqlOptions.CommandTimeout(30);
                });

                // Enable detailed errors in development
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

            // Add repositories
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IHealthCheckRepository, HealthCheckRepository>();
            services.AddScoped<ISmartContractRepository, SmartContractRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IVotingRepository, VotingRepository>();
            services.AddScoped<IZeroKnowledgeRepository, ZeroKnowledgeRepository>();
            services.AddScoped<ISecretsManagementRepository, SecretsManagementRepository>();
            services.AddScoped<IProofOfReserveRepository, ProofOfReserveRepository>();
            services.AddScoped<IOracleRepository, OracleRepository>();
            services.AddScoped<IMonitoringRepository, MonitoringRepository>();
            services.AddScoped<ICrossChainRepository, CrossChainRepository>();
            services.AddScoped<IRandomnessRepository, RandomnessRepository>();
            services.AddScoped<IEnclaveStorageRepository, EnclaveStorageRepository>();
            services.AddScoped<INetworkSecurityRepository, NetworkSecurityRepository>();

            // Add SGX services
            services.AddScoped<ISealedDataRepository, PostgreSQLSealedDataRepository>();
            services.AddScoped<ISgxConfidentialStore, SgxConfidentialStore>();
            services.AddHostedService<SgxCleanupService>();

            // Add unit of work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Add migration service
            services.AddHostedService<DatabaseMigrationService>();

            return services;
        }

        /// <summary>
        /// Adds PostgreSQL health checks
        /// </summary>
        public static IServiceCollection AddPostgreSQLHealthChecks(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("PostgreSQL");
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddHealthChecks()
                    .AddNpgSql(
                        connectionString,
                        name: "postgresql",
                        tags: new[] { "db", "postgresql", "persistence" });
            }

            return services;
        }

        /// <summary>
        /// Ensures database is created and migrations are applied
        /// </summary>
        public static void EnsurePostgreSQLDatabaseCreated(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<NeoServiceLayerDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<NeoServiceLayerDbContext>>();

            try
            {
                // Create database if it doesn't exist
                context.Database.EnsureCreated();
                
                // Apply any pending migrations
                if (context.Database.GetPendingMigrations().Any())
                {
                    logger.LogInformation("Applying pending PostgreSQL migrations...");
                    context.Database.Migrate();
                    logger.LogInformation("PostgreSQL migrations applied successfully");
                }
                else
                {
                    logger.LogInformation("PostgreSQL database is up to date");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to ensure PostgreSQL database is created");
                throw;
            }
        }
    }
}