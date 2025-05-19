#include "SecretManager.h"
#include "OcclumIntegration.h"
#include <mbedtls/base64.h>

#include <stdexcept>
#include <vector>
#include <cstring>
#include <sstream>
#include <mutex>
#include <cstdio>

std::string SecretManager::encrypt_value(const std::string& value)
{
    try
    {
        // Convert the value to a vector of bytes
        std::vector<uint8_t> data(value.begin(), value.end());

        // Use OcclumIntegration to seal the data
        std::vector<uint8_t> sealed_data = OcclumIntegration::SealData(data);

        // Convert to base64 for storage
        return OcclumIntegration::Base64Encode(sealed_data);
    }
    catch (const std::exception& ex)
    {
        secure_log("Error encrypting value: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error encrypting value");
        throw;
    }
}

std::string SecretManager::decrypt_value(const std::string& encrypted_value)
{
    try
    {
        // Decode from base64
        std::vector<uint8_t> sealed_data = OcclumIntegration::Base64Decode(encrypted_value);

        // Use OcclumIntegration to unseal the data
        std::vector<uint8_t> data = OcclumIntegration::UnsealData(sealed_data);

        // Convert back to a string
        return std::string(data.begin(), data.end());
    }
    catch (const std::exception& ex)
    {
        secure_log("Error decrypting value: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error decrypting value");
        throw;
    }
}

void SecretManager::generate_encryption_key()
{
    try
    {
        secure_log("Generating encryption key");

        // Generate a random encryption key using OcclumIntegration
        _encryption_key = OcclumIntegration::GenerateRandomBytes(_encryption_key.size());

        secure_log("Encryption key generated successfully");
    }
    catch (const std::exception& ex)
    {
        secure_log("Error generating encryption key: " + std::string(ex.what()));
        throw;
    }
    catch (...)
    {
        secure_log("Unknown error generating encryption key");
        throw;
    }
}
