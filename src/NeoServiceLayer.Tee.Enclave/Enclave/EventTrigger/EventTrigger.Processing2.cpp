#include "EventTrigger.h"
#include <nlohmann/json.hpp>
#include <sstream>
#include <algorithm>
#include <cstdio>

using json = nlohmann::json;

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

        // Convert input to string
        context.input = input.dump();
    }
    catch (const std::exception& e) {
        std::string error_message = "Error preparing trigger input: ";
        error_message += e.what();
        fprintf(stderr, "%s\n", error_message.c_str());
        return false;
    }

    // Set gas limit
    context.gas_limit = trigger.gas_limit;

    // Execute JavaScript
    uint64_t gas_used = 0;
    std::string result = _js_manager->execute_javascript(context, gas_used);

    // Log execution
    std::string log_message = "Executed trigger " + trigger.id + ", gas used: " + std::to_string(gas_used);
    fprintf(stderr, "%s\n", log_message.c_str());

    // Check for errors
    try {
        json result_json = json::parse(result);
        if (result_json.contains("error")) {
            std::string error_message = "Error executing trigger: ";
            error_message += result_json["error"].get<std::string>();
            fprintf(stderr, "%s\n", error_message.c_str());
            return false;
        }
    }
    catch (const std::exception& e) {
        // Not a JSON response or other error
        std::string error_message = "Error parsing trigger result: ";
        error_message += e.what();
        fprintf(stderr, "%s\n", error_message.c_str());
        return false;
    }

    return true;
}
