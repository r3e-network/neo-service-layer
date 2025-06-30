/**
 * Neo Service Layer JavaScript SDK
 * Provides easy integration with Neo Service Layer contracts
 */

// Service contract addresses (configure for mainnet/testnet)
const NETWORK = 'testnet'; // Change to 'mainnet' for production
const RPC_URL = NETWORK === 'mainnet' 
    ? 'https://n3seed1.ngd.network:10332'
    : 'https://n3seed1.ngd.network:20332';

const SERVICE_CONTRACTS = {
    // Core Services
    ServiceRegistry: '0xa1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2',
    StorageContract: '0xb2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3',
    OracleContract: '0xc3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4',
    ComputeContract: '0xd4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5',
    AnalyticsContract: '0xe5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6',
    
    // Identity & Security
    IdentityManagementContract: '0xf6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1',
    KeyManagementContract: '0xa2b3c4d5e6f7a2b3c4d5e6f7a2b3c4d5e6f7a2b3',
    ComplianceContract: '0xb3c4d5e6f7a2b3c4d5e6f7a2b3c4d5e6f7a2b3c4',
    
    // DeFi Services
    CrossChainContract: '0xc4d5e6f7a2b3c4d5e6f7a2b3c4d5e6f7a2b3c4d5',
    PaymentProcessingContract: '0xd5e6f7a2b3c4d5e6f7a2b3c4d5e6f7a2b3c4d5e6',
    LendingContract: '0xe6f7a2b3c4d5e6f7a2b3c4d5e6f7a2b3c4d5e6f7',
    
    // Advanced Services
    RandomnessContract: '0xf7a2b3c4d5e6f7a2b3c4d5e6f7a2b3c4d5e6f7a2',
    MonitoringContract: '0xa3b4c5d6e7f8a3b4c5d6e7f8a3b4c5d6e7f8a3b4',
    NotificationContract: '0xb4c5d6e7f8a3b4c5d6e7f8a3b4c5d6e7f8a3b4c5',
    AutomationContract: '0xc5d6e7f8a3b4c5d6e7f8a3b4c5d6e7f8a3b4c5d6'
};

class NeoServiceLayer {
    constructor() {
        this.neoLine = null;
        this.account = null;
        this.network = NETWORK;
        this.rpcUrl = RPC_URL;
        this.contracts = SERVICE_CONTRACTS;
    }

    /**
     * Initialize NeoLine connection
     */
    async init() {
        if (typeof window !== 'undefined' && window.NEOLineN3) {
            this.neoLine = new window.NEOLineN3.Init();
            return true;
        }
        return false;
    }

    /**
     * Connect to user's wallet
     */
    async connect() {
        if (!this.neoLine) {
            throw new Error('NeoLine not initialized. Please install NeoLine wallet.');
        }

        try {
            this.account = await this.neoLine.getAccount();
            const network = await this.neoLine.getNetworks();
            
            if (network.defaultNetwork !== this.network) {
                console.warn(`Connected to ${network.defaultNetwork}, expected ${this.network}`);
            }
            
            return this.account;
        } catch (error) {
            throw new Error('Failed to connect wallet: ' + error.message);
        }
    }

    /**
     * Get wallet balance
     */
    async getBalance() {
        if (!this.account) {
            throw new Error('Wallet not connected');
        }

        try {
            const balances = await this.neoLine.getBalance();
            return balances;
        } catch (error) {
            throw new Error('Failed to get balance: ' + error.message);
        }
    }

    /**
     * Storage Service Methods
     */
    async storeData(key, value) {
        return this._invoke('StorageContract', 'store', [
            { type: 'String', value: key },
            { type: 'String', value: value }
        ], 0.01);
    }

    async retrieveData(key) {
        return this._invokeRead('StorageContract', 'retrieve', [
            { type: 'String', value: key }
        ]);
    }

    /**
     * Oracle Service Methods
     */
    async requestOracleData(url, filter, callback) {
        return this._invoke('OracleContract', 'requestData', [
            { type: 'String', value: url },
            { type: 'String', value: filter },
            { type: 'String', value: callback }
        ], 0.05);
    }

    /**
     * Compute Service Methods
     */
    async executeComputation(taskType, inputData) {
        return this._invoke('ComputeContract', 'execute', [
            { type: 'String', value: taskType },
            { type: 'String', value: JSON.stringify(inputData) }
        ], 0.02);
    }

    /**
     * Analytics Service Methods
     */
    async trackEvent(eventType, eventData) {
        return this._invoke('AnalyticsContract', 'trackEvent', [
            { type: 'String', value: eventType },
            { type: 'String', value: JSON.stringify(eventData) }
        ], 0.001);
    }

    async getAnalytics(query) {
        return this._invokeRead('AnalyticsContract', 'query', [
            { type: 'String', value: JSON.stringify(query) }
        ]);
    }

    /**
     * Identity Service Methods
     */
    async createDID(attributes) {
        return this._invoke('IdentityManagementContract', 'createDID', [
            { type: 'String', value: JSON.stringify(attributes) }
        ], 0.01);
    }

    async verifyIdentity(did, proof) {
        return this._invokeRead('IdentityManagementContract', 'verify', [
            { type: 'String', value: did },
            { type: 'String', value: proof }
        ]);
    }

    /**
     * Cross-Chain Service Methods
     */
    async initiateBridge(targetChain, asset, amount) {
        return this._invoke('CrossChainContract', 'bridge', [
            { type: 'String', value: targetChain },
            { type: 'String', value: asset },
            { type: 'Integer', value: amount }
        ], 0.1);
    }

    /**
     * Payment Processing Methods
     */
    async createPayment(recipient, amount, currency) {
        return this._invoke('PaymentProcessingContract', 'createPayment', [
            { type: 'Hash160', value: recipient },
            { type: 'Integer', value: amount },
            { type: 'String', value: currency }
        ], 0.01);
    }

    /**
     * Monitoring Service Methods
     */
    async getServiceHealth(serviceName) {
        return this._invokeRead('MonitoringContract', 'getHealth', [
            { type: 'String', value: serviceName }
        ]);
    }

    /**
     * Internal invoke method for write operations
     */
    async _invoke(contractName, operation, args, fee = 0.01) {
        if (!this.account) {
            throw new Error('Wallet not connected');
        }

        const contractHash = this.contracts[contractName];
        if (!contractHash) {
            throw new Error(`Unknown contract: ${contractName}`);
        }

        try {
            const result = await this.neoLine.invoke({
                scriptHash: contractHash,
                operation: operation,
                args: args,
                fee: fee.toString(),
                broadcastOverride: false
            });

            return {
                txid: result.txid,
                nodeURL: result.nodeURL,
                signedTx: result.signedTx
            };
        } catch (error) {
            throw new Error(`Invoke failed: ${error.message}`);
        }
    }

    /**
     * Internal invoke method for read operations
     */
    async _invokeRead(contractName, operation, args) {
        const contractHash = this.contracts[contractName];
        if (!contractHash) {
            throw new Error(`Unknown contract: ${contractName}`);
        }

        try {
            const result = await this.neoLine.invokeRead({
                scriptHash: contractHash,
                operation: operation,
                args: args
            });

            return result;
        } catch (error) {
            throw new Error(`InvokeRead failed: ${error.message}`);
        }
    }

    /**
     * Monitor transaction status
     */
    async getTransaction(txid) {
        try {
            const result = await this.neoLine.getTransaction({
                txid: txid
            });
            return result;
        } catch (error) {
            throw new Error(`Failed to get transaction: ${error.message}`);
        }
    }

    /**
     * Utility method to format addresses
     */
    formatAddress(address, length = 8) {
        if (!address) return '';
        return address.substring(0, length) + '...' + address.substring(address.length - length);
    }

    /**
     * Utility method to convert to fixed-point number
     */
    toFixedPoint(value, decimals = 8) {
        return Math.floor(value * Math.pow(10, decimals));
    }

    /**
     * Utility method to convert from fixed-point number
     */
    fromFixedPoint(value, decimals = 8) {
        return value / Math.pow(10, decimals);
    }
}

// Export for use
window.NeoServiceLayer = NeoServiceLayer;

// Auto-initialize if NeoLine is available
document.addEventListener('DOMContentLoaded', async () => {
    if (window.NEOLineN3) {
        const nsl = new NeoServiceLayer();
        const initialized = await nsl.init();
        if (initialized) {
            window.neoServiceLayer = nsl;
            console.log('Neo Service Layer SDK initialized');
        }
    } else {
        console.warn('NeoLine wallet not detected. Please install NeoLine to use Neo Service Layer.');
    }
});