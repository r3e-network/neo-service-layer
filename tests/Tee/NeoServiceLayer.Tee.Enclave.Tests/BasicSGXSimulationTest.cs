using System;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    /// <summary>
    /// Basic SGX simulation tests focusing on core functionality.
    /// </summary>
    public class BasicSGXSimulationTest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly SGXSimulationEnclaveWrapper _enclave;

        public BasicSGXSimulationTest(ITestOutputHelper output)
        {
            _output = output;
            _enclave = new SGXSimulationEnclaveWrapper();
        }

        public void Dispose()
        {
            _enclave?.Dispose();
        }

        [Fact]
        public void SGXSimulation_Initialize_ShouldSucceed()
        {
            // Act
            var result = _enclave.Initialize();

            // Assert
            result.Should().BeTrue();
            _output.WriteLine("✅ SGX Simulation initialization successful");
        }

        [Fact]
        public void SGXSimulation_CryptographicOperations_ShouldWork()
        {
            // Arrange
            _enclave.Initialize();
            var testData = Encoding.UTF8.GetBytes("Test data for encryption");
            var key = _enclave.GenerateRandomBytes(32);

            // Act
            var encrypted = _enclave.Encrypt(testData, key);
            var decrypted = _enclave.Decrypt(encrypted, key);
            var signature = _enclave.Sign(testData, key);
            var isValid = _enclave.Verify(testData, signature, key);

            // Assert
            encrypted.Should().NotEqual(testData);
            decrypted.Should().Equal(testData);
            signature.Should().NotBeNull();
            isValid.Should().BeTrue();

            _output.WriteLine("✅ Cryptographic operations successful");
            _output.WriteLine($"   Original: {testData.Length} bytes");
            _output.WriteLine($"   Encrypted: {encrypted.Length} bytes");
            _output.WriteLine($"   Signature: {signature.Length} bytes");
        }

        [Fact]
        public void SGXSimulation_RandomGeneration_ShouldProvideQualityRandomness()
        {
            // Arrange
            _enclave.Initialize();

            // Act
            var random1 = _enclave.GenerateRandom(1, 100);
            var random2 = _enclave.GenerateRandom(1, 100);
            var randomBytes1 = _enclave.GenerateRandomBytes(32);
            var randomBytes2 = _enclave.GenerateRandomBytes(32);

            // Assert
            random1.Should().BeInRange(1, 100);
            random2.Should().BeInRange(1, 100);
            randomBytes1.Should().HaveCount(32);
            randomBytes2.Should().HaveCount(32);
            randomBytes1.Should().NotEqual(randomBytes2);

            _output.WriteLine("✅ Random generation successful");
            _output.WriteLine($"   Random numbers: {random1}, {random2}");
            _output.WriteLine($"   Random bytes unique: {!randomBytes1.SequenceEqual(randomBytes2)}");
        }

        [Fact]
        public void SGXSimulation_SecureStorage_ShouldStoreAndRetrieveData()
        {
            // Arrange
            _enclave.Initialize();
            var key = "test-storage-key";
            var data = Encoding.UTF8.GetBytes("Sensitive test data");
            var encryptionKey = "encryption-key";

            // Act
            var storeResult = _enclave.StoreData(key, data, encryptionKey, false);
            var retrievedData = _enclave.RetrieveData(key, encryptionKey);
            var metadata = _enclave.GetStorageMetadata(key);

            // Assert
            storeResult.Should().NotBeNullOrEmpty();
            retrievedData.Should().Equal(data);
            metadata.Should().NotBeNullOrEmpty();

            var storeJson = JsonDocument.Parse(storeResult);
            storeJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();

            _output.WriteLine("✅ Secure storage successful");
            _output.WriteLine($"   Store result: {storeResult}");
            _output.WriteLine($"   Retrieved {retrievedData.Length} bytes");
        }

        [Fact]
        public void SGXSimulation_JavaScriptExecution_ShouldWork()
        {
            // Arrange
            _enclave.Initialize();
            var jsCode = "function add(a, b) { return a + b; }";
            var args = """{"a": 10, "b": 20}""";

            // Act
            var result = _enclave.ExecuteJavaScript(jsCode, args);

            // Assert
            result.Should().NotBeNullOrEmpty();
            var resultJson = JsonDocument.Parse(result);
            resultJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();

            _output.WriteLine("✅ JavaScript execution successful");
            _output.WriteLine($"   Result: {result}");
        }

        [Fact]
        public void SGXSimulation_KeyGeneration_ShouldCreateKeys()
        {
            // Arrange
            _enclave.Initialize();
            var keyId = "test-key";
            var keyType = "secp256k1";

            // Act
            var keyResult = _enclave.GenerateKey(keyId, keyType, "Sign,Verify", true, "Test key");

            // Assert
            keyResult.Should().NotBeNullOrEmpty();
            var keyJson = JsonDocument.Parse(keyResult);
            keyJson.RootElement.GetProperty("keyId").GetString().Should().Be(keyId);
            keyJson.RootElement.GetProperty("enclaveGenerated").GetBoolean().Should().BeTrue();

            _output.WriteLine("✅ Key generation successful");
            _output.WriteLine($"   Key result: {keyResult}");
        }

        [Fact]
        public void SGXSimulation_AttestationReport_ShouldProvideEnclaveIdentity()
        {
            // Arrange
            _enclave.Initialize();

            // Act
            var attestationReport = _enclave.GetAttestationReport();

            // Assert
            attestationReport.Should().NotBeNullOrEmpty();
            var reportJson = JsonDocument.Parse(attestationReport);
            reportJson.RootElement.GetProperty("simulation_mode").GetBoolean().Should().BeTrue();

            _output.WriteLine("✅ Attestation report successful");
            _output.WriteLine($"   Report: {attestationReport}");
        }

        [Fact]
        public void SGXSimulation_AIOperations_ShouldTrainAndPredict()
        {
            // Arrange
            _enclave.Initialize();
            var modelId = "test-model";
            var trainingData = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
            var inputData = new double[] { 2.5, 3.5 };

            // Act
            var trainingResult = _enclave.TrainAIModel(modelId, "linear_regression", trainingData, "{}");
            var predictions = _enclave.PredictWithAIModel(modelId, inputData, out string metadata);

            // Assert
            trainingResult.Should().NotBeNullOrEmpty();
            predictions.Should().NotBeNull();
            predictions.Length.Should().Be(inputData.Length);
            metadata.Should().NotBeNullOrEmpty();

            _output.WriteLine("✅ AI operations successful");
            _output.WriteLine($"   Training result: {trainingResult}");
            _output.WriteLine($"   Predictions: [{string.Join(", ", predictions)}]");
            _output.WriteLine($"   Metadata: {metadata}");
        }

        [Fact]
        public void SGXSimulation_AbstractAccounts_ShouldCreateAndSign()
        {
            // Arrange
            _enclave.Initialize();
            var accountId = "test-account";
            var accountData = """{"type": "test"}""";
            var transactionData = """{"to": "test", "amount": 100}""";

            // Act
            var accountResult = _enclave.CreateAbstractAccount(accountId, accountData);
            var signResult = _enclave.SignAbstractAccountTransaction(accountId, transactionData);

            // Assert
            accountResult.Should().NotBeNullOrEmpty();
            signResult.Should().NotBeNullOrEmpty();

            var accountJson = JsonDocument.Parse(accountResult);
            accountJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();

            var signJson = JsonDocument.Parse(signResult);
            signJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();

            _output.WriteLine("✅ Abstract account operations successful");
            _output.WriteLine($"   Account result: {accountResult}");
            _output.WriteLine($"   Sign result: {signResult}");
        }
    }
}
