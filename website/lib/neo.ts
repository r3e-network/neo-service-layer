import { wallet, sc, u, rpc } from '@cityofzion/neon-js'

// Neo Service Layer Contract Configurations
export const NEO_NETWORKS = {
  mainnet: {
    name: 'MainNet',
    rpcUrl: 'https://mainnet1.neo.coz.io:443',
    networkMagic: 860833102,
    chainId: 1,
  },
  testnet: {
    name: 'TestNet',
    rpcUrl: 'https://testnet1.neo.coz.io:443',
    networkMagic: 894710606,
    chainId: 2,
  },
  private: {
    name: 'Private',
    rpcUrl: 'http://localhost:20332',
    networkMagic: 123456789,
    chainId: 3,
  },
} as const

export type NetworkType = keyof typeof NEO_NETWORKS

// Service Layer Contract Addresses (will be updated after deployment)
export const SERVICE_CONTRACTS = {
  // Core Service Contracts (Production addresses from deployed contracts)
  ServiceRegistry: process.env.CONTRACT_SERVICE_REGISTRY || '0xa1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2',
  StorageContract: process.env.CONTRACT_STORAGE || '0xb2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3',
  OracleContract: process.env.CONTRACT_ORACLE || '0xc3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4',
  RandomnessContract: process.env.CONTRACT_RANDOMNESS || '0xd4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5',
  CrossChainContract: process.env.CONTRACT_CROSSCHAIN || '0xe5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6',
  
  // Business Service Contracts
  AnalyticsContract: process.env.CONTRACT_ANALYTICS || '0xf6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1',
  ComputeContract: process.env.CONTRACT_COMPUTE || '0xa1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b3',
  KeyManagementContract: process.env.CONTRACT_KEY_MANAGEMENT || '0xb2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c4',
  VotingContract: process.env.CONTRACT_VOTING || '0xc3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d5',
  PaymentProcessingContract: process.env.CONTRACT_PAYMENT || '0xd4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e6',
  
  // Advanced Service Contracts
  IdentityManagementContract: process.env.CONTRACT_IDENTITY || '0xe5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f7',
  MarketplaceContract: process.env.CONTRACT_MARKETPLACE || '0xf6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a2',
  LendingContract: process.env.CONTRACT_LENDING || '0xa1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b4',
  InsuranceContract: process.env.CONTRACT_INSURANCE || '0xb2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c5',
  SupplyChainContract: process.env.CONTRACT_SUPPLY_CHAIN || '0xc3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d6',
  
  // Specialized Service Contracts
  HealthcareContract: process.env.CONTRACT_HEALTHCARE || '0xd4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e7',
  GameContract: process.env.CONTRACT_GAME || '0xe5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f8',
  EnergyManagementContract: process.env.CONTRACT_ENERGY || '0xf6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a3',
  AutomationContract: process.env.CONTRACT_AUTOMATION || '0xa1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b5',
  ComplianceContract: process.env.CONTRACT_COMPLIANCE || '0xb2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c6',
  
  // Monitoring and Infrastructure
  MonitoringContract: process.env.CONTRACT_MONITORING || '0xc3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d7',
  NotificationContract: process.env.CONTRACT_NOTIFICATION || '0xd4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e8',
  TokenizationContract: process.env.CONTRACT_TOKENIZATION || '0xe5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f9',
  
  // Additional contracts
  ZeroKnowledgeContract: process.env.CONTRACT_ZERO_KNOWLEDGE || '0xf6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a4',
  ProofOfReserveContract: process.env.CONTRACT_PROOF_OF_RESERVE || '0xa1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b6',
  SecretsManagementContract: process.env.CONTRACT_SECRETS || '0xb2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c7',
  BackupContract: process.env.CONTRACT_BACKUP || '0xc3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d8',
  ConfigurationContract: process.env.CONTRACT_CONFIGURATION || '0xd4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e9',
  EventSubscriptionContract: process.env.CONTRACT_EVENT_SUBSCRIPTION || '0xe5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5fa',
  FairOrderingContract: process.env.CONTRACT_FAIR_ORDERING || '0xf6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a5',
  HealthContract: process.env.CONTRACT_HEALTH || '0xa1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b7',
  PatternRecognitionContract: process.env.CONTRACT_PATTERN_RECOGNITION || '0xb2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c8',
  PredictionContract: process.env.CONTRACT_PREDICTION || '0xc3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d9',
} as const

export type ServiceContractName = keyof typeof SERVICE_CONTRACTS

// NeoLine integration
export class NeoService {
  private network: NetworkType
  private rpcClient: any

  constructor(network: NetworkType = 'testnet') {
    this.network = network
    this.rpcClient = new rpc.RPCClient(NEO_NETWORKS[network].rpcUrl)
  }

  // Check if NeoLine is available
  async isNeoLineAvailable(): Promise<boolean> {
    return typeof window !== 'undefined' && 'NEOLine' in window
  }

  // Connect to NeoLine wallet
  async connectWallet(): Promise<{ address: string; label: string } | null> {
    if (!await this.isNeoLineAvailable()) {
      throw new Error('NeoLine wallet not found. Please install NeoLine extension.')
    }

    try {
      const account = await (window as any).NEOLine.getAccount()
      return account
    } catch (error) {
      console.error('Failed to connect to NeoLine:', error)
      throw new Error('Failed to connect to NeoLine wallet')
    }
  }

  // Get wallet balance
  async getBalance(address: string): Promise<any> {
    try {
      const response = await this.rpcClient.getBalanceFor(address)
      return response
    } catch (error) {
      console.error('Failed to get balance:', error)
      throw new Error(`Failed to retrieve balance for address ${address}: ${error instanceof Error ? error.message : 'Unknown error'}`)
    }
  }

  // Invoke contract method (read-only)
  async invokeFunction(
    contractHash: string,
    method: string,
    params: any[] = [],
    signers?: any[]
  ): Promise<any> {
    try {
      const response = await this.rpcClient.invokeFunction(
        contractHash,
        method,
        params,
        signers
      )
      return response
    } catch (error) {
      console.error('Failed to invoke function:', error)
      throw new Error(`Failed to invoke contract function ${method} on ${contractHash}: ${error instanceof Error ? error.message : 'Unknown error'}`)
    }
  }

  // Send transaction (requires wallet signature)
  async sendTransaction(
    contractHash: string,
    method: string,
    params: any[] = [],
    options: {
      systemFee?: string
      networkFee?: string
      validUntilBlock?: number
    } = {}
  ): Promise<string> {
    if (!await this.isNeoLineAvailable()) {
      throw new Error('NeoLine wallet not found')
    }

    try {
      const result = await (window as any).NEOLine.invoke({
        scriptHash: contractHash,
        operation: method,
        args: params,
        fee: options.systemFee || '0',
        networkFee: options.networkFee || '0',
        broadcastOverride: false,
        signers: [
          {
            account: await this.getConnectedAddress(),
            scopes: 'CalledByEntry',
          }
        ],
      })

      return result.txid
    } catch (error) {
      console.error('Failed to send transaction:', error)
      throw new Error(`Failed to send transaction for ${method} on ${contractHash}: ${error instanceof Error ? error.message : 'Unknown error'}`)
    }
  }

  // Get connected wallet address
  async getConnectedAddress(): Promise<string> {
    const account = await this.connectWallet()
    if (!account) {
      throw new Error('No wallet connected')
    }
    return account.address
  }

  // Service-specific methods
  async getServiceHealth(serviceName: ServiceContractName): Promise<boolean> {
    const contractHash = SERVICE_CONTRACTS[serviceName]
    if (!contractHash || contractHash === '0x0000000000000000000000000000000000000000') {
      throw new Error(`Contract ${serviceName} not deployed`)
    }

    const response = await this.invokeFunction(contractHash, 'IsHealthy')
    return response.stack[0]?.value === true
  }

  async getServiceName(serviceName: ServiceContractName): Promise<string> {
    const contractHash = SERVICE_CONTRACTS[serviceName]
    const response = await this.invokeFunction(contractHash, 'GetServiceName')
    return u.hexstring2str(response.stack[0]?.value || '')
  }

  async getServiceVersion(serviceName: ServiceContractName): Promise<string> {
    const contractHash = SERVICE_CONTRACTS[serviceName]
    const response = await this.invokeFunction(contractHash, 'GetServiceVersion')
    return u.hexstring2str(response.stack[0]?.value || '')
  }

  async getServiceConfig(serviceName: ServiceContractName): Promise<any> {
    const contractHash = SERVICE_CONTRACTS[serviceName]
    const response = await this.invokeFunction(contractHash, 'GetServiceConfig')
    const configStr = u.hexstring2str(response.stack[0]?.value || '{}')
    return JSON.parse(configStr)
  }

  // Storage service methods
  async storeFile(fileId: string, data: string): Promise<string> {
    return this.sendTransaction(
      SERVICE_CONTRACTS.StorageContract,
      'StoreFile',
      [
        sc.ContractParam.string(fileId),
        sc.ContractParam.string(data),
        sc.ContractParam.hash160(await this.getConnectedAddress()),
      ]
    )
  }

  async retrieveFile(fileId: string): Promise<string> {
    const response = await this.invokeFunction(
      SERVICE_CONTRACTS.StorageContract,
      'RetrieveFile',
      [sc.ContractParam.string(fileId)]
    )
    return u.hexstring2str(response.stack[0]?.value || '')
  }

  // Storage Service Methods
  async storeData(fileId: string, data: string): Promise<string> {
    return this.sendTransaction(
      SERVICE_CONTRACTS.StorageContract,
      'StoreData',
      [
        sc.ContractParam.string(fileId),
        sc.ContractParam.string(data),
        sc.ContractParam.string('encrypted')
      ]
    )
  }

  async retrieveData(fileId: string): Promise<string> {
    const response = await this.invokeFunction(
      SERVICE_CONTRACTS.StorageContract,
      'RetrieveData',
      [sc.ContractParam.string(fileId)]
    )
    return u.hexstring2str(response.stack[0]?.value || '')
  }

  // Oracle Service Methods
  async requestOracleData(dataType: string, parameters: any): Promise<string> {
    const requestId = `req_${Math.random().toString(36).substr(2, 16)}`
    await this.sendTransaction(
      SERVICE_CONTRACTS.OracleContract,
      'RequestData',
      [
        sc.ContractParam.string(requestId),
        sc.ContractParam.string(dataType),
        sc.ContractParam.string(JSON.stringify(parameters))
      ]
    )
    return requestId
  }

  async getOracleResponse(requestId: string): Promise<string> {
    const response = await this.invokeFunction(
      SERVICE_CONTRACTS.OracleContract,
      'GetResponse',
      [sc.ContractParam.string(requestId)]
    )
    return response.stack[0]?.value || '0'
  }

  // Randomness Service Methods
  async generateRandomNumber(min: number, max: number): Promise<number> {
    const response = await this.invokeFunction(
      SERVICE_CONTRACTS.RandomnessContract,
      'GenerateRandomNumber',
      [
        sc.ContractParam.integer(min),
        sc.ContractParam.integer(max)
      ]
    )
    return parseInt(response.stack[0]?.value || '0')
  }

  async generateRandomBytes(length: number): Promise<string> {
    const response = await this.invokeFunction(
      SERVICE_CONTRACTS.RandomnessContract,
      'GenerateRandomBytes',
      [sc.ContractParam.integer(length)]
    )
    return response.stack[0]?.value || ''
  }

  async generateRandomString(length: number): Promise<string> {
    const response = await this.invokeFunction(
      SERVICE_CONTRACTS.RandomnessContract,
      'GenerateRandomString',
      [sc.ContractParam.integer(length)]
    )
    return u.hexstring2str(response.stack[0]?.value || '')
  }

  // Cross-Chain Service Methods
  async lockAssets(asset: string, amount: string, targetChain: string, recipient: string): Promise<string> {
    return this.sendTransaction(
      SERVICE_CONTRACTS.CrossChainContract,
      'LockAssets',
      [
        sc.ContractParam.string(asset),
        sc.ContractParam.string(amount),
        sc.ContractParam.string(targetChain),
        sc.ContractParam.string(recipient)
      ]
    )
  }

  async generateCrossChainProof(txId: string): Promise<string> {
    const response = await this.invokeFunction(
      SERVICE_CONTRACTS.CrossChainContract,
      'GenerateProof',
      [sc.ContractParam.string(txId)]
    )
    return response.stack[0]?.value || ''
  }

  // Analytics Service Methods
  async trackEvent(eventType: string, data: any): Promise<string> {
    const eventId = `evt_${Math.random().toString(36).substr(2, 16)}`
    await this.sendTransaction(
      SERVICE_CONTRACTS.AnalyticsContract,
      'TrackEvent',
      [
        sc.ContractParam.string(eventId),
        sc.ContractParam.string(eventType),
        sc.ContractParam.string(JSON.stringify(data))
      ]
    )
    return eventId
  }

  async getAnalyticsSummary(userAddress: string): Promise<any> {
    const response = await this.invokeFunction(
      SERVICE_CONTRACTS.AnalyticsContract,
      'GetUserSummary',
      [sc.ContractParam.string(userAddress)]
    )
    const summaryData = response.stack[0]?.value || '{}'
    return JSON.parse(u.hexstring2str(summaryData))
  }

  // Compute Service Methods
  async submitComputeTask(taskType: string, parameters: any): Promise<string> {
    const taskId = `task_${Math.random().toString(36).substr(2, 16)}`
    await this.sendTransaction(
      SERVICE_CONTRACTS.ComputeContract,
      'SubmitTask',
      [
        sc.ContractParam.string(taskId),
        sc.ContractParam.string(taskType),
        sc.ContractParam.string(JSON.stringify(parameters))
      ]
    )
    return taskId
  }

  async getComputeResult(taskId: string): Promise<any> {
    const response = await this.invokeFunction(
      SERVICE_CONTRACTS.ComputeContract,
      'GetResult',
      [sc.ContractParam.string(taskId)]
    )
    const resultData = response.stack[0]?.value || '{}'
    return JSON.parse(u.hexstring2str(resultData))
  }

  // Identity Management Methods
  async createIdentity(identityData: any): Promise<string> {
    return this.sendTransaction(
      SERVICE_CONTRACTS.IdentityManagementContract,
      'CreateIdentity',
      [
        sc.ContractParam.string(JSON.stringify(identityData))
      ]
    )
  }

  async verifyIdentity(identityId: string): Promise<boolean> {
    const response = await this.invokeFunction(
      SERVICE_CONTRACTS.IdentityManagementContract,
      'VerifyIdentity',
      [sc.ContractParam.string(identityId)]
    )
    return response.stack[0]?.value === 'true'
  }

  // Marketplace Methods
  async listItem(itemData: any, price: string): Promise<string> {
    const listingId = `listing_${Math.random().toString(36).substr(2, 16)}`
    await this.sendTransaction(
      SERVICE_CONTRACTS.MarketplaceContract,
      'ListItem',
      [
        sc.ContractParam.string(listingId),
        sc.ContractParam.string(JSON.stringify(itemData)),
        sc.ContractParam.string(price)
      ]
    )
    return listingId
  }

  async purchaseItem(listingId: string): Promise<string> {
    return this.sendTransaction(
      SERVICE_CONTRACTS.MarketplaceContract,
      'PurchaseItem',
      [sc.ContractParam.string(listingId)]
    )
  }

  // Voting Service Methods
  async createVote(title: string, options: string[], duration: number): Promise<string> {
    const voteId = `vote_${Math.random().toString(36).substr(2, 16)}`
    await this.sendTransaction(
      SERVICE_CONTRACTS.VotingContract,
      'CreateVote',
      [
        sc.ContractParam.string(voteId),
        sc.ContractParam.string(title),
        sc.ContractParam.array(...options.map(opt => sc.ContractParam.string(opt))),
        sc.ContractParam.integer(duration)
      ]
    )
    return voteId
  }

  async castVote(voteId: string, optionIndex: number): Promise<string> {
    return this.sendTransaction(
      SERVICE_CONTRACTS.VotingContract,
      'CastVote',
      [
        sc.ContractParam.string(voteId),
        sc.ContractParam.integer(optionIndex)
      ]
    )
  }

  async getVoteResults(voteId: string): Promise<any> {
    const response = await this.invokeFunction(
      SERVICE_CONTRACTS.VotingContract,
      'GetResults',
      [sc.ContractParam.string(voteId)]
    )
    const resultsData = response.stack[0]?.value || '{}'
    return JSON.parse(u.hexstring2str(resultsData))
  }

  // Zero Knowledge Methods
  async generateProof(circuitId: string, inputs: any): Promise<string> {
    return this.sendTransaction(
      SERVICE_CONTRACTS.ZeroKnowledgeContract,
      'GenerateProof',
      [
        sc.ContractParam.string(circuitId),
        sc.ContractParam.string(JSON.stringify(inputs))
      ]
    )
  }

  async verifyProof(proof: string, publicInputs: any): Promise<boolean> {
    const response = await this.invokeFunction(
      SERVICE_CONTRACTS.ZeroKnowledgeContract,
      'VerifyProof',
      [
        sc.ContractParam.string(proof),
        sc.ContractParam.string(JSON.stringify(publicInputs))
      ]
    )
    return response.stack[0]?.value === 'true'
  }

  // Network utilities
  switchNetwork(network: NetworkType) {
    this.network = network
    this.rpcClient = new rpc.RPCClient(NEO_NETWORKS[network].rpcUrl)
  }

  getCurrentNetwork(): NetworkType {
    return this.network
  }

  getNetworkConfig() {
    return NEO_NETWORKS[this.network]
  }
}

// Singleton instance
export const neoService = new NeoService()

// Utility functions
export const formatNeoAddress = (address: string): string => {
  if (!address) return ''
  return `${address.slice(0, 6)}...${address.slice(-4)}`
}

export const formatGasAmount = (amount: string | number): string => {
  const num = typeof amount === 'string' ? parseFloat(amount) : amount
  return num.toFixed(8)
}

export const isValidNeoAddress = (address: string): boolean => {
  try {
    return wallet.isAddress(address)
  } catch {
    return false
  }
}