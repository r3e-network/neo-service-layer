#include "FileStorageProvider.h"
#include "../Core/EnclaveUtils.h"
#include "../EnclaveHost.h"
#include <algorithm>
#include <cstring>
#include <fcntl.h>
#include <nlohmann/json.hpp>
#include <sgx_tcrypto.h>
#include <sgx_trts.h>

using json = nlohmann::json;

// External functions declared in EnclaveHost.h
extern "C" {
    int host_file_open(const char* path, int flags, int mode);
    size_t host_file_read(int fd, void* buf, size_t count);
    size_t host_file_write(int fd, const void* buf, size_t count);
    int64_t host_file_seek(int fd, int64_t offset, int whence);
    int host_file_close(int fd);
    int host_file_unlink(const char* path);
    int host_directory_create(const char* path, int mode);
    char** host_directory_list(const char* path, int* count);
    void host_directory_list_free(char** files, int count);
}

FileStorageProvider::FileStorageProvider()
    : _storage_path(""), _initialized(false), _next_transaction_id(1)
{
}

FileStorageProvider::~FileStorageProvider()
{
    // Clean up any active transactions
    for (auto& transaction : _transactions)
    {
        transaction.second.changes.clear();
        transaction.second.deletions.clear();
    }
    _transactions.clear();
}

bool FileStorageProvider::initialize(const std::string& storage_path)
{
    std::lock_guard<std::mutex> lock(_mutex);

    if (_initialized)
    {
        return true;
    }

    if (storage_path.empty())
    {
        host_log("Error: Empty storage path");
        return false;
    }

    _storage_path = storage_path;

    // Create the storage directory if it doesn't exist
    int result = host_directory_create(_storage_path.c_str(), 0700);
    if (result != 0 && result != EEXIST)
    {
        host_log(("Error creating storage directory: " + _storage_path).c_str());
        return false;
    }

    // Create the metadata directory if it doesn't exist
    std::string metadata_dir = _storage_path + "/.metadata";
    result = host_directory_create(metadata_dir.c_str(), 0700);
    if (result != 0 && result != EEXIST)
    {
        host_log(("Error creating metadata directory: " + metadata_dir).c_str());
        return false;
    }

    _initialized = true;
    return true;
}

bool FileStorageProvider::is_initialized() const
{
    std::lock_guard<std::mutex> lock(_mutex);
    return _initialized;
}

bool FileStorageProvider::store(const std::string& key, const std::vector<uint8_t>& data)
{
    if (!_initialized)
    {
        host_log("Error: Storage provider not initialized");
        return false;
    }

    if (key.empty())
    {
        host_log("Error: Empty key");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        // If we're in a transaction, don't write to disk yet
        auto transaction_it = _transactions.find(_next_transaction_id - 1);
        if (transaction_it != _transactions.end())
        {
            transaction_it->second.changes[key] = data;
            return true;
        }

        // Encrypt the data
        std::vector<uint8_t> encrypted_data = encrypt_data(data);

        // Save to file
        std::string file_path = get_file_path(key);
        return save_to_file(file_path, encrypted_data);
    }
    catch (const std::exception& e)
    {
        std::string error_message = "Error storing data: ";
        error_message += e.what();
        host_log(error_message.c_str());
        return false;
    }
}

std::vector<uint8_t> FileStorageProvider::retrieve(const std::string& key)
{
    if (!_initialized)
    {
        host_log("Error: Storage provider not initialized");
        return {};
    }

    if (key.empty())
    {
        host_log("Error: Empty key");
        return {};
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        // Check if we're in a transaction and the key has been modified
        auto transaction_it = _transactions.find(_next_transaction_id - 1);
        if (transaction_it != _transactions.end())
        {
            auto change_it = transaction_it->second.changes.find(key);
            if (change_it != transaction_it->second.changes.end())
            {
                return change_it->second;
            }

            // Check if the key has been deleted in this transaction
            auto deletion_it = std::find(transaction_it->second.deletions.begin(),
                                        transaction_it->second.deletions.end(), key);
            if (deletion_it != transaction_it->second.deletions.end())
            {
                return {};
            }
        }

        // Load from file
        std::string file_path = get_file_path(key);
        std::vector<uint8_t> encrypted_data = load_from_file(file_path);

        // Decrypt the data
        if (!encrypted_data.empty())
        {
            return decrypt_data(encrypted_data);
        }

        return {};
    }
    catch (const std::exception& e)
    {
        std::string error_message = "Error retrieving data: ";
        error_message += e.what();
        host_log(error_message.c_str());
        return {};
    }
}

bool FileStorageProvider::remove(const std::string& key)
{
    if (!_initialized)
    {
        host_log("Error: Storage provider not initialized");
        return false;
    }

    if (key.empty())
    {
        host_log("Error: Empty key");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        // If we're in a transaction, don't delete from disk yet
        auto transaction_it = _transactions.find(_next_transaction_id - 1);
        if (transaction_it != _transactions.end())
        {
            // Remove from changes if present
            transaction_it->second.changes.erase(key);

            // Add to deletions
            transaction_it->second.deletions.push_back(key);
            return true;
        }

        // Delete file
        std::string file_path = get_file_path(key);
        return delete_file(file_path);
    }
    catch (const std::exception& e)
    {
        std::string error_message = "Error removing data: ";
        error_message += e.what();
        host_log(error_message.c_str());
        return false;
    }
}

bool FileStorageProvider::exists(const std::string& key)
{
    if (!_initialized)
    {
        host_log("Error: Storage provider not initialized");
        return false;
    }

    if (key.empty())
    {
        host_log("Error: Empty key");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        // Check if we're in a transaction and the key has been deleted
        auto transaction_it = _transactions.find(_next_transaction_id - 1);
        if (transaction_it != _transactions.end())
        {
            // Check if the key has been deleted in this transaction
            auto deletion_it = std::find(transaction_it->second.deletions.begin(),
                                        transaction_it->second.deletions.end(), key);
            if (deletion_it != transaction_it->second.deletions.end())
            {
                return false;
            }

            // Check if the key has been added in this transaction
            auto change_it = transaction_it->second.changes.find(key);
            if (change_it != transaction_it->second.changes.end())
            {
                return true;
            }
        }

        // Check file
        std::string file_path = get_file_path(key);
        std::vector<uint8_t> data = load_from_file(file_path);
        return !data.empty();
    }
    catch (const std::exception& e)
    {
        std::string error_message = "Error checking if key exists: ";
        error_message += e.what();
        host_log(error_message.c_str());
        return false;
    }
}

std::vector<std::string> FileStorageProvider::list_keys()
{
    if (!_initialized)
    {
        host_log("Error: Storage provider not initialized");
        return {};
    }

    std::lock_guard<std::mutex> lock(_mutex);

    try
    {
        // Get all files in the storage directory
        std::vector<std::string> files = list_files_in_directory(_storage_path);
        std::vector<std::string> keys;

        // Convert file paths to keys
        for (const auto& file : files)
        {
            // Skip metadata directory and any hidden files
            if (file == ".metadata" || file.empty() || file[0] == '.')
            {
                continue;
            }

            keys.push_back(file);
        }

        // If we're in a transaction, add keys from the transaction
        auto transaction_it = _transactions.find(_next_transaction_id - 1);
        if (transaction_it != _transactions.end())
        {
            // Add keys from changes
            for (const auto& pair : transaction_it->second.changes)
            {
                keys.push_back(pair.first);
            }

            // Remove keys that have been deleted
            for (const auto& key : transaction_it->second.deletions)
            {
                auto it = std::find(keys.begin(), keys.end(), key);
                if (it != keys.end())
                {
                    keys.erase(it);
                }
            }
        }

        // Remove duplicates
        std::sort(keys.begin(), keys.end());
        keys.erase(std::unique(keys.begin(), keys.end()), keys.end());

        return keys;
    }
    catch (const std::exception& e)
    {
        std::string error_message = "Error listing keys: ";
        error_message += e.what();
        host_log(error_message.c_str());
        return {};
    }

uint64_t FileStorageProvider::begin_transaction()
{
    if (!_initialized)
    {
        host_log("Error: Storage provider not initialized");
        return 0;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    // Check if there's already an active transaction
    for (const auto& pair : _transactions)
    {
        if (!pair.second.changes.empty() || !pair.second.deletions.empty())
        {
            // There's an active transaction
            host_log("Error: There's already an active transaction");
            return 0;
        }
    }

    uint64_t transaction_id = _next_transaction_id++;
    _transactions[transaction_id] = Transaction();

    return transaction_id;
}

bool FileStorageProvider::commit_transaction(uint64_t transaction_id)
{
    if (!_initialized)
    {
        host_log("Error: Storage provider not initialized");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    auto it = _transactions.find(transaction_id);
    if (it == _transactions.end())
    {
        host_log("Error: Transaction not found");
        return false;
    }

    try
    {
        // Apply changes
        for (const auto& pair : it->second.changes)
        {
            // Encrypt the data
            std::vector<uint8_t> encrypted_data = encrypt_data(pair.second);

            // Save to file
            std::string file_path = get_file_path(pair.first);
            if (!save_to_file(file_path, encrypted_data))
            {
                // Failed to save to file, rollback
                host_log(("Error: Failed to save file: " + file_path).c_str());
                _transactions.erase(it);
                return false;
            }
        }

        // Apply deletions
        for (const auto& key : it->second.deletions)
        {
            // Delete file
            std::string file_path = get_file_path(key);
            if (!delete_file(file_path))
            {
                // Failed to delete file, but continue anyway
                host_log(("Warning: Failed to delete file: " + file_path).c_str());
            }
        }

        // Remove transaction
        _transactions.erase(it);

        return true;
    }
    catch (const std::exception& e)
    {
        std::string error_message = "Error committing transaction: ";
        error_message += e.what();
        host_log(error_message.c_str());
        return false;
    }
}

bool FileStorageProvider::rollback_transaction(uint64_t transaction_id)
{
    if (!_initialized)
    {
        host_log("Error: Storage provider not initialized");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    auto it = _transactions.find(transaction_id);
    if (it == _transactions.end())
    {
        host_log("Error: Transaction not found");
        return false;
    }

    // Remove transaction
    _transactions.erase(it);

    return true;
}

bool FileStorageProvider::store_in_transaction(uint64_t transaction_id, const std::string& key, const std::vector<uint8_t>& data)
{
    if (!_initialized)
    {
        host_log("Error: Storage provider not initialized");
        return false;
    }

    if (key.empty())
    {
        host_log("Error: Empty key");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    auto it = _transactions.find(transaction_id);
    if (it == _transactions.end())
    {
        host_log("Error: Transaction not found");
        return false;
    }

    // Store in transaction
    it->second.changes[key] = data;

    // Remove from deletions if present
    auto deletion_it = std::find(it->second.deletions.begin(), it->second.deletions.end(), key);
    if (deletion_it != it->second.deletions.end())
    {
        it->second.deletions.erase(deletion_it);
    }

    return true;
}

bool FileStorageProvider::remove_in_transaction(uint64_t transaction_id, const std::string& key)
{
    if (!_initialized)
    {
        host_log("Error: Storage provider not initialized");
        return false;
    }

    if (key.empty())
    {
        host_log("Error: Empty key");
        return false;
    }

    std::lock_guard<std::mutex> lock(_mutex);

    auto it = _transactions.find(transaction_id);
    if (it == _transactions.end())
    {
        host_log("Error: Transaction not found");
        return false;
    }

    // Remove from changes if present
    it->second.changes.erase(key);

    // Add to deletions if not already present
    auto deletion_it = std::find(it->second.deletions.begin(), it->second.deletions.end(), key);
    if (deletion_it == it->second.deletions.end())
    {
        it->second.deletions.push_back(key);
    }

    return true;
}

std::string FileStorageProvider::get_file_path(const std::string& key)
{
    // Sanitize key to make it a valid filename
    std::string sanitized_key = key;
    std::replace(sanitized_key.begin(), sanitized_key.end(), '/', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '\\', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), ':', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '*', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '?', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '"', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '<', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '>', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '|', '_');

    return _storage_path + "/" + sanitized_key;
}

std::string FileStorageProvider::get_metadata_path(const std::string& key)
{
    // Sanitize key to make it a valid filename
    std::string sanitized_key = key;
    std::replace(sanitized_key.begin(), sanitized_key.end(), '/', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '\\', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), ':', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '*', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '?', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '"', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '<', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '>', '_');
    std::replace(sanitized_key.begin(), sanitized_key.end(), '|', '_');

    return _storage_path + "/.metadata/" + sanitized_key + ".metadata";
}

bool FileStorageProvider::save_to_file(const std::string& file_path, const std::vector<uint8_t>& data)
{
    // Open the file for writing
    int fd = host_file_open(file_path.c_str(), O_CREAT | O_TRUNC | O_WRONLY, S_IRUSR | S_IWUSR);
    if (fd < 0)
    {
        host_log(("Error: Failed to open file for writing: " + file_path).c_str());
        return false;
    }

    // Write the data to the file
    size_t bytes_written = host_file_write(fd, data.data(), data.size());
    host_file_close(fd);

    if (bytes_written != data.size())
    {
        host_log(("Error: Failed to write data to file: " + file_path).c_str());
        return false;
    }

    // Write the metadata
    return write_metadata(file_path, data);
}

std::vector<uint8_t> FileStorageProvider::load_from_file(const std::string& file_path)
{
    // Open the file for reading
    int fd = host_file_open(file_path.c_str(), O_RDONLY, 0);
    if (fd < 0)
    {
        return {};
    }

    // Get the file size
    int64_t file_size = host_file_seek(fd, 0, SEEK_END);
    if (file_size < 0)
    {
        host_file_close(fd);
        return {};
    }

    // Seek back to the beginning of the file
    if (host_file_seek(fd, 0, SEEK_SET) < 0)
    {
        host_file_close(fd);
        return {};
    }

    // Read the data from the file
    std::vector<uint8_t> data(file_size);
    size_t bytes_read = host_file_read(fd, data.data(), file_size);
    host_file_close(fd);

    if (bytes_read != file_size)
    {
        host_log(("Error: Failed to read data from file: " + file_path).c_str());
        return {};
    }

    return data;
}

bool FileStorageProvider::delete_file(const std::string& file_path)
{
    // Delete the file
    int result = host_file_unlink(file_path.c_str());
    if (result != 0)
    {
        host_log(("Error: Failed to delete file: " + file_path).c_str());
        return false;
    }

    // Delete the metadata file
    std::string metadata_path = get_metadata_path(file_path);
    result = host_file_unlink(metadata_path.c_str());
    if (result != 0)
    {
        host_log(("Warning: Failed to delete metadata file: " + metadata_path).c_str());
        // Continue anyway
    }

    return true;
}

bool FileStorageProvider::write_metadata(const std::string& file_path, const std::vector<uint8_t>& data)
{
    // Create the metadata
    json metadata = {
        {"path", file_path},
        {"size", data.size()},
        {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch()).count()},
        {"hash", compute_hash(data)}
    };

    // Serialize the metadata to a string
    std::string metadata_str = metadata.dump();

    // Write the metadata to a file
    std::string metadata_path = get_metadata_path(file_path);
    int fd = host_file_open(metadata_path.c_str(), O_CREAT | O_TRUNC | O_WRONLY, S_IRUSR | S_IWUSR);
    if (fd < 0)
    {
        host_log(("Error: Failed to open metadata file for writing: " + metadata_path).c_str());
        return false;
    }

    // Write the metadata to the file
    size_t bytes_written = host_file_write(fd, metadata_str.data(), metadata_str.size());
    host_file_close(fd);

    if (bytes_written != metadata_str.size())
    {
        host_log(("Error: Failed to write metadata to file: " + metadata_path).c_str());
        return false;
    }

    return true;
}

std::string FileStorageProvider::compute_hash(const std::vector<uint8_t>& data)
{
    // Compute SHA-256 hash of the data
    sgx_sha256_hash_t hash;
    sgx_status_t status = sgx_sha256_msg(data.data(), data.size(), &hash);
    if (status != SGX_SUCCESS)
    {
        host_log("Error: Failed to compute hash");
        return "";
    }

    // Convert the hash to a hex string
    std::string hash_str;
    for (size_t i = 0; i < sizeof(hash); i++)
    {
        char hex[3];
        snprintf(hex, sizeof(hex), "%02x", hash[i]);
        hash_str += hex;
    }

    return hash_str;
}

std::vector<uint8_t> FileStorageProvider::encrypt_data(const std::vector<uint8_t>& data)
{
    // Get the encryption key derived from the enclave's sealing key
    std::vector<uint8_t> key = get_sealing_key();
    if (key.empty())
    {
        host_log("Error: Failed to get sealing key for encryption");
        return {};
    }

    // Create a buffer for the encrypted data
    // AES-GCM adds a 12-byte IV and a 16-byte tag
    std::vector<uint8_t> encrypted(data.size() + SGX_AESGCM_IV_SIZE + SGX_AESGCM_MAC_SIZE);

    // Generate a random IV
    sgx_status_t rand_status = sgx_read_rand(encrypted.data(), SGX_AESGCM_IV_SIZE);
    if (rand_status != SGX_SUCCESS)
    {
        host_log("Error: Failed to generate random IV for encryption");
        return {};
    }

    // Encrypt the data
    sgx_status_t status = sgx_rijndael128GCM_encrypt(
        reinterpret_cast<const sgx_aes_gcm_128bit_key_t*>(key.data()),
        reinterpret_cast<const uint8_t*>(data.data()),
        data.size(),
        encrypted.data() + SGX_AESGCM_IV_SIZE + SGX_AESGCM_MAC_SIZE,
        encrypted.data(), // IV
        SGX_AESGCM_IV_SIZE,
        nullptr, 0, // AAD
        reinterpret_cast<sgx_aes_gcm_128bit_tag_t*>(encrypted.data() + SGX_AESGCM_IV_SIZE) // MAC
    );

    // Clear the key from memory for security
    std::fill(key.begin(), key.end(), 0);

    if (status != SGX_SUCCESS)
    {
        std::string error_msg = "Error: Encryption failed with status code: " + std::to_string(status);
        host_log(error_msg.c_str());
        return {};
    }

    return encrypted;
}

std::vector<uint8_t> FileStorageProvider::decrypt_data(const std::vector<uint8_t>& encrypted_data)
{
    // Check if the encrypted data is large enough to contain the IV and MAC
    if (encrypted_data.size() < SGX_AESGCM_IV_SIZE + SGX_AESGCM_MAC_SIZE)
    {
        host_log("Error: Invalid encrypted data - data too small");
        return {};
    }

    // Get the encryption key derived from the enclave's sealing key
    std::vector<uint8_t> key = get_sealing_key();
    if (key.empty())
    {
        host_log("Error: Failed to get sealing key for decryption");
        return {};
    }

    // Calculate the size of the decrypted data
    size_t decrypted_size = encrypted_data.size() - SGX_AESGCM_IV_SIZE - SGX_AESGCM_MAC_SIZE;

    // Create a buffer for the decrypted data
    std::vector<uint8_t> decrypted(decrypted_size);

    // Decrypt the data
    sgx_status_t status = sgx_rijndael128GCM_decrypt(
        reinterpret_cast<const sgx_aes_gcm_128bit_key_t*>(key.data()),
        reinterpret_cast<const uint8_t*>(encrypted_data.data() + SGX_AESGCM_IV_SIZE + SGX_AESGCM_MAC_SIZE),
        decrypted_size,
        decrypted.data(),
        reinterpret_cast<const uint8_t*>(encrypted_data.data()), // IV
        SGX_AESGCM_IV_SIZE,
        nullptr, 0, // AAD
        reinterpret_cast<const sgx_aes_gcm_128bit_tag_t*>(encrypted_data.data() + SGX_AESGCM_IV_SIZE) // MAC
    );

    // Clear the key from memory for security
    std::fill(key.begin(), key.end(), 0);

    if (status != SGX_SUCCESS)
    {
        std::string error_msg = "Error: Decryption failed with status code: " + std::to_string(status);
        host_log(error_msg.c_str());
        return {};
    }

    return decrypted;
}

std::vector<std::string> FileStorageProvider::list_files_in_directory(const std::string& directory_path)
{
    std::vector<std::string> files;

    // Call the host to list files in the directory
    int count = 0;
    char** file_list = host_directory_list(directory_path.c_str(), &count);
    if (file_list == nullptr)
    {
        host_log(("Error: Failed to list files in directory: " + directory_path).c_str());
        return files;
    }

    // Add files to the vector
    for (int i = 0; i < count; i++)
    {
        files.push_back(file_list[i]);
    }

    // Free the file list
    host_directory_list_free(file_list, count);

    return files;
}

std::vector<uint8_t> FileStorageProvider::get_sealing_key()
{
    // Define key size (16 bytes for AES-128-GCM)
    const size_t key_size = 16;
    std::vector<uint8_t> key(key_size);

    // Get the enclave's sealing key
    sgx_key_request_t key_request;
    memset(&key_request, 0, sizeof(key_request));

    // Set up the key request
    key_request.key_name = SGX_KEYSELECT_SEAL;  // Use the sealing key
    key_request.key_policy = SGX_KEYPOLICY_MRSIGNER;  // Bind to the signer of the enclave

    // Get a random value for key derivation
    sgx_status_t rand_status = sgx_read_rand(key_request.key_id, sizeof(key_request.key_id));
    if (rand_status != SGX_SUCCESS)
    {
        host_log(("Error: Failed to generate random key ID: " + std::to_string(rand_status)).c_str());
        return {};
    }

    // Get the CPU's CPUSVN
    sgx_status_t cpusvn_status = sgx_get_key(&key_request, reinterpret_cast<sgx_key_128bit_t*>(key.data()));
    if (cpusvn_status != SGX_SUCCESS)
    {
        host_log(("Error: Failed to get sealing key: " + std::to_string(cpusvn_status)).c_str());
        return {};
    }

    return key;
}
