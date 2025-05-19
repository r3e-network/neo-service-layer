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

std::string SecretManager::encrypt_value(const std::string& value)
{
    try
    {
        // Convert the value to a vector of bytes
        std::vector<uint8_t> data(value.begin(), value.end());

        // Use OcclumIntegration to seal the data
        std::vector<uint8_t> sealed_data = OcclumIntegration::SealData(data);

        // Convert to base64 for storage
        std::string base64_data;
        size_t base64_len = 0;

        // Calculate the required buffer size
        mbedtls_base64_encode(nullptr, 0, &base64_len, sealed_data.data(), sealed_data.size());

        // Allocate the buffer
        base64_data.resize(base64_len);

        // Encode the data
        mbedtls_base64_encode(
            reinterpret_cast<unsigned char*>(&base64_data[0]),
            base64_data.size(),
            &base64_len,
            sealed_data.data(),
            sealed_data.size());

        // Resize the string to the actual encoded length
        base64_data.resize(base64_len - 1); // Remove the null terminator

        return base64_data;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error encrypting value: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error encrypting value");
        throw;
    }
}

std::string SecretManager::decrypt_value(const std::string& encrypted_value)
{
    try
    {
        // Decode from base64
        std::vector<uint8_t> base64_data(encrypted_value.begin(), encrypted_value.end());
        std::vector<uint8_t> sealed_data;
        size_t sealed_len = 0;

        // Calculate the required buffer size
        mbedtls_base64_decode(nullptr, 0, &sealed_len, base64_data.data(), base64_data.size());

        // Allocate the buffer
        sealed_data.resize(sealed_len);

        // Decode the data
        mbedtls_base64_decode(
            sealed_data.data(),
            sealed_data.size(),
            &sealed_len,
            base64_data.data(),
            base64_data.size());

        // Resize the vector to the actual decoded length
        sealed_data.resize(sealed_len);

        // Use OcclumIntegration to unseal the data
        std::vector<uint8_t> data = OcclumIntegration::UnsealData(sealed_data);

        // Convert back to a string
        return std::string(data.begin(), data.end());
    }
    catch (const std::exception& ex)
    {
        secure_log("Error decrypting value: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error decrypting value");
        throw;
    }
}

void SecretManager::generate_encryption_key()
{
    try
    {
        secure_log("Generating encryption key");

        // Generate a random encryption key using OcclumIntegration
        _encryption_key = OcclumIntegration::GenerateRandomBytes(_encryption_key.size());

        secure_log("Encryption key generated successfully");
    }
    catch (const std::exception& ex)
    {
        secure_log("Error generating encryption key: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error generating encryption key");
        throw;
    }
}

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
        bool result = _storage_manager->store_data("secrets", "user_secrets", secrets_str);

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

void SecretManager::secure_log(const std::string& message)
{
    // Use the secure Logger instead of fprintf
    NeoServiceLayer::Enclave::Logger::getInstance().info("SecretManager", message);
}