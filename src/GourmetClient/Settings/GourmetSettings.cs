namespace GourmetClient.Settings
{
	public class GourmetSettings
	{
		public GourmetSettings()
		{
			UserSettings = new UserSettings();
			UpdateSettings = new UpdateSettings();
		}

		public UserSettings UserSettings { get; set; }

		public WindowSettings WindowSettings { get; set; }

		public UpdateSettings UpdateSettings { get; set; }
	}
}
