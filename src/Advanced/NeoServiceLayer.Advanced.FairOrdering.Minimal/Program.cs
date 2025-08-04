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
        Title = "Neo FairOrdering Service",
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo FairOrdering Service V1");
    });
}

app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

// Add service endpoints
app.MapGet("/", () => "Neo FairOrdering Service");
app.MapGet("/api/status", () => new { status = "healthy", service = "fairordering", timestamp = DateTime.UtcNow });
app.MapGet("/api/fairordering/info", () => new { 
    service = "FairOrdering",
    version = "1.0.0",
    features = new[] { "Feature1", "Feature2", "Feature3" },
    capabilities = new[] { "FairOrderingCap1", "FairOrderingCap2" }
});

app.Run();
