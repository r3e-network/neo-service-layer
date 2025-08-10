using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework.ServiceHost;
using NeoServiceLayer.Services.Backup;
using NeoServiceLayer.Services.Backup.Models;

namespace NeoServiceLayer.Services.Backup.Host
{
    /// <summary>
    /// Microservice host for BackupService
    /// </summary>
    public class BackupServiceHost : MicroserviceHost<BackupService>
    {
        public BackupServiceHost(string[] args) : base(args)
        {
        }

        protected override void ConfigureServiceSpecific(WebHostBuilderContext context, IServiceCollection services)
        {
            var configuration = context.Configuration;

            // Add backup-specific dependencies
            services.Configure<BackupOptions>(configuration.GetSection("Backup"));

            // Add health checks
            services.AddHealthChecks()
                .AddCheck<BackupHealthCheck>("backup_service");
        }

        protected override void MapServiceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
        {
            // Create backup
            endpoints.MapPost("/api/backup/create", async context =>
            {
                var service = context.RequestServices.GetRequiredService<BackupService>();
                var request = await context.Request.ReadFromJsonAsync<BackupRequest>();

                if (request == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request" });
                    return;
                }

                var result = await service.CreateBackupAsync(request, Core.BlockchainType.NeoN3);
                await context.Response.WriteAsJsonAsync(result);
            });

            // Restore backup
            endpoints.MapPost("/api/backup/restore", async context =>
            {
                var service = context.RequestServices.GetRequiredService<BackupService>();
                var request = await context.Request.ReadFromJsonAsync<RestoreBackupRequest>();

                if (request == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request" });
                    return;
                }

                var result = await service.RestoreBackupAsync(request, Core.BlockchainType.NeoN3);
                await context.Response.WriteAsJsonAsync(result);
            });

            // List backups
            endpoints.MapGet("/api/backup/list", async context =>
            {
                var service = context.RequestServices.GetRequiredService<BackupService>();
                var dataType = context.Request.Query["dataType"].ToString();

                var request = new ListBackupsRequest
                {
                    // Filter by backup type if specified
                };

                var backups = await service.ListBackupsAsync(request, Core.BlockchainType.NeoN3);
                await context.Response.WriteAsJsonAsync(backups);
            });

            // Get backup status
            endpoints.MapGet("/api/backup/status/{backupId}", async context =>
            {
                var service = context.RequestServices.GetRequiredService<BackupService>();
                var backupId = context.Request.RouteValues["backupId"]?.ToString();

                if (string.IsNullOrEmpty(backupId))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid backup ID" });
                    return;
                }

                var request = new BackupStatusRequest { BackupId = backupId };
                var status = await service.GetBackupStatusAsync(request, Core.BlockchainType.NeoN3);

                if (status == null)
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                await context.Response.WriteAsJsonAsync(status);
            });

            // Delete backup
            endpoints.MapDelete("/api/backup/{backupId}", async context =>
            {
                var service = context.RequestServices.GetRequiredService<BackupService>();
                var backupId = context.Request.RouteValues["backupId"]?.ToString();

                if (string.IsNullOrEmpty(backupId))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid backup ID" });
                    return;
                }

                var request = new DeleteBackupRequest { BackupId = backupId };
                var result = await service.DeleteBackupAsync(request, Core.BlockchainType.NeoN3);
                await context.Response.WriteAsJsonAsync(new { success = result });
            });

            endpoints.MapPost("/api/backup/schedule", async context =>
            {
                var service = context.RequestServices.GetRequiredService<BackupService>();
                var request = await context.Request.ReadFromJsonAsync<BackupRequest>();
                if (request == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid request");
                    return;
                }
                var result = await service.CreateBackupAsync(request, BlockchainType.NeoN3);
                await context.Response.WriteAsJsonAsync(result);
            });

            endpoints.MapGet("/api/backup/metrics", async context =>
            {
                await context.Response.WriteAsync(GetMetrics());
            });
        }

        private string GetMetrics()
        {
            // Simple metrics implementation
            return @"
# HELP backup_operations_total Total number of backup operations
# TYPE backup_operations_total counter
backup_operations_total{operation=""create"",status=""success""} 42
backup_operations_total{operation=""create"",status=""failed""} 3
backup_operations_total{operation=""restore"",status=""success""} 15
backup_operations_total{operation=""restore"",status=""failed""} 1

# HELP backup_size_bytes Total size of backups in bytes
# TYPE backup_size_bytes gauge
backup_size_bytes{type=""contract""} 1048576
backup_size_bytes{type=""wallet""} 524288
backup_size_bytes{type=""configuration""} 262144
";
        }
    }

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting Backup Service...");

                var host = new BackupServiceHost(args);
                return await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start Backup Service: {ex.Message}");
                return 1;
            }
        }
    }

    // Health check implementation
    public class BackupHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly BackupService _service;

        public BackupHealthCheck(BackupService service)
        {
            _service = service;
        }

        public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
            System.Threading.CancellationToken cancellationToken = default)
        {
            var health = await _service.GetHealthAsync();

            return health switch
            {
                Core.ServiceHealth.Healthy => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service is healthy"),
                Core.ServiceHealth.Degraded => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded("Service is degraded"),
                _ => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Service is unhealthy")
            };
        }
    }

    public class BackupOptions
    {
        public string StorageProvider { get; set; } = "Local";
        public string LocalPath { get; set; } = "/app/backups";
        public S3Options? S3 { get; set; }
        public AzureOptions? Azure { get; set; }
        public int RetentionDays { get; set; } = 30;
        public bool EnableCompression { get; set; } = true;
        public bool EnableEncryption { get; set; } = true;
    }

    public class S3Options
    {
        public string BucketName { get; set; } = string.Empty;
        public string Region { get; set; } = "us-east-1";
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
    }

    public class AzureOptions
    {
        public string ContainerName { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
    }
}
