# Tutorial: Using Neo Service Layer with Neo N3 Smart Contracts

This tutorial demonstrates how to use the Neo Service Layer (NSL) in conjunction with Neo N3 smart contracts to create applications that leverage both blockchain transparency and confidential computing.

## Overview

The Neo Service Layer does not execute smart contracts directly. Instead, it provides a secure environment for executing JavaScript code that can:

1. Access user secrets securely stored in the enclave
2. Perform confidential computations
3. Send callback transactions to the Neo N3 blockchain with the results

In this tutorial, we'll build a simple application that demonstrates this workflow:

1. A Neo N3 smart contract initiates a request for confidential computation
2. The Neo Service Layer executes JavaScript code in the enclave
3. The JavaScript code accesses user secrets and performs confidential computations
4. The results are sent back to the Neo N3 blockchain via a callback transaction

## Prerequisites

- .NET 9.0 or later
- Neo Service Layer installed and configured
- Neo N3 development environment (Neo-CLI, Neo-Express, etc.)
- Basic understanding of C#, JavaScript, and Neo N3 smart contracts

## Step 1: Set Up the Projects

We'll need two projects:
1. A Neo N3 smart contract project
2. A Neo Service Layer client application

### Create the Neo N3 Smart Contract Project

```bash
# Create a new Neo N3 smart contract project
dotnet new neo3-contract -n ConfidentialComputation
cd ConfidentialComputation
```

### Create the Neo Service Layer Client Application

```bash
# Create a new console application for the Neo Service Layer client
dotnet new console -n NSLClient
cd NSLClient
dotnet add package NeoServiceLayer.Client
dotnet add package Neo.SmartContract.Framework
dotnet add package Neo
```

## Step 2: Implement the Neo N3 Smart Contract

Create a smart contract that can initiate requests for confidential computation and receive the results.

```csharp
using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;

namespace ConfidentialComputation
{
    [DisplayName("ConfidentialComputation")]
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "Confidential Computation Contract")]
    public class ConfidentialComputationContract : SmartContract
    {
        // Events
        [DisplayName("ComputationRequested")]
        public static event Action<UInt160, string, string> OnComputationRequested;

        [DisplayName("ComputationCompleted")]
        public static event Action<UInt160, string, string> OnComputationCompleted;

        // Storage keys
        private static readonly byte[] RequestPrefix = new byte[] { 0x01 };
        private static readonly byte[] ResultPrefix = new byte[] { 0x02 };

        // Request a confidential computation
        public static void RequestComputation(string functionId, string input)
        {
            // Get the caller's script hash
            UInt160 caller = Runtime.CallingScriptHash;

            // Ensure the caller is a valid address
            if (!caller.IsValid)
                throw new Exception("Invalid caller");

            // Create a unique request ID
            string requestId = $"{caller}_{Runtime.Time}";

            // Store the request
            StorageMap requests = new StorageMap(Storage.CurrentContext, RequestPrefix);
            requests.Put(requestId, input);

            // Emit an event to notify the Neo Service Layer
            OnComputationRequested(caller, requestId, functionId);

            Runtime.Log($"Computation requested: {requestId}");
        }

        // Callback method for the Neo Service Layer to return the computation result
        public static void ReceiveComputationResult(string requestId, string result)
        {
            // Ensure the caller is authorized (in a real implementation, this would check a whitelist)
            // For simplicity, we're allowing any caller in this example

            // Get the original request
            StorageMap requests = new StorageMap(Storage.CurrentContext, RequestPrefix);
            ByteString requestData = requests.Get(requestId);

            if (requestData is null)
                throw new Exception("Request not found");

            // Store the result
            StorageMap results = new StorageMap(Storage.CurrentContext, ResultPrefix);
            results.Put(requestId, result);

            // Get the original requester from the request ID
            string[] parts = requestId.Split('_');
            UInt160 requester = UInt160.Parse(parts[0]);

            // Emit an event to notify the requester
            OnComputationCompleted(requester, requestId, result);

            Runtime.Log($"Computation completed: {requestId}");
        }

        // Get the result of a computation
        public static string GetComputationResult(string requestId)
        {
            StorageMap results = new StorageMap(Storage.CurrentContext, ResultPrefix);
            ByteString result = results.Get(requestId);

            if (result is null)
                return "Pending";

            return result;
        }
    }
}
```

## Step 3: Implement the JavaScript Function for the Enclave

Create a JavaScript function that will run in the enclave. This function will access user secrets and perform confidential computations.

```javascript
// EncryptionFunction.js

// This function performs encryption using a user's secret key
function main(input) {
    try {
        // Parse the input
        const { data, userId, requestId } = input;

        // Access the user's secret encryption key
        // This is securely stored in the enclave and not accessible outside
        const encryptionKey = SECRETS.encryption_key;

        if (!encryptionKey) {
            return {
                success: false,
                error: "Encryption key not found"
            };
        }

        // Perform the encryption (simplified for this example)
        // In a real implementation, this would use a proper encryption algorithm
        let encryptedData = "";
        for (let i = 0; i < data.length; i++) {
            const charCode = data.charCodeAt(i) ^ encryptionKey.charCodeAt(i % encryptionKey.length);
            encryptedData += String.fromCharCode(charCode);
        }

        // Convert to Base64 for safe transmission
        const base64Result = btoa(encryptedData);

        // Send the result back to the blockchain
        blockchain.callback(
            input.contractHash,
            "ReceiveComputationResult",
            {
                requestId: requestId,
                result: base64Result
            }
        );

        return {
            success: true,
            requestId: requestId,
            result: base64Result
        };
    } catch (error) {
        return {
            success: false,
            error: error.message
        };
    }
}
```

## Step 4: Implement the Neo Service Layer Client

Create a client application that listens for events from the Neo N3 blockchain, executes JavaScript functions in the enclave, and sends the results back to the blockchain.

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neo;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using NeoServiceLayer.Tee.Host;
using NeoServiceLayer.Tee.Shared;

namespace NSLClient
{
    class Program
    {
        // Configuration
        private static readonly string RpcUrl = "http://localhost:10332";
        private static readonly string ContractHash = "0x1234567890abcdef1234567890abcdef12345678"; // Replace with your contract hash
        private static readonly string WalletPath = "wallet.json";
        private static readonly string WalletPassword = "password";

        static async Task Main(string[] args)
        {
            // Set up logging
            var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            var logger = loggerFactory.CreateLogger<Program>();

            // Initialize the enclave host
            logger.LogInformation("Initializing enclave host...");
            var enclaveHost = new TeeEnclaveHost(loggerFactory, simulationMode: true);
            var enclaveInterface = enclaveHost.GetEnclaveInterface();

            // Store the encryption function in the enclave
            await StoreEncryptionFunction(enclaveInterface, logger);

            // Store a user secret in the enclave
            await StoreUserSecret(enclaveInterface, logger);

            // Start listening for blockchain events
            await StartEventListener(enclaveInterface, logger);

            // Keep the application running
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static async Task StoreEncryptionFunction(ITeeInterface enclaveInterface, ILogger logger)
        {
            // Read the function code from file
            string functionCode = System.IO.File.ReadAllText("EncryptionFunction.js");

            // Store the function in the enclave
            string functionId = "encryption-function";
            string userId = "owner";

            bool result = await enclaveInterface.StoreJavaScriptFunctionAsync(
                functionId, functionCode, userId);

            if (result)
                logger.LogInformation("Encryption function stored successfully");
            else
                logger.LogError("Failed to store encryption function");
        }

        static async Task StoreUserSecret(ITeeInterface enclaveInterface, ILogger logger)
        {
            // Store a secret encryption key for a user
            string userId = "user1";
            string secretName = "encryption_key";
            string secretValue = "ThisIsASecretKey123!";

            bool result = await enclaveInterface.StoreUserSecretAsync(
                userId, secretName, secretValue);

            if (result)
                logger.LogInformation("User secret stored successfully");
            else
                logger.LogError("Failed to store user secret");
        }

        static async Task StartEventListener(ITeeInterface enclaveInterface, ILogger logger)
        {
            logger.LogInformation("Starting event listener...");

            // Initialize RPC client
            RpcClient client = new RpcClient(new Uri(RpcUrl));

            // Open the wallet
            WalletAPI wallet = new WalletAPI(WalletPath);
            wallet.Open(WalletPassword);

            // Subscribe to ComputationRequested events
            await client.SubscribeAsync("notifications", async notification => {
                try {
                    // Check if this is a ComputationRequested event from our contract
                    if (notification.Contract == ContractHash &&
                        notification.EventName == "ComputationRequested")
                    {
                        // Parse the event parameters
                        UInt160 caller = (UInt160)notification.State[0].GetObject();
                        string requestId = notification.State[1].GetString();
                        string functionId = notification.State[2].GetString();

                        logger.LogInformation($"Computation requested: {requestId}, Function: {functionId}");

                        // Get the request input from the contract
                        string input = await GetRequestInput(client, requestId);

                        // Execute the function in the enclave
                        await ExecuteFunction(enclaveInterface, functionId, caller.ToString(), requestId, input, logger);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error processing event: {ex.Message}");
                }
            });

            logger.LogInformation("Event listener started");
        }

        static async Task<string> GetRequestInput(RpcClient client, string requestId)
        {
            // Call the contract to get the request input
            // This is a simplified example - in a real implementation, you would use the Neo SDK
            // to call the contract and get the request input

            // For this example, we'll just return a dummy input
            return "This is some confidential data that needs to be encrypted";
        }

        static async Task ExecuteFunction(ITeeInterface enclaveInterface, string functionId,
            string userId, string requestId, string input, ILogger logger)
        {
            // Prepare the input for the JavaScript function
            string jsInput = $@"{{
                ""data"": ""{input}"",
                ""userId"": ""{userId}"",
                ""requestId"": ""{requestId}"",
                ""contractHash"": ""{ContractHash}""
            }}";

            // Execute the function in the enclave
            logger.LogInformation($"Executing function {functionId} in the enclave");
            var result = await enclaveInterface.ExecuteJavaScriptAsync(
                "", jsInput, "{}", functionId, userId);

            // Parse the result
            var resultObj = System.Text.Json.JsonDocument.Parse(result.Result).RootElement;
            bool success = resultObj.GetProperty("success").GetBoolean();

            if (success)
            {
                string computationResult = resultObj.GetProperty("result").GetString();
                logger.LogInformation($"Function executed successfully: {computationResult}");

                // Send the result back to the blockchain
                await SendResultToBlockchain(requestId, computationResult, logger);
            }
            else
            {
                string error = resultObj.GetProperty("error").GetString();
                logger.LogError($"Function execution failed: {error}");
            }
        }

        static async Task SendResultToBlockchain(string requestId, string result, ILogger logger)
        {
            logger.LogInformation($"Sending result to blockchain: {requestId}");

            // Initialize RPC client
            RpcClient client = new RpcClient(new Uri(RpcUrl));

            // Open the wallet
            WalletAPI wallet = new WalletAPI(WalletPath);
            wallet.Open(WalletPassword);

            // Get the account to use for the transaction
            WalletAccount account = wallet.GetDefaultAccount();

            // Create a script to call the ReceiveComputationResult method
            UInt160 contractHash = UInt160.Parse(ContractHash.Substring(2), 16);
            using ScriptBuilder scriptBuilder = new ScriptBuilder();
            scriptBuilder.EmitDynamicCall(contractHash, "ReceiveComputationResult", requestId, result);

            // Create and send the transaction
            Transaction tx = await wallet.MakeTransactionAsync(scriptBuilder.ToArray(), account.ScriptHash);

            // Sign the transaction
            tx.Sign(account.KeyPair);

            // Send the transaction
            await client.SendRawTransactionAsync(tx);

            logger.LogInformation($"Result sent to blockchain: {requestId}");
        }
    }
}
```

## Step 5: Run the Application

1. Deploy the Neo N3 smart contract to the blockchain:
   ```bash
   cd ConfidentialComputation
   dotnet build
   # Deploy the contract using Neo-CLI or Neo-Express
   ```

2. Run the Neo Service Layer client:
   ```bash
   cd NSLClient
   dotnet run
   ```

3. Interact with the smart contract to request a confidential computation:
   ```bash
   # Using Neo-CLI or Neo-Express
   invoke <contract-hash> RequestComputation encryption-function "This is some confidential data"
   ```

4. The Neo Service Layer client will:
   - Detect the ComputationRequested event
   - Execute the JavaScript function in the enclave
   - Send the result back to the blockchain

5. Check the result of the computation:
   ```bash
   # Using Neo-CLI or Neo-Express
   invoke <contract-hash> GetComputationResult <request-id>
   ```

## How It Works

1. **Smart Contract Initiates Request**: The Neo N3 smart contract initiates a request for confidential computation by emitting an event.

2. **Neo Service Layer Detects Event**: The Neo Service Layer client listens for events from the blockchain and detects the computation request.

3. **Enclave Executes JavaScript**: The Neo Service Layer executes the JavaScript function in the enclave, which has access to user secrets.

4. **Callback to Blockchain**: The Neo Service Layer sends the result back to the blockchain by calling the smart contract's callback method.

5. **Result Verification**: The smart contract verifies the result and makes it available to the requester.

## Security Considerations

- **User Secrets**: User secrets are stored securely in the enclave and are not accessible outside the enclave.
- **Confidential Computation**: The computation is performed securely within the enclave, ensuring that sensitive data is not exposed.
- **Callback Authentication**: In a production environment, you should implement proper authentication for the callback to ensure that only authorized entities can send results to the blockchain.

## Conclusion

This tutorial demonstrated how to use the Neo Service Layer in conjunction with Neo N3 smart contracts to create applications that leverage both blockchain transparency and confidential computing. By using this approach, you can:

1. Store sensitive data securely in the enclave
2. Perform confidential computations on that data
3. Send the results back to the blockchain for transparent verification and use

This pattern is useful for a wide range of applications, including:
- Privacy-preserving voting systems
- Confidential financial transactions
- Secure identity verification
- Private data marketplaces

For more information, see the [Neo Service Layer Documentation](../index.md).
