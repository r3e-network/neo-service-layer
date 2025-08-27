using Neo.Storage.Service.Models;

namespace Neo.Storage.Service.Services;

public interface IBucketService
{
    Task<BucketResponse> CreateBucketAsync(CreateBucketRequest request, string userId);
    Task<BucketResponse?> GetBucketAsync(string bucketName, string userId);
    Task<List<BucketResponse>> ListBucketsAsync(string userId);
    Task<bool> DeleteBucketAsync(string bucketName, string userId);
    Task<bool> SetBucketPolicyAsync(string bucketName, BucketPolicy policy, string userId);
    Task<List<BucketPolicy>> GetBucketPoliciesAsync(string bucketName, string userId);
    Task<bool> DeleteBucketPolicyAsync(string bucketName, Guid policyId, string userId);
    Task<bool> EnableVersioningAsync(string bucketName, string userId);
    Task<bool> DisableVersioningAsync(string bucketName, string userId);
    Task<bool> SetBucketEncryptionAsync(string bucketName, string encryptionKey, string userId);
    Task<StorageStatistics> GetBucketStatisticsAsync(string bucketName, string userId);
}