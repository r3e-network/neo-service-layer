#!/bin/bash

# Phase 3 Deployment Script for Neo Service Layer
# Services: Oracle, Storage, CrossChain, ProofOfReserve, Randomness
# Advanced Services: Fair Ordering, TEE Host

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
COMPOSE_FILE="docker-compose.phase3.yml"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
DEPLOYMENT_TIMEOUT=900 # 15 minutes (Oracle and CrossChain need more time)

log_info() {
    echo "[$(date +'%H:%M:%S')] $1"
}

log_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

log_warn() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

log_error() {
    echo -e "${RED}✗ $1${NC}"
}

log_section() {
    echo ""
    echo -e "${BLUE}=== $1 ===${NC}"
}

# Check if Phase 2 is healthy
check_phase2_health() {
    log_section "Checking Phase 2 Health"
    
    if ! "$SCRIPT_DIR/health-check-phase2.sh"; then
        log_error "Phase 2 is not healthy. Fix Phase 2 issues before deploying Phase 3."
        exit 1
    fi
    
    log_success "Phase 2 is healthy, proceeding with Phase 3 deployment"
}

# Generate Phase 3 Docker Compose file
generate_compose_file() {
    log_section "Generating Phase 3 Docker Compose Configuration"
    
    cat > "$PROJECT_ROOT/$COMPOSE_FILE" << 'EOF'
version: '3.8'

services:
  # Oracle Service
  neo-oracle-phase3:
    build:
      context: ./src/Services/NeoServiceLayer.Services.Oracle
      dockerfile: Dockerfile
    container_name: neo-oracle-phase3
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - DB_CONNECTION_STRING=Host=neo-postgres-phase1;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
      - REDIS_CONNECTION_STRING=neo-redis-phase1:6379
      - CONSUL_ADDRESS=http://neo-consul-phase1:8500
      - SERVICE_NAME=OracleService
      - SERVICE_PORT=8088
      - SGX_MODE=${SGX_MODE:-SIM}
      - ENCLAVE_CONFIG_FILE=/app/enclave.json
      - ORACLE_DATA_SOURCES=${ORACLE_DATA_SOURCES:-https://api.coinbase.com/v2,https://api.binance.com/api/v3}
      - ORACLE_UPDATE_INTERVAL=60
      - ORACLE_CONSENSUS_THRESHOLD=0.66
      - MAX_PRICE_DEVIATION=0.05
      - RATE_LIMIT_REQUESTS_PER_MINUTE=100
    ports:
      - "8088:8080"
    depends_on:
      - neo-postgres-phase1
      - neo-redis-phase1
      - neo-consul-phase1
    volumes:
      - oracle-data:/app/data
      - oracle-cache:/app/cache
      - ./enclave.json:/app/enclave.json:ro
    networks:
      - neo-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 90s
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '1.0'
        reservations:
          memory: 512M
          cpus: '0.5'

  # Storage Service
  neo-storage-phase3:
    build:
      context: ./src/Services/NeoServiceLayer.Services.Storage
      dockerfile: Dockerfile
    container_name: neo-storage-phase3
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - DB_CONNECTION_STRING=Host=neo-postgres-phase1;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
      - REDIS_CONNECTION_STRING=neo-redis-phase1:6379
      - CONSUL_ADDRESS=http://neo-consul-phase1:8500
      - SERVICE_NAME=StorageService
      - SERVICE_PORT=8089
      - SGX_MODE=${SGX_MODE:-SIM}
      - ENCLAVE_CONFIG_FILE=/app/enclave.json
      - STORAGE_ENCRYPTION_KEY=${STORAGE_ENCRYPTION_KEY}
      - BACKUP_ENABLED=true
      - BACKUP_INTERVAL=3600
      - BACKUP_RETENTION_DAYS=30
      - AUDIT_LOGGING_ENABLED=true
      - COMPRESSION_ENABLED=true
    ports:
      - "8089:8080"
    depends_on:
      - neo-postgres-phase1
      - neo-redis-phase1
      - neo-consul-phase1
    volumes:
      - storage-data:/app/data
      - storage-backup:/app/backup
      - storage-audit:/app/audit
      - ./enclave.json:/app/enclave.json:ro
    networks:
      - neo-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    deploy:
      resources:
        limits:
          memory: 2G
          cpus: '1.5'
        reservations:
          memory: 1G
          cpus: '0.75'

  # CrossChain Service
  neo-crosschain-phase3:
    build:
      context: ./src/Services/NeoServiceLayer.Services.CrossChain
      dockerfile: Dockerfile
    container_name: neo-crosschain-phase3
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - DB_CONNECTION_STRING=Host=neo-postgres-phase1;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
      - REDIS_CONNECTION_STRING=neo-redis-phase1:6379
      - CONSUL_ADDRESS=http://neo-consul-phase1:8500
      - SERVICE_NAME=CrossChainService
      - SERVICE_PORT=8090
      - SGX_MODE=${SGX_MODE:-SIM}
      - ENCLAVE_CONFIG_FILE=/app/enclave.json
      - SUPPORTED_CHAINS=NeoN3,NeoX,Ethereum,Bitcoin
      - BRIDGE_CONTRACT_NEO_N3=${BRIDGE_CONTRACT_NEO_N3}
      - BRIDGE_CONTRACT_NEO_X=${BRIDGE_CONTRACT_NEO_X}
      - ETHEREUM_RPC_URL=${ETHEREUM_RPC_URL:-https://mainnet.infura.io/v3/YOUR_PROJECT_ID}
      - BITCOIN_RPC_URL=${BITCOIN_RPC_URL:-https://bitcoin-rpc.example.com}
      - CONFIRMATION_BLOCKS_NEO_N3=1
      - CONFIRMATION_BLOCKS_NEO_X=12
      - CONFIRMATION_BLOCKS_ETHEREUM=12
      - CONFIRMATION_BLOCKS_BITCOIN=6
    ports:
      - "8090:8080"
    depends_on:
      - neo-postgres-phase1
      - neo-redis-phase1
      - neo-consul-phase1
    volumes:
      - crosschain-data:/app/data
      - crosschain-keys:/app/keys
      - ./enclave.json:/app/enclave.json:ro
    networks:
      - neo-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 120s
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '1.0'
        reservations:
          memory: 512M
          cpus: '0.5'

  # Proof of Reserve Service
  neo-proofofreserve-phase3:
    build:
      context: ./src/Services/NeoServiceLayer.Services.ProofOfReserve
      dockerfile: Dockerfile
    container_name: neo-proofofreserve-phase3
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - DB_CONNECTION_STRING=Host=neo-postgres-phase1;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
      - REDIS_CONNECTION_STRING=neo-redis-phase1:6379
      - CONSUL_ADDRESS=http://neo-consul-phase1:8500
      - SERVICE_NAME=ProofOfReserveService
      - SERVICE_PORT=8091
      - SGX_MODE=${SGX_MODE:-SIM}
      - ENCLAVE_CONFIG_FILE=/app/enclave.json
      - AUDIT_INTERVAL=86400
      - MERKLE_TREE_ENABLED=true
      - PROOF_GENERATION_INTERVAL=3600
      - RESERVE_ADDRESSES=${RESERVE_ADDRESSES}
      - ATTESTATION_ENABLED=true
    ports:
      - "8091:8080"
    depends_on:
      - neo-postgres-phase1
      - neo-redis-phase1
      - neo-consul-phase1
    volumes:
      - proofofreserve-data:/app/data
      - proofofreserve-proofs:/app/proofs
      - ./enclave.json:/app/enclave.json:ro
    networks:
      - neo-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.5'
        reservations:
          memory: 256M
          cpus: '0.25'

  # Randomness Service
  neo-randomness-phase3:
    build:
      context: ./src/Services/NeoServiceLayer.Services.Randomness
      dockerfile: Dockerfile
    container_name: neo-randomness-phase3
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - DB_CONNECTION_STRING=Host=neo-postgres-phase1;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
      - REDIS_CONNECTION_STRING=neo-redis-phase1:6379
      - CONSUL_ADDRESS=http://neo-consul-phase1:8500
      - SERVICE_NAME=RandomnessService
      - SERVICE_PORT=8092
      - SGX_MODE=${SGX_MODE:-SIM}
      - ENCLAVE_CONFIG_FILE=/app/enclave.json
      - VRF_ENABLED=true
      - ENTROPY_SOURCES=Hardware,DRAND,Beacon
      - RANDOMNESS_CACHE_SIZE=10000
      - SECURITY_LEVEL=256
      - AUDIT_TRAIL_ENABLED=true
    ports:
      - "8092:8080"
    depends_on:
      - neo-postgres-phase1
      - neo-redis-phase1
      - neo-consul-phase1
    volumes:
      - randomness-data:/app/data
      - randomness-entropy:/app/entropy
      - ./enclave.json:/app/enclave.json:ro
    networks:
      - neo-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.5'
        reservations:
          memory: 256M
          cpus: '0.25'

  # Fair Ordering Service
  neo-fair-ordering-phase3:
    build:
      context: ./src/Advanced/NeoServiceLayer.Advanced.FairOrdering
      dockerfile: Dockerfile
    container_name: neo-fair-ordering-phase3
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - DB_CONNECTION_STRING=Host=neo-postgres-phase1;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
      - REDIS_CONNECTION_STRING=neo-redis-phase1:6379
      - CONSUL_ADDRESS=http://neo-consul-phase1:8500
      - SERVICE_NAME=FairOrderingService
      - SERVICE_PORT=8093
      - SGX_MODE=${SGX_MODE:-SIM}
      - ENCLAVE_CONFIG_FILE=/app/enclave.json
      - ORDERING_ALGORITHM=FIFO
      - BATCH_SIZE=100
      - BATCH_TIMEOUT=1000
      - MEV_PROTECTION_ENABLED=true
      - COMMIT_REVEAL_ENABLED=true
    ports:
      - "8093:8080"
    depends_on:
      - neo-postgres-phase1
      - neo-redis-phase1
      - neo-consul-phase1
    volumes:
      - fair-ordering-data:/app/data
      - fair-ordering-batches:/app/batches
      - ./enclave.json:/app/enclave.json:ro
    networks:
      - neo-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '1.0'
        reservations:
          memory: 512M
          cpus: '0.5'

  # TEE Host Service
  neo-tee-host-phase3:
    build:
      context: ./src/Tee/NeoServiceLayer.Tee.Host
      dockerfile: Dockerfile
    container_name: neo-tee-host-phase3
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - DB_CONNECTION_STRING=Host=neo-postgres-phase1;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
      - REDIS_CONNECTION_STRING=neo-redis-phase1:6379
      - CONSUL_ADDRESS=http://neo-consul-phase1:8500
      - SERVICE_NAME=TEEHostService
      - SERVICE_PORT=8094
      - SGX_MODE=${SGX_MODE:-SIM}
      - ENCLAVE_CONFIG_FILE=/app/enclave.json
      - ATTESTATION_SERVICE_URL=${ATTESTATION_SERVICE_URL}
      - ENCLAVE_MEASUREMENT=${ENCLAVE_MEASUREMENT}
      - REMOTE_ATTESTATION_ENABLED=true
      - SEALED_STORAGE_ENABLED=true
    ports:
      - "8094:8080"
    depends_on:
      - neo-postgres-phase1
      - neo-redis-phase1
      - neo-consul-phase1
    volumes:
      - tee-host-data:/app/data
      - tee-host-sealed:/app/sealed
      - ./enclave.json:/app/enclave.json:ro
    networks:
      - neo-network
    restart: unless-stopped
    privileged: true # Required for SGX
    devices:
      - /dev/sgx_enclave:/dev/sgx_enclave
      - /dev/sgx_provision:/dev/sgx_provision
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 90s
    deploy:
      resources:
        limits:
          memory: 2G
          cpus: '2.0'
        reservations:
          memory: 1G
          cpus: '1.0'

volumes:
  oracle-data:
    driver: local
  oracle-cache:
    driver: local
  storage-data:
    driver: local
  storage-backup:
    driver: local
  storage-audit:
    driver: local
  crosschain-data:
    driver: local
  crosschain-keys:
    driver: local
  proofofreserve-data:
    driver: local
  proofofreserve-proofs:
    driver: local
  randomness-data:
    driver: local
  randomness-entropy:
    driver: local
  fair-ordering-data:
    driver: local
  fair-ordering-batches:
    driver: local
  tee-host-data:
    driver: local
  tee-host-sealed:
    driver: local

networks:
  neo-network:
    external: true

EOF

    log_success "Generated Phase 3 Docker Compose configuration"
}

# Update gateway configuration for Phase 3 services
update_gateway_config() {
    log_section "Updating API Gateway Configuration for Phase 3"
    
    # Add Phase 3 services to Consul configuration
    cat > "$PROJECT_ROOT/consul/config/phase3-services.json" << EOF
{
  "services": [
    {
      "name": "OracleService",
      "id": "oracle-1",
      "address": "neo-oracle-phase3",
      "port": 8080,
      "tags": ["phase3", "oracle", "data-feeds"],
      "check": {
        "http": "http://neo-oracle-phase3:8080/health",
        "interval": "30s",
        "timeout": "5s"
      }
    },
    {
      "name": "StorageService",
      "id": "storage-1",
      "address": "neo-storage-phase3",
      "port": 8080,
      "tags": ["phase3", "storage", "data-persistence"],
      "check": {
        "http": "http://neo-storage-phase3:8080/health",
        "interval": "30s",
        "timeout": "5s"
      }
    },
    {
      "name": "CrossChainService",
      "id": "crosschain-1",
      "address": "neo-crosschain-phase3",
      "port": 8080,
      "tags": ["phase3", "cross-chain", "interoperability"],
      "check": {
        "http": "http://neo-crosschain-phase3:8080/health",
        "interval": "30s",
        "timeout": "5s"
      }
    },
    {
      "name": "ProofOfReserveService",
      "id": "proofofreserve-1",
      "address": "neo-proofofreserve-phase3",
      "port": 8080,
      "tags": ["phase3", "proof-of-reserve", "auditing"],
      "check": {
        "http": "http://neo-proofofreserve-phase3:8080/health",
        "interval": "30s",
        "timeout": "5s"
      }
    },
    {
      "name": "RandomnessService",
      "id": "randomness-1",
      "address": "neo-randomness-phase3",
      "port": 8080,
      "tags": ["phase3", "randomness", "cryptography"],
      "check": {
        "http": "http://neo-randomness-phase3:8080/health",
        "interval": "30s",
        "timeout": "5s"
      }
    },
    {
      "name": "FairOrderingService",
      "id": "fair-ordering-1",
      "address": "neo-fair-ordering-phase3",
      "port": 8080,
      "tags": ["phase3", "fair-ordering", "mev-protection"],
      "check": {
        "http": "http://neo-fair-ordering-phase3:8080/health",
        "interval": "30s",
        "timeout": "5s"
      }
    },
    {
      "name": "TEEHostService",
      "id": "tee-host-1",
      "address": "neo-tee-host-phase3",
      "port": 8080,
      "tags": ["phase3", "tee", "secure-computation"],
      "check": {
        "http": "http://neo-tee-host-phase3:8080/health",
        "interval": "30s",
        "timeout": "5s"
      }
    }
  ]
}
EOF

    log_success "Updated API Gateway configuration for Phase 3 services"
}

# Create additional configuration files
create_additional_configs() {
    log_section "Creating Additional Configuration Files"
    
    # Create oracle configuration
    mkdir -p "$PROJECT_ROOT/oracle/config"
    cat > "$PROJECT_ROOT/oracle/config/data-sources.json" << 'EOF'
{
  "dataSources": [
    {
      "name": "CoinGecko",
      "url": "https://api.coingecko.com/api/v3",
      "enabled": true,
      "weight": 1.0,
      "timeout": 5000
    },
    {
      "name": "CoinMarketCap",
      "url": "https://pro-api.coinmarketcap.com/v1",
      "enabled": true,
      "weight": 1.0,
      "timeout": 5000
    },
    {
      "name": "Binance",
      "url": "https://api.binance.com/api/v3",
      "enabled": true,
      "weight": 1.0,
      "timeout": 5000
    }
  ]
}
EOF

    # Create cross-chain configuration
    mkdir -p "$PROJECT_ROOT/crosschain/config"
    cat > "$PROJECT_ROOT/crosschain/config/chains.json" << 'EOF'
{
  "supportedChains": {
    "neo-n3": {
      "chainId": "neo-n3",
      "rpcUrl": "http://localhost:40332",
      "confirmationBlocks": 1,
      "bridgeContract": "0x..."
    },
    "neo-x": {
      "chainId": "neo-x", 
      "rpcUrl": "http://localhost:8545",
      "confirmationBlocks": 12,
      "bridgeContract": "0x..."
    }
  }
}
EOF

    log_success "Created additional configuration files"
}

# Deploy Phase 3 services
deploy_services() {
    log_section "Deploying Phase 3 Services"
    
    cd "$PROJECT_ROOT"
    
    # Load environment variables
    if [ -f ".env.production" ]; then
        set -a
        source .env.production
        set +a
        log_info "Loaded production environment variables"
    else
        log_error "Production environment file not found"
        exit 1
    fi
    
    # Pull images first
    log_info "Pulling required Docker images..."
    docker compose -f "$COMPOSE_FILE" pull --ignore-pull-failures
    
    # Build and start services
    log_info "Building and starting Phase 3 services..."
    docker compose -f "$COMPOSE_FILE" up -d --build
    
    log_success "Phase 3 services deployment initiated"
}

# Wait for services to be ready
wait_for_services() {
    log_section "Waiting for Phase 3 Services to be Ready"
    
    local services=("neo-oracle-phase3" "neo-storage-phase3" "neo-crosschain-phase3" "neo-proofofreserve-phase3" "neo-randomness-phase3" "neo-fair-ordering-phase3" "neo-tee-host-phase3")
    local timeout=$DEPLOYMENT_TIMEOUT
    local start_time=$(date +%s)
    
    while [ $(($(date +%s) - start_time)) -lt $timeout ]; do
        local all_ready=true
        
        for service in "${services[@]}"; do
            if ! docker exec "$service" curl -sf http://localhost:8080/health >/dev/null 2>&1; then
                all_ready=false
                break
            fi
        done
        
        if [ "$all_ready" = true ]; then
            log_success "All Phase 3 services are ready"
            return 0
        fi
        
        log_info "Waiting for services to be ready... (${services[*]})"
        sleep 15
    done
    
    log_error "Timeout waiting for Phase 3 services to be ready"
    return 1
}

# Register services with Consul
register_services() {
    log_section "Registering Phase 3 Services with Consul"
    
    # Wait for Consul to be ready
    timeout 30 bash -c 'until curl -sf http://localhost:8500/v1/status/leader; do sleep 1; done'
    
    # Register services
    curl -X PUT http://localhost:8500/v1/agent/service/register -d @"$PROJECT_ROOT/consul/config/phase3-services.json" >/dev/null 2>&1
    
    if [ $? -eq 0 ]; then
        log_success "Registered Phase 3 services with Consul"
    else
        log_warn "Failed to register some Phase 3 services with Consul"
    fi
}

# Run Phase 3 health checks
run_health_checks() {
    log_section "Running Phase 3 Health Checks"
    
    if [ -f "$SCRIPT_DIR/health-check-phase3.sh" ]; then
        if "$SCRIPT_DIR/health-check-phase3.sh"; then
            log_success "Phase 3 health checks passed"
            return 0
        else
            log_error "Phase 3 health checks failed"
            return 1
        fi
    else
        log_warn "Phase 3 health check script not found, skipping"
        return 0
    fi
}

# Main deployment function
main() {
    echo -e "${BLUE}=========================================${NC}"
    echo -e "${BLUE}        Phase 3 Deployment Script       ${NC}"
    echo -e "${BLUE}=========================================${NC}"
    echo ""
    echo "Services to deploy:"
    echo "  - Oracle Service"
    echo "  - Storage Service"
    echo "  - CrossChain Service"
    echo "  - Proof of Reserve Service"
    echo "  - Randomness Service"
    echo "  - Fair Ordering Service"
    echo "  - TEE Host Service"
    echo ""
    
    # Verify prerequisites
    if ! "$SCRIPT_DIR/verify-prerequisites.sh"; then
        log_error "Prerequisites not met"
        exit 1
    fi
    
    # Check Phase 2 health
    check_phase2_health
    
    # Generate configuration files
    generate_compose_file
    update_gateway_config
    create_additional_configs
    
    # Deploy services
    deploy_services
    
    # Wait for services to be ready
    if ! wait_for_services; then
        log_error "Phase 3 deployment failed - services not ready"
        
        # Show logs for debugging
        echo ""
        echo "Service logs for debugging:"
        docker compose -f "$COMPOSE_FILE" logs --tail=50
        
        exit 1
    fi
    
    # Register services
    register_services
    
    # Run health checks
    if ! run_health_checks; then
        log_warn "Phase 3 deployment completed but health checks failed"
        echo ""
        echo "Check service logs:"
        echo "  docker compose -f $COMPOSE_FILE logs [service-name]"
        echo ""
        exit 1
    fi
    
    # Success
    log_success "Phase 3 deployment completed successfully!"
    echo ""
    echo "Phase 3 Services Status:"
    docker compose -f "$COMPOSE_FILE" ps
    echo ""
    echo "Access Points:"
    echo "  Oracle API:                   http://localhost:8088"
    echo "  Storage API:                  http://localhost:8089"
    echo "  CrossChain API:               http://localhost:8090"
    echo "  Proof of Reserve API:         http://localhost:8091"
    echo "  Randomness API:               http://localhost:8092"
    echo "  Fair Ordering API:            http://localhost:8093"
    echo "  TEE Host API:                 http://localhost:8094"
    echo ""
    echo "Next Steps:"
    echo "  1. Verify Phase 3 functionality with integration tests"
    echo "  2. Configure oracle data feeds"
    echo "  3. Set up cross-chain bridges"
    echo "  4. Proceed with Phase 4 deployment when ready"
    echo ""
    echo "✅ Phase 3 deployment complete - Ready for Phase 4"
}

# Run main function
main "$@"