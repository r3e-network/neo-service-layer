#include "JavaScriptEngineFactory.h"
#include "QuickJs/QuickJsEngineAdapter.h"
#include "NeoServiceLayerEnclave_t.h"
#include <algorithm>
#include <cctype>

std::unique_ptr<IJavaScriptEngine> JavaScriptEngineFactory::CreateEngine(
    EngineType type,
    GasAccounting* gas_accounting,
    SecretManager* secret_manager,
    StorageManager* storage_manager)
{
    switch (type) {
        case EngineType::QuickJs:
            return std::make_unique<QuickJsEngineAdapter>(gas_accounting, secret_manager, storage_manager);
        
        case EngineType::V8:
            // V8 engine not implemented yet
            ocall_print_string("V8 engine not implemented yet, falling back to QuickJs");
            return std::make_unique<QuickJsEngineAdapter>(gas_accounting, secret_manager, storage_manager);
        
        case EngineType::Duktape:
            // Duktape engine not implemented yet
            ocall_print_string("Duktape engine not implemented yet, falling back to QuickJs");
            return std::make_unique<QuickJsEngineAdapter>(gas_accounting, secret_manager, storage_manager);
        
        default:
            // Default to QuickJs
            ocall_print_string("Unknown engine type, falling back to QuickJs");
            return std::make_unique<QuickJsEngineAdapter>(gas_accounting, secret_manager, storage_manager);
    }
}

JavaScriptEngineFactory::EngineType JavaScriptEngineFactory::GetDefaultEngineType()
{
    return EngineType::QuickJs;
}

JavaScriptEngineFactory::EngineType JavaScriptEngineFactory::GetEngineTypeFromString(const std::string& type_str)
{
    // Convert to lowercase for case-insensitive comparison
    std::string lower_type = type_str;
    std::transform(lower_type.begin(), lower_type.end(), lower_type.begin(),
                   [](unsigned char c) { return std::tolower(c); });
    
    if (lower_type == "quickjs" || lower_type == "quick" || lower_type == "qjs") {
        return EngineType::QuickJs;
    } else if (lower_type == "v8") {
        return EngineType::V8;
    } else if (lower_type == "duktape" || lower_type == "duk") {
        return EngineType::Duktape;
    } else {
        // Default to QuickJs
        return EngineType::QuickJs;
    }
}
