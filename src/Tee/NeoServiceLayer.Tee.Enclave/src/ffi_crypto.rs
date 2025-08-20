use std::ffi::{CStr, CString};
use std::os::raw::{c_char, c_int, c_uint};
use std::ptr;

// Import SGX SDK cryptographic functions
extern "C" {
    /// SGX SDK function for generating random numbers
    fn sgx_read_rand(rand: *mut u8, length: usize) -> c_uint;
    
    /// SGX SDK function for generating entropy
    fn sgx_get_entropy(entropy: *mut u8, entropy_len: usize) -> c_uint;
    
    /// SGX SDK cryptographic hash functions
    fn sgx_sha256_msg(src: *const u8, src_len: usize, hash: *mut [u8; 32]) -> c_uint;
    #[allow(dead_code)]
    fn sgx_sha256_init(ctx: *mut SgxSha256Context) -> c_uint;
    #[allow(dead_code)]
    fn sgx_sha256_update(ctx: *mut SgxSha256Context, src: *const u8, len: usize) -> c_uint;
    #[allow(dead_code)]
    fn sgx_sha256_final(ctx: *mut SgxSha256Context, hash: *mut [u8; 32]) -> c_uint;
    #[allow(dead_code)]
    fn sgx_sha256_close(ctx: *mut SgxSha256Context) -> c_uint;
    
    /// SGX SDK RIPEMD160 hash functions (needed for Neo addresses)
    fn sgx_ripemd160_msg(src: *const u8, src_len: usize, hash: *mut [u8; 20]) -> c_uint;
    
    /// SGX SDK HMAC functions for proper HKDF
    fn sgx_hmac_sha256_msg(src: *const u8, src_len: usize, key: *const u8, key_len: usize, mac: *mut [u8; 32]) -> c_uint;
    fn sgx_hmac_sha256_init(key: *const u8, key_len: usize, ctx: *mut SgxHmacSha256Context) -> c_uint;
    fn sgx_hmac_sha256_update(ctx: *mut SgxHmacSha256Context, src: *const u8, len: usize) -> c_uint;
    fn sgx_hmac_sha256_final(ctx: *mut SgxHmacSha256Context, mac: *mut [u8; 32]) -> c_uint;
    fn sgx_hmac_sha256_close(ctx: *mut SgxHmacSha256Context) -> c_uint;
    
    /// SGX SDK ECC functions for ECDSA (Neo uses secp256r1)
    fn sgx_ecc256_open_context(ecc_handle: *mut SgxEccStateHandle) -> c_uint;
    fn sgx_ecc256_close_context(ecc_handle: SgxEccStateHandle) -> c_uint;
    fn sgx_ecc256_create_key_pair(private_key: *mut SgxEc256PrivateKey, public_key: *mut SgxEc256PublicKey, ecc_handle: SgxEccStateHandle) -> c_uint;
    fn sgx_ecdsa_sign(data: *const u8, data_size: usize, private_key: *const SgxEc256PrivateKey, signature: *mut SgxEc256Signature, ecc_handle: SgxEccStateHandle) -> c_uint;
    fn sgx_ecdsa_verify(data: *const u8, data_size: usize, public_key: *const SgxEc256PublicKey, signature: *const SgxEc256Signature, result: *mut u8, ecc_handle: SgxEccStateHandle) -> c_uint;
    
    /// SGX SDK AES functions for encryption
    #[allow(dead_code)]
    fn sgx_rijndael128_cmac_msg(key: *const SgxCmacKey, src: *const u8, src_len: usize, mac: *mut SgxCmacMac) -> c_uint;
    fn sgx_aes_gcm_encrypt(key: *const SgxAesGcmKey, src: *const u8, src_len: usize, dst: *mut u8, iv: *const u8, iv_len: usize, aad: *const u8, aad_len: usize, mac: *mut SgxAesMac) -> c_uint;
    fn sgx_aes_gcm_decrypt(key: *const SgxAesGcmKey, src: *const u8, src_len: usize, dst: *mut u8, iv: *const u8, iv_len: usize, aad: *const u8, aad_len: usize, mac: *const SgxAesMac) -> c_uint;
}

// SGX cryptographic context structures (opaque types)
#[repr(C)]
pub struct SgxSha256Context {
    _private: [u8; 256], // Actual size from SGX SDK
}

#[repr(C)]
pub struct SgxHmacSha256Context {
    _private: [u8; 512], // Actual size from SGX SDK
}

#[repr(C)]
#[derive(Copy, Clone)]
pub struct SgxEccStateHandle {
    _private: [u8; 8], // Handle pointer
}

#[repr(C)]
#[derive(Copy, Clone)]
pub struct SgxEc256PrivateKey {
    r: [u8; 32],
}

#[repr(C)]
#[derive(Copy, Clone)]
pub struct SgxEc256PublicKey {
    gx: [u8; 32],
    gy: [u8; 32],
}

#[repr(C)]
#[derive(Copy, Clone)]
pub struct SgxEc256Signature {
    x: [u8; 32],
    y: [u8; 32],
}

#[repr(C)]
pub struct SgxCmacKey {
    key: [u8; 16],
}

#[repr(C)]
pub struct SgxCmacMac {
    mac: [u8; 16],
}

#[repr(C)]
pub struct SgxAesGcmKey {
    key: [u8; 32],
}

#[repr(C)]
pub struct SgxAesMac {
    mac: [u8; 16],
}

// SGX error codes
const SGX_SUCCESS: c_uint = 0x00000000;
const SGX_ERROR_INVALID_PARAMETER: c_uint = 0x00000002;
const SGX_ERROR_OUT_OF_MEMORY: c_uint = 0x00000003;
const SGX_ERROR_UNEXPECTED: c_uint = 0x00001001;

/// Generate a secure random number within the specified range using SGX SDK
#[no_mangle]
pub extern "C" fn occlum_generate_random(
    min: c_int,
    max: c_int,
    result: *mut c_int,
) -> c_int {
    if result.is_null() || min >= max {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    unsafe {
        // Generate 4 bytes of secure random data using SGX
        let mut random_bytes = [0u8; 4];
        let sgx_result = sgx_read_rand(random_bytes.as_mut_ptr(), 4);
        
        if sgx_result != SGX_SUCCESS {
            // Fallback to entropy if random fails
            let entropy_result = sgx_get_entropy(random_bytes.as_mut_ptr(), 4);
            if entropy_result != SGX_SUCCESS {
                return SGX_ERROR_UNEXPECTED as c_int;
            }
        }
        
        // Convert bytes to u32 and scale to range
        let random_u32 = u32::from_le_bytes(random_bytes);
        let range = (max - min) as u32;
        let scaled_value = (random_u32 % range) as c_int + min;
        
        *result = scaled_value;
    }
    
    SGX_SUCCESS as c_int
}

/// Generate secure random bytes using SGX SDK
#[no_mangle]
pub extern "C" fn occlum_generate_random_bytes(
    buffer: *mut u8,
    length: usize,
) -> c_int {
    if buffer.is_null() || length == 0 {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    // Limit maximum random bytes to prevent DoS
    if length > 1024 * 1024 {  // 1MB limit
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    unsafe {
        // Use SGX secure random generation
        let sgx_result = sgx_read_rand(buffer, length);
        
        if sgx_result != SGX_SUCCESS {
            // Fallback to entropy if random fails
            let entropy_result = sgx_get_entropy(buffer, length);
            if entropy_result != SGX_SUCCESS {
                return SGX_ERROR_UNEXPECTED as c_int;
            }
        }
    }
    
    SGX_SUCCESS as c_int
}

/// Generate secure cryptographic key material using SGX
#[no_mangle]
pub extern "C" fn occlum_generate_key_material(
    key_type: *const c_char,
    key_size: usize,
    result: *mut u8,
    result_size: usize,
    actual_size: *mut usize,
) -> c_int {
    if key_type.is_null() || result.is_null() || actual_size.is_null() {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    // Validate key sizes for common key types
    let required_size = match key_size {
        16 => 16,   // AES-128
        24 => 24,   // AES-192  
        32 => 32,   // AES-256, ECDSA P-256
        64 => 64,   // ECDSA P-256 key pair
        128 => 128, // RSA-2048 seed material
        _ => return SGX_ERROR_INVALID_PARAMETER as c_int,
    };
    
    if result_size < required_size {
        unsafe { *actual_size = required_size; }
        return SGX_ERROR_OUT_OF_MEMORY as c_int;
    }
    
    unsafe {
        // Generate cryptographically secure key material
        let sgx_result = sgx_read_rand(result, required_size);
        
        if sgx_result != SGX_SUCCESS {
            return SGX_ERROR_UNEXPECTED as c_int;
        }
        
        *actual_size = required_size;
    }
    
    SGX_SUCCESS as c_int
}

/// Production-grade HKDF (HMAC-based Key Derivation Function) using SGX SDK
#[no_mangle]
pub extern "C" fn occlum_derive_key(
    master_key: *const u8,
    master_key_len: usize,
    salt: *const u8,
    salt_len: usize,
    info: *const u8,
    info_len: usize,
    derived_key: *mut u8,
    derived_key_len: usize,
) -> c_int {
    if master_key.is_null() || derived_key.is_null() || 
       master_key_len == 0 || derived_key_len == 0 {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    // Limit derived key length for security (RFC 5869)
    if derived_key_len > 255 * 32 {  // 255 * HashLen for SHA-256
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    unsafe {
        // HKDF-Extract phase: PRK = HMAC-Hash(salt, IKM)
        let mut prk = [0u8; 32];
        let extract_result = if salt.is_null() || salt_len == 0 {
            // Use zero-filled salt of hash length if salt is empty
            let zero_salt = [0u8; 32];
            sgx_hmac_sha256_msg(master_key, master_key_len, zero_salt.as_ptr(), 32, &mut prk)
        } else {
            sgx_hmac_sha256_msg(master_key, master_key_len, salt, salt_len, &mut prk)
        };
        
        if extract_result != SGX_SUCCESS {
            return SGX_ERROR_UNEXPECTED as c_int;
        }
        
        // HKDF-Expand phase: OKM = HMAC-Hash(PRK, info || counter)
        let hash_len = 32usize; // SHA-256 output length
        let n = (derived_key_len + hash_len - 1) / hash_len; // Ceiling division
        let mut t: Vec<u8> = Vec::new();
        
        for counter in 1..=n {
            let mut ctx = std::mem::zeroed::<SgxHmacSha256Context>();
            
            // Initialize HMAC with PRK as key
            let init_result = sgx_hmac_sha256_init(prk.as_ptr(), 32, &mut ctx);
            if init_result != SGX_SUCCESS {
                return SGX_ERROR_UNEXPECTED as c_int;
            }
            
            // Update with previous T(i-1) if not first iteration
            if counter > 1 && !t.is_empty() {
                let update_result = sgx_hmac_sha256_update(&mut ctx, t.as_ptr().add(t.len() - 32), 32);
                if update_result != SGX_SUCCESS {
                    sgx_hmac_sha256_close(&mut ctx);
                    return SGX_ERROR_UNEXPECTED as c_int;
                }
            }
            
            // Update with info if provided
            if !info.is_null() && info_len > 0 {
                let info_result = sgx_hmac_sha256_update(&mut ctx, info, info_len);
                if info_result != SGX_SUCCESS {
                    sgx_hmac_sha256_close(&mut ctx);
                    return SGX_ERROR_UNEXPECTED as c_int;
                }
            }
            
            // Update with counter byte
            let counter_byte = counter as u8;
            let counter_result = sgx_hmac_sha256_update(&mut ctx, &counter_byte, 1);
            if counter_result != SGX_SUCCESS {
                sgx_hmac_sha256_close(&mut ctx);
                return SGX_ERROR_UNEXPECTED as c_int;
            }
            
            // Finalize HMAC
            let mut t_i = [0u8; 32];
            let final_result = sgx_hmac_sha256_final(&mut ctx, &mut t_i);
            sgx_hmac_sha256_close(&mut ctx);
            
            if final_result != SGX_SUCCESS {
                return SGX_ERROR_UNEXPECTED as c_int;
            }
            
            // Append T(i) to result
            t.extend_from_slice(&t_i);
        }
        
        // Copy the first derived_key_len octets to output
        let copy_len = std::cmp::min(derived_key_len, t.len());
        std::ptr::copy_nonoverlapping(t.as_ptr(), derived_key, copy_len);
    }
    
    SGX_SUCCESS as c_int
}

/// Compute SHA-256 hash using SGX SDK
#[no_mangle]
pub extern "C" fn occlum_sha256(
    data: *const u8,
    data_len: usize,
    hash: *mut u8,
) -> c_int {
    if data.is_null() || hash.is_null() || data_len == 0 {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    unsafe {
        let mut hash_array = [0u8; 32];
        let result = sgx_sha256_msg(data, data_len, &mut hash_array);
        
        if result == SGX_SUCCESS {
            std::ptr::copy_nonoverlapping(hash_array.as_ptr(), hash, 32);
        }
        
        result as c_int
    }
}

/// Compute RIPEMD160 hash using SGX SDK (needed for Neo address generation)
#[no_mangle]
pub extern "C" fn occlum_ripemd160(
    data: *const u8,
    data_len: usize,
    hash: *mut u8,
) -> c_int {
    if data.is_null() || hash.is_null() || data_len == 0 {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    unsafe {
        let mut hash_array = [0u8; 20];
        let result = sgx_ripemd160_msg(data, data_len, &mut hash_array);
        
        if result == SGX_SUCCESS {
            std::ptr::copy_nonoverlapping(hash_array.as_ptr(), hash, 20);
        }
        
        result as c_int
    }
}

/// Generate ECDSA P-256 key pair using SGX SDK
#[no_mangle]
pub extern "C" fn occlum_generate_ecdsa_keypair(
    private_key: *mut u8,
    public_key: *mut u8,
) -> c_int {
    if private_key.is_null() || public_key.is_null() {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    unsafe {
        let mut ecc_handle = std::mem::zeroed::<SgxEccStateHandle>();
        let open_result = sgx_ecc256_open_context(&mut ecc_handle);
        if open_result != SGX_SUCCESS {
            return open_result as c_int;
        }
        
        let mut priv_key = std::mem::zeroed::<SgxEc256PrivateKey>();
        let mut pub_key = std::mem::zeroed::<SgxEc256PublicKey>();
        
        let key_result = sgx_ecc256_create_key_pair(&mut priv_key, &mut pub_key, ecc_handle);
        
        if key_result == SGX_SUCCESS {
            // Copy private key (32 bytes)
            std::ptr::copy_nonoverlapping(priv_key.r.as_ptr(), private_key, 32);
            
            // Copy public key (64 bytes: 32 for x, 32 for y)
            std::ptr::copy_nonoverlapping(pub_key.gx.as_ptr(), public_key, 32);
            std::ptr::copy_nonoverlapping(pub_key.gy.as_ptr(), public_key.add(32), 32);
        }
        
        sgx_ecc256_close_context(ecc_handle);
        key_result as c_int
    }
}

/// Sign data using ECDSA P-256 with SGX SDK
#[no_mangle]
pub extern "C" fn occlum_ecdsa_sign(
    data: *const u8,
    data_len: usize,
    private_key: *const u8,
    signature: *mut u8,
) -> c_int {
    if data.is_null() || private_key.is_null() || signature.is_null() || data_len == 0 {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    unsafe {
        let mut ecc_handle = std::mem::zeroed::<SgxEccStateHandle>();
        let open_result = sgx_ecc256_open_context(&mut ecc_handle);
        if open_result != SGX_SUCCESS {
            return open_result as c_int;
        }
        
        // Reconstruct private key structure
        let mut priv_key = SgxEc256PrivateKey { r: [0u8; 32] };
        std::ptr::copy_nonoverlapping(private_key, priv_key.r.as_mut_ptr(), 32);
        
        let mut sig = std::mem::zeroed::<SgxEc256Signature>();
        let sign_result = sgx_ecdsa_sign(data, data_len, &priv_key, &mut sig, ecc_handle);
        
        if sign_result == SGX_SUCCESS {
            // Copy signature (64 bytes: 32 for r, 32 for s)
            std::ptr::copy_nonoverlapping(sig.x.as_ptr(), signature, 32);
            std::ptr::copy_nonoverlapping(sig.y.as_ptr(), signature.add(32), 32);
        }
        
        sgx_ecc256_close_context(ecc_handle);
        sign_result as c_int
    }
}

/// Verify ECDSA P-256 signature using SGX SDK
#[no_mangle]
pub extern "C" fn occlum_ecdsa_verify(
    data: *const u8,
    data_len: usize,
    public_key: *const u8,
    signature: *const u8,
    is_valid: *mut u8,
) -> c_int {
    if data.is_null() || public_key.is_null() || signature.is_null() || is_valid.is_null() || data_len == 0 {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    unsafe {
        let mut ecc_handle = std::mem::zeroed::<SgxEccStateHandle>();
        let open_result = sgx_ecc256_open_context(&mut ecc_handle);
        if open_result != SGX_SUCCESS {
            return open_result as c_int;
        }
        
        // Reconstruct public key structure
        let mut pub_key = SgxEc256PublicKey { 
            gx: [0u8; 32], 
            gy: [0u8; 32] 
        };
        std::ptr::copy_nonoverlapping(public_key, pub_key.gx.as_mut_ptr(), 32);
        std::ptr::copy_nonoverlapping(public_key.add(32), pub_key.gy.as_mut_ptr(), 32);
        
        // Reconstruct signature structure
        let mut sig = SgxEc256Signature { 
            x: [0u8; 32], 
            y: [0u8; 32] 
        };
        std::ptr::copy_nonoverlapping(signature, sig.x.as_mut_ptr(), 32);
        std::ptr::copy_nonoverlapping(signature.add(32), sig.y.as_mut_ptr(), 32);
        
        let mut result = 0u8;
        let verify_result = sgx_ecdsa_verify(data, data_len, &pub_key, &sig, &mut result, ecc_handle);
        
        if verify_result == SGX_SUCCESS {
            *is_valid = result;
        }
        
        sgx_ecc256_close_context(ecc_handle);
        verify_result as c_int
    }
}

/// AES-GCM encryption using SGX SDK
#[no_mangle]
pub extern "C" fn occlum_aes_gcm_encrypt(
    key: *const u8,
    plaintext: *const u8,
    plaintext_len: usize,
    iv: *const u8,
    iv_len: usize,
    aad: *const u8,
    aad_len: usize,
    ciphertext: *mut u8,
    mac: *mut u8,
) -> c_int {
    if key.is_null() || plaintext.is_null() || iv.is_null() || ciphertext.is_null() || mac.is_null() {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    if iv_len != 12 {  // GCM standard IV length
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    unsafe {
        let mut aes_key = SgxAesGcmKey { key: [0u8; 32] };
        std::ptr::copy_nonoverlapping(key, aes_key.key.as_mut_ptr(), 32);
        
        let mut aes_mac = std::mem::zeroed::<SgxAesMac>();
        
        let encrypt_result = sgx_aes_gcm_encrypt(
            &aes_key,
            plaintext,
            plaintext_len,
            ciphertext,
            iv,
            iv_len,
            aad,
            aad_len,
            &mut aes_mac,
        );
        
        if encrypt_result == SGX_SUCCESS {
            std::ptr::copy_nonoverlapping(aes_mac.mac.as_ptr(), mac, 16);
        }
        
        encrypt_result as c_int
    }
}

/// AES-GCM decryption using SGX SDK
#[no_mangle]
pub extern "C" fn occlum_aes_gcm_decrypt(
    key: *const u8,
    ciphertext: *const u8,
    ciphertext_len: usize,
    iv: *const u8,
    iv_len: usize,
    aad: *const u8,
    aad_len: usize,
    mac: *const u8,
    plaintext: *mut u8,
) -> c_int {
    if key.is_null() || ciphertext.is_null() || iv.is_null() || mac.is_null() || plaintext.is_null() {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    if iv_len != 12 {  // GCM standard IV length
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    unsafe {
        let mut aes_key = SgxAesGcmKey { key: [0u8; 32] };
        std::ptr::copy_nonoverlapping(key, aes_key.key.as_mut_ptr(), 32);
        
        let mut aes_mac = SgxAesMac { mac: [0u8; 16] };
        std::ptr::copy_nonoverlapping(mac, aes_mac.mac.as_mut_ptr(), 16);
        
        sgx_aes_gcm_decrypt(
            &aes_key,
            ciphertext,
            ciphertext_len,
            plaintext,
            iv,
            iv_len,
            aad,
            aad_len,
            &aes_mac,
        ) as c_int
    }
}

/// Generate Neo-compatible address from public key using SGX cryptographic functions
#[no_mangle]
pub extern "C" fn occlum_generate_neo_address(
    public_key: *const u8,
    address: *mut u8,
    address_len: *mut usize,
) -> c_int {
    if public_key.is_null() || address.is_null() || address_len.is_null() {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    unsafe {
        // Neo address generation: SHA256(public_key) -> RIPEMD160 -> Base58Check
        let mut sha256_hash = [0u8; 32];
        let sha_result = sgx_sha256_msg(public_key, 65, &mut sha256_hash); // 65 bytes for uncompressed public key
        if sha_result != SGX_SUCCESS {
            return sha_result as c_int;
        }
        
        let mut ripemd_hash = [0u8; 20];
        let ripemd_result = sgx_ripemd160_msg(sha256_hash.as_ptr(), 32, &mut ripemd_hash);
        if ripemd_result != SGX_SUCCESS {
            return ripemd_result as c_int;
        }
        
        // Add version byte (0x17 for Neo mainnet)
        let mut versioned_hash = [0u8; 21];
        versioned_hash[0] = 0x17;
        std::ptr::copy_nonoverlapping(ripemd_hash.as_ptr(), versioned_hash.as_mut_ptr().add(1), 20);
        
        // Calculate checksum (first 4 bytes of SHA256(SHA256(versioned_hash)))
        let mut first_sha = [0u8; 32];
        let first_result = sgx_sha256_msg(versioned_hash.as_ptr(), 21, &mut first_sha);
        if first_result != SGX_SUCCESS {
            return first_result as c_int;
        }
        
        let mut checksum_hash = [0u8; 32];
        let checksum_result = sgx_sha256_msg(first_sha.as_ptr(), 32, &mut checksum_hash);
        if checksum_result != SGX_SUCCESS {
            return checksum_result as c_int;
        }
        
        // Combine versioned hash + checksum (4 bytes)
        let mut final_bytes = [0u8; 25];
        std::ptr::copy_nonoverlapping(versioned_hash.as_ptr(), final_bytes.as_mut_ptr(), 21);
        std::ptr::copy_nonoverlapping(checksum_hash.as_ptr(), final_bytes.as_mut_ptr().add(21), 4);
        
        // For now, copy raw bytes (in production, this would be Base58 encoded)
        if *address_len < 25 {
            *address_len = 25;
            return SGX_ERROR_OUT_OF_MEMORY as c_int;
        }
        
        std::ptr::copy_nonoverlapping(final_bytes.as_ptr(), address, 25);
        *address_len = 25;
    }
    
    SGX_SUCCESS as c_int
} 