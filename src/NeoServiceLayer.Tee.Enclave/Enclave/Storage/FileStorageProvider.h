#ifndef FILE_STORAGE_PROVIDER_H
#define FILE_STORAGE_PROVIDER_H

#include "IPersistentStorageProvider.h"
#include <mutex>
#include <map>
#include <string>
#include <vector>
#include <cstdint>

/**
 * @brief File-based implementation of IPersistentStorageProvider
 *
 * This class provides a file-based implementation of the IPersistentStorageProvider interface.
 * It stores data in files on the host file system, with each key corresponding to a separate file.
 * The data is encrypted before being stored, and a hash is computed to ensure data integrity.
 */
class FileStorageProvider : public IPersistentStorageProvider {
public:
    /**
     * @brief Constructor
     */
    FileStorageProvider();

    /**
     * @brief Destructor
     */
    ~FileStorageProvider() override;

    /**
     * @brief Initialize the storage provider
     * @param storage_path The path to the storage directory
     * @return True if initialization was successful, false otherwise
     */
    bool initialize(const std::string& storage_path) override;

    /**
     * @brief Check if the storage provider is initialized
     * @return True if the storage provider is initialized, false otherwise
     */
    bool is_initialized() const override;

    /**
     * @brief Store data
     * @param key The key to store the data under
     * @param data The data to store
     * @return True if the operation was successful, false otherwise
     */
    bool store(const std::string& key, const std::vector<uint8_t>& data) override;

    /**
     * @brief Retrieve data
     * @param key The key to retrieve the data for
     * @return The retrieved data, or an empty vector if the key does not exist
     */
    std::vector<uint8_t> retrieve(const std::string& key) override;

    /**
     * @brief Delete data
     * @param key The key to delete
     * @return True if the operation was successful, false otherwise
     */
    bool remove(const std::string& key) override;

    /**
     * @brief Check if a key exists
     * @param key The key to check
     * @return True if the key exists, false otherwise
     */
    bool exists(const std::string& key) override;

    /**
     * @brief List all keys
     * @return A list of all keys
     */
    std::vector<std::string> list_keys() override;

    /**
     * @brief Begin a transaction
     * @return A transaction ID, or 0 if the transaction could not be started
     */
    uint64_t begin_transaction() override;

    /**
     * @brief Commit a transaction
     * @param transaction_id The transaction ID
     * @return True if the transaction was committed successfully, false otherwise
     */
    bool commit_transaction(uint64_t transaction_id) override;

    /**
     * @brief Rollback a transaction
     * @param transaction_id The transaction ID
     * @return True if the transaction was rolled back successfully, false otherwise
     */
    bool rollback_transaction(uint64_t transaction_id) override;

    /**
     * @brief Store data within a transaction
     * @param transaction_id The transaction ID
     * @param key The key to store the data under
     * @param data The data to store
     * @return True if the operation was successful, false otherwise
     */
    bool store_in_transaction(uint64_t transaction_id, const std::string& key, const std::vector<uint8_t>& data) override;

    /**
     * @brief Remove data within a transaction
     * @param transaction_id The transaction ID
     * @param key The key to remove
     * @return True if the operation was successful, false otherwise
     */
    bool remove_in_transaction(uint64_t transaction_id, const std::string& key) override;

private:
    std::string _storage_path;
    mutable std::mutex _mutex;
    bool _initialized;
    uint64_t _next_transaction_id;

    // Transaction data
    struct Transaction {
        std::map<std::string, std::vector<uint8_t>> changes;
        std::vector<std::string> deletions;
    };

    std::map<uint64_t, Transaction> _transactions;

    // Helper methods
    std::string get_file_path(const std::string& key);
    std::string get_metadata_path(const std::string& key);
    bool save_to_file(const std::string& file_path, const std::vector<uint8_t>& data);
    std::vector<uint8_t> load_from_file(const std::string& file_path);
    bool delete_file(const std::string& file_path);
    bool write_metadata(const std::string& file_path, const std::vector<uint8_t>& data);
    std::string compute_hash(const std::vector<uint8_t>& data);
    std::vector<uint8_t> encrypt_data(const std::vector<uint8_t>& data);
    std::vector<uint8_t> decrypt_data(const std::vector<uint8_t>& encrypted_data);
    std::vector<std::string> list_files_in_directory(const std::string& directory_path);
    std::vector<uint8_t> get_sealing_key();
};

#endif // FILE_STORAGE_PROVIDER_H
