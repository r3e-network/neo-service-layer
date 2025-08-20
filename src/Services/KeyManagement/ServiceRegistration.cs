using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Infrastructure.CQRS.Repositories;
using NeoServiceLayer.Services.KeyManagement.CommandHandlers;
using NeoServiceLayer.Services.KeyManagement.Commands;
using NeoServiceLayer.Services.KeyManagement.Domain;
using NeoServiceLayer.Services.KeyManagement.Infrastructure;
using NeoServiceLayer.Services.KeyManagement.Projections;
using NeoServiceLayer.Services.KeyManagement.Queries;
using NeoServiceLayer.Services.KeyManagement.QueryHandlers;
using NeoServiceLayer.Services.KeyManagement.ReadModels;
using NeoServiceLayer.ServiceFramework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.KeyManagement
{
    /// <summary>
    /// Service registration for Key Management domain
    /// </summary>
    public static class ServiceRegistration
    {
        public static IServiceCollection AddKeyManagementServices(this IServiceCollection services)
        {
            // Infrastructure - Key storage with configurable persistence
            services.AddScoped<ICryptographicService, CryptographicService>();

            // Register persistent storage by default with in-memory fallback for development
            services.AddScoped<IKeyStore>(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                var storageType = configuration.GetValue<string>("KeyManagement:StorageType", "InMemory");

                return storageType.ToLowerInvariant() switch
                {
                    "database" => new DatabaseKeyStore(serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DatabaseKeyStore>>(),
                                                     serviceProvider.GetRequiredService<Microsoft.EntityFrameworkCore.DbContext>()),
                    "redis" => new RedisKeyStore(serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RedisKeyStore>>(),
                                               serviceProvider.GetRequiredService<StackExchange.Redis.IDatabase>()),
                    _ => new InMemoryKeyStore() // Development/testing fallback
                };
            });

            services.AddScoped<IKeyReadModelStore>(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                var storageType = configuration.GetValue<string>("KeyManagement:StorageType", "InMemory");

                return storageType.ToLowerInvariant() switch
                {
                    "database" => new DatabaseKeyReadModelStore(serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DatabaseKeyReadModelStore>>(),
                                                              serviceProvider.GetRequiredService<Microsoft.EntityFrameworkCore.DbContext>()),
                    "redis" => new RedisKeyReadModelStore(serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RedisKeyReadModelStore>>(),
                                                        serviceProvider.GetRequiredService<StackExchange.Redis.IDatabase>()),
                    _ => new InMemoryKeyReadModelStore() // Development/testing fallback
                };
            });

            // Repository
            services.AddScoped<IAggregateRepository<CryptographicKey>, EventSourcedAggregateRepository<CryptographicKey>>();

            // Command Handlers
            services.AddScoped<ICommandHandler<GenerateKeyCommand, string>, GenerateKeyCommandHandler>();
            services.AddScoped<ICommandHandler<ActivateKeyCommand>, ActivateKeyCommandHandler>();
            services.AddScoped<ICommandHandler<RevokeKeyCommand>, RevokeKeyCommandHandler>();
            services.AddScoped<ICommandHandler<RotateKeyCommand, string>, RotateKeyCommandHandler>();
            services.AddScoped<ICommandHandler<SignDataCommand, string>, SignDataCommandHandler>();
            services.AddScoped<ICommandHandler<VerifySignatureCommand, bool>, VerifySignatureCommandHandler>();
            services.AddScoped<ICommandHandler<GrantKeyAccessCommand>, GrantKeyAccessCommandHandler>();
            services.AddScoped<ICommandHandler<RevokeKeyAccessCommand>, RevokeKeyAccessCommandHandler>();

            // Query Handlers
            services.AddScoped<IQueryHandler<GetKeyByIdQuery, KeyReadModel?>, GetKeyByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetKeysByTypeQuery, IEnumerable<KeyReadModel>>, GetKeysByTypeQueryHandler>();
            services.AddScoped<IQueryHandler<GetActiveKeysQuery, IEnumerable<KeyReadModel>>, GetActiveKeysQueryHandler>();
            services.AddScoped<IQueryHandler<GetKeysExpiringBeforeQuery, IEnumerable<KeyReadModel>>, GetKeysExpiringBeforeQueryHandler>();
            services.AddScoped<IQueryHandler<GetKeysByUserQuery, IEnumerable<KeyReadModel>>, GetKeysByUserQueryHandler>();
            services.AddScoped<IQueryHandler<GetKeyUsageStatisticsQuery, KeyUsageStatistics>, GetKeyUsageStatisticsQueryHandler>();
            services.AddScoped<IQueryHandler<SearchKeysQuery, IEnumerable<KeyReadModel>>, SearchKeysQueryHandler>();

            // Projection
            services.AddScoped<KeyManagementProjection>();

            return services;
        }
    }
}