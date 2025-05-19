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

// Verification methods
bool KeyManager::verify(const std::string& key_id, const std::vector<uint8_t>& data, const std::vector<uint8_t>& signature) {
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized && !initialize()) {
        return false;
    }

    // Get key
    auto it = _keys.find(key_id);
    if (it == _keys.end()) {
        ocall_print_string("Key not found");
        return false;
    }

    const KeyInfo& key_info = it->second;

    // Verify signature based on key type
    switch (key_info.type) {
        case KeyType::RSA:
        case KeyType::EC: {
            // Load key
            BIO* bio = BIO_new_mem_buf(key_info.data.data(), key_info.data.size());
            EVP_PKEY* pkey = d2i_PrivateKey_bio(bio, nullptr);
            BIO_free(bio);

            if (pkey == nullptr) {
                ocall_print_string("Failed to load key");
                return false;
            }

            // Create message digest context
            EVP_MD_CTX* md_ctx = EVP_MD_CTX_new();
            if (md_ctx == nullptr) {
                EVP_PKEY_free(pkey);
                ocall_print_string("Failed to create message digest context");
                return false;
            }

            // Initialize verification
            if (EVP_DigestVerifyInit(md_ctx, nullptr, EVP_sha256(), nullptr, pkey) != 1) {
                EVP_PKEY_free(pkey);
                EVP_MD_CTX_free(md_ctx);
                ocall_print_string("Failed to initialize verification");
                return false;
            }

            // Update with data
            if (EVP_DigestVerifyUpdate(md_ctx, data.data(), data.size()) != 1) {
                EVP_PKEY_free(pkey);
                EVP_MD_CTX_free(md_ctx);
                ocall_print_string("Failed to update verification");
                return false;
            }

            // Verify signature
            int result = EVP_DigestVerifyFinal(md_ctx, signature.data(), signature.size());

            // Clean up
            EVP_PKEY_free(pkey);
            EVP_MD_CTX_free(md_ctx);

            return result == 1;
        }
        default: {
            ocall_print_string("Unsupported key type for verification");
            return false;
        }
    }
}
