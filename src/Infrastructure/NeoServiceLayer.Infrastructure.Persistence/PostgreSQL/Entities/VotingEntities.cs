using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Entities.VotingEntities
{
    /// <summary>
    /// Voting proposal entity for PostgreSQL persistence.
    /// </summary>
    [Table("voting_proposals", Schema = "voting")]
    public class VotingProposal
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("proposal_type")]
        public string ProposalType { get; set; } = "SimpleVoting";

        [Required]
        [MaxLength(50)]
        [Column("status")]
        public string Status { get; set; } = "Active";

        [Column("created_by")]
        public Guid? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("starts_at")]
        public DateTime StartsAt { get; set; }

        [Column("ends_at")]
        public DateTime EndsAt { get; set; }

        [Column("minimum_participation")]
        public int? MinimumParticipation { get; set; }

        [Column("required_majority", TypeName = "decimal(5,4)")]
        public decimal? RequiredMajority { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("metadata", TypeName = "jsonb")]
        public string? Metadata { get; set; }

        // Navigation properties
        public virtual ICollection<VotingOption> Options { get; set; } = new List<VotingOption>();
        public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
        public virtual VotingResult? Result { get; set; }
    }

    /// <summary>
    /// Voting option entity for PostgreSQL persistence.
    /// </summary>
    [Table("voting_options", Schema = "voting")]
    public class VotingOption
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("proposal_id")]
        public Guid ProposalId { get; set; }

        [Required]
        [MaxLength(500)]
        [Column("option_text")]
        public string OptionText { get; set; } = string.Empty;

        [Column("option_order")]
        public int OptionOrder { get; set; } = 1;

        [Column("vote_count")]
        public int VoteCount { get; set; } = 0;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("metadata", TypeName = "jsonb")]
        public string? Metadata { get; set; }

        // Navigation properties
        [ForeignKey("ProposalId")]
        public virtual VotingProposal? Proposal { get; set; }
        public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
    }

    /// <summary>
    /// Vote entity for PostgreSQL persistence.
    /// </summary>
    [Table("votes", Schema = "voting")]
    public class Vote
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("proposal_id")]
        public Guid ProposalId { get; set; }

        [Required]
        [Column("option_id")]
        public Guid OptionId { get; set; }

        [Column("voter_id")]
        public Guid? VoterId { get; set; }

        [MaxLength(255)]
        [Column("voter_identifier")]
        public string? VoterIdentifier { get; set; }

        [Column("vote_weight", TypeName = "decimal(18,8)")]
        public decimal VoteWeight { get; set; } = 1.0m;

        [Column("cast_at")]
        public DateTime CastAt { get; set; } = DateTime.UtcNow;

        [Column("is_valid")]
        public bool IsValid { get; set; } = true;

        [Column("metadata", TypeName = "jsonb")]
        public string? Metadata { get; set; }

        // Navigation properties
        [ForeignKey("ProposalId")]
        public virtual VotingProposal? Proposal { get; set; }

        [ForeignKey("OptionId")]
        public virtual VotingOption? Option { get; set; }
    }

    /// <summary>
    /// Voting result entity for PostgreSQL persistence.
    /// </summary>
    [Table("voting_results", Schema = "voting")]
    public class VotingResult
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("proposal_id")]
        public Guid ProposalId { get; set; }

        [Column("total_votes")]
        public int TotalVotes { get; set; } = 0;

        [Column("total_weight", TypeName = "decimal(18,8)")]
        public decimal TotalWeight { get; set; } = 0;

        [Column("participation_rate", TypeName = "decimal(5,4)")]
        public decimal? ParticipationRate { get; set; }

        [Column("winning_option_id")]
        public Guid? WinningOptionId { get; set; }

        [Column("calculated_at")]
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

        [Column("is_final")]
        public bool IsFinal { get; set; } = false;

        [Column("metadata", TypeName = "jsonb")]
        public string? Metadata { get; set; }

        // Navigation properties
        [ForeignKey("ProposalId")]
        public virtual VotingProposal? Proposal { get; set; }

        [ForeignKey("WinningOptionId")]
        public virtual VotingOption? WinningOption { get; set; }
    }
}