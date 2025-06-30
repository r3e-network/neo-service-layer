'use client'

import Link from 'next/link'
import {
  CubeIcon,
  HeartIcon,
  CodeBracketIcon,
  DocumentTextIcon,
  BeakerIcon,
  UserGroupIcon,
} from '@heroicons/react/24/outline'

const footerNavigation = {
  main: [
    { name: 'Services', href: '/services' },
    { name: 'Documentation', href: '/docs' },
    { name: 'Playground', href: '/playground' },
    { name: 'API Reference', href: '/api' },
    { name: 'Blog', href: '/blog' },
    { name: 'Community', href: '/community' },
  ],
  services: [
    { name: 'Storage Service', href: '/services/storage' },
    { name: 'Analytics Service', href: '/services/analytics' },
    { name: 'Oracle Service', href: '/services/oracle' },
    { name: 'Randomness Service', href: '/services/randomness' },
    { name: 'Cross-Chain Service', href: '/services/cross-chain' },
    { name: 'All Services', href: '/services' },
  ],
  developers: [
    { name: 'Quick Start', href: '/docs/quick-start' },
    { name: 'SDK Reference', href: '/docs/sdk' },
    { name: 'Examples', href: '/docs/examples' },
    { name: 'GitHub', href: 'https://github.com/neo-service-layer' },
    { name: 'API Status', href: '/status' },
    { name: 'Changelog', href: '/changelog' },
  ],
  company: [
    { name: 'About', href: '/about' },
    { name: 'Team', href: '/team' },
    { name: 'Careers', href: '/careers' },
    { name: 'Contact', href: '/contact' },
    { name: 'Press Kit', href: '/press' },
    { name: 'Partners', href: '/partners' },
  ],
  legal: [
    { name: 'Privacy Policy', href: '/privacy' },
    { name: 'Terms of Service', href: '/terms' },
    { name: 'Cookie Policy', href: '/cookies' },
    { name: 'Security', href: '/security' },
    { name: 'License', href: '/license' },
  ],
  social: [
    {
      name: 'Twitter',
      href: 'https://twitter.com/neoservicelayer',
      icon: (props: any) => (
        <svg fill="currentColor" viewBox="0 0 24 24" {...props}>
          <path d="M8.29 20.251c7.547 0 11.675-6.253 11.675-11.675 0-.178 0-.355-.012-.53A8.348 8.348 0 0022 5.92a8.19 8.19 0 01-2.357.646 4.118 4.118 0 001.804-2.27 8.224 8.224 0 01-2.605.996 4.107 4.107 0 00-6.993 3.743 11.65 11.65 0 01-8.457-4.287 4.106 4.106 0 001.27 5.477A4.072 4.072 0 012.8 9.713v.052a4.105 4.105 0 003.292 4.022 4.095 4.095 0 01-1.853.07 4.108 4.108 0 003.834 2.85A8.233 8.233 0 012 18.407a11.616 11.616 0 006.29 1.84" />
        </svg>
      ),
    },
    {
      name: 'GitHub',
      href: 'https://github.com/neo-service-layer',
      icon: (props: any) => (
        <svg fill="currentColor" viewBox="0 0 24 24" {...props}>
          <path
            fillRule="evenodd"
            d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z"
            clipRule="evenodd"
          />
        </svg>
      ),
    },
    {
      name: 'Discord',
      href: 'https://discord.gg/neoservicelayer',
      icon: (props: any) => (
        <svg fill="currentColor" viewBox="0 0 24 24" {...props}>
          <path d="M20.317 4.37a19.791 19.791 0 0 0-4.885-1.515a.074.074 0 0 0-.079.037c-.21.375-.444.864-.608 1.25a18.27 18.27 0 0 0-5.487 0a12.64 12.64 0 0 0-.617-1.25a.077.077 0 0 0-.079-.037A19.736 19.736 0 0 0 3.677 4.37a.07.07 0 0 0-.032.027C.533 9.046-.32 13.58.099 18.057a.082.082 0 0 0 .031.057a19.9 19.9 0 0 0 5.993 3.03a.078.078 0 0 0 .084-.028a14.09 14.09 0 0 0 1.226-1.994a.076.076 0 0 0-.041-.106a13.107 13.107 0 0 1-1.872-.892a.077.077 0 0 1-.008-.128a10.2 10.2 0 0 0 .372-.292a.074.074 0 0 1 .077-.010c3.928 1.793 8.18 1.793 12.062 0a.074.074 0 0 1 .078.01c.12.098.246.196.373.292a.077.077 0 0 1-.006.127a12.299 12.299 0 0 1-1.873.892a.077.077 0 0 0-.041.107c.36.698.772 1.362 1.225 1.993a.076.076 0 0 0 .084.028a19.839 19.839 0 0 0 6.002-3.03a.077.077 0 0 0 .032-.054c.5-5.177-.838-9.674-3.549-13.66a.061.061 0 0 0-.031-.03zM8.02 15.33c-1.183 0-2.157-1.085-2.157-2.419c0-1.333.956-2.419 2.157-2.419c1.21 0 2.176 1.096 2.157 2.42c0 1.333-.956 2.418-2.157 2.418zm7.975 0c-1.183 0-2.157-1.085-2.157-2.419c0-1.333.955-2.419 2.157-2.419c1.21 0 2.176 1.096 2.157 2.42c0 1.333-.946 2.418-2.157 2.418z"/>
        </svg>
      ),
    },
    {
      name: 'Telegram',
      href: 'https://t.me/neoservicelayer',
      icon: (props: any) => (
        <svg fill="currentColor" viewBox="0 0 24 24" {...props}>
          <path d="M11.944 0A12 12 0 0 0 0 12a12 12 0 0 0 12 12 12 12 0 0 0 12-12A12 12 0 0 0 12 0a12 12 0 0 0-.056 0zm4.962 7.224c.1-.002.321.023.465.14a.506.506 0 0 1 .171.325c.016.093.036.306.02.472-.18 1.898-.962 6.502-1.36 8.627-.168.9-.499 1.201-.82 1.23-.696.065-1.225-.46-1.9-.902-1.056-.693-1.653-1.124-2.678-1.8-1.185-.78-.417-1.21.258-1.91.177-.184 3.247-2.977 3.307-3.23.007-.032.014-.15-.056-.212s-.174-.041-.249-.024c-.106.024-1.793 1.14-5.061 3.345-.48.33-.913.49-1.302.48-.428-.008-1.252-.241-1.865-.44-.752-.245-1.349-.374-1.297-.789.027-.216.325-.437.893-.663 3.498-1.524 5.83-2.529 6.998-3.014 3.332-1.386 4.025-1.627 4.476-1.635z"/>
        </svg>
      ),
    },
  ],
}

export function Footer() {
  return (
    <footer className="relative bg-background-secondary border-t border-gray-800">
      {/* Background decoration */}
      <div className="absolute inset-0 overflow-hidden">
        <div className="absolute -top-40 -right-32 w-80 h-80 bg-neo-500/5 rounded-full blur-3xl" />
        <div className="absolute -bottom-40 -left-32 w-80 h-80 bg-services-business/5 rounded-full blur-3xl" />
      </div>

      <div className="relative mx-auto max-w-7xl px-6 lg:px-8">
        <div className="py-16">
          <div className="xl:grid xl:grid-cols-3 xl:gap-8">
            {/* Company info */}
            <div className="space-y-6">
              <Link href="/" className="flex items-center space-x-2">
                <CubeIcon className="h-8 w-8 text-neo-500" />
                <span className="text-xl font-bold text-gradient">
                  Neo Service Layer
                </span>
              </Link>
              <p className="text-base text-gray-400 max-w-md">
                Advanced smart contract services for Neo N3 blockchain. 
                Build better dApps with our comprehensive service layer.
              </p>
              <div className="flex space-x-6">
                {footerNavigation.social.map((item) => (
                  <a
                    key={item.name}
                    href={item.href}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-gray-400 hover:text-neo-500 transition-colors"
                  >
                    <span className="sr-only">{item.name}</span>
                    <item.icon className="h-6 w-6" aria-hidden="true" />
                  </a>
                ))}
              </div>
            </div>

            {/* Navigation links */}
            <div className="mt-12 xl:mt-0 xl:col-span-2">
              <div className="grid grid-cols-2 md:grid-cols-4 gap-8">
                {/* Services */}
                <div>
                  <h3 className="text-sm font-semibold text-white tracking-wider uppercase">
                    Services
                  </h3>
                  <ul className="mt-4 space-y-3">
                    {footerNavigation.services.map((item) => (
                      <li key={item.name}>
                        <Link
                          href={item.href}
                          className="text-sm text-gray-400 hover:text-neo-500 transition-colors"
                        >
                          {item.name}
                        </Link>
                      </li>
                    ))}
                  </ul>
                </div>

                {/* Developers */}
                <div>
                  <h3 className="text-sm font-semibold text-white tracking-wider uppercase">
                    Developers
                  </h3>
                  <ul className="mt-4 space-y-3">
                    {footerNavigation.developers.map((item) => (
                      <li key={item.name}>
                        <Link
                          href={item.href}
                          className="text-sm text-gray-400 hover:text-neo-500 transition-colors"
                          {...(item.href.startsWith('http') && {
                            target: '_blank',
                            rel: 'noopener noreferrer'
                          })}
                        >
                          {item.name}
                        </Link>
                      </li>
                    ))}
                  </ul>
                </div>

                {/* Company */}
                <div>
                  <h3 className="text-sm font-semibold text-white tracking-wider uppercase">
                    Company
                  </h3>
                  <ul className="mt-4 space-y-3">
                    {footerNavigation.company.map((item) => (
                      <li key={item.name}>
                        <Link
                          href={item.href}
                          className="text-sm text-gray-400 hover:text-neo-500 transition-colors"
                        >
                          {item.name}
                        </Link>
                      </li>
                    ))}
                  </ul>
                </div>

                {/* Legal */}
                <div>
                  <h3 className="text-sm font-semibold text-white tracking-wider uppercase">
                    Legal
                  </h3>
                  <ul className="mt-4 space-y-3">
                    {footerNavigation.legal.map((item) => (
                      <li key={item.name}>
                        <Link
                          href={item.href}
                          className="text-sm text-gray-400 hover:text-neo-500 transition-colors"
                        >
                          {item.name}
                        </Link>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
            </div>
          </div>

          {/* Bottom section */}
          <div className="mt-12 pt-8 border-t border-gray-800">
            <div className="md:flex md:items-center md:justify-between">
              <div className="flex items-center space-x-4">
                <p className="text-sm text-gray-400">
                  © {new Date().getFullYear()} Neo Service Layer. All rights reserved.
                </p>
                <div className="flex items-center space-x-1 text-sm text-gray-400">
                  <span>Made with</span>
                  <HeartIcon className="h-4 w-4 text-red-500" />
                  <span>for the Neo community</span>
                </div>
              </div>
              <div className="mt-4 md:mt-0 flex items-center space-x-4">
                <div className="flex items-center space-x-2 text-sm text-gray-400">
                  <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse" />
                  <span>All systems operational</span>
                </div>
                <Link
                  href="/status"
                  className="text-sm text-gray-400 hover:text-neo-500 transition-colors"
                >
                  Status Page
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>
    </footer>
  )
}