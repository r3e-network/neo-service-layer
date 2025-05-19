#include "EventTrigger.h"
#include <nlohmann/json.hpp>
#include <sstream>
#include <algorithm>
#include <cstdio>

using json = nlohmann::json;

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
        fprintf(stderr, "%s\n", error_message.c_str());
        return 0;
    }

    // Log event
    std::string log_message = "Processing blockchain event: " + event_data;
    fprintf(stderr, "%s\n", log_message.c_str());

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
            fprintf(stderr, "%s\n", error_message.c_str());
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
    fprintf(stderr, "%s\n", log_message.c_str());

    return processed_count;
}
