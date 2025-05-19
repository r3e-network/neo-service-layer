#include "RandomnessService.h"
#include "../Core/EnclaveUtils.h"
#include "../Occlum/OcclumEnclave.h"
#include "KeyManager.h"
#include <nlohmann/json.hpp>
#include <vector>
#include <string>
#include <cstring>
#include <stdexcept>
#include <chrono>
#include <random>
#include <sstream>
#include <iomanip>
#include <openssl/sha.h>
#include <openssl/rand.h>

// External function declarations for host calls
extern "C" {
    void host_log(const char* message);
}

using json = nlohmann::json;

// Implementation class for RandomnessService
class RandomnessService::Impl {
public:
    Impl() : initialized_(false) {}

    ~Impl() {}

    bool initialize() {
        if (initialized_) {
            return true;
        }

        try {
            // Initialize the random number generator
            std::vector<uint8_t> seed_data = get_entropy();
            std::seed_seq seed(seed_data.begin(), seed_data.end());
            rng_.seed(seed);

            initialized_ = true;
            return true;
        } catch (const std::exception& e) {
            host_log(e.what());
            return false;
        } catch (...) {
            host_log("Unknown error initializing RandomnessService");
            return false;
        }
    }

    bool is_initialized() const {
        return initialized_;
    }

    uint64_t generate_random_number(uint64_t min, uint64_t max, const std::string& user_id, const std::string& request_id) {
        if (!initialized_) {
            throw std::runtime_error("RandomnessService not initialized");
        }

        if (min > max) {
            throw std::runtime_error("Minimum value cannot be greater than maximum value");
        }

        // Generate a random number
        std::uniform_int_distribution<uint64_t> dist(min, max);
        uint64_t random_number = dist(rng_);

        // Store the random number and its proof
        std::string proof = get_random_number_proof(random_number, min, max, user_id, request_id);
        random_numbers_[request_id] = {random_number, min, max, user_id, proof};

        return random_number;
    }

    std::vector<uint8_t> generate_random_bytes(size_t length, const std::string& user_id, const std::string& request_id) {
        if (!initialized_) {
            throw std::runtime_error("RandomnessService not initialized");
        }

        // Generate random bytes
        std::vector<uint8_t> random_bytes(length);
        std::uniform_int_distribution<uint16_t> dist(0, 255);
        for (size_t i = 0; i < length; i++) {
            random_bytes[i] = static_cast<uint8_t>(dist(rng_));
        }

        // Store the random bytes and their proof
        std::string proof = generate_proof(random_bytes);
        random_bytes_[request_id] = {random_bytes, user_id, proof};

        return random_bytes;
    }

    bool verify_random_number(uint64_t random_number, uint64_t min, uint64_t max,
                             const std::string& user_id, const std::string& request_id,
                             const std::string& proof) {
        if (!initialized_) {
            throw std::runtime_error("RandomnessService not initialized");
        }

        // Check if we have this random number in our records
        auto it = random_numbers_.find(request_id);
        if (it != random_numbers_.end()) {
            // Check if the random number matches our records
            if (it->second.random_number == random_number &&
                it->second.min == min &&
                it->second.max == max &&
                it->second.user_id == user_id &&
                it->second.proof == proof) {
                return true;
            }
        }

        // If we don't have this random number in our records, verify the proof
        std::string expected_proof = get_random_number_proof(random_number, min, max, user_id, request_id);
        return expected_proof == proof;
    }

    std::string get_random_number_proof(uint64_t random_number, uint64_t min, uint64_t max,
                                       const std::string& user_id, const std::string& request_id) {
        if (!initialized_) {
            throw std::runtime_error("RandomnessService not initialized");
        }

        // Create a JSON object with the random number information
        json j = {
            {"random_number", random_number},
            {"min", min},
            {"max", max},
            {"user_id", user_id},
            {"request_id", request_id},
            {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(
                std::chrono::system_clock::now().time_since_epoch()).count()}
        };

        // Convert the JSON object to a string
        std::string data = j.dump();

        // Generate a proof for the data
        return generate_proof(std::vector<uint8_t>(data.begin(), data.end()));
    }

    std::string generate_seed(const std::string& user_id, const std::string& request_id) {
        if (!initialized_) {
            throw std::runtime_error("RandomnessService not initialized");
        }

        // Generate a random seed
        std::vector<uint8_t> seed_data = get_entropy();

        // Convert the seed to a hex string
        std::stringstream ss;
        for (auto b : seed_data) {
            ss << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(b);
        }
        std::string seed = ss.str();

        // Store the seed and its proof
        std::string proof = get_seed_proof(seed, user_id, request_id);
        seeds_[request_id] = {seed, user_id, proof};

        return seed;
    }

    bool verify_seed(const std::string& seed, const std::string& user_id,
                    const std::string& request_id, const std::string& proof) {
        if (!initialized_) {
            throw std::runtime_error("RandomnessService not initialized");
        }

        // Check if we have this seed in our records
        auto it = seeds_.find(request_id);
        if (it != seeds_.end()) {
            // Check if the seed matches our records
            if (it->second.seed == seed &&
                it->second.user_id == user_id &&
                it->second.proof == proof) {
                return true;
            }
        }

        // If we don't have this seed in our records, verify the proof
        std::string expected_proof = get_seed_proof(seed, user_id, request_id);
        return expected_proof == proof;
    }

    std::string get_seed_proof(const std::string& seed, const std::string& user_id,
                              const std::string& request_id) {
        if (!initialized_) {
            throw std::runtime_error("RandomnessService not initialized");
        }

        // Create a JSON object with the seed information
        json j = {
            {"seed", seed},
            {"user_id", user_id},
            {"request_id", request_id},
            {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(
                std::chrono::system_clock::now().time_since_epoch()).count()}
        };

        // Convert the JSON object to a string
        std::string data = j.dump();

        // Generate a proof for the data
        return generate_proof(std::vector<uint8_t>(data.begin(), data.end()));
    }

    std::vector<uint8_t> get_entropy() {
        // Get random bytes from the enclave
        std::vector<uint8_t> entropy(32);
        RAND_bytes(entropy.data(), entropy.size());
        return entropy;
    }

    std::string generate_proof(const std::vector<uint8_t>& data) {
        // Compute the SHA-256 hash of the data
        std::vector<uint8_t> hash(SHA256_DIGEST_LENGTH);
        SHA256_CTX sha256;
        SHA256_Init(&sha256);
        SHA256_Update(&sha256, data.data(), data.size());
        SHA256_Final(hash.data(), &sha256);

        // Sign the hash with the enclave's private key
        std::vector<uint8_t> signature = sign_data(hash);

        // Convert the signature to a base64 string
        return base64_encode(signature);
    }

    bool verify_proof(const std::vector<uint8_t>& data, const std::string& proof) {
        // Compute the SHA-256 hash of the data
        std::vector<uint8_t> hash(SHA256_DIGEST_LENGTH);
        SHA256_CTX sha256;
        SHA256_Init(&sha256);
        SHA256_Update(&sha256, data.data(), data.size());
        SHA256_Final(hash.data(), &sha256);

        // Decode the proof from base64
        std::vector<uint8_t> signature = base64_decode(proof);

        // Verify the signature
        return verify_signature(hash, signature);
    }

    std::vector<uint8_t> sign_data(const std::vector<uint8_t>& data) {
        // Get the key manager from the OcclumEnclave
        KeyManager* key_manager = OcclumEnclave::getInstance().get_key_manager();
        if (!key_manager) {
            throw std::runtime_error("KeyManager not available");
        }

        // Get the active signing key
        const KeyInfo* key_info = key_manager->get_active_key(KeyType::EC);
        if (!key_info) {
            // If no active key exists, create one
            std::string key_id = key_manager->create_key(KeyType::EC, "randomness_service_key");
            key_info = key_manager->get_key(key_id);
            if (!key_info) {
                throw std::runtime_error("Failed to create signing key");
            }
        }

        // Sign the data with the key
        return key_manager->sign(key_info->id, data);
    }

    bool verify_signature(const std::vector<uint8_t>& data, const std::vector<uint8_t>& signature) {
        // Get the key manager from the OcclumEnclave
        KeyManager* key_manager = OcclumEnclave::getInstance().get_key_manager();
        if (!key_manager) {
            throw std::runtime_error("KeyManager not available");
        }

        // Get the active verification key
        const KeyInfo* key_info = key_manager->get_active_key(KeyType::EC);
        if (!key_info) {
            throw std::runtime_error("No verification key available");
        }

        // Verify the signature with the key
        return key_manager->verify(key_info->id, data, signature);
    }

    // Base64 encoding and decoding functions
    std::string base64_encode(const std::vector<uint8_t>& data) {
        static const char* base64_chars = 
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        
        std::string result;
        int i = 0;
        int j = 0;
        unsigned char char_array_3[3];
        unsigned char char_array_4[4];
        
        for (const auto& byte : data) {
            char_array_3[i++] = byte;
            if (i == 3) {
                char_array_4[0] = (char_array_3[0] & 0xfc) >> 2;
                char_array_4[1] = ((char_array_3[0] & 0x03) << 4) + ((char_array_3[1] & 0xf0) >> 4);
                char_array_4[2] = ((char_array_3[1] & 0x0f) << 2) + ((char_array_3[2] & 0xc0) >> 6);
                char_array_4[3] = char_array_3[2] & 0x3f;
                
                for (i = 0; i < 4; i++) {
                    result += base64_chars[char_array_4[i]];
                }
                i = 0;
            }
        }
        
        if (i) {
            for (j = i; j < 3; j++) {
                char_array_3[j] = '\0';
            }
            
            char_array_4[0] = (char_array_3[0] & 0xfc) >> 2;
            char_array_4[1] = ((char_array_3[0] & 0x03) << 4) + ((char_array_3[1] & 0xf0) >> 4);
            char_array_4[2] = ((char_array_3[1] & 0x0f) << 2) + ((char_array_3[2] & 0xc0) >> 6);
            
            for (j = 0; j < i + 1; j++) {
                result += base64_chars[char_array_4[j]];
            }
            
            while (i++ < 3) {
                result += '=';
            }
        }
        
        return result;
    }

    std::vector<uint8_t> base64_decode(const std::string& encoded_string) {
        static const std::string base64_chars = 
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
            
        std::vector<uint8_t> result;
        int i = 0;
        int j = 0;
        int in_ = 0;
        unsigned char char_array_4[4], char_array_3[3];
        
        for (const auto& c : encoded_string) {
            if (c == '=') {
                break;
            }
            
            if (base64_chars.find(c) == std::string::npos) {
                continue;
            }
            
            char_array_4[i++] = c;
            if (i == 4) {
                for (i = 0; i < 4; i++) {
                    char_array_4[i] = base64_chars.find(char_array_4[i]);
                }
                
                char_array_3[0] = (char_array_4[0] << 2) + ((char_array_4[1] & 0x30) >> 4);
                char_array_3[1] = ((char_array_4[1] & 0xf) << 4) + ((char_array_4[2] & 0x3c) >> 2);
                char_array_3[2] = ((char_array_4[2] & 0x3) << 6) + char_array_4[3];
                
                for (i = 0; i < 3; i++) {
                    result.push_back(char_array_3[i]);
                }
                i = 0;
            }
        }
        
        if (i) {
            for (j = 0; j < i; j++) {
                char_array_4[j] = base64_chars.find(char_array_4[j]);
            }
            
            char_array_3[0] = (char_array_4[0] << 2) + ((char_array_4[1] & 0x30) >> 4);
            char_array_3[1] = ((char_array_4[1] & 0xf) << 4) + ((char_array_4[2] & 0x3c) >> 2);
            
            for (j = 0; j < i - 1; j++) {
                result.push_back(char_array_3[j]);
            }
        }
        
        return result;
    }

private:
    bool initialized_;
    std::mt19937_64 rng_;

    // Structures to store random numbers, bytes, and seeds
    struct RandomNumberInfo {
        uint64_t random_number;
        uint64_t min;
        uint64_t max;
        std::string user_id;
        std::string proof;
    };

    struct RandomBytesInfo {
        std::vector<uint8_t> bytes;
        std::string user_id;
        std::string proof;
    };

    struct SeedInfo {
        std::string seed;
        std::string user_id;
        std::string proof;
    };

    // Maps to store random numbers, bytes, and seeds
    std::map<std::string, RandomNumberInfo> random_numbers_;
    std::map<std::string, RandomBytesInfo> random_bytes_;
    std::map<std::string, SeedInfo> seeds_;
};
// RandomnessService implementation

RandomnessService::RandomnessService()
    : pimpl(std::make_unique<Impl>()),
      initialized_(false) {
}

RandomnessService::~RandomnessService() = default;

bool RandomnessService::initialize() {
    std::lock_guard<std::mutex> lock(mutex_);

    if (initialized_) {
        return true;
    }

    initialized_ = pimpl->initialize();
    return initialized_;
}

bool RandomnessService::is_initialized() const {
    std::lock_guard<std::mutex> lock(mutex_);
    return initialized_;
}

uint64_t RandomnessService::generate_random_number(uint64_t min, uint64_t max, const std::string& user_id, const std::string& request_id) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_) {
        if (!initialize()) {
            throw std::runtime_error("Failed to initialize RandomnessService");
        }
    }

    return pimpl->generate_random_number(min, max, user_id, request_id);
}

std::vector<uint8_t> RandomnessService::generate_random_bytes(size_t length, const std::string& user_id, const std::string& request_id) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_) {
        if (!initialize()) {
            throw std::runtime_error("Failed to initialize RandomnessService");
        }
    }

    return pimpl->generate_random_bytes(length, user_id, request_id);
}

bool RandomnessService::verify_random_number(uint64_t random_number, uint64_t min, uint64_t max,
                                           const std::string& user_id, const std::string& request_id,
                                           const std::string& proof) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_) {
        if (!initialize()) {
            throw std::runtime_error("Failed to initialize RandomnessService");
        }
    }

    return pimpl->verify_random_number(random_number, min, max, user_id, request_id, proof);
}

std::string RandomnessService::get_random_number_proof(uint64_t random_number, uint64_t min, uint64_t max,
                                                     const std::string& user_id, const std::string& request_id) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_) {
        if (!initialize()) {
            throw std::runtime_error("Failed to initialize RandomnessService");
        }
    }

    return pimpl->get_random_number_proof(random_number, min, max, user_id, request_id);
}

std::string RandomnessService::generate_seed(const std::string& user_id, const std::string& request_id) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_) {
        if (!initialize()) {
            throw std::runtime_error("Failed to initialize RandomnessService");
        }
    }

    return pimpl->generate_seed(user_id, request_id);
}

bool RandomnessService::verify_seed(const std::string& seed, const std::string& user_id,
                                  const std::string& request_id, const std::string& proof) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_) {
        if (!initialize()) {
            throw std::runtime_error("Failed to initialize RandomnessService");
        }
    }

    return pimpl->verify_seed(seed, user_id, request_id, proof);
}

std::string RandomnessService::get_seed_proof(const std::string& seed, const std::string& user_id,
                                            const std::string& request_id) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_) {
        if (!initialize()) {
            throw std::runtime_error("Failed to initialize RandomnessService");
        }
    }

    return pimpl->get_seed_proof(seed, user_id, request_id);
}

std::vector<uint8_t> RandomnessService::get_entropy() {
    return pimpl->get_entropy();
}

std::string RandomnessService::generate_proof(const std::vector<uint8_t>& data) {
    return pimpl->generate_proof(data);
}

bool RandomnessService::verify_proof(const std::vector<uint8_t>& data, const std::string& proof) {
    return pimpl->verify_proof(data, proof);
}
