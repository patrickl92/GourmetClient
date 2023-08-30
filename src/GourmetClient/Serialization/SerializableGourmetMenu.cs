namespace GourmetClient.Serialization
{
	using System;
    using System.Linq;
	using Model;

    internal class SerializableGourmetMenu
	{
		public SerializableGourmetMenu()
		{
			// Used for deserialization
        }

		public SerializableGourmetMenu(GourmetMenu menu)
		{
			menu = menu ?? throw new ArgumentNullException(nameof(menu));

            Days = menu.Days.Select(day => new SerializableGourmetMenuDay(day)).ToArray();
        }

		public SerializableGourmetMenuDay[] Days { get; set; }

		public GourmetMenu ToGourmetMenu()
		{
			return new GourmetMenu(Days?.Select(day => day.ToGourmetMenuDay()).ToArray() ?? Array.Empty<GourmetMenuDay>());
		}
	}
}
