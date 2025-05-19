#include "MetricsCollector.h"
#include <sstream>
#include <iomanip>
#include <thread>
#include <nlohmann/json.hpp>

namespace NeoServiceLayer {
namespace Enclave {

// MetricValue implementation

MetricValue::MetricValue(const std::string& name, MetricType type)
    : _name(name),
      _type(type),
      _counter(0),
      _gauge(0.0),
      _timer_duration(0) {
}

void MetricValue::increment(int64_t value) {
    if (_type != MetricType::COUNTER) {
        return;
    }
    
    _counter.fetch_add(value);
}

void MetricValue::set(double value) {
    if (_type != MetricType::GAUGE) {
        return;
    }
    
    _gauge.store(value);
}

void MetricValue::increment_gauge(double value) {
    if (_type != MetricType::GAUGE) {
        return;
    }
    
    _gauge.fetch_add(value);
}

void MetricValue::decrement(double value) {
    if (_type != MetricType::GAUGE) {
        return;
    }
    
    _gauge.fetch_sub(value);
}

void MetricValue::observe(double value) {
    if (_type != MetricType::HISTOGRAM) {
        return;
    }
    
    std::lock_guard<std::mutex> lock(_mutex);
    
    // Find the appropriate bucket
    for (auto& bucket : _histogram) {
        if (value <= bucket.first) {
            bucket.second++;
            break;
        }
    }
    
    // If no bucket was found, add to the last bucket
    if (_histogram.empty() || value > _histogram.rbegin()->first) {
        if (_histogram.empty()) {
            _histogram[value] = 1;
        } else {
            _histogram[_histogram.rbegin()->first]++;
        }
    }
}

void MetricValue::start_timer() {
    if (_type != MetricType::TIMER) {
        return;
    }
    
    _timer_start = std::chrono::high_resolution_clock::now();
}

void MetricValue::stop_timer() {
    if (_type != MetricType::TIMER) {
        return;
    }
    
    auto end = std::chrono::high_resolution_clock::now();
    _timer_duration = std::chrono::duration_cast<std::chrono::milliseconds>(end - _timer_start);
}

std::string MetricValue::get_name() const {
    return _name;
}

MetricType MetricValue::get_type() const {
    return _type;
}

double MetricValue::get_value() const {
    switch (_type) {
        case MetricType::COUNTER:
            return static_cast<double>(_counter.load());
        case MetricType::GAUGE:
            return _gauge.load();
        case MetricType::TIMER:
            return static_cast<double>(_timer_duration.count());
        default:
            return 0.0;
    }
}

std::map<double, uint64_t> MetricValue::get_histogram() const {
    if (_type != MetricType::HISTOGRAM) {
        return {};
    }
    
    std::lock_guard<std::mutex> lock(_mutex);
    return _histogram;
}

std::chrono::milliseconds MetricValue::get_timer() const {
    if (_type != MetricType::TIMER) {
        return std::chrono::milliseconds(0);
    }
    
    return _timer_duration;
}

// MetricsCollector implementation

MetricsCollector& MetricsCollector::getInstance() {
    static MetricsCollector instance;
    return instance;
}

MetricsCollector::MetricsCollector()
    : _export_interval_ms(60000),
      _initialized(false),
      _running(false) {
}

MetricsCollector::~MetricsCollector() {
    _running = false;
    
    if (_export_thread.joinable()) {
        _export_thread.join();
    }
    
    // Clean up metrics
    for (auto& metric : _metrics) {
        delete metric.second;
    }
    
    _metrics.clear();
}

bool MetricsCollector::initialize(uint64_t export_interval_ms) {
    std::lock_guard<std::mutex> lock(_mutex);
    
    if (_initialized) {
        return true;
    }
    
    _export_interval_ms = export_interval_ms;
    _initialized = true;
    _running = true;
    
    // Start the export thread
    _export_thread = std::thread(&MetricsCollector::export_metrics_periodically, this);
    
    Logger::getInstance().info("MetricsCollector", "Metrics collector initialized with export interval: " + std::to_string(_export_interval_ms) + "ms");
    return true;
}

MetricValue* MetricsCollector::register_counter(const std::string& name) {
    std::lock_guard<std::mutex> lock(_mutex);
    
    // Check if the metric already exists
    auto it = _metrics.find(name);
    if (it != _metrics.end()) {
        if (it->second->get_type() == MetricType::COUNTER) {
            return it->second;
        } else {
            Logger::getInstance().warning("MetricsCollector", "Metric " + name + " already exists with a different type");
            return nullptr;
        }
    }
    
    // Create a new counter metric
    MetricValue* metric = new MetricValue(name, MetricType::COUNTER);
    _metrics[name] = metric;
    
    Logger::getInstance().debug("MetricsCollector", "Registered counter metric: " + name);
    return metric;
}

MetricValue* MetricsCollector::register_gauge(const std::string& name) {
    std::lock_guard<std::mutex> lock(_mutex);
    
    // Check if the metric already exists
    auto it = _metrics.find(name);
    if (it != _metrics.end()) {
        if (it->second->get_type() == MetricType::GAUGE) {
            return it->second;
        } else {
            Logger::getInstance().warning("MetricsCollector", "Metric " + name + " already exists with a different type");
            return nullptr;
        }
    }
    
    // Create a new gauge metric
    MetricValue* metric = new MetricValue(name, MetricType::GAUGE);
    _metrics[name] = metric;
    
    Logger::getInstance().debug("MetricsCollector", "Registered gauge metric: " + name);
    return metric;
}

MetricValue* MetricsCollector::register_histogram(const std::string& name, const std::vector<double>& buckets) {
    std::lock_guard<std::mutex> lock(_mutex);
    
    // Check if the metric already exists
    auto it = _metrics.find(name);
    if (it != _metrics.end()) {
        if (it->second->get_type() == MetricType::HISTOGRAM) {
            return it->second;
        } else {
            Logger::getInstance().warning("MetricsCollector", "Metric " + name + " already exists with a different type");
            return nullptr;
        }
    }
    
    // Create a new histogram metric
    MetricValue* metric = new MetricValue(name, MetricType::HISTOGRAM);
    _metrics[name] = metric;
    
    // Initialize buckets
    std::map<double, uint64_t> histogram_buckets;
    for (const auto& bucket : buckets) {
        histogram_buckets[bucket] = 0;
    }
    
    // If no buckets were provided, use default buckets
    if (buckets.empty()) {
        histogram_buckets[0.005] = 0;  // 5ms
        histogram_buckets[0.01] = 0;   // 10ms
        histogram_buckets[0.025] = 0;  // 25ms
        histogram_buckets[0.05] = 0;   // 50ms
        histogram_buckets[0.1] = 0;    // 100ms
        histogram_buckets[0.25] = 0;   // 250ms
        histogram_buckets[0.5] = 0;    // 500ms
        histogram_buckets[1.0] = 0;    // 1s
        histogram_buckets[2.5] = 0;    // 2.5s
        histogram_buckets[5.0] = 0;    // 5s
        histogram_buckets[10.0] = 0;   // 10s
    }
    
    Logger::getInstance().debug("MetricsCollector", "Registered histogram metric: " + name);
    return metric;
}

MetricValue* MetricsCollector::register_timer(const std::string& name) {
    std::lock_guard<std::mutex> lock(_mutex);
    
    // Check if the metric already exists
    auto it = _metrics.find(name);
    if (it != _metrics.end()) {
        if (it->second->get_type() == MetricType::TIMER) {
            return it->second;
        } else {
            Logger::getInstance().warning("MetricsCollector", "Metric " + name + " already exists with a different type");
            return nullptr;
        }
    }
    
    // Create a new timer metric
    MetricValue* metric = new MetricValue(name, MetricType::TIMER);
    _metrics[name] = metric;
    
    Logger::getInstance().debug("MetricsCollector", "Registered timer metric: " + name);
    return metric;
}

MetricValue* MetricsCollector::get_metric(const std::string& name) {
    std::lock_guard<std::mutex> lock(_mutex);
    
    auto it = _metrics.find(name);
    if (it != _metrics.end()) {
        return it->second;
    }
    
    return nullptr;
}

std::map<std::string, MetricValue*> MetricsCollector::get_all_metrics() const {
    std::lock_guard<std::mutex> lock(_mutex);
    return _metrics;
}

std::string MetricsCollector::export_metrics() const {
    std::lock_guard<std::mutex> lock(_mutex);
    
    nlohmann::json metrics_json;
    
    for (const auto& metric : _metrics) {
        nlohmann::json metric_json;
        
        metric_json["name"] = metric.first;
        
        switch (metric.second->get_type()) {
            case MetricType::COUNTER:
                metric_json["type"] = "counter";
                metric_json["value"] = metric.second->get_value();
                break;
            case MetricType::GAUGE:
                metric_json["type"] = "gauge";
                metric_json["value"] = metric.second->get_value();
                break;
            case MetricType::HISTOGRAM:
                metric_json["type"] = "histogram";
                
                nlohmann::json buckets_json;
                for (const auto& bucket : metric.second->get_histogram()) {
                    buckets_json[std::to_string(bucket.first)] = bucket.second;
                }
                
                metric_json["buckets"] = buckets_json;
                break;
            case MetricType::TIMER:
                metric_json["type"] = "timer";
                metric_json["value"] = metric.second->get_value();
                break;
        }
        
        metrics_json[metric.first] = metric_json;
    }
    
    return metrics_json.dump();
}

void MetricsCollector::set_export_callback(std::function<void(const std::string&)> callback) {
    std::lock_guard<std::mutex> lock(_mutex);
    _export_callback = callback;
}

void MetricsCollector::export_metrics_periodically() {
    while (_running) {
        // Sleep for the export interval
        std::this_thread::sleep_for(std::chrono::milliseconds(_export_interval_ms));
        
        if (!_running) {
            break;
        }
        
        // Export metrics
        std::string metrics_str = export_metrics();
        
        // Call the export callback if set
        if (_export_callback) {
            _export_callback(metrics_str);
        }
        
        // Log the metrics
        Logger::getInstance().debug("MetricsCollector", "Exported metrics: " + metrics_str);
    }
}

} // namespace Enclave
} // namespace NeoServiceLayer
