using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
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
    public OracleController(IOracleService oracleService, ILogger<OracleController> logger) : base(logger)
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
        [FromBody] NeoServiceLayer.Services.Oracle.Models.CreateDataSourceRequest request,
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
            
            return Ok(CreateResponse(dataSourceId, "Data source created successfully"));
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
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
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
            var dataRequest = new NeoServiceLayer.Services.Oracle.Models.OracleDataRequest
            {
                DataSourceId = dataSourceId,
                FetchLatest = true
            };
            var data = await _oracleService.GetDataAsync(dataRequest, blockchain);
            
            if (data == null)
            {
                return NotFound(CreateErrorResponse($"Data source not found: {dataSourceId}"));
            }
            
            _logger.LogInformation("Data retrieved from source: {DataSourceId} on {Blockchain}", 
                dataSourceId, blockchainType);
            
            return Ok(CreateResponse(data, "Data retrieved successfully"));
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
        [FromBody] NeoServiceLayer.Services.Oracle.Models.UpdateDataSourceRequest update,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            update.DataSourceId = dataSourceId; // Set the DataSourceId from route parameter
            var result = await _oracleService.UpdateDataSourceAsync(update, blockchain);
            var success = result.Success;
            
            if (!success)
            {
                return NotFound(CreateErrorResponse($"Data source not found: {dataSourceId}"));
            }
            
            _logger.LogInformation("Data source updated: {DataSourceId} on {Blockchain}", 
                dataSourceId, blockchainType);
            
            return Ok(CreateResponse(success, "Data source updated successfully"));
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
            var deleteRequest = new NeoServiceLayer.Services.Oracle.Models.DeleteDataSourceRequest
            {
                DataSourceId = dataSourceId
            };
            var result = await _oracleService.DeleteDataSourceAsync(deleteRequest, blockchain);
            var success = result.Success;
            
            if (!success)
            {
                return NotFound(CreateErrorResponse($"Data source not found: {dataSourceId}"));
            }
            
            _logger.LogInformation("Data source deleted: {DataSourceId} from {Blockchain}", 
                dataSourceId, blockchainType);
            
            return Ok(CreateResponse(success, "Data source deleted successfully"));
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
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<object>>), 200)]
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
            var listRequest = new NeoServiceLayer.Services.Oracle.Models.ListDataSourcesRequest
            {
                PageSize = take,
                PageNumber = (skip / take) + 1
            };
            var result = await _oracleService.GetDataSourcesAsync(listRequest, blockchain);
            var sources = result.DataSources;
            // Return the service result directly since it already contains pagination
            return Ok(CreateResponse(result, "Data sources retrieved successfully"));
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
        [FromBody] object request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            // CreateSubscriptionAsync method is not available in service interface - return not implemented
            return StatusCode(501, CreateResponse<object>(null, "Subscription functionality not implemented in current interface"));
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

            // CancelSubscriptionAsync method is not available in service interface - return not implemented
            return StatusCode(501, CreateResponse<object>(null, "Cancel subscription functionality not implemented in current interface"));
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
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> ExecuteBatchRequest(
        [FromBody] object request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            // GetBatchDataAsync method is not available in service interface - return not implemented
            return StatusCode(501, CreateResponse<object>(null, "Batch data functionality not implemented in current interface"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing batch request");
            return StatusCode(500, CreateErrorResponse($"Failed to execute batch request: {ex.Message}"));
        }
    }


    /// <summary>
    /// Lists all active subscriptions for the current user.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="skip">Number of items to skip (for pagination).</param>
    /// <param name="take">Number of items to take (for pagination).</param>
    /// <returns>List of active subscriptions.</returns>
    /// <response code="200">Subscriptions retrieved successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("subscriptions/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<object>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> ListSubscriptions(
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

            if (take > 100)
            {
                return BadRequest(CreateErrorResponse("Maximum page size is 100"));
            }

            // ListSubscriptionsAsync method is not available in service interface - return not implemented
            return StatusCode(501, CreateResponse<object>(null, "List subscriptions functionality not implemented in current interface"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing subscriptions");
            return StatusCode(500, CreateErrorResponse($"Failed to list subscriptions: {ex.Message}"));
        }
    }
}