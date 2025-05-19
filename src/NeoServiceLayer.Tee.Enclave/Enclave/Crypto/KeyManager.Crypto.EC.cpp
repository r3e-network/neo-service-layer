#include "KeyManager.h"
#include <cstdio>
#include <openssl/evp.h>
#include <openssl/ec.h>

// EC signing method
std::vector<uint8_t> KeyManager::sign_ec(const KeyInfo& key_info, const std::vector<uint8_t>& data)
{
    try
    {
        // Load the EC key
        const unsigned char* der_data = key_info.data.data();
        EVP_PKEY* pkey = d2i_PrivateKey(EVP_PKEY_EC, nullptr, &der_data, key_info.data.size());
        if (!pkey)
        {
            secure_log("Failed to load EC key");
            return {};
        }

        // Create a message digest context
        EVP_MD_CTX* md_ctx = EVP_MD_CTX_new();
        if (!md_ctx)
        {
            secure_log("Failed to create message digest context");
            EVP_PKEY_free(pkey);
            return {};
        }

        // Initialize the signing operation
        if (EVP_DigestSignInit(md_ctx, nullptr, EVP_sha256(), nullptr, pkey) != 1)
        {
            secure_log("Failed to initialize signing operation");
            EVP_MD_CTX_free(md_ctx);
            EVP_PKEY_free(pkey);
            return {};
        }

        // Update the message digest with the data
        if (EVP_DigestSignUpdate(md_ctx, data.data(), data.size()) != 1)
        {
            secure_log("Failed to update message digest");
            EVP_MD_CTX_free(md_ctx);
            EVP_PKEY_free(pkey);
            return {};
        }

        // Determine the signature size
        size_t sig_len = 0;
        if (EVP_DigestSignFinal(md_ctx, nullptr, &sig_len) != 1)
        {
            secure_log("Failed to determine signature size");
            EVP_MD_CTX_free(md_ctx);
            EVP_PKEY_free(pkey);
            return {};
        }

        // Generate the signature
        std::vector<uint8_t> signature(sig_len);
        if (EVP_DigestSignFinal(md_ctx, signature.data(), &sig_len) != 1)
        {
            secure_log("Failed to generate signature");
            EVP_MD_CTX_free(md_ctx);
            EVP_PKEY_free(pkey);
            return {};
        }

        signature.resize(sig_len);

        EVP_MD_CTX_free(md_ctx);
        EVP_PKEY_free(pkey);

        return signature;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error in EC signing: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error in EC signing");
        return {};
    }
}

// EC verification method
bool KeyManager::verify_ec(const KeyInfo& key_info, const std::vector<uint8_t>& data, const std::vector<uint8_t>& signature)
{
    try
    {
        // Load the EC key
        const unsigned char* der_data = key_info.data.data();
        EVP_PKEY* pkey = d2i_PrivateKey(EVP_PKEY_EC, nullptr, &der_data, key_info.data.size());
        if (!pkey)
        {
            secure_log("Failed to load EC key");
            return false;
        }

        // Create a message digest context
        EVP_MD_CTX* md_ctx = EVP_MD_CTX_new();
        if (!md_ctx)
        {
            secure_log("Failed to create message digest context");
            EVP_PKEY_free(pkey);
            return false;
        }

        // Initialize the verification operation
        if (EVP_DigestVerifyInit(md_ctx, nullptr, EVP_sha256(), nullptr, pkey) != 1)
        {
            secure_log("Failed to initialize verification operation");
            EVP_MD_CTX_free(md_ctx);
            EVP_PKEY_free(pkey);
            return false;
        }

        // Update the message digest with the data
        if (EVP_DigestVerifyUpdate(md_ctx, data.data(), data.size()) != 1)
        {
            secure_log("Failed to update message digest");
            EVP_MD_CTX_free(md_ctx);
            EVP_PKEY_free(pkey);
            return false;
        }

        // Verify the signature
        int result = EVP_DigestVerifyFinal(md_ctx, signature.data(), signature.size());
        EVP_MD_CTX_free(md_ctx);
        EVP_PKEY_free(pkey);

        return (result == 1);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error in EC verification: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error in EC verification");
        return false;
    }
}

// Main sign method that dispatches to the appropriate signing method based on key type
std::vector<uint8_t> KeyManager::sign(const std::string& key_id, const std::vector<uint8_t>& data)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("KeyManager not initialized");
        return {};
    }

    try
    {
        secure_log("Signing data with key: " + key_id);

        // Find the key
        auto it = _keys.find(key_id);
        if (it == _keys.end())
        {
            secure_log("Key not found: " + key_id);
            return {};
        }

        // Sign the data based on the key type
        switch (it->second.type)
        {
            case KeyType::RSA:
                return sign_rsa(it->second, data);
            case KeyType::EC:
                return sign_ec(it->second, data);
            case KeyType::AES:
                secure_log("AES keys cannot be used for signing");
                return {};
            default:
                secure_log("Invalid key type");
                return {};
        }
    }
    catch (const std::exception& ex)
    {
        secure_log("Error signing data: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error signing data");
        return {};
    }
}

// Main verify method that dispatches to the appropriate verification method based on key type
bool KeyManager::verify(const std::string& key_id, const std::vector<uint8_t>& data, const std::vector<uint8_t>& signature)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("KeyManager not initialized");
        return false;
    }

    try
    {
        secure_log("Verifying signature with key: " + key_id);

        // Find the key
        auto it = _keys.find(key_id);
        if (it == _keys.end())
        {
            secure_log("Key not found: " + key_id);
            return false;
        }

        // Verify the signature based on the key type
        switch (it->second.type)
        {
            case KeyType::RSA:
                return verify_rsa(it->second, data, signature);
            case KeyType::EC:
                return verify_ec(it->second, data, signature);
            case KeyType::AES:
                secure_log("AES keys cannot be used for signature verification");
                return false;
            default:
                secure_log("Invalid key type");
                return false;
        }
    }
    catch (const std::exception& ex)
    {
        secure_log("Error verifying signature: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error verifying signature");
        return false;
    }
}
