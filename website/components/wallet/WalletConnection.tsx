'use client'

import { useState, useEffect, useRef } from 'react'
import {
  WalletIcon,
  ChevronDownIcon,
  ExclamationTriangleIcon,
  CheckCircleIcon,
  XCircleIcon,
} from '@heroicons/react/24/outline'
import { neoService, formatNeoAddress, formatGasAmount } from '@/lib/neo'
import { classNames } from '@/utils/classNames'
import toast from 'react-hot-toast'

interface WalletState {
  isConnected: boolean
  address: string
  label: string
  balance: {
    neo: string
    gas: string
  }
  network: string
}

interface WalletConnectionProps {
  onConnectionChange?: (connected: boolean) => void
}

export function WalletConnection({ onConnectionChange }: WalletConnectionProps) {
  const [walletState, setWalletState] = useState<WalletState>({
    isConnected: false,
    address: '',
    label: '',
    balance: { neo: '0', gas: '0' },
    network: 'testnet',
  })
  const [isConnecting, setIsConnecting] = useState(false)
  const [isNeoLineAvailable, setIsNeoLineAvailable] = useState(false)
  const [isDropdownOpen, setIsDropdownOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    checkNeoLineAvailability()
    loadWalletState()
  }, [])

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsDropdownOpen(false)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const checkNeoLineAvailability = async () => {
    const available = await neoService.isNeoLineAvailable()
    setIsNeoLineAvailable(available)
  }

  const loadWalletState = async () => {
    try {
      if (await neoService.isNeoLineAvailable()) {
        const account = await neoService.connectWallet()
        if (account) {
          const balance = await neoService.getBalance(account.address)
          setWalletState({
            isConnected: true,
            address: account.address,
            label: account.label,
            balance: {
              neo: balance.neo?.amount || '0',
              gas: balance.gas?.amount || '0',
            },
            network: neoService.getCurrentNetwork(),
          })
        }
      }
    } catch (error) {
      console.error('Failed to load wallet state:', error)
    }
  }

  const connectWallet = async () => {
    if (!isNeoLineAvailable) {
      toast.error('NeoLine wallet not found. Please install the NeoLine browser extension.')
      window.open('https://neoline.io/', '_blank')
      return
    }

    setIsConnecting(true)
    try {
      const account = await neoService.connectWallet()
      if (account) {
        const balance = await neoService.getBalance(account.address)
        setWalletState({
          isConnected: true,
          address: account.address,
          label: account.label,
          balance: {
            neo: balance.neo?.amount || '0',
            gas: balance.gas?.amount || '0',
          },
          network: neoService.getCurrentNetwork(),
        })
        onConnectionChange?.(true)
        toast.success('Wallet connected successfully!')
      }
    } catch (error) {
      console.error('Failed to connect wallet:', error)
      toast.error('Failed to connect wallet. Please try again.')
    } finally {
      setIsConnecting(false)
    }
  }

  const disconnectWallet = () => {
    setWalletState({
      isConnected: false,
      address: '',
      label: '',
      balance: { neo: '0', gas: '0' },
      network: 'testnet',
    })
    onConnectionChange?.(false)
    toast.success('Wallet disconnected')
  }

  const switchNetwork = (network: 'mainnet' | 'testnet') => {
    neoService.switchNetwork(network)
    setWalletState(prev => ({ ...prev, network }))
    toast.success(`Switched to ${network}`)
    // Reload balance for new network
    loadWalletState()
  }

  if (!walletState.isConnected) {
    return (
      <button
        onClick={connectWallet}
        disabled={isConnecting}
        className="flex items-center space-x-2 rounded-md bg-gray-800 px-3 py-2 text-sm font-medium text-white hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        <WalletIcon className="h-4 w-4" />
        <span>{isConnecting ? 'Connecting...' : 'Connect Wallet'}</span>
      </button>
    )
  }

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        onClick={() => setIsDropdownOpen(!isDropdownOpen)}
        className="flex items-center space-x-2 rounded-md bg-neo-500/10 border border-neo-500/20 px-3 py-2 text-sm font-medium text-neo-500 hover:bg-neo-500/20 transition-colors"
      >
        <div className="flex items-center space-x-2">
          <div className="h-2 w-2 rounded-full bg-green-500" />
          <WalletIcon className="h-4 w-4" />
          <span className="hidden sm:inline">{formatNeoAddress(walletState.address)}</span>
        </div>
        <ChevronDownIcon className="h-4 w-4" />
      </button>

      {isDropdownOpen && (
        <div className="absolute right-0 z-10 mt-2 w-80 origin-top-right rounded-lg bg-gray-800 border border-gray-700 shadow-lg ring-1 ring-black ring-opacity-5">
          <div className="p-4">
            {/* Wallet Info */}
            <div className="mb-4">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm font-medium text-gray-300">Connected Wallet</span>
                <div className="flex items-center space-x-1">
                  <CheckCircleIcon className="h-4 w-4 text-green-500" />
                  <span className="text-xs text-green-500">Connected</span>
                </div>
              </div>
              <div className="text-xs text-gray-400 mb-1">
                {walletState.label || 'NeoLine Wallet'}
              </div>
              <div className="text-sm font-mono text-white bg-gray-900 rounded px-2 py-1">
                {walletState.address}
              </div>
            </div>

            {/* Network */}
            <div className="mb-4">
              <div className="text-sm font-medium text-gray-300 mb-2">Network</div>
              <div className="flex space-x-2">
                <button
                  onClick={() => switchNetwork('testnet')}
                  className={classNames(
                    'px-3 py-1 rounded text-xs font-medium transition-colors',
                    walletState.network === 'testnet'
                      ? 'bg-neo-500 text-white'
                      : 'bg-gray-700 text-gray-300 hover:bg-gray-600'
                  )}
                >
                  TestNet
                </button>
                <button
                  onClick={() => switchNetwork('mainnet')}
                  className={classNames(
                    'px-3 py-1 rounded text-xs font-medium transition-colors',
                    walletState.network === 'mainnet'
                      ? 'bg-neo-500 text-white'
                      : 'bg-gray-700 text-gray-300 hover:bg-gray-600'
                  )}
                >
                  MainNet
                </button>
              </div>
            </div>

            {/* Balance */}
            <div className="mb-4">
              <div className="text-sm font-medium text-gray-300 mb-2">Balance</div>
              <div className="space-y-1">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-400">NEO:</span>
                  <span className="text-white">{formatGasAmount(walletState.balance.neo)}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-400">GAS:</span>
                  <span className="text-white">{formatGasAmount(walletState.balance.gas)}</span>
                </div>
              </div>
            </div>

            {/* Actions */}
            <div className="space-y-2">
              <button
                onClick={loadWalletState}
                className="w-full text-left px-3 py-2 text-sm text-gray-300 rounded hover:bg-gray-700"
              >
                Refresh Balance
              </button>
              <button
                onClick={disconnectWallet}
                className="w-full text-left px-3 py-2 text-sm text-red-400 rounded hover:bg-gray-700"
              >
                Disconnect Wallet
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

// Wallet status indicator component
export function WalletStatus() {
  const [isConnected, setIsConnected] = useState(false)

  useEffect(() => {
    const checkConnection = async () => {
      try {
        const account = await neoService.connectWallet()
        setIsConnected(!!account)
      } catch {
        setIsConnected(false)
      }
    }
    checkConnection()
  }, [])

  return (
    <div className="flex items-center space-x-2">
      <div
        className={classNames(
          'h-2 w-2 rounded-full',
          isConnected ? 'bg-green-500' : 'bg-red-500'
        )}
      />
      <span className="text-sm text-gray-400">
        {isConnected ? 'Wallet Connected' : 'Wallet Disconnected'}
      </span>
    </div>
  )
}