#pragma once

#include "QuickJsEngine.h"
#include "JsApi.h"
#include <string>
#include <functional>
#include <memory>

namespace NeoServiceLayer
{
    namespace Tee
    {
        namespace Enclave
        {
            namespace QuickJs
            {
                /**
                 * @brief Represents a JavaScript executor.
                 */
                class QuickJsExecutor
                {
                public:
                    /**
                     * @brief Creates a new QuickJsExecutor.
                     */
                    QuickJsExecutor();

                    /**
                     * @brief Destructor.
                     */
                    ~QuickJsExecutor();

                    /**
                     * @brief Sets the log callback.
                     * @param callback The log callback.
                     */
                    void SetLogCallback(std::function<void(const std::string&)> callback);

                    /**
                     * @brief Sets the storage callbacks.
                     * @param getCallback The get callback.
                     * @param setCallback The set callback.
                     * @param removeCallback The remove callback.
                     * @param clearCallback The clear callback.
                     */
                    void SetStorageCallbacks(
                        std::function<std::string(const std::string&)> getCallback,
                        std::function<void(const std::string&, const std::string&)> setCallback,
                        std::function<void(const std::string&)> removeCallback,
                        std::function<void()> clearCallback);

                    /**
                     * @brief Sets the crypto callbacks.
                     * @param randomBytesCallback The random bytes callback.
                     * @param sha256Callback The SHA-256 callback.
                     * @param signCallback The sign callback.
                     * @param verifyCallback The verify callback.
                     */
                    void SetCryptoCallbacks(
                        std::function<std::string(int)> randomBytesCallback,
                        std::function<std::string(const std::string&)> sha256Callback,
                        std::function<std::string(const std::string&, const std::string&)> signCallback,
                        std::function<bool(const std::string&, const std::string&, const std::string&)> verifyCallback);

                    /**
                     * @brief Sets the gas callbacks.
                     * @param getGasCallback The get gas callback.
                     * @param useGasCallback The use gas callback.
                     */
                    void SetGasCallbacks(
                        std::function<int64_t()> getGasCallback,
                        std::function<bool(int64_t)> useGasCallback);

                    /**
                     * @brief Sets the secrets callbacks.
                     * @param getSecretCallback The get secret callback.
                     * @param setSecretCallback The set secret callback.
                     * @param removeSecretCallback The remove secret callback.
                     */
                    void SetSecretsCallbacks(
                        std::function<std::string(const std::string&)> getSecretCallback,
                        std::function<void(const std::string&, const std::string&)> setSecretCallback,
                        std::function<void(const std::string&)> removeSecretCallback);

                    /**
                     * @brief Sets the blockchain callbacks.
                     * @param callbackCallback The callback callback.
                     */
                    void SetBlockchainCallbacks(
                        std::function<void(const std::string&, const std::string&)> callbackCallback);

                    /**
                     * @brief Executes JavaScript code.
                     * @param code The JavaScript code.
                     * @param filename The filename for error reporting.
                     * @return The result of the execution.
                     */
                    std::string Execute(const std::string& code, const std::string& filename = "<eval>");

                    /**
                     * @brief Executes a JavaScript function.
                     * @param functionName The function name.
                     * @param args The function arguments.
                     * @return The result of the execution.
                     */
                    std::string ExecuteFunction(const std::string& functionName, const std::vector<std::string>& args);

                    /**
                     * @brief Collects garbage.
                     */
                    void CollectGarbage();

                private:
                    std::shared_ptr<QuickJsEngine> _engine;
                    std::shared_ptr<JsApi> _api;
                    std::function<void(const std::string&)> _logCallback;
                    std::function<std::string(const std::string&)> _getStorageCallback;
                    std::function<void(const std::string&, const std::string&)> _setStorageCallback;
                    std::function<void(const std::string&)> _removeStorageCallback;
                    std::function<void()> _clearStorageCallback;
                    std::function<std::string(int)> _randomBytesCallback;
                    std::function<std::string(const std::string&)> _sha256Callback;
                    std::function<std::string(const std::string&, const std::string&)> _signCallback;
                    std::function<bool(const std::string&, const std::string&, const std::string&)> _verifyCallback;
                    std::function<int64_t()> _getGasCallback;
                    std::function<bool(int64_t)> _useGasCallback;
                    std::function<std::string(const std::string&)> _getSecretCallback;
                    std::function<void(const std::string&, const std::string&)> _setSecretCallback;
                    std::function<void(const std::string&)> _removeSecretCallback;
                    std::function<void(const std::string&, const std::string&)> _callbackCallback;
                    bool _initialized;

                    /**
                     * @brief Initializes the JavaScript API.
                     */
                    void InitializeApi();
                };
            }
        }
    }
}
