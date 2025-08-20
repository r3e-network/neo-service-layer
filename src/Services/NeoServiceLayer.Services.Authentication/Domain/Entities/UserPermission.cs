using System;

namespace NeoServiceLayer.Services.Authentication.Domain.Entities
{
    public class UserPermission
    {
        public Guid UserId { get; set; }
        public Guid PermissionId { get; set; }
        public DateTime GrantedAt { get; set; }
        public string? GrantedBy { get; set; }
        public DateTime? ExpiresAt { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
    }
}