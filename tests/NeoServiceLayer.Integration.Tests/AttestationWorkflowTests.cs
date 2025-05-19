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
    public class AttestationWorkflowTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public AttestationWorkflowTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAttestationProof_ReturnsProof()
        {
            // Act
            var response = await _client.GetAsync("/api/attestation/proof");

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<ApiResponse<AttestationProofResponse>>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(responseObj);
            Assert.True(responseObj.Success);
            Assert.NotNull(responseObj.Data);
            Assert.NotNull(responseObj.Data.Id);
            Assert.NotNull(responseObj.Data.Report);
            Assert.NotNull(responseObj.Data.Signature);
            Assert.NotNull(responseObj.Data.MrEnclave);
            Assert.NotNull(responseObj.Data.MrSigner);
            Assert.NotNull(responseObj.Data.ProductId);
            Assert.NotNull(responseObj.Data.SecurityVersion);
            Assert.NotNull(responseObj.Data.Attributes);
            Assert.NotEqual(default, responseObj.Data.CreatedAt);
            Assert.NotEqual(default, responseObj.Data.ExpiresAt);
        }

        [Fact]
        public async System.Threading.Tasks.Task VerifyAttestationProof_ValidProof_ReturnsVerificationResult()
        {
            // Arrange
            // First get an attestation proof
            var proofResponse = await _client.GetAsync("/api/attestation/proof");
            proofResponse.EnsureSuccessStatusCode();

            var proofResponseString = await proofResponse.Content.ReadAsStringAsync();
            var proofResponseObj = JsonSerializer.Deserialize<ApiResponse<AttestationProofResponse>>(proofResponseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var proof = proofResponseObj.Data;

            // Now verify the proof
            var verifyRequest = new
            {
                Id = proof.Id,
                Report = proof.Report,
                Signature = proof.Signature,
                MrEnclave = proof.MrEnclave,
                MrSigner = proof.MrSigner,
                ProductId = proof.ProductId,
                SecurityVersion = proof.SecurityVersion,
                Attributes = proof.Attributes,
                CreatedAt = proof.CreatedAt,
                ExpiresAt = proof.ExpiresAt
            };

            var verifyContent = new StringContent(JsonSerializer.Serialize(verifyRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/attestation/verify", verifyContent);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<ApiResponse<VerifyAttestationResponse>>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(responseObj);
            Assert.True(responseObj.Success);
            Assert.NotNull(responseObj.Data);
            Assert.True(responseObj.Data.IsValid);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetEnclaveIdentity_ReturnsIdentity()
        {
            // Act
            var response = await _client.GetAsync("/api/attestation/identity");

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<ApiResponse<EnclaveIdentityResponse>>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(responseObj);
            Assert.True(responseObj.Success);
            Assert.NotNull(responseObj.Data);
            Assert.NotNull(responseObj.Data.EnclaveId);
            Assert.NotNull(responseObj.Data.MrEnclave);
            Assert.NotNull(responseObj.Data.MrSigner);
            Assert.True(responseObj.Data.IsSimulationMode);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetEnclaveStatus_ReturnsStatus()
        {
            // Act
            var response = await _client.GetAsync("/api/attestation/status");

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<ApiResponse<EnclaveStatusResponse>>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(responseObj);
            Assert.True(responseObj.Success);
            Assert.NotNull(responseObj.Data);
            Assert.Equal("Running", responseObj.Data.Status);
            Assert.True(responseObj.Data.IsInitialized);
            Assert.True(responseObj.Data.IsSimulationMode);
            Assert.NotNull(responseObj.Data.Version);
            Assert.NotNull(responseObj.Data.MrEnclave);
            Assert.NotNull(responseObj.Data.MrSigner);
            Assert.NotNull(responseObj.Data.EnclaveId);
        }
    }

    // Helper classes for deserialization
    public class AttestationProofResponse
    {
        public string Id { get; set; }
        public string Report { get; set; }
        public string Signature { get; set; }
        public string MrEnclave { get; set; }
        public string MrSigner { get; set; }
        public string ProductId { get; set; }
        public string SecurityVersion { get; set; }
        public string Attributes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class VerifyAttestationResponse
    {
        public bool IsValid { get; set; }
        public string Reason { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class EnclaveIdentityResponse
    {
        public string EnclaveId { get; set; }
        public string MrEnclave { get; set; }
        public string MrSigner { get; set; }
        public bool IsSimulationMode { get; set; }
    }

    public class EnclaveStatusResponse
    {
        public string Status { get; set; }
        public bool IsInitialized { get; set; }
        public bool IsSimulationMode { get; set; }
        public string Version { get; set; }
        public string MrEnclave { get; set; }
        public string MrSigner { get; set; }
        public string EnclaveId { get; set; }
    }
}
