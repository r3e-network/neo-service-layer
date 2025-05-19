# Event Trigger Workflow

```mermaid
sequenceDiagram
    participant Blockchain as Neo N3 Blockchain
    participant SmartContract as Neo N3 Smart Contract
    participant App as NSL Application
    participant Host as TeeEnclaveHost
    participant Interface as OpenEnclaveInterface
    participant Enclave as OpenEnclaveEnclave
    participant Trigger as EventTrigger
    participant JS as JsEngine

    Note over Blockchain,JS: Registration Phase

    App->>Host: RegisterTriggerAsync(eventType, functionId, userId, condition)
    Host->>Interface: RegisterTriggerAsync
    Interface->>Enclave: register_trigger
    Enclave->>Trigger: register_trigger
    Trigger-->>Enclave: trigger_id
    Enclave-->>Interface: trigger_id
    Interface-->>Host: trigger_id
    Host-->>App: trigger_id

    Note over Blockchain,JS: Blockchain Event Processing

    SmartContract->>Blockchain: Emit Event
    Blockchain-->>App: Event Notification

    App->>Host: ProcessBlockchainEventAsync(eventData)
    Host->>Interface: ProcessBlockchainEventAsync
    Interface->>Enclave: process_blockchain_event
    Enclave->>Trigger: process_blockchain_event

    Trigger->>Trigger: match_triggers(eventData)

    loop For each matching trigger
        Trigger->>Enclave: execute_trigger(trigger, eventData)
        Enclave->>JS: execute(code, input, userId, functionId)
        JS-->>Enclave: result
        Enclave-->>Trigger: execution result
    end

    Trigger-->>Enclave: processed_count
    Enclave-->>Interface: processed_count
    Interface-->>Host: processed_count
    Host-->>App: processed_count

    Note over Blockchain,JS: Callback with Results

    App->>Blockchain: Send Callback Transaction
    Blockchain->>SmartContract: Execute Callback Method
    SmartContract->>Blockchain: Store Result

    Note over Blockchain,JS: Scheduled Trigger Processing

    App->>Host: ProcessScheduledTriggersAsync(currentTime)
    Host->>Interface: ProcessScheduledTriggersAsync
    Interface->>Enclave: process_scheduled_triggers
    Enclave->>Trigger: process_scheduled_triggers

    Trigger->>Trigger: find_due_triggers(currentTime)

    loop For each due trigger
        Trigger->>Enclave: execute_trigger(trigger, {})
        Enclave->>JS: execute(code, input, userId, functionId)
        JS-->>Enclave: result
        Enclave-->>Trigger: execution result
        Trigger->>Trigger: update_next_execution_time(trigger)
    end

    Trigger-->>Enclave: processed_count
    Enclave-->>Interface: processed_count
    Interface-->>Host: processed_count
    Host-->>App: processed_count

    Note over Blockchain,JS: Callback with Results

    App->>Blockchain: Send Callback Transaction
    Blockchain->>SmartContract: Execute Callback Method
    SmartContract->>Blockchain: Store Result
```

## Workflow Description

### Registration Phase

1. The NSL application calls RegisterTriggerAsync with the event type, function ID, user ID, and condition.
2. The TeeEnclaveHost forwards the call to the OpenEnclaveInterface.
3. The OpenEnclaveInterface calls the register_trigger method of the OpenEnclaveEnclave.
4. The OpenEnclaveEnclave calls the register_trigger method of the EventTrigger.
5. The EventTrigger creates a new trigger and returns the trigger ID.
6. The trigger ID is returned to the NSL application.

### Blockchain Event Processing

1. A Neo N3 smart contract emits an event on the blockchain.
2. The NSL application receives the event notification from the blockchain.
3. The NSL application calls ProcessBlockchainEventAsync with the event data.
4. The TeeEnclaveHost forwards the call to the OpenEnclaveInterface.
5. The OpenEnclaveInterface calls the process_blockchain_event method of the OpenEnclaveEnclave.
6. The OpenEnclaveEnclave calls the process_blockchain_event method of the EventTrigger.
7. The EventTrigger matches the event data against registered triggers.
8. For each matching trigger:
   - The EventTrigger calls the execute_trigger method of the OpenEnclaveEnclave.
   - The OpenEnclaveEnclave executes the JavaScript function associated with the trigger.
   - The result is returned to the EventTrigger.
9. The number of processed triggers is returned to the NSL application.

### Callback with Results

1. The NSL application sends a callback transaction to the blockchain with the execution results.
2. The blockchain executes the callback method on the smart contract.
3. The smart contract stores the results on the blockchain.

### Scheduled Trigger Processing

1. The NSL application calls ProcessScheduledTriggersAsync with the current time.
2. The TeeEnclaveHost forwards the call to the OpenEnclaveInterface.
3. The OpenEnclaveInterface calls the process_scheduled_triggers method of the OpenEnclaveEnclave.
4. The OpenEnclaveEnclave calls the process_scheduled_triggers method of the EventTrigger.
5. The EventTrigger finds triggers that are due for execution.
6. For each due trigger:
   - The EventTrigger calls the execute_trigger method of the OpenEnclaveEnclave.
   - The OpenEnclaveEnclave executes the JavaScript function associated with the trigger.
   - The result is returned to the EventTrigger.
   - The EventTrigger updates the next execution time of the trigger.
7. The number of processed triggers is returned to the NSL application.
8. The NSL application sends callback transactions to the blockchain with the execution results.
9. The blockchain executes the callback methods on the smart contracts.
10. The smart contracts store the results on the blockchain.
