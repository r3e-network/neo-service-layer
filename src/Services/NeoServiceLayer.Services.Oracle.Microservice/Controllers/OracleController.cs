using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Neo.Oracle.Service.Models;
using Neo.Oracle.Service.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Neo.Oracle.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("OracleApi")]
public class OracleController : ControllerBase
{
    private readonly IOracleDataService _oracleDataService;
    private readonly IPriceFeedService _priceFeedService;
    private readonly IConsensusService _consensusService;
    private readonly ILogger<OracleController> _logger;

    public OracleController(
        IOracleDataService oracleDataService,
        IPriceFeedService priceFeedService,
        IConsensusService consensusService,
        ILogger<OracleController> logger)
    {
        _oracleDataService = oracleDataService;
        _priceFeedService = priceFeedService;
        _consensusService = consensusService;
        _logger = logger;
    }

    /// <summary>
    /// Get current price data for a symbol
    /// </summary>
    /// <param name="symbol">Trading symbol (e.g., BTC, ETH)</param>
    /// <param name="includeSources">Include individual source data</param>
    /// <returns>Current price information with consensus data</returns>
    [HttpGet("price/{symbol}")]
    [Authorize(Policy = "OracleRead")]
    [ProducesResponseType(typeof(PriceFeedResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> GetPrice(
        [FromRoute] string symbol,
        [FromQuery] bool includeSources = false)
    {
        try
        {
            var priceData = await _priceFeedService.GetCurrentPriceAsync(symbol, includeSources);
            
            if (priceData == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Symbol Not Found",
                    Detail = $"Price data not available for symbol: {symbol}",
                    Status = 404
                });
            }

            return Ok(priceData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price for symbol: {Symbol}", symbol);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving price data",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get multiple price feeds
    /// </summary>
    /// <param name="request">Bulk price feed request</param>
    /// <returns>Price data for multiple symbols</returns>
    [HttpPost("price/bulk")]
    [Authorize(Policy = "OracleRead")]
    [ProducesResponseType(typeof(Dictionary<string, PriceFeedResponse>), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IActionResult> GetBulkPrices([FromBody] PriceFeedRequest request)
    {
        try
        {
            var symbols = request.Symbol.Split(',').Select(s => s.Trim().ToUpper()).ToArray();
            var priceData = new Dictionary<string, PriceFeedResponse>();

            foreach (var symbol in symbols)
            {
                var data = await _priceFeedService.GetCurrentPriceAsync(symbol, false);
                if (data != null)
                {
                    priceData[symbol] = data;
                }
            }

            return Ok(priceData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bulk price data");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving bulk price data",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get historical price data
    /// </summary>
    /// <param name="request">Historical data request parameters</param>
    /// <returns>Historical price data</returns>
    [HttpPost("price/historical")]
    [Authorize(Policy = "OracleRead")]
    [ProducesResponseType(typeof(HistoricalDataResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IActionResult> GetHistoricalData([FromBody] HistoricalDataRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var historicalData = await _priceFeedService.GetHistoricalDataAsync(request);
            return Ok(historicalData);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = ex.Message,
                Status = 400
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving historical data for symbol: {Symbol}", request.Symbol);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving historical data",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get consensus data for a symbol
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="includeDetails">Include detailed consensus information</param>
    /// <returns>Consensus price calculation details</returns>
    [HttpGet("consensus/{symbol}")]
    [Authorize(Policy = "OracleRead")]
    [ProducesResponseType(typeof(ConsensusResult), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> GetConsensus(
        [FromRoute] string symbol,
        [FromQuery] bool includeDetails = false)
    {
        try
        {
            var consensus = await _consensusService.GetLatestConsensusAsync(symbol, includeDetails);
            
            if (consensus == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Consensus Not Found",
                    Detail = $"No consensus data available for symbol: {symbol}",
                    Status = 404
                });
            }

            return Ok(consensus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving consensus for symbol: {Symbol}", symbol);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving consensus data",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Create a new oracle configuration
    /// </summary>
    /// <param name="request">Configuration creation parameters</param>
    /// <returns>Created configuration</returns>
    [HttpPost("config")]
    [Authorize(Policy = "OracleAdmin")]
    [ProducesResponseType(typeof(OracleConfiguration), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CreateConfiguration([FromBody] CreateConfigurationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var configuration = await _oracleDataService.CreateConfigurationAsync(request, userId);

            _logger.LogInformation("Oracle configuration created: {Symbol} by user: {UserId}", 
                request.Symbol, userId);

            return CreatedAtAction(nameof(GetConfiguration), new { id = configuration.Id }, configuration);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Configuration Exists",
                Detail = ex.Message,
                Status = 409
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating oracle configuration for symbol: {Symbol}", request.Symbol);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while creating the configuration",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get oracle configuration by ID
    /// </summary>
    /// <param name="id">Configuration ID</param>
    /// <returns>Oracle configuration details</returns>
    [HttpGet("config/{id:guid}")]
    [Authorize(Policy = "OracleRead")]
    [ProducesResponseType(typeof(OracleConfiguration), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> GetConfiguration([FromRoute] Guid id)
    {
        try
        {
            var configuration = await _oracleDataService.GetConfigurationAsync(id);
            
            if (configuration == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Configuration Not Found",
                    Detail = $"Oracle configuration not found: {id}",
                    Status = 404
                });
            }

            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving oracle configuration: {ConfigId}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving the configuration",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get all oracle configurations
    /// </summary>
    /// <param name="activeOnly">Filter by active configurations only</param>
    /// <returns>List of oracle configurations</returns>
    [HttpGet("config")]
    [Authorize(Policy = "OracleRead")]
    [ProducesResponseType(typeof(List<OracleConfiguration>), 200)]
    public async Task<IActionResult> GetConfigurations([FromQuery] bool activeOnly = true)
    {
        try
        {
            var configurations = await _oracleDataService.GetConfigurationsAsync(activeOnly);
            return Ok(configurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving oracle configurations");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving configurations",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Update oracle configuration
    /// </summary>
    /// <param name="id">Configuration ID</param>
    /// <param name="request">Update parameters</param>
    /// <returns>Updated configuration</returns>
    [HttpPut("config/{id:guid}")]
    [Authorize(Policy = "OracleAdmin")]
    [ProducesResponseType(typeof(OracleConfiguration), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> UpdateConfiguration(
        [FromRoute] Guid id,
        [FromBody] UpdateConfigurationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var configuration = await _oracleDataService.UpdateConfigurationAsync(id, request, userId);
            
            if (configuration == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Configuration Not Found",
                    Detail = $"Oracle configuration not found: {id}",
                    Status = 404
                });
            }

            _logger.LogInformation("Oracle configuration updated: {ConfigId} by user: {UserId}", id, userId);

            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating oracle configuration: {ConfigId}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while updating the configuration",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Delete oracle configuration
    /// </summary>
    /// <param name="id">Configuration ID</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("config/{id:guid}")]
    [Authorize(Policy = "OracleAdmin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> DeleteConfiguration([FromRoute] Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _oracleDataService.DeleteConfigurationAsync(id, userId);
            
            if (!result)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Configuration Not Found",
                    Detail = $"Oracle configuration not found: {id}",
                    Status = 404
                });
            }

            _logger.LogInformation("Oracle configuration deleted: {ConfigId} by user: {UserId}", id, userId);

            return Ok(new { message = "Configuration deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting oracle configuration: {ConfigId}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while deleting the configuration",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Create price feed subscription
    /// </summary>
    /// <param name="request">Subscription parameters</param>
    /// <returns>Created subscription</returns>
    [HttpPost("subscribe")]
    [Authorize(Policy = "OracleWrite")]
    [ProducesResponseType(typeof(OracleSubscription), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IActionResult> CreateSubscription([FromBody] SubscriptionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var subscription = await _oracleDataService.CreateSubscriptionAsync(request);

            _logger.LogInformation("Oracle subscription created: {ClientId} for symbol: {Symbol}", 
                request.ClientId, request.Symbol);

            return CreatedAtAction(nameof(GetSubscription), new { id = subscription.Id }, subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating oracle subscription for client: {ClientId}", request.ClientId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while creating the subscription",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get subscription by ID
    /// </summary>
    /// <param name="id">Subscription ID</param>
    /// <returns>Subscription details</returns>
    [HttpGet("subscribe/{id:guid}")]
    [Authorize(Policy = "OracleRead")]
    [ProducesResponseType(typeof(OracleSubscription), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> GetSubscription([FromRoute] Guid id)
    {
        try
        {
            var subscription = await _oracleDataService.GetSubscriptionAsync(id);
            
            if (subscription == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Subscription Not Found",
                    Detail = $"Oracle subscription not found: {id}",
                    Status = 404
                });
            }

            return Ok(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving oracle subscription: {SubscriptionId}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving the subscription",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get oracle service statistics
    /// </summary>
    /// <returns>Service performance and health statistics</returns>
    [HttpGet("stats")]
    [Authorize(Policy = "OracleRead")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var stats = await _oracleDataService.GetStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving oracle statistics");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while retrieving statistics",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Trigger manual price feed update
    /// </summary>
    /// <param name="symbol">Symbol to update</param>
    /// <returns>Update trigger confirmation</returns>
    [HttpPost("update/{symbol}")]
    [Authorize(Policy = "OracleAdmin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> TriggerUpdate([FromRoute] string symbol)
    {
        try
        {
            var result = await _priceFeedService.TriggerManualUpdateAsync(symbol);
            
            if (!result)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Symbol Not Found",
                    Detail = $"No configuration found for symbol: {symbol}",
                    Status = 404
                });
            }

            _logger.LogInformation("Manual price feed update triggered for symbol: {Symbol}", symbol);

            return Ok(new { message = $"Update triggered for {symbol}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering manual update for symbol: {Symbol}", symbol);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while triggering the update",
                Status = 500
            });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }
}