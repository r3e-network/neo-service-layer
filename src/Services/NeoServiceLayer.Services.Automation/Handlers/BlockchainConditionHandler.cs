using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Automation.Services;

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
                
                _logger.LogDebug("Blockchain condition evaluated: {Field} {Operator} {Value} = {Result}",
                    condition.Field, condition.Operator, condition.Value, result);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating blockchain condition");
                return false;
            }
        }

        private async Task<string> GetBlockchainDataAsync(string field)
        {
            if (_blockchainClientFactory == null)
            {
                _logger.LogWarning("Blockchain client factory not available");
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
                    "neo" or "neon3" => _blockchainClientFactory.GetClient(BlockchainType.NeoN3),
                    "neox" => _blockchainClientFactory.GetClient(BlockchainType.NeoX),
                    _ => null
                };

                if (client == null)
                {
                    _logger.LogWarning("Unknown blockchain: {Blockchain}", blockchain);
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
                _logger.LogError(ex, "Error fetching blockchain data for {Field}", field);
                return string.Empty;
            }
        }
    }
}