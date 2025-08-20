using System;

namespace NeoServiceLayer.Services.Authentication.Domain.Entities
{
    public class BackupCode
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Code { get; set; } = string.Empty;
        public bool IsUsed { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        
        // Navigation property
        public virtual User User { get; set; } = null!;
    }
}