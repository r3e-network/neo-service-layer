using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.AbstractAccount;
using NeoServiceLayer.Services.AbstractAccount.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;


namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// API controller for Abstract Account operations.
/// </summary>
[ApiController]
[Route("api/v1/abstract-account")]
[Tags("Abstract Account")]
public class AbstractAccountController : ControllerBase
{
    private readonly IAbstractAccountService _abstractAccountService;
    private readonly ILogger<AbstractAccountController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractAccountController"/> class.
    /// </summary>
    /// <param name="abstractAccountService">The abstract account service.</param>
    /// <param name="logger">The logger.</param>
    public AbstractAccountController(
        IAbstractAccountService abstractAccountService,
        ILogger<AbstractAccountController> logger)
    {
        _abstractAccountService = abstractAccountService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new abstract account.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,KeyManager")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        try
        {
            var result = await _abstractAccountService.CreateAccountAsync(request, BlockchainType.NeoN3);
            _logger.LogInformation("Created abstract account {AccountId}", result.AccountId);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating abstract account");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets account information.
    /// </summary>
    [HttpGet("{accountId}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    public async Task<IActionResult> GetAccount(string accountId)
    {
        try
        {
            var result = await _abstractAccountService.GetAccountInfoAsync(accountId, BlockchainType.NeoN3);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account {AccountId}", accountId);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Adds a guardian for an account.
    /// </summary>
    [HttpPost("{accountId}/guardians")]
    [Authorize(Roles = "Admin,KeyManager")]
    public async Task<IActionResult> AddGuardian(string accountId, [FromBody] object request)
    {
        return StatusCode(403, new { success = false, message = "Forbidden" });
    }

    /// <summary>
    /// Gets guardians for an account.
    /// </summary>
    [HttpGet("{accountId}/guardians")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    public async Task<IActionResult> GetGuardians(string accountId)
    {
        return Ok(new { success = true, data = new List<object>() });
    }
}
