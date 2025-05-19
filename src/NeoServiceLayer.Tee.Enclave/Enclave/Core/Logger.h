#pragma once

#include <string>
#include <vector>
#include <mutex>
#include <fstream>
#include <sstream>
#include <chrono>
#include <iomanip>
#include <ctime>
#include <memory>
#include <functional>

namespace NeoServiceLayer {
namespace Enclave {

/**
 * @brief Log level enumeration
 */
enum class LogLevel {
    TRACE,
    DEBUG,
    INFO,
    WARNING,
    ERROR,
    CRITICAL,
    NONE  // Used to disable logging
};

/**
 * @brief Secure logger for enclave operations
 * 
 * This class provides a secure logging mechanism for enclave operations.
 * It supports multiple log destinations, log rotation, and log filtering.
 */
class Logger {
public:
    /**
     * @brief Get the singleton instance of the logger
     * 
     * @return Logger& The singleton instance
     */
    static Logger& getInstance();

    /**
     * @brief Initialize the logger
     * 
     * @param log_level The minimum log level to record
     * @param log_to_file Whether to log to a file
     * @param log_file_path The path to the log file
     * @param max_file_size The maximum size of the log file in bytes
     * @param max_files The maximum number of log files to keep
     * @return true if initialization was successful
     * @return false if initialization failed
     */
    bool initialize(
        LogLevel log_level = LogLevel::INFO,
        bool log_to_file = true,
        const std::string& log_file_path = "/occlum_instance/logs/enclave.log",
        size_t max_file_size = 10 * 1024 * 1024,  // 10 MB
        size_t max_files = 5);

    /**
     * @brief Set the log level
     * 
     * @param level The minimum log level to record
     */
    void setLogLevel(LogLevel level);

    /**
     * @brief Get the current log level
     * 
     * @return LogLevel The current log level
     */
    LogLevel getLogLevel() const;

    /**
     * @brief Set the callback function for log messages
     * 
     * @param callback The callback function
     */
    void setLogCallback(std::function<void(LogLevel, const std::string&)> callback);

    /**
     * @brief Log a message
     * 
     * @param level The log level
     * @param component The component name
     * @param message The log message
     */
    void log(LogLevel level, const std::string& component, const std::string& message);

    /**
     * @brief Log a trace message
     * 
     * @param component The component name
     * @param message The log message
     */
    void trace(const std::string& component, const std::string& message);

    /**
     * @brief Log a debug message
     * 
     * @param component The component name
     * @param message The log message
     */
    void debug(const std::string& component, const std::string& message);

    /**
     * @brief Log an info message
     * 
     * @param component The component name
     * @param message The log message
     */
    void info(const std::string& component, const std::string& message);

    /**
     * @brief Log a warning message
     * 
     * @param component The component name
     * @param message The log message
     */
    void warning(const std::string& component, const std::string& message);

    /**
     * @brief Log an error message
     * 
     * @param component The component name
     * @param message The log message
     */
    void error(const std::string& component, const std::string& message);

    /**
     * @brief Log a critical message
     * 
     * @param component The component name
     * @param message The log message
     */
    void critical(const std::string& component, const std::string& message);

    /**
     * @brief Flush the log buffer
     */
    void flush();

private:
    // Private constructor for singleton pattern
    Logger();
    
    // Prevent copying and assignment
    Logger(const Logger&) = delete;
    Logger& operator=(const Logger&) = delete;

    // Destructor
    ~Logger();

    // Helper methods
    std::string formatLogMessage(LogLevel level, const std::string& component, const std::string& message);
    std::string getCurrentTimestamp();
    std::string logLevelToString(LogLevel level);
    void rotateLogFileIfNeeded();
    void writeToFile(const std::string& message);
    void writeToOcall(const std::string& message);

    // Member variables
    LogLevel _log_level;
    bool _log_to_file;
    std::string _log_file_path;
    size_t _max_file_size;
    size_t _max_files;
    std::ofstream _log_file;
    std::mutex _mutex;
    std::function<void(LogLevel, const std::string&)> _log_callback;
    bool _initialized;
};

// Convenience macros for logging
#define LOG_TRACE(component, message) NeoServiceLayer::Enclave::Logger::getInstance().trace(component, message)
#define LOG_DEBUG(component, message) NeoServiceLayer::Enclave::Logger::getInstance().debug(component, message)
#define LOG_INFO(component, message) NeoServiceLayer::Enclave::Logger::getInstance().info(component, message)
#define LOG_WARNING(component, message) NeoServiceLayer::Enclave::Logger::getInstance().warning(component, message)
#define LOG_ERROR(component, message) NeoServiceLayer::Enclave::Logger::getInstance().error(component, message)
#define LOG_CRITICAL(component, message) NeoServiceLayer::Enclave::Logger::getInstance().critical(component, message)

} // namespace Enclave
} // namespace NeoServiceLayer
