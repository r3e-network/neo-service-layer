#include "SecretManager.h"
#include "StorageManager.h"
#include "KeyManager.h"
#include "OcclumIntegration.h"
#include <nlohmann/json.hpp>

#include <stdexcept>
#include <vector>
#include <cstring>
#include <sstream>
#include <mutex>
#include <cstdio>

using json = nlohmann::json;

SecretManager::SecretManager(StorageManager* storage_manager, KeyManager* key_manager)
    : _storage_manager(storage_manager),
      _key_manager(key_manager),
      _encryption_key(32),
      _initialized(false)
{
    // Encryption key will be generated in initialize()
}

SecretManager::~SecretManager()
{
    // Clear sensitive data
    for (auto& user_entry : _user_secrets)
    {
        for (auto& secret_entry : user_entry.second)
        {
            // Overwrite with zeros
            std::fill(secret_entry.second.begin(), secret_entry.second.end(), 0);
        }
    }

    // Clear encryption key
    std::fill(_encryption_key.begin(), _encryption_key.end(), 0);
}

bool SecretManager::initialize()
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (_initialized)
    {
        secure_log("SecretManager already initialized");
        return true;
    }

    try
    {
        secure_log("Initializing SecretManager...");

        // Check if storage manager and key manager are initialized
        if (!_storage_manager)
        {
            secure_log("Storage manager not provided");
            return false;
        }

        if (!_key_manager)
        {
            secure_log("Key manager not provided");
            return false;
        }

        // Generate encryption key
        generate_encryption_key();

        // Try to load secrets from persistent storage
        load_from_persistent_storage();

        _initialized = true;
        secure_log("SecretManager initialized successfully");
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error initializing SecretManager: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error initializing SecretManager");
        return false;
    }
}

bool SecretManager::is_initialized() const {
    std::lock_guard<std::mutex> lock(_mutex);
    return _initialized;
}

void SecretManager::secure_log(const std::string& message)
{
    // Use fprintf for logging to stderr
    fprintf(stderr, "[SecretManager] %s\n", message.c_str());
}
