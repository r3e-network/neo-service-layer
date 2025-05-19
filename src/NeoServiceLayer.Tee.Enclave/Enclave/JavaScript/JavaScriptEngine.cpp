#include "JavaScriptEngine.h"
#include "StorageManager.h"
#include "NeoServiceLayerEnclave_t.h"
#include "../Core/Logger.h"
#include "../Core/MetricsCollector.h"
#include <sstream>
#include <iomanip>
#include <openssl/sha.h>
#include <chrono>

// This file contains common implementations for JavaScript engine functionality
// that can be shared across different engine implementations.

// Helper function to calculate SHA-256 hash
std::string calculate_sha256(const std::string& data)
{
    unsigned char hash[SHA256_DIGEST_LENGTH];
    SHA256_CTX sha256;
    SHA256_Init(&sha256);
    SHA256_Update(&sha256, data.c_str(), data.length());
    SHA256_Final(hash, &sha256);

    std::stringstream ss;
    for (int i = 0; i < SHA256_DIGEST_LENGTH; i++)
    {
        ss << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(hash[i]);
    }

    return ss.str();
}

// Helper function to log JavaScript errors
void log_js_error(const std::string& error)
{
    // Use the secure Logger instead of ocall_print_string
    NeoServiceLayer::Enclave::Logger::getInstance().error("JavaScriptEngine", "JavaScript error: " + error);
}

// Helper function to wrap JavaScript code with enhanced error handling
std::string wrap_js_code(const std::string& code)
{
    return R"(
        try {
            )" + code + R"(
        } catch (e) {
            // Enhanced error handling with more detailed information
            return {
                error: e.message || 'Unknown error',
                stack: e.stack || '',
                type: e.name || 'Error',
                code: e.code || 0,
                lineNumber: e.lineNumber || 0,
                columnNumber: e.columnNumber || 0,
                fileName: e.fileName || ''
            };
        }
    )";
}

// Helper function to parse JavaScript result with enhanced error handling
bool parse_js_result(const std::string& result, std::string& output, std::string& error)
{
    try
    {
        // Check if the result is a JSON string
        if (result.empty())
        {
            error = "Empty result from JavaScript execution";
            return false;
        }

        if (result[0] != '{')
        {
            // Not a JSON object, return as-is
            output = result;
            return true;
        }

        // Check if the result contains an error
        if (result.find("\"error\"") != std::string::npos)
        {
            // Use nlohmann/json to properly parse the error object
            try {
                nlohmann::json error_json = nlohmann::json::parse(result);

                // Build a detailed error message
                std::stringstream error_ss;

                // Add the main error message
                if (error_json.contains("error") && !error_json["error"].is_null()) {
                    error_ss << error_json["error"].get<std::string>();
                } else {
                    error_ss << "Unknown error";
                }

                // Add error type if available
                if (error_json.contains("type") && !error_json["type"].is_null()) {
                    error_ss << " [Type: " << error_json["type"].get<std::string>() << "]";
                }

                // Add line and column information if available
                if (error_json.contains("lineNumber") && error_json.contains("columnNumber") &&
                    !error_json["lineNumber"].is_null() && !error_json["columnNumber"].is_null()) {
                    error_ss << " at line " << error_json["lineNumber"].get<int>()
                             << ", column " << error_json["columnNumber"].get<int>();
                }

                // Add file name if available
                if (error_json.contains("fileName") && !error_json["fileName"].is_null() &&
                    !error_json["fileName"].get<std::string>().empty()) {
                    error_ss << " in " << error_json["fileName"].get<std::string>();
                }

                // Add stack trace if available
                if (error_json.contains("stack") && !error_json["stack"].is_null() &&
                    !error_json["stack"].get<std::string>().empty()) {
                    error_ss << "\nStack trace: " << error_json["stack"].get<std::string>();
                }

                error = error_ss.str();
                return false;
            }
            catch (const nlohmann::json::exception& json_ex) {
                // Fallback to simple error extraction if JSON parsing fails
                size_t error_start = result.find("\"error\"");
                size_t value_start = result.find(":", error_start);
                if (value_start != std::string::npos)
                {
                    value_start++;
                    size_t value_end = result.find(",", value_start);
                    if (value_end == std::string::npos)
                    {
                        value_end = result.find("}", value_start);
                    }

                    if (value_end != std::string::npos)
                    {
                        std::string error_value = result.substr(value_start, value_end - value_start);

                        // Remove quotes and whitespace
                        error_value.erase(0, error_value.find_first_not_of(" \t\n\r\""));
                        error_value.erase(error_value.find_last_not_of(" \t\n\r\"") + 1);

                        error = error_value + " (JSON parsing failed: " + json_ex.what() + ")";
                        return false;
                    }
                }

                error = "Unknown error (JSON parsing failed: " + std::string(json_ex.what()) + ")";
                return false;
            }
        }

        // No error found, return the result
        output = result;
        return true;
    }
    catch (const std::exception& ex)
    {
        error = std::string("Exception during result parsing: ") + ex.what();
        return false;
    }
    catch (...)
    {
        error = "Unknown exception during result parsing";
        return false;
    }
}

// Simple JavaScript engine implementation for testing
class SimpleJavaScriptEngine : public IJavaScriptEngine {
public:
    SimpleJavaScriptEngine(
        GasAccounting* gas_accounting,
        SecretManager* secret_manager,
        StorageManager* storage_manager)
        : _gas_accounting(gas_accounting),
          _secret_manager(secret_manager),
          _storage_manager(storage_manager),
          _gas_used(0),
          _initialized(false) {

        // Register metrics for precompilation
        _precompile_count = REGISTER_COUNTER("javascript_precompile_count");
        _precompile_cache_hits = REGISTER_COUNTER("javascript_precompile_cache_hits");
        _precompile_cache_misses = REGISTER_COUNTER("javascript_precompile_cache_misses");
        _precompile_cache_size = REGISTER_GAUGE("javascript_precompile_cache_size");
    }

    bool initialize() override {
        _initialized = true;
        return true;
    }

    std::string execute(
        const std::string& code,
        const std::string& input_json,
        const std::string& secrets_json,
        const std::string& function_id,
        const std::string& user_id,
        uint64_t& gas_used) override {

        if (!_initialized) {
            throw std::runtime_error("JavaScript engine not initialized");
        }

        // Get metrics
        auto js_execution_count = REGISTER_COUNTER("javascript_execution_count");
        auto js_execution_time = REGISTER_HISTOGRAM("javascript_execution_time_ms");
        auto js_execution_errors = REGISTER_COUNTER("javascript_execution_errors");
        auto js_gas_used = REGISTER_HISTOGRAM("javascript_gas_used");
        auto js_code_size = REGISTER_HISTOGRAM("javascript_code_size_bytes");
        auto js_input_size = REGISTER_HISTOGRAM("javascript_input_size_bytes");
        auto js_secrets_size = REGISTER_HISTOGRAM("javascript_secrets_size_bytes");

        // Record code and input sizes
        js_code_size->observe(static_cast<double>(code.size()));
        js_input_size->observe(static_cast<double>(input_json.size()));
        js_secrets_size->observe(static_cast<double>(secrets_json.size()));

        // Increment execution count
        js_execution_count->increment();

        // Start execution timer
        auto start_time = std::chrono::high_resolution_clock::now();

        try {
            // Validate inputs
            if (code.empty()) {
                throw std::invalid_argument("JavaScript code cannot be empty");
            }

            // Validate JSON inputs
            try {
                if (!input_json.empty()) {
                    nlohmann::json::parse(input_json);
                }
                if (!secrets_json.empty()) {
                    nlohmann::json::parse(secrets_json);
                }
            }
            catch (const nlohmann::json::exception& ex) {
                throw std::invalid_argument(std::string("Invalid JSON input: ") + ex.what());
            }

            // In a real implementation, this would execute the JavaScript code
            // For now, we'll just simulate execution with enhanced error handling

            // Use some gas
            if (_gas_accounting) {
                _gas_accounting->use_gas(1000);
                _gas_used = 1000;
            }
            else {
                _gas_used = 1000; // Default if gas accounting is not available
            }

            gas_used = _gas_used;

            // Record gas usage
            js_gas_used->observe(static_cast<double>(gas_used));

            // Calculate execution time
            auto end_time = std::chrono::high_resolution_clock::now();
            auto execution_time_ms = std::chrono::duration_cast<std::chrono::milliseconds>(end_time - start_time).count();

            // Record execution time
            js_execution_time->observe(static_cast<double>(execution_time_ms));

            // Return a mock result with more detailed information
            nlohmann::json result_json = {
                {"result", "Executed JavaScript code"},
                {"function_id", function_id},
                {"user_id", user_id},
                {"gas_used", gas_used},
                {"execution_time_ms", execution_time_ms},
                {"memory_used_bytes", 1024}, // Mock memory usage
                {"status", "success"}
            };

            // Log successful execution
            NeoServiceLayer::Enclave::Logger::getInstance().info(
                "JavaScriptEngine",
                "Successfully executed JavaScript function: " + function_id +
                " for user: " + user_id +
                " (execution time: " + std::to_string(execution_time_ms) + "ms, " +
                "gas used: " + std::to_string(gas_used) + ")"
            );

            return result_json.dump();
        }
        catch (const std::exception& ex) {
            // Log the error
            log_js_error(ex.what());

            // Increment error count
            js_execution_errors->increment();

            // Calculate execution time
            auto end_time = std::chrono::high_resolution_clock::now();
            auto execution_time_ms = std::chrono::duration_cast<std::chrono::milliseconds>(end_time - start_time).count();

            // Record execution time
            js_execution_time->observe(static_cast<double>(execution_time_ms));

            // Return a structured error response
            nlohmann::json error_json = {
                {"error", ex.what()},
                {"function_id", function_id},
                {"user_id", user_id},
                {"gas_used", _gas_used},
                {"execution_time_ms", execution_time_ms},
                {"status", "error"}
            };

            return error_json.dump();
        }
        catch (...) {
            // Log the unknown error
            log_js_error("Unknown error during JavaScript execution");

            // Increment error count
            js_execution_errors->increment();

            // Calculate execution time
            auto end_time = std::chrono::high_resolution_clock::now();
            auto execution_time_ms = std::chrono::duration_cast<std::chrono::milliseconds>(end_time - start_time).count();

            // Record execution time
            js_execution_time->observe(static_cast<double>(execution_time_ms));

            // Return a structured error response for unknown errors
            nlohmann::json error_json = {
                {"error", "Unknown error during JavaScript execution"},
                {"function_id", function_id},
                {"user_id", user_id},
                {"gas_used", _gas_used},
                {"execution_time_ms", execution_time_ms},
                {"status", "error"}
            };

            return error_json.dump();
        }
    }

    bool verify_code_hash(
        const std::string& code,
        const std::string& hash) override {

        std::string calculated_hash = calculate_code_hash(code);
        return calculated_hash == hash;
    }

    std::string calculate_code_hash(
        const std::string& code) override {

        // Use the helper function to calculate the hash
        return calculate_sha256(code);
    }

    void reset_gas_used() override {
        _gas_used = 0;
    }

    uint64_t get_gas_used() const override {
        return _gas_used;
    }

    bool precompile(
        const std::string& code,
        const std::string& function_id) override {

        if (!_initialized) {
            throw std::runtime_error("JavaScript engine not initialized");
        }

        try {
            // Validate inputs
            if (code.empty()) {
                throw std::invalid_argument("JavaScript code cannot be empty");
            }

            if (function_id.empty()) {
                throw std::invalid_argument("Function ID cannot be empty");
            }

            // Increment precompile count
            _precompile_count->increment();

            // In a real implementation, this would precompile the JavaScript code
            // For now, we'll just store the code in the cache

            std::lock_guard<std::mutex> lock(_mutex);

            // Store the code in the cache
            _precompiled_code[function_id] = code;

            // Update cache size metric
            _precompile_cache_size->set(static_cast<double>(_precompiled_code.size()));

            // Log successful precompilation
            NeoServiceLayer::Enclave::Logger::getInstance().info(
                "JavaScriptEngine",
                "Successfully precompiled JavaScript function: " + function_id +
                " (code size: " + std::to_string(code.size()) + " bytes)"
            );

            return true;
        }
        catch (const std::exception& ex) {
            // Log the error
            log_js_error(std::string("Error precompiling JavaScript: ") + ex.what());
            return false;
        }
        catch (...) {
            // Log the unknown error
            log_js_error("Unknown error precompiling JavaScript");
            return false;
        }
    }

    bool is_precompiled(
        const std::string& function_id) const override {

        std::lock_guard<std::mutex> lock(_mutex);
        return _precompiled_code.find(function_id) != _precompiled_code.end();
    }

    std::string execute_precompiled(
        const std::string& function_id,
        const std::string& input_json,
        const std::string& secrets_json,
        const std::string& user_id,
        uint64_t& gas_used) override {

        if (!_initialized) {
            throw std::runtime_error("JavaScript engine not initialized");
        }

        // Get metrics
        auto js_execution_count = REGISTER_COUNTER("javascript_execution_count");
        auto js_execution_time = REGISTER_HISTOGRAM("javascript_execution_time_ms");
        auto js_execution_errors = REGISTER_COUNTER("javascript_execution_errors");
        auto js_gas_used = REGISTER_HISTOGRAM("javascript_gas_used");

        // Increment execution count
        js_execution_count->increment();

        // Start execution timer
        auto start_time = std::chrono::high_resolution_clock::now();

        try {
            // Check if the function is precompiled
            std::string code;
            {
                std::lock_guard<std::mutex> lock(_mutex);
                auto it = _precompiled_code.find(function_id);
                if (it == _precompiled_code.end()) {
                    // Increment cache miss count
                    _precompile_cache_misses->increment();

                    throw std::runtime_error("Function not precompiled: " + function_id);
                }

                // Increment cache hit count
                _precompile_cache_hits->increment();

                // Get the code
                code = it->second;
            }

            // Execute the code
            return execute(code, input_json, secrets_json, function_id, user_id, gas_used);
        }
        catch (const std::exception& ex) {
            // Log the error
            log_js_error(ex.what());

            // Increment error count
            js_execution_errors->increment();

            // Calculate execution time
            auto end_time = std::chrono::high_resolution_clock::now();
            auto execution_time_ms = std::chrono::duration_cast<std::chrono::milliseconds>(end_time - start_time).count();

            // Record execution time
            js_execution_time->observe(static_cast<double>(execution_time_ms));

            // Return a structured error response
            nlohmann::json error_json = {
                {"error", ex.what()},
                {"function_id", function_id},
                {"user_id", user_id},
                {"gas_used", _gas_used},
                {"execution_time_ms", execution_time_ms},
                {"status", "error"}
            };

            return error_json.dump();
        }
        catch (...) {
            // Log the unknown error
            log_js_error("Unknown error executing precompiled JavaScript");

            // Increment error count
            js_execution_errors->increment();

            // Calculate execution time
            auto end_time = std::chrono::high_resolution_clock::now();
            auto execution_time_ms = std::chrono::duration_cast<std::chrono::milliseconds>(end_time - start_time).count();

            // Record execution time
            js_execution_time->observe(static_cast<double>(execution_time_ms));

            // Return a structured error response for unknown errors
            nlohmann::json error_json = {
                {"error", "Unknown error executing precompiled JavaScript"},
                {"function_id", function_id},
                {"user_id", user_id},
                {"gas_used", _gas_used},
                {"execution_time_ms", execution_time_ms},
                {"status", "error"}
            };

            return error_json.dump();
        }
    }

    void clear_precompiled_cache() override {
        std::lock_guard<std::mutex> lock(_mutex);

        // Log cache clear
        NeoServiceLayer::Enclave::Logger::getInstance().info(
            "JavaScriptEngine",
            "Clearing precompiled JavaScript cache (size: " +
            std::to_string(_precompiled_code.size()) + ")"
        );

        // Clear the cache
        _precompiled_code.clear();

        // Update cache size metric
        _precompile_cache_size->set(0.0);
    }

private:
    GasAccounting* _gas_accounting;
    SecretManager* _secret_manager;
    StorageManager* _storage_manager;
    uint64_t _gas_used;
    bool _initialized;

    // Precompiled code cache
    std::unordered_map<std::string, std::string> _precompiled_code;
    mutable std::mutex _mutex;

    // Metrics
    NeoServiceLayer::Enclave::MetricValue* _precompile_count;
    NeoServiceLayer::Enclave::MetricValue* _precompile_cache_hits;
    NeoServiceLayer::Enclave::MetricValue* _precompile_cache_misses;
    NeoServiceLayer::Enclave::MetricValue* _precompile_cache_size;
};
