#include "StorageManager.h"
#include "OcclumIntegration.h"
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

bool StorageManager::store(const std::string& namespace_id, const std::string& key, const std::string& value)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    try
    {
        secure_log("Storing value for key: " + key + " in namespace: " + namespace_id);

        // Convert the string to bytes
        std::vector<uint8_t> data(value.begin(), value.end());

        // Store the data
        return store_data(namespace_id, key, data);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error storing value: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error storing value");
        return false;
    }
}

bool StorageManager::store_data(const std::string& namespace_id, const std::string& key, const std::vector<uint8_t>& data)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    try
    {
        secure_log("Storing data for key: " + key + " in namespace: " + namespace_id + " (" + std::to_string(data.size()) + " bytes)");

        // Create the namespace directory if it doesn't exist
        std::string namespace_path = get_namespace_path(namespace_id);
        struct stat st;
        if (stat(namespace_path.c_str(), &st) != 0)
        {
            secure_log("Creating namespace directory: " + namespace_path);
            if (mkdir(namespace_path.c_str(), 0755) != 0)
            {
                secure_log("Failed to create namespace directory: " + namespace_path + " (errno: " + std::to_string(errno) + ")");
                return false;
            }
        }

        // Get the file path
        std::string file_path = get_file_path(namespace_id, key);

        // If we're in a transaction, store in the transaction
        if (_in_transaction)
        {
            return store_in_transaction(file_path, data);
        }

        // Encrypt the data
        std::vector<uint8_t> encrypted_data = encrypt_data(data);
        if (encrypted_data.empty())
        {
            secure_log("Failed to encrypt data");
            return false;
        }

        // Save the data to the file
        if (!save_to_file(file_path, encrypted_data))
        {
            secure_log("Failed to save data to file: " + file_path);
            return false;
        }

        secure_log("Data stored successfully");
        return true;
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

bool StorageManager::store_in_transaction(const std::string& file_path, const std::vector<uint8_t>& data)
{
    try
    {
        secure_log("Storing data in transaction for file: " + file_path);

        // Add to the transaction
        _transaction_data[file_path] = data;
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

std::vector<uint8_t> StorageManager::encrypt_data(const std::vector<uint8_t>& data)
{
    try
    {
        secure_log("Encrypting data (" + std::to_string(data.size()) + " bytes)");

        // Use Occlum to seal the data
        std::vector<uint8_t> encrypted_data = OcclumIntegration::SealData(data);
        if (encrypted_data.empty())
        {
            secure_log("Failed to seal data");
            return {};
        }

        secure_log("Data encrypted successfully (" + std::to_string(encrypted_data.size()) + " bytes)");
        return encrypted_data;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error encrypting data: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error encrypting data");
        return {};
    }
}

bool StorageManager::save_to_file(const std::string& file_path, const std::vector<uint8_t>& data)
{
    try
    {
        secure_log("Saving data to file: " + file_path + " (" + std::to_string(data.size()) + " bytes)");

        // Open the file
        int fd = open(file_path.c_str(), O_WRONLY | O_CREAT | O_TRUNC, 0644);
        if (fd < 0)
        {
            secure_log("Failed to open file: " + file_path + " (errno: " + std::to_string(errno) + ")");
            return false;
        }

        // Write the data
        ssize_t bytes_written = write(fd, data.data(), data.size());
        if (bytes_written != static_cast<ssize_t>(data.size()))
        {
            secure_log("Failed to write data to file: " + file_path + " (errno: " + std::to_string(errno) + ")");
            close(fd);
            return false;
        }

        // Close the file
        close(fd);

        secure_log("Data saved to file successfully");
        return true;
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
