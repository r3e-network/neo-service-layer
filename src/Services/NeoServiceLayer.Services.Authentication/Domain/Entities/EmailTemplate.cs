using System;

namespace NeoServiceLayer.Services.Authentication.Domain.Entities
{
    public class EmailTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? TextBody { get; set; }
        public string? HtmlBody { get; set; }
        public string Language { get; set; } = "en";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}