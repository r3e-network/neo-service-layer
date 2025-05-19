#pragma once

#include <stddef.h>
#include <vector>
#include <string>

/**
 * @brief Integration with Occlum LibOS for running JavaScript code in SGX enclaves
 *
 * This class provides a comprehensive interface for interacting with Occlum LibOS,
 * including enclave operations, JavaScript execution, cryptographic operations,
 * and secure storage.
 */
class OcclumIntegration {
public:
    /**
     * @brief Initialize Occlum
     *
     * @param instanceDir The Occlum instance directory
     * @param logLevel The log level
     * @return true if successful, false otherwise
     */
    static bool Initialize(const char* instanceDir = "/occlum_instance", const char* logLevel = "info");

    /**
     * @brief Execute JavaScript code in Occlum
     *
     * @param code The JavaScript code to execute
     * @param input The input data as JSON
     * @param secrets The secrets as JSON
     * @param functionId The function ID
     * @param userId The user ID
     * @param output Buffer to store the output
     * @param outputSize Size of the output buffer
     * @param outputSizeOut Actual size of the output
     * @return true if successful, false otherwise
     */
    static bool ExecuteJavaScript(
        const char* code,
        const char* input,
        const char* secrets,
        const char* functionId,
        const char* userId,
        char* output,
        size_t outputSize,
        size_t* outputSizeOut);

    /**
     * @brief Clean up Occlum resources
     */
    static void Cleanup();

    /**
     * @brief Get the enclave measurement (MRENCLAVE)
     *
     * @return The MRENCLAVE value as a hex string
     */
    static std::string GetMrEnclave();

    /**
     * @brief Get the enclave signer measurement (MRSIGNER)
     *
     * @return The MRSIGNER value as a hex string
     */
    static std::string GetMrSigner();

    /**
     * @brief Generate a random UUID
     *
     * @return A random UUID as a string
     */
    static std::string GenerateUuid();

    /**
     * @brief Generate random bytes
     *
     * @param length The number of random bytes to generate
     * @return The random bytes
     */
    static std::vector<uint8_t> GenerateRandomBytes(size_t length);

    /**
     * @brief Calculate the SHA-256 hash of data
     *
     * @param data The data to hash
     * @return The SHA-256 hash
     */
    static std::vector<uint8_t> Sha256(const std::vector<uint8_t>& data);

    /**
     * @brief Sign data using the enclave's private key
     *
     * @param data The data to sign
     * @return The signature
     */
    static std::vector<uint8_t> SignData(const std::vector<uint8_t>& data);

    /**
     * @brief Verify a signature using the enclave's public key
     *
     * @param data The data that was signed
     * @param signature The signature to verify
     * @return True if the signature is valid, false otherwise
     */
    static bool VerifySignature(const std::vector<uint8_t>& data, const std::vector<uint8_t>& signature);

    /**
     * @brief Get the enclave's public key
     *
     * @return The enclave's public key
     */
    static std::vector<uint8_t> GetEnclavePublicKey();

    /**
     * @brief Seal data using the enclave's sealing key
     *
     * @param data The data to seal
     * @return The sealed data
     */
    static std::vector<uint8_t> SealData(const std::vector<uint8_t>& data);

    /**
     * @brief Unseal data using the enclave's sealing key
     *
     * @param sealedData The sealed data
     * @return The unsealed data
     */
    static std::vector<uint8_t> UnsealData(const std::vector<uint8_t>& sealedData);

    /**
     * @brief Generate a remote attestation report
     *
     * @return The attestation report
     */
    static std::vector<uint8_t> GenerateAttestationEvidence();

    /**
     * @brief Verify a remote attestation report
     *
     * @param evidence The attestation evidence
     * @param endorsements The attestation endorsements
     * @return True if the attestation is valid, false otherwise
     */
    static bool VerifyAttestationEvidence(const std::vector<uint8_t>& evidence, const std::vector<uint8_t>& endorsements);

    /**
     * @brief Get the current time from the enclave
     *
     * @return The current time in milliseconds since epoch
     */
    static uint64_t GetCurrentTime();

    /**
     * @brief Execute a command in Occlum
     *
     * @param path The path to the executable
     * @param argv The arguments
     * @param env The environment variables
     * @param exitValue Pointer to store the exit value
     * @return True if successful, false otherwise
     */
    static bool ExecuteCommand(
        const char* path,
        const char** argv,
        const char** env,
        int* exitValue);

    /**
     * @brief Encode data to Base64
     *
     * @param data The data to encode
     * @return The Base64-encoded string
     */
    static std::string Base64Encode(const std::vector<uint8_t>& data);

    /**
     * @brief Decode Base64 data
     *
     * @param base64_str The Base64-encoded string
     * @return The decoded data
     */
    static std::vector<uint8_t> Base64Decode(const std::string& base64_str);

    /**
     * @brief Encode data to hexadecimal
     *
     * @param data The data to encode
     * @return The hex-encoded string
     */
    static std::string HexEncode(const std::vector<uint8_t>& data);

    /**
     * @brief Decode hexadecimal data
     *
     * @param hex_str The hex-encoded string
     * @return The decoded data
     */
    static std::vector<uint8_t> HexDecode(const std::string& hex_str);

    /**
     * @brief Convert a string to bytes
     *
     * @param str The string to convert
     * @return The bytes
     */
    static std::vector<uint8_t> StringToBytes(const std::string& str);

    /**
     * @brief Convert bytes to a string
     *
     * @param bytes The bytes to convert
     * @return The string
     */
    static std::string BytesToString(const std::vector<uint8_t>& bytes);
};
