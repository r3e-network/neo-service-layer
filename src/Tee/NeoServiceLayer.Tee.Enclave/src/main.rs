use anyhow::Result;
use log::{info, error};
use std::env;
use tokio::signal;

mod crypto;
mod storage;
mod oracle;
mod computation;
mod ai;
mod account;

use neo_service_enclave::{
    EncaveRuntime,
    EncaveConfig,
};

/// Main entry point for Neo Service Layer Occlum enclave application.
#[tokio::main]
async fn main() -> Result<()> {
    // Initialize logging
    env_logger::init();
    
    info!("Starting Neo Service Layer Occlum Enclave");
    info!("Version: {}", env!("CARGO_PKG_VERSION"));
    
    // Load configuration
    let config = load_config()?;
    info!("Configuration loaded successfully");
    
    // Initialize enclave runtime
    let mut runtime = EncaveRuntime::new(config).await?;
    info!("Enclave runtime initialized");
    
    // Start the enclave services
    runtime.start().await?;
    info!("Enclave services started");
    
    // Set up graceful shutdown
    let shutdown_signal = async {
        signal::ctrl_c()
            .await
            .expect("Failed to install CTRL+C signal handler");
        info!("Shutdown signal received");
    };
    
    // Wait for shutdown signal
    tokio::select! {
        result = runtime.run() => {
            match result {
                Ok(_) => info!("Enclave runtime completed successfully"),
                Err(e) => error!("Enclave runtime error: {}", e),
            }
        }
        _ = shutdown_signal => {
            info!("Initiating graceful shutdown");
            runtime.shutdown().await?;
        }
    }
    
    info!("Neo Service Layer Occlum Enclave stopped");
    Ok(())
}

/// Load enclave configuration from environment and files.
fn load_config() -> Result<EncaveConfig> {
    let mut config = EncaveConfig::default();
    
    // Read environment variables
    if let Ok(mode) = env::var("NEO_SERVICE_MODE") {
        config.mode = mode;
    }
    
    if let Ok(log_level) = env::var("OCCLUM_LOG_LEVEL") {
        config.log_level = log_level;
    }
    
    if let Ok(sgx_mode) = env::var("SGX_MODE") {
        config.sgx_simulation_mode = sgx_mode == "SIM";
    }
    
    // Load from configuration file if it exists
    if let Ok(config_data) = std::fs::read_to_string("/secure/config.json") {
        if let Ok(file_config) = serde_json::from_str::<EncaveConfig>(&config_data) {
            config.merge(file_config);
        }
    }
    
    config.validate()?;
    Ok(config)
} 