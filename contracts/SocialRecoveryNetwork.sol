// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

import "@openzeppelin/contracts/security/ReentrancyGuard.sol";
import "@openzeppelin/contracts/access/Ownable.sol";
import "@openzeppelin/contracts/token/ERC20/IERC20.sol";
import "@openzeppelin/contracts/utils/cryptography/ECDSA.sol";

interface IAbstractAccount {
    function executeRecovery(address newOwner, bytes32 recoveryId) external returns (bool);
}

interface IAttestationService {
    function verifyAttestation(address subject, bytes32 attestationType, bytes calldata proof) external view returns (bool);
}

contract SocialRecoveryNetwork is ReentrancyGuard, Ownable {
    using ECDSA for bytes32;

    // Constants
    uint256 public constant MIN_REPUTATION_SCORE = 100;
    uint256 public constant MAX_REPUTATION_SCORE = 10000;
    uint256 public constant REPUTATION_DECAY_RATE = 10; // Points per month
    uint256 public constant MIN_GUARDIAN_STAKE = 100 ether; // 100 GAS equivalent
    uint256 public constant SLASH_PERCENTAGE = 10; // 10% slash
    uint256 public constant RECOVERY_TIMEOUT = 7 days;
    uint256 public constant EMERGENCY_RECOVERY_TIMEOUT = 24 hours;
    uint256 public constant MIN_GUARDIANS = 3;
    uint256 public constant MAX_GUARDIANS = 20;

    // Structs
    struct Guardian {
        address addr;
        uint256 reputationScore;
        uint256 successfulRecoveries;
        uint256 failedAttempts;
        uint256 stakedAmount;
        uint256 lastActivityTime;
        bool isActive;
        uint256 totalEndorsements;
    }

    struct RecoveryRequest {
        bytes32 recoveryId;
        address accountAddress;
        address newOwner;
        address initiator;
        string recoveryStrategy;
        uint256 requiredConfirmations;
        uint256 currentConfirmations;
        mapping(address => bool) guardianConfirmations;
        mapping(address => uint256) confirmationWeights;
        uint256 initiatedAt;
        uint256 expiresAt;
        bool isExecuted;
        bool isEmergency;
        uint256 recoveryFee;
        address[] confirmedGuardians;
    }

    struct TrustRelation {
        address truster;
        address trustee;
        uint256 trustLevel; // 0-100
        uint256 establishedAt;
        uint256 lastInteraction;
    }

    struct RecoveryStrategy {
        string strategyId;
        string name;
        uint256 minGuardians;
        uint256 timeoutPeriod;
        bool requiresReputation;
        uint256 minReputationRequired;
        bool allowsEmergency;
        bool requiresAttestation;
        bytes32[] requiredAttestations;
    }

    struct AccountRecoveryConfig {
        address accountAddress;
        string preferredStrategy;
        mapping(address => bool) trustedGuardians;
        uint256 recoveryThreshold;
        bool allowNetworkGuardians;
        uint256 minGuardianReputation;
        bool multiFactorEnabled;
        bytes32[] requiredFactors;
    }

    struct MultiFactorAuth {
        bytes32 factorType; // email, sms, totp, biometric, attestation
        bytes32 factorHash;
        uint256 addedAt;
        bool isActive;
    }

    // State variables
    mapping(address => Guardian) public guardians;
    mapping(bytes32 => RecoveryRequest) public recoveryRequests;
    mapping(address => mapping(address => TrustRelation)) public trustGraph;
    mapping(string => RecoveryStrategy) public recoveryStrategies;
    mapping(address => AccountRecoveryConfig) public accountConfigs;
    mapping(address => mapping(bytes32 => MultiFactorAuth)) public accountFactors;
    
    address[] public activeGuardians;
    bytes32[] public activeRecoveries;
    
    IERC20 public stakingToken;
    IAttestationService public attestationService;
    
    uint256 public totalStaked;
    uint256 public totalRecoveries;
    uint256 public successfulRecoveries;

    // Events
    event GuardianEnrolled(address indexed guardian, uint256 stakeAmount, uint256 reputationScore);
    event RecoveryInitiated(bytes32 indexed recoveryId, address indexed account, address newOwner, string strategy);
    event RecoveryConfirmed(bytes32 indexed recoveryId, address indexed guardian, uint256 totalConfirmations);
    event RecoveryExecuted(bytes32 indexed recoveryId, address indexed account, address indexed newOwner);
    event GuardianSlashed(address indexed guardian, uint256 slashAmount, string reason);
    event TrustEstablished(address indexed truster, address indexed trustee, uint256 trustLevel);
    event ReputationUpdated(address indexed guardian, uint256 newScore, int256 change);
    event MultiFactorAdded(address indexed account, bytes32 factorType);
    event MultiFactorVerified(address indexed account, bytes32 factorType);

    constructor(address _stakingToken, address _attestationService) {
        stakingToken = IERC20(_stakingToken);
        attestationService = IAttestationService(_attestationService);
        _initializeDefaultStrategies();
    }

    // Guardian Management
    function enrollGuardian(uint256 stakeAmount) external nonReentrant {
        require(stakeAmount >= MIN_GUARDIAN_STAKE, "Insufficient stake");
        require(stakingToken.transferFrom(msg.sender, address(this), stakeAmount), "Stake transfer failed");

        Guardian storage guardian = guardians[msg.sender];
        
        if (guardian.addr == address(0)) {
            guardian.addr = msg.sender;
            guardian.reputationScore = MIN_REPUTATION_SCORE;
            activeGuardians.push(msg.sender);
        }
        
        guardian.stakedAmount += stakeAmount;
        guardian.isActive = true;
        guardian.lastActivityTime = block.timestamp;
        
        totalStaked += stakeAmount;
        
        emit GuardianEnrolled(msg.sender, stakeAmount, guardian.reputationScore);
    }

    // Recovery Initiation with Multi-Factor Support
    function initiateRecovery(
        address accountAddress,
        address newOwner,
        string memory strategyId,
        bool isEmergency,
        uint256 recoveryFee,
        bytes32[] memory authFactors,
        bytes[] memory authProofs
    ) external nonReentrant returns (bytes32) {
        RecoveryStrategy memory strategy = recoveryStrategies[strategyId];
        require(bytes(strategy.strategyId).length > 0, "Invalid strategy");
        require(!isEmergency || strategy.allowsEmergency, "Emergency not allowed");
        
        Guardian memory guardian = guardians[msg.sender];
        if (strategy.requiresReputation) {
            require(guardian.isActive && guardian.reputationScore >= strategy.minReputationRequired, 
                "Insufficient reputation");
        }
        
        // Verify multi-factor authentication if enabled
        AccountRecoveryConfig storage config = accountConfigs[accountAddress];
        if (config.multiFactorEnabled) {
            _verifyMultiFactorAuth(accountAddress, authFactors, authProofs);
        }
        
        bytes32 recoveryId = _generateRecoveryId(accountAddress, newOwner, block.timestamp);
        RecoveryRequest storage request = recoveryRequests[recoveryId];
        
        request.recoveryId = recoveryId;
        request.accountAddress = accountAddress;
        request.newOwner = newOwner;
        request.initiator = msg.sender;
        request.recoveryStrategy = strategyId;
        request.requiredConfirmations = _calculateRequiredConfirmations(accountAddress, strategy);
        request.currentConfirmations = 1;
        request.initiatedAt = block.timestamp;
        request.expiresAt = block.timestamp + (isEmergency ? EMERGENCY_RECOVERY_TIMEOUT : strategy.timeoutPeriod);
        request.isEmergency = isEmergency;
        request.recoveryFee = recoveryFee;
        
        request.guardianConfirmations[msg.sender] = true;
        request.confirmedGuardians.push(msg.sender);
        uint256 weight = _calculateConfirmationWeight(guardian, accountAddress);
        request.confirmationWeights[msg.sender] = weight;
        request.currentConfirmations = weight;
        
        activeRecoveries.push(recoveryId);
        
        emit RecoveryInitiated(recoveryId, accountAddress, newOwner, strategyId);
        
        return recoveryId;
    }

    // Confirm Recovery with Reputation Weighting
    function confirmRecovery(bytes32 recoveryId) external nonReentrant {
        RecoveryRequest storage request = recoveryRequests[recoveryId];
        require(request.accountAddress != address(0), "Invalid recovery");
        require(!request.isExecuted, "Already executed");
        require(block.timestamp <= request.expiresAt, "Recovery expired");
        require(!request.guardianConfirmations[msg.sender], "Already confirmed");
        
        Guardian memory guardian = guardians[msg.sender];
        require(guardian.isActive, "Not active guardian");
        
        RecoveryStrategy memory strategy = recoveryStrategies[request.recoveryStrategy];
        if (strategy.requiresReputation) {
            require(guardian.reputationScore >= strategy.minReputationRequired, 
                "Insufficient reputation");
        }
        
        uint256 weight = _calculateConfirmationWeight(guardian, request.accountAddress);
        request.guardianConfirmations[msg.sender] = true;
        request.confirmedGuardians.push(msg.sender);
        request.confirmationWeights[msg.sender] = weight;
        request.currentConfirmations += weight;
        
        emit RecoveryConfirmed(recoveryId, msg.sender, request.currentConfirmations);
        
        if (request.currentConfirmations >= request.requiredConfirmations) {
            _executeRecovery(recoveryId);
        }
    }

    // Execute Recovery
    function _executeRecovery(bytes32 recoveryId) private {
        RecoveryRequest storage request = recoveryRequests[recoveryId];
        require(!request.isExecuted, "Already executed");
        
        bool success = IAbstractAccount(request.accountAddress).executeRecovery(
            request.newOwner, 
            recoveryId
        );
        
        if (success) {
            request.isExecuted = true;
            successfulRecoveries++;
            
            _updateGuardianStats(request, true);
            _distributeRecoveryFees(request);
            
            emit RecoveryExecuted(recoveryId, request.accountAddress, request.newOwner);
        }
    }

    // Multi-Factor Authentication
    function addAuthFactor(
        bytes32 factorType,
        bytes32 factorHash
    ) external {
        MultiFactorAuth storage factor = accountFactors[msg.sender][factorType];
        factor.factorType = factorType;
        factor.factorHash = factorHash;
        factor.addedAt = block.timestamp;
        factor.isActive = true;
        
        AccountRecoveryConfig storage config = accountConfigs[msg.sender];
        config.multiFactorEnabled = true;
        if (!_containsFactor(config.requiredFactors, factorType)) {
            config.requiredFactors.push(factorType);
        }
        
        emit MultiFactorAdded(msg.sender, factorType);
    }

    function _verifyMultiFactorAuth(
        address account,
        bytes32[] memory factors,
        bytes[] memory proofs
    ) private view {
        require(factors.length == proofs.length, "Factor proof mismatch");
        
        AccountRecoveryConfig storage config = accountConfigs[account];
        
        for (uint i = 0; i < factors.length; i++) {
            MultiFactorAuth storage factor = accountFactors[account][factors[i]];
            require(factor.isActive, "Factor not active");
            
            if (factors[i] == keccak256("attestation")) {
                require(attestationService.verifyAttestation(
                    account, 
                    factor.factorHash, 
                    proofs[i]
                ), "Attestation failed");
            } else {
                // Verify other factor types (email, SMS, TOTP, etc.)
                bytes32 proofHash = keccak256(proofs[i]);
                require(proofHash == factor.factorHash, "Factor verification failed");
            }
        }
    }

    // Trust Management
    function establishTrust(address trustee, uint256 trustLevel) external {
        require(trustLevel <= 100, "Invalid trust level");
        require(guardians[trustee].isActive, "Trustee not active");
        
        TrustRelation storage relation = trustGraph[msg.sender][trustee];
        relation.truster = msg.sender;
        relation.trustee = trustee;
        relation.trustLevel = trustLevel;
        relation.establishedAt = block.timestamp;
        relation.lastInteraction = block.timestamp;
        
        if (trustLevel >= 70) {
            guardians[trustee].totalEndorsements++;
        }
        
        emit TrustEstablished(msg.sender, trustee, trustLevel);
    }

    // Reputation Management
    function updateReputation(address guardian, int256 change) external onlyOwner {
        Guardian storage g = guardians[guardian];
        require(g.addr != address(0), "Guardian not found");
        
        if (change > 0) {
            g.reputationScore = _min(g.reputationScore + uint256(change), MAX_REPUTATION_SCORE);
        } else {
            uint256 decrease = uint256(-change);
            g.reputationScore = g.reputationScore > decrease ? g.reputationScore - decrease : 0;
        }
        
        emit ReputationUpdated(guardian, g.reputationScore, change);
    }

    // Slashing
    function slashGuardian(address guardian, string memory reason) external onlyOwner {
        Guardian storage g = guardians[guardian];
        require(g.addr != address(0), "Guardian not found");
        
        uint256 slashAmount = g.stakedAmount * SLASH_PERCENTAGE / 100;
        g.stakedAmount -= slashAmount;
        g.reputationScore = g.reputationScore > 500 ? g.reputationScore - 500 : 0;
        g.failedAttempts++;
        
        if (g.stakedAmount < MIN_GUARDIAN_STAKE) {
            g.isActive = false;
        }
        
        totalStaked -= slashAmount;
        
        emit GuardianSlashed(guardian, slashAmount, reason);
    }

    // Account Configuration
    function configureRecovery(
        string memory preferredStrategy,
        uint256 recoveryThreshold,
        bool allowNetworkGuardians,
        uint256 minGuardianReputation
    ) external {
        AccountRecoveryConfig storage config = accountConfigs[msg.sender];
        config.accountAddress = msg.sender;
        config.preferredStrategy = preferredStrategy;
        config.recoveryThreshold = recoveryThreshold;
        config.allowNetworkGuardians = allowNetworkGuardians;
        config.minGuardianReputation = minGuardianReputation;
    }

    function addTrustedGuardian(address guardian) external {
        require(guardians[guardian].isActive, "Guardian not active");
        accountConfigs[msg.sender].trustedGuardians[guardian] = true;
    }

    // Helper Functions
    function _calculateRequiredConfirmations(
        address accountAddress,
        RecoveryStrategy memory strategy
    ) private view returns (uint256) {
        AccountRecoveryConfig storage config = accountConfigs[accountAddress];
        if (config.recoveryThreshold > 0) {
            return config.recoveryThreshold;
        }
        return strategy.minGuardians;
    }

    function _calculateConfirmationWeight(
        Guardian memory guardian,
        address accountAddress
    ) private view returns (uint256) {
        uint256 weight = 100; // Base weight
        
        // Reputation bonus (up to 2x)
        uint256 reputationBonus = guardian.reputationScore * 100 / MAX_REPUTATION_SCORE;
        weight = weight * (100 + reputationBonus) / 100;
        
        // Trust bonus (1.5x for trusted guardians)
        TrustRelation memory relation = trustGraph[accountAddress][guardian.addr];
        if (relation.trustLevel >= 50) {
            weight = weight * 150 / 100;
        }
        
        // Account-specific trusted guardian bonus (2x)
        if (accountConfigs[accountAddress].trustedGuardians[guardian.addr]) {
            weight = weight * 200 / 100;
        }
        
        return weight;
    }

    function _updateGuardianStats(RecoveryRequest storage request, bool success) private {
        for (uint i = 0; i < request.confirmedGuardians.length; i++) {
            Guardian storage guardian = guardians[request.confirmedGuardians[i]];
            
            if (success) {
                guardian.successfulRecoveries++;
                guardian.reputationScore = _min(
                    guardian.reputationScore + 50,
                    MAX_REPUTATION_SCORE
                );
            } else {
                guardian.failedAttempts++;
                guardian.reputationScore = guardian.reputationScore > 100 ? 
                    guardian.reputationScore - 100 : 0;
            }
            
            guardian.lastActivityTime = block.timestamp;
            
            emit ReputationUpdated(
                guardian.addr,
                guardian.reputationScore,
                success ? int256(50) : int256(-100)
            );
        }
    }

    function _distributeRecoveryFees(RecoveryRequest storage request) private {
        if (request.recoveryFee == 0) return;
        
        uint256 totalWeight = 0;
        for (uint i = 0; i < request.confirmedGuardians.length; i++) {
            totalWeight += request.confirmationWeights[request.confirmedGuardians[i]];
        }
        
        if (totalWeight == 0) return;
        
        for (uint i = 0; i < request.confirmedGuardians.length; i++) {
            address guardian = request.confirmedGuardians[i];
            uint256 weight = request.confirmationWeights[guardian];
            uint256 fee = request.recoveryFee * weight / totalWeight;
            
            if (fee > 0) {
                stakingToken.transfer(guardian, fee);
            }
        }
    }

    function _generateRecoveryId(
        address account,
        address newOwner,
        uint256 timestamp
    ) private pure returns (bytes32) {
        return keccak256(abi.encodePacked(account, newOwner, timestamp));
    }

    function _initializeDefaultStrategies() private {
        // Standard recovery
        recoveryStrategies["standard"] = RecoveryStrategy({
            strategyId: "standard",
            name: "Standard Guardian Recovery",
            minGuardians: 3,
            timeoutPeriod: 7 days,
            requiresReputation: true,
            minReputationRequired: 100,
            allowsEmergency: false,
            requiresAttestation: false,
            requiredAttestations: new bytes32[](0)
        });
        
        // Emergency recovery
        recoveryStrategies["emergency"] = RecoveryStrategy({
            strategyId: "emergency",
            name: "Emergency Recovery",
            minGuardians: 5,
            timeoutPeriod: 24 hours,
            requiresReputation: true,
            minReputationRequired: 500,
            allowsEmergency: true,
            requiresAttestation: false,
            requiredAttestations: new bytes32[](0)
        });
        
        // Multi-factor recovery
        recoveryStrategies["multifactor"] = RecoveryStrategy({
            strategyId: "multifactor",
            name: "Multi-Factor Recovery",
            minGuardians: 2,
            timeoutPeriod: 3 days,
            requiresReputation: true,
            minReputationRequired: 200,
            allowsEmergency: false,
            requiresAttestation: true,
            requiredAttestations: new bytes32[](0)
        });
    }

    function _containsFactor(bytes32[] memory factors, bytes32 factor) private pure returns (bool) {
        for (uint i = 0; i < factors.length; i++) {
            if (factors[i] == factor) return true;
        }
        return false;
    }

    function _min(uint256 a, uint256 b) private pure returns (uint256) {
        return a < b ? a : b;
    }

    // View Functions
    function getGuardianInfo(address guardian) external view returns (
        uint256 reputationScore,
        uint256 successfulRecoveries,
        uint256 failedAttempts,
        uint256 stakedAmount,
        bool isActive
    ) {
        Guardian memory g = guardians[guardian];
        return (
            g.reputationScore,
            g.successfulRecoveries,
            g.failedAttempts,
            g.stakedAmount,
            g.isActive
        );
    }

    function getRecoveryInfo(bytes32 recoveryId) external view returns (
        address accountAddress,
        address newOwner,
        uint256 currentConfirmations,
        uint256 requiredConfirmations,
        uint256 expiresAt,
        bool isExecuted
    ) {
        RecoveryRequest storage request = recoveryRequests[recoveryId];
        return (
            request.accountAddress,
            request.newOwner,
            request.currentConfirmations,
            request.requiredConfirmations,
            request.expiresAt,
            request.isExecuted
        );
    }

    function getTrustLevel(address truster, address trustee) external view returns (uint256) {
        return trustGraph[truster][trustee].trustLevel;
    }

    function getActiveGuardianCount() external view returns (uint256) {
        uint256 count = 0;
        for (uint i = 0; i < activeGuardians.length; i++) {
            if (guardians[activeGuardians[i]].isActive) {
                count++;
            }
        }
        return count;
    }

    function getActiveGuardians() external view returns (address[] memory) {
        uint256 activeCount = 0;
        
        // First count active guardians
        for (uint i = 0; i < activeGuardians.length; i++) {
            if (guardians[activeGuardians[i]].isActive) {
                activeCount++;
            }
        }
        
        // Create array of active guardians
        address[] memory active = new address[](activeCount);
        uint256 index = 0;
        
        for (uint i = 0; i < activeGuardians.length; i++) {
            if (guardians[activeGuardians[i]].isActive) {
                active[index] = activeGuardians[i];
                index++;
            }
        }
        
        return active;
    }

    function getTrustRelationsForGuardian(address guardian) external view returns (
        address[] memory trusters,
        address[] memory trustees,
        uint256[] memory trustLevels,
        uint256[] memory establishedTimes,
        uint256[] memory lastInteractions
    ) {
        // Count trust relations where guardian is trustee
        uint256 relationCount = 0;
        for (uint i = 0; i < activeGuardians.length; i++) {
            if (trustGraph[activeGuardians[i]][guardian].trustLevel > 0) {
                relationCount++;
            }
        }
        
        // Initialize arrays
        trusters = new address[](relationCount);
        trustees = new address[](relationCount);
        trustLevels = new uint256[](relationCount);
        establishedTimes = new uint256[](relationCount);
        lastInteractions = new uint256[](relationCount);
        
        // Populate arrays
        uint256 index = 0;
        for (uint i = 0; i < activeGuardians.length; i++) {
            TrustRelation memory relation = trustGraph[activeGuardians[i]][guardian];
            if (relation.trustLevel > 0) {
                trusters[index] = activeGuardians[i];
                trustees[index] = guardian;
                trustLevels[index] = relation.trustLevel;
                establishedTimes[index] = relation.establishedAt;
                lastInteractions[index] = relation.lastInteraction;
                index++;
            }
        }
        
        return (trusters, trustees, trustLevels, establishedTimes, lastInteractions);
    }

    function getActiveRecoveriesForAccount(address account) external view returns (bytes32[] memory) {
        uint256 activeCount = 0;
        
        // Count active recoveries for account
        for (uint i = 0; i < activeRecoveries.length; i++) {
            RecoveryRequest storage request = recoveryRequests[activeRecoveries[i]];
            if (request.accountAddress == account && 
                !request.isExecuted && 
                block.timestamp <= request.expiresAt) {
                activeCount++;
            }
        }
        
        // Create array of active recovery IDs
        bytes32[] memory active = new bytes32[](activeCount);
        uint256 index = 0;
        
        for (uint i = 0; i < activeRecoveries.length; i++) {
            RecoveryRequest storage request = recoveryRequests[activeRecoveries[i]];
            if (request.accountAddress == account && 
                !request.isExecuted && 
                block.timestamp <= request.expiresAt) {
                active[index] = activeRecoveries[i];
                index++;
            }
        }
        
        return active;
    }

    function cancelRecovery(bytes32 recoveryId) external nonReentrant {
        RecoveryRequest storage request = recoveryRequests[recoveryId];
        require(request.accountAddress != address(0), "Invalid recovery");
        require(!request.isExecuted, "Already executed");
        
        // Only initiator or account owner can cancel
        require(
            msg.sender == request.initiator || 
            msg.sender == request.accountAddress,
            "Not authorized to cancel"
        );
        
        // Mark as executed to prevent further actions
        request.isExecuted = true;
        
        // Refund recovery fee to initiator if any
        if (request.recoveryFee > 0) {
            stakingToken.transfer(request.initiator, request.recoveryFee);
        }
        
        // Remove from active recoveries
        for (uint i = 0; i < activeRecoveries.length; i++) {
            if (activeRecoveries[i] == recoveryId) {
                activeRecoveries[i] = activeRecoveries[activeRecoveries.length - 1];
                activeRecoveries.pop();
                break;
            }
        }
        
        emit RecoveryExecuted(recoveryId, request.accountAddress, address(0));
    }

    function getRecoveryStrategy(string memory strategyId) external view returns (
        string memory id,
        string memory name,
        uint256 minGuardians,
        uint256 timeoutPeriod,
        bool requiresReputation,
        uint256 minReputationRequired,
        bool allowsEmergency,
        bool requiresAttestation,
        bytes32[] memory requiredAttestations
    ) {
        RecoveryStrategy memory strategy = recoveryStrategies[strategyId];
        require(bytes(strategy.strategyId).length > 0, "Strategy not found");
        
        return (
            strategy.strategyId,
            strategy.name,
            strategy.minGuardians,
            strategy.timeoutPeriod,
            strategy.requiresReputation,
            strategy.minReputationRequired,
            strategy.allowsEmergency,
            strategy.requiresAttestation,
            strategy.requiredAttestations
        );
    }
}