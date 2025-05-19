#pragma once

#include <string>
#include <vector>
#include <map>
#include <set>
#include <mutex>
#include <memory>
#include "../Core/Logger.h"

/**
 * @brief Manager for persistent storage operations
 *
 * This class provides functionality for storing and retrieving data
 * in the enclave using Occlum LibOS.
 */
class StorageManager {
public:
    /**
     * @brief Constructor
     */
    StorageManager();

    /**
     * @brief Destructor
     */
    ~StorageManager();

    /**
     * @brief Initialize the storage manager
     *
     * @return True if initialization was successful, false otherwise
     */
    bool initialize();

    /**
     * @brief Set the storage path
     *
     * @param storage_path The path to the storage directory
     * @return True if the path was set successfully, false otherwise
     */
    bool set_storage_path(const std::string& storage_path);

    /**
     * @brief Store string data with a namespace
     *
     * @param namespace_id The namespace ID
     * @param key The key to store the data under
     * @param value The string value to store
     * @return True if the operation was successful, false otherwise
     */
    bool store(const std::string& namespace_id, const std::string& key, const std::string& value);

    /**
     * @brief Store data with a namespace
     *
     * @param namespace_id The namespace ID
     * @param key The key to store the data under
     * @param data The data to store
     * @return True if the operation was successful, false otherwise
     */
    bool store_data(const std::string& namespace_id, const std::string& key, const std::vector<uint8_t>& data);

    /**
     * @brief Retrieve string data with a namespace
     *
     * @param namespace_id The namespace ID
     * @param key The key to retrieve the data for
     * @return The retrieved string value, or an empty string if the key does not exist
     */
    std::string retrieve(const std::string& namespace_id, const std::string& key);

    /**
     * @brief Retrieve data with a namespace
     *
     * @param namespace_id The namespace ID
     * @param key The key to retrieve the data for
     * @param data The retrieved data
     * @return True if the operation was successful, false otherwise
     */
    bool retrieve_data(const std::string& namespace_id, const std::string& key, std::vector<uint8_t>& data);

    /**
     * @brief Delete data with a namespace
     *
     * @param namespace_id The namespace ID
     * @param key The key to delete
     * @return True if the operation was successful, false otherwise
     */
    bool remove(const std::string& namespace_id, const std::string& key);

    /**
     * @brief Check if a key exists with a namespace
     *
     * @param namespace_id The namespace ID
     * @param key The key to check
     * @return True if the key exists, false otherwise
     */
    bool exists(const std::string& namespace_id, const std::string& key);

    /**
     * @brief List all keys with a namespace
     *
     * @param namespace_id The namespace ID
     * @return A list of all keys in the namespace
     */
    std::vector<std::string> list_keys(const std::string& namespace_id);

    /**
     * @brief Begin a transaction
     *
     * @return True if the transaction was started successfully, false otherwise
     */
    bool begin_transaction();

    /**
     * @brief Commit a transaction
     *
     * @return True if the transaction was committed successfully, false otherwise
     */
    bool commit_transaction();

    /**
     * @brief Rollback a transaction
     *
     * @return True if the transaction was rolled back successfully, false otherwise
     */
    bool rollback_transaction();

    /**
     * @brief Check if the storage manager is initialized
     *
     * @return True if the storage manager is initialized, false otherwise
     */
    bool is_initialized() const;

private:
    // Storage path
    std::string _storage_path;

    // Initialization flag
    bool _initialized;

    // Mutex for thread safety
    mutable std::mutex _mutex;

    // Transaction flag
    bool _in_transaction;

    // Transaction data
    std::map<std::string, std::vector<uint8_t>> _transaction_data;
    std::set<std::string> _transaction_deleted;

    // Helper methods
    void secure_log(const std::string& message);
    std::string get_namespace_path(const std::string& namespace_id);
    std::string get_file_path(const std::string& namespace_id, const std::string& key);
    bool save_to_file(const std::string& file_path, const std::vector<uint8_t>& data);
    bool load_from_file(const std::string& file_path, std::vector<uint8_t>& data);
    std::vector<uint8_t> encrypt_data(const std::vector<uint8_t>& data);
    std::vector<uint8_t> decrypt_data(const std::vector<uint8_t>& encrypted_data);
    bool store_in_transaction(const std::string& file_path, const std::vector<uint8_t>& data);
};
