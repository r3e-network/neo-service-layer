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

// External declarations for logging functions
extern "C" {
    extern void log_message(const char* level, const char* format, ...);
}

// Define log macros if not already defined
#ifndef LOG_INFO
#define LOG_INFO(format, ...) log_message("INFO", format, ##__VA_ARGS__)
#endif

#ifndef LOG_ERROR
#define LOG_ERROR(format, ...) log_message("ERROR", format, ##__VA_ARGS__)
#endif

// External declarations for Occlum PAL API
extern "C" {
    typedef struct {
        const char* path;
        const char** argv;
        const char** env;
        int* stdio;
        int* exit_value;
    } occlum_pal_exec_args_t;

    int occlum_pal_exec(const occlum_pal_exec_args_t* args);
}

// External declarations for global variables
extern bool g_occlum_initialized;
extern const char* g_node_path;
extern const char* g_tmp_dir;
extern const char* g_js_file;
extern const char* g_input_file;
extern const char* g_output_file;
extern const char* g_secrets_file;

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
