#include "enclave_core.h"
#include <cstring>
#include <cstdlib>
#include <chrono>
#include <random>
#include <unistd.h>
#include <sys/stat.h>
#include <fcntl.h>

// Occlum LibOS specific includes
#ifdef __cplusplus
extern "C" {
#endif
#include <occlum_pal_api.h>
#ifdef __cplusplus
}
#endif

// Global state
static bool g_occlum_initialized = false;
static std::mt19937 g_random_generator;
static occlum_pal_attr_t g_occlum_attr;

// Initialize the Occlum LibOS enclave
extern "C" int enclave_init() {
    if (g_occlum_initialized) {
        return ENCLAVE_ERROR_ALREADY_INITIALIZED;
    }

    try {
        // Initialize Occlum LibOS
        memset(&g_occlum_attr, 0, sizeof(g_occlum_attr));
        g_occlum_attr.instance_dir = "/opt/occlum/instance";
        g_occlum_attr.log_level = "info";

        int ret = occlum_pal_init(&g_occlum_attr);
        if (ret != 0) {
            return ENCLAVE_ERROR_OPERATION_FAILED;
        }

        // Initialize random number generator with high-resolution clock
        auto seed = std::chrono::high_resolution_clock::now().time_since_epoch().count();
        g_random_generator.seed(static_cast<unsigned int>(seed));

        // Create secure storage directory
        mkdir("/secure_storage", 0755);

        g_occlum_initialized = true;
        return ENCLAVE_SUCCESS;
    } catch (...) {
        return ENCLAVE_ERROR_OPERATION_FAILED;
    }
}

// Destroy the Occlum LibOS enclave
extern "C" int enclave_destroy() {
    if (!g_occlum_initialized) {
        return ENCLAVE_ERROR_NOT_INITIALIZED;
    }

    try {
        // Cleanup Occlum LibOS
        occlum_pal_destroy();
        g_occlum_initialized = false;
        return ENCLAVE_SUCCESS;
    } catch (...) {
        return ENCLAVE_ERROR_OPERATION_FAILED;
    }
}

// Buffer management functions
extern "C" int enclave_buffer_init(enclave_buffer_t* buffer, size_t initial_capacity) {
    if (!buffer) {
        return ENCLAVE_ERROR_INVALID_PARAMETER;
    }

    buffer->data = static_cast<char*>(malloc(initial_capacity));
    if (!buffer->data) {
        return ENCLAVE_ERROR_OUT_OF_MEMORY;
    }

    buffer->size = 0;
    buffer->capacity = initial_capacity;
    return ENCLAVE_SUCCESS;
}

extern "C" int enclave_buffer_resize(enclave_buffer_t* buffer, size_t new_capacity) {
    if (!buffer || !buffer->data) {
        return ENCLAVE_ERROR_INVALID_PARAMETER;
    }

    char* new_data = static_cast<char*>(realloc(buffer->data, new_capacity));
    if (!new_data) {
        return ENCLAVE_ERROR_OUT_OF_MEMORY;
    }

    buffer->data = new_data;
    buffer->capacity = new_capacity;
    if (buffer->size > new_capacity) {
        buffer->size = new_capacity;
    }

    return ENCLAVE_SUCCESS;
}

extern "C" int enclave_buffer_append(enclave_buffer_t* buffer, const void* data, size_t size) {
    if (!buffer || !buffer->data || !data) {
        return ENCLAVE_ERROR_INVALID_PARAMETER;
    }

    if (buffer->size + size > buffer->capacity) {
        size_t new_capacity = buffer->capacity * 2;
        while (new_capacity < buffer->size + size) {
            new_capacity *= 2;
        }

        int result = enclave_buffer_resize(buffer, new_capacity);
        if (result != ENCLAVE_SUCCESS) {
            return result;
        }
    }

    memcpy(buffer->data + buffer->size, data, size);
    buffer->size += size;
    return ENCLAVE_SUCCESS;
}

extern "C" void enclave_buffer_free(enclave_buffer_t* buffer) {
    if (buffer && buffer->data) {
        free(buffer->data);
        buffer->data = nullptr;
        buffer->size = 0;
        buffer->capacity = 0;
    }
}

// Utility functions
extern "C" int enclave_validate_parameters(const void* param1, size_t size1, const void* param2, size_t size2) {
    if (!param1 || size1 == 0) {
        return ENCLAVE_ERROR_INVALID_PARAMETER;
    }
    if (param2 && size2 == 0) {
        return ENCLAVE_ERROR_INVALID_PARAMETER;
    }
    return ENCLAVE_SUCCESS;
}

extern "C" int enclave_copy_result(const char* source, size_t source_size, char* dest, size_t dest_capacity, size_t* actual_size) {
    if (!source || !dest || !actual_size) {
        return ENCLAVE_ERROR_INVALID_PARAMETER;
    }

    *actual_size = source_size;
    if (source_size > dest_capacity) {
        return ENCLAVE_ERROR_BUFFER_TOO_SMALL;
    }

    memcpy(dest, source, source_size);
    return ENCLAVE_SUCCESS;
}

extern "C" uint64_t enclave_get_timestamp() {
    auto now = std::chrono::system_clock::now();
    auto duration = now.time_since_epoch();
    return std::chrono::duration_cast<std::chrono::milliseconds>(duration).count();
}

extern "C" int enclave_generate_uuid(char* uuid_buffer, size_t buffer_size) {
    if (!uuid_buffer || buffer_size < 37) { // UUID string length + null terminator
        return ENCLAVE_ERROR_INVALID_PARAMETER;
    }

    if (!g_enclave_initialized) {
        return ENCLAVE_ERROR_NOT_INITIALIZED;
    }

    // Generate a simple UUID-like string using random numbers
    std::uniform_int_distribution<int> hex_dist(0, 15);
    const char hex_chars[] = "0123456789abcdef";

    // Format: xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx
    for (int i = 0; i < 36; ++i) {
        if (i == 8 || i == 13 || i == 18 || i == 23) {
            uuid_buffer[i] = '-';
        } else if (i == 14) {
            uuid_buffer[i] = '4'; // Version 4 UUID
        } else if (i == 19) {
            uuid_buffer[i] = hex_chars[8 + (hex_dist(g_random_generator) & 3)]; // Variant bits
        } else {
            uuid_buffer[i] = hex_chars[hex_dist(g_random_generator)];
        }
    }
    uuid_buffer[36] = '\0';

    return ENCLAVE_SUCCESS;
}
