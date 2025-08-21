using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Api.GraphQL.Mutations;
using NeoServiceLayer.Api.GraphQL.Queries;
using NeoServiceLayer.Api.GraphQL.Subscriptions;
using NeoServiceLayer.Api.GraphQL.Types;

namespace NeoServiceLayer.Api.GraphQL;

/// <summary>
/// GraphQL service configuration extension methods.
/// </summary>
public static class GraphQLServiceConfiguration
{
    /// <summary>
    /// Adds GraphQL services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddGraphQLServices(this IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            // Query Types
            .AddQueryType<Query>()
            .AddTypeExtension<KeyManagementQueries>()
            .AddTypeExtension<AuthenticationQueries>()
            .AddTypeExtension<OracleQueries>()
            .AddTypeExtension<PatternRecognitionQueries>()
            .AddTypeExtension<PredictionQueries>()
            .AddTypeExtension<ProofOfReserveQueries>()
            .AddTypeExtension<VotingQueries>()
            .AddTypeExtension<StorageQueries>()
            .AddTypeExtension<ConfigurationQueries>()
            .AddTypeExtension<HealthQueries>()
            
            // Mutation Types
            .AddMutationType<Mutation>()
            .AddTypeExtension<KeyManagementMutations>()
            .AddTypeExtension<AuthenticationMutations>()
            .AddTypeExtension<OracleMutations>()
            .AddTypeExtension<PatternRecognitionMutations>()
            .AddTypeExtension<PredictionMutations>()
            .AddTypeExtension<ProofOfReserveMutations>()
            .AddTypeExtension<VotingMutations>()
            .AddTypeExtension<StorageMutations>()
            .AddTypeExtension<ConfigurationMutations>()
            
            // Subscription Types
            .AddSubscriptionType<Subscription>()
            .AddTypeExtension<KeyManagementSubscriptions>()
            .AddTypeExtension<OracleSubscriptions>()
            .AddTypeExtension<VotingSubscriptions>()
            .AddTypeExtension<MonitoringSubscriptions>()
            
            // Custom Types
            .AddType<KeyMetadataType>()
            .AddType<UserType>()
            .AddType<ServiceHealthType>()
            .AddType<PredictionResultType>()
            .AddType<PatternResultType>()
            .AddType<ProofOfReserveType>()
            .AddType<VotingProposalType>()
            .AddType<StorageMetadataType>()
            
            // Authorization
            .AddAuthorization()
            
            // Filtering, Sorting, and Projection
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            
            // Error Handling
            .AddErrorFilter<GraphQLErrorFilter>()
            
            // Field Middleware
            .UseField<AuthorizationMiddleware>()
            
            // Performance Optimizations
            .AddDataLoader()
            .AddInMemorySubscriptions()
            .EnableRelaySupport()
            
            // Schema Configuration
            .ModifyRequestOptions(opt => 
            {
                opt.IncludeExceptionDetails = false;
                opt.ExecutionTimeout = TimeSpan.FromSeconds(30);
            })
            
            // Introspection (disable in production)
            .ModifyOptions(opt =>
            {
                opt.EnableIntrospection = true; // Should be based on environment
            });

        return services;
    }
}