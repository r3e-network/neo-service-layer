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

// Decryption methods
std::vector<uint8_t> KeyManager::decrypt(const std::string& key_id, const std::vector<uint8_t>& data) {
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

    // Decrypt data based on key type
    switch (key_info.type) {
        case KeyType::AES: {
            // Check if data is large enough to contain IV
            if (data.size() <= AES_BLOCK_SIZE) {
                ocall_print_string("Data too small to contain IV");
                return {};
            }

            // Extract IV
            std::vector<uint8_t> iv(data.begin(), data.begin() + AES_BLOCK_SIZE);

            // Create cipher context
            EVP_CIPHER_CTX* ctx = EVP_CIPHER_CTX_new();
            if (ctx == nullptr) {
                ocall_print_string("Failed to create cipher context");
                return {};
            }

            // Initialize decryption
            if (EVP_DecryptInit_ex(ctx, EVP_aes_256_cbc(), nullptr, key_info.data.data(), iv.data()) != 1) {
                EVP_CIPHER_CTX_free(ctx);
                ocall_print_string("Failed to initialize decryption");
                return {};
            }

            // Allocate output buffer
            std::vector<uint8_t> output(data.size() - AES_BLOCK_SIZE);
            int output_len = 0;

            // Decrypt data
            if (EVP_DecryptUpdate(ctx, output.data(), &output_len, data.data() + AES_BLOCK_SIZE, data.size() - AES_BLOCK_SIZE) != 1) {
                EVP_CIPHER_CTX_free(ctx);
                ocall_print_string("Failed to decrypt data");
                return {};
            }

            int final_len = 0;
            if (EVP_DecryptFinal_ex(ctx, output.data() + output_len, &final_len) != 1) {
                EVP_CIPHER_CTX_free(ctx);
                ocall_print_string("Failed to finalize decryption");
                return {};
            }

            // Clean up
            EVP_CIPHER_CTX_free(ctx);

            // Resize output buffer
            output.resize(output_len + final_len);

            return output;
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
                EVP_PKEY_decrypt_init(ctx) <= 0 ||
                EVP_PKEY_CTX_set_rsa_padding(ctx, RSA_PKCS1_OAEP_PADDING) <= 0) {

                EVP_PKEY_free(pkey);
                EVP_PKEY_CTX_free(ctx);
                ocall_print_string("Failed to initialize RSA decryption");
                return {};
            }

            // Determine output size
            size_t output_len = 0;
            if (EVP_PKEY_decrypt(ctx, nullptr, &output_len, data.data(), data.size()) <= 0) {
                EVP_PKEY_free(pkey);
                EVP_PKEY_CTX_free(ctx);
                ocall_print_string("Failed to determine output size");
                return {};
            }

            // Allocate output buffer
            std::vector<uint8_t> output(output_len);

            // Decrypt data
            if (EVP_PKEY_decrypt(ctx, output.data(), &output_len, data.data(), data.size()) <= 0) {
                EVP_PKEY_free(pkey);
                EVP_PKEY_CTX_free(ctx);
                ocall_print_string("Failed to decrypt data");
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
            ocall_print_string("Unsupported key type for decryption");
            return {};
        }
    }
}

// Signing methods
std::vector<uint8_t> KeyManager::sign(const std::string& key_id, const std::vector<uint8_t>& data) {
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

    // Sign data based on key type
    switch (key_info.type) {
        case KeyType::RSA:
        case KeyType::EC: {
            // Load key
            BIO* bio = BIO_new_mem_buf(key_info.data.data(), key_info.data.size());
            EVP_PKEY* pkey = d2i_PrivateKey_bio(bio, nullptr);
            BIO_free(bio);

            if (pkey == nullptr) {
                ocall_print_string("Failed to load key");
                return {};
            }

            // Create message digest context
            EVP_MD_CTX* md_ctx = EVP_MD_CTX_new();
            if (md_ctx == nullptr) {
                EVP_PKEY_free(pkey);
                ocall_print_string("Failed to create message digest context");
                return {};
            }

            // Initialize signing
            if (EVP_DigestSignInit(md_ctx, nullptr, EVP_sha256(), nullptr, pkey) != 1) {
                EVP_PKEY_free(pkey);
                EVP_MD_CTX_free(md_ctx);
                ocall_print_string("Failed to initialize signing");
                return {};
            }

            // Update with data
            if (EVP_DigestSignUpdate(md_ctx, data.data(), data.size()) != 1) {
                EVP_PKEY_free(pkey);
                EVP_MD_CTX_free(md_ctx);
                ocall_print_string("Failed to update signing");
                return {};
            }

            // Determine signature size
            size_t sig_len = 0;
            if (EVP_DigestSignFinal(md_ctx, nullptr, &sig_len) != 1) {
                EVP_PKEY_free(pkey);
                EVP_MD_CTX_free(md_ctx);
                ocall_print_string("Failed to determine signature size");
                return {};
            }

            // Allocate signature buffer
            std::vector<uint8_t> signature(sig_len);

            // Get signature
            if (EVP_DigestSignFinal(md_ctx, signature.data(), &sig_len) != 1) {
                EVP_PKEY_free(pkey);
                EVP_MD_CTX_free(md_ctx);
                ocall_print_string("Failed to get signature");
                return {};
            }

            // Clean up
            EVP_PKEY_free(pkey);
            EVP_MD_CTX_free(md_ctx);

            // Resize signature buffer
            signature.resize(sig_len);

            return signature;
        }
        default: {
            ocall_print_string("Unsupported key type for signing");
            return {};
        }
    }
}
