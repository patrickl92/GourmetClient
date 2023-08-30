namespace GourmetClient.Settings
{
	public class WindowSettings
	{
		public WindowSettings(int windowPositionTop, int windowPositionLeft, int windowWidth, int windowHeight)
		{
			WindowPositionTop = windowPositionTop;
			WindowPositionLeft = windowPositionLeft;
			WindowWidth = windowWidth;
			WindowHeight = windowHeight;
		}

		public int WindowPositionTop { get; }

		public int WindowPositionLeft { get; }

		public int WindowWidth { get; }

		public int WindowHeight { get; }

		public override bool Equals(object obj)
		{
			var otherWindowSettings = obj as WindowSettings;

			if (otherWindowSettings == null)
			{
				return false;
			}

			return WindowPositionTop == otherWindowSettings.WindowPositionTop &&
				   WindowPositionLeft == otherWindowSettings.WindowPositionLeft &&
				   WindowWidth == otherWindowSettings.WindowWidth &&
				   WindowHeight == otherWindowSettings.WindowHeight;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = WindowPositionTop;
				hashCode = (hashCode * 397) ^ WindowPositionLeft;
				hashCode = (hashCode * 397) ^ WindowWidth;
				hashCode = (hashCode * 397) ^ WindowHeight;
				return hashCode;
			}
		}
	}
}
