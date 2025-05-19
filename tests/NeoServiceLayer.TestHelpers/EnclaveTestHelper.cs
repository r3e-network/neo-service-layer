using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host;
using Xunit;

namespace NeoServiceLayer.TestHelpers
{
    /// <summary>
    /// Helper class for enclave-related tests.
    /// </summary>
    public static class EnclaveTestHelper
    {
        /// <summary>
        /// Creates an OpenEnclaveInterface instance for testing.
        /// </summary>
        /// <param name="fixture">The simulation mode fixture.</param>
        /// <param name="skipIfEnclaveNotFound">Whether to skip the test if the enclave is not found.</param>
        /// <returns>An OpenEnclaveInterface instance.</returns>
        public static OpenEnclaveInterface CreateOpenEnclaveInterface(
            SimulationModeFixture fixture,
            bool skipIfEnclaveNotFound = true)
        {
            var logger = fixture.LoggerFactory.CreateLogger<OpenEnclaveInterface>();

            try
            {
                // Check if the enclave file exists
                if (!File.Exists(fixture.EnclavePath))
                {
                    if (skipIfEnclaveNotFound)
                    {
                        throw new SkipException($"Skipping test because enclave not found at {fixture.EnclavePath}");
                    }

                    throw new FileNotFoundException($"Enclave not found at {fixture.EnclavePath}", fixture.EnclavePath);
                }

                return new OpenEnclaveInterface(logger, fixture.EnclavePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create Open Enclave interface");

                if (skipIfEnclaveNotFound)
                {
                    throw new SkipException($"Skipping test because enclave could not be created: {ex.Message}");
                }

                throw;
            }
        }

        /// <summary>
        /// Checks if the enclave file exists and skips the test if it doesn't.
        /// </summary>
        /// <param name="fixture">The simulation mode fixture.</param>
        /// <param name="skipIfEnclaveNotFound">Whether to skip the test if the enclave is not found.</param>
        public static void CheckEnclaveExists(
            SimulationModeFixture fixture,
            bool skipIfEnclaveNotFound = true)
        {
            var logger = fixture.LoggerFactory.CreateLogger<SimulationModeFixture>();

            try
            {
                // Check if the enclave file exists
                if (!File.Exists(fixture.EnclavePath))
                {
                    if (skipIfEnclaveNotFound)
                    {
                        throw new SkipException($"Skipping test because enclave not found at {fixture.EnclavePath}");
                    }

                    throw new FileNotFoundException($"Enclave not found at {fixture.EnclavePath}", fixture.EnclavePath);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check enclave file");

                if (skipIfEnclaveNotFound)
                {
                    throw new SkipException($"Skipping test because enclave could not be found: {ex.Message}");
                }

                throw;
            }
        }

        /// <summary>
        /// Generates random data for testing.
        /// </summary>
        /// <param name="length">The length of the data to generate.</param>
        /// <returns>Random data.</returns>
        public static byte[] GenerateRandomData(int length)
        {
            var data = new byte[length];
            new Random().NextBytes(data);
            return data;
        }

        /// <summary>
        /// Creates a sample JavaScript function for testing.
        /// </summary>
        /// <param name="complexity">The complexity of the function (1-5).</param>
        /// <returns>A JavaScript function as a string.</returns>
        public static string CreateSampleJavaScriptFunction(int complexity = 1)
        {
            switch (complexity)
            {
                case 1:
                    return @"
                        function main(input) {
                            return { result: input.value * 2 };
                        }
                    ";
                case 2:
                    return @"
                        function main(input) {
                            let result = 0;
                            for (let i = 0; i < input.iterations; i++) {
                                result += i;
                            }
                            return { result: result };
                        }
                    ";
                case 3:
                    return @"
                        function main(input) {
                            // Use a secret if available
                            const apiKey = SECRETS && SECRETS.API_KEY ? SECRETS.API_KEY : 'no-key';

                            // Process the input
                            let result = {};
                            if (input.operation === 'add') {
                                result.value = input.a + input.b;
                            } else if (input.operation === 'multiply') {
                                result.value = input.a * input.b;
                            } else {
                                throw new Error('Unsupported operation: ' + input.operation);
                            }

                            result.apiKey = apiKey;
                            return result;
                        }
                    ";
                case 4:
                    return @"
                        function fibonacci(n) {
                            if (n <= 1) return n;
                            return fibonacci(n - 1) + fibonacci(n - 2);
                        }

                        function main(input) {
                            const n = input.n || 10;
                            const result = fibonacci(n);
                            return { result: result };
                        }
                    ";
                case 5:
                    return @"
                        function processData(data) {
                            return data.map(item => {
                                return {
                                    id: item.id,
                                    value: item.value * 2,
                                    processed: true
                                };
                            });
                        }

                        function calculateStatistics(data) {
                            let sum = 0;
                            let min = Number.MAX_VALUE;
                            let max = Number.MIN_VALUE;

                            for (const item of data) {
                                sum += item.value;
                                min = Math.min(min, item.value);
                                max = Math.max(max, item.value);
                            }

                            return {
                                count: data.length,
                                sum: sum,
                                average: sum / data.length,
                                min: min,
                                max: max
                            };
                        }

                        function main(input) {
                            const processedData = processData(input.data);
                            const stats = calculateStatistics(processedData);

                            return {
                                processedData: processedData,
                                statistics: stats
                            };
                        }
                    ";
                default:
                    throw new ArgumentOutOfRangeException(nameof(complexity), "Complexity must be between 1 and 5");
            }
        }

        /// <summary>
        /// Creates sample input for a JavaScript function.
        /// </summary>
        /// <param name="complexity">The complexity of the input (1-5).</param>
        /// <returns>A JSON string representing the input.</returns>
        public static string CreateSampleJavaScriptInput(int complexity = 1)
        {
            switch (complexity)
            {
                case 1:
                    return @"{ ""value"": 42 }";
                case 2:
                    return @"{ ""iterations"": 1000 }";
                case 3:
                    return @"{ ""operation"": ""add"", ""a"": 5, ""b"": 7 }";
                case 4:
                    return @"{ ""n"": 10 }";
                case 5:
                    return @"{ ""data"": [
                        { ""id"": 1, ""value"": 10 },
                        { ""id"": 2, ""value"": 20 },
                        { ""id"": 3, ""value"": 30 },
                        { ""id"": 4, ""value"": 40 },
                        { ""id"": 5, ""value"": 50 }
                    ]}";
                default:
                    throw new ArgumentOutOfRangeException(nameof(complexity), "Complexity must be between 1 and 5");
            }
        }

        /// <summary>
        /// Creates sample secrets for a JavaScript function.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A JSON string representing the secrets.</returns>
        public static string CreateSampleSecrets(string userId)
        {
            return $@"{{
                ""API_KEY"": ""test_api_key_for_{userId}"",
                ""DB_CONNECTION"": ""test_db_connection_for_{userId}"",
                ""AUTH_TOKEN"": ""test_auth_token_for_{userId}""
            }}";
        }
    }
}
