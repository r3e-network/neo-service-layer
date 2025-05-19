#include "EventTrigger.h"
#include "NeoServiceLayerEnclave_t.h"
#include <nlohmann/json.hpp>
#include <sstream>
#include <algorithm>

using json = nlohmann::json;

EventTriggerManager::EventTriggerManager(
    StorageManager* storage_manager,
    JavaScriptManager* js_manager)
    : _storage_manager(storage_manager),
      _js_manager(js_manager),
      _initialized(false) {
}

bool EventTriggerManager::initialize() {
    std::lock_guard<std::mutex> lock(_mutex);

    if (_initialized) {
        return true;
    }

    // Load triggers from storage
    if (!load_triggers()) {
        ocall_print_string("Failed to load triggers from storage");
        return false;
    }

    _initialized = true;

    // Log initialization
    std::string log_message = "EventTriggerManager initialized with " + std::to_string(_triggers.size()) + " triggers";
    ocall_print_string(log_message.c_str());

    return true;
}

bool EventTriggerManager::register_trigger(const EventTriggerInfo& trigger) {
    if (!_initialized) {
        return false;
    }

    if (trigger.id.empty() || trigger.function_id.empty() || trigger.code.empty()) {
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    // Check if trigger already exists
    if (_triggers.find(trigger.id) != _triggers.end()) {
        return false;
    }

    // Save trigger to storage
    if (!save_trigger(trigger)) {
        return false;
    }

    // Add trigger to in-memory maps
    auto trigger_ptr = std::make_shared<EventTriggerInfo>(trigger);
    _triggers[trigger.id] = trigger_ptr;
    _triggers_by_type[trigger.type].push_back(trigger.id);

    return true;
}

bool EventTriggerManager::unregister_trigger(const std::string& trigger_id) {
    if (!_initialized) {
        return false;
    }

    if (trigger_id.empty()) {
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    // Check if trigger exists
    auto it = _triggers.find(trigger_id);
    if (it == _triggers.end()) {
        return false;
    }

    // Remove trigger from storage
    std::string storage_key = "trigger:" + trigger_id;
    if (!_storage_manager->remove(storage_key)) {
        return false;
    }

    // Remove trigger from in-memory maps
    EventTriggerType type = it->second->type;
    _triggers.erase(it);

    auto& type_triggers = _triggers_by_type[type];
    type_triggers.erase(std::remove(type_triggers.begin(), type_triggers.end(), trigger_id), type_triggers.end());

    return true;
}

std::shared_ptr<EventTriggerInfo> EventTriggerManager::get_trigger(const std::string& trigger_id) {
    if (!_initialized) {
        return nullptr;
    }

    if (trigger_id.empty()) {
        return nullptr;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    auto it = _triggers.find(trigger_id);
    if (it == _triggers.end()) {
        return nullptr;
    }

    return it->second;
}

std::vector<std::shared_ptr<EventTriggerInfo>> EventTriggerManager::list_triggers() {
    if (!_initialized) {
        return {};
    }

    std::lock_guard<std::mutex> lock(_mutex);

    std::vector<std::shared_ptr<EventTriggerInfo>> result;
    result.reserve(_triggers.size());

    for (const auto& pair : _triggers) {
        result.push_back(pair.second);
    }

    return result;
}

bool EventTriggerManager::enable_trigger(const std::string& trigger_id) {
    if (!_initialized) {
        return false;
    }

    if (trigger_id.empty()) {
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    auto it = _triggers.find(trigger_id);
    if (it == _triggers.end()) {
        return false;
    }

    // Update trigger
    it->second->enabled = true;

    // Save trigger to storage
    return save_trigger(*it->second);
}

bool EventTriggerManager::disable_trigger(const std::string& trigger_id) {
    if (!_initialized) {
        return false;
    }

    if (trigger_id.empty()) {
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    auto it = _triggers.find(trigger_id);
    if (it == _triggers.end()) {
        return false;
    }

    // Update trigger
    it->second->enabled = false;

    // Save trigger to storage
    return save_trigger(*it->second);
}

int EventTriggerManager::process_scheduled_triggers(uint64_t current_time) {
    if (!_initialized) {
        return 0;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    int processed_count = 0;

    // Get scheduled triggers
    auto it = _triggers_by_type.find(EventTriggerType::Schedule);
    if (it == _triggers_by_type.end()) {
        return 0;
    }

    for (const auto& trigger_id : it->second) {
        auto trigger_it = _triggers.find(trigger_id);
        if (trigger_it == _triggers.end()) {
            continue;
        }

        auto& trigger = trigger_it->second;

        // Skip disabled triggers
        if (!trigger->enabled) {
            continue;
        }

        // Check if it's time to execute
        if (current_time >= trigger->next_execution_time) {
            // Execute trigger
            if (execute_trigger(*trigger, "{}")) {
                processed_count++;
            }

            // Update next execution time
            trigger->next_execution_time = current_time + trigger->interval_seconds;

            // Save trigger to storage
            save_trigger(*trigger);
        }
    }

    return processed_count;
}

int EventTriggerManager::process_blockchain_event(const std::string& event_data) {
    if (!_initialized) {
        return 0;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    int processed_count = 0;

    // Get blockchain triggers
    auto it = _triggers_by_type.find(EventTriggerType::Blockchain);
    if (it == _triggers_by_type.end()) {
        return 0;
    }

    // Parse event data
    json event;
    try {
        event = json::parse(event_data);
    } catch (const std::exception& e) {
        std::string error_message = "Error parsing blockchain event data: ";
        error_message += e.what();
        ocall_print_string(error_message.c_str());
        return 0;
    }

    // Log event
    std::string log_message = "Processing blockchain event: " + event_data;
    ocall_print_string(log_message.c_str());

    for (const auto& trigger_id : it->second) {
        auto trigger_it = _triggers.find(trigger_id);
        if (trigger_it == _triggers.end()) {
            continue;
        }

        auto& trigger = trigger_it->second;

        // Skip disabled triggers
        if (!trigger->enabled) {
            continue;
        }

        // Check if the event matches the condition
        bool condition_met = false;

        try {
            // Parse condition
            json condition = json::parse(trigger->condition);

            // Check event type
            if (condition.contains("event_type") && event.contains("type")) {
                if (condition["event_type"] != event["type"]) {
                    continue;
                }
            }

            // Check contract address
            if (condition.contains("contract_address") && event.contains("contract")) {
                if (condition["contract_address"] != event["contract"]) {
                    continue;
                }
            }

            // Check event name
            if (condition.contains("event_name") && event.contains("name")) {
                if (condition["event_name"] != event["name"]) {
                    continue;
                }
            }

            // If we got here, the condition is met
            condition_met = true;
        } catch (const std::exception& e) {
            std::string error_message = "Error parsing trigger condition: ";
            error_message += e.what();
            ocall_print_string(error_message.c_str());
            continue;
        }

        // Execute trigger if condition is met
        if (condition_met) {
            if (execute_trigger(*trigger, event_data)) {
                processed_count++;
            }
        }
    }

    // Log processed count
    log_message = "Processed " + std::to_string(processed_count) + " blockchain triggers";
    ocall_print_string(log_message.c_str());

    return processed_count;
}

int EventTriggerManager::process_storage_event(const std::string& key, const std::string& operation) {
    if (!_initialized) {
        return 0;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    int processed_count = 0;

    // Get storage triggers
    auto it = _triggers_by_type.find(EventTriggerType::Storage);
    if (it == _triggers_by_type.end()) {
        return 0;
    }

    // Create event data
    json event_data = {
        {"key", key},
        {"operation", operation}
    };

    std::string event_data_str = event_data.dump();

    for (const auto& trigger_id : it->second) {
        auto trigger_it = _triggers.find(trigger_id);
        if (trigger_it == _triggers.end()) {
            continue;
        }

        auto& trigger = trigger_it->second;

        // Skip disabled triggers
        if (!trigger->enabled) {
            continue;
        }

        // Check if the event matches the condition
        // In a real implementation, this would parse the condition and check against the event

        // For now, just execute all storage triggers
        if (execute_trigger(*trigger, event_data_str)) {
            processed_count++;
        }
    }

    return processed_count;
}

int EventTriggerManager::process_external_event(const std::string& event_type, const std::string& event_data) {
    if (!_initialized) {
        return 0;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    int processed_count = 0;

    // Get external triggers
    auto it = _triggers_by_type.find(EventTriggerType::External);
    if (it == _triggers_by_type.end()) {
        return 0;
    }

    for (const auto& trigger_id : it->second) {
        auto trigger_it = _triggers.find(trigger_id);
        if (trigger_it == _triggers.end()) {
            continue;
        }

        auto& trigger = trigger_it->second;

        // Skip disabled triggers
        if (!trigger->enabled) {
            continue;
        }

        // Check if the event type matches the condition
        if (trigger->condition == event_type) {
            if (execute_trigger(*trigger, event_data)) {
                processed_count++;
            }
        }
    }

    return processed_count;
}

bool EventTriggerManager::save_trigger(const EventTriggerInfo& trigger) {
    // Serialize trigger to JSON
    json j = {
        {"id", trigger.id},
        {"type", static_cast<int>(trigger.type)},
        {"condition", trigger.condition},
        {"function_id", trigger.function_id},
        {"user_id", trigger.user_id},
        {"code", trigger.code},
        {"input_json", trigger.input_json},
        {"gas_limit", trigger.gas_limit},
        {"enabled", trigger.enabled},
        {"next_execution_time", trigger.next_execution_time},
        {"interval_seconds", trigger.interval_seconds}
    };

    std::string json_str = j.dump();

    // Save to storage
    std::string storage_key = "trigger:" + trigger.id;
    return _storage_manager->store_string(storage_key, json_str);
}

bool EventTriggerManager::load_triggers() {
    // Get all keys from storage
    std::vector<std::string> keys = _storage_manager->list_keys();

    for (const auto& key : keys) {
        // Check if this is a trigger key
        if (key.find("trigger:") != 0) {
            continue;
        }

        // Load trigger from storage
        std::string json_str = _storage_manager->retrieve_string(key);
        if (json_str.empty()) {
            continue;
        }

        try {
            // Parse JSON
            json j = json::parse(json_str);

            // Create trigger
            EventTriggerInfo trigger;
            trigger.id = j["id"];
            trigger.type = static_cast<EventTriggerType>(j["type"].get<int>());
            trigger.condition = j["condition"];
            trigger.function_id = j["function_id"];
            trigger.user_id = j["user_id"];
            trigger.code = j["code"];
            trigger.input_json = j["input_json"];
            trigger.gas_limit = j["gas_limit"];
            trigger.enabled = j["enabled"];
            trigger.next_execution_time = j["next_execution_time"];
            trigger.interval_seconds = j["interval_seconds"];

            // Add trigger to in-memory maps
            auto trigger_ptr = std::make_shared<EventTriggerInfo>(trigger);
            _triggers[trigger.id] = trigger_ptr;
            _triggers_by_type[trigger.type].push_back(trigger.id);
        }
        catch (const std::exception& e) {
            std::string error_message = "Error parsing trigger JSON: ";
            error_message += e.what();
            ocall_print_string(error_message.c_str());
        }
    }

    return true;
}

bool EventTriggerManager::execute_trigger(const EventTriggerInfo& trigger, const std::string& event_data) {
    // Create execution context
    JavaScriptContext context;
    context.function_id = trigger.function_id;
    context.user_id = trigger.user_id;
    context.code = trigger.code;

    // Combine input JSON with event data
    try {
        json input = json::parse(trigger.input_json);
        json event = json::parse(event_data);

        // Add event data to input
        input["event"] = event;

        // Add trigger information to input
        input["trigger"] = {
            {"id", trigger.id},
            {"type", static_cast<int>(trigger.type)},
            {"condition", trigger.condition}
        };

        // Add timestamp to input
        input["timestamp"] = std::chrono::duration_cast<std::chrono::milliseconds>(
            std::chrono::system_clock::now().time_since_epoch()).count();

        context.input_json = input.dump();
    }
    catch (const std::exception& e) {
        std::string error_message = "Error parsing JSON: ";
        error_message += e.what();
        ocall_print_string(error_message.c_str());
        return false;
    }

    // Set gas limit
    context.gas_limit = trigger.gas_limit;

    // Log trigger execution
    std::string log_message = "Executing trigger " + trigger.id + " for function " + trigger.function_id;
    ocall_print_string(log_message.c_str());

    // Execute JavaScript
    bool result = _js_manager->execute(context);

    // Log execution result
    if (result) {
        log_message = "Trigger " + trigger.id + " executed successfully";
        ocall_print_string(log_message.c_str());
    } else {
        log_message = "Trigger " + trigger.id + " execution failed: " + context.error;
        ocall_print_string(log_message.c_str());
    }

    return result;
}
