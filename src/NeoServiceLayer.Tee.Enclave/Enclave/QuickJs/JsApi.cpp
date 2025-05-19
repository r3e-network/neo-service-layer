#include "JsApi.h"
#include <stdexcept>

namespace NeoServiceLayer
{
    namespace Tee
    {
        namespace Enclave
        {
            namespace QuickJs
            {
                JsApi::JsApi(std::shared_ptr<QuickJsEngine> engine)
                    : _engine(engine)
                {
                    if (!engine)
                    {
                        throw std::invalid_argument("Engine cannot be null");
                    }
                }

                void JsApi::RegisterConsoleApi(std::function<void(const std::string&)> logCallback)
                {
                    if (!logCallback)
                    {
                        throw std::invalid_argument("Log callback cannot be null");
                    }

                    // Create the console object
                    auto console = _engine->CreateObject({});

                    // Add the log function
                    console->SetProperty("log", _engine->CreateFunction([this, logCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        std::string message;
                        for (size_t i = 0; i < args.size(); i++)
                        {
                            if (i > 0)
                            {
                                message += " ";
                            }
                            message += args[i]->ToString();
                        }
                        logCallback(message);
                        return _engine->CreateValue(true);
                    }));

                    // Add the info function
                    console->SetProperty("info", _engine->CreateFunction([this, logCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        std::string message = "[INFO] ";
                        for (size_t i = 0; i < args.size(); i++)
                        {
                            if (i > 0)
                            {
                                message += " ";
                            }
                            message += args[i]->ToString();
                        }
                        logCallback(message);
                        return _engine->CreateValue(true);
                    }));

                    // Add the warn function
                    console->SetProperty("warn", _engine->CreateFunction([this, logCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        std::string message = "[WARN] ";
                        for (size_t i = 0; i < args.size(); i++)
                        {
                            if (i > 0)
                            {
                                message += " ";
                            }
                            message += args[i]->ToString();
                        }
                        logCallback(message);
                        return _engine->CreateValue(true);
                    }));

                    // Add the error function
                    console->SetProperty("error", _engine->CreateFunction([this, logCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        std::string message = "[ERROR] ";
                        for (size_t i = 0; i < args.size(); i++)
                        {
                            if (i > 0)
                            {
                                message += " ";
                            }
                            message += args[i]->ToString();
                        }
                        logCallback(message);
                        return _engine->CreateValue(true);
                    }));

                    // Add the console object to the global object
                    _engine->GetGlobalObject()->SetProperty("console", console);
                }

                void JsApi::RegisterStorageApi(
                    std::function<std::string(const std::string&)> getCallback,
                    std::function<void(const std::string&, const std::string&)> setCallback,
                    std::function<void(const std::string&)> removeCallback,
                    std::function<void()> clearCallback)
                {
                    if (!getCallback || !setCallback || !removeCallback || !clearCallback)
                    {
                        throw std::invalid_argument("Callbacks cannot be null");
                    }

                    // Create the storage object
                    auto storage = _engine->CreateObject({});

                    // Add the get function
                    storage->SetProperty("get", _engine->CreateFunction([this, getCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        if (args.size() < 1)
                        {
                            throw std::runtime_error("storage.get requires a key parameter");
                        }
                        std::string key = args[0]->ToString();
                        std::string value = getCallback(key);
                        return _engine->CreateValue(value);
                    }));

                    // Add the set function
                    storage->SetProperty("set", _engine->CreateFunction([this, setCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        if (args.size() < 2)
                        {
                            throw std::runtime_error("storage.set requires key and value parameters");
                        }
                        std::string key = args[0]->ToString();
                        std::string value = args[1]->ToString();
                        setCallback(key, value);
                        return _engine->CreateValue(true);
                    }));

                    // Add the remove function
                    storage->SetProperty("remove", _engine->CreateFunction([this, removeCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        if (args.size() < 1)
                        {
                            throw std::runtime_error("storage.remove requires a key parameter");
                        }
                        std::string key = args[0]->ToString();
                        removeCallback(key);
                        return _engine->CreateValue(true);
                    }));

                    // Add the clear function
                    storage->SetProperty("clear", _engine->CreateFunction([this, clearCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        clearCallback();
                        return _engine->CreateValue(true);
                    }));

                    // Add the storage object to the global object
                    _engine->GetGlobalObject()->SetProperty("storage", storage);
                }

                void JsApi::RegisterCryptoApi(
                    std::function<std::string(int)> randomBytesCallback,
                    std::function<std::string(const std::string&)> sha256Callback,
                    std::function<std::string(const std::string&, const std::string&)> signCallback,
                    std::function<bool(const std::string&, const std::string&, const std::string&)> verifyCallback)
                {
                    if (!randomBytesCallback || !sha256Callback || !signCallback || !verifyCallback)
                    {
                        throw std::invalid_argument("Callbacks cannot be null");
                    }

                    // Create the crypto object
                    auto crypto = _engine->CreateObject({});

                    // Add the randomBytes function
                    crypto->SetProperty("randomBytes", _engine->CreateFunction([this, randomBytesCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        if (args.size() < 1)
                        {
                            throw std::runtime_error("crypto.randomBytes requires a size parameter");
                        }
                        int size = args[0]->ToInt32();
                        std::string bytes = randomBytesCallback(size);
                        return _engine->CreateValue(bytes);
                    }));

                    // Add the sha256 function
                    crypto->SetProperty("sha256", _engine->CreateFunction([this, sha256Callback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        if (args.size() < 1)
                        {
                            throw std::runtime_error("crypto.sha256 requires a data parameter");
                        }
                        std::string data = args[0]->ToString();
                        std::string hash = sha256Callback(data);
                        return _engine->CreateValue(hash);
                    }));

                    // Add the sign function
                    crypto->SetProperty("sign", _engine->CreateFunction([this, signCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        if (args.size() < 2)
                        {
                            throw std::runtime_error("crypto.sign requires data and key parameters");
                        }
                        std::string data = args[0]->ToString();
                        std::string key = args[1]->ToString();
                        std::string signature = signCallback(data, key);
                        return _engine->CreateValue(signature);
                    }));

                    // Add the verify function
                    crypto->SetProperty("verify", _engine->CreateFunction([this, verifyCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        if (args.size() < 3)
                        {
                            throw std::runtime_error("crypto.verify requires data, signature, and key parameters");
                        }
                        std::string data = args[0]->ToString();
                        std::string signature = args[1]->ToString();
                        std::string key = args[2]->ToString();
                        bool valid = verifyCallback(data, signature, key);
                        return _engine->CreateValue(valid);
                    }));

                    // Add the crypto object to the global object
                    _engine->GetGlobalObject()->SetProperty("crypto", crypto);
                }

                void JsApi::RegisterGasApi(
                    std::function<int64_t()> getGasCallback,
                    std::function<bool(int64_t)> useGasCallback)
                {
                    if (!getGasCallback || !useGasCallback)
                    {
                        throw std::invalid_argument("Callbacks cannot be null");
                    }

                    // Create the gas object
                    auto gas = _engine->CreateObject({});

                    // Add the get function
                    gas->SetProperty("get", _engine->CreateFunction([this, getGasCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        int64_t gasAmount = getGasCallback();
                        return _engine->CreateValue(static_cast<double>(gasAmount));
                    }));

                    // Add the use function
                    gas->SetProperty("use", _engine->CreateFunction([this, useGasCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        if (args.size() < 1)
                        {
                            throw std::runtime_error("gas.use requires an amount parameter");
                        }
                        int64_t amount = static_cast<int64_t>(args[0]->ToDouble());
                        bool success = useGasCallback(amount);
                        return _engine->CreateValue(success);
                    }));

                    // Add the gas object to the global object
                    _engine->GetGlobalObject()->SetProperty("gas", gas);
                }

                void JsApi::RegisterSecretsApi(
                    std::function<std::string(const std::string&)> getSecretCallback,
                    std::function<void(const std::string&, const std::string&)> setSecretCallback,
                    std::function<void(const std::string&)> removeSecretCallback)
                {
                    if (!getSecretCallback || !setSecretCallback || !removeSecretCallback)
                    {
                        throw std::invalid_argument("Callbacks cannot be null");
                    }

                    // Create the SECRETS object
                    auto secrets = _engine->CreateObject({});

                    // Add the get function
                    secrets->SetProperty("get", _engine->CreateFunction([this, getSecretCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        if (args.size() < 1)
                        {
                            throw std::runtime_error("SECRETS.get requires a key parameter");
                        }
                        std::string key = args[0]->ToString();
                        std::string value = getSecretCallback(key);
                        return _engine->CreateValue(value);
                    }));

                    // Add the set function
                    secrets->SetProperty("set", _engine->CreateFunction([this, setSecretCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        if (args.size() < 2)
                        {
                            throw std::runtime_error("SECRETS.set requires key and value parameters");
                        }
                        std::string key = args[0]->ToString();
                        std::string value = args[1]->ToString();
                        setSecretCallback(key, value);
                        return _engine->CreateValue(true);
                    }));

                    // Add the remove function
                    secrets->SetProperty("remove", _engine->CreateFunction([this, removeSecretCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        if (args.size() < 1)
                        {
                            throw std::runtime_error("SECRETS.remove requires a key parameter");
                        }
                        std::string key = args[0]->ToString();
                        removeSecretCallback(key);
                        return _engine->CreateValue(true);
                    }));

                    // Add the SECRETS object to the global object
                    _engine->GetGlobalObject()->SetProperty("SECRETS", secrets);
                }

                void JsApi::RegisterBlockchainApi(
                    std::function<void(const std::string&, const std::string&)> callbackCallback)
                {
                    if (!callbackCallback)
                    {
                        throw std::invalid_argument("Callback cannot be null");
                    }

                    // Create the blockchain object
                    auto blockchain = _engine->CreateObject({});

                    // Add the callback function
                    blockchain->SetProperty("callback", _engine->CreateFunction([this, callbackCallback](const std::vector<std::shared_ptr<JsValue>>& args) {
                        if (args.size() < 2)
                        {
                            throw std::runtime_error("blockchain.callback requires method and result parameters");
                        }
                        std::string method = args[0]->ToString();
                        std::string result = args[1]->ToString();
                        callbackCallback(method, result);
                        return _engine->CreateValue(true);
                    }));

                    // Add the blockchain object to the global object
                    _engine->GetGlobalObject()->SetProperty("blockchain", blockchain);
                }
            }
        }
    }
}
