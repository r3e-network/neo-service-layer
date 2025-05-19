#include "StorageManager.h"
#include "OcclumIntegration.h"
#include <algorithm>
#include <sstream>
#include <iomanip>
#include <cstdio>
#include <ctime>
#include <sys/stat.h>
#include <sys/types.h>
#include <fcntl.h>
#include <unistd.h>
#include <dirent.h>
#include <errno.h>
#include <nlohmann/json.hpp>

using json = nlohmann::json;

// File open flags
#define O_RDONLY    00000000
#define O_WRONLY    00000001
#define O_RDWR      00000002
#define O_CREAT     00000100
#define O_TRUNC     00001000
#define O_APPEND    00002000

// File seek whence values
#define SEEK_SET    0
#define SEEK_CUR    1
#define SEEK_END    2

// File permissions
#define S_IRUSR     00400
#define S_IWUSR     00200
#define S_IRGRP     00040
#define S_IWGRP     00020
#define S_IROTH     00004
#define S_IWOTH     00002

StorageManager::StorageManager()
    : _storage_path("/occlum_instance/data"),
      _initialized(false),
      _next_transaction_id(1)
{
}

StorageManager::~StorageManager()
{
    // Clean up any resources
}

bool StorageManager::initialize()
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (_initialized)
    {
        secure_log("StorageManager already initialized");
        return true;
    }

    try
    {
        secure_log("Initializing StorageManager...");

        // Create the storage directory if it doesn't exist
        std::string storage_dir = _storage_path;
        if (mkdir(storage_dir.c_str(), S_IRWXU | S_IRWXG | S_IROTH | S_IXOTH) != 0 && errno != EEXIST)
        {
            secure_log("Failed to create storage directory: " + storage_dir + ", error: " + std::to_string(errno));
            return false;
        }

        // Create the metadata directory if it doesn't exist
        std::string metadata_dir = storage_dir + "/.metadata";
        if (mkdir(metadata_dir.c_str(), S_IRWXU | S_IRWXG | S_IROTH | S_IXOTH) != 0 && errno != EEXIST)
        {
            secure_log("Failed to create metadata directory: " + metadata_dir + ", error: " + std::to_string(errno));
            return false;
        }

        _initialized = true;
        secure_log("StorageManager initialized with path: " + _storage_path);
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error initializing StorageManager: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error initializing StorageManager");
        return false;
    }
}

bool StorageManager::set_storage_path(const std::string& storage_path)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (_initialized)
    {
        secure_log("Cannot change storage path after initialization");
        return false;
    }

    _storage_path = storage_path;
    secure_log("Storage path set to: " + _storage_path);
    return true;
}

bool StorageManager::store(const std::string& key, const std::vector<uint8_t>& data)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    if (key.empty())
    {
        secure_log("Invalid key: empty");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        secure_log("Storing data for key: " + key);

        // Update cache
        _cache[key] = data;

        // If we're in a transaction, don't write to disk yet
        auto transaction_it = _transactions.find(_next_transaction_id - 1);
        if (transaction_it != _transactions.end())
        {
            transaction_it->second.changes[key] = data;
            return true;
        }

        // Save to file
        std::string file_path = get_file_path(key);
        bool result = save_to_file(file_path, data);

        if (result)
        {
            secure_log("Data stored successfully for key: " + key);
        }
        else
        {
            secure_log("Failed to store data for key: " + key);
        }

        return result;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error storing data: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error storing data");
        return false;
    }
}

bool StorageManager::store_string(const std::string& key, const std::string& value)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    if (key.empty())
    {
        secure_log("Invalid key: empty");
        return false;
    }

    try
    {
        secure_log("Storing string data for key: " + key);
        std::vector<uint8_t> data(value.begin(), value.end());
        return store(key, data);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error storing string data: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error storing string data");
        return false;
    }
}

bool StorageManager::store_data(const std::string& namespace_name, const std::string& key, const std::vector<uint8_t>& data)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    if (namespace_name.empty() || key.empty())
    {
        secure_log("Invalid namespace or key: empty");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        secure_log("Storing data for namespace: " + namespace_name + ", key: " + key);

        // Create the namespace directory if it doesn't exist
        std::string namespace_dir = _storage_path + "/" + namespace_name;
        if (mkdir(namespace_dir.c_str(), S_IRWXU | S_IRWXG | S_IROTH | S_IXOTH) != 0 && errno != EEXIST)
        {
            secure_log("Failed to create namespace directory: " + namespace_dir + ", error: " + std::to_string(errno));
            return false;
        }

        // Save to file
        std::string file_path = get_file_path(namespace_name, key);
        bool result = save_to_file(file_path, data);

        if (result)
        {
            secure_log("Data stored successfully for namespace: " + namespace_name + ", key: " + key);
        }
        else
        {
            secure_log("Failed to store data for namespace: " + namespace_name + ", key: " + key);
        }

        return result;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error storing data: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error storing data");
        return false;
    }
}

bool StorageManager::store_data(const std::string& namespace_name, const std::string& key, const std::string& value)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    if (namespace_name.empty() || key.empty())
    {
        secure_log("Invalid namespace or key: empty");
        return false;
    }

    try
    {
        secure_log("Storing string data for namespace: " + namespace_name + ", key: " + key);
        std::vector<uint8_t> data(value.begin(), value.end());
        return store_data(namespace_name, key, data);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error storing string data: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error storing string data");
        return false;
    }
}

std::vector<uint8_t> StorageManager::retrieve(const std::string& key)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return {};
    }

    if (key.empty())
    {
        secure_log("Invalid key: empty");
        return {};
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        secure_log("Retrieving data for key: " + key);

        // Check if we're in a transaction and the key has been modified
        auto transaction_it = _transactions.find(_next_transaction_id - 1);
        if (transaction_it != _transactions.end())
        {
            auto change_it = transaction_it->second.changes.find(key);
            if (change_it != transaction_it->second.changes.end())
            {
                secure_log("Retrieved data from transaction for key: " + key);
                return change_it->second;
            }

            // Check if the key has been deleted in this transaction
            auto deletion_it = std::find(transaction_it->second.deletions.begin(),
                                        transaction_it->second.deletions.end(), key);
            if (deletion_it != transaction_it->second.deletions.end())
            {
                secure_log("Key deleted in transaction: " + key);
                return {};
            }
        }

        // Check cache first
        auto it = _cache.find(key);
        if (it != _cache.end())
        {
            secure_log("Retrieved data from cache for key: " + key);
            return it->second;
        }

        // Load from file
        std::string file_path = get_file_path(key);
        std::vector<uint8_t> data = load_from_file(file_path);

        // Update cache
        if (!data.empty())
        {
            _cache[key] = data;
            secure_log("Retrieved data from file for key: " + key);
        }
        else
        {
            secure_log("No data found for key: " + key);
        }

        return data;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error retrieving data: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error retrieving data");
        return {};
    }
}

std::string StorageManager::retrieve_string(const std::string& key)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return "";
    }

    if (key.empty())
    {
        secure_log("Invalid key: empty");
        return "";
    }

    try
    {
        secure_log("Retrieving string data for key: " + key);
        std::vector<uint8_t> data = retrieve(key);
        if (data.empty())
        {
            return "";
        }

        return std::string(data.begin(), data.end());
    }
    catch (const std::exception& ex)
    {
        secure_log("Error retrieving string data: " + std::string(ex.what()));
        return "";
    }
    catch (...)
    {
        secure_log("Unknown error retrieving string data");
        return "";
    }
}

bool StorageManager::retrieve_data(const std::string& namespace_name, const std::string& key, std::vector<uint8_t>& data)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    if (namespace_name.empty() || key.empty())
    {
        secure_log("Invalid namespace or key: empty");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        secure_log("Retrieving data for namespace: " + namespace_name + ", key: " + key);

        // Load from file
        std::string file_path = get_file_path(namespace_name, key);
        data = load_from_file(file_path);

        if (!data.empty())
        {
            secure_log("Retrieved data from file for namespace: " + namespace_name + ", key: " + key);
            return true;
        }
        else
        {
            secure_log("No data found for namespace: " + namespace_name + ", key: " + key);
            return false;
        }
    }
    catch (const std::exception& ex)
    {
        secure_log("Error retrieving data: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error retrieving data");
        return false;
    }
}

bool StorageManager::retrieve_data(const std::string& namespace_name, const std::string& key, std::string& value)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    if (namespace_name.empty() || key.empty())
    {
        secure_log("Invalid namespace or key: empty");
        return false;
    }

    try
    {
        secure_log("Retrieving string data for namespace: " + namespace_name + ", key: " + key);
        std::vector<uint8_t> data;
        bool result = retrieve_data(namespace_name, key, data);
        if (!result || data.empty())
        {
            return false;
        }

        value = std::string(data.begin(), data.end());
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error retrieving string data: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error retrieving string data");
        return false;
    }
}

bool StorageManager::remove(const std::string& key)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    if (key.empty())
    {
        secure_log("Invalid key: empty");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        secure_log("Removing data for key: " + key);

        // Remove from cache
        _cache.erase(key);

        // If we're in a transaction, don't delete from disk yet
        auto transaction_it = _transactions.find(_next_transaction_id - 1);
        if (transaction_it != _transactions.end())
        {
            // Remove from changes if present
            transaction_it->second.changes.erase(key);

            // Add to deletions
            transaction_it->second.deletions.push_back(key);
            secure_log("Key added to transaction deletions: " + key);
            return true;
        }

        // Delete file
        std::string file_path = get_file_path(key);
        bool result = delete_file(file_path);

        if (result)
        {
            secure_log("Data removed successfully for key: " + key);
        }
        else
        {
            secure_log("Failed to remove data for key: " + key);
        }

        return result;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error removing data: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error removing data");
        return false;
    }
}

bool StorageManager::remove_data(const std::string& namespace_name, const std::string& key)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    if (namespace_name.empty() || key.empty())
    {
        secure_log("Invalid namespace or key: empty");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        secure_log("Removing data for namespace: " + namespace_name + ", key: " + key);

        // Delete file
        std::string file_path = get_file_path(namespace_name, key);
        bool result = delete_file(file_path);

        if (result)
        {
            secure_log("Data removed successfully for namespace: " + namespace_name + ", key: " + key);
        }
        else
        {
            secure_log("Failed to remove data for namespace: " + namespace_name + ", key: " + key);
        }

        return result;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error removing data: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error removing data");
        return false;
    }
}

bool StorageManager::exists(const std::string& key)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    if (key.empty())
    {
        secure_log("Invalid key: empty");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        secure_log("Checking if key exists: " + key);

        // Check if we're in a transaction and the key has been deleted
        auto transaction_it = _transactions.find(_next_transaction_id - 1);
        if (transaction_it != _transactions.end())
        {
            // Check if the key has been deleted in this transaction
            auto deletion_it = std::find(transaction_it->second.deletions.begin(),
                                        transaction_it->second.deletions.end(), key);
            if (deletion_it != transaction_it->second.deletions.end())
            {
                secure_log("Key deleted in transaction: " + key);
                return false;
            }

            // Check if the key has been added in this transaction
            auto change_it = transaction_it->second.changes.find(key);
            if (change_it != transaction_it->second.changes.end())
            {
                secure_log("Key exists in transaction: " + key);
                return true;
            }
        }

        // Check cache first
        auto it = _cache.find(key);
        if (it != _cache.end())
        {
            secure_log("Key exists in cache: " + key);
            return true;
        }

        // Check file
        std::string file_path = get_file_path(key);
        std::vector<uint8_t> data = load_from_file(file_path);
        bool exists = !data.empty();

        if (exists)
        {
            secure_log("Key exists in file: " + key);
        }
        else
        {
            secure_log("Key does not exist: " + key);
        }

        return exists;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error checking if key exists: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error checking if key exists");
        return false;
    }
}

bool StorageManager::exists_data(const std::string& namespace_name, const std::string& key)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    if (namespace_name.empty() || key.empty())
    {
        secure_log("Invalid namespace or key: empty");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        secure_log("Checking if key exists in namespace: " + namespace_name + ", key: " + key);

        // Check file
        std::string file_path = get_file_path(namespace_name, key);
        std::vector<uint8_t> data = load_from_file(file_path);
        bool exists = !data.empty();

        if (exists)
        {
            secure_log("Key exists in namespace: " + namespace_name + ", key: " + key);
        }
        else
        {
            secure_log("Key does not exist in namespace: " + namespace_name + ", key: " + key);
        }

        return exists;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error checking if key exists in namespace: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error checking if key exists in namespace");
        return false;
    }
}

std::vector<std::string> StorageManager::list_keys()
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return {};
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        secure_log("Listing keys");
        std::vector<std::string> keys;

        // Add keys from cache
        for (const auto& pair : _cache)
        {
            keys.push_back(pair.first);
        }

        // If we're in a transaction, add keys from the transaction
        auto transaction_it = _transactions.find(_next_transaction_id - 1);
        if (transaction_it != _transactions.end())
        {
            // Add keys from changes
            for (const auto& pair : transaction_it->second.changes)
            {
                keys.push_back(pair.first);
            }

            // Remove keys that have been deleted
            for (const auto& key : transaction_it->second.deletions)
            {
                auto it = std::find(keys.begin(), keys.end(), key);
                if (it != keys.end())
                {
                    keys.erase(it);
                }
            }
        }

        // List files in the storage directory
        DIR* dir = opendir(_storage_path.c_str());
        if (dir != nullptr)
        {
            struct dirent* entry;
            while ((entry = readdir(dir)) != nullptr)
            {
                std::string filename = entry->d_name;

                // Skip . and .. directories
                if (filename == "." || filename == ".." || filename == ".metadata")
                {
                    continue;
                }

                // Skip directories
                if (entry->d_type == DT_DIR)
                {
                    continue;
                }

                // Add the key
                keys.push_back(filename);
            }

            closedir(dir);
        }

        // Remove duplicates
        std::sort(keys.begin(), keys.end());
        keys.erase(std::unique(keys.begin(), keys.end()), keys.end());

        secure_log("Listed " + std::to_string(keys.size()) + " keys");
        return keys;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error listing keys: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error listing keys");
        return {};
    }
}

std::vector<std::string> StorageManager::list_keys(const std::string& namespace_name)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return {};
    }

    if (namespace_name.empty())
    {
        secure_log("Invalid namespace: empty");
        return {};
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        secure_log("Listing keys for namespace: " + namespace_name);
        std::vector<std::string> keys;

        // List files in the namespace directory
        std::string namespace_dir = _storage_path + "/" + namespace_name;
        DIR* dir = opendir(namespace_dir.c_str());
        if (dir != nullptr)
        {
            struct dirent* entry;
            while ((entry = readdir(dir)) != nullptr)
            {
                std::string filename = entry->d_name;

                // Skip . and .. directories
                if (filename == "." || filename == "..")
                {
                    continue;
                }

                // Skip directories
                if (entry->d_type == DT_DIR)
                {
                    continue;
                }

                // Add the key
                keys.push_back(filename);
            }

            closedir(dir);
        }

        secure_log("Listed " + std::to_string(keys.size()) + " keys for namespace: " + namespace_name);
        return keys;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error listing keys for namespace: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error listing keys for namespace");
        return {};
    }
}

uint64_t StorageManager::begin_transaction()
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return 0;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        secure_log("Beginning transaction");

        // Check if there's already an active transaction
        for (const auto& pair : _transactions)
        {
            if (!pair.second.changes.empty() || !pair.second.deletions.empty())
            {
                // There's an active transaction
                secure_log("Cannot begin transaction: another transaction is active");
                return 0;
            }
        }

        uint64_t transaction_id = _next_transaction_id++;
        _transactions[transaction_id] = Transaction();

        secure_log("Transaction begun with ID: " + std::to_string(transaction_id));
        return transaction_id;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error beginning transaction: " + std::string(ex.what()));
        return 0;
    }
    catch (...)
    {
        secure_log("Unknown error beginning transaction");
        return 0;
    }
}

bool StorageManager::commit_transaction(uint64_t transaction_id)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        secure_log("Committing transaction: " + std::to_string(transaction_id));

        auto it = _transactions.find(transaction_id);
        if (it == _transactions.end())
        {
            secure_log("Transaction not found: " + std::to_string(transaction_id));
            return false;
        }

        // Apply changes
        for (const auto& pair : it->second.changes)
        {
            // Update cache
            _cache[pair.first] = pair.second;

            // Save to file
            std::string file_path = get_file_path(pair.first);
            if (!save_to_file(file_path, pair.second))
            {
                // Failed to save to file, rollback
                secure_log("Failed to save file: " + file_path + ", rolling back transaction");
                _transactions.erase(it);
                return false;
            }
        }

        // Apply deletions
        for (const auto& key : it->second.deletions)
        {
            // Remove from cache
            _cache.erase(key);

            // Delete file
            std::string file_path = get_file_path(key);
            if (!delete_file(file_path))
            {
                // Failed to delete file, but continue anyway
                secure_log("Failed to delete file: " + file_path);
            }
        }

        // Remove transaction
        _transactions.erase(it);

        secure_log("Transaction committed successfully: " + std::to_string(transaction_id));
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error committing transaction: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error committing transaction");
        return false;
    }
}

bool StorageManager::rollback_transaction(uint64_t transaction_id)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        secure_log("Rolling back transaction: " + std::to_string(transaction_id));

        auto it = _transactions.find(transaction_id);
        if (it == _transactions.end())
        {
            secure_log("Transaction not found: " + std::to_string(transaction_id));
            return false;
        }

        // Remove transaction
        _transactions.erase(it);

        secure_log("Transaction rolled back successfully: " + std::to_string(transaction_id));
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error rolling back transaction: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error rolling back transaction");
        return false;
    }
}

bool StorageManager::store_in_transaction(uint64_t transaction_id, const std::string& key, const std::vector<uint8_t>& data)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    if (key.empty())
    {
        secure_log("Invalid key: empty");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        secure_log("Storing data in transaction: " + std::to_string(transaction_id) + ", key: " + key);

        auto it = _transactions.find(transaction_id);
        if (it == _transactions.end())
        {
            secure_log("Transaction not found: " + std::to_string(transaction_id));
            return false;
        }

        // Store in transaction
        it->second.changes[key] = data;

        // Remove from deletions if present
        auto deletion_it = std::find(it->second.deletions.begin(), it->second.deletions.end(), key);
        if (deletion_it != it->second.deletions.end())
        {
            it->second.deletions.erase(deletion_it);
        }

        secure_log("Data stored in transaction successfully: " + std::to_string(transaction_id) + ", key: " + key);
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error storing data in transaction: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error storing data in transaction");
        return false;
    }
}

bool StorageManager::store_string_in_transaction(uint64_t transaction_id, const std::string& key, const std::string& value)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    if (key.empty())
    {
        secure_log("Invalid key: empty");
        return false;
    }

    try
    {
        secure_log("Storing string data in transaction: " + std::to_string(transaction_id) + ", key: " + key);
        std::vector<uint8_t> data(value.begin(), value.end());
        return store_in_transaction(transaction_id, key, data);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error storing string data in transaction: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error storing string data in transaction");
        return false;
    }
}

bool StorageManager::remove_in_transaction(uint64_t transaction_id, const std::string& key)
{
    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    if (key.empty())
    {
        secure_log("Invalid key: empty");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        secure_log("Removing data in transaction: " + std::to_string(transaction_id) + ", key: " + key);

        auto it = _transactions.find(transaction_id);
        if (it == _transactions.end())
        {
            secure_log("Transaction not found: " + std::to_string(transaction_id));
            return false;
        }

        // Remove from changes if present
        it->second.changes.erase(key);

        // Add to deletions if not already present
        auto deletion_it = std::find(it->second.deletions.begin(), it->second.deletions.end(), key);
        if (deletion_it == it->second.deletions.end())
        {
            it->second.deletions.push_back(key);
        }

        secure_log("Data removed in transaction successfully: " + std::to_string(transaction_id) + ", key: " + key);
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error removing data in transaction: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error removing data in transaction");
        return false;
    }
}

bool StorageManager::is_initialized() const
{
    std::lock_guard<std::mutex> lock(_mutex);
    return _initialized;
}

void StorageManager::secure_log(const std::string& message)
{
    // Use the secure Logger instead of fprintf
    NeoServiceLayer::Enclave::Logger::getInstance().info("StorageManager", message);
}

std::string StorageManager::get_file_path(const std::string& key)
{
    // Sanitize key to make it a valid filename
    std::string sanitized_key = key;
    std::replace(sanitized_key.begin(), sanitized_key.end(), '/', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '\\', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), ':', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '*', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '?', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '"', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '<', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '>', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '|', '_');

    return _storage_path + "/" + sanitized_key;
}

std::string StorageManager::get_file_path(const std::string& namespace_name, const std::string& key)
{
    // Sanitize namespace and key to make valid filenames
    std::string sanitized_namespace = namespace_name;
    std::replace(sanitized_namespace.begin(), sanitized_namespace.end(), '/', '_');
    std::replace(sanitized_namespace.begin(), sanitized_namespace.end(), '\\', '_');
    std::replace(sanitized_namespace.begin(), sanitized_namespace.end(), ':', '_');
    std::replace(sanitized_namespace.begin(), sanitized_namespace.end(), '*', '_');
    std::replace(sanitized_namespace.begin(), sanitized_namespace.end(), '?', '_');
    std::replace(sanitized_namespace.begin(), sanitized_namespace.end(), '"', '_');
    std::replace(sanitized_namespace.begin(), sanitized_namespace.end(), '<', '_');
    std::replace(sanitized_namespace.begin(), sanitized_namespace.end(), '>', '_');
    std::replace(sanitized_namespace.begin(), sanitized_namespace.end(), '|', '_');

    std::string sanitized_key = key;
    std::replace(sanitized_key.begin(), sanitized_key.end(), '/', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '\\', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), ':', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '*', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '?', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '"', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '<', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '>', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '|', '_');

    return _storage_path + "/" + sanitized_namespace + "/" + sanitized_key;
}

bool StorageManager::save_to_file(const std::string& file_path, const std::vector<uint8_t>& data)
{
    try
    {
        secure_log("Saving data to file: " + file_path);

        // Open the file for writing
        int fd = open(file_path.c_str(), O_CREAT | O_TRUNC | O_WRONLY, S_IRUSR | S_IWUSR);
        if (fd < 0)
        {
            secure_log("Failed to open file for writing: " + file_path + ", error: " + std::to_string(errno));
            return false;
        }

        // Write the data to the file
        size_t bytes_written = write(fd, data.data(), data.size());
        close(fd);

        if (bytes_written != data.size())
        {
            secure_log("Failed to write data to file: " + file_path + ", bytes written: " + std::to_string(bytes_written) + ", expected: " + std::to_string(data.size()));
            return false;
        }

        // Write the metadata
        bool result = write_metadata(file_path, data);

        if (result)
        {
            secure_log("Data saved to file successfully: " + file_path);
        }
        else
        {
            secure_log("Failed to write metadata for file: " + file_path);
        }

        return result;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error saving data to file: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error saving data to file");
        return false;
    }
}

std::vector<uint8_t> StorageManager::load_from_file(const std::string& file_path)
{
    try
    {
        secure_log("Loading data from file: " + file_path);

        // Open the file for reading
        int fd = open(file_path.c_str(), O_RDONLY);
        if (fd < 0)
        {
            if (errno != ENOENT)
            {
                secure_log("Failed to open file for reading: " + file_path + ", error: " + std::to_string(errno));
            }
            return {};
        }

        // Get the file size
        struct stat st;
        if (fstat(fd, &st) < 0)
        {
            secure_log("Failed to get file size: " + file_path + ", error: " + std::to_string(errno));
            close(fd);
            return {};
        }

        // Read the data from the file
        std::vector<uint8_t> data(st.st_size);
        size_t bytes_read = read(fd, data.data(), st.st_size);
        close(fd);

        if (bytes_read != static_cast<size_t>(st.st_size))
        {
            secure_log("Failed to read data from file: " + file_path + ", bytes read: " + std::to_string(bytes_read) + ", expected: " + std::to_string(st.st_size));
            return {};
        }

        secure_log("Data loaded from file successfully: " + file_path + ", size: " + std::to_string(data.size()));
        return data;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error loading data from file: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error loading data from file");
        return {};
    }
}

bool StorageManager::delete_file(const std::string& file_path)
{
    try
    {
        secure_log("Deleting file: " + file_path);

        // Delete the file
        if (unlink(file_path.c_str()) < 0 && errno != ENOENT)
        {
            secure_log("Failed to delete file: " + file_path + ", error: " + std::to_string(errno));
            return false;
        }

        // Delete the metadata file
        std::string metadata_path = file_path + ".metadata";
        if (unlink(metadata_path.c_str()) < 0 && errno != ENOENT)
        {
            secure_log("Failed to delete metadata file: " + metadata_path + ", error: " + std::to_string(errno));
            // Continue anyway
        }

        secure_log("File deleted successfully: " + file_path);
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error deleting file: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error deleting file");
        return false;
    }
}

bool StorageManager::write_metadata(const std::string& file_path, const std::vector<uint8_t>& data)
{
    try
    {
        secure_log("Writing metadata for file: " + file_path);

        // Create the metadata
        json metadata = {
            {"path", file_path},
            {"size", data.size()},
            {"timestamp", std::time(nullptr)},
            {"hash", ""}  // TODO: Compute hash
        };

        // Convert the metadata to a string
        std::string metadata_str = metadata.dump();

        // Open the metadata file for writing
        std::string metadata_path = file_path + ".metadata";
        int fd = open(metadata_path.c_str(), O_CREAT | O_TRUNC | O_WRONLY, S_IRUSR | S_IWUSR);
        if (fd < 0)
        {
            secure_log("Failed to open metadata file for writing: " + metadata_path + ", error: " + std::to_string(errno));
            return false;
        }

        // Write the metadata to the file
        size_t bytes_written = write(fd, metadata_str.c_str(), metadata_str.size());
        close(fd);

        if (bytes_written != metadata_str.size())
        {
            secure_log("Failed to write metadata to file: " + metadata_path + ", bytes written: " + std::to_string(bytes_written) + ", expected: " + std::to_string(metadata_str.size()));
            return false;
        }

        secure_log("Metadata written successfully: " + metadata_path);
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error writing metadata: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error writing metadata");
        return false;
    }
}
