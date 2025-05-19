#ifndef EVENT_TRIGGER_H
#define EVENT_TRIGGER_H

#include <string>
#include <vector>
#include <map>
#include <mutex>
#include <functional>
#include <memory>
#include "StorageManager.h"
#include "JavaScriptEngine.h"

/**
 * @brief Event trigger types
 */
enum class EventTriggerType {
    /**
     * @brief Trigger on a schedule
     */
    Schedule,
    
    /**
     * @brief Trigger on a blockchain event
     */
    Blockchain,
    
    /**
     * @brief Trigger on a storage event
     */
    Storage,
    
    /**
     * @brief Trigger on an external event
     */
    External
};

/**
 * @brief Event trigger information
 */
struct EventTriggerInfo {
    /**
     * @brief The trigger ID
     */
    std::string id;
    
    /**
     * @brief The trigger type
     */
    EventTriggerType type;
    
    /**
     * @brief The trigger condition
     */
    std::string condition;
    
    /**
     * @brief The function ID to execute
     */
    std::string function_id;
    
    /**
     * @brief The user ID
     */
    std::string user_id;
    
    /**
     * @brief The JavaScript code to execute
     */
    std::string code;
    
    /**
     * @brief The input data as a JSON string
     */
    std::string input_json;
    
    /**
     * @brief The gas limit for execution
     */
    uint64_t gas_limit;
    
    /**
     * @brief Whether the trigger is enabled
     */
    bool enabled;
    
    /**
     * @brief The next execution time for scheduled triggers
     */
    uint64_t next_execution_time;
    
    /**
     * @brief The interval in seconds for scheduled triggers
     */
    uint64_t interval_seconds;
};

/**
 * @brief Event trigger manager
 */
class EventTriggerManager {
public:
    /**
     * @brief Initialize a new instance of the EventTriggerManager class
     * @param storage_manager The storage manager
     * @param js_manager The JavaScript manager
     */
    EventTriggerManager(
        StorageManager* storage_manager,
        JavaScriptManager* js_manager);
    
    /**
     * @brief Initialize the event trigger manager
     * @return True if initialization was successful, false otherwise
     */
    bool initialize();
    
    /**
     * @brief Register a trigger
     * @param trigger The trigger information
     * @return True if registration was successful, false otherwise
     */
    bool register_trigger(const EventTriggerInfo& trigger);
    
    /**
     * @brief Unregister a trigger
     * @param trigger_id The trigger ID
     * @return True if unregistration was successful, false otherwise
     */
    bool unregister_trigger(const std::string& trigger_id);
    
    /**
     * @brief Get a trigger
     * @param trigger_id The trigger ID
     * @return The trigger information, or nullptr if the trigger does not exist
     */
    std::shared_ptr<EventTriggerInfo> get_trigger(const std::string& trigger_id);
    
    /**
     * @brief List all triggers
     * @return A list of all triggers
     */
    std::vector<std::shared_ptr<EventTriggerInfo>> list_triggers();
    
    /**
     * @brief Enable a trigger
     * @param trigger_id The trigger ID
     * @return True if the trigger was enabled successfully, false otherwise
     */
    bool enable_trigger(const std::string& trigger_id);
    
    /**
     * @brief Disable a trigger
     * @param trigger_id The trigger ID
     * @return True if the trigger was disabled successfully, false otherwise
     */
    bool disable_trigger(const std::string& trigger_id);
    
    /**
     * @brief Process scheduled triggers
     * @param current_time The current time in seconds since epoch
     * @return The number of triggers processed
     */
    int process_scheduled_triggers(uint64_t current_time);
    
    /**
     * @brief Process a blockchain event
     * @param event_data The event data as a JSON string
     * @return The number of triggers processed
     */
    int process_blockchain_event(const std::string& event_data);
    
    /**
     * @brief Process a storage event
     * @param key The storage key
     * @param operation The operation (e.g., "create", "update", "delete")
     * @return The number of triggers processed
     */
    int process_storage_event(const std::string& key, const std::string& operation);
    
    /**
     * @brief Process an external event
     * @param event_type The event type
     * @param event_data The event data as a JSON string
     * @return The number of triggers processed
     */
    int process_external_event(const std::string& event_type, const std::string& event_data);
    
private:
    StorageManager* _storage_manager;
    JavaScriptManager* _js_manager;
    std::mutex _mutex;
    bool _initialized;
    
    // Triggers by ID
    std::map<std::string, std::shared_ptr<EventTriggerInfo>> _triggers;
    
    // Triggers by type
    std::map<EventTriggerType, std::vector<std::string>> _triggers_by_type;
    
    // Helper methods
    bool save_trigger(const EventTriggerInfo& trigger);
    bool load_triggers();
    bool execute_trigger(const EventTriggerInfo& trigger, const std::string& event_data);
};

#endif // EVENT_TRIGGER_H
