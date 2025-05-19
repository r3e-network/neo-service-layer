#include "Logger.h"
#include "NeoServiceLayerEnclave_t.h"
#include <sys/stat.h>
#include <unistd.h>
#include <filesystem>
#include <algorithm>
#include <iostream>

namespace NeoServiceLayer {
namespace Enclave {

// External function declaration for ocall
extern "C" {
    void ocall_print_string(const char* str);
}

// Singleton instance
Logger& Logger::getInstance() {
    static Logger instance;
    return instance;
}

Logger::Logger()
    : _log_level(LogLevel::INFO),
      _log_to_file(false),
      _log_file_path(""),
      _max_file_size(10 * 1024 * 1024),  // 10 MB
      _max_files(5),
      _initialized(false) {
}

Logger::~Logger() {
    if (_log_file.is_open()) {
        _log_file.close();
    }
}

bool Logger::initialize(
    LogLevel log_level,
    bool log_to_file,
    const std::string& log_file_path,
    size_t max_file_size,
    size_t max_files) {
    
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (_initialized) {
        // Already initialized
        return true;
    }
    
    _log_level = log_level;
    _log_to_file = log_to_file;
    _log_file_path = log_file_path;
    _max_file_size = max_file_size;
    _max_files = max_files;
    
    if (_log_to_file) {
        // Create the log directory if it doesn't exist
        std::string log_dir = _log_file_path.substr(0, _log_file_path.find_last_of('/'));
        
        // Create the directory recursively
        std::filesystem::create_directories(log_dir);
        
        // Open the log file
        _log_file.open(_log_file_path, std::ios::app);
        if (!_log_file.is_open()) {
            writeToOcall("Failed to open log file: " + _log_file_path);
            _log_to_file = false;
            return false;
        }
    }
    
    _initialized = true;
    info("Logger", "Logger initialized with level: " + logLevelToString(_log_level));
    return true;
}

void Logger::setLogLevel(LogLevel level) {
    std::lock_guard<std::mutex> lock(_mutex);
    _log_level = level;
    info("Logger", "Log level set to: " + logLevelToString(_log_level));
}

LogLevel Logger::getLogLevel() const {
    return _log_level;
}

void Logger::setLogCallback(std::function<void(LogLevel, const std::string&)> callback) {
    std::lock_guard<std::mutex> lock(_mutex);
    _log_callback = callback;
}

void Logger::log(LogLevel level, const std::string& component, const std::string& message) {
    if (level < _log_level) {
        return;
    }
    
    std::string formatted_message = formatLogMessage(level, component, message);
    
    std::lock_guard<std::mutex> lock(_mutex);
    
    // Write to file if enabled
    if (_log_to_file) {
        rotateLogFileIfNeeded();
        writeToFile(formatted_message);
    }
    
    // Write to ocall
    writeToOcall(formatted_message);
    
    // Call the callback if set
    if (_log_callback) {
        _log_callback(level, formatted_message);
    }
}

void Logger::trace(const std::string& component, const std::string& message) {
    log(LogLevel::TRACE, component, message);
}

void Logger::debug(const std::string& component, const std::string& message) {
    log(LogLevel::DEBUG, component, message);
}

void Logger::info(const std::string& component, const std::string& message) {
    log(LogLevel::INFO, component, message);
}

void Logger::warning(const std::string& component, const std::string& message) {
    log(LogLevel::WARNING, component, message);
}

void Logger::error(const std::string& component, const std::string& message) {
    log(LogLevel::ERROR, component, message);
}

void Logger::critical(const std::string& component, const std::string& message) {
    log(LogLevel::CRITICAL, component, message);
}

void Logger::flush() {
    std::lock_guard<std::mutex> lock(_mutex);
    if (_log_file.is_open()) {
        _log_file.flush();
    }
}

std::string Logger::formatLogMessage(LogLevel level, const std::string& component, const std::string& message) {
    std::stringstream ss;
    ss << getCurrentTimestamp() << " [" << logLevelToString(level) << "] [" << component << "] " << message;
    return ss.str();
}

std::string Logger::getCurrentTimestamp() {
    auto now = std::chrono::system_clock::now();
    auto now_time_t = std::chrono::system_clock::to_time_t(now);
    auto now_ms = std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()) % 1000;
    
    std::stringstream ss;
    ss << std::put_time(std::localtime(&now_time_t), "%Y-%m-%d %H:%M:%S");
    ss << '.' << std::setfill('0') << std::setw(3) << now_ms.count();
    
    return ss.str();
}

std::string Logger::logLevelToString(LogLevel level) {
    switch (level) {
        case LogLevel::TRACE:    return "TRACE";
        case LogLevel::DEBUG:    return "DEBUG";
        case LogLevel::INFO:     return "INFO";
        case LogLevel::WARNING:  return "WARNING";
        case LogLevel::ERROR:    return "ERROR";
        case LogLevel::CRITICAL: return "CRITICAL";
        case LogLevel::NONE:     return "NONE";
        default:                 return "UNKNOWN";
    }
}

void Logger::rotateLogFileIfNeeded() {
    if (!_log_to_file || !_log_file.is_open()) {
        return;
    }
    
    // Get the current file size
    struct stat stat_buf;
    int rc = stat(_log_file_path.c_str(), &stat_buf);
    if (rc != 0) {
        return;
    }
    
    // Check if rotation is needed
    if (static_cast<size_t>(stat_buf.st_size) < _max_file_size) {
        return;
    }
    
    // Close the current log file
    _log_file.close();
    
    // Rotate the log files
    for (size_t i = _max_files - 1; i > 0; --i) {
        std::string old_file = _log_file_path + "." + std::to_string(i);
        std::string new_file = _log_file_path + "." + std::to_string(i + 1);
        
        // Remove the oldest log file if it exists
        if (i == _max_files - 1) {
            std::remove(new_file.c_str());
        }
        
        // Rename the log file
        std::rename(old_file.c_str(), new_file.c_str());
    }
    
    // Rename the current log file
    std::rename(_log_file_path.c_str(), (_log_file_path + ".1").c_str());
    
    // Open a new log file
    _log_file.open(_log_file_path, std::ios::app);
    if (!_log_file.is_open()) {
        writeToOcall("Failed to open log file after rotation: " + _log_file_path);
        _log_to_file = false;
    }
}

void Logger::writeToFile(const std::string& message) {
    if (!_log_file.is_open()) {
        return;
    }
    
    _log_file << message << std::endl;
}

void Logger::writeToOcall(const std::string& message) {
    ocall_print_string(message.c_str());
}

} // namespace Enclave
} // namespace NeoServiceLayer
