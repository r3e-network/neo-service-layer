namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// Message types for communication between the host and the enclave.
    /// </summary>
    public enum EnclaveMessageType
    {
        // Basic operations
        GET_STATUS = 1,
        
        // JavaScript execution
        CREATE_JS_CONTEXT = 10,
        DESTROY_JS_CONTEXT = 11,
        EXECUTE_JS_CODE = 12,
        VERIFY_JS_CODE = 13,
        
        // New JavaScript executor
        INITIALIZE_JS_EXECUTOR = 14,
        EXECUTE_JS_CODE_NEW = 15,
        EXECUTE_JS_FUNCTION = 16,
        COLLECT_JS_GARBAGE = 17,
        SHUTDOWN_JS_EXECUTOR = 18,
        
        // User secrets
        STORE_USER_SECRET = 20,
        GET_USER_SECRET = 21,
        DELETE_USER_SECRET = 22,
        LIST_USER_SECRETS = 23,
        
        // Attestation
        GENERATE_ATTESTATION = 30,
        VERIFY_ATTESTATION = 31,
        
        // Persistent storage
        INITIALIZE_STORAGE = 40,
        STORE_PERSISTENT_DATA = 41,
        RETRIEVE_PERSISTENT_DATA = 42,
        DELETE_PERSISTENT_DATA = 43,
        CHECK_PERSISTENT_DATA = 44,
        LIST_PERSISTENT_DATA = 45,
        
        // Gas accounting
        GET_GAS_BALANCE = 50,
        UPDATE_GAS_BALANCE = 51,
        GET_GAS_USAGE = 52
    }
}
