using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neo.Storage.Service.Models;
using Neo.Storage.Service.Services;
using System.Security.Claims;

namespace Neo.Storage.Service.Controllers;

[ApiController]
[Route("api/v1/storage")]
[Authorize]
public class StorageController : ControllerBase
{
    private readonly IStorageObjectService _objectService;
    private readonly IBucketService _bucketService;
    private readonly IReplicationService _replicationService;
    private readonly IStorageTransactionService _transactionService;
    private readonly ILogger<StorageController> _logger;

    public StorageController(
        IStorageObjectService objectService,
        IBucketService bucketService,
        IReplicationService replicationService,
        IStorageTransactionService transactionService,
        ILogger<StorageController> logger)
    {
        _objectService = objectService;
        _bucketService = bucketService;
        _replicationService = replicationService;
        _transactionService = transactionService;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();

    #region Bucket Operations

    [HttpPost("buckets")]
    public async Task<ActionResult<BucketResponse>> CreateBucket([FromBody] CreateBucketRequest request)
    {
        try
        {
            var userId = GetUserId();
            var bucket = await _bucketService.CreateBucketAsync(request, userId);
            return CreatedAtAction(nameof(GetBucket), new { bucketName = bucket.Name }, bucket);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create bucket {BucketName}", request.Name);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("buckets/{bucketName}")]
    public async Task<ActionResult<BucketResponse>> GetBucket(string bucketName)
    {
        try
        {
            var userId = GetUserId();
            var bucket = await _bucketService.GetBucketAsync(bucketName, userId);
            
            if (bucket == null)
            {
                return NotFound(new { error = "Bucket not found" });
            }

            return Ok(bucket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get bucket {BucketName}", bucketName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("buckets")]
    public async Task<ActionResult<List<BucketResponse>>> ListBuckets()
    {
        try
        {
            var userId = GetUserId();
            var buckets = await _bucketService.ListBucketsAsync(userId);
            return Ok(buckets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list buckets for user {UserId}", GetUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpDelete("buckets/{bucketName}")]
    public async Task<IActionResult> DeleteBucket(string bucketName)
    {
        try
        {
            var userId = GetUserId();
            var deleted = await _bucketService.DeleteBucketAsync(bucketName, userId);
            
            if (!deleted)
            {
                return NotFound(new { error = "Bucket not found or access denied" });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete bucket {BucketName}", bucketName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("buckets/{bucketName}/policies")]
    public async Task<IActionResult> SetBucketPolicy(string bucketName, [FromBody] BucketPolicy policy)
    {
        try
        {
            var userId = GetUserId();
            var success = await _bucketService.SetBucketPolicyAsync(bucketName, policy, userId);
            
            if (!success)
            {
                return NotFound(new { error = "Bucket not found or access denied" });
            }

            return Ok(new { message = "Bucket policy set successfully" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set bucket policy for {BucketName}", bucketName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("buckets/{bucketName}/policies")]
    public async Task<ActionResult<List<BucketPolicy>>> GetBucketPolicies(string bucketName)
    {
        try
        {
            var userId = GetUserId();
            var policies = await _bucketService.GetBucketPoliciesAsync(bucketName, userId);
            return Ok(policies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get bucket policies for {BucketName}", bucketName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPut("buckets/{bucketName}/versioning")]
    public async Task<IActionResult> SetBucketVersioning(string bucketName, [FromBody] SetVersioningRequest request)
    {
        try
        {
            var userId = GetUserId();
            var success = request.Enabled 
                ? await _bucketService.EnableVersioningAsync(bucketName, userId)
                : await _bucketService.DisableVersioningAsync(bucketName, userId);
            
            if (!success)
            {
                return NotFound(new { error = "Bucket not found or access denied" });
            }

            return Ok(new { message = $"Versioning {(request.Enabled ? "enabled" : "disabled")} successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set versioning for bucket {BucketName}", bucketName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("buckets/{bucketName}/statistics")]
    public async Task<ActionResult<StorageStatistics>> GetBucketStatistics(string bucketName)
    {
        try
        {
            var userId = GetUserId();
            var stats = await _bucketService.GetBucketStatisticsAsync(bucketName, userId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get bucket statistics for {BucketName}", bucketName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #endregion

    #region Object Operations

    [HttpPost("buckets/{bucketName}/objects")]
    [RequestSizeLimit(5L * 1024 * 1024 * 1024)] // 5GB limit
    public async Task<ActionResult<StorageObjectResponse>> UploadObject(string bucketName, [FromForm] UploadObjectFormRequest formRequest)
    {
        try
        {
            var userId = GetUserId();
            
            if (formRequest.File == null || formRequest.File.Length == 0)
            {
                return BadRequest(new { error = "File is required" });
            }

            var request = new UploadObjectRequest
            {
                BucketName = bucketName,
                Key = formRequest.Key ?? formRequest.File.FileName,
                Content = formRequest.File.OpenReadStream(),
                ContentType = formRequest.File.ContentType,
                Metadata = formRequest.Metadata ?? new Dictionary<string, string>(),
                StorageClass = formRequest.StorageClass,
                Tags = formRequest.Tags ?? new Dictionary<string, string>()
            };

            var response = await _objectService.UploadObjectAsync(request, userId);
            return CreatedAtAction(nameof(GetObject), new { bucketName, key = response.Key }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload object to bucket {BucketName}", bucketName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("buckets/{bucketName}/objects/{key}")]
    public async Task<IActionResult> GetObject(string bucketName, string key, [FromQuery] string? versionId = null)
    {
        try
        {
            var userId = GetUserId();
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                VersionId = versionId
            };

            var response = await _objectService.GetObjectAsync(request, userId);
            
            if (response == null)
            {
                return NotFound(new { error = "Object not found" });
            }

            return File(response.Content, response.ContentType ?? "application/octet-stream", response.Key);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get object {Key} from bucket {BucketName}", key, bucketName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("buckets/{bucketName}/objects/{key}/metadata")]
    public async Task<ActionResult<StorageObjectResponse>> GetObjectMetadata(string bucketName, string key, [FromQuery] string? versionId = null)
    {
        try
        {
            var userId = GetUserId();
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                VersionId = versionId
            };

            var response = await _objectService.GetObjectMetadataAsync(request, userId);
            
            if (response == null)
            {
                return NotFound(new { error = "Object not found" });
            }

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get object metadata for {Key} from bucket {BucketName}", key, bucketName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("buckets/{bucketName}/objects")]
    public async Task<ActionResult<List<StorageObjectResponse>>> ListObjects(
        string bucketName, 
        [FromQuery] string? prefix = null,
        [FromQuery] string? delimiter = null,
        [FromQuery] int maxKeys = 1000,
        [FromQuery] string? continuationToken = null)
    {
        try
        {
            var userId = GetUserId();
            var request = new ListObjectsRequest
            {
                BucketName = bucketName,
                Prefix = prefix,
                Delimiter = delimiter,
                MaxKeys = Math.Min(maxKeys, 1000), // Limit to 1000 max
                ContinuationToken = continuationToken
            };

            var response = await _objectService.ListObjectsAsync(request, userId);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list objects in bucket {BucketName}", bucketName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpDelete("buckets/{bucketName}/objects/{key}")]
    public async Task<IActionResult> DeleteObject(string bucketName, string key, [FromQuery] string? versionId = null)
    {
        try
        {
            var userId = GetUserId();
            var request = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                VersionId = versionId
            };

            var success = await _objectService.DeleteObjectAsync(request, userId);
            
            if (!success)
            {
                return NotFound(new { error = "Object not found or access denied" });
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete object {Key} from bucket {BucketName}", key, bucketName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("buckets/{bucketName}/objects/{key}/copy")]
    public async Task<ActionResult<StorageObjectResponse>> CopyObject(string bucketName, string key, [FromBody] CopyObjectRequest request)
    {
        try
        {
            var userId = GetUserId();
            request.SourceBucketName = bucketName;
            request.SourceKey = key;

            var response = await _objectService.CopyObjectAsync(request, userId);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy object {Key} from bucket {BucketName}", key, bucketName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("buckets/{bucketName}/objects/{key}/presigned-url")]
    public async Task<ActionResult<PresignedUrlResponse>> GeneratePresignedUrl(
        string bucketName, 
        string key, 
        [FromBody] GeneratePresignedUrlRequest request)
    {
        try
        {
            var userId = GetUserId();
            request.BucketName = bucketName;
            request.Key = key;

            var response = await _objectService.GeneratePresignedUrlAsync(request, userId);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for {Key} in bucket {BucketName}", key, bucketName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #endregion

    #region Replication Operations

    [HttpGet("replication/health")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<ReplicationHealthReport>> GetReplicationHealth()
    {
        try
        {
            var health = await _replicationService.GetReplicationHealthAsync();
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get replication health");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("objects/{objectId}/replicas")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<List<StorageReplica>>> GetObjectReplicas(Guid objectId)
    {
        try
        {
            var replicas = await _replicationService.GetObjectReplicasAsync(objectId);
            return Ok(replicas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get replicas for object {ObjectId}", objectId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("objects/{objectId}/repair")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> RepairObjectReplication(Guid objectId)
    {
        try
        {
            var success = await _replicationService.RepairObjectReplicationAsync(objectId);
            
            if (!success)
            {
                return NotFound(new { error = "Object not found" });
            }

            return Ok(new { message = "Replication repair initiated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to repair replication for object {ObjectId}", objectId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("replication/rebalance")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> RebalanceReplicas()
    {
        try
        {
            var success = await _replicationService.RebalanceReplicasAsync();
            
            if (!success)
            {
                return BadRequest(new { error = "Failed to initiate rebalance" });
            }

            return Ok(new { message = "Replication rebalance initiated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rebalance replicas");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #endregion

    #region Transaction Operations

    [HttpPost("transactions")]
    public async Task<ActionResult<StorageTransaction>> BeginTransaction([FromBody] BeginTransactionRequest request)
    {
        try
        {
            var userId = GetUserId();
            var transaction = await _transactionService.BeginTransactionAsync(userId, request.Type);
            return CreatedAtAction(nameof(GetTransaction), new { transactionId = transaction.Id }, transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin transaction for user {UserId}", GetUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("transactions/{transactionId}")]
    public async Task<ActionResult<StorageTransaction>> GetTransaction(Guid transactionId)
    {
        try
        {
            var transaction = await _transactionService.GetTransactionAsync(transactionId);
            
            if (transaction == null || transaction.UserId != GetUserId())
            {
                return NotFound(new { error = "Transaction not found" });
            }

            return Ok(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transaction {TransactionId}", transactionId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("transactions/{transactionId}/commit")]
    public async Task<IActionResult> CommitTransaction(Guid transactionId)
    {
        try
        {
            var transaction = await _transactionService.GetTransactionAsync(transactionId);
            
            if (transaction == null || transaction.UserId != GetUserId())
            {
                return NotFound(new { error = "Transaction not found" });
            }

            var success = await _transactionService.CommitTransactionAsync(transactionId);
            
            if (!success)
            {
                return BadRequest(new { error = "Failed to commit transaction" });
            }

            return Ok(new { message = "Transaction committed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit transaction {TransactionId}", transactionId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("transactions/{transactionId}/rollback")]
    public async Task<IActionResult> RollbackTransaction(Guid transactionId)
    {
        try
        {
            var transaction = await _transactionService.GetTransactionAsync(transactionId);
            
            if (transaction == null || transaction.UserId != GetUserId())
            {
                return NotFound(new { error = "Transaction not found" });
            }

            var success = await _transactionService.RollbackTransactionAsync(transactionId);
            
            if (!success)
            {
                return BadRequest(new { error = "Failed to rollback transaction" });
            }

            return Ok(new { message = "Transaction rolled back successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback transaction {TransactionId}", transactionId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #endregion
}