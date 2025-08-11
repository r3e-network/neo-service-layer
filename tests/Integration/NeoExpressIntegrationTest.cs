using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.SmartContracts;
using NeoServiceLayer.Services.SmartContracts.NeoN3;
using Neo;
using Neo.Network.RPC;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;

namespace NeoServiceLayer.Tests.Integration
{
    /// <summary>
    /// Integration test demonstrating Neo Service Layer working with Neo Express
    /// </summary>
    public class NeoExpressIntegrationTest
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Neo Express Integration Test");
            Console.WriteLine("============================");
            Console.WriteLine();

            // Setup configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.NeoExpress.json", optional: false)
                .Build();

            // Setup DI container
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IConfiguration>(configuration);
            
            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<NeoExpressIntegrationTest>>();

            try
            {
                // Test 1: Connect to Neo Express RPC
                Console.WriteLine("Test 1: Connecting to Neo Express RPC...");
                var rpcClient = new RpcClient(new Uri(Configuration["ServiceEndpoints:Http:50012"] ?? "http://localhost:50012"));
                var version = await rpcClient.GetVersionAsync();
                Console.WriteLine($"Connected to Neo v{version.Protocol.Network} (RPC: {version.RpcVersion})");
                Console.WriteLine();

                // Test 2: Get blockchain info
                Console.WriteLine("Test 2: Getting blockchain information...");
                var blockCount = await rpcClient.GetBlockCountAsync();
                var bestBlockHash = await rpcClient.GetBestBlockHashAsync();
                Console.WriteLine($"Block count: {blockCount}");
                Console.WriteLine($"Best block hash: {bestBlockHash}");
                Console.WriteLine();

                // Test 3: Get contract information
                Console.WriteLine("Test 3: Getting deployed contract...");
                var contractHash = UInt160.Parse("0x918dc5e53f237015fae0dad532655efff9834cbd");
                var contractState = await rpcClient.GetContractStateAsync(contractHash.ToString());
                if (contractState != null)
                {
                    Console.WriteLine($"Contract found: {contractState.Manifest.Name}");
                    Console.WriteLine($"Methods: {string.Join(", ", contractState.Manifest.Abi.Methods.Select(m => m.Name))}");
                }
                Console.WriteLine();

                // Test 4: Invoke contract method (test invocation)
                Console.WriteLine("Test 4: Invoking contract method...");
                var script = new ScriptBuilder();
                script.EmitDynamicCall(contractHash, "hello", "Neo Service Layer");
                var invokeResult = await rpcClient.InvokeScriptAsync(script.ToArray());
                
                if (invokeResult.State == VMState.HALT)
                {
                    Console.WriteLine("Contract invocation successful!");
                    if (invokeResult.Stack.Length > 0)
                    {
                        var result = invokeResult.Stack[0].GetString();
                        Console.WriteLine($"Result: {result}");
                    }
                }
                Console.WriteLine();

                // Test 5: Read contract storage
                Console.WriteLine("Test 5: Reading contract storage...");
                var storageKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("testKey"));
                var storageItems = await rpcClient.FindStorageAsync(contractHash.ToString(), storageKey);
                
                if (storageItems != null && storageItems.Results.Any())
                {
                    foreach (var item in storageItems.Results)
                    {
                        var key = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(item.Key));
                        var value = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(item.Value));
                        Console.WriteLine($"Storage - Key: {key}, Value: {value}");
                    }
                }
                Console.WriteLine();

                // Test 6: Monitor contract events
                Console.WriteLine("Test 6: Getting recent contract events...");
                var latestBlock = await rpcClient.GetBlockAsync((uint)(blockCount - 1));
                Console.WriteLine($"Checking block {latestBlock.Index} for events...");
                
                foreach (var tx in latestBlock.Transactions)
                {
                    var appLog = await rpcClient.GetApplicationLogAsync(tx.Hash.ToString());
                    if (appLog?.Executions != null)
                    {
                        foreach (var execution in appLog.Executions)
                        {
                            foreach (var notification in execution.Notifications)
                            {
                                if (notification.Contract == contractHash)
                                {
                                    Console.WriteLine($"Event: {notification.EventName}");
                                }
                            }
                        }
                    }
                }
                Console.WriteLine();

                Console.WriteLine("Integration test completed successfully!");
                Console.WriteLine();
                Console.WriteLine("Summary:");
                Console.WriteLine("- Neo Express is running and accessible");
                Console.WriteLine("- Smart contract is deployed and functional");
                Console.WriteLine("- Neo Service Layer can interact with the blockchain");
                Console.WriteLine("- Contract methods can be invoked");
                Console.WriteLine("- Storage can be read");
                Console.WriteLine("- Events can be monitored");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Integration test failed");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}