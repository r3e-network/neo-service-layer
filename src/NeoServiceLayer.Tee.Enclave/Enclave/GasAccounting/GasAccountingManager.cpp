#include "GasAccountingManager.h"
#include <cstdio>
#include <sstream>
#include <iomanip>

GasAccountingManager::GasAccountingManager()
    : _initialized(false),
      _current_gas_usage(0)
{
}

GasAccountingManager::~GasAccountingManager()
{
}

bool GasAccountingManager::initialize()
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (_initialized)
    {
        secure_log("GasAccountingManager already initialized");
        return true;
    }
    
    try
    {
        secure_log("Initializing GasAccountingManager...");
        
        // Initialize gas balances and usages
        _gas_balances.clear();
        _gas_usages.clear();
        _start_times.clear();
        
        _current_function_id.clear();
        _current_user_id.clear();
        _current_gas_usage = 0;
        
        _initialized = true;
        secure_log("GasAccountingManager initialized successfully");
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error initializing GasAccountingManager: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error initializing GasAccountingManager");
        return false;
    }
}

bool GasAccountingManager::start_accounting(const std::string& function_id, const std::string& user_id)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        secure_log("GasAccountingManager not initialized and initialization failed");
        return false;
    }
    
    try
    {
        secure_log("Starting gas accounting for function " + function_id + ", user " + user_id);
        
        // Store the current function ID and user ID
        _current_function_id = function_id;
        _current_user_id = user_id;
        
        // Reset the current gas usage
        _current_gas_usage = 0;
        
        // Store the start time
        _start_times[std::make_pair(function_id, user_id)] = std::chrono::steady_clock::now();
        
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error starting gas accounting: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error starting gas accounting");
        return false;
    }
}

uint64_t GasAccountingManager::stop_accounting(const std::string& function_id, const std::string& user_id)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        secure_log("GasAccountingManager not initialized and initialization failed");
        return 0;
    }
    
    try
    {
        secure_log("Stopping gas accounting for function " + function_id + ", user " + user_id);
        
        // Check if we have a start time for this function and user
        auto key = std::make_pair(function_id, user_id);
        auto it = _start_times.find(key);
        if (it == _start_times.end())
        {
            secure_log("No start time found for function " + function_id + ", user " + user_id);
            return 0;
        }
        
        // Calculate the elapsed time
        auto start_time = it->second;
        auto end_time = std::chrono::steady_clock::now();
        auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(end_time - start_time).count();
        
        // Calculate the gas used based on elapsed time and current gas usage
        uint64_t gas_used = _current_gas_usage + static_cast<uint64_t>(elapsed);
        
        // Update the gas usage for this function
        _gas_usages[function_id] += gas_used;
        
        // Update the gas balance for this user
        if (_gas_balances.find(user_id) == _gas_balances.end())
        {
            _gas_balances[user_id] = 0;
        }
        
        if (_gas_balances[user_id] < gas_used)
        {
            _gas_balances[user_id] = 0;
        }
        else
        {
            _gas_balances[user_id] -= gas_used;
        }
        
        // Remove the start time
        _start_times.erase(it);
        
        // Reset the current function ID, user ID, and gas usage
        _current_function_id.clear();
        _current_user_id.clear();
        _current_gas_usage = 0;
        
        secure_log("Gas accounting stopped for function " + function_id + ", user " + user_id + ", gas used: " + std::to_string(gas_used));
        return gas_used;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error stopping gas accounting: " + std::string(ex.what()));
        return 0;
    }
    catch (...)
    {
        secure_log("Unknown error stopping gas accounting");
        return 0;
    }
}

bool GasAccountingManager::use_gas(uint64_t amount)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        secure_log("GasAccountingManager not initialized and initialization failed");
        return false;
    }
    
    try
    {
        // Add the amount to the current gas usage
        _current_gas_usage += amount;
        
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error using gas: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error using gas");
        return false;
    }
}

uint64_t GasAccountingManager::get_gas_balance(const std::string& user_id)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        secure_log("GasAccountingManager not initialized and initialization failed");
        return 0;
    }
    
    try
    {
        // Get the gas balance for this user
        auto it = _gas_balances.find(user_id);
        if (it == _gas_balances.end())
        {
            return 0;
        }
        
        return it->second;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error getting gas balance: " + std::string(ex.what()));
        return 0;
    }
    catch (...)
    {
        secure_log("Unknown error getting gas balance");
        return 0;
    }
}

bool GasAccountingManager::update_gas_balance(const std::string& user_id, int64_t amount)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        secure_log("GasAccountingManager not initialized and initialization failed");
        return false;
    }
    
    try
    {
        // Update the gas balance for this user
        if (_gas_balances.find(user_id) == _gas_balances.end())
        {
            _gas_balances[user_id] = 0;
        }
        
        if (amount < 0 && static_cast<uint64_t>(-amount) > _gas_balances[user_id])
        {
            _gas_balances[user_id] = 0;
        }
        else
        {
            _gas_balances[user_id] += amount;
        }
        
        return true;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error updating gas balance: " + std::string(ex.what()));
        return false;
    }
    catch (...)
    {
        secure_log("Unknown error updating gas balance");
        return false;
    }
}

uint64_t GasAccountingManager::get_gas_usage(const std::string& function_id)
{
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (!_initialized && !initialize())
    {
        secure_log("GasAccountingManager not initialized and initialization failed");
        return 0;
    }
    
    try
    {
        // Get the gas usage for this function
        auto it = _gas_usages.find(function_id);
        if (it == _gas_usages.end())
        {
            return 0;
        }
        
        return it->second;
    }
    catch (const std::exception& ex)
    {
        secure_log("Error getting gas usage: " + std::string(ex.what()));
        return 0;
    }
    catch (...)
    {
        secure_log("Unknown error getting gas usage");
        return 0;
    }
}

bool GasAccountingManager::is_initialized() const
{
    std::lock_guard<std::mutex> lock(_mutex);
    return _initialized;
}

void GasAccountingManager::secure_log(const std::string& message)
{
    // Use fprintf for logging to stderr
    fprintf(stderr, "[GasAccountingManager] %s\n", message.c_str());
}
