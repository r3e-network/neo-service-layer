using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NeoServiceLayer.Contracts.Core
{
    /// <summary>
    /// Central registry for all Neo Service Layer services.
    /// Manages service registration, discovery, versioning, and metadata.
    /// </summary>
    [DisplayName("ServiceRegistry")]
    [ManifestExtra("Author", "Neo Service Layer Team")]
    [ManifestExtra("Description", "Central registry for all Neo Service Layer services")]
    [ManifestExtra("Version", "1.0.0")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class ServiceRegistry : SmartContract
    {
        #region Storage Keys
        private static readonly byte[] OwnerKey = "owner".ToByteArray();
        private static readonly byte[] ServiceCountKey = "serviceCount".ToByteArray();
        private static readonly byte[] ServicePrefix = "service:".ToByteArray();
        private static readonly byte[] ServiceByNamePrefix = "serviceName:".ToByteArray();
        private static readonly byte[] ServiceVersionPrefix = "version:".ToByteArray();
        private static readonly byte[] ServiceStatusPrefix = "status:".ToByteArray();
        private static readonly byte[] ServiceMetadataPrefix = "metadata:".ToByteArray();
        private static readonly byte[] AccessControlPrefix = "access:".ToByteArray();
        private static readonly byte[] ServiceDependencyPrefix = "dependency:".ToByteArray();
        #endregion

        #region Events
        [DisplayName("ServiceRegistered")]
        public static event Action<UInt160, string, string, UInt160, string> ServiceRegistered;

        [DisplayName("ServiceUpdated")]
        public static event Action<UInt160, string, string, UInt160> ServiceUpdated;

        [DisplayName("ServiceStatusChanged")]
        public static event Action<UInt160, string, byte, byte> ServiceStatusChanged;

        [DisplayName("ServiceDeregistered")]
        public static event Action<UInt160, string> ServiceDeregistered;

        [DisplayName("OwnershipTransferred")]
        public static event Action<UInt160, UInt160> OwnershipTransferred;

        [DisplayName("AccessGranted")]
        public static event Action<UInt160, UInt160, string> AccessGranted;

        [DisplayName("AccessRevoked")]
        public static event Action<UInt160, UInt160, string> AccessRevoked;
        #endregion

        #region Service Status Enum
        public const byte STATUS_INACTIVE = 0;
        public const byte STATUS_ACTIVE = 1;
        public const byte STATUS_MAINTENANCE = 2;
        public const byte STATUS_DEPRECATED = 3;
        public const byte STATUS_SUSPENDED = 4;
        #endregion

        #region Access Roles
        public const string ROLE_ADMIN = "admin";
        public const string ROLE_SERVICE_MANAGER = "service_manager";
        public const string ROLE_AUDITOR = "auditor";
        public const string ROLE_USER = "user";
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the ServiceRegistry contract.
        /// </summary>
        /// <param name="owner">The initial owner of the contract</param>
        /// <param name="data">Additional initialization data</param>
        /// <param name="update">Whether this is an update operation</param>
        public static void _deploy(object data, bool update)
        {
            if (update) return;

            var tx = (Transaction)Runtime.ScriptContainer;
            var owner = tx.Sender;
            
            Storage.Put(Storage.CurrentContext, OwnerKey, owner);
            Storage.Put(Storage.CurrentContext, ServiceCountKey, 0);
            
            // Grant admin role to owner
            GrantAccess(owner, owner, ROLE_ADMIN);
            
            Runtime.Log($"ServiceRegistry deployed with owner: {owner}");
        }
        #endregion

        #region Service Management
        /// <summary>
        /// Registers a new service in the registry.
        /// </summary>
        /// <param name="serviceId">Unique identifier for the service</param>
        /// <param name="name">Human-readable name of the service</param>
        /// <param name="version">Version of the service</param>
        /// <param name="contractAddress">Contract address implementing the service</param>
        /// <param name="endpoint">API endpoint for the service</param>
        /// <param name="metadata">Additional metadata as JSON string</param>
        /// <returns>True if registration successful</returns>
        public static bool RegisterService(UInt160 serviceId, string name, string version, 
            UInt160 contractAddress, string endpoint, string metadata)
        {
            // Validate caller has permission
            if (!HasAccess(Runtime.CallingScriptHash, ROLE_SERVICE_MANAGER) && 
                !HasAccess(Runtime.CallingScriptHash, ROLE_ADMIN))
            {
                throw new InvalidOperationException("Insufficient permissions to register service");
            }

            // Validate inputs
            if (serviceId is null || serviceId.IsZero)
                throw new ArgumentException("Invalid service ID");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Service name cannot be empty");
            if (string.IsNullOrEmpty(version))
                throw new ArgumentException("Service version cannot be empty");
            if (contractAddress is null || contractAddress.IsZero)
                throw new ArgumentException("Invalid contract address");

            // Check if service already exists
            var serviceKey = ServicePrefix.Concat(serviceId);
            if (Storage.Get(Storage.CurrentContext, serviceKey) != null)
                throw new InvalidOperationException("Service already registered");

            // Check if name is already taken
            var nameKey = ServiceByNamePrefix.Concat(name.ToByteArray());
            if (Storage.Get(Storage.CurrentContext, nameKey) != null)
                throw new InvalidOperationException("Service name already taken");

            // Create service record
            var serviceData = new ServiceInfo
            {
                Id = serviceId,
                Name = name,
                Version = version,
                ContractAddress = contractAddress,
                Endpoint = endpoint ?? "",
                Status = STATUS_ACTIVE,
                RegisteredAt = Runtime.Time,
                UpdatedAt = Runtime.Time,
                RegisteredBy = Runtime.CallingScriptHash
            };

            // Store service data
            Storage.Put(Storage.CurrentContext, serviceKey, StdLib.Serialize(serviceData));
            Storage.Put(Storage.CurrentContext, nameKey, serviceId);
            
            // Store version info
            var versionKey = ServiceVersionPrefix.Concat(serviceId).Concat(version.ToByteArray());
            Storage.Put(Storage.CurrentContext, versionKey, Runtime.Time);
            
            // Store status
            var statusKey = ServiceStatusPrefix.Concat(serviceId);
            Storage.Put(Storage.CurrentContext, statusKey, STATUS_ACTIVE);
            
            // Store metadata if provided
            if (!string.IsNullOrEmpty(metadata))
            {
                var metadataKey = ServiceMetadataPrefix.Concat(serviceId);
                Storage.Put(Storage.CurrentContext, metadataKey, metadata);
            }

            // Increment service count
            var count = GetServiceCount();
            Storage.Put(Storage.CurrentContext, ServiceCountKey, count + 1);

            // Emit event
            ServiceRegistered(serviceId, name, version, contractAddress, endpoint);
            
            Runtime.Log($"Service registered: {name} v{version} at {contractAddress}");
            return true;
        }

        /// <summary>
        /// Updates an existing service registration.
        /// </summary>
        /// <param name="serviceId">Service identifier</param>
        /// <param name="version">New version</param>
        /// <param name="contractAddress">New contract address</param>
        /// <param name="endpoint">New endpoint</param>
        /// <param name="metadata">Updated metadata</param>
        /// <returns>True if update successful</returns>
        public static bool UpdateService(UInt160 serviceId, string version, 
            UInt160 contractAddress, string endpoint, string metadata)
        {
            // Validate caller has permission
            if (!HasAccess(Runtime.CallingScriptHash, ROLE_SERVICE_MANAGER) && 
                !HasAccess(Runtime.CallingScriptHash, ROLE_ADMIN))
            {
                throw new InvalidOperationException("Insufficient permissions to update service");
            }

            var serviceKey = ServicePrefix.Concat(serviceId);
            var serviceDataBytes = Storage.Get(Storage.CurrentContext, serviceKey);
            if (serviceDataBytes == null)
                throw new InvalidOperationException("Service not found");

            var serviceData = (ServiceInfo)StdLib.Deserialize(serviceDataBytes);
            
            // Update fields
            if (!string.IsNullOrEmpty(version))
            {
                serviceData.Version = version;
                // Store new version
                var versionKey = ServiceVersionPrefix.Concat(serviceId).Concat(version.ToByteArray());
                Storage.Put(Storage.CurrentContext, versionKey, Runtime.Time);
            }
            
            if (contractAddress != null && !contractAddress.IsZero)
                serviceData.ContractAddress = contractAddress;
            
            if (endpoint != null)
                serviceData.Endpoint = endpoint;
            
            serviceData.UpdatedAt = Runtime.Time;

            // Save updated service data
            Storage.Put(Storage.CurrentContext, serviceKey, StdLib.Serialize(serviceData));
            
            // Update metadata if provided
            if (metadata != null)
            {
                var metadataKey = ServiceMetadataPrefix.Concat(serviceId);
                if (string.IsNullOrEmpty(metadata))
                    Storage.Delete(Storage.CurrentContext, metadataKey);
                else
                    Storage.Put(Storage.CurrentContext, metadataKey, metadata);
            }

            // Emit event
            ServiceUpdated(serviceId, serviceData.Name, version ?? serviceData.Version, contractAddress ?? serviceData.ContractAddress);
            
            Runtime.Log($"Service updated: {serviceData.Name}");
            return true;
        }

        /// <summary>
        /// Changes the status of a service.
        /// </summary>
        /// <param name="serviceId">Service identifier</param>
        /// <param name="newStatus">New status</param>
        /// <returns>True if status change successful</returns>
        public static bool ChangeServiceStatus(UInt160 serviceId, byte newStatus)
        {
            // Validate caller has permission
            if (!HasAccess(Runtime.CallingScriptHash, ROLE_SERVICE_MANAGER) && 
                !HasAccess(Runtime.CallingScriptHash, ROLE_ADMIN))
            {
                throw new InvalidOperationException("Insufficient permissions to change service status");
            }

            if (newStatus > STATUS_SUSPENDED)
                throw new ArgumentException("Invalid status value");

            var serviceKey = ServicePrefix.Concat(serviceId);
            var serviceDataBytes = Storage.Get(Storage.CurrentContext, serviceKey);
            if (serviceDataBytes == null)
                throw new InvalidOperationException("Service not found");

            var serviceData = (ServiceInfo)StdLib.Deserialize(serviceDataBytes);
            var oldStatus = serviceData.Status;
            
            serviceData.Status = newStatus;
            serviceData.UpdatedAt = Runtime.Time;

            // Save updated service data
            Storage.Put(Storage.CurrentContext, serviceKey, StdLib.Serialize(serviceData));
            
            // Update status index
            var statusKey = ServiceStatusPrefix.Concat(serviceId);
            Storage.Put(Storage.CurrentContext, statusKey, newStatus);

            // Emit event
            ServiceStatusChanged(serviceId, serviceData.Name, oldStatus, newStatus);
            
            Runtime.Log($"Service status changed: {serviceData.Name} from {oldStatus} to {newStatus}");
            return true;
        }

        /// <summary>
        /// Deregisters a service from the registry.
        /// </summary>
        /// <param name="serviceId">Service identifier</param>
        /// <returns>True if deregistration successful</returns>
        public static bool DeregisterService(UInt160 serviceId)
        {
            // Only admin can deregister services
            if (!HasAccess(Runtime.CallingScriptHash, ROLE_ADMIN))
            {
                throw new InvalidOperationException("Only admin can deregister services");
            }

            var serviceKey = ServicePrefix.Concat(serviceId);
            var serviceDataBytes = Storage.Get(Storage.CurrentContext, serviceKey);
            if (serviceDataBytes == null)
                throw new InvalidOperationException("Service not found");

            var serviceData = (ServiceInfo)StdLib.Deserialize(serviceDataBytes);

            // Remove all service data
            Storage.Delete(Storage.CurrentContext, serviceKey);
            Storage.Delete(Storage.CurrentContext, ServiceByNamePrefix.Concat(serviceData.Name.ToByteArray()));
            Storage.Delete(Storage.CurrentContext, ServiceStatusPrefix.Concat(serviceId));
            Storage.Delete(Storage.CurrentContext, ServiceMetadataPrefix.Concat(serviceId));

            // Remove version entries (simplified - in production might want to keep history)
            var versionKey = ServiceVersionPrefix.Concat(serviceId).Concat(serviceData.Version.ToByteArray());
            Storage.Delete(Storage.CurrentContext, versionKey);

            // Decrement service count
            var count = GetServiceCount();
            Storage.Put(Storage.CurrentContext, ServiceCountKey, count - 1);

            // Emit event
            ServiceDeregistered(serviceId, serviceData.Name);
            
            Runtime.Log($"Service deregistered: {serviceData.Name}");
            return true;
        }
        #endregion

        #region Service Discovery
        /// <summary>
        /// Gets service information by ID.
        /// </summary>
        /// <param name="serviceId">Service identifier</param>
        /// <returns>Service information</returns>
        public static ServiceInfo GetService(UInt160 serviceId)
        {
            var serviceKey = ServicePrefix.Concat(serviceId);
            var serviceDataBytes = Storage.Get(Storage.CurrentContext, serviceKey);
            if (serviceDataBytes == null)
                return null;

            return (ServiceInfo)StdLib.Deserialize(serviceDataBytes);
        }

        /// <summary>
        /// Gets service information by name.
        /// </summary>
        /// <param name="name">Service name</param>
        /// <returns>Service information</returns>
        public static ServiceInfo GetServiceByName(string name)
        {
            var nameKey = ServiceByNamePrefix.Concat(name.ToByteArray());
            var serviceIdBytes = Storage.Get(Storage.CurrentContext, nameKey);
            if (serviceIdBytes == null)
                return null;

            var serviceId = (UInt160)serviceIdBytes;
            return GetService(serviceId);
        }

        /// <summary>
        /// Checks if a service is active.
        /// </summary>
        /// <param name="serviceId">Service identifier</param>
        /// <returns>True if service is active</returns>
        public static bool IsServiceActive(UInt160 serviceId)
        {
            var statusKey = ServiceStatusPrefix.Concat(serviceId);
            var statusBytes = Storage.Get(Storage.CurrentContext, statusKey);
            if (statusBytes == null)
                return false;

            return statusBytes[0] == STATUS_ACTIVE;
        }

        /// <summary>
        /// Gets the total number of registered services.
        /// </summary>
        /// <returns>Service count</returns>
        public static int GetServiceCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, ServiceCountKey);
            return countBytes?.ToInteger() ?? 0;
        }

        /// <summary>
        /// Gets service metadata.
        /// </summary>
        /// <param name="serviceId">Service identifier</param>
        /// <returns>Metadata as JSON string</returns>
        public static string GetServiceMetadata(UInt160 serviceId)
        {
            var metadataKey = ServiceMetadataPrefix.Concat(serviceId);
            var metadataBytes = Storage.Get(Storage.CurrentContext, metadataKey);
            return metadataBytes?.ToByteString() ?? "";
        }
        #endregion

        #region Access Control
        /// <summary>
        /// Grants access role to an address.
        /// </summary>
        /// <param name="target">Target address</param>
        /// <param name="role">Role to grant</param>
        /// <returns>True if successful</returns>
        public static bool GrantAccess(UInt160 caller, UInt160 target, string role)
        {
            // Only admin can grant access
            if (!HasAccess(caller, ROLE_ADMIN))
            {
                throw new InvalidOperationException("Only admin can grant access");
            }

            var accessKey = AccessControlPrefix.Concat(target).Concat(role.ToByteArray());
            Storage.Put(Storage.CurrentContext, accessKey, true);

            AccessGranted(caller, target, role);
            Runtime.Log($"Access granted: {target} role {role}");
            return true;
        }

        /// <summary>
        /// Revokes access role from an address.
        /// </summary>
        /// <param name="target">Target address</param>
        /// <param name="role">Role to revoke</param>
        /// <returns>True if successful</returns>
        public static bool RevokeAccess(UInt160 target, string role)
        {
            // Only admin can revoke access
            if (!HasAccess(Runtime.CallingScriptHash, ROLE_ADMIN))
            {
                throw new InvalidOperationException("Only admin can revoke access");
            }

            var accessKey = AccessControlPrefix.Concat(target).Concat(role.ToByteArray());
            Storage.Delete(Storage.CurrentContext, accessKey);

            AccessRevoked(Runtime.CallingScriptHash, target, role);
            Runtime.Log($"Access revoked: {target} role {role}");
            return true;
        }

        /// <summary>
        /// Checks if an address has a specific role.
        /// </summary>
        /// <param name="address">Address to check</param>
        /// <param name="role">Role to check</param>
        /// <returns>True if address has the role</returns>
        public static bool HasAccess(UInt160 address, string role)
        {
            var accessKey = AccessControlPrefix.Concat(address).Concat(role.ToByteArray());
            var accessBytes = Storage.Get(Storage.CurrentContext, accessKey);
            return accessBytes != null;
        }
        #endregion

        #region Owner Management
        /// <summary>
        /// Gets the current owner of the contract.
        /// </summary>
        /// <returns>Owner address</returns>
        public static UInt160 GetOwner()
        {
            return (UInt160)Storage.Get(Storage.CurrentContext, OwnerKey);
        }

        /// <summary>
        /// Transfers ownership to a new address.
        /// </summary>
        /// <param name="newOwner">New owner address</param>
        /// <returns>True if successful</returns>
        public static bool TransferOwnership(UInt160 newOwner)
        {
            var currentOwner = GetOwner();
            if (!Runtime.CheckWitness(currentOwner))
            {
                throw new InvalidOperationException("Only current owner can transfer ownership");
            }

            if (newOwner is null || newOwner.IsZero)
                throw new ArgumentException("Invalid new owner address");

            // Revoke admin access from old owner
            RevokeAccess(currentOwner, ROLE_ADMIN);
            
            // Grant admin access to new owner
            GrantAccess(currentOwner, newOwner, ROLE_ADMIN);
            
            // Update owner
            Storage.Put(Storage.CurrentContext, OwnerKey, newOwner);

            OwnershipTransferred(currentOwner, newOwner);
            Runtime.Log($"Ownership transferred from {currentOwner} to {newOwner}");
            return true;
        }
        #endregion

        #region Service Dependencies
        /// <summary>
        /// Adds a dependency relationship between services.
        /// </summary>
        /// <param name="serviceId">Service that depends on another</param>
        /// <param name="dependencyId">Service that is depended upon</param>
        /// <returns>True if successful</returns>
        public static bool AddServiceDependency(UInt160 serviceId, UInt160 dependencyId)
        {
            if (!HasAccess(Runtime.CallingScriptHash, ROLE_SERVICE_MANAGER) && 
                !HasAccess(Runtime.CallingScriptHash, ROLE_ADMIN))
            {
                throw new InvalidOperationException("Insufficient permissions");
            }

            // Verify both services exist
            if (GetService(serviceId) == null)
                throw new InvalidOperationException("Service not found");
            if (GetService(dependencyId) == null)
                throw new InvalidOperationException("Dependency service not found");

            var dependencyKey = ServiceDependencyPrefix.Concat(serviceId).Concat(dependencyId);
            Storage.Put(Storage.CurrentContext, dependencyKey, true);

            Runtime.Log($"Dependency added: {serviceId} depends on {dependencyId}");
            return true;
        }

        /// <summary>
        /// Checks if a service depends on another service.
        /// </summary>
        /// <param name="serviceId">Service to check</param>
        /// <param name="dependencyId">Potential dependency</param>
        /// <returns>True if dependency exists</returns>
        public static bool HasServiceDependency(UInt160 serviceId, UInt160 dependencyId)
        {
            var dependencyKey = ServiceDependencyPrefix.Concat(serviceId).Concat(dependencyId);
            return Storage.Get(Storage.CurrentContext, dependencyKey) != null;
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Validates that a service exists and is active.
        /// </summary>
        /// <param name="serviceId">Service identifier</param>
        /// <returns>True if service exists and is active</returns>
        public static bool ValidateService(UInt160 serviceId)
        {
            var service = GetService(serviceId);
            return service != null && service.Status == STATUS_ACTIVE;
        }

        /// <summary>
        /// Gets the contract version.
        /// </summary>
        /// <returns>Version string</returns>
        public static string GetVersion()
        {
            return "1.0.0";
        }
        #endregion
    }

    /// <summary>
    /// Service information structure.
    /// </summary>
    public class ServiceInfo
    {
        public UInt160 Id;
        public string Name;
        public string Version;
        public UInt160 ContractAddress;
        public string Endpoint;
        public byte Status;
        public ulong RegisteredAt;
        public ulong UpdatedAt;
        public UInt160 RegisteredBy;
    }
}