#ifndef COMPLIANCE_SERVICE_H
#define COMPLIANCE_SERVICE_H

#include <string>
#include <vector>
#include <map>
#include <mutex>
#include <memory>
#include <cstdint>

/**
 * @brief Compliance verification service for the enclave.
 * 
 * This class provides a compliance verification service for the enclave, allowing
 * JavaScript functions to be verified for compliance with regulatory requirements.
 */
class ComplianceService {
public:
    /**
     * @brief Constructs a new ComplianceService instance.
     */
    ComplianceService();
    
    /**
     * @brief Destructor.
     */
    ~ComplianceService();
    
    /**
     * @brief Initializes the compliance service.
     * 
     * @return True if initialization was successful, false otherwise.
     */
    bool initialize();
    
    /**
     * @brief Checks if the compliance service is initialized.
     * 
     * @return True if the compliance service is initialized, false otherwise.
     */
    bool is_initialized() const;
    
    /**
     * @brief Verifies a JavaScript function for compliance.
     * 
     * @param code The JavaScript code to verify.
     * @param user_id The ID of the user who owns the function.
     * @param function_id The ID of the function.
     * @param compliance_rules The compliance rules to check against (JSON string).
     * @return A JSON string containing the verification result.
     */
    std::string verify_compliance(const std::string& code, const std::string& user_id, 
                                 const std::string& function_id, const std::string& compliance_rules);
    
    /**
     * @brief Gets the compliance rules for a specific jurisdiction.
     * 
     * @param jurisdiction The jurisdiction code (e.g., "US", "EU", "JP").
     * @return A JSON string containing the compliance rules.
     */
    std::string get_compliance_rules(const std::string& jurisdiction);
    
    /**
     * @brief Sets the compliance rules for a specific jurisdiction.
     * 
     * @param jurisdiction The jurisdiction code (e.g., "US", "EU", "JP").
     * @param rules The compliance rules (JSON string).
     * @return True if the rules were set successfully, false otherwise.
     */
    bool set_compliance_rules(const std::string& jurisdiction, const std::string& rules);
    
    /**
     * @brief Gets the compliance status for a specific function.
     * 
     * @param function_id The ID of the function.
     * @param jurisdiction The jurisdiction code (e.g., "US", "EU", "JP").
     * @return A JSON string containing the compliance status.
     */
    std::string get_compliance_status(const std::string& function_id, const std::string& jurisdiction);
    
    /**
     * @brief Sets the compliance status for a specific function.
     * 
     * @param function_id The ID of the function.
     * @param jurisdiction The jurisdiction code (e.g., "US", "EU", "JP").
     * @param status The compliance status (JSON string).
     * @return True if the status was set successfully, false otherwise.
     */
    bool set_compliance_status(const std::string& function_id, const std::string& jurisdiction, 
                              const std::string& status);
    
    /**
     * @brief Verifies a user's identity.
     * 
     * @param user_id The ID of the user.
     * @param identity_data The identity data (JSON string).
     * @param jurisdiction The jurisdiction code (e.g., "US", "EU", "JP").
     * @return A JSON string containing the verification result.
     */
    std::string verify_identity(const std::string& user_id, const std::string& identity_data, 
                               const std::string& jurisdiction);
    
    /**
     * @brief Gets the identity verification status for a specific user.
     * 
     * @param user_id The ID of the user.
     * @param jurisdiction The jurisdiction code (e.g., "US", "EU", "JP").
     * @return A JSON string containing the identity verification status.
     */
    std::string get_identity_status(const std::string& user_id, const std::string& jurisdiction);
    
    /**
     * @brief Sets the identity verification status for a specific user.
     * 
     * @param user_id The ID of the user.
     * @param jurisdiction The jurisdiction code (e.g., "US", "EU", "JP").
     * @param status The identity verification status (JSON string).
     * @return True if the status was set successfully, false otherwise.
     */
    bool set_identity_status(const std::string& user_id, const std::string& jurisdiction, 
                            const std::string& status);
    
private:
    // Implementation details
    class Impl;
    std::unique_ptr<Impl> pimpl;
    
    // Mutex for thread safety
    mutable std::mutex mutex_;
    
    // Initialized flag
    bool initialized_;
    
    // Helper methods
    bool check_code_for_prohibited_apis(const std::string& code, const std::vector<std::string>& prohibited_apis);
    bool check_code_for_data_access(const std::string& code, const std::vector<std::string>& prohibited_data);
    bool check_code_for_network_access(const std::string& code);
    bool check_code_for_resource_usage(const std::string& code, uint64_t max_gas);
};

#endif // COMPLIANCE_SERVICE_H
