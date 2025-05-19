#include "OcclumEnclave.h"
#include "OcclumIntegration.h"
#include "JavaScriptEngine.h"
#include "SecretManager.h"
#include <nlohmann/json.hpp>
#include <sstream>
#include <iomanip>
#include <cstdio>

using json = nlohmann::json;

std::string OcclumEnclave::execute_javascript(
    const std::string& code,
    const std::string& input,
    const std::string& secrets,
    const std::string& function_id,
    const std::string& user_id,
    uint64_t& gas_used)
{
    if (!_initialized && !initialize())
    {
        return "{\"error\":\"Enclave not initialized\"}";
    }
    
    try
    {
        secure_log("Executing JavaScript code for function " + function_id + ", user " + user_id);
        
        // Start gas accounting
        if (_gas_accounting)
        {
            _gas_accounting->start_accounting(function_id, user_id);
        }
        
        // Execute the JavaScript code using Occlum
        char output[4096] = {0}; // Buffer for the output
        size_t output_size = 0;
        
        bool success = OcclumIntegration::ExecuteJavaScript(
            code.c_str(),
            input.c_str(),
            secrets.c_str(),
            function_id.c_str(),
            user_id.c_str(),
            output,
            sizeof(output),
            &output_size);
        
        // Stop gas accounting
        if (_gas_accounting)
        {
            gas_used = _gas_accounting->stop_accounting(function_id, user_id);
        }
        
        if (!success)
        {
            secure_log("Failed to execute JavaScript code");
            return "{\"error\":\"Failed to execute JavaScript code\"}";
        }
        
        secure_log("JavaScript code executed successfully, gas used: " + std::to_string(gas_used));
        return std::string(output, output_size);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error executing JavaScript code: " + std::string(ex.what()));
        return "{\"error\":\"" + std::string(ex.what()) + "\"}";
    }
    catch (...)
    {
        secure_log("Unknown error executing JavaScript code");
        return "{\"error\":\"Unknown error\"}";
    }
}

uint64_t OcclumEnclave::create_js_context()
{
    if (!_initialized && !initialize())
    {
        return 0;
    }
    
    std::lock_guard<std::mutex> lock(_mutex);
    
    try
    {
        secure_log("Creating JavaScript context");
        
        // Create a new JavaScript engine
        auto engine = std::make_unique<JavaScriptEngine>(_gas_accounting.get(), _secret_manager.get(), _storage_manager.get());
        
        // Initialize the engine
        if (!engine->initialize())
        {
            secure_log("Failed to initialize JavaScript engine");
            return 0;
        }
        
        // Assign a context ID
        uint64_t context_id = _next_context_id++;
        
        // Store the engine
        _js_contexts[context_id] = std::move(engine);
        
        secure_log("JavaScript context created with ID: " + std::to_string(context_id));
        return context_id;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error creating JavaScript context: " + std::string(ex.what()));
        return 0;
    }
    catch (...)
    {
        secure_log("Unknown error creating JavaScript context");
        return 0;
    }
}

bool OcclumEnclave::destroy_js_context(uint64_t context_id)
{
    if (!_initialized && !initialize())
    {
        return false;
    }
    
    std::lock_guard<std::mutex> lock(_mutex);
    
    try
    {
        secure_log("Destroying JavaScript context: " + std::to_string(context_id));
        
        // Find the context
        auto it = _js_contexts.find(context_id);
        if (it == _js_contexts.end())
        {
            secure_log("JavaScript context not found: " + std::to_string(context_id));
            return false;
        }
        
        // Remove the context
        _js_contexts.erase(it);
        
        secure_log("JavaScript context destroyed: " + std::to_string(context_id));
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error destroying JavaScript context: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error destroying JavaScript context");
        return false;
    }
}

std::string OcclumEnclave::execute_js_code(
    uint64_t context_id,
    const std::string& code,
    const std::string& input,
    const std::string& user_id,
    const std::string& function_id)
{
    if (!_initialized && !initialize())
    {
        return "{\"error\":\"Enclave not initialized\"}";
    }
    
    try
    {
        secure_log("Executing JavaScript code for context " + std::to_string(context_id) + ", function " + function_id);
        
        // Find the context
        std::lock_guard<std::mutex> lock(_mutex);
        auto it = _js_contexts.find(context_id);
        if (it == _js_contexts.end())
        {
            secure_log("JavaScript context not found: " + std::to_string(context_id));
            return "{\"error\":\"JavaScript context not found\"}";
        }
        
        // Get the secrets for the user
        std::string secrets = "{}";
        if (_secret_manager)
        {
            secrets = _secret_manager->get_user_secrets_json(user_id);
        }
        
        // Execute the code
        uint64_t gas_used = 0;
        std::string result = it->second->execute(code, input, secrets, function_id, user_id, gas_used);
        
        secure_log("JavaScript code executed for context " + std::to_string(context_id) + ", gas used: " + std::to_string(gas_used));
        return result;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error executing JavaScript code: " + std::string(ex.what()));
        return "{\"error\":\"" + std::string(ex.what()) + "\"}";
    }
    catch (...)
    {
        secure_log("Unknown error executing JavaScript code");
        return "{\"error\":\"Unknown error\"}";
    }
}
