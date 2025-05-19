using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NeoServiceLayer.Examples
{
    [DisplayName("EventSubscription")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Email", "info@neoservicelayer.io")]
    [ManifestExtra("Description", "Event Subscription Example for Neo Service Layer")]
    public class EventSubscription : SmartContract
    {
        // Events
        [DisplayName("SubscriptionCreated")]
        public static event Action<UInt160, string, UInt160, string, string> OnSubscriptionCreated;

        [DisplayName("SubscriptionCancelled")]
        public static event Action<string> OnSubscriptionCancelled;

        [DisplayName("ActionExecuted")]
        public static event Action<string, string, string> OnActionExecuted;

        // Storage keys
        private static readonly byte[] OwnerKey = "owner".ToByteArray();
        private static readonly byte[] SubscriptionPrefix = "subscription".ToByteArray();
        private static readonly byte[] ServiceLayerKey = "serviceLayer".ToByteArray();
        private static readonly byte[] FeeKey = "fee".ToByteArray();

        // Custom properties
        [DisplayName("ServiceLayerAddress")]
        public static UInt160 ServiceLayerAddress()
        {
            return (UInt160)Storage.Get(Storage.CurrentContext, ServiceLayerKey);
        }

        [DisplayName("Fee")]
        public static BigInteger Fee()
        {
            return (BigInteger)Storage.Get(Storage.CurrentContext, FeeKey);
        }

        // Constructor
        public static void _deploy(object data, bool update)
        {
            if (!update)
            {
                // Set initial owner to the contract deployer
                Storage.Put(Storage.CurrentContext, OwnerKey, Runtime.CallingScriptHash);
                
                // Set initial fee to 0.1 GAS
                Storage.Put(Storage.CurrentContext, FeeKey, 10_000_000);
            }
        }

        // Initialize the contract with the Neo Service Layer address
        public static void Initialize(UInt160 serviceLayerAddress)
        {
            // Only owner can initialize
            if (!IsOwner()) throw new Exception("No authorization");
            
            // Validate service layer address
            if (serviceLayerAddress is null || !serviceLayerAddress.IsValid)
                throw new Exception("Invalid service layer address");
                
            // Store service layer address
            Storage.Put(Storage.CurrentContext, ServiceLayerKey, serviceLayerAddress);
        }

        // Set the fee for subscription creation
        public static void SetFee(BigInteger fee)
        {
            // Only owner can set fee
            if (!IsOwner()) throw new Exception("No authorization");
            
            // Validate fee
            if (fee < 0)
                throw new Exception("Fee cannot be negative");
                
            // Store fee
            Storage.Put(Storage.CurrentContext, FeeKey, fee);
        }

        // Create a new subscription
        public static string CreateSubscription(UInt160 sourceContract, string eventName, string actionType, string actionData)
        {
            // Validate parameters
            if (sourceContract is null || !sourceContract.IsValid)
                throw new Exception("Invalid source contract");
            if (string.IsNullOrEmpty(eventName))
                throw new Exception("Invalid event name");
            if (string.IsNullOrEmpty(actionType))
                throw new Exception("Invalid action type");
                
            // Generate subscription ID
            string subscriptionId = GenerateSubscriptionId();
            
            // Store subscription
            var subscription = new Subscription
            {
                Owner = Runtime.CallingScriptHash,
                SourceContract = sourceContract,
                EventName = eventName,
                ActionType = actionType,
                ActionData = actionData,
                CreatedAt = Runtime.Time,
                Status = SubscriptionStatus.Active
            };
            
            byte[] subscriptionKey = SubscriptionPrefix.Concat(subscriptionId.ToByteArray());
            Storage.Put(Storage.CurrentContext, subscriptionKey, StdLib.Serialize(subscription));
            
            // Collect fee
            BigInteger fee = Fee();
            if (fee > 0)
            {
                // Transfer GAS from caller to contract
                if (!GAS.Transfer(Runtime.CallingScriptHash, Runtime.ExecutingScriptHash, fee))
                    throw new Exception("Failed to transfer fee");
            }
            
            // Register subscription with Neo Service Layer
            bool success = (bool)Contract.Call(
                ServiceLayerAddress(),
                "registerEventSubscription",
                CallFlags.All,
                new object[] { subscriptionId, sourceContract.ToString(), eventName, actionType, actionData }
            );
            
            if (!success)
                throw new Exception("Failed to register subscription with Neo Service Layer");
                
            // Notify subscription created event
            OnSubscriptionCreated(Runtime.CallingScriptHash, subscriptionId, sourceContract, eventName, actionType);
            
            return subscriptionId;
        }

        // Cancel a subscription
        public static bool CancelSubscription(string subscriptionId)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(subscriptionId))
                throw new Exception("Invalid subscription ID");
                
            // Get subscription
            byte[] subscriptionKey = SubscriptionPrefix.Concat(subscriptionId.ToByteArray());
            byte[] subscriptionBytes = Storage.Get(Storage.CurrentContext, subscriptionKey);
            if (subscriptionBytes is null)
                throw new Exception("Subscription not found");
                
            // Deserialize subscription
            Subscription subscription = (Subscription)StdLib.Deserialize(subscriptionBytes);
            
            // Check if caller is the owner
            if (!Runtime.CheckWitness(subscription.Owner) && !IsOwner())
                throw new Exception("Only the subscription owner or contract owner can cancel the subscription");
                
            // Check if subscription is already cancelled
            if (subscription.Status == SubscriptionStatus.Cancelled)
                return true;
                
            // Update subscription status
            subscription.Status = SubscriptionStatus.Cancelled;
            Storage.Put(Storage.CurrentContext, subscriptionKey, StdLib.Serialize(subscription));
            
            // Unregister subscription with Neo Service Layer
            bool success = (bool)Contract.Call(
                ServiceLayerAddress(),
                "unregisterEventSubscription",
                CallFlags.All,
                new object[] { subscriptionId }
            );
            
            // Notify subscription cancelled event
            OnSubscriptionCancelled(subscriptionId);
            
            return success;
        }

        // Execute an action based on an event
        public static bool ExecuteAction(string subscriptionId, string eventData)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(subscriptionId))
                throw new Exception("Invalid subscription ID");
            if (string.IsNullOrEmpty(eventData))
                throw new Exception("Invalid event data");
                
            // Only Neo Service Layer can execute actions
            if (!Runtime.CheckWitness(ServiceLayerAddress()))
                throw new Exception("Only the Neo Service Layer can execute actions");
                
            // Get subscription
            byte[] subscriptionKey = SubscriptionPrefix.Concat(subscriptionId.ToByteArray());
            byte[] subscriptionBytes = Storage.Get(Storage.CurrentContext, subscriptionKey);
            if (subscriptionBytes is null)
                throw new Exception("Subscription not found");
                
            // Deserialize subscription
            Subscription subscription = (Subscription)StdLib.Deserialize(subscriptionBytes);
            
            // Check if subscription is active
            if (subscription.Status != SubscriptionStatus.Active)
                throw new Exception("Subscription is not active");
                
            // Execute action based on action type
            bool success = false;
            
            if (subscription.ActionType == "ContractCall")
            {
                // Parse action data
                string[] parts = subscription.ActionData.Split(',');
                if (parts.Length < 2)
                    throw new Exception("Invalid action data for ContractCall");
                    
                string targetContractHash = parts[0];
                string operation = parts[1];
                
                // Call target contract
                UInt160 targetContract = UInt160.Parse(targetContractHash);
                success = (bool)Contract.Call(targetContract, operation, CallFlags.All, new object[] { eventData });
            }
            else if (subscription.ActionType == "Notification")
            {
                // Just notify the event, no actual action needed
                success = true;
            }
            else
            {
                throw new Exception("Unsupported action type");
            }
            
            // Notify action executed event
            OnActionExecuted(subscriptionId, subscription.ActionType, eventData);
            
            return success;
        }

        // Get subscription information
        public static SubscriptionInfo GetSubscription(string subscriptionId)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(subscriptionId))
                throw new Exception("Invalid subscription ID");
                
            // Get subscription
            byte[] subscriptionKey = SubscriptionPrefix.Concat(subscriptionId.ToByteArray());
            byte[] subscriptionBytes = Storage.Get(Storage.CurrentContext, subscriptionKey);
            if (subscriptionBytes is null)
                throw new Exception("Subscription not found");
                
            // Deserialize subscription
            Subscription subscription = (Subscription)StdLib.Deserialize(subscriptionBytes);
            
            // Create subscription info
            return new SubscriptionInfo
            {
                Owner = subscription.Owner,
                SourceContract = subscription.SourceContract,
                EventName = subscription.EventName,
                ActionType = subscription.ActionType,
                ActionData = subscription.ActionData,
                CreatedAt = subscription.CreatedAt,
                Status = (byte)subscription.Status
            };
        }

        // Withdraw fees
        public static bool WithdrawFees(UInt160 to)
        {
            // Only owner can withdraw fees
            if (!IsOwner()) throw new Exception("No authorization");
            
            // Validate parameters
            if (to is null || !to.IsValid)
                throw new Exception("Invalid to address");
                
            // Get contract balance
            BigInteger balance = GAS.BalanceOf(Runtime.ExecutingScriptHash);
            if (balance <= 0)
                return false;
                
            // Transfer GAS to owner
            return GAS.Transfer(Runtime.ExecutingScriptHash, to, balance);
        }

        // Helper methods
        private static bool IsOwner()
        {
            UInt160 owner = (UInt160)Storage.Get(Storage.CurrentContext, OwnerKey);
            return Runtime.CheckWitness(owner);
        }

        private static string GenerateSubscriptionId()
        {
            // Generate a unique subscription ID based on tx hash and timestamp
            return $"{Runtime.GetTransaction().Hash}-{Runtime.Time}";
        }

        // Data structures
        public enum SubscriptionStatus : byte
        {
            Active = 0,
            Cancelled = 1
        }

        public class Subscription
        {
            public UInt160 Owner;
            public UInt160 SourceContract;
            public string EventName;
            public string ActionType;
            public string ActionData;
            public BigInteger CreatedAt;
            public SubscriptionStatus Status;
        }

        public class SubscriptionInfo
        {
            public UInt160 Owner;
            public UInt160 SourceContract;
            public string EventName;
            public string ActionType;
            public string ActionData;
            public BigInteger CreatedAt;
            public byte Status;
        }
    }
}
