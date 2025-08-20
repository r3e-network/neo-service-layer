use anyhow::{Result, anyhow};
use ring::aead;
use ring::rand::{SecureRandom, SystemRandom};
use ring::aead::BoundKey;
use secp256k1::{Secp256k1, SecretKey, PublicKey, Message, ecdsa::Signature};
use ed25519_dalek::{SigningKey, Signer, Verifier, VerifyingKey, Signature as Ed25519Signature};
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::sync::{Arc, RwLock};
use sha2::{Sha256, Digest};
use log::{info, warn, error, debug};

use crate::EncaveConfig;

/// Supported cryptographic algorithms
#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum CryptoAlgorithm {
    Aes256Gcm,
    ChaCha20Poly1305,
    Secp256k1,
    Ed25519,
    Sha256,
    Sha3_256,
}

/// Key metadata structure
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct KeyMetadata {
    pub key_id: String,
    pub key_type: CryptoAlgorithm,
    pub usage: Vec<String>,
    pub exportable: bool,
    pub created_at: u64,
    pub description: String,
    pub public_key: Option<Vec<u8>>,
}

/// Cryptographic key storage
#[derive(Debug)]
struct KeyStore {
    symmetric_keys: HashMap<String, Vec<u8>>,
    asymmetric_keys: HashMap<String, (Vec<u8>, Vec<u8>)>, // (private, public)
    metadata: HashMap<String, KeyMetadata>,
}

impl KeyStore {
    fn new() -> Self {
        Self {
            symmetric_keys: HashMap::new(),
            asymmetric_keys: HashMap::new(),
            metadata: HashMap::new(),
        }
    }
}

/// Main cryptographic service for the enclave
pub struct CryptoService {
    rng: SystemRandom,
    secp256k1: Secp256k1<secp256k1::All>,
    key_store: Arc<RwLock<KeyStore>>,
    #[allow(dead_code)]
    supported_algorithms: Vec<CryptoAlgorithm>,
}

impl CryptoService {
    /// Create a new crypto service instance
    pub async fn new(config: &EncaveConfig) -> Result<Self> {
        info!("Initializing CryptoService");
        
        let supported_algorithms = config.crypto_algorithms
            .iter()
            .filter_map(|alg| match alg.as_str() {
                "aes-256-gcm" => Some(CryptoAlgorithm::Aes256Gcm),
                "chacha20-poly1305" => Some(CryptoAlgorithm::ChaCha20Poly1305),
                "secp256k1" => Some(CryptoAlgorithm::Secp256k1),
                "ed25519" => Some(CryptoAlgorithm::Ed25519),
                "sha256" => Some(CryptoAlgorithm::Sha256),
                "sha3-256" => Some(CryptoAlgorithm::Sha3_256),
                _ => {
                    warn!("Unsupported crypto algorithm: {}", alg);
                    None
                }
            })
            .collect();
        
        Ok(Self {
            rng: SystemRandom::new(),
            secp256k1: Secp256k1::new(),
            key_store: Arc::new(RwLock::new(KeyStore::new())),
            supported_algorithms,
        })
    }
    
    /// Generate a secure random number within range
    pub fn generate_random(&self, min: i32, max: i32) -> Result<i32> {
        if min >= max {
            return Err(anyhow!("Min must be less than max"));
        }
        
        let range = (max - min) as u32;
        let mut bytes = vec![0u8; 4];
        self.rng.fill(&mut bytes)?;
        
        let random_u32 = u32::from_le_bytes([bytes[0], bytes[1], bytes[2], bytes[3]]);
        let result = min + (random_u32 % range) as i32;
        
        debug!("Generated random number: {} (range: {} - {})", result, min, max);
        Ok(result)
    }
    
    /// Generate secure random bytes
    pub fn generate_random_bytes(&self, length: usize) -> Result<Vec<u8>> {
        if length == 0 || length > 1024 * 1024 {
            return Err(anyhow!("Invalid length: must be between 1 and 1MB"));
        }
        
        let mut bytes = vec![0u8; length];
        self.rng.fill(&mut bytes)?;
        
        debug!("Generated {} random bytes", length);
        Ok(bytes)
    }
    
    /// Generate a cryptographic key
    pub fn generate_key(
        &self,
        key_id: &str,
        key_type: CryptoAlgorithm,
        usage: Vec<String>,
        exportable: bool,
        description: &str,
    ) -> Result<KeyMetadata> {
        if key_id.is_empty() {
            return Err(anyhow!("Key ID cannot be empty"));
        }
        
        let mut key_store = self.key_store.write().map_err(|_| anyhow!("Lock poisoned"))?;
        
        if key_store.metadata.contains_key(key_id) {
            return Err(anyhow!("Key with ID '{}' already exists", key_id));
        }
        
        let (public_key_bytes, created_at) = match key_type {
            CryptoAlgorithm::Aes256Gcm => {
                let mut key = vec![0u8; 32]; // 256 bits
                self.rng.fill(&mut key)?;
                key_store.symmetric_keys.insert(key_id.to_string(), key);
                (None, std::time::SystemTime::now().duration_since(std::time::UNIX_EPOCH)?.as_secs())
            }
            CryptoAlgorithm::Secp256k1 => {
                let mut private_key_bytes = vec![0u8; 32];
                self.rng.fill(&mut private_key_bytes)?;
                
                let private_key = SecretKey::from_slice(&private_key_bytes)?;
                let public_key = PublicKey::from_secret_key(&self.secp256k1, &private_key);
                let public_key_bytes = public_key.serialize().to_vec();
                
                key_store.asymmetric_keys.insert(
                    key_id.to_string(),
                    (private_key_bytes, public_key_bytes.clone())
                );
                
                (Some(public_key_bytes), std::time::SystemTime::now().duration_since(std::time::UNIX_EPOCH)?.as_secs())
            }
            CryptoAlgorithm::Ed25519 => {
                let mut seed = [0u8; 32];
                self.rng.fill(&mut seed)?;
                
                let keypair = SigningKey::from_bytes(&seed);
                let public_key_bytes = keypair.verifying_key().to_bytes().to_vec();
                let private_key_bytes = keypair.to_bytes().to_vec();
                
                key_store.asymmetric_keys.insert(
                    key_id.to_string(),
                    (private_key_bytes, public_key_bytes.clone())
                );
                
                (Some(public_key_bytes), std::time::SystemTime::now().duration_since(std::time::UNIX_EPOCH)?.as_secs())
            }
            _ => return Err(anyhow!("Unsupported key type for generation: {:?}", key_type)),
        };
        
        let metadata = KeyMetadata {
            key_id: key_id.to_string(),
            key_type,
            usage,
            exportable,
            created_at,
            description: description.to_string(),
            public_key: public_key_bytes,
        };
        
        key_store.metadata.insert(key_id.to_string(), metadata.clone());
        
        info!("Generated key '{}' of type {:?}", key_id, metadata.key_type);
        Ok(metadata)
    }
    
    /// Encrypt data using AES-256-GCM
    pub fn encrypt_aes_gcm(&self, data: &[u8], key: &[u8]) -> Result<Vec<u8>> {
        if key.len() != 32 {
            return Err(anyhow!("AES-256 key must be 32 bytes"));
        }
        
        let mut nonce = [0u8; 12];
        self.rng.fill(&mut nonce)?;
        
        let mut in_out = data.to_vec();
        // For ring 0.17, we need to use seal_in_place_append_tag
        let unbound_key = aead::UnboundKey::new(&aead::AES_256_GCM, key)?;
        let less_safe_key = aead::LessSafeKey::new(unbound_key);
        let encrypted_result = less_safe_key.seal_in_place_append_tag(
            aead::Nonce::assume_unique_for_key(nonce),
            aead::Aad::empty(),
            &mut in_out,
        )?;
        
        // The tag is already appended to in_out by seal_in_place_append_tag
        
        // Combine nonce + ciphertext_with_tag
        let mut result = Vec::with_capacity(12 + in_out.len());
        result.extend_from_slice(&nonce);
        result.extend_from_slice(&in_out);
        
        debug!("Encrypted {} bytes with AES-256-GCM", data.len());
        Ok(result)
    }
    
    /// Decrypt data using AES-256-GCM
    pub fn decrypt_aes_gcm(&self, encrypted_data: &[u8], key: &[u8]) -> Result<Vec<u8>> {
        if key.len() != 32 {
            return Err(anyhow!("AES-256 key must be 32 bytes"));
        }
        
        if encrypted_data.len() < 28 { // 12 (nonce) + 16 (tag) minimum
            return Err(anyhow!("Encrypted data too short"));
        }
        
        let nonce = &encrypted_data[0..12];
        let ciphertext_and_tag = &encrypted_data[12..];
        
        let mut in_out = ciphertext_and_tag.to_vec();
        let unbound_key = aead::UnboundKey::new(&aead::AES_256_GCM, key)?;
        let less_safe_key = aead::LessSafeKey::new(unbound_key);
        let plaintext = less_safe_key.open_in_place(
            aead::Nonce::try_assume_unique_for_key(nonce)?,
            aead::Aad::empty(),
            &mut in_out,
        )?;
        
        debug!("Decrypted {} bytes with AES-256-GCM", plaintext.len());
        Ok(plaintext.to_vec())
    }
    
    /// Sign data using a stored key
    pub fn sign_data(&self, key_id: &str, data: &[u8]) -> Result<Vec<u8>> {
        let key_store = self.key_store.read().map_err(|_| anyhow!("Lock poisoned"))?;
        
        let metadata = key_store.metadata.get(key_id)
            .ok_or_else(|| anyhow!("Key '{}' not found", key_id))?;
        
        if !metadata.usage.contains(&"Sign".to_string()) {
            return Err(anyhow!("Key '{}' is not authorized for signing", key_id));
        }
        
        match metadata.key_type {
            CryptoAlgorithm::Secp256k1 => {
                let (private_key_bytes, _) = key_store.asymmetric_keys.get(key_id)
                    .ok_or_else(|| anyhow!("Private key '{}' not found", key_id))?;
                
                let private_key = SecretKey::from_slice(private_key_bytes)?;
                let message_hash = Sha256::digest(data);
                let message = Message::from_slice(&message_hash)?;
                let signature = self.secp256k1.sign_ecdsa(&message, &private_key);
                
                debug!("Signed {} bytes with secp256k1 key '{}'", data.len(), key_id);
                Ok(signature.serialize_compact().to_vec())
            }
            CryptoAlgorithm::Ed25519 => {
                let (private_key_bytes, _) = key_store.asymmetric_keys.get(key_id)
                    .ok_or_else(|| anyhow!("Private key '{}' not found", key_id))?;
                
                if private_key_bytes.len() != 32 {
                    return Err(anyhow!("Invalid key length for Ed25519"));
                }
                let mut key_bytes = [0u8; 32];
                key_bytes.copy_from_slice(&private_key_bytes[..32]);
                let keypair = SigningKey::from_bytes(&key_bytes);
                let signature = keypair.sign(data);
                
                debug!("Signed {} bytes with Ed25519 key '{}'", data.len(), key_id);
                Ok(signature.to_bytes().to_vec())
            }
            _ => Err(anyhow!("Key type {:?} does not support signing", metadata.key_type)),
        }
    }
    
    /// Verify a signature using a stored key
    pub fn verify_signature(&self, key_id: &str, data: &[u8], signature: &[u8]) -> Result<bool> {
        let key_store = self.key_store.read().map_err(|_| anyhow!("Lock poisoned"))?;
        
        let metadata = key_store.metadata.get(key_id)
            .ok_or_else(|| anyhow!("Key '{}' not found", key_id))?;
        
        if !metadata.usage.contains(&"Verify".to_string()) {
            return Err(anyhow!("Key '{}' is not authorized for verification", key_id));
        }
        
        match metadata.key_type {
            CryptoAlgorithm::Secp256k1 => {
                let (_, public_key_bytes) = key_store.asymmetric_keys.get(key_id)
                    .ok_or_else(|| anyhow!("Public key '{}' not found", key_id))?;
                
                let public_key = PublicKey::from_slice(public_key_bytes)?;
                let message_hash = Sha256::digest(data);
                let message = Message::from_slice(&message_hash)?;
                let signature = Signature::from_compact(signature)?;
                
                let is_valid = self.secp256k1.verify_ecdsa(&message, &signature, &public_key).is_ok();
                debug!("Verified signature for {} bytes with secp256k1 key '{}': {}", data.len(), key_id, is_valid);
                Ok(is_valid)
            }
            CryptoAlgorithm::Ed25519 => {
                let (_, public_key_bytes) = key_store.asymmetric_keys.get(key_id)
                    .ok_or_else(|| anyhow!("Public key '{}' not found", key_id))?;
                
                if public_key_bytes.len() != 32 {
                    return Err(anyhow!("Invalid public key length for Ed25519"));
                }
                let mut public_key_array = [0u8; 32];
                public_key_array.copy_from_slice(&public_key_bytes[..32]);
                let public_key = VerifyingKey::from_bytes(&public_key_array)
                    .map_err(|e| anyhow!("Invalid Ed25519 public key: {}", e))?;
                
                if signature.len() != 64 {
                    return Err(anyhow!("Invalid signature length for Ed25519"));
                }
                let mut signature_array = [0u8; 64];
                signature_array.copy_from_slice(&signature[..64]);
                let signature = Ed25519Signature::from_bytes(&signature_array);
                
                let is_valid = public_key.verify(data, &signature).is_ok();
                debug!("Verified signature for {} bytes with Ed25519 key '{}': {}", data.len(), key_id, is_valid);
                Ok(is_valid)
            }
            _ => Err(anyhow!("Key type {:?} does not support verification", metadata.key_type)),
        }
    }
    
    /// Hash data using SHA-256
    pub fn hash_sha256(&self, data: &[u8]) -> Vec<u8> {
        let hash = Sha256::digest(data);
        debug!("Computed SHA-256 hash for {} bytes", data.len());
        hash.to_vec()
    }
    
    /// Get key metadata
    pub fn get_key_metadata(&self, key_id: &str) -> Result<KeyMetadata> {
        let key_store = self.key_store.read().map_err(|_| anyhow!("Lock poisoned"))?;
        
        key_store.metadata.get(key_id)
            .cloned()
            .ok_or_else(|| anyhow!("Key '{}' not found", key_id))
    }
    
    /// List all stored keys
    pub fn list_keys(&self) -> Result<Vec<String>> {
        let key_store = self.key_store.read().map_err(|_| anyhow!("Lock poisoned"))?;
        Ok(key_store.metadata.keys().cloned().collect())
    }
    
    /// Delete a key
    pub fn delete_key(&self, key_id: &str) -> Result<()> {
        let mut key_store = self.key_store.write().map_err(|_| anyhow!("Lock poisoned"))?;
        
        if !key_store.metadata.contains_key(key_id) {
            return Err(anyhow!("Key '{}' not found", key_id));
        }
        
        key_store.metadata.remove(key_id);
        key_store.symmetric_keys.remove(key_id);
        key_store.asymmetric_keys.remove(key_id);
        
        info!("Deleted key '{}'", key_id);
        Ok(())
    }
} 