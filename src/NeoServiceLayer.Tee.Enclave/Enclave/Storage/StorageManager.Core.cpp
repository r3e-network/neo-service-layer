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

StorageManager::StorageManager()
    : _initialized(false),
      _storage_path("/storage"),
      _in_transaction(false)
{
}

StorageManager::~StorageManager()
{
    // Clean up resources
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

        // Initialize Occlum
        if (!OcclumIntegration::Initialize())
        {
            secure_log("Failed to initialize Occlum");
            return false;
        }

        // Create the storage directory if it doesn't exist
        struct stat st;
        if (stat(_storage_path.c_str(), &st) != 0)
        {
            secure_log("Creating storage directory: " + _storage_path);
            if (mkdir(_storage_path.c_str(), 0755) != 0)
            {
                secure_log("Failed to create storage directory: " + _storage_path + " (errno: " + std::to_string(errno) + ")");
                return false;
            }
        }

        _initialized = true;
        secure_log("StorageManager initialized successfully");
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

bool StorageManager::set_storage_path(const std::string& path)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (_initialized)
    {
        secure_log("Cannot change storage path after initialization");
        return false;
    }

    try
    {
        secure_log("Setting storage path to: " + path);
        _storage_path = path;
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error setting storage path: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error setting storage path");
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
    // Use fprintf for logging to stderr
    fprintf(stderr, "[StorageManager] %s\n", message.c_str());
}
