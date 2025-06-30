'use client'

import Link from 'next/link'
import { ArrowRightIcon, PlayIcon, CubeIcon } from '@heroicons/react/24/outline'
import { CodeBlock } from '@/components/ui/CodeBlock'
import { AnimatedDiv } from '@/components/ui/AnimatedDiv'

const codeExample = `// Deploy and interact with Neo Service Layer
import { neoService } from '@neoservicelayer/sdk'

// Connect wallet
await neoService.connectWallet()

// Store data using StorageService
const fileId = await neoService.storage.store({
  data: 'Hello Neo Service Layer!',
  encryption: true
})

// Get random number from RandomnessService
const randomNum = await neoService.randomness.generate({
  min: 1,
  max: 100
})

// Analyze data with AnalyticsService
await neoService.analytics.track('user_action', {
  fileId,
  randomNum,
  timestamp: Date.now()
})`

export function HeroSection() {
  return (
    <section className="relative overflow-hidden py-20 sm:py-32">
      {/* Background gradient */}
      <div className="absolute inset-0 hero-gradient" />
      
      {/* Animated background elements */}
      <div className="absolute inset-0 overflow-hidden">
        <div className="absolute -top-40 -right-32 w-80 h-80 bg-neo-500/10 rounded-full blur-3xl animate-pulse" />
        <div className="absolute -bottom-40 -left-32 w-80 h-80 bg-services-business/10 rounded-full blur-3xl animate-pulse delay-1000" />
        <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-services-technical/5 rounded-full blur-3xl animate-pulse delay-2000" />
      </div>

      <div className="relative mx-auto max-w-7xl px-6 lg:px-8">
        <div className="lg:grid lg:grid-cols-12 lg:gap-8 lg:items-center">
          {/* Left content */}
          <div className="lg:col-span-6">
            <AnimatedDiv direction="up" duration={800}>
              {/* Badge */}
              <div className="flex items-center space-x-2 mb-8">
                <div className="flex items-center space-x-2 glass rounded-full px-4 py-2">
                  <CubeIcon className="h-5 w-5 text-neo-500" />
                  <span className="text-sm font-medium text-gray-300">
                    Production Ready
                  </span>
                  <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse" />
                </div>
              </div>

              {/* Heading */}
              <h1 className="text-4xl font-bold tracking-tight text-white sm:text-6xl lg:text-7xl">
                Advanced{' '}
                <span className="text-gradient">Smart Contract</span>{' '}
                Services for{' '}
                <span className="text-gradient">Neo N3</span>
              </h1>

              {/* Description */}
              <p className="mt-6 text-lg leading-8 text-gray-300 max-w-2xl">
                Deploy, manage, and scale your decentralized applications with our comprehensive 
                service layer. 30+ pre-built smart contracts covering storage, analytics, 
                oracles, cross-chain, and more.
              </p>

              {/* Features list */}
              <div className="mt-8 grid grid-cols-2 gap-4 text-sm text-gray-400">
                <div className="flex items-center space-x-2">
                  <div className="w-2 h-2 bg-neo-500 rounded-full" />
                  <span>30+ Service Contracts</span>
                </div>
                <div className="flex items-center space-x-2">
                  <div className="w-2 h-2 bg-services-business rounded-full" />
                  <span>Production Tested</span>
                </div>
                <div className="flex items-center space-x-2">
                  <div className="w-2 h-2 bg-services-technical rounded-full" />
                  <span>Open Source</span>
                </div>
                <div className="flex items-center space-x-2">
                  <div className="w-2 h-2 bg-services-specialized rounded-full" />
                  <span>Enterprise Ready</span>
                </div>
              </div>

              {/* CTA Buttons */}
              <div className="mt-10 flex flex-col sm:flex-row gap-4">
                <div>
                  <Link
                    href="/playground"
                    className="inline-flex items-center justify-center rounded-lg bg-neo-500 px-6 py-3 text-base font-semibold text-white shadow-sm hover:bg-neo-600 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-neo-500 transition-colors"
                  >
                    Try the Playground
                    <PlayIcon className="ml-2 h-5 w-5" />
                  </Link>
                </div>

                <div>
                  <Link
                    href="/docs"
                    className="inline-flex items-center justify-center rounded-lg border border-neo-500 px-6 py-3 text-base font-semibold text-neo-500 hover:bg-neo-500 hover:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-neo-500 transition-colors"
                  >
                    View Documentation
                    <ArrowRightIcon className="ml-2 h-5 w-5" />
                  </Link>
                </div>
              </div>

              {/* Social proof */}
              <div className="mt-10 flex items-center space-x-6 text-sm text-gray-400">
                <div className="flex items-center space-x-2">
                  <span>✨ Trusted by</span>
                  <span className="font-semibold text-white">500+</span>
                  <span>developers</span>
                </div>
                <div className="flex items-center space-x-2">
                  <span>🚀</span>
                  <span className="font-semibold text-white">1000+</span>
                  <span>deployments</span>
                </div>
              </div>
            </AnimatedDiv>
          </div>

          {/* Right content - Code example */}
          <div className="mt-12 lg:mt-0 lg:col-span-6">
            <AnimatedDiv direction="right" duration={800} delay={200} className="relative">
              {/* Code block with floating effect */}
              <div className="relative">
                <div className="absolute -inset-4 bg-gradient-to-r from-neo-500/20 to-services-business/20 rounded-lg blur opacity-75 animate-pulse-glow" />
                <div className="relative">
                  <CodeBlock
                    code={codeExample}
                    language="typescript"
                    filename="neo-service-layer-example.ts"
                    showLineNumbers={true}
                  />
                </div>
              </div>

              {/* Floating elements */}
              <div className="absolute -top-4 -right-4 glass rounded-lg p-3">
                <div className="flex items-center space-x-2 text-sm">
                  <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse" />
                  <span className="text-gray-300">Live on TestNet</span>
                </div>
              </div>

              <div className="absolute -bottom-4 -left-4 glass rounded-lg p-3">
                <div className="flex items-center space-x-2 text-sm">
                  <CubeIcon className="h-4 w-4 text-neo-500" />
                  <span className="text-gray-300">Neo N3 Compatible</span>
                </div>
              </div>
            </AnimatedDiv>
          </div>
        </div>
      </div>
    </section>
  )
}