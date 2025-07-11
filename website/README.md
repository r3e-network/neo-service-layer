# Neo Service Layer Website

[![Next.js](https://img.shields.io/badge/Next.js-14-black)](https://nextjs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.0-blue)](https://www.typescriptlang.org/)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind-3.0-06B6D4)](https://tailwindcss.com/)
[![Microservices](https://img.shields.io/badge/architecture-microservices-green)](https://microservices.io/)

> **🚀 Production-Ready Website** - Official website for the Neo Service Layer microservices platform

## 🌟 Features

### **Platform Integration**
- **🏗️ Microservices Dashboard** - Monitor and manage all services
- **🔍 Service Discovery** - Real-time service health and status
- **📊 Analytics Dashboard** - Service usage metrics and insights
- **🔐 API Management** - API key generation and management

### **Developer Experience**
- **💻 Interactive Playground** - Test all microservices with live examples
- **📚 Comprehensive Docs** - Complete API reference and guides
- **🧪 Code Examples** - Copy-paste ready code snippets
- **🚀 Quick Start Wizard** - Get started in minutes

### **Technology Stack**
- **Framework**: Next.js 14 with App Router and TypeScript
- **Styling**: Tailwind CSS with custom design system
- **Animation**: Framer Motion for smooth interactions
- **Database**: PostgreSQL with Prisma ORM
- **Authentication**: NextAuth.js with OAuth providers
- **Blockchain**: Neo N3 and NeoX integration
- **Deployment**: Vercel/Docker ready

## 🚀 Quick Start

### **Prerequisites**
- Node.js 18+ and npm/yarn
- PostgreSQL 14+ database
- Docker (optional, for containerized deployment)

### **Development Setup**

```bash
# 1. Clone the repository
git clone https://github.com/r3e-network/neo-service-layer.git
cd neo-service-layer/website

# 2. Install dependencies
npm install

# 3. Set up environment variables
cp .env.local.example .env.local
# Edit .env.local with your configuration

# 4. Set up the database
npx prisma generate
npx prisma db push
npx prisma db seed

# 5. Start development server
npm run dev

# 6. Open browser
open http://localhost:3000
```

### **Docker Deployment**

```bash
# Build and run with Docker
docker build -t neo-website .
docker run -p 3000:3000 --env-file .env.local neo-website

# Or use Docker Compose
docker-compose up -d
```

## ⚙️ Configuration

### **Environment Variables**

Create a `.env.local` file with the following configuration:

```env
# Database Configuration
DATABASE_URL="postgresql://postgres:password@localhost:5432/neo_website"

# NextAuth.js Configuration
NEXTAUTH_URL="http://localhost:3000"
NEXTAUTH_SECRET="generate-with-openssl-rand-base64-32"

# OAuth Providers (Optional)
GOOGLE_CLIENT_ID="your-google-client-id"
GOOGLE_CLIENT_SECRET="your-google-client-secret"
GITHUB_CLIENT_ID="your-github-client-id"
GITHUB_CLIENT_SECRET="your-github-client-secret"

# Neo Service Layer Integration
NEO_SERVICE_API_GATEWAY="http://localhost:7000"
NEO_SERVICE_CONSUL_URL="http://localhost:8500"
NEO_SERVICE_JAEGER_URL="http://localhost:16686"
NEO_SERVICE_GRAFANA_URL="http://localhost:3000"

# Blockchain Configuration
NEO_NETWORK="testnet"
NEO_N3_RPC_URL="https://rpc.testnet.neo.org:443"
NEO_X_RPC_URL="https://neoxt4seed1.ngd.network:443"

# Feature Flags
ENABLE_PLAYGROUND=true
ENABLE_ANALYTICS=true
ENABLE_WALLET_INTEGRATION=true
```

### **OAuth Provider Setup**

Configure OAuth providers for user authentication:

#### **GitHub OAuth**
1. Go to GitHub Settings > Developer settings > OAuth Apps
2. Create new OAuth App with:
   - Homepage URL: `http://localhost:3000`
   - Authorization callback URL: `http://localhost:3000/api/auth/callback/github`

#### **Google OAuth**
1. Visit [Google Cloud Console](https://console.cloud.google.com/)
2. Create OAuth 2.0 credentials
3. Add authorized redirect URI: `http://localhost:3000/api/auth/callback/google`

### **Database Setup**

```bash
# Using Docker (Recommended)
docker run -d \
  --name neo-website-db \
  -e POSTGRES_DB=neo_website \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=password \
  -p 5432:5432 \
  postgres:16-alpine

# Initialize database schema
npx prisma db push
npx prisma generate
npx prisma db seed
```

## 🎨 Core Features

### **1. Microservices Dashboard**
Real-time monitoring and management of all platform services:
- **Service Health**: Live status of all microservices
- **Metrics Visualization**: CPU, memory, request metrics
- **Log Streaming**: Real-time log aggregation
- **Service Discovery**: Consul integration
- **Deployment Status**: Container and service states

### **2. Interactive Playground**
Test and explore all microservices with live examples:
- **Monaco Editor**: Full VS Code editing experience
- **Service Examples**: Pre-built code snippets for each service
- **Live Execution**: Real-time API calls and responses
- **Multi-Language**: Support for .NET, JavaScript, Python
- **Save & Share**: Shareable playground links

### **3. API Management Portal**
Complete API lifecycle management:
- **API Key Generation**: Secure key creation and rotation
- **Usage Analytics**: Detailed API usage metrics
- **Rate Limiting**: Configure per-key rate limits
- **Access Control**: Service-level permissions
- **Billing Integration**: Usage-based billing support

### **4. Developer Documentation**
Comprehensive documentation system:
- **API Reference**: Auto-generated from OpenAPI specs
- **Interactive Examples**: Try API calls directly
- **SDK Documentation**: Client library guides
- **Video Tutorials**: Step-by-step walkthroughs
- **Search Integration**: Full-text search across docs

### **5. Service Integration Tools**
- **Service Catalog**: Browse all available services
- **Quick Start Wizard**: Generate starter projects
- **Code Generation**: SDK code generation
- **Webhook Management**: Configure service webhooks
- **Event Subscriptions**: Real-time event streams

## 📁 Page Structure

```
/                           # Homepage with service overview
├── /services              # Service catalog and details
│   ├── /storage           # Storage service documentation
│   ├── /key-management    # Key management service docs
│   └── /[service]         # Dynamic service pages
├── /playground            # Interactive API playground
│   ├── /examples          # Pre-built examples
│   └── /share/[id]        # Shared playground sessions
├── /dashboard             # User dashboard (authenticated)
│   ├── /projects          # Project management
│   ├── /api-keys          # API key management
│   ├── /analytics         # Usage analytics
│   └── /billing           # Billing and usage
├── /docs                  # Documentation hub
│   ├── /api               # API reference
│   ├── /sdk               # SDK documentation
│   ├── /guides            # How-to guides
│   └── /tutorials         # Video tutorials
├── /monitor               # Service monitoring
│   ├── /health            # Health dashboard
│   ├── /metrics           # Grafana embed
│   └── /traces            # Jaeger tracing
└── /api                   # API routes
    ├── /auth/*            # NextAuth.js endpoints
    ├── /services/*        # Service proxy endpoints
    └── /webhooks/*        # Webhook receivers
```

## 🛠️ Development

### **Available Scripts**

```bash
# Development
npm run dev              # Start development server with hot reload
npm run dev:services     # Start with service mocking
npm run dev:docker       # Start with Docker services

# Building
npm run build            # Build production bundle
npm run build:analyze    # Analyze bundle size
npm run build:docker     # Build Docker image

# Testing
npm run test             # Run unit tests
npm run test:e2e         # Run E2E tests
npm run test:coverage    # Generate coverage report

# Code Quality
npm run lint             # Run ESLint
npm run lint:fix         # Fix linting issues
npm run type-check       # TypeScript type checking
npm run format           # Format with Prettier

# Database
npm run db:push          # Push schema changes
npm run db:seed          # Seed development data
npm run db:studio        # Open Prisma Studio
```

### **Project Structure**

```
website/
├── app/                      # Next.js 14 App Router
│   ├── (auth)/              # Authentication routes
│   ├── (dashboard)/         # Dashboard routes (protected)
│   ├── (marketing)/         # Marketing pages
│   ├── api/                 # API routes
│   └── layout.tsx           # Root layout
├── components/              # React components
│   ├── dashboard/           # Dashboard components
│   ├── services/            # Service-specific components
│   ├── playground/          # Playground components
│   ├── monitoring/          # Monitoring components
│   └── ui/                  # Shared UI components
├── lib/                     # Core libraries
│   ├── api/                 # API client utilities
│   ├── auth/                # Authentication logic
│   ├── services/            # Service integrations
│   └── utils/               # Utility functions
├── hooks/                   # Custom React hooks
├── styles/                  # Global styles
├── prisma/                  # Database schema
│   ├── schema.prisma        # Prisma schema
│   └── seed.ts              # Seed script
├── public/                  # Static assets
├── tests/                   # Test files
└── docker/                  # Docker configuration
```

## 🚀 Deployment

### **Vercel Deployment (Recommended)**

```bash
# Install Vercel CLI
npm i -g vercel

# Deploy to Vercel
vercel

# Set environment variables
vercel env add DATABASE_URL
vercel env add NEXTAUTH_SECRET
# ... add all required env vars

# Deploy to production
vercel --prod
```

### **Docker Deployment**

```bash
# Build production image
docker build -t neo-website:latest .

# Run with Docker Compose
docker-compose -f docker-compose.production.yml up -d

# Or run standalone
docker run -d \
  --name neo-website \
  -p 3000:3000 \
  --env-file .env.production \
  neo-website:latest
```

### **Kubernetes Deployment**

```yaml
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: neo-website
spec:
  replicas: 3
  selector:
    matchLabels:
      app: neo-website
  template:
    metadata:
      labels:
        app: neo-website
    spec:
      containers:
      - name: website
        image: neo-website:latest
        ports:
        - containerPort: 3000
        envFrom:
        - secretRef:
            name: neo-website-secrets
```

### **Production Environment Variables**

```env
# Required for production
NODE_ENV=production
DATABASE_URL=postgresql://user:pass@db-host:5432/neo_website
NEXTAUTH_URL=https://your-domain.com
NEXTAUTH_SECRET=production-secret-min-32-chars

# Service Integration
NEO_SERVICE_API_GATEWAY=https://api.your-domain.com
NEO_SERVICE_CONSUL_URL=https://consul.your-domain.com

# Analytics (Optional)
NEXT_PUBLIC_GA_ID=G-XXXXXXXXXX
NEXT_PUBLIC_SENTRY_DSN=https://xxx@sentry.io/xxx
```

## 🔒 Security Best Practices

### **Security Features**
- **Authentication**: OAuth 2.0 with PKCE flow
- **Session Management**: Secure, httpOnly cookies
- **CSRF Protection**: Double-submit cookie pattern
- **XSS Prevention**: Content Security Policy headers
- **Rate Limiting**: API endpoint protection
- **Input Validation**: Zod schema validation

### **Security Headers**
```typescript
// next.config.js
const securityHeaders = [
  {
    key: 'X-DNS-Prefetch-Control',
    value: 'on'
  },
  {
    key: 'Strict-Transport-Security',
    value: 'max-age=63072000; includeSubDomains; preload'
  },
  {
    key: 'X-Frame-Options',
    value: 'SAMEORIGIN'
  },
  {
    key: 'X-Content-Type-Options',
    value: 'nosniff'
  },
  {
    key: 'Referrer-Policy',
    value: 'origin-when-cross-origin'
  }
]
```

## 🧪 Testing

### **Unit Testing**
```bash
# Run unit tests
npm run test

# Run with coverage
npm run test:coverage

# Run in watch mode
npm run test:watch
```

### **E2E Testing**
```bash
# Install Playwright
npx playwright install

# Run E2E tests
npm run test:e2e

# Run E2E tests in UI mode
npm run test:e2e:ui
```

### **Performance Testing**
```bash
# Run Lighthouse CI
npm run lighthouse

# Analyze bundle size
npm run build:analyze
```

## 📊 Monitoring & Analytics

### **Application Monitoring**
- **Error Tracking**: Sentry integration
- **Performance**: Web Vitals tracking
- **Analytics**: Google Analytics 4
- **User Sessions**: FullStory/LogRocket
- **API Monitoring**: Service health checks

### **Dashboard Metrics**
- Page load times
- API response times
- Error rates
- User engagement
- Service availability

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](../CONTRIBUTING.md) for details.

### **Development Workflow**
1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push branch (`git push origin feature/amazing-feature`)
5. Open Pull Request

### **Code Standards**
- TypeScript strict mode
- ESLint + Prettier formatting
- Conventional commits
- 90%+ test coverage
- Accessibility (WCAG 2.1 AA)

## 📚 Resources

### **Documentation**
- **[Platform Docs](../docs/README.md)** - Complete platform documentation
- **[API Reference](../docs/api/README.md)** - API documentation
- **[SDK Guide](../src/SDK/NeoServiceLayer.SDK/README.md)** - SDK documentation
- **[Service Catalog](../docs/services/README.md)** - All services documentation

### **Support Channels**
- **GitHub Issues**: [Report bugs or request features](https://github.com/r3e-network/neo-service-layer/issues)
- **Discussions**: [Community discussions](https://github.com/r3e-network/neo-service-layer/discussions)
- **Email**: support@r3e.network

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

---

**🚀 Ready to build the future of blockchain applications? Start with the Neo Service Layer website!**

**Built with ❤️ for the Neo Ecosystem**