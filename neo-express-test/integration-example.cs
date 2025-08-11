// Example: How Neo Service Layer can interact with Neo Express blockchain

// Configuration for Neo Express RPC endpoint
var neoExpressRpcUrl = Configuration["ServiceEndpoints:Http:50012"] ?? "http://localhost:50012"; // Default Neo Express RPC port

// Example 1: Invoking smart contract through Neo Service Layer
var contractHash = "0x918dc5e53f237015fae0dad532655efff9834cbd";
var scriptBuilder = new ScriptBuilder();
scriptBuilder.EmitDynamicCall(contractHash, "hello", "Neo Service Layer");

// Example 2: Reading contract storage
var storageKey = "testKey";
var result = await smartContractManager.GetStorage(contractHash, storageKey);

// Example 3: Monitoring contract events
var events = await smartContractManager.GetContractEvents(contractHash);

// Example 4: Deploying contracts through service layer
var nefFile = File.ReadAllBytes("SimpleContract.nef");
var manifest = File.ReadAllText("SimpleContract.manifest.json");
var deployResult = await smartContractManager.DeployContract(nefFile, manifest);
