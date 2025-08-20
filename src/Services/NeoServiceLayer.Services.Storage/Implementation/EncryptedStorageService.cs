using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Storage.Models;
using NeoServiceLayer.Infrastructure.Caching;

namespace NeoServiceLayer.Services.Storage.Implementation
{
    /// <summary>
    /// Encrypted storage service with multi-tier storage support
    /// </summary>
    public class EncryptedStorageService : ServiceBase, IStorageService
    {
        private readonly ILogger<EncryptedStorageService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IFileRepository _fileRepository;
        private readonly IObjectStorageProvider _objectStorage;
        private readonly IEncryptionService _encryptionService;
        private readonly ICacheService _cacheService;
        private readonly IMetricsCollector _metrics;
        
        private readonly string _storagePath;
        private readonly long _maxFileSize;
        private readonly bool _encryptionEnabled;
        private readonly string[] _allowedFileTypes;
        private readonly Dictionary<StorageTier, StorageTierConfig> _tierConfigs;

        public EncryptedStorageService(
            ILogger<EncryptedStorageService> logger,
            IConfiguration configuration,
            IFileRepository fileRepository,
            IObjectStorageProvider objectStorage,
            IEncryptionService encryptionService,
            ICacheService cacheService,
            IMetricsCollector metrics)
            : base("EncryptedStorageService", "Secure file storage service with encryption", "1.0.0", logger)
        {
            _logger = logger;
            _configuration = configuration;
            _fileRepository = fileRepository;
            _objectStorage = objectStorage;
            _encryptionService = encryptionService;
            _cacheService = cacheService;
            _metrics = metrics;

            _storagePath = configuration["Storage:LocalPath"] ?? "/data/storage";
            _maxFileSize = configuration.GetValue<long>("Storage:MaxFileSizeBytes", 100 * 1024 * 1024); // 100MB default
            _encryptionEnabled = configuration.GetValue<bool>("Storage:EncryptionEnabled", true);
            _allowedFileTypes = configuration.GetSection("Storage:AllowedFileTypes").Get<string[]>() 
                ?? new[] { ".txt", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".png", ".zip" };

            // Configure storage tiers
            _tierConfigs = new Dictionary<StorageTier, StorageTierConfig>
            {
                [StorageTier.Hot] = new StorageTierConfig 
                { 
                    MaxSizeMB = 1024, 
                    TTLDays = 7,
                    UseCache = true 
                },
                [StorageTier.Warm] = new StorageTierConfig 
                { 
                    MaxSizeMB = 10240, 
                    TTLDays = 30,
                    UseCache = false 
                },
                [StorageTier.Cold] = new StorageTierConfig 
                { 
                    MaxSizeMB = long.MaxValue, 
                    TTLDays = 365,
                    UseCache = false,
                    UseCompression = true 
                }
            };
        }

        public async Task<StorageFile> UploadFileAsync(UploadFileRequest request)
        {
            try
            {
                // Validate request
                ValidateUploadRequest(request);

                var fileId = Guid.NewGuid();
                var fileName = SanitizeFileName(request.FileName);
                var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();

                // Check file type
                if (!_allowedFileTypes.Contains(fileExtension))
                {
                    throw new InvalidOperationException($"File type {fileExtension} is not allowed");
                }

                // Check file size
                if (request.FileStream.Length > _maxFileSize)
                {
                    throw new InvalidOperationException($"File size exceeds maximum allowed size of {_maxFileSize} bytes");
                }

                _logger.LogInformation("Uploading file {FileName} with ID {FileId}", fileName, fileId);

                // Calculate file hash
                var fileHash = await CalculateFileHashAsync(request.FileStream);

                // Check for duplicate
                var existingFile = await _fileRepository.GetByHashAsync(fileHash);
                if (existingFile != null && request.PreventDuplicates)
                {
                    _logger.LogInformation("Duplicate file detected, returning existing file {FileId}", existingFile.Id);
                    return existingFile;
                }

                // Encrypt file if enabled
                byte[] encryptedData = null;
                string encryptionKey = null;
                if (_encryptionEnabled && request.Encrypt)
                {
                    (encryptedData, encryptionKey) = await EncryptFileAsync(request.FileStream);
                }

                // Determine storage tier
                var storageTier = DetermineStorageTier(request.FileStream.Length, request.AccessPattern);

                // Store file
                string storagePath;
                if (storageTier == StorageTier.Hot)
                {
                    storagePath = await StoreLocallyAsync(fileId, encryptedData ?? await StreamToByteArrayAsync(request.FileStream));
                }
                else
                {
                    storagePath = await StoreInObjectStorageAsync(fileId, encryptedData ?? await StreamToByteArrayAsync(request.FileStream), storageTier);
                }

                // Create file record
                var file = new StorageFile
                {
                    Id = fileId,
                    FileName = fileName,
                    OriginalFileName = request.FileName,
                    FileSize = request.FileStream.Length,
                    FileHash = fileHash,
                    ContentType = request.ContentType ?? GetContentType(fileExtension),
                    StoragePath = storagePath,
                    StorageTier = storageTier,
                    IsEncrypted = _encryptionEnabled && request.Encrypt,
                    EncryptionKeyId = encryptionKey,
                    Metadata = request.Metadata ?? new Dictionary<string, string>(),
                    Tags = request.Tags ?? new List<string>(),
                    UploadedBy = request.UserId,
                    UploadedAt = DateTime.UtcNow,
                    AccessCount = 0,
                    LastAccessedAt = null,
                    ExpiresAt = storageTier == StorageTier.Hot 
                        ? DateTime.UtcNow.AddDays(_tierConfigs[storageTier].TTLDays) 
                        : null
                };

                // Save to database
                await _fileRepository.CreateAsync(file);

                // Cache if hot tier
                if (storageTier == StorageTier.Hot && _tierConfigs[StorageTier.Hot].UseCache)
                {
                    await _cacheService.SetAsync($"file:{fileId}", file, TimeSpan.FromHours(1));
                }

                _metrics.IncrementCounter("storage.files.uploaded",
                    new[] { ("tier", storageTier.ToString()), ("encrypted", file.IsEncrypted.ToString()) });
                _metrics.RecordValue("storage.file.size", request.FileStream.Length);

                _logger.LogInformation("File {FileId} uploaded successfully to {StorageTier} tier", fileId, storageTier);

                return file;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName}", request.FileName);
                _metrics.IncrementCounter("storage.files.upload_error");
                throw;
            }
        }

        public async Task<Stream> DownloadFileAsync(Guid fileId, string userId = null)
        {
            try
            {
                _logger.LogDebug("Downloading file {FileId}", fileId);

                // Try cache first
                var file = await _cacheService.GetAsync<StorageFile>($"file:{fileId}");
                if (file == null)
                {
                    file = await _fileRepository.GetByIdAsync(fileId);
                    if (file == null)
                    {
                        throw new FileNotFoundException($"File {fileId} not found");
                    }
                }

                // Check access permissions
                if (!await CheckAccessPermissionsAsync(file, userId))
                {
                    throw new UnauthorizedAccessException($"Access denied to file {fileId}");
                }

                // Update access metrics
                file.AccessCount++;
                file.LastAccessedAt = DateTime.UtcNow;
                await _fileRepository.UpdateAccessMetricsAsync(fileId, file.AccessCount, file.LastAccessedAt.Value);

                // Retrieve file data
                byte[] fileData;
                if (file.StorageTier == StorageTier.Hot)
                {
                    fileData = await RetrieveLocalFileAsync(file.StoragePath);
                }
                else
                {
                    fileData = await RetrieveFromObjectStorageAsync(file.StoragePath);
                    
                    // Consider promoting to hot tier if frequently accessed
                    if (file.AccessCount > 10 && (DateTime.UtcNow - file.LastAccessedAt.Value).TotalDays < 7)
                    {
                        _ = Task.Run(() => PromoteToHotTierAsync(file));
                    }
                }

                // Decrypt if encrypted
                if (file.IsEncrypted)
                {
                    fileData = await DecryptFileAsync(fileData, file.EncryptionKeyId);
                }

                _metrics.IncrementCounter("storage.files.downloaded",
                    new[] { ("tier", file.StorageTier.ToString()) });

                return new MemoryStream(fileData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileId}", fileId);
                _metrics.IncrementCounter("storage.files.download_error");
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(Guid fileId, string userId = null)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                {
                    return false;
                }

                // Check delete permissions
                if (!await CheckDeletePermissionsAsync(file, userId))
                {
                    throw new UnauthorizedAccessException($"Delete access denied for file {fileId}");
                }

                _logger.LogInformation("Deleting file {FileId}: {FileName}", fileId, file.FileName);

                // Delete from storage
                if (file.StorageTier == StorageTier.Hot)
                {
                    await DeleteLocalFileAsync(file.StoragePath);
                }
                else
                {
                    await DeleteFromObjectStorageAsync(file.StoragePath);
                }

                // Delete from database
                await _fileRepository.DeleteAsync(fileId);

                // Remove from cache
                await _cacheService.RemoveAsync($"file:{fileId}");

                _metrics.IncrementCounter("storage.files.deleted");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileId}", fileId);
                _metrics.IncrementCounter("storage.files.delete_error");
                throw;
            }
        }

        public async Task<StorageFile> GetFileMetadataAsync(Guid fileId)
        {
            var file = await _cacheService.GetAsync<StorageFile>($"file:{fileId}");
            if (file == null)
            {
                file = await _fileRepository.GetByIdAsync(fileId);
                if (file != null && file.StorageTier == StorageTier.Hot)
                {
                    await _cacheService.SetAsync($"file:{fileId}", file, TimeSpan.FromHours(1));
                }
            }
            return file;
        }

        public async Task<IEnumerable<StorageFile>> ListFilesAsync(ListFilesRequest request)
        {
            var filter = new FileFilter
            {
                UserId = request.UserId,
                Tags = request.Tags,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                StorageTier = request.StorageTier,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            return await _fileRepository.ListAsync(filter);
        }

        public async Task<StorageStatistics> GetStorageStatisticsAsync(string userId = null)
        {
            var stats = await _fileRepository.GetStatisticsAsync(userId);
            
            return new StorageStatistics
            {
                TotalFiles = stats.TotalFiles,
                TotalSizeBytes = stats.TotalSizeBytes,
                FilesByTier = stats.FilesByTier,
                FilesByType = stats.FilesByType,
                StorageUsageByDay = stats.StorageUsageByDay,
                AverageFileSizeBytes = stats.TotalFiles > 0 ? stats.TotalSizeBytes / stats.TotalFiles : 0
            };
        }

        private void ValidateUploadRequest(UploadFileRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            
            if (string.IsNullOrWhiteSpace(request.FileName))
                throw new ArgumentException("File name is required", nameof(request.FileName));
            
            if (request.FileStream == null || request.FileStream.Length == 0)
                throw new ArgumentException("File stream is required and must contain data", nameof(request.FileStream));
        }

        private string SanitizeFileName(string fileName)
        {
            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            
            // Limit length
            if (sanitized.Length > 255)
            {
                var extension = Path.GetExtension(sanitized);
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
                sanitized = nameWithoutExtension.Substring(0, 255 - extension.Length) + extension;
            }

            return sanitized;
        }

        private async Task<string> CalculateFileHashAsync(Stream fileStream)
        {
            fileStream.Position = 0;
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(fileStream);
            fileStream.Position = 0;
            return Convert.ToBase64String(hash);
        }

        private async Task<(byte[] encryptedData, string keyId)> EncryptFileAsync(Stream fileStream)
        {
            var data = await StreamToByteArrayAsync(fileStream);
            var keyId = Guid.NewGuid().ToString();
            var encryptedData = await _encryptionService.EncryptAsync(data, keyId);
            return (encryptedData, keyId);
        }

        private async Task<byte[]> DecryptFileAsync(byte[] encryptedData, string keyId)
        {
            return await _encryptionService.DecryptAsync(encryptedData, keyId);
        }

        private StorageTier DetermineStorageTier(long fileSize, AccessPattern? accessPattern)
        {
            // Use access pattern if specified
            if (accessPattern.HasValue)
            {
                return accessPattern.Value switch
                {
                    AccessPattern.Frequent => StorageTier.Hot,
                    AccessPattern.Moderate => StorageTier.Warm,
                    AccessPattern.Rare => StorageTier.Cold,
                    _ => StorageTier.Warm
                };
            }

            // Otherwise determine by size
            if (fileSize < 1024 * 1024) // < 1MB
                return StorageTier.Hot;
            else if (fileSize < 100 * 1024 * 1024) // < 100MB
                return StorageTier.Warm;
            else
                return StorageTier.Cold;
        }

        private async Task<string> StoreLocallyAsync(Guid fileId, byte[] data)
        {
            var directory = Path.Combine(_storagePath, "hot", DateTime.UtcNow.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(directory);
            
            var filePath = Path.Combine(directory, $"{fileId}.dat");
            await File.WriteAllBytesAsync(filePath, data);
            
            return filePath;
        }

        private async Task<string> StoreInObjectStorageAsync(Guid fileId, byte[] data, StorageTier tier)
        {
            var key = $"{tier.ToString().ToLower()}/{DateTime.UtcNow:yyyy-MM-dd}/{fileId}.dat";
            
            if (_tierConfigs[tier].UseCompression)
            {
                data = await CompressDataAsync(data);
                key += ".gz";
            }

            await _objectStorage.PutObjectAsync(key, data);
            return key;
        }

        private async Task<byte[]> RetrieveLocalFileAsync(string path)
        {
            return await File.ReadAllBytesAsync(path);
        }

        private async Task<byte[]> RetrieveFromObjectStorageAsync(string key)
        {
            var data = await _objectStorage.GetObjectAsync(key);
            
            if (key.EndsWith(".gz"))
            {
                data = await DecompressDataAsync(data);
            }

            return data;
        }

        private async Task DeleteLocalFileAsync(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            await Task.CompletedTask;
        }

        private async Task DeleteFromObjectStorageAsync(string key)
        {
            await _objectStorage.DeleteObjectAsync(key);
        }

        private async Task PromoteToHotTierAsync(StorageFile file)
        {
            try
            {
                _logger.LogInformation("Promoting file {FileId} to hot tier", file.Id);
                
                // Retrieve from current location
                var data = await RetrieveFromObjectStorageAsync(file.StoragePath);
                
                // Store in hot tier
                var newPath = await StoreLocallyAsync(file.Id, data);
                
                // Delete from old location
                await DeleteFromObjectStorageAsync(file.StoragePath);
                
                // Update database
                file.StoragePath = newPath;
                file.StorageTier = StorageTier.Hot;
                await _fileRepository.UpdateStorageInfoAsync(file.Id, newPath, StorageTier.Hot);
                
                // Update cache
                await _cacheService.SetAsync($"file:{file.Id}", file, TimeSpan.FromHours(1));
                
                _metrics.IncrementCounter("storage.files.promoted_to_hot");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting file {FileId} to hot tier", file.Id);
            }
        }

        private async Task<bool> CheckAccessPermissionsAsync(StorageFile file, string userId)
        {
            // Implement your access control logic
            if (string.IsNullOrEmpty(userId))
                return file.IsPublic;
            
            return file.UploadedBy == userId || file.SharedWith?.Contains(userId) == true;
        }

        private async Task<bool> CheckDeletePermissionsAsync(StorageFile file, string userId)
        {
            // Only owner can delete
            return await Task.FromResult(file.UploadedBy == userId);
        }

        private string GetContentType(string extension)
        {
            return extension.ToLower() switch
            {
                ".txt" => "text/plain",
                ".pdf" => "application/pdf",
                ".doc" or ".docx" => "application/msword",
                ".xls" or ".xlsx" => "application/vnd.ms-excel",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
        }

        private async Task<byte[]> StreamToByteArrayAsync(Stream stream)
        {
            if (stream is MemoryStream ms)
                return ms.ToArray();

            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        private async Task<byte[]> CompressDataAsync(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.Optimal))
            {
                await gzip.WriteAsync(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private async Task<byte[]> DecompressDataAsync(byte[] compressedData)
        {
            using var input = new MemoryStream(compressedData);
            using var output = new MemoryStream();
            using (var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress))
            {
                await gzip.CopyToAsync(output);
            }
            return output.ToArray();
        }

        protected override async Task<ServiceHealth> OnGetHealthAsync()
        {
            try
            {
                var dbHealthy = await _fileRepository.CheckHealthAsync();
                var storageHealthy = await _objectStorage.CheckHealthAsync();
                var cacheHealthy = await _cacheService.CheckHealthAsync();

                if (dbHealthy && storageHealthy && cacheHealthy)
                    return ServiceHealth.Healthy;
                else if (dbHealthy && storageHealthy)
                    return ServiceHealth.Degraded;
                else
                    return ServiceHealth.Unhealthy;
            }
            catch
            {
                return ServiceHealth.Unhealthy;
            }
        }

        protected override Task<bool> OnInitializeAsync()
        {
            // Ensure storage directories exist
            Directory.CreateDirectory(Path.Combine(_storagePath, "hot"));
            _logger.LogInformation("Encrypted Storage Service initialized");
            return Task.FromResult(true);
        }

        protected override Task<bool> OnStartAsync()
        {
            _logger.LogInformation("Encrypted Storage Service started");
            return Task.FromResult(true);
        }

        protected override Task<bool> OnStopAsync()
        {
            _logger.LogInformation("Encrypted Storage Service stopped");
            return Task.FromResult(true);
        }
    }
}