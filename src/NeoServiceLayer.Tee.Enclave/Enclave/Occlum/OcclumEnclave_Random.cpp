#include "OcclumEnclave.h"
#include "OcclumIntegration.h"
#include <nlohmann/json.hpp>
#include <sstream>
#include <iomanip>
#include <cstdio>
#include <random>

using json = nlohmann::json;

int OcclumEnclave::generate_random_number(int min, int max)
{
    if (!_initialized && !initialize())
    {
        return min;
    }
    
    try
    {
        secure_log("Generating random number between " + std::to_string(min) + " and " + std::to_string(max));
        
        if (min > max)
        {
            std::swap(min, max);
        }
        
        if (min == max)
        {
            return min;
        }
        
        // Generate random bytes
        std::vector<uint8_t> random_bytes = OcclumIntegration::GenerateRandomBytes(4);
        
        // Convert to integer
        uint32_t random_value = 0;
        for (size_t i = 0; i < random_bytes.size(); i++)
        {
            random_value = (random_value << 8) | random_bytes[i];
        }
        
        // Scale to range
        int range = max - min + 1;
        int result = min + (random_value % range);
        
        secure_log("Generated random number: " + std::to_string(result));
        return result;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error generating random number: " + std::string(ex.what()));
        return min;
    }
    catch (...)
    {
        secure_log("Unknown error generating random number");
        return min;
    }
}

std::vector<uint8_t> OcclumEnclave::generate_random_bytes(size_t length)
{
    if (!_initialized && !initialize())
    {
        return std::vector<uint8_t>(length, 0);
    }
    
    try
    {
        secure_log("Generating " + std::to_string(length) + " random bytes");
        
        std::vector<uint8_t> random_bytes = OcclumIntegration::GenerateRandomBytes(length);
        
        secure_log("Generated " + std::to_string(random_bytes.size()) + " random bytes");
        return random_bytes;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error generating random bytes: " + std::string(ex.what()));
        return std::vector<uint8_t>(length, 0);
    }
    catch (...)
    {
        secure_log("Unknown error generating random bytes");
        return std::vector<uint8_t>(length, 0);
    }
}

std::string OcclumEnclave::generate_uuid()
{
    if (!_initialized && !initialize())
    {
        return "";
    }
    
    try
    {
        secure_log("Generating UUID");
        
        std::string uuid = OcclumIntegration::GenerateUuid();
        
        secure_log("Generated UUID: " + uuid);
        return uuid;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error generating UUID: " + std::string(ex.what()));
        return "";
    }
    catch (...)
    {
        secure_log("Unknown error generating UUID");
        return "";
    }
}

std::vector<uint8_t> OcclumEnclave::generate_attestation()
{
    if (!_initialized && !initialize())
    {
        return {};
    }
    
    try
    {
        secure_log("Generating attestation evidence");
        
        if (!_remote_attestation_manager)
        {
            secure_log("Remote attestation manager not initialized");
            return {};
        }
        
        std::vector<uint8_t> evidence = _remote_attestation_manager->generate_evidence();
        
        secure_log("Generated attestation evidence: " + std::to_string(evidence.size()) + " bytes");
        return evidence;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error generating attestation evidence: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error generating attestation evidence");
        return {};
    }
}

bool OcclumEnclave::verify_attestation(const std::vector<uint8_t>& evidence, const std::vector<uint8_t>& endorsements)
{
    if (!_initialized && !initialize())
    {
        return false;
    }
    
    try
    {
        secure_log("Verifying attestation evidence");
        
        if (!_remote_attestation_manager)
        {
            secure_log("Remote attestation manager not initialized");
            return false;
        }
        
        bool result = _remote_attestation_manager->verify_evidence(evidence, endorsements);
        
        if (result)
        {
            secure_log("Attestation evidence verified successfully");
        }
        else
        {
            secure_log("Attestation evidence verification failed");
        }
        
        return result;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error verifying attestation evidence: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error verifying attestation evidence");
        return false;
    }
}

std::string OcclumEnclave::verify_compliance(const std::string& code, const std::string& user_id,
                                           const std::string& function_id, const std::string& compliance_rules)
{
    if (!_initialized && !initialize())
    {
        return "{\"compliant\":false,\"error\":\"Enclave not initialized\"}";
    }
    
    try
    {
        secure_log("Verifying compliance for function " + function_id + ", user " + user_id);
        
        if (!_compliance_service)
        {
            secure_log("Compliance service not initialized");
            return "{\"compliant\":false,\"error\":\"Compliance service not initialized\"}";
        }
        
        std::string result = _compliance_service->verify_compliance(code, user_id, function_id, compliance_rules);
        
        secure_log("Compliance verification completed");
        return result;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error verifying compliance: " + std::string(ex.what()));
        return "{\"compliant\":false,\"error\":\"" + std::string(ex.what()) + "\"}";
    }
    catch (...)
    {
        secure_log("Unknown error verifying compliance");
        return "{\"compliant\":false,\"error\":\"Unknown error\"}";
    }
}

bool OcclumEnclave::occlum_init(const std::string& instance_dir, const std::string& log_level)
{
    try
    {
        secure_log("Initializing Occlum with instance directory: " + instance_dir + ", log level: " + log_level);
        
        bool result = OcclumIntegration::Initialize(instance_dir.c_str(), log_level.c_str());
        
        if (result)
        {
            secure_log("Occlum initialized successfully");
        }
        else
        {
            secure_log("Failed to initialize Occlum");
        }
        
        return result;
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
    try
    {
        secure_log("Executing command in Occlum: " + path);
        
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
        
        int exit_value = 0;
        bool result = OcclumIntegration::ExecuteCommand(
            path.c_str(),
            argv_cstr.data(),
            env_cstr.data(),
            &exit_value);
        
        if (result)
        {
            secure_log("Command executed successfully, exit value: " + std::to_string(exit_value));
        }
        else
        {
            secure_log("Failed to execute command");
            exit_value = -1;
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
