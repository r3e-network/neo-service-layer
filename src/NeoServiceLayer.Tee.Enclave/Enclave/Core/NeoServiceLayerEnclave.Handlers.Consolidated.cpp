#include "NeoServiceLayerEnclave.h"
#include "../Occlum/OcclumEnclave.h"
#include <string>
#include <vector>
#include <memory>
#include <iostream>
#include <sstream>
#include <fstream>
#include <mutex>
#include <cstdarg>
#include <cstdio>
#include <cstring>
#include <ctime>
#include <random>
#include <chrono>

// External function declarations for host calls
extern "C" {
    void host_log(const char* message);
    void host_log_message(const char* message);
    void host_send_metric(const char* metric_name, const char* metric_value);
}

// Helper function to log messages
static void log_message(const char* message) {
    host_log(message);
}

static int handle_exception(const std::exception& e) {
    std::string error_message = "Exception: ";
    error_message += e.what();
    log_message(error_message.c_str());
    return -1;
}

static int copy_string_to_buffer(
    const std::string& str,
    char* buffer,
    size_t buffer_size,
    size_t* size_out) {

    if (buffer == nullptr || size_out == nullptr) {
        return -1;
    }

    size_t str_size = str.size() + 1; // Include null terminator
    *size_out = str_size;

    if (buffer_size < str_size) {
        return -2;
    }

    memcpy(buffer, str.c_str(), str_size);
    return 0;
}

static int copy_binary_to_buffer(
    const std::vector<uint8_t>& data,
    unsigned char* buffer,
    size_t buffer_size,
    size_t* size_out) {

    if (buffer == nullptr || size_out == nullptr) {
        return -1;
    }

    *size_out = data.size();
    if (buffer_size < data.size()) {
        return -2;
    }

    memcpy(buffer, data.data(), data.size());
    return 0;
}

int enclave_execute_javascript(
    uint64_t context_id,
    const char* code,
    const char* input,
    const char* user_id,
    const char* function_id,
    char* result_buffer,
    size_t result_size,
    size_t* result_size_out) {

    try {
        if (code == nullptr || input == nullptr || user_id == nullptr ||
            function_id == nullptr || result_buffer == nullptr || result_size_out == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        std::string result = enclave.execute_js_code(
            context_id, code, input, user_id, function_id);

        return copy_string_to_buffer(result, result_buffer, result_size, result_size_out);
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_execute_javascript");
        return -1;
    }
}

int enclave_store_user_secret(
    const char* user_id,
    const char* secret_name,
    const char* secret_value) {

    try {
        if (user_id == nullptr || secret_name == nullptr || secret_value == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        if (!enclave.store_user_secret(user_id, secret_name, secret_value)) {
            return -1;
        }
        return 0;
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_store_user_secret");
        return -1;
    }
}

int enclave_get_user_secret(
    const char* user_id,
    const char* secret_name,
    char* value_buffer,
    size_t value_size,
    size_t* value_size_out) {

    try {
        if (user_id == nullptr || secret_name == nullptr ||
            value_buffer == nullptr || value_size_out == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        std::string secret_value = enclave.get_user_secret(user_id, secret_name);

        return copy_string_to_buffer(secret_value, value_buffer, value_size, value_size_out);
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_get_user_secret");
        return -1;
    }
}

int enclave_delete_user_secret(
    const char* user_id,
    const char* secret_name) {

    try {
        if (user_id == nullptr || secret_name == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        if (!enclave.delete_user_secret(user_id, secret_name)) {
            return -1;
        }
        return 0;
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_delete_user_secret");
        return -1;
    }
}

int enclave_generate_random_bytes(
    size_t length,
    unsigned char* buffer) {

    try {
        if (buffer == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        std::vector<uint8_t> random_bytes = enclave.generate_random_bytes(length);

        if (random_bytes.size() != length) {
            return -1;
        }

        memcpy(buffer, random_bytes.data(), length);
        return 0;
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_generate_random_bytes");
        return -1;
    }
}

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
        std::vector<uint8_t> sealed_vec = enclave.seal_data(data_vec);

        *sealed_size_out = sealed_vec.size();
        if (sealed_size < sealed_vec.size()) {
            return -2;
        }

        memcpy(sealed_data, sealed_vec.data(), sealed_vec.size());
        return 0;
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_seal_data");
        return -1;
    }
}

int enclave_unseal_data(
    const unsigned char* sealed_data,
    size_t sealed_size,
    unsigned char* data,
    size_t data_size,
    size_t* data_size_out) {

    try {
        if (sealed_data == nullptr || data == nullptr || data_size_out == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        std::vector<uint8_t> sealed_vec(sealed_data, sealed_data + sealed_size);
        std::vector<uint8_t> data_vec = enclave.unseal_data(sealed_vec);

        *data_size_out = data_vec.size();
        if (data_size < data_vec.size()) {
            return -2;
        }

        memcpy(data, data_vec.data(), data_vec.size());
        return 0;
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_unseal_data");
        return -1;
    }
}

int enclave_generate_attestation(
    unsigned char* evidence_buffer,
    size_t evidence_size,
    size_t* evidence_size_out) {

    try {
        if (evidence_buffer == nullptr || evidence_size_out == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        std::vector<uint8_t> evidence = enclave.generate_attestation_evidence();

        *evidence_size_out = evidence.size();
        if (evidence_size < evidence.size()) {
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
