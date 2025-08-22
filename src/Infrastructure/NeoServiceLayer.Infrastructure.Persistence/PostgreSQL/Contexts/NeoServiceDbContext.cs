using Microsoft.EntityFrameworkCore;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Entities;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Contexts
{
    /// <summary>
    /// PostgreSQL Entity Framework DbContext for Neo Service Layer
    /// </summary>
    public class NeoServiceDbContext : DbContext
    {
        public NeoServiceDbContext(DbContextOptions<NeoServiceDbContext> options) : base(options)
        {
        }

        // Entity sets will be configured via model configuration
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure entities
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(NeoServiceDbContext).Assembly);
            
            // Set default schema
            modelBuilder.HasDefaultSchema("neoservice");
        }
    }
}