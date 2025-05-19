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

std::string StorageManager::retrieve(const std::string& namespace_id, const std::string& key)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return "";
    }

    try
    {
        secure_log("Retrieving value for key: " + key + " from namespace: " + namespace_id);

        // Retrieve the data
        std::vector<uint8_t> data;
        if (!retrieve_data(namespace_id, key, data))
        {
            secure_log("Failed to retrieve data");
            return "";
        }

        // Convert the data to a string
        std::string value(data.begin(), data.end());
        return value;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error retrieving value: " + std::string(ex.what()));
        return "";
    }
    catch (...)
    {
        secure_log("Unknown error retrieving value");
        return "";
    }
}

bool StorageManager::retrieve_data(const std::string& namespace_id, const std::string& key, std::vector<uint8_t>& data)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("StorageManager not initialized");
        return false;
    }

    try
    {
        secure_log("Retrieving data for key: " + key + " from namespace: " + namespace_id);

        // Get the file path
        std::string file_path = get_file_path(namespace_id, key);

        // If we're in a transaction, check if the file is in the transaction
        if (_in_transaction)
        {
            auto it = _transaction_data.find(file_path);
            if (it != _transaction_data.end())
            {
                secure_log("Found data in transaction");
                data = it->second;
                return true;
            }
        }

        // Check if the file exists
        struct stat st;
        if (stat(file_path.c_str(), &st) != 0)
        {
            secure_log("File not found: " + file_path);
            return false;
        }

        // Load the data from the file
        std::vector<uint8_t> encrypted_data;
        if (!load_from_file(file_path, encrypted_data))
        {
            secure_log("Failed to load data from file: " + file_path);
            return false;
        }

        // Decrypt the data
        data = decrypt_data(encrypted_data);
        if (data.empty())
        {
            secure_log("Failed to decrypt data");
            return false;
        }

        secure_log("Data retrieved successfully (" + std::to_string(data.size()) + " bytes)");
        return true;
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

std::vector<uint8_t> StorageManager::decrypt_data(const std::vector<uint8_t>& encrypted_data)
{
    try
    {
        secure_log("Decrypting data (" + std::to_string(encrypted_data.size()) + " bytes)");

        // Use Occlum to unseal the data
        std::vector<uint8_t> decrypted_data = OcclumIntegration::UnsealData(encrypted_data);
        if (decrypted_data.empty())
        {
            secure_log("Failed to unseal data");
            return {};
        }

        secure_log("Data decrypted successfully (" + std::to_string(decrypted_data.size()) + " bytes)");
        return decrypted_data;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error decrypting data: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error decrypting data");
        return {};
    }
}

bool StorageManager::load_from_file(const std::string& file_path, std::vector<uint8_t>& data)
{
    try
    {
        secure_log("Loading data from file: " + file_path);

        // Get the file size
        struct stat st;
        if (stat(file_path.c_str(), &st) != 0)
        {
            secure_log("Failed to get file size: " + file_path + " (errno: " + std::to_string(errno) + ")");
            return false;
        }

        // Open the file
        int fd = open(file_path.c_str(), O_RDONLY);
        if (fd < 0)
        {
            secure_log("Failed to open file: " + file_path + " (errno: " + std::to_string(errno) + ")");
            return false;
        }

        // Allocate memory for the data
        data.resize(st.st_size);

        // Read the data
        ssize_t bytes_read = read(fd, data.data(), data.size());
        if (bytes_read != static_cast<ssize_t>(data.size()))
        {
            secure_log("Failed to read data from file: " + file_path + " (errno: " + std::to_string(errno) + ")");
            close(fd);
            return false;
        }

        // Close the file
        close(fd);

        secure_log("Data loaded from file successfully (" + std::to_string(data.size()) + " bytes)");
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error loading data from file: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error loading data from file");
        return false;
    }
}
