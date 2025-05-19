#include "KeyManager.h"
#include "NeoServiceLayerEnclave_t.h"
#include <openssl/rand.h>
#include <openssl/aes.h>
#include <openssl/rsa.h>
#include <openssl/ec.h>
#include <openssl/evp.h>
#include <openssl/pem.h>
#include <openssl/err.h>
#include <openssl/sha.h>
#include <sstream>
#include <iomanip>

// Key generation methods
std::string KeyManager::generate_key(KeyType type, int bits, int expiration_days) {
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized && !initialize()) {
        return "";
    }

    // Generate key ID
    std::string key_id = generate_key_id();

    // Create key info
    KeyInfo key_info;
    key_info.id = key_id;
    key_info.type = type;
    key_info.creation_time = get_current_time();
    key_info.expiration_time = get_expiration_time(expiration_days);
    key_info.active = true;

    // Generate key data based on type
    switch (type) {
        case KeyType::AES: {
            // Generate AES key
            key_info.data.resize(bits / 8);
            if (RAND_bytes(key_info.data.data(), key_info.data.size()) != 1) {
                ocall_print_string("Failed to generate AES key");
                return "";
            }
            break;
        }
        case KeyType::RSA: {
            // Generate RSA key
            EVP_PKEY* pkey = nullptr;
            EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new_id(EVP_PKEY_RSA, nullptr);

            if (ctx == nullptr ||
                EVP_PKEY_keygen_init(ctx) <= 0 ||
                EVP_PKEY_CTX_set_rsa_keygen_bits(ctx, bits) <= 0 ||
                EVP_PKEY_keygen(ctx, &pkey) <= 0) {

                EVP_PKEY_CTX_free(ctx);
                ocall_print_string("Failed to generate RSA key");
                return "";
            }

            // Convert key to DER format
            BIO* bio = BIO_new(BIO_s_mem());
            if (bio == nullptr ||
                i2d_PrivateKey_bio(bio, pkey) <= 0) {

                EVP_PKEY_free(pkey);
                EVP_PKEY_CTX_free(ctx);
                BIO_free(bio);
                ocall_print_string("Failed to convert RSA key to DER format");
                return "";
            }

            // Get DER data
            BUF_MEM* bptr = nullptr;
            BIO_get_mem_ptr(bio, &bptr);
            key_info.data.assign(reinterpret_cast<uint8_t*>(bptr->data),
                               reinterpret_cast<uint8_t*>(bptr->data) + bptr->length);

            // Clean up
            EVP_PKEY_free(pkey);
            EVP_PKEY_CTX_free(ctx);
            BIO_free(bio);
            break;
        }
        case KeyType::EC: {
            // Generate EC key
            EVP_PKEY* pkey = nullptr;
            EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new_id(EVP_PKEY_EC, nullptr);

            if (ctx == nullptr ||
                EVP_PKEY_keygen_init(ctx) <= 0 ||
                EVP_PKEY_CTX_set_ec_paramgen_curve_nid(ctx, NID_X9_62_prime256v1) <= 0 ||
                EVP_PKEY_keygen(ctx, &pkey) <= 0) {

                EVP_PKEY_CTX_free(ctx);
                ocall_print_string("Failed to generate EC key");
                return "";
            }

            // Convert key to DER format
            BIO* bio = BIO_new(BIO_s_mem());
            if (bio == nullptr ||
                i2d_PrivateKey_bio(bio, pkey) <= 0) {

                EVP_PKEY_free(pkey);
                EVP_PKEY_CTX_free(ctx);
                BIO_free(bio);
                ocall_print_string("Failed to convert EC key to DER format");
                return "";
            }

            // Get DER data
            BUF_MEM* bptr = nullptr;
            BIO_get_mem_ptr(bio, &bptr);
            key_info.data.assign(reinterpret_cast<uint8_t*>(bptr->data),
                               reinterpret_cast<uint8_t*>(bptr->data) + bptr->length);

            // Clean up
            EVP_PKEY_free(pkey);
            EVP_PKEY_CTX_free(ctx);
            BIO_free(bio);
            break;
        }
        default: {
            ocall_print_string("Unsupported key type");
            return "";
        }
    }

    // Add key to map
    _keys[key_id] = key_info;

    // Set as active key for this type
    _active_keys[type] = key_id;

    // Save keys
    save_keys();

    return key_id;
}

std::vector<uint8_t> KeyManager::encrypt(const std::string& key_id, const std::vector<uint8_t>& data) {
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized && !initialize()) {
        return {};
    }

    // Get key
    auto it = _keys.find(key_id);
    if (it == _keys.end()) {
        ocall_print_string("Key not found");
        return {};
    }

    const KeyInfo& key_info = it->second;

    // Encrypt data based on key type
    switch (key_info.type) {
        case KeyType::AES: {
            // Generate IV
            std::vector<uint8_t> iv(AES_BLOCK_SIZE);
            if (RAND_bytes(iv.data(), iv.size()) != 1) {
                ocall_print_string("Failed to generate IV");
                return {};
            }

            // Create cipher context
            EVP_CIPHER_CTX* ctx = EVP_CIPHER_CTX_new();
            if (ctx == nullptr) {
                ocall_print_string("Failed to create cipher context");
                return {};
            }

            // Initialize encryption
            if (EVP_EncryptInit_ex(ctx, EVP_aes_256_cbc(), nullptr, key_info.data.data(), iv.data()) != 1) {
                EVP_CIPHER_CTX_free(ctx);
                ocall_print_string("Failed to initialize encryption");
                return {};
            }

            // Allocate output buffer
            std::vector<uint8_t> output(data.size() + AES_BLOCK_SIZE);
            int output_len = 0;

            // Encrypt data
            if (EVP_EncryptUpdate(ctx, output.data(), &output_len, data.data(), data.size()) != 1) {
                EVP_CIPHER_CTX_free(ctx);
                ocall_print_string("Failed to encrypt data");
                return {};
            }

            int final_len = 0;
            if (EVP_EncryptFinal_ex(ctx, output.data() + output_len, &final_len) != 1) {
                EVP_CIPHER_CTX_free(ctx);
                ocall_print_string("Failed to finalize encryption");
                return {};
            }

            // Clean up
            EVP_CIPHER_CTX_free(ctx);

            // Resize output buffer
            output.resize(output_len + final_len);

            // Prepend IV
            std::vector<uint8_t> result(iv.size() + output.size());
            std::copy(iv.begin(), iv.end(), result.begin());
            std::copy(output.begin(), output.end(), result.begin() + iv.size());

            return result;
        }
        case KeyType::RSA: {
            // Load key
            BIO* bio = BIO_new_mem_buf(key_info.data.data(), key_info.data.size());
            EVP_PKEY* pkey = d2i_PrivateKey_bio(bio, nullptr);
            BIO_free(bio);

            if (pkey == nullptr) {
                ocall_print_string("Failed to load RSA key");
                return {};
            }

            // Create cipher context
            EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new(pkey, nullptr);
            if (ctx == nullptr ||
                EVP_PKEY_encrypt_init(ctx) <= 0 ||
                EVP_PKEY_CTX_set_rsa_padding(ctx, RSA_PKCS1_OAEP_PADDING) <= 0) {

                EVP_PKEY_free(pkey);
                EVP_PKEY_CTX_free(ctx);
                ocall_print_string("Failed to initialize RSA encryption");
                return {};
            }

            // Determine output size
            size_t output_len = 0;
            if (EVP_PKEY_encrypt(ctx, nullptr, &output_len, data.data(), data.size()) <= 0) {
                EVP_PKEY_free(pkey);
                EVP_PKEY_CTX_free(ctx);
                ocall_print_string("Failed to determine output size");
                return {};
            }

            // Allocate output buffer
            std::vector<uint8_t> output(output_len);

            // Encrypt data
            if (EVP_PKEY_encrypt(ctx, output.data(), &output_len, data.data(), data.size()) <= 0) {
                EVP_PKEY_free(pkey);
                EVP_PKEY_CTX_free(ctx);
                ocall_print_string("Failed to encrypt data");
                return {};
            }

            // Clean up
            EVP_PKEY_free(pkey);
            EVP_PKEY_CTX_free(ctx);

            // Resize output buffer
            output.resize(output_len);

            return output;
        }
        default: {
            ocall_print_string("Unsupported key type for encryption");
            return {};
        }
    }
}
