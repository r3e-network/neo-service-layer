#include "JavaScriptEngine.h"
#include "JavaScriptEngineFactory.h"
#include "NeoServiceLayerEnclave_t.h"
#include <openssl/sha.h>
#include <sstream>
#include <iomanip>

JavaScriptManager::JavaScriptManager(
    GasAccounting* gas_accounting,
    SecretManager* secret_manager,
    StorageManager* storage_manager)
    : _gas_accounting(gas_accounting),
      _secret_manager(secret_manager),
      _storage_manager(storage_manager),
      _engine(nullptr)
{
    try
    {
        // Create the JavaScript engine
        _engine = JavaScriptEngineFactory::CreateEngine(
            JavaScriptEngineFactory::GetDefaultEngineType(),
            gas_accounting,
            secret_manager,
            storage_manager);

        if (!_engine)
        {
            ocall_print_string("Error: Failed to create JavaScript engine");
        }
    }
    catch (const std::exception& ex)
    {
        ocall_print_string(("Error creating JavaScript engine: " + std::string(ex.what())).c_str());
    }
    catch (...)
    {
        ocall_print_string("Unknown error creating JavaScript engine");
    }
}

bool JavaScriptManager::execute(JavaScriptContext& context)
{
    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        // Initialize the engine if needed
        if (!_engine || !_engine->initialize())
        {
            context.success = false;
            context.error = "Failed to initialize JavaScript engine";
            return false;
        }

        // Execute the code
        context.result = _engine->execute(
            context.code,
            context.input_json,
            context.secrets_json,
            context.function_id,
            context.user_id,
            context.gas_used);

        context.success = true;
        return true;
    }
    catch (const std::exception& ex)
    {
        context.success = false;
        context.error = ex.what();
        return false;
    }
    catch (...)
    {
        context.success = false;
        context.error = "Unknown error";
        return false;
    }
}

bool JavaScriptManager::verify_code_hash(
    const std::string& code,
    const std::string& hash)
{
    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        // Initialize the engine if needed
        if (!_engine || !_engine->initialize())
        {
            return false;
        }

        return _engine->verify_code_hash(code, hash);
    }
    catch (const std::exception& ex)
    {
        ocall_print_string(("Error verifying code hash: " + std::string(ex.what())).c_str());
        return false;
    }
    catch (...)
    {
        ocall_print_string("Unknown error verifying code hash");
        return false;
    }
}

std::string JavaScriptManager::calculate_code_hash(
    const std::string& code)
{
    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        // Initialize the engine if needed
        if (!_engine || !_engine->initialize())
        {
            return "";
        }

        return _engine->calculate_code_hash(code);
    }
    catch (const std::exception& ex)
    {
        ocall_print_string(("Error calculating code hash: " + std::string(ex.what())).c_str());
        return "";
    }
    catch (...)
    {
        ocall_print_string("Unknown error calculating code hash");
        return "";
    }
}
