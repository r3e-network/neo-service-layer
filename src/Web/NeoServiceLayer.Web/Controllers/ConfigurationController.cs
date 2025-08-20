using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Configuration;
using NeoServiceLayer.Services.Configuration.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;


namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// API controller for configuration management operations.
/// </summary>
[Tags("Configuration")]
public class ConfigurationController : BaseApiController
{
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationController"/> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="logger">The logger.</param>
    public ConfigurationController(
        IConfigurationService configurationService,
        ILogger<ConfigurationController> logger) : base(logger)
    {
        _configurationService = configurationService;
    }

    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    /// <param name="request">The configuration get request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The configuration value.</returns>
    /// <response code="200">Configuration value retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Configuration not found.</response>
    [HttpPost("get/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<ConfigurationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetConfiguration(
        [FromBody] GetConfigurationRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _configurationService.GetConfigurationAsync(request, blockchain);

            if (!result.Found)
            {
                return NotFound(CreateErrorResponse($"Configuration key '{request.Key}' not found"));
            }

            Logger.LogInformation("Retrieved configuration {Key} for user {UserId} on {BlockchainType}",
                request.Key, GetCurrentUserId(), blockchainType);

            return Ok(CreateResponse(result, "Configuration retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetConfiguration");
        }
    }

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <param name="request">The configuration set request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The configuration set result.</returns>
    /// <response code="200">Configuration set successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Configuration set failed.</response>
    [HttpPost("set/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ConfigurationSetResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> SetConfiguration(
        [FromBody] SetConfigurationRequest request,
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

            Logger.LogInformation("Set configuration {Key} to value {Value} by user {UserId} on {BlockchainType}",
                request.Key, request.Value, GetCurrentUserId(), blockchainType);

            return Ok(CreateResponse(result, "Configuration set successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "SetConfiguration");
        }
    }

    /// <summary>
    /// Deletes a configuration value.
    /// </summary>
    /// <param name="request">The configuration delete request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The configuration delete result.</returns>
    /// <response code="200">Configuration deleted successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Configuration not found.</response>
    [HttpDelete("{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ConfigurationDeleteResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteConfiguration(
        [FromBody] DeleteConfigurationRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _configurationService.DeleteConfigurationAsync(request, blockchain);

            Logger.LogInformation("Deleted configuration {Key} by user {UserId} on {BlockchainType}",
                request.Key, GetCurrentUserId(), blockchainType);

            return Ok(CreateResponse(result, "Configuration deleted successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "DeleteConfiguration");
        }
    }

    /// <summary>
    /// Lists configuration keys and values.
    /// </summary>
    /// <param name="request">The configuration list request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The configuration list result.</returns>
    /// <response code="200">Configuration list retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("list/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<ConfigurationListResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> ListConfigurations(
        [FromBody] ListConfigurationsRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _configurationService.ListConfigurationsAsync(request, blockchain);

            Logger.LogInformation("Listed configurations for scope {Scope} by user {UserId} on {BlockchainType}",
                request.Scope, GetCurrentUserId(), blockchainType);

            return Ok(CreateResponse(result, "Configuration list retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "ListConfigurations");
        }
    }

    /// <summary>
    /// Validates configuration values.
    /// </summary>
    /// <param name="request">The configuration validation request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The validation result.</returns>
    /// <response code="200">Configuration validation completed.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("validate/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<ConfigurationValidationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> ValidateConfiguration(
        [FromBody] ValidateConfigurationRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _configurationService.ValidateConfigurationAsync(request, blockchain);

            Logger.LogInformation("Validated configuration by user {UserId} on {BlockchainType}",
                GetCurrentUserId(), blockchainType);

            return Ok(CreateResponse(result, "Configuration validation completed"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "ValidateConfiguration");
        }
    }

    /// <summary>
    /// Creates a configuration schema.
    /// </summary>
    /// <param name="request">The schema creation request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The schema creation result.</returns>
    /// <response code="200">Configuration schema created successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Schema creation failed.</response>
    [HttpPost("schema/create/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ConfigurationSchemaResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> CreateSchema(
        [FromBody] CreateSchemaRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _configurationService.CreateSchemaAsync(request, blockchain);

            Logger.LogInformation("Created configuration schema {SchemaName} by user {UserId} on {BlockchainType}",
                request.SchemaName, GetCurrentUserId(), blockchainType);

            return Ok(CreateResponse(result, "Configuration schema created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "CreateSchema");
        }
    }

    /// <summary>
    /// Exports configuration data.
    /// </summary>
    /// <param name="request">The configuration export request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The export result.</returns>
    /// <response code="200">Configuration exported successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Export failed.</response>
    [HttpPost("export/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ConfigurationExportResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ExportConfiguration(
        [FromBody] ExportConfigurationRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _configurationService.ExportConfigurationAsync(request, blockchain);

            Logger.LogInformation("Exported configuration with format {Format} by user {UserId} on {BlockchainType}",
                request.Format, GetCurrentUserId(), blockchainType);

            return Ok(CreateResponse(result, "Configuration exported successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "ExportConfiguration");
        }
    }

    /// <summary>
    /// Imports configuration data.
    /// </summary>
    /// <param name="request">The configuration import request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The import result.</returns>
    /// <response code="200">Configuration imported successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Import failed.</response>
    [HttpPost("import/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ConfigurationImportResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ImportConfiguration(
        [FromBody] ImportConfigurationRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _configurationService.ImportConfigurationAsync(request, blockchain);

            Logger.LogInformation("Imported configuration from format {Format} by user {UserId} on {BlockchainType}",
                request.Format, GetCurrentUserId(), blockchainType);

            return Ok(CreateResponse(result, "Configuration imported successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "ImportConfiguration");
        }
    }

    /// <summary>
    /// Subscribes to configuration changes.
    /// </summary>
    /// <param name="request">The configuration subscription request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The subscription result.</returns>
    /// <response code="200">Subscription created successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Subscription failed.</response>
    [HttpPost("subscribe/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<ConfigurationSubscriptionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> SubscribeToChanges(
        [FromBody] SubscribeToChangesRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _configurationService.SubscribeToChangesAsync(request, blockchain);

            Logger.LogInformation("Created configuration subscription {SubscriptionId} by user {UserId} on {BlockchainType}",
                result.SubscriptionId, GetCurrentUserId(), blockchainType);

            return Ok(CreateResponse(result, "Subscription created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "SubscribeToChanges");
        }
    }

    /// <summary>
    /// Gets configuration change history.
    /// </summary>
    /// <param name="request">The configuration history request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The configuration history.</returns>
    /// <response code="200">Configuration history retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("history/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<ConfigurationHistoryResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetConfigurationHistory(
        [FromBody] GetConfigurationHistoryRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _configurationService.GetConfigurationHistoryAsync(request, blockchain);

            Logger.LogInformation("Retrieved configuration history for key {Key} by user {UserId} on {BlockchainType}",
                request.Key, GetCurrentUserId(), blockchainType);

            return Ok(CreateResponse(result, "Configuration history retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetConfigurationHistory");
        }
    }
}
