using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host;
using Xunit;

namespace NeoServiceLayer.Tee.Enclave.Tests
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
        /// <returns>An OpenEnclaveInterface instance.</returns>
        public static Tee.Host.OpenEnclaveInterface CreateOpenEnclaveInterface(SimulationModeFixture fixture)
        {
            return fixture.TeeInterface as Tee.Host.OpenEnclaveInterface;
        }

        /// <summary>
        /// Creates a JavaScriptEngine instance for testing.
        /// </summary>
        /// <param name="fixture">The simulation mode fixture.</param>
        /// <returns>A JavaScriptEngine instance.</returns>
        public static JavaScriptEngine CreateJavaScriptEngine(SimulationModeFixture fixture)
        {
            var logger = fixture.LoggerFactory.CreateLogger<JavaScriptEngine>();
            var gasAccountingManager = new GasAccountingManager(
                fixture.LoggerFactory.CreateLogger<GasAccountingManager>());

            return new MockJavaScriptEngine(logger, gasAccountingManager, fixture.SgxInterface);
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
                            return { result: fibonacci(n) };
                        }
                    ";
                case 5:
                    return @"
                        function main(input) {
                            const data = input.data || [20, 40, 60, 80, 100];

                            // Calculate statistics
                            const sum = data.reduce((a, b) => a + b, 0);
                            const avg = sum / data.length;
                            const max = Math.max(...data);
                            const min = Math.min(...data);

                            // Process each item
                            const processed = data.map(item => {
                                return {
                                    original: item,
                                    squared: item * item,
                                    sqrt: Math.sqrt(item),
                                    normalized: (item - min) / (max - min)
                                };
                            });

                            return {
                                statistics: {
                                    count: data.length,
                                    sum: sum,
                                    average: avg,
                                    max: max,
                                    min: min
                                },
                                processed: processed
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
                    return @"{ ""data"": [20, 40, 60, 80, 100] }";
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
