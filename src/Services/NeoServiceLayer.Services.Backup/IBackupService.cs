using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Backup.Models;

namespace NeoServiceLayer.Services.Backup;

/// <summary>
/// Interface for the Backup Service that provides data backup and recovery capabilities.
/// </summary>
public interface IBackupService : IService
{
    /// <summary>
    /// Creates a backup of the specified data.
    /// </summary>
    /// <param name="request">The backup creation request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The backup creation result.</returns>
    Task<BackupResult> CreateBackupAsync(CreateBackupRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Restores data from a backup.
    /// </summary>
    /// <param name="request">The restore request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The restore result.</returns>
    Task<RestoreResult> RestoreBackupAsync(RestoreBackupRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets the status of a backup operation.
    /// </summary>
    /// <param name="request">The backup status request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The backup status.</returns>
    Task<BackupStatusResult> GetBackupStatusAsync(BackupStatusRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Lists available backups.
    /// </summary>
    /// <param name="request">The list backups request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The list of available backups.</returns>
    Task<BackupListResult> ListBackupsAsync(ListBackupsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Deletes a backup.
    /// </summary>
    /// <param name="request">The delete backup request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The deletion result.</returns>
    Task<BackupDeletionResult> DeleteBackupAsync(DeleteBackupRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Validates the integrity of a backup.
    /// </summary>
    /// <param name="request">The backup validation request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The validation result.</returns>
    Task<BackupValidationResult> ValidateBackupAsync(ValidateBackupRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Creates a scheduled backup job.
    /// </summary>
    /// <param name="request">The scheduled backup request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The scheduled backup result.</returns>
    Task<ScheduledBackupResult> CreateScheduledBackupAsync(CreateScheduledBackupRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets backup statistics and metrics.
    /// </summary>
    /// <param name="request">The backup statistics request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The backup statistics.</returns>
    Task<BackupStatisticsResult> GetBackupStatisticsAsync(BackupStatisticsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Exports a backup to an external location.
    /// </summary>
    /// <param name="request">The backup export request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The export result.</returns>
    Task<BackupExportResult> ExportBackupAsync(ExportBackupRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Imports a backup from an external location.
    /// </summary>
    /// <param name="request">The backup import request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The import result.</returns>
    Task<BackupImportResult> ImportBackupAsync(ImportBackupRequest request, BlockchainType blockchainType);
}


