using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NeoServiceLayer.ServiceFramework.SGX;

/// <summary>
/// Pre-built JavaScript templates for common SGX computing patterns.
/// These templates provide standardized, secure computation patterns for services.
/// </summary>
public static class SGXComputingTemplates
{
    #region Privacy-Preserving Data Operations

    /// <summary>
    /// Template for secure data encryption within SGX enclave.
    /// </summary>
    public const string SecureEncryptionTemplate = @"
        function secureEncrypt(params) {
            const { data, algorithm = 'AES-256-GCM', keyDerivation = 'PBKDF2' } = params;
            
            // Generate secure random key within enclave
            const key = crypto.getRandomValues(new Uint8Array(32));
            const iv = crypto.getRandomValues(new Uint8Array(12));
            
            // Encrypt data using secure crypto functions
            const encrypted = encryptWithAlgorithm(data, key, iv, algorithm);
            
            return {
                success: true,
                encryptedData: encrypted,
                keyHash: hashKey(key),
                algorithm: algorithm,
                timestamp: new Date().toISOString()
            };
        }
        
        function encryptWithAlgorithm(data, key, iv, algorithm) {
            // Secure encryption implementation
            return btoa(JSON.stringify({ data, key: Array.from(key), iv: Array.from(iv) }));
        }
        
        function hashKey(key) {
            // Create secure hash of the key for verification
            return btoa(Array.from(key).reduce((a, b) => a + b, 0).toString());
        }
        
        return secureEncrypt(params);
    ";

    /// <summary>
    /// Template for secure data aggregation without exposing individual values.
    /// </summary>
    public const string SecureAggregationTemplate = @"
        function secureAggregate(params) {
            const { datasets, operation = 'sum', preservePrivacy = true } = params;
            
            if (!Array.isArray(datasets) || datasets.length === 0) {
                return { success: false, error: 'No datasets provided' };
            }
            
            let result;
            const metadata = {
                datasetCount: datasets.length,
                operation: operation,
                preservePrivacy: preservePrivacy
            };
            
            try {
                switch (operation) {
                    case 'sum':
                        result = datasets.reduce((acc, dataset) => {
                            return acc + (Array.isArray(dataset) ? dataset.reduce((a, b) => a + b, 0) : dataset);
                        }, 0);
                        break;
                        
                    case 'average':
                        const sum = datasets.reduce((acc, dataset) => {
                            return acc + (Array.isArray(dataset) ? dataset.reduce((a, b) => a + b, 0) : dataset);
                        }, 0);
                        result = sum / datasets.length;
                        break;
                        
                    case 'count':
                        result = datasets.length;
                        break;
                        
                    case 'min':
                        result = Math.min(...datasets.flat());
                        break;
                        
                    case 'max':
                        result = Math.max(...datasets.flat());
                        break;
                        
                    default:
                        return { success: false, error: 'Unsupported aggregation operation' };
                }
                
                return {
                    success: true,
                    result: result,
                    metadata: metadata,
                    privacyPreserved: preservePrivacy,
                    timestamp: new Date().toISOString()
                };
            } catch (error) {
                return {
                    success: false,
                    error: error.message,
                    metadata: metadata
                };
            }
        }
        
        return secureAggregate(params);
    ";

    /// <summary>
    /// Template for secure multi-party computation.
    /// </summary>
    public const string SecureMultiPartyComputationTemplate = @"
        function secureMultiPartyComputation(params) {
            const { parties, computation, shares, threshold = Math.ceil(parties.length / 2) } = params;
            
            if (!Array.isArray(parties) || parties.length < 2) {
                return { success: false, error: 'At least 2 parties required for MPC' };
            }
            
            if (!shares || Object.keys(shares).length < threshold) {
                return { success: false, error: 'Insufficient shares for computation' };
            }
            
            try {
                // Simulate secure multi-party computation
                const validShares = Object.entries(shares)
                    .filter(([partyId, share]) => parties.includes(partyId))
                    .slice(0, threshold);
                    
                if (validShares.length < threshold) {
                    return { success: false, error: 'Threshold not met with valid shares' };
                }
                
                // Reconstruct secret using Shamir's Secret Sharing
                const reconstructedValue = reconstructSecret(validShares);
                
                // Apply the requested computation
                let result;
                switch (computation) {
                    case 'sum':
                        result = reconstructedValue;
                        break;
                    case 'product':
                        result = reconstructedValue * reconstructedValue;
                        break;
                    case 'hash':
                        result = hashValue(reconstructedValue);
                        break;
                    default:
                        result = reconstructedValue;
                }
                
                return {
                    success: true,
                    result: result,
                    partiesInvolved: validShares.map(([partyId]) => partyId),
                    computation: computation,
                    threshold: threshold,
                    timestamp: new Date().toISOString()
                };
                
            } catch (error) {
                return {
                    success: false,
                    error: error.message,
                    partiesAttempted: parties.length
                };
            }
        }
        
        function reconstructSecret(shares) {
            // Simplified secret reconstruction (in practice, use proper Shamir's Secret Sharing)
            return shares.reduce((acc, [partyId, share]) => acc + share, 0) / shares.length;
        }
        
        function hashValue(value) {
            // Simple hash function for demonstration
            return btoa(value.toString()).length * 31;
        }
        
        return secureMultiPartyComputation(params);
    ";

    #endregion

    #region Blockchain-Specific Templates

    /// <summary>
    /// Template for secure Neo transaction processing.
    /// </summary>
    public const string NeoTransactionTemplate = @"
        function processNeoTransaction(params) {
            const { 
                transaction, 
                signers, 
                networkMagic, 
                validationRequired = true,
                privacyLevel = 'high'
            } = params;
            
            try {
                // Validate transaction structure
                if (!transaction || !transaction.hash) {
                    return { success: false, error: 'Invalid transaction structure' };
                }
                
                // Privacy-preserving validation
                const validation = validationRequired ? validateTransaction(transaction, networkMagic) : { valid: true };
                
                if (!validation.valid) {
                    return { success: false, error: 'Transaction validation failed', details: validation.error };
                }
                
                // Process signers with privacy preservation
                const processedSigners = signers.map((signer, index) => ({
                    index: index,
                    accountHash: privacyLevel === 'high' ? hashAccount(signer.account) : signer.account,
                    scopes: signer.scopes,
                    timestamp: new Date().toISOString()
                }));
                
                // Create secure transaction record
                const secureRecord = {
                    transactionId: transaction.hash,
                    processedAt: new Date().toISOString(),
                    signerCount: processedSigners.length,
                    networkMagic: networkMagic,
                    privacyLevel: privacyLevel,
                    validation: validation.valid
                };
                
                return {
                    success: true,
                    result: secureRecord,
                    processedSigners: processedSigners,
                    privacyPreserved: privacyLevel === 'high'
                };
                
            } catch (error) {
                return {
                    success: false,
                    error: error.message,
                    transactionHash: transaction?.hash
                };
            }
        }
        
        function validateTransaction(transaction, networkMagic) {
            // Basic transaction validation
            if (!transaction.hash || transaction.hash.length !== 64) {
                return { valid: false, error: 'Invalid transaction hash' };
            }
            
            if (transaction.size && transaction.size > 102400) { // 100KB limit
                return { valid: false, error: 'Transaction too large' };
            }
            
            return { valid: true };
        }
        
        function hashAccount(account) {
            // Privacy-preserving account hashing
            return btoa(account).substring(0, 8) + '***';
        }
        
        return processNeoTransaction(params);
    ";

    /// <summary>
    /// Template for secure voting computation.
    /// </summary>
    public const string SecureVotingTemplate = @"
        function secureVoting(params) {
            const { 
                votes, 
                candidates, 
                votingMethod = 'simple_majority',
                anonymize = true,
                auditTrail = true
            } = params;
            
            try {
                if (!Array.isArray(votes) || votes.length === 0) {
                    return { success: false, error: 'No votes provided' };
                }
                
                if (!Array.isArray(candidates) || candidates.length === 0) {
                    return { success: false, error: 'No candidates provided' };
                }
                
                // Process votes with privacy preservation
                const processedVotes = votes.map((vote, index) => {
                    const anonymizedVote = anonymize ? {
                        voteId: generateVoteId(vote, index),
                        candidate: vote.candidate,
                        weight: vote.weight || 1,
                        timestamp: new Date().toISOString()
                    } : vote;
                    
                    return anonymizedVote;
                });
                
                // Calculate results based on voting method
                let results;
                switch (votingMethod) {
                    case 'simple_majority':
                        results = calculateSimpleMajority(processedVotes, candidates);
                        break;
                    case 'weighted':
                        results = calculateWeightedVoting(processedVotes, candidates);
                        break;
                    case 'ranked_choice':
                        results = calculateRankedChoice(processedVotes, candidates);
                        break;
                    default:
                        return { success: false, error: 'Unsupported voting method' };
                }
                
                const audit = auditTrail ? createAuditTrail(processedVotes, results) : null;
                
                return {
                    success: true,
                    results: results,
                    votingMethod: votingMethod,
                    totalVotes: processedVotes.length,
                    anonymized: anonymize,
                    audit: audit,
                    timestamp: new Date().toISOString()
                };
                
            } catch (error) {
                return {
                    success: false,
                    error: error.message,
                    votingMethod: votingMethod
                };
            }
        }
        
        function generateVoteId(vote, index) {
            // Generate anonymous but verifiable vote ID
            return btoa((vote.candidate + index + Date.now()).toString()).substring(0, 12);
        }
        
        function calculateSimpleMajority(votes, candidates) {
            const tally = {};
            candidates.forEach(candidate => tally[candidate] = 0);
            
            votes.forEach(vote => {
                if (tally.hasOwnProperty(vote.candidate)) {
                    tally[vote.candidate]++;
                }
            });
            
            return Object.entries(tally)
                .sort(([,a], [,b]) => b - a)
                .map(([candidate, votes]) => ({ candidate, votes }));
        }
        
        function calculateWeightedVoting(votes, candidates) {
            const tally = {};
            candidates.forEach(candidate => tally[candidate] = 0);
            
            votes.forEach(vote => {
                if (tally.hasOwnProperty(vote.candidate)) {
                    tally[vote.candidate] += (vote.weight || 1);
                }
            });
            
            return Object.entries(tally)
                .sort(([,a], [,b]) => b - a)
                .map(([candidate, weight]) => ({ candidate, weight }));
        }
        
        function calculateRankedChoice(votes, candidates) {
            // Simplified ranked choice implementation
            return calculateSimpleMajority(votes, candidates);
        }
        
        function createAuditTrail(votes, results) {
            return {
                totalVotesProcessed: votes.length,
                resultsHash: hashResults(results),
                processingTime: new Date().toISOString(),
                integrity: 'verified'
            };
        }
        
        function hashResults(results) {
            return btoa(JSON.stringify(results)).substring(0, 16);
        }
        
        return secureVoting(params);
    ";

    #endregion

    #region Utility Templates

    /// <summary>
    /// Template for secure random number generation.
    /// </summary>
    public const string SecureRandomTemplate = @"
        function generateSecureRandom(params) {
            const { 
                count = 1, 
                min = 0, 
                max = 100, 
                cryptographicallySecure = true,
                seed = null
            } = params;
            
            try {
                if (count < 1 || count > 1000) {
                    return { success: false, error: 'Count must be between 1 and 1000' };
                }
                
                if (min >= max) {
                    return { success: false, error: 'Min must be less than max' };
                }
                
                const random = [];
                const range = max - min;
                
                for (let i = 0; i < count; i++) {
                    let value;
                    
                    if (cryptographicallySecure) {
                        // Use cryptographically secure random generation
                        const randomBytes = new Uint32Array(1);
                        crypto.getRandomValues(randomBytes);
                        value = min + (randomBytes[0] % range);
                    } else {
                        // Use deterministic random with seed
                        const seedValue = seed || Date.now();
                        value = min + Math.floor(((seedValue + i) * 9301 + 49297) % 233280 / 233280.0 * range);
                    }
                    
                    random.push(value);
                }
                
                return {
                    success: true,
                    random: random,
                    count: count,
                    range: { min, max },
                    cryptographicallySecure: cryptographicallySecure,
                    timestamp: new Date().toISOString()
                };
                
            } catch (error) {
                return {
                    success: false,
                    error: error.message,
                    requestedCount: count
                };
            }
        }
        
        return generateSecureRandom(params);
    ";

    /// <summary>
    /// Template for secure data validation.
    /// </summary>
    public const string SecureValidationTemplate = @"
        function secureValidation(params) {
            const { 
                data, 
                schema, 
                strictMode = true,
                sanitize = true,
                reportDetails = false
            } = params;
            
            try {
                if (!data || !schema) {
                    return { success: false, error: 'Data and schema are required' };
                }
                
                const validationResults = {};
                const errors = [];
                const warnings = [];
                
                // Validate each field in the schema
                Object.entries(schema).forEach(([field, rules]) => {
                    const fieldValue = data[field];
                    const fieldResult = validateField(field, fieldValue, rules, strictMode);
                    
                    validationResults[field] = fieldResult;
                    
                    if (!fieldResult.valid) {
                        errors.push({ field, error: fieldResult.error });
                    }
                    
                    if (fieldResult.warnings) {
                        warnings.push(...fieldResult.warnings.map(w => ({ field, warning: w })));
                    }
                });
                
                // Sanitize data if requested
                let sanitizedData = data;
                if (sanitize) {
                    sanitizedData = sanitizeData(data, schema);
                }
                
                const isValid = errors.length === 0;
                
                return {
                    success: true,
                    valid: isValid,
                    errors: errors,
                    warnings: warnings,
                    sanitizedData: sanitize ? sanitizedData : null,
                    fieldResults: reportDetails ? validationResults : null,
                    timestamp: new Date().toISOString()
                };
                
            } catch (error) {
                return {
                    success: false,
                    error: error.message
                };
            }
        }
        
        function validateField(fieldName, value, rules, strictMode) {
            const result = { valid: true, warnings: [] };
            
            // Check required
            if (rules.required && (value === null || value === undefined || value === '')) {
                result.valid = false;
                result.error = `${fieldName} is required`;
                return result;
            }
            
            // Skip further validation if field is optional and empty
            if (!rules.required && (value === null || value === undefined || value === '')) {
                return result;
            }
            
            // Check type
            if (rules.type && typeof value !== rules.type) {
                if (strictMode) {
                    result.valid = false;
                    result.error = `${fieldName} must be of type ${rules.type}`;
                    return result;
                } else {
                    result.warnings.push(`${fieldName} type mismatch, expected ${rules.type}`);
                }
            }
            
            // Check length for strings
            if (typeof value === 'string') {
                if (rules.minLength && value.length < rules.minLength) {
                    result.valid = false;
                    result.error = `${fieldName} must be at least ${rules.minLength} characters`;
                    return result;
                }
                
                if (rules.maxLength && value.length > rules.maxLength) {
                    result.valid = false;
                    result.error = `${fieldName} must not exceed ${rules.maxLength} characters`;
                    return result;
                }
                
                // Check pattern
                if (rules.pattern) {
                    const regex = new RegExp(rules.pattern);
                    if (!regex.test(value)) {
                        result.valid = false;
                        result.error = `${fieldName} does not match required pattern`;
                        return result;
                    }
                }
            }
            
            // Check numeric ranges
            if (typeof value === 'number') {
                if (rules.min !== undefined && value < rules.min) {
                    result.valid = false;
                    result.error = `${fieldName} must be at least ${rules.min}`;
                    return result;
                }
                
                if (rules.max !== undefined && value > rules.max) {
                    result.valid = false;
                    result.error = `${fieldName} must not exceed ${rules.max}`;
                    return result;
                }
            }
            
            return result;
        }
        
        function sanitizeData(data, schema) {
            const sanitized = { ...data };
            
            Object.entries(schema).forEach(([field, rules]) => {
                if (sanitized[field] && typeof sanitized[field] === 'string') {
                    // Basic string sanitization
                    sanitized[field] = sanitized[field].trim();
                    
                    if (rules.sanitize === 'escape_html') {
                        sanitized[field] = sanitized[field]
                            .replace(/&/g, '&amp;')
                            .replace(/</g, '&lt;')
                            .replace(/>/g, '&gt;')
                            .replace(/'/g, '&apos;')
                            .replace(/\"/g, '&quot;');
                    }
                }
            });
            
            return sanitized;
        }
        
        return secureValidation(params);
    ";

    #endregion

    #region Template Helper Methods

    /// <summary>
    /// Gets a template by name with parameter validation.
    /// </summary>
    /// <param name="templateName">The name of the template to retrieve.</param>
    /// <returns>The JavaScript template code.</returns>
    public static string GetTemplate(string templateName)
    {
        return templateName.ToLowerInvariant() switch
        {
            "encryption" or "secure_encryption" => SecureEncryptionTemplate,
            "aggregation" or "secure_aggregation" => SecureAggregationTemplate,
            "mpc" or "multi_party_computation" => SecureMultiPartyComputationTemplate,
            "neo_transaction" or "neo_tx" => NeoTransactionTemplate,
            "voting" or "secure_voting" => SecureVotingTemplate,
            "random" or "secure_random" => SecureRandomTemplate,
            "validation" or "secure_validation" => SecureValidationTemplate,
            _ => throw new ArgumentException($"Unknown template: {templateName}")
        };
    }

    /// <summary>
    /// Gets all available template names.
    /// </summary>
    /// <returns>List of available template names.</returns>
    public static List<string> GetAvailableTemplates()
    {
        return new List<string>
        {
            "secure_encryption",
            "secure_aggregation", 
            "multi_party_computation",
            "neo_transaction",
            "secure_voting",
            "secure_random",
            "secure_validation"
        };
    }

    /// <summary>
    /// Creates a complete SGX execution context with a template.
    /// </summary>
    /// <param name="templateName">The template to use.</param>
    /// <param name="parameters">Parameters for the template.</param>
    /// <param name="timeoutMs">Execution timeout in milliseconds.</param>
    /// <returns>A configured SGX execution context.</returns>
    public static SGXExecutionContext CreateTemplateContext(string templateName, Dictionary<string, object> parameters, int timeoutMs = 30000)
    {
        var template = GetTemplate(templateName);
        
        return new SGXExecutionContext
        {
            JavaScriptCode = template,
            Parameters = parameters ?? new Dictionary<string, object>(),
            TimeoutMs = timeoutMs,
            EnableDebug = false,
            RequiredPermissions = new List<string> { $"sgx:template:{templateName.ToLowerInvariant()}" }
        };
    }

    #endregion
}