'use client'

import { useState } from 'react'
// import { motion } from 'framer-motion'
import {
  MagnifyingGlassIcon,
  BookOpenIcon,
  CodeBracketIcon,
  CubeIcon,
  RocketLaunchIcon,
  ShieldCheckIcon,
  BeakerIcon,
  ChartBarIcon,
  GlobeAltIcon,
  LightBulbIcon,
  DocumentTextIcon,
  ArrowRightIcon,
} from '@heroicons/react/24/outline'
import Link from 'next/link'

interface DocSection {
  id: string
  title: string
  description: string
  icon: any
  color: string
  bgColor: string
  sections: Array<{
    title: string
    href: string
    description: string
  }>
}

const docSections: DocSection[] = [
  {
    id: 'getting-started',
    title: 'Getting Started',
    description: 'Quick start guides and tutorials to get you up and running',
    icon: RocketLaunchIcon,
    color: 'text-green-500',
    bgColor: 'bg-green-500/10',
    sections: [
      {
        title: 'Quick Start Guide',
        href: '/docs/getting-started/quick-start',
        description: 'Get started with Neo Service Layer in 5 minutes'
      },
      {
        title: 'Installation',
        href: '/docs/getting-started/installation',
        description: 'Install and configure the Neo Service Layer SDK'
      },
      {
        title: 'Your First Project',
        href: '/docs/getting-started/first-project',
        description: 'Build your first dApp using our service layer'
      },
      {
        title: 'Authentication Setup',
        href: '/docs/getting-started/authentication',
        description: 'Set up user authentication and wallet integration'
      }
    ]
  },
  {
    id: 'core-services',
    title: 'Core Services',
    description: 'Essential infrastructure services for every dApp',
    icon: CubeIcon,
    color: 'text-neo-500',
    bgColor: 'bg-neo-500/10',
    sections: [
      {
        title: 'Storage Service',
        href: '/docs/services/storage',
        description: 'Decentralized data storage and retrieval'
      },
      {
        title: 'Oracle Service',
        href: '/docs/services/oracle',
        description: 'External data feeds and API integrations'
      },
      {
        title: 'Randomness Service',
        href: '/docs/services/randomness',
        description: 'Verifiable random number generation'
      },
      {
        title: 'Service Registry',
        href: '/docs/services/registry',
        description: 'Service discovery and management'
      }
    ]
  },
  {
    id: 'business-services',
    title: 'Business Services',
    description: 'Ready-to-use business logic components',
    icon: ChartBarIcon,
    color: 'text-purple-500',
    bgColor: 'bg-purple-500/10',
    sections: [
      {
        title: 'Payment Processing',
        href: '/docs/business/payments',
        description: 'Multi-token payment solutions'
      },
      {
        title: 'Marketplace',
        href: '/docs/business/marketplace',
        description: 'NFT and digital asset marketplace'
      },
      {
        title: 'Voting System',
        href: '/docs/business/voting',
        description: 'Secure and transparent governance'
      },
      {
        title: 'Identity Management',
        href: '/docs/business/identity',
        description: 'Decentralized identity and verification'
      }
    ]
  },
  {
    id: 'advanced-services',
    title: 'Advanced Services',
    description: 'Cutting-edge blockchain technology',
    icon: BeakerIcon,
    color: 'text-pink-500',
    bgColor: 'bg-pink-500/10',
    sections: [
      {
        title: 'Cross-Chain Bridge',
        href: '/docs/advanced/cross-chain',
        description: 'Interoperability with other blockchains'
      },
      {
        title: 'Zero Knowledge Proofs',
        href: '/docs/advanced/zero-knowledge',
        description: 'Privacy-preserving computations'
      },
      {
        title: 'Compute Service',
        href: '/docs/advanced/compute',
        description: 'Off-chain computation and verification'
      },
      {
        title: 'AI Integration',
        href: '/docs/advanced/ai',
        description: 'Machine learning and pattern recognition'
      }
    ]
  },
  {
    id: 'api-reference',
    title: 'API Reference',
    description: 'Complete API documentation and SDK reference',
    icon: CodeBracketIcon,
    color: 'text-blue-500',
    bgColor: 'bg-blue-500/10',
    sections: [
      {
        title: 'REST API',
        href: '/docs/api/rest',
        description: 'HTTP REST API endpoints and authentication'
      },
      {
        title: 'GraphQL API',
        href: '/docs/api/graphql',
        description: 'GraphQL schema and queries'
      },
      {
        title: 'WebSocket Events',
        href: '/docs/api/websockets',
        description: 'Real-time events and subscriptions'
      },
      {
        title: 'SDK Reference',
        href: '/docs/api/sdk',
        description: 'Complete SDK documentation and examples'
      }
    ]
  },
  {
    id: 'deployment',
    title: 'Deployment',
    description: 'Deploy and manage your applications',
    icon: GlobeAltIcon,
    color: 'text-cyan-500',
    bgColor: 'bg-cyan-500/10',
    sections: [
      {
        title: 'Production Deployment',
        href: '/docs/deployment/production',
        description: 'Deploy to Neo N3 MainNet'
      },
      {
        title: 'Testing & Staging',
        href: '/docs/deployment/testing',
        description: 'TestNet deployment and testing strategies'
      },
      {
        title: 'Monitoring & Analytics',
        href: '/docs/deployment/monitoring',
        description: 'Monitor your deployed applications'
      },
      {
        title: 'Scaling & Performance',
        href: '/docs/deployment/scaling',
        description: 'Optimize performance and handle growth'
      }
    ]
  },
  {
    id: 'security',
    title: 'Security',
    description: 'Security best practices and guidelines',
    icon: ShieldCheckIcon,
    color: 'text-red-500',
    bgColor: 'bg-red-500/10',
    sections: [
      {
        title: 'Security Best Practices',
        href: '/docs/security/best-practices',
        description: 'Essential security guidelines for dApp development'
      },
      {
        title: 'Audit Reports',
        href: '/docs/security/audits',
        description: 'Third-party security audit reports'
      },
      {
        title: 'Vulnerability Disclosure',
        href: '/docs/security/disclosure',
        description: 'How to report security vulnerabilities'
      },
      {
        title: 'Compliance',
        href: '/docs/security/compliance',
        description: 'Regulatory compliance and standards'
      }
    ]
  },
  {
    id: 'tutorials',
    title: 'Tutorials',
    description: 'Step-by-step guides and examples',
    icon: LightBulbIcon,
    color: 'text-yellow-500',
    bgColor: 'bg-yellow-500/10',
    sections: [
      {
        title: 'Build a DeFi App',
        href: '/docs/tutorials/defi-app',
        description: 'Create a complete DeFi application'
      },
      {
        title: 'NFT Marketplace',
        href: '/docs/tutorials/nft-marketplace',
        description: 'Build an NFT trading platform'
      },
      {
        title: 'DAO Governance',
        href: '/docs/tutorials/dao',
        description: 'Implement decentralized governance'
      },
      {
        title: 'Cross-Chain Bridge',
        href: '/docs/tutorials/bridge',
        description: 'Create asset bridges between chains'
      }
    ]
  }
]

const popularDocs = [
  {
    title: 'Quick Start Guide',
    href: '/docs/getting-started/quick-start',
    description: 'Get up and running in 5 minutes',
    category: 'Getting Started'
  },
  {
    title: 'Storage Service API',
    href: '/docs/services/storage',
    description: 'Store and retrieve data on Neo blockchain',
    category: 'Core Services'
  },
  {
    title: 'Payment Processing',
    href: '/docs/business/payments',
    description: 'Accept payments in multiple tokens',
    category: 'Business Services'
  },
  {
    title: 'Cross-Chain Integration',
    href: '/docs/advanced/cross-chain',
    description: 'Bridge assets between blockchains',
    category: 'Advanced'
  }
]

export default function DocsPage() {
  const [searchQuery, setSearchQuery] = useState('')
  const [filteredSections, setFilteredSections] = useState(docSections)

  const handleSearch = (query: string) => {
    setSearchQuery(query)
    if (!query.trim()) {
      setFilteredSections(docSections)
      return
    }

    const filtered = docSections.map(section => ({
      ...section,
      sections: section.sections.filter(
        item =>
          item.title.toLowerCase().includes(query.toLowerCase()) ||
          item.description.toLowerCase().includes(query.toLowerCase())
      )
    })).filter(section => 
      section.title.toLowerCase().includes(query.toLowerCase()) ||
      section.description.toLowerCase().includes(query.toLowerCase()) ||
      section.sections.length > 0
    )

    setFilteredSections(filtered)
  }

  return (
    <div className="min-h-screen bg-background-primary pt-20">
      <div className="mx-auto max-w-7xl px-6 lg:px-8 py-12">
        {/* Header */}
        <div className="text-center mb-12">
          <h1 className="text-4xl font-bold text-white mb-4">
            Neo Service Layer <span className="text-gradient">Documentation</span>
          </h1>
          <p className="text-lg text-gray-400 max-w-3xl mx-auto mb-8">
            Everything you need to build powerful decentralized applications on Neo N3.
            From quick start guides to advanced integrations.
          </p>

          {/* Search */}
          <div className="max-w-xl mx-auto">
            <div className="relative">
              <MagnifyingGlassIcon className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-gray-400" />
              <input
                type="text"
                placeholder="Search documentation..."
                value={searchQuery}
                onChange={(e) => handleSearch(e.target.value)}
                className="w-full pl-10 pr-4 py-3 bg-gray-800 border border-gray-600 rounded-lg text-white placeholder-gray-400 focus:ring-2 focus:ring-neo-500 focus:border-transparent"
              />
            </div>
          </div>
        </div>

        {/* Popular Docs */}
        {!searchQuery && (
          <div className="mb-12">
            <h2 className="text-2xl font-bold text-white mb-6">Popular Documentation</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              {popularDocs.map((doc, index) => (
                <div key={doc.href}>
                  <Link href={doc.href}>
                    <div className="glass rounded-xl p-6 hover:border-neo-500/30 transition-all duration-300 group h-full">
                      <div className="flex items-center justify-between mb-3">
                        <span className="text-xs px-2 py-1 bg-neo-500/20 text-neo-500 rounded-full">
                          {doc.category}
                        </span>
                        <ArrowRightIcon className="h-4 w-4 text-gray-400 group-hover:text-neo-500 group-hover:translate-x-1 transition-all duration-200" />
                      </div>
                      <h3 className="text-lg font-semibold text-white mb-2">{doc.title}</h3>
                      <p className="text-sm text-gray-400">{doc.description}</p>
                    </div>
                  </Link>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Documentation Sections */}
        <div>
          <h2 className="text-2xl font-bold text-white mb-8">
            {searchQuery ? `Search Results for "${searchQuery}"` : 'Documentation'}
          </h2>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
            {filteredSections.map((section, sectionIndex) => (
              <div
                key={section.id}
                className="glass rounded-xl p-6"
              >
                {/* Section Header */}
                <div className="flex items-center space-x-4 mb-6">
                  <div className={`inline-flex rounded-lg p-3 ${section.bgColor}`}>
                    <section.icon className={`h-6 w-6 ${section.color}`} />
                  </div>
                  <div>
                    <h3 className="text-xl font-semibold text-white">{section.title}</h3>
                    <p className="text-sm text-gray-400 mt-1">{section.description}</p>
                  </div>
                </div>

                {/* Section Items */}
                <div className="space-y-3">
                  {section.sections.map((item, itemIndex) => (
                    <Link key={item.href} href={item.href}>
                      <div className="flex items-center justify-between p-3 rounded-lg hover:bg-gray-800/50 transition-colors duration-200 group">
                        <div className="flex-1">
                          <h4 className="text-sm font-medium text-white group-hover:text-neo-500 transition-colors">
                            {item.title}
                          </h4>
                          <p className="text-xs text-gray-400 mt-1">{item.description}</p>
                        </div>
                        <ArrowRightIcon className="h-4 w-4 text-gray-400 group-hover:text-neo-500 group-hover:translate-x-1 transition-all duration-200" />
                      </div>
                    </Link>
                  ))}
                </div>
              </div>
            ))}
          </div>

          {searchQuery && filteredSections.length === 0 && (
            <div className="text-center py-12">
              <DocumentTextIcon className="h-12 w-12 text-gray-600 mx-auto mb-4" />
              <h3 className="text-lg font-semibold text-white mb-2">No results found</h3>
              <p className="text-gray-400">
                Try searching with different keywords or browse our documentation sections above.
              </p>
            </div>
          )}
        </div>

        {/* Help Section */}
        <div className="mt-16">
          <div className="glass rounded-xl p-8 text-center">
            <BookOpenIcon className="h-12 w-12 text-neo-500 mx-auto mb-4" />
            <h3 className="text-2xl font-bold text-white mb-4">Need More Help?</h3>
            <p className="text-gray-400 mb-6 max-w-2xl mx-auto">
              Can't find what you're looking for? Join our community, check out interactive examples,
              or contact our support team for personalized assistance.
            </p>
            <div className="flex flex-col sm:flex-row items-center justify-center space-y-4 sm:space-y-0 sm:space-x-4">
              <Link
                href="/playground"
                className="flex items-center space-x-2 px-6 py-3 bg-neo-500 text-white rounded-lg hover:bg-neo-600 transition-colors"
              >
                <BeakerIcon className="h-5 w-5" />
                <span>Try Playground</span>
              </Link>
              <a
                href="https://discord.gg/neoservicelayer"
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center space-x-2 px-6 py-3 glass text-white rounded-lg hover:border-neo-500/50 transition-colors"
              >
                <svg className="h-5 w-5" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M20.317 4.37a19.791 19.791 0 0 0-4.885-1.515.074.074 0 0 0-.079.037c-.21.375-.444.864-.608 1.25a18.27 18.27 0 0 0-5.487 0 12.64 12.64 0 0 0-.617-1.25.077.077 0 0 0-.079-.037A19.736 19.736 0 0 0 3.677 4.37a.07.07 0 0 0-.032.027C.533 9.046-.32 13.58.099 18.057a.082.082 0 0 0 .031.057 19.9 19.9 0 0 0 5.993 3.03.078.078 0 0 0 .084-.028c.462-.63.874-1.295 1.226-1.994a.076.076 0 0 0-.041-.106 13.107 13.107 0 0 1-1.872-.892.077.077 0 0 1-.008-.128 10.2 10.2 0 0 0 .372-.292.074.074 0 0 1 .077-.010c3.928 1.793 8.18 1.793 12.062 0a.074.074 0 0 1 .078.01c.12.098.246.198.373.292a.077.077 0 0 1-.006.127 12.299 12.299 0 0 1-1.873.892.077.077 0 0 0-.041.107c.36.698.772 1.362 1.225 1.993a.076.076 0 0 0 .084.028 19.839 19.839 0 0 0 6.002-3.03.077.077 0 0 0 .032-.054c.5-5.177-.838-9.674-3.549-13.66a.061.061 0 0 0-.031-.03zM8.02 15.33c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.956-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.956 2.418-2.157 2.418zm7.975 0c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.955-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.946 2.418-2.157 2.418z"/>
                </svg>
                <span>Join Discord</span>
              </a>
              <a
                href="mailto:support@neoservicelayer.com"
                className="flex items-center space-x-2 px-6 py-3 glass text-white rounded-lg hover:border-neo-500/50 transition-colors"
              >
                <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 4.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
                <span>Email Support</span>
              </a>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}