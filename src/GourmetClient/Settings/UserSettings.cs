namespace GourmetClient.Settings
{
    using System;
    using System.Security;

    public record UserSettings
    {
        public string GourmetLoginUsername { get; set; }

        public SecureString GourmetLoginPassword { get; set; }

        public TimeSpan CacheValidity { get; set; } = TimeSpan.FromHours(4);

        public string VentopayUsername { get; set; }

        public SecureString VentopayPassword { get; set; }
    }
}
