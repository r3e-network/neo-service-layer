#ifndef ENCLAVE_AI_H
#define ENCLAVE_AI_H

#include "enclave_core.h"

#ifdef __cplusplus
extern "C" {
#endif

// AI model training and prediction
int enclave_ai_train_model(
    const char* model_id,
    const char* model_type,
    const double* training_data,
    size_t data_size,
    const char* parameters,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

int enclave_ai_predict(
    const char* model_id,
    const double* input_data,
    size_t input_size,
    double* output_data,
    size_t output_size,
    size_t* actual_output_size,
    char* result_metadata,
    size_t metadata_size,
    size_t* actual_metadata_size
);

int enclave_ai_get_model_info(
    const char* model_id,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

int enclave_ai_delete_model(
    const char* model_id,
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

int enclave_ai_list_models(
    char* result,
    size_t result_size,
    size_t* actual_result_size
);

// Model types
#define MODEL_TYPE_LINEAR_REGRESSION "LinearRegression"
#define MODEL_TYPE_LOGISTIC_REGRESSION "LogisticRegression"
#define MODEL_TYPE_NEURAL_NETWORK "NeuralNetwork"
#define MODEL_TYPE_DECISION_TREE "DecisionTree"
#define MODEL_TYPE_RANDOM_FOREST "RandomForest"
#define MODEL_TYPE_SVM "SVM"
#define MODEL_TYPE_KMEANS "KMeans"

// AI model metadata
typedef struct {
    char model_id[MAX_KEY_ID_SIZE];
    char model_type[64];
    char description[512];
    size_t input_size;
    size_t output_size;
    uint64_t created_at;
    uint64_t last_trained_at;
    uint64_t prediction_count;
    double accuracy;
    double training_time_ms;
} ai_model_metadata_t;

// AI engine structure
typedef struct {
    void* model_store;
    int initialized;
    uint64_t total_models;
    uint64_t total_predictions;
} ai_engine_t;

// AI engine functions
int ai_engine_init(ai_engine_t* engine);
int ai_engine_destroy(ai_engine_t* engine);
int ai_engine_train_model(ai_engine_t* engine, const ai_model_metadata_t* metadata, const double* training_data, size_t data_size, const char* parameters);
int ai_engine_predict(ai_engine_t* engine, const char* model_id, const double* input_data, size_t input_size, double* output_data, size_t output_size, size_t* actual_output_size);
int ai_engine_get_model(ai_engine_t* engine, const char* model_id, ai_model_metadata_t* metadata);
int ai_engine_delete_model(ai_engine_t* engine, const char* model_id);
int ai_engine_list_models(ai_engine_t* engine, ai_model_metadata_t* models, size_t max_count, size_t* actual_count);

#ifdef __cplusplus
}
#endif

#endif // ENCLAVE_AI_H
