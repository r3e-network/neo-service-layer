#pragma once

#include <string>
#include <vector>
#include <map>
#include <functional>
#include <memory>
#include "quickjs.h"

namespace NeoServiceLayer
{
    namespace Tee
    {
        namespace Enclave
        {
            namespace QuickJs
            {
                /**
                 * @brief Represents a JavaScript value.
                 */
                class JsValue
                {
                public:
                    /**
                     * @brief Creates a new JsValue.
                     * @param ctx The JavaScript context.
                     * @param val The JavaScript value.
                     */
                    JsValue(JSContext* ctx, JSValue val);

                    /**
                     * @brief Destructor.
                     */
                    ~JsValue();

                    /**
                     * @brief Gets the JavaScript value.
                     * @return The JavaScript value.
                     */
                    JSValue GetValue() const;

                    /**
                     * @brief Converts the JavaScript value to a string.
                     * @return The string representation of the JavaScript value.
                     */
                    std::string ToString() const;

                    /**
                     * @brief Converts the JavaScript value to a boolean.
                     * @return The boolean representation of the JavaScript value.
                     */
                    bool ToBoolean() const;

                    /**
                     * @brief Converts the JavaScript value to an integer.
                     * @return The integer representation of the JavaScript value.
                     */
                    int32_t ToInt32() const;

                    /**
                     * @brief Converts the JavaScript value to a double.
                     * @return The double representation of the JavaScript value.
                     */
                    double ToDouble() const;

                    /**
                     * @brief Checks if the JavaScript value is undefined.
                     * @return True if the JavaScript value is undefined, false otherwise.
                     */
                    bool IsUndefined() const;

                    /**
                     * @brief Checks if the JavaScript value is null.
                     * @return True if the JavaScript value is null, false otherwise.
                     */
                    bool IsNull() const;

                    /**
                     * @brief Checks if the JavaScript value is a boolean.
                     * @return True if the JavaScript value is a boolean, false otherwise.
                     */
                    bool IsBoolean() const;

                    /**
                     * @brief Checks if the JavaScript value is a number.
                     * @return True if the JavaScript value is a number, false otherwise.
                     */
                    bool IsNumber() const;

                    /**
                     * @brief Checks if the JavaScript value is a string.
                     * @return True if the JavaScript value is a string, false otherwise.
                     */
                    bool IsString() const;

                    /**
                     * @brief Checks if the JavaScript value is an object.
                     * @return True if the JavaScript value is an object, false otherwise.
                     */
                    bool IsObject() const;

                    /**
                     * @brief Checks if the JavaScript value is an array.
                     * @return True if the JavaScript value is an array, false otherwise.
                     */
                    bool IsArray() const;

                    /**
                     * @brief Checks if the JavaScript value is a function.
                     * @return True if the JavaScript value is a function, false otherwise.
                     */
                    bool IsFunction() const;

                    /**
                     * @brief Gets a property from the JavaScript object.
                     * @param name The property name.
                     * @return The property value.
                     */
                    std::shared_ptr<JsValue> GetProperty(const std::string& name) const;

                    /**
                     * @brief Sets a property on the JavaScript object.
                     * @param name The property name.
                     * @param value The property value.
                     */
                    void SetProperty(const std::string& name, std::shared_ptr<JsValue> value);

                    /**
                     * @brief Gets an element from the JavaScript array.
                     * @param index The element index.
                     * @return The element value.
                     */
                    std::shared_ptr<JsValue> GetElement(int32_t index) const;

                    /**
                     * @brief Sets an element in the JavaScript array.
                     * @param index The element index.
                     * @param value The element value.
                     */
                    void SetElement(int32_t index, std::shared_ptr<JsValue> value);

                    /**
                     * @brief Gets the length of the JavaScript array.
                     * @return The length of the JavaScript array.
                     */
                    int32_t GetArrayLength() const;

                    /**
                     * @brief Calls the JavaScript function.
                     * @param thisObj The this object.
                     * @param args The function arguments.
                     * @return The function result.
                     */
                    std::shared_ptr<JsValue> Call(std::shared_ptr<JsValue> thisObj, const std::vector<std::shared_ptr<JsValue>>& args) const;

                private:
                    JSContext* _ctx;
                    JSValue _val;
                };

                /**
                 * @brief Represents a JavaScript engine.
                 */
                class QuickJsEngine
                {
                public:
                    /**
                     * @brief Creates a new QuickJsEngine.
                     */
                    QuickJsEngine();

                    /**
                     * @brief Destructor.
                     */
                    ~QuickJsEngine();

                    /**
                     * @brief Evaluates JavaScript code.
                     * @param code The JavaScript code.
                     * @param filename The filename for error reporting.
                     * @return The result of the evaluation.
                     */
                    std::shared_ptr<JsValue> Evaluate(const std::string& code, const std::string& filename = "<eval>");

                    /**
                     * @brief Creates a new JavaScript value.
                     * @param value The value to create.
                     * @return The JavaScript value.
                     */
                    std::shared_ptr<JsValue> CreateValue(const std::string& value);

                    /**
                     * @brief Creates a new JavaScript value.
                     * @param value The value to create.
                     * @return The JavaScript value.
                     */
                    std::shared_ptr<JsValue> CreateValue(bool value);

                    /**
                     * @brief Creates a new JavaScript value.
                     * @param value The value to create.
                     * @return The JavaScript value.
                     */
                    std::shared_ptr<JsValue> CreateValue(int32_t value);

                    /**
                     * @brief Creates a new JavaScript value.
                     * @param value The value to create.
                     * @return The JavaScript value.
                     */
                    std::shared_ptr<JsValue> CreateValue(double value);

                    /**
                     * @brief Creates a new JavaScript array.
                     * @param values The array values.
                     * @return The JavaScript array.
                     */
                    std::shared_ptr<JsValue> CreateArray(const std::vector<std::shared_ptr<JsValue>>& values);

                    /**
                     * @brief Creates a new JavaScript object.
                     * @param properties The object properties.
                     * @return The JavaScript object.
                     */
                    std::shared_ptr<JsValue> CreateObject(const std::map<std::string, std::shared_ptr<JsValue>>& properties);

                    /**
                     * @brief Creates a new JavaScript function.
                     * @param func The function implementation.
                     * @return The JavaScript function.
                     */
                    std::shared_ptr<JsValue> CreateFunction(std::function<std::shared_ptr<JsValue>(const std::vector<std::shared_ptr<JsValue>>&)> func);

                    /**
                     * @brief Gets the global object.
                     * @return The global object.
                     */
                    std::shared_ptr<JsValue> GetGlobalObject();

                    /**
                     * @brief Registers a native function.
                     * @param name The function name.
                     * @param func The function implementation.
                     */
                    void RegisterFunction(const std::string& name, std::function<std::shared_ptr<JsValue>(const std::vector<std::shared_ptr<JsValue>>&)> func);

                    /**
                     * @brief Registers a native module.
                     * @param name The module name.
                     * @param properties The module properties.
                     */
                    void RegisterModule(const std::string& name, const std::map<std::string, std::shared_ptr<JsValue>>& properties);

                    /**
                     * @brief Collects garbage.
                     */
                    void CollectGarbage();

                private:
                    JSRuntime* _rt;
                    JSContext* _ctx;
                    std::map<std::string, std::function<std::shared_ptr<JsValue>(const std::vector<std::shared_ptr<JsValue>>&)>> _functions;
                };
            }
        }
    }
}
