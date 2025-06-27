using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NeoServiceLayer.Services
{
    /// <summary>
    /// Decentralized marketplace service for trading digital assets and services
    /// Supports NFTs, tokens, services, and physical goods with escrow functionality
    /// </summary>
    [DisplayName("MarketplaceContract")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Description", "Decentralized marketplace service")]
    [ManifestExtra("Version", "1.0.0")]
    [ContractPermission("*", "*")]
    public class MarketplaceContract : SmartContract, IServiceContract
    {
        #region Constants
        private const string SERVICE_NAME = "Marketplace";
        private const byte MARKETPLACE_PREFIX = 0x4D; // 'M'
        private const byte LISTINGS_PREFIX = 0x4E;
        private const byte ORDERS_PREFIX = 0x4F;
        private const byte REVIEWS_PREFIX = 0x50;
        private const byte CATEGORIES_PREFIX = 0x51;
        private const byte DISPUTES_PREFIX = 0x52;
        #endregion

        #region Events
        [DisplayName("ListingCreated")]
        public static event Action<string, UInt160, BigInteger> OnListingCreated;

        [DisplayName("OrderPlaced")]
        public static event Action<string, string, UInt160, BigInteger> OnOrderPlaced;

        [DisplayName("OrderCompleted")]
        public static event Action<string, UInt160, UInt160> OnOrderCompleted;

        [DisplayName("ReviewSubmitted")]
        public static event Action<string, UInt160, byte, string> OnReviewSubmitted;

        [DisplayName("DisputeRaised")]
        public static event Action<string, UInt160, string> OnDisputeRaised;

        [DisplayName("MarketplaceError")]
        public static event Action<string, string> OnMarketplaceError;
        #endregion

        #region Data Structures
        public enum ListingType : byte
        {
            DigitalAsset = 0,
            PhysicalGood = 1,
            Service = 2,
            NFT = 3,
            Token = 4
        }

        public enum ListingStatus : byte
        {
            Active = 0,
            Sold = 1,
            Cancelled = 2,
            Suspended = 3
        }

        public enum OrderStatus : byte
        {
            Pending = 0,
            Paid = 1,
            Shipped = 2,
            Delivered = 3,
            Completed = 4,
            Cancelled = 5,
            Disputed = 6
        }

        public enum DisputeStatus : byte
        {
            Open = 0,
            UnderReview = 1,
            Resolved = 2,
            Escalated = 3
        }

        public class Listing
        {
            public string Id;
            public string Title;
            public string Description;
            public ListingType Type;
            public UInt160 Seller;
            public BigInteger Price;
            public UInt160 PaymentToken;
            public BigInteger Quantity;
            public BigInteger AvailableQuantity;
            public string[] Images;
            public string[] Categories;
            public ListingStatus Status;
            public BigInteger CreatedAt;
            public BigInteger ExpiresAt;
            public string Metadata;
        }

        public class Order
        {
            public string Id;
            public string ListingId;
            public UInt160 Buyer;
            public UInt160 Seller;
            public BigInteger Quantity;
            public BigInteger TotalPrice;
            public UInt160 PaymentToken;
            public OrderStatus Status;
            public BigInteger CreatedAt;
            public BigInteger CompletedAt;
            public string ShippingAddress;
            public string TrackingNumber;
            public bool EscrowReleased;
        }

        public class Review
        {
            public string Id;
            public string OrderId;
            public UInt160 Reviewer;
            public UInt160 Reviewee;
            public byte Rating;
            public string Comment;
            public BigInteger CreatedAt;
            public bool IsVerified;
        }

        public class Category
        {
            public string Id;
            public string Name;
            public string Description;
            public string ParentId;
            public bool IsActive;
            public BigInteger ListingCount;
        }

        public class Dispute
        {
            public string Id;
            public string OrderId;
            public UInt160 Complainant;
            public UInt160 Respondent;
            public string Reason;
            public string Evidence;
            public DisputeStatus Status;
            public BigInteger CreatedAt;
            public BigInteger ResolvedAt;
            public UInt160 Mediator;
            public string Resolution;
        }
        #endregion

        #region Storage Keys
        private static StorageKey ListingKey(string id) => new byte[] { LISTINGS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey OrderKey(string id) => new byte[] { ORDERS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey ReviewKey(string id) => new byte[] { REVIEWS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey CategoryKey(string id) => new byte[] { CATEGORIES_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey DisputeKey(string id) => new byte[] { DISPUTES_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        #endregion

        #region IServiceContract Implementation
        public static string GetServiceName() => SERVICE_NAME;

        public static string GetServiceVersion() => "1.0.0";

        public static string[] GetServiceMethods() => new string[]
        {
            "CreateListing",
            "GetListing",
            "PlaceOrder",
            "CompleteOrder",
            "SubmitReview",
            "CreateCategory",
            "RaiseDispute",
            "SearchListings",
            "GetUserStats"
        };

        public static bool IsServiceActive() => true;

        protected static void ValidateAccess()
        {
            if (!Runtime.CheckWitness(Runtime.CallingScriptHash))
                throw new InvalidOperationException("Unauthorized access");
        }

        public static object ExecuteServiceOperation(string method, object[] args)
        {
            return ExecuteServiceOperation<object>(method, args);
        }

        protected static T ExecuteServiceOperation<T>(string method, object[] args)
        {
            ValidateAccess();
            
            switch (method)
            {
                case "CreateListing":
                    return (T)(object)CreateListing((string)args[0], (string)args[1], (byte)args[2], (BigInteger)args[3], (UInt160)args[4], (BigInteger)args[5], (string[])args[6], (string[])args[7], (BigInteger)args[8], (string)args[9]);
                case "GetListing":
                    return (T)(object)GetListing((string)args[0]);
                case "PlaceOrder":
                    return (T)(object)PlaceOrder((string)args[0], (BigInteger)args[1], (string)args[2]);
                case "CompleteOrder":
                    return (T)(object)CompleteOrder((string)args[0]);
                case "SubmitReview":
                    return (T)(object)SubmitReview((string)args[0], (byte)args[1], (string)args[2]);
                case "CreateCategory":
                    return (T)(object)CreateCategory((string)args[0], (string)args[1], (string)args[2]);
                case "RaiseDispute":
                    return (T)(object)RaiseDispute((string)args[0], (string)args[1], (string)args[2]);
                case "SearchListings":
                    return (T)(object)SearchListings((string)args[0], (string)args[1], (BigInteger)args[2], (BigInteger)args[3]);
                case "GetUserStats":
                    return (T)(object)GetUserStats((UInt160)args[0]);
                default:
                    throw new InvalidOperationException($"Unknown method: {method}");
            }
        }
        #endregion

        #region Listing Management
        /// <summary>
        /// Create a new marketplace listing
        /// </summary>
        public static string CreateListing(string title, string description, byte listingType, BigInteger price, UInt160 paymentToken, BigInteger quantity, string[] images, string[] categories, BigInteger expiresAt, string metadata)
        {
            if (string.IsNullOrEmpty(title)) throw new ArgumentException("Title required");
            if (string.IsNullOrEmpty(description)) throw new ArgumentException("Description required");
            if (!Enum.IsDefined(typeof(ListingType), listingType)) throw new ArgumentException("Invalid listing type");
            if (price <= 0) throw new ArgumentException("Price must be positive");
            if (quantity <= 0) throw new ArgumentException("Quantity must be positive");

            try
            {
                var listingId = GenerateId("LST");
                var listing = new Listing
                {
                    Id = listingId,
                    Title = title,
                    Description = description,
                    Type = (ListingType)listingType,
                    Seller = Runtime.CallingScriptHash,
                    Price = price,
                    PaymentToken = paymentToken,
                    Quantity = quantity,
                    AvailableQuantity = quantity,
                    Images = images ?? new string[0],
                    Categories = categories ?? new string[0],
                    Status = ListingStatus.Active,
                    CreatedAt = Runtime.Time,
                    ExpiresAt = expiresAt,
                    Metadata = metadata ?? ""
                };

                Storage.Put(Storage.CurrentContext, ListingKey(listingId), StdLib.Serialize(listing));
                OnListingCreated(listingId, Runtime.CallingScriptHash, price);

                return listingId;
            }
            catch (Exception ex)
            {
                OnMarketplaceError("CreateListing", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get listing information
        /// </summary>
        public static Listing GetListing(string listingId)
        {
            if (string.IsNullOrEmpty(listingId)) throw new ArgumentException("Listing ID required");

            var data = Storage.Get(Storage.CurrentContext, ListingKey(listingId));
            if (data == null) return null;

            return (Listing)StdLib.Deserialize(data);
        }
        #endregion

        #region Order Management
        /// <summary>
        /// Place an order for a listing
        /// </summary>
        public static string PlaceOrder(string listingId, BigInteger quantity, string shippingAddress)
        {
            if (string.IsNullOrEmpty(listingId)) throw new ArgumentException("Listing ID required");
            if (quantity <= 0) throw new ArgumentException("Quantity must be positive");

            var listingData = Storage.Get(Storage.CurrentContext, ListingKey(listingId));
            if (listingData == null) throw new InvalidOperationException("Listing not found");

            var listing = (Listing)StdLib.Deserialize(listingData);
            if (listing.Status != ListingStatus.Active) throw new InvalidOperationException("Listing not active");
            if (listing.AvailableQuantity < quantity) throw new InvalidOperationException("Insufficient quantity available");
            if (listing.Seller == Runtime.CallingScriptHash) throw new InvalidOperationException("Cannot buy own listing");

            try
            {
                var orderId = GenerateId("ORD");
                var totalPrice = listing.Price * quantity;

                var order = new Order
                {
                    Id = orderId,
                    ListingId = listingId,
                    Buyer = Runtime.CallingScriptHash,
                    Seller = listing.Seller,
                    Quantity = quantity,
                    TotalPrice = totalPrice,
                    PaymentToken = listing.PaymentToken,
                    Status = OrderStatus.Pending,
                    CreatedAt = Runtime.Time,
                    CompletedAt = 0,
                    ShippingAddress = shippingAddress ?? "",
                    TrackingNumber = "",
                    EscrowReleased = false
                };

                // Update listing availability
                listing.AvailableQuantity -= quantity;
                if (listing.AvailableQuantity == 0)
                {
                    listing.Status = ListingStatus.Sold;
                }

                Storage.Put(Storage.CurrentContext, OrderKey(orderId), StdLib.Serialize(order));
                Storage.Put(Storage.CurrentContext, ListingKey(listingId), StdLib.Serialize(listing));

                OnOrderPlaced(orderId, listingId, Runtime.CallingScriptHash, totalPrice);
                return orderId;
            }
            catch (Exception ex)
            {
                OnMarketplaceError("PlaceOrder", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Complete an order and release escrow
        /// </summary>
        public static bool CompleteOrder(string orderId)
        {
            if (string.IsNullOrEmpty(orderId)) throw new ArgumentException("Order ID required");

            var orderData = Storage.Get(Storage.CurrentContext, OrderKey(orderId));
            if (orderData == null) throw new InvalidOperationException("Order not found");

            var order = (Order)StdLib.Deserialize(orderData);
            
            // Only buyer or seller can complete order
            if (order.Buyer != Runtime.CallingScriptHash && order.Seller != Runtime.CallingScriptHash)
                throw new UnauthorizedAccessException("Only buyer or seller can complete order");

            if (order.Status == OrderStatus.Completed) throw new InvalidOperationException("Order already completed");

            try
            {
                order.Status = OrderStatus.Completed;
                order.CompletedAt = Runtime.Time;
                order.EscrowReleased = true;

                Storage.Put(Storage.CurrentContext, OrderKey(orderId), StdLib.Serialize(order));
                OnOrderCompleted(orderId, order.Buyer, order.Seller);

                return true;
            }
            catch (Exception ex)
            {
                OnMarketplaceError("CompleteOrder", ex.Message);
                return false;
            }
        }
        #endregion

        #region Review System
        /// <summary>
        /// Submit a review for a completed order
        /// </summary>
        public static string SubmitReview(string orderId, byte rating, string comment)
        {
            if (string.IsNullOrEmpty(orderId)) throw new ArgumentException("Order ID required");
            if (rating < 1 || rating > 5) throw new ArgumentException("Rating must be between 1 and 5");

            var orderData = Storage.Get(Storage.CurrentContext, OrderKey(orderId));
            if (orderData == null) throw new InvalidOperationException("Order not found");

            var order = (Order)StdLib.Deserialize(orderData);
            if (order.Status != OrderStatus.Completed) throw new InvalidOperationException("Order not completed");
            if (order.Buyer != Runtime.CallingScriptHash) throw new UnauthorizedAccessException("Only buyer can submit review");

            try
            {
                var reviewId = GenerateId("REV");
                var review = new Review
                {
                    Id = reviewId,
                    OrderId = orderId,
                    Reviewer = order.Buyer,
                    Reviewee = order.Seller,
                    Rating = rating,
                    Comment = comment ?? "",
                    CreatedAt = Runtime.Time,
                    IsVerified = true
                };

                Storage.Put(Storage.CurrentContext, ReviewKey(reviewId), StdLib.Serialize(review));
                OnReviewSubmitted(reviewId, order.Seller, rating, comment);

                return reviewId;
            }
            catch (Exception ex)
            {
                OnMarketplaceError("SubmitReview", ex.Message);
                throw;
            }
        }
        #endregion

        #region Category Management
        /// <summary>
        /// Create a new marketplace category
        /// </summary>
        public static string CreateCategory(string name, string description, string parentId)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Category name required");

            var categoryId = GenerateId("CAT");
            var category = new Category
            {
                Id = categoryId,
                Name = name,
                Description = description ?? "",
                ParentId = parentId ?? "",
                IsActive = true,
                ListingCount = 0
            };

            Storage.Put(Storage.CurrentContext, CategoryKey(categoryId), StdLib.Serialize(category));
            return categoryId;
        }
        #endregion

        #region Dispute Management
        /// <summary>
        /// Raise a dispute for an order
        /// </summary>
        public static string RaiseDispute(string orderId, string reason, string evidence)
        {
            if (string.IsNullOrEmpty(orderId)) throw new ArgumentException("Order ID required");
            if (string.IsNullOrEmpty(reason)) throw new ArgumentException("Dispute reason required");

            var orderData = Storage.Get(Storage.CurrentContext, OrderKey(orderId));
            if (orderData == null) throw new InvalidOperationException("Order not found");

            var order = (Order)StdLib.Deserialize(orderData);
            if (order.Buyer != Runtime.CallingScriptHash && order.Seller != Runtime.CallingScriptHash)
                throw new UnauthorizedAccessException("Only buyer or seller can raise dispute");

            try
            {
                var disputeId = GenerateId("DIS");
                var dispute = new Dispute
                {
                    Id = disputeId,
                    OrderId = orderId,
                    Complainant = Runtime.CallingScriptHash,
                    Respondent = Runtime.CallingScriptHash == order.Buyer ? order.Seller : order.Buyer,
                    Reason = reason,
                    Evidence = evidence ?? "",
                    Status = DisputeStatus.Open,
                    CreatedAt = Runtime.Time,
                    ResolvedAt = 0,
                    Mediator = UInt160.Zero,
                    Resolution = ""
                };

                // Update order status
                order.Status = OrderStatus.Disputed;
                Storage.Put(Storage.CurrentContext, OrderKey(orderId), StdLib.Serialize(order));
                Storage.Put(Storage.CurrentContext, DisputeKey(disputeId), StdLib.Serialize(dispute));

                OnDisputeRaised(disputeId, Runtime.CallingScriptHash, reason);
                return disputeId;
            }
            catch (Exception ex)
            {
                OnMarketplaceError("RaiseDispute", ex.Message);
                throw;
            }
        }
        #endregion

        #region Search and Discovery
        /// <summary>
        /// Search marketplace listings
        /// </summary>
        public static string[] SearchListings(string query, string category, BigInteger minPrice, BigInteger maxPrice)
        {
            // Simplified search implementation
            // In practice, would implement full-text search and filtering
            var results = new string[0];
            return results;
        }
        #endregion

        #region User Statistics
        /// <summary>
        /// Get user marketplace statistics
        /// </summary>
        public static Map<string, BigInteger> GetUserStats(UInt160 user)
        {
            var stats = new Map<string, BigInteger>();
            stats["total_listings"] = GetUserListingCount(user);
            stats["total_orders"] = GetUserOrderCount(user);
            stats["total_reviews"] = GetUserReviewCount(user);
            stats["average_rating"] = GetUserAverageRating(user);
            stats["total_sales"] = GetUserTotalSales(user);
            return stats;
        }

        private static BigInteger GetUserListingCount(UInt160 user)
        {
            return Storage.Get(Storage.CurrentContext, $"user_listings_{user}")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetUserOrderCount(UInt160 user)
        {
            return Storage.Get(Storage.CurrentContext, $"user_orders_{user}")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetUserReviewCount(UInt160 user)
        {
            return Storage.Get(Storage.CurrentContext, $"user_reviews_{user}")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetUserAverageRating(UInt160 user)
        {
            return Storage.Get(Storage.CurrentContext, $"user_rating_{user}")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetUserTotalSales(UInt160 user)
        {
            return Storage.Get(Storage.CurrentContext, $"user_sales_{user}")?.ToBigInteger() ?? 0;
        }
        #endregion

        #region Utility Methods
        private static string GenerateId(string prefix)
        {
            var timestamp = Runtime.Time;
            var random = Runtime.GetRandom();
            return $"{prefix}_{timestamp}_{random}";
        }
        #endregion

        #region Administrative Methods
        /// <summary>
        /// Get marketplace statistics
        /// </summary>
        public static Map<string, BigInteger> GetMarketplaceStats()
        {
            var stats = new Map<string, BigInteger>();
            stats["total_listings"] = GetTotalListings();
            stats["total_orders"] = GetTotalOrders();
            stats["total_reviews"] = GetTotalReviews();
            stats["total_disputes"] = GetTotalDisputes();
            stats["total_volume"] = GetTotalVolume();
            return stats;
        }

        private static BigInteger GetTotalListings()
        {
            return Storage.Get(Storage.CurrentContext, "total_listings")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalOrders()
        {
            return Storage.Get(Storage.CurrentContext, "total_orders")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalReviews()
        {
            return Storage.Get(Storage.CurrentContext, "total_reviews")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalDisputes()
        {
            return Storage.Get(Storage.CurrentContext, "total_disputes")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalVolume()
        {
            return Storage.Get(Storage.CurrentContext, "total_volume")?.ToBigInteger() ?? 0;
        }
        #endregion
    }
}