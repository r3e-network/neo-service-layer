using Neo.Storage.Service.Models;

namespace Neo.Storage.Service.Services;

public interface IStorageObjectService
{
    Task<StorageObjectResponse> UploadObjectAsync(UploadObjectRequest request, string userId);
    Task<Stream> DownloadObjectAsync(string bucketName, string key, string userId);
    Task<StorageObjectResponse?> GetObjectAsync(string bucketName, string key, string userId);
    Task<List<StorageObjectResponse>> ListObjectsAsync(string bucketName, string userId, string? prefix = null, int maxKeys = 1000);
    Task<bool> DeleteObjectAsync(string bucketName, string key, string userId);
    Task<StorageObjectResponse> CopyObjectAsync(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey, string userId);
    Task<List<ObjectVersion>> GetObjectVersionsAsync(string bucketName, string key, string userId);
    Task<bool> RestoreObjectAsync(string bucketName, string key, string versionId, string userId);
    Task<string> GeneratePresignedUrlAsync(string bucketName, string key, TimeSpan expiry, string action = "GET");
    Task<StorageStatistics> GetStorageStatisticsAsync(string userId);
}