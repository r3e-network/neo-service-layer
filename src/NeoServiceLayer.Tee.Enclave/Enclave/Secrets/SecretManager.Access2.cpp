#include "SecretManager.h"
#include <nlohmann/json.hpp>

#include <stdexcept>
#include <vector>
#include <cstring>
#include <sstream>
#include <mutex>
#include <cstdio>

using json = nlohmann::json;

std::vector<std::string> SecretManager::list_secrets(const std::string& user_id)
{
    std::vector<std::string> names;

    if (user_id.empty())
    {
        secure_log("Invalid parameter: user_id is empty");
        return names;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        if (!initialize())
        {
            secure_log("Failed to initialize SecretManager");
            return names;
        }
    }

    try
    {
        secure_log("Listing secrets for user " + user_id);

        // Check if the user exists
        auto user_it = _user_secrets.find(user_id);
        if (user_it == _user_secrets.end())
        {
            secure_log("User not found: " + user_id);
            return names; // Empty vector
        }

        // Collect all secret names
        for (const auto& secret_entry : user_it->second)
        {
            names.push_back(secret_entry.first);
        }

        secure_log("Listed " + std::to_string(names.size()) + " secrets for user " + user_id);
        return names;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error listing secrets: " + std::string(ex.what()));
        return names;
    }
    catch (...)
    {
        secure_log("Unknown error listing secrets");
        return names;
    }
}

std::string SecretManager::get_user_secrets_json(const std::string& user_id)
{
    if (user_id.empty())
    {
        secure_log("Invalid parameter: user_id is empty");
        return "{}";
    }

    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        if (!initialize())
        {
            secure_log("Failed to initialize SecretManager");
            return "{}";
        }
    }

    try
    {
        secure_log("Getting secrets JSON for user " + user_id);

        // Check if the user exists
        auto user_it = _user_secrets.find(user_id);
        if (user_it == _user_secrets.end())
        {
            secure_log("User not found: " + user_id);
            return "{}"; // Empty JSON object
        }

        // Create a JSON object for the user's secrets
        json secrets_json = json::object();

        // Add all secrets to the JSON object
        for (const auto& secret_entry : user_it->second)
        {
            // Decrypt the secret value
            std::string decrypted_value = decrypt_value(secret_entry.second);
            secrets_json[secret_entry.first] = decrypted_value;
        }

        secure_log("Got secrets JSON for user " + user_id);
        return secrets_json.dump();
    }
    catch (const std::exception& ex)
    {
        secure_log("Error getting secrets JSON: " + std::string(ex.what()));
        return "{}";
    }
    catch (...)
    {
        secure_log("Unknown error getting secrets JSON");
        return "{}";
    }
}
