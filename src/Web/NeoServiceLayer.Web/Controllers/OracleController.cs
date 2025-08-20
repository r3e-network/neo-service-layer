using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Oracle;
using NeoServiceLayer.Services.Oracle.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;


namespace NeoServiceLayer.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OracleController : ControllerBase
{
    private readonly IOracleService _oracleService;
    private readonly ILogger<OracleController> _logger;

    public OracleController(IOracleService oracleService, ILogger<OracleController> logger)
    {
        _oracleService = oracleService;
        _logger = logger;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] OracleSubscriptionRequest request)
    {
        try
        {
            var result = await _oracleService.SubscribeAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to oracle");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("unsubscribe/{subscriptionId}")]
    public async Task<IActionResult> Unsubscribe(string subscriptionId)
    {
        try
        {
            var request = new OracleUnsubscribeRequest { SubscriptionId = subscriptionId };
            var result = await _oracleService.UnsubscribeAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from oracle");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("data/{dataSourceId}")]
    public async Task<IActionResult> GetData(string dataSourceId)
    {
        try
        {
            var request = new OracleDataRequest { DataSourceId = dataSourceId };
            var result = await _oracleService.GetDataAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting oracle data");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("data-source")]
    public async Task<IActionResult> CreateDataSource([FromBody] CreateDataSourceRequest request)
    {
        try
        {
            var result = await _oracleService.CreateDataSourceAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating data source");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("data-source/{dataSourceId}")]
    public async Task<IActionResult> UpdateDataSource(string dataSourceId, [FromBody] UpdateDataSourceRequest request)
    {
        try
        {
            request.DataSourceId = dataSourceId;
            var result = await _oracleService.UpdateDataSourceAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating data source");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("data-source/{dataSourceId}")]
    public async Task<IActionResult> DeleteDataSource(string dataSourceId)
    {
        try
        {
            var request = new DeleteDataSourceRequest { DataSourceId = dataSourceId };
            var result = await _oracleService.DeleteDataSourceAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting data source");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions([FromQuery] int pageSize = 20, [FromQuery] int pageNumber = 1)
    {
        try
        {
            var request = new ListSubscriptionsRequest { PageSize = pageSize, PageNumber = pageNumber };
            var result = await _oracleService.GetSubscriptionsAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscriptions");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("data-sources")]
    public async Task<IActionResult> GetDataSources([FromQuery] int pageSize = 20, [FromQuery] int pageNumber = 1)
    {
        try
        {
            var request = new ListDataSourcesRequest { PageSize = pageSize, PageNumber = pageNumber };
            var result = await _oracleService.GetDataSourcesAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data sources");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("batch-request")]
    public async Task<IActionResult> BatchRequest([FromBody] BatchOracleRequest request)
    {
        try
        {
            var result = await _oracleService.BatchRequestAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch oracle request");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("status/{subscriptionId}")]
    public async Task<IActionResult> GetSubscriptionStatus(string subscriptionId)
    {
        try
        {
            var request = new OracleStatusRequest { SubscriptionId = subscriptionId };
            var result = await _oracleService.GetSubscriptionStatusAsync(request, BlockchainType.NeoN3);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription status");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
