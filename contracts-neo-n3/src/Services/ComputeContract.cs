using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using NeoServiceLayer.Contracts.Core;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NeoServiceLayer.Contracts.Services
{
    /// <summary>
    /// Provides confidential computing services using Intel SGX enclaves
    /// for secure computation on encrypted data.
    /// </summary>
    [DisplayName("ComputeContract")]
    [ManifestExtra("Author", "Neo Service Layer Team")]
    [ManifestExtra("Description", "Confidential computing service with Intel SGX")]
    [ManifestExtra("Version", "1.0.0")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class ComputeContract : BaseServiceContract
    {
        #region Storage Keys
        private static readonly byte[] ComputeJobPrefix = "computeJob:".ToByteArray();
        private static readonly byte[] ComputeResultPrefix = "computeResult:".ToByteArray();
        private static readonly byte[] EnclavePrefix = "enclave:".ToByteArray();
        private static readonly byte[] JobCounterKey = "jobCounter".ToByteArray();
        private static readonly byte[] ComputeFeeKey = "computeFee".ToByteArray();
        private static readonly byte[] MaxComputeTimeKey = "maxComputeTime".ToByteArray();
        private static readonly byte[] EnclaveCountKey = "enclaveCount".ToByteArray();
        private static readonly byte[] AttestationPrefix = "attestation:".ToByteArray();
        #endregion

        #region Events
        [DisplayName("ComputeJobSubmitted")]
        public static event Action<UInt160, ByteString, string, ByteString, BigInteger> ComputeJobSubmitted;

        [DisplayName("ComputeJobCompleted")]
        public static event Action<UInt160, ByteString, ByteString, bool> ComputeJobCompleted;

        [DisplayName("EnclaveRegistered")]
        public static event Action<UInt160, string, ByteString> EnclaveRegistered;

        [DisplayName("AttestationVerified")]
        public static event Action<UInt160, ByteString, bool> AttestationVerified;

        [DisplayName("ComputeError")]
        public static event Action<UInt160, ByteString, string> ComputeError;
        #endregion

        #region Constants
        private const long DEFAULT_COMPUTE_FEE = 10000000; // 0.1 GAS
        private const ulong DEFAULT_MAX_COMPUTE_TIME = 3600; // 1 hour
        private const int MAX_ENCLAVES = 100;
        #endregion

        #region Initialization
        public static void _deploy(object data, bool update)
        {
            if (update) return;

            var serviceId = Runtime.ExecutingScriptHash;
            var contract = new ComputeContract();
            contract.InitializeBaseService(serviceId, "ComputeService", "1.0.0", "{}");
            
            Storage.Put(Storage.CurrentContext, ComputeFeeKey, DEFAULT_COMPUTE_FEE);
            Storage.Put(Storage.CurrentContext, MaxComputeTimeKey, DEFAULT_MAX_COMPUTE_TIME);
            Storage.Put(Storage.CurrentContext, JobCounterKey, 0);
            Storage.Put(Storage.CurrentContext, EnclaveCountKey, 0);

            Runtime.Log("ComputeContract deployed successfully");
        }
        #endregion

        #region Service Implementation
        protected override void InitializeService(string config)
        {
            Runtime.Log("ComputeContract service initialized");
        }

        protected override bool PerformHealthCheck()
        {
            try
            {
                var enclaveCount = GetEnclaveCount();
                return enclaveCount > 0;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Compute Operations
        /// <summary>
        /// Submits a confidential compute job to be executed in SGX enclave.
        /// </summary>
        public static ByteString SubmitComputeJob(string algorithm, ByteString encryptedData, 
            ByteString publicKey, string requirements)
        {
            return ExecuteServiceOperation(() =>
            {
                var caller = Runtime.CallingScriptHash;
                var jobId = GenerateJobId();
                var fee = GetComputeFee();
                
                // Validate inputs
                if (string.IsNullOrEmpty(algorithm))
                    throw new ArgumentException("Algorithm cannot be empty");
                if (encryptedData.Length == 0)
                    throw new ArgumentException("Encrypted data cannot be empty");
                
                // Create compute job
                var job = new ComputeJob
                {
                    Id = jobId,
                    Requester = caller,
                    Algorithm = algorithm,
                    EncryptedData = encryptedData,
                    PublicKey = publicKey,
                    Requirements = requirements,
                    Fee = fee,
                    SubmittedAt = Runtime.Time,
                    Status = JobStatus.Pending,
                    AssignedEnclave = UInt160.Zero
                };
                
                // Store job
                var jobKey = ComputeJobPrefix.Concat(jobId);
                Storage.Put(Storage.CurrentContext, jobKey, StdLib.Serialize(job));
                
                // Emit event for enclave assignment
                ComputeJobSubmitted(caller, jobId, algorithm, encryptedData, fee);
                
                Runtime.Log($"Compute job submitted: {jobId} for algorithm {algorithm}");
                return jobId;
            });
        }

        /// <summary>
        /// Assigns a compute job to a verified enclave.
        /// </summary>
        public static bool AssignJobToEnclave(ByteString jobId, UInt160 enclaveAddress)
        {
            return ExecuteServiceOperation(() =>
            {
                // Validate caller is authorized (enclave manager)
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Unauthorized enclave assignment");
                
                var job = GetComputeJob(jobId);
                if (job == null)
                    throw new InvalidOperationException("Job not found");
                
                if (job.Status != JobStatus.Pending)
                    throw new InvalidOperationException("Job not in pending status");
                
                // Verify enclave is registered and active
                var enclave = GetEnclave(enclaveAddress);
                if (enclave == null || !enclave.IsActive)
                    throw new InvalidOperationException("Enclave not available");
                
                // Assign job
                job.AssignedEnclave = enclaveAddress;
                job.Status = JobStatus.Assigned;
                
                var jobKey = ComputeJobPrefix.Concat(jobId);
                Storage.Put(Storage.CurrentContext, jobKey, StdLib.Serialize(job));
                
                Runtime.Log($"Job {jobId} assigned to enclave {enclaveAddress}");
                return true;
            });
        }

        /// <summary>
        /// Submits the result of a confidential compute job.
        /// </summary>
        public static bool SubmitComputeResult(ByteString jobId, ByteString encryptedResult, 
            ByteString proof, string attestation)
        {
            return ExecuteServiceOperation(() =>
            {
                var caller = Runtime.CallingScriptHash;
                var job = GetComputeJob(jobId);
                
                if (job == null)
                    throw new InvalidOperationException("Job not found");
                
                if (!job.AssignedEnclave.Equals(caller))
                    throw new InvalidOperationException("Only assigned enclave can submit result");
                
                if (job.Status != JobStatus.Assigned && job.Status != JobStatus.Processing)
                    throw new InvalidOperationException("Job not in correct status");
                
                // Verify attestation
                if (!VerifyAttestation(caller, attestation))
                    throw new InvalidOperationException("Invalid attestation");
                
                // Create result
                var result = new ComputeResult
                {
                    JobId = jobId,
                    EncryptedResult = encryptedResult,
                    Proof = proof,
                    Attestation = attestation,
                    CompletedAt = Runtime.Time,
                    ComputedBy = caller
                };
                
                // Store result
                var resultKey = ComputeResultPrefix.Concat(jobId);
                Storage.Put(Storage.CurrentContext, resultKey, StdLib.Serialize(result));
                
                // Update job status
                job.Status = JobStatus.Completed;
                var jobKey = ComputeJobPrefix.Concat(jobId);
                Storage.Put(Storage.CurrentContext, jobKey, StdLib.Serialize(job));
                
                ComputeJobCompleted(job.Requester, jobId, encryptedResult, true);
                Runtime.Log($"Compute job completed: {jobId}");
                return true;
            });
        }

        /// <summary>
        /// Gets the result of a compute job.
        /// </summary>
        public static ComputeResult GetComputeResult(ByteString jobId)
        {
            var resultKey = ComputeResultPrefix.Concat(jobId);
            var resultBytes = Storage.Get(Storage.CurrentContext, resultKey);
            if (resultBytes == null)
                return null;
            
            return (ComputeResult)StdLib.Deserialize(resultBytes);
        }

        /// <summary>
        /// Gets compute job information.
        /// </summary>
        public static ComputeJob GetComputeJob(ByteString jobId)
        {
            var jobKey = ComputeJobPrefix.Concat(jobId);
            var jobBytes = Storage.Get(Storage.CurrentContext, jobKey);
            if (jobBytes == null)
                return null;
            
            return (ComputeJob)StdLib.Deserialize(jobBytes);
        }
        #endregion

        #region Enclave Management
        /// <summary>
        /// Registers a new SGX enclave.
        /// </summary>
        public static bool RegisterEnclave(UInt160 enclaveAddress, string enclaveInfo, 
            ByteString attestationReport)
        {
            return ExecuteServiceOperation(() =>
            {
                // Validate caller has permission to register enclaves
                if (!ValidateAccess(Runtime.CallingScriptHash))
                    throw new InvalidOperationException("Insufficient permissions");
                
                var enclaveCount = GetEnclaveCount();
                if (enclaveCount >= MAX_ENCLAVES)
                    throw new InvalidOperationException("Maximum enclaves limit reached");
                
                // Verify attestation report
                if (!VerifyEnclaveAttestation(enclaveAddress, attestationReport))
                    throw new InvalidOperationException("Invalid enclave attestation");
                
                var enclave = new EnclaveInfo
                {
                    Address = enclaveAddress,
                    Info = enclaveInfo,
                    AttestationReport = attestationReport,
                    RegisteredAt = Runtime.Time,
                    IsActive = true,
                    JobsCompleted = 0,
                    LastHeartbeat = Runtime.Time
                };
                
                var enclaveKey = EnclavePrefix.Concat(enclaveAddress);
                Storage.Put(Storage.CurrentContext, enclaveKey, StdLib.Serialize(enclave));
                
                // Store attestation
                var attestationKey = AttestationPrefix.Concat(enclaveAddress);
                Storage.Put(Storage.CurrentContext, attestationKey, attestationReport);
                
                Storage.Put(Storage.CurrentContext, EnclaveCountKey, enclaveCount + 1);
                
                EnclaveRegistered(enclaveAddress, enclaveInfo, attestationReport);
                Runtime.Log($"Enclave registered: {enclaveAddress}");
                return true;
            });
        }

        /// <summary>
        /// Updates enclave heartbeat to show it's still active.
        /// </summary>
        public static bool UpdateEnclaveHeartbeat(UInt160 enclaveAddress)
        {
            return ExecuteServiceOperation(() =>
            {
                var caller = Runtime.CallingScriptHash;
                if (!caller.Equals(enclaveAddress))
                    throw new InvalidOperationException("Only enclave itself can update heartbeat");
                
                var enclave = GetEnclave(enclaveAddress);
                if (enclave == null)
                    throw new InvalidOperationException("Enclave not registered");
                
                enclave.LastHeartbeat = Runtime.Time;
                
                var enclaveKey = EnclavePrefix.Concat(enclaveAddress);
                Storage.Put(Storage.CurrentContext, enclaveKey, StdLib.Serialize(enclave));
                
                return true;
            });
        }

        /// <summary>
        /// Gets enclave information.
        /// </summary>
        public static EnclaveInfo GetEnclave(UInt160 enclaveAddress)
        {
            var enclaveKey = EnclavePrefix.Concat(enclaveAddress);
            var enclaveBytes = Storage.Get(Storage.CurrentContext, enclaveKey);
            if (enclaveBytes == null)
                return null;
            
            return (EnclaveInfo)StdLib.Deserialize(enclaveBytes);
        }

        /// <summary>
        /// Gets the total number of registered enclaves.
        /// </summary>
        public static int GetEnclaveCount()
        {
            var countBytes = Storage.Get(Storage.CurrentContext, EnclaveCountKey);
            return (int)(countBytes?.ToBigInteger() ?? 0);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Verifies SGX attestation report.
        /// </summary>
        private static bool VerifyEnclaveAttestation(UInt160 enclaveAddress, ByteString attestationReport)
        {
            // In production, this would verify the SGX attestation report
            // For now, simplified verification
            return attestationReport.Length > 0;
        }

        /// <summary>
        /// Verifies runtime attestation.
        /// </summary>
        private static bool VerifyAttestation(UInt160 enclaveAddress, string attestation)
        {
            // In production, this would verify the runtime attestation
            // For now, simplified verification
            return !string.IsNullOrEmpty(attestation);
        }

        /// <summary>
        /// Generates a unique job ID.
        /// </summary>
        private static ByteString GenerateJobId()
        {
            var counter = GetJobCounter();
            Storage.Put(Storage.CurrentContext, JobCounterKey, counter + 1);
            
            var tx = (Transaction)Runtime.ScriptContainer;
            var data = Runtime.Time.ToByteArray()
                .Concat(counter.ToByteArray())
                .Concat(tx.Hash);
            
            return CryptoLib.Sha256(data);
        }

        /// <summary>
        /// Gets the current job counter.
        /// </summary>
        private static BigInteger GetJobCounter()
        {
            var counterBytes = Storage.Get(Storage.CurrentContext, JobCounterKey);
            return counterBytes?.ToBigInteger() ?? 0;
        }

        /// <summary>
        /// Gets the compute fee.
        /// </summary>
        private static BigInteger GetComputeFee()
        {
            var feeBytes = Storage.Get(Storage.CurrentContext, ComputeFeeKey);
            return feeBytes?.ToBigInteger() ?? DEFAULT_COMPUTE_FEE;
        }

        /// <summary>
        /// Executes a service operation with proper error handling.
        /// </summary>
        private static T ExecuteServiceOperation<T>(Func<T> operation)
        {
            ValidateServiceActive();
            IncrementRequestCount();

            try
            {
                return operation();
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                throw;
            }
        }
        #endregion

        #region Data Structures
        /// <summary>
        /// Represents a confidential compute job.
        /// </summary>
        public class ComputeJob
        {
            public ByteString Id;
            public UInt160 Requester;
            public string Algorithm;
            public ByteString EncryptedData;
            public ByteString PublicKey;
            public string Requirements;
            public BigInteger Fee;
            public ulong SubmittedAt;
            public JobStatus Status;
            public UInt160 AssignedEnclave;
        }

        /// <summary>
        /// Represents a compute result.
        /// </summary>
        public class ComputeResult
        {
            public ByteString JobId;
            public ByteString EncryptedResult;
            public ByteString Proof;
            public string Attestation;
            public ulong CompletedAt;
            public UInt160 ComputedBy;
        }

        /// <summary>
        /// Represents an SGX enclave.
        /// </summary>
        public class EnclaveInfo
        {
            public UInt160 Address;
            public string Info;
            public ByteString AttestationReport;
            public ulong RegisteredAt;
            public bool IsActive;
            public int JobsCompleted;
            public ulong LastHeartbeat;
        }

        /// <summary>
        /// Job status enumeration.
        /// </summary>
        public enum JobStatus : byte
        {
            Pending = 0,
            Assigned = 1,
            Processing = 2,
            Completed = 3,
            Failed = 4,
            Cancelled = 5
        }
        #endregion
    }
}