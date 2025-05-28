// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

import "@openzeppelin/contracts/security/ReentrancyGuard.sol";
import "@openzeppelin/contracts/utils/cryptography/ECDSA.sol";
import "./AbstractAccountFactory.sol";

/**
 * @title AbstractAccount
 * @dev Individual abstract account contract with advanced features
 * @notice This contract provides account abstraction with social recovery and session keys
 */
contract AbstractAccount is ReentrancyGuard {
    using ECDSA for bytes32;
    
    address public owner;
    address[] public guardians;
    uint256 public recoveryThreshold;
    AbstractAccountFactory public immutable factory;
    
    struct SessionKey {
        address keyAddress;
        uint256 expiresAt;
        uint256 maxTransactionValue;
        address[] allowedContracts;
        bool isActive;
        uint256 usageCount;
        uint256 createdAt;
    }
    
    struct Recovery {
        address newOwner;
        uint256 initiatedAt;
        uint256 confirmationsCount;
        mapping(address => bool) confirmations;
        bool isCompleted;
    }
    
    // Session key address => SessionKey
    mapping(address => SessionKey) public sessionKeys;
    
    // Array of active session key addresses
    address[] public activeSessionKeys;
    
    // Recovery ID => Recovery
    mapping(bytes32 => Recovery) public recoveries;
    
    // Array of active recovery IDs
    bytes32[] public activeRecoveries;
    
    // Transaction nonce for replay protection
    uint256 public nonce;
    
    // Events
    event TransactionExecuted(
        address indexed to,
        uint256 value,
        bytes data,
        bool success,
        uint256 nonce
    );
    
    event SessionKeyCreated(
        address indexed sessionKey,
        uint256 expiresAt,
        uint256 maxTransactionValue
    );
    
    event SessionKeyRevoked(address indexed sessionKey);
    
    event RecoveryInitiated(
        bytes32 indexed recoveryId,
        address indexed newOwner,
        uint256 threshold
    );
    
    event RecoveryConfirmed(
        bytes32 indexed recoveryId,
        address indexed guardian,
        uint256 confirmationsCount
    );
    
    event RecoveryCompleted(
        bytes32 indexed recoveryId,
        address indexed oldOwner,
        address indexed newOwner
    );
    
    event OwnershipTransferred(address indexed previousOwner, address indexed newOwner);
    
    // Modifiers
    modifier onlyOwner() {
        require(msg.sender == owner, "Not the owner");
        _;
    }
    
    modifier onlyOwnerOrSessionKey() {
        require(
            msg.sender == owner || 
            (sessionKeys[msg.sender].isActive && sessionKeys[msg.sender].expiresAt > block.timestamp),
            "Not authorized"
        );
        _;
    }
    
    modifier onlyGuardian() {
        require(_isGuardian(msg.sender), "Not a guardian");
        _;
    }
    
    modifier validRecovery(bytes32 recoveryId) {
        require(recoveries[recoveryId].initiatedAt > 0, "Recovery does not exist");
        require(!recoveries[recoveryId].isCompleted, "Recovery already completed");
        _;
    }
    
    constructor(
        address _owner,
        address[] memory _guardians,
        uint256 _recoveryThreshold,
        address _factory
    ) {
        require(_owner != address(0), "Owner cannot be zero address");
        require(_guardians.length >= _recoveryThreshold, "Not enough guardians");
        require(_recoveryThreshold > 0, "Recovery threshold must be greater than 0");
        require(_factory != address(0), "Factory cannot be zero address");
        
        owner = _owner;
        guardians = _guardians;
        recoveryThreshold = _recoveryThreshold;
        factory = AbstractAccountFactory(_factory);
    }
    
    /**
     * @dev Executes a transaction from the account
     * @param to The target address
     * @param value The value to send
     * @param data The transaction data
     * @return success Whether the transaction was successful
     */
    function executeTransaction(
        address to,
        uint256 value,
        bytes memory data
    ) external onlyOwnerOrSessionKey nonReentrant returns (bool success) {
        // Check session key limits if called by session key
        if (msg.sender != owner) {
            SessionKey storage sessionKey = sessionKeys[msg.sender];
            require(value <= sessionKey.maxTransactionValue, "Transaction value exceeds limit");
            
            // Check if target contract is allowed
            if (sessionKey.allowedContracts.length > 0) {
                bool isAllowed = false;
                for (uint256 i = 0; i < sessionKey.allowedContracts.length; i++) {
                    if (sessionKey.allowedContracts[i] == to) {
                        isAllowed = true;
                        break;
                    }
                }
                require(isAllowed, "Target contract not allowed");
            }
            
            sessionKey.usageCount++;
        }
        
        // Increment nonce for replay protection
        nonce++;
        factory.incrementNonce(factory.getAccountId(address(this)));
        
        // Execute the transaction
        (success, ) = to.call{value: value}(data);
        
        emit TransactionExecuted(to, value, data, success, nonce);
        
        return success;
    }
    
    /**
     * @dev Creates a new session key
     * @param sessionKeyAddress The address of the session key
     * @param expiresAt When the session key expires
     * @param maxTransactionValue Maximum value per transaction
     * @param allowedContracts Array of allowed contract addresses (empty for all)
     */
    function createSessionKey(
        address sessionKeyAddress,
        uint256 expiresAt,
        uint256 maxTransactionValue,
        address[] memory allowedContracts
    ) external onlyOwner {
        require(sessionKeyAddress != address(0), "Session key cannot be zero address");
        require(expiresAt > block.timestamp, "Expiration must be in the future");
        require(!sessionKeys[sessionKeyAddress].isActive, "Session key already exists");
        
        sessionKeys[sessionKeyAddress] = SessionKey({
            keyAddress: sessionKeyAddress,
            expiresAt: expiresAt,
            maxTransactionValue: maxTransactionValue,
            allowedContracts: allowedContracts,
            isActive: true,
            usageCount: 0,
            createdAt: block.timestamp
        });
        
        activeSessionKeys.push(sessionKeyAddress);
        
        emit SessionKeyCreated(sessionKeyAddress, expiresAt, maxTransactionValue);
    }
    
    /**
     * @dev Revokes a session key
     * @param sessionKeyAddress The address of the session key to revoke
     */
    function revokeSessionKey(address sessionKeyAddress) external onlyOwner {
        require(sessionKeys[sessionKeyAddress].isActive, "Session key not active");
        
        sessionKeys[sessionKeyAddress].isActive = false;
        
        // Remove from active session keys array
        for (uint256 i = 0; i < activeSessionKeys.length; i++) {
            if (activeSessionKeys[i] == sessionKeyAddress) {
                activeSessionKeys[i] = activeSessionKeys[activeSessionKeys.length - 1];
                activeSessionKeys.pop();
                break;
            }
        }
        
        emit SessionKeyRevoked(sessionKeyAddress);
    }
    
    /**
     * @dev Initiates account recovery
     * @param newOwner The new owner address
     * @return recoveryId The unique identifier for this recovery
     */
    function initiateRecovery(address newOwner) external onlyGuardian returns (bytes32) {
        require(newOwner != address(0), "New owner cannot be zero address");
        require(newOwner != owner, "New owner must be different");
        
        bytes32 recoveryId = keccak256(abi.encodePacked(
            newOwner,
            block.timestamp,
            activeRecoveries.length
        ));
        
        Recovery storage recovery = recoveries[recoveryId];
        recovery.newOwner = newOwner;
        recovery.initiatedAt = block.timestamp;
        recovery.confirmationsCount = 1;
        recovery.confirmations[msg.sender] = true;
        recovery.isCompleted = false;
        
        activeRecoveries.push(recoveryId);
        
        emit RecoveryInitiated(recoveryId, newOwner, recoveryThreshold);
        emit RecoveryConfirmed(recoveryId, msg.sender, 1);
        
        // Check if threshold is already met
        if (recovery.confirmationsCount >= recoveryThreshold) {
            _completeRecovery(recoveryId);
        }
        
        return recoveryId;
    }
    
    /**
     * @dev Confirms a recovery (called by guardians)
     * @param recoveryId The recovery ID to confirm
     */
    function confirmRecovery(bytes32 recoveryId) 
        external 
        onlyGuardian 
        validRecovery(recoveryId) 
    {
        Recovery storage recovery = recoveries[recoveryId];
        require(!recovery.confirmations[msg.sender], "Already confirmed");
        require(block.timestamp < recovery.initiatedAt + 7 days, "Recovery expired");
        
        recovery.confirmations[msg.sender] = true;
        recovery.confirmationsCount++;
        
        emit RecoveryConfirmed(recoveryId, msg.sender, recovery.confirmationsCount);
        
        // Check if threshold is met
        if (recovery.confirmationsCount >= recoveryThreshold) {
            _completeRecovery(recoveryId);
        }
    }
    
    /**
     * @dev Internal function to complete recovery
     * @param recoveryId The recovery ID to complete
     */
    function _completeRecovery(bytes32 recoveryId) internal {
        Recovery storage recovery = recoveries[recoveryId];
        
        address oldOwner = owner;
        owner = recovery.newOwner;
        recovery.isCompleted = true;
        
        // Remove from active recoveries
        for (uint256 i = 0; i < activeRecoveries.length; i++) {
            if (activeRecoveries[i] == recoveryId) {
                activeRecoveries[i] = activeRecoveries[activeRecoveries.length - 1];
                activeRecoveries.pop();
                break;
            }
        }
        
        // Revoke all session keys
        for (uint256 i = 0; i < activeSessionKeys.length; i++) {
            sessionKeys[activeSessionKeys[i]].isActive = false;
        }
        delete activeSessionKeys;
        
        emit RecoveryCompleted(recoveryId, oldOwner, recovery.newOwner);
        emit OwnershipTransferred(oldOwner, recovery.newOwner);
    }
    
    /**
     * @dev Checks if an address is a guardian
     * @param guardian The address to check
     * @return True if the address is a guardian
     */
    function _isGuardian(address guardian) internal view returns (bool) {
        for (uint256 i = 0; i < guardians.length; i++) {
            if (guardians[i] == guardian) {
                return true;
            }
        }
        return false;
    }
    
    /**
     * @dev Gets all active session keys
     * @return Array of active session key addresses
     */
    function getActiveSessionKeys() external view returns (address[] memory) {
        return activeSessionKeys;
    }
    
    /**
     * @dev Gets all guardians
     * @return Array of guardian addresses
     */
    function getGuardians() external view returns (address[] memory) {
        return guardians;
    }
    
    /**
     * @dev Gets session key information
     * @param sessionKeyAddress The session key address
     * @return SessionKey struct containing session key details
     */
    function getSessionKey(address sessionKeyAddress) external view returns (SessionKey memory) {
        return sessionKeys[sessionKeyAddress];
    }
    
    /**
     * @dev Receives Ether
     */
    receive() external payable {}
    
    /**
     * @dev Fallback function
     */
    fallback() external payable {}
}
