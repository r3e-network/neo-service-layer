using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Randomness;

/// <summary>
/// Interface for the Randomness service.
/// </summary>
public interface IRandomnessService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Generates a random number.
    /// </summary>
    /// <param name="min">The minimum value (inclusive).</param>
    /// <param name="max">The maximum value (inclusive).</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>A random number between min and max (inclusive).</returns>
    Task<int> GenerateRandomNumberAsync(int min, int max, BlockchainType blockchainType);

    /// <summary>
    /// Generates random bytes.
    /// </summary>
    /// <param name="length">The number of bytes to generate.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Random bytes.</returns>
    Task<byte[]> GenerateRandomBytesAsync(int length, BlockchainType blockchainType);

    /// <summary>
    /// Generates a random string.
    /// </summary>
    /// <param name="length">The length of the string.</param>
    /// <param name="charset">The character set to use. If null, uses alphanumeric characters.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>A random string.</returns>
    Task<string> GenerateRandomStringAsync(int length, string? charset, BlockchainType blockchainType);

    /// <summary>
    /// Generates a verifiable random number between min and max (inclusive).
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="seed">The seed for the random number generation.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>A verifiable random result containing the random number and proof.</returns>
    Task<VerifiableRandomResult> GenerateVerifiableRandomNumberAsync(int min, int max, string seed, BlockchainType blockchainType);

    /// <summary>
    /// Verifies a random number generation.
    /// </summary>
    /// <param name="result">The verifiable random result to verify.</param>
    /// <returns>True if the result is valid, false otherwise.</returns>
    Task<bool> VerifyRandomNumberAsync(VerifiableRandomResult result);
}

/// <summary>
/// Represents a randomness verification result.
/// </summary>
public class RandomnessVerificationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the verification was successful.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the request.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the block height at which the randomness was generated.
    /// </summary>
    public long BlockHeight { get; set; }

    /// <summary>
    /// Gets or sets the block hash at which the randomness was generated.
    /// </summary>
    public string BlockHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the verification message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Represents a randomness request.
/// </summary>
public class RandomnessRequest
{
    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the request.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the block height at which the randomness was generated.
    /// </summary>
    public long BlockHeight { get; set; }

    /// <summary>
    /// Gets or sets the block hash at which the randomness was generated.
    /// </summary>
    public string BlockHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seed used for the randomness generation.
    /// </summary>
    public string Seed { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the result of the randomness generation.
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signature of the result.
    /// </summary>
    public string Signature { get; set; } = string.Empty;
}
