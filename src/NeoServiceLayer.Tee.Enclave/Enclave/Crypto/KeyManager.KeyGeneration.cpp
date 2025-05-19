#include "KeyManager.h"
#include "OcclumIntegration.h"
#include <cstdio>
#include <openssl/evp.h>
#include <openssl/rsa.h>
#include <openssl/ec.h>

std::string KeyManager::generate_key(KeyType type, int bits, int expiration_days)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("KeyManager not initialized");
        return "";
    }

    try
    {
        secure_log("Generating key of type " + std::to_string(static_cast<int>(type)) + " with " + std::to_string(bits) + " bits");

        // Generate a new key ID
        std::string key_id = generate_key_id();

        // Create a new key info
        KeyInfo key_info;
        key_info.id = key_id;
        key_info.type = type;
        key_info.creation_time = get_current_time();
        key_info.expiration_time = get_expiration_time(expiration_days);
        key_info.active = true;

        // Generate the key data based on the type
        switch (type)
        {
            case KeyType::AES:
            {
                // Generate a random AES key
                key_info.data = OcclumIntegration::GenerateRandomBytes(bits / 8);
                break;
            }
            case KeyType::RSA:
            {
                // Generate an RSA key pair
                EVP_PKEY* pkey = nullptr;
                EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new_id(EVP_PKEY_RSA, nullptr);
                if (!ctx)
                {
                    secure_log("Failed to create RSA key context");
                    return "";
                }

                if (EVP_PKEY_keygen_init(ctx) <= 0)
                {
                    secure_log("Failed to initialize RSA key generation");
                    EVP_PKEY_CTX_free(ctx);
                    return "";
                }

                if (EVP_PKEY_CTX_set_rsa_keygen_bits(ctx, bits) <= 0)
                {
                    secure_log("Failed to set RSA key size");
                    EVP_PKEY_CTX_free(ctx);
                    return "";
                }

                if (EVP_PKEY_keygen(ctx, &pkey) <= 0)
                {
                    secure_log("Failed to generate RSA key");
                    EVP_PKEY_CTX_free(ctx);
                    return "";
                }

                EVP_PKEY_CTX_free(ctx);

                // Serialize the key to DER format
                int der_len = i2d_PrivateKey(pkey, nullptr);
                if (der_len <= 0)
                {
                    secure_log("Failed to get RSA key DER length");
                    EVP_PKEY_free(pkey);
                    return "";
                }

                key_info.data.resize(der_len);
                unsigned char* der_data = key_info.data.data();
                if (i2d_PrivateKey(pkey, &der_data) <= 0)
                {
                    secure_log("Failed to serialize RSA key to DER");
                    EVP_PKEY_free(pkey);
                    return "";
                }

                EVP_PKEY_free(pkey);
                break;
            }
            case KeyType::EC:
            {
                // Generate an EC key pair
                EVP_PKEY* pkey = nullptr;
                EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new_id(EVP_PKEY_EC, nullptr);
                if (!ctx)
                {
                    secure_log("Failed to create EC key context");
                    return "";
                }

                if (EVP_PKEY_keygen_init(ctx) <= 0)
                {
                    secure_log("Failed to initialize EC key generation");
                    EVP_PKEY_CTX_free(ctx);
                    return "";
                }

                // Set the curve based on the key size
                int nid;
                if (bits <= 256)
                {
                    nid = NID_X9_62_prime256v1; // P-256
                }
                else if (bits <= 384)
                {
                    nid = NID_secp384r1; // P-384
                }
                else
                {
                    nid = NID_secp521r1; // P-521
                }

                if (EVP_PKEY_CTX_set_ec_paramgen_curve_nid(ctx, nid) <= 0)
                {
                    secure_log("Failed to set EC curve");
                    EVP_PKEY_CTX_free(ctx);
                    return "";
                }

                if (EVP_PKEY_keygen(ctx, &pkey) <= 0)
                {
                    secure_log("Failed to generate EC key");
                    EVP_PKEY_CTX_free(ctx);
                    return "";
                }

                EVP_PKEY_CTX_free(ctx);

                // Serialize the key to DER format
                int der_len = i2d_PrivateKey(pkey, nullptr);
                if (der_len <= 0)
                {
                    secure_log("Failed to get EC key DER length");
                    EVP_PKEY_free(pkey);
                    return "";
                }

                key_info.data.resize(der_len);
                unsigned char* der_data = key_info.data.data();
                if (i2d_PrivateKey(pkey, &der_data) <= 0)
                {
                    secure_log("Failed to serialize EC key to DER");
                    EVP_PKEY_free(pkey);
                    return "";
                }

                EVP_PKEY_free(pkey);
                break;
            }
            default:
            {
                secure_log("Invalid key type");
                return "";
            }
        }

        // Store the key
        _keys[key_id] = key_info;

        // Update the active key for this type
        _active_keys[type] = key_id;

        // Save the keys to persistent storage
        if (!save_keys())
        {
            secure_log("Failed to save keys to persistent storage");
            // Continue anyway
        }

        secure_log("Key generated successfully: " + key_id);
        return key_id;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error generating key: " + std::string(ex.what()));
        return "";
    }
    catch (...)
    {
        secure_log("Unknown error generating key");
        return "";
    }
}

std::string KeyManager::rotate_key(KeyType type, int bits, int expiration_days)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("KeyManager not initialized");
        return "";
    }

    try
    {
        secure_log("Rotating key of type " + std::to_string(static_cast<int>(type)));

        // Find the active key for this type
        auto it = _active_keys.find(type);
        if (it != _active_keys.end())
        {
            // Deactivate the current active key
            auto key_it = _keys.find(it->second);
            if (key_it != _keys.end())
            {
                key_it->second.active = false;
            }
        }

        // Generate a new key
        return generate_key(type, bits, expiration_days);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error rotating key: " + std::string(ex.what()));
        return "";
    }
    catch (...)
    {
        secure_log("Unknown error rotating key");
        return "";
    }
}
