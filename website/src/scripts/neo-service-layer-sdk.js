/**
 * Neo Service Layer JavaScript SDK
 * Comprehensive SDK for interacting with Neo Service Layer smart contracts
 * 
 * @version 1.0.0
 * @author Neo Service Layer Team
 */

class NeoServiceLayerSDK {
    constructor(config = {}) {
        this.config = {
            network: config.network || 'testnet',
            rpcUrl: config.rpcUrl || 'https://testnet1.neo.coz.io:443',
            networkMagic: config.networkMagic || 894710606, // TestNet magic
            contracts: config.contracts || {},
            privateKey: config.privateKey || null,
            account: config.account || null,
            ...config
        };

        this.isInitialized = false;
        this.wallet = null;
        this.contracts = {};
        
        // Event handlers
        this.eventHandlers = {
            'wallet-connected': [],
            'wallet-disconnected': [],
            'transaction-sent': [],
            'transaction-confirmed': [],
            'error': []
        };

        // Initialize SDK
        this.init();
    }

    /**
     * Initialize the SDK
     */
    async init() {
        try {
            // Set up default contract addresses for testnet
            this.setupDefaultContracts();
            
            // Initialize Neo-related libraries if available
            if (typeof window !== 'undefined' && window.Neo) {
                await this.initializeNeoLibraries();
            }
            
            this.isInitialized = true;
            console.log('Neo Service Layer SDK initialized successfully');
        } catch (error) {
            console.error('SDK initialization failed:', error);
            this.emit('error', error);
        }
    }

    /**
     * Setup default contract addresses
     */
    setupDefaultContracts() {
        this.contracts = {
            // Core Service Contracts
            storage: this.config.contracts.storage || '0x1234567890abcdef1234567890abcdef12345678',
            oracle: this.config.contracts.oracle || '0x2345678901bcdef12345678901bcdef123456789',
            compute: this.config.contracts.compute || '0x3456789012cdef123456789012cdef12345678a',
            identity: this.config.contracts.identity || '0x456789013def123456789013def123456789ab',
            analytics: this.config.contracts.analytics || '0x56789014ef123456789014ef123456789abc',
            
            // Cross-Chain Services
            crosschain: this.config.contracts.crosschain || '0x6789015f123456789015f123456789abcd',
            
            // DeFi Services
            lending: this.config.contracts.lending || '0x789016f123456789016f123456789abcde',
            marketplace: this.config.contracts.marketplace || '0x89017f123456789017f123456789abcdef',
            tokenization: this.config.contracts.tokenization || '0x9018f123456789018f123456789abcdef1',
            insurance: this.config.contracts.insurance || '0xa019f123456789019f123456789abcdef12',
            
            // Advanced Services
            zeroknowledge: this.config.contracts.zeroknowledge || '0xb01af123456789a1af123456789abcdef123',
            automation: this.config.contracts.automation || '0xc01bf123456789b1bf123456789abcdef1234',
            keymanagement: this.config.contracts.keymanagement || '0xd01cf123456789c1cf123456789abcdef12345',
            voting: this.config.contracts.voting || '0xe01df123456789d1df123456789abcdef123456',
            randomness: this.config.contracts.randomness || '0xf01ef123456789e1ef123456789abcdef1234567',
            gaming: this.config.contracts.gaming || '0x101ff123456789f1ff123456789abcdef12345678'
        };
    }

    /**
     * Initialize Neo libraries
     */
    async initializeNeoLibraries() {
        // This would integrate with actual Neo libraries like neon-js, neo3-boa, etc.
        // For now, we'll set up the structure for real integration
        
        if (window.neonJs) {
            this.neon = window.neonJs;
            this.api = new this.neon.api.neoCli.instance(this.config.rpcUrl);
        }
    }

    /**
     * Connect wallet (NeoLine, O3, etc.)
     */
    async connectWallet(walletType = 'neoline') {
        try {
            let walletProvider;
            
            switch (walletType.toLowerCase()) {
                case 'neoline':
                    if (typeof NEOLine !== 'undefined') {
                        walletProvider = new NEOLine.Init();
                        this.wallet = await walletProvider.getAccount();
                    } else {
                        throw new Error('NeoLine wallet not found. Please install NeoLine extension.');
                    }
                    break;
                    
                case 'o3':
                    if (typeof o3dapi !== 'undefined') {
                        await o3dapi.initPlugins([o3dapi.NEO]);
                        walletProvider = o3dapi.NEO;
                        this.wallet = await walletProvider.getAccount();
                    } else {
                        throw new Error('O3 wallet not found. Please install O3 wallet.');
                    }
                    break;
                    
                default:
                    // Fallback to demo wallet for testing
                    this.wallet = this.createDemoWallet();
            }

            if (this.wallet) {
                this.emit('wallet-connected', this.wallet);
                return this.wallet;
            }
            
            throw new Error('Failed to connect wallet');
        } catch (error) {
            console.error('Wallet connection failed:', error);
            this.emit('error', error);
            throw error;
        }
    }

    /**
     * Create demo wallet for testing
     */
    createDemoWallet() {
        return {
            address: 'NX8GreRFGFK5wpGMWetpX93HmtrezGogzk',
            label: 'Demo Wallet',
            isConnected: true,
            balance: {
                NEO: '100',
                GAS: '1000.12345678'
            }
        };
    }

    /**
     * Disconnect wallet
     */
    async disconnectWallet() {
        this.wallet = null;
        this.emit('wallet-disconnected');
    }

    /**
     * Get wallet balance
     */
    async getBalance(address = null) {
        const targetAddress = address || (this.wallet ? this.wallet.address : null);
        
        if (!targetAddress) {
            throw new Error('No wallet address provided');
        }

        try {
            // In a real implementation, this would query the Neo blockchain
            // For now, return demo data
            return {
                NEO: '100.00000000',
                GAS: '1000.12345678',
                address: targetAddress
            };
        } catch (error) {
            console.error('Failed to get balance:', error);
            throw error;
        }
    }

    /**
     * Storage Service Methods
     */
    storage = {
        /**
         * Store data on blockchain
         */
        store: async (key, value, options = {}) => {
            return this.invokeContract('storage', 'store', [
                key,
                value,
                options.encrypted || false,
                options.accessLevel || 'private'
            ]);
        },

        /**
         * Retrieve data from blockchain
         */
        get: async (key) => {
            return this.invokeContract('storage', 'get', [key]);
        },

        /**
         * Delete data from blockchain
         */
        delete: async (key) => {
            return this.invokeContract('storage', 'delete', [key]);
        },

        /**
         * List all keys for current user
         */
        listKeys: async () => {
            return this.invokeContract('storage', 'listKeys', []);
        }
    };

    /**
     * Oracle Service Methods
     */
    oracle = {
        /**
         * Request external data
         */
        requestData: async (url, path = null, callback = null) => {
            return this.invokeContract('oracle', 'requestData', [
                url,
                path,
                callback
            ]);
        },

        /**
         * Get oracle data
         */
        getData: async (requestId) => {
            return this.invokeContract('oracle', 'getData', [requestId]);
        }
    };

    /**
     * Compute Service Methods
     */
    compute = {
        /**
         * Execute computation
         */
        execute: async (computationType, input, verify = true) => {
            return this.invokeContract('compute', 'execute', [
                computationType,
                input,
                verify
            ]);
        },

        /**
         * Get computation result
         */
        getResult: async (jobId) => {
            return this.invokeContract('compute', 'getResult', [jobId]);
        }
    };

    /**
     * Cross-Chain Service Methods
     */
    crosschain = {
        /**
         * Initiate cross-chain transfer
         */
        bridge: async (targetChain, asset, amount, recipient) => {
            return this.invokeContract('crosschain', 'initiateBridge', [
                targetChain,
                asset,
                this.toScriptHash(amount),
                recipient
            ]);
        },

        /**
         * Get bridge status
         */
        getBridgeStatus: async (bridgeId) => {
            return this.invokeContract('crosschain', 'getBridgeStatus', [bridgeId]);
        }
    };

    /**
     * DeFi Lending Service Methods
     */
    lending = {
        /**
         * Supply assets to lending pool
         */
        supply: async (asset, amount) => {
            return this.invokeContract('lending', 'supply', [
                asset,
                this.toScriptHash(amount)
            ]);
        },

        /**
         * Borrow assets from lending pool
         */
        borrow: async (asset, amount, collateral) => {
            return this.invokeContract('lending', 'borrow', [
                asset,
                this.toScriptHash(amount),
                collateral
            ]);
        },

        /**
         * Repay borrowed assets
         */
        repay: async (asset, amount) => {
            return this.invokeContract('lending', 'repay', [
                asset,
                this.toScriptHash(amount)
            ]);
        },

        /**
         * Get lending pool info
         */
        getPoolInfo: async (asset) => {
            return this.invokeContract('lending', 'getPoolInfo', [asset]);
        }
    };

    /**
     * NFT Marketplace Service Methods
     */
    marketplace = {
        /**
         * Mint NFT
         */
        mint: async (name, description, image, attributes = {}) => {
            return this.invokeContract('marketplace', 'mint', [
                name,
                description,
                image,
                JSON.stringify(attributes)
            ]);
        },

        /**
         * List NFT for sale
         */
        list: async (tokenId, price, duration = 30) => {
            return this.invokeContract('marketplace', 'list', [
                tokenId,
                this.toScriptHash(price),
                duration
            ]);
        },

        /**
         * Buy NFT
         */
        buy: async (tokenId, maxPrice) => {
            return this.invokeContract('marketplace', 'buy', [
                tokenId,
                this.toScriptHash(maxPrice)
            ]);
        },

        /**
         * Cancel listing
         */
        cancelListing: async (tokenId) => {
            return this.invokeContract('marketplace', 'cancelListing', [tokenId]);
        }
    };

    /**
     * Token Creation Service Methods
     */
    tokenization = {
        /**
         * Create new token
         */
        createToken: async (name, symbol, decimals, totalSupply, features = []) => {
            return this.invokeContract('tokenization', 'createToken', [
                name,
                symbol,
                decimals,
                this.toScriptHash(totalSupply),
                features.join(',')
            ]);
        },

        /**
         * Mint additional tokens (if mintable)
         */
        mint: async (tokenContract, amount, recipient) => {
            return this.invokeContract('tokenization', 'mint', [
                tokenContract,
                this.toScriptHash(amount),
                recipient
            ]);
        }
    };

    /**
     * Insurance Service Methods
     */
    insurance = {
        /**
         * Purchase insurance policy
         */
        purchasePolicy: async (type, coverage, duration, target) => {
            return this.invokeContract('insurance', 'purchasePolicy', [
                type,
                this.toScriptHash(coverage),
                duration,
                target
            ]);
        },

        /**
         * File insurance claim
         */
        fileClaim: async (policyId, evidence) => {
            return this.invokeContract('insurance', 'fileClaim', [
                policyId,
                evidence
            ]);
        }
    };

    /**
     * Zero Knowledge Service Methods
     */
    zeroknowledge = {
        /**
         * Generate ZK proof
         */
        generateProof: async (proofType, privateInput, publicParams, circuit = 'default') => {
            return this.invokeContract('zeroknowledge', 'generateProof', [
                proofType,
                privateInput,
                publicParams,
                circuit
            ]);
        },

        /**
         * Verify ZK proof
         */
        verifyProof: async (proof, publicInputs) => {
            return this.invokeContract('zeroknowledge', 'verifyProof', [
                proof,
                publicInputs
            ]);
        }
    };

    /**
     * Automation Service Methods
     */
    automation = {
        /**
         * Create automation job
         */
        createJob: async (triggerType, condition, contract, method, params) => {
            return this.invokeContract('automation', 'createJob', [
                triggerType,
                condition,
                contract,
                method,
                JSON.stringify(params)
            ]);
        },

        /**
         * Cancel automation job
         */
        cancelJob: async (jobId) => {
            return this.invokeContract('automation', 'cancelJob', [jobId]);
        }
    };

    /**
     * Key Management Service Methods
     */
    keymanagement = {
        /**
         * Generate key pair
         */
        generateKeyPair: async (keyType, security, policy) => {
            return this.invokeContract('keymanagement', 'generateKeyPair', [
                keyType,
                security,
                policy
            ]);
        },

        /**
         * Rotate keys
         */
        rotateKeys: async (keyId) => {
            return this.invokeContract('keymanagement', 'rotateKeys', [keyId]);
        }
    };

    /**
     * Voting/Governance Service Methods
     */
    voting = {
        /**
         * Create proposal
         */
        createProposal: async (title, description, actions, duration = 7) => {
            return this.invokeContract('voting', 'createProposal', [
                title,
                description,
                JSON.stringify(actions),
                duration
            ]);
        },

        /**
         * Vote on proposal
         */
        vote: async (proposalId, choice) => {
            return this.invokeContract('voting', 'vote', [
                proposalId,
                choice
            ]);
        },

        /**
         * Delegate voting power
         */
        delegate: async (delegate, amount) => {
            return this.invokeContract('voting', 'delegate', [
                delegate,
                this.toScriptHash(amount)
            ]);
        }
    };

    /**
     * Randomness Service Methods
     */
    randomness = {
        /**
         * Generate random number
         */
        generateRandom: async (type, params, callback = null, verify = true) => {
            return this.invokeContract('randomness', 'generateRandom', [
                type,
                params,
                callback,
                verify
            ]);
        },

        /**
         * Get random value
         */
        getRandom: async (requestId) => {
            return this.invokeContract('randomness', 'getRandom', [requestId]);
        }
    };

    /**
     * Gaming Service Methods
     */
    gaming = {
        /**
         * Create game
         */
        createGame: async (gameType, entryFee, gameData) => {
            return this.invokeContract('gaming', 'createGame', [
                gameType,
                this.toScriptHash(entryFee),
                JSON.stringify(gameData)
            ]);
        },

        /**
         * Join game
         */
        joinGame: async (gameId) => {
            return this.invokeContract('gaming', 'joinGame', [gameId]);
        },

        /**
         * Submit score
         */
        submitScore: async (gameId, score, proof) => {
            return this.invokeContract('gaming', 'submitScore', [
                gameId,
                score,
                proof
            ]);
        }
    };

    /**
     * Identity Management Service Methods
     */
    identity = {
        /**
         * Create DID
         */
        createDID: async (name, email, publicKey = null) => {
            return this.invokeContract('identity', 'createDID', [
                name,
                email,
                publicKey || ''
            ]);
        },

        /**
         * Verify identity
         */
        verifyIdentity: async (did, proof) => {
            return this.invokeContract('identity', 'verifyIdentity', [
                did,
                proof
            ]);
        },

        /**
         * Update attributes
         */
        updateAttributes: async (did, attributes) => {
            return this.invokeContract('identity', 'updateAttributes', [
                did,
                JSON.stringify(attributes)
            ]);
        }
    };

    /**
     * Analytics Service Methods
     */
    analytics = {
        /**
         * Track event
         */
        trackEvent: async (eventType, eventData, tags = []) => {
            return this.invokeContract('analytics', 'trackEvent', [
                eventType,
                JSON.stringify(eventData),
                tags.join(',')
            ]);
        },

        /**
         * Get analytics data
         */
        getAnalytics: async (timeframe, eventType = null) => {
            return this.invokeContract('analytics', 'getAnalytics', [
                timeframe,
                eventType || ''
            ]);
        }
    };

    /**
     * Core method to invoke smart contracts
     */
    async invokeContract(serviceName, method, params = []) {
        try {
            if (!this.wallet && !this.config.privateKey) {
                throw new Error('No wallet connected. Please connect a wallet first.');
            }

            const contractHash = this.contracts[serviceName];
            if (!contractHash) {
                throw new Error(`Contract not found for service: ${serviceName}`);
            }

            // Create transaction object
            const transaction = {
                txid: this.generateTxId(),
                contract: contractHash,
                method: method,
                params: params,
                sender: this.wallet ? this.wallet.address : null,
                timestamp: new Date().toISOString(),
                status: 'pending'
            };

            console.log(`Invoking ${serviceName}.${method}`, { transaction, params });

            // Emit transaction sent event
            this.emit('transaction-sent', transaction);

            // In a real implementation, this would:
            // 1. Build the Neo transaction
            // 2. Sign with wallet
            // 3. Broadcast to network
            // 4. Return transaction hash

            // For demo, simulate transaction
            const result = await this.simulateTransaction(transaction);

            return result;
        } catch (error) {
            console.error(`Contract invocation failed:`, error);
            this.emit('error', error);
            throw error;
        }
    }

    /**
     * Simulate transaction for demo purposes
     */
    async simulateTransaction(transaction) {
        // Simulate network delay
        await new Promise(resolve => setTimeout(resolve, 1000 + Math.random() * 2000));

        // Simulate success/failure
        if (Math.random() < 0.95) { // 95% success rate
            const result = {
                txid: transaction.txid,
                blockHeight: Math.floor(Math.random() * 1000000) + 8000000,
                gasConsumed: (Math.random() * 0.1 + 0.01).toFixed(8),
                result: this.generateMockResult(transaction.method),
                confirmations: 1
            };

            // Emit confirmation after a delay
            setTimeout(() => {
                this.emit('transaction-confirmed', { ...transaction, ...result });
            }, 3000);

            return result;
        } else {
            throw new Error('Transaction failed: Insufficient GAS or contract error');
        }
    }

    /**
     * Generate mock results based on method
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
     * Get transaction details
     */
    async getTransaction(txid) {
        // In real implementation, query Neo blockchain
        // For demo, return mock data
        return {
            txid: txid,
            blockHeight: Math.floor(Math.random() * 1000000) + 8000000,
            confirmations: Math.floor(Math.random() * 10) + 1,
            gasConsumed: (Math.random() * 0.1 + 0.01).toFixed(8),
            status: 'confirmed'
        };
    }

    /**
     * Utility method to convert numbers to script hash format
     */
    toScriptHash(value) {
        if (typeof value === 'string') {
            return value;
        }
        // Convert number to appropriate format for Neo smart contracts
        return Math.floor(value * 100000000).toString(); // Convert to 8 decimal places
    }

    /**
     * Generate transaction ID
     */
    generateTxId() {
        return '0x' + Array.from({length: 64}, () => Math.floor(Math.random() * 16).toString(16)).join('');
    }

    /**
     * Event system
     */
    on(event, handler) {
        if (!this.eventHandlers[event]) {
            this.eventHandlers[event] = [];
        }
        this.eventHandlers[event].push(handler);
    }

    off(event, handler) {
        if (this.eventHandlers[event]) {
            this.eventHandlers[event] = this.eventHandlers[event].filter(h => h !== handler);
        }
    }

    emit(event, data) {
        if (this.eventHandlers[event]) {
            this.eventHandlers[event].forEach(handler => handler(data));
        }
    }

    /**
     * Get SDK status
     */
    getStatus() {
        return {
            isInitialized: this.isInitialized,
            isWalletConnected: !!this.wallet,
            wallet: this.wallet,
            network: this.config.network,
            contracts: this.contracts
        };
    }
}

// Export for both browser and Node.js environments
if (typeof module !== 'undefined' && module.exports) {
    module.exports = NeoServiceLayerSDK;
} else if (typeof window !== 'undefined') {
    window.NeoServiceLayerSDK = NeoServiceLayerSDK;
}