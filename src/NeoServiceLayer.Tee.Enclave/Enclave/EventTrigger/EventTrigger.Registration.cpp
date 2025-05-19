#include "EventTrigger.h"
#include <nlohmann/json.hpp>
#include <sstream>
#include <algorithm>
#include <cstdio>

using json = nlohmann::json;

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
    if (!_storage_manager->remove("triggers", storage_key)) {
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
