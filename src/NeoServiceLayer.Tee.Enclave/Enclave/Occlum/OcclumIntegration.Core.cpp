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
