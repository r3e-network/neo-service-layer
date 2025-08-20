using System;
using System.Text.Json;

class TestDebug
{
    static void Main()
    {
        // Simulate what the mock returns for generation
        var generateResult = JsonSerializer.Serialize(new
        {
            success = true,
            result = new
            {
                statement = "test_statement",
                commitment = "0x1234567890abcdef",
                challenge = "0xfedcba0987654321",
                response = "0xabcdef1234567890",
                proofId = Guid.NewGuid().ToString(),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            }
        });
        
        Console.WriteLine("Generate Result:");
        Console.WriteLine(generateResult);
        
        // Parse it to get the result
        var parsed = JsonSerializer.Deserialize<JsonElement>(generateResult);
        var proofResult = parsed.GetProperty("result");
        
        // This is what gets serialized as the proof
        var proofData = JsonSerializer.Serialize(new
        {
            statement = proofResult.GetProperty("statement"),
            commitment = proofResult.GetProperty("commitment").GetString(),
            challenge = proofResult.GetProperty("challenge").GetString(),
            response = proofResult.GetProperty("response").GetString(),
            proofId = proofResult.GetProperty("proofId").GetString(),
            timestamp = proofResult.GetProperty("timestamp").GetInt64()
        });
        
        Console.WriteLine("\nProof Data that would be stored:");
        Console.WriteLine(proofData);
        
        // This is what ProofData bytes would be
        var proofDataBytes = System.Text.Encoding.UTF8.GetBytes(proofData);
        Console.WriteLine($"\nProof Data byte length: {proofDataBytes.Length}");
        
        // Now for verification, we'd parse it back
        var proofString = System.Text.Encoding.UTF8.GetString(proofDataBytes);
        var proof = JsonSerializer.Deserialize<JsonElement>(proofString);
        
        Console.WriteLine("\nParsed proof for verification:");
        Console.WriteLine($"Has commitment: {proof.TryGetProperty("commitment", out _)}");
        Console.WriteLine($"Has challenge: {proof.TryGetProperty("challenge", out _)}");
        Console.WriteLine($"Has response: {proof.TryGetProperty("response", out _)}");
    }
}