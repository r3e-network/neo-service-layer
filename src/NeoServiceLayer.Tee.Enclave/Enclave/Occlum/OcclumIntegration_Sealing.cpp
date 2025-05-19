#include "OcclumIntegration.h"
#include <string>
#include <vector>
#include <mbedtls/pk.h>
#include <mbedtls/entropy.h>
#include <mbedtls/ctr_drbg.h>
#include <mbedtls/gcm.h>

// External declarations for global variables
extern bool g_occlum_initialized;
extern bool g_crypto_initialized;
extern mbedtls_pk_context g_enclave_key;
extern mbedtls_ctr_drbg_context g_ctr_drbg;
extern mbedtls_entropy_context g_entropy;

// External declarations for helper functions
extern bool initialize_crypto();
extern void log_message(const char* level, const char* format, ...);

// Logging macros
#define LOG_INFO(format, ...) log_message("INFO", format, ##__VA_ARGS__)
#define LOG_ERROR(format, ...) log_message("ERROR", format, ##__VA_ARGS__)
#define LOG_DEBUG(format, ...) log_message("DEBUG", format, ##__VA_ARGS__)
#define LOG_WARNING(format, ...) log_message("WARNING", format, ##__VA_ARGS__)

std::vector<uint8_t> OcclumIntegration::SealData(const std::vector<uint8_t>& data) {
    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return {};
    }
    
    // Generate a random key and IV
    std::vector<uint8_t> key = GenerateRandomBytes(32); // 256-bit key
    std::vector<uint8_t> iv = GenerateRandomBytes(12); // 96-bit IV for GCM
    std::vector<uint8_t> tag(16); // 128-bit tag
    
    // Encrypt the data using AES-GCM
    mbedtls_gcm_context gcm;
    mbedtls_gcm_init(&gcm);
    
    int ret = mbedtls_gcm_setkey(&gcm, MBEDTLS_CIPHER_ID_AES, key.data(), key.size() * 8);
    if (ret != 0) {
        LOG_ERROR("Failed to set key: error code %d", ret);
        mbedtls_gcm_free(&gcm);
        return {};
    }
    
    std::vector<uint8_t> ciphertext(data.size());
    ret = mbedtls_gcm_crypt_and_tag(&gcm, MBEDTLS_GCM_ENCRYPT, data.size(), iv.data(), iv.size(),
                                   NULL, 0, data.data(), ciphertext.data(), tag.size(), tag.data());
    
    mbedtls_gcm_free(&gcm);
    
    if (ret != 0) {
        LOG_ERROR("Failed to encrypt data: error code %d", ret);
        return {};
    }
    
    // Combine IV, ciphertext, and tag
    std::vector<uint8_t> sealed_data;
    sealed_data.reserve(iv.size() + ciphertext.size() + tag.size());
    sealed_data.insert(sealed_data.end(), iv.begin(), iv.end());
    sealed_data.insert(sealed_data.end(), ciphertext.begin(), ciphertext.end());
    sealed_data.insert(sealed_data.end(), tag.begin(), tag.end());
    
    // Encrypt the key with the enclave's public key
    std::vector<uint8_t> encrypted_key(256); // RSA-2048 output size
    size_t encrypted_key_len = 0;
    
    ret = mbedtls_pk_encrypt(&g_enclave_key, key.data(), key.size(),
                            encrypted_key.data(), &encrypted_key_len, encrypted_key.size(),
                            mbedtls_ctr_drbg_random, &g_ctr_drbg);
    
    if (ret != 0) {
        LOG_ERROR("Failed to encrypt key: error code %d", ret);
        return {};
    }
    
    encrypted_key.resize(encrypted_key_len);
    
    // Combine encrypted key and sealed data
    std::vector<uint8_t> result;
    result.reserve(4 + encrypted_key.size() + sealed_data.size());
    
    // Add encrypted key size (4 bytes)
    uint32_t key_size = static_cast<uint32_t>(encrypted_key.size());
    result.push_back((key_size >> 24) & 0xFF);
    result.push_back((key_size >> 16) & 0xFF);
    result.push_back((key_size >> 8) & 0xFF);
    result.push_back(key_size & 0xFF);
    
    // Add encrypted key
    result.insert(result.end(), encrypted_key.begin(), encrypted_key.end());
    
    // Add sealed data
    result.insert(result.end(), sealed_data.begin(), sealed_data.end());
    
    return result;
}

std::vector<uint8_t> OcclumIntegration::UnsealData(const std::vector<uint8_t>& sealedData) {
    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return {};
    }
    
    if (sealedData.size() < 4) {
        LOG_ERROR("Invalid sealed data: too small");
        return {};
    }
    
    // Extract encrypted key size
    uint32_t key_size = (static_cast<uint32_t>(sealedData[0]) << 24) |
                        (static_cast<uint32_t>(sealedData[1]) << 16) |
                        (static_cast<uint32_t>(sealedData[2]) << 8) |
                        static_cast<uint32_t>(sealedData[3]);
    
    if (sealedData.size() < 4 + key_size) {
        LOG_ERROR("Invalid sealed data: too small for key");
        return {};
    }
    
    // Extract encrypted key
    std::vector<uint8_t> encrypted_key(sealedData.begin() + 4, sealedData.begin() + 4 + key_size);
    
    // Extract sealed data
    std::vector<uint8_t> sealed_data(sealedData.begin() + 4 + key_size, sealedData.end());
    
    if (sealed_data.size() < 12 + 16) { // IV + tag
        LOG_ERROR("Invalid sealed data: too small for IV and tag");
        return {};
    }
    
    // Decrypt the key with the enclave's private key
    std::vector<uint8_t> key(32); // 256-bit key
    size_t key_len = 0;
    
    int ret = mbedtls_pk_decrypt(&g_enclave_key, encrypted_key.data(), encrypted_key.size(),
                                key.data(), &key_len, key.size(),
                                mbedtls_ctr_drbg_random, &g_ctr_drbg);
    
    if (ret != 0) {
        LOG_ERROR("Failed to decrypt key: error code %d", ret);
        return {};
    }
    
    key.resize(key_len);
    
    // Extract IV, ciphertext, and tag
    std::vector<uint8_t> iv(sealed_data.begin(), sealed_data.begin() + 12);
    std::vector<uint8_t> tag(sealed_data.end() - 16, sealed_data.end());
    std::vector<uint8_t> ciphertext(sealed_data.begin() + 12, sealed_data.end() - 16);
    
    // Decrypt the data using AES-GCM
    mbedtls_gcm_context gcm;
    mbedtls_gcm_init(&gcm);
    
    ret = mbedtls_gcm_setkey(&gcm, MBEDTLS_CIPHER_ID_AES, key.data(), key.size() * 8);
    if (ret != 0) {
        LOG_ERROR("Failed to set key: error code %d", ret);
        mbedtls_gcm_free(&gcm);
        return {};
    }
    
    std::vector<uint8_t> plaintext(ciphertext.size());
    ret = mbedtls_gcm_auth_decrypt(&gcm, ciphertext.size(), iv.data(), iv.size(),
                                  NULL, 0, tag.data(), tag.size(),
                                  ciphertext.data(), plaintext.data());
    
    mbedtls_gcm_free(&gcm);
    
    if (ret != 0) {
        LOG_ERROR("Failed to decrypt data: error code %d", ret);
        return {};
    }
    
    return plaintext;
}
