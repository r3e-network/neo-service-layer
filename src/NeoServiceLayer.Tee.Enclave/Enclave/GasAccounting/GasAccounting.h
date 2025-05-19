#ifndef GAS_ACCOUNTING_H
#define GAS_ACCOUNTING_H

#include <cstdint>
#include <mutex>

class GasAccounting {
public:
    GasAccounting();
    ~GasAccounting();

    // Initialize the gas accounting system
    bool initialize();

    // Check if the gas accounting system is initialized
    bool is_initialized() const;

    // Reset the gas used counter
    void reset_gas_used();

    // Get the current gas used
    uint64_t get_gas_used() const;

    // Use a specified amount of gas
    void use_gas(uint64_t amount);

    // Set the gas limit
    void set_gas_limit(uint64_t limit);

    // Calculate gas cost for a specific operation
    uint64_t calculate_gas_cost(const char* operation_type, uint64_t size = 0);

    // Get the gas limit
    uint64_t get_gas_limit() const;

    // Check if gas limit is exceeded
    bool is_gas_limit_exceeded() const;

private:
    uint64_t gas_used;
    uint64_t gas_limit;
    mutable std::mutex mutex;
    bool initialized_;
};

#endif // GAS_ACCOUNTING_H
