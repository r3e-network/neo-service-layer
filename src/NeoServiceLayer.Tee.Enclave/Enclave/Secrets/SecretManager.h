#pragma once

#include <string>
#include <map>
#include <mutex>
#include <vector>
#include "../Core/Logger.h"

// Forward declarations
class StorageManager;
class KeyManager;

/**
 * @brief Manager for user secrets
 *
 * This class provides functionality for storing and retrieving user secrets
 * in the enclave.
 */
class SecretManager {
public:
    /**
     * @brief Constructor
     *
     * @param storage_manager The storage manager
     * @param key_manager The key manager
     */
    SecretManager(StorageManager* storage_manager, KeyManager* key_manager);

    /**
     * @brief Destructor
     */
    ~SecretManager();

    /**
     * @brief Initialize the secret manager
     *
     * @return True if initialization was successful, false otherwise
     */
    bool initialize();

    /**
     * @brief Store a secret for a user
     *
     * @param user_id The user ID
     * @param secret_name The secret name
     * @param secret_value The secret value
     * @return True if the secret was stored successfully, false otherwise
     */
    bool store_secret(const std::string& user_id, const std::string& secret_name, const std::string& secret_value);

    /**
     * @brief Get a secret for a user
     *
     * @param user_id The user ID
     * @param secret_name The secret name
     * @return The secret value, or an empty string if not found
     */
    std::string get_secret(const std::string& user_id, const std::string& secret_name);

    /**
     * @brief Delete a secret for a user
     *
     * @param user_id The user ID
     * @param secret_name The secret name
     * @return True if the secret was deleted successfully, false otherwise
     */
    bool delete_secret(const std::string& user_id, const std::string& secret_name);

    /**
     * @brief List all secrets for a user
     *
     * @param user_id The user ID
     * @return A vector of secret names
     */
    std::vector<std::string> list_secrets(const std::string& user_id);

    /**
     * @brief Get all secrets for a user as a JSON string
     *
     * @param user_id The user ID
     * @return A JSON string containing all secrets for the user
     */
    std::string get_user_secrets_json(const std::string& user_id);

    /**
     * @brief Check if the secret manager is initialized
     *
     * @return True if the secret manager is initialized, false otherwise
     */
    bool is_initialized() const;

    /**
     * @brief Save secrets to persistent storage
     *
     * @return True if the secrets were saved successfully, false otherwise
     */
    bool save_to_persistent_storage();

    /**
     * @brief Load secrets from persistent storage
     *
     * @return True if the secrets were loaded successfully, false otherwise
     */
    bool load_from_persistent_storage();

private:
    // Storage manager
    StorageManager* _storage_manager;

    // Key manager
    KeyManager* _key_manager;

    // Initialization flag
    bool _initialized;

    // Mutex for thread safety
    mutable std::mutex _mutex;

    // Map of user_id -> (secret_name -> encrypted_secret_value)
    std::map<std::string, std::map<std::string, std::string>> _user_secrets;

    // Encryption key for this enclave instance
    std::vector<uint8_t> _encryption_key;

    // Helper methods
    void secure_log(const std::string& message);

    // Encrypt a secret value
    std::string encrypt_value(const std::string& value);

    // Decrypt a secret value
    std::string decrypt_value(const std::string& encrypted_value);

    // Generate a random encryption key
    void generate_encryption_key();
};
