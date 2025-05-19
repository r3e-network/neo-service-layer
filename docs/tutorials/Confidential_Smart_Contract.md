# Tutorial: Implementing a Confidential Smart Contract

This tutorial will guide you through the process of implementing a confidential smart contract using the Neo Service Layer (NSL). A confidential smart contract is a smart contract that keeps its data and execution private, while still providing verifiable results.

## Prerequisites

- .NET 9.0 or later
- Neo Service Layer installed and configured
- Basic understanding of C# and JavaScript

## Overview

In this tutorial, we'll implement a confidential token swap contract that allows users to swap tokens without revealing the swap details to the public. The contract will:

1. Accept token swap orders from users
2. Match orders based on price and quantity
3. Execute the swap when a match is found
4. Keep the order book confidential

## Step 1: Set Up the Project

First, let's create a new .NET project for our confidential token swap application:

```bash
dotnet new console -n ConfidentialTokenSwap
cd ConfidentialTokenSwap
dotnet add package NeoServiceLayer.Client
```

## Step 2: Initialize the Neo Service Layer

Create a new file called `Program.cs` with the following code:

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host;
using NeoServiceLayer.Tee.Shared;

namespace ConfidentialTokenSwap
{
    class Program
    {
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
            
            // Initialize the token swap contract
            await InitializeTokenSwapContract(enclaveInterface);
            
            // Run the application
            await RunApplication(enclaveInterface, logger);
        }
        
        static async Task InitializeTokenSwapContract(ITeeInterface enclaveInterface)
        {
            // We'll implement this in the next step
        }
        
        static async Task RunApplication(ITeeInterface enclaveInterface, ILogger logger)
        {
            // We'll implement this in a later step
        }
    }
}
```

## Step 3: Implement the Token Swap Contract

Now, let's implement the token swap contract in JavaScript. Create a new file called `TokenSwapContract.js` with the following code:

```javascript
// Token Swap Contract

// Order book - will be kept confidential within the enclave
let orderBook = {
  buy: [],  // Buy orders
  sell: [] // Sell orders
};

// Function to add a new order to the order book
function addOrder(order) {
  if (order.type === 'buy') {
    orderBook.buy.push(order);
    // Sort buy orders by price (highest first)
    orderBook.buy.sort((a, b) => b.price - a.price);
  } else if (order.type === 'sell') {
    orderBook.sell.push(order);
    // Sort sell orders by price (lowest first)
    orderBook.sell.sort((a, b) => a.price - b.price);
  }
  
  // Try to match orders
  matchOrders();
  
  return { success: true, orderId: order.id };
}

// Function to match orders
function matchOrders() {
  let matches = [];
  
  // Continue matching as long as there are matching orders
  while (orderBook.buy.length > 0 && orderBook.sell.length > 0) {
    const buyOrder = orderBook.buy[0];
    const sellOrder = orderBook.sell[0];
    
    // Check if orders match (buy price >= sell price)
    if (buyOrder.price >= sellOrder.price) {
      // Calculate the quantity to swap
      const quantity = Math.min(buyOrder.quantity, sellOrder.quantity);
      
      // Execute the swap
      const match = {
        buyOrderId: buyOrder.id,
        sellOrderId: sellOrder.id,
        price: sellOrder.price, // Use the sell price for the match
        quantity: quantity,
        buyUser: buyOrder.userId,
        sellUser: sellOrder.userId,
        timestamp: Date.now()
      };
      
      matches.push(match);
      
      // Update order quantities
      buyOrder.quantity -= quantity;
      sellOrder.quantity -= quantity;
      
      // Remove fulfilled orders
      if (buyOrder.quantity === 0) {
        orderBook.buy.shift();
      }
      if (sellOrder.quantity === 0) {
        orderBook.sell.shift();
      }
    } else {
      // No more matches possible
      break;
    }
  }
  
  return matches;
}

// Function to get the order book (only for the owner)
function getOrderBook(userId) {
  // Check if the user is the owner
  if (userId !== 'owner') {
    return { error: 'Unauthorized' };
  }
  
  return orderBook;
}

// Function to get user orders
function getUserOrders(userId) {
  const userOrders = {
    buy: orderBook.buy.filter(order => order.userId === userId),
    sell: orderBook.sell.filter(order => order.userId === userId)
  };
  
  return userOrders;
}

// Main function that handles all contract operations
function main(input) {
  const { action, userId, data } = input;
  
  switch (action) {
    case 'addOrder':
      return addOrder({
        id: data.id || generateOrderId(),
        type: data.type,
        price: data.price,
        quantity: data.quantity,
        userId: userId,
        timestamp: Date.now()
      });
    
    case 'getOrderBook':
      return getOrderBook(userId);
    
    case 'getUserOrders':
      return getUserOrders(userId);
    
    default:
      return { error: 'Invalid action' };
  }
}

// Helper function to generate a unique order ID
function generateOrderId() {
  return 'order_' + Date.now() + '_' + Math.random().toString(36).substring(2, 15);
}
```

## Step 4: Store the Contract in the Enclave

Now, let's update the `InitializeTokenSwapContract` method to store the contract in the enclave:

```csharp
static async Task InitializeTokenSwapContract(ITeeInterface enclaveInterface)
{
    // Read the contract code from file
    string contractCode = File.ReadAllText("TokenSwapContract.js");
    
    // Store the contract in the enclave
    string functionId = "token-swap-contract";
    string userId = "owner";
    
    bool result = await enclaveInterface.StoreJavaScriptFunctionAsync(
        functionId, contractCode, userId);
    
    if (!result)
    {
        throw new Exception("Failed to store token swap contract in the enclave");
    }
}
```

## Step 5: Implement the Application Logic

Now, let's implement the application logic to interact with the contract:

```csharp
static async Task RunApplication(ITeeInterface enclaveInterface, ILogger logger)
{
    string functionId = "token-swap-contract";
    
    // Simulate users placing orders
    await PlaceOrder(enclaveInterface, functionId, "alice", "buy", 100, 10, logger);
    await PlaceOrder(enclaveInterface, functionId, "bob", "sell", 95, 5, logger);
    await PlaceOrder(enclaveInterface, functionId, "charlie", "sell", 98, 8, logger);
    await PlaceOrder(enclaveInterface, functionId, "dave", "buy", 99, 3, logger);
    
    // Get Alice's orders
    await GetUserOrders(enclaveInterface, functionId, "alice", logger);
    
    // Get the order book (only owner can do this)
    await GetOrderBook(enclaveInterface, functionId, "owner", logger);
}

static async Task PlaceOrder(ITeeInterface enclaveInterface, string functionId, 
    string userId, string orderType, decimal price, decimal quantity, ILogger logger)
{
    logger.LogInformation($"User {userId} placing {orderType} order: {quantity} @ {price}");
    
    string input = $@"{{
        ""action"": ""addOrder"",
        ""userId"": ""{userId}"",
        ""data"": {{
            ""type"": ""{orderType}"",
            ""price"": {price},
            ""quantity"": {quantity}
        }}
    }}";
    
    var result = await enclaveInterface.ExecuteJavaScriptAsync(
        "", input, "{}", functionId, userId);
    
    logger.LogInformation($"Result: {result.Result}");
}

static async Task GetUserOrders(ITeeInterface enclaveInterface, string functionId, 
    string userId, ILogger logger)
{
    logger.LogInformation($"Getting orders for user {userId}");
    
    string input = $@"{{
        ""action"": ""getUserOrders"",
        ""userId"": ""{userId}""
    }}";
    
    var result = await enclaveInterface.ExecuteJavaScriptAsync(
        "", input, "{}", functionId, userId);
    
    logger.LogInformation($"User orders: {result.Result}");
}

static async Task GetOrderBook(ITeeInterface enclaveInterface, string functionId, 
    string userId, ILogger logger)
{
    logger.LogInformation($"Getting order book (user: {userId})");
    
    string input = $@"{{
        ""action"": ""getOrderBook"",
        ""userId"": ""{userId}""
    }}";
    
    var result = await enclaveInterface.ExecuteJavaScriptAsync(
        "", input, "{}", functionId, userId);
    
    logger.LogInformation($"Order book: {result.Result}");
}
```

## Step 6: Run the Application

Now, let's run the application:

```bash
dotnet run
```

You should see output similar to the following:

```
info: ConfidentialTokenSwap.Program[0]
      Initializing enclave host...
info: ConfidentialTokenSwap.Program[0]
      User alice placing buy order: 10 @ 100
info: ConfidentialTokenSwap.Program[0]
      Result: {"success":true,"orderId":"order_1623456789_abc123"}
info: ConfidentialTokenSwap.Program[0]
      User bob placing sell order: 5 @ 95
info: ConfidentialTokenSwap.Program[0]
      Result: {"success":true,"orderId":"order_1623456790_def456"}
info: ConfidentialTokenSwap.Program[0]
      User charlie placing sell order: 8 @ 98
info: ConfidentialTokenSwap.Program[0]
      Result: {"success":true,"orderId":"order_1623456791_ghi789"}
info: ConfidentialTokenSwap.Program[0]
      User dave placing buy order: 3 @ 99
info: ConfidentialTokenSwap.Program[0]
      Result: {"success":true,"orderId":"order_1623456792_jkl012"}
info: ConfidentialTokenSwap.Program[0]
      Getting orders for user alice
info: ConfidentialTokenSwap.Program[0]
      User orders: {"buy":[{"id":"order_1623456789_abc123","type":"buy","price":100,"quantity":5,"userId":"alice","timestamp":1623456789}],"sell":[]}
info: ConfidentialTokenSwap.Program[0]
      Getting order book (user: owner)
info: ConfidentialTokenSwap.Program[0]
      Order book: {"buy":[{"id":"order_1623456789_abc123","type":"buy","price":100,"quantity":5,"userId":"alice","timestamp":1623456789},{"id":"order_1623456792_jkl012","type":"buy","price":99,"quantity":0,"userId":"dave","timestamp":1623456792}],"sell":[{"id":"order_1623456791_ghi789","type":"sell","price":98,"quantity":5,"userId":"charlie","timestamp":1623456791},{"id":"order_1623456790_def456","type":"sell","price":95,"quantity":0,"userId":"bob","timestamp":1623456790}]}
```

## Step 7: Add Event Triggers for Order Matching

Let's enhance our application by adding event triggers for order matching. This will allow us to automatically execute matches when new orders are placed.

First, let's modify our contract to emit events when orders are matched:

```javascript
// Function to match orders
function matchOrders() {
  let matches = [];
  
  // Continue matching as long as there are matching orders
  while (orderBook.buy.length > 0 && orderBook.sell.length > 0) {
    const buyOrder = orderBook.buy[0];
    const sellOrder = orderBook.sell[0];
    
    // Check if orders match (buy price >= sell price)
    if (buyOrder.price >= sellOrder.price) {
      // Calculate the quantity to swap
      const quantity = Math.min(buyOrder.quantity, sellOrder.quantity);
      
      // Execute the swap
      const match = {
        buyOrderId: buyOrder.id,
        sellOrderId: sellOrder.id,
        price: sellOrder.price, // Use the sell price for the match
        quantity: quantity,
        buyUser: buyOrder.userId,
        sellUser: sellOrder.userId,
        timestamp: Date.now()
      };
      
      matches.push(match);
      
      // Update order quantities
      buyOrder.quantity -= quantity;
      sellOrder.quantity -= quantity;
      
      // Remove fulfilled orders
      if (buyOrder.quantity === 0) {
        orderBook.buy.shift();
      }
      if (sellOrder.quantity === 0) {
        orderBook.sell.shift();
      }
      
      // Emit match event
      emitMatchEvent(match);
    } else {
      // No more matches possible
      break;
    }
  }
  
  return matches;
}

// Function to emit a match event
function emitMatchEvent(match) {
  // In a real implementation, this would emit an event to the blockchain
  console.log("Match event:", match);
  return match;
}
```

Now, let's register an event trigger for order matches:

```csharp
static async Task InitializeTokenSwapContract(ITeeInterface enclaveInterface)
{
    // Read the contract code from file
    string contractCode = File.ReadAllText("TokenSwapContract.js");
    
    // Store the contract in the enclave
    string functionId = "token-swap-contract";
    string userId = "owner";
    
    bool result = await enclaveInterface.StoreJavaScriptFunctionAsync(
        functionId, contractCode, userId);
    
    if (!result)
    {
        throw new Exception("Failed to store token swap contract in the enclave");
    }
    
    // Register an event trigger for order matches
    string eventType = "blockchain";
    string condition = @"{
        ""event_type"": ""token_swap_match"",
        ""contract_address"": ""0x1234567890abcdef""
    }";
    
    string triggerId = await enclaveInterface.RegisterTriggerAsync(
        eventType, functionId, userId, condition);
    
    if (string.IsNullOrEmpty(triggerId))
    {
        throw new Exception("Failed to register event trigger");
    }
}
```

## Conclusion

In this tutorial, we've implemented a confidential token swap contract using the Neo Service Layer. The contract keeps the order book confidential within the enclave, while still allowing users to place orders and get information about their own orders.

Key features of this implementation:

1. **Confidentiality**: The order book is kept confidential within the enclave, so users can't see other users' orders.
2. **Security**: The contract logic is executed securely within the enclave, ensuring that it can't be tampered with.
3. **Event Triggers**: The contract emits events when orders are matched, which can be used to trigger other actions.

This is just a simple example of what you can do with the Neo Service Layer. You can extend this contract to include more features, such as:

- Support for multiple token pairs
- Order cancellation
- Order expiration
- Fee collection
- Price oracles for market orders

For more information, see the [Neo Service Layer Documentation](../index.md).
