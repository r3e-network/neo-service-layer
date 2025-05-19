#include "SecretManager.h"
#include <nlohmann/json.hpp>

#include <stdexcept>
#include <vector>
#include <cstring>
#include <sstream>
#include <mutex>
#include <cstdio>

using json = nlohmann::json;

bool SecretManager::store_secret(const std::string& user_id, const std::string& secret_name, const std::string& secret_value)
{
    if (user_id.empty() || secret_name.empty())
    {
        secure_log("Invalid parameters: user_id or secret_name is empty");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        if (!initialize())
        {
            secure_log("Failed to initialize SecretManager");
            return false;
        }
    }

    try
    {
        secure_log("Storing secret for user " + user_id + ": " + secret_name);

        // Encrypt the secret value
        std::string encrypted_value = encrypt_value(secret_value);

        // Store the encrypted secret
        _user_secrets[user_id][secret_name] = encrypted_value;

        // Save to persistent storage
        save_to_persistent_storage();

        secure_log("Secret stored successfully");
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error storing secret: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error storing secret");
        return false;
    }
}

std::string SecretManager::get_secret(const std::string& user_id, const std::string& secret_name)
{
    if (user_id.empty() || secret_name.empty())
    {
        secure_log("Invalid parameters: user_id or secret_name is empty");
        return "";
    }

    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        if (!initialize())
        {
            secure_log("Failed to initialize SecretManager");
            return "";
        }
    }

    try
    {
        secure_log("Getting secret for user " + user_id + ": " + secret_name);

        // Check if the user exists
        auto user_it = _user_secrets.find(user_id);
        if (user_it == _user_secrets.end())
        {
            secure_log("User not found: " + user_id);
            return ""; // User not found
        }

        // Check if the secret exists
        auto secret_it = user_it->second.find(secret_name);
        if (secret_it == user_it->second.end())
        {
            secure_log("Secret not found: " + secret_name);
            return ""; // Secret not found
        }

        // Decrypt and return the secret value
        std::string decrypted_value = decrypt_value(secret_it->second);
        secure_log("Secret retrieved successfully");
        return decrypted_value;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error getting secret: " + std::string(ex.what()));
        return "";
    }
    catch (...)
    {
        secure_log("Unknown error getting secret");
        return "";
    }
}

bool SecretManager::delete_secret(const std::string& user_id, const std::string& secret_name)
{
    if (user_id.empty() || secret_name.empty())
    {
        secure_log("Invalid parameters: user_id or secret_name is empty");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        if (!initialize())
        {
            secure_log("Failed to initialize SecretManager");
            return false;
        }
    }

    try
    {
        secure_log("Deleting secret for user " + user_id + ": " + secret_name);

        // Check if the user exists
        auto user_it = _user_secrets.find(user_id);
        if (user_it == _user_secrets.end())
        {
            secure_log("User not found: " + user_id);
            return false; // User not found
        }

        // Check if the secret exists
        auto secret_it = user_it->second.find(secret_name);
        if (secret_it == user_it->second.end())
        {
            secure_log("Secret not found: " + secret_name);
            return false; // Secret not found
        }

        // Clear the secret value
        std::fill(secret_it->second.begin(), secret_it->second.end(), 0);

        // Remove the secret
        user_it->second.erase(secret_it);

        // If the user has no more secrets, remove the user
        if (user_it->second.empty())
        {
            _user_secrets.erase(user_it);
        }

        // Save changes to persistent storage
        save_to_persistent_storage();

        secure_log("Secret deleted successfully");
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error deleting secret: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error deleting secret");
        return false;
    }
}
