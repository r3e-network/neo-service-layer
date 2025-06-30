'use client'

import { useState, useEffect } from 'react'
// import { useSession } from 'next-auth/react'
import Link from 'next/link'
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

const serviceExamples: ServiceExample[] = [
  {
    id: 'storage-basic',
    name: 'Storage Service - Store Data',
    description: 'Store encrypted data on the Neo blockchain',
    category: 'Storage',
    language: 'typescript',
    code: `// Store data using the Storage Service
import { neoService } from '@neoservicelayer/sdk'

async function storeData() {
  try {
    // Connect wallet
    await neoService.connectWallet()
    
    // Store a file
    const result = await neoService.storage.storeFile({
      fileId: 'my-important-data',
      content: 'Hello from Neo Service Layer!',
      encrypted: true,
      metadata: {
        type: 'text',
        created: Date.now()
      }
    })
    
    console.log('File stored successfully:', result.transactionId)
    return result
  } catch (error) {
    console.error('Storage failed:', error)
    throw error
  }
}

// Execute the function
storeData()`,
  },
  {
    id: 'analytics-track',
    name: 'Analytics Service - Track Events',
    description: 'Track user interactions and analyze data',
    category: 'Analytics',
    language: 'typescript',
    code: `// Track events with Analytics Service
import { neoService } from '@neoservicelayer/sdk'

async function trackUserAction() {
  try {
    // Track a custom event
    const result = await neoService.analytics.track({
      event: 'user_interaction',
      properties: {
        action: 'button_click',
        component: 'hero_cta',
        timestamp: Date.now(),
        user_id: 'user123'
      }
    })
    
    // Increment a counter
    await neoService.analytics.increment('page_views')
    
    console.log('Event tracked:', result)
    return result
  } catch (error) {
    console.error('Tracking failed:', error)
    throw error
  }
}

trackUserAction()`,
  },
  {
    id: 'oracle-request',
    name: 'Oracle Service - External Data',
    description: 'Request external data through Oracle service',
    category: 'Oracle',
    language: 'typescript',
    code: `// Request external data via Oracle
import { neoService } from '@neoservicelayer/sdk'

async function getExternalData() {
  try {
    // Request price data from external API
    const priceRequest = await neoService.oracle.requestData({
      url: 'https://api.coinbase.com/v2/exchange-rates?currency=NEO',
      path: 'data.rates.USD',
      callback: 'onPriceReceived'
    })
    
    console.log('Oracle request submitted:', priceRequest.requestId)
    
    // Listen for oracle response
    neoService.oracle.onResponse((response) => {
      console.log('Price data received:', response.data)
    })
    
    return priceRequest
  } catch (error) {
    console.error('Oracle request failed:', error)
    throw error
  }
}

getExternalData()`,
  },
  {
    id: 'randomness-generate',
    name: 'Randomness Service - Random Numbers',
    description: 'Generate cryptographically secure random numbers',
    category: 'Randomness',
    language: 'typescript',
    code: `// Generate random numbers
import { neoService } from '@neoservicelayer/sdk'

async function generateRandomNumber() {
  try {
    // Generate a random number between 1 and 100
    const randomResult = await neoService.randomness.generate({
      min: 1,
      max: 100,
      seed: Date.now().toString()
    })
    
    console.log('Random number:', randomResult.value)
    console.log('Proof:', randomResult.proof)
    
    // Generate random bytes
    const randomBytes = await neoService.randomness.generateBytes(32)
    console.log('Random bytes:', randomBytes)
    
    return randomResult
  } catch (error) {
    console.error('Random generation failed:', error)
    throw error
  }
}

generateRandomNumber()`,
  },
  {
    id: 'cross-chain-transfer',
    name: 'Cross-Chain Service - Asset Transfer',
    description: 'Transfer assets across different blockchains',
    category: 'Cross-Chain',
    language: 'typescript',
    code: `// Cross-chain asset transfer
import { neoService } from '@neoservicelayer/sdk'

async function crossChainTransfer() {
  try {
    // Initiate cross-chain transfer
    const transfer = await neoService.crossChain.transfer({
      fromChain: 'neo-n3',
      toChain: 'ethereum',
      asset: 'NEO',
      amount: '10',
      toAddress: '0x742d35Cc6482C123434243Cfdae02847c1567',
      fee: '0.01'
    })
    
    console.log('Transfer initiated:', transfer.transactionId)
    
    // Monitor transfer status
    const status = await neoService.crossChain.getTransferStatus(transfer.id)
    console.log('Transfer status:', status)
    
    return transfer
  } catch (error) {
    console.error('Cross-chain transfer failed:', error)
    throw error
  }
}

crossChainTransfer()`,
  },
]

const categories = [
  'All',
  'Storage',
  'Analytics',
  'Oracle',
  'Randomness',
  'Cross-Chain',
  'Compute',
  'Voting',
  'Payment',
]

interface ExecutionResult {
  success: boolean
  output: string
  error?: string
  transactionId?: string
  gasUsed?: string
  duration?: number
}

export default function PlaygroundPage() {
  const session = null // Temporarily disable session for build test
  const [selectedExample, setSelectedExample] = useState(serviceExamples[0])
  const [selectedCategory, setSelectedCategory] = useState('All')
  const [code, setCode] = useState(selectedExample.code)
  const [isExecuting, setIsExecuting] = useState(false)
  const [executionResult, setExecutionResult] = useState<ExecutionResult | null>(null)
  const [editorReady, setEditorReady] = useState(false)

  useEffect(() => {
    setCode(selectedExample.code)
    setExecutionResult(null)
  }, [selectedExample])

  const filteredExamples = selectedCategory === 'All' 
    ? serviceExamples 
    : serviceExamples.filter(example => example.category === selectedCategory)

  const executeCode = async () => {
    if (!session) {
      toast.error('Please sign in to execute code')
      return
    }

    setIsExecuting(true)
    setExecutionResult(null)

    try {
      const startTime = Date.now()
      
      // Simulate code execution (in a real implementation, this would call the actual Neo services)
      await new Promise(resolve => setTimeout(resolve, 2000))
      
      const duration = Date.now() - startTime
      
      // Mock successful execution
      const mockResult: ExecutionResult = {
        success: true,
        output: `Code executed successfully!\n\nExample output:\n- Transaction ID: 0xabc123...def789\n- Gas Used: 0.00123 GAS\n- Block: #2847569\n- Service: ${selectedExample.category}\n\nResult: Operation completed successfully.`,
        transactionId: '0xabc123def789ghi012jkl345mno678pqr901stu234vwx567yz890',
        gasUsed: '0.00123',
        duration,
      }

      setExecutionResult(mockResult)
      toast.success('Code executed successfully!')
    } catch (error) {
      const errorResult: ExecutionResult = {
        success: false,
        output: '',
        error: error instanceof Error ? error.message : 'Unknown error occurred',
        duration: Date.now() - Date.now(),
      }
      setExecutionResult(errorResult)
      toast.error('Code execution failed')
    } finally {
      setIsExecuting(false)
    }
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-background-primary to-background-secondary">
      <div className="mx-auto max-w-7xl px-6 py-12 lg:px-8">
        {/* Header */}
        <div
          className="text-center mb-12"
        >
          <div className="flex items-center justify-center space-x-2 mb-4">
            <BeakerIcon className="h-8 w-8 text-neo-500" />
            <h1 className="text-4xl font-bold text-white">
              Neo Service Layer <span className="text-gradient">Playground</span>
            </h1>
          </div>
          <p className="text-lg text-gray-400 max-w-3xl mx-auto">
            Experiment with Neo Service Layer APIs in an interactive environment. 
            Try out our smart contract services and see them in action.
          </p>
        </div>

        {/* Wallet Connection Notice */}
        {!session && (
          <div className="mb-8 p-4 rounded-lg bg-yellow-500/10 border border-yellow-500/20">
            <div className="flex items-center space-x-2">
              <ExclamationTriangleIcon className="h-5 w-5 text-yellow-500" />
              <p className="text-yellow-400">
                Sign in to execute code and interact with the Neo blockchain.
              </p>
            </div>
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
          {/* Sidebar - Examples */}
          <div
            className="lg:col-span-1"
          >
            <div className="glass rounded-lg p-6">
              <h3 className="text-lg font-semibold text-white mb-4">Examples</h3>
              
              {/* Category filter */}
              <div className="mb-4">
                <select
                  value={selectedCategory}
                  onChange={(e) => setSelectedCategory(e.target.value)}
                  className="w-full bg-gray-800 border border-gray-600 text-white rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-neo-500"
                >
                  {categories.map((category) => (
                    <option key={category} value={category}>
                      {category}
                    </option>
                  ))}
                </select>
              </div>

              {/* Examples list */}
              <div className="space-y-2">
                {filteredExamples.map((example) => (
                  <button
                    key={example.id}
                    onClick={() => setSelectedExample(example)}
                    className={classNames(
                      'w-full text-left p-3 rounded-lg transition-colors text-sm',
                      selectedExample.id === example.id
                        ? 'bg-neo-500/20 border border-neo-500/30 text-white'
                        : 'bg-gray-800 hover:bg-gray-700 text-gray-300 hover:text-white border border-gray-700'
                    )}
                  >
                    <div className="font-medium mb-1">{example.name}</div>
                    <div className="text-xs opacity-75">{example.description}</div>
                    <div className="flex items-center mt-2">
                      <span className="text-xs px-2 py-1 rounded bg-gray-700 text-gray-300">
                        {example.category}
                      </span>
                    </div>
                  </button>
                ))}
              </div>
            </div>

            {/* Wallet Connection */}
            <div className="mt-6 glass rounded-lg p-6">
              <h3 className="text-lg font-semibold text-white mb-4">Wallet</h3>
              <WalletConnection />
            </div>
          </div>

          {/* Main Content - Code Editor and Results */}
          <div
            className="lg:col-span-3 space-y-6"
          >
            {/* Code Editor */}
            <div className="glass rounded-lg overflow-hidden">
              <div className="bg-gray-800 px-6 py-3 border-b border-gray-700">
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-3">
                    <CodeBracketIcon className="h-5 w-5 text-neo-500" />
                    <h3 className="text-lg font-semibold text-white">
                      {selectedExample.name}
                    </h3>
                  </div>
                  <div className="flex items-center space-x-2">
                    <span className="text-sm text-gray-400">
                      {selectedExample.language}
                    </span>
                    <button
                      onClick={executeCode}
                      disabled={isExecuting || !session}
                      className="flex items-center space-x-2 px-4 py-2 bg-neo-500 text-white rounded-lg hover:bg-neo-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                    >
                      {isExecuting ? (
                        <>
                          <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                          <span>Executing...</span>
                        </>
                      ) : (
                        <>
                          <PlayIcon className="h-4 w-4" />
                          <span>Run</span>
                        </>
                      )}
                    </button>
                  </div>
                </div>
              </div>

              <div className="p-0">
                <MonacoEditor
                  height="400px"
                  language={selectedExample.language}
                  value={code}
                  onChange={(value) => setCode(value || '')}
                  onMount={() => setEditorReady(true)}
                  theme="vs-dark"
                  options={{
                    fontSize: 14,
                    lineNumbers: 'on',
                    roundedSelection: false,
                    scrollBeyondLastLine: false,
                    automaticLayout: true,
                    minimap: { enabled: false },
                    padding: { top: 20, bottom: 20 },
                    suggestOnTriggerCharacters: true,
                    acceptSuggestionOnEnter: 'on',
                    wordWrap: 'on',
                  }}
                />
              </div>
            </div>

            {/* Execution Results */}
            {executionResult && (
              <div className="glass rounded-lg overflow-hidden">
                <div className="bg-gray-800 px-6 py-3 border-b border-gray-700">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center space-x-2">
                      {executionResult.success ? (
                        <CheckCircleIcon className="h-5 w-5 text-green-500" />
                      ) : (
                        <ExclamationTriangleIcon className="h-5 w-5 text-red-500" />
                      )}
                      <h3 className="text-lg font-semibold text-white">
                        Execution {executionResult.success ? 'Successful' : 'Failed'}
                      </h3>
                    </div>
                    <div className="flex items-center space-x-4 text-sm text-gray-400">
                      {executionResult.duration && (
                        <div className="flex items-center space-x-1">
                          <ClockIcon className="h-4 w-4" />
                          <span>{executionResult.duration}ms</span>
                        </div>
                      )}
                      {executionResult.gasUsed && (
                        <div className="flex items-center space-x-1">
                          <CubeIcon className="h-4 w-4" />
                          <span>{executionResult.gasUsed} GAS</span>
                        </div>
                      )}
                    </div>
                  </div>
                </div>

                <div className="p-6">
                  {executionResult.success ? (
                    <div className="space-y-4">
                      <pre className="bg-gray-900 rounded-lg p-4 text-sm text-green-400 whitespace-pre-wrap overflow-x-auto">
                        {executionResult.output}
                      </pre>
                      {executionResult.transactionId && (
                        <div className="flex items-center space-x-2 text-sm">
                          <span className="text-gray-400">Transaction ID:</span>
                          <code className="bg-gray-800 px-2 py-1 rounded text-neo-500 font-mono">
                            {executionResult.transactionId}
                          </code>
                        </div>
                      )}
                    </div>
                  ) : (
                    <pre className="bg-gray-900 rounded-lg p-4 text-sm text-red-400 whitespace-pre-wrap overflow-x-auto">
                      Error: {executionResult.error}
                    </pre>
                  )}
                </div>
              </div>
            )}

            {/* Documentation Link */}
            <div
              className="text-center"
            >
              <p className="text-gray-400 mb-4">
                Want to learn more about this service?
              </p>
              <Link
                href={`/docs/services/${selectedExample.category.toLowerCase()}`}
                className="inline-flex items-center space-x-2 text-neo-500 hover:text-neo-400 transition-colors"
              >
                <DocumentTextIcon className="h-5 w-5" />
                <span>View {selectedExample.category} Documentation</span>
              </Link>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}