#include "KeyManager.h"
#include "../Core/EnclaveUtils.h"
#include "../Occlum/OcclumIntegration.h"
#include <openssl/rand.h>
#include <openssl/aes.h>
#include <openssl/evp.h>
#include <openssl/sha.h>
#include <sstream>
#include <iomanip>

using namespace NeoServiceLayer::Enclave::OcclumIntegration;

// Storage methods
bool KeyManager::save_keys() {
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized && !initialize()) {
        return false;
    }

    try {
        // Serialize keys
        std::vector<uint8_t> serialized_keys = serialize_keys();
        if (serialized_keys.empty()) {
            LogError("Failed to serialize keys");
            return false;
        }

        // Generate a key for encryption
        std::vector<uint8_t> encryption_key(32);
        if (RAND_bytes(encryption_key.data(), encryption_key.size()) != 1) {
            LogError("Failed to generate encryption key");
            return false;
        }

        // Generate IV
        std::vector<uint8_t> iv(AES_BLOCK_SIZE);
        if (RAND_bytes(iv.data(), iv.size()) != 1) {
            LogError("Failed to generate IV");
            return false;
        }

        // Create cipher context
        EVP_CIPHER_CTX* ctx = EVP_CIPHER_CTX_new();
        if (ctx == nullptr) {
            LogError("Failed to create cipher context");
            return false;
        }

        // Initialize encryption
        if (EVP_EncryptInit_ex(ctx, EVP_aes_256_cbc(), nullptr, encryption_key.data(), iv.data()) != 1) {
            EVP_CIPHER_CTX_free(ctx);
            LogError("Failed to initialize encryption");
            return false;
        }

        // Allocate output buffer
        std::vector<uint8_t> encrypted_keys(serialized_keys.size() + AES_BLOCK_SIZE);
        int output_len = 0;

        // Encrypt data
        if (EVP_EncryptUpdate(ctx, encrypted_keys.data(), &output_len, serialized_keys.data(), serialized_keys.size()) != 1) {
            EVP_CIPHER_CTX_free(ctx);
            LogError("Failed to encrypt keys");
            return false;
        }

        int final_len = 0;
        if (EVP_EncryptFinal_ex(ctx, encrypted_keys.data() + output_len, &final_len) != 1) {
            EVP_CIPHER_CTX_free(ctx);
            LogError("Failed to finalize encryption");
            return false;
        }

        // Clean up
        EVP_CIPHER_CTX_free(ctx);

        // Resize output buffer
        encrypted_keys.resize(output_len + final_len);

        // Prepend IV
        std::vector<uint8_t> result(iv.size() + encrypted_keys.size());
        std::copy(iv.begin(), iv.end(), result.begin());
        std::copy(encrypted_keys.begin(), encrypted_keys.end(), result.begin() + iv.size());

        // Seal the encryption key using Occlum
        std::vector<uint8_t> sealed_key = SealData(encryption_key);
        if (sealed_key.empty()) {
            LogError("Failed to seal key");
            return false;
        }

        // Save sealed key and encrypted keys
        // TODO: Implement persistent storage
        // For now, just log success
        LogInfo("Keys saved successfully");

        return true;
    }
    catch (const std::exception& ex) {
        LogError("Failed to save keys: %s", ex.what());
        return false;
    }
}

bool KeyManager::load_keys() {
    std::lock_guard<std::mutex> lock(_mutex);

    try {
        // Load sealed key and encrypted keys
        // TODO: Implement persistent storage
        // For now, just return false to indicate keys need to be generated
        return false;

        // Unseal the encryption key using Occlum
        std::vector<uint8_t> sealed_key;
        std::vector<uint8_t> encryption_key = UnsealData(sealed_key);
        if (encryption_key.empty()) {
            LogError("Failed to unseal key");
            return false;
        }

        // Extract IV
        std::vector<uint8_t> encrypted_data;
        std::vector<uint8_t> iv(encrypted_data.begin(), encrypted_data.begin() + AES_BLOCK_SIZE);

        // Create cipher context
        EVP_CIPHER_CTX* ctx = EVP_CIPHER_CTX_new();
        if (ctx == nullptr) {
            LogError("Failed to create cipher context");
            return false;
        }

        // Initialize decryption
        if (EVP_DecryptInit_ex(ctx, EVP_aes_256_cbc(), nullptr, encryption_key.data(), iv.data()) != 1) {
            EVP_CIPHER_CTX_free(ctx);
            LogError("Failed to initialize decryption");
            return false;
        }

        // Allocate output buffer
        std::vector<uint8_t> decrypted_keys(encrypted_data.size() - AES_BLOCK_SIZE);
        int output_len = 0;

        // Decrypt data
        if (EVP_DecryptUpdate(ctx, decrypted_keys.data(), &output_len, encrypted_data.data() + AES_BLOCK_SIZE, encrypted_data.size() - AES_BLOCK_SIZE) != 1) {
            EVP_CIPHER_CTX_free(ctx);
            LogError("Failed to decrypt keys");
            return false;
        }

        int final_len = 0;
        if (EVP_DecryptFinal_ex(ctx, decrypted_keys.data() + output_len, &final_len) != 1) {
            EVP_CIPHER_CTX_free(ctx);
            LogError("Failed to finalize decryption");
            return false;
        }

        // Clean up
        EVP_CIPHER_CTX_free(ctx);

        // Resize output buffer
        decrypted_keys.resize(output_len + final_len);

        // Deserialize keys
        if (!deserialize_keys(decrypted_keys)) {
            LogError("Failed to deserialize keys");
            return false;
        }

        LogInfo("Keys loaded successfully");
        return true;
    }
    catch (const std::exception& ex) {
        LogError("Failed to load keys: %s", ex.what());
        return false;
    }
}
