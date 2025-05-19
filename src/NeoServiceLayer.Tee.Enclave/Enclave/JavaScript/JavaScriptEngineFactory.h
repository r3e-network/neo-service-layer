#pragma once

#include "JavaScriptEngine.h"
#include "StorageManager.h"
#include "GasAccounting.h"
#include "SecretManager.h"
#include <memory>
#include <string>

/**
 * @brief Factory for creating JavaScript engine instances
 */
class JavaScriptEngineFactory {
public:
    /**
     * @brief Engine types supported by the factory
     */
    enum class EngineType {
        QuickJs,
        V8,
        Duktape
    };

    /**
     * @brief Create a JavaScript engine instance
     * @param type The type of engine to create
     * @param gas_accounting The gas accounting manager
     * @param secret_manager The secret manager
     * @param storage_manager The storage manager
     * @return A unique pointer to the created engine
     */
    static std::unique_ptr<IJavaScriptEngine> CreateEngine(
        EngineType type,
        GasAccounting* gas_accounting,
        SecretManager* secret_manager,
        StorageManager* storage_manager);

    /**
     * @brief Get the default engine type
     * @return The default engine type
     */
    static EngineType GetDefaultEngineType();

    /**
     * @brief Get the engine type from a string
     * @param type_str The engine type as a string
     * @return The engine type
     */
    static EngineType GetEngineTypeFromString(const std::string& type_str);
};
