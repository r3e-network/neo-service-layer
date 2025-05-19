#include "QuickJsExecutor.h"
#include <stdexcept>

namespace NeoServiceLayer
{
    namespace Tee
    {
        namespace Enclave
        {
            namespace QuickJs
            {
                QuickJsExecutor::QuickJsExecutor()
                    : _initialized(false)
                {
                    _engine = std::make_shared<QuickJsEngine>();
                    _api = std::make_shared<JsApi>(_engine);
                }

                QuickJsExecutor::~QuickJsExecutor()
                {
                    _api.reset();
                    _engine.reset();
                }

                void QuickJsExecutor::SetLogCallback(std::function<void(const std::string&)> callback)
                {
                    _logCallback = callback;
                }

                void QuickJsExecutor::SetStorageCallbacks(
                    std::function<std::string(const std::string&)> getCallback,
                    std::function<void(const std::string&, const std::string&)> setCallback,
                    std::function<void(const std::string&)> removeCallback,
                    std::function<void()> clearCallback)
                {
                    _getStorageCallback = getCallback;
                    _setStorageCallback = setCallback;
                    _removeStorageCallback = removeCallback;
                    _clearStorageCallback = clearCallback;
                }

                void QuickJsExecutor::SetCryptoCallbacks(
                    std::function<std::string(int)> randomBytesCallback,
                    std::function<std::string(const std::string&)> sha256Callback,
                    std::function<std::string(const std::string&, const std::string&)> signCallback,
                    std::function<bool(const std::string&, const std::string&, const std::string&)> verifyCallback)
                {
                    _randomBytesCallback = randomBytesCallback;
                    _sha256Callback = sha256Callback;
                    _signCallback = signCallback;
                    _verifyCallback = verifyCallback;
                }

                void QuickJsExecutor::SetGasCallbacks(
                    std::function<int64_t()> getGasCallback,
                    std::function<bool(int64_t)> useGasCallback)
                {
                    _getGasCallback = getGasCallback;
                    _useGasCallback = useGasCallback;
                }

                void QuickJsExecutor::SetSecretsCallbacks(
                    std::function<std::string(const std::string&)> getSecretCallback,
                    std::function<void(const std::string&, const std::string&)> setSecretCallback,
                    std::function<void(const std::string&)> removeSecretCallback)
                {
                    _getSecretCallback = getSecretCallback;
                    _setSecretCallback = setSecretCallback;
                    _removeSecretCallback = removeSecretCallback;
                }

                void QuickJsExecutor::SetBlockchainCallbacks(
                    std::function<void(const std::string&, const std::string&)> callbackCallback)
                {
                    _callbackCallback = callbackCallback;
                }

                void QuickJsExecutor::InitializeApi()
                {
                    if (_initialized)
                    {
                        return;
                    }

                    // Register the console API
                    if (_logCallback)
                    {
                        _api->RegisterConsoleApi(_logCallback);
                    }

                    // Register the storage API
                    if (_getStorageCallback && _setStorageCallback && _removeStorageCallback && _clearStorageCallback)
                    {
                        _api->RegisterStorageApi(_getStorageCallback, _setStorageCallback, _removeStorageCallback, _clearStorageCallback);
                    }

                    // Register the crypto API
                    if (_randomBytesCallback && _sha256Callback && _signCallback && _verifyCallback)
                    {
                        _api->RegisterCryptoApi(_randomBytesCallback, _sha256Callback, _signCallback, _verifyCallback);
                    }

                    // Register the gas API
                    if (_getGasCallback && _useGasCallback)
                    {
                        _api->RegisterGasApi(_getGasCallback, _useGasCallback);
                    }

                    // Register the secrets API
                    if (_getSecretCallback && _setSecretCallback && _removeSecretCallback)
                    {
                        _api->RegisterSecretsApi(_getSecretCallback, _setSecretCallback, _removeSecretCallback);
                    }

                    // Register the blockchain API
                    if (_callbackCallback)
                    {
                        _api->RegisterBlockchainApi(_callbackCallback);
                    }

                    _initialized = true;
                }

                std::string QuickJsExecutor::Execute(const std::string& code, const std::string& filename)
                {
                    try
                    {
                        // Initialize the API
                        InitializeApi();

                        // Execute the code
                        auto result = _engine->Evaluate(code, filename);

                        // Convert the result to a string
                        return result->ToString();
                    }
                    catch (const std::exception& ex)
                    {
                        if (_logCallback)
                        {
                            _logCallback(std::string("Error executing JavaScript: ") + ex.what());
                        }
                        throw;
                    }
                }

                std::string QuickJsExecutor::ExecuteFunction(const std::string& functionName, const std::vector<std::string>& args)
                {
                    try
                    {
                        // Initialize the API
                        InitializeApi();

                        // Get the global object
                        auto global = _engine->GetGlobalObject();

                        // Get the function
                        auto func = global->GetProperty(functionName);
                        if (!func->IsFunction())
                        {
                            throw std::runtime_error("Function '" + functionName + "' not found");
                        }

                        // Convert the arguments
                        std::vector<std::shared_ptr<JsValue>> jsArgs;
                        for (const auto& arg : args)
                        {
                            jsArgs.push_back(_engine->CreateValue(arg));
                        }

                        // Call the function
                        auto result = func->Call(global, jsArgs);

                        // Convert the result to a string
                        return result->ToString();
                    }
                    catch (const std::exception& ex)
                    {
                        if (_logCallback)
                        {
                            _logCallback(std::string("Error executing JavaScript function: ") + ex.what());
                        }
                        throw;
                    }
                }

                void QuickJsExecutor::CollectGarbage()
                {
                    _engine->CollectGarbage();
                }
            }
        }
    }
}
