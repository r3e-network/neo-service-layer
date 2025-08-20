using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


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

            // TODO: Add Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer package
            // services.AddVersionedApiExplorer(options =>
            // {
            //     // Format: 'v'major[.minor][-status]
            //     options.GroupNameFormat = "'v'VVV";

            //     // Substitute version in URL
            //     options.SubstituteApiVersionInUrl = true;
            // });

            return services;
        }
    }
}
