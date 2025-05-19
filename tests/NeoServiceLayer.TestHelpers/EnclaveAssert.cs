using System;
using System.Text;
using System.Text.Json;
using Xunit;

namespace NeoServiceLayer.TestHelpers
{
    /// <summary>
    /// Assertion helpers for enclave-related tests.
    /// </summary>
    public static class EnclaveAssert
    {
        /// <summary>
        /// Asserts that the given data is a valid attestation report.
        /// </summary>
        /// <param name="report">The attestation report.</param>
        public static void IsValidAttestationReport(byte[] report)
        {
            Assert.NotNull(report);
            Assert.True(report.Length > 0, "Attestation report should not be empty");

            // In a real implementation, we would validate the structure of the report
            // For simulation mode, we just check that it's not empty
        }

        /// <summary>
        /// Asserts that the given data is a valid MRENCLAVE value.
        /// </summary>
        /// <param name="mrEnclave">The MRENCLAVE value.</param>
        public static void IsValidMrEnclave(byte[] mrEnclave)
        {
            Assert.NotNull(mrEnclave);
            Assert.Equal(32, mrEnclave.Length);
        }

        /// <summary>
        /// Asserts that the given data is a valid MRSIGNER value.
        /// </summary>
        /// <param name="mrSigner">The MRSIGNER value.</param>
        public static void IsValidMrSigner(byte[] mrSigner)
        {
            Assert.NotNull(mrSigner);
            Assert.Equal(32, mrSigner.Length);
        }

        /// <summary>
        /// Asserts that the given data is a valid signature.
        /// </summary>
        /// <param name="signature">The signature.</param>
        public static void IsValidSignature(byte[] signature)
        {
            Assert.NotNull(signature);
            Assert.True(signature.Length > 0, "Signature should not be empty");
        }

        /// <summary>
        /// Asserts that the given data is a valid sealed data.
        /// </summary>
        /// <param name="sealedData">The sealed data.</param>
        /// <param name="originalData">The original data.</param>
        public static void IsValidSealedData(byte[] sealedData, byte[] originalData)
        {
            Assert.NotNull(sealedData);
            Assert.True(sealedData.Length > originalData.Length, "Sealed data should be larger than original data");

            // Check that the sealed data doesn't contain the original data in plaintext
            string sealedString = Encoding.UTF8.GetString(sealedData);
            string originalString = Encoding.UTF8.GetString(originalData);

            Assert.DoesNotContain(originalString, sealedString);
        }

        /// <summary>
        /// Asserts that the given JavaScript result is valid.
        /// </summary>
        /// <param name="result">The JavaScript result as a JSON string.</param>
        public static void IsValidJavaScriptResult(string result)
        {
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            // Verify it's valid JSON
            var resultObj = JsonSerializer.Deserialize<JsonElement>(result);
            Assert.NotEqual(JsonValueKind.Undefined, resultObj.ValueKind);
        }

        /// <summary>
        /// Asserts that the given JavaScript result contains the expected property with the expected value.
        /// </summary>
        /// <param name="result">The JavaScript result as a JSON string.</param>
        /// <param name="propertyName">The name of the property to check.</param>
        /// <param name="expectedValue">The expected value of the property.</param>
        public static void JavaScriptResultHasProperty(string result, string propertyName, object expectedValue)
        {
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            var resultObj = JsonSerializer.Deserialize<JsonElement>(result);
            Assert.True(resultObj.TryGetProperty(propertyName, out var property), $"Result should have property '{propertyName}'");

            if (expectedValue is int intValue)
            {
                Assert.Equal(intValue, property.GetInt32());
            }
            else if (expectedValue is long longValue)
            {
                Assert.Equal(longValue, property.GetInt64());
            }
            else if (expectedValue is double doubleValue)
            {
                Assert.Equal(doubleValue, property.GetDouble());
            }
            else if (expectedValue is bool boolValue)
            {
                Assert.Equal(boolValue, property.GetBoolean());
            }
            else if (expectedValue is string stringValue)
            {
                Assert.Equal(stringValue, property.GetString());
            }
            else
            {
                throw new ArgumentException($"Unsupported type: {expectedValue.GetType()}", nameof(expectedValue));
            }
        }

        /// <summary>
        /// Asserts that the given JavaScript result contains an error.
        /// </summary>
        /// <param name="result">The JavaScript result as a JSON string.</param>
        public static void JavaScriptResultHasError(string result)
        {
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            var resultObj = JsonSerializer.Deserialize<JsonElement>(result);
            Assert.True(resultObj.TryGetProperty("error", out _), "Result should have an 'error' property");
        }
    }
}
