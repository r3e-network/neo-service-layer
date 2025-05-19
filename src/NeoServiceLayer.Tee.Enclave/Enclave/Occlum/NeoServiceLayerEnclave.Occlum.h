#ifndef NEO_SERVICE_LAYER_ENCLAVE_H
#define NEO_SERVICE_LAYER_ENCLAVE_H

#include <stdint.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief Initialize the enclave.
 * 
 * This function initializes the enclave and all its components.
 * 
 * @return 0 on success, error code on failure.
 */
int enclave_initialize();

/**
 * @brief Get the status of the enclave.
 * 
 * This function returns the status of the enclave as a JSON string.
 * 
 * @param status_buffer Buffer to store the status.
 * @param status_size Size of the status buffer.
 * @param status_size_out Pointer to store the size of the status.
 * @return 0 on success, error code on failure.
 */
int enclave_get_status(
    char* status_buffer,
    size_t status_size,
    size_t* status_size_out);

/**
 * @brief Process a message from the host.
 * 
 * This function processes a message from the host and returns a response.
 * 
 * @param message_type Type of the message.
 * @param message_data Message data.
 * @param message_size Size of the message data.
 * @param response_buffer Buffer to store the response.
 * @param response_size Size of the response buffer.
 * @param response_size_out Pointer to store the size of the response.
 * @return 0 on success, error code on failure.
 */
int enclave_process_message(
    int message_type,
    const char* message_data,
    size_t message_size,
    char* response_buffer,
    size_t response_size,
    size_t* response_size_out);

/**
 * @brief Create a JavaScript context.
 * 
 * This function creates a new JavaScript context and returns its ID.
 * 
 * @param context_id Pointer to store the context ID.
 * @return 0 on success, error code on failure.
 */
int enclave_create_js_context(
    uint64_t* context_id);

/**
 * @brief Destroy a JavaScript context.
 * 
 * This function destroys a JavaScript context.
 * 
 * @param context_id ID of the context to destroy.
 * @return 0 on success, error code on failure.
 */
int enclave_destroy_js_context(
    uint64_t context_id);

/**
 * @brief Execute JavaScript code in a context.
 * 
 * This function executes JavaScript code in a context and returns the result.
 * 
 * @param context_id ID of the context to use.
 * @param code JavaScript code to execute.
 * @param input Input data for the JavaScript code.
 * @param user_id ID of the user executing the code.
 * @param function_id ID of the function being executed.
 * @param result_buffer Buffer to store the result.
 * @param result_size Size of the result buffer.
 * @param result_size_out Pointer to store the size of the result.
 * @return 0 on success, error code on failure.
 */
int enclave_execute_js_code(
    uint64_t context_id,
    const char* code,
    const char* input,
    const char* user_id,
    const char* function_id,
    char* result_buffer,
    size_t result_size,
    size_t* result_size_out);

/**
 * @brief Store a user secret.
 * 
 * This function stores a secret for a user.
 * 
 * @param user_id ID of the user.
 * @param secret_name Name of the secret.
 * @param secret_value Value of the secret.
 * @return 0 on success, error code on failure.
 */
int enclave_store_user_secret(
    const char* user_id,
    const char* secret_name,
    const char* secret_value);

/**
 * @brief Get a user secret.
 * 
 * This function retrieves a secret for a user.
 * 
 * @param user_id ID of the user.
 * @param secret_name Name of the secret.
 * @param value_buffer Buffer to store the secret value.
 * @param value_size Size of the value buffer.
 * @param value_size_out Pointer to store the size of the secret value.
 * @return 0 on success, error code on failure.
 */
int enclave_get_user_secret(
    const char* user_id,
    const char* secret_name,
    char* value_buffer,
    size_t value_size,
    size_t* value_size_out);

/**
 * @brief Delete a user secret.
 * 
 * This function deletes a secret for a user.
 * 
 * @param user_id ID of the user.
 * @param secret_name Name of the secret.
 * @return 0 on success, error code on failure.
 */
int enclave_delete_user_secret(
    const char* user_id,
    const char* secret_name);

/**
 * @brief Generate random bytes.
 * 
 * This function generates random bytes.
 * 
 * @param length Number of random bytes to generate.
 * @param buffer Buffer to store the random bytes.
 * @return 0 on success, error code on failure.
 */
int enclave_generate_random_bytes(
    size_t length,
    unsigned char* buffer);

/**
 * @brief Sign data with the enclave's private key.
 * 
 * This function signs data with the enclave's private key.
 * 
 * @param data Data to sign.
 * @param data_size Size of the data.
 * @param signature Buffer to store the signature.
 * @param signature_size Size of the signature buffer.
 * @param signature_size_out Pointer to store the size of the signature.
 * @return 0 on success, error code on failure.
 */
int enclave_sign_data(
    const unsigned char* data,
    size_t data_size,
    unsigned char* signature,
    size_t signature_size,
    size_t* signature_size_out);

/**
 * @brief Verify a signature.
 * 
 * This function verifies a signature.
 * 
 * @param data Data that was signed.
 * @param data_size Size of the data.
 * @param signature Signature to verify.
 * @param signature_size Size of the signature.
 * @param is_valid Pointer to store whether the signature is valid.
 * @return 0 on success, error code on failure.
 */
int enclave_verify_signature(
    const unsigned char* data,
    size_t data_size,
    const unsigned char* signature,
    size_t signature_size,
    bool* is_valid);

/**
 * @brief Seal data with the enclave's sealing key.
 * 
 * This function seals data with the enclave's sealing key.
 * 
 * @param data Data to seal.
 * @param data_size Size of the data.
 * @param sealed_data Buffer to store the sealed data.
 * @param sealed_size Size of the sealed data buffer.
 * @param sealed_size_out Pointer to store the size of the sealed data.
 * @return 0 on success, error code on failure.
 */
int enclave_seal_data(
    const unsigned char* data,
    size_t data_size,
    unsigned char* sealed_data,
    size_t sealed_size,
    size_t* sealed_size_out);

/**
 * @brief Unseal data with the enclave's sealing key.
 * 
 * This function unseals data with the enclave's sealing key.
 * 
 * @param sealed_data Sealed data to unseal.
 * @param sealed_size Size of the sealed data.
 * @param data Buffer to store the unsealed data.
 * @param data_size Size of the data buffer.
 * @param data_size_out Pointer to store the size of the unsealed data.
 * @return 0 on success, error code on failure.
 */
int enclave_unseal_data(
    const unsigned char* sealed_data,
    size_t sealed_size,
    unsigned char* data,
    size_t data_size,
    size_t* data_size_out);

/**
 * @brief Generate attestation evidence.
 * 
 * This function generates attestation evidence for the enclave.
 * 
 * @param evidence_buffer Buffer to store the evidence.
 * @param evidence_size Size of the evidence buffer.
 * @param evidence_size_out Pointer to store the size of the evidence.
 * @return 0 on success, error code on failure.
 */
int enclave_generate_attestation(
    unsigned char* evidence_buffer,
    size_t evidence_size,
    size_t* evidence_size_out);

#ifdef __cplusplus
}
#endif

#endif /* NEO_SERVICE_LAYER_ENCLAVE_H */
