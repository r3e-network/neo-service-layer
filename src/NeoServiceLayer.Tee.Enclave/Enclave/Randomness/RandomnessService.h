#ifndef RANDOMNESS_SERVICE_H
#define RANDOMNESS_SERVICE_H

#include <string>
#include <vector>
#include <map>
#include <mutex>
#include <memory>
#include <cstdint>

/**
 * @brief Provably fair randomness service for the enclave.
 * 
 * This class provides a provably fair randomness service for the enclave, allowing
 * JavaScript functions to generate random numbers that can be verified by external parties.
 */
class RandomnessService {
public:
    /**
     * @brief Constructs a new RandomnessService instance.
     */
    RandomnessService();
    
    /**
     * @brief Destructor.
     */
    ~RandomnessService();
    
    /**
     * @brief Initializes the randomness service.
     * 
     * @return True if initialization was successful, false otherwise.
     */
    bool initialize();
    
    /**
     * @brief Checks if the randomness service is initialized.
     * 
     * @return True if the randomness service is initialized, false otherwise.
     */
    bool is_initialized() const;
    
    /**
     * @brief Generates a random number between min and max (inclusive).
     * 
     * @param min The minimum value.
     * @param max The maximum value.
     * @param user_id The ID of the user requesting the random number.
     * @param request_id The ID of the request.
     * @return The generated random number.
     */
    uint64_t generate_random_number(uint64_t min, uint64_t max, const std::string& user_id, const std::string& request_id);
    
    /**
     * @brief Generates random bytes.
     * 
     * @param length The number of bytes to generate.
     * @param user_id The ID of the user requesting the random bytes.
     * @param request_id The ID of the request.
     * @return The generated random bytes.
     */
    std::vector<uint8_t> generate_random_bytes(size_t length, const std::string& user_id, const std::string& request_id);
    
    /**
     * @brief Verifies a random number.
     * 
     * @param random_number The random number to verify.
     * @param min The minimum value.
     * @param max The maximum value.
     * @param user_id The ID of the user who requested the random number.
     * @param request_id The ID of the request.
     * @param proof The proof of the random number.
     * @return True if the random number is valid, false otherwise.
     */
    bool verify_random_number(uint64_t random_number, uint64_t min, uint64_t max, 
                             const std::string& user_id, const std::string& request_id, 
                             const std::string& proof);
    
    /**
     * @brief Gets the proof for a random number.
     * 
     * @param random_number The random number.
     * @param min The minimum value.
     * @param max The maximum value.
     * @param user_id The ID of the user who requested the random number.
     * @param request_id The ID of the request.
     * @return The proof of the random number.
     */
    std::string get_random_number_proof(uint64_t random_number, uint64_t min, uint64_t max, 
                                       const std::string& user_id, const std::string& request_id);
    
    /**
     * @brief Generates a random seed.
     * 
     * @param user_id The ID of the user requesting the seed.
     * @param request_id The ID of the request.
     * @return The generated seed.
     */
    std::string generate_seed(const std::string& user_id, const std::string& request_id);
    
    /**
     * @brief Verifies a seed.
     * 
     * @param seed The seed to verify.
     * @param user_id The ID of the user who requested the seed.
     * @param request_id The ID of the request.
     * @param proof The proof of the seed.
     * @return True if the seed is valid, false otherwise.
     */
    bool verify_seed(const std::string& seed, const std::string& user_id, 
                    const std::string& request_id, const std::string& proof);
    
    /**
     * @brief Gets the proof for a seed.
     * 
     * @param seed The seed.
     * @param user_id The ID of the user who requested the seed.
     * @param request_id The ID of the request.
     * @return The proof of the seed.
     */
    std::string get_seed_proof(const std::string& seed, const std::string& user_id, 
                              const std::string& request_id);
    
private:
    // Implementation details
    class Impl;
    std::unique_ptr<Impl> pimpl;
    
    // Mutex for thread safety
    mutable std::mutex mutex_;
    
    // Initialized flag
    bool initialized_;
    
    // Helper methods
    std::vector<uint8_t> get_entropy();
    std::string generate_proof(const std::vector<uint8_t>& data);
    bool verify_proof(const std::vector<uint8_t>& data, const std::string& proof);
};

#endif // RANDOMNESS_SERVICE_H
