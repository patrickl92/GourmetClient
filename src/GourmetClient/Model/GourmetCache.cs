namespace GourmetClient.Model
{
	using System;

	public class GourmetCache
	{
		public GourmetCache()
			: this(DateTime.MinValue, null, new GourmetMenu(), new OrderedGourmetMenu())
		{
		}

		public GourmetCache(DateTime timestamp, GourmetUserData userData, GourmetMenu menu, OrderedGourmetMenu orderedMenu)
		{
			Menu = menu ?? throw new ArgumentNullException(nameof(menu));
			OrderedMenu = orderedMenu ?? throw new ArgumentNullException(nameof(orderedMenu));
			
			UserData = userData;
			Timestamp = timestamp;
		}

		public DateTime Timestamp { get; }

		public GourmetUserData UserData { get; }

		public GourmetMenu Menu { get; }

		public OrderedGourmetMenu OrderedMenu { get; }
	}
}
