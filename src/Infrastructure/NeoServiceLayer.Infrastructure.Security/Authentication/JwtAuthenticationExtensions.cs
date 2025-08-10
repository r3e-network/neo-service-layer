using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace NeoServiceLayer.Infrastructure.Security.Authentication
{
    /// <summary>
    /// Extension methods for configuring JWT authentication across all services.
    /// </summary>
    public static class JwtAuthenticationExtensions
    {
        /// <summary>
        /// Adds JWT authentication to the service collection with standard configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNeoServiceLayerJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"] ?? "NeoServiceLayer";
            var audience = jwtSettings["Audience"] ?? "NeoServiceLayerServices";

            // Validate JWT secret key
            if (string.IsNullOrEmpty(secretKey))
            {
                // In microservices mode, JWT might be optional for internal communication
                var deploymentMode = configuration["DeploymentMode"] ?? "Monolithic";
                if (deploymentMode == "Monolithic")
                {
                    throw new InvalidOperationException("JWT secret key must be configured via JWT_SECRET_KEY environment variable");
                }
                // Use a default key for development/testing only
                secretKey = "development-only-key-not-for-production-use-32chars";
            }

            // Ensure minimum key length for security
            if (secretKey.Length < 32)
            {
                throw new InvalidOperationException("JWT secret key must be at least 32 characters long");
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                        ClockSkew = TimeSpan.FromMinutes(5)
                    };

                    // Add events for debugging in development
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            {
                                context.Response.Headers.Add("Token-Expired", "true");
                            }
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            // Additional token validation logic can be added here
                            return Task.CompletedTask;
                        }
                    };
                });

            // Add authorization policies
            services.AddAuthorization(options =>
            {
                // Service-to-service communication policy
                options.AddPolicy("ServiceToService", policy => 
                    policy.RequireAuthenticatedUser()
                          .RequireClaim("service", "true"));

                // Admin policy
                options.AddPolicy("AdminOnly", policy => 
                    policy.RequireRole("Admin"));

                // Service user policy
                options.AddPolicy("ServiceUser", policy => 
                    policy.RequireRole("Admin", "ServiceUser", "User"));

                // Key management policies
                options.AddPolicy("KeyManager", policy => 
                    policy.RequireRole("Admin", "KeyManager"));

                options.AddPolicy("KeyUser", policy => 
                    policy.RequireRole("Admin", "KeyManager", "KeyUser"));
            });

            return services;
        }

        /// <summary>
        /// Adds JWT token generation service.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddJwtTokenService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IJwtTokenService, JwtTokenService>();
            return services;
        }
    }
}