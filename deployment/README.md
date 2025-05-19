# Neo Service Layer Deployment

This directory contains deployment configurations for the Neo Service Layer.

## Docker Deployment

### Standard Deployment

The standard deployment uses Docker Compose to run the Neo Service Layer without SGX support. This is useful for development and testing.

```bash
cd docker
docker-compose up -d
```

### SGX Deployment

The SGX deployment uses Docker Compose to run the Neo Service Layer with SGX support. This requires a host with Intel SGX hardware support.

```bash
cd docker
docker-compose -f docker-compose.sgx.yml up -d
```

If SGX hardware is not available, the services will run in simulation mode.

## Kubernetes Deployment

The Kubernetes deployment uses Kubernetes manifests to deploy the Neo Service Layer to a Kubernetes cluster.

### Prerequisites

- Kubernetes cluster with Intel SGX support
- kubectl configured to access the cluster
- SGX device plugin installed on the cluster

### Deployment Steps

1. Create the namespace:

```bash
kubectl apply -f kubernetes/namespace.yaml
```

2. Create the ConfigMap and Secret:

```bash
kubectl apply -f kubernetes/configmap.yaml
kubectl apply -f kubernetes/secret.yaml
```

3. Deploy the SGX device plugin:

```bash
kubectl apply -f kubernetes/sgx-device-plugin.yaml
```

4. Deploy the services:

```bash
kubectl apply -f kubernetes/deployment.yaml
kubectl apply -f kubernetes/service.yaml
```

5. Deploy the ingress:

```bash
kubectl apply -f kubernetes/ingress.yaml
```

6. Deploy the monitoring and logging components:

```bash
kubectl apply -f kubernetes/monitoring/prometheus.yaml
kubectl apply -f kubernetes/monitoring/grafana.yaml
kubectl apply -f kubernetes/logging/elasticsearch.yaml
kubectl apply -f kubernetes/logging/kibana.yaml
kubectl apply -f kubernetes/logging/filebeat.yaml
kubectl apply -f kubernetes/tracing/jaeger.yaml
```

## Monitoring and Logging

The deployment includes the following monitoring and logging components:

- Prometheus: Metrics collection and storage
- Grafana: Metrics visualization
- Elasticsearch: Log storage and search
- Kibana: Log visualization
- Filebeat: Log collection
- Jaeger: Distributed tracing

### Accessing the Monitoring and Logging UIs

- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (admin/admin)
- Kibana: http://localhost:5601
- Jaeger: http://localhost:16686

## Configuration

The deployment can be configured using environment variables or configuration files. See the `configmap.yaml` and `secret.yaml` files for the available configuration options.

## Troubleshooting

### SGX Not Available

If SGX hardware is not available, the services will run in simulation mode. This is useful for development and testing, but does not provide the security guarantees of SGX.

### SGX Device Plugin Not Installed

If the SGX device plugin is not installed on the Kubernetes cluster, the pods will not be able to access the SGX devices. Install the SGX device plugin using the `sgx-device-plugin.yaml` file.

### SGX Enclave Initialization Failed

If the SGX enclave initialization fails, check the logs for error messages. Common issues include:

- SGX not enabled in BIOS
- SGX driver not installed
- SGX device files not accessible
- Insufficient memory for the enclave

### Service Not Starting

If a service is not starting, check the logs for error messages. Common issues include:

- Database connection failed
- Message queue connection failed
- Storage connection failed
- TEE host connection failed
