#include "StorageManager.h"
#include <cstdio>
#include <cstring>
#include <sys/stat.h>
#include <sys/types.h>
#include <fcntl.h>
#include <unistd.h>
#include <dirent.h>
#include <fstream>
#include <sstream>
#include <nlohmann/json.hpp>

using json = nlohmann::json;

bool StorageManager::remove(const std::string& namespace_id, const std::string& key)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    try
    {
        secure_log("Removing key: " + key + " from namespace: " + namespace_id);

        // Get the file path
        std::string file_path = get_file_path(namespace_id, key);

        // If we're in a transaction, mark the file for deletion
        if (_in_transaction)
        {
            secure_log("Marking file for deletion in transaction: " + file_path);
            _transaction_deleted.insert(file_path);
            _transaction_data.erase(file_path);
            return true;
        }

        // Check if the file exists
        struct stat st;
        if (stat(file_path.c_str(), &st) != 0)
        {
            secure_log("File not found: " + file_path);
            return false;
        }

        // Remove the file
        if (unlink(file_path.c_str()) != 0)
        {
            secure_log("Failed to remove file: " + file_path + " (errno: " + std::to_string(errno) + ")");
            return false;
        }

        secure_log("Key removed successfully");
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error removing key: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error removing key");
        return false;
    }
}

bool StorageManager::exists(const std::string& namespace_id, const std::string& key)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    try
    {
        secure_log("Checking if key exists: " + key + " in namespace: " + namespace_id);

        // Get the file path
        std::string file_path = get_file_path(namespace_id, key);

        // If we're in a transaction, check if the file is in the transaction
        if (_in_transaction)
        {
            // Check if the file is marked for deletion
            if (_transaction_deleted.find(file_path) != _transaction_deleted.end())
            {
                secure_log("File is marked for deletion in transaction");
                return false;
            }

            // Check if the file is in the transaction
            if (_transaction_data.find(file_path) != _transaction_data.end())
            {
                secure_log("File is in transaction");
                return true;
            }
        }

        // Check if the file exists
        struct stat st;
        bool exists = (stat(file_path.c_str(), &st) == 0);

        secure_log("Key " + (exists ? "exists" : "does not exist"));
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

std::vector<std::string> StorageManager::list_keys(const std::string& namespace_id)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return {};
    }

    try
    {
        secure_log("Listing keys in namespace: " + namespace_id);

        // Get the namespace directory path
        std::string namespace_path = get_namespace_path(namespace_id);

        // Check if the namespace directory exists
        struct stat st;
        if (stat(namespace_path.c_str(), &st) != 0)
        {
            secure_log("Namespace directory not found: " + namespace_path);
            return {};
        }

        // Open the directory
        DIR* dir = opendir(namespace_path.c_str());
        if (!dir)
        {
            secure_log("Failed to open namespace directory: " + namespace_path + " (errno: " + std::to_string(errno) + ")");
            return {};
        }

        // Read the directory entries
        std::vector<std::string> keys;
        struct dirent* entry;
        while ((entry = readdir(dir)) != nullptr)
        {
            // Skip "." and ".." entries
            if (strcmp(entry->d_name, ".") == 0 || strcmp(entry->d_name, "..") == 0)
            {
                continue;
            }

            // Add the key to the list
            keys.push_back(entry->d_name);
        }

        // Close the directory
        closedir(dir);

        // If we're in a transaction, add keys from the transaction
        if (_in_transaction)
        {
            std::string namespace_path_prefix = namespace_path + "/";
            for (const auto& pair : _transaction_data)
            {
                // Check if the file is in this namespace
                if (pair.first.find(namespace_path_prefix) == 0)
                {
                    // Extract the key from the file path
                    std::string key = pair.first.substr(namespace_path_prefix.size());
                    
                    // Add the key to the list if it's not already there
                    if (std::find(keys.begin(), keys.end(), key) == keys.end())
                    {
                        keys.push_back(key);
                    }
                }
            }

            // Remove keys that are marked for deletion
            for (const auto& file_path : _transaction_deleted)
            {
                // Check if the file is in this namespace
                if (file_path.find(namespace_path_prefix) == 0)
                {
                    // Extract the key from the file path
                    std::string key = file_path.substr(namespace_path_prefix.size());
                    
                    // Remove the key from the list
                    auto it = std::find(keys.begin(), keys.end(), key);
                    if (it != keys.end())
                    {
                        keys.erase(it);
                    }
                }
            }
        }

        secure_log("Found " + std::to_string(keys.size()) + " keys");
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
