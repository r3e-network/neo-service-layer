namespace NeoServiceLayer.Performance.Tests.Infrastructure
{
    /// <summary>
    /// Simple sample cache data for performance testing.
    /// </summary>
    public class SimpleSampleCacheData
    {
        public string Id { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();

        /// <summary>
        /// Generates sample cache data with specified size.
        /// </summary>
        /// <param name="dataSize">Size of data in bytes.</param>
        /// <returns>Sample cache data.</returns>
        public static SimpleSampleCacheData Generate(int dataSize)
        {
            var data = new string('x', Math.Max(1, dataSize - 100)); // Reserve space for other properties

            return new SimpleSampleCacheData
            {
                Id = Guid.NewGuid().ToString(),
                Data = data,
                CreatedAt = DateTime.UtcNow,
                Properties = new Dictionary<string, object>
                {
                    ["Size"] = dataSize,
                    ["Generated"] = DateTime.UtcNow,
                    ["Type"] = "Benchmark"
                }
            };
        }
    }
}
