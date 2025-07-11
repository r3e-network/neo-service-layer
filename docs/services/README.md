# Neo Service Layer Services

[![Microservices](https://img.shields.io/badge/architecture-microservices-green)](https://microservices.io/)
[![Intel SGX](https://img.shields.io/badge/Intel-SGX-blue)](https://software.intel.com/en-us/sgx)
[![Services](https://img.shields.io/badge/services-20+-brightgreen)](https://github.com/r3e-network/neo-service-layer)
[![Docker](https://img.shields.io/badge/docker-ready-blue)](https://www.docker.com/)

> **🚀 Production-Ready Microservices** - Comprehensive service catalog for the Neo blockchain ecosystem

The Neo Service Layer provides a comprehensive suite of **production-ready microservices** organized into specialized categories. All services leverage Intel SGX with Occlum LibOS enclaves to ensure security, privacy, and verifiability for enterprise blockchain applications.

## 🏗️ **Service Architecture Overview**

### **Microservices Deployment**

All services are deployed as independent microservices with:
- **🌐 API Gateway**: Unified entry point at `http://localhost:7000`
- **🔍 Service Discovery**: Consul-based service registry
- **📊 Observability**: Distributed tracing and metrics
- **🔒 Security**: JWT authentication and encrypted communication
- **⚡ Performance**: Load balancing and circuit breakers

### **Service Access Patterns**

**Production Access (API Gateway)**:
```bash
# Via API Gateway (Recommended)
curl http://localhost:7000/api/v1/storage/documents
curl http://localhost:7000/api/v1/keys/generate
curl http://localhost:7000/api/v1/notifications/send

# Health check
curl http://localhost:7000/health
```

**Development Access (Direct Services)**:
```bash
# Direct service access for development
curl http://localhost:8081/health  # Storage Service
curl http://localhost:8082/health  # Key Management Service
curl http://localhost:8083/health  # Notification Service
```

**Service Discovery**:
```bash
# Check service registry
curl http://localhost:8500/v1/catalog/services

# Service health status
curl http://localhost:8500/v1/health/service/storage-service
```

## 🔧 Core Services

Essential blockchain operations and infrastructure:

### 🔑 Key Management Service
**Port**: 8082 | **API Endpoint**: `/api/v1/keys`

Secure cryptographic key operations using Intel SGX hardware protection.

**Production Features:**
- **Hardware Security**: SGX-protected key generation and storage
- **Multi-Algorithm Support**: ECDSA-P256, Ed25519, RSA-2048/4096
- **Key Lifecycle**: Generation, rotation, revocation, archival
- **Compliance**: FIPS 140-2 Level 3 equivalent security
- **Audit Trail**: Complete cryptographic audit logs

**API Examples:**
```bash
# Generate new signing key
POST /api/v1/keys/generate
{
  "algorithm": "ECDSA_P256",
  "usage": ["sign", "verify"],
  "metadata": {"purpose": "smart-contract-signing"}
}

# Sign data
POST /api/v1/keys/{keyId}/sign
{
  "data": "transaction-data-to-sign",
  "algorithm": "SHA256withECDSA"
}
```

### 🗄️ Storage Service
**Port**: 8081 | **API Endpoint**: `/api/v1/storage`

Encrypted data storage with enterprise-grade security and performance.

**Production Features:**
- **Encryption**: AES-256-GCM with SGX-protected keys
- **Compression**: LZ4 compression for large datasets
- **Versioning**: Complete version control for all data
- **Access Control**: Fine-grained permissions and policies
- **Backup**: Automated backup with cross-region replication

**API Examples:**
```bash
# Store encrypted document
POST /api/v1/storage/documents
{
  "name": "smart-contract-v2.json",
  "content": "base64-encoded-content",
  "encryption": true,
  "metadata": {"version": "2.0", "network": "neo-n3"}
}

# Retrieve document
GET /api/v1/storage/documents/{documentId}
```

### 📧 Notification Service
**Port**: 8083 | **API Endpoint**: `/api/v1/notifications`

Multi-channel notification system with delivery guarantees.

**Production Features:**
- **Multi-Channel**: Email, SMS, Webhook, Push notifications
- **Reliability**: Guaranteed delivery with retry mechanisms
- **Templates**: Rich template system with variable substitution
- **Analytics**: Delivery tracking and engagement metrics
- **Rate Limiting**: Intelligent throttling and queue management

**API Examples:**
```bash
# Send notification
POST /api/v1/notifications/send
{
  "channel": "email",
  "recipient": "user@example.com",
  "subject": "Transaction Confirmed",
  "message": "Your transaction has been confirmed",
  "priority": "normal"
}

# Check delivery status
GET /api/v1/notifications/{notificationId}/status
```

### 🔮 Oracle Service  
**Port**: 8086 | **API Endpoint**: `/api/v1/oracle`

External data feeds with cryptographic integrity verification.

**Production Features:**
- **Data Sources**: Multiple premium data providers
- **Integrity**: Cryptographic proofs for all data feeds
- **Real-Time**: Sub-second price feeds and market data
- **Redundancy**: Multi-source aggregation and validation
- **Historical**: Complete historical data with analytics

**API Examples:**
```bash
# Request price data
POST /api/v1/oracle/request
{
  "source": "coinmarketcap",
  "query": {"symbol": "NEO", "convert": "USD"},
  "callback_url": "https://your-app.com/oracle-callback"
}

# Subscribe to price feed
POST /api/v1/oracle/subscriptions
{
  "feed_id": "crypto-prices",
  "symbols": ["NEO", "GAS"],
  "update_interval": "60s"
}
```

## ⚙️ Infrastructure Services

System management and blockchain integration:

### ⛓️ Cross-Chain Service
**Port**: 8087 | **API Endpoint**: `/api/v1/crosschain`

Multi-blockchain interoperability and asset transfers.

**Production Features:**
- **Multi-Network**: Neo N3, NeoX, Ethereum, Bitcoin support
- **Asset Bridging**: Secure cross-chain token transfers
- **Message Passing**: Smart contract calls across chains
- **Verification**: Cryptographic proof verification
- **Atomic Swaps**: Trustless cross-chain exchanges

**API Examples:**
```bash
# Bridge tokens between chains
POST /api/v1/crosschain/bridge
{
  "source_network": "neo-n3",
  "destination_network": "neo-x",
  "asset": "GAS",
  "amount": "50.0",
  "destination_address": "0x742d35cc6ab4b16c..."
}

# Check bridge status
GET /api/v1/crosschain/transactions/{transactionId}
```

### 📋 Configuration Service
**Port**: 8085 | **API Endpoint**: `/api/v1/configuration`

Dynamic configuration management with validation and versioning.

**Production Features:**
- **Dynamic Updates**: Real-time configuration changes
- **Validation**: Schema-based configuration validation
- **Versioning**: Complete configuration history
- **Environment Profiles**: Development, staging, production configs
- **Rollback**: Instant configuration rollback capabilities

**API Examples:**
```bash
# Get configuration value
GET /api/v1/configuration/app/database-url

# Update configuration
PUT /api/v1/configuration/app/database-url
{
  "value": "postgres://newhost/db",
  "environment": "production"
}
```

### 🤖 Smart Contracts Service
**Port**: 8088 | **API Endpoint**: `/api/v1/smart-contracts`

Smart contract deployment and lifecycle management.

**Production Features:**
- **Multi-Chain**: Neo N3 and NeoX deployment support
- **Template Library**: Audited smart contract templates
- **Gas Optimization**: Automatic gas estimation and optimization
- **Testing**: Automated contract testing and verification
- **Upgrades**: Secure contract upgrade mechanisms

**API Examples:**
```bash
# Deploy smart contract
POST /api/v1/smart-contracts/deploy
{
  "network": "neo-n3",
  "contract_code": "0x...",
  "parameters": {...},
  "metadata": {"version": "1.0"}
}

# Invoke contract method
POST /api/v1/smart-contracts/invoke
{
  "contract_hash": "0x...",
  "method": "transfer",
  "parameters": ["sender", "recipient", 100]
}
```

## 🤖 AI & Analytics Services

Machine learning and intelligent data analysis:

### 🧠 AI Pattern Recognition Service
**Port**: 8084 | **API Endpoint**: `/api/v1/ai/pattern-recognition`

Advanced AI-powered analysis and fraud detection.

**Production Features:**
- **Fraud Detection**: Real-time transaction monitoring
- **Behavioral Analysis**: User behavior pattern recognition
- **Anomaly Detection**: Statistical anomaly identification
- **Risk Scoring**: ML-based risk assessment models
- **Explainable AI**: Transparent decision explanations

**API Examples:**
```bash
# Analyze transaction patterns
POST /api/v1/ai/pattern-recognition/analyze
{
  "data_source": "blockchain-transactions",
  "time_range": {"start": "2024-01-01", "end": "2024-01-31"},
  "analysis_type": "fraud_detection",
  "parameters": {"min_amount": 1000}
}

# Get analysis results
GET /api/v1/ai/pattern-recognition/analysis/{analysisId}
```

### 📊 AI Prediction Service
**Port**: 8089 | **API Endpoint**: `/api/v1/ai/prediction`

Machine learning forecasting and predictive analytics.

**Production Features:**
- **Price Prediction**: Market price forecasting models
- **Trend Analysis**: Statistical trend identification
- **Sentiment Analysis**: Multi-source sentiment aggregation
- **Risk Modeling**: Portfolio and systemic risk models
- **Confidence Intervals**: Uncertainty quantification

**API Examples:**
```bash
# Request price prediction
POST /api/v1/ai/prediction/forecast
{
  "model": "market-prediction",
  "input_data": {"historical_prices": [...], "market_indicators": {...}},
  "prediction_horizon": "7d"
}

# Get prediction results
GET /api/v1/ai/prediction/forecasts/{forecastId}
```

## 🛡️ Security & Compliance Services

Enterprise-grade security and regulatory compliance:

### 🔐 Zero Knowledge Service
**Port**: 8090 | **API Endpoint**: `/api/v1/zk`

Privacy-preserving computations and proof systems.

**Production Features:**
- **zk-SNARKs**: Efficient zero-knowledge proofs
- **Private Transactions**: Confidential value transfers
- **Selective Disclosure**: Controlled information sharing
- **Circuit Compilation**: Custom proof circuit generation
- **Batch Verification**: Efficient proof aggregation

**API Examples:**
```bash
# Generate zero-knowledge proof
POST /api/v1/zk/prove
{
  "circuit": "range-proof",
  "private_inputs": {"value": 1000, "random": "0x..."},
  "public_inputs": {"min": 0, "max": 10000}
}

# Verify proof
POST /api/v1/zk/verify
{
  "proof": "0x...",
  "public_inputs": {...},
  "circuit": "range-proof"
}
```

### 🏛️ Compliance Service
**Port**: 8091 | **API Endpoint**: `/api/v1/compliance`

Regulatory compliance and risk management.

**Production Features:**
- **AML/KYC**: Automated identity verification
- **Sanctions Screening**: Real-time watchlist checking
- **Transaction Monitoring**: Suspicious activity detection
- **Risk Scoring**: ML-based risk assessment
- **Regulatory Reporting**: Automated compliance reports

**API Examples:**
```bash
# Perform KYC verification
POST /api/v1/compliance/kyc/verify
{
  "user_id": "user-123",
  "documents": ["id_card", "proof_of_address"],
  "personal_info": {"name": "John Doe", "dob": "1990-01-01"}
}

# Screen for sanctions
POST /api/v1/compliance/sanctions/screen
{
  "name": "John Doe",
  "address": "123 Main St",
  "country": "US"
}
```

## 📊 Monitoring & Operations Services

System observability and operational management:

### 📈 Monitoring Service
**Port**: 8092 | **API Endpoint**: `/api/v1/monitoring`

Comprehensive system monitoring and observability.

**Production Features:**
- **Real-Time Metrics**: Live system performance monitoring
- **Custom Dashboards**: Configurable monitoring views
- **Alerting**: Intelligent threshold-based alerts
- **SLA Tracking**: Service level agreement monitoring
- **Historical Analysis**: Long-term trend analysis

**API Examples:**
```bash
# Get system metrics
GET /api/v1/monitoring/metrics?service=all&timerange=1h

# Create custom alert
POST /api/v1/monitoring/alerts
{
  "name": "High CPU Usage",
  "condition": "cpu_usage > 80",
  "severity": "warning",
  "notification_channels": ["email", "slack"]
}
```

### 💚 Health Service
**Port**: 8093 | **API Endpoint**: `/api/v1/health`

Service health diagnostics and dependency monitoring.

**Production Features:**
- **Dependency Monitoring**: Service dependency health checks
- **Health Aggregation**: System-wide health status
- **Issue Detection**: Automated problem identification
- **Recovery Suggestions**: AI-powered recovery recommendations
- **Health Trends**: Predictive health analytics

**API Examples:**
```bash
# Check overall system health
GET /api/v1/health/system

# Check service dependencies
GET /api/v1/health/dependencies/{serviceName}

# Get health trend analysis
GET /api/v1/health/trends?period=7d
```

### ⚙️ Automation Service
**Port**: 8094 | **API Endpoint**: `/api/v1/automation`

Workflow automation and smart contract scheduling.

**Production Features:**
- **Workflow Engine**: Complex multi-step automation
- **Smart Scheduling**: Intelligent job scheduling
- **Gas Optimization**: Automated gas price management
- **Retry Logic**: Robust failure handling
- **Audit Trail**: Complete automation history

**API Examples:**
```bash
# Create automation workflow
POST /api/v1/automation/workflows
{
  "name": "daily-backup",
  "trigger": {"type": "schedule", "cron": "0 2 * * *"},
  "steps": [
    {"action": "backup_database"},
    {"action": "verify_backup"},
    {"action": "notify_completion"}
  ]
}

# Execute workflow
POST /api/v1/automation/workflows/{workflowId}/execute
```

## 🏗️ Platform Services

Core platform infrastructure and service management:

### 🔍 Service Discovery
**Consul-based** | **Port**: 8500 | **UI**: `http://localhost:8500`

Dynamic service registry and health monitoring.

**Production Features:**
- **Service Registration**: Automatic service discovery
- **Health Checks**: Continuous service health monitoring
- **Load Balancing**: Intelligent traffic distribution
- **Configuration**: Distributed configuration management
- **Multi-Datacenter**: Cross-datacenter service mesh

### 🌐 API Gateway
**YARP-based** | **Port**: 7000 | **Endpoint**: `http://localhost:7000`

Unified API entry point with advanced routing capabilities.

**Production Features:**
- **Request Routing**: Intelligent service routing
- **Authentication**: JWT and API key authentication
- **Rate Limiting**: DDoS protection and fair usage
- **Circuit Breakers**: Fault tolerance and resilience
- **Load Balancing**: High availability service access

### 📊 Observability Stack
**Integrated** | **Jaeger**: 16686 | **Grafana**: 3000

Complete observability and monitoring solution.

**Production Features:**
- **Distributed Tracing**: Request flow visualization
- **Metrics Collection**: Prometheus-based metrics
- **Log Aggregation**: Centralized log management
- **Dashboards**: Real-time system dashboards
- **Alerting**: Intelligent alert management

## 🚀 Service Deployment Status

### ✅ **Production Ready Services**

| Service Category | Services Count | Status | Port Range |
|------------------|----------------|--------|------------|
| **Core Services** | 4 | ✅ Ready | 8081-8084 |
| **Infrastructure** | 3 | ✅ Ready | 8085-8087 |
| **AI & Analytics** | 2 | ✅ Ready | 8088-8089 |
| **Security** | 2 | ✅ Ready | 8090-8091 |
| **Operations** | 3 | ✅ Ready | 8092-8094 |
| **Platform** | 3 | ✅ Ready | 7000, 8500, 16686 |

### 🔧 **Quick Service Access**

**API Gateway (Recommended)**:
```bash
# System health
curl http://localhost:7000/health

# Service-specific endpoints
curl http://localhost:7000/api/v1/storage/documents
curl http://localhost:7000/api/v1/keys/generate
curl http://localhost:7000/api/v1/notifications/send
```

**Direct Service Access (Development)**:
```bash
# Core services
curl http://localhost:8081/health  # Storage
curl http://localhost:8082/health  # Key Management
curl http://localhost:8083/health  # Notification
curl http://localhost:8086/health  # Oracle

# AI services
curl http://localhost:8084/health  # Pattern Recognition
curl http://localhost:8089/health  # Prediction

# Security services
curl http://localhost:8090/health  # Zero Knowledge
curl http://localhost:8091/health  # Compliance
```

**Service Discovery**:
```bash
# View all services
curl http://localhost:8500/v1/catalog/services

# Check service health
curl http://localhost:8500/v1/health/service/storage-service

# Access Consul UI
open http://localhost:8500
```

**Observability**:
```bash
# Jaeger tracing UI
open http://localhost:16686

# Grafana dashboards
open http://localhost:3000

# Prometheus metrics
open http://localhost:9090
```

## 🛡️ Security & Intel SGX Integration

All services implement enterprise-grade security with Intel SGX hardware protection:

### 🔒 **Intel SGX + Occlum LibOS**
- **Hardware Security**: SGX enclaves protect sensitive operations
- **Trusted Execution**: Confidential computing for critical workloads
- **Remote Attestation**: Cryptographic proof of enclave integrity
- **Sealed Storage**: Hardware-protected data persistence
- **Memory Protection**: Real-time memory encryption and isolation

### 🔐 **Security Features**
- **End-to-End Encryption**: All data encrypted in transit and at rest
- **Cryptographic Verification**: All operations cryptographically verifiable
- **Audit Trails**: Comprehensive logging and compliance support
- **Zero-Trust Architecture**: Never trust, always verify
- **Compliance Ready**: GDPR, SOX, FIPS 140-2 Level 3 equivalent

## 🚀 **Getting Started with Services**

### **Option 1: Complete Microservices Stack (Recommended)**

```bash
# 1. Clone and start complete stack
git clone https://github.com/r3e-network/neo-service-layer.git
cd neo-service-layer

# 2. Start all microservices
docker-compose -f docker-compose.microservices-complete.yml up -d

# 3. Verify services
curl http://localhost:7000/health
curl http://localhost:8500/v1/catalog/services
```

### **Option 2: Individual Service Development**

```bash
# Start infrastructure
docker-compose up -d

# Run specific services
dotnet run --project src/Services/NeoServiceLayer.Services.Storage/
dotnet run --project src/Services/NeoServiceLayer.Services.KeyManagement/
dotnet run --project src/Services/NeoServiceLayer.Services.Notification/
```

### **Option 3: API Gateway Only**

```bash
# Start API Gateway with service discovery
docker-compose -f docker-compose.microservices.yml up -d

# Access all services through gateway
curl http://localhost:7000/api/v1/storage/documents
curl http://localhost:7000/api/v1/keys/generate
```

## 📊 **Service Framework**

### **Common Service Patterns**
All services implement standardized patterns:
- **Health Checks**: `/health` endpoint with dependency monitoring
- **Metrics**: Prometheus metrics at `/metrics` endpoint  
- **Configuration**: Environment-based configuration management
- **Logging**: Structured logging with correlation IDs
- **Authentication**: JWT-based security with API key fallback

### **Service Lifecycle**
1. **Registration**: Automatic service discovery registration
2. **Health Monitoring**: Continuous health and dependency checks
3. **Load Balancing**: Intelligent traffic distribution
4. **Circuit Breaking**: Fault tolerance and resilience
5. **Graceful Shutdown**: Clean service termination

### **Development Guidelines**
- **Interface-First**: Well-defined service contracts
- **Testing**: Comprehensive unit, integration, and performance tests
- **Documentation**: Complete OpenAPI/Swagger documentation
- **Containerization**: Docker-ready with optimized images
- **Observability**: Built-in tracing, metrics, and logging

## 📚 **Documentation & Resources**

### **Core Documentation**
- **[Deployment Guide](../deployment/DEPLOYMENT_GUIDE.md)** - Production deployment
- **[API Reference](../api/README.md)** - Complete API documentation
- **[SDK Guide](../../src/SDK/NeoServiceLayer.SDK/README.md)** - Client SDK usage
- **[Quick Start](../deployment/QUICK_START.md)** - 5-minute setup guide

### **Development Resources**
- **[Service Framework](../architecture/service-framework.md)** - Service development patterns
- **[Testing Guide](../testing/README.md)** - Testing strategies and tools
- **[Security Guide](../security/README.md)** - Security implementation
- **[Contributing](../../CONTRIBUTING.md)** - Contribution guidelines

### **Operations Guides**
- **[Monitoring Setup](../monitoring/README.md)** - Observability configuration
- **[Troubleshooting](../troubleshooting/README.md)** - Common issues and solutions
- **[Performance Tuning](../performance/README.md)** - Optimization guidelines

## 🎯 **Next Steps**

### **For Developers**
1. **Explore APIs**: Start with the [API Reference](../api/README.md)
2. **Try the SDK**: Use the [.NET SDK](../../src/SDK/NeoServiceLayer.SDK/README.md)
3. **Build Applications**: Follow the [Quick Start Guide](../deployment/QUICK_START.md)

### **For Operations**
1. **Deploy Services**: Follow the [Deployment Guide](../deployment/DEPLOYMENT_GUIDE.md)
2. **Monitor Health**: Set up [Monitoring](../monitoring/README.md)
3. **Scale Services**: Configure load balancing and auto-scaling

### **For Contributors**
1. **Review Code**: Understand the architecture and patterns
2. **Add Services**: Follow the service development framework
3. **Improve Documentation**: Help enhance the documentation

---

**🚀 The Neo Service Layer provides a complete, production-ready microservices platform for blockchain applications. Start building the future of decentralized systems today!**

**Built with ❤️ for the Neo Ecosystem**
