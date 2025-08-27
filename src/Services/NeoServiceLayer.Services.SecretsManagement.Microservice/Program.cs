using Neo.SecretsManagement.Service.Data;
using Neo.SecretsManagement.Service.Services;
using Neo.SecretsManagement.Service.BackgroundServices;
using Neo.SecretsManagement.Service.HealthChecks;
using NeoServiceLayer.Common.Extensions;
using System.Reflection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add common Neo Service Layer components
builder.Services.AddNeoServiceCommon(builder.Configuration, Assembly.GetExecutingAssembly());
builder.Services.AddNeoServiceDatabase<SecretsDbContext>(builder.Configuration);
builder.Services.AddNeoServiceAuthentication(builder.Configuration);
builder.Services.AddNeoServiceTelemetry(builder.Configuration, "neo-secrets-management");
builder.Services.AddNeoServiceCors(builder.Configuration);

// JSON serialization options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Redis for distributed coordination and caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "SecretsManagement";
});

// Core services
builder.Services.AddScoped<ISecretService, SecretService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IKeyManagementService, KeyManagementService>();
builder.Services.AddScoped<ISecretPolicyService, SecretPolicyService>();
builder.Services.AddScoped<IRotationService, RotationService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IHsmService, HsmService>();

// Background services
builder.Services.AddHostedService<SecretRotationService>();
builder.Services.AddHostedService<KeyRotationService>();
builder.Services.AddHostedService<SecretExpirationService>();
builder.Services.AddHostedService<AuditLogCleanupService>();
builder.Services.AddHostedService<BackupSchedulerService>();

// HTTP clients for external services
builder.Services.AddHttpClient("HSM", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "Neo-SecretsManagement/1.0");
});

builder.Services.AddHttpClient("VaultBackend", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.Add("User-Agent", "Neo-SecretsManagement/1.0");
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (!string.IsNullOrEmpty(jwtKey) && !string.IsNullOrEmpty(jwtIssuer))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };
            
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });
}

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SecretReader", policy => 
        policy.RequireClaim("role", "secret-reader", "secret-admin", "admin"));
    
    options.AddPolicy("SecretWriter", policy => 
        policy.RequireClaim("role", "secret-writer", "secret-admin", "admin"));
    
    options.AddPolicy("SecretAdmin", policy => 
        policy.RequireClaim("role", "secret-admin", "admin"));
    
    options.AddPolicy("SystemAdmin", policy => 
        policy.RequireClaim("role", "admin"));
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContext<SecretsDbContext>(name: "database")
    .AddCheck<SecretsVaultHealthCheck>("secrets-vault")
    .AddCheck<EncryptionHealthCheck>("encryption")
    .AddCheck<HsmHealthCheck>("hsm")
    .AddCheck<RotationHealthCheck>("rotation");

// OpenTelemetry
var serviceName = "neo-secrets-management";
var serviceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName, serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["service.namespace"] = "neo-service-layer",
                    ["service.instance.id"] = Environment.MachineName,
                    ["deployment.environment"] = builder.Environment.EnvironmentName
                }))
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequest = (activity, request) =>
                {
                    activity.SetTag("secrets.operation", request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault());
                };
                options.EnrichWithHttpResponse = (activity, response) =>
                {
                    activity.SetTag("secrets.status_code", response.StatusCode);
                };
            })
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
                options.EnrichWithIDbCommand = (activity, command) =>
                {
                    activity.SetTag("secrets.db.operation", command.CommandText?.Split(' ').FirstOrDefault()?.ToUpper());
                };
            })
            .AddHttpClientInstrumentation()
            .AddJaegerExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName, serviceVersion))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter();
    });

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Neo Secrets Management API", 
        Version = "v1",
        Description = "Enterprise-grade secrets management service with HSM integration",
        Contact = new OpenApiContact
        {
            Name = "Neo Service Layer",
            Url = new Uri("https://github.com/neo-service-layer")
        }
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Memory cache
builder.Services.AddMemoryCache();

// Logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddJsonConsole();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug();
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo Secrets Management API v1");
    c.RoutePrefix = string.Empty;
});

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    await next();
});

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Routing
app.MapControllers()
   .RequireAuthorization();

// Health checks endpoint
app.MapHealthChecks("/health")
   .AllowAnonymous();

// Metrics endpoint for Prometheus
app.MapPrometheusScrapingEndpoint("/metrics")
   .AllowAnonymous();

// Startup tasks
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SecretsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync();
        
        // Initialize default encryption keys if none exist
        var keyService = scope.ServiceProvider.GetRequiredService<IKeyManagementService>();
        await keyService.EnsureDefaultKeysAsync();
        
        logger.LogInformation("Database initialization completed");
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database");
        throw;
    }
}

app.Logger.LogInformation("Neo Secrets Management Service started");

app.Run();