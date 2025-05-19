#include "QuickJsEngine.h"
#include <stdexcept>

namespace NeoServiceLayer
{
    namespace Tee
    {
        namespace Enclave
        {
            namespace QuickJs
            {
                // Function callback for native functions
                static JSValue NativeFunctionCallback(JSContext* ctx, JSValueConst this_val, int argc, JSValueConst* argv, int magic, JSValue* func_data)
                {
                    // Get the function pointer from the function data
                    void* ptr = JS_GetOpaque(func_data[0], JS_CLASS_OBJECT);
                    if (!ptr)
                    {
                        return JS_EXCEPTION;
                    }

                    auto& func = *static_cast<std::function<std::shared_ptr<JsValue>(const std::vector<std::shared_ptr<JsValue>>&)>*>(ptr);

                    // Convert arguments
                    std::vector<std::shared_ptr<JsValue>> args;
                    for (int i = 0; i < argc; i++)
                    {
                        args.push_back(std::make_shared<JsValue>(ctx, JS_DupValue(ctx, argv[i])));
                    }

                    try
                    {
                        // Call the function
                        auto result = func(args);
                        if (!result)
                        {
                            return JS_UNDEFINED;
                        }
                        return JS_DupValue(ctx, result->GetValue());
                    }
                    catch (const std::exception& ex)
                    {
                        return JS_ThrowInternalError(ctx, "Native function error: %s", ex.what());
                    }
                }

                // Module initialization callback
                static JSValue ModuleInitCallback(JSContext* ctx, JSModuleDef* m)
                {
                    // Get the module name
                    const char* name = JS_GetModuleName(ctx, m);
                    if (!name)
                    {
                        return JS_EXCEPTION;
                    }

                    // Get the module properties
                    JSValue moduleData = JS_GetModuleImportMeta(ctx, m);
                    if (JS_IsException(moduleData))
                    {
                        return JS_EXCEPTION;
                    }

                    // Get the properties object
                    JSValue propsObj = JS_GetPropertyStr(ctx, moduleData, "properties");
                    JS_FreeValue(ctx, moduleData);
                    if (JS_IsException(propsObj))
                    {
                        return JS_EXCEPTION;
                    }

                    // Get the properties
                    JSPropertyEnum* props;
                    uint32_t propCount;
                    if (JS_GetOwnPropertyNames(ctx, &props, &propCount, propsObj, JS_GPN_STRING_MASK | JS_GPN_ENUM_ONLY) < 0)
                    {
                        JS_FreeValue(ctx, propsObj);
                        return JS_EXCEPTION;
                    }

                    // Add the properties to the module
                    for (uint32_t i = 0; i < propCount; i++)
                    {
                        JSValue propName = JS_AtomToString(ctx, props[i].atom);
                        if (JS_IsException(propName))
                        {
                            JS_FreeValue(ctx, propsObj);
                            JS_FreePropertyEnum(ctx, props, propCount);
                            return JS_EXCEPTION;
                        }

                        const char* propNameStr = JS_ToCString(ctx, propName);
                        JS_FreeValue(ctx, propName);
                        if (!propNameStr)
                        {
                            JS_FreeValue(ctx, propsObj);
                            JS_FreePropertyEnum(ctx, props, propCount);
                            return JS_EXCEPTION;
                        }

                        JSValue propValue = JS_GetProperty(ctx, propsObj, props[i].atom);
                        if (JS_IsException(propValue))
                        {
                            JS_FreeCString(ctx, propNameStr);
                            JS_FreeValue(ctx, propsObj);
                            JS_FreePropertyEnum(ctx, props, propCount);
                            return JS_EXCEPTION;
                        }

                        if (JS_SetModuleExport(ctx, m, propNameStr, propValue) < 0)
                        {
                            JS_FreeValue(ctx, propValue);
                            JS_FreeCString(ctx, propNameStr);
                            JS_FreeValue(ctx, propsObj);
                            JS_FreePropertyEnum(ctx, props, propCount);
                            return JS_EXCEPTION;
                        }

                        JS_FreeCString(ctx, propNameStr);
                    }

                    JS_FreeValue(ctx, propsObj);
                    JS_FreePropertyEnum(ctx, props, propCount);
                    return JS_UNDEFINED;
                }

                QuickJsEngine::QuickJsEngine()
                {
                    _rt = JS_NewRuntime();
                    if (!_rt)
                    {
                        throw std::runtime_error("Failed to create JavaScript runtime");
                    }

                    _ctx = JS_NewContext(_rt);
                    if (!_ctx)
                    {
                        JS_FreeRuntime(_rt);
                        throw std::runtime_error("Failed to create JavaScript context");
                    }

                    // Set memory limit
                    JS_SetMemoryLimit(_rt, 16 * 1024 * 1024); // 16 MB

                    // Set maximum stack size
                    JS_SetMaxStackSize(_rt, 1024 * 1024); // 1 MB

                    // Initialize standard modules
                    js_init_module_std(_ctx, "std");
                    js_init_module_os(_ctx, "os");
                }

                QuickJsEngine::~QuickJsEngine()
                {
                    // Free all functions
                    _functions.clear();

                    // Free the context and runtime
                    JS_FreeContext(_ctx);
                    JS_FreeRuntime(_rt);
                }

                std::shared_ptr<JsValue> QuickJsEngine::Evaluate(const std::string& code, const std::string& filename)
                {
                    JSValue result = JS_Eval(_ctx, code.c_str(), code.length(), filename.c_str(), JS_EVAL_TYPE_GLOBAL);
                    if (JS_IsException(result))
                    {
                        JSValue exception = JS_GetException(_ctx);
                        std::string errorMessage = "JavaScript error: ";
                        const char* str = JS_ToCString(_ctx, exception);
                        if (str)
                        {
                            errorMessage += str;
                            JS_FreeCString(_ctx, str);
                        }
                        JS_FreeValue(_ctx, exception);
                        JS_FreeValue(_ctx, result);
                        throw std::runtime_error(errorMessage);
                    }

                    return std::make_shared<JsValue>(_ctx, result);
                }

                std::shared_ptr<JsValue> QuickJsEngine::CreateValue(const std::string& value)
                {
                    JSValue val = JS_NewString(_ctx, value.c_str());
                    if (JS_IsException(val))
                    {
                        JS_FreeValue(_ctx, val);
                        throw std::runtime_error("Failed to create JavaScript string");
                    }

                    return std::make_shared<JsValue>(_ctx, val);
                }

                std::shared_ptr<JsValue> QuickJsEngine::CreateValue(bool value)
                {
                    JSValue val = JS_NewBool(_ctx, value);
                    return std::make_shared<JsValue>(_ctx, val);
                }

                std::shared_ptr<JsValue> QuickJsEngine::CreateValue(int32_t value)
                {
                    JSValue val = JS_NewInt32(_ctx, value);
                    return std::make_shared<JsValue>(_ctx, val);
                }

                std::shared_ptr<JsValue> QuickJsEngine::CreateValue(double value)
                {
                    JSValue val = JS_NewFloat64(_ctx, value);
                    return std::make_shared<JsValue>(_ctx, val);
                }

                std::shared_ptr<JsValue> QuickJsEngine::CreateArray(const std::vector<std::shared_ptr<JsValue>>& values)
                {
                    JSValue array = JS_NewArray(_ctx);
                    if (JS_IsException(array))
                    {
                        JS_FreeValue(_ctx, array);
                        throw std::runtime_error("Failed to create JavaScript array");
                    }

                    for (size_t i = 0; i < values.size(); i++)
                    {
                        if (!values[i])
                        {
                            JS_FreeValue(_ctx, array);
                            throw std::invalid_argument("Array element cannot be null");
                        }

                        JSValue val = JS_DupValue(_ctx, values[i]->GetValue());
                        if (JS_SetPropertyUint32(_ctx, array, static_cast<uint32_t>(i), val) < 0)
                        {
                            JS_FreeValue(_ctx, val);
                            JS_FreeValue(_ctx, array);
                            throw std::runtime_error("Failed to set array element");
                        }
                    }

                    return std::make_shared<JsValue>(_ctx, array);
                }

                std::shared_ptr<JsValue> QuickJsEngine::CreateObject(const std::map<std::string, std::shared_ptr<JsValue>>& properties)
                {
                    JSValue obj = JS_NewObject(_ctx);
                    if (JS_IsException(obj))
                    {
                        JS_FreeValue(_ctx, obj);
                        throw std::runtime_error("Failed to create JavaScript object");
                    }

                    for (const auto& prop : properties)
                    {
                        if (!prop.second)
                        {
                            JS_FreeValue(_ctx, obj);
                            throw std::invalid_argument("Property value cannot be null");
                        }

                        JSValue val = JS_DupValue(_ctx, prop.second->GetValue());
                        if (JS_SetPropertyStr(_ctx, obj, prop.first.c_str(), val) < 0)
                        {
                            JS_FreeValue(_ctx, val);
                            JS_FreeValue(_ctx, obj);
                            throw std::runtime_error("Failed to set object property");
                        }
                    }

                    return std::make_shared<JsValue>(_ctx, obj);
                }

                std::shared_ptr<JsValue> QuickJsEngine::CreateFunction(std::function<std::shared_ptr<JsValue>(const std::vector<std::shared_ptr<JsValue>>&)> func)
                {
                    // Create a function data object to store the function pointer
                    JSValue funcData = JS_NewObject(_ctx);
                    if (JS_IsException(funcData))
                    {
                        JS_FreeValue(_ctx, funcData);
                        throw std::runtime_error("Failed to create JavaScript function data");
                    }

                    // Store the function pointer in the function data
                    auto funcPtr = new std::function<std::shared_ptr<JsValue>(const std::vector<std::shared_ptr<JsValue>>&)>(func);
                    JS_SetOpaque(funcData, funcPtr);

                    // Create the function
                    JSValue jsFunc = JS_NewCFunctionData(_ctx, NativeFunctionCallback, 0, 0, 1, &funcData);
                    JS_FreeValue(_ctx, funcData);
                    if (JS_IsException(jsFunc))
                    {
                        delete funcPtr;
                        JS_FreeValue(_ctx, jsFunc);
                        throw std::runtime_error("Failed to create JavaScript function");
                    }

                    return std::make_shared<JsValue>(_ctx, jsFunc);
                }

                std::shared_ptr<JsValue> QuickJsEngine::GetGlobalObject()
                {
                    JSValue global = JS_GetGlobalObject(_ctx);
                    return std::make_shared<JsValue>(_ctx, global);
                }

                void QuickJsEngine::RegisterFunction(const std::string& name, std::function<std::shared_ptr<JsValue>(const std::vector<std::shared_ptr<JsValue>>&)> func)
                {
                    // Store the function
                    _functions[name] = func;

                    // Create the function
                    auto jsFunc = CreateFunction(func);

                    // Add the function to the global object
                    auto global = GetGlobalObject();
                    global->SetProperty(name, jsFunc);
                }

                void QuickJsEngine::RegisterModule(const std::string& name, const std::map<std::string, std::shared_ptr<JsValue>>& properties)
                {
                    // Create the module definition
                    JSModuleDef* m = JS_NewCModule(_ctx, name.c_str(), ModuleInitCallback);
                    if (!m)
                    {
                        throw std::runtime_error("Failed to create JavaScript module");
                    }

                    // Store the properties in the module import meta
                    JSValue moduleData = JS_GetImportMeta(_ctx, m);
                    if (JS_IsException(moduleData))
                    {
                        throw std::runtime_error("Failed to get module import meta");
                    }

                    // Create the properties object
                    JSValue propsObj = JS_NewObject(_ctx);
                    if (JS_IsException(propsObj))
                    {
                        JS_FreeValue(_ctx, moduleData);
                        throw std::runtime_error("Failed to create module properties object");
                    }

                    // Add the properties to the properties object
                    for (const auto& prop : properties)
                    {
                        if (!prop.second)
                        {
                            JS_FreeValue(_ctx, propsObj);
                            JS_FreeValue(_ctx, moduleData);
                            throw std::invalid_argument("Module property value cannot be null");
                        }

                        JSValue val = JS_DupValue(_ctx, prop.second->GetValue());
                        if (JS_SetPropertyStr(_ctx, propsObj, prop.first.c_str(), val) < 0)
                        {
                            JS_FreeValue(_ctx, val);
                            JS_FreeValue(_ctx, propsObj);
                            JS_FreeValue(_ctx, moduleData);
                            throw std::runtime_error("Failed to set module property");
                        }
                    }

                    // Set the properties object in the module import meta
                    if (JS_SetPropertyStr(_ctx, moduleData, "properties", propsObj) < 0)
                    {
                        JS_FreeValue(_ctx, propsObj);
                        JS_FreeValue(_ctx, moduleData);
                        throw std::runtime_error("Failed to set module properties");
                    }

                    JS_FreeValue(_ctx, moduleData);
                }

                void QuickJsEngine::CollectGarbage()
                {
                    JS_RunGC(_rt);
                }
            }
        }
    }
}
