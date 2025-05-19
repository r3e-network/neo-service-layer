# JavaScript Execution Workflow

```mermaid
sequenceDiagram
    participant Blockchain as Neo N3 Blockchain
    participant SmartContract as Neo N3 Smart Contract
    participant App as NSL Application
    participant Host as TeeEnclaveHost
    participant Interface as OpenEnclaveInterface
    participant Enclave as OpenEnclaveEnclave
    participant JS as JsEngine
    participant Gas as GasAccounting
    participant Secret as SecretManager

    App->>Host: Initialize
    Host->>Interface: Create
    Interface->>Enclave: Initialize
    Enclave->>JS: Initialize
    Enclave->>Gas: Initialize
    Enclave->>Secret: Initialize

    Note over Blockchain,Secret: Smart Contract Invokes JavaScript Execution

    SmartContract->>Blockchain: Emit ExecutionRequest Event
    Blockchain-->>App: Event Notification

    App->>Host: ExecuteJavaScriptAsync(functionId, input, userId)
    Host->>Interface: ExecuteJavaScriptAsync
    Interface->>Enclave: execute_javascript

    Enclave->>Secret: get_user_secrets(userId)
    Secret-->>Enclave: user secrets

    Enclave->>Gas: reset_gas_used()

    Enclave->>JS: create_js_context()
    JS-->>Enclave: context_id

    Enclave->>JS: execute(code, input, userId, functionId)

    JS->>Gas: use_gas(amount)
    Gas-->>JS: OK

    JS-->>Enclave: result

    Enclave->>Gas: get_gas_used()
    Gas-->>Enclave: gas_used

    Enclave->>JS: destroy_js_context(context_id)

    Enclave-->>Interface: result JSON
    Interface-->>Host: JavaScriptExecutionResult
    Host-->>App: JavaScriptExecutionResult

    Note over Blockchain,Secret: Callback with Result

    App->>Blockchain: Send Callback Transaction
    Blockchain->>SmartContract: Execute Callback Method
    SmartContract->>Blockchain: Store Result
```

## Workflow Description

1. **Initialization**:
   - The NSL application initializes the TeeEnclaveHost.
   - The TeeEnclaveHost creates the OpenEnclaveInterface.
   - The OpenEnclaveInterface initializes the OpenEnclaveEnclave.
   - The OpenEnclaveEnclave initializes its components: JsEngine, GasAccounting, and SecretManager.

2. **Smart Contract Invokes JavaScript Execution**:
   - A Neo N3 smart contract emits an ExecutionRequest event on the blockchain.
   - The NSL application receives the event notification from the blockchain.
   - The NSL application calls ExecuteJavaScriptAsync with the function ID, input data, and user ID.
   - The TeeEnclaveHost forwards the call to the OpenEnclaveInterface.
   - The OpenEnclaveInterface calls the execute_javascript method of the OpenEnclaveEnclave.

3. **JavaScript Execution in Enclave**:
   - The OpenEnclaveEnclave retrieves the user's secrets from the SecretManager.
   - The OpenEnclaveEnclave resets the gas used counter.
   - The OpenEnclaveEnclave creates a JavaScript context.
   - The OpenEnclaveEnclave executes the JavaScript code using the JsEngine.
   - The JsEngine tracks gas usage during execution.
   - The JsEngine returns the result to the OpenEnclaveEnclave.
   - The OpenEnclaveEnclave gets the gas used from the GasAccounting.
   - The OpenEnclaveEnclave destroys the JavaScript context.
   - The OpenEnclaveEnclave returns the result as a JSON string to the OpenEnclaveInterface.
   - The OpenEnclaveInterface converts the JSON string to a JavaScriptExecutionResult object and returns it to the TeeEnclaveHost.
   - The TeeEnclaveHost returns the JavaScriptExecutionResult to the NSL application.

4. **Callback with Result**:
   - The NSL application sends a callback transaction to the blockchain with the execution result.
   - The blockchain executes the callback method on the smart contract.
   - The smart contract stores the result on the blockchain.
