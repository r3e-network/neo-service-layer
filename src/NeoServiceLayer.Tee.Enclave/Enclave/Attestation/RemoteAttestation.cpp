#include "RemoteAttestation.h"
#include "../Core/EnclaveUtils.h"
#include <nlohmann/json.hpp>
#include <sstream>
#include <iomanip>

using json = nlohmann::json;
using namespace NeoServiceLayer::Enclave::OcclumIntegration;

RemoteAttestationManager::RemoteAttestationManager()
    : _initialized(false), _format_id("sgx_ecdsa") {
}

RemoteAttestationManager::~RemoteAttestationManager() {
    // No explicit cleanup needed for Occlum
}

bool RemoteAttestationManager::initialize() {
    if (_initialized) {
        return true;
    }
    
    // Initialize attester
    if (!initialize_attester()) {
        LogError("Failed to initialize attester");
        return false;
    }
    
    // Initialize verifier
    if (!initialize_verifier()) {
        LogError("Failed to initialize verifier");
        return false;
    }
    
    _initialized = true;
    return true;
}

bool RemoteAttestationManager::initialize_attester() {
    // Occlum doesn't require explicit attester initialization
    return true;
}

bool RemoteAttestationManager::initialize_verifier() {
    // Occlum doesn't require explicit verifier initialization
    return true;
}

std::vector<uint8_t> RemoteAttestationManager::generate_evidence(const std::vector<uint8_t>& custom_claims) {
    if (!_initialized && !initialize()) {
        return {};
    }
    
    try {
        // Generate attestation evidence using Occlum
        std::vector<uint8_t> evidence = GenerateAttestationEvidence();
        
        // If we have custom claims, append them to the evidence
        if (!custom_claims.empty()) {
            // Create a JSON object with the evidence and custom claims
            json j;
            j["evidence"] = Base64Encode(evidence);
            j["custom_claims"] = Base64Encode(custom_claims);
            
            // Convert to string and then to bytes
            std::string json_str = j.dump();
            return std::vector<uint8_t>(json_str.begin(), json_str.end());
        }
        
        return evidence;
    }
    catch (const std::exception& ex) {
        LogError("Exception in generate_evidence: %s", ex.what());
        return {};
    }
}

std::vector<uint8_t> RemoteAttestationManager::generate_report(const std::vector<uint8_t>& custom_claims, const std::string& format) {
    if (!_initialized && !initialize()) {
        return {};
    }
    
    try {
        // Set format ID based on format string
        if (format != "sgx_ecdsa" && format != "sgx_epid") {
            LogError("Unsupported format: %s", format.c_str());
            return {};
        }
        
        _format_id = format;
        
        // Generate attestation evidence using Occlum
        std::vector<uint8_t> report = GenerateAttestationEvidence();
        
        // If we have custom claims, append them to the report
        if (!custom_claims.empty()) {
            // Create a JSON object with the report and custom claims
            json j;
            j["report"] = Base64Encode(report);
            j["format"] = format;
            j["custom_claims"] = Base64Encode(custom_claims);
            
            // Convert to string and then to bytes
            std::string json_str = j.dump();
            return std::vector<uint8_t>(json_str.begin(), json_str.end());
        }
        
        return report;
    }
    catch (const std::exception& ex) {
        LogError("Exception in generate_report: %s", ex.what());
        return {};
    }
}

bool RemoteAttestationManager::verify_evidence(const std::vector<uint8_t>& evidence, std::vector<uint8_t>& custom_claims) {
    if (!_initialized && !initialize()) {
        return false;
    }
    
    try {
        // Check if the evidence is in JSON format
        if (evidence.size() > 0 && evidence[0] == '{') {
            // Parse the JSON
            std::string json_str(evidence.begin(), evidence.end());
            json j = json::parse(json_str);
            
            // Extract the evidence and custom claims
            std::vector<uint8_t> raw_evidence = Base64Decode(j["evidence"].get<std::string>());
            
            if (j.contains("custom_claims")) {
                custom_claims = Base64Decode(j["custom_claims"].get<std::string>());
            }
            
            // Verify the evidence
            return VerifyAttestationEvidence(raw_evidence, {});
        }
        
        // If not in JSON format, verify directly
        return VerifyAttestationEvidence(evidence, {});
    }
    catch (const std::exception& ex) {
        LogError("Exception in verify_evidence: %s", ex.what());
        return false;
    }
}

bool RemoteAttestationManager::verify_report(const std::vector<uint8_t>& report, const std::string& format, std::vector<uint8_t>& custom_claims) {
    if (!_initialized && !initialize()) {
        return false;
    }
    
    try {
        // Check if the report is in JSON format
        if (report.size() > 0 && report[0] == '{') {
            // Parse the JSON
            std::string json_str(report.begin(), report.end());
            json j = json::parse(json_str);
            
            // Extract the report and custom claims
            std::vector<uint8_t> raw_report = Base64Decode(j["report"].get<std::string>());
            
            if (j.contains("custom_claims")) {
                custom_claims = Base64Decode(j["custom_claims"].get<std::string>());
            }
            
            // Verify the report
            return VerifyAttestationEvidence(raw_report, {});
        }
        
        // If not in JSON format, verify directly
        return VerifyAttestationEvidence(report, {});
    }
    catch (const std::exception& ex) {
        LogError("Exception in verify_report: %s", ex.what());
        return false;
    }
}

std::vector<uint8_t> RemoteAttestationManager::create_custom_claims(const std::map<std::string, std::string>& claims) {
    // Convert claims to JSON
    json j = claims;
    std::string json_str = j.dump();
    
    // Convert JSON string to byte vector
    return std::vector<uint8_t>(json_str.begin(), json_str.end());
}

std::map<std::string, std::string> RemoteAttestationManager::parse_custom_claims(const std::vector<uint8_t>& custom_claims) {
    // Convert byte vector to JSON string
    std::string json_str(custom_claims.begin(), custom_claims.end());
    
    // Parse JSON string
    try {
        json j = json::parse(json_str);
        return j.get<std::map<std::string, std::string>>();
    }
    catch (const std::exception& e) {
        LogError("Failed to parse custom claims: %s", e.what());
        return {};
    }
}
