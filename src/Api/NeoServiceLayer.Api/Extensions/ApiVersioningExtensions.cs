using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace NeoServiceLayer.Api.Extensions
{
    /// <summary>
    /// Extension methods for configuring API versioning.
    /// </summary>
    public static class ApiVersioningExtensions
    {
        public static IServiceCollection AddApiVersioningSupport(this IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                // Report API versions in response headers
                options.ReportApiVersions = true;

                // Use default version when not specified
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);

                // Support multiple versioning schemes
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("X-Api-Version"),
                    new MediaTypeApiVersionReader("version")
                );
            });

            services.AddVersionedApiExplorer(options =>
            {
                // Format: 'v'major[.minor][-status]
                options.GroupNameFormat = "'v'VVV";

                // Substitute version in URL
                options.SubstituteApiVersionInUrl = true;
            });

            return services;
        }
    }
}
