using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Api.Configuration
{
    /// <summary>
    /// Swagger/OpenAPI configuration
    /// </summary>
    public static class SwaggerConfiguration
    {
        /// <summary>
        /// Add Swagger services to DI container
        /// </summary>
        public static IServiceCollection AddSwaggerDocumentation(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(options =>
            {
                // API Information
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Neo Service Layer Authentication API",
                    Version = "v1",
                    Description = "Comprehensive authentication and authorization API with JWT tokens, 2FA, and advanced security features",
                    Contact = new OpenApiContact
                    {
                        Name = "Neo Service Layer Team",
                        Email = "support@neoservicelayer.com",
                        Url = new Uri("https://github.com/neoservicelayer")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    },
                    TermsOfService = new Uri("https://neoservicelayer.com/terms")
                });

                // JWT Authentication
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // API Key Authentication (optional)
                options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Name = "X-API-Key",
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Description = "API Key authentication. Enter your API key in the text input below."
                });

                // Include XML comments
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }

                // Custom operation filters
                options.OperationFilter<SwaggerDefaultValues>();
                options.OperationFilter<AuthorizeCheckOperationFilter>();
                options.OperationFilter<RateLimitHeadersOperationFilter>();

                // Custom schema filters
                options.SchemaFilter<EnumSchemaFilter>();

                // Group by tags
                options.TagActionsBy(api => new[] { api.GroupName ?? "Other" });
                options.DocInclusionPredicate((name, api) => true);

                // Custom naming
                options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
            });

            // Configure Swagger options
            services.ConfigureSwaggerGen(options =>
            {
                options.CustomOperationIds(apiDesc =>
                {
                    return apiDesc.TryGetMethodInfo(out MethodInfo methodInfo)
                        ? methodInfo.Name
                        : null;
                });
            });

            return services;
        }

        /// <summary>
        /// Configure Swagger middleware
        /// </summary>
        public static IApplicationBuilder UseSwaggerDocumentation(
            this IApplicationBuilder app,
            IConfiguration configuration)
        {
            var swaggerEnabled = configuration.GetValue<bool>("Swagger:Enabled", true);

            if (!swaggerEnabled)
            {
                return app;
            }

            app.UseSwagger(options =>
            {
                options.RouteTemplate = "swagger/{documentName}/swagger.json";
                options.PreSerializeFilters.Add((swagger, httpReq) =>
                {
                    swagger.Servers = new List<OpenApiServer>
                    {
                        new OpenApiServer
                        {
                            Url = $"{httpReq.Scheme}://{httpReq.Host.Value}",
                            Description = "Current Server"
                        }
                    };
                });
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo Service Layer Auth API v1");
                options.RoutePrefix = "api-docs";

                // UI Customization
                options.DocumentTitle = "Neo Service Layer API Documentation";
                options.InjectStylesheet("/swagger-ui/custom.css");
                options.InjectJavascript("/swagger-ui/custom.js");

                // UI Features
                options.DisplayRequestDuration();
                options.EnableDeepLinking();
                options.EnableFilter();
                options.ShowExtensions();
                options.ShowCommonExtensions();
                options.EnableValidator();

                // Try it out
                options.EnableTryItOutByDefault();

                // Collapse sections
                options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                options.DefaultModelExpandDepth(2);
                options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
                options.DefaultModelsExpandDepth(1);

                // Authentication
                options.DisplayOperationId();
                options.EnablePersistAuthorization();
            });

            // Redirect root to Swagger UI
            app.UseWhen(
                context => context.Request.Path == "/",
                builder => builder.Run(async context =>
                {
                    context.Response.Redirect("/api-docs");
                }));

            return app;
        }
    }

    /// <summary>
    /// Swagger default values operation filter
    /// </summary>
    public class SwaggerDefaultValues : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var apiDescription = context.ApiDescription;

            if (operation.Parameters == null)
            {
                return;
            }

            foreach (var parameter in operation.Parameters)
            {
                var description = apiDescription.ParameterDescriptions
                    .First(p => p.Name == parameter.Name);

                parameter.Description ??= description.ModelMetadata?.Description;

                if (parameter.Schema.Default == null &&
                    description.DefaultValue != null)
                {
                    parameter.Schema.Default =
                        new Microsoft.OpenApi.Any.OpenApiString(
                            description.DefaultValue.ToString());
                }

                parameter.Required |= description.IsRequired;
            }
        }
    }

    /// <summary>
    /// Add authorization requirements to Swagger
    /// </summary>
    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasAuthorize = context.MethodInfo.DeclaringType?
                .GetCustomAttributes(true)
                .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
                .Any() ?? false;

            hasAuthorize = hasAuthorize || context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
                .Any();

            var hasAllowAnonymous = context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>()
                .Any();

            if (hasAuthorize && !hasAllowAnonymous)
            {
                operation.Responses.TryAdd("401", new OpenApiResponse
                {
                    Description = "Unauthorized - Authentication required"
                });

                operation.Responses.TryAdd("403", new OpenApiResponse
                {
                    Description = "Forbidden - Insufficient permissions"
                });

                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    }
                };
            }
        }
    }

    /// <summary>
    /// Add rate limit headers to Swagger documentation
    /// </summary>
    public class RateLimitHeadersOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Responses == null)
            {
                operation.Responses = new OpenApiResponses();
            }

            // Add rate limit response
            operation.Responses.TryAdd("429", new OpenApiResponse
            {
                Description = "Too Many Requests - Rate limit exceeded",
                Headers = new Dictionary<string, OpenApiHeader>
                {
                    ["X-RateLimit-Limit"] = new OpenApiHeader
                    {
                        Description = "The maximum number of requests allowed per time window",
                        Schema = new OpenApiSchema { Type = "integer" }
                    },
                    ["X-RateLimit-Remaining"] = new OpenApiHeader
                    {
                        Description = "The number of requests remaining in the current time window",
                        Schema = new OpenApiSchema { Type = "integer" }
                    },
                    ["X-RateLimit-Reset"] = new OpenApiHeader
                    {
                        Description = "The time at which the current rate limit window resets (Unix timestamp)",
                        Schema = new OpenApiSchema { Type = "integer" }
                    },
                    ["Retry-After"] = new OpenApiHeader
                    {
                        Description = "The number of seconds to wait before making another request",
                        Schema = new OpenApiSchema { Type = "integer" }
                    }
                }
            });

            // Add rate limit headers to successful responses
            foreach (var response in operation.Responses.Where(r => r.Key.StartsWith("2")))
            {
                if (response.Value.Headers == null)
                {
                    response.Value.Headers = new Dictionary<string, OpenApiHeader>();
                }

                response.Value.Headers.TryAdd("X-RateLimit-Limit", new OpenApiHeader
                {
                    Description = "The maximum number of requests allowed per time window",
                    Schema = new OpenApiSchema { Type = "integer" }
                });

                response.Value.Headers.TryAdd("X-RateLimit-Remaining", new OpenApiHeader
                {
                    Description = "The number of requests remaining in the current time window",
                    Schema = new OpenApiSchema { Type = "integer" }
                });

                response.Value.Headers.TryAdd("X-RateLimit-Reset", new OpenApiHeader
                {
                    Description = "The time at which the current rate limit window resets (Unix timestamp)",
                    Schema = new OpenApiSchema { Type = "integer" }
                });
            }
        }
    }

    /// <summary>
    /// Enhance enum documentation in Swagger
    /// </summary>
    public class EnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.IsEnum)
            {
                schema.Enum.Clear();
                var enumNames = Enum.GetNames(context.Type);
                var enumValues = Enum.GetValues(context.Type);

                for (int i = 0; i < enumNames.Length; i++)
                {
                    schema.Enum.Add(new Microsoft.OpenApi.Any.OpenApiString(enumNames[i]));
                }

                // Add enum descriptions
                schema.Description = string.Join(", ", enumNames);
            }
        }
    }
}