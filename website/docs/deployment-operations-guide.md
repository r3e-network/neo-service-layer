# Neo Service Layer Deployment & Operations Guide

Version 1.5.0 | Last Updated: January 30, 2024

## Table of Contents

1. [Introduction](#1-introduction)
2. [System Requirements](#2-system-requirements)
3. [Pre-Deployment Checklist](#3-pre-deployment-checklist)
4. [Deployment Architecture](#4-deployment-architecture)
5. [Installation Guide](#5-installation-guide)
6. [Configuration Management](#6-configuration-management)
7. [Security Hardening](#7-security-hardening)
8. [Intel SGX Setup](#8-intel-sgx-setup)
9. [Database Setup](#9-database-setup)
10. [Container Orchestration](#10-container-orchestration)
11. [Monitoring & Observability](#11-monitoring--observability)
12. [Backup & Recovery](#12-backup--recovery)
13. [Scaling & Performance](#13-scaling--performance)
14. [Maintenance Procedures](#14-maintenance-procedures)
15. [Troubleshooting](#15-troubleshooting)
16. [Disaster Recovery](#16-disaster-recovery)
17. [Compliance & Auditing](#17-compliance--auditing)
18. [Appendices](#18-appendices)

---

## 1. Introduction

### 1.1 Purpose

This guide provides comprehensive instructions for deploying, configuring, and operating the Neo Service Layer in production environments. It covers infrastructure setup, security hardening, monitoring, and maintenance procedures.

### 1.2 Audience

- DevOps Engineers
- System Administrators
- Infrastructure Engineers
- Security Engineers
- Site Reliability Engineers (SREs)

### 1.3 Deployment Models

| Model | Description | Use Case |
|-------|-------------|----------|
| **On-Premises** | Full control, hardware security modules | High security requirements |
| **Cloud (AWS/Azure/GCP)** | Scalable, managed services | Standard deployments |
| **Hybrid** | Mix of on-prem and cloud | Compliance + scalability |
| **Edge** | Distributed edge nodes | Low latency requirements |

---

## 2. System Requirements

### 2.1 Hardware Requirements

#### 2.1.1 Minimum Requirements (Development)

| Component | Specification |
|-----------|---------------|
| CPU | 8 cores (Intel Xeon E3 or equivalent) |
| RAM | 32 GB DDR4 |
| Storage | 500 GB NVMe SSD |
| Network | 1 Gbps |
| SGX Support | Intel SGX v2 (optional) |

#### 2.1.2 Recommended Requirements (Production)

| Component | Specification |
|-----------|---------------|
| CPU | 32 cores (Intel Xeon Gold 6248R or better) |
| RAM | 128 GB DDR4 ECC |
| Storage | 2 TB NVMe SSD RAID 10 |
| Network | 10 Gbps redundant |
| SGX Support | Intel SGX v2 (required for secure services) |

#### 2.1.3 High-Performance Requirements

| Component | Specification |
|-----------|---------------|
| CPU | 64+ cores (Intel Xeon Platinum 8380) |
| RAM | 512 GB DDR4 ECC |
| Storage | 8 TB NVMe SSD RAID 10 |
| Network | 25 Gbps redundant |
| SGX Support | Intel SGX v2 with 128GB EPC |

### 2.2 Software Requirements

| Software | Version | Purpose |
|----------|---------|---------|
| Ubuntu Server | 22.04 LTS | Operating System |
| Docker | 24.0+ | Container Runtime |
| Kubernetes | 1.28+ | Container Orchestration |
| .NET | 9.0 | Application Runtime |
| PostgreSQL | 15+ | Primary Database |
| Redis | 7.0+ | Caching & Sessions |
| Nginx | 1.24+ | Load Balancer |
| Prometheus | 2.45+ | Metrics Collection |
| Grafana | 10.0+ | Metrics Visualization |
| Elasticsearch | 8.9+ | Log Storage |

### 2.3 Network Requirements

| Port | Protocol | Service | Direction |
|------|----------|---------|-----------|
| 80 | TCP | HTTP (redirects) | Inbound |
| 443 | TCP | HTTPS API | Inbound |
| 5000 | TCP | API Internal | Internal |
| 6379 | TCP | Redis | Internal |
| 5432 | TCP | PostgreSQL | Internal |
| 9090 | TCP | Prometheus | Internal |
| 3000 | TCP | Grafana | Internal |
| 9200 | TCP | Elasticsearch | Internal |
| 10333 | TCP | Neo P2P | Outbound |
| 8545 | TCP | Neo X RPC | Outbound |

---

## 3. Pre-Deployment Checklist

### 3.1 Infrastructure Checklist

- [ ] Servers provisioned with required specifications
- [ ] Network connectivity verified
- [ ] DNS records configured
- [ ] SSL certificates obtained
- [ ] Firewall rules configured
- [ ] Load balancer configured
- [ ] Storage volumes provisioned
- [ ] Backup infrastructure ready

### 3.2 Security Checklist

- [ ] Security audit completed
- [ ] Penetration testing performed
- [ ] Access control lists defined
- [ ] SSH keys generated and distributed
- [ ] Secrets management system configured
- [ ] SGX attestation service configured
- [ ] Security monitoring enabled
- [ ] Incident response plan documented

### 3.3 Software Checklist

- [ ] All software dependencies installed
- [ ] Container images built and scanned
- [ ] Database schemas created
- [ ] Configuration files prepared
- [ ] Environment variables documented
- [ ] Monitoring dashboards created
- [ ] Alert rules configured
- [ ] Runbooks documented

### 3.4 Compliance Checklist

- [ ] Data residency requirements verified
- [ ] Encryption at rest configured
- [ ] Encryption in transit enabled
- [ ] Audit logging enabled
- [ ] Compliance certifications obtained
- [ ] Privacy policy updated
- [ ] Terms of service updated
- [ ] DPA agreements signed

---

## 4. Deployment Architecture

### 4.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Load Balancer (HA)                        │
│                    Nginx / AWS ALB / Azure LB                    │
└─────────────────┬───────────────────────────┬───────────────────┘
                  │                           │
         ┌────────▼────────┐         ┌────────▼────────┐
         │   API Gateway   │         │   API Gateway   │
         │   (Primary)     │         │  (Secondary)    │
         └────────┬────────┘         └────────┬────────┘
                  │                           │
    ┌─────────────▼───────────────────────────▼─────────────┐
    │                  Kubernetes Cluster                     │
    ├─────────────────────────────────────────────────────────┤
    │  ┌─────────────┐  ┌──────────────┐  ┌──────────────┐  │
    │  │   Service   │  │   Service    │  │   Service    │  │
    │  │   Pods      │  │   Pods       │  │   Pods       │  │
    │  └─────────────┘  └──────────────┘  └──────────────┘  │
    │                                                         │
    │  ┌─────────────────────────────────────────────────┐  │
    │  │              Intel SGX Enclaves                  │  │
    │  └─────────────────────────────────────────────────┘  │
    └─────────────────────────────────────────────────────────┘
                  │                           │
    ┌─────────────▼─────────┐   ┌────────────▼────────────┐
    │   PostgreSQL Cluster  │   │    Redis Cluster        │
    │   (Primary-Replica)   │   │   (Master-Slave)        │
    └───────────────────────┘   └─────────────────────────┘
```

### 4.2 Network Architecture

```
Internet
    │
    ▼
┌─────────────────────────────────────────┐
│          WAF (Web Application Firewall) │
└─────────────────────┬───────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────┐
│         Public Subnet (DMZ)             │
│  ┌────────────┐    ┌────────────┐      │
│  │   Nginx    │    │   Nginx    │      │
│  │   LB 1     │    │   LB 2     │      │
│  └────────────┘    └────────────┘      │
└─────────────────────┬───────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────┐
│        Application Subnet               │
│  ┌─────────────────────────────┐       │
│  │   Kubernetes Worker Nodes    │       │
│  └─────────────────────────────┘       │
└─────────────────────┬───────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────┐
│         Database Subnet                 │
│  ┌──────────┐    ┌──────────┐         │
│  │PostgreSQL│    │  Redis   │         │
│  └──────────┘    └──────────┘         │
└─────────────────────────────────────────┘
```

### 4.3 Service Distribution

| Service Group | Nodes | Replicas | Resources |
|---------------|-------|----------|-----------|
| Core Services | 3-5 | 3 | 4 CPU, 8GB RAM |
| DeFi Services | 3-5 | 3 | 8 CPU, 16GB RAM |
| Advanced Services | 2-3 | 2 | 16 CPU, 32GB RAM |
| Infrastructure | 2-3 | 2 | 2 CPU, 4GB RAM |

---

## 5. Installation Guide

### 5.1 Base System Setup

#### 5.1.1 Update System

```bash
#!/bin/bash
# Update Ubuntu system
sudo apt update && sudo apt upgrade -y

# Install essential packages
sudo apt install -y \
    build-essential \
    git \
    curl \
    wget \
    vim \
    htop \
    net-tools \
    software-properties-common \
    apt-transport-https \
    ca-certificates \
    gnupg \
    lsb-release
```

#### 5.1.2 Configure System Limits

```bash
# /etc/security/limits.conf
* soft nofile 65536
* hard nofile 65536
* soft nproc 32768
* hard nproc 32768

# /etc/sysctl.conf
net.core.somaxconn = 65535
net.ipv4.tcp_max_syn_backlog = 65535
net.ipv4.ip_local_port_range = 1024 65535
net.ipv4.tcp_tw_reuse = 1
net.ipv4.tcp_fin_timeout = 15
net.core.netdev_max_backlog = 65535
vm.max_map_count = 262144
```

### 5.2 Docker Installation

```bash
#!/bin/bash
# Install Docker
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg

echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Configure Docker
sudo usermod -aG docker $USER

# Docker daemon configuration
sudo tee /etc/docker/daemon.json << EOF
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "100m",
    "max-file": "5"
  },
  "storage-driver": "overlay2",
  "storage-opts": [
    "overlay2.override_kernel_check=true"
  ],
  "exec-opts": ["native.cgroupdriver=systemd"],
  "features": {
    "buildkit": true
  },
  "experimental": true
}
EOF

sudo systemctl restart docker
```

### 5.3 Kubernetes Installation

```bash
#!/bin/bash
# Install Kubernetes
curl -fsSLo /usr/share/keyrings/kubernetes-archive-keyring.gpg https://packages.cloud.google.com/apt/doc/apt-key.gpg

echo "deb [signed-by=/usr/share/keyrings/kubernetes-archive-keyring.gpg] https://apt.kubernetes.io/ kubernetes-xenial main" | sudo tee /etc/apt/sources.list.d/kubernetes.list

sudo apt update
sudo apt install -y kubelet kubeadm kubectl
sudo apt-mark hold kubelet kubeadm kubectl

# Initialize Kubernetes cluster (master node)
sudo kubeadm init --pod-network-cidr=10.244.0.0/16

# Configure kubectl
mkdir -p $HOME/.kube
sudo cp -i /etc/kubernetes/admin.conf $HOME/.kube/config
sudo chown $(id -u):$(id -g) $HOME/.kube/config

# Install Flannel network plugin
kubectl apply -f https://raw.githubusercontent.com/coreos/flannel/master/Documentation/kube-flannel.yml

# Install metrics server
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml
```

### 5.4 Neo Service Layer Deployment

#### 5.4.1 Clone Repository

```bash
git clone https://github.com/neo-service-layer/neo-service-layer.git
cd neo-service-layer
```

#### 5.4.2 Build Container Images

```bash
#!/bin/bash
# Build all service images
docker build -f Dockerfile -t neo-service-layer:latest .

# Tag for registry
docker tag neo-service-layer:latest registry.example.com/neo-service-layer:latest

# Push to registry
docker push registry.example.com/neo-service-layer:latest
```

#### 5.4.3 Deploy to Kubernetes

```bash
# Create namespace
kubectl create namespace neo-service-layer

# Create secrets
kubectl create secret generic neo-service-layer-secrets \
  --from-file=jwt-secret=./secrets/jwt-secret.txt \
  --from-file=db-password=./secrets/db-password.txt \
  --namespace=neo-service-layer

# Apply configurations
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/persistent-volumes.yaml
kubectl apply -f k8s/services.yaml
kubectl apply -f k8s/deployments.yaml
kubectl apply -f k8s/ingress.yaml
```

---

## 6. Configuration Management

### 6.1 Environment Configuration

#### 6.1.1 Production Configuration

```yaml
# k8s/configmap-production.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: neo-service-layer-config
  namespace: neo-service-layer
data:
  appsettings.Production.json: |
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft": "Warning",
          "System": "Warning"
        }
      },
      "ConnectionStrings": {
        "DefaultConnection": "Host=postgres-service;Database=neoservicelayer;Username=neoservice;Password=$(DB_PASSWORD);Maximum Pool Size=100;",
        "Redis": "redis-service:6379,abortConnect=false,ssl=false,password=$(REDIS_PASSWORD)"
      },
      "Jwt": {
        "Issuer": "neo-service-layer",
        "Audience": "https://api.neo-service-layer.io",
        "ExpiryInDays": 1,
        "RefreshExpiryInDays": 7
      },
      "RateLimiting": {
        "EnableRateLimiting": true,
        "PermitLimit": 100,
        "Window": "00:01:00",
        "QueueProcessingOrder": "OldestFirst",
        "QueueLimit": 50
      },
      "Services": {
        "EnableAllServices": true,
        "ServiceTimeoutSeconds": 30,
        "MaxConcurrentRequests": 1000
      },
      "Blockchain": {
        "Neo": {
          "MainNet": "https://mainnet1.neo.coz.io:443",
          "TestNet": "https://testnet1.neo.coz.io:443"
        },
        "NeoX": {
          "MainNet": "https://mainnet.neox.org",
          "TestNet": "https://testnet.neox.org"
        }
      },
      "Security": {
        "EnableSgx": true,
        "RequireHttps": true,
        "EnableAuditLogging": true,
        "AuditRetentionDays": 2555
      }
    }
```

### 6.2 Secret Management

#### 6.2.1 Azure Key Vault Integration

```csharp
// Program.cs
builder.Configuration.AddAzureKeyVault(
    $"https://{keyVaultName}.vault.azure.net/",
    new DefaultAzureCredential(),
    new KeyVaultSecretManager());

// Retrieve secrets
var jwtSecret = configuration["JwtSecret"];
var dbPassword = configuration["DatabasePassword"];
```

#### 6.2.2 Kubernetes Secrets

```yaml
# k8s/secrets.yaml
apiVersion: v1
kind: Secret
metadata:
  name: neo-service-layer-secrets
  namespace: neo-service-layer
type: Opaque
data:
  jwt-secret: <base64-encoded-secret>
  db-password: <base64-encoded-password>
  redis-password: <base64-encoded-password>
  sgx-seal-key: <base64-encoded-key>
```

### 6.3 Configuration Hot Reload

```csharp
// Enable configuration hot reload
public class ConfigurationService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationService> _logger;
    private FileSystemWatcher _watcher;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var configPath = Path.GetDirectoryName(_configuration["ConfigPath"]);
        _watcher = new FileSystemWatcher(configPath)
        {
            Filter = "*.json",
            NotifyFilter = NotifyFilters.LastWrite
        };

        _watcher.Changed += OnConfigurationChanged;
        _watcher.EnableRaisingEvents = true;

        return Task.CompletedTask;
    }

    private void OnConfigurationChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation($"Configuration file {e.Name} changed, reloading...");
        // Reload configuration logic
    }
}
```

---

## 7. Security Hardening

### 7.1 Network Security

#### 7.1.1 Firewall Configuration

```bash
#!/bin/bash
# UFW firewall configuration
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Allow SSH (restrict source IPs in production)
sudo ufw allow from 10.0.0.0/8 to any port 22

# Allow HTTPS
sudo ufw allow 443/tcp

# Allow Kubernetes API
sudo ufw allow 6443/tcp

# Internal services (from specific subnets only)
sudo ufw allow from 10.244.0.0/16 to any port 5432  # PostgreSQL
sudo ufw allow from 10.244.0.0/16 to any port 6379  # Redis

# Enable firewall
sudo ufw enable
```

#### 7.1.2 Network Policies

```yaml
# k8s/network-policy.yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: neo-service-layer-netpol
  namespace: neo-service-layer
spec:
  podSelector:
    matchLabels:
      app: neo-service-layer
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: neo-service-layer
    - podSelector:
        matchLabels:
          app: nginx-ingress
    ports:
    - protocol: TCP
      port: 5000
  egress:
  - to:
    - namespaceSelector:
        matchLabels:
          name: neo-service-layer
    ports:
    - protocol: TCP
      port: 5432
    - protocol: TCP
      port: 6379
  - to:
    - namespaceSelector: {}
    ports:
    - protocol: TCP
      port: 53
    - protocol: UDP
      port: 53
```

### 7.2 Application Security

#### 7.2.1 Security Headers

```csharp
// SecurityHeadersMiddleware.cs
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self' wss://;");
        
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Add("Strict-Transport-Security", 
                "max-age=31536000; includeSubDomains; preload");
        }

        await _next(context);
    }
}
```

#### 7.2.2 Input Validation

```csharp
// Input validation example
public class StorageRequestValidator : AbstractValidator<StorageRequest>
{
    public StorageRequestValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(256)
            .Matches(@"^[a-zA-Z0-9:_-]+$")
            .WithMessage("Key must be alphanumeric with colons, underscores, and hyphens only");

        RuleFor(x => x.Value)
            .NotNull()
            .Must(BeValidJson)
            .WithMessage("Value must be valid JSON");

        RuleFor(x => x.Size)
            .LessThanOrEqualTo(1048576)
            .WithMessage("Data size cannot exceed 1MB");
    }

    private bool BeValidJson(object value)
    {
        try
        {
            JsonSerializer.Serialize(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

### 7.3 Access Control

#### 7.3.1 RBAC Configuration

```yaml
# k8s/rbac.yaml
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  namespace: neo-service-layer
  name: neo-service-layer-role
rules:
- apiGroups: [""]
  resources: ["pods", "services", "configmaps", "secrets"]
  verbs: ["get", "list", "watch"]
- apiGroups: ["apps"]
  resources: ["deployments", "replicasets"]
  verbs: ["get", "list", "watch"]

---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: neo-service-layer-rolebinding
  namespace: neo-service-layer
subjects:
- kind: ServiceAccount
  name: neo-service-layer-sa
  namespace: neo-service-layer
roleRef:
  kind: Role
  name: neo-service-layer-role
  apiGroup: rbac.authorization.k8s.io
```

### 7.4 Secrets Rotation

```bash
#!/bin/bash
# Automated secret rotation script

# Rotate JWT secret
NEW_JWT_SECRET=$(openssl rand -base64 64)
kubectl create secret generic jwt-secret \
  --from-literal=value=$NEW_JWT_SECRET \
  --dry-run=client -o yaml | kubectl apply -f -

# Rotate database password
NEW_DB_PASSWORD=$(openssl rand -base64 32)
kubectl create secret generic db-password \
  --from-literal=value=$NEW_DB_PASSWORD \
  --dry-run=client -o yaml | kubectl apply -f -

# Update database password
PGPASSWORD=$OLD_DB_PASSWORD psql -h postgres-service -U postgres -c "ALTER USER neoservice PASSWORD '$NEW_DB_PASSWORD';"

# Restart pods to pick up new secrets
kubectl rollout restart deployment/neo-service-layer -n neo-service-layer
```

---

## 8. Intel SGX Setup

### 8.1 SGX Prerequisites

#### 8.1.1 Hardware Verification

```bash
#!/bin/bash
# Check SGX support
cpuid | grep -i sgx

# Check SGX status in BIOS
sudo apt install cpuid msr-tools
sudo modprobe msr
sudo rdmsr 0x3a

# Expected output: 0x1 (SGX enabled)
```

#### 8.1.2 SGX Driver Installation

```bash
#!/bin/bash
# Install Intel SGX driver
wget https://download.01.org/intel-sgx/latest/linux-latest/distro/ubuntu22.04-server/sgx_linux_x64_driver_2.18.100.3.bin
chmod +x sgx_linux_x64_driver_2.18.100.3.bin
sudo ./sgx_linux_x64_driver_2.18.100.3.bin

# Verify installation
ls -la /dev/sgx*
# Should show: /dev/sgx_enclave and /dev/sgx_provision
```

### 8.2 SGX SDK Installation

```bash
#!/bin/bash
# Install SGX SDK dependencies
sudo apt-get install -y build-essential ocaml ocamlbuild automake autoconf libtool wget python3 libssl-dev git cmake perl

# Install SGX SDK
wget https://download.01.org/intel-sgx/latest/linux-latest/distro/ubuntu22.04-server/sgx_linux_x64_sdk_2.19.100.3.bin
chmod +x sgx_linux_x64_sdk_2.19.100.3.bin
sudo ./sgx_linux_x64_sdk_2.19.100.3.bin --prefix=/opt/intel

# Source SGX SDK environment
source /opt/intel/sgxsdk/environment
```

### 8.3 Occlum Configuration

```bash
#!/bin/bash
# Install Occlum
docker pull occlum/occlum:latest-ubuntu20.04

# Create Occlum instance
mkdir -p /opt/neo-service-layer/sgx
cd /opt/neo-service-layer/sgx
occlum init

# Configure Occlum.json
cat > Occlum.json << EOF
{
  "version": "0.29.0",
  "target": "SGX",
  "mode": "HW",
  "entrypoint": "/bin/NeoServiceLayer.Api",
  "env": {
    "default": [
      "ASPNETCORE_ENVIRONMENT=Production",
      "ASPNETCORE_URLS=http://+:5000"
    ]
  },
  "resource_limits": {
    "kernel_space_heap_size": "256MB",
    "user_space_heap_size": "2GB",
    "max_num_of_threads": 128
  },
  "process": {
    "default_stack_size": "8MB",
    "default_heap_size": "256MB",
    "default_mmap_size": "512MB"
  }
}
EOF

# Build Occlum image
occlum build
```

### 8.4 Enclave Service Configuration

```csharp
// EnclaveConfiguration.cs
public class EnclaveConfiguration
{
    public bool EnableSgx { get; set; }
    public string EnclavePath { get; set; }
    public string SealingKeyPath { get; set; }
    public int EnclaveHeapSize { get; set; }
    public int EnclaveStackSize { get; set; }
    public int MaxEnclaveThreads { get; set; }
}

// Startup.cs
services.Configure<EnclaveConfiguration>(configuration.GetSection("Enclave"));

if (enclaveConfig.EnableSgx)
{
    services.AddSingleton<IEnclaveProvider, SgxEnclaveProvider>();
    services.AddHostedService<EnclaveAttestationService>();
}
```

---

## 9. Database Setup

### 9.1 PostgreSQL Cluster

#### 9.1.1 Primary-Replica Setup

```bash
#!/bin/bash
# Install PostgreSQL 15
sudo sh -c 'echo "deb http://apt.postgresql.org/pub/repos/apt $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list'
wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | sudo apt-key add -
sudo apt update
sudo apt install -y postgresql-15 postgresql-client-15 postgresql-contrib-15

# Configure primary server
sudo -u postgres psql << EOF
CREATE USER replicator WITH REPLICATION ENCRYPTED PASSWORD 'replication_password';
CREATE DATABASE neoservicelayer;
CREATE USER neoservice WITH ENCRYPTED PASSWORD 'neoservice_password';
GRANT ALL PRIVILEGES ON DATABASE neoservicelayer TO neoservice;
EOF

# postgresql.conf (primary)
cat >> /etc/postgresql/15/main/postgresql.conf << EOF
listen_addresses = '*'
wal_level = replica
max_wal_senders = 10
wal_keep_size = 1GB
hot_standby = on
archive_mode = on
archive_command = 'test ! -f /var/lib/postgresql/15/archive/%f && cp %p /var/lib/postgresql/15/archive/%f'
EOF

# pg_hba.conf (primary)
echo "host replication replicator 10.0.0.0/8 md5" >> /etc/postgresql/15/main/pg_hba.conf
echo "host all all 10.0.0.0/8 md5" >> /etc/postgresql/15/main/pg_hba.conf
```

#### 9.1.2 Database Schema

```sql
-- Create schema
CREATE SCHEMA IF NOT EXISTS neo_service_layer;
SET search_path TO neo_service_layer;

-- Storage service tables
CREATE TABLE storage_data (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_address VARCHAR(42) NOT NULL,
    key VARCHAR(256) NOT NULL,
    value JSONB NOT NULL,
    encrypted BOOLEAN DEFAULT false,
    access_level VARCHAR(20) DEFAULT 'private',
    size_bytes INTEGER NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    accessed_count INTEGER DEFAULT 0,
    last_accessed TIMESTAMP WITH TIME ZONE,
    expires_at TIMESTAMP WITH TIME ZONE,
    metadata JSONB,
    UNIQUE(user_address, key)
);

CREATE INDEX idx_storage_user_address ON storage_data(user_address);
CREATE INDEX idx_storage_key ON storage_data(key);
CREATE INDEX idx_storage_created_at ON storage_data(created_at);
CREATE INDEX idx_storage_expires_at ON storage_data(expires_at) WHERE expires_at IS NOT NULL;

-- Oracle service tables
CREATE TABLE oracle_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id VARCHAR(100) UNIQUE NOT NULL,
    user_address VARCHAR(42) NOT NULL,
    url TEXT NOT NULL,
    json_path VARCHAR(500),
    callback_contract VARCHAR(42),
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    result JSONB,
    error TEXT,
    gas_used NUMERIC(20, 8),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP WITH TIME ZONE,
    metadata JSONB
);

CREATE INDEX idx_oracle_request_id ON oracle_requests(request_id);
CREATE INDEX idx_oracle_user_address ON oracle_requests(user_address);
CREATE INDEX idx_oracle_status ON oracle_requests(status);
CREATE INDEX idx_oracle_created_at ON oracle_requests(created_at);

-- Audit log table
CREATE TABLE audit_logs (
    id BIGSERIAL PRIMARY KEY,
    event_type VARCHAR(50) NOT NULL,
    user_address VARCHAR(42),
    service_name VARCHAR(50) NOT NULL,
    method_name VARCHAR(100) NOT NULL,
    request_id VARCHAR(100),
    correlation_id VARCHAR(100),
    ip_address INET,
    user_agent TEXT,
    request_data JSONB,
    response_data JSONB,
    status_code INTEGER,
    error_message TEXT,
    execution_time_ms INTEGER,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_audit_event_type ON audit_logs(event_type);
CREATE INDEX idx_audit_user_address ON audit_logs(user_address);
CREATE INDEX idx_audit_service ON audit_logs(service_name, method_name);
CREATE INDEX idx_audit_created_at ON audit_logs(created_at);
CREATE INDEX idx_audit_request_id ON audit_logs(request_id);

-- Partitioning for audit logs (monthly)
CREATE TABLE audit_logs_2024_01 PARTITION OF audit_logs
    FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');
    
CREATE TABLE audit_logs_2024_02 PARTITION OF audit_logs
    FOR VALUES FROM ('2024-02-01') TO ('2024-03-01');

-- Performance optimization
VACUUM ANALYZE;
```

### 9.2 Redis Cluster

#### 9.2.1 Redis Configuration

```bash
# /etc/redis/redis.conf
bind 0.0.0.0
protected-mode yes
port 6379
tcp-backlog 511
timeout 0
tcp-keepalive 300
daemonize yes
supervised systemd
pidfile /var/run/redis/redis-server.pid
loglevel notice
logfile /var/log/redis/redis-server.log
databases 16
always-show-logo no

# Persistence
save 900 1
save 300 10
save 60 10000
stop-writes-on-bgsave-error yes
rdbcompression yes
rdbchecksum yes
dbfilename dump.rdb
dir /var/lib/redis

# Replication
replicaof no one
replica-read-only yes
repl-diskless-sync no
repl-backlog-size 256mb

# Security
requirepass your_redis_password
rename-command FLUSHDB ""
rename-command FLUSHALL ""
rename-command CONFIG ""

# Memory management
maxmemory 4gb
maxmemory-policy allkeys-lru
```

#### 9.2.2 Redis Sentinel

```bash
# /etc/redis/sentinel.conf
port 26379
bind 0.0.0.0
protected-mode yes
daemonize yes
pidfile /var/run/redis/redis-sentinel.pid
logfile /var/log/redis/redis-sentinel.log

sentinel monitor mymaster 10.0.1.10 6379 2
sentinel auth-pass mymaster your_redis_password
sentinel down-after-milliseconds mymaster 5000
sentinel parallel-syncs mymaster 1
sentinel failover-timeout mymaster 10000
```

---

## 10. Container Orchestration

### 10.1 Kubernetes Deployment

#### 10.1.1 Namespace and Service Account

```yaml
# k8s/namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: neo-service-layer
  labels:
    name: neo-service-layer
    monitoring: enabled

---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: neo-service-layer-sa
  namespace: neo-service-layer
```

#### 10.1.2 Deployment Configuration

```yaml
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: neo-service-layer-api
  namespace: neo-service-layer
  labels:
    app: neo-service-layer-api
    version: v1.5.0
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  selector:
    matchLabels:
      app: neo-service-layer-api
  template:
    metadata:
      labels:
        app: neo-service-layer-api
        version: v1.5.0
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "9090"
        prometheus.io/path: "/metrics"
    spec:
      serviceAccountName: neo-service-layer-sa
      affinity:
        podAntiAffinity:
          requiredDuringSchedulingIgnoredDuringExecution:
          - labelSelector:
              matchExpressions:
              - key: app
                operator: In
                values:
                - neo-service-layer-api
            topologyKey: kubernetes.io/hostname
      containers:
      - name: api
        image: registry.example.com/neo-service-layer:v1.5.0
        imagePullPolicy: Always
        ports:
        - containerPort: 5000
          name: http
          protocol: TCP
        - containerPort: 9090
          name: metrics
          protocol: TCP
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:5000"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: neo-service-layer-secrets
              key: db-connection-string
        - name: Jwt__Secret
          valueFrom:
            secretKeyRef:
              name: neo-service-layer-secrets
              key: jwt-secret
        resources:
          requests:
            cpu: "1"
            memory: "2Gi"
          limits:
            cpu: "4"
            memory: "8Gi"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 3
        volumeMounts:
        - name: config
          mountPath: /app/config
          readOnly: true
        - name: sgx-device
          mountPath: /dev/sgx
        securityContext:
          allowPrivilegeEscalation: false
          readOnlyRootFilesystem: true
          runAsNonRoot: true
          runAsUser: 1000
          capabilities:
            drop:
            - ALL
            add:
            - NET_BIND_SERVICE
      volumes:
      - name: config
        configMap:
          name: neo-service-layer-config
      - name: sgx-device
        hostPath:
          path: /dev/sgx
          type: Directory
      imagePullSecrets:
      - name: regcred
```

#### 10.1.3 Service Configuration

```yaml
# k8s/service.yaml
apiVersion: v1
kind: Service
metadata:
  name: neo-service-layer-api
  namespace: neo-service-layer
  labels:
    app: neo-service-layer-api
  annotations:
    service.beta.kubernetes.io/aws-load-balancer-type: "nlb"
    service.beta.kubernetes.io/aws-load-balancer-cross-zone-load-balancing-enabled: "true"
spec:
  type: LoadBalancer
  selector:
    app: neo-service-layer-api
  ports:
  - name: http
    port: 80
    targetPort: 5000
    protocol: TCP
  - name: https
    port: 443
    targetPort: 5000
    protocol: TCP
  sessionAffinity: ClientIP
  sessionAffinityConfig:
    clientIP:
      timeoutSeconds: 10800
```

#### 10.1.4 Horizontal Pod Autoscaler

```yaml
# k8s/hpa.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: neo-service-layer-hpa
  namespace: neo-service-layer
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: neo-service-layer-api
  minReplicas: 3
  maxReplicas: 20
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
  - type: Pods
    pods:
      metric:
        name: http_requests_per_second
      target:
        type: AverageValue
        averageValue: "1000"
  behavior:
    scaleUp:
      stabilizationWindowSeconds: 60
      policies:
      - type: Percent
        value: 100
        periodSeconds: 60
      - type: Pods
        value: 4
        periodSeconds: 60
      selectPolicy: Max
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
      - type: Percent
        value: 10
        periodSeconds: 60
      - type: Pods
        value: 2
        periodSeconds: 60
      selectPolicy: Min
```

### 10.2 Docker Compose (Development)

```yaml
# docker-compose.yml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: neo-service-layer-api
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:5000
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=neoservicelayer;Username=neoservice;Password=neoservice_password
      - ConnectionStrings__Redis=redis:6379
      - Jwt__Secret=development_secret_key_change_in_production
    ports:
      - "5000:5000"
    depends_on:
      - postgres
      - redis
    networks:
      - neo-service-layer
    volumes:
      - ./appsettings.Development.json:/app/appsettings.Development.json:ro
    restart: unless-stopped

  postgres:
    image: postgres:15-alpine
    container_name: neo-service-layer-postgres
    environment:
      - POSTGRES_DB=neoservicelayer
      - POSTGRES_USER=neoservice
      - POSTGRES_PASSWORD=neoservice_password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/init.sql:ro
    networks:
      - neo-service-layer
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    container_name: neo-service-layer-redis
    command: redis-server --requirepass redis_password
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    networks:
      - neo-service-layer
    restart: unless-stopped

  prometheus:
    image: prom/prometheus:latest
    container_name: neo-service-layer-prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/usr/share/prometheus/console_libraries'
      - '--web.console.templates=/usr/share/prometheus/consoles'
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus_data:/prometheus
    networks:
      - neo-service-layer
    restart: unless-stopped

  grafana:
    image: grafana/grafana:latest
    container_name: neo-service-layer-grafana
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
      - GF_USERS_ALLOW_SIGN_UP=false
    ports:
      - "3000:3000"
    volumes:
      - grafana_data:/var/lib/grafana
      - ./monitoring/grafana/provisioning:/etc/grafana/provisioning:ro
    networks:
      - neo-service-layer
    restart: unless-stopped

volumes:
  postgres_data:
  redis_data:
  prometheus_data:
  grafana_data:

networks:
  neo-service-layer:
    driver: bridge
```

---

## 11. Monitoring & Observability

### 11.1 Prometheus Configuration

```yaml
# monitoring/prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s
  external_labels:
    cluster: 'neo-service-layer-prod'
    region: 'us-east-1'

rule_files:
  - /etc/prometheus/rules/*.yml

scrape_configs:
  - job_name: 'neo-service-layer-api'
    kubernetes_sd_configs:
    - role: pod
      namespaces:
        names:
        - neo-service-layer
    relabel_configs:
    - source_labels: [__meta_kubernetes_pod_annotation_prometheus_io_scrape]
      action: keep
      regex: true
    - source_labels: [__meta_kubernetes_pod_annotation_prometheus_io_path]
      action: replace
      target_label: __metrics_path__
      regex: (.+)
    - source_labels: [__address__, __meta_kubernetes_pod_annotation_prometheus_io_port]
      action: replace
      regex: ([^:]+)(?::\d+)?;(\d+)
      replacement: $1:$2
      target_label: __address__
    - action: labelmap
      regex: __meta_kubernetes_pod_label_(.+)
    - source_labels: [__meta_kubernetes_namespace]
      action: replace
      target_label: kubernetes_namespace
    - source_labels: [__meta_kubernetes_pod_name]
      action: replace
      target_label: kubernetes_pod_name

  - job_name: 'kubernetes-nodes'
    kubernetes_sd_configs:
    - role: node
    relabel_configs:
    - action: labelmap
      regex: __meta_kubernetes_node_label_(.+)

  - job_name: 'postgres-exporter'
    static_configs:
    - targets: ['postgres-exporter:9187']

  - job_name: 'redis-exporter'
    static_configs:
    - targets: ['redis-exporter:9121']
```

### 11.2 Alert Rules

```yaml
# monitoring/alerts.yml
groups:
  - name: neo-service-layer
    interval: 30s
    rules:
    - alert: HighErrorRate
      expr: |
        sum(rate(http_requests_total{status=~"5.."}[5m])) by (service)
        /
        sum(rate(http_requests_total[5m])) by (service)
        > 0.05
      for: 5m
      labels:
        severity: critical
        team: platform
      annotations:
        summary: High error rate on {{ $labels.service }}
        description: "Error rate is {{ $value | humanizePercentage }} for {{ $labels.service }}"

    - alert: HighResponseTime
      expr: |
        histogram_quantile(0.95,
          sum(rate(http_request_duration_seconds_bucket[5m])) by (service, le)
        ) > 1
      for: 5m
      labels:
        severity: warning
        team: platform
      annotations:
        summary: High response time on {{ $labels.service }}
        description: "95th percentile response time is {{ $value }}s for {{ $labels.service }}"

    - alert: PodCrashLooping
      expr: |
        rate(kube_pod_container_status_restarts_total[15m]) > 0
      for: 5m
      labels:
        severity: critical
        team: platform
      annotations:
        summary: Pod {{ $labels.namespace }}/{{ $labels.pod }} is crash looping
        description: "Pod {{ $labels.namespace }}/{{ $labels.pod }} has restarted {{ $value }} times in the last 15 minutes"

    - alert: HighMemoryUsage
      expr: |
        container_memory_usage_bytes{pod=~"neo-service-layer-.*"}
        / container_spec_memory_limit_bytes
        > 0.9
      for: 5m
      labels:
        severity: warning
        team: platform
      annotations:
        summary: High memory usage for {{ $labels.pod }}
        description: "Memory usage is {{ $value | humanizePercentage }} for {{ $labels.pod }}"

    - alert: DatabaseConnectionPoolExhausted
      expr: |
        pg_stat_database_numbackends{datname="neoservicelayer"}
        / pg_settings_max_connections
        > 0.8
      for: 5m
      labels:
        severity: critical
        team: database
      annotations:
        summary: Database connection pool near exhaustion
        description: "Connection pool usage is {{ $value | humanizePercentage }}"
```

### 11.3 Grafana Dashboards

```json
{
  "dashboard": {
    "title": "Neo Service Layer Overview",
    "panels": [
      {
        "title": "Request Rate",
        "targets": [
          {
            "expr": "sum(rate(http_requests_total[5m])) by (service)"
          }
        ],
        "type": "graph"
      },
      {
        "title": "Error Rate",
        "targets": [
          {
            "expr": "sum(rate(http_requests_total{status=~\"5..\"}[5m])) by (service)"
          }
        ],
        "type": "graph"
      },
      {
        "title": "Response Time (95th percentile)",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket[5m])) by (service, le))"
          }
        ],
        "type": "graph"
      },
      {
        "title": "Active Connections",
        "targets": [
          {
            "expr": "sum(api_active_connections) by (service)"
          }
        ],
        "type": "graph"
      }
    ]
  }
}
```

### 11.4 Application Performance Monitoring

```csharp
// APM Integration
services.AddApplicationInsightsTelemetry(configuration["ApplicationInsights:InstrumentationKey"]);

services.Configure<TelemetryConfiguration>(config =>
{
    config.TelemetryProcessorChainBuilder
        .Use((next) => new CustomTelemetryProcessor(next))
        .Build();
});

// Custom metrics
public class MetricsService
{
    private readonly IMetrics _metrics;

    public void RecordTransaction(string service, string method, double duration, bool success)
    {
        _metrics.Measure.Counter.Increment(
            new CounterOptions { Name = "transactions_total" },
            new MetricTags("service", service, "method", method, "success", success.ToString()));

        _metrics.Measure.Histogram.Update(
            new HistogramOptions { Name = "transaction_duration_seconds" },
            new MetricTags("service", service, "method", method),
            duration);
    }
}
```

---

## 12. Backup & Recovery

### 12.1 Backup Strategy

#### 12.1.1 Database Backups

```bash
#!/bin/bash
# PostgreSQL backup script

BACKUP_DIR="/backup/postgres"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
DB_NAME="neoservicelayer"
DB_USER="postgres"
S3_BUCKET="neo-service-layer-backups"

# Create backup directory
mkdir -p $BACKUP_DIR

# Perform backup
PGPASSWORD=$DB_PASSWORD pg_dump -h postgres-service -U $DB_USER -d $DB_NAME -Fc -f $BACKUP_DIR/backup_$TIMESTAMP.dump

# Compress backup
gzip $BACKUP_DIR/backup_$TIMESTAMP.dump

# Upload to S3
aws s3 cp $BACKUP_DIR/backup_$TIMESTAMP.dump.gz s3://$S3_BUCKET/postgres/$TIMESTAMP/

# Clean up old local backups (keep 7 days)
find $BACKUP_DIR -name "backup_*.dump.gz" -mtime +7 -delete

# Verify backup
if [ $? -eq 0 ]; then
    echo "Backup successful: backup_$TIMESTAMP.dump.gz"
    # Send success notification
    curl -X POST $SLACK_WEBHOOK -d "{\"text\":\"PostgreSQL backup completed successfully\"}"
else
    echo "Backup failed!"
    # Send failure alert
    curl -X POST $SLACK_WEBHOOK -d "{\"text\":\"PostgreSQL backup failed!\"}"
    exit 1
fi
```

#### 12.1.2 Application State Backup

```yaml
# k8s/backup-cronjob.yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: neo-service-layer-backup
  namespace: neo-service-layer
spec:
  schedule: "0 2 * * *"  # Daily at 2 AM
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: backup
            image: neo-service-layer/backup:latest
            env:
            - name: BACKUP_TYPE
              value: "full"
            - name: S3_BUCKET
              value: "neo-service-layer-backups"
            - name: RETENTION_DAYS
              value: "30"
            volumeMounts:
            - name: data
              mountPath: /data
            - name: backup-credentials
              mountPath: /credentials
              readOnly: true
          volumes:
          - name: data
            persistentVolumeClaim:
              claimName: neo-service-layer-data
          - name: backup-credentials
            secret:
              secretName: backup-credentials
          restartPolicy: OnFailure
```

### 12.2 Disaster Recovery

#### 12.2.1 Recovery Time Objectives

| Component | RTO | RPO | Strategy |
|-----------|-----|-----|----------|
| API Services | 5 min | 0 min | Multi-region active-active |
| Database | 30 min | 5 min | Streaming replication |
| Object Storage | 15 min | 0 min | Cross-region replication |
| Configuration | 10 min | 0 min | GitOps |

#### 12.2.2 Recovery Procedures

```bash
#!/bin/bash
# Disaster recovery script

# 1. Verify backup availability
aws s3 ls s3://neo-service-layer-backups/postgres/ --recursive | sort | tail -n 1

# 2. Restore database
LATEST_BACKUP=$(aws s3 ls s3://neo-service-layer-backups/postgres/ --recursive | sort | tail -n 1 | awk '{print $4}')
aws s3 cp s3://neo-service-layer-backups/$LATEST_BACKUP /tmp/restore.dump.gz
gunzip /tmp/restore.dump.gz
PGPASSWORD=$DB_PASSWORD pg_restore -h postgres-service -U postgres -d neoservicelayer_restore /tmp/restore.dump

# 3. Verify data integrity
psql -h postgres-service -U postgres -d neoservicelayer_restore -c "SELECT COUNT(*) FROM neo_service_layer.storage_data;"

# 4. Switch traffic to DR region
kubectl patch service neo-service-layer-api -p '{"spec":{"selector":{"region":"dr"}}}'

# 5. Update DNS
aws route53 change-resource-record-sets --hosted-zone-id Z123456 --change-batch file://dr-dns-update.json
```

---

## 13. Scaling & Performance

### 13.1 Horizontal Scaling

#### 13.1.1 Auto-Scaling Configuration

```yaml
# k8s/vpa.yaml
apiVersion: autoscaling.k8s.io/v1
kind: VerticalPodAutoscaler
metadata:
  name: neo-service-layer-vpa
  namespace: neo-service-layer
spec:
  targetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: neo-service-layer-api
  updatePolicy:
    updateMode: "Auto"
  resourcePolicy:
    containerPolicies:
    - containerName: api
      minAllowed:
        cpu: 500m
        memory: 1Gi
      maxAllowed:
        cpu: 8
        memory: 16Gi
```

#### 13.1.2 Database Connection Pooling

```csharp
// Connection pool configuration
services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
}, ServiceLifetime.Scoped);

// Configure connection pool
NpgsqlConnection.GlobalTypeMapper.UseJsonNet();
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.ConnectionStringBuilder.Pooling = true;
dataSourceBuilder.ConnectionStringBuilder.MinPoolSize = 10;
dataSourceBuilder.ConnectionStringBuilder.MaxPoolSize = 100;
dataSourceBuilder.ConnectionStringBuilder.ConnectionLifetime = 300;
```

### 13.2 Performance Optimization

#### 13.2.1 Caching Strategy

```csharp
// Multi-level caching
public class CacheService
{
    private readonly IMemoryCache _l1Cache;
    private readonly IDistributedCache _l2Cache;
    private readonly ILogger<CacheService> _logger;

    public async Task<T> GetOrSetAsync<T>(
        string key, 
        Func<Task<T>> factory, 
        TimeSpan? l1Expiry = null, 
        TimeSpan? l2Expiry = null)
    {
        // L1 Cache (Memory)
        if (_l1Cache.TryGetValue(key, out T cachedValue))
        {
            _logger.LogDebug("L1 cache hit for key: {Key}", key);
            return cachedValue;
        }

        // L2 Cache (Redis)
        var l2Value = await _l2Cache.GetAsync(key);
        if (l2Value != null)
        {
            _logger.LogDebug("L2 cache hit for key: {Key}", key);
            var deserializedValue = JsonSerializer.Deserialize<T>(l2Value);
            _l1Cache.Set(key, deserializedValue, l1Expiry ?? TimeSpan.FromMinutes(5));
            return deserializedValue;
        }

        // Cache miss - execute factory
        _logger.LogDebug("Cache miss for key: {Key}", key);
        var value = await factory();

        // Set in both caches
        _l1Cache.Set(key, value, l1Expiry ?? TimeSpan.FromMinutes(5));
        await _l2Cache.SetAsync(
            key, 
            JsonSerializer.SerializeToUtf8Bytes(value), 
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = l2Expiry ?? TimeSpan.FromHours(1)
            });

        return value;
    }
}
```

#### 13.2.2 Query Optimization

```sql
-- Optimize slow queries
CREATE INDEX CONCURRENTLY idx_storage_composite 
ON storage_data(user_address, created_at DESC) 
WHERE deleted_at IS NULL;

-- Analyze query performance
EXPLAIN (ANALYZE, BUFFERS) 
SELECT * FROM storage_data 
WHERE user_address = '0x123...' 
AND created_at > NOW() - INTERVAL '7 days'
ORDER BY created_at DESC
LIMIT 100;

-- Enable query plan caching
ALTER DATABASE neoservicelayer SET plan_cache_mode = 'force_generic_plan';

-- Optimize table statistics
ANALYZE storage_data;
VACUUM ANALYZE storage_data;
```

### 13.3 Load Testing

```yaml
# k6 load test script
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

export let errorRate = new Rate('errors');

export let options = {
  stages: [
    { duration: '5m', target: 100 },   // Ramp up
    { duration: '10m', target: 100 },  // Stay at 100
    { duration: '5m', target: 500 },   // Ramp up
    { duration: '10m', target: 500 },  // Stay at 500
    { duration: '5m', target: 1000 },  // Ramp up
    { duration: '10m', target: 1000 }, // Stay at 1000
    { duration: '5m', target: 0 },     // Ramp down
  ],
  thresholds: {
    'http_req_duration': ['p(95)<500'], // 95% of requests under 500ms
    'errors': ['rate<0.1'],             // Error rate under 10%
  },
};

export default function() {
  let params = {
    headers: {
      'Authorization': 'Bearer ' + __ENV.API_TOKEN,
      'Content-Type': 'application/json',
    },
  };

  // Test storage endpoint
  let payload = JSON.stringify({
    key: `test_${__VU}_${__ITER}`,
    value: { data: 'test data' },
    encrypted: false
  });

  let response = http.post(
    'https://api.neo-service-layer.io/v1/storage/store/neo-n3',
    payload,
    params
  );

  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });

  errorRate.add(response.status !== 200);
  sleep(1);
}
```

---

## 14. Maintenance Procedures

### 14.1 Regular Maintenance Tasks

#### 14.1.1 Daily Tasks

```bash
#!/bin/bash
# Daily maintenance script

echo "=== Daily Maintenance Started: $(date) ==="

# 1. Check disk space
df -h | grep -E '^/dev/' | awk '{if ($5+0 > 80) print "WARNING: " $6 " is " $5 " full"}'

# 2. Check service health
kubectl get pods -n neo-service-layer -o wide
kubectl top pods -n neo-service-layer

# 3. Check database connections
psql -h postgres-service -U postgres -c "SELECT count(*) FROM pg_stat_activity;"

# 4. Review error logs
kubectl logs -n neo-service-layer -l app=neo-service-layer-api --since=24h | grep ERROR | wc -l

# 5. Verify backups
aws s3 ls s3://neo-service-layer-backups/postgres/ --recursive | grep $(date +%Y%m%d) | wc -l

echo "=== Daily Maintenance Completed: $(date) ==="
```

#### 14.1.2 Weekly Tasks

```bash
#!/bin/bash
# Weekly maintenance script

echo "=== Weekly Maintenance Started: $(date) ==="

# 1. Update container images
kubectl set image deployment/neo-service-layer-api api=registry.example.com/neo-service-layer:latest -n neo-service-layer

# 2. Database maintenance
psql -h postgres-service -U postgres -d neoservicelayer << EOF
VACUUM ANALYZE;
REINDEX DATABASE neoservicelayer;
EOF

# 3. Clear old logs
find /var/log/neo-service-layer -name "*.log" -mtime +30 -delete

# 4. Security updates
apt update
apt list --upgradable

# 5. Certificate renewal check
certbot certificates

echo "=== Weekly Maintenance Completed: $(date) ==="
```

### 14.2 Upgrade Procedures

#### 14.2.1 Rolling Update

```bash
#!/bin/bash
# Rolling update procedure

VERSION=$1
if [ -z "$VERSION" ]; then
    echo "Usage: $0 <version>"
    exit 1
fi

echo "Starting rolling update to version $VERSION"

# 1. Build and push new image
docker build -t neo-service-layer:$VERSION .
docker tag neo-service-layer:$VERSION registry.example.com/neo-service-layer:$VERSION
docker push registry.example.com/neo-service-layer:$VERSION

# 2. Update deployment
kubectl set image deployment/neo-service-layer-api \
    api=registry.example.com/neo-service-layer:$VERSION \
    -n neo-service-layer \
    --record

# 3. Monitor rollout
kubectl rollout status deployment/neo-service-layer-api -n neo-service-layer

# 4. Verify deployment
kubectl get pods -n neo-service-layer
kubectl logs -n neo-service-layer -l app=neo-service-layer-api --tail=100

# 5. Run smoke tests
./scripts/smoke-tests.sh

echo "Rolling update completed successfully"
```

#### 14.2.2 Database Migration

```csharp
// Database migration service
public class MigrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MigrationService> _logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        _logger.LogInformation("Starting database migration");
        
        // Get pending migrations
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        
        if (pendingMigrations.Any())
        {
            _logger.LogInformation($"Applying {pendingMigrations.Count()} migrations");
            await dbContext.Database.MigrateAsync();
            _logger.LogInformation("Database migration completed");
        }
        else
        {
            _logger.LogInformation("No pending migrations");
        }
    }
}
```

### 14.3 Incident Response

#### 14.3.1 Incident Response Runbook

```markdown
# Incident Response Runbook

## Severity Levels
- **P1**: Complete service outage
- **P2**: Partial service degradation
- **P3**: Minor issues, no user impact
- **P4**: Cosmetic issues

## Response Times
- P1: 15 minutes
- P2: 1 hour
- P3: 4 hours
- P4: Next business day

## Escalation Path
1. On-call engineer
2. Team lead
3. Engineering manager
4. VP of Engineering

## Response Procedure

### 1. Assess Impact
```bash
# Check service status
kubectl get pods -n neo-service-layer
kubectl top nodes
kubectl top pods -n neo-service-layer

# Check error rates
kubectl logs -n neo-service-layer -l app=neo-service-layer-api --tail=1000 | grep ERROR
```

### 2. Communicate
- Update status page
- Notify stakeholders via Slack
- Create incident ticket

### 3. Mitigate
- Scale up pods if needed
- Redirect traffic if necessary
- Enable circuit breakers

### 4. Investigate
- Review logs
- Check metrics
- Analyze traces

### 5. Resolve
- Apply fix
- Verify resolution
- Monitor for stability

### 6. Post-Mortem
- Document timeline
- Identify root cause
- Create action items
```

---

## 15. Troubleshooting

### 15.1 Common Issues

#### 15.1.1 High Memory Usage

```bash
# Diagnose memory issues
kubectl top pods -n neo-service-layer --sort-by=memory

# Get memory dump
kubectl exec -n neo-service-layer <pod-name> -- dotnet-dump collect -p 1

# Analyze dump
dotnet-dump analyze core_<timestamp>
> dumpheap -stat
> dumpheap -type System.String -stat
```

#### 15.1.2 Slow Database Queries

```sql
-- Find slow queries
SELECT 
    query,
    calls,
    total_time,
    mean_time,
    max_time
FROM pg_stat_statements
WHERE mean_time > 100
ORDER BY mean_time DESC
LIMIT 20;

-- Check table bloat
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename) - pg_relation_size(schemaname||'.'||tablename)) AS external_size
FROM pg_tables
WHERE schemaname = 'neo_service_layer'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

#### 15.1.3 Connection Pool Exhaustion

```csharp
// Monitor connection pool
public class ConnectionPoolMonitor : IHostedService
{
    private Timer _timer;
    private readonly ILogger<ConnectionPoolMonitor> _logger;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(CheckConnectionPool, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }

    private void CheckConnectionPool(object state)
    {
        var stats = NpgsqlConnection.GlobalConnectorPool.Statistics;
        
        _logger.LogInformation(
            "Connection Pool Stats - Total: {Total}, Idle: {Idle}, Busy: {Busy}",
            stats.Total,
            stats.Idle,
            stats.Busy);

        if (stats.Busy > stats.Total * 0.8)
        {
            _logger.LogWarning("Connection pool usage above 80%!");
        }
    }
}
```

### 15.2 Debug Tools

#### 15.2.1 Application Debugging

```bash
# Enable debug logging
kubectl set env deployment/neo-service-layer-api ASPNETCORE_ENVIRONMENT=Development -n neo-service-layer

# Attach debugger
kubectl port-forward -n neo-service-layer <pod-name> 5005:5005
# Then attach Visual Studio debugger to localhost:5005

# Get thread dump
kubectl exec -n neo-service-layer <pod-name> -- kill -3 1
```

#### 15.2.2 Network Debugging

```bash
# Test connectivity
kubectl run -it --rm debug --image=nicolaka/netshoot --restart=Never -n neo-service-layer

# Inside debug pod
nslookup postgres-service
telnet postgres-service 5432
curl -v http://neo-service-layer-api:5000/health

# Capture traffic
tcpdump -i any -w capture.pcap host postgres-service
```

---

## 16. Disaster Recovery

### 16.1 Backup Verification

```bash
#!/bin/bash
# Backup verification script

BACKUP_FILE=$1
RESTORE_DB="neoservicelayer_verify"

# Create verification database
PGPASSWORD=$DB_PASSWORD createdb -h postgres-service -U postgres $RESTORE_DB

# Restore backup
PGPASSWORD=$DB_PASSWORD pg_restore -h postgres-service -U postgres -d $RESTORE_DB $BACKUP_FILE

# Verify data integrity
RESULT=$(PGPASSWORD=$DB_PASSWORD psql -h postgres-service -U postgres -d $RESTORE_DB -t -c "
    SELECT 
        (SELECT COUNT(*) FROM neo_service_layer.storage_data) as storage_count,
        (SELECT COUNT(*) FROM neo_service_layer.oracle_requests) as oracle_count,
        (SELECT COUNT(*) FROM neo_service_layer.audit_logs) as audit_count
")

echo "Verification Results: $RESULT"

# Cleanup
PGPASSWORD=$DB_PASSWORD dropdb -h postgres-service -U postgres $RESTORE_DB
```

### 16.2 Failover Procedures

```yaml
# k8s/failover-job.yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: failover-to-dr
  namespace: neo-service-layer
spec:
  template:
    spec:
      containers:
      - name: failover
        image: neo-service-layer/failover:latest
        env:
        - name: DR_REGION
          value: "us-west-2"
        - name: PRIMARY_REGION
          value: "us-east-1"
        command:
        - /bin/bash
        - -c
        - |
          # 1. Verify DR readiness
          kubectl --context=dr-cluster get nodes
          
          # 2. Scale up DR deployment
          kubectl --context=dr-cluster scale deployment neo-service-layer-api --replicas=10
          
          # 3. Update Route53
          aws route53 change-resource-record-sets \
            --hosted-zone-id Z123456 \
            --change-batch file:///scripts/dr-dns.json
          
          # 4. Verify traffic shift
          sleep 60
          curl -f https://api.neo-service-layer.io/health || exit 1
          
          # 5. Scale down primary
          kubectl --context=primary-cluster scale deployment neo-service-layer-api --replicas=0
      restartPolicy: Never
```

---

## 17. Compliance & Auditing

### 17.1 Audit Configuration

```csharp
// Audit middleware
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuditService _auditService;

    public async Task InvokeAsync(HttpContext context)
    {
        var auditEntry = new AuditEntry
        {
            RequestId = context.TraceIdentifier,
            UserId = context.User?.Identity?.Name,
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            Method = context.Request.Method,
            Path = context.Request.Path,
            QueryString = context.Request.QueryString.ToString(),
            Headers = GetSafeHeaders(context.Request.Headers),
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // Capture request body
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            auditEntry.RequestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Capture response
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            auditEntry.StatusCode = context.Response.StatusCode;
            auditEntry.ResponseTime = DateTime.UtcNow - auditEntry.Timestamp;

            // Log response body for non-success codes
            if (context.Response.StatusCode >= 400)
            {
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                auditEntry.ResponseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);
            }

            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            auditEntry.Error = ex.ToString();
            throw;
        }
        finally
        {
            await _auditService.LogAsync(auditEntry);
        }
    }

    private Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
    {
        var safeHeaders = new Dictionary<string, string>();
        var sensitiveHeaders = new[] { "Authorization", "Cookie", "X-API-Key" };

        foreach (var header in headers)
        {
            if (sensitiveHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
            {
                safeHeaders[header.Key] = "[REDACTED]";
            }
            else
            {
                safeHeaders[header.Key] = header.Value.ToString();
            }
        }

        return safeHeaders;
    }
}
```

### 17.2 Compliance Reports

```sql
-- GDPR compliance report
SELECT 
    'Storage Data' as data_type,
    COUNT(*) as record_count,
    COUNT(CASE WHEN encrypted = true THEN 1 END) as encrypted_count,
    COUNT(CASE WHEN created_at < NOW() - INTERVAL '3 years' THEN 1 END) as old_records
FROM neo_service_layer.storage_data
WHERE user_address IN (SELECT address FROM gdpr_requests WHERE type = 'data_request')

UNION ALL

SELECT 
    'Audit Logs' as data_type,
    COUNT(*) as record_count,
    0 as encrypted_count,
    COUNT(CASE WHEN created_at < NOW() - INTERVAL '7 years' THEN 1 END) as old_records
FROM neo_service_layer.audit_logs
WHERE user_address IN (SELECT address FROM gdpr_requests WHERE type = 'data_request');

-- Data retention compliance
DELETE FROM neo_service_layer.storage_data
WHERE expires_at < NOW()
  AND deleted_at IS NULL;

-- Right to be forgotten
UPDATE neo_service_layer.storage_data
SET value = '{"deleted": true}',
    encrypted = false,
    deleted_at = NOW()
WHERE user_address = @UserAddress;
```

### 17.3 Security Compliance

```yaml
# Security scanning pipeline
name: Security Compliance Check

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  security-scan:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Run Trivy vulnerability scanner
      uses: aquasecurity/trivy-action@master
      with:
        scan-type: 'fs'
        scan-ref: '.'
        format: 'sarif'
        output: 'trivy-results.sarif'
        severity: 'CRITICAL,HIGH'
    
    - name: Upload Trivy scan results
      uses: github/codeql-action/upload-sarif@v2
      with:
        sarif_file: 'trivy-results.sarif'
    
    - name: Run OWASP dependency check
      uses: dependency-check/Dependency-Check_Action@main
      with:
        project: 'neo-service-layer'
        path: '.'
        format: 'ALL'
    
    - name: SonarQube scan
      uses: SonarSource/sonarqube-scan-action@master
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        SONAR_HOST_URL: ${{ secrets.SONAR_HOST_URL }}
```

---

## 18. Appendices

### Appendix A: Configuration Reference

#### A.1 Environment Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | Development | Yes |
| `ASPNETCORE_URLS` | Listening URLs | http://+:5000 | Yes |
| `ConnectionStrings__DefaultConnection` | Database connection | - | Yes |
| `ConnectionStrings__Redis` | Redis connection | - | Yes |
| `Jwt__Secret` | JWT signing key | - | Yes |
| `Jwt__Issuer` | JWT issuer | neo-service-layer | No |
| `Jwt__Audience` | JWT audience | - | Yes |
| `Jwt__ExpiryInDays` | Token expiry | 1 | No |
| `RateLimiting__PermitLimit` | Rate limit | 100 | No |
| `Services__EnableSgx` | Enable SGX | false | No |
| `Logging__LogLevel__Default` | Log level | Information | No |

#### A.2 Kubernetes Labels

| Label | Description | Example |
|-------|-------------|---------|
| `app` | Application name | neo-service-layer-api |
| `version` | Application version | v1.5.0 |
| `component` | Component type | api, database, cache |
| `tier` | Application tier | frontend, backend, data |
| `environment` | Deployment environment | production, staging |
| `team` | Owning team | platform, infrastructure |

### Appendix B: Monitoring Queries

#### B.1 Prometheus Queries

```promql
# Request rate by service
sum(rate(http_requests_total[5m])) by (service)

# Error rate
sum(rate(http_requests_total{status=~"5.."}[5m])) by (service)
/ sum(rate(http_requests_total[5m])) by (service)

# P95 latency
histogram_quantile(0.95,
  sum(rate(http_request_duration_seconds_bucket[5m])) by (service, le)
)

# Memory usage
container_memory_usage_bytes{pod=~"neo-service-layer-.*"}
/ container_spec_memory_limit_bytes

# CPU usage
rate(container_cpu_usage_seconds_total{pod=~"neo-service-layer-.*"}[5m])
```

#### B.2 Elasticsearch Queries

```json
// Find all errors in the last hour
{
  "query": {
    "bool": {
      "must": [
        { "term": { "level": "ERROR" } },
        { "range": { "@timestamp": { "gte": "now-1h" } } }
      ]
    }
  },
  "aggs": {
    "services": {
      "terms": { "field": "service.keyword" }
    }
  }
}

// Slow requests
{
  "query": {
    "range": {
      "duration": { "gte": 1000 }
    }
  },
  "sort": [
    { "duration": { "order": "desc" } }
  ]
}
```

### Appendix C: Emergency Contacts

| Role | Name | Phone | Email |
|------|------|-------|-------|
| On-Call Engineer | Rotation | +1-XXX-XXX-XXXX | oncall@neo-service-layer.io |
| Platform Lead | John Doe | +1-XXX-XXX-XXXX | john.doe@neo-service-layer.io |
| VP Engineering | Jane Smith | +1-XXX-XXX-XXXX | jane.smith@neo-service-layer.io |
| Security Team | Security | +1-XXX-XXX-XXXX | security@neo-service-layer.io |
| Database Admin | DBA Team | +1-XXX-XXX-XXXX | dba@neo-service-layer.io |

### Appendix D: Useful Commands

```bash
# Get all pods with issues
kubectl get pods -A | grep -v Running | grep -v Completed

# Describe problematic pod
kubectl describe pod <pod-name> -n neo-service-layer

# Get pod logs
kubectl logs <pod-name> -n neo-service-layer --tail=1000

# Execute command in pod
kubectl exec -it <pod-name> -n neo-service-layer -- /bin/bash

# Port forward for debugging
kubectl port-forward -n neo-service-layer svc/neo-service-layer-api 5000:5000

# Scale deployment
kubectl scale deployment neo-service-layer-api --replicas=5 -n neo-service-layer

# Rolling restart
kubectl rollout restart deployment/neo-service-layer-api -n neo-service-layer

# Check rollout status
kubectl rollout status deployment/neo-service-layer-api -n neo-service-layer

# Get events
kubectl get events -n neo-service-layer --sort-by='.lastTimestamp'

# Resource usage
kubectl top nodes
kubectl top pods -n neo-service-layer

# Get service endpoints
kubectl get endpoints -n neo-service-layer

# Check node status
kubectl get nodes -o wide

# Cordon node (prevent new pods)
kubectl cordon <node-name>

# Drain node (move pods)
kubectl drain <node-name> --ignore-daemonsets --delete-emptydir-data

# Uncordon node
kubectl uncordon <node-name>
```

---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0.0 | 2023-01-15 | Platform Team | Initial deployment guide |
| 1.1.0 | 2023-04-20 | DevOps Team | Added Kubernetes configuration |
| 1.2.0 | 2023-07-10 | Security Team | Enhanced security procedures |
| 1.3.0 | 2023-10-05 | SRE Team | Added monitoring and alerting |
| 1.4.0 | 2024-01-10 | Platform Team | SGX integration guide |
| 1.5.0 | 2024-01-30 | Platform Team | Multi-blockchain support |

---

© 2024 Neo Service Layer. All rights reserved.