#include "NeoServiceLayerEnclave.Occlum.h"
#include "OcclumIntegration.h"
#include "JavaScriptEngine.h"
#include "SecretManager.h"
#include "StorageManager.h"
#include "KeyManager.h"
#include "GasAccountingManager.h"
#include "EventTriggerManager.h"
#include <nlohmann/json.hpp>
#include <string>
#include <vector>
#include <map>
#include <mutex>
#include <memory>
#include <cstdio>

using json = nlohmann::json;

// Global instances of managers
static std::unique_ptr<StorageManager> g_storage_manager;
static std::unique_ptr<KeyManager> g_key_manager;
static std::unique_ptr<SecretManager> g_secret_manager;
static std::unique_ptr<GasAccountingManager> g_gas_accounting_manager;
static std::unique_ptr<JavaScriptEngine> g_js_engine;
static std::unique_ptr<EventTriggerManager> g_event_trigger_manager;

// Map of JavaScript contexts
static std::map<uint64_t, std::unique_ptr<JavaScriptContext>> g_js_contexts;
static std::mutex g_js_contexts_mutex;
static uint64_t g_next_js_context_id = 1;

// Initialize the enclave
int enclave_initialize() {
    try {
        // Initialize Occlum
        if (!OcclumIntegration::Initialize()) {
            fprintf(stderr, "Failed to initialize Occlum\n");
            return -1;
        }

        // Create managers
        g_storage_manager = std::make_unique<StorageManager>();
        if (!g_storage_manager->initialize()) {
            fprintf(stderr, "Failed to initialize StorageManager\n");
            return -1;
        }

        g_key_manager = std::make_unique<KeyManager>();
        if (!g_key_manager->initialize()) {
            fprintf(stderr, "Failed to initialize KeyManager\n");
            return -1;
        }

        g_secret_manager = std::make_unique<SecretManager>(g_storage_manager.get(), g_key_manager.get());
        if (!g_secret_manager->initialize()) {
            fprintf(stderr, "Failed to initialize SecretManager\n");
            return -1;
        }

        g_gas_accounting_manager = std::make_unique<GasAccountingManager>(g_storage_manager.get());
        if (!g_gas_accounting_manager->initialize()) {
            fprintf(stderr, "Failed to initialize GasAccountingManager\n");
            return -1;
        }

        g_js_engine = std::make_unique<JavaScriptEngine>(g_gas_accounting_manager.get(), g_secret_manager.get(), g_storage_manager.get());
        if (!g_js_engine->initialize()) {
            fprintf(stderr, "Failed to initialize JavaScriptEngine\n");
            return -1;
        }

        g_event_trigger_manager = std::make_unique<EventTriggerManager>(g_storage_manager.get(), g_js_engine.get());
        if (!g_event_trigger_manager->initialize()) {
            fprintf(stderr, "Failed to initialize EventTriggerManager\n");
            return -1;
        }

        fprintf(stderr, "Enclave initialized successfully\n");
        return 0;
    }
    catch (const std::exception& ex) {
        fprintf(stderr, "Error initializing enclave: %s\n", ex.what());
        return -1;
    }
    catch (...) {
        fprintf(stderr, "Unknown error initializing enclave\n");
        return -1;
    }
}

// Get the status of the enclave
int enclave_get_status(
    char* status_buffer,
    size_t status_size,
    size_t* status_size_out) {
    try {
        // Create status JSON
        json status = {
            {"initialized", true},
            {"mrenclave", OcclumIntegration::GetMrEnclave()},
            {"mrsigner", OcclumIntegration::GetMrSigner()},
            {"storage_initialized", g_storage_manager ? g_storage_manager->is_initialized() : false},
            {"key_manager_initialized", g_key_manager ? g_key_manager->is_initialized() : false},
            {"secret_manager_initialized", g_secret_manager ? g_secret_manager->is_initialized() : false},
            {"gas_accounting_initialized", g_gas_accounting_manager ? g_gas_accounting_manager->is_initialized() : false},
            {"js_engine_initialized", g_js_engine ? g_js_engine->is_initialized() : false},
            {"event_trigger_initialized", g_event_trigger_manager ? g_event_trigger_manager->is_initialized() : false},
            {"js_contexts", g_js_contexts.size()}
        };

        // Convert to string
        std::string status_str = status.dump();

        // Check if buffer is large enough
        if (status_str.size() + 1 > status_size) {
            *status_size_out = status_str.size() + 1;
            return -1;
        }

        // Copy to buffer
        memcpy(status_buffer, status_str.c_str(), status_str.size() + 1);
        *status_size_out = status_str.size() + 1;

        return 0;
    }
    catch (const std::exception& ex) {
        fprintf(stderr, "Error getting enclave status: %s\n", ex.what());
        return -1;
    }
    catch (...) {
        fprintf(stderr, "Unknown error getting enclave status\n");
        return -1;
    }
}

// Process a message from the host
int enclave_process_message(
    int message_type,
    const char* message_data,
    size_t message_size,
    char* response_buffer,
    size_t response_size,
    size_t* response_size_out) {
    try {
        // Parse message data
        std::string message_str(message_data, message_size);
        json message = json::parse(message_str);

        // Process message based on type
        json response;
        switch (message_type) {
            case 1: // Execute JavaScript
                {
                    std::string code = message["code"];
                    std::string input = message["input"];
                    std::string user_id = message["user_id"];
                    std::string function_id = message["function_id"];
                    uint64_t gas_used = 0;

                    std::string result = g_js_engine->execute(code, input, "{}", function_id, user_id, gas_used);

                    response = {
                        {"result", result},
                        {"gas_used", gas_used}
                    };
                }
                break;

            case 2: // Store secret
                {
                    std::string user_id = message["user_id"];
                    std::string secret_name = message["secret_name"];
                    std::string secret_value = message["secret_value"];

                    bool success = g_secret_manager->store_secret(user_id, secret_name, secret_value);

                    response = {
                        {"success", success}
                    };
                }
                break;

            case 3: // Get secret
                {
                    std::string user_id = message["user_id"];
                    std::string secret_name = message["secret_name"];

                    std::string secret_value = g_secret_manager->get_secret(user_id, secret_name);

                    response = {
                        {"secret_value", secret_value}
                    };
                }
                break;

            case 4: // Delete secret
                {
                    std::string user_id = message["user_id"];
                    std::string secret_name = message["secret_name"];

                    bool success = g_secret_manager->delete_secret(user_id, secret_name);

                    response = {
                        {"success", success}
                    };
                }
                break;

            case 5: // Process blockchain event
                {
                    std::string event_data = message["event_data"];

                    int processed_count = g_event_trigger_manager->process_blockchain_event(event_data);

                    response = {
                        {"processed_count", processed_count}
                    };
                }
                break;

            default:
                response = {
                    {"error", "Unknown message type"}
                };
                break;
        }

        // Convert response to string
        std::string response_str = response.dump();

        // Check if buffer is large enough
        if (response_str.size() + 1 > response_size) {
            *response_size_out = response_str.size() + 1;
            return -1;
        }

        // Copy to buffer
        memcpy(response_buffer, response_str.c_str(), response_str.size() + 1);
        *response_size_out = response_str.size() + 1;

        return 0;
    }
    catch (const std::exception& ex) {
        fprintf(stderr, "Error processing message: %s\n", ex.what());
        return -1;
    }
    catch (...) {
        fprintf(stderr, "Unknown error processing message\n");
        return -1;
    }
}

// Create a JavaScript context
int enclave_create_js_context(uint64_t* context_id) {
    try {
        std::lock_guard<std::mutex> lock(g_js_contexts_mutex);

        // Create a new context
        auto context = std::make_unique<JavaScriptContext>();
        context->context_id = g_next_js_context_id++;

        // Store the context
        *context_id = context->context_id;
        g_js_contexts[*context_id] = std::move(context);

        return 0;
    }
    catch (const std::exception& ex) {
        fprintf(stderr, "Error creating JavaScript context: %s\n", ex.what());
        return -1;
    }
    catch (...) {
        fprintf(stderr, "Unknown error creating JavaScript context\n");
        return -1;
    }
}

// Destroy a JavaScript context
int enclave_destroy_js_context(uint64_t context_id) {
    try {
        std::lock_guard<std::mutex> lock(g_js_contexts_mutex);

        // Find the context
        auto it = g_js_contexts.find(context_id);
        if (it == g_js_contexts.end()) {
            fprintf(stderr, "JavaScript context not found: %lu\n", context_id);
            return -1;
        }

        // Remove the context
        g_js_contexts.erase(it);

        return 0;
    }
    catch (const std::exception& ex) {
        fprintf(stderr, "Error destroying JavaScript context: %s\n", ex.what());
        return -1;
    }
    catch (...) {
        fprintf(stderr, "Unknown error destroying JavaScript context\n");
        return -1;
    }
}

// Execute JavaScript code in a context
int enclave_execute_js_code(
    uint64_t context_id,
    const char* code,
    const char* input,
    const char* user_id,
    const char* function_id,
    char* result_buffer,
    size_t result_size,
    size_t* result_size_out) {
    try {
        std::lock_guard<std::mutex> lock(g_js_contexts_mutex);

        // Find the context
        auto it = g_js_contexts.find(context_id);
        if (it == g_js_contexts.end()) {
            fprintf(stderr, "JavaScript context not found: %lu\n", context_id);
            return -1;
        }

        // Set up the context
        JavaScriptContext& context = *it->second;
        context.code = code;
        context.input = input;
        context.user_id = user_id;
        context.function_id = function_id;
        context.gas_limit = 1000000; // Default gas limit

        // Execute the code
        uint64_t gas_used = 0;
        std::string result = g_js_engine->execute(context.code, context.input, "{}", context.function_id, context.user_id, gas_used);

        // Check if buffer is large enough
        if (result.size() + 1 > result_size) {
            *result_size_out = result.size() + 1;
            return -1;
        }

        // Copy to buffer
        memcpy(result_buffer, result.c_str(), result.size() + 1);
        *result_size_out = result.size() + 1;

        return 0;
    }
    catch (const std::exception& ex) {
        fprintf(stderr, "Error executing JavaScript code: %s\n", ex.what());
        return -1;
    }
    catch (...) {
        fprintf(stderr, "Unknown error executing JavaScript code\n");
        return -1;
    }
}

// Store a user secret
int enclave_store_user_secret(
    const char* user_id,
    const char* secret_name,
    const char* secret_value) {
    try {
        bool success = g_secret_manager->store_secret(user_id, secret_name, secret_value);
        return success ? 0 : -1;
    }
    catch (const std::exception& ex) {
        fprintf(stderr, "Error storing user secret: %s\n", ex.what());
        return -1;
    }
    catch (...) {
        fprintf(stderr, "Unknown error storing user secret\n");
        return -1;
    }
}

// Get a user secret
int enclave_get_user_secret(
    const char* user_id,
    const char* secret_name,
    char* value_buffer,
    size_t value_size,
    size_t* value_size_out) {
    try {
        std::string secret_value = g_secret_manager->get_secret(user_id, secret_name);

        // Check if buffer is large enough
        if (secret_value.size() + 1 > value_size) {
            *value_size_out = secret_value.size() + 1;
            return -1;
        }

        // Copy to buffer
        memcpy(value_buffer, secret_value.c_str(), secret_value.size() + 1);
        *value_size_out = secret_value.size() + 1;

        return 0;
    }
    catch (const std::exception& ex) {
        fprintf(stderr, "Error getting user secret: %s\n", ex.what());
        return -1;
    }
    catch (...) {
        fprintf(stderr, "Unknown error getting user secret\n");
        return -1;
    }
}

// Delete a user secret
int enclave_delete_user_secret(
    const char* user_id,
    const char* secret_name) {
    try {
        bool success = g_secret_manager->delete_secret(user_id, secret_name);
        return success ? 0 : -1;
    }
    catch (const std::exception& ex) {
        fprintf(stderr, "Error deleting user secret: %s\n", ex.what());
        return -1;
    }
    catch (...) {
        fprintf(stderr, "Unknown error deleting user secret\n");
        return -1;
    }
}