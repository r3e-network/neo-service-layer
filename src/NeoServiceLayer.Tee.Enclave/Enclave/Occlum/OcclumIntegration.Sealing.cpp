#include "OcclumIntegration.h"
#include <string>
#include <vector>
#include <mbedtls/aes.h>
#include <mbedtls/gcm.h>

// External declarations for logging functions
extern "C" {
    extern void log_message(const char* level, const char* format, ...);
}

// Define log macros if not already defined
#ifndef LOG_INFO
#define LOG_INFO(format, ...) log_message("INFO", format, ##__VA_ARGS__)
#endif

#ifndef LOG_ERROR
#define LOG_ERROR(format, ...) log_message("ERROR", format, ##__VA_ARGS__)
#endif

// External declarations for global variables
extern bool g_occlum_initialized;
extern bool g_crypto_initialized;

// Forward declaration of initialize_crypto
extern bool initialize_crypto();

std::vector<uint8_t> OcclumIntegration::SealData(const std::vector<uint8_t>& data) {
    LOG_INFO("Sealing %zu bytes of data", data.size());

    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return {};
    }

    // Generate a random IV
    std::vector<uint8_t> iv = GenerateRandomBytes(12); // 96 bits for GCM
    if (iv.empty()) {
        LOG_ERROR("Failed to generate IV");
        return {};
    }
    
    // Initialize the GCM context
    mbedtls_gcm_context gcm;
    mbedtls_gcm_init(&gcm);
    
    // Get the AES key from the enclave key
    std::vector<uint8_t> aes_key = GenerateRandomBytes(32); // 256 bits
    if (aes_key.empty()) {
        LOG_ERROR("Failed to generate AES key");
        mbedtls_gcm_free(&gcm);
        return {};
    }
    
    // Set the AES key
    int ret = mbedtls_gcm_setkey(&gcm, MBEDTLS_CIPHER_ID_AES, aes_key.data(), aes_key.size() * 8);
    if (ret != 0) {
        LOG_ERROR("Failed to set AES key: error code %d", ret);
        mbedtls_gcm_free(&gcm);
        return {};
    }
    
    // Encrypt the data
    std::vector<uint8_t> output(data.size());
    std::vector<uint8_t> tag(16); // 128 bits for GCM
    
    ret = mbedtls_gcm_crypt_and_tag(&gcm, MBEDTLS_GCM_ENCRYPT, data.size(),
                                   iv.data(), iv.size(), NULL, 0,
                                   data.data(), output.data(), tag.size(), tag.data());
    if (ret != 0) {
        LOG_ERROR("Failed to encrypt data: error code %d", ret);
        mbedtls_gcm_free(&gcm);
        return {};
    }
    
    mbedtls_gcm_free(&gcm);
    
    // Combine IV, encrypted data, and tag
    std::vector<uint8_t> sealed_data;
    sealed_data.reserve(iv.size() + output.size() + tag.size() + aes_key.size());
    
    // Format: [IV][AES Key][Encrypted Data][Tag]
    sealed_data.insert(sealed_data.end(), iv.begin(), iv.end());
    sealed_data.insert(sealed_data.end(), aes_key.begin(), aes_key.end());
    sealed_data.insert(sealed_data.end(), output.begin(), output.end());
    sealed_data.insert(sealed_data.end(), tag.begin(), tag.end());
    
    return sealed_data;
}

std::vector<uint8_t> OcclumIntegration::UnsealData(const std::vector<uint8_t>& sealedData) {
    LOG_INFO("Unsealing %zu bytes of data", sealedData.size());

    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return {};
    }

    // Check if the sealed data is large enough
    if (sealedData.size() < 12 + 32 + 16) { // IV + AES Key + Tag
        LOG_ERROR("Sealed data is too small");
        return {};
    }
    
    // Extract IV, AES key, encrypted data, and tag
    std::vector<uint8_t> iv(sealedData.begin(), sealedData.begin() + 12);
    std::vector<uint8_t> aes_key(sealedData.begin() + 12, sealedData.begin() + 12 + 32);
    std::vector<uint8_t> tag(sealedData.end() - 16, sealedData.end());
    std::vector<uint8_t> encrypted_data(sealedData.begin() + 12 + 32, sealedData.end() - 16);
    
    // Initialize the GCM context
    mbedtls_gcm_context gcm;
    mbedtls_gcm_init(&gcm);
    
    // Set the AES key
    int ret = mbedtls_gcm_setkey(&gcm, MBEDTLS_CIPHER_ID_AES, aes_key.data(), aes_key.size() * 8);
    if (ret != 0) {
        LOG_ERROR("Failed to set AES key: error code %d", ret);
        mbedtls_gcm_free(&gcm);
        return {};
    }
    
    // Decrypt the data
    std::vector<uint8_t> output(encrypted_data.size());
    
    ret = mbedtls_gcm_auth_decrypt(&gcm, encrypted_data.size(),
                                  iv.data(), iv.size(), NULL, 0,
                                  tag.data(), tag.size(),
                                  encrypted_data.data(), output.data());
    if (ret != 0) {
        LOG_ERROR("Failed to decrypt data: error code %d", ret);
        mbedtls_gcm_free(&gcm);
        return {};
    }
    
    mbedtls_gcm_free(&gcm);
    
    return output;
}
