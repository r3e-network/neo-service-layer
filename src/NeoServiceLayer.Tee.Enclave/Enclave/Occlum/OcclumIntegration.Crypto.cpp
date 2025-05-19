#include "OcclumIntegration.h"
#include <string>
#include <vector>
#include <sstream>
#include <iomanip>
#include <chrono>
#include <mbedtls/sha256.h>
#include <mbedtls/pk.h>
#include <mbedtls/entropy.h>
#include <mbedtls/ctr_drbg.h>
#include <mbedtls/ecdsa.h>
#include <mbedtls/ecp.h>
#include <mbedtls/rsa.h>
#include <mbedtls/error.h>
#include <mbedtls/aes.h>
#include <mbedtls/gcm.h>
#include <mbedtls/base64.h>

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
extern mbedtls_pk_context g_enclave_key;
extern mbedtls_entropy_context g_entropy;
extern mbedtls_ctr_drbg_context g_ctr_drbg;

// Forward declaration of initialize_crypto
extern bool initialize_crypto();

std::vector<uint8_t> OcclumIntegration::GenerateRandomBytes(size_t length) {
    LOG_INFO("Generating %zu random bytes", length);

    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return {};
    }

    std::vector<uint8_t> random_bytes(length);
    
    int ret = mbedtls_ctr_drbg_random(&g_ctr_drbg, random_bytes.data(), length);
    if (ret != 0) {
        LOG_ERROR("Failed to generate random bytes: error code %d", ret);
        return {};
    }
    
    return random_bytes;
}

std::vector<uint8_t> OcclumIntegration::Sha256(const std::vector<uint8_t>& data) {
    LOG_INFO("Calculating SHA-256 hash of %zu bytes", data.size());

    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return {};
    }

    std::vector<uint8_t> hash(32); // SHA-256 produces a 32-byte hash
    
    mbedtls_sha256_context ctx;
    mbedtls_sha256_init(&ctx);
    
    int ret = mbedtls_sha256_starts_ret(&ctx, 0); // 0 for SHA-256, 1 for SHA-224
    if (ret != 0) {
        LOG_ERROR("Failed to initialize SHA-256: error code %d", ret);
        mbedtls_sha256_free(&ctx);
        return {};
    }
    
    ret = mbedtls_sha256_update_ret(&ctx, data.data(), data.size());
    if (ret != 0) {
        LOG_ERROR("Failed to update SHA-256: error code %d", ret);
        mbedtls_sha256_free(&ctx);
        return {};
    }
    
    ret = mbedtls_sha256_finish_ret(&ctx, hash.data());
    if (ret != 0) {
        LOG_ERROR("Failed to finalize SHA-256: error code %d", ret);
        mbedtls_sha256_free(&ctx);
        return {};
    }
    
    mbedtls_sha256_free(&ctx);
    
    return hash;
}

std::vector<uint8_t> OcclumIntegration::SignData(const std::vector<uint8_t>& data) {
    LOG_INFO("Signing %zu bytes of data", data.size());

    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return {};
    }

    // Calculate the SHA-256 hash of the data
    std::vector<uint8_t> hash = Sha256(data);
    if (hash.empty()) {
        LOG_ERROR("Failed to calculate hash");
        return {};
    }
    
    // Sign the hash
    unsigned char signature[MBEDTLS_ECDSA_MAX_LEN];
    size_t signature_len = 0;
    
    int ret = mbedtls_pk_sign(&g_enclave_key, MBEDTLS_MD_SHA256, hash.data(), hash.size(),
                             signature, &signature_len, mbedtls_ctr_drbg_random, &g_ctr_drbg);
    if (ret != 0) {
        LOG_ERROR("Failed to sign data: error code %d", ret);
        return {};
    }
    
    return std::vector<uint8_t>(signature, signature + signature_len);
}

bool OcclumIntegration::VerifySignature(const std::vector<uint8_t>& data, const std::vector<uint8_t>& signature) {
    LOG_INFO("Verifying signature for %zu bytes of data", data.size());

    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return false;
    }

    // Calculate the SHA-256 hash of the data
    std::vector<uint8_t> hash = Sha256(data);
    if (hash.empty()) {
        LOG_ERROR("Failed to calculate hash");
        return false;
    }
    
    // Verify the signature
    int ret = mbedtls_pk_verify(&g_enclave_key, MBEDTLS_MD_SHA256, hash.data(), hash.size(),
                               signature.data(), signature.size());
    if (ret != 0) {
        LOG_ERROR("Signature verification failed: error code %d", ret);
        return false;
    }
    
    LOG_INFO("Signature verified successfully");
    return true;
}

std::vector<uint8_t> OcclumIntegration::GetEnclavePublicKey() {
    LOG_INFO("Getting enclave public key");

    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return {};
    }

    // Export the public key
    unsigned char key_buf[1024];
    int ret = mbedtls_pk_write_pubkey_der(&g_enclave_key, key_buf, sizeof(key_buf));
    if (ret < 0) {
        LOG_ERROR("Failed to export public key: error code %d", ret);
        return {};
    }
    
    // mbedtls_pk_write_pubkey_der writes at the end of the buffer
    unsigned char* key_start = key_buf + sizeof(key_buf) - ret;
    
    return std::vector<uint8_t>(key_start, key_start + ret);
}

std::string OcclumIntegration::GenerateUuid() {
    LOG_INFO("Generating UUID");

    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return "";
    }

    // Generate 16 random bytes
    std::vector<uint8_t> random_bytes = GenerateRandomBytes(16);
    if (random_bytes.empty()) {
        LOG_ERROR("Failed to generate random bytes");
        return "";
    }

    // Format as UUID
    std::stringstream ss;
    ss << std::hex << std::setfill('0');
    
    for (size_t i = 0; i < random_bytes.size(); ++i) {
        if (i == 4 || i == 6 || i == 8 || i == 10) {
            ss << "-";
        }
        
        ss << std::setw(2) << static_cast<int>(random_bytes[i]);
    }
    
    // Set version (4) and variant (RFC 4122)
    std::string uuid = ss.str();
    uuid[14] = '4';
    uuid[19] = "89ab"[random_bytes[9] & 0x3];
    
    return uuid;
}

uint64_t OcclumIntegration::GetCurrentTime() {
    LOG_INFO("Getting current time");

    // Get the current time in milliseconds since epoch
    auto now = std::chrono::system_clock::now();
    auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()).count();
    
    return static_cast<uint64_t>(ms);
}
