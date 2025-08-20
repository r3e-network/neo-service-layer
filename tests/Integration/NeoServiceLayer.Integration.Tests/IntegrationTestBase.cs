using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Integration.Tests;

/// <summary>
/// Base class for integration tests with full service setup
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IHost Host { get; private set; }
    protected IServiceProvider Services => Host.Services;
    protected IConfiguration Configuration { get; private set; }
    
    public async Task InitializeAsync()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.test.json", optional: true);
                config.AddEnvironmentVariables();
                ConfigureTestConfiguration(config);
            })
            .ConfigureServices((context, services) =>
            {
                Configuration = context.Configuration;
                ConfigureTestServices(services);
            });
        
        Host = builder.Build();
        await Host.StartAsync();
        await OnInitializedAsync();
    }
    
    public async Task DisposeAsync()
    {
        await OnDisposingAsync();
        await Host?.StopAsync();
        Host?.Dispose();
    }
    
    /// <summary>
    /// Override to add test-specific configuration
    /// </summary>
    protected virtual void ConfigureTestConfiguration(IConfigurationBuilder builder)
    {
        // Add test-specific configuration
    }
    
    /// <summary>
    /// Override to add test-specific services
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // Add test-specific services
    }
    
    /// <summary>
    /// Called after host is initialized
    /// </summary>
    protected virtual Task OnInitializedAsync() => Task.CompletedTask;
    
    /// <summary>
    /// Called before host is disposed
    /// </summary>
    protected virtual Task OnDisposingAsync() => Task.CompletedTask;
    
    /// <summary>
    /// Get a required service
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }
    
    /// <summary>
    /// Get an optional service
    /// </summary>
    protected T? GetOptionalService<T>() where T : class
    {
        return Services.GetService<T>();
    }
}