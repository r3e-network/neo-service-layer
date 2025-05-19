#pragma once

#include "../JavaScriptEngine.h"
#include "QuickJsExecutor.h"
#include "../StorageManager.h"
#include "../GasAccounting.h"
#include "../SecretManager.h"
#include "../KeyManager.h"
#include <string>
#include <memory>
#include <mutex>

/**
 * @brief Adapter for the QuickJs engine to implement the IJavaScriptEngine interface
 */
class QuickJsEngineAdapter : public IJavaScriptEngine {
public:
    /**
     * @brief Initialize a new instance of the QuickJsEngineAdapter class
     * @param gas_accounting The gas accounting manager
     * @param secret_manager The secret manager
     * @param storage_manager The storage manager
     */
    QuickJsEngineAdapter(
        GasAccounting* gas_accounting,
        SecretManager* secret_manager,
        StorageManager* storage_manager);

    /**
     * @brief Destructor
     */
    ~QuickJsEngineAdapter() override;

    /**
     * @brief Initialize the JavaScript engine
     * @return True if initialization was successful, false otherwise
     */
    bool initialize() override;

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
    std::string execute(
        const std::string& code,
        const std::string& input_json,
        const std::string& secrets_json,
        const std::string& function_id,
        const std::string& user_id,
        uint64_t& gas_used) override;

    /**
     * @brief Verify the hash of JavaScript code
     * @param code The JavaScript code
     * @param hash The hash to verify
     * @return True if the hash is valid, false otherwise
     */
    bool verify_code_hash(
        const std::string& code,
        const std::string& hash) override;

    /**
     * @brief Calculate the hash of JavaScript code
     * @param code The JavaScript code
     * @return The hash of the code
     */
    std::string calculate_code_hash(
        const std::string& code) override;

    /**
     * @brief Reset the gas accounting
     */
    void reset_gas_used() override;

    /**
     * @brief Get the amount of gas used
     * @return The amount of gas used
     */
    uint64_t get_gas_used() const override;

private:
    std::unique_ptr<NeoServiceLayer::Tee::Enclave::QuickJs::QuickJsExecutor> _executor;
    GasAccounting* _gas_accounting;
    SecretManager* _secret_manager;
    StorageManager* _storage_manager;
    KeyManager* _key_manager;
    uint64_t _gas_used;
    bool _initialized;
    std::mutex _mutex;
    std::string _current_function_id;
    std::string _current_user_id;

    // Callback functions for the QuickJs executor
    void LogCallback(const std::string& message);
    std::string GetStorageCallback(const std::string& key);
    void SetStorageCallback(const std::string& key, const std::string& value);
    void RemoveStorageCallback(const std::string& key);
    void ClearStorageCallback();
    std::string RandomBytesCallback(int size);
    std::string Sha256Callback(const std::string& data);
    std::string SignCallback(const std::string& data, const std::string& key);
    bool VerifyCallback(const std::string& data, const std::string& signature, const std::string& key);
    int64_t GetGasCallback();
    bool UseGasCallback(int64_t amount);
    std::string GetSecretCallback(const std::string& key);
    void SetSecretCallback(const std::string& key, const std::string& value);
    void RemoveSecretCallback(const std::string& key);
    void BlockchainCallbackCallback(const std::string& method, const std::string& result);

    // Helper methods
    std::string GenerateWrappedCode(
        const std::string& code,
        const std::string& input_json,
        const std::string& secrets_json);
};
