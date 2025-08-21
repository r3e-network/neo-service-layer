using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Entities.OracleEntities
{
    /// <summary>
    /// Oracle data feed entity for PostgreSQL persistence.
    /// </summary>
    [Table("oracle_data_feeds", Schema = "oracle")]
    public class OracleDataFeed
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        [Column("feed_id")]
        public string FeedId { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("feed_type")]
        public string FeedType { get; set; } = "PriceFeed";

        [Column("value", TypeName = "decimal(38,18)")]
        public decimal? Value { get; set; }

        [Column("value_string")]
        public string? ValueString { get; set; }

        [Column("confidence_score", TypeName = "decimal(5,4)")]
        public decimal? ConfidenceScore { get; set; }

        [MaxLength(100)]
        [Column("source")]
        public string? Source { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("metadata", TypeName = "jsonb")]
        public string? Metadata { get; set; }

        // Navigation properties
        public virtual ICollection<FeedHistory> History { get; set; } = new List<FeedHistory>();
    }

    /// <summary>
    /// Oracle feed history entity for PostgreSQL persistence.
    /// </summary>
    [Table("feed_history", Schema = "oracle")]
    public class FeedHistory
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        [Column("feed_id")]
        public string FeedId { get; set; } = string.Empty;

        [Column("value", TypeName = "decimal(38,18)")]
        public decimal? Value { get; set; }

        [Column("value_string")]
        public string? ValueString { get; set; }

        [Column("confidence_score", TypeName = "decimal(5,4)")]
        public decimal? ConfidenceScore { get; set; }

        [MaxLength(100)]
        [Column("source")]
        public string? Source { get; set; }

        [Column("recorded_at")]
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        [Column("metadata", TypeName = "jsonb")]
        public string? Metadata { get; set; }

        // Navigation properties
        [ForeignKey("FeedId")]
        public virtual OracleDataFeed? Feed { get; set; }
    }
}