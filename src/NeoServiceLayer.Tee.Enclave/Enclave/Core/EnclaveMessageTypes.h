#ifndef ENCLAVE_MESSAGE_TYPES_H
#define ENCLAVE_MESSAGE_TYPES_H

/**
 * @brief Message types for communication between the host and the enclave
 *
 * This enum defines all the message types that can be sent between the host and the enclave.
 * The enclave uses Occlum LibOS for secure execution of JavaScript code.
 */
enum EnclaveMessageType {
    // Basic operations
    MESSAGE_TYPE_INITIALIZE = 1,
    MESSAGE_TYPE_CLEANUP = 2,
    MESSAGE_TYPE_GET_STATUS = 3,
    GET_STATUS = MESSAGE_TYPE_GET_STATUS, // For backward compatibility

    // JavaScript execution
    MESSAGE_TYPE_EXECUTE_JS = 10,
    MESSAGE_TYPE_CREATE_JS_CONTEXT = 11,
    MESSAGE_TYPE_DESTROY_JS_CONTEXT = 12,
    MESSAGE_TYPE_EXECUTE_JS_CODE = 13,
    CREATE_JS_CONTEXT = MESSAGE_TYPE_CREATE_JS_CONTEXT, // For backward compatibility
    DESTROY_JS_CONTEXT = MESSAGE_TYPE_DESTROY_JS_CONTEXT, // For backward compatibility
    EXECUTE_JS_CODE = MESSAGE_TYPE_EXECUTE_JS_CODE, // For backward compatibility
    VERIFY_JS_CODE = 14,

    // New JavaScript executor
    INITIALIZE_JS_EXECUTOR = 15,
    EXECUTE_JS_CODE_NEW = 16,
    EXECUTE_JS_FUNCTION = 17,
    COLLECT_JS_GARBAGE = 18,
    SHUTDOWN_JS_EXECUTOR = 19,

    // User secrets
    MESSAGE_TYPE_STORE_SECRET = 20,
    MESSAGE_TYPE_GET_SECRET = 21,
    MESSAGE_TYPE_DELETE_SECRET = 22,
    MESSAGE_TYPE_LIST_SECRETS = 23,
    STORE_USER_SECRET = MESSAGE_TYPE_STORE_SECRET, // For backward compatibility
    GET_USER_SECRET = MESSAGE_TYPE_GET_SECRET, // For backward compatibility
    DELETE_USER_SECRET = MESSAGE_TYPE_DELETE_SECRET, // For backward compatibility
    LIST_USER_SECRETS = MESSAGE_TYPE_LIST_SECRETS, // For backward compatibility

    // Random number generation
    MESSAGE_TYPE_GENERATE_RANDOM = 30,
    MESSAGE_TYPE_GENERATE_UUID = 31,

    // Attestation
    MESSAGE_TYPE_GENERATE_ATTESTATION = 32,
    MESSAGE_TYPE_VERIFY_ATTESTATION = 33,
    GENERATE_ATTESTATION = MESSAGE_TYPE_GENERATE_ATTESTATION, // For backward compatibility
    VERIFY_ATTESTATION = MESSAGE_TYPE_VERIFY_ATTESTATION, // For backward compatibility

    // Compliance
    MESSAGE_TYPE_VERIFY_COMPLIANCE = 35,

    // Persistent storage
    MESSAGE_TYPE_INITIALIZE_STORAGE = 40,
    INITIALIZE_STORAGE = MESSAGE_TYPE_INITIALIZE_STORAGE, // For backward compatibility
    STORE_PERSISTENT_DATA = 41,
    RETRIEVE_PERSISTENT_DATA = 42,
    DELETE_PERSISTENT_DATA = 43,
    CHECK_PERSISTENT_DATA = 44,
    LIST_PERSISTENT_DATA = 45,

    // Gas accounting
    GET_GAS_BALANCE = 50,
    UPDATE_GAS_BALANCE = 51,
    GET_GAS_USAGE = 52,

    // Occlum operations
    MESSAGE_TYPE_OCCLUM_INIT = 60,
    MESSAGE_TYPE_OCCLUM_EXEC = 61
};

#endif // ENCLAVE_MESSAGE_TYPES_H
