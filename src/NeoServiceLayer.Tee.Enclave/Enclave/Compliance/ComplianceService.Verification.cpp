#include "ComplianceService.h"
#include "../Core/EnclaveUtils.h"
#include "../Occlum/OcclumIntegration.h"
#include <nlohmann/json.hpp>
#include <vector>
#include <string>
#include <regex>
#include <stdexcept>
#include <chrono>
#include <sstream>
#include <iomanip>

// External function declarations for host calls
extern "C" {
    void host_log(const char* message);
}

using json = nlohmann::json;

// Forward declaration
class ComplianceService::Impl;

std::string ComplianceService::Impl::verify_compliance(const std::string& code, const std::string& user_id,
                                 const std::string& function_id, const std::string& compliance_rules) {
    if (!initialized_) {
        throw std::runtime_error("ComplianceService not initialized");
    }

    try {
        // Parse compliance rules
        json rules;
        if (!compliance_rules.empty()) {
            rules = json::parse(compliance_rules);
        } else {
            // Use default rules
            rules = json::parse(R"({
                "jurisdiction": "global",
                "prohibited_apis": ["eval", "Function", "setTimeout", "setInterval", "XMLHttpRequest", "fetch"],
                "prohibited_data": ["password", "credit_card", "ssn", "passport"],
                "allow_network_access": false,
                "max_gas": 1000000
            })");
        }

        // Check code for compliance
        json result = {
            {"function_id", function_id},
            {"user_id", user_id},
            {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(
                std::chrono::system_clock::now().time_since_epoch()).count()},
            {"compliant", true},
            {"violations", json::array()}
        };

        // Check for prohibited APIs
        std::vector<std::string> prohibited_apis;
        if (rules.contains("prohibited_apis") && rules["prohibited_apis"].is_array()) {
            for (const auto& api : rules["prohibited_apis"]) {
                prohibited_apis.push_back(api.get<std::string>());
            }
        }

        if (!check_code_for_prohibited_apis(code, prohibited_apis, result)) {
            result["compliant"] = false;
        }

        // Check for prohibited data access
        std::vector<std::string> prohibited_data;
        if (rules.contains("prohibited_data") && rules["prohibited_data"].is_array()) {
            for (const auto& data : rules["prohibited_data"]) {
                prohibited_data.push_back(data.get<std::string>());
            }
        }

        if (!check_code_for_data_access(code, prohibited_data, result)) {
            result["compliant"] = false;
        }

        // Check for network access
        bool allow_network_access = false;
        if (rules.contains("allow_network_access") && rules["allow_network_access"].is_boolean()) {
            allow_network_access = rules["allow_network_access"].get<bool>();
        }

        if (!allow_network_access && !check_code_for_network_access(code, result)) {
            result["compliant"] = false;
        }

        // Check for resource usage
        uint64_t max_gas = UINT64_MAX;
        if (rules.contains("max_gas") && rules["max_gas"].is_number()) {
            max_gas = rules["max_gas"].get<uint64_t>();
        }

        if (!check_code_for_resource_usage(code, max_gas, result)) {
            result["compliant"] = false;
        }

        // Store compliance status
        std::string jurisdiction = "global";
        if (rules.contains("jurisdiction") && rules["jurisdiction"].is_string()) {
            jurisdiction = rules["jurisdiction"].get<std::string>();
        }

        set_compliance_status(function_id, jurisdiction, result.dump());

        return result.dump();
    } catch (const std::exception& e) {
        json error_result = {
            {"function_id", function_id},
            {"user_id", user_id},
            {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(
                std::chrono::system_clock::now().time_since_epoch()).count()},
            {"compliant", false},
            {"error", e.what()}
        };

        return error_result.dump();
    }
}

std::string ComplianceService::Impl::verify_identity(const std::string& user_id, const std::string& identity_data,
                               const std::string& jurisdiction) {
    if (!initialized_) {
        throw std::runtime_error("ComplianceService not initialized");
    }

    try {
        // Parse identity data
        json identity = json::parse(identity_data);

        // Get verification rules for jurisdiction
        json rules;
        auto it = identity_rules_.find(jurisdiction);
        if (it != identity_rules_.end()) {
            rules = json::parse(it->second);
        } else {
            // Use default rules
            rules = json::parse(identity_rules_["global"]);
        }

        // Verify identity
        json result = {
            {"user_id", user_id},
            {"jurisdiction", jurisdiction},
            {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(
                std::chrono::system_clock::now().time_since_epoch()).count()},
            {"verified", true},
            {"violations", json::array()}
        };

        // Check required fields
        if (rules.contains("required_fields") && rules["required_fields"].is_array()) {
            for (const auto& field : rules["required_fields"]) {
                std::string field_name = field.get<std::string>();
                if (!identity.contains(field_name) || identity[field_name].is_null()) {
                    result["verified"] = false;
                    result["violations"].push_back({
                        {"type", "missing_field"},
                        {"field", field_name},
                        {"message", "Required field is missing"}
                    });
                }
            }
        }

        // Store identity status
        set_identity_status(user_id, jurisdiction, result.dump());

        return result.dump();
    } catch (const std::exception& e) {
        json error_result = {
            {"user_id", user_id},
            {"jurisdiction", jurisdiction},
            {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(
                std::chrono::system_clock::now().time_since_epoch()).count()},
            {"verified", false},
            {"error", e.what()}
        };

        return error_result.dump();
    }
}

// ComplianceService implementation

std::string ComplianceService::verify_identity(const std::string& user_id, const std::string& identity_data,
                                             const std::string& jurisdiction) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_ && !initialize()) {
        throw std::runtime_error("ComplianceService not initialized");
    }

    return pimpl->verify_identity(user_id, identity_data, jurisdiction);
}
