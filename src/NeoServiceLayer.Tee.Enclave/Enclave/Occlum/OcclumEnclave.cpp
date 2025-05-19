#include "OcclumEnclave.h"
#include "OcclumIntegration.h"
#include "JavaScriptEngine.h"
#include "SecretManager.h"
#include "StorageManager.h"
#include "KeyManager.h"
#include "EventTriggerManager.h"
#include "RemoteAttestationManager.h"
#include "BackupManager.h"
#include "GasAccountingManager.h"
#include "RandomnessService.h"
#include "ComplianceService.h"
#include "EnclaveMessageTypes.h"
#include <nlohmann/json.hpp>
#include <sstream>
#include <iomanip>
#include <cstdio>

using json = nlohmann::json;

// Singleton instance
OcclumEnclave* OcclumEnclave::_instance = nullptr;

OcclumEnclave& OcclumEnclave::getInstance()
{
    if (!_instance)
    {
        _instance = new OcclumEnclave();
    }
    return *_instance;
}

OcclumEnclave::OcclumEnclave()
    : _initialized(false),
      _next_context_id(1)
{
    // Initialize components to nullptr
    _storage_manager = nullptr;
    _key_manager = nullptr;
    _secret_manager = nullptr;
    _event_trigger_manager = nullptr;
    _remote_attestation_manager = nullptr;
    _backup_manager = nullptr;
    _gas_accounting = nullptr;
    _randomness_service = nullptr;
    _compliance_service = nullptr;
}

OcclumEnclave::~OcclumEnclave()
{
    cleanup();
}

bool OcclumEnclave::initialize(const std::vector<uint8_t>& config_data)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (_initialized)
    {
        secure_log("OcclumEnclave already initialized");
        return true;
    }

    try
    {
        secure_log("Initializing OcclumEnclave...");

        // Initialize Occlum
        if (!OcclumIntegration::Initialize())
        {
            secure_log("Failed to initialize Occlum");
            return false;
        }

        // Initialize components
        try
        {
            initialize_components();
        }
        catch (const std::exception& ex)
        {
            secure_log("Failed to initialize components: " + std::string(ex.what()));
            cleanup_components();
            return false;
        }

        // Process configuration data if provided
        if (!config_data.empty())
        {
            std::string config_str(config_data.begin(), config_data.end());
            secure_log("Processing configuration data: " + config_str);

            try
            {
                json config = json::parse(config_str);

                // Process configuration options
                if (config.contains("storage_path") && config["storage_path"].is_string())
                {
                    std::string storage_path = config["storage_path"];
                    if (!initialize_storage(storage_path))
                    {
                        secure_log("Warning: Failed to initialize storage with path: " + storage_path);
                        // Continue with default storage
                    }
                }

                // Process other configuration options as needed
            }
            catch (const json::exception& ex)
            {
                secure_log("Error parsing configuration data: " + std::string(ex.what()));
                // Continue with default configuration
            }
        }

        _initialized = true;
        secure_log("OcclumEnclave initialized successfully");
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error initializing OcclumEnclave: " + std::string(ex.what()));
        cleanup_components();
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error initializing OcclumEnclave");
        cleanup_components();
        return false;
    }
}

bool OcclumEnclave::cleanup()
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("OcclumEnclave not initialized, nothing to clean up");
        return true;
    }

    try
    {
        secure_log("Cleaning up OcclumEnclave...");

        // Clean up components
        cleanup_components();

        // Clean up Occlum
        OcclumIntegration::Cleanup();

        _initialized = false;
        secure_log("OcclumEnclave cleaned up successfully");
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error cleaning up OcclumEnclave: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error cleaning up OcclumEnclave");
        return false;
    }
}

bool OcclumEnclave::initialize_components()
{
    secure_log("Initializing components...");

    // Initialize storage manager
    _storage_manager = std::make_unique<StorageManager>();
    if (!_storage_manager->initialize())
    {
        throw std::runtime_error("Failed to initialize storage manager");
    }

    // Initialize key manager
    _key_manager = std::make_unique<KeyManager>();
    if (!_key_manager->initialize())
    {
        throw std::runtime_error("Failed to initialize key manager");
    }

    // Initialize secret manager
    _secret_manager = std::make_unique<SecretManager>(_storage_manager.get(), _key_manager.get());
    if (!_secret_manager->initialize())
    {
        throw std::runtime_error("Failed to initialize secret manager");
    }

    // Initialize gas accounting
    _gas_accounting = std::make_unique<GasAccountingManager>();
    if (!_gas_accounting->initialize())
    {
        throw std::runtime_error("Failed to initialize gas accounting");
    }

    // Initialize randomness service
    _randomness_service = std::make_unique<RandomnessService>(_key_manager.get());
    if (!_randomness_service->initialize())
    {
        throw std::runtime_error("Failed to initialize randomness service");
    }

    // Initialize compliance service
    _compliance_service = std::make_unique<ComplianceService>();
    if (!_compliance_service->initialize())
    {
        throw std::runtime_error("Failed to initialize compliance service");
    }

    // Initialize event trigger manager
    _event_trigger_manager = std::make_unique<EventTriggerManager>(_storage_manager.get());
    if (!_event_trigger_manager->initialize())
    {
        throw std::runtime_error("Failed to initialize event trigger manager");
    }

    // Initialize remote attestation manager
    _remote_attestation_manager = std::make_unique<RemoteAttestationManager>(_key_manager.get());
    if (!_remote_attestation_manager->initialize())
    {
        throw std::runtime_error("Failed to initialize remote attestation manager");
    }

    // Initialize backup manager
    _backup_manager = std::make_unique<BackupManager>(_storage_manager.get(), _key_manager.get());
    if (!_backup_manager->initialize())
    {
        throw std::runtime_error("Failed to initialize backup manager");
    }

    secure_log("Components initialized successfully");
    return true;
}

void OcclumEnclave::cleanup_components()
{
    secure_log("Cleaning up components...");

    // Clean up JavaScript contexts
    _js_contexts.clear();

    // Clean up components in reverse order of initialization
    _backup_manager.reset();
    _remote_attestation_manager.reset();
    _event_trigger_manager.reset();
    _compliance_service.reset();
    _randomness_service.reset();
    _gas_accounting.reset();
    _secret_manager.reset();
    _key_manager.reset();
    _storage_manager.reset();

    secure_log("Components cleaned up successfully");
}

void OcclumEnclave::secure_log(const std::string& message)
{
    // Use fprintf for logging to stderr
    fprintf(stderr, "[OcclumEnclave] %s\n", message.c_str());
}

std::string OcclumEnclave::process_message(int message_type, const std::string& message_data)
{
    if (!_initialized && !initialize())
    {
        return "{\"success\":false,\"error\":\"Enclave not initialized\"}";
    }
    
    try
    {
        secure_log("Processing message of type " + std::to_string(message_type));
        
        // Process the message based on its type
        switch (message_type)
        {
            case MESSAGE_TYPE_EXECUTE_JS:
            {
                // Parse the message data
                json data = json::parse(message_data);
                
                // Extract the parameters
                std::string code = data["code"];
                std::string input = data["input"];
                std::string secrets = data["secrets"];
                std::string function_id = data["function_id"];
                std::string user_id = data["user_id"];
                
                // Execute the JavaScript code
                uint64_t gas_used = 0;
                std::string result = execute_javascript(code, input, secrets, function_id, user_id, gas_used);
                
                // Return the result
                json response = {
                    {"success", true},
                    {"result", result},
                    {"gas_used", gas_used}
                };
                
                return response.dump();
            }
            
            // Add more message types as needed
            
            default:
                return "{\"success\":false,\"error\":\"Unknown message type\"}";
        }
    }
    catch (const std::exception& ex)
    {
        secure_log("Error processing message: " + std::string(ex.what()));
        return "{\"success\":false,\"error\":\"" + std::string(ex.what()) + "\"}";
    }
    catch (...)
    {
        secure_log("Unknown error processing message");
        return "{\"success\":false,\"error\":\"Unknown error\"}";
    }
}
std::string OcclumEnclave::get_status()
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        return "{\"status\":\"not_initialized\",\"error\":\"Failed to initialize enclave\"}";
    }
    
    try
    {
        json status = {
            {"status", "running"},
            {"initialized", _initialized},
            {"js_contexts", _js_contexts.size()},
            {"components", {
                {"storage_manager", _storage_manager ? _storage_manager->is_initialized() : false},
                {"key_manager", _key_manager ? _key_manager->is_initialized() : false},
                {"secret_manager", _secret_manager ? _secret_manager->is_initialized() : false},
                {"event_trigger_manager", _event_trigger_manager ? _event_trigger_manager->is_initialized() : false},
                {"remote_attestation_manager", _remote_attestation_manager ? _remote_attestation_manager->is_initialized() : false},
                {"backup_manager", _backup_manager ? _backup_manager->is_initialized() : false},
                {"gas_accounting", _gas_accounting ? _gas_accounting->is_initialized() : false},
                {"randomness_service", _randomness_service ? _randomness_service->is_initialized() : false},
                {"compliance_service", _compliance_service ? _compliance_service->is_initialized() : false}
            }}
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
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        secure_log("Enclave not initialized, cannot initialize storage");
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
        
        return _storage_manager->set_storage_path(storage_path);
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

uint64_t OcclumEnclave::create_js_context()
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        throw std::runtime_error("Enclave not initialized");
    }
    
    try
    {
        secure_log("Creating JavaScript context");
        
        // Create a new JavaScript engine
        auto js_engine = std::make_unique<JavaScriptEngine>();
        if (!js_engine->initialize())
        {
            throw std::runtime_error("Failed to initialize JavaScript engine");
        }
        
        // Assign a context ID
        uint64_t context_id = _next_context_id++;
        
        // Store the JavaScript engine
        _js_contexts[context_id] = std::move(js_engine);
        
        secure_log("Created JavaScript context with ID: " + std::to_string(context_id));
        return context_id;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error creating JavaScript context: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error creating JavaScript context");
        throw std::runtime_error("Unknown error creating JavaScript context");
    }
}

bool OcclumEnclave::destroy_js_context(uint64_t context_id)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        secure_log("Enclave not initialized, cannot destroy JavaScript context");
        return false;
    }
    
    try
    {
        secure_log("Destroying JavaScript context with ID: " + std::to_string(context_id));
        
        // Find the JavaScript context
        auto it = _js_contexts.find(context_id);
        if (it == _js_contexts.end())
        {
            secure_log("JavaScript context not found: " + std::to_string(context_id));
            return false;
        }
        
        // Remove the JavaScript context
        _js_contexts.erase(it);
        
        secure_log("Destroyed JavaScript context with ID: " + std::to_string(context_id));
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

std::string OcclumEnclave::execute_js_code(uint64_t context_id, const std::string& code, const std::string& input,
                                         const std::string& user_id, const std::string& function_id)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        throw std::runtime_error("Enclave not initialized");
    }
    
    try
    {
        secure_log("Executing JavaScript code in context " + std::to_string(context_id) +
                  " for function " + function_id + ", user " + user_id);
        
        // Find the JavaScript context
        auto it = _js_contexts.find(context_id);
        if (it == _js_contexts.end())
        {
            throw std::runtime_error("JavaScript context not found: " + std::to_string(context_id));
        }
        
        // Get user secrets
        std::string secrets = "{}";
        if (_secret_manager)
        {
            secrets = _secret_manager->get_user_secrets(user_id);
        }
        
        // Start gas accounting
        uint64_t gas_used = 0;
        if (_gas_accounting)
        {
            _gas_accounting->start_accounting(function_id);
        }
        
        // Execute the JavaScript code
        std::string result = it->second->execute(code, input, secrets);
        
        // Stop gas accounting
        if (_gas_accounting)
        {
            gas_used = _gas_accounting->stop_accounting(function_id);
        }
        
        secure_log("JavaScript execution completed, gas used: " + std::to_string(gas_used));
        return result;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error executing JavaScript code: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error executing JavaScript code");
        throw std::runtime_error("Unknown error executing JavaScript code");
    }
}

std::string OcclumEnclave::execute_javascript(const std::string& code, const std::string& input, const std::string& secrets,
                                            const std::string& function_id, const std::string& user_id, uint64_t& gas_used)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        throw std::runtime_error("Enclave not initialized");
    }
    
    try
    {
        secure_log("Executing JavaScript for function " + function_id + ", user " + user_id);
        
        // Create a temporary JavaScript context
        auto js_engine = std::make_unique<JavaScriptEngine>();
        if (!js_engine->initialize())
        {
            throw std::runtime_error("Failed to initialize JavaScript engine");
        }
        
        // Start gas accounting
        gas_used = 0;
        if (_gas_accounting)
        {
            _gas_accounting->start_accounting(function_id);
        }
        
        // Execute the JavaScript code
        std::string result = js_engine->execute(code, input, secrets);
        
        // Stop gas accounting
        if (_gas_accounting)
        {
            gas_used = _gas_accounting->stop_accounting(function_id);
        }
        
        secure_log("JavaScript execution completed, gas used: " + std::to_string(gas_used));
        return result;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error executing JavaScript: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error executing JavaScript");
        throw std::runtime_error("Unknown error executing JavaScript");
    }
}
bool OcclumEnclave::store_user_secret(const std::string& user_id, const std::string& secret_name, const std::string& secret_value)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        secure_log("Enclave not initialized, cannot store user secret");
        return false;
    }
    
    try
    {
        secure_log("Storing secret " + secret_name + " for user " + user_id);
        
        if (!_secret_manager)
        {
            secure_log("Secret manager not initialized");
            return false;
        }
        
        return _secret_manager->store_secret(user_id, secret_name, secret_value);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error storing user secret: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error storing user secret");
        return false;
    }
}

std::string OcclumEnclave::get_user_secret(const std::string& user_id, const std::string& secret_name)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        throw std::runtime_error("Enclave not initialized");
    }
    
    try
    {
        secure_log("Getting secret " + secret_name + " for user " + user_id);
        
        if (!_secret_manager)
        {
            throw std::runtime_error("Secret manager not initialized");
        }
        
        return _secret_manager->get_secret(user_id, secret_name);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error getting user secret: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error getting user secret");
        throw std::runtime_error("Unknown error getting user secret");
    }
}

bool OcclumEnclave::delete_user_secret(const std::string& user_id, const std::string& secret_name)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        secure_log("Enclave not initialized, cannot delete user secret");
        return false;
    }
    
    try
    {
        secure_log("Deleting secret " + secret_name + " for user " + user_id);
        
        if (!_secret_manager)
        {
            secure_log("Secret manager not initialized");
            return false;
        }
        
        return _secret_manager->delete_secret(user_id, secret_name);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error deleting user secret: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error deleting user secret");
        return false;
    }
}

std::vector<std::string> OcclumEnclave::list_user_secrets(const std::string& user_id)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        throw std::runtime_error("Enclave not initialized");
    }
    
    try
    {
        secure_log("Listing secrets for user " + user_id);
        
        if (!_secret_manager)
        {
            throw std::runtime_error("Secret manager not initialized");
        }
        
        return _secret_manager->list_secrets(user_id);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error listing user secrets: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error listing user secrets");
        throw std::runtime_error("Unknown error listing user secrets");
    }
}

int OcclumEnclave::generate_random_number(int min, int max)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        throw std::runtime_error("Enclave not initialized");
    }
    
    try
    {
        secure_log("Generating random number between " + std::to_string(min) + " and " + std::to_string(max));
        
        if (!_randomness_service)
        {
            throw std::runtime_error("Randomness service not initialized");
        }
        
        // Generate a random number using the randomness service
        std::string request_id = OcclumIntegration::GenerateUuid();
        uint64_t random_number = _randomness_service->generate_random_number(min, max, "system", request_id);
        
        return static_cast<int>(random_number);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error generating random number: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error generating random number");
        throw std::runtime_error("Unknown error generating random number");
    }
}

std::vector<uint8_t> OcclumEnclave::generate_random_bytes(size_t length)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        throw std::runtime_error("Enclave not initialized");
    }
    
    try
    {
        secure_log("Generating " + std::to_string(length) + " random bytes");
        
        if (!_randomness_service)
        {
            throw std::runtime_error("Randomness service not initialized");
        }
        
        // Generate random bytes using the randomness service
        std::string request_id = OcclumIntegration::GenerateUuid();
        return _randomness_service->generate_random_bytes(length, "system", request_id);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error generating random bytes: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error generating random bytes");
        throw std::runtime_error("Unknown error generating random bytes");
    }
}

std::string OcclumEnclave::generate_uuid()
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        throw std::runtime_error("Enclave not initialized");
    }
    
    try
    {
        secure_log("Generating UUID");
        return OcclumIntegration::GenerateUuid();
    }
    catch (const std::exception& ex)
    {
        secure_log("Error generating UUID: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error generating UUID");
        throw std::runtime_error("Unknown error generating UUID");
    }
}

std::vector<uint8_t> OcclumEnclave::generate_attestation_evidence()
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        throw std::runtime_error("Enclave not initialized");
    }
    
    try
    {
        secure_log("Generating attestation evidence");
        
        if (!_remote_attestation_manager)
        {
            throw std::runtime_error("Remote attestation manager not initialized");
        }
        
        return _remote_attestation_manager->generate_evidence();
    }
    catch (const std::exception& ex)
    {
        secure_log("Error generating attestation evidence: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error generating attestation evidence");
        throw std::runtime_error("Unknown error generating attestation evidence");
    }
}

bool OcclumEnclave::verify_attestation(const std::vector<uint8_t>& evidence, const std::vector<uint8_t>& endorsements)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        throw std::runtime_error("Enclave not initialized");
    }
    
    try
    {
        secure_log("Verifying attestation evidence");
        
        if (!_remote_attestation_manager)
        {
            throw std::runtime_error("Remote attestation manager not initialized");
        }
        
        return _remote_attestation_manager->verify_evidence(evidence, endorsements);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error verifying attestation evidence: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error verifying attestation evidence");
        throw std::runtime_error("Unknown error verifying attestation evidence");
    }
}
std::string OcclumEnclave::verify_compliance(const std::string& code, const std::string& user_id,
                                   const std::string& function_id, const std::string& compliance_rules)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        throw std::runtime_error("Enclave not initialized");
    }
    
    try
    {
        secure_log("Verifying compliance for function " + function_id + ", user " + user_id);
        
        if (!_compliance_service)
        {
            throw std::runtime_error("Compliance service not initialized");
        }
        
        return _compliance_service->verify_compliance(code, user_id, function_id, compliance_rules);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error verifying compliance: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error verifying compliance");
        throw std::runtime_error("Unknown error verifying compliance");
    }
}

bool OcclumEnclave::occlum_init(const std::string& instance_dir, const std::string& log_level)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    try
    {
        secure_log("Initializing Occlum with instance directory: " + instance_dir + ", log level: " + log_level);
        return OcclumIntegration::Initialize(instance_dir.c_str(), log_level.c_str());
    }
    catch (const std::exception& ex)
    {
        secure_log("Error initializing Occlum: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error initializing Occlum");
        return false;
    }
}

int OcclumEnclave::occlum_exec(const std::string& path, const std::vector<std::string>& argv, const std::vector<std::string>& env)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        secure_log("Enclave not initialized, cannot execute command");
        return -1;
    }
    
    try
    {
        secure_log("Executing command: " + path);
        
        // Convert argv and env to C-style arrays
        std::vector<const char*> argv_cstr;
        for (const auto& arg : argv)
        {
            argv_cstr.push_back(arg.c_str());
        }
        argv_cstr.push_back(nullptr);
        
        std::vector<const char*> env_cstr;
        for (const auto& var : env)
        {
            env_cstr.push_back(var.c_str());
        }
        env_cstr.push_back(nullptr);
        
        // Execute the command
        int exit_value = 0;
        bool success = OcclumIntegration::ExecuteCommand(path.c_str(), argv_cstr.data(), env_cstr.data(), &exit_value);
        
        if (!success)
        {
            secure_log("Failed to execute command: " + path);
            return -1;
        }
        
        return exit_value;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error executing command: " + std::string(ex.what()));
        return -1;
    }
    catch (...)
    {
        secure_log("Unknown error executing command");
        return -1;
    }
}

std::vector<uint8_t> OcclumEnclave::sign_data(const std::vector<uint8_t>& data)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        throw std::runtime_error("Enclave not initialized");
    }
    
    try
    {
        secure_log("Signing data of size " + std::to_string(data.size()));
        
        if (!_key_manager)
        {
            throw std::runtime_error("Key manager not initialized");
        }
        
        // Get the active signing key
        const KeyInfo* key_info = _key_manager->get_active_key(KeyType::EC);
        if (!key_info)
        {
            // If no active key exists, create one
            std::string key_id = _key_manager->create_key(KeyType::EC, "signing_key");
            key_info = _key_manager->get_key(key_id);
            if (!key_info)
            {
                throw std::runtime_error("Failed to create signing key");
            }
        }
        
        // Sign the data with the key
        return _key_manager->sign(key_info->id, data);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error signing data: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error signing data");
        throw std::runtime_error("Unknown error signing data");
    }
}

bool OcclumEnclave::verify_signature(const std::vector<uint8_t>& data, const std::vector<uint8_t>& signature)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        throw std::runtime_error("Enclave not initialized");
    }
    
    try
    {
        secure_log("Verifying signature for data of size " + std::to_string(data.size()));
        
        if (!_key_manager)
        {
            throw std::runtime_error("Key manager not initialized");
        }
        
        // Get the active verification key
        const KeyInfo* key_info = _key_manager->get_active_key(KeyType::EC);
        if (!key_info)
        {
            throw std::runtime_error("No verification key available");
        }
        
        // Verify the signature with the key
        return _key_manager->verify(key_info->id, data, signature);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error verifying signature: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error verifying signature");
        throw std::runtime_error("Unknown error verifying signature");
    }
}

std::vector<uint8_t> OcclumEnclave::seal_data(const std::vector<uint8_t>& data)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        throw std::runtime_error("Enclave not initialized");
    }
    
    try
    {
        secure_log("Sealing data of size " + std::to_string(data.size()));
        
        if (!_key_manager)
        {
            throw std::runtime_error("Key manager not initialized");
        }
        
        // Get the sealing key
        const KeyInfo* key_info = _key_manager->get_key_by_name("sealing_key");
        if (!key_info)
        {
            // If no sealing key exists, create one
            std::string key_id = _key_manager->create_key(KeyType::AES, "sealing_key");
            key_info = _key_manager->get_key(key_id);
            if (!key_info)
            {
                throw std::runtime_error("Failed to create sealing key");
            }
        }
        
        // Seal the data with the key
        return _key_manager->encrypt(key_info->id, data);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error sealing data: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error sealing data");
        throw std::runtime_error("Unknown error sealing data");
    }
}

std::vector<uint8_t> OcclumEnclave::unseal_data(const std::vector<uint8_t>& sealed_data)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        throw std::runtime_error("Enclave not initialized");
    }
    
    try
    {
        secure_log("Unsealing data of size " + std::to_string(sealed_data.size()));
        
        if (!_key_manager)
        {
            throw std::runtime_error("Key manager not initialized");
        }
        
        // Get the sealing key
        const KeyInfo* key_info = _key_manager->get_key_by_name("sealing_key");
        if (!key_info)
        {
            throw std::runtime_error("No sealing key available");
        }
        
        // Unseal the data with the key
        return _key_manager->decrypt(key_info->id, sealed_data);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error unsealing data: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error unsealing data");
        throw std::runtime_error("Unknown error unsealing data");
    }
}

KeyManager* OcclumEnclave::get_key_manager()
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        throw std::runtime_error("Enclave not initialized");
    }
    
    return _key_manager.get();
}
