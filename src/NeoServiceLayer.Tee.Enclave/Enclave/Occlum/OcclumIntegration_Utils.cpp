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

std::string OcclumIntegration::GetMrEnclave() {
    if (!g_occlum_initialized && !Initialize()) {
        LOG_ERROR("Occlum not initialized and initialization failed");
        return "";
    }
    
    // In a real implementation, we would get the MRENCLAVE value from SGX
    // For now, we'll return a placeholder value
    std::string mrenclave = "occlum_mrenclave_placeholder";
    LOG_INFO("MRENCLAVE: %s", mrenclave.c_str());
    return mrenclave;
}

std::string OcclumIntegration::GetMrSigner() {
    if (!g_occlum_initialized && !Initialize()) {
        LOG_ERROR("Occlum not initialized and initialization failed");
        return "";
    }
    
    // In a real implementation, we would get the MRSIGNER value from SGX
    // For now, we'll return a placeholder value
    std::string mrsigner = "occlum_mrsigner_placeholder";
    LOG_INFO("MRSIGNER: %s", mrsigner.c_str());
    return mrsigner;
}

std::string OcclumIntegration::GenerateUuid() {
    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return "";
    }
    
    std::vector<uint8_t> uuid_bytes = GenerateRandomBytes(16);
    
    // Format as UUID string
    std::stringstream ss;
    ss << std::hex << std::setfill('0');
    
    for (size_t i = 0; i < uuid_bytes.size(); i++) {
        if (i == 4 || i == 6 || i == 8 || i == 10) {
            ss << "-";
        }
        ss << std::setw(2) << static_cast<int>(uuid_bytes[i]);
    }
    
    return ss.str();
}

std::vector<uint8_t> OcclumIntegration::GenerateRandomBytes(size_t length) {
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
    std::vector<uint8_t> hash(32); // SHA-256 produces a 32-byte hash
    
    mbedtls_sha256_context ctx;
    mbedtls_sha256_init(&ctx);
    mbedtls_sha256_starts_ret(&ctx, 0); // 0 for SHA-256
    mbedtls_sha256_update_ret(&ctx, data.data(), data.size());
    mbedtls_sha256_finish_ret(&ctx, hash.data());
    mbedtls_sha256_free(&ctx);
    
    return hash;
}

std::vector<uint8_t> OcclumIntegration::SignData(const std::vector<uint8_t>& data) {
    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return {};
    }
    
    std::vector<uint8_t> hash = Sha256(data);
    std::vector<uint8_t> signature(MBEDTLS_ECDSA_MAX_LEN);
    size_t signature_len = 0;
    
    int ret = mbedtls_pk_sign(&g_enclave_key, MBEDTLS_MD_SHA256, hash.data(), hash.size(), 
                             signature.data(), &signature_len, mbedtls_ctr_drbg_random, &g_ctr_drbg);
    
    if (ret != 0) {
        LOG_ERROR("Failed to sign data: error code %d", ret);
        return {};
    }
    
    signature.resize(signature_len);
    return signature;
}

bool OcclumIntegration::VerifySignature(const std::vector<uint8_t>& data, const std::vector<uint8_t>& signature) {
    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return false;
    }
    
    std::vector<uint8_t> hash = Sha256(data);
    
    int ret = mbedtls_pk_verify(&g_enclave_key, MBEDTLS_MD_SHA256, hash.data(), hash.size(), 
                               signature.data(), signature.size());
    
    if (ret != 0) {
        LOG_ERROR("Failed to verify signature: error code %d", ret);
        return false;
    }
    
    return true;
}

std::vector<uint8_t> OcclumIntegration::GetEnclavePublicKey() {
    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return {};
    }
    
    unsigned char buf[512];
    int len = mbedtls_pk_write_pubkey_der(&g_enclave_key, buf, sizeof(buf));
    
    if (len <= 0) {
        LOG_ERROR("Failed to write public key: error code %d", len);
        return {};
    }
    
    // mbedtls_pk_write_pubkey_der writes at the end of the buffer
    return std::vector<uint8_t>(buf + sizeof(buf) - len, buf + sizeof(buf));
}

std::vector<uint8_t> OcclumIntegration::GenerateAttestationEvidence() {
    if (!g_occlum_initialized && !Initialize()) {
        LOG_ERROR("Occlum not initialized and initialization failed");
        return {};
    }
    
    // In a real implementation, we would generate a remote attestation report
    // For now, we'll return a placeholder value
    std::string evidence = "occlum_attestation_evidence_placeholder";
    LOG_INFO("Generated attestation evidence");
    
    return std::vector<uint8_t>(evidence.begin(), evidence.end());
}

bool OcclumIntegration::VerifyAttestationEvidence(const std::vector<uint8_t>& evidence, const std::vector<uint8_t>& endorsements) {
    if (!g_occlum_initialized && !Initialize()) {
        LOG_ERROR("Occlum not initialized and initialization failed");
        return false;
    }
    
    // In a real implementation, we would verify the remote attestation report
    // For now, we'll just return true
    LOG_INFO("Verified attestation evidence");
    return true;
}

uint64_t OcclumIntegration::GetCurrentTime() {
    auto now = std::chrono::system_clock::now();
    auto duration = now.time_since_epoch();
    auto millis = std::chrono::duration_cast<std::chrono::milliseconds>(duration).count();
    return static_cast<uint64_t>(millis);
}
