using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Infrastructure.Blockchain;
using NeoServiceLayer.Services.Automation.Models;
using NeoServiceLayer.Services.Automation.Services;
using NeoServiceLayer.ServiceFramework;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Automation.Handlers
{
    /// <summary>
    /// Handles blockchain-related condition evaluations.
    /// </summary>
    public class BlockchainConditionHandler : ConditionHandlerBase
    {
        private readonly IBlockchainClientFactory? _blockchainClientFactory;

        public BlockchainConditionHandler(
            ILogger<BlockchainConditionHandler> logger,
            IBlockchainClientFactory? blockchainClientFactory = null)
            : base(logger)
        {
            _blockchainClientFactory = blockchainClientFactory;
        }

        public override AutomationConditionType SupportedType => AutomationConditionType.Blockchain;

        public override async Task<bool> EvaluateAsync(AutomationCondition condition)
        {
            ArgumentNullException.ThrowIfNull(condition);

            try
            {
                var actualValue = await GetBlockchainDataAsync(condition.Field).ConfigureAwait(false);
                var result = CompareValues(actualValue, condition.Value, condition.Operator);

                Logger.LogDebug("Blockchain condition evaluated: {Field} {Operator} {Value} = {Result}",
                    condition.Field, condition.Operator, condition.Value, result);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error evaluating blockchain condition");
                return false;
            }
        }

        private async Task<string> GetBlockchainDataAsync(string field)
        {
            if (_blockchainClientFactory == null)
            {
                Logger.LogWarning("Blockchain client factory not available");
                return string.Empty;
            }

            // Parse field format: blockchain.property
            var parts = field.Split('.');
            if (parts.Length < 2)
            {
                return string.Empty;
            }

            var blockchain = parts[0].ToLowerInvariant();
            var property = parts[1].ToLowerInvariant();

            try
            {
                var client = blockchain switch
                {
                    "neo" or "neon3" => _blockchainClientFactory.CreateClient(BlockchainType.NeoN3),
                    "neox" => _blockchainClientFactory.CreateClient(BlockchainType.NeoX),
                    _ => null
                };

                if (client == null)
                {
                    Logger.LogWarning("Unknown blockchain: {Blockchain}", blockchain);
                    return string.Empty;
                }

                return property switch
                {
                    "blockheight" => (await client.GetBlockHeightAsync().ConfigureAwait(false)).ToString(),
                    "gasprice" => (await client.GetGasPriceAsync().ConfigureAwait(false)).ToString(),
                    _ => string.Empty
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error fetching blockchain data for {Field}", field);
                return string.Empty;
            }
        }
    }
}
