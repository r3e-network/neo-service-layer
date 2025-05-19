using System;

namespace NeoServiceLayer.Tee.Host.Storage.Providers
{
    /// <summary>
    /// Options for the SQLite storage provider.
    /// </summary>
    public class SqliteStorageOptions
    {
        /// <summary>
        /// Gets or sets the path to the SQLite database file.
        /// </summary>
        public string DatabasePath { get; set; } = "storage.db";

        /// <summary>
        /// Gets or sets a value indicating whether to create the database if it doesn't exist.
        /// </summary>
        public bool CreateDatabaseIfNotExists { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to use a cache.
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of items to keep in the cache.
        /// </summary>
        public int MaxCacheItems { get; set; } = 1000;

        /// <summary>
        /// Gets or sets a value indicating whether to use WAL mode.
        /// </summary>
        public bool UseWalMode { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to use synchronous mode.
        /// </summary>
        public bool UseSynchronousMode { get; set; } = true;

        /// <summary>
        /// Gets or sets the journal mode.
        /// </summary>
        public string JournalMode { get; set; } = "WAL";

        /// <summary>
        /// Gets or sets the synchronous mode.
        /// </summary>
        public string SynchronousMode { get; set; } = "NORMAL";

        /// <summary>
        /// Gets or sets the page size in bytes.
        /// </summary>
        public int PageSize { get; set; } = 4096;

        /// <summary>
        /// Gets or sets the cache size in pages.
        /// </summary>
        public int CacheSize { get; set; } = 2000;

        /// <summary>
        /// Gets or sets a value indicating whether to create a backup before vacuum.
        /// </summary>
        public bool CreateBackupBeforeVacuum { get; set; } = true;

        /// <summary>
        /// Gets or sets the directory where backups are stored.
        /// </summary>
        public string BackupDirectory { get; set; } = "sqlite_backup";
    }
}
