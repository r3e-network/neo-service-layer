# Neo Service Layer Website - Production Deployment Guide

This guide covers the complete deployment process for the Neo Service Layer website to production at service.neoservicelayer.com.

## 📋 Prerequisites

- Node.js 18+ and npm 8+
- PostgreSQL database
- Domain configured (service.neoservicelayer.com)
- SSL certificate
- OAuth application credentials (Google, GitHub, Twitter)

## 🚀 Quick Start Deployment

```bash
# 1. Clone and navigate to website
cd /path/to/neo-service-layer/website

# 2. Set up environment variables
cp .env.local.example .env.production.local
# Edit .env.production.local with production values

# 3. Run deployment script
./deploy.sh --start
```

## 📦 Step-by-Step Deployment

### 1. Environment Configuration

Create `.env.production.local` with production values:

```env
# Database (PostgreSQL)
DATABASE_URL="postgresql://username:password@host:5432/neo_service_layer_prod"

# NextAuth.js
NEXTAUTH_URL="https://service.neoservicelayer.com"
NEXTAUTH_SECRET="generate-with-openssl-rand-base64-32"

# OAuth Providers
GOOGLE_CLIENT_ID="your-production-google-client-id"
GOOGLE_CLIENT_SECRET="your-production-google-client-secret"
GITHUB_CLIENT_ID="your-production-github-client-id"
GITHUB_CLIENT_SECRET="your-production-github-client-secret"
TWITTER_CLIENT_ID="your-production-twitter-client-id"
TWITTER_CLIENT_SECRET="your-production-twitter-client-secret"

# Neo Service Layer
NEO_SERVICE_LAYER_API="https://api.neoservicelayer.com"
NEO_NETWORK="mainnet"

# Optional: Analytics
GOOGLE_ANALYTICS_ID="G-XXXXXXXXXX"
```

### 2. OAuth Provider Setup

#### Google OAuth
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create or select project
3. Enable Google+ API
4. Create OAuth 2.0 credentials
5. Add authorized redirect URI: `https://service.neoservicelayer.com/api/auth/callback/google`

#### GitHub OAuth
1. Go to GitHub Settings > Developer settings > OAuth Apps
2. Create new OAuth App
3. Set Homepage URL: `https://service.neoservicelayer.com`
4. Set Authorization callback URL: `https://service.neoservicelayer.com/api/auth/callback/github`

#### Twitter OAuth
1. Go to [Twitter Developer Portal](https://developer.twitter.com/)
2. Create new app with OAuth 2.0
3. Set callback URL: `https://service.neoservicelayer.com/api/auth/callback/twitter`

### 3. Database Setup

```bash
# Create production database
createdb neo_service_layer_prod

# Set DATABASE_URL in .env.production.local

# Run migrations
NODE_ENV=production npx prisma migrate deploy
NODE_ENV=production npx prisma generate
```

### 4. Build and Deploy

#### Option A: Deploy to Vercel (Recommended)

```bash
# Install Vercel CLI
npm i -g vercel

# Deploy
vercel --prod

# Set environment variables in Vercel dashboard
```

#### Option B: Deploy to VPS/Cloud Server

```bash
# Install dependencies
npm ci --production

# Build application
npm run build

# Start with PM2
npm install -g pm2
pm2 start npm --name "neo-service-layer" -- start
pm2 save
pm2 startup
```

#### Option C: Deploy with Docker

```bash
# Build Docker image
docker build -t neo-service-layer-website .

# Run container
docker run -d \
  --name neo-service-layer \
  -p 3000:3000 \
  --env-file .env.production.local \
  neo-service-layer-website
```

### 5. Nginx Configuration (if using VPS)

```nginx
server {
    listen 80;
    server_name service.neoservicelayer.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name service.neoservicelayer.com;

    ssl_certificate /path/to/ssl/cert.pem;
    ssl_certificate_key /path/to/ssl/key.pem;

    location / {
        proxy_pass http://localhost:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 6. Post-Deployment Tasks

1. **Verify deployment**:
   ```bash
   curl https://service.neoservicelayer.com
   ```

2. **Test OAuth providers**:
   - Sign in with Google
   - Sign in with GitHub  
   - Sign in with Twitter

3. **Test Neo wallet connection**:
   - Connect NeoLine wallet
   - Switch between MainNet/TestNet

4. **Monitor logs**:
   ```bash
   pm2 logs neo-service-layer
   ```

## 🔧 Maintenance

### Update Application

```bash
# Pull latest changes
git pull origin main

# Install dependencies
npm ci

# Build
npm run build

# Restart
pm2 restart neo-service-layer
```

### Database Backups

```bash
# Backup database
pg_dump neo_service_layer_prod > backup_$(date +%Y%m%d).sql

# Restore database
psql neo_service_layer_prod < backup_20240101.sql
```

### SSL Certificate Renewal

```bash
# Using Let's Encrypt
certbot renew
nginx -s reload
```

## 🚨 Troubleshooting

### Build Errors

1. **Missing dependencies**: Run `npm ci`
2. **TypeScript errors**: Run `npm run type-check`
3. **Database connection**: Verify DATABASE_URL

### Runtime Errors

1. **OAuth not working**: Check redirect URIs and credentials
2. **Database errors**: Run `npx prisma migrate deploy`
3. **Neo wallet issues**: Ensure NeoLine extension is installed

### Performance Issues

1. Enable caching in Nginx
2. Use CDN for static assets
3. Enable Next.js image optimization
4. Monitor with `pm2 monit`

## 📊 Monitoring

### Application Monitoring

```bash
# PM2 monitoring
pm2 monit

# Custom metrics
pm2 install pm2-logrotate
pm2 install pm2-auto-pull
```

### Database Monitoring

```sql
-- Active connections
SELECT count(*) FROM pg_stat_activity;

-- Database size
SELECT pg_database_size('neo_service_layer_prod');

-- Slow queries
SELECT * FROM pg_stat_statements ORDER BY total_time DESC LIMIT 10;
```

## 🔐 Security Checklist

- [ ] HTTPS enabled with valid SSL certificate
- [ ] Environment variables secured
- [ ] Database connection encrypted
- [ ] CORS configured properly
- [ ] Rate limiting enabled
- [ ] Security headers configured
- [ ] Regular dependency updates
- [ ] Database backups automated
- [ ] Monitoring and alerts configured
- [ ] OAuth apps configured with minimal scopes

## 📝 Additional Resources

- [Next.js Deployment](https://nextjs.org/docs/deployment)
- [Prisma Production](https://www.prisma.io/docs/guides/deployment)
- [NextAuth.js Production](https://next-auth.js.org/deployment)
- [Neo Documentation](https://docs.neo.org/)

## 🆘 Support

- Discord: [discord.gg/neoservicelayer](https://discord.gg/neoservicelayer)
- Email: support@neoservicelayer.com
- GitHub Issues: [GitHub](https://github.com/neo-service-layer/neo-service-layer/issues)