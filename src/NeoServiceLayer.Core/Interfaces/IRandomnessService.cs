using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Interface for the randomness service.
    /// </summary>
    public interface IRandomnessService
    {
        /// <summary>
        /// Generates a random number.
        /// </summary>
        /// <param name="min">The minimum value (inclusive).</param>
        /// <param name="max">The maximum value (inclusive).</param>
        /// <param name="seed">Optional seed for deterministic randomness.</param>
        /// <returns>A random number between min and max.</returns>
        Task<int> GenerateRandomNumberAsync(int min, int max, string? seed = null);

        /// <summary>
        /// Generates multiple random numbers.
        /// </summary>
        /// <param name="count">The number of random numbers to generate.</param>
        /// <param name="min">The minimum value (inclusive).</param>
        /// <param name="max">The maximum value (inclusive).</param>
        /// <param name="seed">Optional seed for deterministic randomness.</param>
        /// <returns>A list of random numbers between min and max.</returns>
        Task<IEnumerable<int>> GenerateRandomNumbersAsync(int count, int min, int max, string? seed = null);

        /// <summary>
        /// Generates random bytes.
        /// </summary>
        /// <param name="length">The number of bytes to generate.</param>
        /// <param name="seed">Optional seed for deterministic randomness.</param>
        /// <returns>A byte array of random bytes.</returns>
        Task<byte[]> GenerateRandomBytesAsync(int length, string? seed = null);

        /// <summary>
        /// Verifies the randomness proof.
        /// </summary>
        /// <param name="randomNumbers">The random numbers to verify.</param>
        /// <param name="proof">The proof to verify.</param>
        /// <param name="seed">The seed used to generate the random numbers.</param>
        /// <returns>True if the proof is valid, false otherwise.</returns>
        Task<bool> VerifyRandomnessProofAsync(IEnumerable<int> randomNumbers, string proof, string? seed = null);

        /// <summary>
        /// Generates a random string.
        /// </summary>
        /// <param name="length">The length of the string to generate.</param>
        /// <param name="charset">The character set to use.</param>
        /// <param name="seed">Optional seed for deterministic randomness.</param>
        /// <returns>A random string.</returns>
        Task<string> GenerateRandomStringAsync(int length, string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", string? seed = null);

        /// <summary>
        /// Signs data using the TEE's private key.
        /// </summary>
        /// <param name="data">The data to sign.</param>
        /// <returns>The signature.</returns>
        Task<byte[]> SignDataAsync(byte[] data);
    }
}
