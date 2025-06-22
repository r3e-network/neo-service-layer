using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Web.Models;

namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// Demo controller with simulated service responses.
/// </summary>
[Tags("Demo")]
public class DemoController : BaseApiController
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DemoController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public DemoController(ILogger<DemoController> logger) : base(logger)
    {
    }

    /// <summary>
    /// Simulates key generation for demonstration.
    /// </summary>
    /// <param name="request">The key generation request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Simulated key generation response.</returns>
    [HttpPost("keymanagement/generate/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> SimulateKeyGeneration(
        [FromBody] SimulateKeyRequest request,
        [FromRoute] string blockchainType)
    {
        await Task.Delay(500); // Simulate processing time

        var result = new
        {
            KeyId = request.KeyId,
            KeyType = request.KeyType,
            KeyUsage = request.KeyUsage,
            PublicKey = $"04{GenerateRandomHex(64)}",
            Created = DateTime.UtcNow,
            Status = "Active",
            Blockchain = blockchainType,
            Enclave = "SGX Protected",
            Fingerprint = GenerateRandomHex(20)
        };

        Logger.LogInformation("Simulated key generation for {KeyId} on {Blockchain}",
            request.KeyId, blockchainType);

        return Ok(CreateResponse(result, "Key generated successfully"));
    }

    /// <summary>
    /// Simulates listing keys for demonstration.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Simulated key list response.</returns>
    [HttpGet("keymanagement/list/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,KeyUser")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> SimulateListKeys([FromRoute] string blockchainType)
    {
        await Task.Delay(300); // Simulate processing time

        var keys = new[]
        {
            new
            {
                KeyId = "key_demo_001",
                KeyType = "ECDSA",
                KeyUsage = "Signing",
                Created = DateTime.UtcNow.AddDays(-5),
                Status = "Active",
                PublicKey = $"04{GenerateRandomHex(64)}"
            },
            new
            {
                KeyId = "key_demo_002",
                KeyType = "Ed25519",
                KeyUsage = "Encryption",
                Created = DateTime.UtcNow.AddDays(-3),
                Status = "Active",
                PublicKey = $"04{GenerateRandomHex(32)}"
            },
            new
            {
                KeyId = "key_demo_003",
                KeyType = "RSA2048",
                KeyUsage = "KeyExchange",
                Created = DateTime.UtcNow.AddDays(-1),
                Status = "Rotating",
                PublicKey = $"04{GenerateRandomHex(128)}"
            }
        };

        var result = new
        {
            Keys = keys,
            Total = keys.Length,
            Blockchain = blockchainType,
            Timestamp = DateTime.UtcNow
        };

        return Ok(CreateResponse(result, "Keys retrieved successfully"));
    }

    /// <summary>
    /// Simulates random number generation.
    /// </summary>
    /// <param name="request">The random number request.</param>
    /// <returns>Simulated random number response.</returns>
    [HttpPost("randomness/generate-number")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> SimulateRandomNumber([FromBody] RandomNumberRequest request)
    {
        await Task.Delay(200); // Simulate processing time

        var random = new Random();
        var randomNumber = random.Next(request.Min, request.Max + 1);

        var result = new
        {
            Value = randomNumber,
            Min = request.Min,
            Max = request.Max,
            Entropy = GenerateRandomHex(16),
            Generated = DateTime.UtcNow,
            Source = "SGX Hardware Random Number Generator"
        };

        Logger.LogInformation("Generated random number: {Number}", randomNumber);

        return Ok(CreateResponse(result, "Random number generated successfully"));
    }

    /// <summary>
    /// Simulates random bytes generation.
    /// </summary>
    /// <param name="request">The random bytes request.</param>
    /// <returns>Simulated random bytes response.</returns>
    [HttpPost("randomness/generate-bytes")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> SimulateRandomBytes([FromBody] RandomBytesRequest request)
    {
        await Task.Delay(200); // Simulate processing time

        var result = new
        {
            Bytes = GenerateRandomHex(request.Length),
            Length = request.Length,
            Format = "Hexadecimal",
            Generated = DateTime.UtcNow,
            Source = "SGX Hardware Random Number Generator",
            Entropy = "256-bit"
        };

        Logger.LogInformation("Generated {Length} random bytes", request.Length);

        return Ok(CreateResponse(result, "Random bytes generated successfully"));
    }

    /// <summary>
    /// Simulates service health check.
    /// </summary>
    /// <returns>Simulated health status.</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> SimulateHealthCheck()
    {
        await Task.Delay(100); // Simulate processing time

        var result = new
        {
            Status = "Healthy",
            Services = new
            {
                KeyManagement = "Online",
                Randomness = "Online",
                SGXEnclave = "Active",
                Database = "Connected",
                Cache = "Available"
            },
            Timestamp = DateTime.UtcNow,
            Uptime = TimeSpan.FromHours(48.5).ToString(),
            Version = "1.0.0",
            Environment = "Development"
        };

        return Ok(result);
    }

    private string GenerateRandomHex(int length)
    {
        var random = new Random();
        var bytes = new byte[length];
        random.NextBytes(bytes);
        return Convert.ToHexString(bytes).ToLower();
    }
}

#region Request Models

public class SimulateKeyRequest
{
    [Required]
    public string KeyId { get; set; } = string.Empty;

    [Required]
    public string KeyType { get; set; } = string.Empty;

    [Required]
    public string KeyUsage { get; set; } = string.Empty;

    public bool Exportable { get; set; }

    public string Description { get; set; } = string.Empty;
}

public class RandomNumberRequest
{
    [Range(int.MinValue, int.MaxValue)]
    public int Min { get; set; } = 1;

    [Range(int.MinValue, int.MaxValue)]
    public int Max { get; set; } = 1000000;
}

public class RandomBytesRequest
{
    [Range(1, 1024)]
    public int Length { get; set; } = 32;
}

#endregion
