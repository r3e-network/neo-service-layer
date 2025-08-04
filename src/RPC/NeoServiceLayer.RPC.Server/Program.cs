using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using NeoServiceLayer.RPC.Server.Services;
using NeoServiceLayer.RPC.Server.Hubs;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Infrastructure.Blockchain;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Infrastructure.ServiceDiscovery;
using Serilog;
using System.Threading.RateLimiting;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/neo-rpc-server-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Neo Service Layer RPC Server",
        Version = "v1",
        Description = "JSON-RPC 2.0 server for Neo Service Layer enterprise blockchain services"
    });
});

// Add SignalR
builder.Services.AddSignalR();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "NeoServiceLayer",
            ValidAudience = jwtSettings["Audience"] ?? "NeoServiceLayerUsers",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

        // Configure JWT for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var clientId = context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        
        return RateLimitPartition.GetTokenBucketLimiter(clientId, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 100,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 10,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            TokensPerPeriod = 100,
            AutoReplenishment = true
        });
    });

    options.RejectionStatusCode = 429;
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("rpc-server", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("RPC server is running"))
    .AddCheck("json-rpc", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("JSON-RPC processor is ready"));

// Register Neo Service Layer dependencies
builder.Services.AddNeoServiceFramework();
builder.Services.AddBlockchainServices(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddServiceDiscovery(builder.Configuration, "rpc-server");

// Register all Neo Service Layer services
try
{
    // Key Management
    builder.Services.AddScoped<NeoServiceLayer.Services.KeyManagement.IKeyManagementService, NeoServiceLayer.Services.KeyManagement.KeyManagementService>();
    
    // Storage
    builder.Services.AddScoped<NeoServiceLayer.Services.Storage.IStorageService, NeoServiceLayer.Services.Storage.StorageService>();
    
    // Oracle
    builder.Services.AddScoped<NeoServiceLayer.Services.Oracle.IOracleService, NeoServiceLayer.Services.Oracle.OracleService>();
    
    // Voting
    builder.Services.AddScoped<NeoServiceLayer.Services.Voting.IVotingService, NeoServiceLayer.Services.Voting.VotingService>();
    
    // Randomness
    builder.Services.AddScoped<NeoServiceLayer.Services.Randomness.IRandomnessService, NeoServiceLayer.Services.Randomness.RandomnessService>();
    
    // Smart Contracts
    builder.Services.AddScoped<NeoServiceLayer.Services.SmartContracts.ISmartContractService, NeoServiceLayer.Services.SmartContracts.SmartContractService>();
}
catch (Exception ex)
{
    Log.Warning(ex, "Some services could not be registered - continuing with available services");
}

// Register RPC services
builder.Services.AddScoped<JsonRpcProcessor>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map SignalR hub
app.MapHub<NotificationHub>("/ws");

// Map health checks
app.MapHealthChecks("/health");

// API info endpoint
app.MapGet("/", () => new
{
    name = "Neo Service Layer RPC Server",
    version = "1.0.0",
    protocol = "JSON-RPC 2.0",
    endpoints = new
    {
        rpc = "/rpc",
        websocket = "/ws",
        health = "/health",
        swagger = "/swagger"
    },
    timestamp = DateTime.UtcNow
}).AllowAnonymous();

Log.Information("Neo Service Layer RPC Server starting up...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "RPC Server terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}