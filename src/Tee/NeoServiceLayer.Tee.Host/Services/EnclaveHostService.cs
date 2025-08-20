using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Tee.Host.Services;

/// <summary>
/// Hosted service for managing the enclave.
/// </summary>
public class EnclaveHostService : IHostedService
{
    private readonly ILogger<EnclaveHostService> _logger;
    private readonly IEnclaveManager _enclaveManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveHostService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="enclaveManager">The enclave manager.</param>
    public EnclaveHostService(ILogger<EnclaveHostService> logger, IEnclaveManager enclaveManager)
    {
        _logger = logger;
        _enclaveManager = enclaveManager;
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting enclave host service...");
        bool result = await _enclaveManager.InitializeEnclaveAsync();
        if (result)
        {
            _logger.LogInformation("Enclave host service started successfully.");
        }
        else
        {
            _logger.LogError("Failed to start enclave host service.");
        }
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping enclave host service...");
        bool result = await _enclaveManager.DestroyEnclaveAsync();
        if (result)
        {
            _logger.LogInformation("Enclave host service stopped successfully.");
        }
        else
        {
            _logger.LogError("Failed to stop enclave host service.");
        }
    }
}
