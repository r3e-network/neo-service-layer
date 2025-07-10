using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure pipeline
app.MapHealthChecks("/health");

app.MapGet("/", () => new
{
    service = "TestService",
    version = "1.0.0",
    status = "running"
});

app.MapGet("/info", () => new
{
    service = "TestService",
    capabilities = new[] { "test", "demo" },
    endpoints = new[] { "/", "/health", "/info" }
});

Console.WriteLine("Test Service starting on port " + (Environment.GetEnvironmentVariable("SERVICE_PORT") ?? "80"));

app.Run();
