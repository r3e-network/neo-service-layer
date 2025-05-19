#pragma once

#include <string>
#include <vector>
#include <map>
#include <memory>
#include <mutex>

// Forward declarations
class JavaScriptEngine;
class SecretManager;
class StorageManager;
class KeyManager;
class EventTriggerManager;
class RemoteAttestationManager;
class BackupManager;
class GasAccountingManager;
class RandomnessService;
class ComplianceService;

/**
 * @brief Main enclave class for Occlum LibOS implementation
 *
 * This class provides a C++ interface to the enclave functionality and manages
 * all the components of the enclave, including JavaScript execution, secret management,
 * storage, key management, event triggers, remote attestation, and backup.
 */
class OcclumEnclave {
public:
    /**
     * @brief Get the singleton instance of the enclave
     * @return The enclave instance
     */
    static OcclumEnclave& getInstance();

    /**
     * @brief Initialize the enclave
     * @param config_data Optional configuration data
     * @return True if initialization was successful, false otherwise
     */
    bool initialize(const std::vector<uint8_t>& config_data = {});

    /**
     * @brief Clean up the enclave
     * @return True if cleanup was successful, false otherwise
     */
    bool cleanup();

    /**
     * @brief Process a message from the host
     * @param message_type The type of message
     * @param message_data The message data
     * @return The response data
     */
    std::string process_message(int message_type, const std::string& message_data);

    /**
     * @brief Get the enclave status
     * @return The enclave status as a JSON string
     */
    std::string get_status();

    /**
     * @brief Initialize the persistent storage system
     * @param storage_path The path to the storage directory
     * @return True if initialization was successful, false otherwise
     */
    bool initialize_storage(const std::string& storage_path);

    /**
     * @brief Create a JavaScript context
     * @return The context ID
     */
    uint64_t create_js_context();

    /**
     * @brief Destroy a JavaScript context
     * @param context_id The context ID
     * @return True if the context was destroyed, false otherwise
     */
    bool destroy_js_context(uint64_t context_id);

    /**
     * @brief Execute JavaScript code
     * @param context_id The context ID
     * @param code The JavaScript code to execute
     * @param input The input data as a JSON string
     * @param user_id The user ID
     * @param function_id The function ID
     * @return The result as a JSON string
     */
    std::string execute_js_code(uint64_t context_id, const std::string& code, const std::string& input,
                               const std::string& user_id, const std::string& function_id);

    /**
     * @brief Execute JavaScript code (legacy interface)
     * @param code The JavaScript code to execute
     * @param input The input data as a JSON string
     * @param secrets The secrets as a JSON string
     * @param function_id The function ID
     * @param user_id The user ID
     * @param gas_used Output parameter for gas used
     * @return The result as a JSON string
     */
    std::string execute_javascript(const std::string& code, const std::string& input, const std::string& secrets,
                                  const std::string& function_id, const std::string& user_id, uint64_t& gas_used);

    /**
     * @brief Store a user secret
     * @param user_id The user ID
     * @param secret_name The secret name
     * @param secret_value The secret value
     * @return True if the secret was stored, false otherwise
     */
    bool store_user_secret(const std::string& user_id, const std::string& secret_name, const std::string& secret_value);

    /**
     * @brief Get a user secret
     * @param user_id The user ID
     * @param secret_name The secret name
     * @return The secret value, or an empty string if not found
     */
    std::string get_user_secret(const std::string& user_id, const std::string& secret_name);

    /**
     * @brief Delete a user secret
     * @param user_id The user ID
     * @param secret_name The secret name
     * @return True if the secret was deleted, false otherwise
     */
    bool delete_user_secret(const std::string& user_id, const std::string& secret_name);

    /**
     * @brief List user secrets
     * @param user_id The user ID
     * @return A list of secret names
     */
    std::vector<std::string> list_user_secrets(const std::string& user_id);

    /**
     * @brief Generate a random number
     * @param min The minimum value
     * @param max The maximum value
     * @return A random number between min and max
     */
    int generate_random_number(int min, int max);

    /**
     * @brief Generate random bytes
     * @param length The number of bytes to generate
     * @return The random bytes
     */
    std::vector<uint8_t> generate_random_bytes(size_t length);

    /**
     * @brief Generate a random UUID
     * @return A random UUID as a string
     */
    std::string generate_uuid();

    /**
     * @brief Generate attestation evidence
     * @return The attestation evidence
     */
    std::vector<uint8_t> generate_attestation_evidence();

    /**
     * @brief Verify attestation evidence
     * @param evidence The attestation evidence
     * @param endorsements The attestation endorsements
     * @return True if the attestation is valid, false otherwise
     */
    bool verify_attestation(const std::vector<uint8_t>& evidence, const std::vector<uint8_t>& endorsements);

    /**
     * @brief Verify compliance of JavaScript code
     * @param code The JavaScript code to verify
     * @param user_id The user ID
     * @param function_id The function ID
     * @param compliance_rules The compliance rules
     * @return The verification result as a JSON string
     */
    std::string verify_compliance(const std::string& code, const std::string& user_id,
                                 const std::string& function_id, const std::string& compliance_rules);

    /**
     * @brief Initialize Occlum
     * @param instance_dir The Occlum instance directory
     * @param log_level The log level
     * @return True if initialization was successful, false otherwise
     */
    bool occlum_init(const std::string& instance_dir, const std::string& log_level);

    /**
     * @brief Execute a command in Occlum
     * @param path The path to the executable
     * @param argv The arguments
     * @param env The environment variables
     * @return The exit code
     */
    int occlum_exec(const std::string& path, const std::vector<std::string>& argv, const std::vector<std::string>& env);

private:
    OcclumEnclave();
    ~OcclumEnclave();

    // Singleton instance
    static OcclumEnclave* _instance;

    // Mutex for thread safety
    std::mutex _mutex;

    // Initialization flag
    bool _initialized;

    // Next context ID
    uint64_t _next_context_id;

    // JavaScript contexts
    std::map<uint64_t, std::unique_ptr<JavaScriptEngine>> _js_contexts;

    // Components
    std::unique_ptr<SecretManager> _secret_manager;
    std::unique_ptr<StorageManager> _storage_manager;
    std::unique_ptr<KeyManager> _key_manager;
    std::unique_ptr<EventTriggerManager> _event_trigger_manager;
    std::unique_ptr<RemoteAttestationManager> _remote_attestation_manager;
    std::unique_ptr<BackupManager> _backup_manager;
    std::unique_ptr<GasAccountingManager> _gas_accounting;
    std::unique_ptr<RandomnessService> _randomness_service;
    std::unique_ptr<ComplianceService> _compliance_service;

    // Helper methods
    bool initialize_components();
    void cleanup_components();
    void secure_log(const std::string& message);

    /**
     * @brief Sign data using the enclave's private key
     * @param data The data to sign
     * @return The signature
     */
    std::vector<uint8_t> sign_data(const std::vector<uint8_t>& data);

    /**
     * @brief Verify a signature using the enclave's public key
     * @param data The data that was signed
     * @param signature The signature to verify
     * @return True if the signature is valid, false otherwise
     */
    bool verify_signature(const std::vector<uint8_t>& data, const std::vector<uint8_t>& signature);

    /**
     * @brief Seal data using the enclave's sealing key
     * @param data The data to seal
     * @return The sealed data
     */
    std::vector<uint8_t> seal_data(const std::vector<uint8_t>& data);

    /**
     * @brief Unseal data using the enclave's sealing key
     * @param sealed_data The sealed data
     * @return The unsealed data
     */
    std::vector<uint8_t> unseal_data(const std::vector<uint8_t>& sealed_data);

    /**
     * @brief Get the key manager
     * @return The key manager
     */
    KeyManager* get_key_manager();
};
