#pragma once

#include "QuickJsEngine.h"
#include <string>
#include <functional>

namespace NeoServiceLayer
{
    namespace Tee
    {
        namespace Enclave
        {
            namespace QuickJs
            {
                /**
                 * @brief Represents the JavaScript API.
                 */
                class JsApi
                {
                public:
                    /**
                     * @brief Creates a new JsApi.
                     * @param engine The JavaScript engine.
                     */
                    JsApi(std::shared_ptr<QuickJsEngine> engine);

                    /**
                     * @brief Registers the console API.
                     * @param logCallback The log callback.
                     */
                    void RegisterConsoleApi(std::function<void(const std::string&)> logCallback);

                    /**
                     * @brief Registers the storage API.
                     * @param getCallback The get callback.
                     * @param setCallback The set callback.
                     * @param removeCallback The remove callback.
                     * @param clearCallback The clear callback.
                     */
                    void RegisterStorageApi(
                        std::function<std::string(const std::string&)> getCallback,
                        std::function<void(const std::string&, const std::string&)> setCallback,
                        std::function<void(const std::string&)> removeCallback,
                        std::function<void()> clearCallback);

                    /**
                     * @brief Registers the crypto API.
                     * @param randomBytesCallback The random bytes callback.
                     * @param sha256Callback The SHA-256 callback.
                     * @param signCallback The sign callback.
                     * @param verifyCallback The verify callback.
                     */
                    void RegisterCryptoApi(
                        std::function<std::string(int)> randomBytesCallback,
                        std::function<std::string(const std::string&)> sha256Callback,
                        std::function<std::string(const std::string&, const std::string&)> signCallback,
                        std::function<bool(const std::string&, const std::string&, const std::string&)> verifyCallback);

                    /**
                     * @brief Registers the gas API.
                     * @param getGasCallback The get gas callback.
                     * @param useGasCallback The use gas callback.
                     */
                    void RegisterGasApi(
                        std::function<int64_t()> getGasCallback,
                        std::function<bool(int64_t)> useGasCallback);

                    /**
                     * @brief Registers the secrets API.
                     * @param getSecretCallback The get secret callback.
                     * @param setSecretCallback The set secret callback.
                     * @param removeSecretCallback The remove secret callback.
                     */
                    void RegisterSecretsApi(
                        std::function<std::string(const std::string&)> getSecretCallback,
                        std::function<void(const std::string&, const std::string&)> setSecretCallback,
                        std::function<void(const std::string&)> removeSecretCallback);

                    /**
                     * @brief Registers the blockchain API.
                     * @param callbackCallback The callback callback.
                     */
                    void RegisterBlockchainApi(
                        std::function<void(const std::string&, const std::string&)> callbackCallback);

                private:
                    std::shared_ptr<QuickJsEngine> _engine;
                };
            }
        }
    }
}
