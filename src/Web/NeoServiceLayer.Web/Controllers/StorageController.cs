using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Services.Storage.Models;

namespace NeoServiceLayer.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StorageController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly ILogger<StorageController> _logger;

    public StorageController(IStorageService storageService, ILogger<StorageController> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    [HttpPost("store")]
    public async Task<IActionResult> StoreData([FromBody] StoreDataRequest request)
    {
        try
        {
            var result = await _storageService.StoreDataAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing data");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("retrieve/{dataId}")]
    public async Task<IActionResult> RetrieveData(string dataId)
    {
        try
        {
            var request = new RetrieveDataRequest { DataId = dataId };
            var result = await _storageService.RetrieveDataAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{dataId}")]
    public async Task<IActionResult> DeleteData(string dataId)
    {
        try
        {
            var request = new DeleteDataRequest { DataId = dataId };
            var result = await _storageService.DeleteDataAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting data");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{dataId}")]
    public async Task<IActionResult> UpdateData(string dataId, [FromBody] UpdateDataRequest request)
    {
        try
        {
            request.DataId = dataId;
            var result = await _storageService.UpdateDataAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating data");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("metadata/{dataId}")]
    public async Task<IActionResult> GetMetadata(string dataId)
    {
        try
        {
            var request = new GetMetadataRequest { DataId = dataId };
            var result = await _storageService.GetMetadataAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchData([FromBody] SearchDataRequest request)
    {
        try
        {
            var result = await _storageService.SearchDataAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching data");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListData([FromQuery] int pageSize = 20, [FromQuery] int pageNumber = 1)
    {
        try
        {
            var request = new ListDataRequest { PageSize = pageSize, PageNumber = pageNumber };
            var result = await _storageService.ListDataAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing data");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("batch-store")]
    public async Task<IActionResult> BatchStoreData([FromBody] BatchStoreDataRequest request)
    {
        try
        {
            var result = await _storageService.BatchStoreDataAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch storing data");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStorageStatistics()
    {
        try
        {
            var request = new StorageStatisticsRequest();
            var result = await _storageService.GetStorageStatisticsAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage statistics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("transaction/begin")]
    public async Task<IActionResult> BeginTransaction([FromBody] BeginTransactionRequest request)
    {
        try
        {
            var result = await _storageService.BeginTransactionAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error beginning transaction");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("transaction/commit")]
    public async Task<IActionResult> CommitTransaction([FromBody] CommitTransactionRequest request)
    {
        try
        {
            var result = await _storageService.CommitTransactionAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing transaction");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("transaction/rollback")]
    public async Task<IActionResult> RollbackTransaction([FromBody] RollbackTransactionRequest request)
    {
        try
        {
            var result = await _storageService.RollbackTransactionAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back transaction");
            return StatusCode(500, new { error = ex.Message });
        }
    }
} 