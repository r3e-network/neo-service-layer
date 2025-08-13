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

namespace NeoServiceLayer.Services.KeyManagement
{
    /// <summary>
    /// Service registration for Key Management domain
    /// </summary>
    public static class ServiceRegistration
    {
        public static IServiceCollection AddKeyManagementServices(this IServiceCollection services)
        {
            // Infrastructure
            services.AddScoped<ICryptographicService, CryptographicService>();
            services.AddScoped<IKeyStore, InMemoryKeyStore>(); // TODO: Replace with persistent store
            services.AddScoped<IKeyReadModelStore, InMemoryKeyReadModelStore>(); // TODO: Replace with persistent store

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