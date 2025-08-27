using Microsoft.EntityFrameworkCore;
using Neo.Compute.Service.Data;
using Neo.Compute.Service.Models;
using Neo.Compute.Service.Services;
using System.Text.Json;

namespace Neo.Compute.Service.Services;

public class SgxEnclaveService : ISgxEnclaveService
{
    private readonly ComputeDbContext _context;
    private readonly ISgxHardwareService _hardwareService;
    private readonly ILogger<SgxEnclaveService> _logger;

    public SgxEnclaveService(
        ComputeDbContext context,
        ISgxHardwareService hardwareService,
        ILogger<SgxEnclaveService> logger)
    {
        _context = context;
        _hardwareService = hardwareService;
        _logger = logger;
    }

    public async Task<EnclaveResponse> CreateEnclaveAsync(CreateEnclaveRequest request)
    {
        // Check if SGX is available
        if (!await _hardwareService.IsSgxAvailableAsync())
        {
            throw new InvalidOperationException("SGX hardware is not available on this system");
        }

        // Check if we can create more enclaves
        if (!await _hardwareService.CanCreateEnclaveAsync())
        {
            throw new InvalidOperationException("Cannot create more enclaves - resource limit reached");
        }

        var enclave = new SgxEnclave
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            EnclaveHash = request.EnclaveHash,
            Version = request.Version,
            MaxConcurrentJobs = request.MaxConcurrentJobs,
            Configuration = request.Configuration != null 
                ? JsonSerializer.Serialize(request.Configuration) 
                : "{}",
            Status = SgxEnclaveStatus.Initializing,
            CreatedAt = DateTime.UtcNow,
            Port = GetAvailablePort()
        };

        _context.SgxEnclaves.Add(enclave);
        await _context.SaveChangesAsync();

        _logger.LogInformation("SGX enclave created: {EnclaveId} with name: {Name}", enclave.Id, request.Name);

        // In a real implementation, this would trigger enclave initialization
        _ = Task.Run(() => InitializeEnclaveAsync(enclave.Id));

        return MapToResponse(enclave);
    }

    public async Task<EnclaveResponse?> GetEnclaveAsync(Guid enclaveId)
    {
        var enclave = await _context.SgxEnclaves
            .FirstOrDefaultAsync(e => e.Id == enclaveId);

        return enclave != null ? MapToResponse(enclave) : null;
    }

    public async Task<List<EnclaveResponse>> GetEnclavesAsync(SgxEnclaveStatus? status = null)
    {
        var query = _context.SgxEnclaves.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        var enclaves = await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return enclaves.Select(MapToResponse).ToList();
    }

    public async Task<bool> UpdateEnclaveStatusAsync(Guid enclaveId, SgxEnclaveStatus status)
    {
        var enclave = await _context.SgxEnclaves.FirstOrDefaultAsync(e => e.Id == enclaveId);
        if (enclave == null) return false;

        var previousStatus = enclave.Status;
        enclave.Status = status;
        
        if (status == SgxEnclaveStatus.Ready && previousStatus == SgxEnclaveStatus.Initializing)
        {
            _logger.LogInformation("Enclave {EnclaveId} is now ready", enclaveId);
        }
        else if (status == SgxEnclaveStatus.Error || status == SgxEnclaveStatus.Crashed)
        {
            _logger.LogError("Enclave {EnclaveId} status changed to {Status}", enclaveId, status);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateEnclaveHeartbeatAsync(Guid enclaveId)
    {
        var enclave = await _context.SgxEnclaves.FirstOrDefaultAsync(e => e.Id == enclaveId);
        if (enclave == null) return false;

        enclave.LastHeartbeat = DateTime.UtcNow;
        
        // Update status to Ready if it was Running and no active jobs
        if (enclave.Status == SgxEnclaveStatus.Running && enclave.ActiveJobs == 0)
        {
            enclave.Status = SgxEnclaveStatus.Ready;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<EnclaveResponse?> GetAvailableEnclaveAsync(ComputeJobPriority priority = ComputeJobPriority.Normal)
    {
        var availableEnclaves = await _context.SgxEnclaves
            .Where(e => e.Status == SgxEnclaveStatus.Ready && 
                       e.ActiveJobs < e.MaxConcurrentJobs)
            .OrderBy(e => e.ActiveJobs) // Prefer enclaves with fewer active jobs
            .ThenByDescending(e => e.LastHeartbeat) // Prefer recently active enclaves
            .ToListAsync();

        var selectedEnclave = availableEnclaves.FirstOrDefault();
        
        if (selectedEnclave != null)
        {
            // Reserve the enclave for the job
            selectedEnclave.ActiveJobs++;
            selectedEnclave.Status = selectedEnclave.ActiveJobs >= selectedEnclave.MaxConcurrentJobs 
                ? SgxEnclaveStatus.Busy 
                : SgxEnclaveStatus.Running;

            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Enclave {EnclaveId} assigned for priority {Priority}", 
                selectedEnclave.Id, priority);
        }

        return selectedEnclave != null ? MapToResponse(selectedEnclave) : null;
    }

    public async Task<bool> DeleteEnclaveAsync(Guid enclaveId)
    {
        var enclave = await _context.SgxEnclaves
            .Include(e => e.Jobs)
            .FirstOrDefaultAsync(e => e.Id == enclaveId);

        if (enclave == null) return false;

        // Check if there are active jobs
        var activeJobs = enclave.Jobs.Where(j => 
            j.Status == ComputeJobStatus.Running || 
            j.Status == ComputeJobStatus.Queued).Any();

        if (activeJobs)
        {
            throw new InvalidOperationException("Cannot delete enclave with active jobs");
        }

        // Mark enclave as stopped first
        enclave.Status = SgxEnclaveStatus.Stopped;
        await _context.SaveChangesAsync();

        // In a real implementation, this would trigger enclave shutdown
        _ = Task.Run(() => ShutdownEnclaveAsync(enclaveId));

        _logger.LogInformation("Enclave {EnclaveId} marked for deletion", enclaveId);
        return true;
    }

    public async Task<List<EnclaveResponse>> GetEnclavesForNodeAsync(string nodeName)
    {
        var enclaves = await _context.SgxEnclaves
            .Where(e => e.NodeName == nodeName)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return enclaves.Select(MapToResponse).ToList();
    }

    private async Task InitializeEnclaveAsync(Guid enclaveId)
    {
        try
        {
            _logger.LogInformation("Initializing enclave {EnclaveId}", enclaveId);
            
            // Simulate enclave initialization process
            await Task.Delay(5000); // 5 second initialization

            await UpdateEnclaveStatusAsync(enclaveId, SgxEnclaveStatus.Ready);
            
            _logger.LogInformation("Enclave {EnclaveId} initialization completed", enclaveId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize enclave {EnclaveId}", enclaveId);
            await UpdateEnclaveStatusAsync(enclaveId, SgxEnclaveStatus.Error);
        }
    }

    private async Task ShutdownEnclaveAsync(Guid enclaveId)
    {
        try
        {
            _logger.LogInformation("Shutting down enclave {EnclaveId}", enclaveId);
            
            // Simulate enclave shutdown process
            await Task.Delay(2000); // 2 second shutdown

            // Remove from database
            var enclave = await _context.SgxEnclaves.FirstOrDefaultAsync(e => e.Id == enclaveId);
            if (enclave != null)
            {
                _context.SgxEnclaves.Remove(enclave);
                await _context.SaveChangesAsync();
            }
            
            _logger.LogInformation("Enclave {EnclaveId} shutdown completed", enclaveId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to shutdown enclave {EnclaveId}", enclaveId);
        }
    }

    private static int GetAvailablePort()
    {
        // In a real implementation, this would find an available port
        // For now, use a random port in the range 8080-8999
        var random = new Random();
        return random.Next(8080, 9000);
    }

    private EnclaveResponse MapToResponse(SgxEnclave enclave)
    {
        return new EnclaveResponse
        {
            Id = enclave.Id.ToString(),
            Name = enclave.Name,
            Status = enclave.Status.ToString(),
            CreatedAt = enclave.CreatedAt,
            LastHeartbeat = enclave.LastHeartbeat,
            ActiveJobs = enclave.ActiveJobs,
            MaxConcurrentJobs = enclave.MaxConcurrentJobs,
            IsAttested = enclave.IsAttested,
            LastAttestation = enclave.LastAttestation,
            ResourceUsage = new ComputeResourceUsage
            {
                CpuCores = 0, // Would be calculated from actual usage
                MemoryUsedMb = enclave.MemoryUsageMb,
                StorageUsedMb = 0,
                Duration = enclave.LastHeartbeat.HasValue 
                    ? DateTime.UtcNow - enclave.CreatedAt 
                    : TimeSpan.Zero,
                Cost = 0 // Would be calculated based on resource usage
            }
        };
    }
}