#ifndef ENCLAVE_UTILS_H
#define ENCLAVE_UTILS_H

#include <string>
#include <vector>
#include "sgx_tcrypto.h"

// Utility functions for the enclave

// Get the MRENCLAVE (enclave measurement)
std::vector<uint8_t> get_mr_enclave();

// Get the MRSIGNER (enclave signer measurement)
std::vector<uint8_t> get_mr_signer();

// Convert a byte array to a hex string
std::string bytes_to_hex_string(const std::vector<uint8_t>& bytes);

// Convert a hex string to a byte array
std::vector<uint8_t> hex_string_to_bytes(const std::string& hex_string);

// Base64 encode a byte array
std::string base64_encode(const std::vector<uint8_t>& bytes);

// Base64 decode a string
std::vector<uint8_t> base64_decode(const std::string& base64_string);

// Get the current timestamp (seconds since epoch)
uint64_t get_current_timestamp();

// Get a random UUID
std::string get_random_uuid();

// Seal data to the enclave
std::vector<uint8_t> seal_data_to_enclave(const std::vector<uint8_t>& data);

// Unseal data that was sealed to the enclave
std::vector<uint8_t> unseal_data_from_enclave(const std::vector<uint8_t>& sealed_data);

// Sign data with the enclave's private key
std::vector<uint8_t> sign_data_with_enclave_key(const std::vector<uint8_t>& data);

// Verify a signature with the enclave's public key
bool verify_signature_with_enclave_key(const std::vector<uint8_t>& data, const std::vector<uint8_t>& signature);

// Get the enclave's public key
sgx_ec256_public_t get_enclave_public_key();

// Get the enclave's report for attestation
sgx_report_t get_enclave_report(const sgx_target_info_t& target_info);

#endif // ENCLAVE_UTILS_H
