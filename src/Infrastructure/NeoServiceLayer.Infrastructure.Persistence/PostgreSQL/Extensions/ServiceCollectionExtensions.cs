using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using NeoServiceLayer.Core.Persistence;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Extensions;

/// <summary>
/// Extension methods for configuring PostgreSQL persistence services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PostgreSQL database services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="environment">Hosting environment</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPostgreSQLPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Get connection string
        var connectionString = GetConnectionString(configuration, environment);
        
        // Add DbContext
        services.AddDbContext<NeoServiceLayerDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Enable retry on failure for production reliability
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                
                // Set command timeout
                npgsqlOptions.CommandTimeout(30);
                
                // Use UTC timestamps
                npgsqlOptions.UseNodaTime();
            });
            
            // Enable sensitive data logging in development
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
            
            // Configure logging
            options.UseLoggerFactory(LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                if (environment.IsDevelopment())
                {
                    builder.SetMinimumLevel(LogLevel.Information);
                }
            }));
        });
        
        // Add connection pool configuration
        services.Configure<NpgsqlConnectionStringBuilder>(options =>
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            
            // Connection pooling settings
            options.Pooling = true;
            options.MinPoolSize = 5;
            options.MaxPoolSize = environment.IsDevelopment() ? 20 : 100;
            options.ConnectionLifetime = 300; // 5 minutes
            options.ConnectionIdleLifetime = 60; // 1 minute
            
            // Performance settings
            options.MaxAutoPrepare = 20;
            options.AutoPrepareMinUsages = 2;
            
            // Security settings
            options.TrustServerCertificate = false;
            options.SslMode = SslMode.Require;
            
            // Copy other settings
            options.Host = builder.Host;
            options.Database = builder.Database;
            options.Username = builder.Username;
            options.Password = builder.Password;
            options.Port = builder.Port;
        });
        
        // Register repositories and unit of work
        services.AddScoped<IUnitOfWork, PostgreSQLUnitOfWork>();
        services.AddScoped<IUserRepository, PostgreSQLUserRepository>();
        services.AddScoped<ISealedDataRepository, PostgreSQLSealedDataRepository>();
        services.AddScoped<IOracleRepository, PostgreSQLOracleRepository>();
        services.AddScoped<IVotingRepository, PostgreSQLVotingRepository>();
        services.AddScoped<IAuditRepository, PostgreSQLAuditRepository>();
        services.AddScoped<IEventStore, PostgreSQLEventStore>();
        
        // Register health checks
        services.AddHealthChecks()
            .AddNpgSql(connectionString, 
                name: "postgresql", 
                tags: new[] { "database", "postgresql" });
        
        return services;
    }
    
    /// <summary>
    /// Adds PostgreSQL database services for SGX confidential computing
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="environment">Hosting environment</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSGXPostgreSQLPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Add base PostgreSQL services
        services.AddPostgreSQLPersistence(configuration, environment);
        
        // Register SGX-specific repositories with encryption
        services.AddScoped<ISGXSealedDataRepository, SGXPostgreSQLSealedDataRepository>();
        services.AddScoped<IEnclaveAttestationRepository, PostgreSQLEnclaveAttestationRepository>();
        
        // Add SGX-specific configuration
        services.Configure<SGXDatabaseOptions>(configuration.GetSection("SGX:Database"));
        
        return services;
    }
    
    /// <summary>
    /// Gets the appropriate connection string based on environment and configuration
    /// </summary>
    private static string GetConnectionString(IConfiguration configuration, IHostEnvironment environment)
    {
        // Try environment-specific connection string first
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            // Fallback to environment variables
            var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
            var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
            var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "neo_service_layer";
            var username = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
            var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
            
            connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";
        }
        
        // Add SSL mode for production
        if (environment.IsProduction())
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            if (builder.SslMode == SslMode.Disable)
            {
                builder.SslMode = SslMode.Require;
                connectionString = builder.ConnectionString;
            }
        }
        
        return connectionString;
    }
}

/// <summary>
/// Configuration options for SGX database integration
/// </summary>
public class SGXDatabaseOptions
{
    public bool EnableEncryption { get; set; } = true;
    public bool EnableIntegrityChecking { get; set; } = true;
    public string EncryptionKey { get; set; } = string.Empty;
    public int DataRetentionDays { get; set; } = 90;
}