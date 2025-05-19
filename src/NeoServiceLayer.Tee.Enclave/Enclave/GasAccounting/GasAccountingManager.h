#pragma once

#include <string>
#include <map>
#include <mutex>
#include <chrono>

/**
 * @brief Manager for gas accounting
 * 
 * This class provides functionality for tracking gas usage in the enclave.
 * Gas is a measure of computational resources used by JavaScript code.
 */
class GasAccountingManager {
public:
    /**
     * @brief Constructor
     */
    GasAccountingManager();
    
    /**
     * @brief Destructor
     */
    ~GasAccountingManager();
    
    /**
     * @brief Initialize the gas accounting manager
     * 
     * @return True if initialization was successful, false otherwise
     */
    bool initialize();
    
    /**
     * @brief Start accounting for gas usage
     * 
     * @param function_id The function ID
     * @param user_id The user ID
     * @return True if accounting was started successfully, false otherwise
     */
    bool start_accounting(const std::string& function_id, const std::string& user_id);
    
    /**
     * @brief Stop accounting for gas usage
     * 
     * @param function_id The function ID
     * @param user_id The user ID
     * @return The amount of gas used
     */
    uint64_t stop_accounting(const std::string& function_id, const std::string& user_id);
    
    /**
     * @brief Use gas
     * 
     * @param amount The amount of gas to use
     * @return True if the gas was used successfully, false otherwise
     */
    bool use_gas(uint64_t amount);
    
    /**
     * @brief Get the gas balance for a user
     * 
     * @param user_id The user ID
     * @return The gas balance
     */
    uint64_t get_gas_balance(const std::string& user_id);
    
    /**
     * @brief Update the gas balance for a user
     * 
     * @param user_id The user ID
     * @param amount The amount to add to the balance (can be negative)
     * @return True if the balance was updated successfully, false otherwise
     */
    bool update_gas_balance(const std::string& user_id, int64_t amount);
    
    /**
     * @brief Get the gas usage for a function
     * 
     * @param function_id The function ID
     * @return The gas usage
     */
    uint64_t get_gas_usage(const std::string& function_id);
    
    /**
     * @brief Check if the gas accounting manager is initialized
     * 
     * @return True if the gas accounting manager is initialized, false otherwise
     */
    bool is_initialized() const;
    
private:
    // Initialization flag
    bool _initialized;
    
    // Mutex for thread safety
    mutable std::mutex _mutex;
    
    // Map of user ID to gas balance
    std::map<std::string, uint64_t> _gas_balances;
    
    // Map of function ID to gas usage
    std::map<std::string, uint64_t> _gas_usages;
    
    // Map of (function ID, user ID) to start time
    std::map<std::pair<std::string, std::string>, std::chrono::steady_clock::time_point> _start_times;
    
    // Current function ID and user ID
    std::string _current_function_id;
    std::string _current_user_id;
    
    // Current gas usage
    uint64_t _current_gas_usage;
    
    // Helper methods
    void secure_log(const std::string& message);
};
