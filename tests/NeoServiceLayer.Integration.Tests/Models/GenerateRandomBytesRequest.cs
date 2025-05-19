using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Integration.Tests.Models
{
    /// <summary>
    /// Represents a request to generate random bytes.
    /// </summary>
    public class GenerateRandomBytesRequest
    {
        /// <summary>
        /// Gets or sets the number of bytes to generate.
        /// </summary>
        [Required]
        [Range(1, 1024)]
        public int Length { get; set; }

        /// <summary>
        /// Gets or sets the seed for the random number generator.
        /// </summary>
        public string Seed { get; set; }
    }
}
