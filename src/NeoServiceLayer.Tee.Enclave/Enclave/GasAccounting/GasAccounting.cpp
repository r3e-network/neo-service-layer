#include "GasAccounting.h"
#include "NeoServiceLayerEnclave_t.h"
#include <stdexcept>
#include <string>
#include <cstring>

// External function declarations for host calls
extern "C" {
    void host_log(const char* message);
}

GasAccounting::GasAccounting() : gas_used(0), gas_limit(UINT64_MAX), initialized_(false) {
}

GasAccounting::~GasAccounting() {
}

bool GasAccounting::initialize() {
    std::lock_guard<std::mutex> lock(mutex);

    if (initialized_) {
        return true;
    }

    try {
        // Reset gas used
        gas_used = 0;

        // Set default gas limit
        gas_limit = UINT64_MAX;

        initialized_ = true;
        return true;
    } catch (const std::exception& e) {
        host_log(e.what());
        return false;
    } catch (...) {
        host_log("Unknown error initializing GasAccounting");
        return false;
    }
}

bool GasAccounting::is_initialized() const {
    std::lock_guard<std::mutex> lock(mutex);
    return initialized_;
}

void GasAccounting::reset_gas_used() {
    std::lock_guard<std::mutex> lock(mutex);

    if (!initialized_) {
        if (!initialize()) {
            host_log("Failed to initialize GasAccounting");
            return;
        }
    }

    gas_used = 0;
}

uint64_t GasAccounting::get_gas_used() const {
    std::lock_guard<std::mutex> lock(mutex);

    if (!initialized_) {
        const_cast<GasAccounting*>(this)->initialize();
    }

    return gas_used;
}

uint64_t GasAccounting::get_gas_limit() const {
    std::lock_guard<std::mutex> lock(mutex);

    if (!initialized_) {
        const_cast<GasAccounting*>(this)->initialize();
    }

    return gas_limit;
}

bool GasAccounting::is_gas_limit_exceeded() const {
    std::lock_guard<std::mutex> lock(mutex);

    if (!initialized_) {
        const_cast<GasAccounting*>(this)->initialize();
    }

    return gas_used > gas_limit;
}

void GasAccounting::use_gas(uint64_t amount) {
    std::lock_guard<std::mutex> lock(mutex);

    if (!initialized_) {
        if (!initialize()) {
            host_log("Failed to initialize GasAccounting");
            throw std::runtime_error("GasAccounting not initialized");
        }
    }

    // Check for overflow
    if (UINT64_MAX - gas_used < amount) {
        host_log("Gas usage overflow");
        throw std::runtime_error("Gas usage overflow");
    }

    gas_used += amount;

    // Check if gas limit is exceeded
    if (gas_used > gas_limit) {
        std::string error_message = "Gas limit exceeded: " + std::to_string(gas_used) + " > " + std::to_string(gas_limit);
        host_log(error_message.c_str());
        throw std::runtime_error(error_message);
    }
}

void GasAccounting::set_gas_limit(uint64_t limit) {
    std::lock_guard<std::mutex> lock(mutex);

    if (!initialized_) {
        if (!initialize()) {
            host_log("Failed to initialize GasAccounting");
            return;
        }
    }

    gas_limit = limit;
}

uint64_t GasAccounting::calculate_gas_cost(const char* operation_type, uint64_t size) {
    if (!initialized_) {
        if (!const_cast<GasAccounting*>(this)->initialize()) {
            host_log("Failed to initialize GasAccounting");
            return 0;
        }
    }

    uint64_t gas_cost = 0;

    if (strcmp(operation_type, "function_call") == 0) {
        gas_cost = 100;
    } else if (strcmp(operation_type, "property_access") == 0) {
        gas_cost = 10;
    } else if (strcmp(operation_type, "array_access") == 0) {
        gas_cost = 20;
    } else if (strcmp(operation_type, "object_creation") == 0) {
        gas_cost = 50 + size;
    } else if (strcmp(operation_type, "array_creation") == 0) {
        gas_cost = 30 + size;
    } else if (strcmp(operation_type, "string_operation") == 0) {
        gas_cost = 5 + size / 100;
    } else if (strcmp(operation_type, "math_operation") == 0) {
        gas_cost = 5;
    } else if (strcmp(operation_type, "comparison") == 0) {
        gas_cost = 3;
    } else if (strcmp(operation_type, "loop_iteration") == 0) {
        gas_cost = 10;
    } else if (strcmp(operation_type, "storage_read") == 0) {
        gas_cost = 100 + size / 1024;
    } else if (strcmp(operation_type, "storage_write") == 0) {
        gas_cost = 200 + size / 512;
    } else if (strcmp(operation_type, "crypto_operation") == 0) {
        gas_cost = 500 + size / 256;
    } else if (strcmp(operation_type, "js_execution") == 0) {
        // Base cost for JavaScript execution plus cost based on code size
        gas_cost = 1000 + size / 100;
    } else if (strcmp(operation_type, "memory_allocation") == 0) {
        // Cost for memory allocation based on size
        gas_cost = 10 + size / 1024;
    } else if (strcmp(operation_type, "network_operation") == 0) {
        // Cost for network operations
        gas_cost = 1000 + size / 512;
    } else if (strcmp(operation_type, "attestation") == 0) {
        // Cost for attestation operations
        gas_cost = 5000;
    } else if (strcmp(operation_type, "sealing") == 0) {
        // Cost for sealing operations
        gas_cost = 1000 + size / 256;
    } else if (strcmp(operation_type, "unsealing") == 0) {
        // Cost for unsealing operations
        gas_cost = 500 + size / 256;
    } else {
        gas_cost = 1; // Default cost
    }

    return gas_cost;
}
