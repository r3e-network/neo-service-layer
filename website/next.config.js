/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  swcMinify: true,
  experimental: {
    serverComponentsExternalPackages: ['@prisma/client', 'bcryptjs'],
  },
  images: {
    domains: [
      'avatars.githubusercontent.com',
      'pbs.twimg.com',
      'lh3.googleusercontent.com',
      'service.neoservicelayer.com',
    ],
    formats: ['image/webp', 'image/avif'],
  },
  env: {
    NEXTAUTH_URL: process.env.NEXTAUTH_URL || 'https://neoservicelayer.com',
    NEO_SERVICE_LAYER_API: process.env.NEO_SERVICE_LAYER_API || 'https://service.neoservicelayer.com',
  },
  async headers() {
    return [
      {
        source: '/(.*)',
        headers: [
          {
            key: 'X-Frame-Options',
            value: 'DENY',
          },
          {
            key: 'X-Content-Type-Options',
            value: 'nosniff',
          },
          {
            key: 'Referrer-Policy',
            value: 'strict-origin-when-cross-origin',
          },
          {
            key: 'Permissions-Policy',
            value: 'camera=(), microphone=(), geolocation=()',
          },
        ],
      },
    ];
  },
  // Temporarily disable redirects during build debugging
  // async redirects() {
  //   return [
  //     {
  //       source: '/github',
  //       destination: 'https://github.com/neo-service-layer/neo-service-layer',
  //       permanent: true,
  //     },
  //     {
  //       source: '/docs',
  //       destination: '/documentation',
  //       permanent: true,
  //     },
  //   ];
  // },
  webpack: (config, { dev, isServer }) => {

    // Monaco Editor support
    config.module.rules.push({
      test: /\.worker\.js$/,
      use: { loader: 'worker-loader' },
    });

    // Neo3-js and related packages
    if (!isServer) {
      config.resolve.fallback = {
        ...config.resolve.fallback,
        fs: false,
        net: false,
        tls: false,
        crypto: false,
      };
    }

    return config;
  },
  // Enable bundle analyzer
  ...(process.env.ANALYZE === 'true' && {
    webpack: (config, { buildId, dev, isServer, defaultLoaders, webpack }) => {
      if (!isServer) {
        const { BundleAnalyzerPlugin } = require('@next/bundle-analyzer')();
        config.plugins.push(
          new BundleAnalyzerPlugin({
            analyzerMode: 'server',
            analyzerPort: isServer ? 8888 : 8889,
            openAnalyzer: true,
          })
        );
      }
      return config;
    },
  }),
};

module.exports = nextConfig;