using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.AbstractAccount;
using NeoServiceLayer.Services.AbstractAccount.Models;
using NeoServiceLayer.Services.Voting;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Web;

namespace NeoServiceLayer.Integration.Tests.Helpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        // Use random available port to avoid conflicts between test runs
        builder.UseUrls("http://localhost:0");

        // Configure JWT secret through configuration instead of environment variable
        builder.UseSetting("Jwt:SecretKey", "SuperSecretTestKeyThatIsLongEnoughForTesting123!");

        builder.ConfigureServices(services =>
        {
            // Remove problematic services that cause initialization issues
            var descriptors = services.Where(d =>
                d.ServiceType.Name.Contains("DbContext") ||
                d.ServiceType.Name.Contains("EnclaveManager") ||
                d.ServiceType == typeof(IEnclaveManager)
            ).ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Add mock enclave manager
            var mockEnclaveManager = new Mock<IEnclaveManager>();
            mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);
            services.AddSingleton(mockEnclaveManager.Object);
        });

        // Configure services after the main application services are registered
        builder.ConfigureServices(services =>
        {
            // Remove the real services that we want to mock
            var servicesToRemove = services.Where(d =>
                d.ServiceType == typeof(IVotingService) ||
                d.ServiceType == typeof(IAbstractAccountService)
            ).ToArray();

            foreach (var descriptor in servicesToRemove)
            {
                services.Remove(descriptor);
            }

            // Add mock voting service
            var mockVotingService = new Mock<IVotingService>();
            mockVotingService.Setup(x => x.GetCouncilNodesAsync(It.IsAny<BlockchainType>()))
                .ReturnsAsync(new List<CouncilNodeInfo>
                {
                    new() { Address = "test-address-1", Name = "Test Node 1", IsActive = true, VotesReceived = 1000 },
                    new() { Address = "test-address-2", Name = "Test Node 2", IsActive = true, VotesReceived = 2000 }
                });
            services.AddSingleton(mockVotingService.Object);

            // Add mock abstract account service
            var mockAbstractAccountService = new Mock<IAbstractAccountService>();
            mockAbstractAccountService.Setup(x => x.CreateAccountAsync(It.IsAny<CreateAccountRequest>(), It.IsAny<BlockchainType>()))
                .ReturnsAsync(new AbstractAccountResult
                {
                    AccountId = "test-account-id",
                    AccountAddress = "test-address",
                    MasterPublicKey = "test-public-key",
                    Success = true
                });
            services.AddSingleton(mockAbstractAccountService.Object);
        });

        base.ConfigureWebHost(builder);
    }
}
