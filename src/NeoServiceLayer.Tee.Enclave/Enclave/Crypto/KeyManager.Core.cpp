#include "KeyManager.h"
#include "NeoServiceLayerEnclave_t.h"
#include <openssl/rand.h>
#include <openssl/aes.h>
#include <openssl/rsa.h>
#include <openssl/ec.h>
#include <openssl/evp.h>
#include <openssl/pem.h>
#include <openssl/err.h>
#include <openssl/sha.h>
#include <nlohmann/json.hpp>
#include <sstream>
#include <iomanip>
#include <chrono>
#include <random>

using json = nlohmann::json;

KeyManager::KeyManager()
    : _initialized(false) {
}

KeyManager::~KeyManager() {
    // Save keys before destruction
    if (_initialized) {
        save_keys();
    }
}

bool KeyManager::initialize() {
    std::lock_guard<std::mutex> lock(_mutex);

    if (_initialized) {
        return true;
    }

    // Initialize OpenSSL
    OpenSSL_add_all_algorithms();

    // Load keys from persistent storage
    if (!load_keys()) {
        ocall_print_string("Failed to load keys from persistent storage, initializing new key set");

        // Generate default keys
        generate_key(KeyType::AES, 256, 365);
        generate_key(KeyType::RSA, 2048, 365);
        generate_key(KeyType::EC, 256, 365);

        // Save keys
        save_keys();
    }

    _initialized = true;
    return true;
}

const KeyInfo* KeyManager::get_key(const std::string& key_id) {
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized && !initialize()) {
        return nullptr;
    }

    auto it = _keys.find(key_id);
    if (it == _keys.end()) {
        return nullptr;
    }

    return &it->second;
}

const KeyInfo* KeyManager::get_active_key(KeyType type) {
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized && !initialize()) {
        return nullptr;
    }

    auto it = _active_keys.find(type);
    if (it == _active_keys.end()) {
        return nullptr;
    }

    return get_key(it->second);
}

std::string KeyManager::rotate_key(KeyType type, int bits, int expiration_days) {
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized && !initialize()) {
        return "";
    }

    // Get current active key
    auto it = _active_keys.find(type);
    if (it != _active_keys.end()) {
        // Deactivate current key
        auto key_it = _keys.find(it->second);
        if (key_it != _keys.end()) {
            key_it->second.active = false;
        }
    }

    // Generate new key
    return generate_key(type, bits, expiration_days);
}

std::vector<std::string> KeyManager::list_keys() {
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized && !initialize()) {
        return {};
    }

    std::vector<std::string> key_ids;
    for (const auto& pair : _keys) {
        key_ids.push_back(pair.first);
    }

    return key_ids;
}

bool KeyManager::delete_key(const std::string& key_id) {
    std::lock_guard<std::mutex> lock(_mutex);

    if (!_initialized && !initialize()) {
        return false;
    }

    // Check if key exists
    auto it = _keys.find(key_id);
    if (it == _keys.end()) {
        return false;
    }

    // Check if key is active
    KeyType type = it->second.type;
    auto active_it = _active_keys.find(type);
    if (active_it != _active_keys.end() && active_it->second == key_id) {
        // Cannot delete active key
        return false;
    }

    // Delete key
    _keys.erase(it);

    // Save keys
    save_keys();

    return true;
}

std::string KeyManager::generate_key_id() {
    // Generate a random key ID
    std::vector<uint8_t> random_bytes(16);
    if (RAND_bytes(random_bytes.data(), random_bytes.size()) != 1) {
        ocall_print_string("Failed to generate random bytes for key ID");
        return "";
    }

    // Convert to hex string
    std::stringstream ss;
    for (const auto& byte : random_bytes) {
        ss << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(byte);
    }

    return ss.str();
}

uint64_t KeyManager::get_current_time() {
    // Get current time in seconds since epoch
    return std::chrono::duration_cast<std::chrono::seconds>(
        std::chrono::system_clock::now().time_since_epoch()).count();
}

uint64_t KeyManager::get_expiration_time(int days) {
    // Get current time
    uint64_t current_time = get_current_time();

    // Add days
    return current_time + (days * 24 * 60 * 60);
}

std::vector<uint8_t> KeyManager::serialize_keys() {
    // Create JSON object
    json keys_json;

    // Add keys
    for (const auto& pair : _keys) {
        const KeyInfo& key_info = pair.second;

        // Convert key data to base64
        std::string key_data_base64;
        // TODO: Implement base64 encoding
        // For now, just convert to hex string
        std::stringstream ss;
        for (const auto& byte : key_info.data) {
            ss << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(byte);
        }
        key_data_base64 = ss.str();

        // Add key info
        keys_json["keys"][key_info.id] = {
            {"type", static_cast<int>(key_info.type)},
            {"data", key_data_base64},
            {"creation_time", key_info.creation_time},
            {"expiration_time", key_info.expiration_time},
            {"active", key_info.active}
        };
    }

    // Add active keys
    for (const auto& pair : _active_keys) {
        keys_json["active_keys"][std::to_string(static_cast<int>(pair.first))] = pair.second;
    }

    // Serialize to string
    std::string keys_str = keys_json.dump();

    // Convert to bytes
    return std::vector<uint8_t>(keys_str.begin(), keys_str.end());
}

bool KeyManager::deserialize_keys(const std::vector<uint8_t>& data) {
    if (data.empty()) {
        return false;
    }

    try {
        // Convert to string
        std::string keys_str(data.begin(), data.end());

        // Parse JSON
        json keys_json = json::parse(keys_str);

        // Clear current keys
        _keys.clear();
        _active_keys.clear();

        // Load keys
        if (keys_json.contains("keys") && keys_json["keys"].is_object()) {
            for (const auto& [key_id, key_info_json] : keys_json["keys"].items()) {
                KeyInfo key_info;
                key_info.id = key_id;
                key_info.type = static_cast<KeyType>(key_info_json["type"].get<int>());
                key_info.creation_time = key_info_json["creation_time"].get<uint64_t>();
                key_info.expiration_time = key_info_json["expiration_time"].get<uint64_t>();
                key_info.active = key_info_json["active"].get<bool>();

                // Convert key data from base64
                std::string key_data_base64 = key_info_json["data"].get<std::string>();
                // TODO: Implement base64 decoding
                // For now, just convert from hex string
                std::vector<uint8_t> key_data;
                for (size_t i = 0; i < key_data_base64.length(); i += 2) {
                    std::string byte_str = key_data_base64.substr(i, 2);
                    uint8_t byte = static_cast<uint8_t>(std::stoi(byte_str, nullptr, 16));
                    key_data.push_back(byte);
                }
                key_info.data = key_data;

                // Add key
                _keys[key_id] = key_info;
            }
        }

        // Load active keys
        if (keys_json.contains("active_keys") && keys_json["active_keys"].is_object()) {
            for (const auto& [type_str, key_id_json] : keys_json["active_keys"].items()) {
                KeyType type = static_cast<KeyType>(std::stoi(type_str));
                std::string key_id = key_id_json.get<std::string>();
                _active_keys[type] = key_id;
            }
        }

        return true;
    }
    catch (const std::exception& ex) {
        ocall_print_string(("Failed to deserialize keys: " + std::string(ex.what())).c_str());
        return false;
    }
}
