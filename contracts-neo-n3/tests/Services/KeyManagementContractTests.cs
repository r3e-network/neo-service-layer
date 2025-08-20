using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Testing;
using NeoServiceLayer.Contracts.Services;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Tests.Services
{
    [TestClass]
    public class KeyManagementContractTests : ContractTestFramework
    {
        private KeyManagementContract keyManagementContract;
        private UInt160 testOwner;

        [TestInitialize]
        public void Setup()
        {
            keyManagementContract = new KeyManagementContract();
            testOwner = UInt160.Parse("0x1234567890123456789012345678901234567890");
            
            // Deploy contract
            keyManagementContract._deploy(null, false);
        }

        [TestMethod]
        public void TestGenerateKey_ValidParameters_Success()
        {
            // Arrange
            var keyType = KeyManagementContract.KeyType.ECDSA;
            var purpose = KeyManagementContract.KeyPurpose.Signing;
            var keyStrength = 256;
            var metadata = "Test key for signing";

            // Act
            var keyId = keyManagementContract.GenerateKey(testOwner, keyType, purpose, keyStrength, metadata);

            // Assert
            Assert.IsNotNull(keyId);
            Assert.IsTrue(keyId.Length > 0);

            var keyRecord = keyManagementContract.GetKeyRecord(keyId);
            Assert.IsNotNull(keyRecord);
            Assert.AreEqual(testOwner, keyRecord.Owner);
            Assert.AreEqual(keyType, keyRecord.Type);
            Assert.AreEqual(purpose, keyRecord.Purpose);
            Assert.AreEqual(keyStrength, keyRecord.Strength);
            Assert.AreEqual(KeyManagementContract.KeyStatus.Active, keyRecord.Status);
            Assert.AreEqual(metadata, keyRecord.Metadata);
        }

        [TestMethod]
        public void TestGenerateKey_InvalidKeyStrength_ThrowsException()
        {
            // Arrange
            var keyType = KeyManagementContract.KeyType.ECDSA;
            var purpose = KeyManagementContract.KeyPurpose.Signing;
            var invalidKeyStrength = 64; // Below minimum
            var metadata = "Test key";

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                keyManagementContract.GenerateKey(testOwner, keyType, purpose, invalidKeyStrength, metadata));
        }

        [TestMethod]
        public void TestDeriveChildKey_ValidParent_Success()
        {
            // Arrange
            var parentKeyId = keyManagementContract.GenerateKey(
                testOwner, 
                KeyManagementContract.KeyType.ECDSA, 
                KeyManagementContract.KeyPurpose.KeyDerivation, 
                256, 
                "Parent key");
            
            var derivationIndex = 1;
            var childPurpose = KeyManagementContract.KeyPurpose.Signing;
            var metadata = "Child key for signing";

            // Act
            var childKeyId = keyManagementContract.DeriveChildKey(parentKeyId, derivationIndex, childPurpose, metadata);

            // Assert
            Assert.IsNotNull(childKeyId);
            Assert.AreNotEqual(parentKeyId, childKeyId);

            var childKeyRecord = keyManagementContract.GetKeyRecord(childKeyId);
            Assert.IsNotNull(childKeyRecord);
            Assert.AreEqual(testOwner, childKeyRecord.Owner);
            Assert.AreEqual(childPurpose, childKeyRecord.Purpose);
            Assert.AreEqual(KeyManagementContract.KeyStatus.Active, childKeyRecord.Status);
        }

        [TestMethod]
        public void TestDeriveChildKey_NonExistentParent_ThrowsException()
        {
            // Arrange
            var nonExistentKeyId = new byte[] { 0x01, 0x02, 0x03 };
            var derivationIndex = 1;
            var childPurpose = KeyManagementContract.KeyPurpose.Signing;

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
                keyManagementContract.DeriveChildKey(nonExistentKeyId, derivationIndex, childPurpose, "metadata"));
        }

        [TestMethod]
        public void TestRotateKey_ActiveKey_Success()
        {
            // Arrange
            var originalKeyId = keyManagementContract.GenerateKey(
                testOwner, 
                KeyManagementContract.KeyType.ECDSA, 
                KeyManagementContract.KeyPurpose.Signing, 
                256, 
                "Original key");
            
            var reason = KeyManagementContract.RotationReason.Manual;
            var justification = "Security policy rotation";

            // Act
            var newKeyId = keyManagementContract.RotateKey(originalKeyId, reason, justification);

            // Assert
            Assert.IsNotNull(newKeyId);
            Assert.AreNotEqual(originalKeyId, newKeyId);

            var originalKeyRecord = keyManagementContract.GetKeyRecord(originalKeyId);
            Assert.AreEqual(KeyManagementContract.KeyStatus.Rotated, originalKeyRecord.Status);
            Assert.AreEqual(newKeyId, originalKeyRecord.RotatedTo);

            var newKeyRecord = keyManagementContract.GetKeyRecord(newKeyId);
            Assert.AreEqual(KeyManagementContract.KeyStatus.Active, newKeyRecord.Status);
            Assert.AreEqual(testOwner, newKeyRecord.Owner);
        }

        [TestMethod]
        public void TestRotateKey_NonActiveKey_ThrowsException()
        {
            // Arrange
            var keyId = keyManagementContract.GenerateKey(
                testOwner, 
                KeyManagementContract.KeyType.ECDSA, 
                KeyManagementContract.KeyPurpose.Signing, 
                256, 
                "Test key");
            
            // Revoke the key first
            keyManagementContract.RevokeKey(keyId, "Test revocation");

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
                keyManagementContract.RotateKey(keyId, KeyManagementContract.RotationReason.Manual, "Test"));
        }

        [TestMethod]
        public void TestConfigureMultiSig_ValidParameters_Success()
        {
            // Arrange
            var participants = new UInt160[]
            {
                UInt160.Parse("0x1111111111111111111111111111111111111111"),
                UInt160.Parse("0x2222222222222222222222222222222222222222"),
                UInt160.Parse("0x3333333333333333333333333333333333333333")
            };
            var threshold = 2;

            // Act
            var result = keyManagementContract.ConfigureMultiSig(testOwner, participants, threshold);

            // Assert
            Assert.IsTrue(result);

            var config = keyManagementContract.GetMultiSigConfig(testOwner);
            Assert.IsNotNull(config);
            Assert.AreEqual(testOwner, config.Owner);
            Assert.AreEqual(participants.Length, config.Participants.Length);
            Assert.AreEqual(threshold, config.Threshold);
            Assert.IsTrue(config.IsActive);
        }

        [TestMethod]
        public void TestConfigureMultiSig_InvalidThreshold_ThrowsException()
        {
            // Arrange
            var participants = new UInt160[]
            {
                UInt160.Parse("0x1111111111111111111111111111111111111111"),
                UInt160.Parse("0x2222222222222222222222222222222222222222")
            };
            var invalidThreshold = 3; // Greater than participants

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                keyManagementContract.ConfigureMultiSig(testOwner, participants, invalidThreshold));
        }

        [TestMethod]
        public void TestLogKeyUsage_ActiveKey_Success()
        {
            // Arrange
            var keyId = keyManagementContract.GenerateKey(
                testOwner, 
                KeyManagementContract.KeyType.ECDSA, 
                KeyManagementContract.KeyPurpose.Signing, 
                256, 
                "Test key");
            
            var operation = "sign_transaction";
            var context = "User authentication";

            // Act
            var result = keyManagementContract.LogKeyUsage(keyId, operation, context);

            // Assert
            Assert.IsTrue(result);

            var keyRecord = keyManagementContract.GetKeyRecord(keyId);
            Assert.AreEqual(1, keyRecord.UsageCount);
            Assert.IsTrue(keyRecord.LastUsed > 0);
        }

        [TestMethod]
        public void TestRevokeKey_ActiveKey_Success()
        {
            // Arrange
            var keyId = keyManagementContract.GenerateKey(
                testOwner, 
                KeyManagementContract.KeyType.ECDSA, 
                KeyManagementContract.KeyPurpose.Signing, 
                256, 
                "Test key");
            
            var reason = "Security breach detected";

            // Act
            var result = keyManagementContract.RevokeKey(keyId, reason);

            // Assert
            Assert.IsTrue(result);

            var keyRecord = keyManagementContract.GetKeyRecord(keyId);
            Assert.AreEqual(KeyManagementContract.KeyStatus.Revoked, keyRecord.Status);
            Assert.AreEqual(reason, keyRecord.RevocationReason);
            Assert.IsTrue(keyRecord.RevokedAt > 0);
        }

        [TestMethod]
        public void TestScheduleKeyRotation_ValidKey_Success()
        {
            // Arrange
            var keyId = keyManagementContract.GenerateKey(
                testOwner, 
                KeyManagementContract.KeyType.ECDSA, 
                KeyManagementContract.KeyPurpose.Signing, 
                256, 
                "Test key");
            
            var rotationTime = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600); // 1 hour from now
            var reason = KeyManagementContract.RotationReason.Scheduled;

            // Act
            var result = keyManagementContract.ScheduleKeyRotation(keyId, rotationTime, reason);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestGetKeyCount_AfterGeneratingKeys_ReturnsCorrectCount()
        {
            // Arrange
            var initialCount = keyManagementContract.GetKeyCount();

            // Act
            keyManagementContract.GenerateKey(testOwner, KeyManagementContract.KeyType.ECDSA, 
                KeyManagementContract.KeyPurpose.Signing, 256, "Key 1");
            keyManagementContract.GenerateKey(testOwner, KeyManagementContract.KeyType.RSA, 
                KeyManagementContract.KeyPurpose.Encryption, 256, "Key 2");

            // Assert
            var finalCount = keyManagementContract.GetKeyCount();
            Assert.AreEqual(initialCount + 2, finalCount);
        }

        [TestMethod]
        public void TestGetSecurityConfig_ReturnsValidConfiguration()
        {
            // Act
            var config = keyManagementContract.GetSecurityConfig();

            // Assert
            Assert.IsNotNull(config);
            Assert.IsTrue(config.DefaultKeyStrength > 0);
            Assert.IsTrue(config.RotationPeriod > 0);
            Assert.IsTrue(config.MaxKeyAge > 0);
        }

        [TestMethod]
        public void TestKeyLifecycle_CompleteFlow_Success()
        {
            // Arrange & Act - Generate key
            var keyId = keyManagementContract.GenerateKey(
                testOwner, 
                KeyManagementContract.KeyType.ECDSA, 
                KeyManagementContract.KeyPurpose.Signing, 
                256, 
                "Lifecycle test key");

            // Log usage
            keyManagementContract.LogKeyUsage(keyId, "sign", "test operation");
            keyManagementContract.LogKeyUsage(keyId, "verify", "test verification");

            // Rotate key
            var newKeyId = keyManagementContract.RotateKey(keyId, 
                KeyManagementContract.RotationReason.Manual, "Lifecycle test");

            // Revoke old key
            keyManagementContract.RevokeKey(keyId, "Lifecycle completion");

            // Assert
            var oldKeyRecord = keyManagementContract.GetKeyRecord(keyId);
            Assert.AreEqual(KeyManagementContract.KeyStatus.Revoked, oldKeyRecord.Status);
            Assert.AreEqual(2, oldKeyRecord.UsageCount);
            Assert.AreEqual(newKeyId, oldKeyRecord.RotatedTo);

            var newKeyRecord = keyManagementContract.GetKeyRecord(newKeyId);
            Assert.AreEqual(KeyManagementContract.KeyStatus.Active, newKeyRecord.Status);
            Assert.AreEqual(testOwner, newKeyRecord.Owner);
        }

        [TestMethod]
        public void TestMultipleKeyTypes_GenerateAndManage_Success()
        {
            // Arrange & Act
            var rsaKeyId = keyManagementContract.GenerateKey(testOwner, 
                KeyManagementContract.KeyType.RSA, KeyManagementContract.KeyPurpose.Encryption, 256, "RSA key");
            var ecdsaKeyId = keyManagementContract.GenerateKey(testOwner, 
                KeyManagementContract.KeyType.ECDSA, KeyManagementContract.KeyPurpose.Signing, 256, "ECDSA key");
            var aesKeyId = keyManagementContract.GenerateKey(testOwner, 
                KeyManagementContract.KeyType.AES, KeyManagementContract.KeyPurpose.Encryption, 256, "AES key");

            // Assert
            var rsaRecord = keyManagementContract.GetKeyRecord(rsaKeyId);
            var ecdsaRecord = keyManagementContract.GetKeyRecord(ecdsaKeyId);
            var aesRecord = keyManagementContract.GetKeyRecord(aesKeyId);

            Assert.AreEqual(KeyManagementContract.KeyType.RSA, rsaRecord.Type);
            Assert.AreEqual(KeyManagementContract.KeyType.ECDSA, ecdsaRecord.Type);
            Assert.AreEqual(KeyManagementContract.KeyType.AES, aesRecord.Type);

            Assert.AreEqual(KeyManagementContract.KeyPurpose.Encryption, rsaRecord.Purpose);
            Assert.AreEqual(KeyManagementContract.KeyPurpose.Signing, ecdsaRecord.Purpose);
            Assert.AreEqual(KeyManagementContract.KeyPurpose.Encryption, aesRecord.Purpose);
        }

        [TestMethod]
        public void TestHierarchicalKeyDerivation_MultipleChildren_Success()
        {
            // Arrange
            var masterKeyId = keyManagementContract.GenerateKey(testOwner, 
                KeyManagementContract.KeyType.ECDSA, KeyManagementContract.KeyPurpose.KeyDerivation, 256, "Master key");

            // Act - Derive multiple child keys
            var child1Id = keyManagementContract.DeriveChildKey(masterKeyId, 1, 
                KeyManagementContract.KeyPurpose.Signing, "Child 1");
            var child2Id = keyManagementContract.DeriveChildKey(masterKeyId, 2, 
                KeyManagementContract.KeyPurpose.Encryption, "Child 2");
            var child3Id = keyManagementContract.DeriveChildKey(masterKeyId, 3, 
                KeyManagementContract.KeyPurpose.Authentication, "Child 3");

            // Assert
            var child1Record = keyManagementContract.GetKeyRecord(child1Id);
            var child2Record = keyManagementContract.GetKeyRecord(child2Id);
            var child3Record = keyManagementContract.GetKeyRecord(child3Id);

            Assert.AreEqual(KeyManagementContract.KeyPurpose.Signing, child1Record.Purpose);
            Assert.AreEqual(KeyManagementContract.KeyPurpose.Encryption, child2Record.Purpose);
            Assert.AreEqual(KeyManagementContract.KeyPurpose.Authentication, child3Record.Purpose);

            // All children should have the same owner and type as master
            Assert.AreEqual(testOwner, child1Record.Owner);
            Assert.AreEqual(testOwner, child2Record.Owner);
            Assert.AreEqual(testOwner, child3Record.Owner);

            Assert.AreEqual(KeyManagementContract.KeyType.ECDSA, child1Record.Type);
            Assert.AreEqual(KeyManagementContract.KeyType.ECDSA, child2Record.Type);
            Assert.AreEqual(KeyManagementContract.KeyType.ECDSA, child3Record.Type);
        }
    }
}