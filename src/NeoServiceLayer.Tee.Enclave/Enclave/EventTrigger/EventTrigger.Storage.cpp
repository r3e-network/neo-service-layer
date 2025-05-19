#include "EventTrigger.h"
#include <nlohmann/json.hpp>
#include <sstream>
#include <algorithm>
#include <cstdio>

using json = nlohmann::json;

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
    return _storage_manager->store("triggers", storage_key, json_str);
}

bool EventTriggerManager::load_triggers() {
    // Get all keys from storage
    std::vector<std::string> keys = _storage_manager->list_keys("triggers");

    for (const auto& key : keys) {
        // Check if this is a trigger key
        if (key.find("trigger:") != 0) {
            continue;
        }

        // Load trigger from storage
        std::string json_str;
        if (!_storage_manager->retrieve_data("triggers", key, json_str) || json_str.empty()) {
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
            fprintf(stderr, "%s\n", error_message.c_str());
        }
    }

    return true;
}
