/**
 * Secure Data Processor
 * 
 * This function processes sensitive data securely within the Open Enclave TEE.
 * It demonstrates how to handle confidential data processing with user secrets.
 */
function main(input) {
    // Log the operation securely
    Neo.secureLog(`Processing secure data for user: ${input.userId}`);
    
    // Validate input
    if (!input.data || !input.operation) {
        return {
            success: false,
            error: "Invalid input: missing required fields"
        };
    }
    
    // Access user secrets (API keys, encryption keys, etc.)
    if (!SECRETS || !SECRETS.API_KEY) {
        return {
            success: false,
            error: "Missing required secrets"
        };
    }
    
    // Process the data based on the requested operation
    let result;
    switch (input.operation) {
        case "encrypt":
            result = encryptData(input.data, SECRETS.API_KEY);
            break;
        case "decrypt":
            result = decryptData(input.data, SECRETS.API_KEY);
            break;
        case "analyze":
            result = analyzeData(input.data);
            break;
        default:
            return {
                success: false,
                error: "Unsupported operation"
            };
    }
    
    // Return the processed result
    return {
        success: true,
        operation: input.operation,
        result: result,
        timestamp: new Date().toISOString()
    };
}

/**
 * Encrypt data using a simple XOR cipher
 * In a real implementation, this would use strong encryption algorithms
 */
function encryptData(data, key) {
    // Generate a secure initialization vector
    let iv = '';
    for (let i = 0; i < 16; i++) {
        iv += String.fromCharCode(Neo.secureRandom(256));
    }
    
    // Simple XOR encryption with key and IV
    let encrypted = iv;
    const fullKey = key + iv;
    
    for (let i = 0; i < data.length; i++) {
        const charCode = data.charCodeAt(i) ^ fullKey.charCodeAt(i % fullKey.length);
        encrypted += String.fromCharCode(charCode);
    }
    
    // Convert to base64 for safe transport
    return btoa(encrypted);
}

/**
 * Decrypt data using a simple XOR cipher
 * In a real implementation, this would use strong encryption algorithms
 */
function decryptData(encryptedBase64, key) {
    try {
        // Decode from base64
        const encrypted = atob(encryptedBase64);
        
        // Extract IV (first 16 bytes)
        const iv = encrypted.substring(0, 16);
        const data = encrypted.substring(16);
        const fullKey = key + iv;
        
        // Decrypt using XOR
        let decrypted = '';
        for (let i = 0; i < data.length; i++) {
            const charCode = data.charCodeAt(i) ^ fullKey.charCodeAt(i % fullKey.length);
            decrypted += String.fromCharCode(charCode);
        }
        
        return decrypted;
    } catch (error) {
        return "Decryption failed: Invalid data format";
    }
}

/**
 * Analyze data for patterns and statistics
 */
function analyzeData(data) {
    // Simple data analysis
    const length = data.length;
    const wordCount = data.split(/\s+/).filter(Boolean).length;
    
    // Count character frequencies
    const charFrequency = {};
    for (let i = 0; i < data.length; i++) {
        const char = data[i];
        charFrequency[char] = (charFrequency[char] || 0) + 1;
    }
    
    // Find most common character
    let mostCommonChar = '';
    let maxFrequency = 0;
    for (const char in charFrequency) {
        if (charFrequency[char] > maxFrequency) {
            mostCommonChar = char;
            maxFrequency = charFrequency[char];
        }
    }
    
    // Calculate entropy (simple version)
    let entropy = 0;
    for (const char in charFrequency) {
        const probability = charFrequency[char] / length;
        entropy -= probability * Math.log2(probability);
    }
    
    return {
        length: length,
        wordCount: wordCount,
        mostCommonChar: mostCommonChar,
        mostCommonCharFrequency: maxFrequency,
        entropy: entropy.toFixed(2),
        isRandom: entropy > 4 ? "high randomness" : "low randomness"
    };
}
