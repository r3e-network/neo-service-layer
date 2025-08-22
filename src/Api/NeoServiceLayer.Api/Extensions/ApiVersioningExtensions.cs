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

            // API Explorer configuration for version discovery
            try 
            {
                // Attempt to add versioned API explorer if package is available
                services.AddVersionedApiExplorer(options =>
                {
                    // Format: 'v'major[.minor][-status]  
                    options.GroupNameFormat = "'v'VVV";
                    
                    // Substitute version in URL
                    options.SubstituteApiVersionInUrl = true;
                });
            }
            catch (Exception)
            {
                // Graceful fallback if versioned API explorer package not available
                // Basic API versioning will still work without explorer features
            }
            // });

            return services;
        }
    }
}
