# Neo Service Layer (NSL) - Enclave Documentation

## Overview

The Neo Service Layer (NSL) is a secure computing environment that uses Trusted Execution Environments (TEEs) to run JavaScript functions in a confidential manner. The NSL provides features for User Secrets, Event Triggers, GAS Accounting, Provably Fair Randomness, and Compliance Verification.

**Important**: The NSL does not execute smart contracts directly. Instead, it provides a secure environment for executing JavaScript code that can:
1. Access user secrets securely stored in the enclave
2. Perform confidential computations
3. Send callback transactions to the Neo N3 blockchain with the results

Neo N3 smart contracts can invoke JavaScript execution in the enclave by emitting on-chain execution events that the NSL detects and processes.

This document provides an overview of the enclave component of the NSL, which is responsible for executing JavaScript code securely within an Intel SGX or OpenEnclave enclave.

## Architecture

The NSL enclave is built using the OpenEnclave SDK, which provides a common API for different TEE technologies. The enclave is implemented in C++ and exposes a C-style interface that can be called from the host application.

For a visual representation of the architecture, see the [NSL Architecture Diagram](diagrams/NSL_Architecture.md).

### Components

The enclave consists of the following main components:

1. **OpenEnclaveEnclave**: The main enclave class that provides the core functionality of the enclave, including JavaScript execution, secret management, and persistent storage. It is organized into multiple files for better maintainability:
   - `OpenEnclaveEnclave.Core.cpp`: Core initialization and cleanup
   - `OpenEnclaveEnclave.JavaScript.cpp`: JavaScript execution methods
   - `OpenEnclaveEnclave.Storage.cpp`: Storage-related methods
   - `OpenEnclaveEnclave.Secrets.cpp`: Secret management methods
   - `OpenEnclaveEnclave.Attestation.cpp`: Attestation-related methods
   - `OpenEnclaveEnclave.Events.cpp`: Event trigger methods
   - `OpenEnclaveEnclave.Randomness.cpp`: Randomness service methods
   - `OpenEnclaveEnclave.Compliance.cpp`: Compliance service methods
   - `OpenEnclaveEnclave.MessageProcessor.cpp`: Message processing methods

2. **JavaScript Engine**: A modular JavaScript execution system:
   - `JavaScriptEngine.h/cpp`: Defines the `IJavaScriptEngine` interface and common functionality
   - `JavaScriptEngineFactory.h/cpp`: Factory for creating JavaScript engine instances
   - `JavaScriptManager.cpp`: Manages JavaScript execution contexts
   - `QuickJs/`: Implementation of the QuickJS engine

3. **KeyManager**: Manages cryptographic keys for the enclave:
   - `KeyManager.Core.cpp`: Core key management functionality
   - `KeyManager.Crypto.cpp`: Cryptographic operations
   - `KeyManager.Storage.cpp`: Key storage and persistence

4. **ComplianceService**: Ensures JavaScript code complies with regulatory requirements:
   - `ComplianceService.Core.cpp`: Core compliance service functionality
   - `ComplianceService.Rules.cpp`: Compliance rules implementation
   - `ComplianceService.Verification.cpp`: Code verification functionality

5. **StorageManager**: A persistent storage manager that provides secure storage for enclave data.

6. **SecretManager**: A secret manager that provides secure storage for user secrets.

7. **GasAccounting**: A gas accounting system that tracks the computational resources used by JavaScript functions.

8. **EventTrigger**: An event trigger system that allows JavaScript functions to be executed in response to specific events.

9. **RandomnessService**: A provably fair randomness service that provides secure random number generation with verifiable proofs.

10. **OpenEnclaveUtils**: Utility functions for OpenEnclave operations.

### Workflow

The typical workflow for executing a JavaScript function in the enclave is as follows:

1. The host application loads the enclave and initializes it.
2. A Neo N3 smart contract emits an event to request JavaScript execution.
3. The NSL detects the event and calls the enclave to execute the corresponding JavaScript function.
4. The JavaScript function can access user secrets stored in the enclave.
5. The enclave executes the JavaScript function securely.
6. The JavaScript function can generate a callback transaction to send results back to the Neo N3 blockchain.
7. The enclave returns the result of the function execution to the host application.
8. The host application can send the callback transaction to the Neo N3 blockchain.

This workflow enables a powerful pattern where:
- Smart contracts remain transparent and auditable on the blockchain
- Sensitive computations and data remain confidential in the enclave
- Results can be verified and used on-chain

For detailed workflow diagrams, see:
- [JavaScript Execution Workflow](diagrams/JavaScript_Execution_Workflow.md)
- [Event Trigger Workflow](diagrams/Event_Trigger_Workflow.md)
- [Randomness Service Workflow](diagrams/Randomness_Service_Workflow.md)
- [Compliance Service Workflow](diagrams/Compliance_Service_Workflow.md)

## OpenEnclaveEnclave

The `OpenEnclaveEnclave` class is the main enclave class that provides the core functionality of the enclave. It is implemented as a singleton to ensure that there is only one instance of the enclave.

### Initialization

The enclave is initialized using the `initialize` method, which sets up the enclave environment, including the JavaScript engine, storage manager, secret manager, and gas accounting system.

```cpp
bool OpenEnclaveEnclave::initialize() {
    if (initialized_) {
        return true;
    }

    try {
        // Initialize components
        js_engine_ = std::make_unique<JsEngine>();
        storage_manager_ = std::make_unique<StorageManager>();
        secret_manager_ = std::make_unique<SecretManager>();
        gas_accounting_ = std::make_unique<GasAccounting>();

        // Initialize JavaScript engine
        if (!js_engine_->initialize()) {
            secure_log("Failed to initialize JavaScript engine");
            return false;
        }

        // Initialize storage manager
        if (!storage_manager_->initialize("enclave_storage")) {
            secure_log("Failed to initialize storage manager");
            return false;
        }

        // Initialize secret manager
        if (!secret_manager_->initialize()) {
            secure_log("Failed to initialize secret manager");
            return false;
        }

        // Initialize gas accounting
        if (!gas_accounting_->initialize()) {
            secure_log("Failed to initialize gas accounting");
            return false;
        }

        initialized_ = true;
        return true;
    } catch (const std::exception& e) {
        secure_log(std::string("Error initializing enclave: ") + e.what());
        return false;
    } catch (...) {
        secure_log("Unknown error initializing enclave");
        return false;
    }
}
```

### JavaScript Execution

The enclave executes JavaScript code using the `execute_javascript` method, which takes the JavaScript code, input data, user secrets, function ID, and user ID as parameters.

```cpp
std::string OpenEnclaveEnclave::execute_javascript(
    const std::string& code,
    const std::string& input,
    const std::string& secrets,
    const std::string& function_id,
    const std::string& user_id,
    uint64_t& gas_used) {

    if (!initialized_) {
        json error_response = {
            {"success", false},
            {"error", "Enclave not initialized"}
        };
        return error_response.dump();
    }

    try {
        // Create a temporary JavaScript context
        uint64_t context_id = create_js_context();
        if (context_id == 0) {
            json error_response = {
                {"success", false},
                {"error", "Failed to create JavaScript context"}
            };
            return error_response.dump();
        }

        // Get the JavaScript engine
        auto it = js_contexts_.find(context_id);
        if (it == js_contexts_.end()) {
            json error_response = {
                {"success", false},
                {"error", "Invalid context ID"}
            };
            return error_response.dump();
        }

        JsEngine* js_engine = it->second.get();

        // Parse the secrets and set them in the context
        if (!secrets.empty()) {
            try {
                json secrets_json = json::parse(secrets);
                for (auto& [key, value] : secrets_json.items()) {
                    if (value.is_string()) {
                        store_user_secret(user_id, key, value.get<std::string>());
                    }
                }
            } catch (const std::exception& e) {
                secure_log(std::string("Error parsing secrets: ") + e.what());
            }
        }

        // Start measuring execution time and memory
        auto start_time = std::chrono::high_resolution_clock::now();

        // Execute the JavaScript code
        std::string result;
        try {
            // Set up the execution environment with user secrets
            js_engine->set_user_id(user_id);

            // Execute the code
            result = js_engine->execute(code, input);

            // Measure execution time
            auto end_time = std::chrono::high_resolution_clock::now();
            auto duration = std::chrono::duration_cast<std::chrono::milliseconds>(end_time - start_time).count();

            // Get gas used
            gas_used = js_engine->get_gas_used();

            // Record metrics
            record_execution_metrics(function_id, user_id, gas_used);

            json response = {
                {"success", true},
                {"result", result},
                {"execution_time_ms", duration},
                {"gas_used", gas_used}
            };

            // Clean up the context
            destroy_js_context(context_id);

            return response.dump();
        } catch (const std::exception& e) {
            // Record failure
            record_execution_failure(function_id, user_id, e.what());

            // Clean up the context
            destroy_js_context(context_id);

            json error_response = {
                {"success", false},
                {"error", e.what()}
            };
            return error_response.dump();
        } catch (...) {
            // Record failure
            record_execution_failure(function_id, user_id, "Unknown error");

            // Clean up the context
            destroy_js_context(context_id);

            json error_response = {
                {"success", false},
                {"error", "Unknown error"}
            };
            return error_response.dump();
        }
    } catch (const std::exception& e) {
        json error_response = {
            {"success", false},
            {"error", e.what()}
        };
        return error_response.dump();
    } catch (...) {
        json error_response = {
            {"success", false},
            {"error", "Unknown error"}
        };
        return error_response.dump();
    }
}
```

## JavaScript Engine Architecture

The JavaScript engine architecture is designed to be modular and extensible, allowing different JavaScript engines to be used interchangeably.

### IJavaScriptEngine Interface

The `IJavaScriptEngine` interface defines the contract for JavaScript engines:

```cpp
class IJavaScriptEngine {
public:
    virtual ~IJavaScriptEngine() = default;

    /**
     * @brief Initialize the JavaScript engine
     * @return True if initialization was successful, false otherwise
     */
    virtual bool initialize() = 0;

    /**
     * @brief Execute JavaScript code
     * @param code The JavaScript code to execute
     * @param input_json The input data as a JSON string
     * @param secrets_json The secrets data as a JSON string
     * @param function_id The function ID
     * @param user_id The user ID
     * @param gas_used Output parameter for the amount of gas used
     * @return The result of the execution as a JSON string
     */
    virtual std::string execute(
        const std::string& code,
        const std::string& input_json,
        const std::string& secrets_json,
        const std::string& function_id,
        const std::string& user_id,
        uint64_t& gas_used) = 0;

    /**
     * @brief Verify the hash of JavaScript code
     * @param code The JavaScript code
     * @param hash The hash to verify
     * @return True if the hash is valid, false otherwise
     */
    virtual bool verify_code_hash(
        const std::string& code,
        const std::string& hash) = 0;

    /**
     * @brief Calculate the hash of JavaScript code
     * @param code The JavaScript code
     * @return The hash of the code
     */
    virtual std::string calculate_code_hash(
        const std::string& code) = 0;

    /**
     * @brief Reset the gas accounting
     */
    virtual void reset_gas_used() = 0;

    /**
     * @brief Get the amount of gas used
     * @return The amount of gas used
     */
    virtual uint64_t get_gas_used() const = 0;
};
```

### JavaScriptEngineFactory

The `JavaScriptEngineFactory` class creates instances of JavaScript engines:

```cpp
class JavaScriptEngineFactory {
public:
    /**
     * @brief Engine types supported by the factory
     */
    enum class EngineType {
        QuickJs,
        V8,
        Duktape
    };

    /**
     * @brief Create a JavaScript engine instance
     * @param type The type of engine to create
     * @param gas_accounting The gas accounting manager
     * @param secret_manager The secret manager
     * @param storage_manager The storage manager
     * @return A unique pointer to the created engine
     */
    static std::unique_ptr<IJavaScriptEngine> CreateEngine(
        EngineType type,
        GasAccounting* gas_accounting,
        SecretManager* secret_manager,
        StorageManager* storage_manager);

    /**
     * @brief Get the default engine type
     * @return The default engine type
     */
    static EngineType GetDefaultEngineType();

    /**
     * @brief Get the engine type from a string
     * @param type_str The engine type as a string
     * @return The engine type
     */
    static EngineType GetEngineTypeFromString(const std::string& type_str);
};
```

### JavaScriptManager

The `JavaScriptManager` class manages JavaScript execution contexts:

```cpp
class JavaScriptManager {
public:
    /**
     * @brief Initialize a new instance of the JavaScriptManager class
     * @param gas_accounting The gas accounting manager
     * @param secret_manager The secret manager
     * @param storage_manager The storage manager
     */
    JavaScriptManager(
        GasAccounting* gas_accounting,
        SecretManager* secret_manager,
        StorageManager* storage_manager);

    /**
     * @brief Execute JavaScript code
     * @param context The JavaScript execution context
     * @return True if execution was successful, false otherwise
     */
    bool execute(JavaScriptContext& context);

    /**
     * @brief Verify the hash of JavaScript code
     * @param code The JavaScript code
     * @param hash The hash to verify
     * @return True if the hash is valid, false otherwise
     */
    bool verify_code_hash(
        const std::string& code,
        const std::string& hash);

    /**
     * @brief Calculate the hash of JavaScript code
     * @param code The JavaScript code
     * @return The hash of the code
     */
    std::string calculate_code_hash(
        const std::string& code);
};
```

### QuickJS Implementation

The QuickJS implementation is located in the `QuickJs` directory and consists of the following files:

- `QuickJsExecutor.h/cpp`: Core QuickJS execution functionality
- `QuickJsEngineAdapter.h/cpp`: Adapter to implement the `IJavaScriptEngine` interface
- `JsApi.h`: JavaScript API definitions

The `QuickJsEngineAdapter` class adapts the `QuickJsExecutor` to the `IJavaScriptEngine` interface:

```cpp
class QuickJsEngineAdapter : public IJavaScriptEngine {
public:
    QuickJsEngineAdapter(
        GasAccounting* gas_accounting,
        SecretManager* secret_manager,
        StorageManager* storage_manager);

    ~QuickJsEngineAdapter() override;

    bool initialize() override;

    std::string execute(
        const std::string& code,
        const std::string& input_json,
        const std::string& secrets_json,
        const std::string& function_id,
        const std::string& user_id,
        uint64_t& gas_used) override;

    bool verify_code_hash(
        const std::string& code,
        const std::string& hash) override;

    std::string calculate_code_hash(
        const std::string& code) override;

    void reset_gas_used() override;

    uint64_t get_gas_used() const override;

private:
    std::unique_ptr<NeoServiceLayer::Tee::Enclave::QuickJs::QuickJsExecutor> _executor;
    GasAccounting* _gas_accounting;
    SecretManager* _secret_manager;
    StorageManager* _storage_manager;
    uint64_t _gas_used;
    bool _initialized;
    std::mutex _mutex;
    std::string _current_function_id;
    std::string _current_user_id;

    // Callback functions and helper methods
    // ...
};
```

## StorageManager

The `StorageManager` class provides persistent storage for the enclave. It supports encryption, compression, and transaction support.

### Initialization

The storage manager is initialized using the `initialize` method, which sets up the storage environment.

```cpp
bool StorageManager::initialize(const std::string& storage_path) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (initialized_) {
        return true;
    }

    initialized_ = pimpl->initialize(storage_path);
    storage_path_ = storage_path;
    return initialized_;
}
```

### Data Storage and Retrieval

The storage manager stores and retrieves data using the `store` and `retrieve` methods.

```cpp
bool StorageManager::store(const std::string& key, const std::vector<uint8_t>& data) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_) {
        throw std::runtime_error("Storage manager not initialized");
    }

    return pimpl->store(key, data);
}

std::vector<uint8_t> StorageManager::retrieve(const std::string& key) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_) {
        throw std::runtime_error("Storage manager not initialized");
    }

    return pimpl->retrieve(key);
}
```

## SecretManager

The `SecretManager` class provides secure storage for user secrets. It supports encryption and persistent storage.

### Initialization

The secret manager is initialized using the `initialize` method, which sets up the secret storage environment.

```cpp
bool SecretManager::initialize() {
    std::lock_guard<std::mutex> lock(mutex);

    if (initialized_) {
        return true;
    }

    try {
        // Generate encryption key
        generate_encryption_key();

        // Try to load secrets from persistent storage
        load_from_persistent_storage();

        initialized_ = true;
        return true;
    } catch (const std::exception& e) {
        host_log(e.what());
        return false;
    } catch (...) {
        host_log("Unknown error initializing SecretManager");
        return false;
    }
}
```

### Secret Storage and Retrieval

The secret manager stores and retrieves secrets using the `store_secret` and `get_secret` methods.

```cpp
bool SecretManager::store_secret(const std::string& user_id, const std::string& secret_name, const std::string& secret_value) {
    if (user_id.empty() || secret_name.empty()) {
        return false;
    }

    std::lock_guard<std::mutex> lock(mutex);

    if (!initialized_) {
        if (!initialize()) {
            host_log("Failed to initialize SecretManager");
            return false;
        }
    }

    try {
        // Encrypt the secret value
        std::string encrypted_value = encrypt_value(secret_value);

        // Store the encrypted secret
        user_secrets[user_id][secret_name] = encrypted_value;

        // Save to persistent storage
        save_to_persistent_storage();

        return true;
    } catch (const std::exception& e) {
        host_log(e.what());
        return false;
    } catch (...) {
        host_log("Unknown error storing secret");
        return false;
    }
}

std::string SecretManager::get_secret(const std::string& user_id, const std::string& secret_name) {
    if (user_id.empty() || secret_name.empty()) {
        return "";
    }

    std::lock_guard<std::mutex> lock(mutex);

    if (!initialized_) {
        if (!initialize()) {
            host_log("Failed to initialize SecretManager");
            return "";
        }
    }

    try {
        // Check if the user exists
        auto user_it = user_secrets.find(user_id);
        if (user_it == user_secrets.end()) {
            return ""; // User not found
        }

        // Check if the secret exists
        auto secret_it = user_it->second.find(secret_name);
        if (secret_it == user_it->second.end()) {
            return ""; // Secret not found
        }

        // Decrypt and return the secret value
        return decrypt_value(secret_it->second);
    } catch (const std::exception& e) {
        host_log(e.what());
        return "";
    } catch (...) {
        host_log("Unknown error getting secret");
        return "";
    }
}
```

## GasAccounting

The `GasAccounting` class provides gas accounting for JavaScript execution. It tracks the computational resources used by JavaScript functions.

### Initialization

The gas accounting system is initialized using the `initialize` method, which sets up the gas accounting environment.

```cpp
bool GasAccounting::initialize() {
    std::lock_guard<std::mutex> lock(mutex);

    if (initialized_) {
        return true;
    }

    try {
        // Reset gas used
        gas_used = 0;

        // Set default gas limit
        gas_limit = UINT64_MAX;

        initialized_ = true;
        return true;
    } catch (const std::exception& e) {
        host_log(e.what());
        return false;
    } catch (...) {
        host_log("Unknown error initializing GasAccounting");
        return false;
    }
}
```

### Gas Usage Tracking

The gas accounting system tracks gas usage using the `use_gas` method.

```cpp
void GasAccounting::use_gas(uint64_t amount) {
    std::lock_guard<std::mutex> lock(mutex);

    if (!initialized_) {
        if (!initialize()) {
            host_log("Failed to initialize GasAccounting");
            throw std::runtime_error("GasAccounting not initialized");
        }
    }

    // Check for overflow
    if (UINT64_MAX - gas_used < amount) {
        host_log("Gas usage overflow");
        throw std::runtime_error("Gas usage overflow");
    }

    gas_used += amount;

    // Check if gas limit is exceeded
    if (gas_used > gas_limit) {
        std::string error_message = "Gas limit exceeded: " + std::to_string(gas_used) + " > " + std::to_string(gas_limit);
        host_log(error_message.c_str());
        throw std::runtime_error(error_message);
    }
}
```

## EventTrigger

The `EventTrigger` class provides an event trigger system for the enclave, allowing JavaScript functions to be executed in response to specific events.

### Event Types

The event trigger system supports the following event types:

1. **Blockchain Events**: Events that occur on the blockchain, such as token transfers or contract executions.
2. **Scheduled Events**: Events that occur at specific times or intervals.
3. **Storage Events**: Events that occur when data is stored or retrieved.
4. **External Events**: Events that are triggered by external systems.

### Trigger Registration

Triggers are registered using the `register_trigger` method, which takes the event type, function ID, user ID, and condition as parameters.

```cpp
std::string OpenEnclaveEnclave::register_trigger(const std::string& event_type, const std::string& function_id,
                                               const std::string& user_id, const std::string& condition) {
    if (!initialized_) {
        return "";
    }

    if (!event_trigger_manager_) {
        secure_log("Event trigger manager not initialized");
        return "";
    }

    try {
        // Create trigger info
        EventTriggerInfo trigger;
        trigger.id = OpenEnclaveUtils::generate_uuid();

        // Set trigger type based on event_type
        if (event_type == "blockchain") {
            trigger.type = EventTriggerType::Blockchain;
        } else if (event_type == "schedule") {
            trigger.type = EventTriggerType::Schedule;
        } else if (event_type == "storage") {
            trigger.type = EventTriggerType::Storage;
        } else {
            trigger.type = EventTriggerType::External;
        }

        trigger.condition = condition;
        trigger.function_id = function_id;
        trigger.user_id = user_id;
        trigger.enabled = true;

        // For scheduled triggers, parse the interval from the condition
        if (trigger.type == EventTriggerType::Schedule) {
            try {
                json condition_json = json::parse(condition);
                if (condition_json.contains("interval_seconds")) {
                    trigger.interval_seconds = condition_json["interval_seconds"].get<uint64_t>();
                    trigger.next_execution_time = OpenEnclaveUtils::get_current_time() + trigger.interval_seconds;
                } else {
                    secure_log("Scheduled trigger missing interval_seconds");
                    return "";
                }
            } catch (const std::exception& e) {
                secure_log(std::string("Error parsing schedule condition: ") + e.what());
                return "";
            }
        }

        // Register the trigger
        if (event_trigger_manager_->register_trigger(trigger)) {
            return trigger.id;
        } else {
            return "";
        }
    } catch (const std::exception& e) {
        secure_log(std::string("Error registering trigger: ") + e.what());
        return "";
    } catch (...) {
        secure_log("Unknown error registering trigger");
        return "";
    }
}
```

### Event Processing

Events are processed using the `process_blockchain_event` and `process_scheduled_triggers` methods.

```cpp
int OpenEnclaveEnclave::process_blockchain_event(const std::string& event_data) {
    if (!initialized_) {
        return 0;
    }

    if (!event_trigger_manager_) {
        secure_log("Event trigger manager not initialized");
        return 0;
    }

    try {
        return event_trigger_manager_->process_blockchain_event(event_data);
    } catch (const std::exception& e) {
        secure_log(std::string("Error processing blockchain event: ") + e.what());
        return 0;
    } catch (...) {
        secure_log("Unknown error processing blockchain event");
        return 0;
    }
}

int OpenEnclaveEnclave::process_scheduled_triggers(uint64_t current_time) {
    if (!initialized_) {
        return 0;
    }

    if (!event_trigger_manager_) {
        secure_log("Event trigger manager not initialized");
        return 0;
    }

    try {
        return event_trigger_manager_->process_scheduled_triggers(current_time);
    } catch (const std::exception& e) {
        secure_log(std::string("Error processing scheduled triggers: ") + e.what());
        return 0;
    } catch (...) {
        secure_log("Unknown error processing scheduled triggers");
        return 0;
    }
}
```

## RandomnessService

The `RandomnessService` class provides a provably fair randomness service for the enclave, allowing JavaScript functions to generate random numbers that can be verified by external parties.

### Random Number Generation

Random numbers are generated using the `generate_random_number` method, which takes the minimum and maximum values, user ID, and request ID as parameters.

```cpp
uint64_t RandomnessService::generate_random_number(uint64_t min, uint64_t max, const std::string& user_id, const std::string& request_id) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_) {
        if (!initialize()) {
            throw std::runtime_error("Failed to initialize RandomnessService");
        }
    }

    return pimpl->generate_random_number(min, max, user_id, request_id);
}
```

### Proof Generation and Verification

Proofs are generated and verified using the `get_random_number_proof` and `verify_random_number` methods.

```cpp
std::string RandomnessService::get_random_number_proof(uint64_t random_number, uint64_t min, uint64_t max,
                                                     const std::string& user_id, const std::string& request_id) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_) {
        if (!initialize()) {
            throw std::runtime_error("Failed to initialize RandomnessService");
        }
    }

    return pimpl->get_random_number_proof(random_number, min, max, user_id, request_id);
}

bool RandomnessService::verify_random_number(uint64_t random_number, uint64_t min, uint64_t max,
                                           const std::string& user_id, const std::string& request_id,
                                           const std::string& proof) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_) {
        if (!initialize()) {
            throw std::runtime_error("Failed to initialize RandomnessService");
        }
    }

    return pimpl->verify_random_number(random_number, min, max, user_id, request_id, proof);
}
```

## ComplianceService

The `ComplianceService` class provides a compliance verification service for the enclave, allowing JavaScript functions to be verified for compliance with regulatory requirements.

### Compliance Verification

JavaScript code is verified for compliance using the `verify_compliance` method, which takes the code, user ID, function ID, and compliance rules as parameters.

```cpp
std::string ComplianceService::verify_compliance(const std::string& code, const std::string& user_id,
                                               const std::string& function_id, const std::string& compliance_rules) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_) {
        if (!initialize()) {
            throw std::runtime_error("Failed to initialize ComplianceService");
        }
    }

    return pimpl->verify_compliance(code, user_id, function_id, compliance_rules);
}
```

### Compliance Rules

Compliance rules are managed using the `get_compliance_rules` and `set_compliance_rules` methods.

```cpp
std::string ComplianceService::get_compliance_rules(const std::string& jurisdiction) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_) {
        if (!initialize()) {
            throw std::runtime_error("Failed to initialize ComplianceService");
        }
    }

    return pimpl->get_compliance_rules(jurisdiction);
}

bool ComplianceService::set_compliance_rules(const std::string& jurisdiction, const std::string& rules) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_) {
        if (!initialize()) {
            throw std::runtime_error("Failed to initialize ComplianceService");
        }
    }

    return pimpl->set_compliance_rules(jurisdiction, rules);
}
```

### Identity Verification

User identities are verified using the `verify_identity` method, which takes the user ID, identity data, and jurisdiction as parameters.

```cpp
std::string ComplianceService::verify_identity(const std::string& user_id, const std::string& identity_data,
                                             const std::string& jurisdiction) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_) {
        if (!initialize()) {
            throw std::runtime_error("Failed to initialize ComplianceService");
        }
    }

    return pimpl->verify_identity(user_id, identity_data, jurisdiction);
}
```

## Building and Testing

The enclave is built using the OpenEnclave SDK and can be tested using the provided test suite.

### Building the Enclave

To build the enclave, run the following command:

```powershell
.\build_enclave.ps1
```

This script compiles the C++ enclave code and signs the enclave.

### Testing the Enclave

To test the enclave, run the following command:

```powershell
dotnet test tests/NeoServiceLayer.Tee.Enclave.Tests
```

This command runs the unit tests for the enclave.

To run the integration tests, use the following command:

```powershell
dotnet test tests/NeoServiceLayer.Integration.Tests
```

This command runs the integration tests for the enclave.

## Conclusion

The Neo Confidential Serverless Layer (NCSL) enclave provides a secure computing environment for executing JavaScript functions. It uses Trusted Execution Environments (TEEs) to ensure the confidentiality and integrity of the executed code and data.

The enclave provides features for JavaScript execution, secret management, persistent storage, and gas accounting, making it suitable for a wide range of confidential computing applications.
