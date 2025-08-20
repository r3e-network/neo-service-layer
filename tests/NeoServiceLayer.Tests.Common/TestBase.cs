using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NeoServiceLayer.Tests.Common
{
    public abstract class TestBase : IDisposable
    {
        protected readonly IServiceProvider ServiceProvider;
        protected readonly Mock<ILogger> LoggerMock;
        
        protected TestBase()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            LoggerMock = new Mock<ILogger>();
        }
        
        protected virtual void ConfigureServices(IServiceCollection services)
        {
            // Override in derived classes to add specific services
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (ServiceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
    
    public abstract class AsyncTestBase : TestBase, IAsyncLifetime
    {
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
        
        public virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
