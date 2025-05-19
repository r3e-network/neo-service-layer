#include "NeoServiceLayerEnclave.h"
#include "../Occlum/OcclumEnclave.h"
#include <string>
#include <vector>
#include <cstring>

// External function declarations for host calls
extern "C" {
    void host_log(const char* message);
}

// Helper function to log messages
static void log_message(const char* message) {
    host_log(message);
}

// Helper function to convert C++ exceptions to result codes
static int handle_exception(const std::exception& e) {
    std::string error_message = "Exception: ";
    error_message += e.what();
    log_message(error_message.c_str());
    return -1;
}

// Implementation of enclave_sign_data
int enclave_sign_data(
    const unsigned char* data,
    size_t data_size,
    unsigned char* signature,
    size_t signature_size,
    size_t* signature_size_out) {

    try {
        if (data == nullptr || signature == nullptr || signature_size_out == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        std::vector<uint8_t> data_vec(data, data + data_size);
        std::vector<uint8_t> signature_vec = enclave.sign_data(data_vec);

        *signature_size_out = signature_vec.size();

        if (signature_size < signature_vec.size()) {
            return -2;
        }

        memcpy(signature, signature_vec.data(), signature_vec.size());
        return 0;
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_sign_data");
        return -1;
    }
}

// Implementation of enclave_verify_signature
int enclave_verify_signature(
    const unsigned char* data,
    size_t data_size,
    const unsigned char* signature,
    size_t signature_size,
    bool* is_valid) {

    try {
        if (data == nullptr || signature == nullptr || is_valid == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        std::vector<uint8_t> data_vec(data, data + data_size);
        std::vector<uint8_t> signature_vec(signature, signature + signature_size);

        *is_valid = enclave.verify_signature(data_vec, signature_vec);
        return 0;
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_verify_signature");
        return -1;
    }
}

// Implementation of enclave_seal_data
int enclave_seal_data(
    const unsigned char* data,
    size_t data_size,
    unsigned char* sealed_data,
    size_t sealed_size,
    size_t* sealed_size_out) {

    try {
        if (data == nullptr || sealed_data == nullptr || sealed_size_out == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        std::vector<uint8_t> data_vec(data, data + data_size);
        std::vector<uint8_t> sealed_data_vec = enclave.seal_data(data_vec);

        *sealed_size_out = sealed_data_vec.size();

        if (sealed_size < sealed_data_vec.size()) {
            return -2;
        }

        memcpy(sealed_data, sealed_data_vec.data(), sealed_data_vec.size());
        return 0;
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_seal_data");
        return -1;
    }
}

// Implementation of enclave_unseal_data
int enclave_unseal_data(
    const unsigned char* sealed_data,
    size_t sealed_data_size,
    unsigned char* data,
    size_t data_size,
    size_t* data_size_out) {

    try {
        if (sealed_data == nullptr || data == nullptr || data_size_out == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        std::vector<uint8_t> sealed_data_vec(sealed_data, sealed_data + sealed_data_size);
        std::vector<uint8_t> unsealed_data = enclave.unseal_data(sealed_data_vec);

        *data_size_out = unsealed_data.size();

        if (data_size < unsealed_data.size()) {
            return -2;
        }

        memcpy(data, unsealed_data.data(), unsealed_data.size());
        return 0;
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_unseal_data");
        return -1;
    }
}

// Implementation of enclave_generate_attestation
int enclave_generate_attestation(
    unsigned char* evidence_buffer,
    size_t evidence_buffer_size,
    size_t* evidence_size_out) {

    try {
        if (evidence_buffer == nullptr || evidence_size_out == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        std::vector<uint8_t> evidence = enclave.generate_attestation_evidence();

        *evidence_size_out = evidence.size();

        if (evidence_buffer_size < evidence.size()) {
            return -2;
        }

        memcpy(evidence_buffer, evidence.data(), evidence.size());
        return 0;
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_generate_attestation");
        return -1;
    }
}

// External function declarations for JavaScript executor
extern "C" {
    bool InitializeJavaScriptExecutor();
    const char* ExecuteJavaScript(const char* code, const char* filename);
    const char* ExecuteJavaScriptFunction(const char* functionName, const char** args, int argCount);
    void FreeJavaScriptResult(const char* result);
    void CollectJavaScriptGarbage();
    void ShutdownJavaScriptExecutor();
}
