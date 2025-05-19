#include "SecretManager.h"
#include "StorageManager.h"
#include "KeyManager.h"
#include <nlohmann/json.hpp>

#include <stdexcept>
#include <vector>
#include <cstring>
#include <sstream>
#include <mutex>
#include <cstdio>

using json = nlohmann::json;

bool SecretManager::save_to_persistent_storage()
{
    if (!_initialized)
    {
        secure_log("SecretManager not initialized");
        return false;
    }

    try
    {
        secure_log("Saving secrets to persistent storage");

        // Check if storage manager is initialized
        if (!_storage_manager)
        {
            secure_log("Storage manager not provided");
            return false;
        }

        // Create a JSON object to store all secrets
        json secrets_json;

        // Add all user secrets to the JSON object
        for (const auto& user_entry : _user_secrets)
        {
            json user_secrets_json;

            for (const auto& secret_entry : user_entry.second)
            {
                // Store the encrypted secret value
                user_secrets_json[secret_entry.first] = secret_entry.second;
            }

            secrets_json[user_entry.first] = user_secrets_json;
        }

        // Serialize the JSON object to a string
        std::string secrets_str = secrets_json.dump();

        // Save the secrets to storage
        bool result = _storage_manager->store("secrets", "user_secrets", secrets_str);

        if (result)
        {
            secure_log("Secrets saved to persistent storage successfully");
        }
        else
        {
            secure_log("Failed to save secrets to persistent storage");
        }

        return result;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error saving secrets to persistent storage: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error saving secrets to persistent storage");
        return false;
    }
}

bool SecretManager::load_from_persistent_storage()
{
    if (!_initialized)
    {
        secure_log("SecretManager not initialized");
        return false;
    }

    try
    {
        secure_log("Loading secrets from persistent storage");

        // Check if storage manager is initialized
        if (!_storage_manager)
        {
            secure_log("Storage manager not provided");
            return false;
        }

        // Load the secrets from storage
        std::string secrets_str;
        bool result = _storage_manager->retrieve_data("secrets", "user_secrets", secrets_str);

        if (!result || secrets_str.empty())
        {
            secure_log("No secrets found in persistent storage");
            return true;
        }

        // Parse the JSON string
        json secrets_json = json::parse(secrets_str);

        // Clear existing secrets
        std::lock_guard<std::mutex> lock(_mutex);
        _user_secrets.clear();

        // Load all user secrets from the JSON object
        for (auto& user_entry : secrets_json.items())
        {
            std::string user_id = user_entry.key();

            for (auto& secret_entry : user_entry.value().items())
            {
                std::string secret_name = secret_entry.key();
                std::string encrypted_value = secret_entry.value();

                // Store the secret
                _user_secrets[user_id][secret_name] = encrypted_value;
            }
        }

        secure_log("Secrets loaded from persistent storage successfully");
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error loading secrets from persistent storage: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error loading secrets from persistent storage");
        return false;
    }
}
