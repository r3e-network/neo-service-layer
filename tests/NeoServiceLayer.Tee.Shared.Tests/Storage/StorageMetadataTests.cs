using System;
using System.Text.Json;
using NeoServiceLayer.Tee.Shared.Storage;
using Xunit;

namespace NeoServiceLayer.Tee.Shared.Tests.Storage
{
    public class StorageMetadataTests
    {
        [Fact]
        public void StorageMetadata_DefaultConstructor_InitializesProperties()
        {
            // Act
            var metadata = new StorageMetadata();

            // Assert
            Assert.Null(metadata.Key);
            Assert.Equal(0, metadata.Size);
            Assert.Equal(DateTime.MinValue, metadata.CreationTime);
            Assert.Equal(DateTime.MinValue, metadata.LastModifiedTime);
            Assert.Equal(DateTime.MinValue, metadata.LastAccessTime);
            Assert.Null(metadata.ContentType);
            Assert.Null(metadata.Hash);
            Assert.Null(metadata.HashAlgorithm);
            Assert.False(metadata.IsChunked);
            Assert.Equal(0, metadata.ChunkSize);
            Assert.Equal(0, metadata.ChunkCount);
            Assert.Null(metadata.Tags);
            Assert.Null(metadata.CustomMetadata);
        }

        [Fact]
        public void StorageMetadata_ParameterizedConstructor_InitializesProperties()
        {
            // Arrange
            string key = "test_key";
            long size = 100;
            DateTime now = DateTime.UtcNow;
            string contentType = "application/json";
            string hash = "hash123";
            string hashAlgorithm = "SHA256";

            // Act
            var metadata = new StorageMetadata(
                key,
                size,
                now,
                now,
                now,
                contentType,
                hash,
                hashAlgorithm);

            // Assert
            Assert.Equal(key, metadata.Key);
            Assert.Equal(size, metadata.Size);
            Assert.Equal(now, metadata.CreationTime);
            Assert.Equal(now, metadata.LastModifiedTime);
            Assert.Equal(now, metadata.LastAccessTime);
            Assert.Equal(contentType, metadata.ContentType);
            Assert.Equal(hash, metadata.Hash);
            Assert.Equal(hashAlgorithm, metadata.HashAlgorithm);
            Assert.False(metadata.IsChunked);
            Assert.Equal(0, metadata.ChunkSize);
            Assert.Equal(0, metadata.ChunkCount);
            Assert.Null(metadata.Tags);
            Assert.Null(metadata.CustomMetadata);
        }

        [Fact]
        public void StorageMetadata_Clone_CreatesDeepCopy()
        {
            // Arrange
            var original = new StorageMetadata
            {
                Key = "test_key",
                Size = 100,
                CreationTime = DateTime.UtcNow,
                LastModifiedTime = DateTime.UtcNow,
                LastAccessTime = DateTime.UtcNow,
                ContentType = "application/json",
                Hash = "hash123",
                HashAlgorithm = "SHA256",
                IsChunked = true,
                ChunkSize = 1024,
                ChunkCount = 5,
                Tags = new[] { "tag1", "tag2" },
                CustomMetadata = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
                }
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.Equal(original.Key, clone.Key);
            Assert.Equal(original.Size, clone.Size);
            Assert.Equal(original.CreationTime, clone.CreationTime);
            Assert.Equal(original.LastModifiedTime, clone.LastModifiedTime);
            Assert.Equal(original.LastAccessTime, clone.LastAccessTime);
            Assert.Equal(original.ContentType, clone.ContentType);
            Assert.Equal(original.Hash, clone.Hash);
            Assert.Equal(original.HashAlgorithm, clone.HashAlgorithm);
            Assert.Equal(original.IsChunked, clone.IsChunked);
            Assert.Equal(original.ChunkSize, clone.ChunkSize);
            Assert.Equal(original.ChunkCount, clone.ChunkCount);
            Assert.Equal(original.Tags, clone.Tags);
            Assert.Equal(original.CustomMetadata, clone.CustomMetadata);

            // Verify it's a deep copy
            Assert.NotSame(original.Tags, clone.Tags);
            Assert.NotSame(original.CustomMetadata, clone.CustomMetadata);
        }

        [Fact]
        public void StorageMetadata_Serialization_Deserialization_PreservesValues()
        {
            // Arrange
            var original = new StorageMetadata
            {
                Key = "test_key",
                Size = 100,
                CreationTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LastModifiedTime = new DateTime(2023, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                LastAccessTime = new DateTime(2023, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                ContentType = "application/json",
                Hash = "hash123",
                HashAlgorithm = "SHA256",
                IsChunked = true,
                ChunkSize = 1024,
                ChunkCount = 5,
                Tags = new[] { "tag1", "tag2" },
                CustomMetadata = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
                }
            };

            // Act
            string json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<StorageMetadata>(json);

            // Assert
            Assert.Equal(original.Key, deserialized.Key);
            Assert.Equal(original.Size, deserialized.Size);
            Assert.Equal(original.CreationTime, deserialized.CreationTime);
            Assert.Equal(original.LastModifiedTime, deserialized.LastModifiedTime);
            Assert.Equal(original.LastAccessTime, deserialized.LastAccessTime);
            Assert.Equal(original.ContentType, deserialized.ContentType);
            Assert.Equal(original.Hash, deserialized.Hash);
            Assert.Equal(original.HashAlgorithm, deserialized.HashAlgorithm);
            Assert.Equal(original.IsChunked, deserialized.IsChunked);
            Assert.Equal(original.ChunkSize, deserialized.ChunkSize);
            Assert.Equal(original.ChunkCount, deserialized.ChunkCount);
            Assert.Equal(original.Tags, deserialized.Tags);
            Assert.Equal(original.CustomMetadata.Count, deserialized.CustomMetadata.Count);
            foreach (var kvp in original.CustomMetadata)
            {
                Assert.Equal(kvp.Value, deserialized.CustomMetadata[kvp.Key]);
            }
        }
    }
}
