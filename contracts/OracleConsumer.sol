// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

import "@openzeppelin/contracts/access/Ownable.sol";
import "@openzeppelin/contracts/security/ReentrancyGuard.sol";
import "./ServiceRegistry.sol";

/**
 * @title OracleConsumer
 * @dev Contract for consuming external data from Neo Service Layer Oracle Service
 * @notice This contract provides secure external data access using the Neo Service Layer
 */
contract OracleConsumer is Ownable, ReentrancyGuard {
    
    ServiceRegistry public immutable serviceRegistry;
    bytes32 public immutable oracleServiceId;
    
    struct OracleRequest {
        address requester;
        string dataSource;
        string dataPath;
        uint256 requestedAt;
        bool fulfilled;
        bytes data;
        uint256 fulfilledAt;
        bool success;
    }
    
    struct PriceData {
        uint256 price;
        uint256 timestamp;
        uint8 decimals;
        bool isValid;
    }
    
    // Request ID => OracleRequest
    mapping(bytes32 => OracleRequest) public oracleRequests;
    
    // Requester => Request IDs
    mapping(address => bytes32[]) public userRequests;
    
    // Data source => Latest price data
    mapping(string => PriceData) public latestPrices;
    
    // Array of all request IDs
    bytes32[] public allRequestIds;
    
    // Supported data sources
    mapping(string => bool) public supportedDataSources;
    
    // Events
    event OracleDataRequested(
        bytes32 indexed requestId,
        address indexed requester,
        string dataSource,
        string dataPath
    );
    
    event OracleDataFulfilled(
        bytes32 indexed requestId,
        address indexed requester,
        bool success,
        bytes data
    );
    
    event PriceUpdated(
        string indexed dataSource,
        uint256 price,
        uint256 timestamp
    );
    
    event DataSourceAdded(string dataSource);
    event DataSourceRemoved(string dataSource);
    
    // Modifiers
    modifier requestExists(bytes32 requestId) {
        require(oracleRequests[requestId].requester != address(0), "Request does not exist");
        _;
    }
    
    modifier requestNotFulfilled(bytes32 requestId) {
        require(!oracleRequests[requestId].fulfilled, "Request already fulfilled");
        _;
    }
    
    modifier supportedDataSource(string memory dataSource) {
        require(supportedDataSources[dataSource], "Data source not supported");
        _;
    }
    
    constructor(address _serviceRegistry, bytes32 _oracleServiceId) {
        require(_serviceRegistry != address(0), "Service registry cannot be zero address");
        serviceRegistry = ServiceRegistry(_serviceRegistry);
        oracleServiceId = _oracleServiceId;
        
        // Add default supported data sources
        supportedDataSources["coinmarketcap"] = true;
        supportedDataSources["coingecko"] = true;
        supportedDataSources["binance"] = true;
        supportedDataSources["coinbase"] = true;
    }
    
    /**
     * @dev Requests external data from a specified source
     * @param dataSource The data source identifier (e.g., "coinmarketcap", "coingecko")
     * @param dataPath The path to the specific data (e.g., "bitcoin/price", "ethereum/volume")
     * @return requestId The unique identifier for this oracle request
     */
    function requestOracleData(string memory dataSource, string memory dataPath) 
        external 
        supportedDataSource(dataSource) 
        nonReentrant 
        returns (bytes32) 
    {
        require(bytes(dataSource).length > 0, "Data source cannot be empty");
        require(bytes(dataPath).length > 0, "Data path cannot be empty");
        
        // Verify oracle service is active
        require(serviceRegistry.isServiceActive(oracleServiceId), "Oracle service is not active");
        
        bytes32 requestId = keccak256(abi.encodePacked(
            msg.sender,
            dataSource,
            dataPath,
            block.timestamp,
            allRequestIds.length
        ));
        
        oracleRequests[requestId] = OracleRequest({
            requester: msg.sender,
            dataSource: dataSource,
            dataPath: dataPath,
            requestedAt: block.timestamp,
            fulfilled: false,
            data: "",
            fulfilledAt: 0,
            success: false
        });
        
        userRequests[msg.sender].push(requestId);
        allRequestIds.push(requestId);
        
        // Log the request in the service registry for metrics
        serviceRegistry.logServiceRequest(oracleServiceId, true);
        
        emit OracleDataRequested(requestId, msg.sender, dataSource, dataPath);
        
        return requestId;
    }
    
    /**
     * @dev Fulfills an oracle request (called by the Neo Service Layer)
     * @param requestId The ID of the request to fulfill
     * @param success Whether the data retrieval was successful
     * @param data The retrieved data
     */
    function fulfillOracleRequest(bytes32 requestId, bool success, bytes memory data) 
        external 
        onlyOwner 
        requestExists(requestId) 
        requestNotFulfilled(requestId) 
    {
        OracleRequest storage request = oracleRequests[requestId];
        
        request.fulfilled = true;
        request.success = success;
        request.data = data;
        request.fulfilledAt = block.timestamp;
        
        // If this is price data, update the latest price
        if (success && _isPriceRequest(request.dataPath)) {
            _updateLatestPrice(request.dataSource, data);
        }
        
        emit OracleDataFulfilled(requestId, request.requester, success, data);
    }
    
    /**
     * @dev Gets the result of an oracle request
     * @param requestId The ID of the request
     * @return fulfilled Whether the request has been fulfilled
     * @return success Whether the data retrieval was successful
     * @return data The retrieved data
     */
    function getOracleResult(bytes32 requestId) 
        external 
        view 
        requestExists(requestId) 
        returns (bool fulfilled, bool success, bytes memory data) 
    {
        OracleRequest memory request = oracleRequests[requestId];
        return (request.fulfilled, request.success, request.data);
    }
    
    /**
     * @dev Gets the latest price for a data source
     * @param dataSource The data source identifier
     * @return price The latest price
     * @return timestamp When the price was last updated
     * @return decimals The number of decimal places
     * @return isValid Whether the price data is valid
     */
    function getLatestPrice(string memory dataSource) 
        external 
        view 
        returns (uint256 price, uint256 timestamp, uint8 decimals, bool isValid) 
    {
        PriceData memory priceData = latestPrices[dataSource];
        return (priceData.price, priceData.timestamp, priceData.decimals, priceData.isValid);
    }
    
    /**
     * @dev Gets all request IDs for a user
     * @param user The address of the user
     * @return Array of request IDs
     */
    function getUserRequests(address user) external view returns (bytes32[] memory) {
        return userRequests[user];
    }
    
    /**
     * @dev Gets the total number of oracle requests
     * @return The number of requests
     */
    function getRequestCount() external view returns (uint256) {
        return allRequestIds.length;
    }
    
    /**
     * @dev Gets request details
     * @param requestId The ID of the request
     * @return OracleRequest struct containing request details
     */
    function getRequest(bytes32 requestId) external view returns (OracleRequest memory) {
        return oracleRequests[requestId];
    }
    
    /**
     * @dev Adds a new supported data source
     * @param dataSource The data source to add
     */
    function addDataSource(string memory dataSource) external onlyOwner {
        require(bytes(dataSource).length > 0, "Data source cannot be empty");
        supportedDataSources[dataSource] = true;
        emit DataSourceAdded(dataSource);
    }
    
    /**
     * @dev Removes a supported data source
     * @param dataSource The data source to remove
     */
    function removeDataSource(string memory dataSource) external onlyOwner {
        supportedDataSources[dataSource] = false;
        emit DataSourceRemoved(dataSource);
    }
    
    /**
     * @dev Checks if a data source is supported
     * @param dataSource The data source to check
     * @return True if supported, false otherwise
     */
    function isDataSourceSupported(string memory dataSource) external view returns (bool) {
        return supportedDataSources[dataSource];
    }
    
    /**
     * @dev Internal function to check if a request is for price data
     * @param dataPath The data path to check
     * @return True if it's a price request
     */
    function _isPriceRequest(string memory dataPath) internal pure returns (bool) {
        return _contains(dataPath, "price") || _contains(dataPath, "usd");
    }
    
    /**
     * @dev Internal function to update latest price data
     * @param dataSource The data source
     * @param data The price data
     */
    function _updateLatestPrice(string memory dataSource, bytes memory data) internal {
        if (data.length >= 32) {
            uint256 price = abi.decode(data, (uint256));
            
            latestPrices[dataSource] = PriceData({
                price: price,
                timestamp: block.timestamp,
                decimals: 8, // Default to 8 decimals
                isValid: true
            });
            
            emit PriceUpdated(dataSource, price, block.timestamp);
        }
    }
    
    /**
     * @dev Internal function to check if a string contains a substring
     * @param str The string to search in
     * @param substr The substring to search for
     * @return True if substring is found
     */
    function _contains(string memory str, string memory substr) internal pure returns (bool) {
        bytes memory strBytes = bytes(str);
        bytes memory substrBytes = bytes(substr);
        
        if (substrBytes.length > strBytes.length) {
            return false;
        }
        
        for (uint256 i = 0; i <= strBytes.length - substrBytes.length; i++) {
            bool found = true;
            for (uint256 j = 0; j < substrBytes.length; j++) {
                if (strBytes[i + j] != substrBytes[j]) {
                    found = false;
                    break;
                }
            }
            if (found) {
                return true;
            }
        }
        
        return false;
    }
}
