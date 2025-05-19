#include "KeyManager.h"
#include <cstdio>

const KeyInfo* KeyManager::get_key(const std::string& key_id)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("KeyManager not initialized");
        return nullptr;
    }

    try
    {
        secure_log("Getting key: " + key_id);

        // Find the key
        auto it = _keys.find(key_id);
        if (it == _keys.end())
        {
            secure_log("Key not found: " + key_id);
            return nullptr;
        }

        secure_log("Key found: " + key_id);
        return &it->second;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error getting key: " + std::string(ex.what()));
        return nullptr;
    }
    catch (...)
    {
        secure_log("Unknown error getting key");
        return nullptr;
    }
}

const KeyInfo* KeyManager::get_active_key(KeyType type)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("KeyManager not initialized");
        return nullptr;
    }

    try
    {
        secure_log("Getting active key of type " + std::to_string(static_cast<int>(type)));

        // Find the active key for this type
        auto it = _active_keys.find(type);
        if (it == _active_keys.end())
        {
            secure_log("No active key found for type " + std::to_string(static_cast<int>(type)));
            return nullptr;
        }

        // Get the key
        auto key_it = _keys.find(it->second);
        if (key_it == _keys.end())
        {
            secure_log("Active key not found: " + it->second);
            return nullptr;
        }

        secure_log("Active key found: " + it->second);
        return &key_it->second;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error getting active key: " + std::string(ex.what()));
        return nullptr;
    }
    catch (...)
    {
        secure_log("Unknown error getting active key");
        return nullptr;
    }
}

std::vector<std::string> KeyManager::list_keys()
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("KeyManager not initialized");
        return {};
    }

    try
    {
        secure_log("Listing keys");

        std::vector<std::string> keys;
        keys.reserve(_keys.size());

        for (const auto& pair : _keys)
        {
            keys.push_back(pair.first);
        }

        secure_log("Listed " + std::to_string(keys.size()) + " keys");
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

bool KeyManager::delete_key(const std::string& key_id)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("KeyManager not initialized");
        return false;
    }

    try
    {
        secure_log("Deleting key: " + key_id);

        // Find the key
        auto it = _keys.find(key_id);
        if (it == _keys.end())
        {
            secure_log("Key not found: " + key_id);
            return false;
        }

        // Check if this key is active
        for (auto& pair : _active_keys)
        {
            if (pair.second == key_id)
            {
                // Remove from active keys
                _active_keys.erase(pair.first);
                break;
            }
        }

        // Clear sensitive data
        std::fill(it->second.data.begin(), it->second.data.end(), 0);

        // Remove the key
        _keys.erase(it);

        // Save the keys to persistent storage
        if (!save_keys())
        {
            secure_log("Failed to save keys to persistent storage");
            // Continue anyway
        }

        secure_log("Key deleted successfully: " + key_id);
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error deleting key: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error deleting key");
        return false;
    }
}
