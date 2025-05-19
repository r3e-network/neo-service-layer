#pragma once

#include <string>
#include <map>
#include <vector>
#include <mutex>
#include <chrono>
#include <atomic>
#include <functional>
#include "Logger.h"

namespace NeoServiceLayer {
namespace Enclave {

/**
 * @brief Metric type enumeration
 */
enum class MetricType {
    COUNTER,    // A value that can only increase
    GAUGE,      // A value that can go up and down
    HISTOGRAM,  // A distribution of values
    TIMER       // A specialized histogram for timing
};

/**
 * @brief Metric value class
 */
class MetricValue {
public:
    MetricValue(const std::string& name, MetricType type);
    
    // Counter operations
    void increment(int64_t value = 1);
    
    // Gauge operations
    void set(double value);
    void increment_gauge(double value);
    void decrement(double value);
    
    // Histogram operations
    void observe(double value);
    
    // Timer operations
    void start_timer();
    void stop_timer();
    
    // Get operations
    std::string get_name() const;
    MetricType get_type() const;
    double get_value() const;
    std::map<double, uint64_t> get_histogram() const;
    std::chrono::milliseconds get_timer() const;
    
private:
    std::string _name;
    MetricType _type;
    std::atomic<int64_t> _counter;
    std::atomic<double> _gauge;
    std::map<double, uint64_t> _histogram;
    std::chrono::time_point<std::chrono::high_resolution_clock> _timer_start;
    std::chrono::milliseconds _timer_duration;
    mutable std::mutex _mutex;
};

/**
 * @brief Metrics collector class
 * 
 * This class collects metrics for the enclave and provides
 * methods to retrieve and export them.
 */
class MetricsCollector {
public:
    /**
     * @brief Get the singleton instance of the metrics collector
     * 
     * @return MetricsCollector& The singleton instance
     */
    static MetricsCollector& getInstance();
    
    /**
     * @brief Initialize the metrics collector
     * 
     * @param export_interval_ms The interval in milliseconds at which to export metrics
     * @return true if initialization was successful
     * @return false if initialization failed
     */
    bool initialize(uint64_t export_interval_ms = 60000);
    
    /**
     * @brief Register a counter metric
     * 
     * @param name The name of the metric
     * @return MetricValue* A pointer to the metric value
     */
    MetricValue* register_counter(const std::string& name);
    
    /**
     * @brief Register a gauge metric
     * 
     * @param name The name of the metric
     * @return MetricValue* A pointer to the metric value
     */
    MetricValue* register_gauge(const std::string& name);
    
    /**
     * @brief Register a histogram metric
     * 
     * @param name The name of the metric
     * @param buckets The histogram buckets
     * @return MetricValue* A pointer to the metric value
     */
    MetricValue* register_histogram(const std::string& name, const std::vector<double>& buckets = {});
    
    /**
     * @brief Register a timer metric
     * 
     * @param name The name of the metric
     * @return MetricValue* A pointer to the metric value
     */
    MetricValue* register_timer(const std::string& name);
    
    /**
     * @brief Get a metric by name
     * 
     * @param name The name of the metric
     * @return MetricValue* A pointer to the metric value, or nullptr if not found
     */
    MetricValue* get_metric(const std::string& name);
    
    /**
     * @brief Get all metrics
     * 
     * @return std::map<std::string, MetricValue*> A map of metric names to metric values
     */
    std::map<std::string, MetricValue*> get_all_metrics() const;
    
    /**
     * @brief Export metrics to a string
     * 
     * @return std::string A string representation of all metrics
     */
    std::string export_metrics() const;
    
    /**
     * @brief Set the export callback
     * 
     * @param callback The callback function to call when exporting metrics
     */
    void set_export_callback(std::function<void(const std::string&)> callback);
    
private:
    // Private constructor for singleton pattern
    MetricsCollector();
    
    // Prevent copying and assignment
    MetricsCollector(const MetricsCollector&) = delete;
    MetricsCollector& operator=(const MetricsCollector&) = delete;
    
    // Destructor
    ~MetricsCollector();
    
    // Export metrics periodically
    void export_metrics_periodically();
    
    // Member variables
    std::map<std::string, MetricValue*> _metrics;
    std::function<void(const std::string&)> _export_callback;
    uint64_t _export_interval_ms;
    bool _initialized;
    bool _running;
    std::mutex _mutex;
    std::thread _export_thread;
};

// Convenience macros for metrics
#define REGISTER_COUNTER(name) NeoServiceLayer::Enclave::MetricsCollector::getInstance().register_counter(name)
#define REGISTER_GAUGE(name) NeoServiceLayer::Enclave::MetricsCollector::getInstance().register_gauge(name)
#define REGISTER_HISTOGRAM(name, buckets) NeoServiceLayer::Enclave::MetricsCollector::getInstance().register_histogram(name, buckets)
#define REGISTER_TIMER(name) NeoServiceLayer::Enclave::MetricsCollector::getInstance().register_timer(name)
#define GET_METRIC(name) NeoServiceLayer::Enclave::MetricsCollector::getInstance().get_metric(name)

} // namespace Enclave
} // namespace NeoServiceLayer
