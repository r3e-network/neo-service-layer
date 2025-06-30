/**
 * Neo Service Layer Contract Configuration
 * 
 * This file contains the deployed contract addresses for Neo Service Layer.
 * Update these addresses after deploying the contracts to mainnet/testnet.
 */

const NETWORKS = {
    MAINNET: {
        name: 'MainNet',
        rpcUrl: 'https://n3seed1.ngd.network:10332',
        networkMagic: 860833102
    },
    TESTNET: {
        name: 'TestNet', 
        rpcUrl: 'https://n3seed1.ngd.network:20332',
        networkMagic: 894710606
    },
    PRIVATE: {
        name: 'PrivateNet',
        rpcUrl: 'http://localhost:20332',
        networkMagic: 1234567890
    }
};

// Current network selection (change for deployment)
const CURRENT_NETWORK = 'TESTNET';

// Contract addresses by network
const CONTRACTS = {
    MAINNET: {
        // Core System Contracts
        ServiceRegistry: '0x0000000000000000000000000000000000000000',
        AdminContract: '0x0000000000000000000000000000000000000000',
        UpgradeManager: '0x0000000000000000000000000000000000000000',
        
        // Service Contracts
        StorageContract: '0x0000000000000000000000000000000000000000',
        OracleContract: '0x0000000000000000000000000000000000000000',
        ComputeContract: '0x0000000000000000000000000000000000000000',
        AnalyticsContract: '0x0000000000000000000000000000000000000000',
        IdentityManagementContract: '0x0000000000000000000000000000000000000000',
        KeyManagementContract: '0x0000000000000000000000000000000000000000',
        ComplianceContract: '0x0000000000000000000000000000000000000000',
        CrossChainContract: '0x0000000000000000000000000000000000000000',
        PaymentProcessingContract: '0x0000000000000000000000000000000000000000',
        LendingContract: '0x0000000000000000000000000000000000000000',
        MarketplaceContract: '0x0000000000000000000000000000000000000000',
        RandomnessContract: '0x0000000000000000000000000000000000000000',
        MonitoringContract: '0x0000000000000000000000000000000000000000',
        NotificationContract: '0x0000000000000000000000000000000000000000',
        AutomationContract: '0x0000000000000000000000000000000000000000',
        EventSubscriptionContract: '0x0000000000000000000000000000000000000000',
        BackupContract: '0x0000000000000000000000000000000000000000',
        ConfigurationContract: '0x0000000000000000000000000000000000000000',
        HealthContract: '0x0000000000000000000000000000000000000000',
        VotingContract: '0x0000000000000000000000000000000000000000',
        ZeroKnowledgeContract: '0x0000000000000000000000000000000000000000',
        ProofOfReserveContract: '0x0000000000000000000000000000000000000000'
    },
    TESTNET: {
        // Core System Contracts
        ServiceRegistry: '0x1234567890abcdef1234567890abcdef12345678',
        AdminContract: '0x2345678901bcdef2345678901bcdef2345678901',
        UpgradeManager: '0x3456789012cdef3456789012cdef3456789012cd',
        
        // Service Contracts - TestNet addresses (to be updated after deployment)
        StorageContract: '0x4567890123def4567890123def4567890123def4',
        OracleContract: '0x567890123ef567890123ef567890123ef567890',
        ComputeContract: '0x67890123f67890123f67890123f67890123f678',
        AnalyticsContract: '0x7890123f7890123f7890123f7890123f7890123f',
        IdentityManagementContract: '0x890123f890123f890123f890123f890123f8901',
        KeyManagementContract: '0x90123f90123f90123f90123f90123f90123f901',
        ComplianceContract: '0xa0123fa0123fa0123fa0123fa0123fa0123fa01',
        CrossChainContract: '0xb0123fb0123fb0123fb0123fb0123fb0123fb01',
        PaymentProcessingContract: '0xc0123fc0123fc0123fc0123fc0123fc0123fc01',
        LendingContract: '0xd0123fd0123fd0123fd0123fd0123fd0123fd01',
        MarketplaceContract: '0xe0123fe0123fe0123fe0123fe0123fe0123fe01',
        RandomnessContract: '0xf0123ff0123ff0123ff0123ff0123ff0123ff01',
        MonitoringContract: '0x0123f0123f0123f0123f0123f0123f0123f0123',
        NotificationContract: '0x123f123f123f123f123f123f123f123f123f123f',
        AutomationContract: '0x23f23f23f23f23f23f23f23f23f23f23f23f23f',
        EventSubscriptionContract: '0x3f3f3f3f3f3f3f3f3f3f3f3f3f3f3f3f3f3f3f3f',
        BackupContract: '0x4f4f4f4f4f4f4f4f4f4f4f4f4f4f4f4f4f4f4f4f',
        ConfigurationContract: '0x5f5f5f5f5f5f5f5f5f5f5f5f5f5f5f5f5f5f5f5f',
        HealthContract: '0x6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f6f',
        VotingContract: '0x7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f',
        ZeroKnowledgeContract: '0x8f8f8f8f8f8f8f8f8f8f8f8f8f8f8f8f8f8f8f8f',
        ProofOfReserveContract: '0x9f9f9f9f9f9f9f9f9f9f9f9f9f9f9f9f9f9f9f9f'
    },
    PRIVATE: {
        // Private network addresses for local development
        ServiceRegistry: '0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
        // ... other contracts
    }
};

// Export configuration
window.NEO_CONFIG = {
    network: NETWORKS[CURRENT_NETWORK],
    contracts: CONTRACTS[CURRENT_NETWORK],
    currentNetwork: CURRENT_NETWORK,
    
    // Helper methods
    getContractAddress: function(contractName) {
        return this.contracts[contractName] || null;
    },
    
    isMainnet: function() {
        return this.currentNetwork === 'MAINNET';
    },
    
    isTestnet: function() {
        return this.currentNetwork === 'TESTNET';
    },
    
    // Contract deployment status
    isDeployed: function(contractName) {
        const address = this.contracts[contractName];
        return address && address !== '0x0000000000000000000000000000000000000000';
    }
};