#include "OcclumIntegration.h"
#include "JavaScriptEngine.h"
#include "EnclaveUtils.h"
#include <string>
#include <vector>
#include <memory>
#include <iostream>
#include <sstream>
#include <fstream>
#include <mutex>

// QuickJS headers
#include "QuickJs/QuickJsEngineAdapter.h"

namespace NeoServiceLayer {
namespace Enclave {
namespace OcclumIntegration {

// JavaScript engine instance
static std::unique_ptr<JavaScriptEngine> g_jsEngine;
static std::mutex g_jsMutex;

// Initialize the JavaScript engine
bool InitializeJavaScriptEngine() {
    std::lock_guard<std::mutex> lock(g_jsMutex);
    
    try {
        // Create the JavaScript engine if it doesn't exist
        if (!g_jsEngine) {
            g_jsEngine = std::make_unique<QuickJsEngineAdapter>();
            
            // Initialize the engine
            if (!g_jsEngine->Initialize()) {
                LogError("Failed to initialize JavaScript engine");
                return false;
            }
            
            LogInfo("JavaScript engine initialized successfully");
        }
        
        return true;
    } catch (const std::exception& ex) {
        LogError("Exception in InitializeJavaScriptEngine: %s", ex.what());
        return false;
    } catch (...) {
        LogError("Unknown exception in InitializeJavaScriptEngine");
        return false;
    }
}

// Execute JavaScript code
std::string ExecuteJavaScript(const std::string& code, const std::string& input, 
                             const std::string& secrets, const std::string& functionId, 
                             const std::string& userId) {
    std::lock_guard<std::mutex> lock(g_jsMutex);
    
    try {
        // Initialize the JavaScript engine if it doesn't exist
        if (!g_jsEngine) {
            if (!InitializeJavaScriptEngine()) {
                return "{\"error\":\"Failed to initialize JavaScript engine\"}";
            }
        }
        
        // Create the wrapper script
        std::string wrapperScript = CreateJavaScriptWrapper(code, input, secrets, functionId, userId);
        
        // Execute the script
        std::string result = g_jsEngine->ExecuteScript(wrapperScript);
        
        return result;
    } catch (const std::exception& ex) {
        LogError("Exception in ExecuteJavaScript: %s", ex.what());
        return "{\"error\":\"" + std::string(ex.what()) + "\"}";
    } catch (...) {
        LogError("Unknown exception in ExecuteJavaScript");
        return "{\"error\":\"Unknown error executing JavaScript\"}";
    }
}

// Create a wrapper script for the JavaScript code
std::string CreateJavaScriptWrapper(const std::string& code, const std::string& input, 
                                   const std::string& secrets, const std::string& functionId, 
                                   const std::string& userId) {
    std::stringstream ss;
    
    // Add the user code
    ss << "// User code\n";
    ss << code << "\n\n";
    
    // Add the wrapper code
    ss << "// Wrapper code\n";
    ss << "try {\n";
    ss << "    // Parse the input\n";
    ss << "    const input = " << input << ";\n";
    ss << "    const secrets = " << secrets << ";\n";
    ss << "    const functionId = \"" << functionId << "\";\n";
    ss << "    const userId = \"" << userId << "\";\n\n";
    
    ss << "    // Execute the main function\n";
    ss << "    const result = main(input, secrets, functionId, userId);\n\n";
    
    ss << "    // Return the result\n";
    ss << "    JSON.stringify(result);\n";
    ss << "} catch (error) {\n";
    ss << "    // Return the error\n";
    ss << "    JSON.stringify({ error: error.message, stack: error.stack });\n";
    ss << "}\n";
    
    return ss.str();
}

// Execute JavaScript file
std::string ExecuteJavaScriptFile(const std::string& filePath, const std::string& input, 
                                 const std::string& secrets, const std::string& functionId, 
                                 const std::string& userId) {
    std::lock_guard<std::mutex> lock(g_jsMutex);
    
    try {
        // Initialize the JavaScript engine if it doesn't exist
        if (!g_jsEngine) {
            if (!InitializeJavaScriptEngine()) {
                return "{\"error\":\"Failed to initialize JavaScript engine\"}";
            }
        }
        
        // Read the file
        std::ifstream file(filePath);
        if (!file.is_open()) {
            LogError("Failed to open JavaScript file: %s", filePath.c_str());
            return "{\"error\":\"Failed to open JavaScript file\"}";
        }
        
        std::stringstream buffer;
        buffer << file.rdbuf();
        std::string code = buffer.str();
        
        // Create the wrapper script
        std::string wrapperScript = CreateJavaScriptWrapper(code, input, secrets, functionId, userId);
        
        // Execute the script
        std::string result = g_jsEngine->ExecuteScript(wrapperScript);
        
        return result;
    } catch (const std::exception& ex) {
        LogError("Exception in ExecuteJavaScriptFile: %s", ex.what());
        return "{\"error\":\"" + std::string(ex.what()) + "\"}";
    } catch (...) {
        LogError("Unknown exception in ExecuteJavaScriptFile");
        return "{\"error\":\"Unknown error executing JavaScript file\"}";
    }
}

// Shutdown the JavaScript engine
bool ShutdownJavaScriptEngine() {
    std::lock_guard<std::mutex> lock(g_jsMutex);
    
    try {
        if (g_jsEngine) {
            g_jsEngine->Shutdown();
            g_jsEngine.reset();
            LogInfo("JavaScript engine shut down successfully");
        }
        
        return true;
    } catch (const std::exception& ex) {
        LogError("Exception in ShutdownJavaScriptEngine: %s", ex.what());
        return false;
    } catch (...) {
        LogError("Unknown exception in ShutdownJavaScriptEngine");
        return false;
    }
}

} // namespace OcclumIntegration
} // namespace Enclave
} // namespace NeoServiceLayer
