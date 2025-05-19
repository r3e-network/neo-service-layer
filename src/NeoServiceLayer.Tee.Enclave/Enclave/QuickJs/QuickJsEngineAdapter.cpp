#include "QuickJsEngineAdapter.h"
#include "../Occlum/OcclumEnclave.h"
#include <openssl/sha.h>
#include <openssl/rand.h>
#include <sstream>
#include <iomanip>
#include <functional>
#include <random>

using namespace NeoServiceLayer::Tee::Enclave::QuickJs;

// Constants
constexpr uint64_t GAS_LIMIT = 10000000; // 10 million gas units

QuickJsEngineAdapter::QuickJsEngineAdapter(
    GasAccounting* gas_accounting,
    SecretManager* secret_manager,
    StorageManager* storage_manager)
    : _gas_accounting(gas_accounting),
      _secret_manager(secret_manager),
      _storage_manager(storage_manager),
      _key_manager(nullptr),
      _gas_used(0),
      _initialized(false),
      _current_function_id(""),
      _current_user_id("")
{
    _executor = std::make_unique<QuickJsExecutor>();

    // Get the KeyManager instance from OcclumEnclave
    _key_manager = OcclumEnclave::getInstance().get_key_manager();
}

QuickJsEngineAdapter::~QuickJsEngineAdapter()
{
    // The executor will be automatically destroyed
}

bool QuickJsEngineAdapter::initialize()
{
    if (_initialized)
    {
        return true;
    }

    try
    {
        // Set up callbacks
        _executor->SetLogCallback(std::bind(&QuickJsEngineAdapter::LogCallback, this, std::placeholders::_1));

        _executor->SetStorageCallbacks(
            std::bind(&QuickJsEngineAdapter::GetStorageCallback, this, std::placeholders::_1),
            std::bind(&QuickJsEngineAdapter::SetStorageCallback, this, std::placeholders::_1, std::placeholders::_2),
            std::bind(&QuickJsEngineAdapter::RemoveStorageCallback, this, std::placeholders::_1),
            std::bind(&QuickJsEngineAdapter::ClearStorageCallback, this));

        _executor->SetCryptoCallbacks(
            std::bind(&QuickJsEngineAdapter::RandomBytesCallback, this, std::placeholders::_1),
            std::bind(&QuickJsEngineAdapter::Sha256Callback, this, std::placeholders::_1),
            std::bind(&QuickJsEngineAdapter::SignCallback, this, std::placeholders::_1, std::placeholders::_2),
            std::bind(&QuickJsEngineAdapter::VerifyCallback, this, std::placeholders::_1, std::placeholders::_2, std::placeholders::_3));

        _executor->SetGasCallbacks(
            std::bind(&QuickJsEngineAdapter::GetGasCallback, this),
            std::bind(&QuickJsEngineAdapter::UseGasCallback, this, std::placeholders::_1));

        _executor->SetSecretsCallbacks(
            std::bind(&QuickJsEngineAdapter::GetSecretCallback, this, std::placeholders::_1),
            std::bind(&QuickJsEngineAdapter::SetSecretCallback, this, std::placeholders::_1, std::placeholders::_2),
            std::bind(&QuickJsEngineAdapter::RemoveSecretCallback, this, std::placeholders::_1));

        _executor->SetBlockchainCallbacks(
            std::bind(&QuickJsEngineAdapter::BlockchainCallbackCallback, this, std::placeholders::_1, std::placeholders::_2));

        _initialized = true;
        return true;
    }
    catch (const std::exception& ex)
    {
        host_log(("Error initializing QuickJsEngineAdapter: " + std::string(ex.what())).c_str());
        return false;
    }
}

std::string QuickJsEngineAdapter::execute(
    const std::string& code,
    const std::string& input_json,
    const std::string& secrets_json,
    const std::string& function_id,
    const std::string& user_id,
    uint64_t& gas_used)
{
    // Validate input parameters
    if (code.empty())
    {
        host_log("Error: Empty code provided to execute");
        return "{\"error\":\"Empty code provided\"}";
    }

    if (function_id.empty())
    {
        host_log("Error: Empty function ID provided to execute");
        return "{\"error\":\"Empty function ID provided\"}";
    }

    if (user_id.empty())
    {
        host_log("Error: Empty user ID provided to execute");
        return "{\"error\":\"Empty user ID provided\"}";
    }

    // Initialize if needed
    if (!_initialized && !initialize())
    {
        host_log("Error: Failed to initialize JavaScript engine");
        return "{\"error\":\"JavaScript engine not initialized\"}";
    }

    std::lock_guard<std::mutex> lock(_mutex);

    // Reset gas used
    _gas_used = 0;

    // Set current function and user IDs
    _current_function_id = function_id;
    _current_user_id = user_id;

    try
    {
        // Generate wrapped code that includes input and secrets
        std::string wrapped_code;
        try
        {
            wrapped_code = GenerateWrappedCode(code, input_json, secrets_json);
        }
        catch (const std::exception& ex)
        {
            host_log(("Error generating wrapped code: " + std::string(ex.what())).c_str());
            gas_used = _gas_used;
            return "{\"error\":\"Failed to prepare code for execution: " + std::string(ex.what()) + "\"}";
        }

        // Execute the code
        std::string result;
        try
        {
            result = _executor->Execute(wrapped_code, function_id + ".js");
        }
        catch (const std::exception& ex)
        {
            host_log(("Error executing JavaScript: " + std::string(ex.what())).c_str());
            gas_used = _gas_used;
            return "{\"error\":\"JavaScript execution failed: " + std::string(ex.what()) + "\"}";
        }

        // Set the gas used output parameter
        gas_used = _gas_used;

        // Validate the result
        if (result.empty())
        {
            host_log("Warning: Empty result from JavaScript execution");
            return "{\"result\":null}";
        }

        return result;
    }
    catch (const std::exception& ex)
    {
        // Set the gas used output parameter
        gas_used = _gas_used;

        host_log(("Unhandled error in execute: " + std::string(ex.what())).c_str());
        return "{\"error\":\"" + std::string(ex.what()) + "\"}";
    }
    catch (...)
    {
        // Set the gas used output parameter
        gas_used = _gas_used;

        host_log("Unknown error in execute");
        return "{\"error\":\"Unknown error during execution\"}";
    }
}

bool QuickJsEngineAdapter::verify_code_hash(
    const std::string& code,
    const std::string& hash)
{
    std::string calculated_hash = calculate_code_hash(code);
    return calculated_hash == hash;
}

std::string QuickJsEngineAdapter::calculate_code_hash(
    const std::string& code)
{
    unsigned char hash[SHA256_DIGEST_LENGTH];
    SHA256_CTX sha256;
    SHA256_Init(&sha256);
    SHA256_Update(&sha256, code.c_str(), code.length());
    SHA256_Final(hash, &sha256);

    std::stringstream ss;
    for (int i = 0; i < SHA256_DIGEST_LENGTH; i++)
    {
        ss << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(hash[i]);
    }

    return ss.str();
}

void QuickJsEngineAdapter::reset_gas_used()
{
    _gas_used = 0;
}

uint64_t QuickJsEngineAdapter::get_gas_used() const
{
    return _gas_used;
}

void QuickJsEngineAdapter::LogCallback(const std::string& message)
{
    host_log(message.c_str());
}

std::string QuickJsEngineAdapter::GetStorageCallback(const std::string& key)
{
    if (!_storage_manager)
    {
        return "";
    }

    // Prefix key with function ID and user ID
    std::string prefixed_key = "js:" + _current_function_id + ":" + _current_user_id + ":" + key;

    // Get value from storage
    return _storage_manager->retrieve_string(prefixed_key);
}

void QuickJsEngineAdapter::SetStorageCallback(const std::string& key, const std::string& value)
{
    if (!_storage_manager)
    {
        return;
    }

    // Prefix key with function ID and user ID
    std::string prefixed_key = "js:" + _current_function_id + ":" + _current_user_id + ":" + key;

    // Store value
    _storage_manager->store_string(prefixed_key, value);
}

void QuickJsEngineAdapter::RemoveStorageCallback(const std::string& key)
{
    if (!_storage_manager)
    {
        return;
    }

    // Prefix key with function ID and user ID
    std::string prefixed_key = "js:" + _current_function_id + ":" + _current_user_id + ":" + key;

    // Delete value
    _storage_manager->remove(prefixed_key);
}

void QuickJsEngineAdapter::ClearStorageCallback()
{
    if (!_storage_manager)
    {
        return;
    }

    // Get all keys
    std::vector<std::string> all_keys = _storage_manager->list_keys();

    // Filter keys by prefix
    std::string prefix = "js:" + _current_function_id + ":" + _current_user_id + ":";

    for (const auto& key : all_keys)
    {
        if (key.find(prefix) == 0)
        {
            // Remove key
            _storage_manager->remove(key);
        }
    }
}

std::string QuickJsEngineAdapter::RandomBytesCallback(int size)
{
    // Use gas
    _gas_used += 10;
    _gas_accounting->use_gas(10);

    // Generate random bytes
    std::string result;
    result.resize(size);

    // Use OpenSSL's RAND_bytes for secure random number generation
    if (RAND_bytes(reinterpret_cast<unsigned char*>(&result[0]), size) != 1)
    {
        // If RAND_bytes fails, log the error and fall back to a less secure method
        host_log("Error: RAND_bytes failed, falling back to less secure method");

        // Use a more secure alternative to rand()
        std::random_device rd;
        std::mt19937 gen(rd());
        std::uniform_int_distribution<> dis(0, 255);

        for (int i = 0; i < size; i++)
        {
            result[i] = static_cast<char>(dis(gen));
        }
    }

    return result;
}

std::string QuickJsEngineAdapter::Sha256Callback(const std::string& data)
{
    // Use gas
    _gas_used += 20;
    _gas_accounting->use_gas(20);

    // Calculate SHA-256 hash
    unsigned char hash[SHA256_DIGEST_LENGTH];
    SHA256_CTX sha256;
    SHA256_Init(&sha256);
    SHA256_Update(&sha256, data.c_str(), data.length());
    SHA256_Final(hash, &sha256);

    // Convert hash to hex string
    std::stringstream ss;
    for (int i = 0; i < SHA256_DIGEST_LENGTH; i++)
    {
        ss << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(hash[i]);
    }

    return ss.str();
}

std::string QuickJsEngineAdapter::SignCallback(const std::string& data, const std::string& key)
{
    // Use gas
    _gas_used += 50;
    _gas_accounting->use_gas(50);

    if (!_storage_manager || !_key_manager)
    {
        host_log("Error: Storage manager or key manager not initialized");
        return "";
    }

    try
    {
        // Get the key from the key manager
        const KeyInfo* key_info = _key_manager->get_key(key);
        if (!key_info)
        {
            // If the key doesn't exist, try to use the active key
            key_info = _key_manager->get_active_key(KeyType::EC);
            if (!key_info)
            {
                host_log("Error: No signing key available");
                return "";
            }
        }

        // Convert data to bytes
        std::vector<uint8_t> data_bytes(data.begin(), data.end());

        // Sign the data
        std::vector<uint8_t> signature = _key_manager->sign(key_info->id, data_bytes);

        // Convert signature to base64
        std::string signature_base64;

        // Simple hex encoding for now (in production, use proper base64)
        std::stringstream ss;
        for (const auto& byte : signature)
        {
            ss << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(byte);
        }
        signature_base64 = ss.str();

        return signature_base64;
    }
    catch (const std::exception& ex)
    {
        host_log(("Error signing data: " + std::string(ex.what())).c_str());
        return "";
    }
}

bool QuickJsEngineAdapter::VerifyCallback(const std::string& data, const std::string& signature, const std::string& key)
{
    // Use gas
    _gas_used += 50;
    _gas_accounting->use_gas(50);

    if (!_storage_manager || !_key_manager)
    {
        host_log("Error: Storage manager or key manager not initialized");
        return false;
    }

    try
    {
        // Get the key from the key manager
        const KeyInfo* key_info = _key_manager->get_key(key);
        if (!key_info)
        {
            // If the key doesn't exist, try to use the active key
            key_info = _key_manager->get_active_key(KeyType::EC);
            if (!key_info)
            {
                host_log("Error: No verification key available");
                return false;
            }
        }

        // Convert data to bytes
        std::vector<uint8_t> data_bytes(data.begin(), data.end());

        // Convert signature from base64/hex to bytes
        std::vector<uint8_t> signature_bytes;

        // Simple hex decoding (in production, use proper base64 decoding)
        for (size_t i = 0; i < signature.length(); i += 2)
        {
            std::string byte_str = signature.substr(i, 2);
            uint8_t byte = static_cast<uint8_t>(std::stoi(byte_str, nullptr, 16));
            signature_bytes.push_back(byte);
        }

        // Verify the signature
        return _key_manager->verify(key_info->id, data_bytes, signature_bytes);
    }
    catch (const std::exception& ex)
    {
        host_log(("Error verifying signature: " + std::string(ex.what())).c_str());
        return false;
    }
}

int64_t QuickJsEngineAdapter::GetGasCallback()
{
    return GAS_LIMIT - _gas_used;
}

bool QuickJsEngineAdapter::UseGasCallback(int64_t amount)
{
    if (amount <= 0)
    {
        return true;
    }

    if (_gas_used + amount > GAS_LIMIT)
    {
        return false;
    }

    _gas_used += amount;
    _gas_accounting->use_gas(amount);

    return true;
}

std::string QuickJsEngineAdapter::GetSecretCallback(const std::string& key)
{
    if (!_secret_manager)
    {
        return "";
    }

    // Use gas
    _gas_used += 10;
    _gas_accounting->use_gas(10);

    // Get secret
    return _secret_manager->get_secret(_current_user_id, key);
}

void QuickJsEngineAdapter::SetSecretCallback(const std::string& key, const std::string& value)
{
    if (!_secret_manager)
    {
        return;
    }

    // Use gas
    _gas_used += 20;
    _gas_accounting->use_gas(20);

    // Set secret
    _secret_manager->store_secret(_current_user_id, key, value);
}

void QuickJsEngineAdapter::RemoveSecretCallback(const std::string& key)
{
    if (!_secret_manager)
    {
        return;
    }

    // Use gas
    _gas_used += 15;
    _gas_accounting->use_gas(15);

    // Remove secret
    _secret_manager->delete_secret(_current_user_id, key);
}

void QuickJsEngineAdapter::BlockchainCallbackCallback(const std::string& method, const std::string& params)
{
    // Use gas
    _gas_used += 100;
    _gas_accounting->use_gas(100);

    // Log the blockchain callback
    host_log(("Blockchain callback: " + method + " - " + params).c_str());

    // Make the actual blockchain call through the host
    char result_buffer[4096] = {0};
    size_t result_size = 0;

    // Note: The ocall_blockchain_call function should be defined in the EDL file as:
    // ocall ocall_blockchain_call([in, string] const char* method, [in, string] const char* params,
    //                            [out, size=result_buffer_size] char* result_buffer, size_t result_buffer_size,
    //                            [out] size_t* result_size);

    // For now, we'll just simulate a successful call
    oe_result_t result = OE_OK;
    std::string result_str = "{\"success\":true,\"result\":\"simulated result\"}";
    if (result_buffer && sizeof(result_buffer) >= result_str.size() + 1)
    {
        memcpy(result_buffer, result_str.c_str(), result_str.size() + 1);
        result_size = result_str.size() + 1;
    }

    if (result != OE_OK)
    {
        host_log(("Error making blockchain call: " + std::to_string(result)).c_str());
        return;
    }

    // Process the result
    host_log(("Blockchain call result: " + result_str).c_str());

    // TODO: Process the result and update the JavaScript context
    // This would typically involve parsing the result and updating the JavaScript context
    // For now, we just log the result
}

std::string QuickJsEngineAdapter::GenerateWrappedCode(
    const std::string& code,
    const std::string& input_json,
    const std::string& secrets_json)
{
    try
    {
        // Validate input parameters
        if (code.empty())
        {
            throw std::invalid_argument("Empty code provided");
        }

        // Validate input_json is valid JSON
        std::string sanitized_input_json;
        if (input_json.empty())
        {
            sanitized_input_json = "{}";
        }
        else
        {
            // Simple validation - check for matching braces
            // In a production environment, use a proper JSON parser for validation
            int brace_count = 0;
            for (char c : input_json)
            {
                if (c == '{') brace_count++;
                else if (c == '}') brace_count--;

                if (brace_count < 0)
                {
                    throw std::invalid_argument("Invalid JSON in input_json: Unmatched braces");
                }
            }

            if (brace_count != 0)
            {
                throw std::invalid_argument("Invalid JSON in input_json: Unmatched braces");
            }

            sanitized_input_json = input_json;
        }

        // Validate secrets_json is valid JSON
        std::string sanitized_secrets_json;
        if (secrets_json.empty())
        {
            sanitized_secrets_json = "{}";
        }
        else
        {
            // Simple validation - check for matching braces
            // In a production environment, use a proper JSON parser for validation
            int brace_count = 0;
            for (char c : secrets_json)
            {
                if (c == '{') brace_count++;
                else if (c == '}') brace_count--;

                if (brace_count < 0)
                {
                    throw std::invalid_argument("Invalid JSON in secrets_json: Unmatched braces");
                }
            }

            if (brace_count != 0)
            {
                throw std::invalid_argument("Invalid JSON in secrets_json: Unmatched braces");
            }

            sanitized_secrets_json = secrets_json;
        }

        // Create a wrapper that sets up the input and secrets, then calls the main function
        std::string wrapped_code = R"(
            // Set up input
            const INPUT = )" + sanitized_input_json + R"(;

            // Set up secrets (legacy)
            const SECRETS_JSON = )" + sanitized_secrets_json + R"(;

            // Set up global error handler
            function __nsl_handle_error(error) {
                return JSON.stringify({
                    error: error.message || 'Unknown error',
                    stack: error.stack || ''
                });
            }

            // Main function wrapper
            function __nsl_execute_main() {
                try {
                    // Execute the user code
                    )" + code + R"(

                    // Find and call the main function
                    if (typeof main !== 'function') {
                        throw new Error('No main function defined');
                    }

                    const result = main(INPUT);
                    return JSON.stringify(result);
                } catch (error) {
                    return __nsl_handle_error(error);
                }
            }

            // Execute the main function with additional error handling
            try {
                __nsl_execute_main();
            } catch (error) {
                __nsl_handle_error(error);
            }
        )";

        return wrapped_code;
    }
    catch (const std::exception& ex)
    {
        host_log(("Error generating wrapped code: " + std::string(ex.what())).c_str());
        throw; // Rethrow to be handled by the caller
    }
}


