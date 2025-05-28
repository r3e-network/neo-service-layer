#ifndef ENCLAVE_STORAGE_H
#define ENCLAVE_STORAGE_H

#include "enclave_core.h"

#ifdef __cplusplus
extern "C" {
#endif

// Storage operations
int enclave_storage_store(
    const char* key,
    const void* data,
    size_t data_size,
    const char* encryption_key,
    int compress,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

int enclave_storage_retrieve(
    const char* key,
    const char* encryption_key,
    void* data,
    size_t data_size,
    size_t* actual_data_size
);

int enclave_storage_delete(
    const char* key,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

int enclave_storage_get_metadata(
    const char* key,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

int enclave_storage_list_keys(
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

int enclave_storage_get_usage(
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

// Storage metadata structure
typedef struct {
    char key[MAX_KEY_ID_SIZE];
    size_t data_size;
    size_t compressed_size;
    int is_compressed;
    int is_encrypted;
    char checksum[64];
    uint64_t created_at;
    uint64_t last_accessed_at;
    uint64_t access_count;
} storage_metadata_t;

// Storage engine structure
typedef struct {
    void* storage_backend;
    int initialized;
    uint64_t total_keys;
    uint64_t total_size_bytes;
    uint64_t available_space_bytes;
    double compression_ratio;
} storage_engine_t;

// Storage engine functions
int storage_engine_init(storage_engine_t* engine);
int storage_engine_destroy(storage_engine_t* engine);
int storage_engine_store(storage_engine_t* engine, const char* key, const void* data, size_t data_size, const storage_metadata_t* metadata);
int storage_engine_retrieve(storage_engine_t* engine, const char* key, void* data, size_t data_size, size_t* actual_size, storage_metadata_t* metadata);
int storage_engine_delete(storage_engine_t* engine, const char* key);
int storage_engine_get_metadata(storage_engine_t* engine, const char* key, storage_metadata_t* metadata);
int storage_engine_list_keys(storage_engine_t* engine, char** keys, size_t max_count, size_t* actual_count);
int storage_engine_get_usage(storage_engine_t* engine, uint64_t* total_keys, uint64_t* total_size, uint64_t* available_space);

// Compression functions
int compress_data(const void* input, size_t input_size, void* output, size_t output_size, size_t* actual_output_size);
int decompress_data(const void* input, size_t input_size, void* output, size_t output_size, size_t* actual_output_size);

// Encryption functions for storage
int encrypt_storage_data(const void* data, size_t data_size, const char* key, void* encrypted_data, size_t encrypted_size, size_t* actual_encrypted_size);
int decrypt_storage_data(const void* encrypted_data, size_t encrypted_size, const char* key, void* data, size_t data_size, size_t* actual_data_size);

// Checksum functions
int calculate_checksum(const void* data, size_t data_size, char* checksum, size_t checksum_size);
int verify_checksum(const void* data, size_t data_size, const char* expected_checksum);

#ifdef __cplusplus
}
#endif

#endif // ENCLAVE_STORAGE_H
