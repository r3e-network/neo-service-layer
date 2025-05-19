#include "NeoServiceLayerEnclave.h"
#include "../Occlum/OcclumEnclave.h"
#include <string>
#include <vector>
#include <cstring>

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

// Helper function to convert C++ exceptions to result codes
static int handle_exception(const std::exception& e) {
    std::string error_message = "Exception: ";
    error_message += e.what();
    log_message(error_message.c_str());
    return -1;
}

// Helper function to copy string to buffer with size check
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

// Implementation of enclave_execute_javascript (legacy interface)
int enclave_execute_javascript(
    const char* code,
    const char* input,
    const char* secrets,
    const char* function_id,
    const char* user_id,
    char* result_buffer,
    size_t result_buffer_size,
    size_t* result_size_out,
    uint64_t* gas_used) {

    try {
        if (code == nullptr || input == nullptr || secrets == nullptr ||
            function_id == nullptr || user_id == nullptr ||
            result_buffer == nullptr || result_size_out == nullptr || gas_used == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        std::string result = enclave.execute_javascript(
            code, input, secrets, function_id, user_id, *gas_used);

        return copy_string_to_buffer(result, result_buffer, result_buffer_size, result_size_out);
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_execute_javascript");
        return -1;
    }
}

// Implementation of enclave_store_user_secret
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
            log_message("Failed to store user secret");
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

// Implementation of enclave_get_user_secret
int enclave_get_user_secret(
    const char* user_id,
    const char* secret_name,
    char* value_buffer,
    size_t value_buffer_size,
    size_t* value_size_out) {

    try {
        if (user_id == nullptr || secret_name == nullptr ||
            value_buffer == nullptr || value_size_out == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        std::string secret_value = enclave.get_user_secret(user_id, secret_name);

        return copy_string_to_buffer(secret_value, value_buffer, value_buffer_size, value_size_out);
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_get_user_secret");
        return -1;
    }
}

// Implementation of enclave_delete_user_secret
int enclave_delete_user_secret(
    const char* user_id,
    const char* secret_name) {

    try {
        if (user_id == nullptr || secret_name == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        if (!enclave.delete_user_secret(user_id, secret_name)) {
            log_message("Failed to delete user secret");
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

// Implementation of enclave_generate_random_bytes
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
            log_message("Failed to generate random bytes");
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
