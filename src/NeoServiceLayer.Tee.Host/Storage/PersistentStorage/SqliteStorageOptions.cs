using System;
using System.IO;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage
{
    /// <summary>
    /// Options for the SQLite storage provider.
    /// </summary>
    public class SqliteStorageOptions
    {
        /// <summary>
        /// Gets or sets the path to the SQLite database file.
        /// </summary>
        public string DatabaseFile { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sqlite_storage.db");

        /// <summary>
        /// Gets or sets whether to use the Write-Ahead Logging (WAL) journal mode.
        /// </summary>
        public bool UseWalJournalMode { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable foreign keys.
        /// </summary>
        public bool EnableForeignKeys { get; set; } = true;

        /// <summary>
        /// Gets or sets the busy timeout in milliseconds.
        /// </summary>
        public int BusyTimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Gets or sets the cache size in pages.
        /// </summary>
        public int CacheSizePages { get; set; } = 2000;

        /// <summary>
        /// Gets or sets whether to enable automatic vacuum.
        /// </summary>
        public bool EnableAutoVacuum { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum size of the database file in bytes.
        /// </summary>
        public long MaxDatabaseSizeBytes { get; set; } = 1024 * 1024 * 1024; // 1 GB

        /// <summary>
        /// Gets or sets a value indicating whether to create a backup before compaction.
        /// </summary>
        public bool CreateBackupBeforeCompaction { get; set; } = true;

        /// <summary>
        /// Gets or sets the directory where backups are stored.
        /// </summary>
        public string BackupDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sqlite_backup");
    }
}
