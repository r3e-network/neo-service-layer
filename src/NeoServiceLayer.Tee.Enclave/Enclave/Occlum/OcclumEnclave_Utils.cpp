#include "OcclumEnclave.h"
#include "OcclumIntegration.h"
#include <nlohmann/json.hpp>
#include <sstream>
#include <iomanip>
#include <cstdio>
#include <random>

using json = nlohmann::json;

std::string OcclumEnclave::get_status()
{
    if (!_initialized && !initialize())
    {
        return "{\"status\":\"not_initialized\"}";
    }
    
    try
    {
        secure_log("Getting enclave status");
        
        json status = {
            {"status", "running"},
            {"mrenclave", OcclumIntegration::GetMrEnclave()},
            {"mrsigner", OcclumIntegration::GetMrSigner()},
            {"js_contexts", _js_contexts.size()},
            {"timestamp", OcclumIntegration::GetCurrentTime()}
        };
        
        return status.dump();
    }
    catch (const std::exception& ex)
    {
        secure_log("Error getting enclave status: " + std::string(ex.what()));
        return "{\"status\":\"error\",\"error\":\"" + std::string(ex.what()) + "\"}";
    }
    catch (...)
    {
        secure_log("Unknown error getting enclave status");
        return "{\"status\":\"error\",\"error\":\"Unknown error\"}";
    }
}

bool OcclumEnclave::initialize_storage(const std::string& storage_path)
{
    if (!_initialized && !initialize())
    {
        return false;
    }
    
    try
    {
        secure_log("Initializing storage with path: " + storage_path);
        
        if (!_storage_manager)
        {
            secure_log("Storage manager not initialized");
            return false;
        }
        
        bool success = _storage_manager->set_storage_path(storage_path);
        if (!success)
        {
            secure_log("Failed to set storage path");
            return false;
        }
        
        secure_log("Storage initialized successfully");
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error initializing storage: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error initializing storage");
        return false;
    }
}

bool OcclumEnclave::store_user_secret(const std::string& user_id, const std::string& secret_name, const std::string& secret_value)
{
    if (!_initialized && !initialize())
    {
        return false;
    }
    
    try
    {
        secure_log("Storing secret for user " + user_id + ": " + secret_name);
        
        if (!_secret_manager)
        {
            secure_log("Secret manager not initialized");
            return false;
        }
        
        bool success = _secret_manager->store_secret(user_id, secret_name, secret_value);
        if (!success)
        {
            secure_log("Failed to store secret");
            return false;
        }
        
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

std::string OcclumEnclave::get_user_secret(const std::string& user_id, const std::string& secret_name)
{
    if (!_initialized && !initialize())
    {
        return "";
    }
    
    try
    {
        secure_log("Getting secret for user " + user_id + ": " + secret_name);
        
        if (!_secret_manager)
        {
            secure_log("Secret manager not initialized");
            return "";
        }
        
        std::string secret_value = _secret_manager->get_secret(user_id, secret_name);
        
        if (secret_value.empty())
        {
            secure_log("Secret not found");
        }
        else
        {
            secure_log("Secret retrieved successfully");
        }
        
        return secret_value;
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

bool OcclumEnclave::delete_user_secret(const std::string& user_id, const std::string& secret_name)
{
    if (!_initialized && !initialize())
    {
        return false;
    }
    
    try
    {
        secure_log("Deleting secret for user " + user_id + ": " + secret_name);
        
        if (!_secret_manager)
        {
            secure_log("Secret manager not initialized");
            return false;
        }
        
        bool success = _secret_manager->delete_secret(user_id, secret_name);
        if (!success)
        {
            secure_log("Failed to delete secret");
            return false;
        }
        
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

std::vector<std::string> OcclumEnclave::list_user_secrets(const std::string& user_id)
{
    if (!_initialized && !initialize())
    {
        return {};
    }
    
    try
    {
        secure_log("Listing secrets for user " + user_id);
        
        if (!_secret_manager)
        {
            secure_log("Secret manager not initialized");
            return {};
        }
        
        std::vector<std::string> secret_names = _secret_manager->list_secrets(user_id);
        
        secure_log("Listed " + std::to_string(secret_names.size()) + " secrets for user " + user_id);
        return secret_names;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error listing secrets: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error listing secrets");
        return {};
    }
}
