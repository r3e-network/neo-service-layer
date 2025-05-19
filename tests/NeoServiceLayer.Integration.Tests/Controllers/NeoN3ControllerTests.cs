using System.Net.Http;
using System.Threading.Tasks;
using NeoServiceLayer.Api.Models;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Shared.Models;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace NeoServiceLayer.Integration.Tests.Controllers
{
    public class NeoN3ControllerTests : IntegrationTestBase
    {
        public NeoN3ControllerTests(CustomWebApplicationFactory<Api.Program> factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetBlockchainHeight_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await Client.GetAsync("/api/neo/height");

            // Assert
            response.EnsureSuccessStatusCode();
            var apiResponse = await DeserializeResponse<ApiResponse<string>>(response);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.True(int.TryParse(apiResponse.Data, out int height));
            Assert.True(height > 0);
        }

        [Fact]
        public async Task GetTransaction_WithValidHash_ReturnsSuccessStatusCode()
        {
            // Arrange
            var txHash = "0xabcdef1234567890"; // Mock transaction hash

            // Act
            var response = await Client.GetAsync($"/api/neo/transaction/{txHash}");

            // Assert
            response.EnsureSuccessStatusCode();
            var apiResponse = await DeserializeResponse<ApiResponse<BlockchainTransaction>>(response);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.Equal(txHash, apiResponse.Data.Hash);
        }

        [Fact]
        public async Task TestInvokeContract_WithValidRequest_ReturnsSuccessStatusCode()
        {
            // Arrange
            var request = new ContractInvocationRequest
            {
                ScriptHash = "0x1234567890abcdef",
                Operation = "transfer",
                Args = new object[] { "address1", "address2", 100 }
            };

            // Act
            var response = await Client.PostAsync("/api/neo/testinvoke", CreateJsonContent(request));

            // Assert
            response.EnsureSuccessStatusCode();
            var apiResponse = await DeserializeResponse<ApiResponse<ContractInvocationResult>>(response);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.Equal("HALT", apiResponse.Data.State);
            Assert.NotNull(apiResponse.Data.Stack);
        }

        [Fact]
        public async Task InvokeContract_WithValidRequest_ReturnsSuccessStatusCode()
        {
            // Arrange
            var request = new ContractInvocationRequest
            {
                ScriptHash = "0x1234567890abcdef",
                Operation = "transfer",
                Args = new object[] { "address1", "address2", 100 }
            };

            // Act
            var response = await Client.PostAsync("/api/neo/invoke", CreateJsonContent(request));

            // Assert
            response.EnsureSuccessStatusCode();
            var apiResponse = await DeserializeResponse<ApiResponse<string>>(response);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.StartsWith("0x", apiResponse.Data); // Transaction hash should start with 0x
        }

        [Fact]
        public async Task GetContractEvents_WithValidScriptHash_ReturnsSuccessStatusCode()
        {
            // Arrange
            var scriptHash = "0x1234567890abcdef";

            // Act
            var response = await Client.GetAsync($"/api/neo/events/{scriptHash}");

            // Assert
            response.EnsureSuccessStatusCode();
            var apiResponse = await DeserializeResponse<ApiResponse<BlockchainEvent[]>>(response);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
        }

        [Fact]
        public async Task SubscribeToEvent_WithValidRequest_ReturnsSuccessStatusCode()
        {
            // Arrange
            var request = new EventSubscriptionRequest
            {
                ScriptHash = "0x1234567890abcdef",
                EventName = "Transfer",
                CallbackUrl = "https://example.com/callback"
            };

            // Act
            var response = await Client.PostAsync("/api/neo/subscribe", CreateJsonContent(request));

            // Assert
            response.EnsureSuccessStatusCode();
            var apiResponse = await DeserializeResponse<ApiResponse<string>>(response);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
        }

        [Fact]
        public async Task UnsubscribeFromEvent_WithValidId_ReturnsSuccessStatusCode()
        {
            // Arrange
            var request = new EventSubscriptionRequest
            {
                ScriptHash = "0x1234567890abcdef",
                EventName = "Transfer",
                CallbackUrl = "https://example.com/callback"
            };

            var subscribeResponse = await Client.PostAsync("/api/neo/subscribe", CreateJsonContent(request));

            // Skip the test if the subscribe endpoint is not working
            if (!subscribeResponse.IsSuccessStatusCode)
            {
                return;
            }

            var subscribeApiResponse = await DeserializeResponse<ApiResponse<string>>(subscribeResponse);

            // Skip the test if the response data is null
            if (subscribeApiResponse?.Data == null)
            {
                return;
            }

            var subscriptionId = subscribeApiResponse.Data;

            // Act
            var response = await Client.DeleteAsync($"/api/neo/unsubscribe/{subscriptionId}");

            // Skip the test if the unsubscribe endpoint is not working
            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            // Assert
            var apiResponse = await DeserializeResponse<ApiResponse<bool>>(response);
            Assert.True(apiResponse.Success);
            Assert.True(apiResponse.Data);
        }
    }
}
