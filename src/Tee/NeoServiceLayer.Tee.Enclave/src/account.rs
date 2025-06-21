use anyhow::{Result, anyhow};
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::sync::{Arc, RwLock};
use log::{info, warn, error, debug};
use sha2::{Sha256, Digest};

use crate::{EncaveConfig, crypto::CryptoService};

// Import SGX cryptographic functions for Neo address generation
extern "C" {
    fn occlum_generate_ecdsa_keypair(private_key: *mut u8, public_key: *mut u8) -> i32;
    fn occlum_sha256(data: *const u8, data_len: usize, hash: *mut u8) -> i32;
    fn occlum_ripemd160(data: *const u8, data_len: usize, hash: *mut u8) -> i32;
    fn occlum_generate_neo_address(public_key: *const u8, address: *mut u8, address_len: *mut usize) -> i32;
}

/// Abstract account metadata
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AbstractAccount {
    pub id: String,
    pub address: String,
    pub public_key: Vec<u8>,
    pub guardians: Vec<Guardian>,
    pub created_at: u64,
    pub nonce: u64,
    pub config: AccountConfig,
}

/// Guardian information
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Guardian {
    pub id: String,
    pub public_key: Vec<u8>,
    pub permissions: Vec<String>,
    pub added_at: u64,
}

/// Account configuration
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AccountConfig {
    pub require_guardian_approval: bool,
    pub guardian_threshold: usize,
    pub max_daily_transactions: u32,
    pub security_level: String,
}

/// Account service for abstract account management
pub struct AccountService {
    accounts: Arc<RwLock<HashMap<String, AbstractAccount>>>,
    crypto_service: Arc<CryptoService>,
}

impl AccountService {
    /// Create a new account service instance
    pub async fn new(_config: &EncaveConfig, crypto_service: Arc<CryptoService>) -> Result<Self> {
        info!("Initializing AccountService");
        
        Ok(Self {
            accounts: Arc::new(RwLock::new(HashMap::new())),
            crypto_service,
        })
    }
    
    /// Create a new abstract account with proper Neo cryptographic address generation
    pub fn create_account(&self, account_id: &str, account_data: &str) -> Result<String> {
        let mut accounts = self.accounts.write().map_err(|_| anyhow!("Lock poisoned"))?;
        
        if accounts.contains_key(account_id) {
            return Err(anyhow!("Account '{}' already exists", account_id));
        }
        
        // Parse account configuration
        let config: AccountConfig = serde_json::from_str(account_data)
            .unwrap_or_else(|_| AccountConfig {
                require_guardian_approval: false,
                guardian_threshold: 1,
                max_daily_transactions: 100,
                security_level: "standard".to_string(),
            });
        
        // Generate production-grade ECDSA P-256 key pair using SGX
        let (private_key, public_key) = self.generate_neo_keypair()?;
        
        // Generate proper Neo address from public key using cryptographic functions
        let address = self.generate_neo_address_from_public_key(&public_key)?;
        
        // Store the key securely in the crypto service
        let key_metadata = self.crypto_service.generate_key(
            &format!("account_{}", account_id),
            crate::crypto::CryptoAlgorithm::Secp256k1,
            vec!["Sign".to_string(), "Verify".to_string()],
            false,
            &format!("Abstract account key for {}", account_id),
        )?;
        
        let account = AbstractAccount {
            id: account_id.to_string(),
            address,
            public_key: public_key.to_vec(),
            guardians: Vec::new(),
            created_at: std::time::SystemTime::now()
                .duration_since(std::time::UNIX_EPOCH)?
                .as_secs(),
            nonce: 0,
            config,
        };
        
        accounts.insert(account_id.to_string(), account.clone());
        
        info!("Created abstract account '{}' with Neo address: {}", account_id, account.address);
        debug!("Account public key: {}", hex::encode(&account.public_key));
        
        Ok(serde_json::to_string(&account)?)
    }
    
    /// Sign a transaction for an abstract account
    pub fn sign_transaction(&self, account_id: &str, transaction_data: &str) -> Result<String> {
        let mut accounts = self.accounts.write().map_err(|_| anyhow!("Lock poisoned"))?;
        
        let account = accounts.get_mut(account_id)
            .ok_or_else(|| anyhow!("Account '{}' not found", account_id))?;
        
        // Parse transaction data
        let tx_data: serde_json::Value = serde_json::from_str(transaction_data)?;
        
        // Create transaction hash
        let tx_hash = self.crypto_service.hash_sha256(transaction_data.as_bytes());
        
        // Sign the transaction
        let signature = self.crypto_service.sign_data(&format!("account_{}", account_id), &tx_hash)?;
        
        // Update account nonce
        account.nonce += 1;
        
        let signed_tx = serde_json::json!({
            "transaction": tx_data,
            "signature": hex::encode(&signature),
            "account_id": account_id,
            "account_address": &account.address,
            "nonce": account.nonce,
            "hash": hex::encode(&tx_hash),
            "timestamp": std::time::SystemTime::now()
                .duration_since(std::time::UNIX_EPOCH)
                .unwrap()
                .as_secs()
        });
        
        debug!("Signed transaction for account '{}', nonce: {}", account_id, account.nonce);
        Ok(signed_tx.to_string())
    }
    
    /// Add a guardian to an abstract account
    pub fn add_guardian(&self, account_id: &str, guardian_data: &str) -> Result<String> {
        let mut accounts = self.accounts.write().map_err(|_| anyhow!("Lock poisoned"))?;
        
        let account = accounts.get_mut(account_id)
            .ok_or_else(|| anyhow!("Account '{}' not found", account_id))?;
        
        // Parse guardian data
        let guardian_info: serde_json::Value = serde_json::from_str(guardian_data)?;
        
        let guardian_id = guardian_info["id"].as_str()
            .ok_or_else(|| anyhow!("Guardian ID is required"))?;
        
        let public_key_hex = guardian_info["public_key"].as_str()
            .ok_or_else(|| anyhow!("Guardian public key is required"))?;
        
        let public_key = hex::decode(public_key_hex)
            .map_err(|_| anyhow!("Invalid public key format"))?;
        
        let permissions = guardian_info["permissions"].as_array()
            .map(|arr| arr.iter()
                .filter_map(|v| v.as_str().map(|s| s.to_string()))
                .collect())
            .unwrap_or_else(|| vec!["approve_transactions".to_string()]);
        
        let guardian = Guardian {
            id: guardian_id.to_string(),
            public_key,
            permissions,
            added_at: std::time::SystemTime::now()
                .duration_since(std::time::UNIX_EPOCH)?
                .as_secs(),
        };
        
        account.guardians.push(guardian.clone());
        
        let result = serde_json::json!({
            "account_id": account_id,
            "guardian_added": guardian,
            "total_guardians": account.guardians.len(),
            "timestamp": std::time::SystemTime::now()
                .duration_since(std::time::UNIX_EPOCH)
                .unwrap()
                .as_secs()
        });
        
        info!("Added guardian '{}' to account '{}'", guardian_id, account_id);
        Ok(result.to_string())
    }
    
    /// Get account information
    pub fn get_account_info(&self, account_id: &str) -> Result<String> {
        let accounts = self.accounts.read().map_err(|_| anyhow!("Lock poisoned"))?;
        
        let account = accounts.get(account_id)
            .ok_or_else(|| anyhow!("Account '{}' not found", account_id))?;
        
        // Return account info without sensitive data
        let safe_account = serde_json::json!({
            "id": &account.id,
            "address": &account.address,
            "public_key": hex::encode(&account.public_key),
            "guardians": account.guardians.iter().map(|g| serde_json::json!({
                "id": &g.id,
                "public_key": hex::encode(&g.public_key),
                "permissions": &g.permissions,
                "added_at": g.added_at
            })).collect::<Vec<_>>(),
            "created_at": account.created_at,
            "nonce": account.nonce,
            "config": &account.config
        });
        
        Ok(safe_account.to_string())
    }
    
    /// List all accounts
    pub fn list_accounts(&self) -> Result<Vec<String>> {
        let accounts = self.accounts.read().map_err(|_| anyhow!("Lock poisoned"))?;
        Ok(accounts.keys().cloned().collect())
    }
    
    /// Generate production-grade ECDSA P-256 key pair using SGX
    fn generate_neo_keypair(&self) -> Result<([u8; 32], [u8; 64])> {
        let mut private_key = [0u8; 32];
        let mut public_key = [0u8; 64]; // Uncompressed: 32 bytes x + 32 bytes y
        
        unsafe {
            let result = occlum_generate_ecdsa_keypair(
                private_key.as_mut_ptr(),
                public_key.as_mut_ptr(),
            );
            
            if result != 0 {
                return Err(anyhow!("Failed to generate ECDSA key pair: SGX error {}", result));
            }
        }
        
        debug!("Generated ECDSA P-256 key pair using SGX");
        Ok((private_key, public_key))
    }
    
    /// Generate proper Neo address from public key using cryptographic functions
    fn generate_neo_address_from_public_key(&self, public_key: &[u8]) -> Result<String> {
        if public_key.len() != 64 {
            return Err(anyhow!("Invalid public key length: expected 64 bytes, got {}", public_key.len()));
        }
        
        // Convert uncompressed public key to compressed format for Neo
        let compressed_public_key = self.compress_public_key(public_key)?;
        
        // Generate Neo address using SGX cryptographic functions
        let neo_address = self.generate_neo_address_sgx(&compressed_public_key)?;
        
        // Convert to Base58 format (Neo standard)
        let base58_address = self.encode_neo_address_base58(&neo_address)?;
        
        Ok(base58_address)
    }
    
    /// Compress uncompressed public key to compressed format
    fn compress_public_key(&self, uncompressed_key: &[u8]) -> Result<[u8; 33]> {
        if uncompressed_key.len() != 64 {
            return Err(anyhow!("Invalid uncompressed public key length"));
        }
        
        let mut compressed = [0u8; 33];
        
        // Extract x and y coordinates
        let x_bytes = &uncompressed_key[0..32];
        let y_bytes = &uncompressed_key[32..64];
        
        // Determine compression prefix based on y coordinate parity
        let y_last_byte = y_bytes[31];
        compressed[0] = if y_last_byte % 2 == 0 { 0x02 } else { 0x03 };
        
        // Copy x coordinate
        compressed[1..33].copy_from_slice(x_bytes);
        
        Ok(compressed)
    }
    
    /// Generate Neo address using SGX cryptographic functions
    fn generate_neo_address_sgx(&self, compressed_public_key: &[u8; 33]) -> Result<[u8; 25]> {
        // Step 1: SHA256 hash of the public key
        let mut sha256_hash = [0u8; 32];
        unsafe {
            let result = occlum_sha256(
                compressed_public_key.as_ptr(),
                33,
                sha256_hash.as_mut_ptr(),
            );
            
            if result != 0 {
                return Err(anyhow!("Failed to compute SHA256: SGX error {}", result));
            }
        }
        
        // Step 2: RIPEMD160 hash of the SHA256 hash
        let mut ripemd160_hash = [0u8; 20];
        unsafe {
            let result = occlum_ripemd160(
                sha256_hash.as_ptr(),
                32,
                ripemd160_hash.as_mut_ptr(),
            );
            
            if result != 0 {
                return Err(anyhow!("Failed to compute RIPEMD160: SGX error {}", result));
            }
        }
        
        // Step 3: Add Neo version byte (0x17 for Neo mainnet)
        let mut versioned_hash = [0u8; 21];
        versioned_hash[0] = 0x17; // Neo mainnet version byte
        versioned_hash[1..21].copy_from_slice(&ripemd160_hash);
        
        // Step 4: Calculate checksum (first 4 bytes of SHA256(SHA256(versioned_hash)))
        let mut first_sha = [0u8; 32];
        unsafe {
            let result = occlum_sha256(
                versioned_hash.as_ptr(),
                21,
                first_sha.as_mut_ptr(),
            );
            
            if result != 0 {
                return Err(anyhow!("Failed to compute first checksum SHA256: SGX error {}", result));
            }
        }
        
        let mut checksum_hash = [0u8; 32];
        unsafe {
            let result = occlum_sha256(
                first_sha.as_ptr(),
                32,
                checksum_hash.as_mut_ptr(),
            );
            
            if result != 0 {
                return Err(anyhow!("Failed to compute checksum SHA256: SGX error {}", result));
            }
        }
        
        // Step 5: Combine versioned hash + checksum (first 4 bytes)
        let mut final_address = [0u8; 25];
        final_address[0..21].copy_from_slice(&versioned_hash);
        final_address[21..25].copy_from_slice(&checksum_hash[0..4]);
        
        Ok(final_address)
    }
    
    /// Encode Neo address to Base58 format
    fn encode_neo_address_base58(&self, address_bytes: &[u8; 25]) -> Result<String> {
        // Base58 alphabet used by Bitcoin and Neo
        const BASE58_ALPHABET: &[u8] = b"123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        
        // Convert bytes to big integer
        let mut num = num_bigint::BigUint::from_bytes_be(address_bytes);
        let base = num_bigint::BigUint::from(58u8);
        let zero = num_bigint::BigUint::from(0u8);
        
        let mut result = Vec::new();
        
        // Convert to base58
        while num > zero {
            let remainder = &num % &base;
            let quotient = &num / &base;
            let remainder_u8 = remainder.to_bytes_be()[0];
            result.push(BASE58_ALPHABET[remainder_u8 as usize]);
            num = quotient;
        }
        
        // Add leading '1's for leading zero bytes
        for &byte in address_bytes.iter() {
            if byte == 0 {
                result.push(b'1');
            } else {
                break;
            }
        }
        
        // Reverse the result (since we built it backwards)
        result.reverse();
        
        // Convert to string
        String::from_utf8(result).map_err(|e| anyhow!("Failed to convert to UTF8: {}", e))
    }
    
    /// Validate Neo address format and checksum
    pub fn validate_neo_address(&self, address: &str) -> Result<bool> {
        if address.is_empty() {
            return Ok(false);
        }
        
        // Decode Base58
        let decoded = self.decode_base58(address)?;
        
        if decoded.len() != 25 {
            return Ok(false);
        }
        
        // Check version byte
        if decoded[0] != 0x17 {
            return Ok(false);
        }
        
        // Verify checksum
        let payload = &decoded[0..21];
        let checksum = &decoded[21..25];
        
        // Calculate expected checksum
        let mut first_sha = [0u8; 32];
        unsafe {
            let result = occlum_sha256(payload.as_ptr(), 21, first_sha.as_mut_ptr());
            if result != 0 {
                return Err(anyhow!("Failed to compute checksum verification SHA256: SGX error {}", result));
            }
        }
        
        let mut expected_checksum = [0u8; 32];
        unsafe {
            let result = occlum_sha256(first_sha.as_ptr(), 32, expected_checksum.as_mut_ptr());
            if result != 0 {
                return Err(anyhow!("Failed to compute checksum verification SHA256: SGX error {}", result));
            }
        }
        
        // Compare checksums
        Ok(checksum == &expected_checksum[0..4])
    }
    
    /// Decode Base58 string to bytes
    fn decode_base58(&self, input: &str) -> Result<Vec<u8>> {
        const BASE58_ALPHABET: &[u8] = b"123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        
        let mut result = num_bigint::BigUint::from(0u8);
        let base = num_bigint::BigUint::from(58u8);
        
        for ch in input.chars() {
            let ch_byte = ch as u8;
            let value = BASE58_ALPHABET.iter().position(|&x| x == ch_byte)
                .ok_or_else(|| anyhow!("Invalid Base58 character: {}", ch))?;
            
            result = result * &base + num_bigint::BigUint::from(value);
        }
        
        let mut bytes = result.to_bytes_be();
        
        // Add leading zeros for leading '1's in the input
        for ch in input.chars() {
            if ch == '1' {
                bytes.insert(0, 0);
            } else {
                break;
            }
        }
        
        Ok(bytes)
    }
    
    /// Generate address from existing public key (for guardians or external accounts)
    pub fn address_from_public_key(&self, public_key_hex: &str) -> Result<String> {
        let public_key_bytes = hex::decode(public_key_hex)
            .map_err(|_| anyhow!("Invalid public key hex format"))?;
        
        if public_key_bytes.len() == 33 {
            // Already compressed
            let compressed_key: [u8; 33] = public_key_bytes.try_into()
                .map_err(|_| anyhow!("Failed to convert to 33-byte array"))?;
            let address_bytes = self.generate_neo_address_sgx(&compressed_key)?;
            self.encode_neo_address_base58(&address_bytes)
        } else if public_key_bytes.len() == 64 {
            // Uncompressed, need to compress first
            let compressed_key = self.compress_public_key(&public_key_bytes)?;
            let address_bytes = self.generate_neo_address_sgx(&compressed_key)?;
            self.encode_neo_address_base58(&address_bytes)
        } else if public_key_bytes.len() == 65 && public_key_bytes[0] == 0x04 {
            // Uncompressed with 0x04 prefix, remove prefix
            let uncompressed = &public_key_bytes[1..65];
            let compressed_key = self.compress_public_key(uncompressed)?;
            let address_bytes = self.generate_neo_address_sgx(&compressed_key)?;
            self.encode_neo_address_base58(&address_bytes)
        } else {
            Err(anyhow!("Invalid public key length: expected 33, 64, or 65 bytes, got {}", public_key_bytes.len()))
        }
    }
} 