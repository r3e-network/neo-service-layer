# Kubernetes Ingress with TLS Configuration

This document describes the Kubernetes Ingress setup with automatic TLS certificate management for the Neo Service Layer.

## Overview

The Neo Service Layer uses NGINX Ingress Controller with cert-manager for automatic TLS certificate provisioning through Let's Encrypt. This setup provides:

- ✅ Automatic HTTPS with TLS 1.2/1.3
- ✅ HTTP to HTTPS redirection
- ✅ Security headers (HSTS, CSP, etc.)
- ✅ Rate limiting protection
- ✅ WebSocket support
- ✅ Monitoring endpoints with authentication
- ✅ Multi-domain support
- ✅ Automatic certificate renewal

## Architecture

```
Internet
    |
    v
Load Balancer (Cloud Provider)
    |
    v
NGINX Ingress Controller
    |
    +---> Main API (api.neoservicelayer.io)
    |        |
    |        v
    |     Neo Service Layer API Service
    |
    +---> Monitoring (monitoring.neoservicelayer.io)
    |        |
    |        +---> Grafana
    |        +---> Prometheus
    |        +---> Alertmanager
    |
    +---> WebSocket (ws.neoservicelayer.io)
             |
             v
         WebSocket Service
```

## Quick Setup

1. **Run the setup script**:
   ```bash
   cd /home/ubuntu/neo-service-layer
   ./k8s/setup-ingress.sh
   ```

2. **Configure DNS**:
   Point your domains to the Load Balancer IP shown by the setup script.

3. **Verify certificates**:
   ```bash
   kubectl get certificates -n neo-service-layer
   ```

## Components

### 1. NGINX Ingress Controller

- Handles incoming HTTP/HTTPS traffic
- Performs SSL termination
- Enforces rate limiting
- Adds security headers
- Routes to backend services

### 2. cert-manager

- Automatically provisions TLS certificates from Let's Encrypt
- Handles certificate renewal (30 days before expiry)
- Supports both staging and production issuers

### 3. Ingress Resources

#### Main API Ingress
- **Domains**: api.neoservicelayer.io, api-v1.neoservicelayer.io
- **Features**: CORS, rate limiting, security headers
- **Backend**: Neo Service Layer API

#### Monitoring Ingress
- **Domains**: monitoring.neoservicelayer.io, grafana.neoservicelayer.io, prometheus.neoservicelayer.io
- **Features**: Basic authentication, IP whitelisting
- **Backend**: Grafana, Prometheus, Alertmanager

#### WebSocket Ingress
- **Domains**: ws.neoservicelayer.io, events.neoservicelayer.io
- **Features**: WebSocket upgrade headers, extended timeouts
- **Backend**: WebSocket and event streaming services

## Security Features

### TLS Configuration
```yaml
ssl-protocols: "TLSv1.2 TLSv1.3"
ssl-ciphers: "ECDHE-ECDSA-AES128-GCM-SHA256:..."
ssl-prefer-server-ciphers: "true"
```

### Security Headers
- `Strict-Transport-Security`: HSTS with preload
- `X-Content-Type-Options`: nosniff
- `X-Frame-Options`: DENY
- `X-XSS-Protection`: 1; mode=block
- `Content-Security-Policy`: Restrictive CSP
- `Referrer-Policy`: strict-origin-when-cross-origin

### Rate Limiting
- **Connections**: 10 concurrent per IP
- **Requests**: 100 requests/second per IP
- **Whitelist**: Internal Kubernetes networks
- **Response**: HTTP 429 when exceeded

### Authentication
- **Monitoring endpoints**: Basic authentication required
- **API endpoints**: JWT token validation
- **Health checks**: No authentication (for load balancer probes)

## Certificate Management

### Production Issuer
```yaml
server: https://acme-v02.api.letsencrypt.org/directory
email: admin@neoservicelayer.io
```

### Staging Issuer (for testing)
```yaml
server: https://acme-staging-v02.api.letsencrypt.org/directory
email: admin@neoservicelayer.io
```

### Certificate Status
```bash
# List all certificates
kubectl get certificates -n neo-service-layer

# Describe a certificate
kubectl describe certificate neo-service-layer-tls -n neo-service-layer

# Check certificate details
kubectl get secret neo-service-layer-tls -n neo-service-layer -o yaml
```

## Monitoring

### Grafana Dashboard
Access at: https://grafana.neoservicelayer.io

Includes:
- Request rate by host
- SSL certificate expiry monitoring
- Response status codes
- TLS protocol versions
- Request latency (P95)
- Active connections
- Rate limited requests
- Upstream response times

### Prometheus Metrics
```
# NGINX metrics
nginx_ingress_controller_requests
nginx_ingress_controller_request_duration_seconds
nginx_ingress_controller_ssl_expire_time_seconds
nginx_ingress_controller_ssl_protocol_count

# cert-manager metrics
certmanager_certificate_ready_status
certmanager_certificate_expiration_timestamp_seconds
```

## Testing

### Test Script
```bash
./test-ingress.sh
```

Tests:
- Health endpoint connectivity
- Security headers presence
- Rate limiting enforcement
- SSL/TLS configuration

### Manual Testing
```bash
# Test HTTPS
curl -v https://api.neoservicelayer.io/health

# Test certificate
openssl s_client -connect api.neoservicelayer.io:443 -servername api.neoservicelayer.io

# Test rate limiting
for i in {1..150}; do curl -s -o /dev/null -w "%{http_code}\n" https://api.neoservicelayer.io/health; done
```

## Troubleshooting

### Certificate Issues
```bash
# Check cert-manager logs
kubectl logs -n cert-manager deployment/cert-manager

# Check certificate status
kubectl describe certificate -n neo-service-layer

# Check ACME orders
kubectl get orders -n neo-service-layer
kubectl describe order <order-name> -n neo-service-layer
```

### Ingress Issues
```bash
# Check NGINX logs
kubectl logs -n ingress-nginx deployment/ingress-nginx-controller

# Check Ingress status
kubectl describe ingress -n neo-service-layer

# Test internal connectivity
kubectl run test-pod --image=busybox -it --rm -- /bin/sh
wget -O- http://neo-service-layer-api.neo-service-layer.svc.cluster.local/health
```

### DNS Issues
```bash
# Verify DNS resolution
nslookup api.neoservicelayer.io
dig api.neoservicelayer.io

# Check Load Balancer IP
kubectl get svc -n ingress-nginx ingress-nginx-controller
```

## Maintenance

### Certificate Renewal
Certificates are automatically renewed 30 days before expiry. No manual intervention required.

### Updating TLS Configuration
1. Edit `k8s/nginx/nginx-config.yaml`
2. Apply changes: `kubectl apply -f k8s/nginx/nginx-config.yaml`
3. Restart NGINX: `kubectl rollout restart deployment/ingress-nginx-controller -n ingress-nginx`

### Adding New Domains
1. Update DNS to point to Load Balancer
2. Add domain to appropriate Ingress resource
3. Apply changes: `kubectl apply -f k8s/ingress/neo-service-ingress.yaml`
4. Certificate will be automatically provisioned

## Best Practices

1. **Use staging issuer first** for new domains to avoid Let's Encrypt rate limits
2. **Monitor certificate expiry** through Grafana dashboard
3. **Keep security headers updated** according to latest standards
4. **Regular security scans** of TLS configuration
5. **Backup certificates** before major changes
6. **Test in staging** before production deployment

## References

- [NGINX Ingress Controller](https://kubernetes.github.io/ingress-nginx/)
- [cert-manager Documentation](https://cert-manager.io/docs/)
- [Let's Encrypt Rate Limits](https://letsencrypt.org/docs/rate-limits/)
- [Mozilla SSL Configuration](https://ssl-config.mozilla.org/)