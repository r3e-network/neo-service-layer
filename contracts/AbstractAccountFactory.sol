// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

import "@openzeppelin/contracts/access/Ownable.sol";
import "@openzeppelin/contracts/security/ReentrancyGuard.sol";
import "@openzeppelin/contracts/utils/Create2.sol";
import "./AbstractAccount.sol";
import "./ServiceRegistry.sol";

/**
 * @title AbstractAccountFactory
 * @dev Factory contract for creating and managing abstract accounts
 * @notice This contract creates abstract accounts that integrate with Neo Service Layer
 */
contract AbstractAccountFactory is Ownable, ReentrancyGuard {
    
    ServiceRegistry public immutable serviceRegistry;
    bytes32 public immutable abstractAccountServiceId;
    
    struct AccountInfo {
        address accountAddress;
        address owner;
        uint256 createdAt;
        bool isActive;
        uint256 nonce;
    }
    
    // Account ID => AccountInfo
    mapping(bytes32 => AccountInfo) public accounts;
    
    // Owner => Account IDs
    mapping(address => bytes32[]) public ownerAccounts;
    
    // Account address => Account ID
    mapping(address => bytes32) public addressToAccountId;
    
    // Array of all account IDs
    bytes32[] public allAccountIds;
    
    // Account implementation address
    address public immutable accountImplementation;
    
    // Events
    event AbstractAccountCreated(
        bytes32 indexed accountId,
        address indexed accountAddress,
        address indexed owner,
        address[] guardians
    );
    
    event AccountDeactivated(bytes32 indexed accountId);
    event AccountActivated(bytes32 indexed accountId);
    
    // Modifiers
    modifier accountExists(bytes32 accountId) {
        require(accounts[accountId].accountAddress != address(0), "Account does not exist");
        _;
    }
    
    modifier onlyAccountOwner(bytes32 accountId) {
        require(accounts[accountId].owner == msg.sender, "Not account owner");
        _;
    }
    
    constructor(
        address _serviceRegistry, 
        bytes32 _abstractAccountServiceId,
        address _accountImplementation
    ) {
        require(_serviceRegistry != address(0), "Service registry cannot be zero address");
        require(_accountImplementation != address(0), "Account implementation cannot be zero address");
        
        serviceRegistry = ServiceRegistry(_serviceRegistry);
        abstractAccountServiceId = _abstractAccountServiceId;
        accountImplementation = _accountImplementation;
    }
    
    /**
     * @dev Creates a new abstract account
     * @param owner The owner of the account
     * @param guardians Array of guardian addresses for social recovery
     * @param recoveryThreshold Number of guardians required for recovery
     * @param salt Salt for deterministic address generation
     * @return accountId The unique identifier for the created account
     * @return accountAddress The address of the created account
     */
    function createAccount(
        address owner,
        address[] memory guardians,
        uint256 recoveryThreshold,
        bytes32 salt
    ) external nonReentrant returns (bytes32 accountId, address accountAddress) {
        require(owner != address(0), "Owner cannot be zero address");
        require(guardians.length >= recoveryThreshold, "Not enough guardians for threshold");
        require(recoveryThreshold > 0, "Recovery threshold must be greater than 0");
        
        // Verify abstract account service is active
        require(serviceRegistry.isServiceActive(abstractAccountServiceId), "Abstract account service is not active");
        
        // Generate account ID
        accountId = keccak256(abi.encodePacked(
            owner,
            guardians,
            recoveryThreshold,
            salt,
            block.timestamp
        ));
        
        // Create account using CREATE2 for deterministic address
        bytes memory bytecode = abi.encodePacked(
            type(AbstractAccount).creationCode,
            abi.encode(owner, guardians, recoveryThreshold, address(this))
        );
        
        accountAddress = Create2.deploy(0, salt, bytecode);
        
        // Store account information
        accounts[accountId] = AccountInfo({
            accountAddress: accountAddress,
            owner: owner,
            createdAt: block.timestamp,
            isActive: true,
            nonce: 0
        });
        
        ownerAccounts[owner].push(accountId);
        addressToAccountId[accountAddress] = accountId;
        allAccountIds.push(accountId);
        
        // Log the request in the service registry for metrics
        serviceRegistry.logServiceRequest(abstractAccountServiceId, true);
        
        emit AbstractAccountCreated(accountId, accountAddress, owner, guardians);
        
        return (accountId, accountAddress);
    }
    
    /**
     * @dev Predicts the address of an account before creation
     * @param owner The owner of the account
     * @param guardians Array of guardian addresses
     * @param recoveryThreshold Number of guardians required for recovery
     * @param salt Salt for deterministic address generation
     * @return The predicted account address
     */
    function predictAccountAddress(
        address owner,
        address[] memory guardians,
        uint256 recoveryThreshold,
        bytes32 salt
    ) external view returns (address) {
        bytes memory bytecode = abi.encodePacked(
            type(AbstractAccount).creationCode,
            abi.encode(owner, guardians, recoveryThreshold, address(this))
        );
        
        return Create2.computeAddress(salt, keccak256(bytecode));
    }
    
    /**
     * @dev Gets account information by ID
     * @param accountId The account ID
     * @return AccountInfo struct containing account details
     */
    function getAccount(bytes32 accountId) external view returns (AccountInfo memory) {
        return accounts[accountId];
    }
    
    /**
     * @dev Gets account ID by address
     * @param accountAddress The account address
     * @return The account ID
     */
    function getAccountId(address accountAddress) external view returns (bytes32) {
        return addressToAccountId[accountAddress];
    }
    
    /**
     * @dev Gets all account IDs for an owner
     * @param owner The owner address
     * @return Array of account IDs
     */
    function getOwnerAccounts(address owner) external view returns (bytes32[] memory) {
        return ownerAccounts[owner];
    }
    
    /**
     * @dev Gets the total number of created accounts
     * @return The number of accounts
     */
    function getAccountCount() external view returns (uint256) {
        return allAccountIds.length;
    }
    
    /**
     * @dev Gets all account IDs
     * @return Array of all account IDs
     */
    function getAllAccountIds() external view returns (bytes32[] memory) {
        return allAccountIds;
    }
    
    /**
     * @dev Deactivates an account (only owner)
     * @param accountId The account ID to deactivate
     */
    function deactivateAccount(bytes32 accountId) 
        external 
        accountExists(accountId) 
        onlyAccountOwner(accountId) 
    {
        accounts[accountId].isActive = false;
        emit AccountDeactivated(accountId);
    }
    
    /**
     * @dev Activates an account (only owner)
     * @param accountId The account ID to activate
     */
    function activateAccount(bytes32 accountId) 
        external 
        accountExists(accountId) 
        onlyAccountOwner(accountId) 
    {
        accounts[accountId].isActive = true;
        emit AccountActivated(accountId);
    }
    
    /**
     * @dev Checks if an account is active
     * @param accountId The account ID
     * @return True if the account is active
     */
    function isAccountActive(bytes32 accountId) external view returns (bool) {
        return accounts[accountId].isActive;
    }
    
    /**
     * @dev Increments the nonce for an account (called by the account contract)
     * @param accountId The account ID
     */
    function incrementNonce(bytes32 accountId) external accountExists(accountId) {
        require(msg.sender == accounts[accountId].accountAddress, "Only account can increment nonce");
        accounts[accountId].nonce++;
    }
    
    /**
     * @dev Gets the current nonce for an account
     * @param accountId The account ID
     * @return The current nonce
     */
    function getNonce(bytes32 accountId) external view returns (uint256) {
        return accounts[accountId].nonce;
    }
    
    /**
     * @dev Batch create multiple accounts for the same owner
     * @param owner The owner of all accounts
     * @param guardiansArray Array of guardian arrays for each account
     * @param recoveryThresholds Array of recovery thresholds for each account
     * @param salts Array of salts for each account
     * @return accountIds Array of created account IDs
     * @return accountAddresses Array of created account addresses
     */
    function batchCreateAccounts(
        address owner,
        address[][] memory guardiansArray,
        uint256[] memory recoveryThresholds,
        bytes32[] memory salts
    ) external nonReentrant returns (bytes32[] memory accountIds, address[] memory accountAddresses) {
        require(guardiansArray.length == recoveryThresholds.length, "Arrays length mismatch");
        require(recoveryThresholds.length == salts.length, "Arrays length mismatch");
        require(guardiansArray.length > 0 && guardiansArray.length <= 5, "Invalid batch size");
        
        accountIds = new bytes32[](guardiansArray.length);
        accountAddresses = new address[](guardiansArray.length);
        
        for (uint256 i = 0; i < guardiansArray.length; i++) {
            (accountIds[i], accountAddresses[i]) = createAccount(
                owner,
                guardiansArray[i],
                recoveryThresholds[i],
                salts[i]
            );
        }
        
        return (accountIds, accountAddresses);
    }
}
