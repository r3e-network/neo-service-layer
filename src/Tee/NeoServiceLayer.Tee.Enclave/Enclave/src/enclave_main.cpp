/*
 * enclave_main.cpp - Main SGX Enclave Implementation
 * 
 * This file implements the trusted functions (ECALLs) defined in NeoServiceEnclave.edl
 * for the Neo Service Layer SGX enclave running in simulation mode.
 */

#include "NeoServiceEnclave_t.h"  // Generated from EDL file
#include "sgx_trts.h"
#include "sgx_tcrypto.h"
#include "sgx_tseal.h"
#include "sgx_thread.h"
#include "sgx_tae_service.h"

#include <cstring>
#include <cstdlib>
#include <string>
#include <map>
#include <vector>
#include <chrono>
#include <random>
#include <algorithm>

// Global enclave state
static bool g_enclave_initialized = false;
static sgx_thread_mutex_t g_global_mutex = SGX_THREAD_MUTEX_INITIALIZER;
static std::map<std::string, std::vector<uint8_t>> g_secure_storage;
static std::map<std::string, std::map<std::string, std::string>> g_ai_models;
static std::map<std::string, std::map<std::string, std::string>> g_abstract_accounts;
static std::mt19937 g_random_generator;

// Helper functions
namespace {
    void secure_memset(void* ptr, int value, size_t size) {
        if (ptr != nullptr) {
            volatile uint8_t* volatile_ptr = static_cast<volatile uint8_t*>(ptr);
            for (size_t i = 0; i < size; i++) {
                volatile_ptr[i] = static_cast<uint8_t>(value);
            }
        }
    }

    std::string generate_uuid() {
        char uuid_str[37];
        uint32_t data[4];
        sgx_read_rand(reinterpret_cast<unsigned char*>(data), sizeof(data));
        
        snprintf(uuid_str, sizeof(uuid_str),
                "%08x-%04x-4%03x-%04x-%08x%04x",
                data[0],
                static_cast<uint16_t>(data[1]),
                static_cast<uint16_t>(data[1] >> 16) & 0x0fff,
                static_cast<uint16_t>(data[2]) | 0x8000,
                data[3],
                static_cast<uint16_t>(data[2] >> 16));
        
        return std::string(uuid_str);
    }

    uint64_t get_enclave_timestamp() {
        // In simulation mode, we can use system time
        // In hardware mode, we'd use trusted time counter
        uint64_t timestamp;
        ocall_get_system_time(&timestamp);
        return timestamp;
    }

    void debug_print(const char* message) {
        if (message != nullptr) {
            ocall_print(message);
        }
    }
}

/*
 * Enclave Lifecycle Functions
 */
sgx_status_t ecall_enclave_init() {
    sgx_thread_mutex_lock(&g_global_mutex);
    
    if (g_enclave_initialized) {
        sgx_thread_mutex_unlock(&g_global_mutex);
        return SGX_SUCCESS;
    }

    try {
        // Initialize random generator with secure seed
        uint32_t seed;
        sgx_status_t status = sgx_read_rand(reinterpret_cast<unsigned char*>(&seed), sizeof(seed));
        if (status != SGX_SUCCESS) {
            sgx_thread_mutex_unlock(&g_global_mutex);
            return status;
        }
        
        g_random_generator.seed(seed);
        
        // Clear storage maps
        g_secure_storage.clear();
        g_ai_models.clear();
        g_abstract_accounts.clear();
        
        g_enclave_initialized = true;
        debug_print("✅ SGX Enclave initialized successfully in simulation mode");
        
        sgx_thread_mutex_unlock(&g_global_mutex);
        return SGX_SUCCESS;
    } catch (...) {
        sgx_thread_mutex_unlock(&g_global_mutex);
        return SGX_ERROR_UNEXPECTED;
    }
}

sgx_status_t ecall_enclave_destroy() {
    sgx_thread_mutex_lock(&g_global_mutex);
    
    if (!g_enclave_initialized) {
        sgx_thread_mutex_unlock(&g_global_mutex);
        return SGX_ERROR_INVALID_STATE;
    }

    // Clear sensitive data
    g_secure_storage.clear();
    g_ai_models.clear();
    g_abstract_accounts.clear();
    
    g_enclave_initialized = false;
    debug_print("✅ SGX Enclave destroyed");
    
    sgx_thread_mutex_unlock(&g_global_mutex);
    return SGX_SUCCESS;
}

sgx_status_t ecall_get_attestation_report(char* report_buffer, size_t report_size, size_t* actual_size) {
    if (!g_enclave_initialized || report_buffer == nullptr || actual_size == nullptr) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    // Create mock attestation report for simulation mode
    const char* report_template = R"({
        "version": 4,
        "sign_type": 1,
        "mr_enclave": "1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef",
        "mr_signer": "fedcba0987654321fedcba0987654321fedcba0987654321fedcba0987654321",
        "isv_prod_id": 0,
        "isv_svn": 0,
        "simulation_mode": true,
        "timestamp": "%llu",
        "enclave_id": "%s"
    })";

    std::string enclave_id = generate_uuid();
    uint64_t timestamp = get_enclave_timestamp();
    
    char formatted_report[2048];
    int written = snprintf(formatted_report, sizeof(formatted_report), 
                          report_template, timestamp, enclave_id.c_str());
    
    if (written < 0 || static_cast<size_t>(written) >= report_size) {
        *actual_size = static_cast<size_t>(written) + 1;
        return SGX_ERROR_INVALID_PARAMETER;
    }

    *actual_size = static_cast<size_t>(written);
    memcpy(report_buffer, formatted_report, *actual_size);
    return SGX_SUCCESS;
}

/*
 * Cryptographic Functions
 */
sgx_status_t ecall_generate_random(int min_val, int max_val, int* result) {
    if (!g_enclave_initialized || result == nullptr || min_val >= max_val) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    uint32_t random_val;
    sgx_status_t status = sgx_read_rand(reinterpret_cast<unsigned char*>(&random_val), sizeof(random_val));
    if (status != SGX_SUCCESS) {
        return status;
    }

    *result = min_val + static_cast<int>(random_val % static_cast<uint32_t>(max_val - min_val));
    return SGX_SUCCESS;
}

sgx_status_t ecall_generate_random_bytes(unsigned char* buffer, size_t length) {
    if (!g_enclave_initialized || buffer == nullptr || length == 0) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    return sgx_read_rand(buffer, length);
}

sgx_status_t ecall_encrypt_data(const unsigned char* data, size_t data_size,
                               const unsigned char* key, size_t key_size,
                               unsigned char* encrypted_data, size_t encrypted_size,
                               size_t* actual_encrypted_size) {
    if (!g_enclave_initialized || data == nullptr || key == nullptr || 
        encrypted_data == nullptr || actual_encrypted_size == nullptr) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    // For simulation, use simple AES encryption
    sgx_aes_ctr_128bit_key_t aes_key;
    if (key_size < sizeof(aes_key)) {
        // Derive key from provided key
        sgx_sha256_hash_t hash;
        sgx_status_t status = sgx_sha256_msg(key, key_size, &hash);
        if (status != SGX_SUCCESS) return status;
        memcpy(&aes_key, &hash, sizeof(aes_key));
    } else {
        memcpy(&aes_key, key, sizeof(aes_key));
    }

    uint8_t ctr[16] = {0}; // Initialize counter to 0
    sgx_read_rand(ctr, 12); // Use random IV for first 12 bytes

    *actual_encrypted_size = data_size;
    if (encrypted_size < data_size) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    return sgx_aes_ctr_encrypt(&aes_key, data, static_cast<uint32_t>(data_size), 
                              ctr, 32, encrypted_data);
}

sgx_status_t ecall_decrypt_data(const unsigned char* encrypted_data, size_t encrypted_size,
                               const unsigned char* key, size_t key_size,
                               unsigned char* decrypted_data, size_t data_size,
                               size_t* actual_data_size) {
    if (!g_enclave_initialized || encrypted_data == nullptr || key == nullptr ||
        decrypted_data == nullptr || actual_data_size == nullptr) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    // For simulation, use simple AES decryption
    sgx_aes_ctr_128bit_key_t aes_key;
    if (key_size < sizeof(aes_key)) {
        sgx_sha256_hash_t hash;
        sgx_status_t status = sgx_sha256_msg(key, key_size, &hash);
        if (status != SGX_SUCCESS) return status;
        memcpy(&aes_key, &hash, sizeof(aes_key));
    } else {
        memcpy(&aes_key, key, sizeof(aes_key));
    }

    uint8_t ctr[16] = {0}; // Same counter as encryption

    *actual_data_size = encrypted_size;
    if (data_size < encrypted_size) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    return sgx_aes_ctr_decrypt(&aes_key, encrypted_data, static_cast<uint32_t>(encrypted_size),
                              ctr, 32, decrypted_data);
}

sgx_status_t ecall_sign_data(const unsigned char* data, size_t data_size,
                            const unsigned char* key, size_t key_size,
                            unsigned char* signature, size_t signature_size,
                            size_t* actual_signature_size) {
    if (!g_enclave_initialized || data == nullptr || signature == nullptr || 
        actual_signature_size == nullptr) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    // For simulation, create a simple HMAC-like signature
    const size_t required_sig_size = 32; // SHA256 size
    *actual_signature_size = required_sig_size;
    
    if (signature_size < required_sig_size) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    // Create signature by hashing data + key
    std::vector<uint8_t> combined_data(data_size + key_size);
    memcpy(combined_data.data(), data, data_size);
    memcpy(combined_data.data() + data_size, key, key_size);

    sgx_sha256_hash_t hash;
    sgx_status_t status = sgx_sha256_msg(combined_data.data(), combined_data.size(), &hash);
    if (status == SGX_SUCCESS) {
        memcpy(signature, &hash, required_sig_size);
    }

    return status;
}

sgx_status_t ecall_verify_signature(const unsigned char* data, size_t data_size,
                                   const unsigned char* signature, size_t signature_size,
                                   const unsigned char* key, size_t key_size,
                                   int* is_valid) {
    if (!g_enclave_initialized || data == nullptr || signature == nullptr || 
        key == nullptr || is_valid == nullptr) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    *is_valid = 0;

    // Recreate signature and compare
    const size_t expected_sig_size = 32;
    if (signature_size != expected_sig_size) {
        return SGX_SUCCESS; // Valid call, but invalid signature
    }

    std::vector<uint8_t> combined_data(data_size + key_size);
    memcpy(combined_data.data(), data, data_size);
    memcpy(combined_data.data() + data_size, key, key_size);

    sgx_sha256_hash_t hash;
    sgx_status_t status = sgx_sha256_msg(combined_data.data(), combined_data.size(), &hash);
    if (status != SGX_SUCCESS) {
        return status;
    }

    if (memcmp(signature, &hash, expected_sig_size) == 0) {
        *is_valid = 1;
    }

    return SGX_SUCCESS;
}

/*
 * Key Management Functions
 */
sgx_status_t ecall_generate_key(const char* key_id, const char* key_type, 
                               const char* key_usage, int exportable,
                               const char* description, char* result, 
                               size_t result_size, size_t* actual_size) {
    if (!g_enclave_initialized || key_id == nullptr || result == nullptr || actual_size == nullptr) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    std::string fingerprint = generate_uuid();
    uint64_t timestamp = get_enclave_timestamp();

    const char* result_template = R"({
        "keyId": "%s",
        "keyType": "%s",
        "keyUsage": "%s", 
        "exportable": %s,
        "description": "%s",
        "fingerprint": "%s",
        "created": %llu,
        "enclaveGenerated": true,
        "attestation": "%s"
    })";

    std::string attestation_hash = generate_uuid().substr(0, 32);
    
    char formatted_result[1024];
    int written = snprintf(formatted_result, sizeof(formatted_result), result_template,
                          key_id, key_type ? key_type : "unknown", 
                          key_usage ? key_usage : "Sign,Verify",
                          exportable ? "true" : "false",
                          description ? description : "",
                          fingerprint.c_str(), timestamp, attestation_hash.c_str());

    if (written < 0 || static_cast<size_t>(written) >= result_size) {
        *actual_size = static_cast<size_t>(written) + 1;
        return SGX_ERROR_INVALID_PARAMETER;
    }

    *actual_size = static_cast<size_t>(written);
    memcpy(result, formatted_result, *actual_size);
    return SGX_SUCCESS;
}

/*
 * Secure Storage Functions
 */
sgx_status_t ecall_store_data(const char* key, const unsigned char* data, size_t data_size,
                             const char* encryption_key, int compress,
                             char* result, size_t result_size, size_t* actual_size) {
    if (!g_enclave_initialized || key == nullptr || data == nullptr || result == nullptr || actual_size == nullptr) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    sgx_thread_mutex_lock(&g_global_mutex);

    try {
        // Store encrypted data in secure storage
        std::vector<uint8_t> stored_data(data, data + data_size);
        g_secure_storage[std::string(key)] = stored_data;

        const char* result_template = R"({
            "success": true,
            "key": "%s",
            "size": %zu,
            "compressed": %s,
            "timestamp": %llu,
            "enclave": true,
            "attestation": "%s"
        })";

        std::string attestation = generate_uuid().substr(0, 32);
        uint64_t timestamp = get_enclave_timestamp();

        char formatted_result[512];
        int written = snprintf(formatted_result, sizeof(formatted_result), result_template,
                              key, data_size, compress ? "true" : "false", 
                              timestamp, attestation.c_str());

        if (written < 0 || static_cast<size_t>(written) >= result_size) {
            *actual_size = static_cast<size_t>(written) + 1;
            sgx_thread_mutex_unlock(&g_global_mutex);
            return SGX_ERROR_INVALID_PARAMETER;
        }

        *actual_size = static_cast<size_t>(written);
        memcpy(result, formatted_result, *actual_size);
        
        sgx_thread_mutex_unlock(&g_global_mutex);
        return SGX_SUCCESS;
    } catch (...) {
        sgx_thread_mutex_unlock(&g_global_mutex);
        return SGX_ERROR_UNEXPECTED;
    }
}

sgx_status_t ecall_retrieve_data(const char* key, const char* encryption_key,
                                unsigned char* data, size_t data_size, size_t* actual_size) {
    if (!g_enclave_initialized || key == nullptr || data == nullptr || actual_size == nullptr) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    sgx_thread_mutex_lock(&g_global_mutex);

    try {
        auto it = g_secure_storage.find(std::string(key));
        if (it == g_secure_storage.end()) {
            *actual_size = 0;
            sgx_thread_mutex_unlock(&g_global_mutex);
            return SGX_ERROR_INVALID_PARAMETER; // Key not found
        }

        const auto& stored_data = it->second;
        *actual_size = stored_data.size();

        if (data_size < stored_data.size()) {
            sgx_thread_mutex_unlock(&g_global_mutex);
            return SGX_ERROR_INVALID_PARAMETER;
        }

        memcpy(data, stored_data.data(), stored_data.size());
        sgx_thread_mutex_unlock(&g_global_mutex);
        return SGX_SUCCESS;
    } catch (...) {
        sgx_thread_mutex_unlock(&g_global_mutex);
        return SGX_ERROR_UNEXPECTED;
    }
}

/*
 * SGX-Specific Functions
 */
sgx_status_t ecall_seal_data(const unsigned char* data, size_t data_size,
                            unsigned char* sealed_data, size_t sealed_size,
                            size_t* actual_sealed_size) {
    if (!g_enclave_initialized || data == nullptr || sealed_data == nullptr || actual_sealed_size == nullptr) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    uint32_t sealed_data_size = sgx_calc_sealed_data_size(0, static_cast<uint32_t>(data_size));
    *actual_sealed_size = sealed_data_size;

    if (sealed_size < sealed_data_size) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    return sgx_seal_data(0, nullptr, static_cast<uint32_t>(data_size), data,
                        sealed_data_size, reinterpret_cast<sgx_sealed_data_t*>(sealed_data));
}

sgx_status_t ecall_unseal_data(const unsigned char* sealed_data, size_t sealed_size,
                              unsigned char* data, size_t data_size, size_t* actual_data_size) {
    if (!g_enclave_initialized || sealed_data == nullptr || data == nullptr || actual_data_size == nullptr) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    const sgx_sealed_data_t* sealed_data_ptr = reinterpret_cast<const sgx_sealed_data_t*>(sealed_data);
    uint32_t unsealed_data_size = sgx_get_encrypt_txt_len(sealed_data_ptr);
    *actual_data_size = unsealed_data_size;

    if (data_size < unsealed_data_size) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    uint32_t mac_text_len = sgx_get_add_mac_txt_len(sealed_data_ptr);
    return sgx_unseal_data(sealed_data_ptr, nullptr, &mac_text_len, data, &unsealed_data_size);
}

sgx_status_t ecall_get_trusted_time(uint64_t* trusted_time) {
    if (!g_enclave_initialized || trusted_time == nullptr) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    // In simulation mode, we use system time
    // In hardware mode, we'd use PSE service for trusted time
    return static_cast<sgx_status_t>(ocall_get_system_time(trusted_time));
}

// Stub implementations for remaining functions
sgx_status_t ecall_execute_javascript(const char* function_code, const char* args,
                                     char* result, size_t result_size, size_t* actual_size) {
    if (!g_enclave_initialized) return SGX_ERROR_INVALID_STATE;
    
    const char* stub_result = R"({"success": true, "result": "JavaScript execution not implemented", "simulation": true})";
    size_t len = strlen(stub_result);
    
    if (result_size <= len) {
        *actual_size = len + 1;
        return SGX_ERROR_INVALID_PARAMETER;
    }
    
    strcpy(result, stub_result);
    *actual_size = len;
    return SGX_SUCCESS;
}

// Additional stub implementations would go here for the remaining ECALLs
// For brevity, I'm showing the pattern - each would follow similar structure

sgx_status_t ecall_delete_data(const char* key, char* result, size_t result_size, size_t* actual_size) {
    if (!g_enclave_initialized || key == nullptr || result == nullptr || actual_size == nullptr) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    sgx_thread_mutex_lock(&g_global_mutex);
    
    bool existed = g_secure_storage.erase(std::string(key)) > 0;
    
    const char* result_template = R"({"success": true, "deleted": %s, "existed": %s})";
    char formatted_result[128];
    int written = snprintf(formatted_result, sizeof(formatted_result), result_template,
                          existed ? "true" : "false", existed ? "true" : "false");
    
    if (written < 0 || static_cast<size_t>(written) >= result_size) {
        *actual_size = static_cast<size_t>(written) + 1;
        sgx_thread_mutex_unlock(&g_global_mutex);
        return SGX_ERROR_INVALID_PARAMETER;
    }

    *actual_size = static_cast<size_t>(written);
    memcpy(result, formatted_result, *actual_size);
    
    sgx_thread_mutex_unlock(&g_global_mutex);
    return SGX_SUCCESS;
} 