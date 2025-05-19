# Neo Service Layer - Deployment Guide

## 1. Introduction

This document provides instructions for deploying the Neo Service Layer (NSL) on Aliyun (Alibaba Cloud) using Kubernetes. The NSL leverages Intel SGX through the Open Enclave SDK with Occlum for confidential computing.

## 2. Prerequisites

### 2.1 Hardware Requirements

- Intel SGX-enabled servers
- Minimum 16GB RAM per node
- Minimum 4 CPU cores per node
- Minimum 100GB storage per node

### 2.2 Software Requirements

- Kubernetes 1.20+
- Docker 20.10+
- Occlum 0.29.0+
- Intel SGX Driver and SDK
- Aliyun CLI

### 2.3 Aliyun Services

- Aliyun Container Service for Kubernetes (ACK)
- Aliyun Database Service (RDS)
- Aliyun Object Storage Service (OSS)
- Aliyun Message Queue Service
- Aliyun Virtual Private Cloud (VPC)

## 3. Development Environment Setup

### 3.1 Install Required Tools

#### 3.1.1 Docker

```bash
# Install Docker on Ubuntu
sudo apt-get update
sudo apt-get install -y apt-transport-https ca-certificates curl software-properties-common
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -
sudo add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"
sudo apt-get update
sudo apt-get install -y docker-ce
```

#### 3.1.2 .NET SDK

```bash
# Install .NET 9.0 SDK
wget https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version 9.0.100
export PATH=$PATH:$HOME/.dotnet
```

#### 3.1.3 Intel SGX SDK

```bash
# Install Intel SGX SDK
wget https://download.01.org/intel-sgx/sgx-linux/2.15/distro/ubuntu20.04-server/sgx_linux_x64_sdk_2.15.100.3.bin
chmod +x sgx_linux_x64_sdk_2.15.100.3.bin
./sgx_linux_x64_sdk_2.15.100.3.bin --prefix=/opt/intel
source /opt/intel/sgxsdk/environment
```

#### 3.1.4 Occlum

```bash
# Install Occlum
docker pull occlum/occlum:0.29.0-ubuntu20.04
```

### 3.2 Configure Development Environment

#### 3.2.1 Clone Repository

```bash
git clone https://github.com/your-org/neo-service-layer.git
cd neo-service-layer
```

#### 3.2.2 Build Solution

```bash
dotnet restore
dotnet build
```

#### 3.2.3 Run Tests

```bash
dotnet test
```

## 4. Deployment to Aliyun

### 4.1 Create Aliyun Resources

#### 4.1.1 Create VPC

```bash
aliyun vpc CreateVpc --RegionId cn-hangzhou --VpcName neo-service-layer-vpc --CidrBlock 10.0.0.0/16
```

#### 4.1.2 Create ACK Cluster

```bash
aliyun cs POST /clusters --header "Content-Type=application/json" --body '{
  "name": "neo-service-layer-cluster",
  "region_id": "cn-hangzhou",
  "cluster_type": "ManagedKubernetes",
  "vpcid": "vpc-xxxxxxx",
  "container_cidr": "172.20.0.0/16",
  "service_cidr": "172.21.0.0/20",
  "worker_instance_types": ["ecs.c7t.large"],
  "num_of_nodes": 3,
  "worker_system_disk_category": "cloud_essd",
  "worker_system_disk_size": 120,
  "worker_data_disk": true,
  "worker_data_disk_category": "cloud_essd",
  "worker_data_disk_size": 200,
  "login_password": "password",
  "runtime": {
    "name": "containerd",
    "version": "1.5.13"
  },
  "platform": "CentOS",
  "os_type": "Linux",
  "addons": [
    {
      "name": "flannel"
    },
    {
      "name": "csi-plugin"
    },
    {
      "name": "csi-provisioner"
    },
    {
      "name": "nginx-ingress-controller"
    }
  ]
}'
```

#### 4.1.3 Create RDS Instance

```bash
aliyun rds CreateDBInstance --Engine MySQL --EngineVersion 8.0 --DBInstanceClass rds.mysql.s2.large --DBInstanceStorage 100 --SecurityIPList 10.0.0.0/16 --PayType Postpaid --DBInstanceNetType Intranet --RegionId cn-hangzhou --ZoneId cn-hangzhou-b
```

#### 4.1.4 Create OSS Bucket

```bash
aliyun oss mb oss://neo-service-layer --region cn-hangzhou
```

### 4.2 Configure Kubernetes

#### 4.2.1 Get Kubernetes Credentials

```bash
aliyun cs GET /clusters/[cluster_id]/user_config | jq -r .config > ~/.kube/config
```

#### 4.2.2 Apply Kubernetes Manifests

```bash
kubectl apply -f deployment/kubernetes/namespace.yaml
kubectl apply -f deployment/kubernetes/configmap.yaml
kubectl apply -f deployment/kubernetes/secret.yaml
kubectl apply -f deployment/kubernetes/deployment.yaml
kubectl apply -f deployment/kubernetes/service.yaml
kubectl apply -f deployment/kubernetes/ingress.yaml
```

### 4.3 Configure SGX on Kubernetes

#### 4.3.1 Deploy SGX Device Plugin

```bash
kubectl apply -f deployment/kubernetes/sgx-device-plugin.yaml
```

#### 4.3.2 Verify SGX Device Plugin

```bash
kubectl get pods -n kube-system | grep sgx
```

## 5. Monitoring and Logging

### 5.1 Deploy Prometheus and Grafana

```bash
kubectl apply -f deployment/kubernetes/monitoring/prometheus.yaml
kubectl apply -f deployment/kubernetes/monitoring/grafana.yaml
```

### 5.2 Deploy ELK Stack

```bash
kubectl apply -f deployment/kubernetes/logging/elasticsearch.yaml
kubectl apply -f deployment/kubernetes/logging/logstash.yaml
kubectl apply -f deployment/kubernetes/logging/kibana.yaml
kubectl apply -f deployment/kubernetes/logging/filebeat.yaml
```

### 5.3 Deploy Jaeger

```bash
kubectl apply -f deployment/kubernetes/tracing/jaeger.yaml
```

## 6. Maintenance

### 6.1 Backup and Restore

#### 6.1.1 Database Backup

```bash
aliyun rds CreateBackup --DBInstanceId [instance_id] --BackupMethod Physical
```

#### 6.1.2 Database Restore

```bash
aliyun rds RestoreDBInstance --DBInstanceId [instance_id] --BackupId [backup_id]
```

### 6.2 Scaling

#### 6.2.1 Scale Kubernetes Nodes

```bash
aliyun cs ScaleClusterNodePool --ClusterId [cluster_id] --NodepoolId [nodepool_id] --Count 5
```

#### 6.2.2 Scale Deployments

```bash
kubectl scale deployment neo-service-layer-api --replicas=5 -n neo-service-layer
```

### 6.3 Upgrading

#### 6.3.1 Upgrade Kubernetes Cluster

```bash
aliyun cs UpgradeCluster --ClusterId [cluster_id] --Version 1.22.3-aliyun.1
```

#### 6.3.2 Upgrade Application

```bash
kubectl set image deployment/neo-service-layer-api neo-service-layer-api=neo-service-layer-api:v1.1.0 -n neo-service-layer
```

## 7. Troubleshooting

### 7.1 Check Pod Logs

```bash
kubectl logs -f [pod_name] -n neo-service-layer
```

### 7.2 Check Pod Status

```bash
kubectl describe pod [pod_name] -n neo-service-layer
```

### 7.3 Check SGX Status

```bash
kubectl exec -it [pod_name] -n neo-service-layer -- sgx-detect
```

### 7.4 Check Occlum Status

```bash
kubectl exec -it [pod_name] -n neo-service-layer -- occlum status
```
