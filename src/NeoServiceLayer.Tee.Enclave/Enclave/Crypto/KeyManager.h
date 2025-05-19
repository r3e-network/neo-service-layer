#ifndef KEY_MANAGER_H
#define KEY_MANAGER_H

#include <string>
#include <vector>
#include <map>
#include <mutex>
#include <openssl/evp.h>
#include <openssl/rsa.h>
#include <openssl/ec.h>

/**
 * @brief Key types
 */
enum class KeyType {
    /**
     * @brief AES key
     */
    AES,

    /**
     * @brief RSA key
     */
    RSA,

    /**
     * @brief EC key
     */
    EC
};

/**
 * @brief Key information
 */
struct KeyInfo {
    /**
     * @brief The key ID
     */
    std::string id;

    /**
     * @brief The key type
     */
    KeyType type;

    /**
     * @brief The key data
     */
    std::vector<uint8_t> data;

    /**
     * @brief The key creation time
     */
    uint64_t creation_time;

    /**
     * @brief The key expiration time
     */
    uint64_t expiration_time;

    /**
     * @brief Whether the key is active
     */
    bool active;
};

/**
 * @brief Key manager
 */
class KeyManager {
public:
    /**
     * @brief Initialize a new instance of the KeyManager class
     */
    KeyManager();

    /**
     * @brief Destructor
     */
    ~KeyManager();

    /**
     * @brief Initialize the key manager
     * @return True if initialization was successful, false otherwise
     */
    bool initialize();

    /**
     * @brief Generate a new key
     * @param type The key type
     * @param bits The key size in bits
     * @param expiration_days The number of days until the key expires
     * @return The key ID, or an empty string if generation failed
     */
    std::string generate_key(KeyType type, int bits, int expiration_days);

    /**
     * @brief Get a key by ID
     * @param key_id The key ID
     * @return The key information, or nullptr if the key does not exist
     */
    const KeyInfo* get_key(const std::string& key_id);

    /**
     * @brief Get the active key of a specific type
     * @param type The key type
     * @return The key information, or nullptr if no active key exists
     */
    const KeyInfo* get_active_key(KeyType type);

    /**
     * @brief Rotate keys
     * @param type The key type
     * @param bits The key size in bits
     * @param expiration_days The number of days until the key expires
     * @return The new key ID, or an empty string if rotation failed
     */
    std::string rotate_key(KeyType type, int bits, int expiration_days);

    /**
     * @brief Encrypt data using a key
     * @param key_id The key ID
     * @param data The data to encrypt
     * @return The encrypted data, or an empty vector if encryption failed
     */
    std::vector<uint8_t> encrypt(const std::string& key_id, const std::vector<uint8_t>& data);

    /**
     * @brief Decrypt data using a key
     * @param key_id The key ID
     * @param data The data to decrypt
     * @return The decrypted data, or an empty vector if decryption failed
     */
    std::vector<uint8_t> decrypt(const std::string& key_id, const std::vector<uint8_t>& data);

    /**
     * @brief Sign data using a key
     * @param key_id The key ID
     * @param data The data to sign
     * @return The signature, or an empty vector if signing failed
     */
    std::vector<uint8_t> sign(const std::string& key_id, const std::vector<uint8_t>& data);

    /**
     * @brief Verify a signature using a key
     * @param key_id The key ID
     * @param data The data to verify
     * @param signature The signature to verify
     * @return True if the signature is valid, false otherwise
     */
    bool verify(const std::string& key_id, const std::vector<uint8_t>& data, const std::vector<uint8_t>& signature);

    /**
     * @brief List all keys
     * @return A list of all key IDs
     */
    std::vector<std::string> list_keys();

    /**
     * @brief Delete a key
     * @param key_id The key ID
     * @return True if the key was deleted, false otherwise
     */
    bool delete_key(const std::string& key_id);

    /**
     * @brief Save keys to persistent storage
     * @return True if the keys were saved, false otherwise
     */
    bool save_keys();

    /**
     * @brief Load keys from persistent storage
     * @return True if the keys were loaded, false otherwise
     */
    bool load_keys();

private:
    std::mutex _mutex;
    bool _initialized;
    std::map<std::string, KeyInfo> _keys;
    std::map<KeyType, std::string> _active_keys;

    // Helper methods
    void secure_log(const std::string& message);
    std::string generate_key_id();
    uint64_t get_current_time();
    uint64_t get_expiration_time(int days);
    std::vector<uint8_t> serialize_keys();
    bool deserialize_keys(const std::vector<uint8_t>& data);

    // Crypto implementation methods
    std::vector<uint8_t> encrypt_aes(const KeyInfo& key_info, const std::vector<uint8_t>& data);
    std::vector<uint8_t> decrypt_aes(const KeyInfo& key_info, const std::vector<uint8_t>& data);
    std::vector<uint8_t> encrypt_rsa(const KeyInfo& key_info, const std::vector<uint8_t>& data);
    std::vector<uint8_t> decrypt_rsa(const KeyInfo& key_info, const std::vector<uint8_t>& data);
    std::vector<uint8_t> sign_rsa(const KeyInfo& key_info, const std::vector<uint8_t>& data);
    bool verify_rsa(const KeyInfo& key_info, const std::vector<uint8_t>& data, const std::vector<uint8_t>& signature);
    std::vector<uint8_t> sign_ec(const KeyInfo& key_info, const std::vector<uint8_t>& data);
    bool verify_ec(const KeyInfo& key_info, const std::vector<uint8_t>& data, const std::vector<uint8_t>& signature);
};

#endif // KEY_MANAGER_H
