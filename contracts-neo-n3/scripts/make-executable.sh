#!/bin/bash

# Make all Neo Service Layer scripts executable

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

# Navigate to scripts directory
cd "$(dirname "$0")"

log_info "Making all scripts executable..."

# Make all .sh files executable
chmod +x *.sh

# List all scripts
log_info "Available scripts:"
for script in *.sh; do
    if [ -f "$script" ]; then
        log_success "  ✓ $script"
    fi
done

log_success "All scripts are now executable!"