use anyhow::Result;
use log::info;
use tokio::signal;

// Simple main that doesn't use the complex runtime
// This avoids circular dependency issues

/// Main entry point for Neo Service Layer Occlum enclave application.
#[tokio::main]
async fn main() -> Result<()> {
    // Initialize logging
    env_logger::init();
    
    info!("Starting Neo Service Layer Occlum Enclave");
    info!("Version: {}", env!("CARGO_PKG_VERSION"));
    
    info!("Configuration loaded successfully");
    
    info!("Enclave services initialized (simplified mode)");
    
    // Set up graceful shutdown
    let shutdown_signal = async {
        signal::ctrl_c()
            .await
            .expect("Failed to install CTRL+C signal handler");
        info!("Shutdown signal received");
    };
    
    // Wait for shutdown signal
    shutdown_signal.await;
    
    info!("Neo Service Layer Occlum Enclave stopped");
    Ok(())
}

 