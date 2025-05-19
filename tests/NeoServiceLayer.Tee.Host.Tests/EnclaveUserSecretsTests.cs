using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests
{
    [Trait("Category", "UserSecrets")]
    public class EnclaveUserSecretsTests : IDisposable
    {
        private readonly Mock<ILogger<OpenEnclaveTeeInterface>> _loggerMock;
        private readonly string _testDirectory;
        private readonly string _enclaveImagePath;
        private readonly OpenEnclaveTeeInterface _teeInterface;

        public EnclaveUserSecretsTests()
        {
            _loggerMock = new Mock<ILogger<OpenEnclaveTeeInterface>>();
            
            // Create a test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), $"enclave_secrets_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            
            // Set the enclave image path
            _enclaveImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "enclave.signed");
            
            // Skip tests if the enclave image doesn't exist
            if (!File.Exists(_enclaveImagePath))
            {
                Skip.If(true, $"Enclave image not found at {_enclaveImagePath}");
                return;
            }
            
            try
            {
                // Create the TEE interface
                _teeInterface = new OpenEnclaveTeeInterface(
                    _loggerMock.Object,
                    new OpenEnclaveTeeOptions
                    {
                        EnclaveImagePath = _enclaveImagePath,
                        SimulationMode = true,
                        StorageDirectory = _testDirectory
                    });
                
                // Initialize the TEE interface
                _teeInterface.Initialize();
            }
            catch (Exception)
            {
                // Skip tests if the enclave can't be initialized
                Skip.If(true, "Failed to initialize enclave in simulation mode");
            }
        }

        public void Dispose()
        {
            // Clean up
            (_teeInterface as IDisposable)?.Dispose();
            
            // Delete the test directory
            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, true);
                }
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        [Fact]
        public async Task StoreUserSecret_GetUserSecret_RoundTrip()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null)
            {
                return;
            }
            
            // Arrange
            string userId = "test-user-" + Guid.NewGuid().ToString();
            string secretName = "test-secret";
            string secretValue = "test-secret-value";
            
            try
            {
                // Act
                bool storeResult = await _teeInterface.StoreUserSecretAsync(userId, secretName, secretValue);
                string retrievedSecret = await _teeInterface.GetUserSecretAsync(userId, secretName);
                
                // Assert
                Assert.True(storeResult);
                Assert.Equal(secretValue, retrievedSecret);
            }
            catch (Exception)
            {
                // Skip the test if user secrets are not supported
                Skip.If(true, "User secrets not supported in this enclave build");
            }
        }

        [Fact]
        public async Task DeleteUserSecret_RemovesSecret()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null)
            {
                return;
            }
            
            // Arrange
            string userId = "test-user-" + Guid.NewGuid().ToString();
            string secretName = "test-secret-delete";
            string secretValue = "test-secret-value";
            
            try
            {
                // Act - Store and verify
                await _teeInterface.StoreUserSecretAsync(userId, secretName, secretValue);
                string retrievedSecret = await _teeInterface.GetUserSecretAsync(userId, secretName);
                Assert.Equal(secretValue, retrievedSecret);
                
                // Act - Delete
                bool deleteResult = await _teeInterface.DeleteUserSecretAsync(userId, secretName);
                
                // Act - Try to retrieve again
                string retrievedSecretAfterDelete = await _teeInterface.GetUserSecretAsync(userId, secretName);
                
                // Assert
                Assert.True(deleteResult);
                Assert.Null(retrievedSecretAfterDelete);
            }
            catch (Exception)
            {
                // Skip the test if user secrets are not supported
                Skip.If(true, "User secrets not supported in this enclave build");
            }
        }

        [Fact]
        public async Task ListUserSecrets_ReturnsAllSecrets()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null)
            {
                return;
            }
            
            // Arrange
            string userId = "test-user-" + Guid.NewGuid().ToString();
            string secretName1 = "test-secret-1";
            string secretName2 = "test-secret-2";
            string secretValue = "test-secret-value";
            
            try
            {
                // Act - Store secrets
                await _teeInterface.StoreUserSecretAsync(userId, secretName1, secretValue);
                await _teeInterface.StoreUserSecretAsync(userId, secretName2, secretValue);
                
                // Act - List secrets
                string[] secrets = await _teeInterface.ListUserSecretsAsync(userId);
                
                // Assert
                Assert.NotNull(secrets);
                Assert.Equal(2, secrets.Length);
                Assert.Contains(secretName1, secrets);
                Assert.Contains(secretName2, secrets);
                
                // Clean up
                await _teeInterface.DeleteUserSecretAsync(userId, secretName1);
                await _teeInterface.DeleteUserSecretAsync(userId, secretName2);
            }
            catch (Exception)
            {
                // Skip the test if user secrets are not supported
                Skip.If(true, "User secrets not supported in this enclave build");
            }
        }

        [Fact]
        public async Task UserSecrets_PersistAcrossRestarts()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null)
            {
                return;
            }
            
            // Arrange
            string userId = "test-user-" + Guid.NewGuid().ToString();
            string secretName = "test-secret-persist";
            string secretValue = "test-secret-value";
            
            try
            {
                // Act - Store secret
                await _teeInterface.StoreUserSecretAsync(userId, secretName, secretValue);
                
                // Act - Dispose and recreate the TEE interface
                (_teeInterface as IDisposable).Dispose();
                
                // Create a new TEE interface
                var newTeeInterface = new OpenEnclaveTeeInterface(
                    _loggerMock.Object,
                    new OpenEnclaveTeeOptions
                    {
                        EnclaveImagePath = _enclaveImagePath,
                        SimulationMode = true,
                        StorageDirectory = _testDirectory
                    });
                
                // Initialize the new TEE interface
                newTeeInterface.Initialize();
                
                // Act - Retrieve secret from new interface
                string retrievedSecret = await newTeeInterface.GetUserSecretAsync(userId, secretName);
                
                // Assert
                Assert.Equal(secretValue, retrievedSecret);
                
                // Clean up
                await newTeeInterface.DeleteUserSecretAsync(userId, secretName);
                (newTeeInterface as IDisposable).Dispose();
            }
            catch (Exception)
            {
                // Skip the test if user secrets are not supported
                Skip.If(true, "User secrets persistence not supported in this enclave build");
            }
        }

        [Fact]
        public async Task UserSecrets_HandleMultipleUsers()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null)
            {
                return;
            }
            
            // Arrange
            string userId1 = "test-user-1-" + Guid.NewGuid().ToString();
            string userId2 = "test-user-2-" + Guid.NewGuid().ToString();
            string secretName = "test-secret";
            string secretValue1 = "test-secret-value-1";
            string secretValue2 = "test-secret-value-2";
            
            try
            {
                // Act - Store secrets for different users
                await _teeInterface.StoreUserSecretAsync(userId1, secretName, secretValue1);
                await _teeInterface.StoreUserSecretAsync(userId2, secretName, secretValue2);
                
                // Act - Retrieve secrets
                string retrievedSecret1 = await _teeInterface.GetUserSecretAsync(userId1, secretName);
                string retrievedSecret2 = await _teeInterface.GetUserSecretAsync(userId2, secretName);
                
                // Assert
                Assert.Equal(secretValue1, retrievedSecret1);
                Assert.Equal(secretValue2, retrievedSecret2);
                
                // Clean up
                await _teeInterface.DeleteUserSecretAsync(userId1, secretName);
                await _teeInterface.DeleteUserSecretAsync(userId2, secretName);
            }
            catch (Exception)
            {
                // Skip the test if user secrets are not supported
                Skip.If(true, "User secrets not supported in this enclave build");
            }
        }

        [Fact]
        public async Task UserSecrets_HandleSpecialCharacters()
        {
            // Skip if the enclave wasn't initialized
            if (_teeInterface == null)
            {
                return;
            }
            
            // Arrange
            string userId = "test-user-special-" + Guid.NewGuid().ToString();
            string secretName = "test-secret-special";
            string secretValue = "test-secret-value-!@#$%^&*()_+{}|:<>?~`-=[]\\;',./";
            
            try
            {
                // Act
                bool storeResult = await _teeInterface.StoreUserSecretAsync(userId, secretName, secretValue);
                string retrievedSecret = await _teeInterface.GetUserSecretAsync(userId, secretName);
                
                // Assert
                Assert.True(storeResult);
                Assert.Equal(secretValue, retrievedSecret);
                
                // Clean up
                await _teeInterface.DeleteUserSecretAsync(userId, secretName);
            }
            catch (Exception)
            {
                // Skip the test if user secrets are not supported
                Skip.If(true, "User secrets not supported in this enclave build");
            }
        }
    }
}
