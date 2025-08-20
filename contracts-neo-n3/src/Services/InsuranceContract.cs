using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Services
{
    /// <summary>
    /// Decentralized insurance service providing parametric and traditional insurance products
    /// Supports automated claims processing, risk assessment, and premium calculations
    /// </summary>
    [DisplayName("InsuranceContract")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Description", "Decentralized insurance service")]
    [ManifestExtra("Version", "1.0.0")]
    [ContractPermission("*", "*")]
    public class InsuranceContract : SmartContract, IServiceContract
    {
        #region Constants
        private const string SERVICE_NAME = "Insurance";
        private const byte INSURANCE_PREFIX = 0x49; // 'I'
        private const byte POLICIES_PREFIX = 0x50;
        private const byte CLAIMS_PREFIX = 0x43;
        private const byte POOLS_PREFIX = 0x52;
        private const byte ASSESSMENTS_PREFIX = 0x41;
        private const byte PREMIUMS_PREFIX = 0x50;
        #endregion

        #region Events
        [DisplayName("PolicyCreated")]
        public static event Action<string, UInt160, BigInteger, BigInteger> OnPolicyCreated;

        [DisplayName("ClaimSubmitted")]
        public static event Action<string, string, UInt160, BigInteger> OnClaimSubmitted;

        [DisplayName("ClaimProcessed")]
        public static event Action<string, byte, BigInteger> OnClaimProcessed;

        [DisplayName("PremiumPaid")]
        public static event Action<string, UInt160, BigInteger> OnPremiumPaid;

        [DisplayName("PayoutMade")]
        public static event Action<string, UInt160, BigInteger> OnPayoutMade;

        [DisplayName("InsuranceError")]
        public static event Action<string, string> OnInsuranceError;
        #endregion

        #region Data Structures
        public enum InsuranceType : byte
        {
            Life = 0,
            Health = 1,
            Property = 2,
            Travel = 3,
            Crop = 4,
            Weather = 5,
            Cyber = 6,
            Parametric = 7
        }

        public enum PolicyStatus : byte
        {
            Active = 0,
            Expired = 1,
            Cancelled = 2,
            Suspended = 3,
            Claimed = 4
        }

        public enum ClaimStatus : byte
        {
            Submitted = 0,
            UnderReview = 1,
            Approved = 2,
            Rejected = 3,
            Paid = 4,
            Disputed = 5
        }

        public enum RiskLevel : byte
        {
            Low = 0,
            Medium = 1,
            High = 2,
            Critical = 3
        }

        public class Policy
        {
            public string Id;
            public string Name;
            public InsuranceType Type;
            public UInt160 Policyholder;
            public BigInteger CoverageAmount;
            public BigInteger Premium;
            public BigInteger Deductible;
            public PolicyStatus Status;
            public BigInteger StartDate;
            public BigInteger EndDate;
            public string[] CoveredRisks;
            public string Terms;
            public string Metadata;
            public BigInteger LastPremiumPaid;
            public BigInteger TotalPremiumsPaid;
        }

        public class Claim
        {
            public string Id;
            public string PolicyId;
            public UInt160 Claimant;
            public BigInteger ClaimAmount;
            public string Description;
            public string[] Evidence;
            public ClaimStatus Status;
            public BigInteger SubmittedAt;
            public BigInteger ProcessedAt;
            public BigInteger ApprovedAmount;
            public string RejectionReason;
            public UInt160 Assessor;
            public string AssessmentNotes;
        }

        public class RiskPool
        {
            public string Id;
            public string Name;
            public InsuranceType Type;
            public BigInteger TotalFunds;
            public BigInteger ReservedFunds;
            public BigInteger TotalPolicies;
            public BigInteger TotalClaims;
            public BigInteger TotalPayouts;
            public RiskLevel RiskLevel;
            public BigInteger MinimumPremium;
            public BigInteger MaximumCoverage;
            public bool IsActive;
        }

        public class RiskAssessment
        {
            public string Id;
            public UInt160 Subject;
            public InsuranceType Type;
            public RiskLevel Level;
            public BigInteger Score;
            public string[] Factors;
            public BigInteger AssessedAt;
            public UInt160 Assessor;
            public BigInteger ValidUntil;
            public string Notes;
        }

        public class Premium
        {
            public string Id;
            public string PolicyId;
            public UInt160 Payer;
            public BigInteger Amount;
            public BigInteger DueDate;
            public BigInteger PaidDate;
            public bool IsPaid;
            public UInt160 PaymentToken;
            public string TransactionHash;
        }
        #endregion

        #region Storage Keys
        private static StorageKey PolicyKey(string id) => new byte[] { POLICIES_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey ClaimKey(string id) => new byte[] { CLAIMS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey PoolKey(string id) => new byte[] { POOLS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey AssessmentKey(string id) => new byte[] { ASSESSMENTS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey PremiumKey(string id) => new byte[] { PREMIUMS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        #endregion

        #region IServiceContract Implementation
        public static string GetServiceName() => SERVICE_NAME;

        public static string GetServiceVersion() => "1.0.0";

        public static string[] GetServiceMethods() => new string[]
        {
            "CreatePolicy",
            "GetPolicy",
            "SubmitClaim",
            "ProcessClaim",
            "PayPremium",
            "AssessRisk",
            "CreateRiskPool",
            "CalculatePremium",
            "GetClaimHistory"
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
                case "CreatePolicy":
                    return (T)(object)CreatePolicy((string)args[0], (byte)args[1], (BigInteger)args[2], (BigInteger)args[3], (BigInteger)args[4], (BigInteger)args[5], (string[])args[6], (string)args[7]);
                case "GetPolicy":
                    return (T)(object)GetPolicy((string)args[0]);
                case "SubmitClaim":
                    return (T)(object)SubmitClaim((string)args[0], (BigInteger)args[1], (string)args[2], (string[])args[3]);
                case "ProcessClaim":
                    return (T)(object)ProcessClaim((string)args[0], (bool)args[1], (BigInteger)args[2], (string)args[3]);
                case "PayPremium":
                    return (T)(object)PayPremium((string)args[0], (BigInteger)args[1]);
                case "AssessRisk":
                    return (T)(object)AssessRisk((UInt160)args[0], (byte)args[1], (string[])args[2]);
                case "CreateRiskPool":
                    return (T)(object)CreateRiskPool((string)args[0], (byte)args[1], (BigInteger)args[2], (BigInteger)args[3]);
                case "CalculatePremium":
                    return (T)(object)CalculatePremium((byte)args[0], (BigInteger)args[1], (byte)args[2], (BigInteger)args[3]);
                case "GetClaimHistory":
                    return (T)(object)GetClaimHistory((UInt160)args[0]);
                default:
                    throw new InvalidOperationException($"Unknown method: {method}");
            }
        }
        #endregion

        #region Policy Management
        /// <summary>
        /// Create a new insurance policy
        /// </summary>
        public static string CreatePolicy(string name, byte insuranceType, BigInteger coverageAmount, BigInteger premium, BigInteger deductible, BigInteger duration, string[] coveredRisks, string terms)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Policy name required");
            if (!Enum.IsDefined(typeof(InsuranceType), insuranceType)) throw new ArgumentException("Invalid insurance type");
            if (coverageAmount <= 0) throw new ArgumentException("Coverage amount must be positive");
            if (premium <= 0) throw new ArgumentException("Premium must be positive");
            if (duration <= 0) throw new ArgumentException("Duration must be positive");

            try
            {
                var policyId = GenerateId("POL");
                var startDate = Runtime.Time;
                var endDate = startDate + duration;

                var policy = new Policy
                {
                    Id = policyId,
                    Name = name,
                    Type = (InsuranceType)insuranceType,
                    Policyholder = Runtime.CallingScriptHash,
                    CoverageAmount = coverageAmount,
                    Premium = premium,
                    Deductible = deductible,
                    Status = PolicyStatus.Active,
                    StartDate = startDate,
                    EndDate = endDate,
                    CoveredRisks = coveredRisks ?? new string[0],
                    Terms = terms ?? "",
                    Metadata = "",
                    LastPremiumPaid = 0,
                    TotalPremiumsPaid = 0
                };

                Storage.Put(Storage.CurrentContext, PolicyKey(policyId), StdLib.Serialize(policy));
                OnPolicyCreated(policyId, Runtime.CallingScriptHash, coverageAmount, premium);

                return policyId;
            }
            catch (Exception ex)
            {
                OnInsuranceError("CreatePolicy", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get policy information
        /// </summary>
        public static Policy GetPolicy(string policyId)
        {
            if (string.IsNullOrEmpty(policyId)) throw new ArgumentException("Policy ID required");

            var data = Storage.Get(Storage.CurrentContext, PolicyKey(policyId));
            if (data == null) return null;

            return (Policy)StdLib.Deserialize(data);
        }
        #endregion

        #region Claims Management
        /// <summary>
        /// Submit an insurance claim
        /// </summary>
        public static string SubmitClaim(string policyId, BigInteger claimAmount, string description, string[] evidence)
        {
            if (string.IsNullOrEmpty(policyId)) throw new ArgumentException("Policy ID required");
            if (claimAmount <= 0) throw new ArgumentException("Claim amount must be positive");
            if (string.IsNullOrEmpty(description)) throw new ArgumentException("Description required");

            var policyData = Storage.Get(Storage.CurrentContext, PolicyKey(policyId));
            if (policyData == null) throw new InvalidOperationException("Policy not found");

            var policy = (Policy)StdLib.Deserialize(policyData);
            if (policy.Policyholder != Runtime.CallingScriptHash) throw new UnauthorizedAccessException("Not policy holder");
            if (policy.Status != PolicyStatus.Active) throw new InvalidOperationException("Policy not active");
            if (Runtime.Time > policy.EndDate) throw new InvalidOperationException("Policy expired");
            if (claimAmount > policy.CoverageAmount) throw new ArgumentException("Claim exceeds coverage");

            try
            {
                var claimId = GenerateId("CLM");
                var claim = new Claim
                {
                    Id = claimId,
                    PolicyId = policyId,
                    Claimant = Runtime.CallingScriptHash,
                    ClaimAmount = claimAmount,
                    Description = description,
                    Evidence = evidence ?? new string[0],
                    Status = ClaimStatus.Submitted,
                    SubmittedAt = Runtime.Time,
                    ProcessedAt = 0,
                    ApprovedAmount = 0,
                    RejectionReason = "",
                    Assessor = UInt160.Zero,
                    AssessmentNotes = ""
                };

                Storage.Put(Storage.CurrentContext, ClaimKey(claimId), StdLib.Serialize(claim));
                OnClaimSubmitted(claimId, policyId, Runtime.CallingScriptHash, claimAmount);

                return claimId;
            }
            catch (Exception ex)
            {
                OnInsuranceError("SubmitClaim", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Process an insurance claim
        /// </summary>
        public static bool ProcessClaim(string claimId, bool approved, BigInteger approvedAmount, string notes)
        {
            if (string.IsNullOrEmpty(claimId)) throw new ArgumentException("Claim ID required");

            var claimData = Storage.Get(Storage.CurrentContext, ClaimKey(claimId));
            if (claimData == null) throw new InvalidOperationException("Claim not found");

            var claim = (Claim)StdLib.Deserialize(claimData);
            if (claim.Status != ClaimStatus.Submitted && claim.Status != ClaimStatus.UnderReview)
                throw new InvalidOperationException("Claim cannot be processed");

            try
            {
                claim.Assessor = Runtime.CallingScriptHash;
                claim.ProcessedAt = Runtime.Time;
                claim.AssessmentNotes = notes ?? "";

                if (approved)
                {
                    claim.Status = ClaimStatus.Approved;
                    claim.ApprovedAmount = approvedAmount > 0 ? approvedAmount : claim.ClaimAmount;
                    
                    // Process payout
                    ProcessPayout(claim);
                }
                else
                {
                    claim.Status = ClaimStatus.Rejected;
                    claim.RejectionReason = notes ?? "Claim rejected";
                }

                Storage.Put(Storage.CurrentContext, ClaimKey(claimId), StdLib.Serialize(claim));
                OnClaimProcessed(claimId, (byte)claim.Status, claim.ApprovedAmount);

                return true;
            }
            catch (Exception ex)
            {
                OnInsuranceError("ProcessClaim", ex.Message);
                return false;
            }
        }

        private static void ProcessPayout(Claim claim)
        {
            // Simplified payout processing
            // In practice, would integrate with payment systems
            claim.Status = ClaimStatus.Paid;
            OnPayoutMade(claim.Id, claim.Claimant, claim.ApprovedAmount);
        }
        #endregion

        #region Premium Management
        /// <summary>
        /// Pay insurance premium
        /// </summary>
        public static bool PayPremium(string policyId, BigInteger amount)
        {
            if (string.IsNullOrEmpty(policyId)) throw new ArgumentException("Policy ID required");
            if (amount <= 0) throw new ArgumentException("Amount must be positive");

            var policyData = Storage.Get(Storage.CurrentContext, PolicyKey(policyId));
            if (policyData == null) throw new InvalidOperationException("Policy not found");

            var policy = (Policy)StdLib.Deserialize(policyData);
            if (policy.Policyholder != Runtime.CallingScriptHash) throw new UnauthorizedAccessException("Not policy holder");

            try
            {
                var premiumId = GenerateId("PRM");
                var premium = new Premium
                {
                    Id = premiumId,
                    PolicyId = policyId,
                    Payer = Runtime.CallingScriptHash,
                    Amount = amount,
                    DueDate = Runtime.Time,
                    PaidDate = Runtime.Time,
                    IsPaid = true,
                    PaymentToken = UInt160.Zero, // Default to NEO
                    TransactionHash = ""
                };

                // Update policy
                policy.LastPremiumPaid = Runtime.Time;
                policy.TotalPremiumsPaid += amount;

                Storage.Put(Storage.CurrentContext, PremiumKey(premiumId), StdLib.Serialize(premium));
                Storage.Put(Storage.CurrentContext, PolicyKey(policyId), StdLib.Serialize(policy));

                OnPremiumPaid(policyId, Runtime.CallingScriptHash, amount);
                return true;
            }
            catch (Exception ex)
            {
                OnInsuranceError("PayPremium", ex.Message);
                return false;
            }
        }
        #endregion

        #region Risk Assessment
        /// <summary>
        /// Assess risk for insurance applicant
        /// </summary>
        public static string AssessRisk(UInt160 subject, byte insuranceType, string[] riskFactors)
        {
            if (subject == UInt160.Zero) throw new ArgumentException("Subject required");
            if (!Enum.IsDefined(typeof(InsuranceType), insuranceType)) throw new ArgumentException("Invalid insurance type");

            try
            {
                var assessmentId = GenerateId("ASS");
                var riskScore = CalculateRiskScore(riskFactors);
                var riskLevel = DetermineRiskLevel(riskScore);

                var assessment = new RiskAssessment
                {
                    Id = assessmentId,
                    Subject = subject,
                    Type = (InsuranceType)insuranceType,
                    Level = riskLevel,
                    Score = riskScore,
                    Factors = riskFactors ?? new string[0],
                    AssessedAt = Runtime.Time,
                    Assessor = Runtime.CallingScriptHash,
                    ValidUntil = Runtime.Time + 86400 * 30, // 30 days
                    Notes = ""
                };

                Storage.Put(Storage.CurrentContext, AssessmentKey(assessmentId), StdLib.Serialize(assessment));
                return assessmentId;
            }
            catch (Exception ex)
            {
                OnInsuranceError("AssessRisk", ex.Message);
                throw;
            }
        }

        private static BigInteger CalculateRiskScore(string[] factors)
        {
            // Simplified risk scoring algorithm
            BigInteger score = 50; // Base score
            
            if (factors != null)
            {
                foreach (var factor in factors)
                {
                    // Adjust score based on risk factors
                    if (factor.Contains("high_risk")) score += 20;
                    else if (factor.Contains("low_risk")) score -= 10;
                }
            }

            return score > 100 ? 100 : (score < 0 ? 0 : score);
        }

        private static RiskLevel DetermineRiskLevel(BigInteger score)
        {
            if (score >= 80) return RiskLevel.Critical;
            if (score >= 60) return RiskLevel.High;
            if (score >= 30) return RiskLevel.Medium;
            return RiskLevel.Low;
        }
        #endregion

        #region Risk Pool Management
        /// <summary>
        /// Create a new risk pool
        /// </summary>
        public static string CreateRiskPool(string name, byte insuranceType, BigInteger minimumPremium, BigInteger maximumCoverage)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Pool name required");
            if (!Enum.IsDefined(typeof(InsuranceType), insuranceType)) throw new ArgumentException("Invalid insurance type");

            var poolId = GenerateId("POOL");
            var pool = new RiskPool
            {
                Id = poolId,
                Name = name,
                Type = (InsuranceType)insuranceType,
                TotalFunds = 0,
                ReservedFunds = 0,
                TotalPolicies = 0,
                TotalClaims = 0,
                TotalPayouts = 0,
                RiskLevel = RiskLevel.Medium,
                MinimumPremium = minimumPremium,
                MaximumCoverage = maximumCoverage,
                IsActive = true
            };

            Storage.Put(Storage.CurrentContext, PoolKey(poolId), StdLib.Serialize(pool));
            return poolId;
        }
        #endregion

        #region Premium Calculation
        /// <summary>
        /// Calculate insurance premium
        /// </summary>
        public static BigInteger CalculatePremium(byte insuranceType, BigInteger coverageAmount, byte riskLevel, BigInteger duration)
        {
            if (!Enum.IsDefined(typeof(InsuranceType), insuranceType)) throw new ArgumentException("Invalid insurance type");
            if (!Enum.IsDefined(typeof(RiskLevel), riskLevel)) throw new ArgumentException("Invalid risk level");
            if (coverageAmount <= 0) throw new ArgumentException("Coverage amount must be positive");
            if (duration <= 0) throw new ArgumentException("Duration must be positive");

            // Base premium calculation (simplified)
            BigInteger basePremium = coverageAmount / 100; // 1% of coverage
            
            // Risk multiplier
            BigInteger riskMultiplier = (BigInteger)riskLevel + 1;
            
            // Duration factor (annual basis)
            BigInteger durationFactor = duration / (365 * 24 * 3600); // Convert to years
            if (durationFactor == 0) durationFactor = 1;

            // Type factor
            BigInteger typeFactor = GetTypeFactor((InsuranceType)insuranceType);

            return basePremium * riskMultiplier * durationFactor * typeFactor / 100;
        }

        private static BigInteger GetTypeFactor(InsuranceType type)
        {
            switch (type)
            {
                case InsuranceType.Life: return 150;
                case InsuranceType.Health: return 120;
                case InsuranceType.Property: return 100;
                case InsuranceType.Travel: return 80;
                case InsuranceType.Crop: return 200;
                case InsuranceType.Weather: return 180;
                case InsuranceType.Cyber: return 250;
                case InsuranceType.Parametric: return 90;
                default: return 100;
            }
        }
        #endregion

        #region History and Reporting
        /// <summary>
        /// Get claim history for a user
        /// </summary>
        public static string[] GetClaimHistory(UInt160 user)
        {
            // Simplified implementation - would return actual claim IDs
            var history = new string[0];
            return history;
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
        /// Get insurance service statistics
        /// </summary>
        public static Map<string, BigInteger> GetInsuranceStats()
        {
            var stats = new Map<string, BigInteger>();
            stats["total_policies"] = GetTotalPolicies();
            stats["total_claims"] = GetTotalClaims();
            stats["total_payouts"] = GetTotalPayouts();
            stats["total_premiums"] = GetTotalPremiums();
            stats["active_policies"] = GetActivePolicies();
            return stats;
        }

        private static BigInteger GetTotalPolicies()
        {
            return Storage.Get(Storage.CurrentContext, "total_policies")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalClaims()
        {
            return Storage.Get(Storage.CurrentContext, "total_claims")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalPayouts()
        {
            return Storage.Get(Storage.CurrentContext, "total_payouts")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalPremiums()
        {
            return Storage.Get(Storage.CurrentContext, "total_premiums")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetActivePolicies()
        {
            return Storage.Get(Storage.CurrentContext, "active_policies")?.ToBigInteger() ?? 0;
        }
        #endregion
    }
}