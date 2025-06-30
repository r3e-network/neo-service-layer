# Neo Service Layer Website

The official website for Neo Service Layer - Advanced smart contract services for Neo N3 blockchain.

## 🚀 Features

- **Modern Next.js 14** with TypeScript and App Router
- **Authentication** with NextAuth.js (Google, GitHub, Twitter)
- **Neo Wallet Integration** with NeoLine support
- **Interactive Playground** for testing smart contracts
- **Responsive Design** with Tailwind CSS and Framer Motion
- **Database Integration** with Prisma and PostgreSQL
- **Real-time Analytics** and user management
- **Professional Documentation** and API reference

## 🛠️ Tech Stack

- **Framework**: Next.js 14 with TypeScript
- **Styling**: Tailwind CSS
- **Animation**: Framer Motion
- **Database**: PostgreSQL with Prisma ORM
- **Authentication**: NextAuth.js
- **Neo Integration**: neo3-js, NeoLine
- **Deployment**: Vercel/Netlify ready

## 📦 Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/neo-service-layer/neo-service-layer.git
   cd neo-service-layer/website
   ```

2. **Install dependencies**
   ```bash
   npm install
   ```

3. **Set up environment variables**
   ```bash
   cp .env.local.example .env.local
   # Edit .env.local with your values
   ```

4. **Set up the database**
   ```bash
   npx prisma generate
   npx prisma db push
   ```

5. **Run the development server**
   ```bash
   npm run dev
   ```

6. **Open your browser**
   Navigate to [http://localhost:3000](http://localhost:3000)

## 🔧 Configuration

### Environment Variables

Create a `.env.local` file with the following variables:

```env
# Database
DATABASE_URL="postgresql://username:password@localhost:5432/neo_service_layer"

# NextAuth.js
NEXTAUTH_URL="http://localhost:3000"
NEXTAUTH_SECRET="your-secret-key"

# OAuth Providers
GOOGLE_CLIENT_ID="your-google-client-id"
GOOGLE_CLIENT_SECRET="your-google-client-secret"
GITHUB_CLIENT_ID="your-github-client-id"
GITHUB_CLIENT_SECRET="your-github-client-secret"
TWITTER_CLIENT_ID="your-twitter-client-id"
TWITTER_CLIENT_SECRET="your-twitter-client-secret"

# Neo Service Layer
NEO_SERVICE_LAYER_API="https://service.neoservicelayer.com"
NEO_NETWORK="testnet"
```

### OAuth Setup

1. **Google OAuth**
   - Go to [Google Cloud Console](https://console.cloud.google.com/)
   - Create a new project or select existing one
   - Enable Google+ API
   - Create OAuth 2.0 credentials
   - Add authorized redirect URI: `http://localhost:3000/api/auth/callback/google`

2. **GitHub OAuth**
   - Go to GitHub Settings > Developer settings > OAuth Apps
   - Create a new OAuth App
   - Set Authorization callback URL: `http://localhost:3000/api/auth/callback/github`

3. **Twitter OAuth**
   - Go to [Twitter Developer Portal](https://developer.twitter.com/)
   - Create a new app
   - Set callback URL: `http://localhost:3000/api/auth/callback/twitter`

### Database Setup

This project uses PostgreSQL with Prisma ORM:

1. **Install PostgreSQL**
2. **Create a database**
3. **Update DATABASE_URL in .env.local**
4. **Run migrations**
   ```bash
   npx prisma db push
   npx prisma generate
   ```

## 🎯 Key Features

### 1. Authentication System
- Multiple OAuth providers (Google, GitHub, Twitter)
- NeoLine wallet integration
- User profile management
- Role-based access control

### 2. Interactive Playground
- Live code editor with Monaco Editor
- Execute smart contract operations
- Real-time results and feedback
- Multiple service examples

### 3. Wallet Integration
- NeoLine wallet connection
- Multiple network support (MainNet/TestNet)
- Balance display and management
- Transaction history

### 4. User Dashboard
- Project management
- API key management
- Usage analytics
- Deployment tracking

### 5. Documentation
- Comprehensive API reference
- Interactive examples
- Getting started guides
- Service-specific documentation

## 📱 Pages Structure

```
/                    # Homepage
/playground          # Interactive code playground
/services           # Services overview
/docs               # Documentation
/dashboard          # User dashboard (authenticated)
/profile            # User profile (authenticated)
/auth/signin        # Sign in page
/api/auth/*         # NextAuth.js API routes
```

## 🔧 Development

### Available Scripts

```bash
npm run dev          # Start development server
npm run build        # Build for production
npm run start        # Start production server
npm run lint         # Run ESLint
npm run type-check   # Run TypeScript type checking
npm run analyze      # Analyze bundle size
```

### Project Structure

```
website/
├── app/                    # Next.js 14 App Router
│   ├── layout.tsx         # Root layout
│   ├── page.tsx           # Homepage
│   ├── playground/        # Playground pages
│   └── auth/              # Authentication pages
├── components/            # React components
│   ├── layout/           # Layout components
│   ├── ui/               # UI components
│   ├── home/             # Homepage components
│   └── wallet/           # Wallet components
├── lib/                  # Utility libraries
│   ├── auth.ts          # NextAuth configuration
│   ├── prisma.ts        # Prisma client
│   └── neo.ts           # Neo blockchain utilities
├── prisma/              # Database schema
├── styles/              # Global styles
├── types/               # TypeScript type definitions
└── utils/               # Utility functions
```

## 🚀 Deployment

### Vercel (Recommended)

1. **Push to GitHub**
2. **Connect to Vercel**
3. **Set environment variables**
4. **Deploy**

### Manual Deployment

1. **Build the application**
   ```bash
   npm run build
   ```

2. **Start the production server**
   ```bash
   npm start
   ```

### Environment Variables for Production

Make sure to set all required environment variables in your deployment platform:
- Database URL
- NextAuth secret and URL
- OAuth provider credentials
- Neo Service Layer API URL

## 🛡️ Security

- All OAuth providers use secure HTTPS redirects
- NextAuth.js handles session management securely
- Environment variables are never exposed to the client
- CSRF protection enabled
- Secure headers configuration

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Documentation**: [docs.neoservicelayer.com](https://docs.neoservicelayer.com)
- **Discord**: [discord.gg/neoservicelayer](https://discord.gg/neoservicelayer)
- **GitHub Issues**: [GitHub Issues](https://github.com/neo-service-layer/neo-service-layer/issues)
- **Email**: support@neoservicelayer.com

## 🔗 Links

- **Main Website**: [neoservicelayer.com](https://neoservicelayer.com)
- **Service API**: [service.neoservicelayer.com](https://service.neoservicelayer.com)
- **GitHub**: [github.com/neo-service-layer](https://github.com/neo-service-layer)
- **Twitter**: [@neoservicelayer](https://twitter.com/neoservicelayer)