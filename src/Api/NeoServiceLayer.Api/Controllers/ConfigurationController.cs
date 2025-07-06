using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Configuration;
using NeoServiceLayer.Services.Configuration.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for configuration management operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/configuration")]
[Authorize]
[Tags("Configuration")]
public class ConfigurationController : BaseApiController
{
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationController"/> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="logger">The logger.</param>
    public ConfigurationController(IConfigurationService configurationService, ILogger<ConfigurationController> logger)
        : base(logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
    }

    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The configuration value.</returns>
    /// <response code="200">Configuration value retrieved successfully.</response>
    /// <response code="404">Configuration key not found.</response>
    [HttpGet("{blockchainType}/{key}")]
    [ProducesResponseType(typeof(ApiResponse<ConfigurationItem>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetConfiguration(
        [FromRoute] string key,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _configurationService.GetConfigurationAsync(key, blockchain);

            if (result == null)
            {
                return NotFound(CreateErrorResponse($"Configuration key not found: {key}"));
            }

            return Ok(CreateResponse(result, "Configuration retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving configuration");
        }
    }

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <param name="request">The configuration update request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Success indicator.</returns>
    /// <response code="200">Configuration updated successfully.</response>
    /// <response code="400">Invalid configuration value.</response>
    [HttpPost("{blockchainType}")]
    [Authorize(Roles = "Admin,ConfigManager")]
    [ProducesResponseType(typeof(ApiResponse<ConfigurationUpdateResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> SetConfiguration(
        [FromBody] ConfigurationUpdateRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _configurationService.SetConfigurationAsync(request, blockchain);

            return Ok(CreateResponse(result, "Configuration updated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "updating configuration");
        }
    }

    /// <summary>
    /// Gets all configuration items for a blockchain.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="prefix">Optional key prefix to filter by.</param>
    /// <returns>List of configuration items.</returns>
    /// <response code="200">Configuration items retrieved successfully.</response>
    [HttpGet("{blockchainType}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ConfigurationItem>>), 200)]
    public async Task<IActionResult> GetAllConfigurations(
        [FromRoute] string blockchainType,
        [FromQuery] string? prefix = null)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _configurationService.GetAllConfigurationsAsync(blockchain, prefix);

            return Ok(CreateResponse(result, "Configuration items retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving configuration items");
        }
    }

    /// <summary>
    /// Deletes a configuration value.
    /// </summary>
    /// <param name="key">The configuration key to delete.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Success indicator.</returns>
    /// <response code="200">Configuration deleted successfully.</response>
    /// <response code="404">Configuration key not found.</response>
    [HttpDelete("{blockchainType}/{key}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteConfiguration(
        [FromRoute] string key,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _configurationService.DeleteConfigurationAsync(key, blockchain);

            if (!result)
            {
                return NotFound(CreateErrorResponse($"Configuration key not found: {key}"));
            }

            return Ok(CreateResponse(result, "Configuration deleted successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "deleting configuration");
        }
    }

    /// <summary>
    /// Creates a backup of current configuration.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Backup details.</returns>
    /// <response code="200">Configuration backup created successfully.</response>
    [HttpPost("{blockchainType}/backup")]
    [Authorize(Roles = "Admin,ConfigManager")]
    [ProducesResponseType(typeof(ApiResponse<ConfigurationBackupResult>), 200)]
    public async Task<IActionResult> BackupConfiguration(
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _configurationService.BackupConfigurationAsync(blockchain);

            return Ok(CreateResponse(result, "Configuration backup created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "backing up configuration");
        }
    }

    /// <summary>
    /// Restores configuration from a backup.
    /// </summary>
    /// <param name="request">The restore request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Restore result.</returns>
    /// <response code="200">Configuration restored successfully.</response>
    /// <response code="404">Backup not found.</response>
    [HttpPost("{blockchainType}/restore")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ConfigurationRestoreResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> RestoreConfiguration(
        [FromBody] ConfigurationRestoreRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _configurationService.RestoreConfigurationAsync(request, blockchain);

            return Ok(CreateResponse(result, "Configuration restored successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "restoring configuration");
        }
    }

    /// <summary>
    /// Gets configuration history for a key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="limit">Maximum number of history entries to return.</param>
    /// <returns>Configuration history.</returns>
    /// <response code="200">Configuration history retrieved successfully.</response>
    [HttpGet("{blockchainType}/{key}/history")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ConfigurationHistoryEntry>>), 200)]
    public async Task<IActionResult> GetConfigurationHistory(
        [FromRoute] string key,
        [FromRoute] string blockchainType,
        [FromQuery] int limit = 10)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _configurationService.GetConfigurationHistoryAsync(key, blockchain, limit);

            return Ok(CreateResponse(result, "Configuration history retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving configuration history");
        }
    }
}