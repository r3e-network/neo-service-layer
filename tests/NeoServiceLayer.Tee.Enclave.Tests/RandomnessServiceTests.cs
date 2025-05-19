using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Enclave;
using Xunit;
using Xunit.Abstractions;
using System.Text.Json;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "OpenEnclave")]
    [Collection("SimulationMode")]
    public class RandomnessServiceTests
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;

        public RandomnessServiceTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<RandomnessServiceTests>();
            
            _logger.LogInformation("Test initialized with {UsingRealSdk}", 
                _fixture.UsingRealSdk ? "real SDK" : "mock implementation");
        }

        [Fact]
        public async Task RandomnessService_ShouldGenerateAndVerifyRandomNumber()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            ulong min = 1;
            ulong max = 100;
            string userId = "test-user";
            string requestId = Guid.NewGuid().ToString();
            
            // Act - Generate random number
            ulong randomNumber = await _fixture.TeeInterface.GenerateRandomNumberAsync(min, max, userId, requestId);
            
            // Act - Get proof
            string proof = await _fixture.TeeInterface.GetRandomNumberProofAsync(randomNumber, min, max, userId, requestId);
            
            // Act - Verify random number
            bool isValid = await _fixture.TeeInterface.VerifyRandomNumberAsync(randomNumber, min, max, userId, requestId, proof);
            
            // Assert
            Assert.InRange(randomNumber, min, max);
            Assert.NotEmpty(proof);
            Assert.True(isValid);
            
            _logger.LogInformation("Random number: {RandomNumber}", randomNumber);
            _logger.LogInformation("Proof: {Proof}", proof);
        }

        [Fact]
        public async Task RandomnessService_ShouldGenerateAndVerifyRandomBytes()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            int length = 32;
            string userId = "test-user";
            string requestId = Guid.NewGuid().ToString();
            
            // Act - Generate random bytes
            byte[] randomBytes = await _fixture.TeeInterface.GenerateRandomBytesAsync(length, userId, requestId);
            
            // Act - Get proof
            string proof = await _fixture.TeeInterface.GetRandomBytesProofAsync(randomBytes, userId, requestId);
            
            // Act - Verify random bytes
            bool isValid = await _fixture.TeeInterface.VerifyRandomBytesAsync(randomBytes, userId, requestId, proof);
            
            // Assert
            Assert.Equal(length, randomBytes.Length);
            Assert.NotEmpty(proof);
            Assert.True(isValid);
            
            _logger.LogInformation("Random bytes: {RandomBytes}", Convert.ToBase64String(randomBytes));
            _logger.LogInformation("Proof: {Proof}", proof);
        }

        [Fact]
        public async Task RandomnessService_ShouldGenerateAndVerifySeed()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string userId = "test-user";
            string requestId = Guid.NewGuid().ToString();
            
            // Act - Generate seed
            string seed = await _fixture.TeeInterface.GenerateSeedAsync(userId, requestId);
            
            // Act - Get proof
            string proof = await _fixture.TeeInterface.GetSeedProofAsync(seed, userId, requestId);
            
            // Act - Verify seed
            bool isValid = await _fixture.TeeInterface.VerifySeedAsync(seed, userId, requestId, proof);
            
            // Assert
            Assert.NotEmpty(seed);
            Assert.NotEmpty(proof);
            Assert.True(isValid);
            
            _logger.LogInformation("Seed: {Seed}", seed);
            _logger.LogInformation("Proof: {Proof}", proof);
        }

        [Fact]
        public async Task RandomnessService_ShouldDetectTamperedRandomNumber()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            ulong min = 1;
            ulong max = 100;
            string userId = "test-user";
            string requestId = Guid.NewGuid().ToString();
            
            // Act - Generate random number
            ulong randomNumber = await _fixture.TeeInterface.GenerateRandomNumberAsync(min, max, userId, requestId);
            
            // Act - Get proof
            string proof = await _fixture.TeeInterface.GetRandomNumberProofAsync(randomNumber, min, max, userId, requestId);
            
            // Act - Verify tampered random number
            ulong tamperedNumber = randomNumber + 1;
            bool isValid = await _fixture.TeeInterface.VerifyRandomNumberAsync(tamperedNumber, min, max, userId, requestId, proof);
            
            // Assert
            Assert.False(isValid);
            
            _logger.LogInformation("Original random number: {RandomNumber}", randomNumber);
            _logger.LogInformation("Tampered random number: {TamperedNumber}", tamperedNumber);
        }
    }
}
