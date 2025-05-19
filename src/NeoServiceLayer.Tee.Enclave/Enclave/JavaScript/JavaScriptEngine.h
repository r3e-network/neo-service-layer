#ifndef JAVASCRIPT_ENGINE_H
#define JAVASCRIPT_ENGINE_H

#include <string>
#include <memory>
#include <map>
#include <vector>
#include <mutex>
#include "GasAccounting.h"
#include "SecretManager.h"

// Forward declarations
class StorageManager;

/**
 * @brief Interface for JavaScript engines
 */
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

    /**
     * @brief Precompile JavaScript code for faster execution
     * @param code The JavaScript code to precompile
     * @param function_id The function ID
     * @return True if precompilation was successful, false otherwise
     */
    virtual bool precompile(
        const std::string& code,
        const std::string& function_id) = 0;

    /**
     * @brief Check if JavaScript code is precompiled
     * @param function_id The function ID
     * @return True if the code is precompiled, false otherwise
     */
    virtual bool is_precompiled(
        const std::string& function_id) const = 0;

    /**
     * @brief Execute precompiled JavaScript code
     * @param function_id The function ID of the precompiled code
     * @param input_json The input data as a JSON string
     * @param secrets_json The secrets data as a JSON string
     * @param user_id The user ID
     * @param gas_used Output parameter for the amount of gas used
     * @return The result of the execution as a JSON string
     */
    virtual std::string execute_precompiled(
        const std::string& function_id,
        const std::string& input_json,
        const std::string& secrets_json,
        const std::string& user_id,
        uint64_t& gas_used) = 0;

    /**
     * @brief Clear the precompiled code cache
     */
    virtual void clear_precompiled_cache() = 0;
};



/**
 * @brief Context for JavaScript execution
 */
struct JavaScriptContext {
    std::string function_id;
    std::string user_id;
    std::string code;
    std::string input_json;
    std::string secrets_json;
    uint64_t gas_limit;
    uint64_t gas_used;
    std::string result;
    bool success;
    std::string error;
};

/**
 * @brief Manager for JavaScript execution
 */
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

private:
    std::unique_ptr<IJavaScriptEngine> _engine;
    GasAccounting* _gas_accounting;
    SecretManager* _secret_manager;
    StorageManager* _storage_manager;
    std::mutex _mutex;
};

#endif // JAVASCRIPT_ENGINE_H
