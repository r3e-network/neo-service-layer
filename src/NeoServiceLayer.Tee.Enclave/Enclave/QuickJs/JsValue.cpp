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
                JsValue::JsValue(JSContext* ctx, JSValue val)
                    : _ctx(ctx), _val(val)
                {
                }

                JsValue::~JsValue()
                {
                    JS_FreeValue(_ctx, _val);
                }

                JSValue JsValue::GetValue() const
                {
                    return _val;
                }

                std::string JsValue::ToString() const
                {
                    if (IsUndefined() || IsNull())
                    {
                        return "";
                    }

                    JSValue strVal = JS_ToString(_ctx, _val);
                    if (JS_IsException(strVal))
                    {
                        JS_FreeValue(_ctx, strVal);
                        throw std::runtime_error("Failed to convert JavaScript value to string");
                    }

                    const char* str = JS_ToCString(_ctx, strVal);
                    if (!str)
                    {
                        JS_FreeValue(_ctx, strVal);
                        throw std::runtime_error("Failed to convert JavaScript value to C string");
                    }

                    std::string result(str);
                    JS_FreeCString(_ctx, str);
                    JS_FreeValue(_ctx, strVal);
                    return result;
                }

                bool JsValue::ToBoolean() const
                {
                    return JS_ToBool(_ctx, _val) != 0;
                }

                int32_t JsValue::ToInt32() const
                {
                    int32_t result;
                    if (JS_ToInt32(_ctx, &result, _val) < 0)
                    {
                        throw std::runtime_error("Failed to convert JavaScript value to int32");
                    }
                    return result;
                }

                double JsValue::ToDouble() const
                {
                    double result;
                    if (JS_ToFloat64(_ctx, &result, _val) < 0)
                    {
                        throw std::runtime_error("Failed to convert JavaScript value to double");
                    }
                    return result;
                }

                bool JsValue::IsUndefined() const
                {
                    return JS_IsUndefined(_val);
                }

                bool JsValue::IsNull() const
                {
                    return JS_IsNull(_val);
                }

                bool JsValue::IsBoolean() const
                {
                    return JS_IsBool(_val);
                }

                bool JsValue::IsNumber() const
                {
                    return JS_IsNumber(_val);
                }

                bool JsValue::IsString() const
                {
                    return JS_IsString(_val);
                }

                bool JsValue::IsObject() const
                {
                    return JS_IsObject(_val);
                }

                bool JsValue::IsArray() const
                {
                    return JS_IsArray(_ctx, _val);
                }

                bool JsValue::IsFunction() const
                {
                    return JS_IsFunction(_ctx, _val);
                }

                std::shared_ptr<JsValue> JsValue::GetProperty(const std::string& name) const
                {
                    if (!IsObject())
                    {
                        throw std::runtime_error("JavaScript value is not an object");
                    }

                    JSValue prop = JS_GetPropertyStr(_ctx, _val, name.c_str());
                    if (JS_IsException(prop))
                    {
                        JS_FreeValue(_ctx, prop);
                        throw std::runtime_error("Failed to get property '" + name + "'");
                    }

                    return std::make_shared<JsValue>(_ctx, prop);
                }

                void JsValue::SetProperty(const std::string& name, std::shared_ptr<JsValue> value)
                {
                    if (!IsObject())
                    {
                        throw std::runtime_error("JavaScript value is not an object");
                    }

                    if (!value)
                    {
                        throw std::invalid_argument("Value cannot be null");
                    }

                    JSValue val = JS_DupValue(_ctx, value->GetValue());
                    if (JS_SetPropertyStr(_ctx, _val, name.c_str(), val) < 0)
                    {
                        JS_FreeValue(_ctx, val);
                        throw std::runtime_error("Failed to set property '" + name + "'");
                    }
                }

                std::shared_ptr<JsValue> JsValue::GetElement(int32_t index) const
                {
                    if (!IsArray())
                    {
                        throw std::runtime_error("JavaScript value is not an array");
                    }

                    JSValue elem = JS_GetPropertyUint32(_ctx, _val, static_cast<uint32_t>(index));
                    if (JS_IsException(elem))
                    {
                        JS_FreeValue(_ctx, elem);
                        throw std::runtime_error("Failed to get element at index " + std::to_string(index));
                    }

                    return std::make_shared<JsValue>(_ctx, elem);
                }

                void JsValue::SetElement(int32_t index, std::shared_ptr<JsValue> value)
                {
                    if (!IsArray())
                    {
                        throw std::runtime_error("JavaScript value is not an array");
                    }

                    if (!value)
                    {
                        throw std::invalid_argument("Value cannot be null");
                    }

                    JSValue val = JS_DupValue(_ctx, value->GetValue());
                    if (JS_SetPropertyUint32(_ctx, _val, static_cast<uint32_t>(index), val) < 0)
                    {
                        JS_FreeValue(_ctx, val);
                        throw std::runtime_error("Failed to set element at index " + std::to_string(index));
                    }
                }

                int32_t JsValue::GetArrayLength() const
                {
                    if (!IsArray())
                    {
                        throw std::runtime_error("JavaScript value is not an array");
                    }

                    JSValue lengthVal = JS_GetPropertyStr(_ctx, _val, "length");
                    if (JS_IsException(lengthVal))
                    {
                        JS_FreeValue(_ctx, lengthVal);
                        throw std::runtime_error("Failed to get array length");
                    }

                    int32_t length;
                    if (JS_ToInt32(_ctx, &length, lengthVal) < 0)
                    {
                        JS_FreeValue(_ctx, lengthVal);
                        throw std::runtime_error("Failed to convert array length to int32");
                    }

                    JS_FreeValue(_ctx, lengthVal);
                    return length;
                }

                std::shared_ptr<JsValue> JsValue::Call(std::shared_ptr<JsValue> thisObj, const std::vector<std::shared_ptr<JsValue>>& args) const
                {
                    if (!IsFunction())
                    {
                        throw std::runtime_error("JavaScript value is not a function");
                    }

                    JSValue thisVal = thisObj ? thisObj->GetValue() : JS_UNDEFINED;
                    std::vector<JSValue> jsArgs;
                    for (const auto& arg : args)
                    {
                        if (!arg)
                        {
                            throw std::invalid_argument("Argument cannot be null");
                        }
                        jsArgs.push_back(JS_DupValue(_ctx, arg->GetValue()));
                    }

                    JSValue result = JS_Call(_ctx, _val, thisVal, static_cast<int>(jsArgs.size()), jsArgs.data());
                    for (auto& arg : jsArgs)
                    {
                        JS_FreeValue(_ctx, arg);
                    }

                    if (JS_IsException(result))
                    {
                        JS_FreeValue(_ctx, result);
                        throw std::runtime_error("Failed to call JavaScript function");
                    }

                    return std::make_shared<JsValue>(_ctx, result);
                }
            }
        }
    }
}
