using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Services;
using NeoServiceLayer.Shared.Models;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace NeoServiceLayer.Infrastructure.Tests.Services
{
    public class RandomnessServiceTests
    {
        private readonly Mock<NeoServiceLayer.Tee.Host.Services.ITeeHostService> _mockTeeHostService;
        private readonly Mock<ILogger<RandomnessService>> _mockLogger;
        private readonly RandomnessService _randomnessService;

        public RandomnessServiceTests()
        {
            _mockTeeHostService = new Mock<NeoServiceLayer.Tee.Host.Services.ITeeHostService>();
            _mockLogger = new Mock<ILogger<RandomnessService>>();

            // Create an adapter that implements Core.Interfaces.ITeeHostService
            var teeHostServiceAdapter = new Mocks.TeeHostServiceAdapter(_mockTeeHostService.Object);

            _randomnessService = new RandomnessService(teeHostServiceAdapter, _mockLogger.Object);
        }

        [Fact]
        public async Task GenerateRandomNumberAsync_ValidRange_ReturnsNumberInRange()
        {
            // Arrange
            int min = 1;
            int max = 100;
            string seed = "test-seed";
            int expectedNumber = 42;

            var teeResponse = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.Randomness,
                Data = JsonSerializer.Serialize(new
                {
                    number = expectedNumber,
                    proof = "random-proof-data"
                }),
                CreatedAt = DateTime.UtcNow
            };

            _mockTeeHostService.Setup(x => x.SendMessageAsync(It.IsAny<TeeMessage>()))
                .ReturnsAsync(teeResponse);

            // Act
            var result = await _randomnessService.GenerateRandomNumberAsync(min, max, seed);

            // Assert
            Assert.Equal(expectedNumber, result);

            // Verify TEE host service was called with correct parameters
            _mockTeeHostService.Verify(x => x.SendMessageAsync(It.Is<TeeMessage>(m =>
                m.Type == TeeMessageType.Randomness &&
                m.Data.Contains("generate_random_number") &&
                m.Data.Contains(min.ToString()) &&
                m.Data.Contains(max.ToString()) &&
                m.Data.Contains(seed))),
                Times.Once);
        }

        [Fact]
        public async Task GenerateRandomNumberAsync_InvalidRange_ThrowsArgumentException()
        {
            // Arrange
            int min = 100;
            int max = 1; // min > max

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _randomnessService.GenerateRandomNumberAsync(min, max));
        }

        [Fact]
        public async Task GenerateRandomNumbersAsync_ValidParameters_ReturnsNumbersInRange()
        {
            // Arrange
            int count = 5;
            int min = 1;
            int max = 100;
            string seed = "test-seed";
            int[] expectedNumbers = new[] { 42, 17, 76, 23, 91 };

            var teeResponse = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.Randomness,
                Data = JsonSerializer.Serialize(new
                {
                    numbers = expectedNumbers,
                    proof = "random-proof-data"
                }),
                CreatedAt = DateTime.UtcNow
            };

            _mockTeeHostService.Setup(x => x.SendMessageAsync(It.IsAny<TeeMessage>()))
                .ReturnsAsync(teeResponse);

            // Act
            var result = await _randomnessService.GenerateRandomNumbersAsync(count, min, max, seed);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(count, result.Count());
            Assert.Equal(expectedNumbers, result);

            // Verify TEE host service was called with correct parameters
            _mockTeeHostService.Verify(x => x.SendMessageAsync(It.Is<TeeMessage>(m =>
                m.Type == TeeMessageType.Randomness &&
                m.Data.Contains("generate_random_numbers") &&
                m.Data.Contains(count.ToString()) &&
                m.Data.Contains(min.ToString()) &&
                m.Data.Contains(max.ToString()) &&
                m.Data.Contains(seed))),
                Times.Once);
        }

        [Fact]
        public async Task GenerateRandomNumbersAsync_InvalidCount_ThrowsArgumentException()
        {
            // Arrange
            int count = 0; // count <= 0
            int min = 1;
            int max = 100;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _randomnessService.GenerateRandomNumbersAsync(count, min, max));
        }

        [Fact]
        public async Task GenerateRandomNumbersAsync_InvalidRange_ThrowsArgumentException()
        {
            // Arrange
            int count = 5;
            int min = 100;
            int max = 1; // min > max

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _randomnessService.GenerateRandomNumbersAsync(count, min, max));
        }

        [Fact]
        public async Task GenerateRandomStringAsync_ValidParameters_ReturnsStringOfCorrectLength()
        {
            // Arrange
            int length = 10;
            string charset = "ABC123";
            string seed = "test-seed";
            string expectedString = "A1B2C3A1B2";

            var teeResponse = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.Randomness,
                Data = JsonSerializer.Serialize(new
                {
                    randomString = expectedString,
                    proof = "random-proof-data"
                }),
                CreatedAt = DateTime.UtcNow
            };

            _mockTeeHostService.Setup(x => x.SendMessageAsync(It.IsAny<TeeMessage>()))
                .ReturnsAsync(teeResponse);

            // Act
            var result = await _randomnessService.GenerateRandomStringAsync(length, charset, seed);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(length, result.Length);
            Assert.Equal(expectedString, result);

            // Verify TEE host service was called with correct parameters
            _mockTeeHostService.Verify(x => x.SendMessageAsync(It.Is<TeeMessage>(m =>
                m.Type == TeeMessageType.Randomness &&
                m.Data.Contains("generate_random_string") &&
                m.Data.Contains(length.ToString()) &&
                m.Data.Contains(charset) &&
                m.Data.Contains(seed))),
                Times.Once);
        }

        [Fact]
        public async Task GenerateRandomStringAsync_InvalidLength_ThrowsArgumentException()
        {
            // Arrange
            int length = 0; // length <= 0
            string charset = "ABC123";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _randomnessService.GenerateRandomStringAsync(length, charset));
        }

        [Fact]
        public async Task GenerateRandomBytesAsync_ValidLength_ReturnsBytesOfCorrectLength()
        {
            // Arrange
            int length = 32;
            string seed = "test-seed";
            byte[] expectedBytes = new byte[length];
            new Random(42).NextBytes(expectedBytes);

            var teeResponse = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.Randomness,
                Data = JsonSerializer.Serialize(new
                {
                    bytes = Convert.ToBase64String(expectedBytes),
                    proof = "random-proof-data"
                }),
                CreatedAt = DateTime.UtcNow
            };

            _mockTeeHostService.Setup(x => x.SendMessageAsync(It.IsAny<TeeMessage>()))
                .ReturnsAsync(teeResponse);

            // Act
            var result = await _randomnessService.GenerateRandomBytesAsync(length, seed);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(length, result.Length);
            Assert.Equal(expectedBytes, result);

            // Verify TEE host service was called with correct parameters
            _mockTeeHostService.Verify(x => x.SendMessageAsync(It.Is<TeeMessage>(m =>
                m.Type == TeeMessageType.Randomness &&
                m.Data.Contains("generate_random_bytes") &&
                m.Data.Contains(length.ToString()) &&
                m.Data.Contains(seed))),
                Times.Once);
        }

        [Fact]
        public async Task GenerateRandomBytesAsync_InvalidLength_ThrowsArgumentException()
        {
            // Arrange
            int length = 0; // length <= 0
            string seed = "test-seed";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _randomnessService.GenerateRandomBytesAsync(length, seed));
        }

        [Fact]
        public async Task VerifyRandomnessProofAsync_ValidProof_ReturnsTrue()
        {
            // Arrange
            int[] randomNumbers = new[] { 42, 17, 76, 23, 91 };
            string proof = "valid-proof-data";
            string seed = "test-seed";

            var teeResponse = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.Randomness,
                Data = JsonSerializer.Serialize(new
                {
                    valid = true
                }),
                CreatedAt = DateTime.UtcNow
            };

            _mockTeeHostService.Setup(x => x.SendMessageAsync(It.IsAny<TeeMessage>()))
                .ReturnsAsync(teeResponse);

            // Act
            var result = await _randomnessService.VerifyRandomnessProofAsync(randomNumbers, proof, seed);

            // Assert
            Assert.True(result);

            // Verify TEE host service was called with correct parameters
            _mockTeeHostService.Verify(x => x.SendMessageAsync(It.Is<TeeMessage>(m =>
                m.Type == TeeMessageType.Randomness &&
                m.Data.Contains("verify_randomness_proof") &&
                m.Data.Contains(proof) &&
                m.Data.Contains(seed))),
                Times.Once);
        }
    }
}
