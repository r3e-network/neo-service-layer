import './globals.css'
import type { Metadata } from 'next'
import { Inter, JetBrains_Mono } from 'next/font/google'
import { ThemeProvider } from 'next-themes'
import { Toaster } from 'react-hot-toast'
import { Header } from '@/components/layout/Header'
import { Footer } from '@/components/layout/Footer'
import { AuthProvider } from '@/components/providers/AuthProvider'

const inter = Inter({
  subsets: ['latin'],
  variable: '--font-inter',
  display: 'swap',
})

const jetbrainsMono = JetBrains_Mono({
  subsets: ['latin'],
  variable: '--font-jetbrains-mono',
  display: 'swap',
})

export const metadata: Metadata = {
  title: {
    default: 'Neo Service Layer - Advanced Smart Contract Services for Neo N3',
    template: '%s | Neo Service Layer',
  },
  description: 'Professional smart contract services for Neo N3 blockchain. Deploy, manage, and scale your decentralized applications with our comprehensive service layer.',
  keywords: [
    'Neo',
    'Neo N3',
    'Smart Contracts',
    'Blockchain',
    'DeFi',
    'dApp',
    'Web3',
    'Service Layer',
    'Oracle',
    'Storage',
    'Analytics',
    'Compute',
    'Cross-Chain',
  ],
  authors: [{ name: 'Neo Service Layer Team' }],
  creator: 'Neo Service Layer Team',
  publisher: 'Neo Service Layer',
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      'max-video-preview': -1,
      'max-image-preview': 'large',
      'max-snippet': -1,
    },
  },
  openGraph: {
    type: 'website',
    locale: 'en_US',
    url: 'https://neoservicelayer.com',
    siteName: 'Neo Service Layer',
    title: 'Neo Service Layer - Advanced Smart Contract Services',
    description: 'Professional smart contract services for Neo N3 blockchain. Deploy, manage, and scale your decentralized applications.',
    images: [
      {
        url: 'https://neoservicelayer.com/og-image.jpg',
        width: 1200,
        height: 630,
        alt: 'Neo Service Layer',
      },
    ],
  },
  twitter: {
    card: 'summary_large_image',
    title: 'Neo Service Layer',
    description: 'Advanced Smart Contract Services for Neo N3',
    images: ['https://neoservicelayer.com/og-image.jpg'],
    creator: '@neoservicelayer',
  },
  icons: {
    icon: '/favicon.ico',
    shortcut: '/favicon-16x16.png',
    apple: '/apple-touch-icon.png',
  },
  manifest: '/site.webmanifest',
  metadataBase: new URL('https://neoservicelayer.com'),
  alternates: {
    canonical: '/',
  },
  category: 'technology',
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body
        className={`${inter.variable} ${jetbrainsMono.variable} font-sans bg-background-primary text-white min-h-screen`}
      >
        <ThemeProvider
          attribute="class"
          defaultTheme="dark"
          enableSystem={false}
          disableTransitionOnChange
        >
          <AuthProvider>
            <div className="flex flex-col min-h-screen">
              <Header />
              <main className="flex-1 pt-20">
                {children}
              </main>
              <Footer />
            </div>
            <Toaster
              position="top-right"
              toastOptions={{
                duration: 4000,
                style: {
                  background: '#1a1f3a',
                  color: '#fff',
                  border: '1px solid rgba(0, 212, 170, 0.3)',
                },
                success: {
                  iconTheme: {
                    primary: '#00d4aa',
                    secondary: '#fff',
                  },
                },
                error: {
                  iconTheme: {
                    primary: '#ef4444',
                    secondary: '#fff',
                  },
                },
              }}
            />
          </AuthProvider>
        </ThemeProvider>
      </body>
    </html>
  )
}