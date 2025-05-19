#include "KeyManager.h"
#include "OcclumIntegration.h"
#include <cstdio>
#include <openssl/evp.h>

// AES encryption method
std::vector<uint8_t> KeyManager::encrypt_aes(const KeyInfo& key_info, const std::vector<uint8_t>& data)
{
    try
    {
        // Use AES-GCM for encryption
        EVP_CIPHER_CTX* ctx = EVP_CIPHER_CTX_new();
        if (!ctx)
        {
            secure_log("Failed to create cipher context");
            return {};
        }

        // Determine the cipher based on the key size
        const EVP_CIPHER* cipher = nullptr;
        switch (key_info.data.size())
        {
            case 16: // 128 bits
                cipher = EVP_aes_128_gcm();
                break;
            case 24: // 192 bits
                cipher = EVP_aes_192_gcm();
                break;
            case 32: // 256 bits
                cipher = EVP_aes_256_gcm();
                break;
            default:
                secure_log("Invalid AES key size: " + std::to_string(key_info.data.size()));
                EVP_CIPHER_CTX_free(ctx);
                return {};
        }

        // Generate a random IV
        std::vector<uint8_t> iv = OcclumIntegration::GenerateRandomBytes(12); // 96 bits for GCM

        // Initialize the encryption operation
        if (EVP_EncryptInit_ex(ctx, cipher, nullptr, key_info.data.data(), iv.data()) != 1)
        {
            secure_log("Failed to initialize encryption");
            EVP_CIPHER_CTX_free(ctx);
            return {};
        }

        // Encrypt the data
        std::vector<uint8_t> encrypted(data.size() + EVP_CIPHER_block_size(cipher));
        int len = 0;
        if (EVP_EncryptUpdate(ctx, encrypted.data(), &len, data.data(), data.size()) != 1)
        {
            secure_log("Failed to encrypt data");
            EVP_CIPHER_CTX_free(ctx);
            return {};
        }

        int encrypted_len = len;
        if (EVP_EncryptFinal_ex(ctx, encrypted.data() + len, &len) != 1)
        {
            secure_log("Failed to finalize encryption");
            EVP_CIPHER_CTX_free(ctx);
            return {};
        }

        encrypted_len += len;
        encrypted.resize(encrypted_len);

        // Get the tag
        std::vector<uint8_t> tag(16); // 128 bits for GCM
        if (EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_GCM_GET_TAG, tag.size(), tag.data()) != 1)
        {
            secure_log("Failed to get tag");
            EVP_CIPHER_CTX_free(ctx);
            return {};
        }

        EVP_CIPHER_CTX_free(ctx);

        // Combine IV, encrypted data, and tag
        std::vector<uint8_t> result;
        result.insert(result.end(), iv.begin(), iv.end());
        result.insert(result.end(), encrypted.begin(), encrypted.end());
        result.insert(result.end(), tag.begin(), tag.end());

        return result;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error in AES encryption: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error in AES encryption");
        return {};
    }
}

// AES decryption method
std::vector<uint8_t> KeyManager::decrypt_aes(const KeyInfo& key_info, const std::vector<uint8_t>& data)
{
    try
    {
        // Use AES-GCM for decryption
        EVP_CIPHER_CTX* ctx = EVP_CIPHER_CTX_new();
        if (!ctx)
        {
            secure_log("Failed to create cipher context");
            return {};
        }

        // Determine the cipher based on the key size
        const EVP_CIPHER* cipher = nullptr;
        switch (key_info.data.size())
        {
            case 16: // 128 bits
                cipher = EVP_aes_128_gcm();
                break;
            case 24: // 192 bits
                cipher = EVP_aes_192_gcm();
                break;
            case 32: // 256 bits
                cipher = EVP_aes_256_gcm();
                break;
            default:
                secure_log("Invalid AES key size: " + std::to_string(key_info.data.size()));
                EVP_CIPHER_CTX_free(ctx);
                return {};
        }

        // Extract IV, encrypted data, and tag
        if (data.size() < 12 + 16) // IV + tag
        {
            secure_log("Invalid encrypted data size");
            EVP_CIPHER_CTX_free(ctx);
            return {};
        }

        std::vector<uint8_t> iv(data.begin(), data.begin() + 12);
        std::vector<uint8_t> tag(data.end() - 16, data.end());
        std::vector<uint8_t> encrypted(data.begin() + 12, data.end() - 16);

        // Initialize the decryption operation
        if (EVP_DecryptInit_ex(ctx, cipher, nullptr, key_info.data.data(), iv.data()) != 1)
        {
            secure_log("Failed to initialize decryption");
            EVP_CIPHER_CTX_free(ctx);
            return {};
        }

        // Set the tag
        if (EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_GCM_SET_TAG, tag.size(), tag.data()) != 1)
        {
            secure_log("Failed to set tag");
            EVP_CIPHER_CTX_free(ctx);
            return {};
        }

        // Decrypt the data
        std::vector<uint8_t> decrypted(encrypted.size());
        int len = 0;
        if (EVP_DecryptUpdate(ctx, decrypted.data(), &len, encrypted.data(), encrypted.size()) != 1)
        {
            secure_log("Failed to decrypt data");
            EVP_CIPHER_CTX_free(ctx);
            return {};
        }

        int decrypted_len = len;
        if (EVP_DecryptFinal_ex(ctx, decrypted.data() + len, &len) != 1)
        {
            secure_log("Failed to finalize decryption");
            EVP_CIPHER_CTX_free(ctx);
            return {};
        }

        decrypted_len += len;
        decrypted.resize(decrypted_len);

        EVP_CIPHER_CTX_free(ctx);

        return decrypted;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error in AES decryption: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error in AES decryption");
        return {};
    }
}

// Main encrypt method that dispatches to the appropriate encryption method based on key type
std::vector<uint8_t> KeyManager::encrypt(const std::string& key_id, const std::vector<uint8_t>& data)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("KeyManager not initialized");
        return {};
    }

    try
    {
        secure_log("Encrypting data with key: " + key_id);

        // Find the key
        auto it = _keys.find(key_id);
        if (it == _keys.end())
        {
            secure_log("Key not found: " + key_id);
            return {};
        }

        // Encrypt the data based on the key type
        switch (it->second.type)
        {
            case KeyType::AES:
                return encrypt_aes(it->second, data);
            case KeyType::RSA:
                return encrypt_rsa(it->second, data);
            case KeyType::EC:
                secure_log("EC keys cannot be used for encryption");
                return {};
            default:
                secure_log("Invalid key type");
                return {};
        }
    }
    catch (const std::exception& ex)
    {
        secure_log("Error encrypting data: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error encrypting data");
        return {};
    }
}

// Main decrypt method that dispatches to the appropriate decryption method based on key type
std::vector<uint8_t> KeyManager::decrypt(const std::string& key_id, const std::vector<uint8_t>& data)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("KeyManager not initialized");
        return {};
    }

    try
    {
        secure_log("Decrypting data with key: " + key_id);

        // Find the key
        auto it = _keys.find(key_id);
        if (it == _keys.end())
        {
            secure_log("Key not found: " + key_id);
            return {};
        }

        // Decrypt the data based on the key type
        switch (it->second.type)
        {
            case KeyType::AES:
                return decrypt_aes(it->second, data);
            case KeyType::RSA:
                return decrypt_rsa(it->second, data);
            case KeyType::EC:
                secure_log("EC keys cannot be used for decryption");
                return {};
            default:
                secure_log("Invalid key type");
                return {};
        }
    }
    catch (const std::exception& ex)
    {
        secure_log("Error decrypting data: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error decrypting data");
        return {};
    }
}
