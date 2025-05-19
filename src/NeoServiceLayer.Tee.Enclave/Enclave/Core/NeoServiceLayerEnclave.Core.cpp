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

// Implementation of enclave_initialize
int enclave_initialize() {
    try {
        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        if (!enclave.initialize()) {
            log_message("Failed to initialize enclave");
            return -1;
        }
        return 0;
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_initialize");
        return -1;
    }
}

// Implementation of enclave_get_status
int enclave_get_status(
    char* status_buffer,
    size_t status_size,
    size_t* status_size_out) {

    try {
        if (status_buffer == nullptr || status_size_out == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        std::string status = enclave.get_status();

        return copy_string_to_buffer(status, status_buffer, status_size, status_size_out);
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_get_status");
        return -1;
    }
}

// Implementation of enclave_process_message
int enclave_process_message(
    int message_type,
    const char* message_data,
    size_t message_size,
    char* response_buffer,
    size_t response_size,
    size_t* response_size_out) {

    try {
        if (message_data == nullptr || response_buffer == nullptr || response_size_out == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        std::string message(message_data, message_size);
        std::string response = enclave.process_message(message_type, message);

        return copy_string_to_buffer(response, response_buffer, response_size, response_size_out);
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_process_message");
        return -1;
    }
}

// Implementation of enclave_create_js_context
int enclave_create_js_context(
    uint64_t* context_id) {
    try {
        if (context_id == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        *context_id = enclave.create_js_context();
        return 0;
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_create_js_context");
        return -1;
    }
}

// Implementation of enclave_destroy_js_context
int enclave_destroy_js_context(
    uint64_t context_id) {
    try {
        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        if (!enclave.destroy_js_context(context_id)) {
            log_message("Failed to destroy JavaScript context");
            return -1;
        }
        return 0;
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_destroy_js_context");
        return -1;
    }
}

// Implementation of enclave_execute_js_code
int enclave_execute_js_code(
    uint64_t context_id,
    const char* code,
    const char* input,
    const char* user_id,
    const char* function_id,
    char* result_buffer,
    size_t result_buffer_size,
    size_t* result_size_out) {

    try {
        if (code == nullptr || input == nullptr || user_id == nullptr ||
            function_id == nullptr || result_buffer == nullptr || result_size_out == nullptr) {
            return -1;
        }

        OcclumEnclave& enclave = OcclumEnclave::getInstance();
        std::string result = enclave.execute_js_code(
            context_id, code, input, user_id, function_id);

        return copy_string_to_buffer(result, result_buffer, result_buffer_size, result_size_out);
    } catch (const std::exception& e) {
        return handle_exception(e);
    } catch (...) {
        log_message("Unknown exception in enclave_execute_js_code");
        return -1;
    }
}
