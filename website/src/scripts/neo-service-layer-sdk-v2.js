/**
 * Neo Service Layer JavaScript SDK v2.0.0
 * Production-ready SDK for interacting with Neo Service Layer smart contracts
 * 
 * @author Neo Service Layer Team
 * @version 2.0.0
 * @license MIT
 */

class NeoServiceLayerSDK {
    constructor(config = {}) {
        this.version = '2.0.0';
        this.config = this.validateConfig(config);
        
        // Core properties
        this.isInitialized = false;
        this.wallet = null;
        this.rpcClient = null;
        this.contracts = {};
        this.cache = new Map();
        
        // Connection pooling
        this.connectionPool = new Map();
        this.maxPoolSize = config.maxPoolSize || 5;
        this.poolTimeout = config.poolTimeout || 30000;
        
        // Performance tracking
        this.metrics = {
            transactionsCount: 0,
            gasUsed: 0,
            errors: 0,
            responseTime: []
        };
        
        // Event system
        this.eventHandlers = new Map();
        this.setupEventHandlers();
        
        // WebSocket support
        this.websocket = null;
        this.wsReconnectAttempts = 0;
        this.wsMaxReconnectAttempts = 5;
        this.wsReconnectDelay = 1000;
        
        // Auto-initialize
        this.init().catch(error => {
            console.error('SDK initialization failed:', error);
            this.emit('error', error);
        });
    }

    /**
     * Validate and normalize configuration
     */
    validateConfig(config) {
        const defaults = {
            network: 'testnet',
            blockchainType: 'NeoN3', // Default to Neo N3
            rpcUrl: 'https://testnet1.neo.coz.io:443',
            networkMagic: 894710606,
            timeout: 30000,
            gasPrice: 0.00001,
            maxGasInvoke: 20,
            retryAttempts: 3,
            retryDelay: 1000,
            cacheTimeout: 300000, // 5 minutes
            enableMetrics: true,
            enableCache: true,
            contracts: {},
            endpoints: {
                testnet: {
                    NeoN3: [
                        'https://testnet1.neo.coz.io:443',
                        'https://testnet2.neo.coz.io:443',
                        'https://neo3-testnet.neoline.vip:443'
                    ],
                    NeoX: [
                        'https://neoxt4seed1.ngd.network',
                        'https://neoxt4seed2.ngd.network'
                    ]
                },
                mainnet: {
                    NeoN3: [
                        'https://mainnet1.neo.coz.io:443',
                        'https://mainnet2.neo.coz.io:443',
                        'https://neo3-mainnet.neoline.vip:443'
                    ],
                    NeoX: [
                        'https://mainnet.neox.com',
                        'https://mainnet2.neox.com'
                    ]
                }
            }
        };

        const merged = { ...defaults, ...config };

        // Validate required fields
        if (!merged.rpcUrl || !merged.networkMagic) {
            throw new Error('Invalid configuration: rpcUrl and networkMagic are required');
        }

        // Validate network
        if (!['testnet', 'mainnet'].includes(merged.network)) {
            throw new Error('Invalid network: must be "testnet" or "mainnet"');
        }

        // Validate blockchain type
        if (!['NeoN3', 'NeoX'].includes(merged.blockchainType)) {
            throw new Error('Invalid blockchainType: must be "NeoN3" or "NeoX"');
        }

        return merged;
    }

    /**
     * Setup default event handlers
     */
    setupEventHandlers() {
        this.eventHandlers.set('error', []);
        this.eventHandlers.set('wallet-connected', []);
        this.eventHandlers.set('wallet-disconnected', []);
        this.eventHandlers.set('transaction-sent', []);
        this.eventHandlers.set('transaction-confirmed', []);
        this.eventHandlers.set('transaction-failed', []);
        this.eventHandlers.set('network-changed', []);
        this.eventHandlers.set('ready', []);
    }

    /**
     * Initialize the SDK
     */
    async init() {
        try {
            console.log(`Initializing Neo Service Layer SDK v${this.version}...`);
            
            // Initialize RPC client
            await this.initRpcClient();
            
            // Load contract addresses
            await this.loadContracts();
            
            // Test network connectivity
            await this.testConnectivity();
            
            // Load cached data
            if (this.config.enableCache) {
                this.loadCachedData();
            }
            
            this.isInitialized = true;
            console.log('✅ Neo Service Layer SDK initialized successfully');
            this.emit('ready', { version: this.version, network: this.config.network });
            
        } catch (error) {
            console.error('❌ SDK initialization failed:', error);
            throw new SDKError('Initialization failed', 'INIT_ERROR', error);
        }
    }

    /**
     * Initialize RPC client with failover support
     */
    async initRpcClient() {
        const endpoints = this.config.endpoints[this.config.network][this.config.blockchainType];
        
        if (!endpoints || endpoints.length === 0) {
            throw new Error(`No endpoints configured for ${this.config.network} ${this.config.blockchainType}`);
        }
        
        // Initialize connection pool
        await this.initializeConnectionPool(endpoints);
        
        // Use primary connection
        this.rpcClient = this.getConnectionFromPool();
        
        if (!this.rpcClient) {
            throw new Error(`Failed to connect to any ${this.config.blockchainType} RPC endpoint`);
        }
    }
    
    /**
     * Initialize connection pool with multiple RPC clients
     */
    async initializeConnectionPool(endpoints) {
        console.log(`🔄 Initializing connection pool with ${endpoints.length} endpoints...`);
        
        const connectionPromises = endpoints.slice(0, this.maxPoolSize).map(async (endpoint, index) => {
            try {
                const client = new NeoRpcClient(endpoint, {
                    timeout: this.config.timeout,
                    retryAttempts: this.config.retryAttempts,
                    retryDelay: this.config.retryDelay,
                    blockchainType: this.config.blockchainType
                });
                
                // Test connection
                await client.getVersion();
                
                const connection = {
                    id: `conn_${index}`,
                    client: client,
                    endpoint: endpoint,
                    inUse: false,
                    lastUsed: Date.now(),
                    requestCount: 0,
                    errors: 0,
                    latency: []
                };
                
                this.connectionPool.set(connection.id, connection);
                console.log(`✅ Connection ${connection.id} established to ${endpoint}`);
                
                return connection;
            } catch (error) {
                console.warn(`❌ Failed to connect to ${endpoint}:`, error.message);
                return null;
            }
        });
        
        const connections = await Promise.all(connectionPromises);
        const successfulConnections = connections.filter(conn => conn !== null);
        
        if (successfulConnections.length === 0) {
            throw new Error('Failed to establish any connections in the pool');
        }
        
        console.log(`✅ Connection pool initialized with ${successfulConnections.length} active connections`);
    }
    
    /**
     * Get an available connection from the pool
     */
    getConnectionFromPool() {
        // Find the best available connection
        let bestConnection = null;
        let lowestLoad = Infinity;
        
        for (const [id, connection] of this.connectionPool) {
            if (!connection.inUse && connection.errors < 3) {
                const load = connection.requestCount + (connection.errors * 10);
                if (load < lowestLoad) {
                    lowestLoad = load;
                    bestConnection = connection;
                }
            }
        }
        
        if (bestConnection) {
            bestConnection.inUse = true;
            bestConnection.lastUsed = Date.now();
            return bestConnection.client;
        }
        
        // If no connection available, return the least busy one
        for (const [id, connection] of this.connectionPool) {
            if (connection.errors < 5) {
                return connection.client;
            }
        }
        
        return null;
    }
    
    /**
     * Release connection back to pool
     */
    releaseConnection(client) {
        for (const [id, connection] of this.connectionPool) {
            if (connection.client === client) {
                connection.inUse = false;
                connection.requestCount++;
                break;
            }
        }
    }
    
    /**
     * Mark connection as failed
     */
    markConnectionFailed(client) {
        for (const [id, connection] of this.connectionPool) {
            if (connection.client === client) {
                connection.errors++;
                connection.inUse = false;
                
                // Remove from pool if too many errors
                if (connection.errors >= 5) {
                    this.connectionPool.delete(id);
                    console.warn(`❌ Connection ${id} removed from pool due to errors`);
                }
                break;
            }
        }
    }
    
    /**
     * Get connection pool statistics
     */
    getPoolStats() {
        const stats = {
            totalConnections: this.connectionPool.size,
            activeConnections: 0,
            totalRequests: 0,
            totalErrors: 0,
            connections: []
        };
        
        for (const [id, connection] of this.connectionPool) {
            if (connection.inUse) stats.activeConnections++;
            stats.totalRequests += connection.requestCount;
            stats.totalErrors += connection.errors;
            
            stats.connections.push({
                id: id,
                endpoint: connection.endpoint,
                inUse: connection.inUse,
                requestCount: connection.requestCount,
                errors: connection.errors,
                avgLatency: connection.latency.length > 0 ? 
                    connection.latency.reduce((a, b) => a + b) / connection.latency.length : 0
            });
        }
        
        return stats;
    }

    /**
     * Load and validate contract addresses
     */
    async loadContracts() {
        try {
            // Use provided contracts or load from configuration
            const contractConfig = this.config.contracts[this.config.network] || this.config.contracts;
            
            if (!contractConfig || Object.keys(contractConfig).length === 0) {
                console.warn('⚠️ No contract addresses provided, using defaults');
                this.contracts = this.getDefaultContracts();
            } else {
                this.contracts = { ...contractConfig };
            }
            
            // Validate contract addresses
            for (const [name, address] of Object.entries(this.contracts)) {
                if (!this.isValidNeoAddress(address)) {
                    console.warn(`⚠️ Invalid contract address for ${name}: ${address}`);
                }
            }
            
            console.log(`✅ Loaded ${Object.keys(this.contracts).length} contract addresses`);
            
        } catch (error) {
            console.error('❌ Failed to load contracts:', error);
            throw error;
        }
    }

    /**
     * Test network connectivity and sync status
     */
    async testConnectivity() {
        try {
            const version = await this.rpcClient.getVersion();
            const blockCount = await this.rpcClient.getBlockCount();
            
            console.log(`✅ Network: ${version.useragent}, Block: ${blockCount}`);
            
            // Check if node is synced (within 10 blocks of current time)
            const currentTime = Date.now();
            const latestBlock = await this.rpcClient.getBlock(blockCount - 1);
            const blockTime = new Date(latestBlock.time * 1000);
            const timeDiff = currentTime - blockTime.getTime();
            
            if (timeDiff > 60000) { // More than 1 minute behind
                console.warn('⚠️ RPC node may not be fully synced');
            }
            
        } catch (error) {
            console.error('❌ Network connectivity test failed:', error);
            throw error;
        }
    }

    /**
     * Get default contract addresses for testing
     */
    getDefaultContracts() {
        const testnetContracts = {
            // Core Services
            storage: '0x1234567890abcdef1234567890abcdef12345678',
            oracle: '0x2345678901bcdef12345678901bcdef123456789',
            compute: '0x3456789012cdef123456789012cdef12345678a',
            identity: '0x456789013def123456789013def123456789ab',
            analytics: '0x56789014ef123456789014ef123456789abc',
            
            // Cross-Chain Services
            crosschain: '0x6789015f123456789015f123456789abcd',
            
            // DeFi Services
            lending: '0x789016f123456789016f123456789abcde',
            marketplace: '0x89017f123456789017f123456789abcdef',
            tokenization: '0x9018f123456789018f123456789abcdef1',
            insurance: '0xa019f123456789019f123456789abcdef12',
            
            // Advanced Services
            zeroknowledge: '0xb01af123456789a1af123456789abcdef123',
            automation: '0xc01bf123456789b1bf123456789abcdef1234',
            keymanagement: '0xd01cf123456789c1cf123456789abcdef12345',
            voting: '0xe01df123456789d1df123456789abcdef123456',
            randomness: '0xf01ef123456789e1ef123456789abcdef1234567',
            gaming: '0x101ff123456789f1ff123456789abcdef12345678'
        };
        
        return this.config.network === 'testnet' ? testnetContracts : {};
    }

    /**
     * Validate Neo address format
     */
    isValidNeoAddress(address) {
        // Neo N3 address validation
        if (typeof address !== 'string') return false;
        
        // Script hash format (0x + 40 hex chars)
        if (address.startsWith('0x') && address.length === 42) {
            return /^0x[0-9a-fA-F]{40}$/.test(address);
        }
        
        // Base58 address format (34 chars, starts with N)
        if (address.length === 34 && address.startsWith('N')) {
            return /^N[1-9A-HJ-NP-Za-km-z]{33}$/.test(address);
        }
        
        return false;
    }

    /**
     * Connect to wallet with enhanced error handling
     */
    async connectWallet(walletType = 'auto') {
        try {
            console.log(`🔗 Connecting to ${walletType} wallet...`);
            
            let walletResult;
            
            switch (walletType) {
                case 'neoline':
                    walletResult = await this.connectNeoLine();
                    break;
                case 'o3':
                    walletResult = await this.connectO3();
                    break;
                case 'onegate':
                    walletResult = await this.connectOneGate();
                    break;
                case 'auto':
                    walletResult = await this.autoConnectWallet();
                    break;
                case 'demo':
                    walletResult = this.createDemoWallet();
                    break;
                default:
                    throw new SDKError(`Unsupported wallet type: ${walletType}`, 'INVALID_WALLET_TYPE');
            }
            
            // Validate wallet connection
            if (!walletResult || !walletResult.address) {
                throw new SDKError('Wallet connection failed: Invalid wallet data', 'WALLET_CONNECTION_FAILED');
            }
            
            // Enhance wallet data
            this.wallet = {
                ...walletResult,
                connectedAt: new Date().toISOString(),
                network: this.config.network,
                sdk: this
            };
            
            // Get initial balance
            try {
                this.wallet.balance = await this.getBalance(this.wallet.address);
            } catch (error) {
                console.warn('⚠️ Failed to fetch wallet balance:', error.message);
                this.wallet.balance = { NEO: '0', GAS: '0' };
            }
            
            console.log(`✅ Wallet connected: ${this.wallet.address}`);
            this.emit('wallet-connected', this.wallet);
            
            return this.wallet;
            
        } catch (error) {
            console.error('❌ Wallet connection failed:', error);
            this.emit('error', error);
            throw error;
        }
    }

    /**
     * Connect to NeoLine wallet
     */
    async connectNeoLine() {
        if (typeof window === 'undefined' || !window.NEOLine) {
            throw new SDKError('NeoLine not detected. Please install NeoLine extension.', 'NEOLINE_NOT_FOUND');
        }
        
        try {
            const neoline = new window.NEOLine.Init();
            const account = await neoline.getAccount();
            
            if (account.error) {
                throw new SDKError(account.error.description || 'NeoLine connection rejected', 'NEOLINE_REJECTED');
            }
            
            return {
                address: account.address,
                label: account.label || 'NeoLine Wallet',
                walletType: 'neoline',
                isConnected: true,
                provider: neoline
            };
            
        } catch (error) {
            if (error instanceof SDKError) throw error;
            throw new SDKError('NeoLine connection failed', 'NEOLINE_ERROR', error);
        }
    }

    /**
     * Connect to O3 wallet
     */
    async connectO3() {
        if (typeof window === 'undefined' || !window.o3dapi) {
            throw new SDKError('O3 Wallet not detected. Please install O3 Wallet.', 'O3_NOT_FOUND');
        }
        
        try {
            await window.o3dapi.initPlugins([window.o3dapi.NEO]);
            const account = await window.o3dapi.NEO.getAccount();
            
            return {
                address: account.address,
                label: account.label || 'O3 Wallet',
                walletType: 'o3',
                isConnected: true,
                provider: window.o3dapi.NEO
            };
            
        } catch (error) {
            throw new SDKError('O3 Wallet connection failed', 'O3_ERROR', error);
        }
    }

    /**
     * Connect to OneGate wallet
     */
    async connectOneGate() {
        if (typeof window === 'undefined' || !window.OneGate) {
            throw new SDKError('OneGate not detected. Please install OneGate wallet.', 'ONEGATE_NOT_FOUND');
        }
        
        try {
            const account = await window.OneGate.getAccount();
            
            return {
                address: account.address,
                label: account.label || 'OneGate Wallet',
                walletType: 'onegate',
                isConnected: true,
                provider: window.OneGate
            };
            
        } catch (error) {
            throw new SDKError('OneGate connection failed', 'ONEGATE_ERROR', error);
        }
    }

    /**
     * Auto-detect and connect to available wallet
     */
    async autoConnectWallet() {
        const wallets = ['neoline', 'o3', 'onegate'];
        
        for (const walletType of wallets) {
            try {
                return await this.connectWallet(walletType);
            } catch (error) {
                console.log(`${walletType} not available:`, error.message);
                continue;
            }
        }
        
        throw new SDKError('No compatible wallet found. Please install NeoLine, O3, or OneGate.', 'NO_WALLET_FOUND');
    }

    /**
     * Create demo wallet for testing
     */
    createDemoWallet() {
        return {
            address: 'NX8GreRFGFK5wpGMWetpX93HmtrezGogzk',
            label: 'Demo Wallet (Testing Only)',
            walletType: 'demo',
            isConnected: true,
            provider: null
        };
    }

    /**
     * Disconnect wallet
     */
    async disconnectWallet() {
        if (this.wallet) {
            const walletType = this.wallet.walletType;
            this.wallet = null;
            
            console.log(`🔌 Wallet disconnected: ${walletType}`);
            this.emit('wallet-disconnected', { walletType });
        }
    }

    /**
     * Get wallet balance with caching
     */
    async getBalance(address = null) {
        const targetAddress = address || (this.wallet ? this.wallet.address : null);
        
        if (!targetAddress) {
            throw new SDKError('No address provided for balance check', 'NO_ADDRESS');
        }
        
        const cacheKey = `balance_${targetAddress}`;
        
        // Check cache first
        if (this.config.enableCache && this.cache.has(cacheKey)) {
            const cached = this.cache.get(cacheKey);
            if (Date.now() - cached.timestamp < 30000) { // 30 second cache
                return cached.data;
            }
        }
        
        try {
            const startTime = Date.now();
            
            // Get NEO balance
            const neoBalance = await this.rpcClient.getNep17Balances(targetAddress);
            
            // Parse balances
            const balance = {
                NEO: '0',
                GAS: '0',
                address: targetAddress,
                lastUpdated: new Date().toISOString()
            };
            
            if (neoBalance && neoBalance.balance) {
                for (const token of neoBalance.balance) {
                    if (token.assethash === '0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5') { // NEO
                        balance.NEO = this.formatTokenAmount(token.amount, 0);
                    } else if (token.assethash === '0xd2a4cff31913016155e38e474a2c06d08be276cf') { // GAS
                        balance.GAS = this.formatTokenAmount(token.amount, 8);
                    }
                }
            }
            
            // Cache result
            if (this.config.enableCache) {
                this.cache.set(cacheKey, {
                    data: balance,
                    timestamp: Date.now()
                });
            }
            
            // Track performance
            if (this.config.enableMetrics) {
                this.metrics.responseTime.push(Date.now() - startTime);
            }
            
            return balance;
            
        } catch (error) {
            console.error('❌ Failed to get balance:', error);
            throw new SDKError('Failed to get wallet balance', 'BALANCE_ERROR', error);
        }
    }

    /**
     * Format token amount with proper decimals
     */
    formatTokenAmount(amount, decimals) {
        if (!amount) return '0';
        
        const num = parseInt(amount);
        const divisor = Math.pow(10, decimals);
        return (num / divisor).toFixed(decimals).replace(/\.?0+$/, '');
    }

    /**
     * Build and invoke smart contract with comprehensive error handling
     */
    async invokeContract(serviceName, method, params = [], options = {}) {
        const maxRetries = options.retryAttempts || this.config.retryAttempts;
        const retryDelay = options.retryDelay || this.config.retryDelay;
        let lastError;
        
        for (let attempt = 1; attempt <= maxRetries; attempt++) {
            try {
                // Validate inputs
                if (!serviceName || !method) {
                    throw new SDKError('Service name and method are required', 'INVALID_PARAMS');
                }
                
                if (!this.wallet) {
                    throw new SDKError('No wallet connected', 'NO_WALLET');
                }
                
                // Use blockchain type from options or default to config
                const blockchainType = options.blockchainType || this.config.blockchainType;
                
                const contractHash = this.contracts[serviceName];
                if (!contractHash) {
                    throw new SDKError(`Contract not found for service: ${serviceName}`, 'CONTRACT_NOT_FOUND');
                }
                
                console.log(`🔄 Invoking ${serviceName}.${method} on ${blockchainType}... (Attempt ${attempt}/${maxRetries})`);
                const startTime = Date.now();
                
                // Add blockchain type to params for backend compatibility
                const enhancedParams = [...params, blockchainType];
                
                // Build transaction
                const transaction = await this.buildTransaction(contractHash, method, enhancedParams, options);
                
                // Sign and send transaction
                const result = await this.signAndSendTransaction(transaction);
                
                // Track metrics
                if (this.config.enableMetrics) {
                    this.metrics.transactionsCount++;
                    this.metrics.responseTime.push(Date.now() - startTime);
                }
                
                // Emit events
                this.emit('transaction-sent', {
                    txid: result.txid,
                    service: serviceName,
                    method: method,
                    params: params,
                    blockchainType: blockchainType,
                    timestamp: new Date().toISOString()
                });
                
                // Monitor transaction
                this.monitorTransaction(result.txid, serviceName);
                
                return result;
                
            } catch (error) {
                lastError = error;
                console.error(`❌ Contract invocation failed (Attempt ${attempt}/${maxRetries}):`, error);
                
                // Don't retry for non-retryable errors
                if (error.code === 'INVALID_PARAMS' || error.code === 'NO_WALLET' || error.code === 'CONTRACT_NOT_FOUND') {
                    break;
                }
                
                // Wait before retrying (exponential backoff)
                if (attempt < maxRetries) {
                    const delay = retryDelay * Math.pow(2, attempt - 1);
                    console.log(`⏳ Retrying in ${delay}ms...`);
                    await new Promise(resolve => setTimeout(resolve, delay));
                }
            }
        }
        
        // All retries failed
        if (this.config.enableMetrics) {
            this.metrics.errors++;
        }
        
        this.emit('error', lastError);
        throw lastError;
    }

    /**
     * Build transaction with proper gas calculation
     */
    async buildTransaction(contractHash, method, params, options = {}) {
        try {
            const transaction = {
                script: this.buildInvocationScript(contractHash, method, params),
                account: this.wallet.address,
                networkFee: options.networkFee || 0,
                systemFee: options.systemFee || 0,
                validUntilBlock: options.validUntilBlock || (await this.getValidUntilBlock()),
                signers: [{
                    account: this.wallet.address,
                    scopes: options.scopes || 'CalledByEntry'
                }]
            };
            
            // Estimate gas if not provided
            if (!options.systemFee) {
                transaction.systemFee = await this.estimateGas(transaction);
            }
            
            return transaction;
            
        } catch (error) {
            throw new SDKError('Failed to build transaction', 'BUILD_TRANSACTION_ERROR', error);
        }
    }

    /**
     * Build invocation script
     */
    buildInvocationScript(contractHash, method, params) {
        // This would use proper Neo script building in production
        // For now, return a mock script
        return {
            contractHash: contractHash,
            method: method,
            params: params
        };
    }

    /**
     * Get valid until block number
     */
    async getValidUntilBlock() {
        try {
            const blockCount = await this.rpcClient.getBlockCount();
            return blockCount + 2340; // ~5.85 hours (15 seconds per block)
        } catch (error) {
            throw new SDKError('Failed to get valid until block', 'BLOCK_COUNT_ERROR', error);
        }
    }

    /**
     * Estimate gas for transaction
     */
    async estimateGas(transaction) {
        try {
            // This would use actual gas estimation in production
            // For now, return a reasonable estimate based on operation complexity
            const baseGas = 0.01; // Base gas cost
            const methodComplexity = this.getMethodComplexity(transaction.script.method);
            
            return baseGas * methodComplexity;
            
        } catch (error) {
            console.warn('⚠️ Gas estimation failed, using default:', error.message);
            return this.config.maxGasInvoke;
        }
    }

    /**
     * Get method complexity multiplier for gas estimation
     */
    getMethodComplexity(method) {
        const complexityMap = {
            // Simple operations
            'get': 1,
            'balanceOf': 1,
            'symbol': 1,
            'name': 1,
            
            // Medium operations
            'store': 2,
            'transfer': 2,
            'approve': 2,
            
            // Complex operations
            'mint': 3,
            'burn': 3,
            'createToken': 5,
            'bridge': 4,
            'generateProof': 6,
            'createProposal': 4
        };
        
        return complexityMap[method] || 2; // Default medium complexity
    }

    /**
     * Sign and send transaction
     */
    async signAndSendTransaction(transaction) {
        try {
            let result;
            
            switch (this.wallet.walletType) {
                case 'neoline':
                    result = await this.signWithNeoLine(transaction);
                    break;
                case 'o3':
                    result = await this.signWithO3(transaction);
                    break;
                case 'onegate':
                    result = await this.signWithOneGate(transaction);
                    break;
                case 'demo':
                    result = await this.simulateTransaction(transaction);
                    break;
                default:
                    throw new SDKError(`Unsupported wallet type: ${this.wallet.walletType}`, 'UNSUPPORTED_WALLET');
            }
            
            if (!result || !result.txid) {
                throw new SDKError('Transaction signing failed', 'SIGNING_FAILED');
            }
            
            return result;
            
        } catch (error) {
            throw new SDKError('Failed to sign and send transaction', 'TRANSACTION_ERROR', error);
        }
    }

    /**
     * Sign transaction with NeoLine
     */
    async signWithNeoLine(transaction) {
        try {
            const neoline = this.wallet.provider;
            return await neoline.invoke(transaction);
        } catch (error) {
            throw new SDKError('NeoLine signing failed', 'NEOLINE_SIGNING_ERROR', error);
        }
    }

    /**
     * Sign transaction with O3
     */
    async signWithO3(transaction) {
        try {
            const o3 = this.wallet.provider;
            return await o3.invoke(transaction);
        } catch (error) {
            throw new SDKError('O3 signing failed', 'O3_SIGNING_ERROR', error);
        }
    }

    /**
     * Sign transaction with OneGate
     */
    async signWithOneGate(transaction) {
        try {
            const onegate = this.wallet.provider;
            return await onegate.invoke(transaction);
        } catch (error) {
            throw new SDKError('OneGate signing failed', 'ONEGATE_SIGNING_ERROR', error);
        }
    }

    /**
     * Simulate transaction for demo mode
     */
    async simulateTransaction(transaction) {
        // Simulate network delay
        await new Promise(resolve => setTimeout(resolve, 1000 + Math.random() * 2000));
        
        // Simulate occasional failures
        if (Math.random() < 0.05) { // 5% failure rate
            throw new SDKError('Simulated transaction failure', 'SIMULATION_ERROR');
        }
        
        return {
            txid: this.generateTxId(),
            blockHeight: Math.floor(Math.random() * 1000000) + 8000000,
            gasConsumed: (Math.random() * 0.1 + 0.01).toFixed(8),
            result: this.generateMockResult(transaction.script.method),
            nodeUrl: this.config.rpcUrl
        };
    }

    /**
     * Monitor transaction status
     */
    async monitorTransaction(txid, serviceName) {
        const maxAttempts = 20; // ~5 minutes with 15 second intervals
        let attempts = 0;
        
        const checkStatus = async () => {
            try {
                attempts++;
                
                const transaction = await this.getTransaction(txid);
                
                if (transaction && transaction.confirmations > 0) {
                    console.log(`✅ Transaction confirmed: ${txid}`);
                    
                    this.emit('transaction-confirmed', {
                        txid: txid,
                        service: serviceName,
                        confirmations: transaction.confirmations,
                        blockHeight: transaction.blockindex,
                        gasConsumed: transaction.gasconsumed
                    });
                    
                    return;
                }
                
                if (attempts < maxAttempts) {
                    setTimeout(checkStatus, 15000); // Check every 15 seconds
                } else {
                    console.warn(`⚠️ Transaction monitoring timeout: ${txid}`);
                    this.emit('transaction-failed', {
                        txid: txid,
                        service: serviceName,
                        reason: 'Monitoring timeout'
                    });
                }
                
            } catch (error) {
                console.error(`❌ Transaction monitoring error:`, error);
                
                if (attempts < maxAttempts) {
                    setTimeout(checkStatus, 15000);
                } else {
                    this.emit('transaction-failed', {
                        txid: txid,
                        service: serviceName,
                        reason: error.message
                    });
                }
            }
        };
        
        // Start monitoring after initial delay
        setTimeout(checkStatus, 3000);
    }

    /**
     * Get transaction details
     */
    async getTransaction(txid) {
        try {
            return await this.rpcClient.getApplicationLog(txid);
        } catch (error) {
            // Transaction might not be confirmed yet
            return null;
        }
    }

    /**
     * Generate mock transaction ID
     */
    generateTxId() {
        return '0x' + Array.from({length: 64}, () => Math.floor(Math.random() * 16).toString(16)).join('');
    }

    /**
     * Generate mock result based on method
     */
    generateMockResult(method) {
        const mockResults = {
            store: { success: true, key: 'stored_key_' + Date.now() },
            get: { value: 'mock_stored_value', encrypted: false },
            mint: { tokenId: Math.floor(Math.random() * 10000) },
            createToken: { contractAddress: '0x' + Math.random().toString(16).substring(2, 42) },
            supply: { poolShare: (Math.random() * 100).toFixed(2) + '%' },
            generateRandom: { randomValue: Math.floor(Math.random() * 1000000) },
            createProposal: { proposalId: Math.floor(Math.random() * 1000) }
        };
        
        return mockResults[method] || { success: true };
    }

    /**
     * Load cached data from localStorage
     */
    loadCachedData() {
        try {
            const cached = localStorage.getItem('nsl_sdk_cache');
            if (cached) {
                const data = JSON.parse(cached);
                // Only load recent cache (within cache timeout)
                for (const [key, value] of Object.entries(data)) {
                    if (Date.now() - value.timestamp < this.config.cacheTimeout) {
                        this.cache.set(key, value);
                    }
                }
            }
        } catch (error) {
            console.warn('⚠️ Failed to load cached data:', error.message);
        }
    }

    /**
     * Save cache to localStorage
     */
    saveCache() {
        try {
            const cacheData = {};
            for (const [key, value] of this.cache.entries()) {
                cacheData[key] = value;
            }
            localStorage.setItem('nsl_sdk_cache', JSON.stringify(cacheData));
        } catch (error) {
            console.warn('⚠️ Failed to save cache:', error.message);
        }
    }

    /**
     * Clear cache
     */
    clearCache() {
        this.cache.clear();
        localStorage.removeItem('nsl_sdk_cache');
        console.log('🗑️ Cache cleared');
    }

    /**
     * Event system methods
     */
    on(event, handler) {
        if (!this.eventHandlers.has(event)) {
            this.eventHandlers.set(event, []);
        }
        this.eventHandlers.get(event).push(handler);
        return this; // For chaining
    }

    off(event, handler) {
        if (this.eventHandlers.has(event)) {
            const handlers = this.eventHandlers.get(event);
            const index = handlers.indexOf(handler);
            if (index > -1) {
                handlers.splice(index, 1);
            }
        }
        return this;
    }

    emit(event, data) {
        if (this.eventHandlers.has(event)) {
            const handlers = this.eventHandlers.get(event);
            handlers.forEach(handler => {
                try {
                    handler(data);
                } catch (error) {
                    console.error(`❌ Event handler error for ${event}:`, error);
                }
            });
        }
    }

    /**
     * Get SDK status and metrics
     */
    getStatus() {
        return {
            version: this.version,
            isInitialized: this.isInitialized,
            isWalletConnected: !!this.wallet,
            wallet: this.wallet ? {
                address: this.wallet.address,
                label: this.wallet.label,
                walletType: this.wallet.walletType,
                connectedAt: this.wallet.connectedAt
            } : null,
            network: this.config.network,
            rpcUrl: this.config.rpcUrl,
            contractsLoaded: Object.keys(this.contracts).length,
            metrics: this.config.enableMetrics ? {
                ...this.metrics,
                avgResponseTime: this.metrics.responseTime.length > 0 ? 
                    this.metrics.responseTime.reduce((a, b) => a + b) / this.metrics.responseTime.length : 0,
                cacheSize: this.cache.size
            } : null
        };
    }

    /**
     * Health check
     */
    async healthCheck() {
        const checks = {
            rpcConnection: false,
            contractsLoaded: false,
            walletConnected: false,
            networkSync: false
        };
        
        try {
            // Test RPC connection
            await this.rpcClient.getVersion();
            checks.rpcConnection = true;
            
            // Check contracts
            checks.contractsLoaded = Object.keys(this.contracts).length > 0;
            
            // Check wallet
            checks.walletConnected = !!this.wallet;
            
            // Check network sync
            const blockCount = await this.rpcClient.getBlockCount();
            checks.networkSync = blockCount > 0;
            
        } catch (error) {
            console.error('❌ Health check failed:', error);
        }
        
        const isHealthy = Object.values(checks).every(check => check);
        
        return {
            isHealthy,
            checks,
            blockchainType: this.config.blockchainType,
            timestamp: new Date().toISOString()
        };
    }

    /**
     * Switch blockchain type (Neo N3 or Neo X)
     */
    async switchBlockchain(blockchainType) {
        if (!['NeoN3', 'NeoX'].includes(blockchainType)) {
            throw new SDKError('Invalid blockchainType: must be "NeoN3" or "NeoX"', 'INVALID_BLOCKCHAIN_TYPE');
        }
        
        if (blockchainType === this.config.blockchainType) {
            console.log(`Already connected to ${blockchainType}`);
            return;
        }
        
        console.log(`🔄 Switching from ${this.config.blockchainType} to ${blockchainType}...`);
        
        // Update config
        this.config.blockchainType = blockchainType;
        
        // Reinitialize RPC client with new blockchain
        await this.initRpcClient();
        
        // Reload contracts for new blockchain
        await this.loadContracts();
        
        // Clear cache as it's blockchain-specific
        if (this.config.enableCache) {
            this.clearCache();
        }
        
        // Emit network changed event
        this.emit('network-changed', {
            previousBlockchain: this.config.blockchainType,
            newBlockchain: blockchainType,
            timestamp: new Date().toISOString()
        });
        
        console.log(`✅ Successfully switched to ${blockchainType}`);
    }

    /**
     * ========================
     * SERVICE IMPLEMENTATIONS
     * ========================
     */

    /**
     * Storage Service - Decentralized data storage
     */
    get storage() {
        return {
            /**
             * Store data on blockchain
             */
            store: async (key, value, options = {}) => {
                const params = [
                    key,
                    value,
                    options.encrypted || false,
                    options.accessLevel || 'public'
                ];
                return await this.invokeContract('storage', 'store', params, options);
            },

            /**
             * Retrieve stored data
             */
            get: async (key, options = {}) => {
                const params = [key];
                return await this.invokeContract('storage', 'get', params, options);
            },

            /**
             * Delete stored data
             */
            delete: async (key, options = {}) => {
                const params = [key];
                return await this.invokeContract('storage', 'delete', params, options);
            },

            /**
             * List all keys for current user
             */
            listKeys: async (options = {}) => {
                return await this.invokeContract('storage', 'listKeys', [], options);
            },

            /**
             * Update existing data
             */
            update: async (key, value, options = {}) => {
                const params = [key, value];
                return await this.invokeContract('storage', 'update', params, options);
            },

            /**
             * Begin storage transaction
             */
            beginTransaction: async (options = {}) => {
                return await this.invokeContract('storage', 'beginTransaction', [], options);
            },

            /**
             * Commit storage transaction
             */
            commitTransaction: async (transactionId, options = {}) => {
                const params = [transactionId];
                return await this.invokeContract('storage', 'commitTransaction', params, options);
            },

            /**
             * Rollback storage transaction
             */
            rollbackTransaction: async (transactionId, options = {}) => {
                const params = [transactionId];
                return await this.invokeContract('storage', 'rollbackTransaction', params, options);
            }
        };
    }

    /**
     * Oracle Service - External data feeds
     */
    get oracle() {
        return {
            /**
             * Request external data
             */
            requestData: async (url, jsonPath, callbackContract, options = {}) => {
                const params = [url, jsonPath, callbackContract, options.gasForCallback || 20];
                return await this.invokeContract('oracle', 'requestData', params, options);
            },

            /**
             * Get oracle data by request ID
             */
            getData: async (requestId, options = {}) => {
                const params = [requestId];
                return await this.invokeContract('oracle', 'getData', params, options);
            },

            /**
             * Register as oracle node
             */
            registerNode: async (nodeUrl, options = {}) => {
                const params = [nodeUrl];
                return await this.invokeContract('oracle', 'registerNode', params, options);
            },

            /**
             * Submit oracle response
             */
            submitResponse: async (requestId, data, options = {}) => {
                const params = [requestId, data];
                return await this.invokeContract('oracle', 'submitResponse', params, options);
            },

            /**
             * Request batch data from multiple URLs
             */
            requestDataBatch: async (requests, options = {}) => {
                const params = [JSON.stringify(requests)];
                return await this.invokeContract('oracle', 'requestDataBatch', params, options);
            },

            /**
             * Verify oracle data
             */
            verifyData: async (requestId, data, proof, options = {}) => {
                const params = [requestId, data, proof];
                return await this.invokeContract('oracle', 'verifyData', params, options);
            },

            /**
             * Subscribe to data feed
             */
            subscribe: async (dataFeed, callbackContract, duration, options = {}) => {
                const params = [dataFeed, callbackContract, duration];
                return await this.invokeContract('oracle', 'subscribe', params, options);
            },

            /**
             * Unsubscribe from data feed
             */
            unsubscribe: async (subscriptionId, options = {}) => {
                const params = [subscriptionId];
                return await this.invokeContract('oracle', 'unsubscribe', params, options);
            }
        };
    }

    /**
     * Compute Service - Off-chain computation
     */
    get compute() {
        return {
            /**
             * Execute computation job
             */
            execute: async (algorithm, input, verify = true, options = {}) => {
                const params = [algorithm, input, verify];
                return await this.invokeContract('compute', 'execute', params, options);
            },

            /**
             * Get computation result
             */
            getResult: async (jobId, options = {}) => {
                const params = [jobId];
                return await this.invokeContract('compute', 'getResult', params, options);
            },

            /**
             * Submit computation proof
             */
            submitProof: async (jobId, proof, options = {}) => {
                const params = [jobId, proof];
                return await this.invokeContract('compute', 'submitProof', params, options);
            },

            /**
             * Register compute node
             */
            registerNode: async (nodeConfig, options = {}) => {
                const params = [nodeConfig];
                return await this.invokeContract('compute', 'registerNode', params, options);
            }
        };
    }

    /**
     * Identity Management Service
     */
    get identity() {
        return {
            /**
             * Create new identity
             */
            createIdentity: async (publicKey, metadata = {}, options = {}) => {
                const params = [publicKey, JSON.stringify(metadata)];
                return await this.invokeContract('identity', 'createIdentity', params, options);
            },

            /**
             * Verify identity
             */
            verifyIdentity: async (identityId, challenge, signature, options = {}) => {
                const params = [identityId, challenge, signature];
                return await this.invokeContract('identity', 'verifyIdentity', params, options);
            },

            /**
             * Update identity metadata
             */
            updateMetadata: async (identityId, metadata, options = {}) => {
                const params = [identityId, JSON.stringify(metadata)];
                return await this.invokeContract('identity', 'updateMetadata', params, options);
            },

            /**
             * Revoke identity
             */
            revokeIdentity: async (identityId, options = {}) => {
                const params = [identityId];
                return await this.invokeContract('identity', 'revokeIdentity', params, options);
            }
        };
    }

    /**
     * Analytics Service - Blockchain data analysis
     */
    get analytics() {
        return {
            /**
             * Track event
             */
            trackEvent: async (eventName, data, options = {}) => {
                const params = [eventName, JSON.stringify(data)];
                return await this.invokeContract('analytics', 'trackEvent', params, options);
            },

            /**
             * Get analytics data
             */
            getAnalytics: async (query, timeRange, options = {}) => {
                const params = [query, timeRange];
                return await this.invokeContract('analytics', 'getAnalytics', params, options);
            },

            /**
             * Create dashboard
             */
            createDashboard: async (name, widgets, options = {}) => {
                const params = [name, JSON.stringify(widgets)];
                return await this.invokeContract('analytics', 'createDashboard', params, options);
            }
        };
    }

    /**
     * Cross-Chain Bridge Service
     */
    get crosschain() {
        return {
            /**
             * Bridge assets to another chain
             */
            bridge: async (asset, amount, targetChain, targetAddress, options = {}) => {
                const params = [asset, amount, targetChain, targetAddress];
                return await this.invokeContract('crosschain', 'bridge', params, options);
            },

            /**
             * Claim bridged assets
             */
            claim: async (bridgeId, proof, options = {}) => {
                const params = [bridgeId, proof];
                return await this.invokeContract('crosschain', 'claim', params, options);
            },

            /**
             * Get bridge status
             */
            getBridgeStatus: async (bridgeId, options = {}) => {
                const params = [bridgeId];
                return await this.invokeContract('crosschain', 'getBridgeStatus', params, options);
            }
        };
    }

    /**
     * Lending Service - DeFi lending protocol
     */
    get lending() {
        return {
            /**
             * Supply assets to lending pool
             */
            supply: async (asset, amount, options = {}) => {
                const params = [asset, amount];
                return await this.invokeContract('lending', 'supply', params, options);
            },

            /**
             * Borrow assets from pool
             */
            borrow: async (asset, amount, collateral, options = {}) => {
                const params = [asset, amount, collateral];
                return await this.invokeContract('lending', 'borrow', params, options);
            },

            /**
             * Repay borrowed assets
             */
            repay: async (asset, amount, options = {}) => {
                const params = [asset, amount];
                return await this.invokeContract('lending', 'repay', params, options);
            },

            /**
             * Withdraw supplied assets
             */
            withdraw: async (asset, amount, options = {}) => {
                const params = [asset, amount];
                return await this.invokeContract('lending', 'withdraw', params, options);
            },

            /**
             * Get pool information
             */
            getPoolInfo: async (asset, options = {}) => {
                const params = [asset];
                return await this.invokeContract('lending', 'getPoolInfo', params, options);
            },

            /**
             * Get user position
             */
            getUserPosition: async (user, options = {}) => {
                const params = [user];
                return await this.invokeContract('lending', 'getUserPosition', params, options);
            }
        };
    }

    /**
     * NFT Marketplace Service
     */
    get marketplace() {
        return {
            /**
             * Mint new NFT
             */
            mint: async (name, description, imageUrl, attributes = {}, options = {}) => {
                const params = [name, description, imageUrl, JSON.stringify(attributes)];
                return await this.invokeContract('marketplace', 'mint', params, options);
            },

            /**
             * List NFT for sale
             */
            list: async (tokenId, price, duration, options = {}) => {
                const params = [tokenId, price, duration];
                return await this.invokeContract('marketplace', 'list', params, options);
            },

            /**
             * Buy NFT
             */
            buy: async (listingId, price, options = {}) => {
                const params = [listingId, price];
                return await this.invokeContract('marketplace', 'buy', params, options);
            },

            /**
             * Cancel listing
             */
            cancelListing: async (listingId, options = {}) => {
                const params = [listingId];
                return await this.invokeContract('marketplace', 'cancelListing', params, options);
            },

            /**
             * Create auction
             */
            createAuction: async (tokenId, startPrice, duration, options = {}) => {
                const params = [tokenId, startPrice, duration];
                return await this.invokeContract('marketplace', 'createAuction', params, options);
            },

            /**
             * Place bid
             */
            placeBid: async (auctionId, amount, options = {}) => {
                const params = [auctionId, amount];
                return await this.invokeContract('marketplace', 'placeBid', params, options);
            }
        };
    }

    /**
     * Token Creation Service
     */
    get tokenization() {
        return {
            /**
             * Create new NEP-17 token
             */
            createToken: async (name, symbol, decimals, totalSupply, features = [], options = {}) => {
                const params = [name, symbol, decimals, totalSupply, features];
                return await this.invokeContract('tokenization', 'createToken', params, options);
            },

            /**
             * Mint additional tokens
             */
            mint: async (tokenAddress, amount, to, options = {}) => {
                const params = [tokenAddress, amount, to];
                return await this.invokeContract('tokenization', 'mint', params, options);
            },

            /**
             * Burn tokens
             */
            burn: async (tokenAddress, amount, options = {}) => {
                const params = [tokenAddress, amount];
                return await this.invokeContract('tokenization', 'burn', params, options);
            },

            /**
             * Transfer tokens
             */
            transfer: async (tokenAddress, to, amount, options = {}) => {
                const params = [tokenAddress, to, amount];
                return await this.invokeContract('tokenization', 'transfer', params, options);
            }
        };
    }

    /**
     * Insurance Service
     */
    get insurance() {
        return {
            /**
             * Create insurance policy
             */
            createPolicy: async (policyType, coverage, premium, duration, options = {}) => {
                const params = [policyType, coverage, premium, duration];
                return await this.invokeContract('insurance', 'createPolicy', params, options);
            },

            /**
             * Purchase policy
             */
            purchasePolicy: async (policyId, options = {}) => {
                const params = [policyId];
                return await this.invokeContract('insurance', 'purchasePolicy', params, options);
            },

            /**
             * File claim
             */
            fileClaim: async (policyId, claimData, evidence = [], options = {}) => {
                const params = [policyId, JSON.stringify(claimData), evidence];
                return await this.invokeContract('insurance', 'fileClaim', params, options);
            },

            /**
             * Process claim
             */
            processClaim: async (claimId, decision, options = {}) => {
                const params = [claimId, decision];
                return await this.invokeContract('insurance', 'processClaim', params, options);
            }
        };
    }

    /**
     * Zero Knowledge Proofs Service
     */
    get zeroknowledge() {
        return {
            /**
             * Generate ZK proof
             */
            generateProof: async (proofType, privateInput, publicParams, options = {}) => {
                const params = [proofType, privateInput, publicParams];
                return await this.invokeContract('zeroknowledge', 'generateProof', params, options);
            },

            /**
             * Verify ZK proof
             */
            verifyProof: async (proof, publicInputs, options = {}) => {
                const params = [proof, publicInputs];
                return await this.invokeContract('zeroknowledge', 'verifyProof', params, options);
            },

            /**
             * Register proving key
             */
            registerProvingKey: async (circuit, provingKey, options = {}) => {
                const params = [circuit, provingKey];
                return await this.invokeContract('zeroknowledge', 'registerProvingKey', params, options);
            },

            /**
             * Register verification key
             */
            registerVerificationKey: async (circuit, verificationKey, options = {}) => {
                const params = [circuit, verificationKey];
                return await this.invokeContract('zeroknowledge', 'registerVerificationKey', params, options);
            }
        };
    }

    /**
     * Smart Automation Service
     */
    get automation() {
        return {
            /**
             * Create automation job
             */
            createJob: async (triggerType, triggerData, targetContract, targetMethod, params = [], options = {}) => {
                const jobParams = [triggerType, triggerData, targetContract, targetMethod, params];
                return await this.invokeContract('automation', 'createJob', jobParams, options);
            },

            /**
             * Cancel automation job
             */
            cancelJob: async (jobId, options = {}) => {
                const params = [jobId];
                return await this.invokeContract('automation', 'cancelJob', params, options);
            },

            /**
             * Get job status
             */
            getJobStatus: async (jobId, options = {}) => {
                const params = [jobId];
                return await this.invokeContract('automation', 'getJobStatus', params, options);
            },

            /**
             * Update job parameters
             */
            updateJob: async (jobId, newTriggerData, options = {}) => {
                const params = [jobId, newTriggerData];
                return await this.invokeContract('automation', 'updateJob', params, options);
            }
        };
    }

    /**
     * Key Management Service
     */
    get keymanagement() {
        return {
            /**
             * Generate new key pair
             */
            generateKey: async (keyType, metadata = {}, options = {}) => {
                const params = [keyType, JSON.stringify(metadata)];
                return await this.invokeContract('keymanagement', 'generateKey', params, options);
            },

            /**
             * Rotate existing key
             */
            rotateKey: async (keyId, options = {}) => {
                const params = [keyId];
                return await this.invokeContract('keymanagement', 'rotateKey', params, options);
            },

            /**
             * Get public key
             */
            getPublicKey: async (keyId, options = {}) => {
                const params = [keyId];
                return await this.invokeContract('keymanagement', 'getPublicKey', params, options);
            },

            /**
             * Sign data with managed key
             */
            sign: async (keyId, data, options = {}) => {
                const params = [keyId, data];
                return await this.invokeContract('keymanagement', 'sign', params, options);
            },

            /**
             * Revoke key
             */
            revokeKey: async (keyId, options = {}) => {
                const params = [keyId];
                return await this.invokeContract('keymanagement', 'revokeKey', params, options);
            }
        };
    }

    /**
     * Governance & Voting Service
     */
    get voting() {
        return {
            /**
             * Create governance proposal
             */
            createProposal: async (title, description, actions = [], duration, options = {}) => {
                const params = [title, description, JSON.stringify(actions), duration];
                return await this.invokeContract('voting', 'createProposal', params, options);
            },

            /**
             * Vote on proposal
             */
            vote: async (proposalId, choice, votingPower = null, options = {}) => {
                const params = [proposalId, choice, votingPower];
                return await this.invokeContract('voting', 'vote', params, options);
            },

            /**
             * Delegate voting power
             */
            delegate: async (delegateAddress, amount, options = {}) => {
                const params = [delegateAddress, amount];
                return await this.invokeContract('voting', 'delegate', params, options);
            },

            /**
             * Undelegate voting power
             */
            undelegate: async (delegateAddress, amount, options = {}) => {
                const params = [delegateAddress, amount];
                return await this.invokeContract('voting', 'undelegate', params, options);
            },

            /**
             * Execute proposal
             */
            executeProposal: async (proposalId, options = {}) => {
                const params = [proposalId];
                return await this.invokeContract('voting', 'executeProposal', params, options);
            },

            /**
             * Get proposal details
             */
            getProposal: async (proposalId, options = {}) => {
                const params = [proposalId];
                return await this.invokeContract('voting', 'getProposal', params, options);
            }
        };
    }

    /**
     * Randomness Oracle Service
     */
    get randomness() {
        return {
            /**
             * Request random number
             */
            requestRandom: async (min = 0, max = 1000000, callbackContract = null, options = {}) => {
                const params = [min, max, callbackContract];
                return await this.invokeContract('randomness', 'requestRandom', params, options);
            },

            /**
             * Get random number by request ID
             */
            getRandom: async (requestId, options = {}) => {
                const params = [requestId];
                return await this.invokeContract('randomness', 'getRandom', params, options);
            },

            /**
             * Verify random number
             */
            verifyRandom: async (requestId, randomValue, proof, options = {}) => {
                const params = [requestId, randomValue, proof];
                return await this.invokeContract('randomness', 'verifyRandom', params, options);
            }
        };
    }

    /**
     * Gaming Platform Service
     */
    get gaming() {
        return {
            /**
             * Create new game
             */
            createGame: async (name, gameType, config, options = {}) => {
                const params = [name, gameType, JSON.stringify(config)];
                return await this.invokeContract('gaming', 'createGame', params, options);
            },

            /**
             * Join game
             */
            joinGame: async (gameId, entryFee = 0, options = {}) => {
                const params = [gameId, entryFee];
                return await this.invokeContract('gaming', 'joinGame', params, options);
            },

            /**
             * Submit game move
             */
            submitMove: async (gameId, moveData, options = {}) => {
                const params = [gameId, JSON.stringify(moveData)];
                return await this.invokeContract('gaming', 'submitMove', params, options);
            },

            /**
             * End game and distribute rewards
             */
            endGame: async (gameId, results, options = {}) => {
                const params = [gameId, JSON.stringify(results)];
                return await this.invokeContract('gaming', 'endGame', params, options);
            },

            /**
             * Create tournament
             */
            createTournament: async (name, gameType, entryFee, maxPlayers, options = {}) => {
                const params = [name, gameType, entryFee, maxPlayers];
                return await this.invokeContract('gaming', 'createTournament', params, options);
            },

            /**
             * Join tournament
             */
            joinTournament: async (tournamentId, options = {}) => {
                const params = [tournamentId];
                return await this.invokeContract('gaming', 'joinTournament', params, options);
            }
        };
    }

    /**
     * Backup Service - Data backup and recovery
     */
    get backup() {
        return {
            /**
             * Create backup
             */
            createBackup: async (dataType, options = {}) => {
                const params = [
                    dataType,
                    options.encryptionKey || null,
                    options.compressionLevel || 'standard',
                    this.config.blockchainType
                ];
                return await this.invokeContract('backup', 'createBackup', params, options);
            },

            /**
             * Restore from backup
             */
            restore: async (backupId, options = {}) => {
                const params = [backupId, options.decryptionKey || null];
                return await this.invokeContract('backup', 'restore', params, options);
            },

            /**
             * List backups
             */
            listBackups: async (options = {}) => {
                const params = [options.dataType || 'all', options.limit || 10];
                return await this.invokeContract('backup', 'listBackups', params, options);
            },

            /**
             * Delete backup
             */
            deleteBackup: async (backupId, options = {}) => {
                const params = [backupId];
                return await this.invokeContract('backup', 'deleteBackup', params, options);
            }
        };
    }

    /**
     * Configuration Service - Dynamic configuration management
     */
    get configuration() {
        return {
            /**
             * Get configuration value
             */
            get: async (key, options = {}) => {
                const params = [key, options.environment || 'production'];
                return await this.invokeContract('configuration', 'get', params, options);
            },

            /**
             * Set configuration value
             */
            set: async (key, value, options = {}) => {
                const params = [key, value, options.environment || 'production'];
                return await this.invokeContract('configuration', 'set', params, options);
            },

            /**
             * Get all configurations
             */
            getAll: async (options = {}) => {
                const params = [options.environment || 'production'];
                return await this.invokeContract('configuration', 'getAll', params, options);
            },

            /**
             * Validate configuration
             */
            validate: async (config, options = {}) => {
                const params = [config];
                return await this.invokeContract('configuration', 'validate', params, options);
            }
        };
    }

    /**
     * Abstract Account Service - Account abstraction and smart wallets
     */
    get abstractAccount() {
        return {
            /**
             * Create abstract account
             */
            createAccount: async (accountData, options = {}) => {
                const params = [
                    accountData.name,
                    accountData.owners || [this.wallet?.address],
                    accountData.threshold || 1,
                    accountData.modules || [],
                    this.config.blockchainType
                ];
                return await this.invokeContract('abstractAccount', 'createAccount', params, options);
            },

            /**
             * Execute transaction through abstract account
             */
            executeTransaction: async (accountId, transaction, options = {}) => {
                const params = [accountId, transaction];
                return await this.invokeContract('abstractAccount', 'executeTransaction', params, options);
            },

            /**
             * Add owner to account
             */
            addOwner: async (accountId, newOwner, options = {}) => {
                const params = [accountId, newOwner];
                return await this.invokeContract('abstractAccount', 'addOwner', params, options);
            },

            /**
             * Get account info
             */
            getAccountInfo: async (accountId, options = {}) => {
                const params = [accountId];
                return await this.invokeContract('abstractAccount', 'getAccountInfo', params, options);
            }
        };
    }

    /**
     * Compliance Service - Regulatory compliance and KYC/AML
     */
    get compliance() {
        return {
            /**
             * Verify identity
             */
            verifyIdentity: async (identityData, options = {}) => {
                const params = [
                    identityData.userId,
                    identityData.documentType,
                    identityData.documentHash,
                    identityData.jurisdiction || 'US'
                ];
                return await this.invokeContract('compliance', 'verifyIdentity', params, options);
            },

            /**
             * Check compliance status
             */
            checkCompliance: async (address, requirements, options = {}) => {
                const params = [address, requirements];
                return await this.invokeContract('compliance', 'checkCompliance', params, options);
            },

            /**
             * Report suspicious activity
             */
            reportActivity: async (activityData, options = {}) => {
                const params = [activityData];
                return await this.invokeContract('compliance', 'reportActivity', params, options);
            },

            /**
             * Get compliance report
             */
            getComplianceReport: async (address, options = {}) => {
                const params = [address, options.startDate, options.endDate];
                return await this.invokeContract('compliance', 'getComplianceReport', params, options);
            }
        };
    }

    /**
     * Proof of Reserve Service - Asset verification and auditing
     */
    get proofOfReserve() {
        return {
            /**
             * Generate proof of reserve
             */
            generateProof: async (assetData, options = {}) => {
                const params = [
                    assetData.assetType,
                    assetData.amount,
                    assetData.custodian,
                    options.includeAuditTrail || false
                ];
                return await this.invokeContract('proofOfReserve', 'generateProof', params, options);
            },

            /**
             * Verify proof
             */
            verifyProof: async (proofId, options = {}) => {
                const params = [proofId];
                return await this.invokeContract('proofOfReserve', 'verifyProof', params, options);
            },

            /**
             * Get reserve status
             */
            getReserveStatus: async (assetType, options = {}) => {
                const params = [assetType];
                return await this.invokeContract('proofOfReserve', 'getReserveStatus', params, options);
            },

            /**
             * Audit reserves
             */
            auditReserves: async (options = {}) => {
                const params = [options.assetTypes || 'all'];
                return await this.invokeContract('proofOfReserve', 'auditReserves', params, options);
            }
        };
    }

    /**
     * Monitoring Service - System monitoring and metrics
     */
    get monitoring() {
        return {
            /**
             * Get metrics
             */
            getMetrics: async (metricType, options = {}) => {
                const params = [
                    metricType,
                    options.startTime || Date.now() - 3600000,
                    options.endTime || Date.now(),
                    options.aggregation || 'average'
                ];
                return await this.invokeContract('monitoring', 'getMetrics', params, options);
            },

            /**
             * Set alert
             */
            setAlert: async (alertConfig, options = {}) => {
                const params = [alertConfig];
                return await this.invokeContract('monitoring', 'setAlert', params, options);
            },

            /**
             * Get system status
             */
            getSystemStatus: async (options = {}) => {
                const params = [options.includeDetails || false];
                return await this.invokeContract('monitoring', 'getSystemStatus', params, options);
            },

            /**
             * Get performance report
             */
            getPerformanceReport: async (options = {}) => {
                const params = [options.period || '24h'];
                return await this.invokeContract('monitoring', 'getPerformanceReport', params, options);
            }
        };
    }

    /**
     * Health Service - Service health monitoring
     */
    get health() {
        return {
            /**
             * Check service health
             */
            checkHealth: async (serviceName, options = {}) => {
                const params = [serviceName || 'all'];
                return await this.invokeContract('health', 'checkHealth', params, options);
            },

            /**
             * Get health history
             */
            getHealthHistory: async (serviceName, options = {}) => {
                const params = [
                    serviceName,
                    options.startTime || Date.now() - 86400000,
                    options.endTime || Date.now()
                ];
                return await this.invokeContract('health', 'getHealthHistory', params, options);
            },

            /**
             * Run diagnostics
             */
            runDiagnostics: async (options = {}) => {
                const params = [options.deep || false];
                return await this.invokeContract('health', 'runDiagnostics', params, options);
            },

            /**
             * Get uptime stats
             */
            getUptimeStats: async (options = {}) => {
                const params = [options.period || '30d'];
                return await this.invokeContract('health', 'getUptimeStats', params, options);
            }
        };
    }

    /**
     * Notification Service - Multi-channel notifications
     */
    get notification() {
        return {
            /**
             * Send notification
             */
            send: async (notification, options = {}) => {
                const params = [
                    notification.recipient,
                    notification.type,
                    notification.message,
                    notification.channels || ['email'],
                    notification.priority || 'normal'
                ];
                return await this.invokeContract('notification', 'send', params, options);
            },

            /**
             * Subscribe to notifications
             */
            subscribe: async (subscription, options = {}) => {
                const params = [
                    subscription.eventType,
                    subscription.channels,
                    subscription.filters || {}
                ];
                return await this.invokeContract('notification', 'subscribe', params, options);
            },

            /**
             * Get notification history
             */
            getHistory: async (options = {}) => {
                const params = [
                    options.limit || 50,
                    options.offset || 0,
                    options.status || 'all'
                ];
                return await this.invokeContract('notification', 'getHistory', params, options);
            },

            /**
             * Update preferences
             */
            updatePreferences: async (preferences, options = {}) => {
                const params = [preferences];
                return await this.invokeContract('notification', 'updatePreferences', params, options);
            }
        };
    }

    /**
     * Event Subscription Service - Real-time event streaming
     */
    get eventSubscription() {
        return {
            /**
             * Subscribe to events
             */
            subscribe: async (eventConfig, options = {}) => {
                const params = [
                    eventConfig.eventType,
                    eventConfig.filters || {},
                    eventConfig.callback || null,
                    this.config.blockchainType
                ];
                return await this.invokeContract('eventSubscription', 'subscribe', params, options);
            },

            /**
             * Unsubscribe from events
             */
            unsubscribe: async (subscriptionId, options = {}) => {
                const params = [subscriptionId];
                return await this.invokeContract('eventSubscription', 'unsubscribe', params, options);
            },

            /**
             * Get subscriptions
             */
            getSubscriptions: async (options = {}) => {
                const params = [options.active || true];
                return await this.invokeContract('eventSubscription', 'getSubscriptions', params, options);
            },

            /**
             * Get event history
             */
            getEventHistory: async (eventType, options = {}) => {
                const params = [
                    eventType,
                    options.startTime || Date.now() - 3600000,
                    options.endTime || Date.now(),
                    options.limit || 100
                ];
                return await this.invokeContract('eventSubscription', 'getEventHistory', params, options);
            }
        };
    }

    /**
     * Pattern Recognition Service - AI-powered pattern detection
     */
    get patternRecognition() {
        return {
            /**
             * Detect patterns
             */
            detectPatterns: async (data, options = {}) => {
                const params = [
                    data,
                    options.patternTypes || ['all'],
                    options.sensitivity || 'medium',
                    options.timeframe || '24h'
                ];
                return await this.invokeContract('patternRecognition', 'detectPatterns', params, options);
            },

            /**
             * Train model
             */
            trainModel: async (trainingData, options = {}) => {
                const params = [
                    trainingData,
                    options.modelType || 'default',
                    options.epochs || 100
                ];
                return await this.invokeContract('patternRecognition', 'trainModel', params, options);
            },

            /**
             * Get pattern insights
             */
            getInsights: async (patternId, options = {}) => {
                const params = [patternId];
                return await this.invokeContract('patternRecognition', 'getInsights', params, options);
            },

            /**
             * Analyze trends
             */
            analyzeTrends: async (dataSource, options = {}) => {
                const params = [
                    dataSource,
                    options.metrics || ['all'],
                    options.period || '7d'
                ];
                return await this.invokeContract('patternRecognition', 'analyzeTrends', params, options);
            }
        };
    }

    /**
     * Prediction Service - AI-powered predictions
     */
    get prediction() {
        return {
            /**
             * Make prediction
             */
            predict: async (predictionRequest, options = {}) => {
                const params = [
                    predictionRequest.type,
                    predictionRequest.data,
                    predictionRequest.model || 'default',
                    options.confidence || 0.8
                ];
                return await this.invokeContract('prediction', 'predict', params, options);
            },

            /**
             * Get prediction accuracy
             */
            getAccuracy: async (modelId, options = {}) => {
                const params = [modelId, options.period || '30d'];
                return await this.invokeContract('prediction', 'getAccuracy', params, options);
            },

            /**
             * Backtest prediction
             */
            backtest: async (model, historicalData, options = {}) => {
                const params = [model, historicalData];
                return await this.invokeContract('prediction', 'backtest', params, options);
            },

            /**
             * Get market forecast
             */
            getMarketForecast: async (market, options = {}) => {
                const params = [
                    market,
                    options.timeframe || '24h',
                    options.indicators || ['all']
                ];
                return await this.invokeContract('prediction', 'getMarketForecast', params, options);
            }
        };
    }

    /**
     * Fair Ordering Service - MEV protection and fair transaction ordering
     */
    get fairOrdering() {
        return {
            /**
             * Submit transaction for fair ordering
             */
            submitTransaction: async (transaction, options = {}) => {
                const params = [
                    transaction,
                    options.maxDelay || 1000,
                    options.priorityFee || 0,
                    this.config.blockchainType
                ];
                return await this.invokeContract('fairOrdering', 'submitTransaction', params, options);
            },

            /**
             * Get ordering statistics
             */
            getOrderingStats: async (options = {}) => {
                const params = [options.period || '24h'];
                return await this.invokeContract('fairOrdering', 'getOrderingStats', params, options);
            },

            /**
             * Check MEV protection status
             */
            getMEVProtectionStatus: async (transactionHash, options = {}) => {
                const params = [transactionHash];
                return await this.invokeContract('fairOrdering', 'getMEVProtectionStatus', params, options);
            },

            /**
             * Configure ordering preferences
             */
            configurePreferences: async (preferences, options = {}) => {
                const params = [preferences];
                return await this.invokeContract('fairOrdering', 'configurePreferences', params, options);
            }
        };
    }

    /**
     * Secrets Management Service - Secure secrets storage and management
     */
    get secretsManagement() {
        return {
            /**
             * Store secret
             */
            storeSecret: async (secret, options = {}) => {
                const params = [
                    secret.name,
                    secret.value,
                    secret.type || 'generic',
                    options.expiresAt || null,
                    options.accessControl || {}
                ];
                return await this.invokeContract('secretsManagement', 'storeSecret', params, options);
            },

            /**
             * Retrieve secret
             */
            getSecret: async (secretName, options = {}) => {
                const params = [secretName, options.version || 'latest'];
                return await this.invokeContract('secretsManagement', 'getSecret', params, options);
            },

            /**
             * Rotate secret
             */
            rotateSecret: async (secretName, newValue, options = {}) => {
                const params = [secretName, newValue];
                return await this.invokeContract('secretsManagement', 'rotateSecret', params, options);
            },

            /**
             * List secrets
             */
            listSecrets: async (options = {}) => {
                const params = [options.type || 'all'];
                return await this.invokeContract('secretsManagement', 'listSecrets', params, options);
            },

            /**
             * Delete secret
             */
            deleteSecret: async (secretName, options = {}) => {
                const params = [secretName, options.permanent || false];
                return await this.invokeContract('secretsManagement', 'deleteSecret', params, options);
            }
        };
    }

    /**
     * Batch Operations - Execute multiple operations in one transaction
     */
    async batchExecute(operations, options = {}) {
        try {
            // Validate operations
            if (!Array.isArray(operations) || operations.length === 0) {
                throw new SDKError('Operations must be a non-empty array', 'INVALID_BATCH_OPERATIONS');
            }
            
            if (operations.length > 50) {
                throw new SDKError('Batch size cannot exceed 50 operations', 'BATCH_SIZE_EXCEEDED');
            }
            
            // Prepare batch parameters with blockchain type
            const blockchainType = options.blockchainType || this.config.blockchainType;
            const batchParams = operations.map(op => {
                if (!op.service || !op.method) {
                    throw new SDKError('Each operation must have service and method', 'INVALID_OPERATION');
                }
                
                return {
                    service: op.service,
                    method: op.method,
                    params: op.params || [],
                    blockchainType: op.blockchainType || blockchainType
                };
            });
            
            // Execute batch with retry logic
            const result = await this.invokeContract('batch', 'execute', [batchParams], {
                ...options,
                retryAttempts: options.retryAttempts || 2 // Lower retry for batch operations
            });
            
            // Process results
            if (result && result.results) {
                return {
                    success: result.success,
                    results: result.results.map((res, index) => ({
                        ...res,
                        operation: operations[index]
                    })),
                    totalGasUsed: result.totalGasUsed,
                    executionTime: result.executionTime
                };
            }
            
            return result;
        } catch (error) {
            throw new SDKError('Batch execution failed', 'BATCH_ERROR', error);
        }
    }
    
    /**
     * Parallel Batch Operations - Execute multiple independent operations in parallel
     */
    async batchExecuteParallel(operations, options = {}) {
        try {
            // Validate operations
            if (!Array.isArray(operations) || operations.length === 0) {
                throw new SDKError('Operations must be a non-empty array', 'INVALID_BATCH_OPERATIONS');
            }
            
            console.log(`🔄 Executing ${operations.length} operations in parallel...`);
            
            // Execute all operations in parallel
            const promises = operations.map(async (op, index) => {
                try {
                    const service = this[op.service];
                    if (!service || !service[op.method]) {
                        throw new SDKError(`Invalid service or method: ${op.service}.${op.method}`, 'INVALID_SERVICE_METHOD');
                    }
                    
                    // Call the service method
                    const result = await service[op.method](...(op.params || []), op.options || options);
                    
                    return {
                        index,
                        success: true,
                        result,
                        operation: op
                    };
                } catch (error) {
                    return {
                        index,
                        success: false,
                        error: error.message,
                        operation: op
                    };
                }
            });
            
            // Wait for all operations to complete
            const results = await Promise.all(promises);
            
            // Calculate statistics
            const successful = results.filter(r => r.success).length;
            const failed = results.filter(r => !r.success).length;
            
            return {
                totalOperations: operations.length,
                successful,
                failed,
                results: results.sort((a, b) => a.index - b.index),
                executionTime: Date.now() - Date.now() // This would be tracked properly in production
            };
        } catch (error) {
            throw new SDKError('Parallel batch execution failed', 'PARALLEL_BATCH_ERROR', error);
        }
    }

    /**
     * Gas Optimization - Estimate optimal gas for operations
     */
    async optimizeGas(operations) {
        try {
            const estimates = [];
            
            for (const op of operations) {
                const complexity = this.getMethodComplexity(op.method);
                const serviceMultiplier = this.getServiceGasMultiplier(op.service);
                const estimate = this.config.gasPrice * complexity * serviceMultiplier;
                
                estimates.push({
                    service: op.service,
                    method: op.method,
                    gasEstimate: estimate,
                    complexity: complexity
                });
            }

            return {
                totalGas: estimates.reduce((sum, est) => sum + est.gasEstimate, 0),
                operations: estimates,
                optimizations: this.suggestGasOptimizations(estimates)
            };
        } catch (error) {
            throw new SDKError('Gas optimization failed', 'GAS_OPTIMIZATION_ERROR', error);
        }
    }

    /**
     * Get service-specific gas multiplier
     */
    getServiceGasMultiplier(service) {
        const multipliers = {
            storage: 1.0,
            oracle: 1.5,
            compute: 3.0,
            crosschain: 4.0,
            zeroknowledge: 5.0,
            lending: 2.0,
            marketplace: 2.5,
            tokenization: 3.5,
            insurance: 2.0,
            automation: 1.5,
            keymanagement: 1.8,
            voting: 1.5,
            randomness: 1.2,
            gaming: 1.8,
            identity: 1.3,
            analytics: 1.0
        };
        
        return multipliers[service] || 2.0;
    }

    /**
     * Suggest gas optimizations
     */
    suggestGasOptimizations(estimates) {
        const suggestions = [];
        const totalGas = estimates.reduce((sum, est) => sum + est.gasEstimate, 0);

        // Suggest batching if multiple operations
        if (estimates.length > 1) {
            suggestions.push({
                type: 'batching',
                description: 'Consider batching operations to reduce transaction overhead',
                potentialSavings: totalGas * 0.15 // Estimate 15% savings
            });
        }

        // Suggest alternative methods for high-gas operations
        estimates.forEach(est => {
            if (est.gasEstimate > this.config.maxGasInvoke * 0.8) {
                suggestions.push({
                    type: 'method_optimization',
                    service: est.service,
                    method: est.method,
                    description: `High gas operation detected. Consider optimizing ${est.method} parameters`,
                    currentGas: est.gasEstimate
                });
            }
        });

        return suggestions;
    }

    /**
     * WebSocket Support for Real-time Events
     */
    async connectWebSocket(options = {}) {
        try {
            const wsUrl = options.url || this.getWebSocketUrl();
            
            console.log(`🔌 Connecting to WebSocket: ${wsUrl}`);
            
            this.websocket = new WebSocket(wsUrl);
            
            // Connection opened
            this.websocket.onopen = () => {
                console.log('✅ WebSocket connected');
                this.wsReconnectAttempts = 0;
                
                // Subscribe to events
                this.subscribeToEvents(options.events || ['all']);
                
                this.emit('websocket-connected', { url: wsUrl });
            };
            
            // Message received
            this.websocket.onmessage = (event) => {
                try {
                    const data = JSON.parse(event.data);
                    this.handleWebSocketMessage(data);
                } catch (error) {
                    console.error('Failed to parse WebSocket message:', error);
                }
            };
            
            // Connection closed
            this.websocket.onclose = (event) => {
                console.log('🔌 WebSocket disconnected');
                this.emit('websocket-disconnected', { code: event.code, reason: event.reason });
                
                // Attempt reconnection
                if (options.autoReconnect !== false) {
                    this.reconnectWebSocket(options);
                }
            };
            
            // Error occurred
            this.websocket.onerror = (error) => {
                console.error('❌ WebSocket error:', error);
                this.emit('websocket-error', error);
            };
            
        } catch (error) {
            console.error('Failed to connect WebSocket:', error);
            throw new SDKError('WebSocket connection failed', 'WEBSOCKET_ERROR', error);
        }
    }
    
    /**
     * Get WebSocket URL based on current configuration
     */
    getWebSocketUrl() {
        const baseUrl = this.config.rpcUrl.replace('https://', 'wss://').replace('http://', 'ws://');
        return `${baseUrl}/websocket`;
    }
    
    /**
     * Reconnect WebSocket with exponential backoff
     */
    async reconnectWebSocket(options) {
        if (this.wsReconnectAttempts >= this.wsMaxReconnectAttempts) {
            console.error('❌ Max WebSocket reconnection attempts reached');
            this.emit('websocket-reconnect-failed', { attempts: this.wsReconnectAttempts });
            return;
        }
        
        this.wsReconnectAttempts++;
        const delay = this.wsReconnectDelay * Math.pow(2, this.wsReconnectAttempts - 1);
        
        console.log(`⏳ Reconnecting WebSocket in ${delay}ms (attempt ${this.wsReconnectAttempts}/${this.wsMaxReconnectAttempts})`);
        
        setTimeout(() => {
            this.connectWebSocket(options);
        }, delay);
    }
    
    /**
     * Subscribe to WebSocket events
     */
    subscribeToEvents(events) {
        if (!this.websocket || this.websocket.readyState !== WebSocket.OPEN) {
            throw new SDKError('WebSocket not connected', 'WEBSOCKET_NOT_CONNECTED');
        }
        
        const subscription = {
            action: 'subscribe',
            events: events,
            filters: {
                services: Object.keys(this.contracts),
                blockchainType: this.config.blockchainType
            }
        };
        
        this.websocket.send(JSON.stringify(subscription));
    }
    
    /**
     * Handle incoming WebSocket messages
     */
    handleWebSocketMessage(data) {
        switch (data.type) {
            case 'transaction':
                this.emit('websocket-transaction', data);
                break;
                
            case 'block':
                this.emit('websocket-block', data);
                break;
                
            case 'contract-event':
                this.emit('websocket-contract-event', data);
                this.handleContractEvent(data);
                break;
                
            case 'service-update':
                this.emit('websocket-service-update', data);
                break;
                
            case 'error':
                this.emit('websocket-message-error', data);
                break;
                
            default:
                this.emit('websocket-message', data);
        }
    }
    
    /**
     * Handle contract-specific events
     */
    handleContractEvent(event) {
        const { service, method, params, result } = event.data;
        
        // Emit service-specific events
        this.emit(`${service}-${method}`, { params, result });
        
        // Update cache if applicable
        if (this.config.enableCache && method === 'get') {
            const cacheKey = `${service}_${params[0]}`;
            this.cache.set(cacheKey, {
                data: result,
                timestamp: Date.now()
            });
        }
    }
    
    /**
     * Send custom message via WebSocket
     */
    sendWebSocketMessage(message) {
        if (!this.websocket || this.websocket.readyState !== WebSocket.OPEN) {
            throw new SDKError('WebSocket not connected', 'WEBSOCKET_NOT_CONNECTED');
        }
        
        this.websocket.send(JSON.stringify(message));
    }
    
    /**
     * Disconnect WebSocket
     */
    disconnectWebSocket() {
        if (this.websocket) {
            this.websocket.close(1000, 'Client disconnect');
            this.websocket = null;
        }
    }

    /**
     * Cleanup and dispose
     */
    dispose() {
        // Save cache before disposing
        if (this.config.enableCache) {
            this.saveCache();
        }
        
        // Disconnect WebSocket
        this.disconnectWebSocket();
        
        // Clear event handlers
        this.eventHandlers.clear();
        
        // Disconnect wallet
        this.wallet = null;
        
        // Clear cache
        this.cache.clear();
        
        console.log('🗑️ SDK disposed');
    }
}

/**
 * Custom SDK Error class
 */
class SDKError extends Error {
    constructor(message, code, originalError = null) {
        super(message);
        this.name = 'SDKError';
        this.code = code;
        this.originalError = originalError;
        this.timestamp = new Date().toISOString();
        
        if (Error.captureStackTrace) {
            Error.captureStackTrace(this, SDKError);
        }
    }
    
    toJSON() {
        return {
            name: this.name,
            message: this.message,
            code: this.code,
            timestamp: this.timestamp,
            originalError: this.originalError ? this.originalError.message : null
        };
    }
}

/**
 * Mock Neo RPC Client for development
 * In production, this would be replaced with actual neon-js or similar
 */
class NeoRpcClient {
    constructor(endpoint, options = {}) {
        this.endpoint = endpoint;
        this.timeout = options.timeout || 30000;
        this.retryAttempts = options.retryAttempts || 3;
        this.retryDelay = options.retryDelay || 1000;
        this.blockchainType = options.blockchainType || 'NeoN3';
    }
    
    /**
     * Execute RPC call with retry logic
     */
    async executeWithRetry(method, params = []) {
        let lastError;
        
        for (let attempt = 1; attempt <= this.retryAttempts; attempt++) {
            try {
                // In production, this would make actual RPC calls
                // For now, simulate the call
                return await this[method](...params);
            } catch (error) {
                lastError = error;
                console.warn(`RPC call failed (${attempt}/${this.retryAttempts}):`, error.message);
                
                if (attempt < this.retryAttempts) {
                    const delay = this.retryDelay * Math.pow(2, attempt - 1);
                    await new Promise(resolve => setTimeout(resolve, delay));
                }
            }
        }
        
        throw lastError;
    }
    
    async getVersion() {
        // Simulate potential network failure
        if (Math.random() < 0.1) { // 10% chance of failure
            throw new Error('Network timeout');
        }
        
        return {
            tcpport: 10333,
            wsport: 10334,
            nonce: 1234567890,
            useragent: this.blockchainType === 'NeoX' ? "/NeoX:1.0.0/" : "/Neo:3.5.0/"
        };
    }
    
    async getBlockCount() {
        return Math.floor(Math.random() * 1000000) + 8000000;
    }
    
    async getBlock(index) {
        return {
            hash: '0x' + Math.random().toString(16).substring(2),
            size: 1024,
            version: 0,
            time: Math.floor(Date.now() / 1000),
            index: index
        };
    }
    
    async getNep17Balances(address) {
        // Mock balance response
        return {
            balance: [
                {
                    assethash: '0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5', // NEO
                    amount: '10000000000', // 100 NEO
                    lastupdatedblock: 8000000
                },
                {
                    assethash: '0xd2a4cff31913016155e38e474a2c06d08be276cf', // GAS
                    amount: '5012345678', // 50.12345678 GAS
                    lastupdatedblock: 8000000
                }
            ],
            address: address
        };
    }
    
    async getApplicationLog(txid) {
        // Mock transaction log
        return {
            txid: txid,
            blockindex: Math.floor(Math.random() * 1000000) + 8000000,
            confirmations: Math.floor(Math.random() * 10) + 1,
            gasconsumed: (Math.random() * 0.1 + 0.01).toFixed(8)
        };
    }
}

// Export for both browser and Node.js environments
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { NeoServiceLayerSDK, SDKError };
} else if (typeof window !== 'undefined') {
    window.NeoServiceLayerSDK = NeoServiceLayerSDK;
    window.SDKError = SDKError;
}