namespace GourmetClient.Serialization
{
    using System;
    using Settings;

    internal class SerializableGourmetSettings
    {
        public SerializableGourmetSettings()
        {
            // Used for deserialization
        }

        public SerializableGourmetSettings(GourmetSettings settings)
        {
            settings = settings ?? throw new ArgumentNullException(nameof(settings));

            Version = 1;
            UserSettings = new SerializableUserSettings(settings.UserSettings);
            UpdateSettings = new SerializableUpdateSettings(settings.UpdateSettings);

            if (settings.WindowSettings != null)
            {
                WindowSettings = new SerializableWindowSettings(settings.WindowSettings);
            }
        }

        public int? Version { get; set; }

        public SerializableUserSettings UserSettings { get; set; }

        public SerializableWindowSettings WindowSettings { get; set; }

        public SerializableUpdateSettings UpdateSettings { get; set; }

        public GourmetSettings ToGourmetSettings()
        {
            if (Version is not 1)
            {
                throw new InvalidOperationException($"Unsupported version of serialized data: {Version}");
            }

            var settings = new GourmetSettings
            {
                WindowSettings = WindowSettings?.ToWindowSettings()
            };

            var userSettings = UserSettings?.ToUserSettings();
            if (userSettings != null)
            {
                settings.UserSettings = userSettings;
            }

            var updateSettings = UpdateSettings?.ToUpdateSettings();
            if (updateSettings != null)
            {
                settings.UpdateSettings = updateSettings;
            }

            return settings;
        }
    }
}
