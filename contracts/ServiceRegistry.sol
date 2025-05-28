// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

import "@openzeppelin/contracts/access/Ownable.sol";
import "@openzeppelin/contracts/security/ReentrancyGuard.sol";
import "@openzeppelin/contracts/security/Pausable.sol";

/**
 * @title ServiceRegistry
 * @dev Central registry for all Neo Service Layer services on NeoX blockchain
 * @notice This contract manages the registration and discovery of Neo Service Layer services
 */
contract ServiceRegistry is Ownable, ReentrancyGuard, Pausable {
    
    struct ServiceInfo {
        address serviceAddress;
        string serviceName;
        string serviceVersion;
        string endpoint;
        bool isActive;
        uint256 registeredAt;
        uint256 lastUpdated;
    }
    
    struct ServiceMetrics {
        uint256 totalRequests;
        uint256 successfulRequests;
        uint256 failedRequests;
        uint256 lastRequestAt;
    }
    
    // Service ID => ServiceInfo
    mapping(bytes32 => ServiceInfo) public services;
    
    // Service ID => ServiceMetrics
    mapping(bytes32 => ServiceMetrics) public serviceMetrics;
    
    // Service name => Service ID (for lookup by name)
    mapping(string => bytes32) public serviceNameToId;
    
    // Array of all service IDs for enumeration
    bytes32[] public allServiceIds;
    
    // Events
    event ServiceRegistered(
        bytes32 indexed serviceId,
        address indexed serviceAddress,
        string serviceName,
        string serviceVersion
    );
    
    event ServiceUpdated(
        bytes32 indexed serviceId,
        address indexed serviceAddress,
        string endpoint
    );
    
    event ServiceDeactivated(bytes32 indexed serviceId);
    event ServiceActivated(bytes32 indexed serviceId);
    
    event ServiceRequestLogged(
        bytes32 indexed serviceId,
        address indexed requester,
        bool success
    );
    
    // Modifiers
    modifier serviceExists(bytes32 serviceId) {
        require(services[serviceId].serviceAddress != address(0), "Service does not exist");
        _;
    }
    
    modifier serviceActive(bytes32 serviceId) {
        require(services[serviceId].isActive, "Service is not active");
        _;
    }
    
    constructor() {}
    
    /**
     * @dev Registers a new service in the registry
     * @param serviceName The name of the service
     * @param serviceVersion The version of the service
     * @param serviceAddress The address of the service contract
     * @param endpoint The endpoint URL for the service
     * @return serviceId The unique identifier for the registered service
     */
    function registerService(
        string memory serviceName,
        string memory serviceVersion,
        address serviceAddress,
        string memory endpoint
    ) external onlyOwner whenNotPaused returns (bytes32) {
        require(bytes(serviceName).length > 0, "Service name cannot be empty");
        require(serviceAddress != address(0), "Service address cannot be zero");
        
        bytes32 serviceId = keccak256(abi.encodePacked(serviceName, serviceVersion, block.timestamp));
        
        // Check if service name is already registered
        if (serviceNameToId[serviceName] != bytes32(0)) {
            // Deactivate the old service
            services[serviceNameToId[serviceName]].isActive = false;
        }
        
        services[serviceId] = ServiceInfo({
            serviceAddress: serviceAddress,
            serviceName: serviceName,
            serviceVersion: serviceVersion,
            endpoint: endpoint,
            isActive: true,
            registeredAt: block.timestamp,
            lastUpdated: block.timestamp
        });
        
        serviceNameToId[serviceName] = serviceId;
        allServiceIds.push(serviceId);
        
        emit ServiceRegistered(serviceId, serviceAddress, serviceName, serviceVersion);
        
        return serviceId;
    }
    
    /**
     * @dev Updates an existing service
     * @param serviceId The ID of the service to update
     * @param serviceAddress The new address of the service contract
     * @param endpoint The new endpoint URL for the service
     */
    function updateService(
        bytes32 serviceId,
        address serviceAddress,
        string memory endpoint
    ) external onlyOwner serviceExists(serviceId) {
        require(serviceAddress != address(0), "Service address cannot be zero");
        
        services[serviceId].serviceAddress = serviceAddress;
        services[serviceId].endpoint = endpoint;
        services[serviceId].lastUpdated = block.timestamp;
        
        emit ServiceUpdated(serviceId, serviceAddress, endpoint);
    }
    
    /**
     * @dev Deactivates a service
     * @param serviceId The ID of the service to deactivate
     */
    function deactivateService(bytes32 serviceId) external onlyOwner serviceExists(serviceId) {
        services[serviceId].isActive = false;
        emit ServiceDeactivated(serviceId);
    }
    
    /**
     * @dev Activates a service
     * @param serviceId The ID of the service to activate
     */
    function activateService(bytes32 serviceId) external onlyOwner serviceExists(serviceId) {
        services[serviceId].isActive = true;
        emit ServiceActivated(serviceId);
    }
    
    /**
     * @dev Gets service information by ID
     * @param serviceId The ID of the service
     * @return ServiceInfo struct containing service details
     */
    function getService(bytes32 serviceId) external view returns (ServiceInfo memory) {
        return services[serviceId];
    }
    
    /**
     * @dev Gets service information by name
     * @param serviceName The name of the service
     * @return ServiceInfo struct containing service details
     */
    function getServiceByName(string memory serviceName) external view returns (ServiceInfo memory) {
        bytes32 serviceId = serviceNameToId[serviceName];
        return services[serviceId];
    }
    
    /**
     * @dev Gets service metrics
     * @param serviceId The ID of the service
     * @return ServiceMetrics struct containing service metrics
     */
    function getServiceMetrics(bytes32 serviceId) external view returns (ServiceMetrics memory) {
        return serviceMetrics[serviceId];
    }
    
    /**
     * @dev Logs a service request for metrics tracking
     * @param serviceId The ID of the service
     * @param success Whether the request was successful
     */
    function logServiceRequest(bytes32 serviceId, bool success) 
        external 
        serviceExists(serviceId) 
        serviceActive(serviceId) 
    {
        serviceMetrics[serviceId].totalRequests++;
        serviceMetrics[serviceId].lastRequestAt = block.timestamp;
        
        if (success) {
            serviceMetrics[serviceId].successfulRequests++;
        } else {
            serviceMetrics[serviceId].failedRequests++;
        }
        
        emit ServiceRequestLogged(serviceId, msg.sender, success);
    }
    
    /**
     * @dev Gets the total number of registered services
     * @return The number of services
     */
    function getServiceCount() external view returns (uint256) {
        return allServiceIds.length;
    }
    
    /**
     * @dev Gets all service IDs
     * @return Array of all service IDs
     */
    function getAllServiceIds() external view returns (bytes32[] memory) {
        return allServiceIds;
    }
    
    /**
     * @dev Checks if a service is active
     * @param serviceId The ID of the service
     * @return True if the service is active, false otherwise
     */
    function isServiceActive(bytes32 serviceId) external view returns (bool) {
        return services[serviceId].isActive;
    }
    
    /**
     * @dev Emergency pause function
     */
    function pause() external onlyOwner {
        _pause();
    }
    
    /**
     * @dev Emergency unpause function
     */
    function unpause() external onlyOwner {
        _unpause();
    }
}
