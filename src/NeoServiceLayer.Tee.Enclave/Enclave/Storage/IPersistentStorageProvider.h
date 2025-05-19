#ifndef IPERSISTENT_STORAGE_PROVIDER_H
#define IPERSISTENT_STORAGE_PROVIDER_H

#include <string>
#include <vector>
#include <cstdint>

/**
 * @brief Interface for persistent storage providers
 * 
 * This interface defines the operations that a persistent storage provider must implement.
 * Implementations of this interface are responsible for storing and retrieving data in a
 * persistent manner, ensuring that data survives enclave restarts.
 */
class IPersistentStorageProvider {
public:
    /**
     * @brief Virtual destructor
     */
    virtual ~IPersistentStorageProvider() = default;

    /**
     * @brief Initialize the storage provider
     * @param storage_path The path to the storage directory
     * @return True if initialization was successful, false otherwise
     */
    virtual bool initialize(const std::string& storage_path) = 0;

    /**
     * @brief Check if the storage provider is initialized
     * @return True if the storage provider is initialized, false otherwise
     */
    virtual bool is_initialized() const = 0;

    /**
     * @brief Store data
     * @param key The key to store the data under
     * @param data The data to store
     * @return True if the operation was successful, false otherwise
     */
    virtual bool store(const std::string& key, const std::vector<uint8_t>& data) = 0;

    /**
     * @brief Retrieve data
     * @param key The key to retrieve the data for
     * @return The retrieved data, or an empty vector if the key does not exist
     */
    virtual std::vector<uint8_t> retrieve(const std::string& key) = 0;

    /**
     * @brief Delete data
     * @param key The key to delete
     * @return True if the operation was successful, false otherwise
     */
    virtual bool remove(const std::string& key) = 0;

    /**
     * @brief Check if a key exists
     * @param key The key to check
     * @return True if the key exists, false otherwise
     */
    virtual bool exists(const std::string& key) = 0;

    /**
     * @brief List all keys
     * @return A list of all keys
     */
    virtual std::vector<std::string> list_keys() = 0;

    /**
     * @brief Begin a transaction
     * @return A transaction ID, or 0 if the transaction could not be started
     */
    virtual uint64_t begin_transaction() = 0;

    /**
     * @brief Commit a transaction
     * @param transaction_id The transaction ID
     * @return True if the transaction was committed successfully, false otherwise
     */
    virtual bool commit_transaction(uint64_t transaction_id) = 0;

    /**
     * @brief Rollback a transaction
     * @param transaction_id The transaction ID
     * @return True if the transaction was rolled back successfully, false otherwise
     */
    virtual bool rollback_transaction(uint64_t transaction_id) = 0;

    /**
     * @brief Store data within a transaction
     * @param transaction_id The transaction ID
     * @param key The key to store the data under
     * @param data The data to store
     * @return True if the operation was successful, false otherwise
     */
    virtual bool store_in_transaction(uint64_t transaction_id, const std::string& key, const std::vector<uint8_t>& data) = 0;

    /**
     * @brief Remove data within a transaction
     * @param transaction_id The transaction ID
     * @param key The key to remove
     * @return True if the operation was successful, false otherwise
     */
    virtual bool remove_in_transaction(uint64_t transaction_id, const std::string& key) = 0;
};

#endif // IPERSISTENT_STORAGE_PROVIDER_H
