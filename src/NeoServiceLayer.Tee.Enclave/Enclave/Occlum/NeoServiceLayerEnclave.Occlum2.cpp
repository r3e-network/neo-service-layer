#include "NeoServiceLayerEnclave.Occlum.h"
#include "OcclumIntegration.h"
#include <string>
#include <vector>
#include <cstdio>

// Generate random bytes
int enclave_generate_random_bytes(
    size_t length,
    unsigned char* buffer) {
    try {
        // Generate random bytes
        std::vector<uint8_t> random_bytes = OcclumIntegration::GenerateRandomBytes(length);
        if (random_bytes.empty()) {
            fprintf(stderr, "Failed to generate random bytes\n");
            return -1;
        }

        // Copy to buffer
        memcpy(buffer, random_bytes.data(), random_bytes.size());

        return 0;
    }
    catch (const std::exception& ex) {
        fprintf(stderr, "Error generating random bytes: %s\n", ex.what());
        return -1;
    }
    catch (...) {
        fprintf(stderr, "Unknown error generating random bytes\n");
        return -1;
    }
}

// Sign data with the enclave's private key
int enclave_sign_data(
    const unsigned char* data,
    size_t data_size,
    unsigned char* signature,
    size_t signature_size,
    size_t* signature_size_out) {
    try {
        // Convert data to vector
        std::vector<uint8_t> data_vec(data, data + data_size);

        // Sign the data
        std::vector<uint8_t> signature_vec = OcclumIntegration::SignData(data_vec);
        if (signature_vec.empty()) {
            fprintf(stderr, "Failed to sign data\n");
            return -1;
        }

        // Check if buffer is large enough
        if (signature_vec.size() > signature_size) {
            *signature_size_out = signature_vec.size();
            return -1;
        }

        // Copy to buffer
        memcpy(signature, signature_vec.data(), signature_vec.size());
        *signature_size_out = signature_vec.size();

        return 0;
    }
    catch (const std::exception& ex) {
        fprintf(stderr, "Error signing data: %s\n", ex.what());
        return -1;
    }
    catch (...) {
        fprintf(stderr, "Unknown error signing data\n");
        return -1;
    }
}

// Verify a signature
int enclave_verify_signature(
    const unsigned char* data,
    size_t data_size,
    const unsigned char* signature,
    size_t signature_size,
    bool* is_valid) {
    try {
        // Convert data and signature to vectors
        std::vector<uint8_t> data_vec(data, data + data_size);
        std::vector<uint8_t> signature_vec(signature, signature + signature_size);

        // Verify the signature
        *is_valid = OcclumIntegration::VerifySignature(data_vec, signature_vec);

        return 0;
    }
    catch (const std::exception& ex) {
        fprintf(stderr, "Error verifying signature: %s\n", ex.what());
        return -1;
    }
    catch (...) {
        fprintf(stderr, "Unknown error verifying signature\n");
        return -1;
    }
}

// Seal data with the enclave's sealing key
int enclave_seal_data(
    const unsigned char* data,
    size_t data_size,
    unsigned char* sealed_data,
    size_t sealed_size,
    size_t* sealed_size_out) {
    try {
        // Convert data to vector
        std::vector<uint8_t> data_vec(data, data + data_size);

        // Seal the data
        std::vector<uint8_t> sealed_vec = OcclumIntegration::SealData(data_vec);
        if (sealed_vec.empty()) {
            fprintf(stderr, "Failed to seal data\n");
            return -1;
        }

        // Check if buffer is large enough
        if (sealed_vec.size() > sealed_size) {
            *sealed_size_out = sealed_vec.size();
            return -1;
        }

        // Copy to buffer
        memcpy(sealed_data, sealed_vec.data(), sealed_vec.size());
        *sealed_size_out = sealed_vec.size();

        return 0;
    }
    catch (const std::exception& ex) {
        fprintf(stderr, "Error sealing data: %s\n", ex.what());
        return -1;
    }
    catch (...) {
        fprintf(stderr, "Unknown error sealing data\n");
        return -1;
    }
}

// Unseal data with the enclave's sealing key
int enclave_unseal_data(
    const unsigned char* sealed_data,
    size_t sealed_size,
    unsigned char* data,
    size_t data_size,
    size_t* data_size_out) {
    try {
        // Convert sealed data to vector
        std::vector<uint8_t> sealed_vec(sealed_data, sealed_data + sealed_size);

        // Unseal the data
        std::vector<uint8_t> data_vec = OcclumIntegration::UnsealData(sealed_vec);
        if (data_vec.empty()) {
            fprintf(stderr, "Failed to unseal data\n");
            return -1;
        }

        // Check if buffer is large enough
        if (data_vec.size() > data_size) {
            *data_size_out = data_vec.size();
            return -1;
        }

        // Copy to buffer
        memcpy(data, data_vec.data(), data_vec.size());
        *data_size_out = data_vec.size();

        return 0;
    }
    catch (const std::exception& ex) {
        fprintf(stderr, "Error unsealing data: %s\n", ex.what());
        return -1;
    }
    catch (...) {
        fprintf(stderr, "Unknown error unsealing data\n");
        return -1;
    }
}

// Generate attestation evidence
int enclave_generate_attestation(
    unsigned char* evidence_buffer,
    size_t evidence_size,
    size_t* evidence_size_out) {
    try {
        // Generate attestation evidence
        std::vector<uint8_t> evidence = OcclumIntegration::GenerateAttestationEvidence();
        if (evidence.empty()) {
            fprintf(stderr, "Failed to generate attestation evidence\n");
            return -1;
        }

        // Check if buffer is large enough
        if (evidence.size() > evidence_size) {
            *evidence_size_out = evidence.size();
            return -1;
        }

        // Copy to buffer
        memcpy(evidence_buffer, evidence.data(), evidence.size());
        *evidence_size_out = evidence.size();

        return 0;
    }
    catch (const std::exception& ex) {
        fprintf(stderr, "Error generating attestation evidence: %s\n", ex.what());
        return -1;
    }
    catch (...) {
        fprintf(stderr, "Unknown error generating attestation evidence\n");
        return -1;
    }
}
