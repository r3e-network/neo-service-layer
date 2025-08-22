using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Infrastructure.Persistence
{
    /// <summary>
    /// Production-ready backup and restore functionality for Occlum storage
    /// </summary>
    public interface IStorageBackupRestore
    {
        Task<string> CreateBackupAsync(string backupName = null, bool compress = true);
        Task<bool> RestoreBackupAsync(string backupPath);
        Task<bool> VerifyBackupIntegrityAsync(string backupPath);
        Task<BackupMetadata> GetBackupMetadataAsync(string backupPath);
        Task<bool> CompactStorageAsync();
        Task<StorageStatistics> GetStorageStatisticsAsync();
    }

    public class BackupMetadata
    {
        public string BackupId { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Version { get; set; } = "1.0.0";
        public long TotalSize { get; set; }
        public int FileCount { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public Dictionary<string, string> Files { get; set; } = new();
        public bool IsCompressed { get; set; }
        public string? CompressionAlgorithm { get; set; }
    }


    public class ProductionOcclumStorageProvider : IStorageBackupRestore
    {
        private readonly string _basePath;
        private readonly ILogger<ProductionOcclumStorageProvider> _logger;
        private readonly SemaphoreSlim _backupSemaphore = new(1, 1);
        private readonly SemaphoreSlim _compactionSemaphore = new(1, 1);

        public ProductionOcclumStorageProvider(
            string basePath,
            ILogger<ProductionOcclumStorageProvider> logger)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> CreateBackupAsync(string? backupName = null, bool compress = true)
        {
            await _backupSemaphore.WaitAsync();
            try
            {
                _logger.LogInformation("Starting backup creation. Compress: {Compress}", compress);

                // Generate backup name if not provided
                backupName ??= $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                
                // Create backup directory
                var backupDir = Path.Combine(_basePath, ".backups");
                Directory.CreateDirectory(backupDir);

                var tempBackupPath = Path.Combine(backupDir, $"{backupName}_temp");
                var finalBackupPath = Path.Combine(backupDir, compress ? $"{backupName}.tar.gz" : $"{backupName}.tar");

                try
                {
                    // Create temporary backup directory
                    if (Directory.Exists(tempBackupPath))
                    {
                        Directory.Delete(tempBackupPath, true);
                    }
                    Directory.CreateDirectory(tempBackupPath);

                    // Copy all data files and metadata
                    var dataDir = Path.Combine(_basePath, "data");
                    var metadataDir = Path.Combine(_basePath, "metadata");
                    
                    var backupMetadata = new BackupMetadata
                    {
                        IsCompressed = compress,
                        CompressionAlgorithm = compress ? "gzip" : null
                    };

                    // Copy data files
                    if (Directory.Exists(dataDir))
                    {
                        await CopyDirectoryAsync(dataDir, Path.Combine(tempBackupPath, "data"), backupMetadata);
                    }

                    // Copy metadata files
                    if (Directory.Exists(metadataDir))
                    {
                        await CopyDirectoryAsync(metadataDir, Path.Combine(tempBackupPath, "metadata"), backupMetadata);
                    }

                    // Calculate total size
                    backupMetadata.TotalSize = GetDirectorySize(tempBackupPath);
                    
                    // Generate checksum
                    backupMetadata.Checksum = await GenerateDirectoryChecksumAsync(tempBackupPath);

                    // Write backup metadata
                    var metadataPath = Path.Combine(tempBackupPath, "backup.json");
                    var metadataJson = JsonSerializer.Serialize(backupMetadata, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    await File.WriteAllTextAsync(metadataPath, metadataJson);

                    // Create tar archive
                    await CreateTarArchiveAsync(tempBackupPath, finalBackupPath, compress);

                    // Cleanup temp directory
                    Directory.Delete(tempBackupPath, true);

                    _logger.LogInformation("Backup created successfully: {BackupPath}", finalBackupPath);
                    return finalBackupPath;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create backup");
                    
                    // Cleanup on failure
                    if (Directory.Exists(tempBackupPath))
                    {
                        Directory.Delete(tempBackupPath, true);
                    }
                    
                    throw;
                }
            }
            finally
            {
                _backupSemaphore.Release();
            }
        }

        public async Task<bool> RestoreBackupAsync(string backupPath)
        {
            if (!File.Exists(backupPath))
            {
                _logger.LogError("Backup file not found: {BackupPath}", backupPath);
                return false;
            }

            await _backupSemaphore.WaitAsync();
            try
            {
                _logger.LogInformation("Starting restore from backup: {BackupPath}", backupPath);

                // Verify backup integrity first
                if (!await VerifyBackupIntegrityAsync(backupPath))
                {
                    _logger.LogError("Backup integrity verification failed");
                    return false;
                }

                // Create restore directory
                var restoreDir = Path.Combine(_basePath, ".restore_temp");
                if (Directory.Exists(restoreDir))
                {
                    Directory.Delete(restoreDir, true);
                }
                Directory.CreateDirectory(restoreDir);

                try
                {
                    // Extract backup
                    await ExtractTarArchiveAsync(backupPath, restoreDir);

                    // Read backup metadata
                    var metadataPath = Path.Combine(restoreDir, "backup.json");
                    if (!File.Exists(metadataPath))
                    {
                        _logger.LogError("Backup metadata not found");
                        return false;
                    }

                    var metadataJson = await File.ReadAllTextAsync(metadataPath);
                    var metadata = JsonSerializer.Deserialize<BackupMetadata>(metadataJson);

                    if (metadata == null)
                    {
                        _logger.LogError("Invalid backup metadata");
                        return false;
                    }

                    // Verify extracted checksum
                    var extractedChecksum = await GenerateDirectoryChecksumAsync(restoreDir);
                    if (extractedChecksum != metadata.Checksum)
                    {
                        _logger.LogError("Checksum mismatch after extraction");
                        return false;
                    }

                    // Create backup of current data
                    var currentBackupName = $"pre_restore_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                    await CreateBackupAsync(currentBackupName, true);

                    // Clear current data
                    var dataDir = Path.Combine(_basePath, "data");
                    var metadataDir = Path.Combine(_basePath, "metadata");
                    
                    if (Directory.Exists(dataDir))
                    {
                        Directory.Delete(dataDir, true);
                    }
                    if (Directory.Exists(metadataDir))
                    {
                        Directory.Delete(metadataDir, true);
                    }

                    // Restore data
                    var restoreDataDir = Path.Combine(restoreDir, "data");
                    var restoreMetadataDir = Path.Combine(restoreDir, "metadata");
                    
                    if (Directory.Exists(restoreDataDir))
                    {
                        Directory.Move(restoreDataDir, dataDir);
                    }
                    if (Directory.Exists(restoreMetadataDir))
                    {
                        Directory.Move(restoreMetadataDir, metadataDir);
                    }

                    // Cleanup restore directory
                    Directory.Delete(restoreDir, true);

                    _logger.LogInformation("Restore completed successfully from: {BackupPath}", backupPath);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to restore backup");
                    
                    // Cleanup on failure
                    if (Directory.Exists(restoreDir))
                    {
                        Directory.Delete(restoreDir, true);
                    }
                    
                    return false;
                }
            }
            finally
            {
                _backupSemaphore.Release();
            }
        }

        public async Task<bool> VerifyBackupIntegrityAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    return false;
                }

                // Calculate file checksum
                using var stream = File.OpenRead(backupPath);
                using var sha256 = SHA256.Create();
                var hash = await sha256.ComputeHashAsync(stream);
                var checksum = Convert.ToBase64String(hash);

                _logger.LogDebug("Backup checksum: {Checksum}", checksum);

                // Verify tar archive structure
                try
                {
                    using var fileStream = File.OpenRead(backupPath);
                    Stream gzipStream = backupPath.EndsWith(".gz") 
                        ? new GZipStream(fileStream, CompressionMode.Decompress) 
                        : fileStream;
                    
                    // Read tar header to verify format
                    var buffer = new byte[512];
                    var bytesRead = await gzipStream.ReadAsync(buffer, 0, 512);
                    
                    if (bytesRead < 512)
                    {
                        _logger.LogError("Invalid tar archive: insufficient header data");
                        return false;
                    }

                    // Basic tar header validation
                    var magic = Encoding.ASCII.GetString(buffer, 257, 5);
                    if (magic != "ustar")
                    {
                        _logger.LogWarning("Non-standard tar archive format");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to verify tar archive structure");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify backup integrity");
                return false;
            }
        }

        public async Task<BackupMetadata> GetBackupMetadataAsync(string backupPath)
        {
            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException("Backup file not found", backupPath);
            }

            // Extract metadata without full restore
            var tempDir = Path.Combine(Path.GetTempPath(), $"backup_meta_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                await ExtractTarArchiveAsync(backupPath, tempDir, "backup.json");
                
                var metadataPath = Path.Combine(tempDir, "backup.json");
                if (!File.Exists(metadataPath))
                {
                    throw new InvalidOperationException("Backup metadata not found");
                }

                var metadataJson = await File.ReadAllTextAsync(metadataPath);
                var metadata = JsonSerializer.Deserialize<BackupMetadata>(metadataJson);
                
                if (metadata == null)
                {
                    throw new InvalidOperationException("Invalid backup metadata");
                }

                return metadata;
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        public async Task<bool> CompactStorageAsync()
        {
            await _compactionSemaphore.WaitAsync();
            try
            {
                _logger.LogInformation("Starting storage compaction");

                var stats = await GetStorageStatisticsAsync();
                
                if (stats.FragmentationRatio < 0.2)
                {
                    _logger.LogInformation("Storage fragmentation is low ({Ratio:P}), skipping compaction", 
                        stats.FragmentationRatio);
                    return true;
                }

                // Create backup before compaction
                var backupPath = await CreateBackupAsync($"pre_compaction_{DateTime.UtcNow:yyyyMMdd_HHmmss}", true);
                _logger.LogInformation("Created pre-compaction backup: {BackupPath}", backupPath);

                var dataDir = Path.Combine(_basePath, "data");
                var metadataDir = Path.Combine(_basePath, "metadata");
                var tempDir = Path.Combine(_basePath, ".compaction_temp");

                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
                Directory.CreateDirectory(tempDir);

                try
                {
                    var tempDataDir = Path.Combine(tempDir, "data");
                    var tempMetadataDir = Path.Combine(tempDir, "metadata");
                    Directory.CreateDirectory(tempDataDir);
                    Directory.CreateDirectory(tempMetadataDir);

                    // Copy non-expired data to temp directory
                    var copiedCount = 0;
                    var skippedCount = 0;

                    if (Directory.Exists(metadataDir))
                    {
                        foreach (var metadataFile in Directory.GetFiles(metadataDir, "*.metadata"))
                        {
                            var metadataJson = await File.ReadAllTextAsync(metadataFile);
                            var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson);
                            
                            if (metadata != null && metadata.TryGetValue("ExpiresAt", out var expiresAtObj))
                            {
                                if (DateTime.TryParse(expiresAtObj.ToString(), out var expiresAt) && 
                                    expiresAt <= DateTime.UtcNow)
                                {
                                    skippedCount++;
                                    continue; // Skip expired data
                                }
                            }

                            // Copy valid data
                            var fileName = Path.GetFileName(metadataFile);
                            var dataFileName = fileName.Replace(".metadata", ".data");
                            var dataFile = Path.Combine(dataDir, dataFileName);

                            if (File.Exists(dataFile))
                            {
                                await Task.Run(() => File.Copy(metadataFile, Path.Combine(tempMetadataDir, fileName), overwrite: true));
                                await Task.Run(() => File.Copy(dataFile, Path.Combine(tempDataDir, dataFileName), overwrite: true));
                                copiedCount++;
                            }
                        }
                    }

                    // Replace old directories with compacted ones
                    if (Directory.Exists(dataDir))
                    {
                        Directory.Delete(dataDir, true);
                    }
                    if (Directory.Exists(metadataDir))
                    {
                        Directory.Delete(metadataDir, true);
                    }

                    Directory.Move(tempDataDir, dataDir);
                    Directory.Move(tempMetadataDir, metadataDir);

                    // Cleanup temp directory
                    Directory.Delete(tempDir, true);

                    _logger.LogInformation("Compaction completed. Copied: {Copied}, Skipped: {Skipped}", 
                        copiedCount, skippedCount);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Compaction failed, restoring from backup");
                    
                    // Restore from backup on failure
                    await RestoreBackupAsync(backupPath);
                    
                    throw;
                }
            }
            finally
            {
                _compactionSemaphore.Release();
            }
        }

        public async Task<StorageStatistics> GetStorageStatisticsAsync()
        {
            var stats = new StorageStatistics();
            
            var dataDir = Path.Combine(_basePath, "data");
            var metadataDir = Path.Combine(_basePath, "metadata");

            if (Directory.Exists(dataDir))
            {
                var dataFiles = Directory.GetFiles(dataDir, "*", SearchOption.AllDirectories);
                stats.FileCount = dataFiles.Length;
                stats.TotalSize = dataFiles.Sum(f => new FileInfo(f).Length);
            }

            if (Directory.Exists(metadataDir))
            {
                var expiredCount = 0;
                foreach (var metadataFile in Directory.GetFiles(metadataDir, "*.metadata"))
                {
                    var metadataJson = await File.ReadAllTextAsync(metadataFile);
                    var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson);
                    
                    if (metadata != null && metadata.TryGetValue("ExpiresAt", out var expiresAtObj))
                    {
                        if (DateTime.TryParse(expiresAtObj.ToString(), out var expiresAt) && 
                            expiresAt <= DateTime.UtcNow)
                        {
                            expiredCount++;
                        }
                    }
                }
                stats.ExpiredCount = expiredCount;
            }

            // Calculate fragmentation ratio
            if (stats.FileCount > 0)
            {
                stats.FragmentationRatio = (double)stats.ExpiredCount / stats.FileCount;
            }

            // Get last compaction time from marker file
            var compactionMarker = Path.Combine(_basePath, ".last_compaction");
            if (File.Exists(compactionMarker))
            {
                stats.LastCompaction = File.GetLastWriteTimeUtc(compactionMarker);
            }

            return stats;
        }

        private async Task CopyDirectoryAsync(string sourceDir, string destDir, BackupMetadata metadata)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                await Task.Run(() => File.Copy(file, destFile, overwrite: true));
                
                // Calculate file checksum
                using var stream = File.OpenRead(destFile);
                using var sha256 = SHA256.Create();
                var hash = await sha256.ComputeHashAsync(stream);
                var checksum = Convert.ToBase64String(hash);
                
                metadata.Files[Path.GetRelativePath(_basePath, file)] = checksum;
                metadata.FileCount++;
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                await CopyDirectoryAsync(dir, destSubDir, metadata);
            }
        }

        private long GetDirectorySize(string path)
        {
            return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);
        }

        private async Task<string> GenerateDirectoryChecksumAsync(string path)
        {
            using var sha256 = SHA256.Create();
            var checksums = new List<string>();

            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories).OrderBy(f => f))
            {
                using var stream = File.OpenRead(file);
                var hash = await sha256.ComputeHashAsync(stream);
                checksums.Add(Convert.ToBase64String(hash));
            }

            var combinedChecksum = string.Join("", checksums);
            var finalHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedChecksum));
            return Convert.ToBase64String(finalHash);
        }

        private async Task CreateTarArchiveAsync(string sourceDir, string outputPath, bool compress)
        {
            // Use system tar command for reliability
            var tarCommand = compress 
                ? $"tar -czf \"{outputPath}\" -C \"{Path.GetDirectoryName(sourceDir)}\" \"{Path.GetFileName(sourceDir)}\""
                : $"tar -cf \"{outputPath}\" -C \"{Path.GetDirectoryName(sourceDir)}\" \"{Path.GetFileName(sourceDir)}\"";

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"{tarCommand}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"Failed to create tar archive: {error}");
            }
        }

        private async Task ExtractTarArchiveAsync(string archivePath, string outputDir, string? specificFile = null)
        {
            // Use system tar command for reliability
            var tarCommand = archivePath.EndsWith(".gz")
                ? $"tar -xzf \"{archivePath}\" -C \"{outputDir}\""
                : $"tar -xf \"{archivePath}\" -C \"{outputDir}\"";

            if (!string.IsNullOrEmpty(specificFile))
            {
                tarCommand += $" \"{specificFile}\"";
            }

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"{tarCommand}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"Failed to extract tar archive: {error}");
            }
        }
    }
}