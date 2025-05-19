#include "KeyManager.h"
#include <cstdio>
#include <openssl/evp.h>
#include <openssl/rsa.h>

// RSA encryption method
std::vector<uint8_t> KeyManager::encrypt_rsa(const KeyInfo& key_info, const std::vector<uint8_t>& data)
{
    try
    {
        // Load the RSA key
        const unsigned char* der_data = key_info.data.data();
        EVP_PKEY* pkey = d2i_PrivateKey(EVP_PKEY_RSA, nullptr, &der_data, key_info.data.size());
        if (!pkey)
        {
            secure_log("Failed to load RSA key");
            return {};
        }

        // Create a cipher context
        EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new(pkey, nullptr);
        if (!ctx)
        {
            secure_log("Failed to create cipher context");
            EVP_PKEY_free(pkey);
            return {};
        }

        // Initialize the encryption operation
        if (EVP_PKEY_encrypt_init(ctx) != 1)
        {
            secure_log("Failed to initialize encryption");
            EVP_PKEY_CTX_free(ctx);
            EVP_PKEY_free(pkey);
            return {};
        }

        // Set the padding mode
        if (EVP_PKEY_CTX_set_rsa_padding(ctx, RSA_PKCS1_OAEP_PADDING) != 1)
        {
            secure_log("Failed to set padding mode");
            EVP_PKEY_CTX_free(ctx);
            EVP_PKEY_free(pkey);
            return {};
        }

        // Determine the output buffer size
        size_t outlen = 0;
        if (EVP_PKEY_encrypt(ctx, nullptr, &outlen, data.data(), data.size()) != 1)
        {
            secure_log("Failed to determine output buffer size");
            EVP_PKEY_CTX_free(ctx);
            EVP_PKEY_free(pkey);
            return {};
        }

        // Encrypt the data
        std::vector<uint8_t> encrypted(outlen);
        if (EVP_PKEY_encrypt(ctx, encrypted.data(), &outlen, data.data(), data.size()) != 1)
        {
            secure_log("Failed to encrypt data");
            EVP_PKEY_CTX_free(ctx);
            EVP_PKEY_free(pkey);
            return {};
        }

        encrypted.resize(outlen);

        EVP_PKEY_CTX_free(ctx);
        EVP_PKEY_free(pkey);

        return encrypted;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error in RSA encryption: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error in RSA encryption");
        return {};
    }
}

// RSA decryption method
std::vector<uint8_t> KeyManager::decrypt_rsa(const KeyInfo& key_info, const std::vector<uint8_t>& data)
{
    try
    {
        // Load the RSA key
        const unsigned char* der_data = key_info.data.data();
        EVP_PKEY* pkey = d2i_PrivateKey(EVP_PKEY_RSA, nullptr, &der_data, key_info.data.size());
        if (!pkey)
        {
            secure_log("Failed to load RSA key");
            return {};
        }

        // Create a cipher context
        EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new(pkey, nullptr);
        if (!ctx)
        {
            secure_log("Failed to create cipher context");
            EVP_PKEY_free(pkey);
            return {};
        }

        // Initialize the decryption operation
        if (EVP_PKEY_decrypt_init(ctx) != 1)
        {
            secure_log("Failed to initialize decryption");
            EVP_PKEY_CTX_free(ctx);
            EVP_PKEY_free(pkey);
            return {};
        }

        // Set the padding mode
        if (EVP_PKEY_CTX_set_rsa_padding(ctx, RSA_PKCS1_OAEP_PADDING) != 1)
        {
            secure_log("Failed to set padding mode");
            EVP_PKEY_CTX_free(ctx);
            EVP_PKEY_free(pkey);
            return {};
        }

        // Determine the output buffer size
        size_t outlen = 0;
        if (EVP_PKEY_decrypt(ctx, nullptr, &outlen, data.data(), data.size()) != 1)
        {
            secure_log("Failed to determine output buffer size");
            EVP_PKEY_CTX_free(ctx);
            EVP_PKEY_free(pkey);
            return {};
        }

        // Decrypt the data
        std::vector<uint8_t> decrypted(outlen);
        if (EVP_PKEY_decrypt(ctx, decrypted.data(), &outlen, data.data(), data.size()) != 1)
        {
            secure_log("Failed to decrypt data");
            EVP_PKEY_CTX_free(ctx);
            EVP_PKEY_free(pkey);
            return {};
        }

        decrypted.resize(outlen);

        EVP_PKEY_CTX_free(ctx);
        EVP_PKEY_free(pkey);

        return decrypted;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error in RSA decryption: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error in RSA decryption");
        return {};
    }
}

// RSA signing method
std::vector<uint8_t> KeyManager::sign_rsa(const KeyInfo& key_info, const std::vector<uint8_t>& data)
{
    try
    {
        // Load the RSA key
        const unsigned char* der_data = key_info.data.data();
        EVP_PKEY* pkey = d2i_PrivateKey(EVP_PKEY_RSA, nullptr, &der_data, key_info.data.size());
        if (!pkey)
        {
            secure_log("Failed to load RSA key");
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
        secure_log("Error in RSA signing: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error in RSA signing");
        return {};
    }
}

// RSA verification method
bool KeyManager::verify_rsa(const KeyInfo& key_info, const std::vector<uint8_t>& data, const std::vector<uint8_t>& signature)
{
    try
    {
        // Load the RSA key
        const unsigned char* der_data = key_info.data.data();
        EVP_PKEY* pkey = d2i_PrivateKey(EVP_PKEY_RSA, nullptr, &der_data, key_info.data.size());
        if (!pkey)
        {
            secure_log("Failed to load RSA key");
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
        secure_log("Error in RSA verification: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error in RSA verification");
        return false;
    }
}
