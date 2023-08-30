namespace GourmetClient.Serialization
{
	using System;
	using Settings;

    internal class SerializableWindowSettings
	{
		public SerializableWindowSettings()
		{
			// Used for deserialization
		}

		public SerializableWindowSettings(WindowSettings windowSettings)
		{
			windowSettings = windowSettings ?? throw new ArgumentNullException(nameof(windowSettings));

			WindowPositionTop = windowSettings.WindowPositionTop;
			WindowPositionLeft = windowSettings.WindowPositionLeft;
			WindowWidth = windowSettings.WindowWidth;
			WindowHeight = windowSettings.WindowHeight;
		}

		public int WindowPositionTop { get; set; }

		public int WindowPositionLeft { get; set; }

		public int WindowWidth { get; set; }

		public int WindowHeight { get; set; }

		public WindowSettings ToWindowSettings()
		{
			if (WindowWidth < 1 || WindowHeight < 1)
			{
				// Settings are not valid
				return null;
			}

			return new WindowSettings(WindowPositionTop, WindowPositionLeft, WindowWidth, WindowHeight);
		}
	}
}
