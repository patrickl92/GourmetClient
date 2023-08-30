namespace GourmetClient.Serialization
{
	using System;
    using Settings;

    internal class SerializableUpdateSettings
    {
        public SerializableUpdateSettings()
		{
			// Used for deserialization
        }

		public SerializableUpdateSettings(UpdateSettings updateSettings)
		{
			updateSettings = updateSettings ?? throw new ArgumentNullException(nameof(updateSettings));

			CheckForUpdates = updateSettings.CheckForUpdates;
		}

		public bool? CheckForUpdates { get; set; }

		public UpdateSettings ToUpdateSettings()
        {
            var updateSettings = new UpdateSettings();

            if (CheckForUpdates.HasValue)
            {
				updateSettings.CheckForUpdates = CheckForUpdates.Value;
            }

			return updateSettings;
		}
	}
}
