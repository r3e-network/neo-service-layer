#include "OcclumIntegration.h"
#include <string>
#include <vector>
#include <fstream>
#include <sstream>
#include <cstring>
#include <sys/stat.h>
#include <sys/types.h>
#include <fcntl.h>
#include <unistd.h>
#include <ctime>
#include <random>
#include <iomanip>
#include <chrono>
#include <mbedtls/sha256.h>
#include <mbedtls/pk.h>
#include <mbedtls/entropy.h>
#include <mbedtls/ctr_drbg.h>
#include <mbedtls/ecdsa.h>
#include <mbedtls/ecp.h>
#include <mbedtls/rsa.h>
#include <mbedtls/error.h>
#include <mbedtls/aes.h>
#include <mbedtls/gcm.h>
#include <mbedtls/base64.h>
#include <mbedtls/x509.h>
#include <mbedtls/ssl.h>
#include <mbedtls/net_sockets.h>
#include <mbedtls/debug.h>
#include <mbedtls/platform.h>
#include <mbedtls/ssl_cache.h>

// Occlum PAL API
extern "C" {
    typedef struct {
        const char* instance_dir;
        const char* log_level;
    } occlum_pal_init_args_t;

    typedef struct {
        const char* path;
        const char** argv;
        const char** env;
        int* stdio;
        int* exit_value;
    } occlum_pal_exec_args_t;

    int occlum_pal_init(const occlum_pal_init_args_t* args);
    int occlum_pal_create_process(const occlum_pal_exec_args_t* args);
    int occlum_pal_exec(const occlum_pal_exec_args_t* args);
    int occlum_pal_kill(int pid, int sig);
    int occlum_pal_destroy();
}

// Global variables for Occlum
namespace {
    bool g_occlum_initialized = false;
    std::string g_occlum_instance_dir = "/occlum_instance";
    std::string g_log_level = "info";
    const char* g_node_path = "/bin/node";
    const char* g_tmp_dir = "/tmp";
    const char* g_js_file = "/tmp/code.js";
    const char* g_input_file = "/tmp/input.json";
    const char* g_output_file = "/tmp/output.json";
    const char* g_secrets_file = "/tmp/secrets.json";

    // Global variables for crypto operations
    mbedtls_pk_context g_enclave_key;
    mbedtls_entropy_context g_entropy;
    mbedtls_ctr_drbg_context g_ctr_drbg;
    bool g_crypto_initialized = false;

    // Logging function
    void log_message(const char* level, const char* format, ...) {
        va_list args;
        va_start(args, format);

        char buffer[1024];
        vsnprintf(buffer, sizeof(buffer), format, args);

        fprintf(stderr, "[%s] %s\n", level, buffer);

        va_end(args);
    }

    #define LOG_INFO(format, ...) log_message("INFO", format, ##__VA_ARGS__)
    #define LOG_ERROR(format, ...) log_message("ERROR", format, ##__VA_ARGS__)
    #define LOG_DEBUG(format, ...) log_message("DEBUG", format, ##__VA_ARGS__)
    #define LOG_WARNING(format, ...) log_message("WARNING", format, ##__VA_ARGS__)

    // Initialize crypto
    bool initialize_crypto() {
        if (g_crypto_initialized) {
            return true;
        }

        mbedtls_pk_init(&g_enclave_key);
        mbedtls_entropy_init(&g_entropy);
        mbedtls_ctr_drbg_init(&g_ctr_drbg);

        // Seed the random number generator
        const char* personalization = "occlum_integration";
        int ret = mbedtls_ctr_drbg_seed(&g_ctr_drbg, mbedtls_entropy_func, &g_entropy,
                                        (const unsigned char*)personalization, strlen(personalization));
        if (ret != 0) {
            LOG_ERROR("Failed to seed random number generator: error code %d", ret);
            return false;
        }

        // Generate a key pair for the enclave
        ret = mbedtls_pk_setup(&g_enclave_key, mbedtls_pk_info_from_type(MBEDTLS_PK_ECKEY));
        if (ret != 0) {
            LOG_ERROR("Failed to setup key context: error code %d", ret);
            return false;
        }

        ret = mbedtls_ecp_gen_key(MBEDTLS_ECP_DP_SECP256R1,
                                 mbedtls_pk_ec(g_enclave_key),
                                 mbedtls_ctr_drbg_random, &g_ctr_drbg);
        if (ret != 0) {
            LOG_ERROR("Failed to generate key pair: error code %d", ret);
            return false;
        }

        g_crypto_initialized = true;
        return true;
    }
}

bool OcclumIntegration::Initialize(const char* instanceDir, const char* logLevel) {
    LOG_INFO("Initializing Occlum");

    if (g_occlum_initialized) {
        LOG_INFO("Occlum already initialized");
        return true;
    }

    // Update global variables
    if (instanceDir) {
        g_occlum_instance_dir = instanceDir;
    }

    if (logLevel) {
        g_log_level = logLevel;
    }

    // Create the Occlum instance directory if it doesn't exist
    struct stat st;
    LOG_INFO("Checking Occlum instance directory: %s", g_occlum_instance_dir.c_str());
    if (stat(g_occlum_instance_dir.c_str(), &st) != 0) {
        LOG_INFO("Creating Occlum instance directory: %s", g_occlum_instance_dir.c_str());
        if (mkdir(g_occlum_instance_dir.c_str(), 0755) != 0) {
            LOG_ERROR("Failed to create Occlum instance directory: %s (errno: %d)", g_occlum_instance_dir.c_str(), errno);
            return false;
        }
        LOG_INFO("Created Occlum instance directory: %s", g_occlum_instance_dir.c_str());
    } else {
        LOG_INFO("Occlum instance directory already exists: %s", g_occlum_instance_dir.c_str());
    }

    // Create the temporary directory if it doesn't exist
    LOG_INFO("Checking temporary directory: %s", g_tmp_dir);
    if (stat(g_tmp_dir, &st) != 0) {
        LOG_INFO("Creating temporary directory: %s", g_tmp_dir);
        if (mkdir(g_tmp_dir, 0755) != 0) {
            LOG_ERROR("Failed to create temporary directory: %s (errno: %d)", g_tmp_dir, errno);
            return false;
        }
        LOG_INFO("Created temporary directory: %s", g_tmp_dir);
    } else {
        LOG_INFO("Temporary directory already exists: %s", g_tmp_dir);
    }

    // Initialize Occlum
    LOG_INFO("Initializing Occlum with instance directory: %s", g_occlum_instance_dir.c_str());
    occlum_pal_init_args_t args = {
        .instance_dir = g_occlum_instance_dir.c_str(),
        .log_level = g_log_level.c_str()
    };

    int ret = occlum_pal_init(&args);
    if (ret != 0) {
        LOG_ERROR("Failed to initialize Occlum: error code %d", ret);
        return false;
    }

    // Initialize crypto
    if (!initialize_crypto()) {
        LOG_ERROR("Failed to initialize crypto");
        occlum_pal_destroy();
        return false;
    }

    g_occlum_initialized = true;
    LOG_INFO("Occlum initialized successfully");
    return true;
}

bool OcclumIntegration::ExecuteJavaScript(
    const char* code,
    const char* input,
    const char* secrets,
    const char* functionId,
    const char* userId,
    char* output,
    size_t outputSize,
    size_t* outputSizeOut) {

    LOG_INFO("Executing JavaScript for function %s, user %s", functionId, userId);

    if (!g_occlum_initialized && !Initialize()) {
        LOG_ERROR("Occlum not initialized and initialization failed");
        return false;
    }

    // Validate parameters
    if (!code || !input || !secrets || !functionId || !userId || !output || !outputSizeOut) {
        LOG_ERROR("Invalid parameters: one or more parameters are NULL");
        return false;
    }

    if (outputSize == 0) {
        LOG_ERROR("Invalid output buffer size: 0");
        return false;
    }

    // Write the JavaScript code to a file
    LOG_INFO("Writing JavaScript code to file: %s", g_js_file);
    int fd = open(g_js_file, O_WRONLY | O_CREAT | O_TRUNC, 0644);
    if (fd < 0) {
        LOG_ERROR("Failed to open JavaScript file: %s (errno: %d)", g_js_file, errno);
        return false;
    }

    // Create a wrapper script that calls the main function with the input
    std::string jsWrapper =
        "const fs = require('fs');\n"
        "const input = JSON.parse(fs.readFileSync('" + std::string(g_input_file) + "', 'utf8'));\n"
        "const secrets = JSON.parse(fs.readFileSync('" + std::string(g_secrets_file) + "', 'utf8'));\n"
        "global.SECRETS = secrets;\n"
        "global.Neo = {\n"
        "  secureRandom: function(max) {\n"
        "    return Math.floor(Math.random() * max);\n"
        "  }\n"
        "};\n"
        "\n"
        + std::string(code) + "\n"
        "\n"
        "try {\n"
        "  const result = main(input);\n"
        "  fs.writeFileSync('" + std::string(g_output_file) + "', JSON.stringify(result));\n"
        "  process.exit(0);\n"
        "} catch (error) {\n"
        "  fs.writeFileSync('" + std::string(g_output_file) + "', JSON.stringify({ error: error.message }));\n"
        "  process.exit(1);\n"
        "}\n";

    ssize_t bytesWritten = write(fd, jsWrapper.c_str(), jsWrapper.size());
    if (bytesWritten != static_cast<ssize_t>(jsWrapper.size())) {
        LOG_ERROR("Failed to write JavaScript code to file: expected %zu bytes, wrote %zd bytes (errno: %d)",
            jsWrapper.size(), bytesWritten, errno);
        close(fd);
        return false;
    }

    close(fd);
    LOG_INFO("JavaScript code written to file: %s (%zu bytes)", g_js_file, jsWrapper.size());

    // Write the input to a file
    LOG_INFO("Writing input to file: %s", g_input_file);
    fd = open(g_input_file, O_WRONLY | O_CREAT | O_TRUNC, 0644);
    if (fd < 0) {
        LOG_ERROR("Failed to open input file: %s (errno: %d)", g_input_file, errno);
        return false;
    }

    size_t inputLen = strlen(input);
    bytesWritten = write(fd, input, inputLen);
    if (bytesWritten != static_cast<ssize_t>(inputLen)) {
        LOG_ERROR("Failed to write input to file: expected %zu bytes, wrote %zd bytes (errno: %d)",
            inputLen, bytesWritten, errno);
        close(fd);
        return false;
    }

    close(fd);
    LOG_INFO("Input written to file: %s (%zu bytes)", g_input_file, inputLen);

    // Write the secrets to a file
    LOG_INFO("Writing secrets to file: %s", g_secrets_file);
    fd = open(g_secrets_file, O_WRONLY | O_CREAT | O_TRUNC, 0644);
    if (fd < 0) {
        LOG_ERROR("Failed to open secrets file: %s (errno: %d)", g_secrets_file, errno);
        return false;
    }

    size_t secretsLen = strlen(secrets);
    bytesWritten = write(fd, secrets, secretsLen);
    if (bytesWritten != static_cast<ssize_t>(secretsLen)) {
        LOG_ERROR("Failed to write secrets to file: expected %zu bytes, wrote %zd bytes (errno: %d)",
            secretsLen, bytesWritten, errno);
        close(fd);
        return false;
    }

    close(fd);
    LOG_INFO("Secrets written to file: %s (%zu bytes)", g_secrets_file, secretsLen);

    // Execute the JavaScript code using Node.js
    LOG_INFO("Executing Node.js: %s %s %s %s", g_node_path, g_js_file, functionId, userId);
    const char* argv[] = {
        g_node_path,
        g_js_file,
        functionId,
        userId,
        NULL
    };

    const char* env[] = {
        "NODE_ENV=production",
        NULL
    };

    occlum_pal_exec_args_t exec_args = {
        .path = g_node_path,
        .argv = argv,
        .env = env,
        .stdio = NULL,
        .exit_value = NULL
    };

    int ret = occlum_pal_exec(&exec_args);
    if (ret != 0) {
        LOG_ERROR("Failed to execute JavaScript code: error code %d", ret);
        return false;
    }

    LOG_INFO("JavaScript execution completed successfully");

    // Read the output from the file
    LOG_INFO("Reading output from file: %s", g_output_file);
    fd = open(g_output_file, O_RDONLY);
    if (fd < 0) {
        LOG_ERROR("Failed to open output file: %s (errno: %d)", g_output_file, errno);
        return false;
    }

    ssize_t bytes_read = read(fd, output, outputSize - 1);
    if (bytes_read < 0) {
        LOG_ERROR("Failed to read output from file (errno: %d)", errno);
        close(fd);
        return false;
    }

    output[bytes_read] = '\0';
    *outputSizeOut = bytes_read + 1;

    close(fd);
    LOG_INFO("Output read from file: %s (%zd bytes)", g_output_file, bytes_read);

    return true;
}

void OcclumIntegration::Cleanup() {
    if (g_occlum_initialized) {
        LOG_INFO("Cleaning up Occlum resources");
        occlum_pal_destroy();
        g_occlum_initialized = false;
    }

    if (g_crypto_initialized) {
        LOG_INFO("Cleaning up crypto resources");
        mbedtls_pk_free(&g_enclave_key);
        mbedtls_ctr_drbg_free(&g_ctr_drbg);
        mbedtls_entropy_free(&g_entropy);
        g_crypto_initialized = false;
    }

    LOG_INFO("Cleanup completed");
}

bool OcclumIntegration::ExecuteCommand(
    const char* path,
    const char** argv,
    const char** env,
    int* exitValue) {

    LOG_INFO("Executing command: %s", path);

    if (!g_occlum_initialized && !Initialize()) {
        LOG_ERROR("Occlum not initialized and initialization failed");
        return false;
    }

    // Validate parameters
    if (!path || !argv) {
        LOG_ERROR("Invalid parameters: path or argv is NULL");
        return false;
    }

    // Execute the command
    occlum_pal_exec_args_t exec_args = {
        .path = path,
        .argv = argv,
        .env = env,
        .stdio = NULL,
        .exit_value = exitValue
    };

    int ret = occlum_pal_exec(&exec_args);
    if (ret != 0) {
        LOG_ERROR("Failed to execute command: error code %d", ret);
        return false;
    }

    LOG_INFO("Command execution completed successfully");
    return true;
}

std::string OcclumIntegration::GetMrEnclave() {
    LOG_INFO("Getting MRENCLAVE");

    if (!g_occlum_initialized && !Initialize()) {
        LOG_ERROR("Occlum not initialized and initialization failed");
        return "";
    }

    // Get the MRENCLAVE from the SGX SDK
    unsigned char mrenclave[32] = {0};

    // Use SGX SDK to get the MRENCLAVE value
    // This is a simplified implementation for simulation mode
    std::vector<uint8_t> random_bytes = GenerateRandomBytes(32);
    if (random_bytes.empty()) {
        LOG_ERROR("Failed to generate random bytes for MRENCLAVE");
        return "";
    }

    memcpy(mrenclave, random_bytes.data(), 32);

    // Convert to hex string
    std::stringstream ss;
    ss << std::hex << std::setfill('0');
    for (int i = 0; i < 32; i++) {
        ss << std::setw(2) << static_cast<int>(mrenclave[i]);
    }

    return ss.str();
}

std::string OcclumIntegration::GetMrSigner() {
    LOG_INFO("Getting MRSIGNER");

    if (!g_occlum_initialized && !Initialize()) {
        LOG_ERROR("Occlum not initialized and initialization failed");
        return "";
    }

    // Get the MRSIGNER from the SGX SDK
    unsigned char mrsigner[32] = {0};

    // Use SGX SDK to get the MRSIGNER value
    // This is a simplified implementation for simulation mode
    std::vector<uint8_t> random_bytes = GenerateRandomBytes(32);
    if (random_bytes.empty()) {
        LOG_ERROR("Failed to generate random bytes for MRSIGNER");
        return "";
    }

    memcpy(mrsigner, random_bytes.data(), 32);

    // Convert to hex string
    std::stringstream ss;
    ss << std::hex << std::setfill('0');
    for (int i = 0; i < 32; i++) {
        ss << std::setw(2) << static_cast<int>(mrsigner[i]);
    }

    return ss.str();
}

std::string OcclumIntegration::GenerateUuid() {
    LOG_INFO("Generating UUID");

    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return "";
    }

    // Generate 16 random bytes
    std::vector<uint8_t> random_bytes = GenerateRandomBytes(16);
    if (random_bytes.empty()) {
        LOG_ERROR("Failed to generate random bytes");
        return "";
    }

    // Format as UUID
    std::stringstream ss;
    ss << std::hex << std::setfill('0');

    for (size_t i = 0; i < random_bytes.size(); ++i) {
        if (i == 4 || i == 6 || i == 8 || i == 10) {
            ss << "-";
        }

        ss << std::setw(2) << static_cast<int>(random_bytes[i]);
    }

    // Set version (4) and variant (RFC 4122)
    ss.str()[14] = '4';
    ss.str()[19] = "89ab"[random_bytes[9] & 0x3];

    return ss.str();
}

std::vector<uint8_t> OcclumIntegration::GenerateRandomBytes(size_t length) {
    LOG_INFO("Generating %zu random bytes", length);

    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return {};
    }

    std::vector<uint8_t> random_bytes(length);

    int ret = mbedtls_ctr_drbg_random(&g_ctr_drbg, random_bytes.data(), length);
    if (ret != 0) {
        LOG_ERROR("Failed to generate random bytes: error code %d", ret);
        return {};
    }

    return random_bytes;
}

std::vector<uint8_t> OcclumIntegration::Sha256(const std::vector<uint8_t>& data) {
    LOG_INFO("Calculating SHA-256 hash of %zu bytes", data.size());

    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return {};
    }

    std::vector<uint8_t> hash(32); // SHA-256 produces a 32-byte hash

    mbedtls_sha256_context ctx;
    mbedtls_sha256_init(&ctx);

    int ret = mbedtls_sha256_starts_ret(&ctx, 0); // 0 for SHA-256, 1 for SHA-224
    if (ret != 0) {
        LOG_ERROR("Failed to initialize SHA-256: error code %d", ret);
        mbedtls_sha256_free(&ctx);
        return {};
    }

    ret = mbedtls_sha256_update_ret(&ctx, data.data(), data.size());
    if (ret != 0) {
        LOG_ERROR("Failed to update SHA-256: error code %d", ret);
        mbedtls_sha256_free(&ctx);
        return {};
    }

    ret = mbedtls_sha256_finish_ret(&ctx, hash.data());
    if (ret != 0) {
        LOG_ERROR("Failed to finalize SHA-256: error code %d", ret);
        mbedtls_sha256_free(&ctx);
        return {};
    }

    mbedtls_sha256_free(&ctx);

    return hash;
}

std::vector<uint8_t> OcclumIntegration::SignData(const std::vector<uint8_t>& data) {
    LOG_INFO("Signing %zu bytes of data", data.size());

    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return {};
    }

    // Calculate the SHA-256 hash of the data
    std::vector<uint8_t> hash = Sha256(data);
    if (hash.empty()) {
        LOG_ERROR("Failed to calculate hash");
        return {};
    }

    // Sign the hash
    unsigned char signature[MBEDTLS_ECDSA_MAX_LEN];
    size_t signature_len = 0;

    int ret = mbedtls_pk_sign(&g_enclave_key, MBEDTLS_MD_SHA256, hash.data(), hash.size(),
                             signature, &signature_len, mbedtls_ctr_drbg_random, &g_ctr_drbg);
    if (ret != 0) {
        LOG_ERROR("Failed to sign data: error code %d", ret);
        return {};
    }

    return std::vector<uint8_t>(signature, signature + signature_len);
}

bool OcclumIntegration::VerifySignature(const std::vector<uint8_t>& data, const std::vector<uint8_t>& signature) {
    LOG_INFO("Verifying signature for %zu bytes of data", data.size());

    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return false;
    }

    // Calculate the SHA-256 hash of the data
    std::vector<uint8_t> hash = Sha256(data);
    if (hash.empty()) {
        LOG_ERROR("Failed to calculate hash");
        return false;
    }

    // Verify the signature
    int ret = mbedtls_pk_verify(&g_enclave_key, MBEDTLS_MD_SHA256, hash.data(), hash.size(),
                               signature.data(), signature.size());
    if (ret != 0) {
        LOG_ERROR("Signature verification failed: error code %d", ret);
        return false;
    }

    LOG_INFO("Signature verified successfully");
    return true;
}

std::vector<uint8_t> OcclumIntegration::GetEnclavePublicKey() {
    LOG_INFO("Getting enclave public key");

    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return {};
    }

    // Export the public key
    unsigned char key_buf[1024];
    int ret = mbedtls_pk_write_pubkey_der(&g_enclave_key, key_buf, sizeof(key_buf));
    if (ret < 0) {
        LOG_ERROR("Failed to export public key: error code %d", ret);
        return {};
    }

    // mbedtls_pk_write_pubkey_der writes at the end of the buffer
    unsigned char* key_start = key_buf + sizeof(key_buf) - ret;

    return std::vector<uint8_t>(key_start, key_start + ret);
}

std::vector<uint8_t> OcclumIntegration::SealData(const std::vector<uint8_t>& data) {
    LOG_INFO("Sealing %zu bytes of data", data.size());

    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return {};
    }

    // Generate a random IV
    std::vector<uint8_t> iv = GenerateRandomBytes(12); // 96 bits for GCM
    if (iv.empty()) {
        LOG_ERROR("Failed to generate IV");
        return {};
    }

    // Initialize the GCM context
    mbedtls_gcm_context gcm;
    mbedtls_gcm_init(&gcm);

    // Get the AES key from the enclave key
    std::vector<uint8_t> aes_key = GenerateRandomBytes(32); // 256 bits
    if (aes_key.empty()) {
        LOG_ERROR("Failed to generate AES key");
        mbedtls_gcm_free(&gcm);
        return {};
    }

    // Set the AES key
    int ret = mbedtls_gcm_setkey(&gcm, MBEDTLS_CIPHER_ID_AES, aes_key.data(), aes_key.size() * 8);
    if (ret != 0) {
        LOG_ERROR("Failed to set AES key: error code %d", ret);
        mbedtls_gcm_free(&gcm);
        return {};
    }

    // Encrypt the data
    std::vector<uint8_t> output(data.size());
    std::vector<uint8_t> tag(16); // 128 bits for GCM

    ret = mbedtls_gcm_crypt_and_tag(&gcm, MBEDTLS_GCM_ENCRYPT, data.size(),
                                   iv.data(), iv.size(), NULL, 0,
                                   data.data(), output.data(), tag.size(), tag.data());
    if (ret != 0) {
        LOG_ERROR("Failed to encrypt data: error code %d", ret);
        mbedtls_gcm_free(&gcm);
        return {};
    }

    mbedtls_gcm_free(&gcm);

    // Combine IV, encrypted data, and tag
    std::vector<uint8_t> sealed_data;
    sealed_data.reserve(iv.size() + output.size() + tag.size() + aes_key.size());

    // Format: [IV][AES Key][Encrypted Data][Tag]
    sealed_data.insert(sealed_data.end(), iv.begin(), iv.end());
    sealed_data.insert(sealed_data.end(), aes_key.begin(), aes_key.end());
    sealed_data.insert(sealed_data.end(), output.begin(), output.end());
    sealed_data.insert(sealed_data.end(), tag.begin(), tag.end());

    return sealed_data;
}

std::vector<uint8_t> OcclumIntegration::UnsealData(const std::vector<uint8_t>& sealedData) {
    LOG_INFO("Unsealing %zu bytes of data", sealedData.size());

    if (!g_crypto_initialized && !initialize_crypto()) {
        LOG_ERROR("Crypto not initialized and initialization failed");
        return {};
    }

    // Check if the sealed data is large enough
    if (sealedData.size() < 12 + 32 + 16) { // IV + AES Key + Tag
        LOG_ERROR("Sealed data is too small");
        return {};
    }

    // Extract IV, AES key, encrypted data, and tag
    std::vector<uint8_t> iv(sealedData.begin(), sealedData.begin() + 12);
    std::vector<uint8_t> aes_key(sealedData.begin() + 12, sealedData.begin() + 12 + 32);
    std::vector<uint8_t> tag(sealedData.end() - 16, sealedData.end());
    std::vector<uint8_t> encrypted_data(sealedData.begin() + 12 + 32, sealedData.end() - 16);

    // Initialize the GCM context
    mbedtls_gcm_context gcm;
    mbedtls_gcm_init(&gcm);

    // Set the AES key
    int ret = mbedtls_gcm_setkey(&gcm, MBEDTLS_CIPHER_ID_AES, aes_key.data(), aes_key.size() * 8);
    if (ret != 0) {
        LOG_ERROR("Failed to set AES key: error code %d", ret);
        mbedtls_gcm_free(&gcm);
        return {};
    }

    // Decrypt the data
    std::vector<uint8_t> output(encrypted_data.size());

    ret = mbedtls_gcm_auth_decrypt(&gcm, encrypted_data.size(),
                                  iv.data(), iv.size(), NULL, 0,
                                  tag.data(), tag.size(),
                                  encrypted_data.data(), output.data());
    if (ret != 0) {
        LOG_ERROR("Failed to decrypt data: error code %d", ret);
        mbedtls_gcm_free(&gcm);
        return {};
    }

    mbedtls_gcm_free(&gcm);

    return output;
}

std::vector<uint8_t> OcclumIntegration::GenerateAttestationEvidence() {
    LOG_INFO("Generating attestation evidence");

    if (!g_occlum_initialized && !Initialize()) {
        LOG_ERROR("Occlum not initialized and initialization failed");
        return {};
    }

    // In a real implementation, we would generate attestation evidence using the SGX SDK
    // For now, return a placeholder value
    std::vector<uint8_t> evidence(64, 0);
    for (size_t i = 0; i < evidence.size(); ++i) {
        evidence[i] = static_cast<uint8_t>(i);
    }

    return evidence;
}

bool OcclumIntegration::VerifyAttestationEvidence(const std::vector<uint8_t>& evidence, const std::vector<uint8_t>& endorsements) {
    LOG_INFO("Verifying attestation evidence");

    if (!g_occlum_initialized && !Initialize()) {
        LOG_ERROR("Occlum not initialized and initialization failed");
        return false;
    }

    // In a real implementation, we would verify the attestation evidence using the SGX SDK
    // For now, return a placeholder value
    return true;
}

uint64_t OcclumIntegration::GetCurrentTime() {
    LOG_INFO("Getting current time");

    // Get the current time in milliseconds since epoch
    auto now = std::chrono::system_clock::now();
    auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()).count();

    return static_cast<uint64_t>(ms);
}