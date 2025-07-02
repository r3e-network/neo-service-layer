/**
 * Wallet Integration Helper
 * Handles wallet detection and connection for Neo Service Layer
 */

class WalletIntegration {
    constructor() {
        this.supportedWallets = {
            neoline: {
                name: 'NeoLine',
                downloadUrl: 'https://neoline.io/',
                icon: 'fas fa-chrome',
                detect: () => typeof window.NEOLine !== 'undefined'
            },
            o3: {
                name: 'O3 Wallet',
                downloadUrl: 'https://o3.network/',
                icon: 'fas fa-mobile-alt',
                detect: () => typeof window.o3dapi !== 'undefined'
            },
            onegate: {
                name: 'OneGate',
                downloadUrl: 'https://onegate.space/',
                icon: 'fas fa-wallet',
                detect: () => typeof window.OneGate !== 'undefined'
            }
        };
    }

    /**
     * Detect available wallets
     */
    detectWallets() {
        const available = [];
        const unavailable = [];

        for (const [key, wallet] of Object.entries(this.supportedWallets)) {
            if (wallet.detect()) {
                available.push({ key, ...wallet });
            } else {
                unavailable.push({ key, ...wallet });
            }
        }

        return { available, unavailable };
    }

    /**
     * Show wallet selection modal
     */
    showWalletSelector() {
        const { available, unavailable } = this.detectWallets();
        
        // Create modal HTML
        const modal = document.createElement('div');
        modal.className = 'wallet-modal-overlay';
        modal.innerHTML = `
            <div class="wallet-modal">
                <div class="wallet-modal-header">
                    <h3>Connect Wallet</h3>
                    <button class="wallet-modal-close" onclick="this.closest('.wallet-modal-overlay').remove()">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
                <div class="wallet-modal-content">
                    ${available.length > 0 ? `
                        <div class="wallet-section">
                            <h4>Available Wallets</h4>
                            <div class="wallet-options">
                                ${available.map(wallet => `
                                    <button class="wallet-option" onclick="connectWithWallet('${wallet.key}')">
                                        <i class="${wallet.icon}"></i>
                                        <span>${wallet.name}</span>
                                        <i class="fas fa-arrow-right"></i>
                                    </button>
                                `).join('')}
                            </div>
                        </div>
                    ` : ''}
                    
                    ${unavailable.length > 0 ? `
                        <div class="wallet-section">
                            <h4>Install Wallet</h4>
                            <div class="wallet-options">
                                ${unavailable.map(wallet => `
                                    <a href="${wallet.downloadUrl}" target="_blank" class="wallet-option wallet-option-install">
                                        <i class="${wallet.icon}"></i>
                                        <span>Install ${wallet.name}</span>
                                        <i class="fas fa-external-link-alt"></i>
                                    </a>
                                `).join('')}
                            </div>
                        </div>
                    ` : ''}
                    
                    <div class="wallet-section">
                        <h4>Demo Mode</h4>
                        <button class="wallet-option wallet-option-demo" onclick="connectWithWallet('demo')">
                            <i class="fas fa-play-circle"></i>
                            <span>Try Demo Wallet</span>
                            <small>For testing purposes only</small>
                        </button>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(modal);
        return modal;
    }

    /**
     * Connect with specific wallet
     */
    async connectWallet(walletType) {
        try {
            let result;
            
            switch (walletType) {
                case 'neoline':
                    if (!window.NEOLine) {
                        throw new Error('NeoLine not detected. Please install NeoLine extension.');
                    }
                    
                    const neolineN3 = new window.NEOLine.Init();
                    result = await neolineN3.getAccount();
                    
                    if (result.error) {
                        throw new Error(result.error.description || 'NeoLine connection failed');
                    }
                    
                    return {
                        address: result.address,
                        label: result.label || 'NeoLine Wallet',
                        isConnected: true,
                        walletType: 'neoline',
                        balance: await this.getBalance(result.address)
                    };
                    
                case 'o3':
                    if (!window.o3dapi) {
                        throw new Error('O3 Wallet not detected. Please install O3 Wallet.');
                    }
                    
                    await window.o3dapi.initPlugins([window.o3dapi.NEO]);
                    const o3Account = await window.o3dapi.NEO.getAccount();
                    
                    return {
                        address: o3Account.address,
                        label: o3Account.label || 'O3 Wallet',
                        isConnected: true,
                        walletType: 'o3',
                        balance: await this.getBalance(o3Account.address)
                    };
                    
                case 'demo':
                    return {
                        address: 'NX8GreRFGFK5wpGMWetpX93HmtrezGogzk',
                        label: 'Demo Wallet',
                        isConnected: true,
                        walletType: 'demo',
                        balance: {
                            NEO: '100.00000000',
                            GAS: '1000.12345678'
                        }
                    };
                    
                default:
                    throw new Error(`Unsupported wallet type: ${walletType}`);
            }
        } catch (error) {
            console.error('Wallet connection error:', error);
            throw error;
        }
    }

    /**
     * Get wallet balance
     */
    async getBalance(address) {
        try {
            // In a real implementation, this would query the Neo blockchain
            // For demo purposes, return mock data
            return {
                NEO: '10.00000000',
                GAS: '50.12345678'
            };
        } catch (error) {
            console.error('Failed to get balance:', error);
            return {
                NEO: '0.00000000',
                GAS: '0.00000000'
            };
        }
    }

    /**
     * Sign transaction with wallet
     */
    async signTransaction(transaction, walletType) {
        try {
            switch (walletType) {
                case 'neoline':
                    const neolineN3 = new window.NEOLine.Init();
                    return await neolineN3.invoke(transaction);
                    
                case 'o3':
                    return await window.o3dapi.NEO.invoke(transaction);
                    
                case 'demo':
                    // Simulate signing for demo
                    await new Promise(resolve => setTimeout(resolve, 1000));
                    return {
                        txid: '0x' + Array.from({length: 64}, () => Math.floor(Math.random() * 16).toString(16)).join(''),
                        nodeUrl: 'https://testnet1.neo.coz.io:443'
                    };
                    
                default:
                    throw new Error(`Signing not supported for wallet type: ${walletType}`);
            }
        } catch (error) {
            console.error('Transaction signing error:', error);
            throw error;
        }
    }
}

// Make wallet integration available globally
window.WalletIntegration = WalletIntegration;

// Global function for connecting with specific wallet
window.connectWithWallet = async function(walletType) {
    try {
        const modal = document.querySelector('.wallet-modal-overlay');
        if (modal) modal.remove();
        
        const integration = new WalletIntegration();
        const wallet = await integration.connectWallet(walletType);
        
        // Emit wallet connected event for SDK
        if (window.neoServiceLayerSDK) {
            window.neoServiceLayerSDK.wallet = wallet;
            window.neoServiceLayerSDK.emit('wallet-connected', wallet);
        }
        
        return wallet;
    } catch (error) {
        console.error('Wallet connection failed:', error);
        if (window.showNotification) {
            window.showNotification('Failed to connect wallet: ' + error.message, 'error');
        }
        throw error;
    }
};

// Add wallet modal styles
const walletStyles = document.createElement('style');
walletStyles.textContent = `
    .wallet-modal-overlay {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: rgba(0, 0, 0, 0.8);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 10000;
        animation: fadeIn 0.3s ease;
    }

    .wallet-modal {
        background: var(--neo-gray-900);
        border: 1px solid var(--neo-gray-800);
        border-radius: var(--neo-radius-xl);
        max-width: 500px;
        width: 90vw;
        max-height: 80vh;
        overflow-y: auto;
    }

    .wallet-modal-header {
        padding: var(--neo-space-6);
        border-bottom: 1px solid var(--neo-gray-800);
        display: flex;
        justify-content: space-between;
        align-items: center;
    }

    .wallet-modal-header h3 {
        margin: 0;
        font-size: var(--neo-text-xl);
        color: var(--neo-white);
    }

    .wallet-modal-close {
        background: none;
        border: none;
        color: var(--neo-gray-400);
        cursor: pointer;
        padding: var(--neo-space-2);
        border-radius: var(--neo-radius-md);
        transition: all var(--neo-duration-200);
    }

    .wallet-modal-close:hover {
        background: var(--neo-gray-800);
        color: var(--neo-white);
    }

    .wallet-modal-content {
        padding: var(--neo-space-6);
    }

    .wallet-section {
        margin-bottom: var(--neo-space-8);
    }

    .wallet-section h4 {
        margin: 0 0 var(--neo-space-4) 0;
        font-size: var(--neo-text-lg);
        color: var(--neo-gray-200);
    }

    .wallet-options {
        display: flex;
        flex-direction: column;
        gap: var(--neo-space-3);
    }

    .wallet-option {
        display: flex;
        align-items: center;
        gap: var(--neo-space-4);
        padding: var(--neo-space-4);
        background: var(--neo-gray-800);
        border: 1px solid var(--neo-gray-700);
        border-radius: var(--neo-radius-lg);
        color: var(--neo-white);
        text-decoration: none;
        cursor: pointer;
        transition: all var(--neo-duration-200);
    }

    .wallet-option:hover {
        background: var(--neo-gray-700);
        border-color: var(--neo-primary);
        transform: translateY(-1px);
    }

    .wallet-option i:first-child {
        font-size: var(--neo-text-xl);
        color: var(--neo-primary);
        width: 24px;
        text-align: center;
    }

    .wallet-option span {
        flex: 1;
        font-weight: var(--neo-font-medium);
    }

    .wallet-option small {
        display: block;
        color: var(--neo-gray-400);
        font-size: var(--neo-text-sm);
    }

    .wallet-option-install {
        opacity: 0.8;
    }

    .wallet-option-demo {
        background: rgba(0, 212, 170, 0.1);
        border-color: var(--neo-primary);
    }

    @keyframes fadeIn {
        from { opacity: 0; }
        to { opacity: 1; }
    }
`;
document.head.appendChild(walletStyles);