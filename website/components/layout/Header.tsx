'use client'

import { useState, useEffect } from 'react'
import Link from 'next/link'
import { useSession, signIn, signOut } from 'next-auth/react'
import { useTheme } from 'next-themes'
import {
  Bars3Icon,
  XMarkIcon,
  SunIcon,
  MoonIcon,
  UserCircleIcon,
  ChevronDownIcon,
  WalletIcon,
  DocumentTextIcon,
  BeakerIcon,
  CubeIcon,
} from '@heroicons/react/24/outline'
import { SimpleDropdown } from '@/components/ui/SimpleDropdown'
import { WalletConnection } from '../wallet/WalletConnection'
import { classNames } from '@/utils/classNames'

const navigation = [
  { name: 'Home', href: '/' },
  { name: 'Services', href: '/services' },
  { name: 'Documentation', href: '/docs' },
  { name: 'Playground', href: '/playground', icon: BeakerIcon },
  { name: 'API', href: '/api' },
]

export function Header() {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const [mounted, setMounted] = useState(false)
  const { data: session, status } = useSession()
  const { theme, setTheme } = useTheme()

  useEffect(() => {
    setMounted(true)
  }, [])

  const toggleMobileMenu = () => setMobileMenuOpen(!mobileMenuOpen)

  return (
    <header className="fixed top-0 left-0 right-0 z-50 bg-background-primary/80 backdrop-blur-md border-b border-neo-light">
      <nav className="mx-auto flex max-w-7xl items-center justify-between p-6 lg:px-8" aria-label="Global">
        {/* Logo */}
        <Link href="/" className="flex items-center space-x-2">
          <div className="flex items-center space-x-2">
            <CubeIcon className="h-8 w-8 text-neo-500" />
            <span className="text-xl font-bold text-gradient">
              Neo Service Layer
            </span>
          </div>
        </Link>

        {/* Desktop Navigation */}
        <div className="hidden lg:flex lg:gap-x-8">
          {navigation.map((item) => (
            <Link
              key={item.name}
              href={item.href}
              className="flex items-center space-x-1 text-sm font-semibold leading-6 text-gray-300 hover:text-neo-500 transition-colors"
            >
              {item.icon && <item.icon className="h-4 w-4" />}
              <span>{item.name}</span>
            </Link>
          ))}
        </div>

        {/* Right side items */}
        <div className="hidden lg:flex lg:items-center lg:gap-x-4">
          {/* Wallet Connection */}
          <WalletConnection />

          {/* Theme Toggle */}
          {mounted && (
            <button
              onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
              className="rounded-md p-2 text-gray-400 hover:text-neo-500 hover:bg-gray-800 transition-colors"
            >
              {theme === 'dark' ? (
                <SunIcon className="h-5 w-5" />
              ) : (
                <MoonIcon className="h-5 w-5" />
              )}
            </button>
          )}

          {/* User Menu */}
          {status === 'loading' ? (
            <div className="h-8 w-8 rounded-full bg-gray-700 animate-pulse" />
          ) : session ? (
            <SimpleDropdown
              trigger={
                <div className="flex rounded-full bg-gray-800 text-sm focus:outline-none focus:ring-2 focus:ring-neo-500 focus:ring-offset-2 focus:ring-offset-gray-800 cursor-pointer">
                  <span className="sr-only">Open user menu</span>
                  {session.user?.image ? (
                    <img
                      className="h-8 w-8 rounded-full"
                      src={session.user.image}
                      alt={session.user.name || 'User'}
                    />
                  ) : (
                    <UserCircleIcon className="h-8 w-8 text-gray-400" />
                  )}
                </div>
              }
              items={[
                { label: 'Dashboard', href: '/dashboard' },
                { label: 'Profile', href: '/profile' },
                { label: 'Settings', href: '/settings' },
                { label: 'Sign out', onClick: () => signOut() }
              ]}
            />
          ) : (
            <button
              onClick={() => signIn()}
              className="rounded-md bg-neo-500 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-neo-600 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-neo-500"
            >
              Sign In
            </button>
          )}
        </div>

        {/* Mobile menu button */}
        <div className="flex lg:hidden">
          <button
            type="button"
            className="-m-2.5 inline-flex items-center justify-center rounded-md p-2.5 text-gray-400"
            onClick={toggleMobileMenu}
          >
            <span className="sr-only">Open main menu</span>
            {mobileMenuOpen ? (
              <XMarkIcon className="h-6 w-6" aria-hidden="true" />
            ) : (
              <Bars3Icon className="h-6 w-6" aria-hidden="true" />
            )}
          </button>
        </div>
      </nav>

      {/* Mobile menu */}
      {mobileMenuOpen && (
        <div className="lg:hidden bg-background-primary border-t border-neo-light">
            <div className="space-y-1 px-6 pb-6 pt-6">
              {navigation.map((item) => (
                <Link
                  key={item.name}
                  href={item.href}
                  className="flex items-center space-x-2 rounded-md px-3 py-2 text-base font-medium text-gray-300 hover:bg-gray-800 hover:text-neo-500"
                  onClick={() => setMobileMenuOpen(false)}
                >
                  {item.icon && <item.icon className="h-5 w-5" />}
                  <span>{item.name}</span>
                </Link>
              ))}

              {/* Mobile auth section */}
              <div className="mt-6 pt-6 border-t border-gray-700">
                {session ? (
                  <div className="space-y-2">
                    <div className="flex items-center space-x-3 px-3 py-2">
                      {session.user?.image ? (
                        <img
                          className="h-8 w-8 rounded-full"
                          src={session.user.image}
                          alt={session.user.name || 'User'}
                        />
                      ) : (
                        <UserCircleIcon className="h-8 w-8 text-gray-400" />
                      )}
                      <div>
                        <div className="text-sm font-medium text-white">
                          {session.user?.name}
                        </div>
                        <div className="text-xs text-gray-400">
                          {session.user?.email}
                        </div>
                      </div>
                    </div>
                    <Link
                      href="/dashboard"
                      className="block rounded-md px-3 py-2 text-base font-medium text-gray-300 hover:bg-gray-800 hover:text-neo-500"
                      onClick={() => setMobileMenuOpen(false)}
                    >
                      Dashboard
                    </Link>
                    <Link
                      href="/profile"
                      className="block rounded-md px-3 py-2 text-base font-medium text-gray-300 hover:bg-gray-800 hover:text-neo-500"
                      onClick={() => setMobileMenuOpen(false)}
                    >
                      Profile
                    </Link>
                    <button
                      onClick={() => {
                        signOut()
                        setMobileMenuOpen(false)
                      }}
                      className="block w-full text-left rounded-md px-3 py-2 text-base font-medium text-gray-300 hover:bg-gray-800 hover:text-red-400"
                    >
                      Sign out
                    </button>
                  </div>
                ) : (
                  <button
                    onClick={() => {
                      signIn()
                      setMobileMenuOpen(false)
                    }}
                    className="w-full rounded-md bg-neo-500 px-4 py-2 text-base font-semibold text-white shadow-sm hover:bg-neo-600"
                  >
                    Sign In
                  </button>
                )}
              </div>

              {/* Mobile wallet section */}
              <div className="mt-4 pt-4 border-t border-gray-700">
                <WalletConnection />
              </div>
            </div>
        </div>
      )}
    </header>
  )
}