using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL
{
    /// <summary>
    /// PostgreSQL migration runner for Neo Service Layer database schema.
    /// </summary>
    public class PostgreSQLMigrationRunner
    {
        private readonly string _connectionString;
        private readonly ILogger<PostgreSQLMigrationRunner> _logger;

        public PostgreSQLMigrationRunner(string connectionString, ILogger<PostgreSQLMigrationRunner> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Runs all pending migrations for the Neo Service Layer database.
        /// </summary>
        public async Task RunMigrationsAsync()
        {
            try
            {
                _logger.LogInformation("Starting PostgreSQL database migrations for Neo Service Layer");

                await CreateMigrationTableIfNotExistsAsync();
                
                var migrationFiles = GetMigrationFiles();
                
                foreach (var migrationFile in migrationFiles)
                {
                    await RunMigrationIfNotAppliedAsync(migrationFile);
                }

                _logger.LogInformation("PostgreSQL database migrations completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run PostgreSQL database migrations");
                throw;
            }
        }

        /// <summary>
        /// Creates the migration tracking table if it doesn't exist.
        /// </summary>
        private async Task CreateMigrationTableIfNotExistsAsync()
        {
            const string createMigrationTableSql = @"
                CREATE TABLE IF NOT EXISTS __migrations (
                    id SERIAL PRIMARY KEY,
                    migration_name VARCHAR(255) NOT NULL UNIQUE,
                    applied_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                    checksum VARCHAR(64)
                );
                
                CREATE INDEX IF NOT EXISTS idx_migrations_name ON __migrations(migration_name);
            ";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(createMigrationTableSql, connection);
            await command.ExecuteNonQueryAsync();
            
            _logger.LogDebug("Migration tracking table ensured");
        }

        /// <summary>
        /// Gets all migration files from the Migrations directory.
        /// </summary>
        private string[] GetMigrationFiles()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyLocation = assembly.Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            var migrationsDirectory = Path.Combine(assemblyDirectory!, "PostgreSQL", "Migrations");
            
            // If running from build output, look for embedded resources instead
            if (!Directory.Exists(migrationsDirectory))
            {
                var resourceNames = assembly.GetManifestResourceNames();
                var migrationResources = Array.FindAll(resourceNames, name => 
                    name.Contains("Migrations") && name.EndsWith(".sql"));
                
                _logger.LogDebug("Found {Count} migration resources in assembly", migrationResources.Length);
                return migrationResources;
            }
            
            var files = Directory.GetFiles(migrationsDirectory, "*.sql");
            Array.Sort(files); // Ensure ordered execution
            
            _logger.LogDebug("Found {Count} migration files in {Directory}", files.Length, migrationsDirectory);
            return files;
        }

        /// <summary>
        /// Runs a migration if it hasn't been applied yet.
        /// </summary>
        private async Task RunMigrationIfNotAppliedAsync(string migrationFileOrResource)
        {
            var migrationName = Path.GetFileNameWithoutExtension(migrationFileOrResource);
            
            // Check if migration has already been applied
            if (await IsMigrationAppliedAsync(migrationName))
            {
                _logger.LogDebug("Migration {MigrationName} has already been applied, skipping", migrationName);
                return;
            }

            _logger.LogInformation("Applying migration: {MigrationName}", migrationName);

            string migrationSql;
            
            // Check if this is an embedded resource or a file path
            if (migrationFileOrResource.Contains(",")) // Embedded resource format
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream(migrationFileOrResource);
                if (stream == null)
                {
                    throw new InvalidOperationException($"Migration resource {migrationFileOrResource} not found");
                }
                
                using var reader = new StreamReader(stream);
                migrationSql = await reader.ReadToEndAsync();
            }
            else
            {
                migrationSql = await File.ReadAllTextAsync(migrationFileOrResource);
            }

            // Calculate checksum for integrity verification
            var checksum = CalculateChecksum(migrationSql);

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                // Execute the migration
                using var migrationCommand = new NpgsqlCommand(migrationSql, connection, transaction);
                migrationCommand.CommandTimeout = 300; // 5 minutes timeout
                await migrationCommand.ExecuteNonQueryAsync();

                // Record the migration as applied
                const string recordMigrationSql = @"
                    INSERT INTO __migrations (migration_name, checksum) 
                    VALUES (@migrationName, @checksum)";
                
                using var recordCommand = new NpgsqlCommand(recordMigrationSql, connection, transaction);
                recordCommand.Parameters.AddWithValue("@migrationName", migrationName);
                recordCommand.Parameters.AddWithValue("@checksum", checksum);
                await recordCommand.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                
                _logger.LogInformation("Successfully applied migration: {MigrationName}", migrationName);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Checks if a migration has already been applied.
        /// </summary>
        private async Task<bool> IsMigrationAppliedAsync(string migrationName)
        {
            const string checkMigrationSql = @"
                SELECT COUNT(*) FROM __migrations WHERE migration_name = @migrationName";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(checkMigrationSql, connection);
            command.Parameters.AddWithValue("@migrationName", migrationName);
            
            var count = (long)(await command.ExecuteScalarAsync() ?? 0L);
            return count > 0;
        }

        /// <summary>
        /// Calculates SHA256 checksum of migration content.
        /// </summary>
        private static string CalculateChecksum(string content)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        /// <summary>
        /// Gets the status of all migrations.
        /// </summary>
        public async Task<(string Name, DateTime AppliedAt, string Checksum)[]> GetMigrationStatusAsync()
        {
            const string getMigrationsSql = @"
                SELECT migration_name, applied_at, checksum 
                FROM __migrations 
                ORDER BY applied_at";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(getMigrationsSql, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            var migrations = new List<(string Name, DateTime AppliedAt, string Checksum)>();
            
            while (await reader.ReadAsync())
            {
                migrations.Add((
                    reader.GetString(reader.GetOrdinal("migration_name")),
                    reader.GetDateTime(reader.GetOrdinal("applied_at")),
                    reader.GetString(reader.GetOrdinal("checksum"))
                ));
            }
            
            return migrations.ToArray();
        }

        /// <summary>
        /// Tests the database connection.
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = new NpgsqlCommand("SELECT 1", connection);
                var result = await command.ExecuteScalarAsync();
                
                return result?.ToString() == "1";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to PostgreSQL database");
                return false;
            }
        }
    }
}