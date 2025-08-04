using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Neo CrossChain Service",
        Version = "v1"
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo CrossChain Service V1");
    });
}

app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

// Add service endpoints
app.MapGet("/", () => "Neo CrossChain Service");
app.MapGet("/api/status", () => new { status = "healthy", service = "crosschain", timestamp = DateTime.UtcNow });
app.MapGet("/api/crosschain/info", () => new { 
    service = "CrossChain",
    version = "1.0.0",
    features = new[] { "Feature1", "Feature2", "Feature3" },
    capabilities = new[] { "CrossChainCap1", "CrossChainCap2" }
});

app.Run();
