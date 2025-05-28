#ifndef ENCLAVE_CRYPTO_H
#define ENCLAVE_CRYPTO_H

#include "enclave_core.h"

#ifdef __cplusplus
extern "C" {
#endif

// Random number generation
int enclave_generate_random(int min, int max, int* result);
int enclave_generate_random_bytes(char* buffer, size_t length);

// Key management
int enclave_kms_generate_key(
    const char* key_id,
    const char* key_type,
    const char* key_usage,
    int exportable,
    const char* description,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

int enclave_kms_get_key(
    const char* key_id,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

int enclave_kms_delete_key(
    const char* key_id,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

int enclave_kms_list_keys(
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

// Cryptographic operations
int enclave_sign_data(
    const char* data,
    size_t data_size,
    const char* key_id,
    char* signature,
    size_t signature_size,
    size_t* actual_signature_size
);

int enclave_verify_signature(
    const char* data,
    size_t data_size,
    const char* signature,
    size_t signature_size,
    const char* key_id,
    int* is_valid
);

int enclave_encrypt_data(
    const char* data,
    size_t data_size,
    const char* key_id,
    char* encrypted_data,
    size_t encrypted_data_size,
    size_t* actual_encrypted_size
);

int enclave_decrypt_data(
    const char* encrypted_data,
    size_t encrypted_data_size,
    const char* key_id,
    char* decrypted_data,
    size_t decrypted_data_size,
    size_t* actual_decrypted_size
);

// Key types
#define KEY_TYPE_SECP256K1 "Secp256k1"
#define KEY_TYPE_ED25519 "Ed25519"
#define KEY_TYPE_RSA2048 "RSA2048"
#define KEY_TYPE_RSA4096 "RSA4096"
#define KEY_TYPE_AES256 "AES256"

// Key usage flags
#define KEY_USAGE_SIGN 0x01
#define KEY_USAGE_VERIFY 0x02
#define KEY_USAGE_ENCRYPT 0x04
#define KEY_USAGE_DECRYPT 0x08
#define KEY_USAGE_DERIVE 0x10

// Key metadata structure
typedef struct {
    char key_id[MAX_KEY_ID_SIZE];
    char key_type[64];
    int key_usage;
    int exportable;
    char description[512];
    uint64_t created_at;
    uint64_t last_used_at;
    uint64_t usage_count;
} key_metadata_t;

// Key management functions
int key_store_init();
int key_store_destroy();
int key_store_add(const key_metadata_t* metadata, const void* key_data, size_t key_data_size);
int key_store_get(const char* key_id, key_metadata_t* metadata, void* key_data, size_t key_data_size, size_t* actual_size);
int key_store_delete(const char* key_id);
int key_store_list(key_metadata_t* keys, size_t max_count, size_t* actual_count);
int key_store_update_usage(const char* key_id);

// Random number generator
typedef struct {
    void* rng_state;
    int initialized;
    uint64_t bytes_generated;
} secure_rng_t;

int secure_rng_init(secure_rng_t* rng);
int secure_rng_destroy(secure_rng_t* rng);
int secure_rng_generate_bytes(secure_rng_t* rng, void* buffer, size_t size);
int secure_rng_generate_int(secure_rng_t* rng, int min, int max, int* result);

#ifdef __cplusplus
}
#endif

#endif // ENCLAVE_CRYPTO_H
