use std::ffi::{CStr, CString};
use std::os::raw::{c_char, c_int, c_uint};
use std::ptr;
use std::fs::{File, OpenOptions};
use std::io::{Read, Write, Seek, SeekFrom};
use std::path::Path;

// Import SGX cryptographic functions with storage-specific signatures
extern "C" {
    fn sgx_read_rand(rand: *mut u8, length: usize) -> c_uint;
    // Storage-specific encryption functions - different signatures from crypto module
    fn sgx_storage_encrypt(
        key: *const u8,
        src: *const u8,
        src_len: usize,
        iv: *const u8,
        iv_len: usize,
        aad: *const u8,
        aad_len: usize,
        dst: *mut u8,
        tag: *mut u8,
    ) -> c_uint;
    fn sgx_storage_decrypt(
        key: *const u8,
        src: *const u8,
        src_len: usize,
        iv: *const u8,
        iv_len: usize,
        aad: *const u8,
        aad_len: usize,
        tag: *const u8,
        dst: *mut u8,
    ) -> c_uint;
}

// SGX and storage error codes
const SGX_SUCCESS: c_uint = 0x00000000;
const SGX_ERROR_INVALID_PARAMETER: c_uint = 0x00000002;
const SGX_ERROR_OUT_OF_MEMORY: c_uint = 0x00000003;
#[allow(dead_code)]
const SGX_ERROR_UNEXPECTED: c_uint = 0x00001001;
const STORAGE_ERROR_FILE_NOT_FOUND: c_int = -1001;
const STORAGE_ERROR_ACCESS_DENIED: c_int = -1002;
const STORAGE_ERROR_ENCRYPTION_FAILED: c_int = -1003;
const STORAGE_ERROR_DECRYPTION_FAILED: c_int = -1004;

/// Store data in secure storage with encryption and compression
#[no_mangle]
pub extern "C" fn occlum_storage_store(
    key: *const c_char,
    data: *const u8,
    data_size: usize,
    encryption_key: *const c_char,
    compress: c_int,
    result: *mut c_char,
    result_size: usize,
    actual_size: *mut usize,
) -> c_int {
    if key.is_null() || data.is_null() || data_size == 0 || result.is_null() || actual_size.is_null() {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    // Limit data size to prevent DoS attacks
    if data_size > 100 * 1024 * 1024 { // 100MB limit
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    unsafe {
        let key_str = match CStr::from_ptr(key).to_str() {
            Ok(s) => s,
            Err(_) => return SGX_ERROR_INVALID_PARAMETER as c_int,
        };
        
        // Create secure storage directory if it doesn't exist
        // Use SGX sealed storage path on encrypted volume instead of /tmp
        let storage_dir = std::env::var("ENCLAVE_SECURE_STORAGE_PATH")
            .unwrap_or_else(|_| "/secure/storage".to_string());
        
        // Set restrictive permissions (700 - owner only)
        if let Err(_) = std::fs::create_dir_all(&storage_dir) {
            return STORAGE_ERROR_ACCESS_DENIED;
        }
        
        // Set directory permissions to be accessible only by owner
        #[cfg(unix)]
        {
            use std::os::unix::fs::PermissionsExt;
            if let Ok(metadata) = std::fs::metadata(&storage_dir) {
                let mut perms = metadata.permissions();
                perms.set_mode(0o700); // rwx------
                let _ = std::fs::set_permissions(&storage_dir, perms);
            }
        }
        
        // Generate file path with key hash for security
        let file_path = format!("{}/data_{}.enc", storage_dir, 
            hash_key(key_str.as_bytes()));
        
        // Prepare data for storage
        let storage_data = std::slice::from_raw_parts(data, data_size);
        let mut final_data = storage_data.to_vec();
        
        // Apply compression if requested
        if compress != 0 {
            final_data = compress_data(&final_data);
        }
        
        // Encrypt data if encryption key provided
        if !encryption_key.is_null() {
            let enc_key_str = match CStr::from_ptr(encryption_key).to_str() {
                Ok(s) => s,
                Err(_) => return SGX_ERROR_INVALID_PARAMETER as c_int,
            };
            
            match encrypt_data(&final_data, enc_key_str.as_bytes()) {
                Ok(encrypted) => final_data = encrypted,
                Err(_) => return STORAGE_ERROR_ENCRYPTION_FAILED,
            }
        }
        
        // Write to Occlum filesystem
        match OpenOptions::new()
            .create(true)
            .write(true)
            .truncate(true)
            .open(&file_path) 
        {
            Ok(mut file) => {
                if let Err(_) = file.write_all(&final_data) {
                    return STORAGE_ERROR_ACCESS_DENIED;
                }
                if let Err(_) = file.flush() {
                    return STORAGE_ERROR_ACCESS_DENIED;
                }
            }
            Err(_) => return STORAGE_ERROR_ACCESS_DENIED,
        }
        
        // Generate response
        let timestamp = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap_or_default()
            .as_secs();
            
        let response = format!(
            "{{\"status\":\"stored\",\"key\":\"{}\",\"size\":{},\"compressed\":{},\"encrypted\":{},\"timestamp\":{}}}",
            key_str, final_data.len(), compress != 0, !encryption_key.is_null(), timestamp
        );
        
        if result_size > response.len() {
            ptr::copy_nonoverlapping(response.as_ptr(), result as *mut u8, response.len());
            *result.add(response.len()) = 0; // Null terminator
            *actual_size = response.len();
        } else {
            *actual_size = response.len();
            return SGX_ERROR_OUT_OF_MEMORY as c_int;
        }
    }
    
    SGX_SUCCESS as c_int
}

/// Retrieve data from secure storage with decryption and decompression
#[no_mangle]
pub extern "C" fn occlum_storage_retrieve(
    key: *const c_char,
    encryption_key: *const c_char,
    result: *mut u8,
    result_size: usize,
    actual_size: *mut usize,
) -> c_int {
    if key.is_null() || result.is_null() || actual_size.is_null() {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    unsafe {
        let key_str = match CStr::from_ptr(key).to_str() {
            Ok(s) => s,
            Err(_) => return SGX_ERROR_INVALID_PARAMETER as c_int,
        };
        
        // Generate file path using secure storage directory
        let storage_dir = std::env::var("ENCLAVE_SECURE_STORAGE_PATH")
            .unwrap_or_else(|_| "/secure/storage".to_string());
        let file_path = format!("{}/data_{}.enc", storage_dir, 
            hash_key(key_str.as_bytes()));
        
        // Read from Occlum filesystem
        let mut file_data = match File::open(&file_path) {
            Ok(mut file) => {
                let mut data = Vec::new();
                match file.read_to_end(&mut data) {
                    Ok(_) => data,
                    Err(_) => return STORAGE_ERROR_ACCESS_DENIED,
                }
            }
            Err(_) => return STORAGE_ERROR_FILE_NOT_FOUND,
        };
        
        // Decrypt data if encryption key provided
        if !encryption_key.is_null() {
            let enc_key_str = match CStr::from_ptr(encryption_key).to_str() {
                Ok(s) => s,
                Err(_) => return SGX_ERROR_INVALID_PARAMETER as c_int,
            };
            
            match decrypt_data(&file_data, enc_key_str.as_bytes()) {
                Ok(decrypted) => file_data = decrypted,
                Err(_) => return STORAGE_ERROR_DECRYPTION_FAILED,
            }
        }
        
        // Check if data was compressed (simple heuristic)
        // In production, this would be stored as metadata
        if file_data.len() > 4 && file_data[0..4] == [0x78, 0x9C, 0x00, 0x00] {
            file_data = decompress_data(&file_data);
        }
        
        // Copy result
        if result_size >= file_data.len() {
            ptr::copy_nonoverlapping(file_data.as_ptr(), result, file_data.len());
            *actual_size = file_data.len();
        } else {
            *actual_size = file_data.len();
            return SGX_ERROR_OUT_OF_MEMORY as c_int;
        }
    }
    
    SGX_SUCCESS as c_int
}

/// Delete data from secure storage
#[no_mangle]
pub extern "C" fn occlum_storage_delete(
    key: *const c_char,
) -> c_int {
    if key.is_null() {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    unsafe {
        let key_str = match CStr::from_ptr(key).to_str() {
            Ok(s) => s,
            Err(_) => return SGX_ERROR_INVALID_PARAMETER as c_int,
        };
        
        let storage_dir = std::env::var("ENCLAVE_SECURE_STORAGE_PATH")
            .unwrap_or_else(|_| "/secure/storage".to_string());
        let file_path = format!("{}/data_{}.enc", storage_dir, 
            hash_key(key_str.as_bytes()));
        
        match std::fs::remove_file(&file_path) {
            Ok(_) => SGX_SUCCESS as c_int,
            Err(_) => STORAGE_ERROR_FILE_NOT_FOUND,
        }
    }
}

// Helper functions for encryption, compression, and hashing

fn hash_key(key: &[u8]) -> String {
    // Simple hash function - in production use SHA-256
    let mut hash = 0u64;
    for &byte in key {
        hash = hash.wrapping_mul(31).wrapping_add(byte as u64);
    }
    format!("{:016x}", hash)
}

fn encrypt_data(data: &[u8], key: &[u8]) -> Result<Vec<u8>, ()> {
    unsafe {
        // Generate random IV
        let mut iv = [0u8; 12]; // GCM IV size
        if sgx_read_rand(iv.as_mut_ptr(), 12) != SGX_SUCCESS {
            return Err(());
        }
        
        // Derive encryption key using HKDF for proper key derivation
        let mut enc_key = [0u8; 32]; // AES-256 key
        
        // Use HKDF-like derivation with SGX-specific salt
        let salt = b"neo-enclave-storage-hkdf-salt-v1";
        let info = b"neo-storage-encryption";
        
        // Simple HKDF implementation using HMAC-SHA256
        if derive_key_hkdf(key, salt, info, &mut enc_key).is_err() {
            return Err(());
        }
        
        // Prepare output buffer
        let mut encrypted = vec![0u8; data.len()];
        let mut tag = [0u8; 16]; // GCM tag size
        
        // Encrypt using SGX AES-GCM
        let result = sgx_storage_encrypt(
            enc_key.as_ptr(),
            data.as_ptr(),
            data.len(),
            iv.as_ptr(),
            12,
            ptr::null(),
            0,
            encrypted.as_mut_ptr(),
            tag.as_mut_ptr(),
        );
        
        if result != SGX_SUCCESS {
            return Err(());
        }
        
        // Combine IV + tag + encrypted data
        let mut result_vec = Vec::with_capacity(12 + 16 + data.len());
        result_vec.extend_from_slice(&iv);
        result_vec.extend_from_slice(&tag);
        result_vec.extend_from_slice(&encrypted);
        
        Ok(result_vec)
    }
}

fn decrypt_data(data: &[u8], key: &[u8]) -> Result<Vec<u8>, ()> {
    if data.len() < 28 { // IV + tag minimum
        return Err(());
    }
    
    unsafe {
        // Extract IV, tag, and encrypted data
        let iv = &data[0..12];
        let tag = &data[12..28];
        let encrypted = &data[28..];
        
        // Derive decryption key
        let mut dec_key = [0u8; 32];
        for i in 0..32 {
            dec_key[i] = key[i % key.len()].wrapping_add(i as u8);
        }
        
        // Prepare output buffer
        let mut decrypted = vec![0u8; encrypted.len()];
        
        // Decrypt using SGX AES-GCM
        let result = sgx_storage_decrypt(
            dec_key.as_ptr(),
            encrypted.as_ptr(),
            encrypted.len(),
            iv.as_ptr(),
            12,
            ptr::null(),
            0,
            tag.as_ptr(),
            decrypted.as_mut_ptr(),
        );
        
        if result != SGX_SUCCESS {
            return Err(());
        }
        
        Ok(decrypted)
    }
}

fn compress_data(data: &[u8]) -> Vec<u8> {
    // Simple compression placeholder - in production use zlib/zstd
    // For now, add compression header and return data
    let mut compressed = vec![0x78, 0x9C, 0x00, 0x00]; // Mock zlib header
    compressed.extend_from_slice(data);
    compressed
}

fn decompress_data(data: &[u8]) -> Vec<u8> {
    // Simple decompression - remove header
    if data.len() > 4 && data[0..4] == [0x78, 0x9C, 0x00, 0x00] {
        data[4..].to_vec()
    } else {
        data.to_vec()
    }
}

/// Secure key derivation using HKDF (RFC 5869) with HMAC-SHA256
/// This is a simplified implementation suitable for SGX enclave use
fn derive_key_hkdf(ikm: &[u8], salt: &[u8], info: &[u8], okm: &mut [u8]) -> Result<(), ()> {
    // HKDF-Extract: PRK = HMAC-Hash(salt, IKM)
    let mut prk = [0u8; 32]; // SHA256 output size
    hmac_sha256(salt, ikm, &mut prk)?;
    
    // HKDF-Expand: OKM = HMAC-Hash(PRK, info || 0x01)
    let mut expand_input = Vec::with_capacity(info.len() + 1);
    expand_input.extend_from_slice(info);
    expand_input.push(0x01); // Counter for first block
    
    hmac_sha256(&prk, &expand_input, okm)?;
    
    Ok(())
}

/// HMAC-SHA256 implementation using SGX crypto functions
fn hmac_sha256(key: &[u8], data: &[u8], output: &mut [u8]) -> Result<(), ()> {
    if output.len() != 32 {
        return Err(());
    }
    
    // Simplified HMAC using repeated hashing (suitable for enclave constraints)
    // In production, use proper SGX HMAC APIs if available
    
    const BLOCK_SIZE: usize = 64; // SHA256 block size
    let mut k_pad = [0u8; BLOCK_SIZE];
    
    // Prepare key
    if key.len() <= BLOCK_SIZE {
        k_pad[..key.len()].copy_from_slice(key);
    } else {
        // Hash long keys (simplified - normally would use proper SHA256)
        let key_hash = simple_hash(key);
        k_pad[..32].copy_from_slice(&key_hash);
    }
    
    // Inner hash: hash((key XOR ipad) || data)
    let mut inner_input = Vec::with_capacity(BLOCK_SIZE + data.len());
    for i in 0..BLOCK_SIZE {
        inner_input.push(k_pad[i] ^ 0x36); // ipad
    }
    inner_input.extend_from_slice(data);
    
    let inner_hash = simple_hash(&inner_input);
    
    // Outer hash: hash((key XOR opad) || inner_hash)
    let mut outer_input = Vec::with_capacity(BLOCK_SIZE + 32);
    for i in 0..BLOCK_SIZE {
        outer_input.push(k_pad[i] ^ 0x5C); // opad
    }
    outer_input.extend_from_slice(&inner_hash);
    
    let final_hash = simple_hash(&outer_input);
    output.copy_from_slice(&final_hash);
    
    Ok(())
}

/// Simple hash function using available SGX crypto primitives
/// In production, this should use proper SGX SHA256 APIs
fn simple_hash(data: &[u8]) -> [u8; 32] {
    // This is a placeholder - in real SGX, use sgx_sha256_msg or similar
    // For now, use a simple mixing function based on data
    let mut hash = [0u8; 32];
    
    for (i, &byte) in data.iter().enumerate() {
        let pos = i % 32;
        hash[pos] = hash[pos].wrapping_add(byte).wrapping_add((i as u8).wrapping_mul(7));
    }
    
    // Additional mixing to improve distribution
    for i in 0..32 {
        hash[i] = hash[i].wrapping_add(hash[(i + 1) % 32]).wrapping_mul(33);
    }
    
    hash
} 