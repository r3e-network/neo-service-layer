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

void ComplianceService::Impl::initialize_default_rules() {
    // Initialize default compliance rules
    compliance_rules_["global"] = R"({
        "prohibited_apis": ["eval", "Function", "setTimeout", "setInterval", "XMLHttpRequest", "fetch"],
        "prohibited_data": ["password", "credit_card", "ssn", "passport"],
        "allow_network_access": false,
        "max_gas": 1000000
    })";

    compliance_rules_["US"] = R"({
        "prohibited_apis": ["eval", "Function", "setTimeout", "setInterval", "XMLHttpRequest", "fetch"],
        "prohibited_data": ["password", "credit_card", "ssn", "passport", "driver_license"],
        "allow_network_access": false,
        "max_gas": 500000
    })";

    compliance_rules_["EU"] = R"({
        "prohibited_apis": ["eval", "Function", "setTimeout", "setInterval", "XMLHttpRequest", "fetch"],
        "prohibited_data": ["password", "credit_card", "national_id", "passport", "health_data"],
        "allow_network_access": false,
        "max_gas": 500000
    })";

    // Initialize default identity rules
    identity_rules_["global"] = R"({
        "required_fields": ["name", "email"]
    })";

    identity_rules_["US"] = R"({
        "required_fields": ["name", "email", "address", "phone"]
    })";

    identity_rules_["EU"] = R"({
        "required_fields": ["name", "email", "address", "phone", "consent"]
    })";
}

bool ComplianceService::Impl::check_code_for_prohibited_apis(const std::string& code, const std::vector<std::string>& prohibited_apis,
                                   json& result) {
    bool compliant = true;

    for (const auto& api : prohibited_apis) {
        // Create a regex pattern to match the API
        std::regex pattern("\\b" + api + "\\b");

        // Check if the code contains the API
        if (std::regex_search(code, pattern)) {
            compliant = false;
            result["violations"].push_back({
                {"type", "prohibited_api"},
                {"api", api},
                {"message", "Code contains prohibited API: " + api}
            });
        }
    }

    return compliant;
}

bool ComplianceService::Impl::check_code_for_data_access(const std::string& code, const std::vector<std::string>& prohibited_data,
                               json& result) {
    bool compliant = true;

    for (const auto& data : prohibited_data) {
        // Create a regex pattern to match the data
        std::regex pattern("\\b" + data + "\\b");

        // Check if the code contains the data
        if (std::regex_search(code, pattern)) {
            compliant = false;
            result["violations"].push_back({
                {"type", "prohibited_data"},
                {"data", data},
                {"message", "Code contains prohibited data: " + data}
            });
        }
    }

    return compliant;
}

bool ComplianceService::Impl::check_code_for_network_access(const std::string& code, json& result) {
    // Create regex patterns to match network access
    std::vector<std::regex> patterns = {
        std::regex("\\bXMLHttpRequest\\b"),
        std::regex("\\bfetch\\b"),
        std::regex("\\bWebSocket\\b"),
        std::regex("\\bnavigator\\.sendBeacon\\b"),
        std::regex("\\bwindow\\.open\\b"),
        std::regex("\\blocation\\.href\\b"),
        std::regex("\\blocation\\.replace\\b"),
        std::regex("\\blocation\\.assign\\b")
    };

    bool compliant = true;

    for (const auto& pattern : patterns) {
        // Check if the code contains network access
        if (std::regex_search(code, pattern)) {
            compliant = false;
            result["violations"].push_back({
                {"type", "network_access"},
                {"message", "Code contains network access"}
            });
            break;
        }
    }

    return compliant;
}

bool ComplianceService::Impl::check_code_for_resource_usage(const std::string& code, uint64_t max_gas, json& result) {
    // Estimate gas usage based on code size and complexity
    uint64_t estimated_gas = code.length() * 10;

    // Add gas for loops
    std::regex loop_pattern("\\b(for|while|do)\\b");
    std::string::const_iterator search_start(code.cbegin());
    std::string::const_iterator search_end(code.cend());
    std::regex_iterator<std::string::const_iterator> loop_it(search_start, search_end, loop_pattern);
    std::regex_iterator<std::string::const_iterator> loop_end;

    int loop_count = 0;
    while (loop_it != loop_end) {
        loop_count++;
        ++loop_it;
    }

    estimated_gas += loop_count * 1000;

    // Check if estimated gas exceeds max gas
    if (estimated_gas > max_gas) {
        result["violations"].push_back({
            {"type", "resource_usage"},
            {"estimated_gas", estimated_gas},
            {"max_gas", max_gas},
            {"message", "Estimated gas usage exceeds maximum allowed"}
        });
        return false;
    }

    return true;
}

// ComplianceService implementation

std::string ComplianceService::verify_compliance(const std::string& code, const std::string& user_id,
                                               const std::string& function_id, const std::string& compliance_rules) {
    std::lock_guard<std::mutex> lock(mutex_);

    if (!initialized_ && !initialize()) {
        throw std::runtime_error("ComplianceService not initialized");
    }

    return pimpl->verify_compliance(code, user_id, function_id, compliance_rules);
}

bool ComplianceService::check_code_for_prohibited_apis(const std::string& code, const std::vector<std::string>& prohibited_apis) {
    json result = {{"violations", json::array()}};
    return pimpl->check_code_for_prohibited_apis(code, prohibited_apis, result);
}

bool ComplianceService::check_code_for_data_access(const std::string& code, const std::vector<std::string>& prohibited_data) {
    json result = {{"violations", json::array()}};
    return pimpl->check_code_for_data_access(code, prohibited_data, result);
}

bool ComplianceService::check_code_for_network_access(const std::string& code) {
    json result = {{"violations", json::array()}};
    return pimpl->check_code_for_network_access(code, result);
}

bool ComplianceService::check_code_for_resource_usage(const std::string& code, uint64_t max_gas) {
    json result = {{"violations", json::array()}};
    return pimpl->check_code_for_resource_usage(code, max_gas, result);
}
