#ifndef REMOTE_ATTESTATION_H
#define REMOTE_ATTESTATION_H

#include <string>
#include <vector>
#include <map>
#include "OcclumIntegration.h"

/**
 * @brief Remote attestation manager
 */
class RemoteAttestationManager {
public:
    /**
     * @brief Initialize a new instance of the RemoteAttestationManager class
     */
    RemoteAttestationManager();
    
    /**
     * @brief Destructor
     */
    ~RemoteAttestationManager();
    
    /**
     * @brief Initialize the remote attestation manager
     * @return True if initialization was successful, false otherwise
     */
    bool initialize();
    
    /**
     * @brief Generate evidence for remote attestation
     * @param custom_claims Custom claims to include in the evidence
     * @return The evidence as a byte vector, or an empty vector if generation failed
     */
    std::vector<uint8_t> generate_evidence(const std::vector<uint8_t>& custom_claims);
    
    /**
     * @brief Generate an attestation report for remote attestation
     * @param custom_claims Custom claims to include in the report
     * @param format The format of the report (e.g., "sgx_ecdsa", "sgx_epid")
     * @return The report as a byte vector, or an empty vector if generation failed
     */
    std::vector<uint8_t> generate_report(const std::vector<uint8_t>& custom_claims, const std::string& format);
    
    /**
     * @brief Verify evidence from a remote enclave
     * @param evidence The evidence to verify
     * @param custom_claims Output parameter for the custom claims from the evidence
     * @return True if the evidence is valid, false otherwise
     */
    bool verify_evidence(const std::vector<uint8_t>& evidence, std::vector<uint8_t>& custom_claims);
    
    /**
     * @brief Verify a report from a remote enclave
     * @param report The report to verify
     * @param format The format of the report (e.g., "sgx_ecdsa", "sgx_epid")
     * @param custom_claims Output parameter for the custom claims from the report
     * @return True if the report is valid, false otherwise
     */
    bool verify_report(const std::vector<uint8_t>& report, const std::string& format, std::vector<uint8_t>& custom_claims);
    
    /**
     * @brief Create custom claims for attestation
     * @param claims The claims to include
     * @return The custom claims as a byte vector
     */
    static std::vector<uint8_t> create_custom_claims(const std::map<std::string, std::string>& claims);
    
    /**
     * @brief Parse custom claims from attestation
     * @param custom_claims The custom claims to parse
     * @return The parsed claims
     */
    static std::map<std::string, std::string> parse_custom_claims(const std::vector<uint8_t>& custom_claims);
    
private:
    bool _initialized;
    std::string _format_id;
    
    // Helper methods
    bool initialize_attester();
    bool initialize_verifier();
};

#endif // REMOTE_ATTESTATION_H
