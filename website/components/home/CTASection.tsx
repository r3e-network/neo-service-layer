'use client'

import { ArrowRightIcon, PlayIcon, DocumentTextIcon, CodeBracketIcon } from '@heroicons/react/24/outline'
import Link from 'next/link'
import { AnimatedDiv } from '@/components/ui/AnimatedDiv'

export function CTASection() {
  return (
    <section className="relative py-20 sm:py-32">
      {/* Background decoration */}
      <div className="absolute inset-0 overflow-hidden">
        <div className="absolute inset-0 bg-gradient-to-r from-neo-500/10 via-services-business/10 to-services-technical/10" />
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-neo-500/5 rounded-full blur-3xl" />
      </div>

      <div className="relative mx-auto max-w-7xl px-6 lg:px-8">
        <AnimatedDiv direction="up" duration={600} className="text-center">
          {/* Main CTA */}
          <div className="max-w-4xl mx-auto mb-16">
            <h2 className="text-3xl font-bold tracking-tight text-white sm:text-4xl lg:text-5xl">
              Ready to build the{' '}
              <span className="text-gradient">future</span> of dApps?
            </h2>
            <p className="mt-6 text-lg leading-8 text-gray-400">
              Join thousands of developers using Neo Service Layer to build production-ready
              decentralized applications faster than ever before.
            </p>

            {/* Primary CTA buttons */}
            <div className="mt-10 flex flex-col sm:flex-row items-center justify-center gap-4">
              <Link href="/playground">
                <button className="group relative inline-flex items-center justify-center px-8 py-4 text-lg font-semibold text-white bg-neo-500 rounded-lg hover:bg-neo-600 transition-all duration-300 shadow-neo hover:shadow-neo-lg">
                  <PlayIcon className="w-5 h-5 mr-2" />
                  Try Playground
                  <ArrowRightIcon className="w-5 h-5 ml-2 group-hover:translate-x-1 transition-transform duration-300" />
                </button>
              </Link>

              <Link href="/auth/signin">
                <button className="group inline-flex items-center justify-center px-8 py-4 text-lg font-semibold text-neo-500 glass rounded-lg hover:border-neo-500/50 transition-all duration-300">
                  Get Started Free
                  <ArrowRightIcon className="w-5 h-5 ml-2 group-hover:translate-x-1 transition-transform duration-300" />
                </button>
              </Link>
            </div>
          </div>

          {/* Feature highlights grid */}
          <AnimatedDiv
            direction="up"
            duration={600}
            delay={200}
            className="grid grid-cols-1 gap-8 md:grid-cols-3 max-w-5xl mx-auto"
          >
            {/* Quick Start */}
            <div className="group text-center">
              <div className="glass rounded-xl p-6 hover:border-neo-500/30 transition-all duration-300">
                <div className="inline-flex rounded-lg p-3 bg-neo-500/10 mb-4">
                  <CodeBracketIcon className="h-6 w-6 text-neo-500" />
                </div>
                <h3 className="text-lg font-semibold text-white mb-2">
                  Quick Start
                </h3>
                <p className="text-sm text-gray-400 mb-4">
                  Deploy your first service in under 5 minutes with our interactive playground.
                </p>
                <Link href="/playground" className="text-neo-500 text-sm font-medium hover:text-neo-400 transition-colors duration-200">
                  Try now →
                </Link>
              </div>
            </div>

            {/* Documentation */}
            <div className="group text-center">
              <div className="glass rounded-xl p-6 hover:border-neo-500/30 transition-all duration-300">
                <div className="inline-flex rounded-lg p-3 bg-services-business/10 mb-4">
                  <DocumentTextIcon className="h-6 w-6 text-services-business" />
                </div>
                <h3 className="text-lg font-semibold text-white mb-2">
                  Full Documentation
                </h3>
                <p className="text-sm text-gray-400 mb-4">
                  Comprehensive guides, API references, and examples for every service.
                </p>
                <Link href="/docs" className="text-services-business text-sm font-medium hover:text-purple-400 transition-colors duration-200">
                  Learn more →
                </Link>
              </div>
            </div>

            {/* Community */}
            <div className="group text-center">
              <div className="glass rounded-xl p-6 hover:border-neo-500/30 transition-all duration-300">
                <div className="inline-flex rounded-lg p-3 bg-services-technical/10 mb-4">
                  <svg className="h-6 w-6 text-services-technical" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M20.317 4.37a19.791 19.791 0 0 0-4.885-1.515.074.074 0 0 0-.079.037c-.21.375-.444.864-.608 1.25a18.27 18.27 0 0 0-5.487 0 12.64 12.64 0 0 0-.617-1.25.077.077 0 0 0-.079-.037A19.736 19.736 0 0 0 3.677 4.37a.07.07 0 0 0-.032.027C.533 9.046-.32 13.58.099 18.057a.082.082 0 0 0 .031.057 19.9 19.9 0 0 0 5.993 3.03.078.078 0 0 0 .084-.028c.462-.63.874-1.295 1.226-1.994a.076.076 0 0 0-.041-.106 13.107 13.107 0 0 1-1.872-.892.077.077 0 0 1-.008-.128 10.2 10.2 0 0 0 .372-.292.074.074 0 0 1 .077-.010c3.928 1.793 8.18 1.793 12.062 0a.074.074 0 0 1 .078.01c.12.098.246.198.373.292a.077.077 0 0 1-.006.127 12.299 12.299 0 0 1-1.873.892.077.077 0 0 0-.041.107c.36.698.772 1.362 1.225 1.993a.076.076 0 0 0 .084.028 19.839 19.839 0 0 0 6.002-3.03.077.077 0 0 0 .032-.054c.5-5.177-.838-9.674-3.549-13.66a.061.061 0 0 0-.031-.03zM8.02 15.33c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.956-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.956 2.418-2.157 2.418zm7.975 0c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.955-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.946 2.418-2.157 2.418z"/>
                  </svg>
                </div>
                <h3 className="text-lg font-semibold text-white mb-2">
                  Join Community
                </h3>
                <p className="text-sm text-gray-400 mb-4">
                  Connect with developers, get support, and share your projects.
                </p>
                <a href="https://discord.gg/neoservicelayer" target="_blank" rel="noopener noreferrer" className="text-services-technical text-sm font-medium hover:text-pink-400 transition-colors duration-200">
                  Join Discord →
                </a>
              </div>
            </div>
          </AnimatedDiv>

          {/* Bottom stats */}
          <AnimatedDiv
            direction="up"
            duration={600}
            delay={400}
            className="mt-16 pt-8 border-t border-white/10"
          >
            <div className="flex flex-col sm:flex-row items-center justify-center space-y-4 sm:space-y-0 sm:space-x-12 text-sm text-gray-400">
              <div className="flex items-center space-x-2">
                <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                <span>Free to start • No credit card required</span>
              </div>
              <div className="flex items-center space-x-2">
                <div className="w-2 h-2 bg-blue-500 rounded-full"></div>
                <span>Open source • MIT license</span>
              </div>
              <div className="flex items-center space-x-2">
                <div className="w-2 h-2 bg-purple-500 rounded-full"></div>
                <span>Enterprise support available</span>
              </div>
            </div>
          </AnimatedDiv>
        </AnimatedDiv>
      </div>
    </section>
  )
}