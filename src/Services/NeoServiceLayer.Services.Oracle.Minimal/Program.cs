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
        Title = "Neo Oracle Service",
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo Oracle Service V1");
    });
}

app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

// Add service endpoints
app.MapGet("/", () => "Neo Oracle Service");
app.MapGet("/api/status", () => new { status = "healthy", service = "oracle", timestamp = DateTime.UtcNow });
app.MapGet("/api/oracle/info", () => new { 
    service = "Oracle",
    version = "1.0.0",
    features = new[] { "Feature1", "Feature2", "Feature3" },
    capabilities = new[] { "OracleCap1", "OracleCap2" }
});

app.Run();
