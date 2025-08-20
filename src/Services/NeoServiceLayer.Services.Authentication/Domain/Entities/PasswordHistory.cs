using System;

namespace NeoServiceLayer.Services.Authentication.Domain.Entities
{
    public class PasswordHistory
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ChangedBy { get; set; }
        
        // Navigation property
        public virtual User User { get; set; } = null!;
    }
}