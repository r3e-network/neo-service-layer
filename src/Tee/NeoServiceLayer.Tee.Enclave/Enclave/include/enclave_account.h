#ifndef ENCLAVE_ACCOUNT_H
#define ENCLAVE_ACCOUNT_H

#include "enclave_core.h"

#ifdef __cplusplus
extern "C" {
#endif

// Abstract account operations
int enclave_account_create(
    const char* account_id,
    const char* account_data,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

int enclave_account_sign_transaction(
    const char* account_id,
    const char* transaction_data,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

int enclave_account_add_guardian(
    const char* account_id,
    const char* guardian_data,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

int enclave_account_remove_guardian(
    const char* account_id,
    const char* guardian_id,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

int enclave_account_get_info(
    const char* account_id,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

int enclave_account_delete(
    const char* account_id,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

int enclave_account_list_all(
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

// Account types
#define ACCOUNT_TYPE_SIMPLE "Simple"
#define ACCOUNT_TYPE_MULTISIG "MultiSig"
#define ACCOUNT_TYPE_SOCIAL_RECOVERY "SocialRecovery"
#define ACCOUNT_TYPE_TIME_LOCKED "TimeLocked"

// Guardian types
#define GUARDIAN_TYPE_EOA "EOA"
#define GUARDIAN_TYPE_CONTRACT "Contract"
#define GUARDIAN_TYPE_HARDWARE "Hardware"
#define GUARDIAN_TYPE_SOCIAL "Social"

// Account metadata structure
typedef struct {
    char account_id[MAX_KEY_ID_SIZE];
    char account_type[64];
    char owner_address[128];
    char implementation_address[128];
    uint64_t created_at;
    uint64_t last_used_at;
    uint64_t transaction_count;
    int guardian_count;
    int required_confirmations;
    int is_active;
} account_metadata_t;

// Guardian structure
typedef struct {
    char guardian_id[MAX_KEY_ID_SIZE];
    char guardian_type[64];
    char guardian_address[128];
    char guardian_name[256];
    uint64_t added_at;
    uint64_t last_used_at;
    int is_active;
    int weight;
} guardian_t;

// Account engine structure
typedef struct {
    void* account_store;
    void* guardian_store;
    int initialized;
    uint64_t total_accounts;
    uint64_t total_transactions;
} account_engine_t;

// Account engine functions
int account_engine_init(account_engine_t* engine);
int account_engine_destroy(account_engine_t* engine);
int account_engine_create_account(account_engine_t* engine, const account_metadata_t* metadata);
int account_engine_get_account(account_engine_t* engine, const char* account_id, account_metadata_t* metadata);
int account_engine_delete_account(account_engine_t* engine, const char* account_id);
int account_engine_list_accounts(account_engine_t* engine, account_metadata_t* accounts, size_t max_count, size_t* actual_count);
int account_engine_add_guardian(account_engine_t* engine, const char* account_id, const guardian_t* guardian);
int account_engine_remove_guardian(account_engine_t* engine, const char* account_id, const char* guardian_id);
int account_engine_get_guardians(account_engine_t* engine, const char* account_id, guardian_t* guardians, size_t max_count, size_t* actual_count);
int account_engine_sign_transaction(account_engine_t* engine, const char* account_id, const char* transaction_data, char* signature, size_t signature_size, size_t* actual_signature_size);

// Transaction structure
typedef struct {
    char transaction_id[MAX_KEY_ID_SIZE];
    char account_id[MAX_KEY_ID_SIZE];
    char to_address[128];
    char data[MAX_DATA_SIZE];
    size_t data_size;
    uint64_t value;
    uint64_t gas_limit;
    uint64_t gas_price;
    uint64_t nonce;
    uint64_t created_at;
    int confirmation_count;
    int required_confirmations;
    int is_executed;
} transaction_t;

// Transaction functions
int transaction_create(const transaction_t* transaction);
int transaction_get(const char* transaction_id, transaction_t* transaction);
int transaction_confirm(const char* transaction_id, const char* guardian_id);
int transaction_execute(const char* transaction_id, char* result, size_t result_size, size_t* actual_result_size);
int transaction_list_pending(const char* account_id, transaction_t* transactions, size_t max_count, size_t* actual_count);

#ifdef __cplusplus
}
#endif

#endif // ENCLAVE_ACCOUNT_H
