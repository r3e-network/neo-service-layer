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
    public class KeyManagementWorkflowTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public KeyManagementWorkflowTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task GenerateKey_ValidRequest_ReturnsKeyPair()
        {
            // Arrange
            var request = new
            {
                KeyType = "secp256r1",
                KeyName = "test-key-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                UserId = "test-user-id"
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/keys/generate", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<ApiResponse<GenerateKeyResponse>>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(responseObj);
            Assert.True(responseObj.Success);
            Assert.NotNull(responseObj.Data);
            Assert.NotNull(responseObj.Data.KeyId);
            Assert.NotNull(responseObj.Data.PublicKey);
            Assert.Equal(request.KeyName, responseObj.Data.KeyName);
            Assert.Equal(request.KeyType, responseObj.Data.KeyType);
        }

        [Fact]
        public async System.Threading.Tasks.Task SignData_ValidRequest_ReturnsSignature()
        {
            // Arrange
            // First generate a key
            var generateRequest = new
            {
                KeyType = "secp256r1",
                KeyName = "test-signing-key-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                UserId = "test-user-id"
            };

            var generateContent = new StringContent(JsonSerializer.Serialize(generateRequest), Encoding.UTF8, "application/json");
            var generateResponse = await _client.PostAsync("/api/keys/generate", generateContent);
            generateResponse.EnsureSuccessStatusCode();

            var generateResponseString = await generateResponse.Content.ReadAsStringAsync();
            var generateResponseObj = JsonSerializer.Deserialize<ApiResponse<GenerateKeyResponse>>(generateResponseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var keyId = generateResponseObj.Data.KeyId;

            // Now sign data with the key
            var signRequest = new
            {
                KeyId = keyId,
                Data = Convert.ToBase64String(Encoding.UTF8.GetBytes("test data to sign")),
                UserId = "test-user-id"
            };

            var signContent = new StringContent(JsonSerializer.Serialize(signRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/keys/sign", signContent);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<ApiResponse<SignDataResponse>>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(responseObj);
            Assert.True(responseObj.Success);
            Assert.NotNull(responseObj.Data);
            Assert.NotNull(responseObj.Data.Signature);
            Assert.Equal(keyId, responseObj.Data.KeyId);
        }

        [Fact]
        public async System.Threading.Tasks.Task VerifySignature_ValidRequest_ReturnsVerificationResult()
        {
            // Arrange
            // First generate a key
            var generateRequest = new
            {
                KeyType = "secp256r1",
                KeyName = "test-verify-key-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                UserId = "test-user-id"
            };

            var generateContent = new StringContent(JsonSerializer.Serialize(generateRequest), Encoding.UTF8, "application/json");
            var generateResponse = await _client.PostAsync("/api/keys/generate", generateContent);
            generateResponse.EnsureSuccessStatusCode();

            var generateResponseString = await generateResponse.Content.ReadAsStringAsync();
            var generateResponseObj = JsonSerializer.Deserialize<ApiResponse<GenerateKeyResponse>>(generateResponseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var keyId = generateResponseObj.Data.KeyId;
            var publicKey = generateResponseObj.Data.PublicKey;

            // Now sign data with the key
            var signRequest = new
            {
                KeyId = keyId,
                Data = Convert.ToBase64String(Encoding.UTF8.GetBytes("test data to verify")),
                UserId = "test-user-id"
            };

            var signContent = new StringContent(JsonSerializer.Serialize(signRequest), Encoding.UTF8, "application/json");
            var signResponse = await _client.PostAsync("/api/keys/sign", signContent);
            signResponse.EnsureSuccessStatusCode();

            var signResponseString = await signResponse.Content.ReadAsStringAsync();
            var signResponseObj = JsonSerializer.Deserialize<ApiResponse<SignDataResponse>>(signResponseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var signature = signResponseObj.Data.Signature;

            // Now verify the signature
            var verifyRequest = new
            {
                PublicKey = publicKey,
                Data = signRequest.Data,
                Signature = signature
            };

            var verifyContent = new StringContent(JsonSerializer.Serialize(verifyRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/keys/verify", verifyContent);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<ApiResponse<VerifySignatureResponse>>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(responseObj);
            Assert.True(responseObj.Success);
            Assert.NotNull(responseObj.Data);
            Assert.True(responseObj.Data.IsValid);
        }
    }

    // Helper classes for deserialization
    public class GenerateKeyResponse
    {
        public string KeyId { get; set; }
        public string KeyName { get; set; }
        public string KeyType { get; set; }
        public string PublicKey { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SignDataResponse
    {
        public string KeyId { get; set; }
        public string Signature { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class VerifySignatureResponse
    {
        public bool IsValid { get; set; }
        public string Reason { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
