using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using NeoServiceLayer.Api;
using Xunit;

namespace NeoServiceLayer.Integration.Tests
{
    public class RandomnessWorkflowTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public RandomnessWorkflowTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task GenerateRandomBytes_ValidRequest_ReturnsRandomBytes()
        {
            // Arrange
            var request = new
            {
                Length = 32,
                Seed = "optional-seed-" + Guid.NewGuid().ToString("N")
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/randomness/bytes", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<ApiResponse<RandomBytesResponse>>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(responseObj);
            Assert.True(responseObj.Success);
            Assert.NotNull(responseObj.Data);
            Assert.NotNull(responseObj.Data.RandomBytes);
            Assert.NotNull(responseObj.Data.Proof);
            Assert.Equal(request.Seed, responseObj.Data.Seed);
        }

        [Fact]
        public async System.Threading.Tasks.Task GenerateRandomNumbers_ValidRequest_ReturnsRandomNumbers()
        {
            // Arrange
            var request = new
            {
                Count = 5,
                Min = 1,
                Max = 100,
                Seed = "optional-seed-" + Guid.NewGuid().ToString("N")
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/randomness/numbers", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<ApiResponse<RandomNumbersResponse>>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(responseObj);
            Assert.True(responseObj.Success);
            Assert.NotNull(responseObj.Data);
            Assert.NotNull(responseObj.Data.RandomNumbers);
            Assert.Equal(request.Count, responseObj.Data.RandomNumbers.Length);
            Assert.NotNull(responseObj.Data.Proof);
            Assert.Equal(request.Seed, responseObj.Data.Seed);

            // Verify that all numbers are within the specified range
            foreach (var number in responseObj.Data.RandomNumbers)
            {
                Assert.InRange(number, request.Min, request.Max);
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task GenerateRandomString_ValidRequest_ReturnsRandomString()
        {
            // Arrange
            var request = new
            {
                Length = 16,
                Charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789",
                Seed = "optional-seed-" + Guid.NewGuid().ToString("N")
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/randomness/string", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<ApiResponse<RandomStringResponse>>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(responseObj);
            Assert.True(responseObj.Success);
            Assert.NotNull(responseObj.Data);
            Assert.NotNull(responseObj.Data.RandomString);
            Assert.Equal(request.Length, responseObj.Data.RandomString.Length);
            Assert.NotNull(responseObj.Data.Proof);
            Assert.Equal(request.Seed, responseObj.Data.Seed);

            // Verify that all characters in the string are from the specified charset
            foreach (var c in responseObj.Data.RandomString)
            {
                Assert.Contains(c, request.Charset);
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task VerifyRandomness_ValidProof_ReturnsVerificationResult()
        {
            // Arrange
            // First generate random bytes to get a proof
            var generateRequest = new
            {
                Length = 32,
                Seed = "verify-seed-" + Guid.NewGuid().ToString("N")
            };

            var generateContent = new StringContent(JsonSerializer.Serialize(generateRequest), Encoding.UTF8, "application/json");
            var generateResponse = await _client.PostAsync("/api/randomness/bytes", generateContent);
            generateResponse.EnsureSuccessStatusCode();

            var generateResponseString = await generateResponse.Content.ReadAsStringAsync();
            var generateResponseObj = JsonSerializer.Deserialize<ApiResponse<RandomBytesResponse>>(generateResponseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var proof = generateResponseObj.Data.Proof;

            // Now verify the proof
            var verifyRequest = new
            {
                RandomNumbers = new int[] { 1, 2, 3, 4, 5 }, // Add random numbers
                Proof = proof,
                Seed = generateResponseObj.Data.Seed // Add the seed
            };

            var verifyContent = new StringContent(JsonSerializer.Serialize(verifyRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/randomness/verify", verifyContent);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<ApiResponse<VerifyRandomnessResponse>>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(responseObj);
            Assert.True(responseObj.Success);
            Assert.NotNull(responseObj.Data);
            Assert.True(responseObj.Data.Valid);
        }
    }

    // Helper classes for deserialization
    public class RandomBytesResponse
    {
        public string RandomBytes { get; set; }
        public string Proof { get; set; }
        public string Seed { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class RandomNumbersResponse
    {
        public int[] RandomNumbers { get; set; }
        public string Proof { get; set; }
        public string Seed { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class RandomStringResponse
    {
        public string RandomString { get; set; }
        public string Proof { get; set; }
        public string Seed { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class VerifyRandomnessResponse
    {
        public bool Valid { get; set; }
        public string Reason { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
