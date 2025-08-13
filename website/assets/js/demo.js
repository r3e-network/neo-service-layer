// Interactive Demo Functionality

// Initialize demos when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    initDemoTabs();
    initEncryptionDemo();
    initSmartContractDemo();
    initConsensusDemo();
    initArchitectureVisualization();
    initCodeEditor();
});

// Demo Tabs
function initDemoTabs() {
    const demoTabs = document.querySelectorAll('.demo-tab');
    const demoPanels = document.querySelectorAll('.demo-panel');
    
    demoTabs.forEach((tab, index) => {
        tab.addEventListener('click', () => {
            // Remove active class from all
            demoTabs.forEach(t => t.classList.remove('active'));
            demoPanels.forEach(p => p.classList.remove('active'));
            
            // Add active class to clicked
            tab.classList.add('active');
            if (demoPanels[index]) {
                demoPanels[index].classList.add('active');
                
                // Initialize specific demo when tab is activated
                const demoType = tab.getAttribute('data-demo');
                initializeDemo(demoType);
            }
        });
    });
}

// Initialize specific demo based on type
function initializeDemo(type) {
    switch(type) {
        case 'encryption':
            resetEncryptionDemo();
            break;
        case 'smart-contract':
            resetSmartContractDemo();
            break;
        case 'consensus':
            resetConsensusDemo();
            break;
    }
}

// Encryption Demo
function initEncryptionDemo() {
    const plainTextInput = document.querySelector('#plain-text');
    const encryptBtn = document.querySelector('#encrypt-btn');
    const decryptBtn = document.querySelector('#decrypt-btn');
    const encryptedOutput = document.querySelector('#encrypted-output');
    const decryptedOutput = document.querySelector('#decrypted-output');
    const keyDisplay = document.querySelector('#encryption-key');
    
    if (!plainTextInput || !encryptBtn) return;
    
    let encryptionKey = generateKey();
    let encryptedData = '';
    
    // Display initial key
    if (keyDisplay) {
        keyDisplay.textContent = encryptionKey;
    }
    
    // Encrypt button
    encryptBtn.addEventListener('click', async () => {
        const plainText = plainTextInput.value.trim();
        if (!plainText) {
            showDemoNotification('Please enter some text to encrypt', 'warning');
            return;
        }
        
        // Simulate encryption process
        encryptBtn.disabled = true;
        encryptBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Encrypting...';
        
        await simulateProcessing(1000);
        
        // Generate encrypted data (simulated)
        encryptedData = btoa(plainText).replace(/=/g, '').split('').map(char => {
            return String.fromCharCode(char.charCodeAt(0) + 3);
        }).join('');
        
        // Display results
        if (encryptedOutput) {
            encryptedOutput.textContent = encryptedData;
            animateText(encryptedOutput);
        }
        
        // Update status
        updateDemoStatus('encryption', {
            'Data Size': plainText.length + ' bytes',
            'Encryption': 'AES-256-GCM',
            'Key Length': '256 bits',
            'Status': 'Encrypted'
        });
        
        encryptBtn.disabled = false;
        encryptBtn.innerHTML = '<i class="fas fa-lock"></i> Encrypt';
        decryptBtn.disabled = false;
        
        showDemoNotification('Data encrypted successfully!', 'success');
    });
    
    // Decrypt button
    decryptBtn.addEventListener('click', async () => {
        if (!encryptedData) {
            showDemoNotification('Please encrypt some data first', 'warning');
            return;
        }
        
        decryptBtn.disabled = true;
        decryptBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Decrypting...';
        
        await simulateProcessing(800);
        
        // Decrypt data (simulated)
        const decrypted = encryptedData.split('').map(char => {
            return String.fromCharCode(char.charCodeAt(0) - 3);
        }).join('');
        
        const originalText = atob(decrypted + '==');
        
        if (decryptedOutput) {
            decryptedOutput.textContent = originalText;
            animateText(decryptedOutput);
        }
        
        // Update status
        updateDemoStatus('encryption', {
            'Data Size': originalText.length + ' bytes',
            'Decryption': 'AES-256-GCM',
            'Verification': 'Passed',
            'Status': 'Decrypted'
        });
        
        decryptBtn.disabled = false;
        decryptBtn.innerHTML = '<i class="fas fa-unlock"></i> Decrypt';
        
        showDemoNotification('Data decrypted successfully!', 'success');
    });
    
    // Generate new key button
    const newKeyBtn = document.querySelector('#new-key-btn');
    if (newKeyBtn) {
        newKeyBtn.addEventListener('click', () => {
            encryptionKey = generateKey();
            keyDisplay.textContent = encryptionKey;
            resetEncryptionDemo();
            showDemoNotification('New encryption key generated', 'info');
        });
    }
}

// Smart Contract Demo
function initSmartContractDemo() {
    const contractCode = document.querySelector('#contract-code');
    const deployBtn = document.querySelector('#deploy-btn');
    const executeBtn = document.querySelector('#execute-btn');
    const contractOutput = document.querySelector('#contract-output');
    const executionLog = document.querySelector('#execution-log');
    
    if (!contractCode || !deployBtn) return;
    
    let contractDeployed = false;
    let contractAddress = '';
    
    // Sample contract code
    const sampleContract = `pragma solidity ^0.8.0;

contract SecureVault {
    mapping(address => uint256) private balances;
    address private owner;
    
    constructor() {
        owner = msg.sender;
    }
    
    function deposit() public payable {
        balances[msg.sender] += msg.value;
    }
    
    function withdraw(uint256 amount) public {
        require(balances[msg.sender] >= amount);
        balances[msg.sender] -= amount;
        payable(msg.sender).transfer(amount);
    }
}`;
    
    contractCode.value = sampleContract;
    
    // Deploy contract
    deployBtn.addEventListener('click', async () => {
        const code = contractCode.value.trim();
        if (!code) {
            showDemoNotification('Please enter contract code', 'warning');
            return;
        }
        
        deployBtn.disabled = true;
        deployBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Deploying...';
        
        // Clear previous output
        if (contractOutput) {
            contractOutput.innerHTML = '<div class="processing">Compiling contract...</div>';
        }
        
        await simulateProcessing(1500);
        
        // Generate contract address
        contractAddress = '0x' + generateRandomHex(40);
        contractDeployed = true;
        
        // Display deployment info
        const deploymentInfo = `
            <div class="deployment-success">
                <h4><i class="fas fa-check-circle"></i> Contract Deployed Successfully</h4>
                <div class="deployment-details">
                    <div class="detail-item">
                        <span class="label">Contract Address:</span>
                        <code>${contractAddress}</code>
                    </div>
                    <div class="detail-item">
                        <span class="label">Network:</span>
                        <span>Neo N3 Testnet</span>
                    </div>
                    <div class="detail-item">
                        <span class="label">Gas Used:</span>
                        <span>0.0231 GAS</span>
                    </div>
                    <div class="detail-item">
                        <span class="label">Block Number:</span>
                        <span>${Math.floor(Math.random() * 1000000) + 5000000}</span>
                    </div>
                </div>
            </div>
        `;
        
        if (contractOutput) {
            contractOutput.innerHTML = deploymentInfo;
            animateElement(contractOutput.querySelector('.deployment-success'));
        }
        
        deployBtn.disabled = false;
        deployBtn.innerHTML = '<i class="fas fa-rocket"></i> Deploy Contract';
        executeBtn.disabled = false;
        
        // Update status
        updateDemoStatus('smart-contract', {
            'Contract': 'Deployed',
            'Network': 'Neo N3',
            'Status': 'Active',
            'Functions': '3'
        });
        
        showDemoNotification('Contract deployed successfully!', 'success');
    });
    
    // Execute contract
    executeBtn.addEventListener('click', async () => {
        if (!contractDeployed) {
            showDemoNotification('Please deploy the contract first', 'warning');
            return;
        }
        
        executeBtn.disabled = true;
        executeBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Executing...';
        
        await simulateProcessing(1000);
        
        // Simulate execution log
        const logs = [
            { time: '12:34:56', action: 'Function call: deposit()', status: 'success', gas: '0.0012' },
            { time: '12:34:57', action: 'State updated: balances[0x742d...]', status: 'success', gas: '0.0008' },
            { time: '12:34:58', action: 'Event emitted: Deposit(address, uint256)', status: 'success', gas: '0.0003' },
            { time: '12:34:59', action: 'Transaction confirmed', status: 'success', gas: '0.0023' }
        ];
        
        if (executionLog) {
            executionLog.innerHTML = '<h4>Execution Log:</h4>';
            
            logs.forEach((log, index) => {
                setTimeout(() => {
                    const logEntry = document.createElement('div');
                    logEntry.className = `log-entry ${log.status}`;
                    logEntry.innerHTML = `
                        <span class="log-time">[${log.time}]</span>
                        <span class="log-action">${log.action}</span>
                        <span class="log-gas">Gas: ${log.gas}</span>
                        <i class="fas fa-check-circle"></i>
                    `;
                    executionLog.appendChild(logEntry);
                    animateElement(logEntry);
                }, index * 300);
            });
        }
        
        executeBtn.disabled = false;
        executeBtn.innerHTML = '<i class="fas fa-play"></i> Execute Function';
        
        setTimeout(() => {
            showDemoNotification('Contract function executed successfully!', 'success');
        }, logs.length * 300);
    });
}

// Consensus Demo
function initConsensusDemo() {
    const startConsensusBtn = document.querySelector('#start-consensus-btn');
    const consensusVisualization = document.querySelector('#consensus-visualization');
    const consensusMetrics = document.querySelector('#consensus-metrics');
    
    if (!startConsensusBtn || !consensusVisualization) return;
    
    let consensusRunning = false;
    let animationInterval;
    
    startConsensusBtn.addEventListener('click', () => {
        if (consensusRunning) {
            stopConsensus();
        } else {
            startConsensus();
        }
    });
    
    function startConsensus() {
        consensusRunning = true;
        startConsensusBtn.innerHTML = '<i class="fas fa-stop"></i> Stop Consensus';
        startConsensusBtn.classList.add('active');
        
        // Create nodes visualization
        consensusVisualization.innerHTML = `
            <div class="consensus-nodes">
                <div class="node node-1" data-node="1">
                    <i class="fas fa-server"></i>
                    <span>Node 1</span>
                    <div class="node-status">Leader</div>
                </div>
                <div class="node node-2" data-node="2">
                    <i class="fas fa-server"></i>
                    <span>Node 2</span>
                    <div class="node-status">Follower</div>
                </div>
                <div class="node node-3" data-node="3">
                    <i class="fas fa-server"></i>
                    <span>Node 3</span>
                    <div class="node-status">Follower</div>
                </div>
                <div class="node node-4" data-node="4">
                    <i class="fas fa-server"></i>
                    <span>Node 4</span>
                    <div class="node-status">Follower</div>
                </div>
            </div>
            <div class="consensus-messages"></div>
        `;
        
        // Start animation
        let round = 1;
        animationInterval = setInterval(() => {
            simulateConsensusRound(round);
            round++;
        }, 3000);
        
        // Initial round
        simulateConsensusRound(1);
    }
    
    function stopConsensus() {
        consensusRunning = false;
        startConsensusBtn.innerHTML = '<i class="fas fa-play"></i> Start Consensus';
        startConsensusBtn.classList.remove('active');
        
        if (animationInterval) {
            clearInterval(animationInterval);
        }
        
        // Clear visualization
        setTimeout(() => {
            consensusVisualization.innerHTML = '<div class="demo-placeholder">Click "Start Consensus" to begin visualization</div>';
            consensusMetrics.innerHTML = '';
        }, 500);
    }
    
    function simulateConsensusRound(round) {
        const nodes = consensusVisualization.querySelectorAll('.node');
        const messagesContainer = consensusVisualization.querySelector('.consensus-messages');
        
        // Clear previous messages
        messagesContainer.innerHTML = '';
        
        // Simulate message passing
        const messages = [
            { from: 1, to: [2, 3, 4], type: 'prepare', delay: 0 },
            { from: 2, to: [1], type: 'prepare-ack', delay: 500 },
            { from: 3, to: [1], type: 'prepare-ack', delay: 700 },
            { from: 4, to: [1], type: 'prepare-ack', delay: 900 },
            { from: 1, to: [2, 3, 4], type: 'commit', delay: 1200 },
            { from: 2, to: [1], type: 'commit-ack', delay: 1700 },
            { from: 3, to: [1], type: 'commit-ack', delay: 1900 },
            { from: 4, to: [1], type: 'commit-ack', delay: 2100 }
        ];
        
        messages.forEach(msg => {
            setTimeout(() => {
                // Animate message
                msg.to.forEach(toNode => {
                    const fromNode = consensusVisualization.querySelector(`.node-${msg.from}`);
                    const targetNode = consensusVisualization.querySelector(`.node-${toNode}`);
                    
                    if (fromNode && targetNode) {
                        const message = document.createElement('div');
                        message.className = `consensus-message ${msg.type}`;
                        message.innerHTML = `<i class="fas fa-envelope"></i>`;
                        
                        // Position message
                        const fromRect = fromNode.getBoundingClientRect();
                        const toRect = targetNode.getBoundingClientRect();
                        const containerRect = consensusVisualization.getBoundingClientRect();
                        
                        message.style.left = (fromRect.left - containerRect.left + fromRect.width / 2) + 'px';
                        message.style.top = (fromRect.top - containerRect.top + fromRect.height / 2) + 'px';
                        
                        messagesContainer.appendChild(message);
                        
                        // Animate to target
                        setTimeout(() => {
                            message.style.left = (toRect.left - containerRect.left + toRect.width / 2) + 'px';
                            message.style.top = (toRect.top - containerRect.top + toRect.height / 2) + 'px';
                        }, 50);
                        
                        // Remove message
                        setTimeout(() => {
                            message.remove();
                        }, 1000);
                    }
                });
            }, msg.delay);
        });
        
        // Update metrics
        updateConsensusMetrics(round);
    }
    
    function updateConsensusMetrics(round) {
        const metrics = {
            'Round': round,
            'Leader': 'Node 1',
            'Consensus Time': (2.1 + Math.random() * 0.4).toFixed(2) + 's',
            'Messages': 8,
            'Throughput': Math.floor(1000 + Math.random() * 500) + ' tx/s'
        };
        
        if (consensusMetrics) {
            consensusMetrics.innerHTML = `
                <h4>Consensus Metrics</h4>
                <div class="metrics-grid">
                    ${Object.entries(metrics).map(([key, value]) => `
                        <div class="metric-item">
                            <span class="metric-label">${key}:</span>
                            <span class="metric-value">${value}</span>
                        </div>
                    `).join('')}
                </div>
            `;
        }
    }
}

// Architecture Visualization
function initArchitectureVisualization() {
    const archComponents = document.querySelectorAll('.arch-component');
    const componentDetails = document.querySelector('#component-details');
    
    archComponents.forEach(component => {
        component.addEventListener('click', () => {
            // Remove active class from all
            archComponents.forEach(c => c.classList.remove('active'));
            
            // Add active class to clicked
            component.classList.add('active');
            
            // Show component details
            const componentName = component.textContent;
            showComponentDetails(componentName);
        });
        
        // Hover effect
        component.addEventListener('mouseenter', () => {
            component.style.transform = 'scale(1.05)';
        });
        
        component.addEventListener('mouseleave', () => {
            component.style.transform = 'scale(1)';
        });
    });
    
    function showComponentDetails(name) {
        const details = {
            'SGX Enclave': {
                description: 'Secure execution environment for sensitive operations',
                features: ['Memory encryption', 'Remote attestation', 'Sealed storage'],
                performance: '< 5% overhead'
            },
            'Consensus Engine': {
                description: 'Byzantine fault-tolerant consensus mechanism',
                features: ['PBFT-based', 'Leader election', 'View changes'],
                performance: '> 10,000 TPS'
            },
            'Smart Contracts': {
                description: 'Turing-complete programmable contracts',
                features: ['Multiple languages', 'Gas optimization', 'State management'],
                performance: '< 100ms execution'
            },
            'P2P Network': {
                description: 'Distributed peer-to-peer communication layer',
                features: ['Node discovery', 'Message routing', 'NAT traversal'],
                performance: '< 50ms latency'
            }
        };
        
        const detail = details[name] || {
            description: 'Core component of the Neo Service Layer',
            features: ['High performance', 'Secure', 'Scalable'],
            performance: 'Optimized'
        };
        
        if (componentDetails) {
            componentDetails.innerHTML = `
                <h4>${name}</h4>
                <p>${detail.description}</p>
                <div class="component-features">
                    <h5>Key Features:</h5>
                    <ul>
                        ${detail.features.map(f => `<li>${f}</li>`).join('')}
                    </ul>
                </div>
                <div class="component-performance">
                    <span class="perf-label">Performance:</span>
                    <span class="perf-value">${detail.performance}</span>
                </div>
            `;
            
            animateElement(componentDetails);
        }
    }
}

// Code Editor Demo
function initCodeEditor() {
    const codeEditors = document.querySelectorAll('.code-editor');
    
    codeEditors.forEach(editor => {
        // Add line numbers
        const lines = editor.value.split('\n');
        const lineNumbers = document.createElement('div');
        lineNumbers.className = 'line-numbers';
        
        for (let i = 1; i <= lines.length; i++) {
            const lineNumber = document.createElement('div');
            lineNumber.textContent = i;
            lineNumbers.appendChild(lineNumber);
        }
        
        editor.parentElement.insertBefore(lineNumbers, editor);
        
        // Syntax highlighting (basic)
        editor.addEventListener('input', () => {
            // Update line numbers
            const newLines = editor.value.split('\n');
            lineNumbers.innerHTML = '';
            
            for (let i = 1; i <= newLines.length; i++) {
                const lineNumber = document.createElement('div');
                lineNumber.textContent = i;
                lineNumbers.appendChild(lineNumber);
            }
        });
        
        // Tab support
        editor.addEventListener('keydown', (e) => {
            if (e.key === 'Tab') {
                e.preventDefault();
                const start = editor.selectionStart;
                const end = editor.selectionEnd;
                
                editor.value = editor.value.substring(0, start) + '    ' + editor.value.substring(end);
                editor.selectionStart = editor.selectionEnd = start + 4;
            }
        });
    });
}

// Helper Functions
function generateKey() {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    let key = '';
    for (let i = 0; i < 32; i++) {
        key += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return key;
}

function generateRandomHex(length) {
    const chars = '0123456789abcdef';
    let hex = '';
    for (let i = 0; i < length; i++) {
        hex += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return hex;
}

function simulateProcessing(duration) {
    return new Promise(resolve => setTimeout(resolve, duration));
}

function animateText(element) {
    element.style.opacity = '0';
    element.style.transform = 'translateY(10px)';
    
    setTimeout(() => {
        element.style.transition = 'all 0.3s ease';
        element.style.opacity = '1';
        element.style.transform = 'translateY(0)';
    }, 50);
}

function animateElement(element) {
    element.classList.add('animate-fadeInUp');
}

function updateDemoStatus(demoType, status) {
    const statusGrid = document.querySelector(`#${demoType}-status`);
    if (!statusGrid) return;
    
    statusGrid.innerHTML = Object.entries(status).map(([key, value]) => `
        <div class="status-item">
            <i class="fas fa-check-circle"></i>
            <span><strong>${key}:</strong> ${value}</span>
        </div>
    `).join('');
}

function showDemoNotification(message, type = 'info') {
    // Use the main notification system if available
    if (window.NeoServiceLayer && window.NeoServiceLayer.showNotification) {
        window.NeoServiceLayer.showNotification(message, type);
    } else {
        console.log(`${type}: ${message}`);
    }
}

function resetEncryptionDemo() {
    const inputs = ['#plain-text', '#encrypted-output', '#decrypted-output'];
    inputs.forEach(selector => {
        const element = document.querySelector(selector);
        if (element) {
            if (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA') {
                element.value = '';
            } else {
                element.textContent = '';
            }
        }
    });
    
    const decryptBtn = document.querySelector('#decrypt-btn');
    if (decryptBtn) {
        decryptBtn.disabled = true;
    }
    
    updateDemoStatus('encryption', {
        'Status': 'Ready',
        'Encryption': 'AES-256-GCM',
        'Mode': 'Secure Enclave',
        'Performance': 'Optimized'
    });
}

function resetSmartContractDemo() {
    const contractOutput = document.querySelector('#contract-output');
    const executionLog = document.querySelector('#execution-log');
    
    if (contractOutput) {
        contractOutput.innerHTML = '<div class="demo-placeholder">Deploy a contract to see results</div>';
    }
    
    if (executionLog) {
        executionLog.innerHTML = '';
    }
    
    updateDemoStatus('smart-contract', {
        'Status': 'Ready',
        'Network': 'Neo N3',
        'Compiler': 'Ready',
        'VM': 'Initialized'
    });
}

function resetConsensusDemo() {
    const consensusVisualization = document.querySelector('#consensus-visualization');
    const consensusMetrics = document.querySelector('#consensus-metrics');
    
    if (consensusVisualization) {
        consensusVisualization.innerHTML = '<div class="demo-placeholder">Click "Start Consensus" to begin visualization</div>';
    }
    
    if (consensusMetrics) {
        consensusMetrics.innerHTML = '';
    }
}