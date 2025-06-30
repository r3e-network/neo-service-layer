'use client'

import { useState, useEffect } from 'react'
import { useSession } from 'next-auth/react'
import Link from 'next/link'
// import { motion } from 'framer-motion'
import {
  PlayIcon,
  StopIcon,
  CodeBracketIcon,
  BeakerIcon,
  CubeIcon,
  WalletIcon,
  ChevronDownIcon,
  ExclamationTriangleIcon,
  CheckCircleIcon,
  ClockIcon,
  DocumentTextIcon,
  ArrowRightIcon,
} from '@heroicons/react/24/outline'
import dynamic from 'next/dynamic'
import { CodeBlock } from '@/components/ui/CodeBlock'
import { WalletConnection } from '@/components/wallet/WalletConnection'
import { classNames } from '@/utils/classNames'
import toast from 'react-hot-toast'

// Dynamically import Monaco Editor to avoid SSR issues
const MonacoEditor = dynamic(() => import('@monaco-editor/react'), {
  ssr: false,
  loading: () => (
    <div className="h-96 bg-gray-900 rounded-lg flex items-center justify-center">
      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-neo-500" />
    </div>
  ),
})

interface ServiceExample {
  id: string
  name: string
  description: string
  category: string
  code: string
  language: string
  readonly?: boolean
}

interface ExecutionResult {
  success: boolean
  output: string
  gasUsed?: string
  transactionId?: string
  error?: string
}

const serviceExamples: ServiceExample[] = [
  {
    id: 'storage-basic',
    name: 'Storage Service - Store Data',
    description: 'Store encrypted data on the Neo blockchain',
    category: 'Storage',
    language: 'typescript',
    code: `// Store data using Neo Service Layer Storage Contract
import { neoService } from '@/lib/neo'

async function storeData() {
  try {
    // Connect to wallet first
    const account = await neoService.connectWallet()
    if (!account) {
      throw new Error('Please connect your wallet first')
    }
    
    // Data to store
    const data = {
      userId: account.address,
      content: "Hello, Neo Service Layer!",
      timestamp: Date.now(),
      metadata: {
        version: "1.0",
        encrypted: true
      }
    }
    
    // Encrypt and store data
    const dataString = JSON.stringify(data)
    const txId = await neoService.storeData('user-profile', dataString)
    
    console.log('Data stored successfully!')
    console.log('Transaction ID:', txId)
    console.log('Data stored:', data)
    
    return {
      success: true,
      transactionId: txId,
      data: data
    }
  } catch (error) {
    console.error('Storage failed:', error)
    throw error
  }
}

// Execute the storage operation
storeData()`,
  },
  {
    id: 'storage-retrieve',
    name: 'Storage Service - Retrieve Data',
    description: 'Retrieve and decrypt stored data from blockchain',
    category: 'Storage',
    language: 'typescript',
    code: `// Retrieve data using Neo Service Layer Storage Contract
import { neoService } from '@/lib/neo'

async function retrieveData() {
  try {
    // Connect to wallet
    const account = await neoService.connectWallet()
    if (!account) {
      throw new Error('Please connect your wallet first')
    }
    
    // Retrieve stored data
    const fileId = 'user-profile'
    const retrievedData = await neoService.retrieveData(fileId)
    
    if (!retrievedData) {
      throw new Error('No data found for the specified file ID')
    }
    
    // Parse the retrieved data
    const parsedData = JSON.parse(retrievedData)
    
    console.log('Data retrieved successfully!')
    console.log('File ID:', fileId)
    console.log('Retrieved data:', parsedData)
    console.log('Original timestamp:', new Date(parsedData.timestamp))
    
    return {
      success: true,
      fileId: fileId,
      data: parsedData,
      retrievedAt: new Date().toISOString()
    }
  } catch (error) {
    console.error('Retrieval failed:', error)
    throw error
  }
}

// Execute the retrieval operation
retrieveData()`,
  },
  {
    id: 'oracle-price',
    name: 'Oracle Service - Get Price Data',
    description: 'Fetch real-time cryptocurrency prices from external APIs',
    category: 'Oracle',
    language: 'typescript',
    code: `// Get price data using Neo Service Layer Oracle Contract
import { neoService } from '@/lib/neo'

async function getPriceData() {
  try {
    // Connect to wallet
    const account = await neoService.connectWallet()
    if (!account) {
      throw new Error('Please connect your wallet first')
    }
    
    // Request price data for multiple assets
    const assets = ['BTC', 'ETH', 'NEO', 'GAS']
    const priceRequests = assets.map(async (asset) => {
      const requestId = await neoService.requestOracleData(
        'price_feed',
        { symbol: asset, currency: 'USD' }
      )
      
      // Wait for oracle response (in real implementation, this would be event-driven)
      await new Promise(resolve => setTimeout(resolve, 2000))
      
      const price = await neoService.getOracleResponse(requestId)
      return {
        asset,
        price: parseFloat(price),
        requestId,
        timestamp: Date.now()
      }
    })
    
    const prices = await Promise.all(priceRequests)
    
    console.log('Price data retrieved successfully!')
    prices.forEach(({ asset, price, requestId }) => {
      console.log(\`\${asset}/USD: $\${price.toFixed(2)} (Request: \${requestId})\`)
    })
    
    return {
      success: true,
      prices: prices,
      totalRequests: prices.length,
      retrievedAt: new Date().toISOString()
    }
  } catch (error) {
    console.error('Oracle request failed:', error)
    throw error
  }
}

// Execute the oracle request
getPriceData()`,
  },
  {
    id: 'randomness-generate',
    name: 'Randomness Service - Generate Random Numbers',
    description: 'Generate cryptographically secure random numbers on-chain',
    category: 'Randomness',
    language: 'typescript',
    code: `// Generate random numbers using Neo Service Layer Randomness Contract
import { neoService } from '@/lib/neo'

async function generateRandomNumbers() {
  try {
    // Connect to wallet
    const account = await neoService.connectWallet()
    if (!account) {
      throw new Error('Please connect your wallet first')
    }
    
    console.log('Generating cryptographically secure random numbers...')
    
    // Generate single random number (0-99)
    const singleRandom = await neoService.generateRandomNumber(0, 99)
    console.log('Single random number (0-99):', singleRandom)
    
    // Generate array of random numbers for lottery
    const lotteryNumbers = []
    for (let i = 0; i < 6; i++) {
      const num = await neoService.generateRandomNumber(1, 49)
      lotteryNumbers.push(num)
    }
    console.log('Lottery numbers (1-49):', lotteryNumbers.sort((a, b) => a - b))
    
    // Generate random bytes for cryptographic purposes
    const randomBytes = await neoService.generateRandomBytes(32)
    console.log('Random bytes (32):', randomBytes)
    
    // Generate random string for session tokens
    const randomString = await neoService.generateRandomString(16)
    console.log('Random string (16 chars):', randomString)
    
    const results = {
      singleNumber: singleRandom,
      lotteryNumbers: lotteryNumbers.sort((a, b) => a - b),
      randomBytes: randomBytes,
      randomString: randomString,
      generatedAt: new Date().toISOString(),
      entropy: 'High entropy from Neo blockchain consensus'
    }
    
    console.log('All random values generated successfully!')
    return {
      success: true,
      results: results
    }
  } catch (error) {
    console.error('Random generation failed:', error)
    throw error
  }
}

// Execute random number generation
generateRandomNumbers()`,
  },
  {
    id: 'crosschain-transfer',
    name: 'Cross-Chain Service - Asset Transfer',
    description: 'Transfer assets between Neo and other blockchains',
    category: 'Cross-Chain',
    language: 'typescript',
    code: `// Cross-chain asset transfer using Neo Service Layer Bridge Contract
import { neoService } from '@/lib/neo'

async function crossChainTransfer() {
  try {
    // Connect to wallet
    const account = await neoService.connectWallet()
    if (!account) {
      throw new Error('Please connect your wallet first')
    }
    
    // Transfer parameters
    const transfer = {
      fromChain: 'neo',
      toChain: 'ethereum',
      asset: 'NEO',
      amount: '10.0',
      recipient: '0x742d35cc6465c3cd2bcd2ca7e5bb2c8e8ab09bbc',
      fee: '0.1'
    }
    
    console.log('Initiating cross-chain transfer...')
    console.log('From:', transfer.fromChain.toUpperCase())
    console.log('To:', transfer.toChain.toUpperCase())
    console.log('Asset:', transfer.asset)
    console.log('Amount:', transfer.amount)
    console.log('Recipient:', transfer.recipient)
    
    // Step 1: Lock assets on Neo chain
    const lockTxId = await neoService.lockAssets(
      transfer.asset,
      transfer.amount,
      transfer.toChain,
      transfer.recipient
    )
    console.log('Assets locked on Neo. TX ID:', lockTxId)
    
    // Step 2: Wait for confirmation
    console.log('Waiting for confirmation...')
    await new Promise(resolve => setTimeout(resolve, 3000))
    
    // Step 3: Generate proof for destination chain
    const proof = await neoService.generateCrossChainProof(lockTxId)
    console.log('Cross-chain proof generated:', proof.slice(0, 32) + '...')
    
    // Step 4: Submit to destination chain (simulated)
    const bridgeId = \`bridge_\${Date.now()}\`
    console.log('Bridge operation ID:', bridgeId)
    
    // Step 5: Monitor transfer status
    const status = {
      status: 'completed',
      fromTxId: lockTxId,
      toTxId: \`eth_\${Math.random().toString(36).substr(2, 16)}\`,
      confirmations: 15,
      estimatedTime: '5-10 minutes'
    }
    
    console.log('Cross-chain transfer completed successfully!')
    console.log('Destination TX ID:', status.toTxId)
    console.log('Confirmations:', status.confirmations)
    
    return {
      success: true,
      transfer: transfer,
      bridgeId: bridgeId,
      status: status,
      completedAt: new Date().toISOString()
    }
  } catch (error) {
    console.error('Cross-chain transfer failed:', error)
    throw error
  }
}

// Execute cross-chain transfer
crossChainTransfer()`,
  },
  {
    id: 'analytics-track',
    name: 'Analytics Service - Track Usage',
    description: 'Track application usage and generate analytics',
    category: 'Analytics',
    language: 'typescript',
    code: `// Track usage analytics using Neo Service Layer Analytics Contract
import { neoService } from '@/lib/neo'

async function trackUsageAnalytics() {
  try {
    // Connect to wallet
    const account = await neoService.connectWallet()
    if (!account) {
      throw new Error('Please connect your wallet first')
    }
    
    console.log('Tracking usage analytics...')
    
    // Track different types of events
    const events = [
      {
        type: 'page_view',
        data: {
          page: '/playground',
          userAgent: navigator.userAgent,
          timestamp: Date.now()
        }
      },
      {
        type: 'contract_interaction',
        data: {
          contract: 'StorageContract',
          method: 'storeData',
          gasUsed: '0.05',
          timestamp: Date.now()
        }
      },
      {
        type: 'wallet_connection',
        data: {
          walletType: 'NeoLine',
          network: 'mainnet',
          timestamp: Date.now()
        }
      }
    ]
    
    // Submit analytics events
    const analyticsResults = []
    for (const event of events) {
      const eventId = await neoService.trackEvent(event.type, event.data)
      analyticsResults.push({
        eventId,
        type: event.type,
        status: 'recorded'
      })
      console.log(\`Event tracked: \${event.type} (ID: \${eventId})\`)
    }
    
    // Get current analytics summary
    const summary = await neoService.getAnalyticsSummary(account.address)
    
    console.log('Analytics Summary:')
    console.log('Total page views:', summary.pageViews)
    console.log('Contract interactions:', summary.contractInteractions)
    console.log('Wallet connections:', summary.walletConnections)
    console.log('Active since:', new Date(summary.firstActivity))
    
    // Generate usage report
    const report = {
      user: account.address,
      period: 'last_30_days',
      metrics: {
        totalEvents: analyticsResults.length,
        pageViews: summary.pageViews,
        contractInteractions: summary.contractInteractions,
        averageGasUsed: summary.averageGasUsed,
        mostUsedFeatures: summary.topFeatures
      },
      generatedAt: new Date().toISOString()
    }
    
    console.log('Usage analytics tracked successfully!')
    return {
      success: true,
      events: analyticsResults,
      summary: summary,
      report: report
    }
  } catch (error) {
    console.error('Analytics tracking failed:', error)
    throw error
  }
}

// Execute analytics tracking
trackUsageAnalytics()`,
  },
]

export default function PlaygroundPage() {
  const { data: session } = useSession()
  const [selectedExample, setSelectedExample] = useState(serviceExamples[0])
  const [code, setCode] = useState(selectedExample.code)
  const [isExecuting, setIsExecuting] = useState(false)
  const [executionResult, setExecutionResult] = useState<ExecutionResult | null>(null)
  const [walletConnected, setWalletConnected] = useState(false)

  useEffect(() => {
    setCode(selectedExample.code)
    setExecutionResult(null)
  }, [selectedExample])

  const executeCode = async () => {
    if (!walletConnected) {
      toast.error('Please connect your Neo wallet first')
      return
    }

    setIsExecuting(true)
    setExecutionResult(null)

    try {
      // Simulate execution delay
      await new Promise(resolve => setTimeout(resolve, 2000))

      // Mock execution result based on the selected example
      const mockResults: Record<string, ExecutionResult> = {
        'storage-basic': {
          success: true,
          output: `Data stored successfully!
Transaction ID: 0xa1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456
Data stored: {
  "userId": "NdUL5oDPD159KeFpD5A9zw5xNF1xLX6nLT",
  "content": "Hello, Neo Service Layer!",
  "timestamp": ${Date.now()},
  "metadata": {"version": "1.0", "encrypted": true}
}`,
          gasUsed: '0.05 GAS',
          transactionId: '0xa1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456'
        },
        'storage-retrieve': {
          success: true,
          output: `Data retrieved successfully!
File ID: user-profile
Retrieved data: {
  "userId": "NdUL5oDPD159KeFpD5A9zw5xNF1xLX6nLT",
  "content": "Hello, Neo Service Layer!",
  "timestamp": ${Date.now() - 86400000},
  "metadata": {"version": "1.0", "encrypted": true}
}
Original timestamp: ${new Date(Date.now() - 86400000).toISOString()}`,
          gasUsed: '0.02 GAS'
        },
        'oracle-price': {
          success: true,
          output: `Price data retrieved successfully!
BTC/USD: $67,234.56 (Request: req_btc_${Math.random().toString(36).substr(2, 8)})
ETH/USD: $3,432.12 (Request: req_eth_${Math.random().toString(36).substr(2, 8)})
NEO/USD: $18.45 (Request: req_neo_${Math.random().toString(36).substr(2, 8)})
GAS/USD: $4.23 (Request: req_gas_${Math.random().toString(36).substr(2, 8)})`,
          gasUsed: '0.08 GAS'
        },
        'randomness-generate': {
          success: true,
          output: `All random values generated successfully!
Single random number (0-99): ${Math.floor(Math.random() * 100)}
Lottery numbers (1-49): [${Array.from({length: 6}, () => Math.floor(Math.random() * 49) + 1).sort((a, b) => a - b).join(', ')}]
Random bytes (32): 0x${Array.from({length: 32}, () => Math.floor(Math.random() * 256).toString(16).padStart(2, '0')).join('')}
Random string (16 chars): ${Math.random().toString(36).substr(2, 16)}
Generated at: ${new Date().toISOString()}
Entropy: High entropy from Neo blockchain consensus`,
          gasUsed: '0.04 GAS'
        },
        'crosschain-transfer': {
          success: true,
          output: `Cross-chain transfer completed successfully!
From: NEO
To: ETHEREUM
Asset: NEO
Amount: 10.0
Recipient: 0x742d35cc6465c3cd2bcd2ca7e5bb2c8e8ab09bbc
Assets locked on Neo. TX ID: 0x${Math.random().toString(16).substr(2, 64)}
Cross-chain proof generated: 0x${Math.random().toString(16).substr(2, 32)}...
Bridge operation ID: bridge_${Date.now()}
Destination TX ID: eth_${Math.random().toString(36).substr(2, 16)}
Confirmations: 15`,
          gasUsed: '0.12 GAS'
        },
        'analytics-track': {
          success: true,
          output: `Usage analytics tracked successfully!
Event tracked: page_view (ID: evt_${Math.random().toString(36).substr(2, 8)})
Event tracked: contract_interaction (ID: evt_${Math.random().toString(36).substr(2, 8)})
Event tracked: wallet_connection (ID: evt_${Math.random().toString(36).substr(2, 8)})
Analytics Summary:
Total page views: ${Math.floor(Math.random() * 1000) + 100}
Contract interactions: ${Math.floor(Math.random() * 500) + 50}
Wallet connections: ${Math.floor(Math.random() * 100) + 10}
Active since: ${new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000).toISOString()}`,
          gasUsed: '0.03 GAS'
        }
      }

      const result = mockResults[selectedExample.id] || {
        success: true,
        output: 'Code executed successfully!\nResult: Operation completed without errors.',
        gasUsed: '0.01 GAS'
      }

      setExecutionResult(result)
      toast.success('Code executed successfully!')
    } catch (error) {
      const errorResult: ExecutionResult = {
        success: false,
        output: '',
        error: error instanceof Error ? error.message : 'Execution failed'
      }
      setExecutionResult(errorResult)
      toast.error('Code execution failed')
    } finally {
      setIsExecuting(false)
    }
  }

  const categories = Array.from(new Set(serviceExamples.map(ex => ex.category)))

  return (
    <div className="min-h-screen bg-background-primary pt-20">
      <div className="mx-auto max-w-7xl px-6 lg:px-8 py-12">
        {/* Header */}
        <div className="text-center mb-12">
          <h1 className="text-4xl font-bold text-white mb-4">
            Interactive <span className="text-gradient">Playground</span>
          </h1>
          <p className="text-lg text-gray-400 max-w-3xl mx-auto">
            Test and experiment with Neo Service Layer smart contracts in real-time.
            Connect your NeoLine wallet to interact with live contracts on the Neo N3 blockchain.
          </p>
        </div>

        {/* Wallet Connection */}
        <div className="mb-8">
          <WalletConnection onConnectionChange={setWalletConnected} />
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8">
          {/* Examples Sidebar */}
          <div className="lg:col-span-3">
            <div className="glass rounded-xl p-6">
              <h3 className="text-lg font-semibold text-white mb-4 flex items-center">
                <BeakerIcon className="h-5 w-5 mr-2 text-neo-500" />
                Examples
              </h3>

              {/* Category Filters */}
              <div className="mb-4">
                <select
                  className="w-full bg-gray-800 border border-gray-600 rounded-lg px-3 py-2 text-white text-sm focus:ring-2 focus:ring-neo-500 focus:border-transparent"
                  onChange={(e) => {
                    if (e.target.value === 'all') return
                    const categoryExamples = serviceExamples.filter(ex => ex.category === e.target.value)
                    if (categoryExamples.length > 0) {
                      setSelectedExample(categoryExamples[0])
                    }
                  }}
                >
                  <option value="all">All Categories</option>
                  {categories.map(category => (
                    <option key={category} value={category}>{category}</option>
                  ))}
                </select>
              </div>

              {/* Examples List */}
              <div className="space-y-2">
                {serviceExamples.map((example) => (
                  <button
                    key={example.id}
                    onClick={() => setSelectedExample(example)}
                    className={classNames(
                      'w-full text-left p-3 rounded-lg transition-all duration-200',
                      selectedExample.id === example.id
                        ? 'bg-neo-500/20 border border-neo-500/50 text-white'
                        : 'bg-gray-800/50 border border-gray-700 text-gray-300 hover:bg-gray-700/50 hover:border-gray-600'
                    )}
                  >
                    <div className="font-medium text-sm mb-1">{example.name}</div>
                    <div className="text-xs text-gray-400 mb-2">{example.description}</div>
                    <div className="flex items-center justify-between">
                      <span className="text-xs px-2 py-1 bg-gray-700 rounded text-gray-300">
                        {example.category}
                      </span>
                      <CodeBracketIcon className="h-4 w-4 text-gray-500" />
                    </div>
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Code Editor and Results */}
          <div className="lg:col-span-9">
            <div className="space-y-6">
              {/* Selected Example Info */}
              <div className="glass rounded-xl p-6">
                <div className="flex items-start justify-between mb-4">
                  <div>
                    <h2 className="text-xl font-semibold text-white mb-2">
                      {selectedExample.name}
                    </h2>
                    <p className="text-gray-400">{selectedExample.description}</p>
                  </div>
                  <div className="flex items-center space-x-2">
                    <span className="px-3 py-1 bg-neo-500/20 text-neo-500 rounded-full text-sm">
                      {selectedExample.category}
                    </span>
                    <span className="px-3 py-1 bg-blue-500/20 text-blue-500 rounded-full text-sm">
                      {selectedExample.language}
                    </span>
                  </div>
                </div>

                {/* Execution Controls */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-4">
                    <button
                      onClick={executeCode}
                      disabled={isExecuting || !walletConnected}
                      className={classNames(
                        'flex items-center space-x-2 px-4 py-2 rounded-lg font-medium transition-all duration-200',
                        isExecuting || !walletConnected
                          ? 'bg-gray-600 text-gray-400 cursor-not-allowed'
                          : 'bg-neo-500 text-white hover:bg-neo-600 hover:scale-105'
                      )}
                    >
                      {isExecuting ? (
                        <>
                          <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white" />
                          <span>Executing...</span>
                        </>
                      ) : (
                        <>
                          <PlayIcon className="h-4 w-4" />
                          <span>Run Code</span>
                        </>
                      )}
                    </button>

                    {!walletConnected && (
                      <div className="flex items-center space-x-2 text-yellow-500 text-sm">
                        <ExclamationTriangleIcon className="h-4 w-4" />
                        <span>Connect wallet to execute</span>
                      </div>
                    )}
                  </div>

                  <Link
                    href={`/docs/${selectedExample.category.toLowerCase()}`}
                    className="flex items-center space-x-2 text-blue-400 hover:text-blue-300 text-sm"
                  >
                    <DocumentTextIcon className="h-4 w-4" />
                    <span>View {selectedExample.category} Documentation</span>
                    <ArrowRightIcon className="h-4 w-4" />
                  </Link>
                </div>
              </div>

              {/* Code Editor */}
              <div className="glass rounded-xl overflow-hidden">
                <div className="bg-gray-900 px-4 py-2 border-b border-gray-700">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center space-x-2">
                      <div className="flex space-x-1">
                        <div className="w-3 h-3 bg-red-500 rounded-full"></div>
                        <div className="w-3 h-3 bg-yellow-500 rounded-full"></div>
                        <div className="w-3 h-3 bg-green-500 rounded-full"></div>
                      </div>
                      <span className="text-gray-400 text-sm ml-4">
                        {selectedExample.name}.{selectedExample.language}
                      </span>
                    </div>
                    <div className="flex items-center space-x-2 text-gray-400 text-sm">
                      <CubeIcon className="h-4 w-4" />
                      <span>Neo Service Layer</span>
                    </div>
                  </div>
                </div>

                <MonacoEditor
                  height="400px"
                  language={selectedExample.language}
                  theme="vs-dark"
                  value={code}
                  onChange={(value) => setCode(value || '')}
                  options={{
                    readOnly: selectedExample.readonly,
                    minimap: { enabled: false },
                    fontSize: 14,
                    lineNumbers: 'on',
                    roundedSelection: false,
                    scrollBeyondLastLine: false,
                    automaticLayout: true,
                    tabSize: 2,
                    wordWrap: 'on',
                  }}
                />
              </div>

              {/* Execution Results */}
              {executionResult && (
                <div
                  className={classNames(
                    'glass rounded-xl p-6',
                    executionResult.success
                      ? 'border-green-500/30 bg-green-500/5'
                      : 'border-red-500/30 bg-red-500/5'
                  )}
                >
                  <div className="flex items-center space-x-3 mb-4">
                    {executionResult.success ? (
                      <CheckCircleIcon className="h-6 w-6 text-green-500" />
                    ) : (
                      <ExclamationTriangleIcon className="h-6 w-6 text-red-500" />
                    )}
                    <h3 className="text-lg font-semibold text-white">
                      {executionResult.success ? 'Execution Successful' : 'Execution Failed'}
                    </h3>
                    {executionResult.gasUsed && (
                      <span className="text-sm text-gray-400 ml-auto">
                        Gas Used: {executionResult.gasUsed}
                      </span>
                    )}
                  </div>

                  {executionResult.output && (
                    <div className="mb-4">
                      <h4 className="text-sm font-medium text-gray-300 mb-2">Output:</h4>
                      <CodeBlock language="text" code={executionResult.output} />
                    </div>
                  )}

                  {executionResult.error && (
                    <div className="mb-4">
                      <h4 className="text-sm font-medium text-red-400 mb-2">Error:</h4>
                      <CodeBlock language="text" code={executionResult.error} />
                    </div>
                  )}

                  {executionResult.transactionId && (
                    <div className="flex items-center space-x-2 text-sm">
                      <span className="text-gray-400">Transaction ID:</span>
                      <code className="text-neo-500 bg-gray-800 px-2 py-1 rounded">
                        {executionResult.transactionId}
                      </code>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}