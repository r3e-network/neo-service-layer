/*
 * test_app.cpp - Simple test application for SGX enclave
 * 
 * This application demonstrates basic enclave functionality in simulation mode.
 */

#include "NeoServiceEnclave_u.h"  // Generated from EDL file
#include <iostream>
#include <string>
#include <vector>
#include <cstring>
#include <cstdlib>

// Function to initialize the enclave
sgx_enclave_id_t initialize_enclave() {
    sgx_enclave_id_t eid = 0;
    sgx_status_t ret = SGX_ERROR_UNEXPECTED;
    sgx_launch_token_t token = {0};
    int updated = 0;

    // Create the Enclave with above launch token
    ret = sgx_create_enclave("NeoServiceEnclave.signed.so", SGX_DEBUG_FLAG, &token, &updated, &eid, nullptr);
    if (ret != SGX_SUCCESS) {
        std::cerr << "❌ Failed to create enclave. Error code: 0x" << std::hex << ret << std::endl;
        return 0;
    }

    std::cout << "✅ Enclave created successfully. Enclave ID: " << eid << std::endl;
    return eid;
}

// Function to test basic enclave operations
void test_enclave_operations(sgx_enclave_id_t eid) {
    sgx_status_t ret;
    sgx_status_t enclave_ret;

    std::cout << "\n🔬 Testing enclave operations..." << std::endl;

    // Test 1: Initialize enclave
    std::cout << "1. Initializing enclave..." << std::endl;
    ret = ecall_enclave_init(eid, &enclave_ret);
    if (ret == SGX_SUCCESS && enclave_ret == SGX_SUCCESS) {
        std::cout << "   ✅ Enclave initialization successful" << std::endl;
    } else {
        std::cout << "   ❌ Enclave initialization failed. SGX: 0x" << std::hex << ret 
                  << ", Enclave: 0x" << enclave_ret << std::endl;
        return;
    }

    // Test 2: Generate random numbers
    std::cout << "2. Testing random number generation..." << std::endl;
    int random_result;
    ret = ecall_generate_random(eid, &enclave_ret, 1, 1000, &random_result);
    if (ret == SGX_SUCCESS && enclave_ret == SGX_SUCCESS) {
        std::cout << "   ✅ Generated random number: " << random_result << std::endl;
    } else {
        std::cout << "   ❌ Random generation failed. SGX: 0x" << std::hex << ret 
                  << ", Enclave: 0x" << enclave_ret << std::endl;
    }

    // Test 3: Generate random bytes
    std::cout << "3. Testing random bytes generation..." << std::endl;
    unsigned char random_bytes[32];
    ret = ecall_generate_random_bytes(eid, &enclave_ret, random_bytes, 32);
    if (ret == SGX_SUCCESS && enclave_ret == SGX_SUCCESS) {
        std::cout << "   ✅ Generated random bytes: ";
        for (int i = 0; i < 16; i++) { // Show first 16 bytes
            printf("%02x", random_bytes[i]);
        }
        std::cout << "..." << std::endl;
    } else {
        std::cout << "   ❌ Random bytes generation failed. SGX: 0x" << std::hex << ret 
                  << ", Enclave: 0x" << enclave_ret << std::endl;
    }

    // Test 4: Get attestation report
    std::cout << "4. Testing attestation report..." << std::endl;
    char report_buffer[2048];
    size_t actual_size;
    ret = ecall_get_attestation_report(eid, &enclave_ret, report_buffer, sizeof(report_buffer), &actual_size);
    if (ret == SGX_SUCCESS && enclave_ret == SGX_SUCCESS) {
        report_buffer[actual_size] = '\0';
        std::cout << "   ✅ Attestation report generated (" << actual_size << " bytes)" << std::endl;
        std::cout << "   " << std::string(report_buffer).substr(0, 100) << "..." << std::endl;
    } else {
        std::cout << "   ❌ Attestation report failed. SGX: 0x" << std::hex << ret 
                  << ", Enclave: 0x" << enclave_ret << std::endl;
    }

    // Test 5: Test encryption/decryption
    std::cout << "5. Testing encryption/decryption..." << std::endl;
    const char* test_message = "Hello, SGX enclave!";
    unsigned char key[32] = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
                            17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32};
    
    unsigned char encrypted_data[256];
    size_t encrypted_size;
    ret = ecall_encrypt_data(eid, &enclave_ret,
                            reinterpret_cast<const unsigned char*>(test_message), strlen(test_message),
                            key, sizeof(key),
                            encrypted_data, sizeof(encrypted_data),
                            &encrypted_size);
    
    if (ret == SGX_SUCCESS && enclave_ret == SGX_SUCCESS) {
        std::cout << "   ✅ Encryption successful (" << encrypted_size << " bytes)" << std::endl;
        
        // Test decryption
        unsigned char decrypted_data[256];
        size_t decrypted_size;
        ret = ecall_decrypt_data(eid, &enclave_ret,
                                encrypted_data, encrypted_size,
                                key, sizeof(key),
                                decrypted_data, sizeof(decrypted_data),
                                &decrypted_size);
        
        if (ret == SGX_SUCCESS && enclave_ret == SGX_SUCCESS) {
            decrypted_data[decrypted_size] = '\0';
            std::cout << "   ✅ Decryption successful: " << reinterpret_cast<char*>(decrypted_data) << std::endl;
        } else {
            std::cout << "   ❌ Decryption failed. SGX: 0x" << std::hex << ret 
                      << ", Enclave: 0x" << enclave_ret << std::endl;
        }
    } else {
        std::cout << "   ❌ Encryption failed. SGX: 0x" << std::hex << ret 
                  << ", Enclave: 0x" << enclave_ret << std::endl;
    }

    // Test 6: Test sealing/unsealing
    std::cout << "6. Testing data sealing/unsealing..." << std::endl;
    const char* seal_message = "Sealed data";
    unsigned char sealed_data[512];
    size_t sealed_size;
    
    ret = ecall_seal_data(eid, &enclave_ret,
                         reinterpret_cast<const unsigned char*>(seal_message), strlen(seal_message),
                         sealed_data, sizeof(sealed_data),
                         &sealed_size);
    
    if (ret == SGX_SUCCESS && enclave_ret == SGX_SUCCESS) {
        std::cout << "   ✅ Data sealing successful (" << sealed_size << " bytes)" << std::endl;
        
        // Test unsealing
        unsigned char unsealed_data[256];
        size_t unsealed_size;
        ret = ecall_unseal_data(eid, &enclave_ret,
                               sealed_data, sealed_size,
                               unsealed_data, sizeof(unsealed_data),
                               &unsealed_size);
        
        if (ret == SGX_SUCCESS && enclave_ret == SGX_SUCCESS) {
            unsealed_data[unsealed_size] = '\0';
            std::cout << "   ✅ Data unsealing successful: " << reinterpret_cast<char*>(unsealed_data) << std::endl;
        } else {
            std::cout << "   ❌ Unsealing failed. SGX: 0x" << std::hex << ret 
                      << ", Enclave: 0x" << enclave_ret << std::endl;
        }
    } else {
        std::cout << "   ❌ Sealing failed. SGX: 0x" << std::hex << ret 
                  << ", Enclave: 0x" << enclave_ret << std::endl;
    }

    // Test 7: Get trusted time
    std::cout << "7. Testing trusted time..." << std::endl;
    uint64_t trusted_time;
    ret = ecall_get_trusted_time(eid, &enclave_ret, &trusted_time);
    if (ret == SGX_SUCCESS && enclave_ret == SGX_SUCCESS) {
        std::cout << "   ✅ Trusted time: " << trusted_time << std::endl;
    } else {
        std::cout << "   ❌ Trusted time failed. SGX: 0x" << std::hex << ret 
                  << ", Enclave: 0x" << enclave_ret << std::endl;
    }

    std::cout << "\n🎉 Enclave operation tests completed!" << std::endl;
}

// Function to cleanup and destroy the enclave
void destroy_enclave(sgx_enclave_id_t eid) {
    sgx_status_t ret;
    sgx_status_t enclave_ret;

    // Call enclave cleanup
    ret = ecall_enclave_destroy(eid, &enclave_ret);
    if (ret != SGX_SUCCESS || enclave_ret != SGX_SUCCESS) {
        std::cerr << "⚠️  Enclave cleanup failed. SGX: 0x" << std::hex << ret 
                  << ", Enclave: 0x" << enclave_ret << std::endl;
    }

    // Destroy the enclave
    ret = sgx_destroy_enclave(eid);
    if (ret != SGX_SUCCESS) {
        std::cerr << "⚠️  Failed to destroy enclave. Error code: 0x" << std::hex << ret << std::endl;
    } else {
        std::cout << "✅ Enclave destroyed successfully" << std::endl;
    }
}

int main() {
    std::cout << "🚀 Neo Service Layer SGX Enclave Test Application" << std::endl;
    std::cout << "=================================================" << std::endl;

    // Check environment
    const char* sgx_mode = getenv("SGX_MODE");
    const char* sgx_debug = getenv("SGX_DEBUG");
    
    std::cout << "Environment:" << std::endl;
    std::cout << "  SGX_MODE: " << (sgx_mode ? sgx_mode : "not set") << std::endl;
    std::cout << "  SGX_DEBUG: " << (sgx_debug ? sgx_debug : "not set") << std::endl;
    
    if (!sgx_mode || (strcmp(sgx_mode, "SIM") != 0 && strcmp(sgx_mode, "sim") != 0)) {
        std::cout << "⚠️  SGX_MODE is not set to SIM. This test requires simulation mode." << std::endl;
        std::cout << "   Please run: export SGX_MODE=SIM" << std::endl;
    }

    // Initialize enclave
    sgx_enclave_id_t eid = initialize_enclave();
    if (eid == 0) {
        std::cerr << "❌ Failed to initialize enclave. Exiting..." << std::endl;
        return 1;
    }

    try {
        // Run tests
        test_enclave_operations(eid);
    } catch (const std::exception& e) {
        std::cerr << "❌ Exception during testing: " << e.what() << std::endl;
    } catch (...) {
        std::cerr << "❌ Unknown exception during testing" << std::endl;
    }

    // Cleanup
    destroy_enclave(eid);

    std::cout << "\n✅ Test application completed successfully!" << std::endl;
    return 0;
} 