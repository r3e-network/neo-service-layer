#include "EnclaveUtils.h"
#include "NeoServiceLayerEnclave_t.h"
#include "sgx_trts.h"
#include "sgx_utils.h"
#include "sgx_tcrypto.h"
#include "sgx_tseal.h"

#include <string>
#include <vector>
#include <sstream>
#include <iomanip>
#include <stdexcept>
#include <cstring>

// External declarations
extern sgx_ec256_private_t g_enclave_private_key;
extern sgx_ec256_public_t g_enclave_public_key;

// Get the MRENCLAVE (enclave measurement)
std::vector<uint8_t> get_mr_enclave() {
    sgx_report_t report;
    sgx_target_info_t target_info = {0};
    sgx_status_t status = sgx_create_report(&target_info, nullptr, &report);

    if (status != SGX_SUCCESS) {
        throw std::runtime_error("Failed to get MRENCLAVE");
    }

    std::vector<uint8_t> mr_enclave(sizeof(report.body.mr_enclave));
    memcpy(mr_enclave.data(), &report.body.mr_enclave, sizeof(report.body.mr_enclave));

    return mr_enclave;
}

// Get the MRSIGNER (enclave signer measurement)
std::vector<uint8_t> get_mr_signer() {
    sgx_report_t report;
    sgx_target_info_t target_info = {0};
    sgx_status_t status = sgx_create_report(&target_info, nullptr, &report);

    if (status != SGX_SUCCESS) {
        throw std::runtime_error("Failed to get MRSIGNER");
    }

    std::vector<uint8_t> mr_signer(sizeof(report.body.mr_signer));
    memcpy(mr_signer.data(), &report.body.mr_signer, sizeof(report.body.mr_signer));

    return mr_signer;
}

// Convert a byte array to a hex string
std::string bytes_to_hex_string(const std::vector<uint8_t>& bytes) {
    std::stringstream ss;
    ss << std::hex << std::setfill('0');

    for (const auto& byte : bytes) {
        ss << std::setw(2) << static_cast<int>(byte);
    }

    return ss.str();
}

// Convert a hex string to a byte array
std::vector<uint8_t> hex_string_to_bytes(const std::string& hex_string) {
    if (hex_string.length() % 2 != 0) {
        throw std::invalid_argument("Hex string must have an even length");
    }

    std::vector<uint8_t> bytes(hex_string.length() / 2);

    for (size_t i = 0; i < hex_string.length(); i += 2) {
        std::string byte_string = hex_string.substr(i, 2);
        bytes[i / 2] = static_cast<uint8_t>(std::stoi(byte_string, nullptr, 16));
    }

    return bytes;
}

// Base64 encode a byte array
std::string base64_encode(const std::vector<uint8_t>& bytes) {
    static const char base64_chars[] =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        "abcdefghijklmnopqrstuvwxyz"
        "0123456789+/";

    std::string encoded;
    encoded.reserve(((bytes.size() + 2) / 3) * 4);

    for (size_t i = 0; i < bytes.size(); i += 3) {
        uint32_t octet_a = i < bytes.size() ? bytes[i] : 0;
        uint32_t octet_b = i + 1 < bytes.size() ? bytes[i + 1] : 0;
        uint32_t octet_c = i + 2 < bytes.size() ? bytes[i + 2] : 0;

        uint32_t triple = (octet_a << 16) + (octet_b << 8) + octet_c;

        encoded.push_back(base64_chars[(triple >> 18) & 0x3F]);
        encoded.push_back(base64_chars[(triple >> 12) & 0x3F]);
        encoded.push_back(base64_chars[(triple >> 6) & 0x3F]);
        encoded.push_back(base64_chars[triple & 0x3F]);
    }

    // Add padding
    switch (bytes.size() % 3) {
        case 1:
            encoded[encoded.size() - 2] = '=';
            encoded[encoded.size() - 1] = '=';
            break;
        case 2:
            encoded[encoded.size() - 1] = '=';
            break;
    }

    return encoded;
}

// Base64 decode a string
std::vector<uint8_t> base64_decode(const std::string& base64_string) {
    static const std::string base64_chars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        "abcdefghijklmnopqrstuvwxyz"
        "0123456789+/";

    std::vector<uint8_t> decoded;
    decoded.reserve(base64_string.length() * 3 / 4);

    uint32_t triple = 0;
    int i = 0;

    for (char c : base64_string) {
        if (c == '=') {
            break;
        }

        size_t value = base64_chars.find(c);
        if (value == std::string::npos) {
            continue; // Skip invalid characters
        }

        triple = (triple << 6) | value;
        i++;

        if (i == 4) {
            decoded.push_back((triple >> 16) & 0xFF);
            decoded.push_back((triple >> 8) & 0xFF);
            decoded.push_back(triple & 0xFF);
            triple = 0;
            i = 0;
        }
    }

    // Handle padding
    if (i == 3) {
        decoded.push_back((triple >> 10) & 0xFF);
        decoded.push_back((triple >> 2) & 0xFF);
    } else if (i == 2) {
        decoded.push_back((triple >> 4) & 0xFF);
    }

    return decoded;
}

// Get the current timestamp (seconds since epoch)
uint64_t get_current_timestamp() {
    // SGX doesn't provide direct access to the system time
    // We need to use a trusted time source

    // Create a buffer for the time request
    sgx_time_source_nonce_t nonce = {0};
    sgx_time_t current_time = 0;

    // Generate a random nonce for the time request
    sgx_status_t status = sgx_read_rand((uint8_t*)&nonce, sizeof(nonce));
    if (status != SGX_SUCCESS) {
        throw std::runtime_error("Failed to generate time request nonce");
    }

    // Get the trusted time from the platform
    status = sgx_get_trusted_time(&current_time, &nonce);
    if (status != SGX_SUCCESS) {
        throw std::runtime_error("Failed to get trusted time");
    }

    // Convert to seconds since epoch
    uint64_t timestamp = static_cast<uint64_t>(current_time);

    return timestamp;
}

// Get a random UUID
std::string get_random_uuid() {
    std::vector<uint8_t> uuid_bytes(16);
    sgx_status_t status = sgx_read_rand(uuid_bytes.data(), uuid_bytes.size());

    if (status != SGX_SUCCESS) {
        throw std::runtime_error("Failed to generate random UUID");
    }

    // Set version (4) and variant (RFC 4122)
    uuid_bytes[6] = (uuid_bytes[6] & 0x0F) | 0x40;
    uuid_bytes[8] = (uuid_bytes[8] & 0x3F) | 0x80;

    std::stringstream ss;
    ss << std::hex << std::setfill('0');

    for (size_t i = 0; i < uuid_bytes.size(); i++) {
        if (i == 4 || i == 6 || i == 8 || i == 10) {
            ss << '-';
        }
        ss << std::setw(2) << static_cast<int>(uuid_bytes[i]);
    }

    return ss.str();
}

// Seal data to the enclave
std::vector<uint8_t> seal_data_to_enclave(const std::vector<uint8_t>& data) {
    // Calculate the size of the sealed data
    uint32_t sealed_data_size = sgx_calc_sealed_data_size(0, data.size());

    if (sealed_data_size == UINT32_MAX) {
        throw std::runtime_error("Failed to calculate sealed data size");
    }

    // Allocate memory for the sealed data
    std::vector<uint8_t> sealed_data(sealed_data_size);

    // Seal the data
    sgx_status_t status = sgx_seal_data(
        0, nullptr, // No additional authenticated data
        data.size(), data.data(),
        sealed_data_size, reinterpret_cast<sgx_sealed_data_t*>(sealed_data.data())
    );

    if (status != SGX_SUCCESS) {
        throw std::runtime_error("Failed to seal data");
    }

    return sealed_data;
}

// Unseal data that was sealed to the enclave
std::vector<uint8_t> unseal_data_from_enclave(const std::vector<uint8_t>& sealed_data) {
    // Get the size of the decrypted data
    uint32_t decrypted_size = sgx_get_encrypt_txt_len(reinterpret_cast<const sgx_sealed_data_t*>(sealed_data.data()));

    if (decrypted_size == UINT32_MAX) {
        throw std::runtime_error("Failed to get decrypted data size");
    }

    // Allocate memory for the decrypted data
    std::vector<uint8_t> decrypted_data(decrypted_size);

    // Unseal the data
    sgx_status_t status = sgx_unseal_data(
        reinterpret_cast<const sgx_sealed_data_t*>(sealed_data.data()),
        nullptr, nullptr, // No additional authenticated data
        decrypted_data.data(), &decrypted_size
    );

    if (status != SGX_SUCCESS) {
        throw std::runtime_error("Failed to unseal data");
    }

    return decrypted_data;
}

// Sign data with the enclave's private key
std::vector<uint8_t> sign_data_with_enclave_key(const std::vector<uint8_t>& data) {
    // Calculate the hash of the data
    sgx_sha256_hash_t hash;
    sgx_status_t status = sgx_sha256_msg(data.data(), data.size(), &hash);

    if (status != SGX_SUCCESS) {
        throw std::runtime_error("Failed to calculate hash");
    }

    // Sign the hash
    sgx_ec256_signature_t signature;
    sgx_ecc_state_handle_t ecc_handle = nullptr;

    status = sgx_ecc256_open_context(&ecc_handle);
    if (status != SGX_SUCCESS) {
        throw std::runtime_error("Failed to open ECC context");
    }

    status = sgx_ecdsa_sign(reinterpret_cast<const uint8_t*>(&hash), sizeof(hash), &g_enclave_private_key, &signature, ecc_handle);

    sgx_ecc256_close_context(ecc_handle);

    if (status != SGX_SUCCESS) {
        throw std::runtime_error("Failed to sign data");
    }

    // Convert the signature to a byte vector
    std::vector<uint8_t> signature_bytes(sizeof(signature));
    memcpy(signature_bytes.data(), &signature, sizeof(signature));

    return signature_bytes;
}

// Verify a signature with the enclave's public key
bool verify_signature_with_enclave_key(const std::vector<uint8_t>& data, const std::vector<uint8_t>& signature) {
    if (signature.size() != sizeof(sgx_ec256_signature_t)) {
        throw std::invalid_argument("Invalid signature size");
    }

    // Calculate the hash of the data
    sgx_sha256_hash_t hash;
    sgx_status_t status = sgx_sha256_msg(data.data(), data.size(), &hash);

    if (status != SGX_SUCCESS) {
        throw std::runtime_error("Failed to calculate hash");
    }

    // Verify the signature
    sgx_ecc_state_handle_t ecc_handle = nullptr;
    status = sgx_ecc256_open_context(&ecc_handle);

    if (status != SGX_SUCCESS) {
        throw std::runtime_error("Failed to open ECC context");
    }

    uint8_t result = 0;
    status = sgx_ecdsa_verify(
        reinterpret_cast<const uint8_t*>(&hash), sizeof(hash),
        &g_enclave_public_key,
        reinterpret_cast<const sgx_ec256_signature_t*>(signature.data()),
        &result,
        ecc_handle
    );

    sgx_ecc256_close_context(ecc_handle);

    if (status != SGX_SUCCESS) {
        throw std::runtime_error("Failed to verify signature");
    }

    return (result == SGX_EC_VALID);
}

// Get the enclave's public key
sgx_ec256_public_t get_enclave_public_key() {
    return g_enclave_public_key;
}

// Get the enclave's report for attestation
sgx_report_t get_enclave_report(const sgx_target_info_t& target_info) {
    sgx_report_t report;
    sgx_report_data_t report_data = {0};

    // Include the public key in the report data
    memcpy(report_data.d, &g_enclave_public_key, sizeof(g_enclave_public_key));

    sgx_status_t status = sgx_create_report(&target_info, &report_data, &report);

    if (status != SGX_SUCCESS) {
        throw std::runtime_error("Failed to create enclave report");
    }

    return report;
}
