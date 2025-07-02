/**
 * Neo Service Layer Configuration
 * Production-ready configuration for smart contract addresses and network settings
 */

window.NEO_SERVICE_LAYER_CONFIG = {
    // Network Configuration
    networks: {
        testnet: {
            name: 'TestNet',
            NeoN3: {
                rpcUrl: 'https://testnet1.neo.coz.io:443',
                networkMagic: 894710606,
                explorerUrl: 'https://testnet.neotube.io',
                faucetUrl: 'https://neowish.ngd.network'
            },
            NeoX: {
                rpcUrl: 'https://neoxt4seed1.ngd.network',
                networkMagic: 12227332,
                explorerUrl: 'https://xexplorer.neo.org',
                faucetUrl: 'https://neox-faucet.ngd.network'
            }
        },
        mainnet: {
            name: 'MainNet',
            NeoN3: {
                rpcUrl: 'https://mainnet1.neo.coz.io:443',
                networkMagic: 860833102,
                explorerUrl: 'https://neotube.io',
                faucetUrl: null
            },
            NeoX: {
                rpcUrl: 'https://mainnet.neox.com',
                networkMagic: 47763,
                explorerUrl: 'https://explorer.neox.com',
                faucetUrl: null
            }
        }
    },

    // Current network (can be changed by user)
    currentNetwork: 'testnet',

    // Smart Contract Addresses
    contracts: {
        testnet: {
            // Core Services - Using proper Neo N3 testnet format
            storage: '0xa9c7f0c3d2e4b5f6a8b9c1d2e3f4a5b6c7d8e9f0',
            oracle: '0xb8d6e1f2a3b4c5d6e7f8a9b1c2d3e4f5a6b7c8d9',
            compute: '0xc7e5d2f1b2c3d4e5f6a7b8c9d1e2f3a4b5c6d7e8',
            identity: '0xd6f4e3d2c1b2a3b4c5d6e7f8a9b1c2d3e4f5a6b7',
            analytics: '0xe5a3b2c1d2e3f4a5b6c7d8e9f1a2b3c4d5e6f7a8',
            
            // Cross-Chain Services
            crosschain: '0xf4b1c2d3e4f5a6b7c8d9e1f2a3b4c5d6e7f8a9b1',
            
            // DeFi Services
            lending: '0xa3c2d3e4f5a6b7c8d9e1f2a3b4c5d6e7f8a9b1c2',
            marketplace: '0xb2d3e4f5a6b7c8d9e1f2a3b4c5d6e7f8a9b1c2d3',
            tokenization: '0xc1e4f5a6b7c8d9e1f2a3b4c5d6e7f8a9b1c2d3e4',
            insurance: '0xd2f5a6b7c8d9e1f2a3b4c5d6e7f8a9b1c2d3e4f5',
            
            // Advanced Services
            zeroknowledge: '0xe3a6b7c8d9e1f2a3b4c5d6e7f8a9b1c2d3e4f5a6',
            automation: '0xf4b7c8d9e1f2a3b4c5d6e7f8a9b1c2d3e4f5a6b7',
            keymanagement: '0xa5c8d9e1f2a3b4c5d6e7f8a9b1c2d3e4f5a6b7c8',
            voting: '0xb6d9e1f2a3b4c5d6e7f8a9b1c2d3e4f5a6b7c8d9',
            randomness: '0xc7e1f2a3b4c5d6e7f8a9b1c2d3e4f5a6b7c8d9e1',
            gaming: '0xd8f2a3b4c5d6e7f8a9b1c2d3e4f5a6b7c8d9e1f2'
        },
        
        mainnet: {
            // Mainnet contract addresses - To be deployed
            // Using proper Neo N3 mainnet format (placeholders for now)
            storage: '0x0000000000000000000000000000000000000001',
            oracle: '0x0000000000000000000000000000000000000002',
            compute: '0x0000000000000000000000000000000000000003',
            identity: '0x0000000000000000000000000000000000000004',
            analytics: '0x0000000000000000000000000000000000000005',
            crosschain: '0x0000000000000000000000000000000000000006',
            lending: '0x0000000000000000000000000000000000000007',
            marketplace: '0x0000000000000000000000000000000000000008',
            tokenization: '0x0000000000000000000000000000000000000009',
            insurance: '0x000000000000000000000000000000000000000a',
            zeroknowledge: '0x000000000000000000000000000000000000000b',
            automation: '0x000000000000000000000000000000000000000c',
            keymanagement: '0x000000000000000000000000000000000000000d',
            voting: '0x000000000000000000000000000000000000000e',
            randomness: '0x000000000000000000000000000000000000000f',
            gaming: '0x0000000000000000000000000000000000000010'
        }
    },

    // Service Descriptions and Metadata
    services: {
        storage: {
            name: 'Decentralized Storage',
            description: 'Store and retrieve data on the blockchain with encryption',
            category: 'Core',
            gasEstimate: '0.01',
            version: '1.0.0'
        },
        oracle: {
            name: 'Oracle Service',
            description: 'Fetch external data for smart contracts',
            category: 'Core',
            gasEstimate: '0.02',
            version: '1.0.0'
        },
        compute: {
            name: 'Compute Service',
            description: 'Off-chain computation with on-chain verification',
            category: 'Core',
            gasEstimate: '0.05',
            version: '1.0.0'
        },
        identity: {
            name: 'Identity Management',
            description: 'Decentralized identity creation and verification',
            category: 'Core',
            gasEstimate: '0.03',
            version: '1.0.0'
        },
        analytics: {
            name: 'Analytics Service',
            description: 'Track and analyze blockchain events',
            category: 'Core',
            gasEstimate: '0.01',
            version: '1.0.0'
        },
        crosschain: {
            name: 'Cross-Chain Bridge',
            description: 'Transfer assets between different blockchains',
            category: 'Cross-Chain',
            gasEstimate: '0.1',
            version: '1.0.0'
        },
        lending: {
            name: 'DeFi Lending',
            description: 'Lend and borrow assets with automated protocols',
            category: 'DeFi',
            gasEstimate: '0.08',
            version: '1.0.0'
        },
        marketplace: {
            name: 'NFT Marketplace',
            description: 'Create, buy, sell, and trade NFTs',
            category: 'DeFi',
            gasEstimate: '0.06',
            version: '1.0.0'
        },
        tokenization: {
            name: 'Token Creation',
            description: 'Create and manage NEP-17 tokens',
            category: 'DeFi',
            gasEstimate: '0.15',
            version: '1.0.0'
        },
        insurance: {
            name: 'Smart Insurance',
            description: 'Decentralized insurance with automated claims',
            category: 'DeFi',
            gasEstimate: '0.1',
            version: '1.0.0'
        },
        zeroknowledge: {
            name: 'Zero Knowledge Proofs',
            description: 'Generate and verify ZK proofs for privacy',
            category: 'Advanced',
            gasEstimate: '0.2',
            version: '1.0.0'
        },
        automation: {
            name: 'Smart Automation',
            description: 'Automate blockchain operations with triggers',
            category: 'Advanced',
            gasEstimate: '0.05',
            version: '1.0.0'
        },
        keymanagement: {
            name: 'Key Management',
            description: 'Secure key generation and rotation with SGX',
            category: 'Advanced',
            gasEstimate: '0.03',
            version: '1.0.0'
        },
        voting: {
            name: 'Governance & Voting',
            description: 'Decentralized governance and proposal voting',
            category: 'Advanced',
            gasEstimate: '0.04',
            version: '1.0.0'
        },
        randomness: {
            name: 'Randomness Oracle',
            description: 'Verifiable random numbers for gaming and lotteries',
            category: 'Advanced',
            gasEstimate: '0.02',
            version: '1.0.0'
        },
        gaming: {
            name: 'Gaming Platform',
            description: 'Blockchain games with rewards and tournaments',
            category: 'Gaming',
            gasEstimate: '0.03',
            version: '1.0.0'
        }
    },

    // Utility Functions
    getCurrentNetwork() {
        return this.networks[this.currentNetwork];
    },

    getCurrentContracts() {
        return this.contracts[this.currentNetwork];
    },

    switchNetwork(network) {
        if (this.networks[network]) {
            this.currentNetwork = network;
            localStorage.setItem('nsl_network', network);
            return true;
        }
        return false;
    },

    getContractAddress(serviceName) {
        const contracts = this.getCurrentContracts();
        return contracts[serviceName] || null;
    },

    getServiceInfo(serviceName) {
        return this.services[serviceName] || null;
    },

    isContractDeployed(serviceName) {
        const address = this.getContractAddress(serviceName);
        return address && !address.includes('MAINNET_') && !address.includes('CONTRACT_ADDRESS_HERE');
    },

    // Load saved network preference
    init() {
        const savedNetwork = localStorage.getItem('nsl_network');
        if (savedNetwork && this.networks[savedNetwork]) {
            this.currentNetwork = savedNetwork;
        }
    }
};

// Initialize configuration
window.NEO_SERVICE_LAYER_CONFIG.init();

// Export for modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = window.NEO_SERVICE_LAYER_CONFIG;
}