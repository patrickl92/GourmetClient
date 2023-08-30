namespace GourmetClient.Settings
{
    using System;
    using System.IO;
    using System.Text.Json;
    using Serialization;

    public class GourmetSettingsService
    {
        private readonly string _settingsFileName;

        private GourmetSettings _currentSettings;

        public event EventHandler SettingsSaved;

        public GourmetSettingsService()
        {
            _settingsFileName = Path.Combine(App.LocalAppDataPath, "GourmetClientSettings.json");
        }

        public UserSettings GetCurrentUserSettings()
        {
            return GetCurrentSettings().UserSettings;
        }

        public void SaveUserSettings(UserSettings userSettings)
        {
            userSettings = userSettings ?? throw new ArgumentNullException(nameof(userSettings));

            var settings = GetCurrentSettings();
            settings.UserSettings = userSettings;

            SaveSettings(settings);
        }

        public WindowSettings GetCurrentWindowSettings()
        {
            return GetCurrentSettings().WindowSettings;
        }

        public void SaveWindowSettings(WindowSettings windowSettings)
        {
            var settings = GetCurrentSettings();
            settings.WindowSettings = windowSettings;

            SaveSettings(settings);
        }

        public UpdateSettings GetCurrentUpdateSettings()
        {
            return GetCurrentSettings().UpdateSettings;
        }

        public void SaveUpdateSettings(UpdateSettings updateSettings)
        {
            var settings = GetCurrentSettings();
            settings.UpdateSettings = updateSettings;

            SaveSettings(settings);
        }

        private GourmetSettings GetCurrentSettings()
        {
            if (_currentSettings == null)
            {
                _currentSettings = ReadSettingsFromFile();
            }

            return _currentSettings;
        }

        private GourmetSettings ReadSettingsFromFile()
        {
            if (!File.Exists(_settingsFileName))
            {
                return new GourmetSettings();
            }

            SerializableGourmetSettings serializedSettings = null;
            GourmetSettings settings = null;

            try
            {
                using var fileStream = new FileStream(_settingsFileName, FileMode.Open, FileAccess.Read, FileShare.None);
                serializedSettings = JsonSerializer.Deserialize<SerializableGourmetSettings>(fileStream);
            }
            catch (Exception exception) when (exception is IOException || exception is JsonException)
            {
            }

            try
            {
                settings = serializedSettings?.ToGourmetSettings();
            }
            catch (InvalidOperationException)
            {
            }

            return settings ?? new GourmetSettings();
        }

        private void SaveSettings(GourmetSettings settings)
        {
            settings = settings ?? throw new ArgumentNullException(nameof(settings));

            var serializedSettings = new SerializableGourmetSettings(settings);

            try
            {
                var settingsDirectory = Path.GetDirectoryName(_settingsFileName);
                if (settingsDirectory != null && !Directory.Exists(settingsDirectory))
                {
                    Directory.CreateDirectory(settingsDirectory);
                }
                
                using var fileStream = new FileStream(_settingsFileName, FileMode.Create, FileAccess.Write, FileShare.None);
                JsonSerializer.Serialize(fileStream, serializedSettings, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (IOException)
            {
            }

            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
    }
}
