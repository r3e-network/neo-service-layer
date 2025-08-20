using System;

namespace NeoServiceLayer.Services.Authentication
{
    public class MfaSettings
    {
        public MfaType Type { get; set; }
        public string Secret { get; set; } = string.Empty;
        public string[] BackupCodes { get; set; } = Array.Empty<string>();
        public bool Enabled { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUsedAt { get; set; }
    }

    public enum MfaType
    {
        None = 0,
        Totp = 1,
        Sms = 2,
        Email = 3
    }
}