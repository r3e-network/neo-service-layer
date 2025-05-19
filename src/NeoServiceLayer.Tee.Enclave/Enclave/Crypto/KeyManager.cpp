#include "KeyManager.h"
#include "StorageManager.h"
#include "OcclumIntegration.h"
#include <cstdio>
#include <ctime>
#include <random>
#include <sstream>
#include <iomanip>
#include <nlohmann/json.hpp>

using json = nlohmann::json;

KeyManager::KeyManager()
    : _initialized(false)
{
}

KeyManager::~KeyManager()
{
    // Clear sensitive data
    for (auto& pair : _keys)
    {
        std::fill(pair.second.data.begin(), pair.second.data.end(), 0);
    }
}

bool KeyManager::initialize()
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (_initialized)
    {
        secure_log("KeyManager already initialized");
        return true;
    }

    try
    {
        secure_log("Initializing KeyManager...");

        // Initialize OpenSSL
        OpenSSL_add_all_algorithms();

        // Load keys from persistent storage
        if (!load_keys())
        {
            secure_log("Failed to load keys from persistent storage");
            // Continue anyway, we'll create new keys as needed
        }

        _initialized = true;
        secure_log("KeyManager initialized successfully");
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error initializing KeyManager: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error initializing KeyManager");
        return false;
    }
}

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

void KeyManager::secure_log(const std::string& message)
{
    // Use fprintf for logging to stderr
    fprintf(stderr, "[KeyManager] %s\n", message.c_str());
}

const KeyInfo* KeyManager::get_key(const std::string& key_id)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("KeyManager not initialized");
        return nullptr;
    }

    try
    {
        secure_log("Getting key: " + key_id);

        // Find the key
        auto it = _keys.find(key_id);
        if (it == _keys.end())
        {
            secure_log("Key not found: " + key_id);
            return nullptr;
        }

        secure_log("Key found: " + key_id);
        return &it->second;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error getting key: " + std::string(ex.what()));
        return nullptr;
    }
    catch (...)
    {
        secure_log("Unknown error getting key");
        return nullptr;
    }
}

const KeyInfo* KeyManager::get_active_key(KeyType type)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("KeyManager not initialized");
        return nullptr;
    }

    try
    {
        secure_log("Getting active key of type " + std::to_string(static_cast<int>(type)));

        // Find the active key for this type
        auto it = _active_keys.find(type);
        if (it == _active_keys.end())
        {
            secure_log("No active key found for type " + std::to_string(static_cast<int>(type)));
            return nullptr;
        }

        // Get the key
        auto key_it = _keys.find(it->second);
        if (key_it == _keys.end())
        {
            secure_log("Active key not found: " + it->second);
            return nullptr;
        }

        secure_log("Active key found: " + it->second);
        return &key_it->second;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error getting active key: " + std::string(ex.what()));
        return nullptr;
    }
    catch (...)
    {
        secure_log("Unknown error getting active key");
        return nullptr;
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
                switch (it->second.data.size())
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
                        secure_log("Invalid AES key size: " + std::to_string(it->second.data.size()));
                        EVP_CIPHER_CTX_free(ctx);
                        return {};
                }

                // Generate a random IV
                std::vector<uint8_t> iv = OcclumIntegration::GenerateRandomBytes(12); // 96 bits for GCM

                // Initialize the encryption operation
                if (EVP_EncryptInit_ex(ctx, cipher, nullptr, it->second.data.data(), iv.data()) != 1)
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

                secure_log("Data encrypted successfully");
                return result;
            }
            case KeyType::RSA:
            {
                // Load the RSA key
                const unsigned char* der_data = it->second.data.data();
                EVP_PKEY* pkey = d2i_PrivateKey(EVP_PKEY_RSA, nullptr, &der_data, it->second.data.size());
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

                secure_log("Data encrypted successfully");
                return encrypted;
            }
            case KeyType::EC:
            {
                // EC keys are typically used for signing, not encryption
                secure_log("EC keys cannot be used for encryption");
                return {};
            }
            default:
            {
                secure_log("Invalid key type");
                return {};
            }
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
                switch (it->second.data.size())
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
                        secure_log("Invalid AES key size: " + std::to_string(it->second.data.size()));
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
                if (EVP_DecryptInit_ex(ctx, cipher, nullptr, it->second.data.data(), iv.data()) != 1)
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

                secure_log("Data decrypted successfully");
                return decrypted;
            }
            case KeyType::RSA:
            {
                // Load the RSA key
                const unsigned char* der_data = it->second.data.data();
                EVP_PKEY* pkey = d2i_PrivateKey(EVP_PKEY_RSA, nullptr, &der_data, it->second.data.size());
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

                secure_log("Data decrypted successfully");
                return decrypted;
            }
            case KeyType::EC:
            {
                // EC keys are typically used for signing, not encryption
                secure_log("EC keys cannot be used for decryption");
                return {};
            }
            default:
            {
                secure_log("Invalid key type");
                return {};
            }
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
            case KeyType::EC:
            {
                // Load the key
                const unsigned char* der_data = it->second.data.data();
                EVP_PKEY* pkey = d2i_PrivateKey(it->second.type == KeyType::RSA ? EVP_PKEY_RSA : EVP_PKEY_EC, nullptr, &der_data, it->second.data.size());
                if (!pkey)
                {
                    secure_log("Failed to load key");
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

                secure_log("Data signed successfully");
                return signature;
            }
            case KeyType::AES:
            {
                // AES keys are typically used for encryption, not signing
                secure_log("AES keys cannot be used for signing");
                return {};
            }
            default:
            {
                secure_log("Invalid key type");
                return {};
            }
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
            case KeyType::EC:
            {
                // Load the key
                const unsigned char* der_data = it->second.data.data();
                EVP_PKEY* pkey = d2i_PrivateKey(it->second.type == KeyType::RSA ? EVP_PKEY_RSA : EVP_PKEY_EC, nullptr, &der_data, it->second.data.size());
                if (!pkey)
                {
                    secure_log("Failed to load key");
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

                if (result == 1)
                {
                    secure_log("Signature verified successfully");
                    return true;
                }
                else if (result == 0)
                {
                    secure_log("Signature verification failed");
                    return false;
                }
                else
                {
                    secure_log("Error during signature verification");
                    return false;
                }
            }
            case KeyType::AES:
            {
                // AES keys are typically used for encryption, not signing
                secure_log("AES keys cannot be used for signature verification");
                return false;
            }
            default:
            {
                secure_log("Invalid key type");
                return false;
            }
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

std::vector<std::string> KeyManager::list_keys()
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("KeyManager not initialized");
        return {};
    }

    try
    {
        secure_log("Listing keys");

        std::vector<std::string> keys;
        keys.reserve(_keys.size());

        for (const auto& pair : _keys)
        {
            keys.push_back(pair.first);
        }

        secure_log("Listed " + std::to_string(keys.size()) + " keys");
        return keys;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error listing keys: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error listing keys");
        return {};
    }
}

bool KeyManager::delete_key(const std::string& key_id)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized)
    {
        secure_log("KeyManager not initialized");
        return false;
    }

    try
    {
        secure_log("Deleting key: " + key_id);

        // Find the key
        auto it = _keys.find(key_id);
        if (it == _keys.end())
        {
            secure_log("Key not found: " + key_id);
            return false;
        }

        // Check if this key is active
        for (auto& pair : _active_keys)
        {
            if (pair.second == key_id)
            {
                // Remove from active keys
                _active_keys.erase(pair.first);
                break;
            }
        }

        // Clear sensitive data
        std::fill(it->second.data.begin(), it->second.data.end(), 0);

        // Remove the key
        _keys.erase(it);

        // Save the keys to persistent storage
        if (!save_keys())
        {
            secure_log("Failed to save keys to persistent storage");
            // Continue anyway
        }

        secure_log("Key deleted successfully: " + key_id);
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error deleting key: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error deleting key");
        return false;
    }
}

bool KeyManager::save_keys()
{
    if (!_initialized)
    {
        secure_log("KeyManager not initialized");
        return false;
    }

    try
    {
        secure_log("Saving keys to persistent storage");

        // Serialize the keys
        std::vector<uint8_t> data = serialize_keys();
        if (data.empty())
        {
            secure_log("Failed to serialize keys");
            return false;
        }

        // Create a storage manager
        StorageManager storage_manager;
        if (!storage_manager.initialize())
        {
            secure_log("Failed to initialize storage manager");
            return false;
        }

        // Store the keys
        bool result = storage_manager.store_data("keys", "key_manager", data);

        if (result)
        {
            secure_log("Keys saved to persistent storage successfully");
        }
        else
        {
            secure_log("Failed to save keys to persistent storage");
        }

        return result;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error saving keys: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error saving keys");
        return false;
    }
}

bool KeyManager::load_keys()
{
    if (!_initialized)
    {
        secure_log("KeyManager not initialized");
        return false;
    }

    try
    {
        secure_log("Loading keys from persistent storage");

        // Create a storage manager
        StorageManager storage_manager;
        if (!storage_manager.initialize())
        {
            secure_log("Failed to initialize storage manager");
            return false;
        }

        // Load the keys
        std::vector<uint8_t> data;
        bool result = storage_manager.retrieve_data("keys", "key_manager", data);
        if (!result || data.empty())
        {
            secure_log("No keys found in persistent storage");
            return false;
        }

        // Deserialize the keys
        if (!deserialize_keys(data))
        {
            secure_log("Failed to deserialize keys");
            return false;
        }

        secure_log("Keys loaded from persistent storage successfully");
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error loading keys: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error loading keys");
        return false;
    }
}

std::string KeyManager::generate_key_id()
{
    try
    {
        // Generate a random 16-byte key ID
        std::vector<uint8_t> random_bytes = OcclumIntegration::GenerateRandomBytes(16);

        // Convert to a hex string
        std::stringstream ss;
        ss << std::hex << std::setfill('0');
        for (const auto& byte : random_bytes)
        {
            ss << std::setw(2) << static_cast<int>(byte);
        }

        return ss.str();
    }
    catch (const std::exception& ex)
    {
        secure_log("Error generating key ID: " + std::string(ex.what()));
        return "";
    }
    catch (...)
    {
        secure_log("Unknown error generating key ID");
        return "";
    }
}

uint64_t KeyManager::get_current_time()
{
    return static_cast<uint64_t>(std::time(nullptr));
}

uint64_t KeyManager::get_expiration_time(int days)
{
    return get_current_time() + static_cast<uint64_t>(days) * 24 * 60 * 60;
}

std::vector<uint8_t> KeyManager::serialize_keys()
{
    try
    {
        // Create a JSON object to store the keys
        json keys_json;

        // Add all keys to the JSON object
        for (const auto& pair : _keys)
        {
            const KeyInfo& key_info = pair.second;

            // Convert the key data to a base64 string
            std::string base64_data;
            size_t base64_len = 0;

            // Calculate the required buffer size
            mbedtls_base64_encode(nullptr, 0, &base64_len, key_info.data.data(), key_info.data.size());

            // Allocate the buffer
            base64_data.resize(base64_len);

            // Encode the data
            mbedtls_base64_encode(
                reinterpret_cast<unsigned char*>(&base64_data[0]),
                base64_data.size(),
                &base64_len,
                key_info.data.data(),
                key_info.data.size());

            // Resize the string to the actual encoded length
            base64_data.resize(base64_len - 1); // Remove the null terminator

            // Create a JSON object for the key
            json key_json = {
                {"id", key_info.id},
                {"type", static_cast<int>(key_info.type)},
                {"data", base64_data},
                {"creation_time", key_info.creation_time},
                {"expiration_time", key_info.expiration_time},
                {"active", key_info.active}
            };

            // Add the key to the keys JSON object
            keys_json[key_info.id] = key_json;
        }

        // Add active keys to the JSON object
        json active_keys_json;
        for (const auto& pair : _active_keys)
        {
            active_keys_json[std::to_string(static_cast<int>(pair.first))] = pair.second;
        }
        keys_json["active_keys"] = active_keys_json;

        // Serialize the JSON object to a string
        std::string json_str = keys_json.dump();

        // Convert the string to a vector of bytes
        return std::vector<uint8_t>(json_str.begin(), json_str.end());
    }
    catch (const std::exception& ex)
    {
        secure_log("Error serializing keys: " + std::string(ex.what()));
        return {};
    }
    catch (...)
    {
        secure_log("Unknown error serializing keys");
        return {};
    }
}

bool KeyManager::deserialize_keys(const std::vector<uint8_t>& data)
{
    try
    {
        // Convert the data to a string
        std::string json_str(data.begin(), data.end());

        // Parse the JSON string
        json keys_json = json::parse(json_str);

        // Clear existing keys
        _keys.clear();
        _active_keys.clear();

        // Load the keys
        for (auto& key_it : keys_json.items())
        {
            // Skip the active_keys entry
            if (key_it.key() == "active_keys")
            {
                continue;
            }

            // Get the key JSON object
            json& key_json = key_it.value();

            // Create a new key info
            KeyInfo key_info;
            key_info.id = key_json["id"];
            key_info.type = static_cast<KeyType>(key_json["type"].get<int>());
            key_info.creation_time = key_json["creation_time"];
            key_info.expiration_time = key_json["expiration_time"];
            key_info.active = key_json["active"];

            // Decode the key data from base64
            std::string base64_data = key_json["data"];
            std::vector<uint8_t> base64_bytes(base64_data.begin(), base64_data.end());
            size_t decoded_len = 0;

            // Calculate the required buffer size
            mbedtls_base64_decode(nullptr, 0, &decoded_len, base64_bytes.data(), base64_bytes.size());

            // Allocate the buffer
            key_info.data.resize(decoded_len);

            // Decode the data
            mbedtls_base64_decode(
                key_info.data.data(),
                key_info.data.size(),
                &decoded_len,
                base64_bytes.data(),
                base64_bytes.size());

            // Resize the vector to the actual decoded length
            key_info.data.resize(decoded_len);

            // Store the key
            _keys[key_info.id] = key_info;
        }

        // Load the active keys
        if (keys_json.contains("active_keys"))
        {
            json& active_keys_json = keys_json["active_keys"];
            for (auto& active_key_it : active_keys_json.items())
            {
                KeyType type = static_cast<KeyType>(std::stoi(active_key_it.key()));
                std::string key_id = active_key_it.value();
                _active_keys[type] = key_id;
            }
        }

        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error deserializing keys: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error deserializing keys");
        return false;
    }
}