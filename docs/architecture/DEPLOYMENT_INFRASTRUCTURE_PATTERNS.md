# Deployment and Infrastructure Patterns

## Overview

This document defines the deployment architecture, infrastructure patterns, and operational procedures for the Neo Service Layer microservices platform. The architecture is designed for enterprise-grade reliability, security, and scalability using cloud-native patterns.

## Infrastructure Architecture

### Cloud-Native Foundation

#### Kubernetes Cluster Architecture
```yaml
cluster_configuration:
  kubernetes_version: "1.28+"
  nodes:
    control_plane:
      count: 3
      size: c5.large
      availability_zones: [us-west-2a, us-west-2b, us-west-2c]
    
    worker_nodes:
      compute_pool:
        count: 6-20 (auto-scaling)
        size: c5.xlarge
        availability_zones: [us-west-2a, us-west-2b, us-west-2c]
        
      sgx_pool:
        count: 3-10 (auto-scaling)
        size: m5.2xlarge (SGX-enabled)
        availability_zones: [us-west-2a, us-west-2b, us-west-2c]
        labels:
          node-type: sgx-enabled
          intel-sgx: "true"
          
  networking:
    pod_cidr: 10.244.0.0/16
    service_cidr: 10.96.0.0/16
    cni: calico
    network_policies: enabled
    
  storage:
    default_storage_class: gp3-encrypted
    backup_storage_class: glacier-ia
    persistent_volumes: dynamic_provisioning
```

#### Infrastructure Components
```
┌─────────────────────────────────────────────────────────────┐
│                    Load Balancer                            │
│                  (AWS ALB / NGINX)                          │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────┴───────────────────────────────────────┐
│                 API Gateway                                 │
│              (Kong / Istio Gateway)                         │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────┴───────────────────────────────────────┐
│                Service Mesh                                 │
│                   (Istio)                                   │
│  ┌──────────────┬──────────────┬──────────────────────────┐ │
│  │   Auth       │   Oracle     │      Compute             │ │
│  │  Services    │  Services    │     Services             │ │
│  │              │              │   (SGX Nodes)            │ │
│  ├──────────────┼──────────────┼──────────────────────────┤ │
│  │   Storage    │  Blockchain  │    Monitoring            │ │
│  │  Services    │   Services   │     Services             │ │
│  └──────────────┴──────────────┴──────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                      │
┌─────────────────────┴───────────────────────────────────────┐
│                Data Layer                                   │
│  ┌──────────────┬──────────────┬──────────────────────────┐ │
│  │  PostgreSQL  │    Redis     │      InfluxDB            │ │
│  │   Cluster    │   Cluster    │      Cluster             │ │
│  ├──────────────┼──────────────┼──────────────────────────┤ │
│  │  EventStore  │  RabbitMQ    │   Prometheus             │ │
│  │   Cluster    │   Cluster    │     + Grafana            │ │
│  └──────────────┴──────────────┴──────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## Service Deployment Patterns

### 1. Kubernetes Deployment Strategy

#### Service Template
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: neo-auth-service
  namespace: neo-services
  labels:
    app: neo-auth-service
    version: v1
    tier: authentication
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 1
  selector:
    matchLabels:
      app: neo-auth-service
      version: v1
  template:
    metadata:
      labels:
        app: neo-auth-service
        version: v1
        tier: authentication
    spec:
      securityContext:
        runAsNonRoot: true
        runAsUser: 1000
        fsGroup: 1000
      containers:
      - name: neo-auth-service
        image: neo-service-layer/auth-service:v1.2.3
        imagePullPolicy: Always
        ports:
        - containerPort: 8080
          name: http
        - containerPort: 9090
          name: grpc
        - containerPort: 8081
          name: metrics
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: JWT_SECRET_KEY
          valueFrom:
            secretKeyRef:
              name: auth-secrets
              key: jwt-secret
        - name: DATABASE_CONNECTION
          valueFrom:
            secretKeyRef:
              name: database-secrets
              key: auth-db-connection
        resources:
          requests:
            cpu: 200m
            memory: 256Mi
          limits:
            cpu: 1000m
            memory: 1Gi
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 2
        volumeMounts:
        - name: config
          mountPath: /app/config
          readOnly: true
        - name: secrets
          mountPath: /app/secrets
          readOnly: true
      volumes:
      - name: config
        configMap:
          name: neo-auth-config
      - name: secrets
        secret:
          secretName: auth-secrets
      affinity:
        podAntiAffinity:
          preferredDuringSchedulingIgnoredDuringExecution:
          - weight: 100
            podAffinityTerm:
              labelSelector:
                matchExpressions:
                - key: app
                  operator: In
                  values:
                  - neo-auth-service
              topologyKey: kubernetes.io/hostname
---
apiVersion: v1
kind: Service
metadata:
  name: neo-auth-service
  namespace: neo-services
  labels:
    app: neo-auth-service
spec:
  selector:
    app: neo-auth-service
  ports:
  - name: http
    port: 80
    targetPort: 8080
  - name: grpc
    port: 9090
    targetPort: 9090
  - name: metrics
    port: 8081
    targetPort: 8081
  type: ClusterIP
```

#### SGX-Enabled Services
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: neo-compute-service
  namespace: neo-services
spec:
  template:
    spec:
      nodeSelector:
        intel-sgx: "true"
        node-type: sgx-enabled
      containers:
      - name: neo-compute-service
        image: neo-service-layer/compute-service:v1.2.3
        env:
        - name: SGX_MODE
          value: "HW"
        - name: ENCLAVE_PATH
          value: "/opt/enclaves/"
        volumeMounts:
        - name: sgx-device
          mountPath: /dev/sgx
        - name: enclaves
          mountPath: /opt/enclaves
          readOnly: true
        securityContext:
          privileged: true  # Required for SGX device access
      volumes:
      - name: sgx-device
        hostPath:
          path: /dev/sgx
      - name: enclaves
        configMap:
          name: sgx-enclaves
      tolerations:
      - key: "sgx-node"
        operator: "Equal"
        value: "true"
        effect: "NoSchedule"
```

### 2. Database Deployment Pattern

#### PostgreSQL Cluster
```yaml
apiVersion: postgresql.cnpg.io/v1
kind: Cluster
metadata:
  name: neo-auth-postgres
  namespace: neo-databases
spec:
  instances: 3
  
  postgresql:
    parameters:
      max_connections: "200"
      shared_buffers: "256MB"
      effective_cache_size: "1GB"
      work_mem: "4MB"
      maintenance_work_mem: "64MB"
      wal_compression: "on"
      log_statement: "all"
      
  bootstrap:
    initdb:
      database: neo_auth
      owner: auth_user
      encoding: UTF8
      localeCType: C
      localeCollate: C
      
  storage:
    size: 100Gi
    storageClass: fast-ssd
    
  monitoring:
    enabled: true
    
  backup:
    retentionPolicy: "30d"
    barmanObjectStore:
      destinationPath: s3://neo-backups/postgres/auth
      s3Credentials:
        accessKeyId:
          name: backup-s3-credentials
          key: ACCESS_KEY_ID
        secretAccessKey:
          name: backup-s3-credentials
          key: SECRET_ACCESS_KEY
        region:
          name: backup-s3-credentials
          key: REGION
```

#### Redis Cluster
```yaml
apiVersion: redis.redis.opstreelabs.in/v1beta1
kind: RedisCluster
metadata:
  name: neo-redis-cluster
  namespace: neo-databases
spec:
  clusterSize: 6
  clusterVersion: v7
  persistenceEnabled: true
  redisExporter:
    enabled: true
    image: oliver006/redis_exporter:latest
  storage:
    volumeClaimTemplate:
      spec:
        accessModes: ["ReadWriteOnce"]
        resources:
          requests:
            storage: 10Gi
        storageClassName: fast-ssd
  securityContext:
    runAsUser: 1000
    runAsGroup: 1000
    fsGroup: 1000
```

### 3. Service Mesh Configuration

#### Istio Service Mesh
```yaml
apiVersion: install.istio.io/v1alpha1
kind: IstioOperator
metadata:
  name: neo-service-layer
spec:
  values:
    global:
      meshID: neo-mesh
      network: neo-network
    pilot:
      env:
        ENABLE_WORKLOAD_ENTRY_AUTOREGISTRATION: true
        EXTERNAL_ISTIOD: true
  components:
    pilot:
      k8s:
        resources:
          requests:
            cpu: 200m
            memory: 512Mi
        hpaSpec:
          maxReplicas: 5
          minReplicas: 2
    ingressGateways:
    - name: istio-ingressgateway
      enabled: true
      k8s:
        service:
          type: LoadBalancer
          annotations:
            service.beta.kubernetes.io/aws-load-balancer-type: nlb
            service.beta.kubernetes.io/aws-load-balancer-ssl-cert: arn:aws:acm:region:account:certificate/cert-id
        hpaSpec:
          maxReplicas: 10
          minReplicas: 3
```

#### Traffic Management
```yaml
apiVersion: networking.istio.io/v1beta1
kind: VirtualService
metadata:
  name: neo-services-routing
  namespace: neo-services
spec:
  hosts:
  - api.neo-service-layer.com
  gateways:
  - neo-gateway
  http:
  - match:
    - uri:
        prefix: /api/auth
    route:
    - destination:
        host: neo-auth-service
        port:
          number: 80
    timeout: 30s
    retries:
      attempts: 3
      perTryTimeout: 10s
      
  - match:
    - uri:
        prefix: /api/oracle
    route:
    - destination:
        host: neo-oracle-service
        port:
          number: 80
    timeout: 60s
    
  - match:
    - uri:
        prefix: /api/compute
    route:
    - destination:
        host: neo-compute-service
        port:
          number: 80
    timeout: 300s  # Longer timeout for compute operations
---
apiVersion: networking.istio.io/v1beta1
kind: DestinationRule
metadata:
  name: neo-services-circuit-breaker
  namespace: neo-services
spec:
  host: "*.neo-services.svc.cluster.local"
  trafficPolicy:
    outlierDetection:
      consecutive5xxErrors: 5
      interval: 30s
      baseEjectionTime: 30s
      maxEjectionPercent: 50
    circuitBreaker:
      consecutiveErrors: 5
      interval: 30s
      baseEjectionTime: 30s
```

### 4. Security Configuration

#### Network Policies
```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: neo-auth-service-netpol
  namespace: neo-services
spec:
  podSelector:
    matchLabels:
      app: neo-auth-service
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: istio-system
    - podSelector:
        matchLabels:
          app: neo-api-gateway
    ports:
    - protocol: TCP
      port: 8080
  egress:
  - to:
    - namespaceSelector:
        matchLabels:
          name: neo-databases
    ports:
    - protocol: TCP
      port: 5432  # PostgreSQL
  - to:
    - namespaceSelector:
        matchLabels:
          name: neo-databases
    ports:
    - protocol: TCP
      port: 6379  # Redis
  - to: []  # Allow DNS
    ports:
    - protocol: UDP
      port: 53
```

#### Pod Security Standards
```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: neo-services
  labels:
    pod-security.kubernetes.io/enforce: restricted
    pod-security.kubernetes.io/audit: restricted
    pod-security.kubernetes.io/warn: restricted
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: neo-auth-service-sa
  namespace: neo-services
automountServiceAccountToken: false
---
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: neo-auth-service-role
  namespace: neo-services
rules:
- apiGroups: [""]
  resources: ["secrets", "configmaps"]
  verbs: ["get", "list"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: neo-auth-service-binding
  namespace: neo-services
subjects:
- kind: ServiceAccount
  name: neo-auth-service-sa
  namespace: neo-services
roleRef:
  kind: Role
  name: neo-auth-service-role
  apiGroup: rbac.authorization.k8s.io
```

## Monitoring and Observability

### Prometheus Configuration
```yaml
apiVersion: monitoring.coreos.com/v1
kind: Prometheus
metadata:
  name: neo-prometheus
  namespace: monitoring
spec:
  serviceAccountName: prometheus
  replicas: 2
  retention: 30d
  storage:
    volumeClaimTemplate:
      spec:
        storageClassName: fast-ssd
        resources:
          requests:
            storage: 100Gi
  serviceMonitorSelector:
    matchLabels:
      monitoring: neo-services
  ruleSelector:
    matchLabels:
      monitoring: neo-services
  resources:
    requests:
      memory: 2Gi
      cpu: 1
    limits:
      memory: 4Gi
      cpu: 2
---
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: neo-services-monitor
  namespace: monitoring
  labels:
    monitoring: neo-services
spec:
  selector:
    matchLabels:
      monitoring: "true"
  endpoints:
  - port: metrics
    interval: 30s
    path: /metrics
```

### Grafana Dashboards
```yaml
apiVersion: integreatly.org/v1alpha1
kind: GrafanaDashboard
metadata:
  name: neo-services-overview
  namespace: monitoring
spec:
  json: |
    {
      "dashboard": {
        "title": "Neo Service Layer Overview",
        "panels": [
          {
            "title": "Service Availability",
            "type": "stat",
            "targets": [
              {
                "expr": "avg(up{job=~\"neo-.*-service\"})"
              }
            ]
          },
          {
            "title": "Request Rate",
            "type": "graph",
            "targets": [
              {
                "expr": "sum(rate(http_requests_total[5m])) by (service)"
              }
            ]
          },
          {
            "title": "Response Time",
            "type": "graph",
            "targets": [
              {
                "expr": "histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))"
              }
            ]
          }
        ]
      }
    }
```

### Distributed Tracing
```yaml
apiVersion: jaegertracing.io/v1
kind: Jaeger
metadata:
  name: neo-jaeger
  namespace: observability
spec:
  strategy: production
  storage:
    type: elasticsearch
    elasticsearch:
      nodeCount: 3
      redundancyPolicy: SingleRedundancy
      storage:
        storageClassName: fast-ssd
        size: 100Gi
  collector:
    replicas: 3
    resources:
      requests:
        cpu: 200m
        memory: 256Mi
      limits:
        cpu: 500m
        memory: 512Mi
  query:
    replicas: 2
    resources:
      requests:
        cpu: 200m
        memory: 256Mi
      limits:
        cpu: 500m
        memory: 512Mi
```

## Auto-Scaling Configuration

### Horizontal Pod Autoscaler
```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: neo-auth-service-hpa
  namespace: neo-services
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: neo-auth-service
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
        averageValue: "100"
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
      - type: Percent
        value: 50
        periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 60
      policies:
      - type: Percent
        value: 100
        periodSeconds: 30
      - type: Pods
        value: 2
        periodSeconds: 30
```

### Vertical Pod Autoscaler
```yaml
apiVersion: autoscaling.k8s.io/v1
kind: VerticalPodAutoscaler
metadata:
  name: neo-oracle-service-vpa
  namespace: neo-services
spec:
  targetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: neo-oracle-service
  updatePolicy:
    updateMode: "Auto"
  resourcePolicy:
    containerPolicies:
    - containerName: neo-oracle-service
      minAllowed:
        cpu: 100m
        memory: 128Mi
      maxAllowed:
        cpu: 2000m
        memory: 4Gi
      controlledResources: ["cpu", "memory"]
```

## Backup and Disaster Recovery

### Velero Backup Configuration
```yaml
apiVersion: velero.io/v1
kind: Schedule
metadata:
  name: neo-services-backup
  namespace: velero
spec:
  schedule: "0 2 * * *"  # Daily at 2 AM
  template:
    includedNamespaces:
    - neo-services
    - neo-databases
    includedResources:
    - "*"
    excludedResources:
    - events
    - events.events.k8s.io
    storageLocation: aws-backup
    ttl: 168h  # 7 days
---
apiVersion: velero.io/v1
kind: BackupStorageLocation
metadata:
  name: aws-backup
  namespace: velero
spec:
  provider: aws
  objectStorage:
    bucket: neo-service-layer-backups
    prefix: kubernetes
  config:
    region: us-west-2
    s3ForcePathStyle: "false"
```

### Database Backup Strategy
```yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: postgres-backup
  namespace: neo-databases
spec:
  schedule: "0 1 * * *"  # Daily at 1 AM
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: postgres-backup
            image: postgres:16-alpine
            env:
            - name: PGHOST
              value: neo-auth-postgres-rw
            - name: PGDATABASE
              value: neo_auth
            - name: PGUSER
              valueFrom:
                secretKeyRef:
                  name: postgres-auth-credentials
                  key: username
            - name: PGPASSWORD
              valueFrom:
                secretKeyRef:
                  name: postgres-auth-credentials
                  key: password
            - name: AWS_ACCESS_KEY_ID
              valueFrom:
                secretKeyRef:
                  name: backup-s3-credentials
                  key: ACCESS_KEY_ID
            - name: AWS_SECRET_ACCESS_KEY
              valueFrom:
                secretKeyRef:
                  name: backup-s3-credentials
                  key: SECRET_ACCESS_KEY
            command:
            - /bin/sh
            - -c
            - |
              BACKUP_FILE="neo_auth_$(date +%Y%m%d_%H%M%S).sql.gz"
              pg_dump --verbose --clean --no-owner --no-privileges | gzip > /tmp/$BACKUP_FILE
              aws s3 cp /tmp/$BACKUP_FILE s3://neo-service-layer-backups/postgres/auth/
              echo "Backup completed: $BACKUP_FILE"
            volumeMounts:
            - name: backup-storage
              mountPath: /tmp
          volumes:
          - name: backup-storage
            emptyDir: {}
          restartPolicy: OnFailure
```

## CI/CD Pipeline Configuration

### GitOps with ArgoCD
```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: neo-services
  namespace: argocd
spec:
  project: default
  source:
    repoURL: https://github.com/neo-service-layer/k8s-manifests
    path: overlays/production
    targetRevision: main
  destination:
    server: https://kubernetes.default.svc
    namespace: neo-services
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
    syncOptions:
    - CreateNamespace=true
    retry:
      limit: 5
      backoff:
        duration: 5s
        factor: 2
        maxDuration: 3m
---
apiVersion: argoproj.io/v1alpha1
kind: AppProject
metadata:
  name: neo-service-layer
  namespace: argocd
spec:
  description: Neo Service Layer Applications
  sourceRepos:
  - https://github.com/neo-service-layer/*
  destinations:
  - namespace: neo-services
    server: https://kubernetes.default.svc
  - namespace: neo-databases
    server: https://kubernetes.default.svc
  clusterResourceWhitelist:
  - group: ''
    kind: Namespace
  - group: networking.k8s.io
    kind: NetworkPolicy
  namespaceResourceWhitelist:
  - group: apps
    kind: Deployment
  - group: ''
    kind: Service
  - group: networking.istio.io
    kind: VirtualService
```

### GitHub Actions Workflow
```yaml
name: Deploy to Kubernetes
on:
  push:
    branches: [main]
    paths: ['src/**', 'k8s/**']

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 9.0.x
        
    - name: Build and Test
      run: |
        dotnet restore
        dotnet build --no-restore
        dotnet test --no-build --verbosity normal
        
    - name: Security Scan
      uses: github/codeql-action/analyze@v2
      with:
        languages: csharp
        
    - name: Build Container Images
      run: |
        docker build -t neo-service-layer/auth-service:${{ github.sha }} -f src/Services/Authentication/Dockerfile .
        docker build -t neo-service-layer/oracle-service:${{ github.sha }} -f src/Services/Oracle/Dockerfile .
        docker build -t neo-service-layer/compute-service:${{ github.sha }} -f src/Services/Compute/Dockerfile .
        
    - name: Push to Registry
      env:
        REGISTRY_URL: ${{ secrets.CONTAINER_REGISTRY_URL }}
      run: |
        echo ${{ secrets.CONTAINER_REGISTRY_PASSWORD }} | docker login -u ${{ secrets.CONTAINER_REGISTRY_USERNAME }} --password-stdin $REGISTRY_URL
        docker push neo-service-layer/auth-service:${{ github.sha }}
        docker push neo-service-layer/oracle-service:${{ github.sha }}
        docker push neo-service-layer/compute-service:${{ github.sha }}
        
    - name: Update Kubernetes Manifests
      run: |
        sed -i 's|image: neo-service-layer/auth-service:.*|image: neo-service-layer/auth-service:${{ github.sha }}|' k8s/auth-service/deployment.yaml
        sed -i 's|image: neo-service-layer/oracle-service:.*|image: neo-service-layer/oracle-service:${{ github.sha }}|' k8s/oracle-service/deployment.yaml
        sed -i 's|image: neo-service-layer/compute-service:.*|image: neo-service-layer/compute-service:${{ github.sha }}|' k8s/compute-service/deployment.yaml
        
    - name: Commit and Push Changes
      run: |
        git config --local user.email "action@github.com"
        git config --local user.name "GitHub Action"
        git add k8s/
        git commit -m "Update container images to ${{ github.sha }}"
        git push
```

## Cost Optimization

### Resource Optimization
```yaml
# Right-sizing recommendations
resource_recommendations:
  small_services:
    requests:
      cpu: 50m
      memory: 64Mi
    limits:
      cpu: 200m
      memory: 256Mi
      
  medium_services:
    requests:
      cpu: 100m
      memory: 128Mi
    limits:
      cpu: 500m
      memory: 512Mi
      
  large_services:
    requests:
      cpu: 200m
      memory: 256Mi
    limits:
      cpu: 1000m
      memory: 1Gi

# Spot instance configuration
node_groups:
  spot_compute:
    instance_types: [c5.large, c5.xlarge, m5.large, m5.xlarge]
    spot_allocation_strategy: diversified
    max_spot_price: 0.05
    labels:
      node-type: spot
      workload-type: batch
    taints:
    - key: spot
      value: "true"
      effect: NoSchedule
```

This comprehensive deployment and infrastructure pattern provides a robust, scalable, and secure foundation for the Neo Service Layer microservices architecture, ensuring high availability, observability, and cost efficiency in production environments.