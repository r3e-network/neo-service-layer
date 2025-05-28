#ifndef ENCLAVE_COMPUTE_H
#define ENCLAVE_COMPUTE_H

#include "enclave_core.h"

#ifdef __cplusplus
extern "C" {
#endif

// JavaScript execution functions
int enclave_execute_js(
    const char* function_code,
    size_t function_code_size,
    const char* args,
    size_t args_size,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

// Data retrieval functions
int enclave_get_data(
    const char* data_source,
    size_t data_source_size,
    const char* data_path,
    size_t data_path_size,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

// Enhanced computation execution
int enclave_compute_execute(
    const char* computation_id,
    const char* computation_code,
    const char* parameters,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

// Oracle data fetching
int enclave_oracle_fetch_data(
    const char* url,
    const char* headers,
    const char* processing_script,
    const char* output_format,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

// JavaScript engine management
typedef struct {
    void* js_context;
    void* js_runtime;
    int initialized;
    uint64_t execution_count;
    uint64_t total_execution_time_ms;
} js_engine_t;

// JavaScript engine functions
int js_engine_init(js_engine_t* engine);
int js_engine_destroy(js_engine_t* engine);
int js_engine_execute(js_engine_t* engine, const char* code, const char* args, char* result, size_t result_size, size_t* actual_size);
int js_engine_reset(js_engine_t* engine);

// Computation management
typedef struct {
    char computation_id[MAX_KEY_ID_SIZE];
    char computation_code[MAX_FUNCTION_CODE_SIZE];
    char computation_type[64];
    char description[512];
    uint64_t created_at;
    uint64_t execution_count;
    double average_execution_time_ms;
    uint64_t last_executed_at;
} computation_metadata_t;

// Computation registry functions
int computation_registry_init();
int computation_registry_destroy();
int computation_register(const computation_metadata_t* metadata);
int computation_unregister(const char* computation_id);
int computation_get_metadata(const char* computation_id, computation_metadata_t* metadata);
int computation_list_all(computation_metadata_t* computations, size_t max_count, size_t* actual_count);
int computation_update_stats(const char* computation_id, double execution_time_ms);

#ifdef __cplusplus
}
#endif

#endif // ENCLAVE_COMPUTE_H
