#ifndef ENCLAVE_CORE_H
#define ENCLAVE_CORE_H

#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

// Core enclave initialization and management
int enclave_init();
int enclave_destroy();

// Error codes
#define ENCLAVE_SUCCESS 0
#define ENCLAVE_ERROR_INVALID_PARAMETER -1
#define ENCLAVE_ERROR_BUFFER_TOO_SMALL -2
#define ENCLAVE_ERROR_OPERATION_FAILED -3
#define ENCLAVE_ERROR_NOT_INITIALIZED -4
#define ENCLAVE_ERROR_ALREADY_INITIALIZED -5
#define ENCLAVE_ERROR_OUT_OF_MEMORY -6
#define ENCLAVE_ERROR_INVALID_STATE -7
#define ENCLAVE_ERROR_TIMEOUT -8
#define ENCLAVE_ERROR_PERMISSION_DENIED -9
#define ENCLAVE_ERROR_NOT_FOUND -10
#define ENCLAVE_ERROR_ALREADY_EXISTS -11
#define ENCLAVE_ERROR_INVALID_FORMAT -12
#define ENCLAVE_ERROR_VERIFICATION_FAILED -13
#define ENCLAVE_ERROR_ENCRYPTION_FAILED -14
#define ENCLAVE_ERROR_DECRYPTION_FAILED -15

// Maximum buffer sizes
#define MAX_FUNCTION_CODE_SIZE 65536    // 64KB
#define MAX_ARGS_SIZE 32768             // 32KB
#define MAX_RESULT_SIZE 1048576         // 1MB
#define MAX_KEY_ID_SIZE 256
#define MAX_DATA_SIZE 16777216          // 16MB
#define MAX_URL_SIZE 2048
#define MAX_HEADERS_SIZE 8192
#define MAX_SCRIPT_SIZE 32768

// Common data structures
typedef struct {
    char* data;
    size_t size;
    size_t capacity;
} enclave_buffer_t;

typedef struct {
    int error_code;
    char error_message[512];
    uint64_t timestamp;
} enclave_result_t;

// Buffer management functions
int enclave_buffer_init(enclave_buffer_t* buffer, size_t initial_capacity);
int enclave_buffer_resize(enclave_buffer_t* buffer, size_t new_capacity);
int enclave_buffer_append(enclave_buffer_t* buffer, const void* data, size_t size);
void enclave_buffer_free(enclave_buffer_t* buffer);

// Utility functions
int enclave_validate_parameters(const void* param1, size_t size1, const void* param2, size_t size2);
int enclave_copy_result(const char* source, size_t source_size, char* dest, size_t dest_capacity, size_t* actual_size);
uint64_t enclave_get_timestamp();
int enclave_generate_uuid(char* uuid_buffer, size_t buffer_size);

#ifdef __cplusplus
}
#endif

#endif // ENCLAVE_CORE_H
