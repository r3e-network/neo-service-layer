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

bool StorageManager::begin_transaction()
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    try
    {
        secure_log("Beginning transaction");

        if (_in_transaction)
        {
            secure_log("Transaction already in progress");
            return false;
        }

        // Clear transaction data
        _transaction_data.clear();
        _transaction_deleted.clear();

        _in_transaction = true;
        secure_log("Transaction begun successfully");
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error beginning transaction: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error beginning transaction");
        return false;
    }
}

bool StorageManager::commit_transaction()
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    try
    {
        secure_log("Committing transaction");

        if (!_in_transaction)
        {
            secure_log("No transaction in progress");
            return false;
        }

        // Process deleted files
        for (const auto& file_path : _transaction_deleted)
        {
            secure_log("Deleting file: " + file_path);
            
            // Check if the file exists
            struct stat st;
            if (stat(file_path.c_str(), &st) == 0)
            {
                // Remove the file
                if (unlink(file_path.c_str()) != 0)
                {
                    secure_log("Failed to remove file: " + file_path + " (errno: " + std::to_string(errno) + ")");
                    // Continue anyway
                }
            }
        }

        // Process updated files
        for (const auto& pair : _transaction_data)
        {
            secure_log("Saving file: " + pair.first);
            
            // Encrypt the data
            std::vector<uint8_t> encrypted_data = encrypt_data(pair.second);
            if (encrypted_data.empty())
            {
                secure_log("Failed to encrypt data for file: " + pair.first);
                continue;
            }

            // Save the data to the file
            if (!save_to_file(pair.first, encrypted_data))
            {
                secure_log("Failed to save data to file: " + pair.first);
                continue;
            }
        }

        // Clear transaction data
        _transaction_data.clear();
        _transaction_deleted.clear();

        _in_transaction = false;
        secure_log("Transaction committed successfully");
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

bool StorageManager::rollback_transaction()
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    try
    {
        secure_log("Rolling back transaction");

        if (!_in_transaction)
        {
            secure_log("No transaction in progress");
            return false;
        }

        // Clear transaction data
        _transaction_data.clear();
        _transaction_deleted.clear();

        _in_transaction = false;
        secure_log("Transaction rolled back successfully");
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
