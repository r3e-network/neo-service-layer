using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Entities.CrossChainEntities
{
    /// <summary>
    /// Cross-chain operation entity for PostgreSQL persistence.
    /// </summary>
    [Table("cross_chain_operations", Schema = "crosschain")]
    public class CrossChainOperation
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        [Column("operation_id")]
        public string OperationId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("operation_type")]
        public string OperationType { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("source_chain")]
        public string SourceChain { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("target_chain")]
        public string TargetChain { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("status")]
        public string Status { get; set; } = "Pending";

        [MaxLength(255)]
        [Column("transaction_hash")]
        public string? TransactionHash { get; set; }

        [Column("block_number")]
        public long? BlockNumber { get; set; }

        [Column("gas_used")]
        public long? GasUsed { get; set; }

        [Column("gas_price")]
        public long? GasPrice { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [Column("retry_count")]
        public int RetryCount { get; set; } = 0;

        [Column("max_retries")]
        public int MaxRetries { get; set; } = 3;

        [Column("metadata", TypeName = "jsonb")]
        public string? Metadata { get; set; }

        // Navigation properties
        public virtual ICollection<TokenTransfer> TokenTransfers { get; set; } = new List<TokenTransfer>();
    }

    /// <summary>
    /// Token transfer entity for PostgreSQL persistence.
    /// </summary>
    [Table("token_transfers", Schema = "crosschain")]
    public class TokenTransfer
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("operation_id")]
        public Guid OperationId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("from_address")]
        public string FromAddress { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("to_address")]
        public string ToAddress { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("token_contract")]
        public string? TokenContract { get; set; }

        [Required]
        [Column("amount", TypeName = "decimal(38,18)")]
        public decimal Amount { get; set; }

        [Column("decimals")]
        public int Decimals { get; set; } = 18;

        [MaxLength(255)]
        [Column("source_tx_hash")]
        public string? SourceTxHash { get; set; }

        [MaxLength(255)]
        [Column("target_tx_hash")]
        public string? TargetTxHash { get; set; }

        [Column("bridge_fee", TypeName = "decimal(38,18)")]
        public decimal? BridgeFee { get; set; }

        [Column("exchange_rate", TypeName = "decimal(38,18)")]
        public decimal? ExchangeRate { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("metadata", TypeName = "jsonb")]
        public string? Metadata { get; set; }

        // Navigation properties
        [ForeignKey("OperationId")]
        public virtual CrossChainOperation? Operation { get; set; }
    }
}