#include "OcclumIntegration.h"
#include <string>
#include <vector>
#include <sstream>
#include <iomanip>
#include <mbedtls/base64.h>

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

// Base64 encoding function
std::string OcclumIntegration::Base64Encode(const std::vector<uint8_t>& data) {
    LOG_INFO("Base64 encoding %zu bytes", data.size());

    if (data.empty()) {
        return "";
    }

    // Calculate the required buffer size
    size_t output_len = 0;
    mbedtls_base64_encode(nullptr, 0, &output_len, data.data(), data.size());

    // Allocate the buffer
    std::vector<unsigned char> output(output_len);

    // Encode the data
    int ret = mbedtls_base64_encode(output.data(), output.size(), &output_len, data.data(), data.size());
    if (ret != 0) {
        LOG_ERROR("Failed to encode data to base64: error code %d", ret);
        return "";
    }

    // Convert to string (excluding null terminator)
    return std::string(reinterpret_cast<char*>(output.data()), output_len - 1);
}

// Base64 decoding function
std::vector<uint8_t> OcclumIntegration::Base64Decode(const std::string& base64_str) {
    LOG_INFO("Base64 decoding %zu bytes", base64_str.size());

    if (base64_str.empty()) {
        return {};
    }

    // Calculate the required buffer size
    size_t output_len = 0;
    mbedtls_base64_decode(nullptr, 0, &output_len, 
                         reinterpret_cast<const unsigned char*>(base64_str.c_str()), base64_str.size());

    // Allocate the buffer
    std::vector<uint8_t> output(output_len);

    // Decode the data
    int ret = mbedtls_base64_decode(output.data(), output.size(), &output_len, 
                                  reinterpret_cast<const unsigned char*>(base64_str.c_str()), base64_str.size());
    if (ret != 0) {
        LOG_ERROR("Failed to decode base64 data: error code %d", ret);
        return {};
    }

    // Resize to actual output length
    output.resize(output_len);
    return output;
}

// Hex encoding function
std::string OcclumIntegration::HexEncode(const std::vector<uint8_t>& data) {
    LOG_INFO("Hex encoding %zu bytes", data.size());

    if (data.empty()) {
        return "";
    }

    std::stringstream ss;
    ss << std::hex << std::setfill('0');

    for (const auto& byte : data) {
        ss << std::setw(2) << static_cast<int>(byte);
    }

    return ss.str();
}

// Hex decoding function
std::vector<uint8_t> OcclumIntegration::HexDecode(const std::string& hex_str) {
    LOG_INFO("Hex decoding %zu bytes", hex_str.size());

    if (hex_str.empty() || hex_str.size() % 2 != 0) {
        LOG_ERROR("Invalid hex string: empty or odd length");
        return {};
    }

    std::vector<uint8_t> output;
    output.reserve(hex_str.size() / 2);

    for (size_t i = 0; i < hex_str.size(); i += 2) {
        std::string byte_str = hex_str.substr(i, 2);
        try {
            uint8_t byte = static_cast<uint8_t>(std::stoi(byte_str, nullptr, 16));
            output.push_back(byte);
        } catch (const std::exception& ex) {
            LOG_ERROR("Failed to decode hex string: %s", ex.what());
            return {};
        }
    }

    return output;
}

// String to bytes conversion
std::vector<uint8_t> OcclumIntegration::StringToBytes(const std::string& str) {
    return std::vector<uint8_t>(str.begin(), str.end());
}

// Bytes to string conversion
std::string OcclumIntegration::BytesToString(const std::vector<uint8_t>& bytes) {
    return std::string(bytes.begin(), bytes.end());
}
