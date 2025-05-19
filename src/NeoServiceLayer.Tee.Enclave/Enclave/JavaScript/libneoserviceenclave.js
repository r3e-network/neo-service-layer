// JavaScript bindings for the Neo Service Layer Enclave

const ffi = require('ffi-napi');
const ref = require('ref-napi');
const ArrayType = require('ref-array-napi');

// Define types
const ByteArray = ArrayType(ref.types.uint8);
const StringArray = ArrayType(ref.types.CString);

// Load the enclave library
const enclaveLib = ffi.Library('libneoserviceenclave', {
    // Basic operations
    'initialize': ['bool', []],
    'cleanup': ['bool', []],
    'get_status': ['string', []],
    'process_message': ['string', ['int', 'string']],
    
    // JavaScript execution
    'create_js_context': ['uint64', []],
    'destroy_js_context': ['bool', ['uint64']],
    'execute_js_code': ['string', ['uint64', 'string', 'string', 'string', 'string']],
    'execute_javascript': ['string', ['string', 'string', 'string', 'string', 'string', ref.refType('uint64')]],
    
    // User secrets
    'store_user_secret': ['bool', ['string', 'string', 'string']],
    'get_user_secret': ['string', ['string', 'string']],
    'delete_user_secret': ['bool', ['string', 'string']],
    'list_user_secrets': [StringArray, ['string']],
    
    // Random number generation
    'generate_random_number': ['int', ['int', 'int']],
    'generate_random_bytes': [ByteArray, ['size_t']],
    'generate_uuid': ['string', []],
    
    // Attestation
    'generate_attestation': [ByteArray, []],
    'verify_attestation': ['bool', [ByteArray, ByteArray]],
    
    // Compliance
    'verify_compliance': ['string', ['string', 'string', 'string', 'string']],
    
    // Storage
    'initialize_storage': ['bool', ['string']],
    
    // Occlum operations
    'occlum_init': ['bool', ['string', 'string']],
    'occlum_exec': ['int', ['string', StringArray, StringArray]]
});

// Export the enclave API
module.exports = {
    // Basic operations
    initialize: () => enclaveLib.initialize(),
    cleanup: () => enclaveLib.cleanup(),
    get_status: () => enclaveLib.get_status(),
    process_message: (messageType, messageData) => enclaveLib.process_message(messageType, messageData),
    
    // JavaScript execution
    create_js_context: () => enclaveLib.create_js_context(),
    destroy_js_context: (contextId) => enclaveLib.destroy_js_context(contextId),
    execute_js_code: (contextId, code, input, userId, functionId) => 
        enclaveLib.execute_js_code(contextId, code, input, userId, functionId),
    execute_javascript: (code, input, secrets, functionId, userId) => {
        const gasUsed = ref.alloc('uint64');
        const result = enclaveLib.execute_javascript(code, input, secrets, functionId, userId, gasUsed);
        return {
            result: result,
            gasUsed: gasUsed.deref()
        };
    },
    
    // User secrets
    store_user_secret: (userId, secretName, secretValue) => 
        enclaveLib.store_user_secret(userId, secretName, secretValue),
    get_user_secret: (userId, secretName) => 
        enclaveLib.get_user_secret(userId, secretName),
    delete_user_secret: (userId, secretName) => 
        enclaveLib.delete_user_secret(userId, secretName),
    list_user_secrets: (userId) => {
        const secretsArray = enclaveLib.list_user_secrets(userId);
        const secrets = [];
        for (let i = 0; i < secretsArray.length; i++) {
            if (secretsArray[i] === null) break;
            secrets.push(secretsArray[i]);
        }
        return secrets;
    },
    
    // Random number generation
    generate_random_number: (min, max) => 
        enclaveLib.generate_random_number(min, max),
    generate_random_bytes: (length) => {
        const bytesArray = enclaveLib.generate_random_bytes(length);
        const bytes = [];
        for (let i = 0; i < bytesArray.length; i++) {
            bytes.push(bytesArray[i]);
        }
        return Buffer.from(bytes);
    },
    generate_uuid: () => enclaveLib.generate_uuid(),
    
    // Attestation
    generate_attestation: () => {
        const evidenceArray = enclaveLib.generate_attestation();
        const evidence = [];
        for (let i = 0; i < evidenceArray.length; i++) {
            evidence.push(evidenceArray[i]);
        }
        return Buffer.from(evidence);
    },
    verify_attestation: (evidence, endorsements) => 
        enclaveLib.verify_attestation(evidence, endorsements),
    
    // Compliance
    verify_compliance: (code, userId, functionId, complianceRules) => 
        enclaveLib.verify_compliance(code, userId, functionId, complianceRules),
    
    // Storage
    initialize_storage: (storagePath) => 
        enclaveLib.initialize_storage(storagePath),
    
    // Occlum operations
    occlum_init: (instanceDir, logLevel) => 
        enclaveLib.occlum_init(instanceDir, logLevel),
    occlum_exec: (path, argv, env) => 
        enclaveLib.occlum_exec(path, argv, env)
};
