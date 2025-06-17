use std::ffi::{CStr, CString};
use std::os::raw::{c_char, c_int, c_uint};
use std::ptr;
use std::fs::{File, OpenOptions};
use std::io::{Read, Write, Seek, SeekFrom};
use std::path::Path;

// Import SGX cryptographic functions
extern "C" {
    fn sgx_read_rand(rand: *mut u8, length: usize) -> c_uint;
    fn sgx_aes_gcm_encrypt(
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
    fn sgx_aes_gcm_decrypt(
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
        let storage_dir = "/tmp/secure_storage";
        if let Err(_) = std::fs::create_dir_all(storage_dir) {
            return STORAGE_ERROR_ACCESS_DENIED;
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
        
        // Generate file path
        let storage_dir = "/tmp/secure_storage";
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
        
        let storage_dir = "/tmp/secure_storage";
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
        
        // Derive encryption key from provided key
        let mut enc_key = [0u8; 32]; // AES-256 key
        for i in 0..32 {
            enc_key[i] = key[i % key.len()].wrapping_add(i as u8);
        }
        
        // Prepare output buffer
        let mut encrypted = vec![0u8; data.len()];
        let mut tag = [0u8; 16]; // GCM tag size
        
        // Encrypt using SGX AES-GCM
        let result = sgx_aes_gcm_encrypt(
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
        let result = sgx_aes_gcm_decrypt(
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