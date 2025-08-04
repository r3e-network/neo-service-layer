// Neo Service Layer RPC Client
// Provides JavaScript client for JSON-RPC 2.0 communication

class NeoRpcClient {
    constructor(endpoint = '/rpc', options = {}) {
        this.endpoint = endpoint;
        this.options = {
            timeout: 30000,
            retries: 3,
            ...options
        };
        this.requestId = 1;
        this.authToken = null;
        this.websocket = null;
        this.subscriptions = new Map();
    }

    // Set authentication token
    setAuthToken(token) {
        this.authToken = token;
    }

    // Make a single RPC call
    async call(method, params = null, timeout = null) {
        const request = {
            jsonrpc: '2.0',
            method: method,
            params: params,
            id: this.requestId++
        };

        try {
            const response = await this.makeRequest(request, timeout);
            return this.handleResponse(response);
        } catch (error) {
            console.error(`RPC call failed for method ${method}:`, error);
            throw error;
        }
    }

    // Make multiple RPC calls in a batch
    async batch(calls) {
        const requests = calls.map(call => ({
            jsonrpc: '2.0',
            method: call.method,
            params: call.params || null,
            id: this.requestId++
        }));

        try {
            const responses = await this.makeRequest(requests);
            return responses.map(response => this.handleResponse(response));
        } catch (error) {
            console.error('Batch RPC call failed:', error);
            throw error;
        }
    }

    // Make HTTP request to RPC endpoint
    async makeRequest(data, timeout = null) {
        const controller = new AbortController();
        const timeoutMs = timeout || this.options.timeout;
        
        const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

        try {
            const headers = {
                'Content-Type': 'application/json'
            };

            if (this.authToken) {
                headers['Authorization'] = `Bearer ${this.authToken}`;
            }

            const response = await fetch(this.endpoint, {
                method: 'POST',
                headers: headers,
                body: JSON.stringify(data),
                signal: controller.signal
            });

            clearTimeout(timeoutId);

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            return await response.json();
        } catch (error) {
            clearTimeout(timeoutId);
            
            if (error.name === 'AbortError') {
                throw new Error(`Request timeout after ${timeoutMs}ms`);
            }
            
            throw error;
        }
    }

    // Handle RPC response
    handleResponse(response) {
        if (response.error) {
            const error = new Error(response.error.message);
            error.code = response.error.code;
            error.data = response.error.data;
            throw error;
        }

        return response.result;
    }

    // WebSocket connection for real-time notifications
    async connectWebSocket(endpoint = '/ws') {
        return new Promise((resolve, reject) => {
            const wsUrl = `${window.location.protocol === 'https:' ? 'wss:' : 'ws:'}//${window.location.host}${endpoint}`;
            const wsUrlWithToken = this.authToken ? `${wsUrl}?access_token=${this.authToken}` : wsUrl;
            
            this.websocket = new WebSocket(wsUrlWithToken);

            this.websocket.onopen = (event) => {
                console.log('🔌 WebSocket connected');
                resolve(event);
            };

            this.websocket.onmessage = (event) => {
                try {
                    const data = JSON.parse(event.data);
                    this.handleWebSocketMessage(data);
                } catch (error) {
                    console.error('Failed to parse WebSocket message:', error);
                }
            };

            this.websocket.onclose = (event) => {
                console.log('🔌 WebSocket disconnected:', event.code, event.reason);
                // Attempt to reconnect after 5 seconds
                setTimeout(() => {
                    if (this.websocket.readyState === WebSocket.CLOSED) {
                        this.connectWebSocket(endpoint);
                    }
                }, 5000);
            };

            this.websocket.onerror = (error) => {
                console.error('🔌 WebSocket error:', error);
                reject(error);
            };
        });
    }

    // Handle WebSocket messages
    handleWebSocketMessage(data) {
        const { method, params } = data;
        
        if (this.subscriptions.has(method)) {
            const callback = this.subscriptions.get(method);
            callback(params);
        }

        // Emit custom event for external listeners
        window.dispatchEvent(new CustomEvent('rpc-notification', {
            detail: data
        }));
    }

    // Subscribe to notifications
    subscribe(method, callback) {
        this.subscriptions.set(method, callback);
    }

    // Unsubscribe from notifications
    unsubscribe(method) {
        this.subscriptions.delete(method);
    }

    // Subscribe to blockchain events
    async subscribeToBlockchain(blockchainType) {
        if (this.websocket && this.websocket.readyState === WebSocket.OPEN) {
            this.websocket.send(JSON.stringify({
                method: 'SubscribeToBlockchain',
                params: [blockchainType]
            }));
        }
    }

    // Subscribe to service events
    async subscribeToService(serviceName) {
        if (this.websocket && this.websocket.readyState === WebSocket.OPEN) {
            this.websocket.send(JSON.stringify({
                method: 'SubscribeToService',
                params: [serviceName]
            }));
        }
    }

    // Service-specific methods
    async generateKey(keyType = 'ECDSA', keyUsage = 'Signing') {
        return await this.call('keymanagement.generatekey', {
            keyId: `key_${Date.now()}`,
            keyType: keyType,
            keyUsage: keyUsage,
            exportable: false
        });
    }

    async listKeys() {
        return await this.call('keymanagement.listkeys');
    }

    async storeData(key, value, encrypted = true) {
        return await this.call('storage.store', {
            key: key,
            value: value,
            encrypted: encrypted
        });
    }

    async retrieveData(key) {
        return await this.call('storage.retrieve', {
            key: key
        });
    }

    async createOracle(name, dataSource, updateInterval = 3600) {
        return await this.call('oracle.create', {
            name: name,
            dataSource: dataSource,
            updateInterval: updateInterval
        });
    }

    async getOracleData(oracleId) {
        return await this.call('oracle.getdata', {
            oracleId: oracleId
        });
    }

    async generateRandomBytes(length = 32) {
        return await this.call('randomness.generatebytes', {
            length: length
        });
    }

    async generateRandomNumber(min = 1, max = 100) {
        return await this.call('randomness.generatenumber', {
            min: min,
            max: max
        });
    }

    // Voting methods
    async createProposal(title, description, options, duration = 86400) {
        return await this.call('voting.createproposal', {
            title: title,
            description: description,
            options: options,
            duration: duration
        });
    }

    async vote(proposalId, optionIndex) {
        return await this.call('voting.vote', {
            proposalId: proposalId,
            optionIndex: optionIndex
        });
    }

    async getProposal(proposalId) {
        return await this.call('voting.getproposal', {
            proposalId: proposalId
        });
    }

    // Smart contract methods
    async deployContract(contractCode, parameters = []) {
        return await this.call('smartcontract.deploy', {
            code: contractCode,
            parameters: parameters
        });
    }

    async invokeContract(contractAddress, method, parameters = []) {
        return await this.call('smartcontract.invoke', {
            contractAddress: contractAddress,
            method: method,
            parameters: parameters
        });
    }

    // AI methods
    async analyzePattern(data, patternType = 'fraud') {
        return await this.call('ai.analyzepattern', {
            data: data,
            patternType: patternType
        });
    }

    async makePrediction(inputData, modelType = 'default') {
        return await this.call('ai.predict', {
            inputData: inputData,
            modelType: modelType
        });
    }

    // Utility methods
    async getServerInfo() {
        return await this.call('system.info');
    }

    async getMethodList() {
        try {
            const response = await fetch(`${this.endpoint}/methods`);
            return await response.json();
        } catch (error) {
            console.error('Failed to get method list:', error);
            throw error;
        }
    }

    async ping() {
        return await this.call('system.ping');
    }

    // Close WebSocket connection
    disconnect() {
        if (this.websocket) {
            this.websocket.close();
            this.websocket = null;
        }
        this.subscriptions.clear();
    }
}

// Global RPC client instance
window.neoRpc = new NeoRpcClient();

// Auto-connect WebSocket when token is available
window.addEventListener('auth-token-updated', async (event) => {
    window.neoRpc.setAuthToken(event.detail.token);
    
    try {
        await window.neoRpc.connectWebSocket();
        console.log('✅ RPC WebSocket connected with authentication');
    } catch (error) {
        console.error('❌ Failed to connect RPC WebSocket:', error);
    }
});

// Example usage notification handler
window.addEventListener('rpc-notification', (event) => {
    const { method, params } = event.detail;
    console.log(`📬 RPC Notification [${method}]:`, params);
    
    // Show toast notification for important events
    if (method === 'BlockchainEvent' || method === 'ServiceEvent') {
        showNotification(`${method}: ${JSON.stringify(params)}`, 'info');
    }
});

console.log('🚀 Neo RPC Client loaded and ready');