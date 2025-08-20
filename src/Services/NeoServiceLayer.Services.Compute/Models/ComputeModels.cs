using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Compute.Models;

/// <summary>
/// Request to create a compute job
/// </summary>
public class ComputeJobRequest
{
    public string JobId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public int Priority { get; set; } = 1;
}

/// <summary>
/// Result of a compute job
/// </summary>
public class ComputeJobResult
{
    public string JobId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Result { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// Status of a compute job
/// </summary>
public class ComputeJobStatus
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// List of compute job results
/// </summary>
public class ComputeJobResults
{
    public List<ComputeJobResult> Jobs { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// Compute job summary
/// </summary>
public class ComputeJobSummary
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Compute metrics
/// </summary>
public class ComputeMetrics
{
    public int ActiveJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
    public double AverageExecutionTime { get; set; }
}

/// <summary>
/// Compute resource info
/// </summary>
public class ComputeResourceInfo
{
    public int CpuCores { get; set; }
    public long MemoryMB { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
}

/// <summary>
/// Request for compute estimation
/// </summary>
public class ComputeEstimationRequest
{
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Compute estimation result
/// </summary>
public class ComputeEstimation
{
    public TimeSpan EstimatedDuration { get; set; }
    public int EstimatedCost { get; set; }
    public string ResourceRequirements { get; set; } = string.Empty;
}
