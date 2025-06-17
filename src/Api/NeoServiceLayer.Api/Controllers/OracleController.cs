using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Oracle;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for oracle data source management and data retrieval.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/oracle")]
[Authorize]
[Tags("Oracle")]
public class OracleController : BaseApiController
{
    private readonly IOracleService _oracleService;
    private readonly ILogger<OracleController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleController"/> class.
    /// </summary>
    /// <param name="oracleService">The oracle service.</param>
    /// <param name="logger">The logger.</param>
    public OracleController(IOracleService oracleService, ILogger<OracleController> logger)
    {
        _oracleService = oracleService ?? throw new ArgumentNullException(nameof(oracleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new data source.
    /// </summary>
    /// <param name="request">The data source creation request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The created data source ID.</returns>
    /// <response code="200">Data source created successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("datasources/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> CreateDataSource(
        [FromBody] DataSourceRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var dataSourceId = await _oracleService.CreateDataSourceAsync(request, blockchain);
            
            _logger.LogInformation("Data source created with ID: {DataSourceId} on {Blockchain}", 
                dataSourceId, blockchainType);
            
            return Ok(CreateSuccessResponse(dataSourceId, "Data source created successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid data source request");
            return BadRequest(CreateErrorResponse($"Invalid data source request: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating data source");
            return StatusCode(500, CreateErrorResponse($"Failed to create data source: {ex.Message}"));
        }
    }

    /// <summary>
    /// Retrieves data from a specified data source.
    /// </summary>
    /// <param name="dataSourceId">The data source ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The oracle data response.</returns>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Data source not found.</response>
    [HttpGet("data/{dataSourceId}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<OracleDataResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetData(
        [FromRoute] string dataSourceId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var data = await _oracleService.GetDataAsync(dataSourceId, blockchain);
            
            if (data == null)
            {
                return NotFound(CreateErrorResponse($"Data source not found: {dataSourceId}"));
            }
            
            _logger.LogInformation("Data retrieved from source: {DataSourceId} on {Blockchain}", 
                dataSourceId, blockchainType);
            
            return Ok(CreateSuccessResponse(data, "Data retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data from source: {DataSourceId}", dataSourceId);
            return StatusCode(500, CreateErrorResponse($"Failed to retrieve data: {ex.Message}"));
        }
    }

    /// <summary>
    /// Updates a data source configuration.
    /// </summary>
    /// <param name="dataSourceId">The data source ID.</param>
    /// <param name="update">The update request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Success indicator.</returns>
    /// <response code="200">Data source updated successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Data source not found.</response>
    [HttpPut("datasources/{dataSourceId}/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateDataSource(
        [FromRoute] string dataSourceId,
        [FromBody] DataSourceUpdate update,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var success = await _oracleService.UpdateDataSourceAsync(dataSourceId, update, blockchain);
            
            if (!success)
            {
                return NotFound(CreateErrorResponse($"Data source not found: {dataSourceId}"));
            }
            
            _logger.LogInformation("Data source updated: {DataSourceId} on {Blockchain}", 
                dataSourceId, blockchainType);
            
            return Ok(CreateSuccessResponse(success, "Data source updated successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid update request for data source: {DataSourceId}", dataSourceId);
            return BadRequest(CreateErrorResponse($"Invalid update request: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating data source: {DataSourceId}", dataSourceId);
            return StatusCode(500, CreateErrorResponse($"Failed to update data source: {ex.Message}"));
        }
    }

    /// <summary>
    /// Deletes a data source.
    /// </summary>
    /// <param name="dataSourceId">The data source ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Success indicator.</returns>
    /// <response code="200">Data source deleted successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Data source not found.</response>
    [HttpDelete("datasources/{dataSourceId}/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteDataSource(
        [FromRoute] string dataSourceId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var success = await _oracleService.DeleteDataSourceAsync(dataSourceId, blockchain);
            
            if (!success)
            {
                return NotFound(CreateErrorResponse($"Data source not found: {dataSourceId}"));
            }
            
            _logger.LogInformation("Data source deleted: {DataSourceId} from {Blockchain}", 
                dataSourceId, blockchainType);
            
            return Ok(CreateSuccessResponse(success, "Data source deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting data source: {DataSourceId}", dataSourceId);
            return StatusCode(500, CreateErrorResponse($"Failed to delete data source: {ex.Message}"));
        }
    }

    /// <summary>
    /// Lists all available data sources.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="skip">Number of items to skip (for pagination).</param>
    /// <param name="take">Number of items to take (for pagination).</param>
    /// <returns>List of data sources.</returns>
    /// <response code="200">Data sources retrieved successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("datasources/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<DataSourceInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> ListDataSources(
        [FromRoute] string blockchainType,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (skip < 0 || take <= 0 || take > 100)
            {
                return BadRequest(CreateErrorResponse("Invalid pagination parameters"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var sources = await _oracleService.GetDataSourcesAsync(blockchain);
            var paginatedSources = sources.Skip(skip).Take(take).ToList();
            
            var response = new PaginatedResponse<DataSourceInfo>
            {
                Items = paginatedSources,
                TotalCount = sources.Count(),
                Skip = skip,
                Take = take,
                HasMore = sources.Count() > skip + take
            };
            
            return Ok(CreateSuccessResponse(response, "Data sources retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing data sources");
            return StatusCode(500, CreateErrorResponse($"Failed to list data sources: {ex.Message}"));
        }
    }

    /// <summary>
    /// Creates a data subscription for automatic updates.
    /// </summary>
    /// <param name="request">The subscription request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The subscription ID.</returns>
    /// <response code="200">Subscription created successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("subscriptions/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] SubscriptionRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var subscriptionId = await _oracleService.CreateSubscriptionAsync(request, blockchain);
            
            _logger.LogInformation("Subscription created with ID: {SubscriptionId} on {Blockchain}", 
                subscriptionId, blockchainType);
            
            return Ok(CreateSuccessResponse(subscriptionId, "Subscription created successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid subscription request");
            return BadRequest(CreateErrorResponse($"Invalid subscription request: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription");
            return StatusCode(500, CreateErrorResponse($"Failed to create subscription: {ex.Message}"));
        }
    }

    /// <summary>
    /// Cancels a data subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Success indicator.</returns>
    /// <response code="200">Subscription cancelled successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Subscription not found.</response>
    [HttpDelete("subscriptions/{subscriptionId}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> CancelSubscription(
        [FromRoute] string subscriptionId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var success = await _oracleService.CancelSubscriptionAsync(subscriptionId, blockchain);
            
            if (!success)
            {
                return NotFound(CreateErrorResponse($"Subscription not found: {subscriptionId}"));
            }
            
            _logger.LogInformation("Subscription cancelled: {SubscriptionId} on {Blockchain}", 
                subscriptionId, blockchainType);
            
            return Ok(CreateSuccessResponse(success, "Subscription cancelled successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription: {SubscriptionId}", subscriptionId);
            return StatusCode(500, CreateErrorResponse($"Failed to cancel subscription: {ex.Message}"));
        }
    }

    /// <summary>
    /// Executes a batch oracle request for multiple data sources.
    /// </summary>
    /// <param name="request">The batch request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The batch results.</returns>
    /// <response code="200">Batch request executed successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("batch/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<OracleBatchResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> ExecuteBatchRequest(
        [FromBody] OracleBatchRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (request.DataSourceIds == null || request.DataSourceIds.Count == 0)
            {
                return BadRequest(CreateErrorResponse("No data sources specified"));
            }

            if (request.DataSourceIds.Count > 50)
            {
                return BadRequest(CreateErrorResponse("Too many data sources (max 50)"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var results = await _oracleService.GetBatchDataAsync(request, blockchain);
            
            _logger.LogInformation("Batch request executed for {Count} data sources on {Blockchain}", 
                request.DataSourceIds.Count, blockchainType);
            
            return Ok(CreateSuccessResponse(results, "Batch request executed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing batch request");
            return StatusCode(500, CreateErrorResponse($"Failed to execute batch request: {ex.Message}"));
        }
    }
}