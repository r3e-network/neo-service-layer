using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Configuration;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Core.Base;
using NeoServiceLayer.Tee.Host.Services;
using ServiceFrameworkConfig = NeoServiceLayer.ServiceFramework.IServiceConfiguration;

namespace NeoServiceLayer.Services.Compute
{
    /// <summary>
    /// PostgreSQL-backed Compute service implementation providing secure computation capabilities.
    /// </summary>
    public class PostgreSQLComputeService : BasePostgreSQLService<PostgreSQLComputeService>, IComputeService
    {
        private readonly ServiceFrameworkConfig _configuration;
        private readonly IEnclaveManager _enclaveManager;
        private readonly IGenericRepository<ComputationEntity> _computationRepository;
        private readonly IGenericRepository<ComputationStatusEntity> _statusRepository;
        private readonly IGenericRepository<ComputationResultEntity> _resultRepository;
        private readonly SemaphoreSlim _executionSemaphore;
        private readonly int _maxConcurrentExecutions;
        private readonly SHA256 _sha256 = SHA256.Create();
        private readonly ILogger<PostgreSQLComputeService> _serviceLogger;

        public PostgreSQLComputeService(
            ServiceFrameworkConfig configuration,
            IEnclaveManager enclaveManager,
            IUnitOfWork unitOfWork,
            ILogger<PostgreSQLComputeService> logger)
            : base(unitOfWork, logger)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(enclaveManager);

            _configuration = configuration;
            _enclaveManager = enclaveManager;
            _serviceLogger = logger;

            _computationRepository = unitOfWork.GetRepository<ComputationEntity>();
            _statusRepository = unitOfWork.GetRepository<ComputationStatusEntity>();
            _resultRepository = unitOfWork.GetRepository<ComputationResultEntity>();

            _maxConcurrentExecutions = configuration.GetValue("Compute:MaxConcurrentExecutions", 10);
            _executionSemaphore = new SemaphoreSlim(_maxConcurrentExecutions, _maxConcurrentExecutions);

            InitializeService();
        }

        private void InitializeService()
        {
            _serviceLogger.LogInformation("Initializing PostgreSQL Compute Service");
        }

        public async Task<ComputationResult> ExecuteComputationAsync(
            string computationId,
            IDictionary<string, string> parameters,
            BlockchainType blockchainType)
        {
            ValidateStringInput(computationId, nameof(computationId));
            ValidateInput(parameters, nameof(parameters));

            if (blockchainType != BlockchainType.NeoN3 && blockchainType != BlockchainType.NeoX)
            {
                throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
            }

            // Check if computation exists
            var computation = await _computationRepository.GetByIdAsync(Guid.Parse(computationId));
            if (computation == null)
            {
                throw new ArgumentException($"Computation {computationId} is not registered.");
            }

            await _executionSemaphore.WaitAsync();
            try
            {
                return await ExecuteInTransactionAsync(async () =>
                {
                    // Create status entry
                    var statusEntity = new ComputationStatusEntity
                    {
                        Id = Guid.NewGuid(),
                        ComputationId = computationId,
                        Status = "InProgress",
                        StartTime = DateTime.UtcNow,
                        BlockchainType = blockchainType.ToString(),
                        Parameters = JsonSerializer.Serialize(parameters)
                    };
                    await _statusRepository.AddAsync(statusEntity);

                    try
                    {
                        // Execute computation in enclave
                        var enclaveRequest = new
                        {
                            ComputationId = computationId,
                            Type = computation.ComputationType,
                            Code = computation.Code,
                            Parameters = parameters
                        };

                        var enclaveResponse = await _enclaveManager.ProcessRequestAsync(
                            "Compute",
                            "Execute",
                            JsonSerializer.Serialize(enclaveRequest));

                        var result = JsonSerializer.Deserialize<ComputationResult>(enclaveResponse);

                        // Store result
                        var resultEntity = new ComputationResultEntity
                        {
                            Id = Guid.NewGuid(),
                            ComputationId = computationId,
                            StatusId = statusEntity.Id,
                            Result = JsonSerializer.Serialize(result.Output),
                            Hash = ComputeHash(result.Output),
                            Timestamp = DateTime.UtcNow,
                            Success = true
                        };
                        await _resultRepository.AddAsync(resultEntity);

                        // Update status
                        statusEntity.Status = "Completed";
                        statusEntity.EndTime = DateTime.UtcNow;
                        await _statusRepository.UpdateAsync(statusEntity);

                        await _unitOfWork.SaveChangesAsync();

                        _serviceLogger.LogInformation("Computation {ComputationId} executed successfully", computationId);

                        return result;
                    }
                    catch (Exception ex)
                    {
                        // Update status on failure
                        statusEntity.Status = "Failed";
                        statusEntity.EndTime = DateTime.UtcNow;
                        statusEntity.ErrorMessage = ex.Message;
                        await _statusRepository.UpdateAsync(statusEntity);
                        await _unitOfWork.SaveChangesAsync();

                        _serviceLogger.LogError(ex, "Failed to execute computation {ComputationId}", computationId);
                        throw;
                    }
                }, "ExecuteComputation");
            }
            finally
            {
                _executionSemaphore.Release();
            }
        }

        public async Task<string> RegisterComputationAsync(
            ComputationMetadata metadata,
            BlockchainType blockchainType)
        {
            ValidateInput(metadata, nameof(metadata));

            if (blockchainType != BlockchainType.NeoN3 && blockchainType != BlockchainType.NeoX)
            {
                throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
            }

            return await ExecuteInTransactionAsync(async () =>
            {
                var computationId = Guid.NewGuid().ToString();
                
                var entity = new ComputationEntity
                {
                    Id = Guid.Parse(computationId),
                    Name = metadata.Name,
                    Description = metadata.Description,
                    ComputationType = metadata.Type.ToString(),
                    Code = metadata.Code,
                    Version = metadata.Version,
                    Author = metadata.Author,
                    BlockchainType = blockchainType.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Metadata = JsonSerializer.Serialize(metadata)
                };

                await _computationRepository.AddAsync(entity);
                await _unitOfWork.SaveChangesAsync();

                _serviceLogger.LogInformation("Registered computation {ComputationId} with name {Name}", 
                    computationId, metadata.Name);

                return computationId;
            }, "RegisterComputation");
        }

        public async Task<ComputationStatus> GetComputationStatusAsync(
            string computationId,
            BlockchainType blockchainType)
        {
            ValidateStringInput(computationId, nameof(computationId));

            var statusEntities = await _statusRepository.GetAllAsync();
            var status = statusEntities
                .Where(s => s.ComputationId == computationId && 
                           s.BlockchainType == blockchainType.ToString())
                .OrderByDescending(s => s.StartTime)
                .FirstOrDefault();

            if (status == null)
            {
                return new ComputationStatus
                {
                    ComputationId = computationId,
                    Status = "NotFound",
                    LastUpdated = DateTime.UtcNow
                };
            }

            return new ComputationStatus
            {
                ComputationId = computationId,
                Status = status.Status,
                StartTime = status.StartTime,
                EndTime = status.EndTime,
                LastUpdated = status.UpdatedAt ?? status.StartTime,
                ErrorMessage = status.ErrorMessage
            };
        }

        public async Task<ComputationResult> GetComputationResultAsync(
            string computationId,
            BlockchainType blockchainType)
        {
            ValidateStringInput(computationId, nameof(computationId));

            var results = await _resultRepository.GetAllAsync();
            var result = results
                .Where(r => r.ComputationId == computationId)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefault();

            if (result == null)
            {
                throw new InvalidOperationException($"No result found for computation {computationId}");
            }

            var output = JsonSerializer.Deserialize<IDictionary<string, object>>(result.Result);

            return new ComputationResult
            {
                ComputationId = computationId,
                Output = output,
                Status = result.Success ? "Success" : "Failed",
                Timestamp = result.Timestamp,
                Hash = result.Hash
            };
        }

        public async Task<IEnumerable<ComputationMetadata>> ListComputationsAsync(BlockchainType blockchainType)
        {
            var computations = await _computationRepository.GetAllAsync();
            
            return computations
                .Where(c => c.BlockchainType == blockchainType.ToString() && c.IsActive)
                .Select(c => JsonSerializer.Deserialize<ComputationMetadata>(c.Metadata))
                .Where(m => m != null);
        }

        public async Task<bool> DeleteComputationAsync(
            string computationId,
            BlockchainType blockchainType)
        {
            ValidateStringInput(computationId, nameof(computationId));

            return await ExecuteInTransactionAsync(async () =>
            {
                var computation = await _computationRepository.GetByIdAsync(Guid.Parse(computationId));
                
                if (computation == null || computation.BlockchainType != blockchainType.ToString())
                {
                    return false;
                }

                // Soft delete - mark as inactive
                computation.IsActive = false;
                computation.UpdatedAt = DateTime.UtcNow;
                
                await _computationRepository.UpdateAsync(computation);
                await _unitOfWork.SaveChangesAsync();

                _serviceLogger.LogInformation("Deleted computation {ComputationId}", computationId);

                return true;
            }, "DeleteComputation");
        }

        public async Task<bool> ValidateComputationAsync(
            string computationId,
            BlockchainType blockchainType)
        {
            ValidateStringInput(computationId, nameof(computationId));

            var computation = await _computationRepository.GetByIdAsync(Guid.Parse(computationId));
            
            if (computation == null || computation.BlockchainType != blockchainType.ToString())
            {
                return false;
            }

            // Validate computation code and metadata
            try
            {
                var metadata = JsonSerializer.Deserialize<ComputationMetadata>(computation.Metadata);
                
                // Basic validation checks
                if (string.IsNullOrWhiteSpace(metadata?.Code))
                {
                    return false;
                }

                if (metadata.Type == ComputationType.Unknown)
                {
                    return false;
                }

                // Additional validation in enclave
                var validationRequest = new
                {
                    ComputationId = computationId,
                    Type = computation.ComputationType,
                    Code = computation.Code
                };

                var response = await _enclaveManager.ProcessRequestAsync(
                    "Compute",
                    "Validate",
                    JsonSerializer.Serialize(validationRequest));

                var validationResult = JsonSerializer.Deserialize<Dictionary<string, object>>(response);
                
                return validationResult?.ContainsKey("valid") == true && 
                       Convert.ToBoolean(validationResult["valid"]);
            }
            catch (Exception ex)
            {
                _serviceLogger.LogError(ex, "Failed to validate computation {ComputationId}", computationId);
                return false;
            }
        }

        private string ComputeHash(IDictionary<string, object> data)
        {
            var json = JsonSerializer.Serialize(data);
            var bytes = Encoding.UTF8.GetBytes(json);
            var hash = _sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public void Dispose()
        {
            _executionSemaphore?.Dispose();
            _sha256?.Dispose();
        }
    }
}