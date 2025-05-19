# Tutorial: Implementing a Provably Fair Game

This tutorial will guide you through the process of implementing a provably fair game using the Neo Service Layer (NSL). A provably fair game is a game where the outcome is determined by a random process that can be verified by the players, ensuring that the game operator cannot manipulate the results.

## Prerequisites

- .NET 9.0 or later
- Neo Service Layer installed and configured
- Basic understanding of C# and JavaScript

## Overview

In this tutorial, we'll implement a provably fair dice game where:

1. Players place bets on the outcome of a dice roll
2. The game generates a random number using the NSL's randomness service
3. Players can verify that the random number was generated fairly
4. Winnings are distributed based on the outcome

## Step 1: Set Up the Project

First, let's create a new .NET project for our provably fair dice game:

```bash
dotnet new console -n ProvablyFairDiceGame
cd ProvablyFairDiceGame
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

namespace ProvablyFairDiceGame
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
            
            // Initialize the dice game
            await InitializeDiceGame(enclaveInterface);
            
            // Run the game
            await RunGame(enclaveInterface, logger);
        }
        
        static async Task InitializeDiceGame(ITeeInterface enclaveInterface)
        {
            // We'll implement this in the next step
        }
        
        static async Task RunGame(ITeeInterface enclaveInterface, ILogger logger)
        {
            // We'll implement this in a later step
        }
    }
}
```

## Step 3: Implement the Dice Game Contract

Now, let's implement the dice game contract in JavaScript. Create a new file called `DiceGameContract.js` with the following code:

```javascript
// Dice Game Contract

// Game state
let games = {};
let gameCounter = 0;

// Function to create a new game
function createGame(userId, betAmount, betNumber) {
    // Validate inputs
    if (betAmount <= 0) {
        return { error: "Bet amount must be positive" };
    }
    
    if (betNumber < 1 || betNumber > 6) {
        return { error: "Bet number must be between 1 and 6" };
    }
    
    // Generate a unique game ID
    const gameId = "game_" + (++gameCounter);
    
    // Create the game
    games[gameId] = {
        id: gameId,
        userId: userId,
        betAmount: betAmount,
        betNumber: betNumber,
        status: "created",
        timestamp: Date.now()
    };
    
    return {
        success: true,
        gameId: gameId,
        game: games[gameId]
    };
}

// Function to roll the dice and determine the outcome
function rollDice(gameId, randomNumber) {
    // Validate inputs
    if (!games[gameId]) {
        return { error: "Game not found" };
    }
    
    const game = games[gameId];
    
    if (game.status !== "created") {
        return { error: "Game already played" };
    }
    
    // Convert the random number to a dice roll (1-6)
    const diceRoll = (randomNumber % 6) + 1;
    
    // Determine if the player won
    const won = diceRoll === game.betNumber;
    
    // Calculate winnings (6x bet amount if won, 0 if lost)
    const winnings = won ? game.betAmount * 6 : 0;
    
    // Update the game state
    game.status = "completed";
    game.diceRoll = diceRoll;
    game.won = won;
    game.winnings = winnings;
    game.completedAt = Date.now();
    
    return {
        success: true,
        gameId: gameId,
        game: game
    };
}

// Function to get a game by ID
function getGame(gameId) {
    if (!games[gameId]) {
        return { error: "Game not found" };
    }
    
    return {
        success: true,
        game: games[gameId]
    };
}

// Function to get all games for a user
function getUserGames(userId) {
    const userGames = Object.values(games).filter(game => game.userId === userId);
    
    return {
        success: true,
        games: userGames
    };
}

// Main function that handles all contract operations
function main(input) {
    const { action, userId, data } = input;
    
    switch (action) {
        case 'createGame':
            return createGame(userId, data.betAmount, data.betNumber);
        
        case 'rollDice':
            return rollDice(data.gameId, data.randomNumber);
        
        case 'getGame':
            return getGame(data.gameId);
        
        case 'getUserGames':
            return getUserGames(userId);
        
        default:
            return { error: 'Invalid action' };
    }
}
```

## Step 4: Store the Contract in the Enclave

Now, let's update the `InitializeDiceGame` method to store the contract in the enclave:

```csharp
static async Task InitializeDiceGame(ITeeInterface enclaveInterface)
{
    // Read the contract code from file
    string contractCode = File.ReadAllText("DiceGameContract.js");
    
    // Store the contract in the enclave
    string functionId = "dice-game-contract";
    string userId = "owner";
    
    bool result = await enclaveInterface.StoreJavaScriptFunctionAsync(
        functionId, contractCode, userId);
    
    if (!result)
    {
        throw new Exception("Failed to store dice game contract in the enclave");
    }
}
```

## Step 5: Implement the Game Logic

Now, let's implement the game logic to interact with the contract:

```csharp
static async Task RunGame(ITeeInterface enclaveInterface, ILogger logger)
{
    string functionId = "dice-game-contract";
    string userId = "player1";
    
    // Step 1: Create a new game
    logger.LogInformation("Creating a new game...");
    string gameId = await CreateGame(enclaveInterface, functionId, userId, 10, 3, logger);
    
    // Step 2: Generate a provably fair random number
    logger.LogInformation("Generating a random number...");
    (ulong randomNumber, string proof) = await GenerateRandomNumber(enclaveInterface, userId, gameId, logger);
    
    // Step 3: Roll the dice
    logger.LogInformation("Rolling the dice...");
    await RollDice(enclaveInterface, functionId, userId, gameId, randomNumber, logger);
    
    // Step 4: Verify the random number
    logger.LogInformation("Verifying the random number...");
    await VerifyRandomNumber(enclaveInterface, randomNumber, userId, gameId, proof, logger);
    
    // Step 5: Get the game result
    logger.LogInformation("Getting the game result...");
    await GetGame(enclaveInterface, functionId, userId, gameId, logger);
}

static async Task<string> CreateGame(ITeeInterface enclaveInterface, string functionId, 
    string userId, decimal betAmount, int betNumber, ILogger logger)
{
    string input = $@"{{
        ""action"": ""createGame"",
        ""userId"": ""{userId}"",
        ""data"": {{
            ""betAmount"": {betAmount},
            ""betNumber"": {betNumber}
        }}
    }}";
    
    var result = await enclaveInterface.ExecuteJavaScriptAsync(
        "", input, "{}", functionId, userId);
    
    logger.LogInformation($"Game created: {result.Result}");
    
    // Parse the result to get the game ID
    var resultObj = System.Text.Json.JsonDocument.Parse(result.Result).RootElement;
    return resultObj.GetProperty("gameId").GetString();
}

static async Task<(ulong, string)> GenerateRandomNumber(ITeeInterface enclaveInterface, 
    string userId, string gameId, ILogger logger)
{
    // Generate a random number between 0 and ULONG_MAX
    ulong min = 0;
    ulong max = ulong.MaxValue;
    string requestId = gameId;
    
    ulong randomNumber = await enclaveInterface.GenerateRandomNumberAsync(
        min, max, userId, requestId);
    
    // Get the proof for the random number
    string proof = await enclaveInterface.GetRandomNumberProofAsync(
        randomNumber, min, max, userId, requestId);
    
    logger.LogInformation($"Random number: {randomNumber}");
    logger.LogInformation($"Proof: {proof}");
    
    return (randomNumber, proof);
}

static async Task RollDice(ITeeInterface enclaveInterface, string functionId, 
    string userId, string gameId, ulong randomNumber, ILogger logger)
{
    string input = $@"{{
        ""action"": ""rollDice"",
        ""userId"": ""{userId}"",
        ""data"": {{
            ""gameId"": ""{gameId}"",
            ""randomNumber"": {randomNumber}
        }}
    }}";
    
    var result = await enclaveInterface.ExecuteJavaScriptAsync(
        "", input, "{}", functionId, userId);
    
    logger.LogInformation($"Dice rolled: {result.Result}");
}

static async Task VerifyRandomNumber(ITeeInterface enclaveInterface, ulong randomNumber, 
    string userId, string gameId, string proof, ILogger logger)
{
    // Verify the random number
    ulong min = 0;
    ulong max = ulong.MaxValue;
    string requestId = gameId;
    
    bool isValid = await enclaveInterface.VerifyRandomNumberAsync(
        randomNumber, min, max, userId, requestId, proof);
    
    logger.LogInformation($"Random number verification: {(isValid ? "Valid" : "Invalid")}");
}

static async Task GetGame(ITeeInterface enclaveInterface, string functionId, 
    string userId, string gameId, ILogger logger)
{
    string input = $@"{{
        ""action"": ""getGame"",
        ""userId"": ""{userId}"",
        ""data"": {{
            ""gameId"": ""{gameId}""
        }}
    }}";
    
    var result = await enclaveInterface.ExecuteJavaScriptAsync(
        "", input, "{}", functionId, userId);
    
    logger.LogInformation($"Game result: {result.Result}");
}
```

## Step 6: Run the Game

Now, let's run the game:

```bash
dotnet run
```

You should see output similar to the following:

```
info: ProvablyFairDiceGame.Program[0]
      Initializing enclave host...
info: ProvablyFairDiceGame.Program[0]
      Creating a new game...
info: ProvablyFairDiceGame.Program[0]
      Game created: {"success":true,"gameId":"game_1","game":{"id":"game_1","userId":"player1","betAmount":10,"betNumber":3,"status":"created","timestamp":1623456789}}
info: ProvablyFairDiceGame.Program[0]
      Generating a random number...
info: ProvablyFairDiceGame.Program[0]
      Random number: 12345678901234567890
info: ProvablyFairDiceGame.Program[0]
      Proof: ABCDEF1234567890...
info: ProvablyFairDiceGame.Program[0]
      Rolling the dice...
info: ProvablyFairDiceGame.Program[0]
      Dice rolled: {"success":true,"gameId":"game_1","game":{"id":"game_1","userId":"player1","betAmount":10,"betNumber":3,"status":"completed","timestamp":1623456789,"diceRoll":1,"won":false,"winnings":0,"completedAt":1623456790}}
info: ProvablyFairDiceGame.Program[0]
      Verifying the random number...
info: ProvablyFairDiceGame.Program[0]
      Random number verification: Valid
info: ProvablyFairDiceGame.Program[0]
      Getting the game result...
info: ProvablyFairDiceGame.Program[0]
      Game result: {"success":true,"game":{"id":"game_1","userId":"player1","betAmount":10,"betNumber":3,"status":"completed","timestamp":1623456789,"diceRoll":1,"won":false,"winnings":0,"completedAt":1623456790}}
```

## Step 7: Add a Web Interface

To make the game more user-friendly, let's add a simple web interface using ASP.NET Core. First, let's create a new ASP.NET Core project:

```bash
dotnet new web -n ProvablyFairDiceGameWeb
cd ProvablyFairDiceGameWeb
dotnet add package NeoServiceLayer.Client
```

Then, create a new file called `Program.cs` with the following code:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host;
using NeoServiceLayer.Tee.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddSingleton<TeeEnclaveHost>(sp => {
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return new TeeEnclaveHost(loggerFactory, simulationMode: true);
});

builder.Services.AddSingleton<ITeeInterface>(sp => {
    var enclaveHost = sp.GetRequiredService<TeeEnclaveHost>();
    return enclaveHost.GetEnclaveInterface();
});

var app = builder.Build();

// Initialize the dice game
await InitializeDiceGame(app.Services.GetRequiredService<ITeeInterface>());

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();

// Define API endpoints
app.MapGet("/", () => "Provably Fair Dice Game");

app.MapPost("/api/games", async (HttpContext context, ITeeInterface enclaveInterface) => {
    // Parse request body
    var request = await System.Text.Json.JsonSerializer.DeserializeAsync<CreateGameRequest>(
        context.Request.Body);
    
    // Create a new game
    string functionId = "dice-game-contract";
    string userId = request.UserId;
    
    string input = $@"{{
        ""action"": ""createGame"",
        ""userId"": ""{userId}"",
        ""data"": {{
            ""betAmount"": {request.BetAmount},
            ""betNumber"": {request.BetNumber}
        }}
    }}";
    
    var result = await enclaveInterface.ExecuteJavaScriptAsync(
        "", input, "{}", functionId, userId);
    
    // Return the result
    await context.Response.WriteAsync(result.Result);
});

app.MapPost("/api/games/{gameId}/roll", async (string gameId, HttpContext context, ITeeInterface enclaveInterface) => {
    // Parse request body
    var request = await System.Text.Json.JsonSerializer.DeserializeAsync<RollDiceRequest>(
        context.Request.Body);
    
    // Generate a random number
    ulong min = 0;
    ulong max = ulong.MaxValue;
    string requestId = gameId;
    
    ulong randomNumber = await enclaveInterface.GenerateRandomNumberAsync(
        min, max, request.UserId, requestId);
    
    // Get the proof for the random number
    string proof = await enclaveInterface.GetRandomNumberProofAsync(
        randomNumber, min, max, request.UserId, requestId);
    
    // Roll the dice
    string functionId = "dice-game-contract";
    
    string input = $@"{{
        ""action"": ""rollDice"",
        ""userId"": ""{request.UserId}"",
        ""data"": {{
            ""gameId"": ""{gameId}"",
            ""randomNumber"": {randomNumber}
        }}
    }}";
    
    var result = await enclaveInterface.ExecuteJavaScriptAsync(
        "", input, "{}", functionId, request.UserId);
    
    // Return the result with the proof
    var resultObj = System.Text.Json.JsonDocument.Parse(result.Result).RootElement;
    var response = new {
        success = resultObj.GetProperty("success").GetBoolean(),
        gameId = resultObj.GetProperty("gameId").GetString(),
        game = resultObj.GetProperty("game"),
        randomNumber = randomNumber,
        proof = proof
    };
    
    await context.Response.WriteAsJsonAsync(response);
});

app.MapGet("/api/games/{gameId}", async (string gameId, string userId, ITeeInterface enclaveInterface) => {
    // Get the game
    string functionId = "dice-game-contract";
    
    string input = $@"{{
        ""action"": ""getGame"",
        ""userId"": ""{userId}"",
        ""data"": {{
            ""gameId"": ""{gameId}""
        }}
    }}";
    
    var result = await enclaveInterface.ExecuteJavaScriptAsync(
        "", input, "{}", functionId, userId);
    
    // Return the result
    return result.Result;
});

app.MapGet("/api/users/{userId}/games", async (string userId, ITeeInterface enclaveInterface) => {
    // Get the user's games
    string functionId = "dice-game-contract";
    
    string input = $@"{{
        ""action"": ""getUserGames"",
        ""userId"": ""{userId}"",
        ""data"": {{}}
    }}";
    
    var result = await enclaveInterface.ExecuteJavaScriptAsync(
        "", input, "{}", functionId, userId);
    
    // Return the result
    return result.Result;
});

app.MapPost("/api/verify", async (HttpContext context, ITeeInterface enclaveInterface) => {
    // Parse request body
    var request = await System.Text.Json.JsonSerializer.DeserializeAsync<VerifyRequest>(
        context.Request.Body);
    
    // Verify the random number
    bool isValid = await enclaveInterface.VerifyRandomNumberAsync(
        request.RandomNumber, 0, ulong.MaxValue, request.UserId, request.GameId, request.Proof);
    
    // Return the result
    var response = new {
        valid = isValid
    };
    
    await context.Response.WriteAsJsonAsync(response);
});

app.Run();

async Task InitializeDiceGame(ITeeInterface enclaveInterface)
{
    // Read the contract code from file
    string contractCode = File.ReadAllText("DiceGameContract.js");
    
    // Store the contract in the enclave
    string functionId = "dice-game-contract";
    string userId = "owner";
    
    bool result = await enclaveInterface.StoreJavaScriptFunctionAsync(
        functionId, contractCode, userId);
    
    if (!result)
    {
        throw new Exception("Failed to store dice game contract in the enclave");
    }
}

// Request models
class CreateGameRequest
{
    public string UserId { get; set; }
    public decimal BetAmount { get; set; }
    public int BetNumber { get; set; }
}

class RollDiceRequest
{
    public string UserId { get; set; }
}

class VerifyRequest
{
    public string UserId { get; set; }
    public string GameId { get; set; }
    public ulong RandomNumber { get; set; }
    public string Proof { get; set; }
}
```

## Conclusion

In this tutorial, we've implemented a provably fair dice game using the Neo Service Layer. The game uses the NSL's randomness service to generate random numbers that can be verified by the players, ensuring that the game operator cannot manipulate the results.

Key features of this implementation:

1. **Provable Fairness**: The game uses the NSL's randomness service to generate random numbers that can be verified by the players.
2. **Security**: The game logic is executed securely within the enclave, ensuring that it can't be tampered with.
3. **Transparency**: Players can verify that the random numbers used to determine the game outcome were generated fairly.

This is just a simple example of what you can do with the Neo Service Layer. You can extend this game to include more features, such as:

- Support for multiple players
- Different types of bets
- Leaderboards
- Tournaments
- Jackpots

For more information, see the [Neo Service Layer Documentation](../index.md).
