using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Asp.Versioning.ApiExplorer;
using Asp.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NeoServiceLayer.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        });

        // Commented out due to API changes in versioning library
        // services.AddVersionedApiExplorer(options =>
        // {
        //     options.GroupNameFormat = "'v'VVV";
        //     options.SubstituteApiVersionInUrl = true;
        // });

        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        
        services.AddSwaggerGen(options =>
        {
            // Add security definition
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = @"JWT Authorization header using the Bearer scheme. 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer 12345abcdef'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header,
                    },
                    new List<string>()
                }
            });

            // Add API Key security definition
            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Description = "API Key Authentication",
                Type = SecuritySchemeType.ApiKey,
                Name = "X-API-Key",
                In = ParameterLocation.Header
            });

            // Include XML comments
            var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly).ToList();
            xmlFiles.ForEach(xmlFile => options.IncludeXmlComments(xmlFile));

            // Add custom operation filters
            options.OperationFilter<SwaggerDefaultValues>();
            options.OperationFilter<AddCorrelationIdHeaderParameter>();
            options.OperationFilter<AddResponseHeadersFilter>();

            // Document filter for additional information
            options.DocumentFilter<HealthCheckDocumentFilter>();

            // Schema filters
            options.SchemaFilter<EnumSchemaFilter>();
            options.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();

            // Custom naming strategy
            options.CustomSchemaIds(type => type.FullName);
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app, IApiVersionDescriptionProvider provider)
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "swagger/{documentName}/swagger.json";
            options.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
            {
                swaggerDoc.Servers = new List<OpenApiServer>
                {
                    new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" }
                };
            });
        });

        app.UseSwaggerUI(options =>
        {
            // Build a swagger endpoint for each discovered API version
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", 
                    $"Neo Service Layer API {description.GroupName.ToUpperInvariant()}");
            }

            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Neo Service Layer API Documentation";
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            options.DefaultModelExpandDepth(2);
            options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
            options.DisplayOperationId();
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();
            options.EnableValidator();
            options.InjectStylesheet("/swagger-ui/custom.css");
            options.InjectJavascript("/swagger-ui/custom.js");
        });

        return app;
    }
}

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;
    private readonly IConfiguration _configuration;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider, IConfiguration configuration)
    {
        _provider = provider;
        _configuration = configuration;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
        }
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var info = new OpenApiInfo()
        {
            Title = "Neo Service Layer API",
            Version = description.ApiVersion.ToString(),
            Description = BuildDescription(description),
            Contact = new OpenApiContact
            {
                Name = "Neo Service Layer Team",
                Email = "support@neoservicelayer.com",
                Url = new Uri("https://neoservicelayer.com")
            },
            License = new OpenApiLicense
            {
                Name = "MIT",
                Url = new Uri("https://opensource.org/licenses/MIT")
            },
            TermsOfService = new Uri("https://neoservicelayer.com/terms")
        };

        if (description.IsDeprecated)
        {
            info.Description += " This API version has been deprecated.";
        }

        return info;
    }

    private static string BuildDescription(ApiVersionDescription description)
    {
        var text = @"
## Overview
The Neo Service Layer API provides a comprehensive set of microservices for blockchain operations, including:

- **Key Management**: Secure key generation, storage, and signing
- **Storage**: Distributed encrypted storage
- **Oracle**: External data integration
- **AI Services**: Pattern recognition and prediction
- **Cross-Chain**: Multi-blockchain support
- **Compliance**: Regulatory compliance tools

## Authentication
This API supports two authentication methods:
1. **JWT Bearer Token**: For user authentication
2. **API Key**: For service-to-service communication

## Rate Limiting
- Default: 1000 requests per minute
- Key Management: 100 requests per minute
- AI Services: 200 requests per minute

## Versioning
API versioning is handled through URL path versioning (e.g., `/api/v1/...`)

## Status Codes
- `200 OK`: Successful request
- `201 Created`: Resource created successfully
- `400 Bad Request`: Invalid request parameters
- `401 Unauthorized`: Missing or invalid authentication
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: Resource not found
- `429 Too Many Requests`: Rate limit exceeded
- `500 Internal Server Error`: Server error

## Error Response Format
```json
{
  ""error"": {
    ""code"": ""ERROR_CODE"",
    ""message"": ""Human-readable error message"",
    ""details"": {},
    ""timestamp"": ""2024-01-01T00:00:00Z"",
    ""correlationId"": ""uuid""
  }
}
```
";
        
        if (description.IsDeprecated)
        {
            text = $"**This API version is deprecated and will be removed in future releases.**\n\n{text}";
        }

        return text;
    }
}

public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        operation.Deprecated |= apiDescription.IsDeprecated();

        if (operation.Parameters == null)
        {
            return;
        }

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);

            parameter.Description ??= description.ModelMetadata?.Description;

            if (parameter.Schema.Default == null && description.DefaultValue != null)
            {
                parameter.Schema.Default = new Microsoft.OpenApi.Any.OpenApiString(description.DefaultValue.ToString());
            }

            parameter.Required |= description.IsRequired;
        }
    }
}

public class AddCorrelationIdHeaderParameter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Correlation-Id",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Correlation ID for request tracking",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid"
            }
        });
    }
}

public class AddResponseHeadersFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var response in operation.Responses)
        {
            response.Value.Headers ??= new Dictionary<string, OpenApiHeader>();
            
            response.Value.Headers.Add("X-Correlation-Id", new OpenApiHeader
            {
                Description = "Correlation ID for request tracking",
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Format = "uuid"
                }
            });

            response.Value.Headers.Add("X-Rate-Limit-Remaining", new OpenApiHeader
            {
                Description = "Number of requests remaining in the current rate limit window",
                Schema = new OpenApiSchema
                {
                    Type = "integer"
                }
            });

            response.Value.Headers.Add("X-Rate-Limit-Reset", new OpenApiHeader
            {
                Description = "Time when the rate limit window resets (Unix timestamp)",
                Schema = new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int64"
                }
            });
        }
    }
}

public class HealthCheckDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var healthCheckPath = new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Health" } },
                    Summary = "Health check endpoint",
                    Description = "Returns the health status of the service and its dependencies",
                    OperationId = "GetHealth",
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Service is healthy",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, OpenApiSchema>
                                        {
                                            ["status"] = new OpenApiSchema { Type = "string" },
                                            ["version"] = new OpenApiSchema { Type = "string" },
                                            ["services"] = new OpenApiSchema { Type = "object" }
                                        }
                                    }
                                }
                            }
                        },
                        ["503"] = new OpenApiResponse
                        {
                            Description = "Service is unhealthy"
                        }
                    }
                }
            }
        };

        swaggerDoc.Paths.Add("/health", healthCheckPath);
    }
}

public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();
            var enumValues = Enum.GetValues(context.Type);

            foreach (var enumValue in enumValues)
            {
                var value = Convert.ToInt64(enumValue);
                var name = enumValue.ToString();
                schema.Enum.Add(new Microsoft.OpenApi.Any.OpenApiString($"{value} - {name}"));
            }

            schema.Type = "string";
            schema.Format = null;
        }
    }
}

public class RequireNonNullablePropertiesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null)
        {
            return;
        }

        foreach (var property in schema.Properties)
        {
            if (!property.Value.Nullable && !schema.Required.Contains(property.Key))
            {
                schema.Required.Add(property.Key);
            }
        }
    }
}