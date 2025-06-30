'use client'

import { AnimatedDiv } from '@/components/ui/AnimatedDiv'
import {
  CubeIcon,
  ShieldCheckIcon,
  BoltIcon,
  CodeBracketIcon,
  CloudIcon,
  CogIcon,
  BeakerIcon,
  ChartBarIcon,
  GlobeAltIcon,
  LockClosedIcon,
  RocketLaunchIcon,
  WrenchScrewdriverIcon,
} from '@heroicons/react/24/outline'

const features = [
  {
    name: 'Production Ready',
    description: 'All contracts are battle-tested and production-ready with comprehensive test coverage.',
    icon: ShieldCheckIcon,
    color: 'text-green-500',
    bgColor: 'bg-green-500/10',
  },
  {
    name: 'High Performance',
    description: 'Optimized smart contracts with minimal gas consumption and maximum throughput.',
    icon: BoltIcon,
    color: 'text-yellow-500',
    bgColor: 'bg-yellow-500/10',
  },
  {
    name: 'Developer Friendly',
    description: 'Simple SDKs, comprehensive documentation, and interactive playground for rapid development.',
    icon: CodeBracketIcon,
    color: 'text-blue-500',
    bgColor: 'bg-blue-500/10',
  },
  {
    name: 'Scalable Architecture',
    description: 'Modular design allows you to use only the services you need and scale as you grow.',
    icon: CloudIcon,
    color: 'text-purple-500',
    bgColor: 'bg-purple-500/10',
  },
  {
    name: 'Easy Integration',
    description: 'Plug-and-play integration with existing Neo N3 applications and development workflows.',
    icon: CogIcon,
    color: 'text-neo-500',
    bgColor: 'bg-neo-500/10',
  },
  {
    name: 'Interactive Testing',
    description: 'Test and experiment with services in our web-based playground before deployment.',
    icon: BeakerIcon,
    color: 'text-pink-500',
    bgColor: 'bg-pink-500/10',
  },
  {
    name: 'Real-time Analytics',
    description: 'Monitor your dApp performance with built-in analytics and reporting tools.',
    icon: ChartBarIcon,
    color: 'text-indigo-500',
    bgColor: 'bg-indigo-500/10',
  },
  {
    name: 'Cross-Chain Ready',
    description: 'Built-in support for cross-chain operations and interoperability protocols.',
    icon: GlobeAltIcon,
    color: 'text-cyan-500',
    bgColor: 'bg-cyan-500/10',
  },
  {
    name: 'Enterprise Security',
    description: 'Advanced security features including access controls, encryption, and audit trails.',
    icon: LockClosedIcon,
    color: 'text-red-500',
    bgColor: 'bg-red-500/10',
  },
  {
    name: 'Rapid Deployment',
    description: 'Deploy your entire service stack in minutes with automated deployment tools.',
    icon: RocketLaunchIcon,
    color: 'text-orange-500',
    bgColor: 'bg-orange-500/10',
  },
  {
    name: 'Comprehensive Toolkit',
    description: 'Complete development toolkit with CLI tools, SDKs, and integration libraries.',
    icon: WrenchScrewdriverIcon,
    color: 'text-teal-500',
    bgColor: 'bg-teal-500/10',
  },
  {
    name: 'Modular Design',
    description: 'Mix and match services to create custom solutions tailored to your specific needs.',
    icon: CubeIcon,
    color: 'text-gray-400',
    bgColor: 'bg-gray-500/10',
  },
]

export function FeaturesSection() {
  return (
    <section className="relative py-20 sm:py-32">
      {/* Background decoration */}
      <div className="absolute inset-0 overflow-hidden">
        <div className="absolute -top-40 -left-32 w-80 h-80 bg-neo-500/5 rounded-full blur-3xl" />
        <div className="absolute -bottom-40 -right-32 w-80 h-80 bg-services-business/5 rounded-full blur-3xl" />
      </div>

      <div className="relative mx-auto max-w-7xl px-6 lg:px-8">
        {/* Section header */}
        <AnimatedDiv direction="up" duration={600} className="text-center mb-16">
          <h2 className="text-3xl font-bold tracking-tight text-white sm:text-4xl lg:text-5xl">
            Everything you need to build on{' '}
            <span className="text-gradient">Neo N3</span>
          </h2>
          <p className="mt-6 text-lg leading-8 text-gray-400 max-w-3xl mx-auto">
            Neo Service Layer provides a comprehensive suite of smart contract services 
            that handle the complex infrastructure so you can focus on building great applications.
          </p>
        </AnimatedDiv>

        {/* Features grid */}
        <div className="grid grid-cols-1 gap-8 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {features.map((feature, index) => (
            <AnimatedDiv
              key={feature.name}
              direction="up"
              duration={600}
              delay={index * 50}
              className="group relative"
            >
              {/* Background glow effect */}
              <div className="absolute -inset-0.5 bg-gradient-to-r from-neo-500/20 to-services-business/20 rounded-lg blur opacity-0 group-hover:opacity-100 transition duration-300" />
              
              {/* Feature card */}
              <div className="relative glass rounded-lg p-6 h-full hover:border-neo-500/30 transition-all duration-300">
                {/* Icon */}
                <div className={`inline-flex rounded-lg p-3 ${feature.bgColor} mb-4`}>
                  <feature.icon className={`h-6 w-6 ${feature.color}`} />
                </div>

                {/* Content */}
                <div>
                  <h3 className="text-lg font-semibold text-white mb-2">
                    {feature.name}
                  </h3>
                  <p className="text-sm text-gray-400 leading-6">
                    {feature.description}
                  </p>
                </div>

                {/* Hover indicator */}
                <div className="absolute bottom-4 right-4 opacity-0 group-hover:opacity-100 transition-opacity duration-300">
                  <div className="w-2 h-2 bg-neo-500 rounded-full animate-pulse" />
                </div>
              </div>
            </AnimatedDiv>
          ))}
        </div>

        {/* Bottom CTA */}
        <AnimatedDiv direction="up" duration={600} delay={600} className="text-center mt-16">
          <div className="inline-flex items-center space-x-2 glass rounded-full px-6 py-3">
            <CubeIcon className="h-5 w-5 text-neo-500" />
            <span className="text-gray-300">
              Discover all 30+ services in our documentation
            </span>
          </div>
        </AnimatedDiv>
      </div>
    </section>
  )
}