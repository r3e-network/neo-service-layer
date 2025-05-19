using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Api;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Shared.Models;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace NeoServiceLayer.Integration.Tests
{
    public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        protected readonly CustomWebApplicationFactory<Program> Factory;
        protected readonly HttpClient Client;
        protected readonly JsonSerializerOptions JsonOptions;
        protected readonly ILoggerFactory LoggerFactory;

        protected IntegrationTestBase(CustomWebApplicationFactory<Program> factory)
        {
            Factory = factory;
            Client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            JsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            LoggerFactory = Factory.Services.GetRequiredService<ILoggerFactory>();
        }

        protected StringContent CreateJsonContent(object content)
        {
            if (content == null)
            {
                content = new { }; // Empty object
            }
            var json = JsonSerializer.Serialize(content, JsonOptions);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            return stringContent;
        }

        protected async Task<T> DeserializeResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }

        protected async Task<ApiResponse<T>> GetApiResponseAsync<T>(string endpoint)
        {
            var response = await Client.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        }

        protected async Task<ApiResponse<T>> PostApiResponseAsync<T>(string endpoint, object data)
        {
            var response = await Client.PostAsJsonAsync(endpoint, data, JsonOptions);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        }

        protected async Task<ApiResponse<T>> PutApiResponseAsync<T>(string endpoint, object data)
        {
            var response = await Client.PutAsJsonAsync(endpoint, data, JsonOptions);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        }

        protected async Task<ApiResponse<T>> DeleteApiResponseAsync<T>(string endpoint)
        {
            var response = await Client.DeleteAsync(endpoint);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        }

        protected T GetService<T>() where T : class
        {
            return Factory.Services.GetRequiredService<T>();
        }
    }
}
