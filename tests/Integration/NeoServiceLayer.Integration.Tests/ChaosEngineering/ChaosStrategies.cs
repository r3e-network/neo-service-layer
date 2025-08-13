using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Integration.Tests.ChaosEngineering
{
    /// <summary>
    /// Base class for chaos injection strategies.
    /// </summary>
    public abstract class ChaosStrategy
    {
        protected readonly ILogger _logger;
        protected readonly Dictionary<string, object> _activeFailures;

        protected ChaosStrategy(ILogger logger)
        {
            _logger = logger;
            _activeFailures = new Dictionary<string, object>();
        }

        public abstract Task ApplyAsync(string target, Dictionary<string, object> configuration);
        public abstract Task RemoveAsync(string target);

        protected string GenerateFailureId() => Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Injects network latency into service communications.
    /// </summary>
    public class NetworkLatencyStrategy : ChaosStrategy
    {
        private readonly Dictionary<string, HttpMessageHandler> _originalHandlers;
        private readonly Dictionary<string, DelayedHttpMessageHandler> _delayedHandlers;

        public NetworkLatencyStrategy(ILogger logger) : base(logger)
        {
            _originalHandlers = new Dictionary<string, HttpMessageHandler>();
            _delayedHandlers = new Dictionary<string, DelayedHttpMessageHandler>();
        }

        public override async Task ApplyAsync(string target, Dictionary<string, object> configuration)
        {
            var latency = (TimeSpan)configuration["latency"];
            var percentage = (double)configuration.GetValueOrDefault("percentage", 1.0);

            _logger.LogInformation("Applying network latency of {Latency}ms to {Target} ({Percentage}% of traffic)",
                latency.TotalMilliseconds, target, percentage * 100);

            var handler = new DelayedHttpMessageHandler(latency, percentage);
            _delayedHandlers[target] = handler;

            await Task.CompletedTask;
        }

        public override async Task RemoveAsync(string target)
        {
            if (_delayedHandlers.TryGetValue(target, out var handler))
            {
                handler.Dispose();
                _delayedHandlers.Remove(target);
                _logger.LogInformation("Removed network latency from {Target}", target);
            }

            await Task.CompletedTask;
        }

        private class DelayedHttpMessageHandler : DelegatingHandler
        {
            private readonly TimeSpan _delay;
            private readonly double _percentage;
            private readonly Random _random;

            public DelayedHttpMessageHandler(TimeSpan delay, double percentage)
            {
                _delay = delay;
                _percentage = percentage;
                _random = new Random();
                InnerHandler = new HttpClientHandler();
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                if (_random.NextDouble() <= _percentage)
                {
                    await Task.Delay(_delay, cancellationToken);
                }

                return await base.SendAsync(request, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Kills service instances to simulate failures.
    /// </summary>
    public class ServiceKillStrategy : ChaosStrategy
    {
        private readonly Dictionary<string, Process> _killedProcesses;

        public ServiceKillStrategy(ILogger logger) : base(logger)
        {
            _killedProcesses = new Dictionary<string, Process>();
        }

        public override async Task ApplyAsync(string target, Dictionary<string, object> configuration)
        {
            var instancesToKill = (int)configuration["instances"];
            var allowRestart = (bool)configuration.GetValueOrDefault("allowRestart", true);

            _logger.LogWarning("Killing {Count} instances of {Target}", instancesToKill, target);

            // In a real implementation, this would interact with the orchestrator
            // For testing, we simulate the behavior
            await SimulateServiceKillAsync(target, instancesToKill, allowRestart);
        }

        public override async Task RemoveAsync(string target)
        {
            if (_killedProcesses.ContainsKey(target))
            {
                _logger.LogInformation("Restarting service {Target}", target);
                await SimulateServiceRestartAsync(target);
                _killedProcesses.Remove(target);
            }
        }

        private async Task SimulateServiceKillAsync(string target, int instances, bool allowRestart)
        {
            // Simulate killing service instances
            await Task.Delay(100);

            if (!allowRestart)
            {
                _activeFailures[target] = new { killed = true, instances };
            }
        }

        private async Task SimulateServiceRestartAsync(string target)
        {
            // Simulate restarting service
            await Task.Delay(500);
            _activeFailures.Remove(target);
        }
    }

    /// <summary>
    /// Injects CPU stress on target services.
    /// </summary>
    public class CpuStressStrategy : ChaosStrategy
    {
        private readonly Dictionary<string, CancellationTokenSource> _stressTasks;

        public CpuStressStrategy(ILogger logger) : base(logger)
        {
            _stressTasks = new Dictionary<string, CancellationTokenSource>();
        }

        public override async Task ApplyAsync(string target, Dictionary<string, object> configuration)
        {
            var percentage = (int)configuration["percentage"];
            var duration = (TimeSpan)configuration["duration"];

            _logger.LogInformation("Applying {Percentage}% CPU stress to {Target} for {Duration}",
                percentage, target, duration);

            var cts = new CancellationTokenSource();
            _stressTasks[target] = cts;

            // Start CPU stress tasks
            var coreCount = Environment.ProcessorCount;
            var tasksToRun = Math.Max(1, coreCount * percentage / 100);

            var tasks = new List<Task>();
            for (int i = 0; i < tasksToRun; i++)
            {
                tasks.Add(Task.Run(() => ConsumeCpuAsync(cts.Token), cts.Token));
            }

            // Auto-remove after duration
            _ = Task.Delay(duration).ContinueWith(_ => RemoveAsync(target));

            await Task.CompletedTask;
        }

        public override async Task RemoveAsync(string target)
        {
            if (_stressTasks.TryGetValue(target, out var cts))
            {
                cts.Cancel();
                _stressTasks.Remove(target);
                _logger.LogInformation("Removed CPU stress from {Target}", target);
            }

            await Task.CompletedTask;
        }

        private async Task ConsumeCpuAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // CPU-intensive operation
                for (int i = 0; i < 1000000; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Perform calculation to consume CPU
                    var result = Math.Sqrt(i) * Math.Sin(i);
                }

                // Brief pause to allow other threads
                await Task.Yield();
            }
        }
    }

    /// <summary>
    /// Simulates memory pressure on services.
    /// </summary>
    public class MemoryPressureStrategy : ChaosStrategy
    {
        private readonly Dictionary<string, List<byte[]>> _memoryAllocations;

        public MemoryPressureStrategy(ILogger logger) : base(logger)
        {
            _memoryAllocations = new Dictionary<string, List<byte[]>>();
        }

        public override async Task ApplyAsync(string target, Dictionary<string, object> configuration)
        {
            var memoryMB = (int)configuration["memoryMB"];
            var duration = (TimeSpan)configuration["duration"];

            _logger.LogInformation("Applying {Memory}MB memory pressure to {Target} for {Duration}",
                memoryMB, target, duration);

            // Allocate memory in chunks
            var allocations = new List<byte[]>();
            var chunkSize = 10 * 1024 * 1024; // 10MB chunks
            var chunks = memoryMB / 10;

            for (int i = 0; i < chunks; i++)
            {
                allocations.Add(new byte[chunkSize]);

                // Fill with data to ensure allocation
                for (int j = 0; j < chunkSize; j += 1024)
                {
                    allocations[i][j] = (byte)(j % 256);
                }
            }

            _memoryAllocations[target] = allocations;

            // Auto-remove after duration
            _ = Task.Delay(duration).ContinueWith(_ => RemoveAsync(target));

            await Task.CompletedTask;
        }

        public override async Task RemoveAsync(string target)
        {
            if (_memoryAllocations.TryGetValue(target, out var allocations))
            {
                allocations.Clear();
                _memoryAllocations.Remove(target);

                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                _logger.LogInformation("Removed memory pressure from {Target}", target);
            }

            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Creates network partitions between services.
    /// </summary>
    public class NetworkPartitionStrategy : ChaosStrategy
    {
        private readonly Dictionary<string, NetworkPartition> _partitions;

        public NetworkPartitionStrategy(ILogger logger) : base(logger)
        {
            _partitions = new Dictionary<string, NetworkPartition>();
        }

        public override async Task ApplyAsync(string target, Dictionary<string, object> configuration)
        {
            var partition1 = (List<string>)configuration["partition1"];
            var partition2 = (List<string>)configuration["partition2"];

            _logger.LogWarning("Creating network partition between {P1Count} and {P2Count} services",
                partition1.Count, partition2.Count);

            var partition = new NetworkPartition
            {
                Partition1 = partition1,
                Partition2 = partition2,
                CreatedAt = DateTime.UtcNow
            };

            _partitions[target] = partition;

            // In a real implementation, this would configure network rules
            await SimulateNetworkPartitionAsync(partition);
        }

        public override async Task RemoveAsync(string target)
        {
            if (_partitions.TryGetValue(target, out var partition))
            {
                _logger.LogInformation("Removing network partition");
                await RemoveNetworkPartitionAsync(partition);
                _partitions.Remove(target);
            }
        }

        private async Task SimulateNetworkPartitionAsync(NetworkPartition partition)
        {
            // Simulate network partition configuration
            await Task.Delay(100);
            _activeFailures["partition"] = partition;
        }

        private async Task RemoveNetworkPartitionAsync(NetworkPartition partition)
        {
            // Simulate removing network partition
            await Task.Delay(100);
            _activeFailures.Remove("partition");
        }

        private class NetworkPartition
        {
            public List<string> Partition1 { get; set; } = new();
            public List<string> Partition2 { get; set; } = new();
            public DateTime CreatedAt { get; set; }
        }
    }

    /// <summary>
    /// Simulates disk failures and I/O errors.
    /// </summary>
    public class DiskFailureStrategy : ChaosStrategy
    {
        private readonly Dictionary<string, DiskFailureSimulation> _diskFailures;

        public DiskFailureStrategy(ILogger logger) : base(logger)
        {
            _diskFailures = new Dictionary<string, DiskFailureSimulation>();
        }

        public override async Task ApplyAsync(string target, Dictionary<string, object> configuration)
        {
            var failureType = (string)configuration.GetValueOrDefault("type", "slow");
            var severity = (double)configuration.GetValueOrDefault("severity", 0.5);

            _logger.LogWarning("Simulating disk failure ({Type}) on {Target} with severity {Severity}",
                failureType, target, severity);

            var simulation = new DiskFailureSimulation
            {
                Type = failureType,
                Severity = severity,
                StartTime = DateTime.UtcNow
            };

            _diskFailures[target] = simulation;

            await SimulateDiskFailureAsync(target, simulation);
        }

        public override async Task RemoveAsync(string target)
        {
            if (_diskFailures.TryGetValue(target, out var simulation))
            {
                _logger.LogInformation("Removing disk failure simulation from {Target}", target);
                await RemoveDiskFailureAsync(target, simulation);
                _diskFailures.Remove(target);
            }
        }

        private async Task SimulateDiskFailureAsync(string target, DiskFailureSimulation simulation)
        {
            // In a real implementation, this would simulate I/O errors or slowdowns
            await Task.Delay(100);

            switch (simulation.Type)
            {
                case "slow":
                    // Simulate slow I/O operations
                    _activeFailures[$"{target}_disk"] = new { delay = 500 * simulation.Severity };
                    break;
                case "error":
                    // Simulate I/O errors
                    _activeFailures[$"{target}_disk"] = new { errorRate = simulation.Severity };
                    break;
                case "full":
                    // Simulate disk full
                    _activeFailures[$"{target}_disk"] = new { full = true };
                    break;
            }
        }

        private async Task RemoveDiskFailureAsync(string target, DiskFailureSimulation simulation)
        {
            await Task.Delay(100);
            _activeFailures.Remove($"{target}_disk");
        }

        private class DiskFailureSimulation
        {
            public string Type { get; set; } = string.Empty;
            public double Severity { get; set; }
            public DateTime StartTime { get; set; }
        }
    }

    /// <summary>
    /// Simulates clock skew between services.
    /// </summary>
    public class ClockSkewStrategy : ChaosStrategy
    {
        private readonly Dictionary<string, TimeSpan> _clockSkews;

        public ClockSkewStrategy(ILogger logger) : base(logger)
        {
            _clockSkews = new Dictionary<string, TimeSpan>();
        }

        public override async Task ApplyAsync(string target, Dictionary<string, object> configuration)
        {
            var skewMinutes = (int)configuration.GetValueOrDefault("minutes", 5);
            var direction = (string)configuration.GetValueOrDefault("direction", "forward");

            var skew = direction == "forward"
                ? TimeSpan.FromMinutes(skewMinutes)
                : TimeSpan.FromMinutes(-skewMinutes);

            _logger.LogWarning("Applying clock skew of {Skew} to {Target}", skew, target);

            _clockSkews[target] = skew;

            // In a real implementation, this would adjust system time for the service
            await SimulateClockSkewAsync(target, skew);
        }

        public override async Task RemoveAsync(string target)
        {
            if (_clockSkews.TryGetValue(target, out var skew))
            {
                _logger.LogInformation("Removing clock skew from {Target}", target);
                await RemoveClockSkewAsync(target, skew);
                _clockSkews.Remove(target);
            }
        }

        private async Task SimulateClockSkewAsync(string target, TimeSpan skew)
        {
            await Task.Delay(100);
            _activeFailures[$"{target}_time"] = new { skew = skew.TotalSeconds };
        }

        private async Task RemoveClockSkewAsync(string target, TimeSpan skew)
        {
            await Task.Delay(100);
            _activeFailures.Remove($"{target}_time");
        }
    }
}
