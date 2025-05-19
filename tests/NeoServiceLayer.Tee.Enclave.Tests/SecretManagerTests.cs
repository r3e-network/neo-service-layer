using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Enclave;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "OpenEnclave")]
    [Collection("SimulationMode")]
    public class SecretManagerTests
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;

        public SecretManagerTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<SecretManagerTests>();
            
            _logger.LogInformation("Test initialized with {UsingRealSdk}", 
                _fixture.UsingRealSdk ? "real SDK" : "mock implementation");
        }

        [Fact]
        public async Task SecretManager_ShouldStoreAndRetrieveSecrets()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string userId = "test-user-" + Guid.NewGuid().ToString();
            string secretName = "test-secret";
            string secretValue = "This is a test secret value";
            
            // Act - Store secret
            bool storeResult = await _fixture.TeeInterface.StoreUserSecretAsync(userId, secretName, secretValue);
            
            // Act - Retrieve secret
            string retrievedSecret = await _fixture.TeeInterface.GetUserSecretAsync(userId, secretName);
            
            // Act - Get secret names
            var secretNames = await _fixture.TeeInterface.GetUserSecretNamesAsync(userId);
            
            // Act - Delete secret
            bool deleteResult = await _fixture.TeeInterface.DeleteUserSecretAsync(userId, secretName);
            
            // Act - Try to retrieve deleted secret
            string deletedSecret = await _fixture.TeeInterface.GetUserSecretAsync(userId, secretName);
            
            // Assert
            Assert.True(storeResult, "Storing secret should succeed");
            Assert.Equal(secretValue, retrievedSecret);
            Assert.Contains(secretName, secretNames);
            Assert.True(deleteResult, "Deleting secret should succeed");
            Assert.Empty(deletedSecret);
            
            _logger.LogInformation("Secret test passed for user {UserId}", userId);
        }

        [Fact]
        public async Task SecretManager_ShouldHandleMultipleSecrets()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string userId = "test-user-multi-" + Guid.NewGuid().ToString();
            int numSecrets = 5;
            
            // Act - Store multiple secrets
            for (int i = 0; i < numSecrets; i++)
            {
                string secretName = $"test-secret-{i}";
                string secretValue = $"This is test secret value {i}";
                bool storeResult = await _fixture.TeeInterface.StoreUserSecretAsync(userId, secretName, secretValue);
                Assert.True(storeResult, $"Storing secret {secretName} should succeed");
            }
            
            // Act - Get secret names
            var secretNames = await _fixture.TeeInterface.GetUserSecretNamesAsync(userId);
            
            // Act - Retrieve and verify all secrets
            for (int i = 0; i < numSecrets; i++)
            {
                string secretName = $"test-secret-{i}";
                string expectedValue = $"This is test secret value {i}";
                string retrievedSecret = await _fixture.TeeInterface.GetUserSecretAsync(userId, secretName);
                Assert.Equal(expectedValue, retrievedSecret);
            }
            
            // Act - Delete all secrets
            for (int i = 0; i < numSecrets; i++)
            {
                string secretName = $"test-secret-{i}";
                bool deleteResult = await _fixture.TeeInterface.DeleteUserSecretAsync(userId, secretName);
                Assert.True(deleteResult, $"Deleting secret {secretName} should succeed");
            }
            
            // Act - Get secret names after deletion
            var secretNamesAfterDeletion = await _fixture.TeeInterface.GetUserSecretNamesAsync(userId);
            
            // Assert
            Assert.Equal(numSecrets, secretNames.Count);
            Assert.Empty(secretNamesAfterDeletion);
            
            _logger.LogInformation("Multiple secrets test passed for user {UserId}", userId);
        }

        [Fact]
        public async Task SecretManager_ShouldHandleMultipleUsers()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            int numUsers = 3;
            string[] userIds = new string[numUsers];
            for (int i = 0; i < numUsers; i++)
            {
                userIds[i] = $"test-user-{i}-{Guid.NewGuid()}";
            }
            
            // Act - Store secrets for each user
            for (int i = 0; i < numUsers; i++)
            {
                string secretName = "test-secret";
                string secretValue = $"This is test secret value for user {i}";
                bool storeResult = await _fixture.TeeInterface.StoreUserSecretAsync(userIds[i], secretName, secretValue);
                Assert.True(storeResult, $"Storing secret for user {userIds[i]} should succeed");
            }
            
            // Act - Retrieve and verify secrets for each user
            for (int i = 0; i < numUsers; i++)
            {
                string secretName = "test-secret";
                string expectedValue = $"This is test secret value for user {i}";
                string retrievedSecret = await _fixture.TeeInterface.GetUserSecretAsync(userIds[i], secretName);
                Assert.Equal(expectedValue, retrievedSecret);
            }
            
            // Act - Delete secrets for each user
            for (int i = 0; i < numUsers; i++)
            {
                string secretName = "test-secret";
                bool deleteResult = await _fixture.TeeInterface.DeleteUserSecretAsync(userIds[i], secretName);
                Assert.True(deleteResult, $"Deleting secret for user {userIds[i]} should succeed");
            }
            
            _logger.LogInformation("Multiple users test passed for {NumUsers} users", numUsers);
        }

        [Fact]
        public async Task SecretManager_ShouldHandlePersistence()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string userId = "test-user-persist-" + Guid.NewGuid().ToString();
            string secretName = "test-secret-persist";
            string secretValue = "This is a persistent test secret value";
            
            // Act - Store secret
            bool storeResult = await _fixture.TeeInterface.StoreUserSecretAsync(userId, secretName, secretValue);
            
            // Act - Force save to persistent storage
            bool saveResult = await _fixture.TeeInterface.SaveSecretsToPersistentStorageAsync();
            
            // Act - Force load from persistent storage
            bool loadResult = await _fixture.TeeInterface.LoadSecretsFromPersistentStorageAsync();
            
            // Act - Retrieve secret after loading
            string retrievedSecret = await _fixture.TeeInterface.GetUserSecretAsync(userId, secretName);
            
            // Act - Clean up
            await _fixture.TeeInterface.DeleteUserSecretAsync(userId, secretName);
            
            // Assert
            Assert.True(storeResult, "Storing secret should succeed");
            Assert.True(saveResult, "Saving to persistent storage should succeed");
            Assert.True(loadResult, "Loading from persistent storage should succeed");
            Assert.Equal(secretValue, retrievedSecret);
            
            _logger.LogInformation("Persistence test passed for user {UserId}", userId);
        }
    }
}
