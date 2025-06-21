use anyhow::Result;
use serde::{Deserialize, Serialize};
use std::ffi::CStr;
use std::os::raw::{c_char, c_int};
use std::ptr;
use std::sync::{Arc, Mutex};
use tokio::runtime::Runtime;
use log::{info, warn, error};

pub mod crypto;
pub mod storage;
pub mod oracle;
pub mod computation;
pub mod ai;
pub mod account;

use crypto::CryptoService;
use storage::StorageService;
use oracle::OracleService;
use computation::ComputationService;
use ai::AIService;
use account::AccountService;

/// Enclave configuration structure.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct EncaveConfig {
    pub mode: String,
    pub log_level: String,
    pub sgx_simulation_mode: bool,
    pub max_threads: usize,
    pub storage_path: String,
    pub network_timeout_seconds: u64,
    pub crypto_algorithms: Vec<String>,
    pub enable_ai: bool,
    pub enable_oracle: bool,
}

impl Default for EncaveConfig {
    fn default() -> Self {
        Self {
            mode: "production".to_string(),
            log_level: "info".to_string(),
            sgx_simulation_mode: false,
            max_threads: 16,
            storage_path: "/secure".to_string(),
            network_timeout_seconds: 30,
            crypto_algorithms: vec![
                "aes-256-gcm".to_string(),
                "secp256k1".to_string(),
                "ed25519".to_string(),
            ],
            enable_ai: true,
            enable_oracle: true,
        }
    }
}

impl EncaveConfig {
    pub fn merge(&mut self, other: EncaveConfig) {
        self.mode = other.mode;
        self.log_level = other.log_level;
        self.sgx_simulation_mode = other.sgx_simulation_mode;
        self.max_threads = other.max_threads;
        self.storage_path = other.storage_path;
        self.network_timeout_seconds = other.network_timeout_seconds;
        self.crypto_algorithms = other.crypto_algorithms;
        self.enable_ai = other.enable_ai;
        self.enable_oracle = other.enable_oracle;
    }
    
    pub fn validate(&self) -> Result<()> {
        if self.max_threads == 0 {
            return Err(anyhow::anyhow!("max_threads must be greater than 0"));
        }
        
        if self.network_timeout_seconds == 0 {
            return Err(anyhow::anyhow!("network_timeout_seconds must be greater than 0"));
        }
        
        Ok(())
    }
    
    pub fn get_number(&self, key: &str) -> Result<usize> {
        match key {
            "computation.max_concurrent_jobs" => Ok(self.max_threads),
            "ai.max_model_size_mb" => Ok(1024), // Default 1GB
            "ai.max_training_data_mb" => Ok(512), // Default 512MB
            _ => Err(anyhow::anyhow!("Unknown config key: {}", key))
        }
    }
}

/// Main enclave runtime that coordinates all services.
pub struct EncaveRuntime {
    config: EncaveConfig,
    crypto_service: Arc<CryptoService>,
    storage_service: Arc<StorageService>,
    oracle_service: Option<Arc<OracleService>>,
    computation_service: Arc<ComputationService>,
    ai_service: Option<Arc<AIService>>,
    account_service: Arc<AccountService>,
    tokio_runtime: Runtime,
}

impl EncaveRuntime {
    pub async fn new(config: EncaveConfig) -> Result<Self> {
        info!("Initializing Neo Service Layer Enclave Runtime");
        
        // Create Tokio runtime
        let tokio_runtime = tokio::runtime::Builder::new_multi_thread()
            .worker_threads(config.max_threads)
            .enable_all()
            .build()?;
        
        // Initialize services
        let crypto_service = Arc::new(CryptoService::new(&config).await?);
        let storage_service = Arc::new(StorageService::new(&config).await?);
        
        let oracle_service = if config.enable_oracle {
            Some(Arc::new(OracleService::new(&config).await?))
        } else {
            None
        };
        
        let computation_service = Arc::new(ComputationService::new(&config).await?);
        
        let ai_service = if config.enable_ai {
            Some(Arc::new(AIService::new(&config).await?))
        } else {
            None
        };
        
        let account_service = Arc::new(AccountService::new(&config, crypto_service.clone()).await?);
        
        Ok(Self {
            config,
            crypto_service,
            storage_service,
            oracle_service,
            computation_service,
            ai_service,
            account_service,
            tokio_runtime,
        })
    }
    
    pub async fn start(&mut self) -> Result<()> {
        info!("Starting enclave services");
        
        // Start storage service
        self.storage_service.start().await?;
        
        // Start oracle service if enabled
        if let Some(oracle) = &self.oracle_service {
            oracle.start().await?;
        }
        
        // Start AI service if enabled
        if let Some(ai) = &self.ai_service {
            ai.start().await?;
        }
        
        info!("All enclave services started successfully");
        Ok(())
    }
    
    pub async fn run(&self) -> Result<()> {
        info!("Running enclave runtime");
        
        // Main runtime loop - this will run indefinitely until shutdown
        loop {
            tokio::time::sleep(tokio::time::Duration::from_secs(1)).await;
            
            // Health checks and maintenance tasks can go here
            // For now, just keep the runtime alive
        }
    }
    
    pub async fn shutdown(&mut self) -> Result<()> {
        info!("Shutting down enclave runtime");
        
        // Shutdown services in reverse order
        if let Some(ai) = &self.ai_service {
            ai.shutdown().await?;
        }
        
        if let Some(oracle) = &self.oracle_service {
            oracle.shutdown().await?;
        }
        
        self.storage_service.shutdown().await?;
        
        info!("Enclave runtime shutdown complete");
        Ok(())
    }
    
    // Getter methods for services
    pub fn crypto_service(&self) -> &Arc<CryptoService> {
        &self.crypto_service
    }
    
    pub fn storage_service(&self) -> &Arc<StorageService> {
        &self.storage_service
    }
    
    pub fn oracle_service(&self) -> Option<&Arc<OracleService>> {
        self.oracle_service.as_ref()
    }
    
    pub fn computation_service(&self) -> &Arc<ComputationService> {
        &self.computation_service
    }
    
    pub fn ai_service(&self) -> Option<&Arc<AIService>> {
        self.ai_service.as_ref()
    }
    
    pub fn account_service(&self) -> &Arc<AccountService> {
        &self.account_service
    }
}

// Global runtime instance for C FFI
static mut RUNTIME: Option<Arc<Mutex<EncaveRuntime>>> = None;

/// Initialize the Occlum enclave runtime.
#[no_mangle]
pub extern "C" fn occlum_init() -> c_int {
    std::panic::catch_unwind(|| {
        let config = EncaveConfig::default();
        
        let rt = tokio::runtime::Runtime::new().unwrap();
        let runtime = rt.block_on(async {
            EncaveRuntime::new(config).await
        });
        
        match runtime {
            Ok(rt) => {
                unsafe {
                    RUNTIME = Some(Arc::new(Mutex::new(rt)));
                }
                0 // Success
            }
            Err(e) => {
                error!("Failed to initialize enclave runtime: {}", e);
                -1 // Error
            }
        }
    }).unwrap_or(-1)
}

/// Destroy the Occlum enclave runtime.
#[no_mangle]
pub extern "C" fn occlum_destroy() -> c_int {
    std::panic::catch_unwind(|| {
        unsafe {
            if let Some(runtime) = RUNTIME.take() {
                // Properly shutdown the runtime and all services
                let rt = tokio::runtime::Runtime::new().unwrap();
                rt.block_on(async {
                    if let Ok(mut runtime_guard) = runtime.lock() {
                        // Shutdown all services gracefully
                        if let Err(e) = runtime_guard.shutdown().await {
                            error!("Error during runtime shutdown: {}", e);
                        }
                    }
                });
                
                // Drop the runtime after proper shutdown
                drop(runtime);
                info!("Enclave runtime destroyed successfully");
                0 // Success
            } else {
                warn!("Runtime not initialized during destroy");
                -1 // Error - not initialized
            }
        }
    }).unwrap_or_else(|e| {
        error!("Panic during runtime destruction: {:?}", e);
        -1
    })
}

/// Helper function to safely get runtime reference.
fn with_runtime<F, R>(f: F) -> c_int 
where
    F: FnOnce(&EncaveRuntime) -> Result<R, Box<dyn std::error::Error>>,
{
    unsafe {
        if let Some(runtime_arc) = &RUNTIME {
            match runtime_arc.lock() {
                Ok(runtime) => {
                    match f(&*runtime) {
                        Ok(_) => 0,
                        Err(e) => {
                            error!("Runtime operation failed: {}", e);
                            -1
                        }
                    }
                }
                Err(e) => {
                    error!("Failed to acquire runtime lock: {}", e);
                    -2
                }
            }
        } else {
            error!("Runtime not initialized");
            -3
        }
    }
}

/// Helper function to convert C string to Rust string.
unsafe fn c_str_to_string(ptr: *const c_char) -> Result<String, Box<dyn std::error::Error>> {
    if ptr.is_null() {
        return Ok(String::new());
    }
    let c_str = CStr::from_ptr(ptr);
    Ok(c_str.to_str()?.to_string())
}

/// Helper function to write result to C buffer.
unsafe fn write_result_to_buffer(
    result: &str,
    buffer: *mut c_char,
    buffer_size: usize,
    actual_size: *mut usize,
) -> c_int {
    let result_bytes = result.as_bytes();
    let write_size = std::cmp::min(result_bytes.len(), buffer_size.saturating_sub(1));
    
    if !buffer.is_null() && buffer_size > 0 {
        ptr::copy_nonoverlapping(result_bytes.as_ptr(), buffer as *mut u8, write_size);
        *(buffer.offset(write_size as isize)) = 0; // Null terminator
    }
    
    if !actual_size.is_null() {
        *actual_size = result_bytes.len();
    }
    
    if result_bytes.len() > buffer_size.saturating_sub(1) {
        1 // Buffer too small
    } else {
        0 // Success
    }
}

// Export the C FFI functions
// These will be implemented in separate modules for better organization
mod ffi_crypto;
mod ffi_storage;
mod ffi_oracle;
mod ffi_computation;
mod ffi_ai;
mod ffi_account;

// Re-export FFI functions
pub use ffi_crypto::*;
pub use ffi_storage::*;
pub use ffi_oracle::*;
pub use ffi_computation::*;
pub use ffi_ai::*;
pub use ffi_account::*; 