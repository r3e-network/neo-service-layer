// Main JavaScript file for testing the enclave

console.log('Neo Service Layer Enclave - Occlum LibOS');
console.log('----------------------------------------');

// Load the enclave library
const enclave = require('./libneoserviceenclave.js');

// Initialize the enclave
console.log('Initializing enclave...');
const initialized = enclave.initialize();
console.log('Enclave initialized:', initialized);

// Get the enclave status
console.log('Getting enclave status...');
const status = enclave.get_status();
console.log('Enclave status:', status);

// Create a JavaScript context
console.log('Creating JavaScript context...');
const contextId = enclave.create_js_context();
console.log('JavaScript context created with ID:', contextId);

// Execute JavaScript code
console.log('Executing JavaScript code...');
const code = `
function main(input) {
    console.log('Input:', input);
    console.log('Secrets:', SECRETS);
    
    // Generate a random number using Neo.secureRandom
    const randomNumber = Neo.secureRandom(100);
    console.log('Random number:', randomNumber);
    
    // Return a result
    return {
        message: 'Hello from the enclave!',
        input: input,
        randomNumber: randomNumber
    };
}
`;

const input = JSON.stringify({ name: 'Test User', value: 42 });
const userId = 'test-user';
const functionId = 'test-function';

// Store a user secret
console.log('Storing user secret...');
const secretStored = enclave.store_user_secret(userId, 'api_key', 'secret-api-key-12345');
console.log('User secret stored:', secretStored);

// Execute the JavaScript code
console.log('Executing JavaScript code...');
const result = enclave.execute_js_code(contextId, code, input, userId, functionId);
console.log('JavaScript execution result:', result);

// List user secrets
console.log('Listing user secrets...');
const secrets = enclave.list_user_secrets(userId);
console.log('User secrets:', secrets);

// Generate a random UUID
console.log('Generating UUID...');
const uuid = enclave.generate_uuid();
console.log('UUID:', uuid);

// Generate attestation evidence
console.log('Generating attestation evidence...');
const evidence = enclave.generate_attestation();
console.log('Attestation evidence generated:', evidence ? 'Yes' : 'No');

// Destroy the JavaScript context
console.log('Destroying JavaScript context...');
const destroyed = enclave.destroy_js_context(contextId);
console.log('JavaScript context destroyed:', destroyed);

// Clean up the enclave
console.log('Cleaning up enclave...');
const cleaned = enclave.cleanup();
console.log('Enclave cleaned up:', cleaned);

console.log('----------------------------------------');
console.log('Neo Service Layer Enclave - Test completed');
