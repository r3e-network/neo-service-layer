#include "EventTrigger.h"
#include <nlohmann/json.hpp>
#include <sstream>
#include <algorithm>
#include <cstdio>

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
        fprintf(stderr, "Failed to load triggers from storage\n");
        return false;
    }

    _initialized = true;

    // Log initialization
    std::string log_message = "EventTriggerManager initialized with " + std::to_string(_triggers.size()) + " triggers";
    fprintf(stderr, "%s\n", log_message.c_str());

    return true;
}
