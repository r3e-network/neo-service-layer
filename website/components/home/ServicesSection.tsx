'use client'

import { AnimatedDiv } from '@/components/ui/AnimatedDiv'
import {
  CloudIcon,
  CogIcon,
  ShieldCheckIcon,
  ChartBarIcon,
  GlobeAltIcon,
  BoltIcon,
  CpuChipIcon,
  KeyIcon,
  IdentificationIcon,
  BanknotesIcon,
  BeakerIcon,
  DocumentTextIcon,
} from '@heroicons/react/24/outline'

const serviceCategories = [
  {
    title: 'Core Services',
    description: 'Essential infrastructure services for every dApp',
    color: 'text-neo-500',
    bgColor: 'bg-neo-500/10',
    icon: CloudIcon,
    services: [
      { name: 'Service Registry', description: 'Centralized service discovery and management' },
      { name: 'Storage Service', description: 'Decentralized data storage and retrieval' },
      { name: 'Oracle Service', description: 'External data feeds and API integrations' },
      { name: 'Randomness Service', description: 'Verifiable random number generation' },
    ]
  },
  {
    title: 'Business Services',
    description: 'Ready-to-use business logic components',
    color: 'text-services-business',
    bgColor: 'bg-services-business/10',
    icon: BanknotesIcon,
    services: [
      { name: 'Payment Processing', description: 'Multi-token payment solutions' },
      { name: 'Marketplace', description: 'NFT and digital asset marketplace' },
      { name: 'Lending Protocol', description: 'Decentralized lending and borrowing' },
      { name: 'Insurance', description: 'Smart contract insurance coverage' },
    ]
  },
  {
    title: 'Technical Services',
    description: 'Advanced technical infrastructure',
    color: 'text-services-technical',
    bgColor: 'bg-services-technical/10',
    icon: CpuChipIcon,
    services: [
      { name: 'Cross-Chain Bridge', description: 'Interoperability with other blockchains' },
      { name: 'Compute Service', description: 'Off-chain computation and verification' },
      { name: 'Key Management', description: 'Secure key storage and recovery' },
      { name: 'Analytics', description: 'Real-time dApp analytics and insights' },
    ]
  },
  {
    title: 'Specialized Services',
    description: 'Domain-specific solutions',
    color: 'text-services-specialized',
    bgColor: 'bg-services-specialized/10',
    icon: BeakerIcon,
    services: [
      { name: 'Healthcare', description: 'Medical data management and privacy' },
      { name: 'Supply Chain', description: 'Product tracking and verification' },
      { name: 'Voting System', description: 'Secure and transparent governance' },
      { name: 'Gaming', description: 'Game mechanics and asset management' },
    ]
  },
]

export function ServicesSection() {
  return (
    <section className="relative py-20 sm:py-32">
      {/* Background decoration */}
      <div className="absolute inset-0 overflow-hidden">
        <div className="absolute top-1/4 left-0 w-72 h-72 bg-services-business/5 rounded-full blur-3xl" />
        <div className="absolute bottom-1/4 right-0 w-72 h-72 bg-services-technical/5 rounded-full blur-3xl" />
      </div>

      <div className="relative mx-auto max-w-7xl px-6 lg:px-8">
        {/* Section header */}
        <AnimatedDiv direction="up" duration={600} className="text-center mb-16">
          <h2 className="text-3xl font-bold tracking-tight text-white sm:text-4xl lg:text-5xl">
            Comprehensive{' '}
            <span className="text-gradient">Service Layer</span>
          </h2>
          <p className="mt-6 text-lg leading-8 text-gray-400 max-w-3xl mx-auto">
            Over 30 production-ready smart contracts covering everything from basic infrastructure
            to advanced business logic. Build faster with our modular service architecture.
          </p>
        </AnimatedDiv>

        {/* Service categories grid */}
        <div className="grid grid-cols-1 gap-8 lg:grid-cols-2 xl:gap-12">
          {serviceCategories.map((category, categoryIndex) => (
            <AnimatedDiv
              key={category.title}
              direction="up"
              duration={600}
              delay={categoryIndex * 100}
              className="group"
            >
              {/* Category card */}
              <div className="glass rounded-xl p-8 h-full hover:border-neo-500/30 transition-all duration-300">
                {/* Category header */}
                <div className="flex items-center space-x-4 mb-6">
                  <div className={`inline-flex rounded-lg p-3 ${category.bgColor}`}>
                    <category.icon className={`h-6 w-6 ${category.color}`} />
                  </div>
                  <div>
                    <h3 className="text-xl font-semibold text-white">
                      {category.title}
                    </h3>
                    <p className="text-sm text-gray-400 mt-1">
                      {category.description}
                    </p>
                  </div>
                </div>

                {/* Services list */}
                <div className="space-y-4">
                  {category.services.map((service, serviceIndex) => (
                    <div
                      key={service.name}
                      className="flex items-start space-x-3 p-3 rounded-lg hover:bg-white/5 transition-colors duration-200"
                    >
                      <div className="flex-shrink-0 w-2 h-2 bg-neo-500 rounded-full mt-2" />
                      <div>
                        <h4 className="text-sm font-medium text-white">
                          {service.name}
                        </h4>
                        <p className="text-xs text-gray-400 mt-1">
                          {service.description}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>

                {/* View more indicator */}
                <div className="mt-6 pt-4 border-t border-white/10">
                  <span className="text-xs text-gray-500">
                    +{Math.floor(Math.random() * 5) + 3} more services available
                  </span>
                </div>
              </div>
            </AnimatedDiv>
          ))}
        </div>

        {/* Bottom section with total services count */}
        <AnimatedDiv direction="up" duration={600} delay={500} className="text-center mt-16">
          <div className="inline-flex items-center justify-center space-x-8 glass rounded-full px-8 py-4">
            <div className="flex items-center space-x-2">
              <ShieldCheckIcon className="h-5 w-5 text-green-500" />
              <span className="text-gray-300 text-sm">30+ Services</span>
            </div>
            <div className="w-px h-6 bg-gray-600" />
            <div className="flex items-center space-x-2">
              <BoltIcon className="h-5 w-5 text-yellow-500" />
              <span className="text-gray-300 text-sm">Production Ready</span>
            </div>
            <div className="w-px h-6 bg-gray-600" />
            <div className="flex items-center space-x-2">
              <DocumentTextIcon className="h-5 w-5 text-blue-500" />
              <span className="text-gray-300 text-sm">Full Documentation</span>
            </div>
          </div>
        </AnimatedDiv>
      </div>
    </section>
  )
}