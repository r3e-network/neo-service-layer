using System.Collections.Concurrent;

namespace NeoServiceLayer.Performance.Tests.Infrastructure
{
    /// <summary>
    /// Provides test data for performance benchmarks.
    /// </summary>
    public static class PerformanceTestData
    {
        private static readonly Random _random = new(42); // Fixed seed for reproducible results

        /// <summary>
        /// Gets sample contract hashes for testing.
        /// </summary>
        public static readonly string[] ContractHashes = [
            "0xd2a4cff31913016155e38e474a2c06d08be276cf",
            "0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5",
            "0x70e2301955bf1e74cbb31d18c2f96972abadb328",
            "0x48c40d4666f93408be1bef038b6722404d9a4c2a",
            "0x24ad28be3b76d02bf3eff34138b9b6c3e6d41b5f"
        ];

        /// <summary>
        /// Gets sample voting proposal IDs for testing.
        /// </summary>
        public static readonly string[] ProposalIds = [
            "proposal-001",
            "proposal-002", 
            "proposal-003",
            "proposal-004",
            "proposal-005"
        ];

        /// <summary>
        /// Gets sample node IDs for testing.
        /// </summary>
        public static readonly string[] NodeIds = [
            "node-001",
            "node-002",
            "node-003",
            "node-004",
            "node-005"
        ];

        /// <summary>
        /// Generates random time series data for pattern recognition testing.
        /// </summary>
        /// <param name="size">Number of data points to generate.</param>
        /// <returns>Array of time series data.</returns>
        public static double[] GenerateTimeSeriesData(int size)
        {
            var data = new double[size];
            var baseValue = 100.0;
            var trend = 0.01;
            var volatility = 0.1;

            for (int i = 0; i < size; i++)
            {
                var noise = (_random.NextDouble() - 0.5) * volatility;
                var trendComponent = trend * i;
                data[i] = baseValue + trendComponent + noise;
            }

            return data;
        }

        /// <summary>
        /// Generates random pattern data with anomalies.
        /// </summary>
        /// <param name="size">Number of data points to generate.</param>
        /// <param name="anomalyRate">Rate of anomalies (0.0 to 1.0).</param>
        /// <returns>Array of pattern data with anomalies.</returns>
        public static double[] GenerateAnomalyData(int size, double anomalyRate = 0.05)
        {
            var data = GenerateTimeSeriesData(size);
            var anomalyCount = (int)(size * anomalyRate);

            for (int i = 0; i < anomalyCount; i++)
            {
                var index = _random.Next(size);
                var multiplier = _random.NextDouble() > 0.5 ? 3.0 : -3.0;
                data[index] *= multiplier;
            }

            return data;
        }

        /// <summary>
        /// Generates sample cache keys for testing.
        /// </summary>
        /// <param name="count">Number of cache keys to generate.</param>
        /// <returns>Array of cache keys.</returns>
        public static string[] GenerateCacheKeys(int count)
        {
            var keys = new string[count];
            
            for (int i = 0; i < count; i++)
            {
                var keyType = _random.Next(4);
                keys[i] = keyType switch
                {
                    0 => $"contract:metadata:{Guid.NewGuid():N}",
                    1 => $"voting:result:{Guid.NewGuid():N}",
                    2 => $"health:node:{Guid.NewGuid():N}",
                    _ => $"pattern:analysis:{Guid.NewGuid():N}"
                };
            }

            return keys;
        }

        /// <summary>
        /// Generates sample JSON data for caching tests.
        /// </summary>
        /// <param name="size">Approximate size of the JSON data in bytes.</param>
        /// <returns>Sample object for caching.</returns>
        public static SampleCacheData GenerateCacheData(int size = 1024)
        {
            var propertyCount = Math.Max(1, size / 100); // Rough estimate
            var properties = new Dictionary<string, object>();

            for (int i = 0; i < propertyCount; i++)
            {
                properties[$"property_{i}"] = GenerateRandomString(50);
            }

            return new SampleCacheData
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Properties = properties,
                Data = GenerateRandomString(Math.Max(0, size - (propertyCount * 60)))
            };
        }

        /// <summary>
        /// Generates concurrent cache operations for load testing.
        /// </summary>
        /// <param name="operationCount">Number of operations to generate.</param>
        /// <returns>Collection of cache operations.</returns>
        public static IEnumerable<CacheOperation> GenerateConcurrentCacheOperations(int operationCount)
        {
            var operations = new ConcurrentBag<CacheOperation>();
            
            Parallel.For(0, operationCount, i =>
            {
                var operationType = _random.Next(3);
                var key = $"concurrent:test:{i}";
                
                var operation = operationType switch
                {
                    0 => new CacheOperation { Type = "GET", Key = key },
                    1 => new CacheOperation { Type = "SET", Key = key, Data = GenerateCacheData(512) },
                    _ => new CacheOperation { Type = "REMOVE", Key = key }
                };
                
                operations.Add(operation);
            });

            return operations;
        }

        /// <summary>
        /// Generates automation job data for testing.
        /// </summary>
        /// <param name="jobCount">Number of jobs to generate.</param>
        /// <returns>Array of job configurations.</returns>
        public static JobConfiguration[] GenerateJobConfigurations(int jobCount)
        {
            var jobs = new JobConfiguration[jobCount];
            
            for (int i = 0; i < jobCount; i++)
            {
                jobs[i] = new JobConfiguration
                {
                    Id = $"job-{i:D4}",
                    Name = $"Test Job {i}",
                    Schedule = "0 */5 * * * *", // Every 5 minutes
                    Conditions = GenerateConditions(_random.Next(1, 5)),
                    Actions = GenerateActions(_random.Next(1, 3))
                };
            }

            return jobs;
        }

        private static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        private static Dictionary<string, object> GenerateConditions(int count)
        {
            var conditions = new Dictionary<string, object>();
            
            for (int i = 0; i < count; i++)
            {
                conditions[$"condition_{i}"] = new
                {
                    Type = "threshold",
                    Value = _random.NextDouble() * 100,
                    Operator = _random.Next(2) == 0 ? "greater_than" : "less_than"
                };
            }

            return conditions;
        }

        private static Dictionary<string, object> GenerateActions(int count)
        {
            var actions = new Dictionary<string, object>();
            
            for (int i = 0; i < count; i++)
            {
                actions[$"action_{i}"] = new
                {
                    Type = "notification",
                    Target = $"target-{i}",
                    Message = $"Action {i} triggered"
                };
            }

            return actions;
        }
    }

    /// <summary>
    /// Sample cache data for testing.
    /// </summary>
    public class SampleCacheData
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public string Data { get; set; } = string.Empty;
    }

    /// <summary>
    /// Cache operation for concurrent testing.
    /// </summary>
    public class CacheOperation
    {
        public string Type { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    /// <summary>
    /// Job configuration for automation testing.
    /// </summary>
    public class JobConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty;
        public Dictionary<string, object> Conditions { get; set; } = new();
        public Dictionary<string, object> Actions { get; set; } = new();
    }
}