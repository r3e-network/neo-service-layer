using Bogus;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.TestInfrastructure.TestDataBuilders;

/// <summary>
/// Test data builder for generating realistic blockchain-related test data.
/// </summary>
public class BlockchainTestDataBuilder
{
    private readonly Faker _faker;
    private readonly Random _random;

    public BlockchainTestDataBuilder()
    {
        _faker = new Faker();
        _random = new Random(42); // Fixed seed for reproducible tests
    }

    #region Address Generation

    /// <summary>
    /// Generates a valid Neo N3 address.
    /// </summary>
    public string GenerateNeoN3Address()
    {
        // Generate a valid-looking Neo N3 address using base58 characters
        // Base58 excludes 0, O, I, and l
        const string base58Chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        var addressChars = new char[33];

        for (int i = 0; i < 33; i++)
        {
            addressChars[i] = base58Chars[_faker.Random.Int(0, base58Chars.Length - 1)];
        }

        // Neo N3 addresses start with 'N'
        return $"N{new string(addressChars)}";
    }

    /// <summary>
    /// Generates a valid Neo X (Ethereum-style) address.
    /// </summary>
    public string GenerateNeoXAddress()
    {
        return $"0x{_faker.Random.Hexadecimal(40)}";
    }

    /// <summary>
    /// Generates an address for the specified blockchain type.
    /// </summary>
    public string GenerateAddress(BlockchainType blockchainType)
    {
        return blockchainType switch
        {
            BlockchainType.NeoN3 => GenerateNeoN3Address(),
            BlockchainType.NeoX => GenerateNeoXAddress(),
            _ => throw new NotSupportedException($"Blockchain type {blockchainType} not supported")
        };
    }

    #endregion

    #region Transaction Generation

    /// <summary>
    /// Generates a realistic transaction for testing.
    /// </summary>
    public Transaction GenerateTransaction(
        BlockchainType blockchainType = BlockchainType.NeoN3,
        decimal? value = null,
        string? hash = null,
        DateTime? timestamp = null)
    {
        return new Transaction
        {
            Hash = hash ?? GenerateTransactionHash(),
            From = GenerateAddress(blockchainType),
            To = GenerateAddress(blockchainType),
            Value = value ?? GenerateRealisticAmount(),
            Data = GenerateTransactionData(),
            BlockHash = GenerateBlockHash(),
            BlockHeight = _faker.Random.Long(1, 10000000),
            Timestamp = (timestamp ?? _faker.Date.RecentOffset(30)).DateTime
        };
    }

    /// <summary>
    /// Generates multiple transactions for testing batch operations.
    /// </summary>
    public List<Transaction> GenerateTransactions(
        int count,
        BlockchainType blockchainType = BlockchainType.NeoN3,
        bool relatedTransactions = false)
    {
        var transactions = new List<Transaction>();
        var baseValue = GenerateRealisticAmount();
        var commonSender = relatedTransactions ? GenerateAddress(blockchainType) : null;

        for (int i = 0; i < count; i++)
        {
            var transaction = GenerateTransaction(
                blockchainType,
                relatedTransactions ? baseValue * (1 + i * 0.1m) : null);

            if (relatedTransactions && commonSender != null)
            {
                transaction.Sender = commonSender;
            }

            transactions.Add(transaction);
        }

        return transactions;
    }

    /// <summary>
    /// Generates a high-value transaction for MEV testing.
    /// </summary>
    public Transaction GenerateHighValueTransaction(BlockchainType blockchainType = BlockchainType.NeoX)
    {
        return GenerateTransaction(
            blockchainType,
            value: _faker.Random.Decimal(100000, 10000000), // High value
            timestamp: DateTime.UtcNow);
    }

    /// <summary>
    /// Generates a DEX swap transaction for testing.
    /// </summary>
    public Transaction GenerateDexSwapTransaction(BlockchainType blockchainType = BlockchainType.NeoX)
    {
        var transaction = GenerateTransaction(blockchainType);
        transaction.Data = GenerateDexSwapData();
        transaction.Value = _faker.Random.Decimal(1000, 100000);
        return transaction;
    }

    #endregion

    #region Block Generation

    /// <summary>
    /// Generates a realistic block for testing.
    /// </summary>
    public Block GenerateBlock(
        long? height = null,
        int transactionCount = 0,
        string? hash = null,
        DateTime? timestamp = null)
    {
        var blockHeight = height ?? _faker.Random.Long(1, 10000000);
        var transactions = transactionCount > 0
            ? GenerateTransactions(transactionCount)
            : new List<Transaction>();

        return new Block
        {
            Hash = hash ?? GenerateBlockHash(),
            Height = blockHeight,
            Timestamp = (timestamp ?? _faker.Date.RecentOffset(7)).DateTime,
            PreviousHash = GenerateBlockHash(),
            Transactions = transactions
        };
    }

    /// <summary>
    /// Generates a sequence of connected blocks for testing.
    /// </summary>
    public List<Block> GenerateBlockchain(
        int blockCount,
        int avgTransactionsPerBlock = 10)
    {
        var blocks = new List<Block>();
        string? previousHash = null;

        for (int i = 0; i < blockCount; i++)
        {
            var transactionCount = Math.Max(0, avgTransactionsPerBlock + _random.Next(-5, 6));
            var block = GenerateBlock(
                height: i + 1,
                transactionCount: transactionCount);

            if (previousHash != null)
            {
                block.PreviousHash = previousHash;
            }

            blocks.Add(block);
            previousHash = block.Hash;
        }

        return blocks;
    }

    #endregion

    #region Smart Contract Data

    /// <summary>
    /// Generates contract event data for testing.
    /// </summary>
    public TestContractEvent GenerateContractEvent(
        string? contractAddress = null,
        string? eventName = null,
        Dictionary<string, object>? parameters = null)
    {
        return new TestContractEvent
        {
            ContractAddress = contractAddress ?? GenerateNeoXAddress(),
            EventName = eventName ?? _faker.PickRandom("Transfer", "Approval", "Mint", "Burn", "Swap"),
            Parameters = parameters ?? GenerateEventParameters(),
            TransactionHash = GenerateTransactionHash(),
            BlockNumber = _faker.Random.Long(1, 10000000),
            Timestamp = _faker.Date.RecentOffset(7).DateTime
        };
    }

    /// <summary>
    /// Generates realistic contract interaction data.
    /// </summary>
    public Dictionary<string, object> GenerateContractCallData(string methodName)
    {
        return methodName switch
        {
            "transfer" => new Dictionary<string, object>
            {
                ["to"] = GenerateNeoXAddress(),
                ["amount"] = _faker.Random.Decimal(1, 10000).ToString()
            },
            "approve" => new Dictionary<string, object>
            {
                ["spender"] = GenerateNeoXAddress(),
                ["amount"] = _faker.Random.Decimal(1, 100000).ToString()
            },
            "swap" => new Dictionary<string, object>
            {
                ["amountIn"] = _faker.Random.Decimal(100, 10000).ToString(),
                ["amountOutMin"] = _faker.Random.Decimal(90, 9500).ToString(),
                ["path"] = new[] { GenerateNeoXAddress(), GenerateNeoXAddress() },
                ["deadline"] = DateTimeOffset.UtcNow.AddMinutes(20).ToUnixTimeSeconds()
            },
            _ => new Dictionary<string, object>
            {
                ["param1"] = _faker.Random.AlphaNumeric(32),
                ["param2"] = _faker.Random.Int(1, 1000000)
            }
        };
    }

    #endregion

    #region Market Data Generation

    /// <summary>
    /// Generates realistic market price data for testing.
    /// </summary>
    public List<PricePoint> GenerateMarketData(
        string symbol,
        int dataPoints,
        decimal basePrice = 100m,
        double volatility = 0.05)
    {
        var prices = new List<PricePoint>();
        var currentPrice = basePrice;
        var startTime = DateTime.UtcNow.AddDays(-dataPoints);

        for (int i = 0; i < dataPoints; i++)
        {
            var change = (_random.NextDouble() - 0.5) * 2 * volatility;
            currentPrice *= (decimal)(1 + change);
            currentPrice = Math.Max(0.01m, currentPrice); // Prevent negative prices

            prices.Add(new PricePoint
            {
                Symbol = symbol,
                Price = Math.Round(currentPrice, 4),
                Volume = _faker.Random.Decimal(1000, 1000000),
                Timestamp = startTime.AddHours(i),
                High = Math.Round(currentPrice * 1.02m, 4),
                Low = Math.Round(currentPrice * 0.98m, 4),
                Open = prices.LastOrDefault()?.Price ?? currentPrice
            });
        }

        return prices;
    }

    /// <summary>
    /// Generates trending market data with specific patterns.
    /// </summary>
    public List<PricePoint> GenerateTrendingMarketData(
        string symbol,
        int dataPoints,
        MarketTrend trend,
        decimal basePrice = 100m)
    {
        var prices = new List<PricePoint>();
        var currentPrice = basePrice;
        var startTime = DateTime.UtcNow.AddDays(-dataPoints);

        for (int i = 0; i < dataPoints; i++)
        {
            var trendFactor = trend switch
            {
                MarketTrend.Bullish => 0.001 + (_random.NextDouble() * 0.02),
                MarketTrend.Bearish => -0.001 - (_random.NextDouble() * 0.02),
                MarketTrend.Sideways => (_random.NextDouble() - 0.5) * 0.01,
                MarketTrend.Volatile => (_random.NextDouble() - 0.5) * 0.1,
                _ => (_random.NextDouble() - 0.5) * 0.02
            };

            currentPrice *= (decimal)(1 + trendFactor);
            currentPrice = Math.Max(0.01m, currentPrice);

            prices.Add(new PricePoint
            {
                Symbol = symbol,
                Price = Math.Round(currentPrice, 4),
                Volume = _faker.Random.Decimal(1000, 1000000),
                Timestamp = startTime.AddHours(i),
                High = Math.Round(currentPrice * 1.01m, 4),
                Low = Math.Round(currentPrice * 0.99m, 4),
                Open = prices.LastOrDefault()?.Price ?? currentPrice
            });
        }

        return prices;
    }

    #endregion

    #region Helper Methods

    private string GenerateTransactionHash()
    {
        return $"0x{_faker.Random.Hexadecimal(64)}";
    }

    private string GenerateBlockHash()
    {
        return $"0x{_faker.Random.Hexadecimal(64)}";
    }

    private string GenerateMerkleRoot()
    {
        return $"0x{_faker.Random.Hexadecimal(64)}";
    }

    private decimal GenerateRealisticAmount()
    {
        return _faker.Random.Decimal(0.001m, 10000m);
    }

    private string GenerateTransactionData()
    {
        var methodSignature = _faker.PickRandom(
            "0xa9059cbb", // transfer
            "0x095ea7b3", // approve
            "0x23b872dd", // transferFrom
            "0x7ff36ab5"  // swapExactETHForTokens
        );
        var parameterData = _faker.Random.Hexadecimal(128);
        return $"{methodSignature}{parameterData}";
    }

    private string GenerateDexSwapData()
    {
        return $"0x7ff36ab5{_faker.Random.Hexadecimal(256)}";
    }

    private string GenerateEventData()
    {
        return $"0x{_faker.Random.Hexadecimal(128)}";
    }

    private Dictionary<string, object> GenerateEventParameters()
    {
        return new Dictionary<string, object>
        {
            ["from"] = GenerateNeoXAddress(),
            ["to"] = GenerateNeoXAddress(),
            ["value"] = _faker.Random.Decimal(1, 100000).ToString(),
            ["tokenId"] = _faker.Random.Int(1, 999999)
        };
    }

    private long CalculateBlockSize(List<Transaction> transactions)
    {
        return 1000 + (transactions.Count * 250); // Rough estimate
    }

    #endregion
}

#region Supporting Types

public class PricePoint
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Volume { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Open { get; set; }
}

public enum MarketTrend
{
    Bullish,
    Bearish,
    Sideways,
    Volatile
}

#endregion
